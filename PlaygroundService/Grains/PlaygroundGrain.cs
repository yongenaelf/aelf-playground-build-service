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
    private readonly ContractSetting _contractSetting;
    
    public PlaygroundGrain(
        IOptions<ContractSetting> contractSetting,
        ILogger<PlaygroundGrain> logger)
    {
        _logger = logger;
        _contractSetting = contractSetting.Value;
    }
    
    public async Task<bool> GenerateContract(string projectName, string contractClass, string contractFileName, string stateClass, string stateFileName, string proto, string protoFileName)
    {
        if (!string.IsNullOrEmpty(contractClass))
        {
            File.WriteAllText(_contractSetting.ContractClassPath + contractFileName, contractClass);
        }
        if (!string.IsNullOrEmpty(stateClass))
        {
            File.WriteAllText(_contractSetting.StateClassPath + stateFileName, stateClass);
        }
        if (!string.IsNullOrEmpty(proto))
        {
            File.WriteAllText(_contractSetting.ProtoPath + protoFileName, proto);
        }

        // Create ProcessStartInfo, and the params of shell
        var psi = new ProcessStartInfo("dotnet", "build " + _contractSetting.ContractClassPath + projectName) {RedirectStandardOutput = true};        // Run psi
        var proc=Process.Start(psi);
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