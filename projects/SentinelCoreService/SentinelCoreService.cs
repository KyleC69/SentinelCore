// Solution: SentinelCore
// Project:   SentinelCoreService
// File:         SentinelCoreService.cs
// Author: Kyle L. Crowder
// Build Num:  092308



using System.Diagnostics;
using System.ServiceProcess;
using System.Threading.Tasks;




namespace SentinelCore;





public sealed partial class SentinelCoreService : ServiceBase
{
    public SentinelCoreService()
    {
        InitializeComponent();
    }








    protected override void OnStart(string[] args)
    {
        // Log service start
        EventLog.WriteEntry("SentinelCoreService", "Service is starting.", EventLogEntryType.Information);

        // Initialize necessary resources or start background tasks
        // Example: Start a background worker or timer
        Task.Run(() =>
        {
            // Simulate background work
            EventLog.WriteEntry("SentinelCoreService", "Background task is running.", EventLogEntryType.Information);
        });
    }








    protected override void OnStop()
    {
        // Log service stop
        EventLog.WriteEntry("SentinelCoreService", "Service is stopping.", EventLogEntryType.Information);

        // Clean up resources or stop background tasks
        // Example: Stop a background worker or timer
        EventLog.WriteEntry("SentinelCoreService", "Background task is stopping.", EventLogEntryType.Information);
    }
}