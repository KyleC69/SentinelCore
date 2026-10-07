using System.Reflection;

using Moq;

using SentinelCore.Contracts.Abstractions;
using SentinelCore.Orchestrations.Services;




namespace SentinelCore.Tests;




/// <summary>
///     Unit tests for <see cref="WorkflowEventProcessor" /> accumulator behavior.
/// </summary>
[TestClass]
public class WorkflowEventProcessorTests
{
    private Mock<ISystemReporter> _mockReporter = null!;
    private WorkflowEventProcessor _processor = null!;







    /// <summary>
    ///     Initializes test dependencies.
    /// </summary>
    [TestInitialize]
    public void Setup()
    {
        _mockReporter = new Mock<ISystemReporter>();
        _processor = new WorkflowEventProcessor(_mockReporter.Object);
    }







    /// <summary>
    ///     Ensures response stream keying falls back to message id when response id is blank.
    /// </summary>
    [TestMethod]
    public void BuildAccumulatorKey_ResponseIdWhitespace_FallsBackToMessageId()
    {
        // Arrange
        MethodInfo buildKey = GetRequiredMethod("BuildAccumulatorKey", BindingFlags.NonPublic | BindingFlags.Static);

        // Act
        string key = (string)buildKey.Invoke(null, ["ClassifierExec", "   ", "message-42"])!;

        // Assert
        Assert.AreEqual("ClassifierExec:message-42", key);
    }







    /// <summary>
    ///     Ensures streamed chunks are accumulated and flushed once per executor stream.
    /// </summary>
    [TestMethod]
    public void AccumulateAndFlush_ForExecutor_ReturnsConcatenatedTextThenClears()
    {
        // Arrange
        MethodInfo accumulate = GetRequiredMethod("AccumulateUpdate", BindingFlags.NonPublic | BindingFlags.Instance);
        MethodInfo flush = GetRequiredMethod("FlushAccumulatedResponsesForExecutor", BindingFlags.NonPublic | BindingFlags.Instance);

        // Act
        accumulate.Invoke(_processor, ["ClassifierExec:response-1", "Hello"]);
        accumulate.Invoke(_processor, ["ClassifierExec:response-1", " world"]);
        string firstFlush = (string)flush.Invoke(_processor, ["ClassifierExec"])!;
        string secondFlush = (string)flush.Invoke(_processor, ["ClassifierExec"])!;

        // Assert
        Assert.AreEqual("Hello world", firstFlush);
        Assert.AreEqual(string.Empty, secondFlush);
    }







    /// <summary>
    ///     Ensures response stream keying falls back to executor id when response and message ids are blank.
    /// </summary>
    [TestMethod]
    public void BuildAccumulatorKey_ResponseAndMessageIdWhitespace_FallsBackToExecutorId()
    {
        // Arrange
        MethodInfo buildKey = GetRequiredMethod("BuildAccumulatorKey", BindingFlags.NonPublic | BindingFlags.Static);

        // Act
        string key = (string)buildKey.Invoke(null, ["ClassifierExec", "   ", "\t"])!;

        // Assert
        Assert.AreEqual("ClassifierExec:ClassifierExec", key);
    }







    /// <summary>
    ///     Ensures resetting the accumulator clears buffered content for all executors.
    /// </summary>
    [TestMethod]
    public void ResetEventAccumulators_ClearsBufferedChunks()
    {
        // Arrange
        MethodInfo accumulate = GetRequiredMethod("AccumulateUpdate", BindingFlags.NonPublic | BindingFlags.Instance);
        MethodInfo flush = GetRequiredMethod("FlushAccumulatedResponsesForExecutor", BindingFlags.NonPublic | BindingFlags.Instance);

        accumulate.Invoke(_processor, ["ClassifierExec:response-1", "Hello"]);
        accumulate.Invoke(_processor, ["PatternExec:response-2", "World"]);

        // Act
        _processor.ResetEventAccumulators();
        string classifierFlush = (string)flush.Invoke(_processor, ["ClassifierExec"])!;
        string patternFlush = (string)flush.Invoke(_processor, ["PatternExec"])!;

        // Assert
        Assert.AreEqual(string.Empty, classifierFlush);
        Assert.AreEqual(string.Empty, patternFlush);
    }







    /// <summary>
    ///     Resolves a private method and fails fast when the method contract drifts.
    /// </summary>
    /// <param name="name">The method name.</param>
    /// <param name="flags">The binding flags.</param>
    /// <returns>The resolved <see cref="MethodInfo" />.</returns>
    private static MethodInfo GetRequiredMethod(string name, BindingFlags flags)
    {
        MethodInfo? method = typeof(WorkflowEventProcessor).GetMethod(name, flags);
        Assert.IsNotNull(method, $"Expected private method '{name}' to exist.");
        return method;
    }
}
