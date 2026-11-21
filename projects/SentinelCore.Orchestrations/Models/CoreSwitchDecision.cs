using System.Text.Json.Serialization;

namespace SentinelCore.Orchestrations.Models;





public sealed class CoreSwitchDecision
{
    [JsonPropertyName("is_direct")]
    public bool IsDirect { get; set; }

    [JsonPropertyName("is_success")]
    public bool IsSuccess { get; set; }


    [JsonConstructor]
    public CoreSwitchDecision(bool isDirect, bool isSuccess)
    {
        IsDirect = isDirect;
        IsSuccess = isSuccess;
    }
}
