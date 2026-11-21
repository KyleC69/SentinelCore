// Solution: Sentinel-MCP-Projects
// Project:   SentinelCore-mcp
// File:         ToolResult.cs
// Author: Kyle L. Crowder
// Build Num:  110104



namespace SentinelCoreMCP.Tools;





/// <summary>
///     Coarse failure classification for tool operations, per the MCP tool standard
///     specification §3.4. Values are stable so models can branch on them without
///     parsing message strings.
/// </summary>
public enum ToolErrorCategory
{
    /// <summary>An input did not validate (missing, malformed, out of range).</summary>
    InvalidInput,

    /// <summary>The target was not found (file, task, key, object).</summary>
    NotFound,

    /// <summary>Access was denied by the operating system.</summary>
    AccessDenied,

    /// <summary>The operation requires an elevated process token.</summary>
    NotElevated,

    /// <summary>A required external binary is not installed on the host.</summary>
    ExternalToolMissing,

    /// <summary>The operation did not finish inside its time budget.</summary>
    Timeout,

    /// <summary>The system state does not support the operation (feature absent).</summary>
    Unsupported,

    /// <summary>An unexpected failure that does not fit another category.</summary>
    InternalError
}





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
    ///     THIS MUST BE USED FOR ALL FAILURES AND CONTAIN THE EXCEPTION MESSAGE AND ANY ADDITIONAL CONTEXTUAL INFORMATION
    ///     ABOUT THE FAILURE. This is critical for debugging and understanding why a tool operation failed.
    ///     ///     If the tool operation was successful, this property should be null.
    /// </summary>
    public string? ErrorDetails { get; set; }

    /// <summary>
    ///     Optional coarse failure classification. Null for successes and unspecified failures.
    ///     Stability contract: the set of values is fixed; message strings are never parsed to recover it.
    /// </summary>
    public ToolErrorCategory? ErrorCategory { get; set; }

    /// <summary>
    ///     Stable machine-readable error code for the failure, when one is agreed for the tool
    ///     family (e.g. "BCD_ACCESS_DENIED"). Null for successes. Models branch on
    ///     ErrorCategory for routing; this carries finer-grained detail where it exists.
    /// </summary>
    public string? ErrorCode { get; set; }

    /// <summary>
    ///     Human readable message providing additional context about the result of the tool operation. This can be used to
    ///     explain the outcome of the operation in a more user-friendly way, or to provide additional information about the
    ///     result. It can be used for logging, debugging, or providing feedback to the user. If the tool operation was
    ///     successful, this property can contain a success message. If the operation failed, it can contain a message
    ///     explaining the failure.
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    ///     Indicates the results of the tool operation. This is oftype object to allow for flexibility in the type of results
    ///     that can be returned. It can be a string, a complex object, or any other type that represents the results of the
    ///     tool operation.
    /// </summary>
    public object? Results { get; set; }

    /// <summary>
    ///     Indicates whether the tool operation was successful.
    /// </summary>
    public bool Success { get; set; }


    public static ToolResult Fail(string errorDetails, string message) => new ToolResult { Success = false, Message = message, ErrorDetails = Helpers.CleanValue(errorDetails), Results = null };








    /// <summary>
    ///     Creates a structured failure with the coarse category and optional error code.
    /// </summary>
    /// <param name="errorDetails">The exception text and contextual detail.</param>
    /// <param name="message">The human-readable operation context.</param>
    /// <param name="category">The coarse failure classification.</param>
    /// <param name="errorCode">Optional stable machine-readable error code.</param>
    /// <returns>The structured failure result.</returns>
    public static ToolResult Fail(string errorDetails, string message, ToolErrorCategory category, string? errorCode = null) => new ToolResult
    {
            Success = false,
            Message = message,
            ErrorDetails = Helpers.CleanValue(errorDetails),
            ErrorCategory = category,
            ErrorCode = errorCode,
            Results = null
    };








    public static ToolResult Ok(object results, string message)
    {
        return new ToolResult { Success = true, ErrorDetails = null, Results = results, Message = message };
    }
}
