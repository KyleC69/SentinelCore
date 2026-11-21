// Solution: Sentinel-MCP-Projects
// Project:   SentinelCore-mcp
// File:         ServiceExtendedReadTool.cs
// Author: Kyle L. Crowder
// Build Num:  110104



using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.Versioning;
using System.Text;

using ModelContextProtocol.Server;




namespace SentinelCoreMCP.Tools;





/// <summary>
///     Read-only tool for querying Windows service ACLs (access control lists)
///     for service permission misconfiguration detection.
///     Uses <c>sc.exe sdshow</c> with strict input validation to prevent argument injection.
/// </summary>
[McpServerToolType]
[SupportedOSPlatform("windows")]
public sealed class ServiceExtendedReadTool
{

    /// <summary>
    ///     Runs sc.exe with the specified arguments and returns the standard output.
    /// </summary>
    /// <param name="arguments">The validated arguments to pass to sc.exe.</param>
    /// <param name="operation">The operation description for error reporting.</param>
    /// <returns>The standard output of sc.exe.</returns>
    /// <exception cref="InvalidOperationException">Thrown when sc.exe fails or times out.</exception>
    private static string RunSc(string arguments, string operation)
    {
        using Process process = new()
        {
                StartInfo = new ProcessStartInfo
                {
                        FileName = "sc.exe",
                        Arguments = arguments,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                }
        };

        process.Start();

        string stdout = process.StandardOutput.ReadToEnd();
        string stderr = process.StandardError.ReadToEnd();

        if (!process.WaitForExit(30_000))
        {
            process.Kill();
            throw new TimeoutException($"sc.exe {operation} timed out after 30 seconds.");
        }

        if (process.ExitCode != 0 && string.IsNullOrWhiteSpace(stdout))
        {
            throw new InvalidOperationException($"sc.exe {operation} failed with exit code {process.ExitCode}: {stderr}");
        }

        return stdout;
    }








    /// <summary>
    ///     Reads the ACL (access control list) of a Windows service for permission auditing.
    /// </summary>
    /// <param name="serviceName">The service name to inspect.</param>
    /// <returns>
    ///     A <see cref="ToolResult" /> containing the service SDDL and configuration. Every line of the
    ///     captured sc.exe output is normalized through <see cref="Helpers.CleanOutputLines" /> before it is
    ///     appended, so the fixed-width padding sc.exe emits never reaches the caller. A failure result is
    ///     returned when sc.exe yields nothing readable.
    /// </returns>
    [SupportedOSPlatform("windows")]
    [McpServerTool(Name = "sc_persistence_list_service_details", ReadOnly = true, Destructive = false)]
    [Description("Reads the ACL (access control list) of a Windows service for permission auditing.")]
    public async Task<ToolResult> ServiceReadAclAsync([Description("The service name to inspect.")] string serviceName)
    {
        try
        {
            ToolResult? nameValidation = ValidateServiceName(serviceName);
            if (nameValidation is not null)
            {
                return nameValidation;
            }

            string sddl = RunSc($"sdshow \"{serviceName}\"", "sdshow");

            if (string.IsNullOrWhiteSpace(sddl.Trim()))
            {
                return ToolResult.Fail($"Service not found or no ACL available: {serviceName}", "ServiceExtendedReadTool");
            }

            string config = RunSc($"qc \"{serviceName}\"", "qc");

            List<string> sddlLines = Helpers.CleanOutputLines(sddl);
            List<string> configLines = Helpers.CleanOutputLines(config);

            if (sddlLines.Count == 0 && configLines.Count == 0)
            {
                return ToolResult.Fail($"Service returned no readable ACL or configuration output: {serviceName}", "ServiceExtendedReadTool");
            }

            StringBuilder sb = new();
            sb.AppendLine($"[Service: {serviceName}]");
            sb.AppendLine("[Security Descriptor (SDDL)]");

            foreach (string sddlLine in sddlLines)
            {
                sb.AppendLine(sddlLine);
            }

            sb.AppendLine("[Service Configuration]");

            foreach (string configLine in configLines)
            {
                sb.AppendLine(configLine);
            }

            return ToolResult.Ok(sb.ToString(), "ServiceExtendedReadTool");
        }
        catch (Exception ex)
        {
            return ToolResult.Fail(ex.Message, $"Service ACL read for {serviceName}");
        }
    }








    /// <summary>
    ///     Validates a service name to prevent argument injection into sc.exe.
    ///     Service names may contain letters, digits, spaces, hyphens, underscores, and dots.
    /// </summary>
    /// <param name="serviceName">The service name to validate.</param>
    /// <returns>A <see cref="ToolResult" /> indicating failure if validation fails, or <c>null</c> if validation passes.</returns>
    private static ToolResult? ValidateServiceName(string? serviceName)
    {
        ToolResult? requiredResult = InputValidator.ValidateRequired(serviceName, "serviceName");
        if (requiredResult is not null)
        {
            return requiredResult;
        }

        if (serviceName!.IndexOfAny(['/', '\\', '"', '\'', '&', '|', ';', '<', '>', '%', '$', '`', '!']) >= 0)
        {
            return ToolResult.Fail($"serviceName contains invalid characters: {serviceName}. Service names must not contain path separators, quotes, or shell metacharacters.", "ServiceExtendedReadTool");
        }

        return null;
    }
}
