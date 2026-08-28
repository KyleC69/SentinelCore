// Solution: SentinelCore
// Project:   SentinelCoreAdmin
// File:         ApplicationInfoService.cs
// Author: Kyle L. Crowder
// Build Num:  082808



using System.Diagnostics;
using System.Reflection;

using SentinelCoreAdmin.Contracts.Services;




namespace SentinelCoreAdmin.Services;





public class ApplicationInfoService : IApplicationInfoService
{

    public Version GetVersion()
    {
        // Set the app version in SentinelCoreAdmin > Properties > Package > PackageVersion
        string assemblyLocation = Assembly.GetExecutingAssembly().Location;
        string? version = FileVersionInfo.GetVersionInfo(assemblyLocation).FileVersion;
        return new Version(string.IsNullOrEmpty(version) ? "0.0.0.0" : version);
    }
}