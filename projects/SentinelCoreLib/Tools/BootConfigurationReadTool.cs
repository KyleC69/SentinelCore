// Solution: SentinelCoreLib
// Project:   SentinelCoreLib
// File:         BootConfigurationReadTool.cs
// Author: Kyle L. Crowder
// Build Date: 2026/07/07



using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;

using Microsoft.Extensions.AI;




namespace SentinelCoreLib.Tools;





/// <summary>
///     Read-only tool for querying system boot configuration using the BCD store APIs.
/// </summary>
public sealed class BootConfigurationReadTool : AITool
{

    private static readonly Guid UnsafeNullGuid = Guid.Empty;








    [Description("Returns the current boot entry GUID from the BCD store.")]
    public Task<ToolResult> bcdedit_current()
    {
        try
        {
            int hResult = NativeMethods.BcdOpenStore(null, out IntPtr store);
            if (hResult < 0 || store == IntPtr.Zero)
            {
                return Task.FromResult(ToolResult.FailureResult("Failed to open the BCD store."));
            }

            try
            {
                IntPtr guid = Marshal.AllocHGlobal(16);
                try
                {
                    int zero = 0;
                    Guid nullGuid = Guid.Empty;
                    hResult = NativeMethods.BcdGetElementData(store, ref nullGuid, BcdLibraryElementType.BcdLibraryObjectType_CurrentBootEntry, guid, ref zero, out _);
                    if (hResult < 0)
                    {
                        return Task.FromResult(ToolResult.FailureResult("Failed to query current BCD entry."));
                    }

                    Guid currentGuid = Marshal.PtrToStructure<Guid>(guid);
                    return Task.FromResult(ToolResult.SuccessResult($"CurrentBootEntry={currentGuid:B}"));
                }
                finally
                {
                    Marshal.FreeHGlobal(guid);
                }
            }
            finally
            {
                if (store != IntPtr.Zero)
                {
                    NativeMethods.BcdCloseStore(store);
                }
            }
        }
        catch (Exception ex)
        {
            return Task.FromResult(ToolResult.FailureResult($"BCD current entry read failed: {ex.Message}"));
        }
    }








    [Description("Enumerates the active boot configuration store entries.")]
    public Task<ToolResult> bcdedit_enum()
    {
        try
        {
            StringBuilder sb = new();
            int hResult = NativeMethods.BcdOpenStore(null, out IntPtr store);
            if (hResult < 0 || store == IntPtr.Zero)
            {
                return Task.FromResult(ToolResult.FailureResult("Failed to open the BCD store."));
            }

            try
            {
                hResult = NativeMethods.BcdEnumerateAndUnpackEntries(store, IntPtr.Zero, BcdLibraryDeviceType.BcdLibraryDeviceTypeBootDevice, 0, IntPtr.Zero, out int count, IntPtr.Zero);

                if (hResult < 0)
                {
                    return Task.FromResult(ToolResult.FailureResult("Failed to enumerate BCD entries."));
                }

                sb.AppendLine($"BcdEnumerateAndUnpackEntries returned {count} entries.");
                sb.AppendLine("Use bcdedit /enum for full human-readable details.");
                return Task.FromResult(ToolResult.SuccessResult(sb.ToString()));
            }
            finally
            {
                if (store != IntPtr.Zero)
                {
                    NativeMethods.BcdCloseStore(store);
                }
            }
        }
        catch (Exception ex)
        {
            return Task.FromResult(ToolResult.FailureResult($"BCD enumeration failed: {ex.Message}"));
        }
    }








    private static class NativeMethods
    {
        private const string BcdDll = "bcd.dll";








        [DllImport(BcdDll, CharSet = CharSet.Unicode, SetLastError = false)]
        public static extern int BcdCloseStore(IntPtr storeHandle);








        [DllImport(BcdDll, CharSet = CharSet.Unicode, SetLastError = false)]
        public static extern int BcdEnumerateAndUnpackEntries(IntPtr storeHandle, IntPtr template, BcdLibraryDeviceType deviceType, uint flags, IntPtr buffer, out int count, IntPtr returnedBufferLength);








        [DllImport(BcdDll, CharSet = CharSet.Unicode, SetLastError = false)]
        public static extern int BcdGetElementData(IntPtr storeHandle, ref Guid objectGuid, BcdLibraryElementType elementType, IntPtr buffer, ref int bufferSize, out int returnedLength);








        [DllImport(BcdDll, CharSet = CharSet.Unicode, SetLastError = false)]
        public static extern int BcdOpenStore(string? fileName, out IntPtr storeHandle);
    }





    private enum BcdLibraryDeviceType : uint
    {
        BcdLibraryDeviceTypeBootDevice = 0x00000001
    }





    private enum BcdLibraryElementType : uint
    {
        BcdLibraryObjectType_CurrentBootEntry = 0x12000002
    }
}