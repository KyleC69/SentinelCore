// Solution: SentinelCore
// Project:   SentinelCoreAdmin
// File:         IdentityCacheService.cs
// Author: Kyle L. Crowder
// Build Num:  081602



using System.IO;
using System.Reflection;
using System.Security.Cryptography;

using SentinelCoreAdmin.Core.Contracts.Services;




namespace SentinelCoreAdmin.Services;





/// <summary>
///     DPAPI-backed MSAL token cache. Token data is encrypted with
///     <see cref="DataProtectionScope.CurrentUser" /> before being
///     written to disk so only the current Windows user can decrypt it.
/// </summary>
public class IdentityCacheService : IIdentityCacheService
{

    private readonly object _fileLock = new();

    private static readonly string CacheDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), Assembly.GetExecutingAssembly().GetName().Name);

    private static readonly string CacheFilePath = Path.Combine(CacheDirectory, ".msalcache.bin3");








    public byte[] ReadMsalToken()
    {
        lock (_fileLock)
        {
            if (!File.Exists(CacheFilePath))
            {
                return null;
            }

            byte[] encryptedData = File.ReadAllBytes(CacheFilePath);
            return ProtectedData.Unprotect(encryptedData, null, DataProtectionScope.CurrentUser);
        }
    }








    public void SaveMsalToken(byte[] token)
    {
        lock (_fileLock)
        {
            if (!Directory.Exists(CacheDirectory))
            {
                Directory.CreateDirectory(CacheDirectory);
            }

            byte[] encryptedData = ProtectedData.Protect(token, null, DataProtectionScope.CurrentUser);
            File.WriteAllBytes(CacheFilePath, encryptedData);
        }
    }
}