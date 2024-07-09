using System.Threading.Tasks;
using Orleans;

namespace PlaygroundService.Grains;

public interface IPlaygroundGrain : IGrainWithStringKey
{
    public Task<(bool, string)> BuildProject(string directory);
    public Task<bool> DelData(string zipFile, string extractPath);
    public Task<string> GenerateZip(string template, string templateName);
}