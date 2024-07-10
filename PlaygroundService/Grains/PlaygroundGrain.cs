using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Concurrency;

namespace PlaygroundService.Grains;
public class PlaygroundGrain : Grain, IPlaygroundGrain
{
    private readonly ILogger<PlaygroundGrain> _logger;

    public PlaygroundGrain(
        ILogger<PlaygroundGrain> logger)
    {
        _logger = logger;
    }

    public async Task<List<string>> GetTemplateConfig()
    {
        var templateCon = new List<string> { "item1", "item2", "item3" };
        return templateCon;
    }
    
    public async Task <string> GenerateTemplate(string template, string templateName)
    {
        var tempPath = Path.GetTempPath();
        var templatePath = Path.Combine(tempPath, Path.GetFileNameWithoutExtension(template), Guid.NewGuid().ToString());
        var sourceFolder = templatePath + "/code";
        var command = "dotnet new --output " + templatePath + "/code " + template + " -n " + templateName;
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
                process.WaitForExit(); // Waiting for process to exit
            }
            _logger.LogInformation("PlayGroundGrain GenerateZip  dotnet new end command: " + command + " time: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
        
            ZipFile.CreateFromDirectory(sourceFolder, templatePath + "/src.zip");
            var zipFile = Convert.ToBase64String(Read(templatePath + "/src.zip"));
            _logger.LogInformation("PlayGroundGrain GenerateZip  zip end templatePath: " + templatePath + " time: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            // DeactivateOnIdle();
            await DelData(templatePath + "/src.zip", templatePath);
            return zipFile;
        }
        catch (Exception ex)
        {
            _logger.LogError("PlayGroundGrain GenerateZip exception time: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + ex.Message);
            return "";
        }
    }
    
    public byte[] Read(string path)
    {
        try
        {
            byte[] code = File.ReadAllBytes(path);
            return code;
        }
        catch (Exception e)
        {
            return null;
        }
    }
    
    
    
    public async Task <bool> DelData(string zipFile, string extractPath)
    {
        // Check if the fild exists
        try
        {
            if (File.Exists(zipFile))
            {
                // Using the Process class to execute the rm command to delete files
                ProcessStartInfo startInfo = new ProcessStartInfo("rm", zipFile);
                Process.Start(startInfo);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError("PlayGroundGrain DelData del zipFile fail time: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + ex.Message);
        }
        try
        {
            // Check if the folder exists
            if (Directory.Exists(extractPath))
            {
                // Recursively delete folders and all their contents
                Directory.Delete(extractPath, true);
                return true;
            }
            else
            {
                _logger.LogInformation("PlayGroundGrain DelData del dllPath fail time: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            }
        }
        catch (Exception ex)
        {
            // Handle possible exceptions, such as insufficient permissions, folders being occupied by other processes, etc
            _logger.LogError("PlayGroundGrain DelData del dllPath fail time: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + ex.Message);
        }
        return false;
    }

    public async Task<(bool, string)> BuildProject(string directory)
    {
        string projectDirectory = directory;
        try
        {
            _logger.LogInformation("PlayGroundGrain BuildProject begin time: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            // Check if directory exists
            if (!Directory.Exists(directory))
            {
                return (false, "Directory does not exist: " + directory);
            }

            // Get all files in the directory
            string[] files;
            try
            {
                files = Directory.GetFiles(directory, "*.*", SearchOption.AllDirectories);
            }
            catch (Exception e)
            {
                return (false, "Error getting files from directory: " + e.Message);
            }

            // Print all files in the directory
            foreach (var file in files)
            {
                _logger.LogInformation("file name uploaded is: " + file + " time:" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            }

            // Create ProcessStartInfo
            ProcessStartInfo psi;

            var directoryTree = PrintDirectoryTree(directory);
            Console.WriteLine("files in extracted path");
            Console.WriteLine(directoryTree);
            
            // before running the process dotnet build check if the directory has a .csproj file and .sln file
            var csprojFiles = files.Where(file => file.EndsWith(".csproj")).ToList();
            var slnFiles = files.Where(file => file.EndsWith(".sln")).ToList();
            
            if (csprojFiles.Count == 0)
            {
                return (false, "No .csproj file found in the directory");
            }
            
            // if (slnFiles.Count == 0)
            // {
            //     return (false, "No .sln file found in the directory");
            // }
            
            try
            {
                // Check if directory exists
                if (!Directory.Exists(directory))
                {
                    return (false, "Directory does not exist: " + directory);
                }

                // Get the first subdirectory
                var subdirectory = Directory.GetDirectories(directory).FirstOrDefault();
                if (subdirectory == null)
                {
                    return (false, "No subdirectories found in the directory");
                }

                // Use the subdirectory as the project directory
                projectDirectory = subdirectory;
                psi = new ProcessStartInfo("dotnet", "build " + projectDirectory)
                {
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };
            }
            catch (Exception e)
            {
                return (false, "Error creating ProcessStartInfo: " + e.Message);
            }

            // Run psi
            _logger.LogInformation("PlaygroundGrains BuildProject before dotnet build " + " time:" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            Process proc;
            try
            {
                proc = Process.Start(psi);
                if (proc == null)
                {
                    return (false, "Process could not be started.");
                }

                proc.WaitForExit();

                string errorMessage;
                using (var sr = proc.StandardError)
                {
                    errorMessage = await sr.ReadToEndAsync();
                }
                _logger.LogInformation("PlaygroundGrains BuildProject after dotnet build " + " time:" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

                if (string.IsNullOrEmpty(errorMessage))
                {
                    using (var sr = proc.StandardOutput)
                    {
                        errorMessage = await sr.ReadToEndAsync();
                    }
                }

                if (proc.ExitCode != 0)
                {
                    _logger.LogError("Error executing process: " + errorMessage);
                    return (false, "Error executing process: " + errorMessage);
                }
            }
            catch (Exception e)
            {
                return (false, "Error starting process: " + e.Message);
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
        var isLast = directoryInfo.Parent.GetDirectories().Last().Equals(directoryInfo);

        stringBuilder.AppendLine($"{prefix}{(isLast ? "└── " : "├── ")}{directoryInfo.Name}");

        var newPrefix = prefix + (isLast ? "    " : "│   ");

        foreach (var fileInfo in directoryInfo.GetFiles())
        {
            isLast = fileInfo.Directory.GetFiles().Last().Equals(fileInfo);
            stringBuilder.AppendLine($"{newPrefix}{(isLast ? "└── " : "├── ")}{fileInfo.Name}");
        }

        foreach (var subDirectoryInfo in directoryInfo.GetDirectories())
        {
            PrintDirectory(subDirectoryInfo, newPrefix, indent, stringBuilder);
        }
    }
}