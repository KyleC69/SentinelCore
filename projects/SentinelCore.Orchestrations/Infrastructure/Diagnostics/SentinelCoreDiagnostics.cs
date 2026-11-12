// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         SentinelCoreDiagnostics.cs
// Author: Kyle L. Crowder
// Build Num:  110813

using System.Diagnostics;

using OpenTelemetry.Resources;




namespace SentinelCore.Orchestrations.Infrastructure.Diagnostics;




/// <summary>
///     Exposes the canonical diagnostics identifiers used by SentinelCore instrumentation.
/// </summary>
/// <remarks>
///     A host that wants SentinelCore spans must subscribe to <see cref="ActivitySourceName" /> on its
///     tracer provider (<c>TracerProviderBuilder.AddSource</c>). Without that subscription the
///     <see cref="ActivitySource" /> has no listener, so the spans created by the agent pipeline are
///     never recorded or exported. Emitters and subscribers both read this constant so the two sides
///     cannot drift apart.
/// </remarks>
public static class SentinelCoreDiagnostics
{
    /// <summary>
    ///     The <see cref="ActivitySource" /> name under which SentinelCore emits spans.
    /// </summary>
    /// <remarks>
    ///     Passed to <c>AIAgentBuilder.UseOpenTelemetry</c> by <c>SentinelAgentFactory</c>, used as the
    ///     workflow telemetry source, and registered by the host's tracer provider.
    /// </remarks>
    public const string ActivitySourceName = "SentinelCore";

    /// <summary>
    ///     The OpenTelemetry <c>service.name</c> that identifies this process to every exporter.
    /// </summary>
    /// <remarks>
    ///     Deliberately the same value as <see cref="ActivitySourceName" /> so traces, spans, and log
    ///     records group under one service in a backend. Both are declared rather than aliased because
    ///     they are different semantic conventions: one names the instrumentation library, the other
    ///     names the process. Every OpenTelemetry pipeline in the process must report this name, because
    ///     a pipeline left on the default resource exports <c>unknown_service:&lt;process name&gt;</c> and
    ///     its telemetry can no longer be correlated with the rest of the process.
    /// </remarks>
    public const string ServiceName = "SentinelCore";

    /// <summary>
    ///     Creates the resource that identifies this process to an OpenTelemetry pipeline.
    /// </summary>
    /// <returns>A new <see cref="ResourceBuilder" /> carrying <see cref="ServiceName" />.</returns>
    /// <remarks>
    ///     Every pipeline in the process must pass the result to <c>SetResourceBuilder</c>: the tracer
    ///     provider and the logging pipeline each build their own resource, so configuring only one of
    ///     them produces the <c>unknown_service:&lt;process name&gt;</c> mismatch this method exists to
    ///     prevent. <see cref="ResourceBuilder" /> is mutable and caches the resource it builds, so a
    ///     fresh instance is returned per call instead of a shared singleton.
    /// </remarks>
    public static ResourceBuilder CreateResourceBuilder()
    {
        return ResourceBuilder.CreateDefault().AddService(ServiceName);
    }

    /// <summary>
    ///     The environment variable that opts in to capturing raw prompt, response, and tool payloads
    ///     in spans.
    /// </summary>
    /// <remarks>
    ///     Set to <c>true</c> (case-insensitive) to enable sensitive capture. Any other value, or an
    ///     unset variable, leaves spans limited to metadata such as token counts, operation names, and
    ///     durations.
    /// </remarks>
    public const string SensitiveDataEnvironmentVariable = "SENTINEL_TELEMETRY_SENSITIVE_DATA";

    /// <summary>
    ///     Gets the <see cref="ActivitySource" /> shared by SentinelCore instrumentation.
    /// </summary>
    /// <remarks>
    ///     Workflow telemetry is pointed at this instance through the <c>activitySource</c> argument of
    ///     <c>WithOpenTelemetry</c>. Doing so keeps workflow spans under
    ///     <see cref="ActivitySourceName" /> instead of the workflow framework's internal default source
    ///     name, which is a private implementation detail and therefore not safe to hardcode. The
    ///     instance lives for the process lifetime, matching the framework's own default source.
    ///     <para>
    ///         Workflow spans are metadata only: the framework no longer emits payload attributes, and its
    ///         <c>WorkflowTelemetryOptions.EnableSensitiveData</c> switch is obsolete and has no effect.
    ///         Sensitive payload capture therefore applies to agent spans only, through
    ///         <see cref="IsSensitiveDataCaptureEnabled" />.
    ///     </para>
    /// </remarks>
    public static ActivitySource SharedActivitySource { get; } = new(ActivitySourceName);

    /// <summary>
    ///     Gets a value indicating whether agent spans may include raw prompt, response, and tool payloads.
    /// </summary>
    /// <remarks>
    ///     Sensitive capture is opt-in and off by default because spans carry user prompts, model
    ///     responses, tool arguments, and tool results. The environment variable is read on every access
    ///     so the gate can be exercised by tests and changed between orchestrations without restarting
    ///     the process.
    /// </remarks>
    public static bool IsSensitiveDataCaptureEnabled => bool.TryParse(Environment.GetEnvironmentVariable(SensitiveDataEnvironmentVariable), out bool enabled) && enabled;
}
