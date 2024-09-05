using System.Collections.Generic;
using System.Threading.Tasks;
using Orleans;
using PlaygroundService.Dtos;

namespace PlaygroundService.Grains;

public interface IPlaygroundGrain : IGrainWithStringKey
{
    public Task<(bool, string)> TestProject(ZipFileDto dto);
    public Task<(bool, string)> BuildProject(ZipFileDto dto);
    public Task<string> GenerateTemplate(string template, string templateName);
    public Task<List<string>> GetTemplates();
}