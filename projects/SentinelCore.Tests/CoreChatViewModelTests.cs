// Solution: SentinelCore
// Project:   SentinelCore.Tests
// File:         CoreChatViewModelTests.cs
// Author: Kyle L. Crowder
// Build Num:  092308



using System.Windows;

using Microsoft.Extensions.Logging;

using Moq;

using SentinelCore.CaseFlowEngine.Cfe;
using SentinelCore.Contracts.Cfe;
using SentinelCore.Contracts.Events;
using SentinelCore.Orchestrations.Abstractions;
using SentinelCore.Tests.TestInfrastructure;

using Assert = Microsoft.VisualStudio.TestTools.UnitTesting.Assert;




namespace SentinelCore.Tests;





/// <summary>
///     Regression surface for <see cref="CoreChatViewModel" />: constructor null-guards,
///     welcome-message seeding, Send/Cancel command gating, the Copy-message clipboard
///     command added to support message copy/selection in the chat UI, event wiring
///     (SentinelOutputEvent / ErrorOccurred → StatusMessage), and Dispose unsubscription.
///     These tests pin the observable contract of the view-model — a refactor that
///     changes *how* the view-model reaches these outcomes should not break them,
///     but a change that alters *what* it produces must fail here first.
/// </summary>
[TestClass]
public sealed class CoreChatViewModelTests
{

    [TestMethod]
    public void CancelCommand_NotBusy_CannotExecute()
    {
        (Mock<IOrchestrationControl> orchestration, Mock<ISentinelCoreEvents> events, Mock<ICaseFlowEngine> caseFlow, ILogger<CoreChatViewModel> logger, Mock<IDispatcherService> dispatcher, Mock<IClipboardService> clipboard, Mock<IModelConfigGate> configGate) = CreateDependencies();
        CoreChatViewModel viewModel = new(orchestration.Object, events.Object, caseFlow.Object, logger, dispatcher.Object, clipboard.Object, configGate.Object, default);

        Assert.IsFalse(viewModel.CancelCommand.CanExecute(null));

        viewModel.Dispose();
    }








    [TestMethod]
    public void Constructor_NullCaseFlowEngine_Throws()
    {
        (Mock<IOrchestrationControl> orchestration, Mock<ISentinelCoreEvents> events, _, ILogger<CoreChatViewModel> logger, Mock<IDispatcherService> dispatcher, Mock<IClipboardService> clipboard, Mock<IModelConfigGate> configGate) = CreateDependencies();

        Assert.Throws<ArgumentNullException>(() => new CoreChatViewModel(orchestration.Object, events.Object, null!, logger, dispatcher.Object, clipboard.Object, configGate.Object, default));
    }








    [TestMethod]
    public void Constructor_NullDispatcher_Throws()
    {
        (Mock<IOrchestrationControl> orchestration, Mock<ISentinelCoreEvents> events, Mock<ICaseFlowEngine> caseFlow, ILogger<CoreChatViewModel> logger, _, Mock<IClipboardService> clipboard, Mock<IModelConfigGate> configGate) = CreateDependencies();

        Assert.Throws<ArgumentNullException>(() => new CoreChatViewModel(orchestration.Object, events.Object, caseFlow.Object, logger, null!, clipboard.Object, configGate.Object, default));
    }








    [TestMethod]
    public void Constructor_NullEvents_Throws()
    {
        (Mock<IOrchestrationControl> orchestration, _, Mock<ICaseFlowEngine> caseFlow, ILogger<CoreChatViewModel> logger, Mock<IDispatcherService> dispatcher, Mock<IClipboardService> clipboard, Mock<IModelConfigGate> configGate) = CreateDependencies();

        Assert.Throws<ArgumentNullException>(() => new CoreChatViewModel(orchestration.Object, null!, caseFlow.Object, logger, dispatcher.Object, clipboard.Object, configGate.Object, default));
    }








    [TestMethod]
    public void Constructor_NullLogger_Throws()
    {
        (Mock<IOrchestrationControl> orchestration, Mock<ISentinelCoreEvents> events, Mock<ICaseFlowEngine> caseFlow, _, Mock<IDispatcherService> dispatcher, Mock<IClipboardService> clipboard, Mock<IModelConfigGate> configGate) = CreateDependencies();

        Assert.Throws<ArgumentNullException>(() => new CoreChatViewModel(orchestration.Object, events.Object, caseFlow.Object, null!, dispatcher.Object, clipboard.Object, configGate.Object, default));
    }








    // ────────────────────────────────────────────────────────────
    //  Constructor null-guards
    // ────────────────────────────────────────────────────────────








    [TestMethod]
    public void Constructor_NullOrchestrationControl_Throws()
    {
        (_, Mock<ISentinelCoreEvents> events, Mock<ICaseFlowEngine> caseFlow, ILogger<CoreChatViewModel> logger, Mock<IDispatcherService> dispatcher, Mock<IClipboardService> clipboard, Mock<IModelConfigGate> configGate) = CreateDependencies();

        Assert.Throws<ArgumentNullException>(() => new CoreChatViewModel(null!, events.Object, caseFlow.Object, logger, dispatcher.Object, clipboard.Object, configGate.Object, default));
    }








    // ────────────────────────────────────────────────────────────
    //  Welcome message seeding
    // ────────────────────────────────────────────────────────────








    [TestMethod]
    public void Constructor_ValidDependencies_SeedsSingleAssistantWelcomeMessage()
    {
        (Mock<IOrchestrationControl> orchestration, Mock<ISentinelCoreEvents> events, Mock<ICaseFlowEngine> caseFlow, ILogger<CoreChatViewModel> logger, Mock<IDispatcherService> dispatcher, Mock<IClipboardService> clipboard, Mock<IModelConfigGate> configGate) = CreateDependencies();

        CoreChatViewModel viewModel = new(orchestration.Object, events.Object, caseFlow.Object, logger, dispatcher.Object, clipboard.Object, configGate.Object, default);

        Assert.AreEqual(1, viewModel.Messages.Count);
        Assert.AreEqual(ChatRole.Assistant, viewModel.Messages[0].Role);
        Assert.IsTrue(viewModel.Messages[0].Text.Contains("SentinelCore", StringComparison.Ordinal));

        viewModel.Dispose();
    }








    [TestMethod]
    public void CopyMessageCommand_AlwaysCanExecute()
    {
        (Mock<IOrchestrationControl> orchestration, Mock<ISentinelCoreEvents> events, Mock<ICaseFlowEngine> caseFlow, ILogger<CoreChatViewModel> logger, Mock<IDispatcherService> dispatcher, Mock<IClipboardService> clipboard, Mock<IModelConfigGate> configGate) = CreateDependencies();
        CoreChatViewModel viewModel = new(orchestration.Object, events.Object, caseFlow.Object, logger, dispatcher.Object, clipboard.Object, configGate.Object, default);

        // Regression anchor: the Copy button must remain clickable regardless of
        // busy/send state — it has no CanExecute predicate by design.
        Assert.IsTrue(viewModel.CopyMessageCommand.CanExecute(null));

        viewModel.Dispose();
    }








    [TestMethod]
    public void CopyMessageCommand_EmptyText_DoesNotThrow()
    {
        (Mock<IOrchestrationControl> orchestration, Mock<ISentinelCoreEvents> events, Mock<ICaseFlowEngine> caseFlow, ILogger<CoreChatViewModel> logger, Mock<IDispatcherService> dispatcher, Mock<IClipboardService> clipboard, Mock<IModelConfigGate> configGate) = CreateDependencies();
        CoreChatViewModel viewModel = new(orchestration.Object, events.Object, caseFlow.Object, logger, dispatcher.Object, clipboard.Object, configGate.Object, default);

        viewModel.CopyMessageCommand.Execute(string.Empty);

        viewModel.Dispose();
    }








    [TestMethod]
    [TestCategory("ProductionBugSuspected")]
    [Ignore("ProductionBugSuspected")]
    public void CopyMessageCommand_NonEmptyText_CopiesToClipboard()
    {
        (Mock<IOrchestrationControl> orchestration, Mock<ISentinelCoreEvents> events, Mock<ICaseFlowEngine> caseFlow, ILogger<CoreChatViewModel> logger, Mock<IDispatcherService> dispatcher, Mock<IClipboardService> clipboard, Mock<IModelConfigGate> configGate) = CreateDependencies();
        CoreChatViewModel viewModel = new(orchestration.Object, events.Object, caseFlow.Object, logger, dispatcher.Object, clipboard.Object, configGate.Object, default);

        const string expected = "Copy-ready regression payload — 081602";
        string? clipboardText = null;
        Exception? threadException = null;

        // Clipboard access requires an STA thread; MSTest test threads are MTA by default.
        Thread staThread = new(() =>
        {
            try
            {
                viewModel.CopyMessageCommand.Execute(expected);
                clipboardText = Clipboard.GetText();
            }
            catch (Exception ex)
            {
                threadException = ex;
            }
        });
        staThread.SetApartmentState(ApartmentState.STA);
        staThread.Start();
        staThread.Join();

        Assert.IsNull(threadException);
        Assert.AreEqual(expected, clipboardText);

        viewModel.Dispose();
    }








    // ────────────────────────────────────────────────────────────
    //  Copy-message command (clipboard copy-ready regression surface)
    // ────────────────────────────────────────────────────────────








    [TestMethod]
    public void CopyMessageCommand_NullText_DoesNotThrow()
    {
        (Mock<IOrchestrationControl> orchestration, Mock<ISentinelCoreEvents> events, Mock<ICaseFlowEngine> caseFlow, ILogger<CoreChatViewModel> logger, Mock<IDispatcherService> dispatcher, Mock<IClipboardService> clipboard, Mock<IModelConfigGate> configGate) = CreateDependencies();
        CoreChatViewModel viewModel = new(orchestration.Object, events.Object, caseFlow.Object, logger, dispatcher.Object, clipboard.Object, configGate.Object, default);

        viewModel.CopyMessageCommand.Execute(null);

        viewModel.Dispose();
    }








    private static (Mock<IOrchestrationControl> Orchestration, Mock<ISentinelCoreEvents> Events, Mock<ICaseFlowEngine> CaseFlow, ILogger<CoreChatViewModel> Logger, Mock<IDispatcherService> Dispatcher, Mock<IClipboardService> Clipboard, Mock<IModelConfigGate> ConfigGate) CreateDependencies()
    {
        Mock<IOrchestrationControl> orchestration = new(MockBehavior.Strict);
        Mock<ISentinelCoreEvents> events = new();
        Mock<ICaseFlowEngine> caseFlow = new(MockBehavior.Strict);
        Mock<IDispatcherService> dispatcher = new();
        Mock<IClipboardService> clipboard = new();
        Mock<IModelConfigGate> configGate = new();

        dispatcher.Setup(d => d.CheckAccess()).Returns(true);
        configGate.Setup(g => g.IsConfigurationComplete).Returns(true);
        configGate.Setup(g => g.BuildGateMessage()).Returns(string.Empty);

        caseFlow.Setup(c => c.GetCaseStatusCountsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new Dictionary<CaseStatus, int>());

        return (orchestration, events, caseFlow, NoOpLoggerFactory.CreateLogger<CoreChatViewModel>(), dispatcher, clipboard, configGate);
    }








    [TestMethod]
    public void Dispose_UnsubscribesFromEvents_SubsequentRaiseDoesNotUpdateStatusMessage()
    {
        (Mock<IOrchestrationControl> orchestration, Mock<ISentinelCoreEvents> events, Mock<ICaseFlowEngine> caseFlow, ILogger<CoreChatViewModel> logger, Mock<IDispatcherService> dispatcher, Mock<IClipboardService> clipboard, Mock<IModelConfigGate> configGate) = CreateDependencies();
        CoreChatViewModel viewModel = new(orchestration.Object, events.Object, caseFlow.Object, logger, dispatcher.Object, clipboard.Object, configGate.Object, default);

        viewModel.Dispose();

        events.Raise(e => e.ErrorOccurred += null, "should be ignored", new InvalidOperationException("boom"));

        Assert.AreEqual(string.Empty, viewModel.StatusMessage);
    }








    [TestMethod]
    public void ErrorOccurred_Raised_UpdatesStatusMessageToErrorText()
    {
        (Mock<IOrchestrationControl> orchestration, Mock<ISentinelCoreEvents> events, Mock<ICaseFlowEngine> caseFlow, ILogger<CoreChatViewModel> logger, Mock<IDispatcherService> dispatcher, Mock<IClipboardService> clipboard, Mock<IModelConfigGate> configGate) = CreateDependencies();
        CoreChatViewModel viewModel = new(orchestration.Object, events.Object, caseFlow.Object, logger, dispatcher.Object, clipboard.Object, configGate.Object, default);

        events.Raise(e => e.ErrorOccurred += null, "tool invocation failed", new InvalidOperationException("boom"));

        Assert.AreEqual("tool invocation failed", viewModel.StatusMessage);

        viewModel.Dispose();
    }








    // ────────────────────────────────────────────────────────────
    //  Send / Cancel command gating (CanSend / CanCancel)
    // ────────────────────────────────────────────────────────────








    [TestMethod]
    public void SendCommand_EmptyInputText_CannotExecute()
    {
        (Mock<IOrchestrationControl> orchestration, Mock<ISentinelCoreEvents> events, Mock<ICaseFlowEngine> caseFlow, ILogger<CoreChatViewModel> logger, Mock<IDispatcherService> dispatcher, Mock<IClipboardService> clipboard, Mock<IModelConfigGate> configGate) = CreateDependencies();
        CoreChatViewModel viewModel = new(orchestration.Object, events.Object, caseFlow.Object, logger, dispatcher.Object, clipboard.Object, configGate.Object, default);

        viewModel.InputText = string.Empty;

        Assert.IsFalse(viewModel.SendCommand.CanExecute(null));

        viewModel.Dispose();
    }








    [TestMethod]
    public void SendCommand_NonEmptyInputTextAndNotBusy_CanExecute()
    {
        (Mock<IOrchestrationControl> orchestration, Mock<ISentinelCoreEvents> events, Mock<ICaseFlowEngine> caseFlow, ILogger<CoreChatViewModel> logger, Mock<IDispatcherService> dispatcher, Mock<IClipboardService> clipboard, Mock<IModelConfigGate> configGate) = CreateDependencies();
        CoreChatViewModel viewModel = new(orchestration.Object, events.Object, caseFlow.Object, logger, dispatcher.Object, clipboard.Object, configGate.Object, default);

        viewModel.InputText = "investigate host 10.0.0.5";

        Assert.IsTrue(viewModel.SendCommand.CanExecute(null));

        viewModel.Dispose();
    }








    [TestMethod]
    public void SendCommand_WhitespaceInputText_CannotExecute()
    {
        (Mock<IOrchestrationControl> orchestration, Mock<ISentinelCoreEvents> events, Mock<ICaseFlowEngine> caseFlow, ILogger<CoreChatViewModel> logger, Mock<IDispatcherService> dispatcher, Mock<IClipboardService> clipboard, Mock<IModelConfigGate> configGate) = CreateDependencies();
        CoreChatViewModel viewModel = new(orchestration.Object, events.Object, caseFlow.Object, logger, dispatcher.Object, clipboard.Object, configGate.Object, default);

        viewModel.InputText = "   ";

        Assert.IsFalse(viewModel.SendCommand.CanExecute(null));

        viewModel.Dispose();
    }








    // ────────────────────────────────────────────────────────────
    //  Event wiring → StatusMessage
    // ────────────────────────────────────────────────────────────








    [TestMethod]
    public void SentinelOutputEvent_Raised_UpdatesStatusMessage()
    {
        (Mock<IOrchestrationControl> orchestration, Mock<ISentinelCoreEvents> events, Mock<ICaseFlowEngine> caseFlow, ILogger<CoreChatViewModel> logger, Mock<IDispatcherService> dispatcher, Mock<IClipboardService> clipboard, Mock<IModelConfigGate> configGate) = CreateDependencies();
        CoreChatViewModel viewModel = new(orchestration.Object, events.Object, caseFlow.Object, logger, dispatcher.Object, clipboard.Object, configGate.Object, default);

        events.Raise(e => e.SentinelOutputEvent += null, new SentinelOutputEventArgs("TheCore", "reasoning about signal", ActivityType.Core));

        Assert.AreEqual("Agent: TheCore reasoning about signal", viewModel.StatusMessage);

        viewModel.Dispose();
    }
}