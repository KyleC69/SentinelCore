// Solution: SentinelCore
// Project:   SentinelCore.Tests
// File:         SentinelCoreDiagnosticsTests.cs
// Author: Kyle L. Crowder
// Build Num:  110813



using System.Diagnostics;

using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using Moq;

using OpenTelemetry;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

using SentinelCore.Contracts.Contracts;
using SentinelCore.Contracts.Mcp;
using SentinelCore.Orchestrations.Abstractions;
using SentinelCore.Orchestrations.Agents.AgentPresets;
using SentinelCore.Orchestrations.Agents.Middleware;
using SentinelCore.Orchestrations.Infrastructure.Diagnostics;
using SentinelCore.Orchestrations.Workflows.Executors;
using SentinelCore.Tests.TestInfrastructure;




namespace SentinelCore.Tests;




/// <summary>
///     Locks the telemetry contract between the agent pipeline (which emits spans) and the host
///     (which must subscribe to <see cref="SentinelCoreDiagnostics.ActivitySourceName" />).
/// </summary>
/// <remarks>
///     Spans are silently discarded when no tracer provider subscribes to the emitted source name, which
///     is exactly how agent tracing was previously lost. Subscribing an <see cref="ActivityListener" /> to
///     the shared constant turns that silent loss into a test failure.
/// </remarks>
[TestClass]
public sealed class SentinelCoreDiagnosticsTests
{
    private const string PromptText = "ping";








    /// <summary>
    ///     Verifies that running an agent produced by <see cref="SentinelAgentFactory" /> emits at least one
    ///     activity under <see cref="SentinelCoreDiagnostics.ActivitySourceName" />.
    /// </summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [TestMethod]
    public async Task FactoryCreatedAgent_WhenRun_EmitsActivityUnderSentinelCoreSourceAsync()
    {
        // Arrange
        List<Activity> captured = new();
        using ActivityListener listener = CreateListener(captured);
        ActivitySource.AddActivityListener(listener);

        SentinelAgentFactory factory = CreateFactory();
        AIAgent agent = await factory.CreateAgentAsync(AgentPreset.CoreAgent);

        // Act
        await agent.RunAsync(PromptText);

        // Assert
        Activity[] activities;
        lock (captured)
        {
            activities = captured.ToArray();
        }

        Assert.IsTrue(activities.Length > 0, $"Expected at least one activity from source '{SentinelCoreDiagnostics.ActivitySourceName}', but none were emitted. The agent pipeline is no longer tracing under the shared source name.");
        Assert.IsTrue(Array.TrueForAll(activities, activity => activity.Source.Name == SentinelCoreDiagnostics.ActivitySourceName), "Every captured activity must originate from the shared source name.");
    }








    /// <summary>
    ///     Verifies that a workflow configured with SentinelCore telemetry emits its build span under the
    ///     shared source name instead of the workflow framework's internal default source.
    /// </summary>
    /// <remarks>
    ///     Workflow telemetry is disabled unless <c>WithOpenTelemetry</c> is applied, and the framework's
    ///     default source name is private. Pointing the workflow at the shared source is what lets a
    ///     single host subscription export agent and workflow spans together.
    /// </remarks>
    [TestMethod]
    public void WorkflowWithSentinelTelemetry_WhenBuilt_EmitsActivityUnderSentinelCoreSource()
    {
        // Arrange
        List<Activity> captured = new();
        using ActivityListener listener = CreateListener(captured);
        ActivitySource.AddActivityListener(listener);

        // Act
        Workflow workflow = new WorkflowBuilder(new EvaluationExec("telemetry-probe"))
                           .WithOpenTelemetry(activitySource: SentinelCoreDiagnostics.SharedActivitySource)
                           .Build();

        // Assert
        Assert.IsNotNull(workflow);
        Assert.IsTrue(captured.Count > 0, $"Expected the workflow build to emit at least one activity from source '{SentinelCoreDiagnostics.ActivitySourceName}', but none were emitted. Workflow telemetry is no longer routed to the shared source.");
        Assert.IsTrue(captured.TrueForAll(activity => activity.Source.Name == SentinelCoreDiagnostics.ActivitySourceName), "Every workflow activity must originate from the shared source name.");
    }







    /// <summary>
    ///     Verifies that the shared process resource reports <see cref="SentinelCoreDiagnostics.ServiceName" />
    ///     instead of the OpenTelemetry default <c>unknown_service:&lt;process name&gt;</c>.
    /// </summary>
    /// <remarks>
    ///     Every pipeline builds its own resource, so a pipeline configured without this factory exports
    ///     the default name and its telemetry cannot be correlated with the rest of the process. That
    ///     mismatch is what this test locks down.
    /// </remarks>
    [TestMethod]
    public void CreateResourceBuilder_ReportsSentinelCoreAsServiceName()
    {
        // Act
        Resource resource = SentinelCoreDiagnostics.CreateResourceBuilder().Build();

        // Assert
        string? serviceName = GetServiceName(resource);
        Assert.AreEqual(SentinelCoreDiagnostics.ServiceName, serviceName, "The shared resource must report the SentinelCore service name.");
        Assert.IsFalse(serviceName?.StartsWith("unknown_service", StringComparison.Ordinal) ?? true, "The resource fell back to the OpenTelemetry default, so this pipeline's telemetry cannot be correlated with the rest of the process.");
    }







    /// <summary>
    ///     Verifies that a tracer provider configured the way the host configures it reports the shared
    ///     service name rather than the OpenTelemetry default.
    /// </summary>
    /// <remarks>
    ///     The logging pipeline consumes the same factory, but its resource cannot be asserted directly:
    ///     <c>OpenTelemetryLoggerOptions</c> exposes no readable resource and <c>LogRecord</c> carries no
    ///     resource reference. This test therefore guards the shared factory end to end through a real
    ///     provider, and the host's two <c>SetResourceBuilder</c> call sites are covered by review.
    /// </remarks>
    [TestMethod]
    public void TracerProvider_WhenConfiguredWithSharedResource_ReportsSentinelCoreAsServiceName()
    {
        // Arrange
        using TracerProvider provider = Sdk.CreateTracerProviderBuilder()
                                           .SetResourceBuilder(SentinelCoreDiagnostics.CreateResourceBuilder())
                                           .Build();

        // Act
        string? serviceName = GetServiceName(provider.GetResource());

        // Assert
        Assert.AreEqual(SentinelCoreDiagnostics.ServiceName, serviceName, "The exporter would stamp 'unknown_service:<process name>' on every span.");
    }







    /// <summary>
    ///     Verifies that spans do not carry prompt, response, or tool payloads unless the operator opts in.
    /// </summary>
    [TestMethod]
    public void IsSensitiveDataCaptureEnabled_WhenEnvironmentVariableIsUnset_ReturnsFalse()    {
        WithSensitiveDataVariable(null, () => Assert.IsFalse(SentinelCoreDiagnostics.IsSensitiveDataCaptureEnabled, "Sensitive payload capture must default to off."));
    }







    /// <summary>
    ///     Verifies that the environment variable turns sensitive payload capture on.
    /// </summary>
    [TestMethod]
    public void IsSensitiveDataCaptureEnabled_WhenEnvironmentVariableIsTrue_ReturnsTrue()
    {
        WithSensitiveDataVariable("true", () => Assert.IsTrue(SentinelCoreDiagnostics.IsSensitiveDataCaptureEnabled, "The opt-in environment variable must enable sensitive payload capture."));
    }







    /// <summary>
    ///     Verifies that the sensitive-data gate actually reaches the agent pipeline: with the gate off,
    ///     no span may carry the prompt text.
    /// </summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [TestMethod]
    public async Task AgentSpans_WhenSensitiveDataIsNotEnabled_DoNotCarryPromptContentAsync()
    {
        string? original = Environment.GetEnvironmentVariable(SentinelCoreDiagnostics.SensitiveDataEnvironmentVariable);
        List<Activity> captured = new();
        using ActivityListener listener = CreateListener(captured);
        ActivitySource.AddActivityListener(listener);

        try
        {
            Environment.SetEnvironmentVariable(SentinelCoreDiagnostics.SensitiveDataEnvironmentVariable, null);

            SentinelAgentFactory factory = CreateFactory();
            AIAgent agent = await factory.CreateAgentAsync(AgentPreset.CoreAgent);

            // Act
            await agent.RunAsync(PromptText);

            // Assert
            Activity[] activities;
            lock (captured)
            {
                activities = captured.ToArray();
            }

            Assert.IsTrue(activities.Length > 0, "The agent produced no spans, so the gate could not be observed.");
            Assert.IsFalse(Array.Exists(activities, activity => ContainsPayload(activity, PromptText)), "A span carried the raw prompt while sensitive capture was disabled.");
        }
        finally
        {
            Environment.SetEnvironmentVariable(SentinelCoreDiagnostics.SensitiveDataEnvironmentVariable, original);
        }
    }








    /// <summary>
    ///     Reads the <c>service.name</c> attribute from <paramref name="resource" />.
    /// </summary>
    /// <param name="resource">The resource to inspect.</param>
    /// <returns>The service name, or <see langword="null" /> when the resource does not declare one.</returns>
    private static string? GetServiceName(Resource resource)
    {
        return resource.Attributes.FirstOrDefault(attribute => attribute.Key == "service.name").Value?.ToString();
    }






    /// <summary>
    ///     Determines whether <paramref name="activity" /> recorded <paramref name="payload" /> in its tags or
    ///     in the tags of its events.
    /// </summary>
    /// <param name="activity">The activity to inspect.</param>
    /// <param name="payload">The text that must not appear.</param>
    /// <returns><see langword="true" /> when the payload appears anywhere in the activity's recorded data.</returns>
    private static bool ContainsPayload(Activity activity, string payload)
    {
        if (activity.Tags.Any(tag => tag.Value?.Contains(payload, StringComparison.Ordinal) == true))
        {
            return true;
        }

        return activity.Events.Any(activityEvent => activityEvent.Tags.Any(tag => tag.Value?.ToString()?.Contains(payload, StringComparison.Ordinal) == true));
    }








    /// <summary>
    ///     Runs <paramref name="assertion" /> with the sensitive-data environment variable set to
    ///     <paramref name="value" />, restoring the original value afterwards so the process environment
    ///     is left untouched.
    /// </summary>
    /// <param name="value">The value to assign, or <see langword="null" /> to remove the variable.</param>
    /// <param name="assertion">The assertion to run while the variable holds the requested value.</param>
    private static void WithSensitiveDataVariable(string? value, Action assertion)
    {
        string? original = Environment.GetEnvironmentVariable(SentinelCoreDiagnostics.SensitiveDataEnvironmentVariable);
        try
        {
            Environment.SetEnvironmentVariable(SentinelCoreDiagnostics.SensitiveDataEnvironmentVariable, value);
            assertion();
        }
        finally
        {
            Environment.SetEnvironmentVariable(SentinelCoreDiagnostics.SensitiveDataEnvironmentVariable, original);
        }
    }







    /// <summary>
    ///     Creates an <see cref="ActivityListener" /> that records every activity emitted under the
    ///     SentinelCore source name into <paramref name="captured" />.
    /// </summary>
    /// <param name="captured">The collection that receives stopped activities.</param>
    /// <returns>A listener that must be added to <see cref="ActivitySource.AddActivityListener" /> to take effect.</returns>
    private static ActivityListener CreateListener(List<Activity> captured)
    {
        return new ActivityListener
        {
                ShouldListenTo = source => source.Name == SentinelCoreDiagnostics.ActivitySourceName,
                Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
                ActivityStopped = activity =>
                {
                    lock (captured)
                    {
                        captured.Add(activity);
                    }
                }
        };
    }








    /// <summary>
    ///     Builds a <see cref="SentinelAgentFactory" /> whose external collaborators are deterministic
    ///     test doubles, so the only real behaviour under test is the agent construction pipeline.
    /// </summary>
    /// <returns>A factory ready to create agents without touching the network.</returns>
    private static SentinelAgentFactory CreateFactory()
    {
        Mock<IChatClientFactory> chatClientFactory = new();
        chatClientFactory.Setup(factory => factory.CreateChatClient(It.IsAny<ModelProfile>()))
                         .Returns(() => new FakeChatClient(new ChatResponse(new ChatMessage(ChatRole.Assistant, "pong"))));

        Mock<IModelResolver> modelResolver = new();
        modelResolver.Setup(resolver => resolver.ResolveByTier(It.IsAny<ModelTier>()))
                     .Returns(new ModelProfile("test-model", "http://test.endpoint", 0.7f));

        Mock<IPatternMatcher> patternMatcher = new();

        Mock<ILoggerFactory> loggerFactory = new();
        loggerFactory.Setup(factory => factory.CreateLogger(It.IsAny<string>())).Returns(NullLogger.Instance);

        Mock<IMcpServerRegistry> mcpServerRegistry = new();
        mcpServerRegistry.Setup(registry => registry.GetToolsFromCache()).Returns(new List<AITool> { AIFunctionFactory.Create(() => "ok", "noop") }.AsReadOnly());
        mcpServerRegistry.Setup(registry => registry.ToolCount).Returns(1);

        return new SentinelAgentFactory(chatClientFactory.Object, modelResolver.Object, patternMatcher.Object, loggerFactory.Object, mcpServerRegistry.Object);
    }
}
