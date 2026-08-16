// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         PInvokeHelper.cs
// Author: Kyle L. Crowder
// Build Num:  081602



using System.ComponentModel;
using System.Runtime.InteropServices;




namespace SentinelCore.Tools.Interop;





/// <summary>
///     Shared P/Invoke helpers used by the read-only system query tools.
///     Only wraps documented Win32 functions; no custom semantics are added.
/// </summary>
internal static class PInvokeHelper
{
    public static int FindNullTerminator(ReadOnlySpan<byte> bytes)
    {
        for (int i = 0; i < bytes.Length - 1; i += 2)
            if (bytes[i] == 0 && bytes[i + 1] == 0)
                return i;

        return bytes.Length;
    }








    public static string? GetLastErrorMessage()
    {
        Win32Exception ex = new(Marshal.GetLastWin32Error());
        return ex.Message;
    }








    public static string? PtrToStringUni(IntPtr ptr)
    {
        if (ptr == IntPtr.Zero) return null;

        return Marshal.PtrToStringUni(ptr);
    }
}