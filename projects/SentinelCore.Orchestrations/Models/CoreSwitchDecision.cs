using System.Text.Json.Serialization;

namespace SentinelCore.Orchestrations.Models;





public class CoreSwitchDecision
{
    [JsonPropertyName("is_direct")]
    public bool IsDirect { get; set; }
}
