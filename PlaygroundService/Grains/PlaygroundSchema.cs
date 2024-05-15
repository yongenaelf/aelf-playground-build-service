using System.Text.Json.Serialization;

namespace PlaygroundService.Grains;

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
    
    public class PlaygroundContractGenerateResponse
    {
        [JsonPropertyName("success")]
        public bool success { get; set; }
    }

}