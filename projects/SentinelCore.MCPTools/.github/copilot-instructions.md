# Sentinel Core Tools MCP Server - Windows Tools

## Tool Mandates

- All tools must return ToolResult object.
- Each tool command results must be clean, normalized and free from ctrl characters and shaped for machine consumption.
- No tool should ever throw or swallow errors, the exception message should be returned in the ErrorDetails property. Additional context or details should be returned using the Message property
- If the underlying command is a SysInternal command you must silence the banner with -nobanner or equivalent to keep results clean. Also the eula should be suppressed with /accepteula or equivalent.
- Ensure a cancellationtoken is used with a 20 second timout and is propegated throughout all tools.



## Asynchronous Function Pattern

**Recommended** pattern for cmd line utilities

```using System.Diagnostics;

using Process process = new();

process.StartInfo = new ProcessStartInfo
{
    FileName = "ipconfig.exe",
    Arguments = "/all",
    RedirectStandardOutput = true,
    RedirectStandardError = true,
    UseShellExecute = false
};

process.WaitForExitAsync();

Normalize output for machine consumption
```
