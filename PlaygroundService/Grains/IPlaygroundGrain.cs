using System.Threading.Tasks;
using Orleans;

namespace PlaygroundService.Grains;

public interface IPlaygroundGrain : IGrainWithStringKey
{
    Task<bool> GenerateContract(string projectName, string contractClass, string contractFileName, string stateClass, string stateFileName, string proto, string protoFileName);
}