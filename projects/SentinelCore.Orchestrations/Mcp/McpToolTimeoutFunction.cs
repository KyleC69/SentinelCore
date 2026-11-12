// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         McpToolTimeoutFunction.cs
// Author: Kyle L. Crowder
// Build Num:  110813



using System.Text.Json;

using SentinelCore.Orchestrations.Tools;




namespace SentinelCore.Orchestrations.Mcp;





/// <summary>
///     Wraps an MCP-backed <see cref="AIFunction" /> so that a single invocation can never wait
///     longer than a fixed timeout (for example on a stdio server that stops responding) and always
///     returns a <see cref="ToolResult" />.
/// </summary>
/// <remarks>
///     Tools never throw: a timeout or any server/transport failure is returned as <see cref="ToolResult.Fail" />,
///     and a successful call is returned as <see cref="ToolResult.Ok" /> with the server result serialized to JSON.
///     Cancellation requested by the caller is still propagated as <see cref="OperationCanceledException" />
///     because it is not a tool failure.
/// </remarks>
public sealed class McpToolTimeoutFunction : DelegatingAIFunction
{

    private readonly TimeSpan _timeout;

    /// <summary>
    ///     Default maximum duration of a single MCP tool call.
    /// </summary>
    public static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(30);








    /// <summary>
    ///     Initializes a new instance of the <see cref="McpToolTimeoutFunction" /> class.
    /// </summary>
    /// <param name="innerFunction">The function to wrap.</param>
    /// <param name="timeout">Maximum duration of one invocation; defaults to <see cref="DefaultTimeout" />.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="innerFunction" /> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="timeout" /> is not positive.</exception>
    public McpToolTimeoutFunction(AIFunction innerFunction, TimeSpan? timeout = null) : base(innerFunction)
    {
        TimeSpan value = timeout ?? DefaultTimeout;
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(value, TimeSpan.Zero);

        _timeout = value;
    }








    /// <summary>
    ///     Invokes the wrapped function, cancelling it when the timeout elapses.
    /// </summary>
    /// <param name="arguments">The arguments for the call.</param>
    /// <param name="cancellationToken">The caller's cancellation token.</param>
    /// <returns>A <see cref="ToolResult" />: Ok with the serialized server result, or Fail on timeout or error.</returns>
    protected override async ValueTask<object?> InvokeCoreAsync(AIFunctionArguments arguments, CancellationToken cancellationToken)
    {
        using CancellationTokenSource timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(_timeout);

        try
        {
            object? result = await base.InvokeCoreAsync(arguments, timeoutCts.Token).ConfigureAwait(false);

            return ToolResult.Ok(result as string ?? JsonSerializer.Serialize(result, AIJsonUtilities.DefaultOptions));
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return ToolResult.Fail($"MCP tool '{Name}' did not respond within {_timeout}.");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return ToolResult.Fail($"MCP tool '{Name}' failed: {ex.Message}");
        }
    }
}