using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans;

namespace PlaygroundService.Grains;

public class PlaygroundGrain : Grain, IPlaygroundGrain
{
    private readonly ILogger<PlaygroundGrain> _logger;

    public PlaygroundGrain(
        ILogger<PlaygroundGrain> logger)
    {
        _logger = logger;
    }

    public async Task<(bool, string)> BuildProject(string directory)
    {
        try
        {
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
                _logger.LogInformation("file name uploaded is: " + file);
            }

            // Create ProcessStartInfo
            ProcessStartInfo psi;

            var directoryTree = PrintDirectoryTree(directory);
            Console.WriteLine("files in extracted path");
            Console.WriteLine(directoryTree);
            try
            {
                psi = new ProcessStartInfo("dotnet", "build " + directory)
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

            _logger.LogInformation("-------------Start read standard output--------------");

            // Read standard output
            try
            {
                using (var sr = proc.StandardOutput)
                {
                    while (!sr.EndOfStream)
                    {
                        _logger.LogInformation(sr.ReadLine());
                    }
                }
            }
            catch (Exception e)
            {
                return (false, "Error reading standard output: " + e.Message);
            }

            _logger.LogInformation("---------------Read end------------------");

            return (true, "Success");
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