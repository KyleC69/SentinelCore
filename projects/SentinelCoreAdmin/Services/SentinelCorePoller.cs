// Solution: SentinelCore
// Project:   SentinelCoreAdmin
// File:         SentinelCorePoller.cs
// Author: Kyle L. Crowder
// Build Num:  082808



using System.Diagnostics;
using System.ServiceProcess;




namespace SentinelCoreAdmin;





/// <summary>
///     Represents a Windows Service responsible for managing and executing TheCore
///     to process any cases that are in a states that requires processing. This service is designed to handle background
///     polling or periodic processing tasks within the Sentinel Core. Anomaly detectors (future module) and other windows
///     components that can feed TheCore signals
///     require the system to be active to process.
/// </summary>
/// <remarks>
///     This service is designed to create an almost autonomous AI application that runs continuously in the background.
///     Current AI applications are not designed to run continuously, and this service aims to address that limitation by
///     providing a framework for background processing
///     or periodic task execution. Another approach to achieve this is to use the Windows Task Scheduler to schedule the
///     execution of TheCore at regular intervals.
///     However, this service provides a more integrated and controlled solution for managing background tasks within the
///     Sentinel Core application.
///     Research and development of this service are ongoing, and it is expected to evolve over time to meet the needs of
///     the Sentinel Core application and its users.
///     The rapid advancement of AI technology and the increasing demand for autonomous applications make this service a
///     valuable addition to the Sentinel Core ecosystem.
/// </remarks>
internal partial class SentinelCorePoller : ServiceBase
{
    public SentinelCorePoller()
    {
        InitializeComponent();
    }








    protected override void OnStart(string[] args)
    {
        // Initialize necessary resources or configurations for the service.
        // Log the service start event for monitoring purposes.
        EventLog.WriteEntry("SentinelCorePoller service is starting.", EventLogEntryType.Information);

        // Start the main polling or processing logic of the service.
        Task.Run(() =>
        {
            try
            {
                // Replace with actual polling or processing logic.
                // Will process cases that are in an initialized or open state, these are the only states that have not been seen by TheCore yet.
                // TheCore will process these cases and update their state accordingly. This must happen regularly to ensure critical conditions are detected and handled in a timely manner.
                while (true)
                        // Example: Polling logic or periodic task execution.
                    Thread.Sleep(1000); // Simulate work with a delay.
            }
            catch (Exception ex)
            {
                // Log any exceptions that occur during execution.
                EventLog.WriteEntry($"An error occurred: {ex.Message}", EventLogEntryType.Error);
            }
        });
    }








    protected override void OnStop()
    {
        // Log the service stop event for monitoring purposes.
        EventLog.WriteEntry("SentinelCorePoller service is stopping.", EventLogEntryType.Information);

        // Perform any necessary cleanup or resource disposal.
        // Example: Signal any running tasks to stop and wait for their completion.
        try
        {
            // Add logic to gracefully terminate any ongoing operations.
            // For example, setting a cancellation token or stopping threads.
        }
        catch (Exception ex)
        {
            // Log any exceptions that occur during the stop process.
            EventLog.WriteEntry($"An error occurred while stopping: {ex.Message}", EventLogEntryType.Error);
        }
    }
}