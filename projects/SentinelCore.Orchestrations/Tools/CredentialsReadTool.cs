// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         CredentialsReadTool.cs
// Author: Kyle L. Crowder
// Build Num:  081602



using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;




namespace SentinelCore.Tools;





/// <summary>
///     Read-only tool for enumerating generic credential targets stored in Windows Credential Manager.
///     Uses P/Invoke over CredEnumerate (NOT CredRead, to avoid exposing secrets).
/// </summary>
public sealed class CredentialsReadTool : AITool
{
    public override string Description { get; } = "Read-only tool for enumerating credential targets stored in Windows Credential Manager.";
    public override string Name { get; } = "Credentials_Read";








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
                    if (credentialArray != IntPtr.Zero) NativeMethods.CredFree(credentialArray);
                }
            }
            else
            {
                int error = Marshal.GetLastWin32Error();
                if (error != 1168) // ERROR_NOT_FOUND
                    return Task.FromResult(ToolResult.Fail($"Credential enumeration failed with error {error}"));
            }

            sb.Insert(0, $"CredentialCount={count}\n");
            return Task.FromResult(ToolResult.Ok(sb.ToString()));
        }
        catch (Exception ex)
        {
            return Task.FromResult(ToolResult.Fail($"Credential target listing failed: {ex.Message}"));
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