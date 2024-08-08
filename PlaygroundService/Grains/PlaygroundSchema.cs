using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace PlaygroundService.Grains;

public abstract class PlaygroundSchema
{
    public class PlaygroundContractGenerateResponse
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; }
    }

}