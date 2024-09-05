using Orleans;

namespace PlaygroundService.Dtos;

[GenerateSerializer]
public class ZipFileDto
{
    [Id(0)]
    public byte[]? ZipFile { get; set; }
    
    [Id(1)]
    public string? Filename { get; set; }
}