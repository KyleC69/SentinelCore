// Solution: Sentinel-MCP-Projects
// Project:   SentinelCore-mcp
// File:         PrinterReadTool.cs
// Author: Kyle L. Crowder
// Build Num:  110104



using System.ComponentModel;
using System.Management;
using System.Runtime.Versioning;

using ModelContextProtocol.Server;




namespace SentinelCoreMCP.Tools;





/// <summary>
///     Read-only tool for querying printer configuration and queues via the Win32_Printer CIM class,
///     replacing the winspool.drv P/Invoke approach per spec §6.2.
/// </summary>
[McpServerToolType]
[SupportedOSPlatform("windows")]
public sealed class PrinterReadTool
{

    /// <summary>
    ///     Lists installed printers and their queue status via the Win32_Printer CIM class.
    /// </summary>
    /// <param name="maxRecords">Maximum number of printers to return. Defaults to 50.</param>
    /// <returns>A <see cref="ToolResult" /> containing typed printer records.</returns>
    [SupportedOSPlatform("windows")]
    [McpServerTool(Name = "sc_hardware_list_printers", ReadOnly = true, Destructive = false)]
    [Description("Lists installed printers and their queue status via the Win32_Printer CIM class.")]
    public async Task<ToolResult> PrinterListAsync([Description("Maximum number of printers to return. Defaults to 50.")] int maxRecords = 50)
    {
        try
        {
            ToolResult? maxValidation = InputValidator.ValidateMaxRecords(maxRecords);
            if (maxValidation is not null)
            {
                return maxValidation;
            }

            List<PrinterRecord> results = await Task.Run(() =>
                    {
                        List<PrinterRecord> records = new();
                        using ManagementObjectSearcher searcher = new("root\\cimv2", "SELECT Name, PortName, DriverName, Status, ServerName, Default, Shared FROM Win32_Printer");
                        foreach (ManagementObject printer in searcher.Get())
                        {
                            if (records.Count >= maxRecords)
                            {
                                break;
                            }

                            records.Add(new PrinterRecord(Name: printer["Name"]?.ToString() ?? string.Empty, PortName: printer["PortName"]?.ToString() ?? string.Empty, DriverName: printer["DriverName"]?.ToString() ?? string.Empty, Status: printer["Status"]?.ToString() ?? string.Empty, ServerName: printer["ServerName"]?.ToString() ?? string.Empty, IsDefault: printer["Default"] is bool d && d, IsShared: printer["Shared"] is bool s && s));
                        }

                        return records;
                    })
                    .ConfigureAwait(false);

            return ToolResult.Ok(results, $"Enumerated {results.Count} printer(s).");
        }
        catch (Exception ex)
        {
            return ToolResult.Fail(ex.Message, "Printer listing");
        }
    }








    /// <summary>
    ///     Reads details of a specific printer queue via the Win32_Printer CIM class.
    /// </summary>
    /// <param name="printerName">The printer name to inspect.</param>
    /// <returns>A <see cref="ToolResult" /> containing the printer record.</returns>
    [SupportedOSPlatform("windows")]
    [McpServerTool(Name = "sc_hardware_read_printer", ReadOnly = true, Destructive = false)]
    [Description("Reads details of a specific printer queue via the Win32_Printer CIM class.")]
    public async Task<ToolResult> PrinterReadAsync([Description("The printer name to inspect.")] string printerName)
    {
        try
        {
            ToolResult? nameValidation = InputValidator.ValidateRequired(printerName, "printerName");
            if (nameValidation is not null)
            {
                return nameValidation;
            }

            ToolResult? sanitizedValidation = InputValidator.ValidateSanitizedWqlValue(printerName, "printerName");
            if (sanitizedValidation is not null)
            {
                return sanitizedValidation;
            }

            string query = $"SELECT Name, PortName, DriverName, Status, ServerName, Default, Shared, WorkOffline, PrinterStatus, DetectedErrorState FROM Win32_Printer WHERE Name='{printerName.Replace("'", "''")}'";

            PrinterRecord? record = await Task.Run(() =>
                    {
                        using ManagementObjectSearcher searcher = new("root\\cimv2", query);
                        foreach (ManagementObject printer in searcher.Get())
                        {
                            return new PrinterRecord(Name: printer["Name"]?.ToString() ?? string.Empty, PortName: printer["PortName"]?.ToString() ?? string.Empty, DriverName: printer["DriverName"]?.ToString() ?? string.Empty, Status: printer["Status"]?.ToString() ?? string.Empty, ServerName: printer["ServerName"]?.ToString() ?? string.Empty, IsDefault: printer["Default"] is bool d && d, IsShared: printer["Shared"] is bool s && s);
                        }

                        return null;
                    })
                    .ConfigureAwait(false);

            if (record is null)
            {
                return ToolResult.Fail($"Printer not found: {printerName}", "Printer read");
            }

            return ToolResult.Ok(record, $"Printer read for {printerName}");
        }
        catch (Exception ex)
        {
            return ToolResult.Fail(ex.Message, $"Printer read for {printerName}");
        }
    }








    /// <summary>
    ///     A single installed printer record.
    /// </summary>
    /// <param name="Name">The printer name.</param>
    /// <param name="PortName">The port the printer is attached to.</param>
    /// <param name="DriverName">The installed printer driver.</param>
    /// <param name="Status">The printer status.</param>
    /// <param name="ServerName">The print server, if a network printer.</param>
    /// <param name="IsDefault">Whether this is the default printer.</param>
    /// <param name="IsShared">Whether the printer is shared.</param>
    public sealed record PrinterRecord(string Name, string PortName, string DriverName, string Status, string ServerName, bool IsDefault, bool IsShared);
}
