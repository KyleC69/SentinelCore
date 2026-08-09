// Solution: SentinelCore
// Project:   SentinelCore.Tests
// File:         EventPublishingChatClientTests.cs
// Author: Kyle L. Crowder
// Build Num:  080801



using SentinelCore.Events;
using SentinelCore.Tests.TestInfrastructure;




namespace SentinelCore.Tests;





/// <summary>
///     Tests for <see cref="EventPublishingChatClient" /> — verifies that
///     <see cref="EventPublishingChatClient.PublishTextOutput" /> publishes
///     events through the unified <see cref="ISentinelCoreEvents.SentinelOutputEvent" /> channel.
/// </summary>
[TestClass]
public sealed class EventPublishingChatClientTests
{

    [TestMethod]
    public void Constructor_NullAgentName_Throws()
    {
        EventCapture events = new EventCapture();
        FakeChatClient inner = new FakeChatClient(CreateTextResponse("x"));
        Assert.Throws<ArgumentNullException>(() => new EventPublishingChatClient(inner, events, null!, NoOpLoggerFactory.CreateLogger<EventPublishingChatClient>()));
    }








    [TestMethod]
    public void Constructor_NullEvents_Throws()
    {
        FakeChatClient inner = new FakeChatClient(CreateTextResponse("x"));
        Assert.Throws<ArgumentNullException>(() => new EventPublishingChatClient(inner, null!, "TheCore", NoOpLoggerFactory.CreateLogger<EventPublishingChatClient>()));
    }








    private static ChatResponse CreateTextResponse(string text)
    {
        return new ChatResponse(new ChatMessage(ChatRole.Assistant, text));
    }








    [TestMethod]
    public void PublishTextOutput_CoreActivity_RaisesSentinelOutputEvent()
    {
        EventCapture events = new EventCapture();
        FakeChatClient inner = new FakeChatClient(CreateTextResponse("x"));
        EventPublishingChatClient client = new(inner, events, "TheCore", NoOpLoggerFactory.CreateLogger<EventPublishingChatClient>());

        client.PublishTextOutput("investigation hypothesis", ActivityType.Core);

        Assert.AreEqual(1, events.SentinelOutputEvents.Count);
        Assert.AreEqual("TheCore", events.SentinelOutputEvents[0].AgentName);
        Assert.AreEqual("investigation hypothesis", events.SentinelOutputEvents[0].Message);
        Assert.AreEqual(ActivityType.Core, events.SentinelOutputEvents[0].ActivityType);
    }








    [TestMethod]
    public void PublishTextOutput_DefaultActivityType_IsCore()
    {
        EventCapture events = new EventCapture();
        FakeChatClient inner = new FakeChatClient(CreateTextResponse("x"));
        EventPublishingChatClient client = new(inner, events, "TheCore", NoOpLoggerFactory.CreateLogger<EventPublishingChatClient>());

        client.PublishTextOutput("default activity");

        Assert.AreEqual(1, events.SentinelOutputEvents.Count);
        Assert.AreEqual(ActivityType.Core, events.SentinelOutputEvents[0].ActivityType, "Default ActivityType should be Core.");
    }








    [TestMethod]
    public void PublishTextOutput_EmptyText_DoesNotRaiseSentinelOutputEvent()
    {
        EventCapture events = new EventCapture();
        FakeChatClient inner = new FakeChatClient(CreateTextResponse("x"));
        EventPublishingChatClient client = new(inner, events, "TheCore", NoOpLoggerFactory.CreateLogger<EventPublishingChatClient>());

        client.PublishTextOutput("", ActivityType.Core);

        Assert.AreEqual(0, events.SentinelOutputEvents.Count, "Empty text should not raise SentinelOutputEvent.");
    }








    [TestMethod]
    public void PublishTextOutput_ManagerActivity_RaisesSentinelOutputEvent()
    {
        EventCapture events = new EventCapture();
        FakeChatClient inner = new FakeChatClient(CreateTextResponse("x"));
        EventPublishingChatClient client = new(inner, events, "WorkflowManager", NoOpLoggerFactory.CreateLogger<EventPublishingChatClient>());

        client.PublishTextOutput("executing plan", ActivityType.Manager);

        Assert.AreEqual(1, events.SentinelOutputEvents.Count);
        Assert.AreEqual("WorkflowManager", events.SentinelOutputEvents[0].AgentName);
        Assert.AreEqual(ActivityType.Manager, events.SentinelOutputEvents[0].ActivityType);
    }








    [TestMethod]
    public void PublishTextOutput_ParticipantActivity_RaisesSentinelOutputEvent()
    {
        EventCapture events = new EventCapture();
        FakeChatClient inner = new FakeChatClient(CreateTextResponse("x"));
        EventPublishingChatClient client = new(inner, events, "registry_agent", NoOpLoggerFactory.CreateLogger<EventPublishingChatClient>());

        client.PublishTextOutput("registry scan complete", ActivityType.Participant);

        Assert.AreEqual(1, events.SentinelOutputEvents.Count);
        Assert.AreEqual("registry_agent", events.SentinelOutputEvents[0].AgentName);
        Assert.AreEqual(ActivityType.Participant, events.SentinelOutputEvents[0].ActivityType);
    }








    [TestMethod]
    public void PublishTextOutput_ReasoningActivity_RaisesSentinelOutputEvent()
    {
        EventCapture events = new EventCapture();
        FakeChatClient inner = new FakeChatClient(CreateTextResponse("x"));
        EventPublishingChatClient client = new(inner, events, "TheCore", NoOpLoggerFactory.CreateLogger<EventPublishingChatClient>());

        client.PublishTextOutput("thinking step", ActivityType.Reasoning);

        Assert.AreEqual(1, events.SentinelOutputEvents.Count);
        Assert.AreEqual(ActivityType.Reasoning, events.SentinelOutputEvents[0].ActivityType);
    }








    [TestMethod]
    public void PublishTextOutput_ToolingActivity_RaisesSentinelOutputEvent()
    {
        EventCapture events = new EventCapture();
        FakeChatClient inner = new FakeChatClient(CreateTextResponse("x"));
        EventPublishingChatClient client = new(inner, events, "TheCore", NoOpLoggerFactory.CreateLogger<EventPublishingChatClient>());

        client.PublishTextOutput("tool result", ActivityType.Tooling);

        Assert.AreEqual(1, events.SentinelOutputEvents.Count);
        Assert.AreEqual(ActivityType.Tooling, events.SentinelOutputEvents[0].ActivityType);
    }








    [TestMethod]
    public void PublishTextOutput_WhitespaceText_DoesNotRaiseSentinelOutputEvent()
    {
        EventCapture events = new EventCapture();
        FakeChatClient inner = new FakeChatClient(CreateTextResponse("x"));
        EventPublishingChatClient client = new(inner, events, "TheCore", NoOpLoggerFactory.CreateLogger<EventPublishingChatClient>());

        client.PublishTextOutput("   ", ActivityType.Core);

        Assert.AreEqual(0, events.SentinelOutputEvents.Count, "Whitespace text should not raise SentinelOutputEvent.");
    }
}