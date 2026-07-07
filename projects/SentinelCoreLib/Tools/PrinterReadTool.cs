// Solution: SentinelCoreLib
// Project:   SentinelCoreLib
// File:         PrinterReadTool.cs
// Author: Kyle L. Crowder
// Build Date: 2026/07/07



using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;

using Microsoft.Extensions.AI;

using SentinelCoreLib.Tools.Interop;




namespace SentinelCoreLib.Tools;





/// <summary>
///     Read-only tool for querying printer configuration and queues via the Print Spooler API (winspool.drv).
/// </summary>
public sealed class PrinterReadTool : AITool
{
    private const uint Flags = PrinterEnumLocal | PrinterEnumConnections;
    private const uint LevelTwo = 2;
    private const uint PrinterEnumConnections = 0x00000004;
    private const uint PrinterEnumLocal = 0x00000002;








    [DllImport("winspool.drv", SetLastError = true)]
    private static extern bool ClosePrinter(IntPtr hPrinter);








    [DllImport("winspool.drv", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern bool EnumPrinters(uint flags, string? name, uint level, IntPtr pPrinterEnum, uint cbBuf, out uint pcbNeeded, out uint pcReturned);








    [DllImport("winspool.drv", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern bool GetPrinter(IntPtr hPrinter, int level, IntPtr pPrinter, int cbBuf, out int pcbNeeded);








    [DllImport("winspool.drv", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern bool OpenPrinter(string pPrinterName, out IntPtr phPrinter, IntPtr pDefault);








    [Description("Lists installed printers and their queue status via the Print Spooler API.")]
    public Task<ToolResult> printer_list()
    {
        try
        {
            StringBuilder sb = new();
            if (!EnumPrinters(Flags, null, LevelTwo, IntPtr.Zero, 0, out uint needed, out uint returned) && needed == 0)
            {
                return Task.FromResult(ToolResult.FailureResult($"Printer enumeration failed: {PInvokeHelper.GetLastErrorMessage()}"));
            }

            IntPtr buffer = Marshal.AllocHGlobal((int)needed);
            try
            {
                if (!EnumPrinters(Flags, null, LevelTwo, buffer, needed, out _, out returned))
                {
                    return Task.FromResult(ToolResult.FailureResult($"Printer enumeration failed: {PInvokeHelper.GetLastErrorMessage()}"));
                }

                int entrySize = Marshal.SizeOf<PrinterInfo2>();
                for (int i = 0; i < returned; i++)
                {
                    IntPtr ptr = IntPtr.Add(buffer, i * entrySize);
                    PrinterInfo2 info = Marshal.PtrToStructure<PrinterInfo2>(ptr);
                    sb.AppendLine($"Name={info.pPrinterName}, PortName={info.pPortName}, DriverName={info.pDriverName}, Status={info.Status}, ServerName={info.pServerName}");
                }
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }

            return Task.FromResult(ToolResult.SuccessResult(sb.ToString()));
        }
        catch (Exception ex)
        {
            return Task.FromResult(ToolResult.FailureResult($"Printer listing failed: {ex.Message}"));
        }
    }








    [Description("Reads details of a specific printer queue via the Print Spooler API.")]
    public Task<ToolResult> printer_read([Description("The printer name to inspect.")] string printerName)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(printerName))
            {
                return Task.FromResult(ToolResult.FailureResult("printerName is required."));
            }

            if (!OpenPrinter(printerName, out IntPtr hPrinter, IntPtr.Zero))
            {
                return Task.FromResult(ToolResult.FailureResult($"OpenPrinter failed: {PInvokeHelper.GetLastErrorMessage()}"));
            }

            try
            {
                if (!GetPrinter(hPrinter, 2, IntPtr.Zero, 0, out int needed))
                {
                    if (needed == 0)
                    {
                        return Task.FromResult(ToolResult.FailureResult($"GetPrinter failed: {PInvokeHelper.GetLastErrorMessage()}"));
                    }
                }

                IntPtr buffer = Marshal.AllocHGlobal(needed);
                try
                {
                    if (!GetPrinter(hPrinter, 2, buffer, needed, out _))
                    {
                        return Task.FromResult(ToolResult.FailureResult($"GetPrinter failed: {PInvokeHelper.GetLastErrorMessage()}"));
                    }

                    PrinterInfo2 info = Marshal.PtrToStructure<PrinterInfo2>(buffer);
                    StringBuilder sb = new();
                    sb.AppendLine($"Name={info.pPrinterName}");
                    sb.AppendLine($"PortName={info.pPortName}");
                    sb.AppendLine($"DriverName={info.pDriverName}");
                    sb.AppendLine($"Status={info.Status}");
                    sb.AppendLine($"Comment={info.pComment}");
                    sb.AppendLine($"Location={info.pLocation}");
                    sb.AppendLine($"ServerName={info.pServerName}");
                    sb.AppendLine($"Jobs={info.cJobs}");

                    return Task.FromResult(ToolResult.SuccessResult(sb.ToString()));
                }
                finally
                {
                    Marshal.FreeHGlobal(buffer);
                }
            }
            finally
            {
                ClosePrinter(hPrinter);
            }
        }
        catch (Exception ex)
        {
            return Task.FromResult(ToolResult.FailureResult($"Printer read failed: {ex.Message}"));
        }
    }








    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private struct PrinterInfo2
    {
        public string pServerName;
        public string pPrinterName;
        public string pShareName;
        public string pPortName;
        public string pDriverName;
        public string pComment;
        public string pLocation;
        public IntPtr pDevMode;
        public string pSepFile;
        public string pPrintProcessor;
        public string pDatatype;
        public string pParameters;
        public IntPtr pSecurityDescriptor;
        public uint Attributes;
        public uint Priority;
        public uint DefaultPriority;
        public uint StartTime;
        public uint UntilTime;
        public uint Status;
        public uint cJobs;
        public uint AveragePPM;
    }
}