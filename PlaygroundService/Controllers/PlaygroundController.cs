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
using MongoDB.Driver.GridFS;
using MongoDB.Bson;
using System.Diagnostics;
using System.Linq;
using System.Collections.Generic;
using Custom;
namespace PlaygroundService.Controllers
{
    [ApiController]
    [Route("playground")]
    public class PlaygroundController : ControllerBase
    {
        private readonly IClusterClient _client;
        private readonly ILogger<PlaygroundController> _logger;
        private readonly IGridFSBucket _gridFS;
        private readonly long _maxFileSizeInBytes = 100 * 1024 * 1024;
        public PlaygroundController(IClusterClient client, ILogger<PlaygroundController> logger, IGridFSBucket gridFS)
        {
            _client = client;
            _logger = logger;
            _gridFS = gridFS;
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

        private async Task<IActionResult> BuildService(IFormFile contractFiles)
        {
            _logger.LogInformation("PlaygroundController - Build method started for: " + contractFiles.FileName + " time:" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            var startTime = DateTime.Now;

            var zipFileDto = await GetZipFileDto(contractFiles);

            var guid = Guid.NewGuid();
            var codeGeneratorGrain = _client.GetGrain<IPlaygroundGrain>(guid.ToString());
            var (success, message) = await codeGeneratorGrain.BuildProject(zipFileDto);

            if (success)
            {
                _logger.LogInformation("PlaygroundController - BuildProject method returned success: " + message + " time:" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                var pathToDll = message;
                var fileName = Path.GetFileName(pathToDll);
                _logger.LogInformation("PlaygroundController - Files return fileName:" + pathToDll);
                if (!System.IO.File.Exists(pathToDll))
                {
                    _logger.LogError("PlaygroundController - BuildProject method returned error: file not exist " + message + " time:" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                    return BadRequest(new PlaygroundSchema.PlaygroundContractGenerateResponse
                    {
                        Success = success,
                        Message = message
                    });
                }

                var dllBytes = BytesExtension.Read(pathToDll);
                if (dllBytes == null)
                {
                    _logger.LogError("PlaygroundController - BuildProject method returned error: dllBytes is null " + message + " time:" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
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

        private static async Task<ZipFileDto> GetZipFileDto(IFormFile contractFiles)
        {
            var zipFileDto = new ZipFileDto
            {
                ZipFile = await contractFiles.ToBytes(),
                Filename = contractFiles.FileName
            };
            return zipFileDto;
        }

        [HttpPost("test")]
        public async Task<IActionResult> Test(IFormFile contractFiles)
        {
            _logger.LogInformation("Test  - Test started time: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            return await TestService(contractFiles);
        }

        private async Task<IActionResult> TestService(IFormFile contractFiles)
        {
            _logger.LogInformation("PlaygroundController - Test method started for: " + contractFiles.FileName + " time:" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

            var zipFileDto = await GetZipFileDto(contractFiles);

            var guid = Guid.NewGuid();
            var codeGeneratorGrain = _client.GetGrain<IPlaygroundGrain>(guid.ToString());
            var (success, message) = await codeGeneratorGrain.TestProject(zipFileDto);

            if (success)
            {
                return Ok(message);
            }

            _logger.LogError("PlaygroundController - TestProject method returned error: " + message);
            return BadRequest(new PlaygroundSchema.PlaygroundContractGenerateResponse
            {
                Success = success,
                Message = message
            });
        }


        [HttpPost("share/create")]
        public async Task<IActionResult> UploadZipFile(IFormFile file)
        {
            // Check if the file is a valid ZIP file
            if (file == null || file.Length == 0 || !file.FileName.EndsWith(".zip"))
            {
                return BadRequest("Invalid file. Only ZIP files are allowed.");
            }
            // Check file size
            if (file.Length > _maxFileSizeInBytes)
            {
                return BadRequest($"File size exceeds the {_maxFileSizeInBytes / (1024 * 1024)} MB limit.");
            }

            // Save the file temporarily to scan for viruses
            var tempFilePath = Path.GetTempFileName();

            using (var fileStream = new FileStream(tempFilePath, FileMode.Create))
            {
                await file.CopyToAsync(fileStream);
            }

            // Run the file through a virus scanner (ClamAV in this example)
            var isSafe = ScanFileForViruses(tempFilePath);

            if (!isSafe)
            {
                System.IO.File.Delete(tempFilePath);
                return BadRequest("The uploaded file is potentially dangerous and has been rejected.");
            }

            // Extract ZIP file and validate its contents
            var extractionPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(extractionPath);

            try
            {
                System.IO.Compression.ZipFile.ExtractToDirectory(tempFilePath, extractionPath);

                // Define the expected structure
                var expectedStructure = new Dictionary<string, List<string>>
                {
                      { "src", new List<string> { "*.csproj" } },
                      { "test", new List<string> { "*.csproj" } }
                };
                // { FOR ALL THE FILES
                //     { "src", new List<string> { "*.cs", "*State.cs", "*.csproj", "Protobuf/contract/*.proto", "Protobuf/message/*.proto", "Protobuf/reference/*.proto" } },
                //     { "test", new List<string> { "*.cs", "*.cs", "*.csproj", "Protobuf/contract/*.proto", "Protobuf/message/*.proto", "Protobuf/reference/*.proto" } }
                //  };

                var missingFiles = ValidateFolderStructure(extractionPath, expectedStructure);


                if (missingFiles.Any())
                {
                    return BadRequest($"The uploaded ZIP file is missing the following files: {string.Join(", ", missingFiles)}");
                }

                // If the structure is valid, upload the file to MongoDB GridFS
                using (var stream = new FileStream(tempFilePath, FileMode.Open))
                {
                    var fileId = await _gridFS.UploadFromStreamAsync(file.FileName, stream);
                    System.IO.File.Delete(tempFilePath);
                    Directory.Delete(extractionPath, true);
                    return Ok(new { id = fileId.ToString() });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while processing the uploaded ZIP file.");
                return StatusCode(500, "An internal server error occurred.");
            }
            finally
            {
                System.IO.File.Delete(tempFilePath);
                if (Directory.Exists(extractionPath))
                {
                    Directory.Delete(extractionPath, true);
                }
            }
        }
        private static List<string> ValidateFolderStructure(string extractionPath, Dictionary<string, List<string>> expectedStructure)
        {
            var missingFiles = new List<string>();
            bool csprojFound = false;

            foreach (var folder in expectedStructure)
            {
                var folderPath = Path.Combine(extractionPath, folder.Key);
                if (!Directory.Exists(folderPath))
                {
                    missingFiles.Add($"Folder '{folder.Key}' is missing.");
                    continue;
                }

                foreach (var pattern in folder.Value)
                {
                    if (pattern == "*.csproj" && csprojFound)
                    {
                        continue;
                    }

                    try
                    {
                        var matchedFiles = Directory.GetFiles(folderPath, pattern, SearchOption.AllDirectories);
                        if (!matchedFiles.Any())
                        {
                            missingFiles.Add($"Pattern '{pattern}' is missing in '{folder.Key}' folder.");
                        }
                        else if (pattern == "*.csproj")
                        {
                            csprojFound = true;
                        }
                    }
                    catch (DirectoryNotFoundException)
                    {
                        missingFiles.Add($"Directory not found for pattern '{pattern}' in folder '{folder.Key}'.");
                    }
                }
            }

            return missingFiles;
        }


        // private static List<string> ValidateFolderStructure(string extractionPath, Dictionary<string, List<string>> expectedStructure)
        // {
        //     var missingFiles = new List<string>();

        //     foreach (var folder in expectedStructure)
        //     {
        //         var folderPath = Path.Combine(extractionPath, folder.Key);
        //         if (!Directory.Exists(folderPath))
        //         {
        //             missingFiles.Add($"Folder '{folder.Key}' is missing.");
        //             continue; // Skip to the next folder since this one doesn't exist
        //         }

        //         foreach (var pattern in folder.Value)
        //         {
        //             try
        //             {
        //                 var matchedFiles = Directory.GetFiles(folderPath, pattern, SearchOption.AllDirectories);
        //                 if (!matchedFiles.Any())
        //                 {
        //                     missingFiles.Add($"Pattern '{pattern}' is missing in '{folder.Key}' folder.");
        //                 }
        //             }
        //             catch (DirectoryNotFoundException)
        //             {
        //                 missingFiles.Add($"Directory not found for pattern '{pattern}' in folder '{folder.Key}'.");
        //             }
        //         }
        //     }

        //     return missingFiles;
        // }

        [HttpGet("share/get/{id}")]
        public async Task<IActionResult> DownloadZipFile(string id)
        {
            if (!ObjectId.TryParse(id, out var objectId))
            {
                return BadRequest("Invalid ID.");
            }

            var stream = await _gridFS.OpenDownloadStreamAsync(objectId);

            if (stream == null)
            {
                return NotFound("File not found.");
            }

            return File(stream, "application/zip", stream.FileInfo.Filename);
        }

        private static bool ScanFileForViruses(string filePath)
        {
            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = CustomConfigurationManager.AppSetting["ContractSetting:ClamScanPath"],  // Adjust this to the actual path
                Arguments = $"--no-summary {filePath}",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            Process? process = Process.Start(psi);
            if (process == null)
            {
                throw new InvalidOperationException("Failed to start the virus scanning process.");
            }

            using (process)
            {
                string output = process.StandardOutput?.ReadToEnd() ?? string.Empty;
                string error = process.StandardError?.ReadToEnd() ?? string.Empty;
                process.WaitForExit();

                if (!string.IsNullOrEmpty(error))
                {
                    if (error.Contains("LibClamAV Warning: The virus database is older than 7 days"))
                    {
                        Console.WriteLine("ClamAV database is outdated. Please update.");
                    }
                    else
                    {
                        throw new InvalidOperationException($"Virus scan failed: {error}");
                    }
                }

                // Assuming ClamAV returns 0 if no virus is found
                return process.ExitCode == 0;
            }
        }
    }
}