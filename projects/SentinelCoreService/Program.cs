// Solution: SentinelCore
// Project:   SentinelCoreService
// File:         Program.cs
// Author: Kyle L. Crowder
// Build Num:  082808



using System.ServiceProcess;




namespace SentinelCore;





internal static class Program
{
    /// <summary>
    ///     The main entry point for the application.
    /// </summary>
    private static void Main()
    {
        ServiceBase[] ServicesToRun;
        ServicesToRun = new ServiceBase[] { new SentinelCoreService() };
        ServiceBase.Run(ServicesToRun);
    }
}