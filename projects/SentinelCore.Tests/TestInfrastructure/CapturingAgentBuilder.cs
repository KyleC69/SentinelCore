// Solution: SentinelCore
// Project:   SentinelCore.Tests
// File:         CapturingAgentBuilder.cs
// Author: Kyle L. Crowder
// Build Num:  080801



namespace SentinelCore.Tests.TestInfrastructure;





/// <summary>
///     A test double for <see cref="IAgentBuilder" /> that records every
///     <see cref="AgentProfile" /> passed to <see cref="Build" /> and returns a
///     simple stub <see cref="AIAgent" />. Used to verify factories produce
///     the correct specs without requiring a real LLM endpoint.
/// </summary>
public sealed class CapturingAgentBuilder : IAgentBuilder
{
    private readonly AIAgent _stubAgent;








    public CapturingAgentBuilder(AIAgent? stubAgent = null)
    {
        _stubAgent = stubAgent ?? CreateStubAgent();
    }








    public List<AgentProfile> CapturedSpecs { get; } = [];

    /// <summary>
    ///     Returns the last spec passed to <see cref="Build" />, or throws if none.
    /// </summary>
    public AgentProfile LastSpec
    {
        get => CapturedSpecs.Count > 0 ? CapturedSpecs[^1] : throw new InvalidOperationException("No AgentProfile has been captured yet.");
    }








    /// <summary>
    ///     Asserts that exactly one spec was captured and returns it.
    /// </summary>
    public AgentProfile AssertSingleSpec()
    {
        Assert.AreEqual(1, CapturedSpecs.Count, "Expected exactly one AgentProfile to be built.");
        return CapturedSpecs[0];
    }








    public AIAgent Build(AgentProfile spec)
    {
        ArgumentNullException.ThrowIfNull(spec);
        CapturedSpecs.Add(spec);
        return _stubAgent;
    }








    public ChatClientAgent BuildChatClientAgent(AgentProfile spec)
    {
        ArgumentNullException.ThrowIfNull(spec);
        CapturedSpecs.Add(spec);
        throw new NotImplementedException("BuildChatClientAgent is not implemented in the test double.");
    }








    private static AIAgent CreateStubAgent()
    {
        // Create a minimal ChatClientAgent backed by a no-op chat client
        // that simply returns empty responses.
        return new ChatClientAgent(chatClient: new FakeChatClient(new ChatResponse(new ChatMessage(ChatRole.Assistant, "stub"))), instructions: "stub", name: "StubAgent", description: "Test stub");
    }
}