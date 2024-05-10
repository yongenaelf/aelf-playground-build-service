using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using ProofService.interfaces;

namespace ProofService.controllers;

[Route("playground")]
[ApiController]
public class PlaygroundController : ControllerBase
{
    private readonly ILogger<PlaygroundController> _logger;
    private readonly ContractSetting _contractSetting;
    
    public PlaygroundController(ILogger<PlaygroundController> logger, ContractSetting contractSetting)
    {
        _logger = logger;
        _contractSetting = contractSetting;
    }

    [HttpPost("generate")]
    public async Task<IActionResult> GenerateContract(PlaygroundSchema.PlaygroundContractGenerateRequest request)
    {
        if (!string.IsNullOrEmpty(request.contractClass))
        {
            System.IO.File.WriteAllText(_contractSetting.ContractClassPath + "Test.cs", request.contractClass);
        }
        if (!string.IsNullOrEmpty(request.stateClass))
        {
            System.IO.File.WriteAllText(_contractSetting.StateClassPath + "TestState.cs", request.stateClass);
        }
        if (!string.IsNullOrEmpty(request.proto))
        {
            System.IO.File.WriteAllText(_contractSetting.ProtoPath + "test.proto", request.proto);
        }
        
        // Create ProcessStartInfo, and the params of shell
        var psi = new ProcessStartInfo("dotnet", "build " + _contractSetting.ContractClassPath + "HelloWorld.csproj") {RedirectStandardOutput = true};
        // Run psi
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
        return StatusCode(200, "generate successful");
    }

}