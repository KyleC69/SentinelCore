// Solution: SentinelCore
// Project:   SentinelCore.Tests
// File:         SentinelCoreEventsTests.cs
// Author: Kyle L. Crowder
// Build Num:  080801



using SentinelCore.Events;
using SentinelCore.Tests.TestInfrastructure;




namespace SentinelCore.Tests;





/// <summary>
///     Tests for <see cref="SentinelCoreEvents" /> — verifies that the event hub
///     correctly invokes subscribers and that <see cref="EventCapture" /> records
///     all raises. These tests prevent drift in the event contract that the Host UI
///     depends on.
/// </summary>
[TestClass]
public sealed class SentinelCoreEventsTests
{

    [TestMethod]
    public void EventCapture_Clear_ResetsAllLists()
    {
        EventCapture capture = new EventCapture();
        capture.RaiseSentinelOutputEvent(new SentinelOutputEventArgs("a", "1", ActivityType.Core));
        capture.RaiseOrchestrationEvent(new OrchestrationActivityArgs("msg", "src"));
        capture.RaiseError("err", new Exception("test"));

        capture.Clear();

        Assert.AreEqual(0, capture.SentinelOutputEvents.Count);
        Assert.AreEqual(0, capture.OrchestrationEvents.Count);
        Assert.AreEqual(0, capture.ErrorEvents.Count);
    }








    [TestMethod]
    public void EventCapture_RaisesAllChannels()
    {
        EventCapture capture = new EventCapture();

        capture.RaiseSentinelOutputEvent(new SentinelOutputEventArgs("TheCore", "output", ActivityType.Core));
        capture.RaiseOrchestrationEvent(new OrchestrationActivityArgs("msg", "src"));
        capture.RaiseError("err", new Exception("test"));

        Assert.AreEqual(1, capture.SentinelOutputEvents.Count);
        Assert.AreEqual(1, capture.OrchestrationEvents.Count);
        Assert.AreEqual(1, capture.ErrorEvents.Count);
    }








    [TestMethod]
    public void RaiseError_WithSubscriber_InvokesSubscriber()
    {
        SentinelCoreEvents events = new SentinelCoreEvents();
        string? receivedMessage = null;
        Exception? receivedException = null;
        events.ErrorOccurred += (msg, ex) =>
        {
            receivedMessage = msg;
            receivedException = ex;
        };

        Exception testEx = new InvalidOperationException("test");
        events.RaiseError("something failed", testEx);

        Assert.AreEqual("something failed", receivedMessage);
        Assert.AreSame(testEx, receivedException);
    }








    [TestMethod]
    public void RaiseOrchestrationEvent_WithSubscriber_InvokesSubscriber()
    {
        SentinelCoreEvents events = new SentinelCoreEvents();
        OrchestrationActivityArgs? received = null;
        events.OrchestrationEvent += args => received = args;

        OrchestrationActivityArgs payload = new OrchestrationActivityArgs("starting workflow", "Orchestrator");
        events.RaiseOrchestrationEvent(payload);

        Assert.IsNotNull(received);
        Assert.AreEqual("starting workflow", received.Message);
        Assert.AreEqual("Orchestrator", received.Source);
    }








    [TestMethod]
    public void RaiseSentinelOutputEvent_CapturesAllActivityTypes()
    {
        SentinelCoreEvents events = new SentinelCoreEvents();
        List<SentinelOutputEventArgs> received = [];
        events.SentinelOutputEvent += args => received.Add(args);

        events.RaiseSentinelOutputEvent(new SentinelOutputEventArgs("TheCore", "reasoning", ActivityType.Reasoning));
        events.RaiseSentinelOutputEvent(new SentinelOutputEventArgs("TheCore", "tool result", ActivityType.Tooling));
        events.RaiseSentinelOutputEvent(new SentinelOutputEventArgs("WorkflowManager", "plan", ActivityType.Manager));
        events.RaiseSentinelOutputEvent(new SentinelOutputEventArgs("registry_agent", "scan", ActivityType.Participant));
        events.RaiseSentinelOutputEvent(new SentinelOutputEventArgs("System", "info", ActivityType.System));

        Assert.AreEqual(5, received.Count);
        Assert.AreEqual(ActivityType.Reasoning, received[0].ActivityType);
        Assert.AreEqual(ActivityType.Tooling, received[1].ActivityType);
        Assert.AreEqual(ActivityType.Manager, received[2].ActivityType);
        Assert.AreEqual(ActivityType.Participant, received[3].ActivityType);
        Assert.AreEqual(ActivityType.System, received[4].ActivityType);
    }








    [TestMethod]
    public void RaiseSentinelOutputEvent_WithNoSubscriber_DoesNotThrow()
    {
        SentinelCoreEvents events = new SentinelCoreEvents();

        events.RaiseSentinelOutputEvent(new SentinelOutputEventArgs("TheCore", "output", ActivityType.Core));
    }








    [TestMethod]
    public void RaiseSentinelOutputEvent_WithSubscriber_InvokesSubscriber()
    {
        SentinelCoreEvents events = new SentinelCoreEvents();
        SentinelOutputEventArgs? received = null;
        events.SentinelOutputEvent += args => received = args;

        SentinelOutputEventArgs payload = new SentinelOutputEventArgs("TheCore", "activity output", ActivityType.Core);
        events.RaiseSentinelOutputEvent(payload);

        Assert.IsNotNull(received);
        Assert.AreEqual("TheCore", received.AgentName);
        Assert.AreEqual("activity output", received.Message);
        Assert.AreEqual(ActivityType.Core, received.ActivityType);
    }








    [TestMethod]
    public void SentinelOutputEventArgs_Constructor_SetsProperties()
    {
        SentinelOutputEventArgs args = new SentinelOutputEventArgs("TheCore", "hypothesis", ActivityType.Core);

        Assert.AreEqual("TheCore", args.AgentName);
        Assert.AreEqual("hypothesis", args.Message);
        Assert.AreEqual(ActivityType.Core, args.ActivityType);
    }
}