// Solution: SentinelCoreLib
// Project:   SentinelCoreLib
// File:         CredentialsReadTool.cs
// Author: Kyle L. Crowder
// Build Date: 2026/07/07



using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;

using Microsoft.Extensions.AI;




namespace SentinelCoreLib.Tools;





/// <summary>
///     Read-only tool for enumerating generic credential targets stored in Windows Credential Manager.
///     Uses P/Invoke over CredEnumerate (NOT CredRead, to avoid exposing secrets).
/// </summary>
public sealed class CredentialsReadTool : AITool
{
    [Description("Lists the names (targets) of stored Windows credentials without reading passwords.")]
    public Task<ToolResult> credential_list_targets()
    {
        try
        {
            int count = 0;
            StringBuilder sb = new();
            if (NativeMethods.CredEnumerate(null, 0, out int credentialCount, out IntPtr credentialArray))
            {
                try
                {
                    for (int i = 0; i < credentialCount; i++)
                    {
                        IntPtr credential = Marshal.ReadIntPtr(credentialArray, i * IntPtr.Size);
                        string? targetName = Marshal.PtrToStringUni(credential);
                        if (!string.IsNullOrWhiteSpace(targetName))
                        {
                            sb.AppendLine($"Target={targetName}");
                            count++;
                        }
                    }
                }
                finally
                {
                    if (credentialArray != IntPtr.Zero)
                    {
                        NativeMethods.CredFree(credentialArray);
                    }
                }
            }
            else
            {
                int error = Marshal.GetLastWin32Error();
                if (error != 1168) // ERROR_NOT_FOUND
                {
                    return Task.FromResult(ToolResult.FailureResult($"Credential enumeration failed with error {error}"));
                }
            }

            sb.Insert(0, $"CredentialCount={count}\n");
            return Task.FromResult(ToolResult.SuccessResult(sb.ToString()));
        }
        catch (Exception ex)
        {
            return Task.FromResult(ToolResult.FailureResult($"Credential target listing failed: {ex.Message}"));
        }
    }








    private static class NativeMethods
    {
        private const string Advapi32 = "advapi32.dll";








        [DllImport(Advapi32, SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern bool CredEnumerate(string? filter, int flags, out int count, out IntPtr credentials);








        [DllImport(Advapi32, SetLastError = false)]
        public static extern void CredFree(IntPtr buffer);
    }
}