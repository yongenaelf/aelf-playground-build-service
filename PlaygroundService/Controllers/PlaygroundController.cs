using System.Threading.Tasks;
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
        public async Task<PlaygroundSchema.PlaygroundContractGenerateResponse> GenerateContract(PlaygroundSchema.PlaygroundContractGenerateRequest request)
        {
            var zkProofGrain = _client.GetGrain<IPlaygroundGrain>("userId");
            var res = await zkProofGrain.GenerateContract(request.ProjectName, request.contractClass, request.contractClassName, request.stateClass, request.stateClassName, request.proto, request.protoFilename);

            return new PlaygroundSchema.PlaygroundContractGenerateResponse
            {
                success = res
            };
        }
    }
    
}