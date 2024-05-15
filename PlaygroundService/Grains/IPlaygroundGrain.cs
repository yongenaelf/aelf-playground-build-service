using System.Threading.Tasks;
using Orleans;

namespace PlaygroundService.Grains;

public interface IPlaygroundGrain : IGrainWithStringKey
{
    Task<bool> GenerateContract(string contractClass, string stateClass, string proto);
}