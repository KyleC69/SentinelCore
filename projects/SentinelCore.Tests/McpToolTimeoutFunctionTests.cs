// Solution: SentinelCore
// Project:   SentinelCore.Tests
// File:         McpToolTimeoutFunctionTests.cs
// Author: Kyle L. Crowder
// Build Num:  110813



using SentinelCore.Orchestrations.Mcp;
using SentinelCore.Orchestrations.Tools;

using Xunit;

using XunitAssert = Xunit.Assert;




namespace SentinelCore.Tests;





/// <summary>
///     Verifies that <see cref="McpToolTimeoutFunction" /> bounds tool calls without changing normal results.
/// </summary>
public class McpToolTimeoutFunctionTests
{



    /// <summary>
    ///     A non-positive timeout is rejected at construction.
    /// </summary>
    [Fact]
    public void Constructor_NonPositiveTimeout_Throws()
    {
        // Arrange
        AIFunction inner = AIFunctionFactory.Create(() => "ok", "fast");

        // Act / Assert
        XunitAssert.Throws<ArgumentOutOfRangeException>(() => new McpToolTimeoutFunction(inner, TimeSpan.Zero));
    }








    /// <summary>
    ///     Caller cancellation is not disguised as a timeout.
    /// </summary>
    [Fact]
    public async Task InvokeAsync_CallerCancels_ThrowsOperationCanceledAsync()
    {
        // Arrange
        AIFunction inner = AIFunctionFactory.Create(async (CancellationToken ct) =>
        {
            await Task.Delay(Timeout.Infinite, ct);
            return "never";
        }, "hung");
        McpToolTimeoutFunction sut = new(inner, TimeSpan.FromSeconds(30));
        using CancellationTokenSource cts = new();

        // Act
        Task<object?> call = sut.InvokeAsync(new AIFunctionArguments(), cts.Token).AsTask();
        await cts.CancelAsync();

        // Assert
        await XunitAssert.ThrowsAnyAsync<OperationCanceledException>(() => call);
    }








    /// <summary>
    ///     A call that finishes in time returns the wrapped function's result.
    /// </summary>
    [Fact]
    public async Task InvokeAsync_FastTool_ReturnsInnerResultAsync()
    {
        // Arrange
        AIFunction inner = AIFunctionFactory.Create(() => "ok", "fast");
        McpToolTimeoutFunction sut = new(inner, TimeSpan.FromSeconds(5));

        // Act
        object? result = await sut.InvokeAsync(new AIFunctionArguments());

        // Assert
        XunitAssert.Equal("fast", sut.Name);
        ToolResult tool = XunitAssert.IsType<ToolResult>(result);
        XunitAssert.True(tool.Success);
        XunitAssert.Equal("ok", tool.Results);
    }








    /// <summary>
    ///     A call that never finishes is cancelled and reported as a failed <see cref="ToolResult" />, never an exception.
    /// </summary>
    [Fact]
    public async Task InvokeAsync_HungTool_ReturnsFailResultAsync()
    {
        // Arrange
        AIFunction inner = AIFunctionFactory.Create(async (CancellationToken ct) =>
        {
            await Task.Delay(Timeout.Infinite, ct);
            return "never";
        }, "hung");
        McpToolTimeoutFunction sut = new(inner, TimeSpan.FromMilliseconds(200));

        // Act
        Task<object?> call = sut.InvokeAsync(new AIFunctionArguments()).AsTask();
        Task winner = await Task.WhenAny(call, Task.Delay(TimeSpan.FromSeconds(10)));

        // Assert
        XunitAssert.Same(call, winner);
        ToolResult tool = XunitAssert.IsType<ToolResult>(await call);
        XunitAssert.False(tool.Success);
        XunitAssert.Contains("did not respond", tool.ErrorDetails);
    }








    /// <summary>
    ///     An exception from the wrapped tool is returned as a failed <see cref="ToolResult" />.
    /// </summary>
    [Fact]
    public async Task InvokeAsync_ThrowingTool_ReturnsFailResultAsync()
    {
        // Arrange
        AIFunction inner = AIFunctionFactory.Create((Func<string>)(() => throw new InvalidOperationException("server died")), "broken");
        McpToolTimeoutFunction sut = new(inner, TimeSpan.FromSeconds(5));

        // Act
        object? result = await sut.InvokeAsync(new AIFunctionArguments());

        // Assert
        ToolResult tool = XunitAssert.IsType<ToolResult>(result);
        XunitAssert.False(tool.Success);
        XunitAssert.Contains("server died", tool.ErrorDetails);
    }
}