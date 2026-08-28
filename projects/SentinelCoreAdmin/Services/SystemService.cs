// Solution: SentinelCore
// Project:   SentinelCoreAdmin
// File:         SystemService.cs
// Author: Kyle L. Crowder
// Build Num:  082808



using System.Diagnostics;

using JetBrains.Annotations;

using SentinelCoreAdmin.Contracts.Services;




namespace SentinelCoreAdmin.Services;





public class SystemService : ISystemService
{

    public void OpenInWebBrowser([CanBeNull] string url)
    {
        // For more info see https://github.com/dotnet/corefx/issues/10361
        ProcessStartInfo psi = new() { FileName = url, UseShellExecute = true };
        Process.Start(psi);
    }
}