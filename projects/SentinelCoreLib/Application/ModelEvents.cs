// Solution: SentinelCoreLib
// Project:   SentinelCoreLib
// File:         ModelEvents.cs
// Author: Kyle L. Crowder
// Build Date: 2026/07/07



namespace SentinelCoreLib.Application;





/// <summary>
///     Represents a collection of events related to core activities, reasoning, and tooling within the application.
/// </summary>
/// <remarks>
///     This class provides mechanisms to subscribe to and handle events that signify various core operations.
/// </remarks>
public class ModelEvents
{
    public event EventHandler? MagnetOrchestrationActivity;








    protected virtual void OnMagneticActivity(MagenticOrchestratorEventArgs e)
    {
        MagnetOrchestrationActivity?.Invoke(this, e);
    }








    protected virtual void OnTheCoreActivity(EventArgs e)
    {
        TheCoreActivity?.Invoke(this, e);
    }








    protected virtual void OnTheCoreReasoning(EventArgs e)
    {
        TheCoreReasoning?.Invoke(this, e);
    }








    protected virtual void OnTheCoreTooling(EventArgs e)
    {
        TheCoreTooling?.Invoke(this, e);
    }








    public event EventHandler? TheCoreActivity;
    public event EventHandler? TheCoreReasoning;
    public event EventHandler? TheCoreTooling;
}