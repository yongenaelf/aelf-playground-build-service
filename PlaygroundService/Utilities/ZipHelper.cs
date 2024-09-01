using System;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace PlaygroundService.Utilities;

public static class BytesExtension
{
    public static byte[]? Read(string path)
    {
        try
        {
            var code = File.ReadAllBytes(path);
            return code;
        }
        catch (Exception e)
        {
            return null;
        }
    }
    
    // extract byte array to zip file
    public static void ExtractTo(this byte[] zipBytes, string path)
    {
        using var memoryStream = new MemoryStream(zipBytes);
        memoryStream.ExtractTo(path);
    }
    
    // extract zip contents from filestream
    private static void ExtractTo(this Stream zipStream, string path)
    {
        using var archive = new ZipArchive(zipStream, ZipArchiveMode.Read);
        foreach (var entry in archive.Entries)
        {
            var destinationPath = Path.GetFullPath(Path.Combine(path, entry.FullName));

            // Ensure the destination file path is within the destination directory
            if (!destinationPath.StartsWith(path, StringComparison.Ordinal))
            {
                throw new FormatException($"Invalid entry in the zip file: {entry.FullName}");
            }
            
            // Check if destinationPath is a directory
            if (string.IsNullOrEmpty(entry.Name))
            {
                continue;
            }

            // Create the directory for the file if it does not exist
            var destinationDirectory = Path.GetDirectoryName(destinationPath);
            if (destinationDirectory == null)
            {
                throw new InvalidOperationException($"Invalid destination directory: {destinationPath}");
            }
            if (!Directory.Exists(destinationDirectory))
            {
                Directory.CreateDirectory(destinationDirectory);
            }

            // Extract the entry to the destination path
            entry.ExtractToFile(destinationPath, overwrite: true);
        }
    }
}

public static class FormFileExtension
{
    public static async Task<byte[]?> ToBytes(this IFormFile formFile)
    {
        if( formFile.ContentType != "application/zip")
        {
            return null;
        }
        
        var memoryStream = new MemoryStream();
        await formFile.CopyToAsync(memoryStream);

        return memoryStream.ToArray();
    }
}