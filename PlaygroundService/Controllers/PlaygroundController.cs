using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Orleans;
using PlaygroundService.Dtos;
using PlaygroundService.Grains;
using PlaygroundService.Utilities;

namespace PlaygroundService.Controllers
{
    [ApiController]
    [Route("playground")]
    public class PlaygroundController : ControllerBase
    {
        private readonly IClusterClient _client;
        private readonly ILogger<PlaygroundController> _logger;

        public PlaygroundController(IClusterClient client, ILogger<PlaygroundController> logger)
        {
            _client = client;
            _logger = logger;
        }
        
        [HttpGet("templates")]
        public async Task<IActionResult> GetTemplates()
        {
            var userId = Guid.NewGuid().ToString();
            _logger.LogInformation("templates  - GetTemplates started userId: " +userId+ " time: "+ DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            var templateConfGrain = _client.GetGrain<IPlaygroundGrain>(userId);
            var templateConf = await templateConfGrain.GetTemplates();

            return Ok(templateConf); 
        }
        
        [HttpGet("template")]
        public async Task<IActionResult> GetTemplateInfo([FromQuery] string template, [FromQuery] string projectName)
        {
            var userId = Guid.NewGuid().ToString();
            _logger.LogInformation("templates  - GetTemplateInfo started userId: " + userId+ " time: "+ DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            var codeGeneratorGrain = _client.GetGrain<IPlaygroundGrain>(userId);
            var zipFilePath = await codeGeneratorGrain.GenerateTemplate(template, projectName);
            return Content(zipFilePath);
        }

        [HttpPost("build")]
        public async Task<IActionResult> Build(IFormFile contractFiles)
        {
            _logger.LogInformation("Build  - Build started time: "+ DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            return await BuildService(contractFiles);
        }

        public async Task<IActionResult> BuildService(IFormFile contractFiles)
        {
            _logger.LogInformation("PlaygroundController - Build method started for: "+ contractFiles.FileName + " time:" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")) ;
            var startTime = DateTime.Now;
            
            var buildDto = new BuildDto
            {
                ZipFile = await contractFiles.ToBytes(),
                Filename = contractFiles.FileName
            };
            
            var guid = Guid.NewGuid();
            var codeGeneratorGrain = _client.GetGrain<IPlaygroundGrain>(guid.ToString());
            var (success, message) = await codeGeneratorGrain.BuildProject(buildDto);

            if (success)
            {
                _logger.LogInformation("PlaygroundController - BuildProject method returned success: " + message  + " time:" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                var pathToDll = message;
                var fileName = Path.GetFileName(pathToDll);
                _logger.LogInformation("PlaygroundController - Files return fileName:" + pathToDll);
                if (!System.IO.File.Exists(pathToDll))
                {
                    _logger.LogError("PlaygroundController - BuildProject method returned error: file not exist " + message  + " time:" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                    return BadRequest(new PlaygroundSchema.PlaygroundContractGenerateResponse
                    {
                        Success = success,
                        Message = message
                    });
                }

                var dllBytes = BytesExtension.Read(pathToDll);
                if (dllBytes == null)
                {
                    _logger.LogError("PlaygroundController - BuildProject method returned error: dllBytes is null " + message  + " time:" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                    return BadRequest(new PlaygroundSchema.PlaygroundContractGenerateResponse
                    {
                        Success = false,
                        Message = "Error in dll."
                    });
                }
                
                var res = Content(Convert.ToBase64String(dllBytes));
                
                _logger.LogInformation("PlaygroundController - BuildProject method over: " + " time:" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                var endTime = DateTime.Now;
                
                _logger.LogInformation("PlaygroundController - BuildProject method took: " + (endTime - startTime).TotalSeconds + " seconds");
                return res;
            }
            
            _logger.LogError("PlaygroundController - BuildProject method returned error: " + message);
            return BadRequest(new PlaygroundSchema.PlaygroundContractGenerateResponse
            {
                Success = success,
                Message = message
            });
        }
        
        [HttpPost("test")]
        public async Task<IActionResult> Test(IFormFile contractFiles)
        {
            _logger.LogInformation("Test  - Test started time: "+ DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            return await TestService(contractFiles);
        }
        
        public async Task<IActionResult> TestService(IFormFile contractFiles)
        {
            _logger.LogInformation("PlaygroundController - Test method started for: "+ contractFiles.FileName + " time:" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")) ;
            var startTime = DateTime.Now;
            
            var buildDto = new BuildDto
            {
                ZipFile = await contractFiles.ToBytes(),
                Filename = contractFiles.FileName
            };
            
            var guid = Guid.NewGuid();
            var codeGeneratorGrain = _client.GetGrain<IPlaygroundGrain>(guid.ToString());
            var (success, message) = await codeGeneratorGrain.TestProject(buildDto);

            if (success)
            {
                _logger.LogInformation("PlaygroundController - BuildProject method returned success: " + message  + " time:" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                var pathToDll = message;
                var fileName = Path.GetFileName(pathToDll);
                _logger.LogInformation("PlaygroundController - Files return fileName:" + pathToDll);
                if (!System.IO.File.Exists(pathToDll))
                {
                    _logger.LogError("PlaygroundController - BuildProject method returned error: file not exist " + message  + " time:" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                    return BadRequest(new PlaygroundSchema.PlaygroundContractGenerateResponse
                    {
                        Success = success,
                        Message = message
                    });
                }

                var dllBytes = BytesExtension.Read(pathToDll);
                if (dllBytes == null)
                {
                    _logger.LogError("PlaygroundController - BuildProject method returned error: dllBytes is null " + message  + " time:" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                    return BadRequest(new PlaygroundSchema.PlaygroundContractGenerateResponse
                    {
                        Success = false,
                        Message = "Error in dll."
                    });
                }
                
                var res = Content(Convert.ToBase64String(dllBytes));
                
                _logger.LogInformation("PlaygroundController - BuildProject method over: " + " time:" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                var endTime = DateTime.Now;
                
                _logger.LogInformation("PlaygroundController - BuildProject method took: " + (endTime - startTime).TotalSeconds + " seconds");
                return res;
            }
            
            _logger.LogError("PlaygroundController - BuildProject method returned error: " + message);
            return BadRequest(new PlaygroundSchema.PlaygroundContractGenerateResponse
            {
                Success = success,
                Message = message
            });
        }
    }
}