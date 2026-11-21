namespace SentinelCoreMCP.Tools;





public class PowerShellResult
{
    /// <summary>
    /// Indicates whether the PowerShell command executed successfully.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// The structured results returned by the PowerShell command. For queries this is the
    /// projected payload built by <c>PowerShellReadTool.PowerShellQueryPayload</c>; an
    /// embedded JSON string is never returned.
    /// </summary>
    public object? Results { get; set; }

    /// <summary>
    /// The error message, if any, returned by the PowerShell command.
    /// </summary>
    public string? Error { get; set; } = string.Empty;

    /// <summary>
    /// A custom message associated with the PowerShell command execution.
    /// </summary>
    public string? Message { get; set; } = string.Empty;


    /// <summary>
    /// The PowerShell command that was executed.
    /// </summary>
    public string? Command { get; set; } = string.Empty;








    public static PowerShellResult Fail(string error = "", string message = "", string command = "")
    {
        return new PowerShellResult()
        {
                Success = false,
                Results = null,
                Error = error,
                Message = message,
                Command = command
        };
    }








    public static PowerShellResult Ok(object? results = null, string message = "", string command = "")
    {
        return new PowerShellResult()
        {
                Success = true,
                Results = results,
                Error = string.Empty,
                Message = message,
                Command = command
        };
    }
}
