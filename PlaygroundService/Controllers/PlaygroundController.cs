using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Orleans;
using PlaygroundService.Grains;

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
            _logger.LogInformation("templates  - GetTemplates started userId: " + userId + " time: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            var templateConfGrain = _client.GetGrain<IPlaygroundGrain>(userId);
            var templateConf = await templateConfGrain.GetTemplates();

            return Ok(templateConf);
        }
        
        [HttpGet("template")]
        public async Task<IActionResult> GetTemplateInfo([FromQuery] string template, [FromQuery] string projectName)
        {
            var userId = Guid.NewGuid().ToString();
            _logger.LogInformation("templates  - GetTemplateInfo started userId: " + userId + " time: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            var codeGeneratorGrain = _client.GetGrain<IPlaygroundGrain>(userId);
            var zipFilePath = await codeGeneratorGrain.GenerateTemplate(template, projectName);
            return Content(zipFilePath);
        }

        [HttpPost("build")]
        public async Task<IActionResult> Build(IFormFile contractFiles)
        {
            _logger.LogInformation("Build  - Build started time: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            return await BuildService(contractFiles);
        }

        public byte[] Read(string path)
        {
            try
            {
                byte[] code = System.IO.File.ReadAllBytes(path);
                return code;
            }
            catch (Exception e)
            {
                return null;
            }
        }


        private async Task<(bool Success, string ExtractPath, string ZipFilePath, string ErrorMessage)> ExtractZipFileAsync(IFormFile contractFiles)
        {
            var tempPath = Path.GetTempPath();
            var zipFile = Path.Combine(tempPath, contractFiles.FileName);
            _logger.LogInformation("PlaygroundController - Zip file path: " + zipFile);

            await using var zipStream = new FileStream(zipFile, FileMode.Create);
            await contractFiles.CopyToAsync(zipStream);
            await zipStream.FlushAsync(); // Ensure all data is written to the file

            _logger.LogInformation("PlaygroundController - Zip file saved to disk at: " + zipFile);

            try
            {
                using var archive = ZipFile.OpenRead(zipFile);
                // If we get here, the file is a valid zip file
            }
            catch (InvalidDataException)
            {
                _logger.LogError("PlaygroundController - The uploaded file is not a valid zip file at: " + zipFile);
                return (false, string.Empty, string.Empty, "The uploaded file is not a valid zip file.");
            }

            var extractPath = Path.Combine(tempPath, Path.GetFileNameWithoutExtension(contractFiles.FileName), Guid.NewGuid().ToString());
            _logger.LogInformation("PlaygroundController - ExtractPath or destination directory where files are extracted is: " + extractPath);

            using (var archive = new ZipArchive(zipStream, ZipArchiveMode.Read))
            {
                foreach (var entry in archive.Entries)
                {
                    var destinationPath = Path.GetFullPath(Path.Combine(extractPath, entry.FullName));

                    if (!destinationPath.StartsWith(extractPath, StringComparison.Ordinal))
                    {
                        _logger.LogError("PlaygroundController - Invalid entry in the zip file: " + entry.FullName);
                        return (false, string.Empty, string.Empty, $"Invalid entry in the zip file: {entry.FullName}");
                    }

                    var destinationDirectory = Path.GetDirectoryName(destinationPath);
                    if (!Directory.Exists(destinationDirectory))
                    {
                        Directory.CreateDirectory(destinationDirectory);
                    }

                    try
                    {
                        entry.ExtractToFile(destinationPath, overwrite: true);
                    }
                    catch (UnauthorizedAccessException ex)
                    {
                        _logger.LogError("PlaygroundController - UnauthorizedAccessException: " + ex);
                        return (false, string.Empty, string.Empty, $"Unauthorized access: {ex.Message}");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError("PlaygroundController - Exception: " + ex);
                        return (false, string.Empty, string.Empty, $"Error extracting file: {ex.Message}");
                    }
                }
            }

            _logger.LogInformation("PlaygroundController - Files extracted to disk at: " + extractPath);
            return (true, extractPath, zipFile, string.Empty);
        }

        public async Task<IActionResult> BuildService(IFormFile contractFiles)
        {
            _logger.LogInformation("PlaygroundController - Build method started for: " + contractFiles.FileName);

            var (success, extractPath, zipFilePath, errorMessage) = await ExtractZipFileAsync(contractFiles);
            if (!success)
            {
                return BadRequest(new PlaygroundSchema.PlaygroundContractGenerateResponse
                {
                    Success = false,
                    Message = "PlaygroundController BuildService - " + errorMessage
                });
            }

            var csprojFiles = Directory.GetFiles(extractPath, "*.csproj", SearchOption.AllDirectories);
            if (csprojFiles.Length == 0)
            {
                return BadRequest(new PlaygroundSchema.PlaygroundContractGenerateResponse
                {
                    Success = false,
                    Message = "PlaygroundController - No .csproj file found in the uploaded zip file"
                });
            }

            var guid = Guid.NewGuid();
            var codeGeneratorGrain = _client.GetGrain<IPlaygroundGrain>(guid.ToString());
            var (buildSuccess, message) = await codeGeneratorGrain.BuildProject(extractPath);

            if (buildSuccess)
            {
                _logger.LogInformation("PlaygroundController - BuildProject method returned success: " + message);
                var pathToDll = message;

                if (!System.IO.File.Exists(pathToDll))
                {
                    _logger.LogError("PlaygroundController - BuildProject method returned error: file not exist " + message);
                    return BadRequest(new PlaygroundSchema.PlaygroundContractGenerateResponse
                    {
                        Success = buildSuccess,
                        Message = message
                    });
                }

                var res = Content(Convert.ToBase64String(Read(pathToDll)));
                _logger.LogInformation("PlaygroundController - BuildProject method over");
                await codeGeneratorGrain.DelData(zipFilePath, extractPath);

                return res;
            }

            _logger.LogError("PlaygroundController - BuildProject method returned error: " + message);
            return BadRequest(new PlaygroundSchema.PlaygroundContractGenerateResponse
            {
                Success = buildSuccess,
                Message = message
            });
        }

        [HttpPost("build-test")]
        public async Task<IActionResult> BuildTest(IFormFile contractFiles)
        {
            _logger.LogInformation("Build  - Build started time: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            return await TestService(contractFiles);
        }

        public async Task<IActionResult> TestService(IFormFile contractFiles)
        {
            _logger.LogInformation("PlaygroundController - Test method started for: " + contractFiles.FileName);

            var (success, extractPath, zipFilePath, errorMessage) = await ExtractZipFileAsync(contractFiles);
            if (!success)
            {
                return BadRequest(new PlaygroundSchema.PlaygroundContractGenerateResponse
                {
                    Success = false,
                    Message = "PlaygroundController TestService - " + errorMessage
                });
            }

            var csprojFiles = Directory.GetFiles(extractPath, "*.csproj", SearchOption.AllDirectories);
            if (csprojFiles.Length == 0)
            {
                return BadRequest(new PlaygroundSchema.PlaygroundContractGenerateResponse
                {
                    Success = false,
                    Message = "PlaygroundController - No .csproj file found in the uploaded zip file"
                });
            }

            var guid = Guid.NewGuid();
            var codeGeneratorGrain = _client.GetGrain<IPlaygroundGrain>(guid.ToString());
            var (testSuccess, message) = await codeGeneratorGrain.TestProject(extractPath);

            if (testSuccess)
            {
                _logger.LogInformation("PlaygroundController - TestService method returned success: " + message);
                var pathToDll = message;

                if (!System.IO.File.Exists(pathToDll))
                {
                    _logger.LogError("PlaygroundController - TestService method returned error: file not exist " + message);
                    return BadRequest(new PlaygroundSchema.PlaygroundContractGenerateResponse
                    {
                        Success = testSuccess,
                        Message = message
                    });
                }

                var res = Content(Convert.ToBase64String(Read(pathToDll)));
                _logger.LogInformation("PlaygroundController - TestService method over");
                await codeGeneratorGrain.DelData(zipFilePath, extractPath);

                return res;
            }

            _logger.LogError("PlaygroundController - TestService method returned error: " + message);
            return BadRequest(new PlaygroundSchema.PlaygroundContractGenerateResponse
            {
                Success = testSuccess,
                Message = message
            });
        }
    }
}