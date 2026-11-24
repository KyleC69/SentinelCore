// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         CoreSwitchDecision.cs
// Author: Kyle L. Crowder
// Build Num:  112101



using System.Text.Json.Serialization;




namespace SentinelCore.Orchestrations.Models;





public sealed class CoreSwitchDecision
{


    [JsonConstructor]
    public CoreSwitchDecision(bool isDirect, bool isSuccess)
    {
        IsDirect = isDirect;
        IsSuccess = isSuccess;
    }








    [JsonPropertyName("is_direct")] public bool IsDirect { get; set; }

    [JsonPropertyName("is_success")] public bool IsSuccess { get; set; }
}