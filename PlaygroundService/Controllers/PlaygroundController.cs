using System;
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

        [HttpPost("build")]
        public async Task<IActionResult> Build(IFormFile contractFiles)
        {
            _logger.LogInformation("PlaygroundController - Build method started for: "+ contractFiles.FileName);
            
            var tempPath = Path.GetTempPath();
            var zipPath = Path.Combine(tempPath, contractFiles.FileName);
            
            _logger.LogInformation("PlaygroundController - Zip file path: " + zipPath);

            await using var zipStream = new FileStream(zipPath, FileMode.Create);
            await contractFiles.CopyToAsync(zipStream);
            await zipStream.FlushAsync(); // Ensure all data is written to the file
            
            _logger.LogInformation("PlaygroundController - Zip file saved to disk");
            
            try
            {
                using var archive = ZipFile.OpenRead(zipPath);
                // If we get here, the file is a valid zip file
            }
            catch (InvalidDataException)
            {
                _logger.LogError("PlaygroundController - The uploaded file is not a valid zip file");
                
                // The file is not a valid zip file
                return BadRequest(new PlaygroundSchema.PlaygroundContractGenerateResponse
                {
                    Success = false,
                    Message = "PlaygroundController - The uploaded file is not a valid zip file"
                });
            }
            
            var extractPath = Path.Combine(tempPath, Path.GetFileNameWithoutExtension(contractFiles.FileName), Guid.NewGuid().ToString());

            _logger.LogInformation("PlaygroundController - ExtractPath or destination directory where files are extracted is: "+extractPath);
            
            using (var archive = new ZipArchive(zipStream, ZipArchiveMode.Read))
            {
                foreach (var entry in archive.Entries)
                {
                    var destinationPath = Path.GetFullPath(Path.Combine(extractPath, entry.FullName));
                    
                    _logger.LogInformation("PlaygroundController - Extracting file from extractPath:" + extractPath + "to destinationPath: "+destinationPath);

                    // Ensure the destination file path is within the destination directory
                    if (!destinationPath.StartsWith(extractPath, StringComparison.Ordinal))
                    {
                        _logger.LogError("PlaygroundController - Invalid entry in the zip file: " + entry.FullName);
                        
                        return BadRequest(new PlaygroundSchema.PlaygroundContractGenerateResponse
                        {
                            Success = false,
                            Message = $"PlaygroundController - Invalid entry in the zip file: {entry.FullName}"
                        });
                    }

                    // Extract the entry to the destination path
                    entry.ExtractToFile(destinationPath, overwrite: true);
                }
            }
            
            _logger.LogInformation("PlaygroundController - Files extracted to disk");
            
            //validate if the extracted path contain .csProj file
            var csprojFiles = Directory.GetFiles(extractPath, "*.csproj", SearchOption.AllDirectories);
            if (csprojFiles.Length == 0)
            {
                return BadRequest(new PlaygroundSchema.PlaygroundContractGenerateResponse
                {
                    Success = false,
                    Message = "PlaygroundController - No .csproj file found in the uploaded zip file"
                });
            }
            
            var codeGeneratorGrain = _client.GetGrain<IPlaygroundGrain>("userId");
            var (success, message) = await codeGeneratorGrain.BuildProject(extractPath);

            if (success)
            {
                _logger.LogInformation("PlaygroundController - BuildProject method returned success: " + message);
                var pathToDll = message;
                var fileName = Path.GetFileName(pathToDll);
                return PhysicalFile(pathToDll, "application/octet-stream", fileName);
            }
            else
            {
                _logger.LogError("PlaygroundController - BuildProject method returned error: " + message);
                return BadRequest(new PlaygroundSchema.PlaygroundContractGenerateResponse
                {
                    Success = success,
                    Message = message
                });
            }
        }
    }
}