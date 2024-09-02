using Orleans;

namespace PlaygroundService.Dtos;

[GenerateSerializer]
public class BuildDto
{
    [Id(0)]
    public byte[]? ZipFile { get; set; }
    
    [Id(1)]
    public string? Filename { get; set; }
}