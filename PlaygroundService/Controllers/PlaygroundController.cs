using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
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

        [HttpPost("generate")]
        public async Task<PlaygroundSchema.PlaygroundContractGenerateResponse> Generate(List<IFormFile> contractFiles)
        {
            var tempPath = Path.GetTempPath();
            var directoryPath = Path.Combine(tempPath, Guid.NewGuid().ToString());

            Directory.CreateDirectory(directoryPath);

            foreach (var file in contractFiles)
            {
                if (file.Length > 0)
                {
                    var filePath = Path.Combine(directoryPath, file.FileName);
                    var directoryName = Path.GetDirectoryName(filePath);

                    if (!Directory.Exists(directoryName))
                    {
                        Directory.CreateDirectory(directoryName);
                    }

                    await using var fileStream = new FileStream(filePath, FileMode.Create);
                    await file.CopyToAsync(fileStream);
                }
            }

            var codeGeneratorGrain = _client.GetGrain<IPlaygroundGrain>("userId");
            var res = await codeGeneratorGrain.GenerateContract(directoryPath);

            return new PlaygroundSchema.PlaygroundContractGenerateResponse
            {
                success = res
            };
        }
        
        [HttpPost("upload")]
        public async Task<string> Upload(IFormFile contractFiles)
        {
            var tempPath = Path.GetTempPath();
            var zipPath = Path.Combine(tempPath, contractFiles.FileName);

            await using var zipStream = new FileStream(zipPath, FileMode.Create);
            await contractFiles.CopyToAsync(zipStream);

            var extractPath = Path.Combine(tempPath, Path.GetFileNameWithoutExtension(contractFiles.FileName));
            System.IO.Compression.ZipFile.ExtractToDirectory(zipPath, extractPath);

            var directoryTree = PrintDirectoryTree(extractPath);

            return directoryTree;
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
}