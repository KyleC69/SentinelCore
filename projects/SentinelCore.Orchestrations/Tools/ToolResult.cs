// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         ToolResult.cs
// Author: Kyle L. Crowder
// Build Num:  082808



namespace SentinelCore.Tools;





/// <summary>
///     a universal result object for tool operations. It contains information about the success or failure of the
///     operation, as well as any relevant results or failure reasons.
///     Every AITool in the Sentinel Core Platform *must* return a ToolResult object to indicate the outcome of its
///     operation. This allows for consistent handling of tool results across the system.
/// </summary>
public class ToolResult
{

    /// <summary>
    ///     Indicates the reason for the failure of the tool operation, if any.
    /// </summary>
    public string? ErrorDetails { get; set; }

    public string? Message { get; set; }

    /// <summary>
    ///     Indicates the results of the tool operation. This is a free-form string that can be used to store any information
    ///     that the tool wants to return to the caller.
    /// </summary>
    public string? Results { get; set; }

    /// <summary>
    ///     Indicates whether the tool operation was successful.
    /// </summary>
    public bool Success { get; set; }


    public static ToolResult Fail(string errorDetails, string message = "Fail") => new ToolResult { Success = false, ErrorDetails = errorDetails, Results = null };








    public static ToolResult Ok(string results, string message = "Ok")
    {
        return new ToolResult { Success = true, ErrorDetails = null, Results = results, Message = message };
    }
}