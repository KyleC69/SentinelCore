using System.ServiceProcess;

namespace WindowsService1
{
    public sealed partial class SentinelCoreService : ServiceBase
    {
        public SentinelCoreService()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            // Log service start
            System.Diagnostics.EventLog.WriteEntry("SentinelCoreService", "Service is starting.", System.Diagnostics.EventLogEntryType.Information);

            // Initialize necessary resources or start background tasks
            // Example: Start a background worker or timer
            System.Threading.Tasks.Task.Run(() =>
            {
                // Simulate background work
                System.Diagnostics.EventLog.WriteEntry("SentinelCoreService", "Background task is running.", System.Diagnostics.EventLogEntryType.Information);
            });
        }

        protected override void OnStop()
        {
            // Log service stop
            System.Diagnostics.EventLog.WriteEntry("SentinelCoreService", "Service is stopping.", System.Diagnostics.EventLogEntryType.Information);

            // Clean up resources or stop background tasks
            // Example: Stop a background worker or timer
            System.Diagnostics.EventLog.WriteEntry("SentinelCoreService", "Background task is stopping.", System.Diagnostics.EventLogEntryType.Information);
        }
    }
}
