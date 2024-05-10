using System.Text.Json.Serialization;

namespace ProofService.interfaces;

public abstract class PlaygroundSchema
{
    public class PlaygroundContractGenerateRequest
    {
        [JsonPropertyName("ContractClass")]
        public string contractClass { get; set; }
        
        [JsonPropertyName("StateClass")]
        public string stateClass { get; set; }
        
        [JsonPropertyName("Proto")]
        public string proto { get; set; }
    }

}