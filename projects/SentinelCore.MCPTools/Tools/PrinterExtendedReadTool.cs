// Solution: Sentinel-MCP-Projects
// Project:   SentinelCore-mcp
// File:         PrinterExtendedReadTool.cs
// Author: Kyle L. Crowder
// Build Num:  110104



using System.ComponentModel;
using System.Management;
using System.Runtime.Versioning;

using ModelContextProtocol.Server;




namespace SentinelCoreMCP.Tools;





/// <summary>
///     Read-only tool for querying print spooler jobs for print spooler vulnerability investigation.
///     Uses the Win32_PrintJob CIM class instead of shelling out to PowerShell, per spec §6.2.
/// </summary>
[McpServerToolType]
[SupportedOSPlatform("windows")]
public sealed class PrinterExtendedReadTool
{

    /// <summary>
    ///     Lists current print spooler jobs for print spooler vulnerability investigation.
    /// </summary>
    /// <param name="maxRecords">Maximum number of jobs to return. Defaults to 50.</param>
    /// <returns>A <see cref="ToolResult" /> containing typed print job records.</returns>
    [SupportedOSPlatform("windows")]
    [McpServerTool(Name = "sc_hardware_list_printer_jobs", ReadOnly = true, Destructive = false)]
    [Description("Lists current print spooler jobs for print spooler vulnerability investigation.")]
    public async Task<ToolResult> PrinterListJobsAsync([Description("Maximum number of jobs to return. Defaults to 50.")] int maxRecords = 50)
    {
        try
        {
            ToolResult? maxValidation = InputValidator.ValidateMaxRecords(maxRecords);
            if (maxValidation is not null)
            {
                return maxValidation;
            }

            List<PrintJobRecord> results = await Task.Run(() =>
                    {
                        List<PrintJobRecord> records = new();
                        using ManagementObjectSearcher searcher = new("root\\cimv2", "SELECT JobId, Name, Document, Owner, Status, TotalPages, PagesPrinted, TimeSubmitted FROM Win32_PrintJob");
                        foreach (ManagementObject job in searcher.Get())
                        {
                            if (records.Count >= maxRecords)
                            {
                                break;
                            }

                            // Win32_PrintJob.Name is "PrinterName,JobId"
                            string fullName = job["Name"]?.ToString() ?? string.Empty;
                            string printerName = fullName.Contains(',') ? fullName.Split(',')[0] : fullName;

                            records.Add(new PrintJobRecord(JobId: job["JobId"] is uint id ? id : 0, PrinterName: printerName, Document: job["Document"]?.ToString() ?? string.Empty, UserName: job["Owner"]?.ToString() ?? string.Empty, Status: job["Status"]?.ToString() ?? string.Empty, TotalPages: job["TotalPages"] is uint tp ? tp : 0, PagesPrinted: job["PagesPrinted"] is uint pp ? pp : 0, SubmittedAt: job["TimeSubmitted"] is DateTime dt ? dt.ToString("O") : string.Empty));
                        }

                        return records;
                    })
                    .ConfigureAwait(false);

            return ToolResult.Ok(results, $"Enumerated {results.Count} print job(s).");
        }
        catch (Exception ex)
        {
            return ToolResult.Fail(ex.Message, "Print job listing");
        }
    }








    /// <summary>
    ///     A single print spooler job record.
    /// </summary>
    /// <param name="JobId">The spooler job identifier.</param>
    /// <param name="PrinterName">The printer the job is queued on.</param>
    /// <param name="Document">The document name.</param>
    /// <param name="UserName">The user who submitted the job.</param>
    /// <param name="Status">The job status.</param>
    /// <param name="TotalPages">Total pages in the job.</param>
    /// <param name="PagesPrinted">Pages printed so far.</param>
    /// <param name="SubmittedAt">When the job was submitted.</param>
    public sealed record PrintJobRecord(uint JobId, string PrinterName, string Document, string UserName, string Status, uint TotalPages, uint PagesPrinted, string SubmittedAt);
}
