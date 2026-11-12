// Solution: SentinelCore
// Project:   SentinelCore.Tests
// File:         TaskExtensionsTests.cs
// Author: Kyle L. Crowder
// Build Num:  110813



using Assert = Microsoft.VisualStudio.TestTools.UnitTesting.Assert;




namespace SentinelCore.Tests;





/// <summary>
///     Verifies that <see cref="TaskExtensions.WithTimeout" /> bounds the wait for an operation without
///     disguising the outcome of operations that finish, fault, or are cancelled on their own.
/// </summary>
[TestClass]
public sealed class TaskExtensionsTests
{



    /// <summary>
    ///     A budget generous enough that only a deliberately non-completing task can exceed it.
    /// </summary>
    private static readonly TimeSpan GenerousTimeout = TimeSpan.FromSeconds(30);

    /// <summary>
    ///     A budget short enough to keep the suite fast, used only for tasks that never complete.
    /// </summary>
    private static readonly TimeSpan TightTimeout = TimeSpan.FromMilliseconds(50);








    /// <summary>
    ///     A completed task yields its value unchanged.
    /// </summary>
    [TestMethod]
    public async Task WithTimeout_CompletesWithinBudget_ReturnsResultAsync()
    {
        // Arrange
        Task<int> completed = Task.FromResult(42);

        // Act
        int result = await completed.WithTimeout(GenerousTimeout);

        // Assert
        Assert.AreEqual(42, result);
    }








    /// <summary>
    ///     Caller cancellation is reported as cancellation, never as a timeout.
    /// </summary>
    [TestMethod]
    public async Task WithTimeout_CallerCancels_ThrowsOperationCanceledAsync()
    {
        // Arrange
        using CancellationTokenSource cts = new CancellationTokenSource();
        await cts.CancelAsync();
        Task<int> neverCompletes = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously).Task;

        // Act / Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() => neverCompletes.WithTimeout(GenerousTimeout, cts.Token));
    }








    /// <summary>
    ///     An operation that never completes is abandoned once the budget expires.
    /// </summary>
    [TestMethod]
    public async Task WithTimeout_DoesNotComplete_ThrowsTimeoutExceptionAsync()
    {
        // Arrange
        Task<int> neverCompletes = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously).Task;

        // Act / Assert
        await Assert.ThrowsAsync<TimeoutException>(() => neverCompletes.WithTimeout(TightTimeout));
    }








    /// <summary>
    ///     The non-generic overload completes normally when the operation finishes inside the budget.
    /// </summary>
    [TestMethod]
    public async Task WithTimeout_NonGenericCompletesWithinBudget_CompletesAsync()
    {
        // Arrange
        TaskCompletionSource completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        Task bounded = completion.Task.WithTimeout(GenerousTimeout);

        // Act
        completion.SetResult();
        await bounded;

        // Assert
        Assert.IsTrue(completion.Task.IsCompletedSuccessfully);
    }








    /// <summary>
    ///     The non-generic overload abandons an operation that never completes.
    /// </summary>
    [TestMethod]
    public async Task WithTimeout_NonGenericDoesNotComplete_ThrowsTimeoutExceptionAsync()
    {
        // Arrange
        Task neverCompletes = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously).Task;

        // Act / Assert
        await Assert.ThrowsAsync<TimeoutException>(() => neverCompletes.WithTimeout(TightTimeout));
    }








    /// <summary>
    ///     A <c>null</c> task is rejected instead of surfacing a <see cref="NullReferenceException" />.
    /// </summary>
    [TestMethod]
    public void WithTimeout_NullTask_ThrowsArgumentNullException()
    {
        // Arrange
        Task? missing = null;

        // Act / Assert
        Assert.Throws<ArgumentNullException>(() => { _ = missing!.WithTimeout(GenerousTimeout); });
    }








    /// <summary>
    ///     Zero and negative budgets are rejected at the call site; an unbounded wait must be requested by
    ///     not calling this method at all.
    /// </summary>
    /// <param name="timeoutMilliseconds">The rejected budget, in milliseconds.</param>
    [TestMethod]
    [DataRow(0)]
    [DataRow(-1)]
    public void WithTimeout_NonPositiveTimeout_ThrowsArgumentOutOfRangeException(int timeoutMilliseconds)
    {
        // Arrange
        Task<int> completed = Task.FromResult(7);

        // Act / Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => { _ = completed.WithTimeout(TimeSpan.FromMilliseconds(timeoutMilliseconds)); });
    }








    /// <summary>
    ///     A faulted operation surfaces its own exception rather than being reported as a timeout.
    /// </summary>
    [TestMethod]
    public async Task WithTimeout_TaskFaults_PropagatesOriginalExceptionAsync()
    {
        // Arrange
        Task<int> faulted = Task.FromException<int>(new InvalidOperationException("case database unavailable"));

        // Act
        InvalidOperationException thrown = await Assert.ThrowsAsync<InvalidOperationException>(() => faulted.WithTimeout(GenerousTimeout));

        // Assert
        Assert.AreEqual("case database unavailable", thrown.Message);
    }
}
