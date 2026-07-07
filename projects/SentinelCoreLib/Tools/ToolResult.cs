// Solution: SentinelCoreLib
// Project:   SentinelCoreLib
// File:         ToolResult.cs
// Author: Kyle L. Crowder
// Build Date: 2026/07/07



namespace SentinelCoreLib.Tools;





/// <summary>
///     a universal result object for tool operations. It contains information about the success or failure of the
///     operation, as well as any relevant results or failure reasons.
///     Every AITool in the Sentinel Core Platform *must* return a ToolResult object to indicate the outcome of its
///     operation. This allows for consistent handling of tool results across the system.
/// </summary>
public class ToolResult
{

    public ToolResult(bool success, string failReason)
    {
        Success = success;
        FailReason = failReason;
    }








    /// <summary>
    ///     Indicates the reason for the failure of the tool operation, if any.
    /// </summary>
    public string FailReason { get; set; }

    /// <summary>
    ///     Indicates the results of the tool operation. This is a free-form string that can be used to store any information
    ///     that the tool wants to return to the caller.
    /// </summary>
    public string Results { get; set; }

    /// <summary>
    ///     Indicates whether the tool operation was successful.
    /// </summary>
    public bool Success { get; }


    public static ToolResult FailureResult(string message) => new(false, message);


    public static ToolResult SuccessResult(string message = "") => new(true, message);
}