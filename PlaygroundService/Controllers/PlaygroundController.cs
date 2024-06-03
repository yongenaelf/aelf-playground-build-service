using System;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Orleans;
using PlaygroundService.Grains;

namespace PlaygroundService.Controllers
{
    [ApiController]
    [Route("playground")]
    public class PlaygroundController : ControllerBase
    {
        private readonly IClusterClient _client;

        public PlaygroundController(IClusterClient client)
        {
            _client = client;
        }

        [HttpPost("build")]
        public async Task<IActionResult> Build(IFormFile contractFiles)
        {
            var tempPath = Path.GetTempPath();
            var zipPath = Path.Combine(tempPath, contractFiles.FileName);
            
            try
            {
                using var archive = ZipFile.OpenRead(zipPath);
                // If we get here, the file is a valid zip file
            }
            catch (InvalidDataException)
            {
                // The file is not a valid zip file
                return BadRequest(new PlaygroundSchema.PlaygroundContractGenerateResponse
                {
                    Success = false,
                    Message = "The uploaded file is not a valid zip file"
                });
            }

            await using var zipStream = new FileStream(zipPath, FileMode.Create);
            await contractFiles.CopyToAsync(zipStream);
            await zipStream.FlushAsync(); // Ensure all data is written to the file

            var extractPath = Path.Combine(tempPath, Path.GetFileNameWithoutExtension(contractFiles.FileName), Guid.NewGuid().ToString());

            System.IO.Compression.ZipFile.ExtractToDirectory(zipPath, extractPath);
            
            //validate if the extracted path contain .csProj file
            var csprojFiles = Directory.GetFiles(extractPath, "*.csproj", SearchOption.AllDirectories);
            if (csprojFiles.Length == 0)
            {
                return BadRequest(new PlaygroundSchema.PlaygroundContractGenerateResponse
                {
                    Success = false,
                    Message = "No .csproj file found in the uploaded zip file"
                });
            }
            
            var codeGeneratorGrain = _client.GetGrain<IPlaygroundGrain>("userId");
            var (success, message) = await codeGeneratorGrain.BuildProject(extractPath);

            if (success)
            {
                var pathToDll = message;
                var fileName = Path.GetFileName(pathToDll);
                return PhysicalFile(pathToDll, "application/octet-stream", fileName);
            }
            else
            {
                return BadRequest(new PlaygroundSchema.PlaygroundContractGenerateResponse
                {
                    Success = success,
                    Message = message
                });
            }
        }
    }
}