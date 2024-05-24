using System.Text.Json.Serialization;

namespace PlaygroundService.Grains;

public abstract class PlaygroundSchema
{
    public class PlaygroundContractGenerateRequest
    {
        
        [JsonPropertyName("ProjectName")]
        public string ProjectName { get; set; }
        
        [JsonPropertyName("ContractClass")]
        public string contractClass { get; set; }
        
        [JsonPropertyName("ContractClassName")]
        public string contractClassName { get; set; }
        
        [JsonPropertyName("StateClass")]
        public string stateClass { get; set; }
        
        [JsonPropertyName("StateClassName")]
        public string stateClassName { get; set; }
        
        [JsonPropertyName("Proto")]
        public string proto { get; set; }
        
        [JsonPropertyName("ProtoFilename")]
        public string protoFilename { get; set; }
    }
    
    public class PlaygroundContractGenerateResponse
    {
        [JsonPropertyName("success")]
        public bool success { get; set; }
    }

}