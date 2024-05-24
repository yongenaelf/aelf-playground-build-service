using System.Diagnostics;
using System.IO;
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
    
    public async Task<bool> GenerateContract(string directory)
    {
        // Create ProcessStartInfo, and the params of shell
        var psi = new ProcessStartInfo("dotnet", "build " + directory) {RedirectStandardOutput = true};

        // Run psi
        var proc = Process.Start(psi);
        if (proc == null)
        {
            _logger.LogError("Can not exec.");
        }
        else
        {
            _logger.LogInformation("-------------Start read standard output--------------");
            using (var sr = proc.StandardOutput)
            {
                while (!sr.EndOfStream)
                {
                    _logger.LogInformation(sr.ReadLine());
                }

                if (!proc.HasExited)
                {
                    proc.Kill();
                }
            }
            _logger.LogInformation("---------------Read end------------------");
        }

        return await Task.FromResult(true);
    }
}