using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Text;

namespace PlaygroundService.Utilities;

public class ProcessHelper
{
    public static async Task<(bool, string)> RunDotnetCommand(string directory, string command)
    {
        if (!Directory.Exists(directory))
            return (false, $"Directory does not exist: {directory}");

        var csprojFiles = Directory.GetFiles(directory, "*.csproj", SearchOption.AllDirectories).ToList();
        if (!csprojFiles.Any())
            return (false, "No .csproj file found in the directory");

        var projectDirectory = Path.GetDirectoryName(csprojFiles.First());
        if (!Directory.Exists(projectDirectory))
            return (false, $"Project directory does not exist: {projectDirectory}");

        try
        {
            var result = await RunProcess("dotnet", command, projectDirectory);
            return (true, result);
        }
        catch (Exception e)
        {
            return (false, e.Message);
        }
    }

    private static async Task<string> RunProcess(string fileName, string arguments, string workingDirectory = null)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            EnvironmentVariables ={
                                    ["LANG"] = "en_US.UTF-8",
                                    ["LC_ALL"] = "en_US.UTF-8"
                                }
        };

        using var process = new Process { StartInfo = startInfo };
        var output = new StringBuilder();
        var error = new StringBuilder();
        process.OutputDataReceived += (sender, args) => output.AppendLine(args.Data);
        process.ErrorDataReceived += (sender, args) => error.AppendLine(args.Data);

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        await process.WaitForExitAsync();

        var allOutput = output.ToString() + error.ToString(); // Combine stdout and stderr
        if (process.ExitCode != 0)
        {
            return $"Process exited with code {process.ExitCode}. Details: {allOutput}";
        }

        return allOutput;
    }

}