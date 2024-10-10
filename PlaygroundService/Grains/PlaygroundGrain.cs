using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Concurrency;
using PlaygroundService.Dtos;
using PlaygroundService.Utilities;

namespace PlaygroundService.Grains;

[StatelessWorker(10)] // max 10 activations per silo
public class PlaygroundGrain : Grain, IPlaygroundGrain
{
    private readonly ILogger<PlaygroundGrain> _logger;
    private string _workspacePath;

    public PlaygroundGrain(
        ILogger<PlaygroundGrain> logger)
    {
        _logger = logger;
    }
    
    public override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        await base.OnActivateAsync(cancellationToken);
        CreateWorkspaceDirectory();
    }
    
    public override async Task OnDeactivateAsync(DeactivationReason reason, CancellationToken cancellationToken)
    {
        await base.OnDeactivateAsync(reason, cancellationToken);
        DeleteWorkspaceDirectory();
    }

    public async Task<List<string>> GetTemplates()
    {
        var templateCon = new List<string> { "aelf", "aelf-lottery", "aelf-nft-sale", "aelf-simple-dao"};
        
        //TODO: refactor to use AOP
        DeactivateOnIdle();
        
        return templateCon;
    }

    public async Task <string> GenerateTemplate(string template, string templateName)
    {
        var zip = await GenerateTemplateZip(template, templateName);
            
        DeactivateOnIdle();

        return zip;
    }

    public async Task<(bool, string)> BuildProject(ZipFileDto dto)
    {
        var (success, message) = await ExtractThen(dto, async () => await Build(_workspacePath));
        
        DeactivateOnIdle();
        
        return (success, message);
    }
    
    public async Task<(bool, string)> TestProject(ZipFileDto dto)
    {
        var (success, message) = await ExtractThen(dto, async () => await Test(_workspacePath));
        
        DeactivateOnIdle();
        
        return (success, message);
    }
    
    private async Task <string> GenerateTemplateZip(string template, string templateName)
    {
        var sourceFolder = _workspacePath + "/code";
        var command = "dotnet new " + template + " --output " + _workspacePath + "/code -n " + templateName;
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "/bin/bash", // We use bash to execute commands
                Arguments = $"-c \"{command}\"", // -Option c allows bash to execute a string command
                UseShellExecute = false,
                RedirectStandardOutput = true, // If necessary, you can redirect the output to the C # program
                CreateNoWindow = true // Do not create a new window
            };

            // Using the Process class to start a process
            using (var process = new Process { StartInfo = startInfo })
            {
                process.Start(); // start process
                // If necessary,can read the output of the process
                // string output = process.StandardOutput.ReadToEnd();
                await process.WaitForExitAsync(); // Waiting for process to exit
            }
            _logger.LogInformation("PlayGroundGrain GenerateZip  dotnet new end command: " + command + " time: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
        
            ZipFile.CreateFromDirectory(sourceFolder, _workspacePath + "/code.zip");

            var codeZip = BytesExtension.Read(_workspacePath + "/code.zip");
            if (codeZip == null)
            {
                _logger.LogError("Error: code zip cannot be found. time:" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                return "";
            }
            
            var zipFile = Convert.ToBase64String(codeZip);
            _logger.LogInformation("PlayGroundGrain GenerateZip  zip end templatePath: " + _workspacePath + " time: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            
            return zipFile;
        }
        catch (Exception ex)
        {
            _logger.LogError("PlayGroundGrain GenerateZip exception time: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + ex.Message);
            return "";
        }
    }

    private async Task<(bool, string)> ExtractThen(ZipFileDto dto, Func<Task<(bool, string)>> action)
    {
        var zipBytes = dto.ZipFile;
        
        if (zipBytes == null)
        {
            return (false, "The uploaded file is not a valid zip file.");
        }
        if (string.IsNullOrEmpty(dto.Filename))
        {
            return (false, "The uploaded file does not have a filename.");
        }
        
        try
        {
            zipBytes.ExtractTo(_workspacePath);
        }
        catch (Exception e)
        {
            _logger.LogError($"PlaygroundController - Error extracting zip file - {e.Message}"  + " time:" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            return (false, "Error extracting zip file.");
        }

        return await action();
    }

    private void CreateWorkspaceDirectory()
    {
        _workspacePath = Path.Combine(Path.GetTempPath(), this.GetGrainId().ToString());
        if (!Directory.Exists(_workspacePath))
        {
            Directory.CreateDirectory(_workspacePath);
        }
    }

    private async Task<(bool, string)> Build(string directory)
    {
        try
        {
            _logger.LogInformation("PlayGroundGrain BuildProject begin time: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            // before running the process dotnet build check if the directory has a .csproj file and .sln file
            List<string> csprojFiles;
            try 
            {
                csprojFiles = GetCsprojFiles(directory);
            }
            catch (Exception e)
            {
                return (false, e.Message);
            }

            var projectDirectory = "";
            try
            {
                // get extracted path of the first .csproj file
                var csprojPath = Path.GetDirectoryName(csprojFiles[0]);
                projectDirectory = csprojPath;
                
                if (!Directory.Exists(projectDirectory))
                {
                    return (false, "CS Project Directory does not exist: " + projectDirectory);
                }
                
                var (success, message) = await ProcessHelper.RunDotnetCommand(projectDirectory, "build");
                if (!success)
                {
                    return (false, message);
                }
            }
            catch (Exception e)
            {
                return (false, "Error creating ProcessStartInfo: " + e.Message);
            }
            
            // as the build is successful. lookup for the dll file in the bin folder 
            // dll file will be under one of the subdirectories of the projectDirectory
            var binDirectory = Path.Combine(projectDirectory, "bin");
            var dllFiles = Directory.GetFiles(binDirectory, "*.dll.patched", SearchOption.AllDirectories);
            
            //print dll file name
            foreach (var dllFile in dllFiles)
            {
                _logger.LogInformation("dll file name is: " + dllFile);
            }
            
            if (dllFiles.Length == 0)
            {
                return (false, "No .dll file found in the bin directory");
            }
            
            //extract the first dll file entry and return it in response
            var dllFileName = Path.GetFileName(dllFiles[0]);
            
            _logger.LogInformation("dll file name is: " + dllFileName + " time:" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

            return (true, dllFiles[0]);
        }
        catch (Exception e)
        {
            _logger.LogError(e.Message);
            return (false, e.Message);
        }
    }
    
    private async Task<(bool, string)> Test(string directory)
    {
        _logger.LogInformation("PlayGroundGrain BuildProject begin time: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
        // before running the process dotnet build check if the directory has a .csproj file and .sln file
        List<string> csprojFiles;
        try 
        {
            csprojFiles = GetCsprojFiles(directory);
        }
        catch (Exception e)
        {
            return (false, e.Message);
        }

        try
        {
            var testCsprojFiles = csprojFiles.Where(csprojFile => csprojFile.ToLower().Contains("test")).ToList();
            if (testCsprojFiles.Count == 0)
            {
                return (false, "No .csproj file found with test in its name for unit testing.");
            }
            
            // get extracted path of the first .csproj file
            var csprojPath = Path.GetDirectoryName(testCsprojFiles[0]);
            
            if (!Directory.Exists(csprojPath))
            {
                return (false, "CS Project Directory does not exist: " + csprojPath);
            }
            
            return await ProcessHelper.RunDotnetCommand(csprojPath, "test --logger \\\"console;verbosity=detailed\\\"");
        }
        catch (Exception e)
        {
            return (false, "Error running dotnet: " + e.Message);
        }
    }

    private List<string> GetCsprojFiles(string directory)
    {
        // Check if directory exists
        if (!Directory.Exists(directory))
        {
            throw new Exception("Directory does not exist: " + directory);
        }

        // Get all files in the directory
        string[] files;
        try
        {
            files = Directory.GetFiles(directory, "*.*", SearchOption.AllDirectories);
        }
        catch (Exception e)
        {
            throw new Exception("Error getting files from directory: " + e.Message);
        }

        // Print all files in the directory
        foreach (var file in files)
        {
            _logger.LogInformation("file name uploaded is: " + file + " time:" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
        }

        var directoryTree = PrintDirectoryTree(directory);
        Console.WriteLine("files in extracted path");
        Console.WriteLine(directoryTree);
        
        // before running the process dotnet build check if the directory has a .csproj file and .sln file
        var csprojFiles = files.Where(file => file.EndsWith(".csproj")).ToList();
        
        if (csprojFiles.Count == 0)
        {
            throw new Exception("No .csproj file found in the directory");
        }
        
        return csprojFiles;
    }

    private string PrintDirectoryTree(string directoryPath)
    {
        var indent = new string(' ', 4);
        var directoryInfo = new DirectoryInfo(directoryPath);
        var stringBuilder = new StringBuilder();

        PrintDirectory(directoryInfo, string.Empty, indent, stringBuilder);

        return stringBuilder.ToString();
    }

    private void PrintDirectory(DirectoryInfo directoryInfo, string prefix, string indent, StringBuilder stringBuilder)
    {
        if(directoryInfo.Parent == null)
        {
            return;
        }
        
        var isLast = directoryInfo.Parent.GetDirectories().Last().Equals(directoryInfo);

        stringBuilder.AppendLine($"{prefix}{(isLast ? "└── " : "├── ")}{directoryInfo.Name}");

        var newPrefix = prefix + (isLast ? "    " : "│   ");

        foreach (var fileInfo in directoryInfo.GetFiles())
        {
            if (fileInfo.Directory == null)
            {
                continue;
            }

            isLast = fileInfo.Directory.GetFiles().Last().Equals(fileInfo);
            stringBuilder.AppendLine($"{newPrefix}{(isLast ? "└── " : "├── ")}{fileInfo.Name}");
        }

        foreach (var subDirectoryInfo in directoryInfo.GetDirectories())
        {
            PrintDirectory(subDirectoryInfo, newPrefix, indent, stringBuilder);
        }
    }
    
    private void DeleteWorkspaceDirectory()
    {
        if (Directory.Exists(_workspacePath))
        {
            Directory.Delete(_workspacePath, true);
        }
    }
}