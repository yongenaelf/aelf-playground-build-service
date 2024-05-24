using System.Threading.Tasks;
using Orleans;

namespace PlaygroundService.Grains;

public interface IPlaygroundGrain : IGrainWithStringKey
{
    public Task<bool> GenerateContract(string directory);
}