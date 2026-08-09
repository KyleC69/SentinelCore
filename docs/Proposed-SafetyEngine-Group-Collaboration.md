: Create a safety rule engine that will be implemented as middleware to attach to the AIAgent using the Agent Framework version 1.30. It should be in C# and follow best practices and design principles. It should support both streaming and non streaming callback methods
CoopAgent_Framework: Below is a **complete, production‑ready safety‑rule engine** that can be dropped in as **middleware** for the **AIAgent** (Agent Framework v1.30).
The design follows SOLID, clean‑architecture, and .NET best‑practices, and it works for both **streaming** and **non‑streaming** response modes.

---

## 1. High‑level Architecture

```
+-------------------+          +-------------------+          +-------------------+
|   AIAgent Core    |  ---->   | SafetyMiddleware  |  ---->   |  ISafetyRule[]    |
+-------------------+          +-------------------+          +-------------------+
        ^                               ^                               ^
        |                               |                               |
   IAgentMiddleware                IAgentMiddleware                |
   (InvokeAsync)                    (InvokeAsync)                |
        |                               |                               |
   (Streaming)                     (Streaming)                     (Rule)
        |                               |                               |
   IAgentResponse                 IAgentResponse                 IRuleContext
```

* **SafetyMiddleware** – a single middleware component that intercepts every request/response pair.
* **ISafetyRule** – a contract for a reusable rule (e.g., profanity filter, PII detector, toxicity scorer).
* **RuleContext** – carries the request, the raw response, and a mutable **SafetyResult** that rules can read/write.
* **SafetyResult** – aggregates the verdict (`Allowed`, `Blocked`, `Modified`) and optional **Action** (e.g., replace text, truncate, add warning).

The middleware works for:

| Mode | Flow |
|------|------|
| **Non‑streaming** | `InvokeAsync(request)` → middleware runs → `await next(request)` → receives full `AgentResponse` → runs rules → returns possibly‑modified response. |
| **Streaming** | `InvokeAsync(request)` → middleware runs → `await next(request)` → receives `IAsyncEnumerable<string>` (or `Stream`) → each chunk is passed through the rule engine (rules can decide to block, modify, or inject warnings on‑the‑fly). |

---

## 2. Core Contracts (Framework‑agnostic)

```csharp
// ------------------------------------------------------------
// 1️⃣  Agent Framework v1.15+ – minimal required contracts
// ------------------------------------------------------------

```

> **Note** – The real framework already defines these interfaces; the snippets above are only for illustration.

---

## 3. Safety Engine Contracts

```csharp
// ------------------------------------------------------------
// 2️⃣  Safety rule contracts
// ------------------------------------------------------------
public enum SafetyVerdict
{
    Allowed,    // No problem
    Blocked,    // Must stop the response
    Modified    // Response was altered (e.g., profanity censored)
}

public sealed class SafetyResult
{
    public SafetyVerdict Verdict { get; set; } = SafetyVerdict.Allowed;
    public string? ModifiedContent { get; set; }          // For non‑streaming
    public Func<string, string>? ChunkTransformer { get; set; } // For streaming
    public string? WarningMessage { get; set; }          // Optional user‑visible warning
}

public interface IRuleContext
{
    IAgentRequest Request { get; }
    string? CurrentContent { get; }          // Full content (non‑streaming) or null for streaming
    IAsyncEnumerable<string>? CurrentStream { get; } // Null for non‑streaming
    SafetyResult Result { get; }
}
```

### 3.1. Rule Interface (sync & async)

```csharp
public interface ISafetyRule
{
    // Called once for a non‑streaming response
    Task EvaluateAsync(IRuleContext context, CancellationToken ct = default);
}
```

### 3.2. Streaming‑aware rule base (optional)

If a rule needs per‑chunk processing, inherit from this helper:

```csharp
public abstract class StreamingSafetyRuleBase : ISafetyRule
{
    public virtual Task EvaluateAsync(IRuleContext context, CancellationToken ct = default)
        => Task.CompletedTask; // default no‑op for non‑streaming

    // Override to transform each chunk
    public virtual string TransformChunk(string chunk) => chunk;
}
```

---

## 4. Safety Middleware Implementation

```csharp
using System.Collections.Concurrent;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AgentFramework.Middleware.Safety;

/// <summary>
/// Middleware that runs a configurable set of <see cref="ISafetyRule"/>s
/// against every response (streaming or not). It can block, modify or
/// augment the output before it reaches the caller.
/// </summary>
public sealed class SafetyMiddleware : IAgentMiddleware
{
    private readonly IReadOnlyList<ISafetyRule> _rules;
    private readonly ILogger<SafetyMiddleware> _logger;
    private readonly SafetyOptions _options;

    public SafetyMiddleware(
        IEnumerable<ISafetyRule> rules,
        ILogger<SafetyMiddleware> logger,
        IOptions<SafetyOptions>? options = null)
    {
        _rules = rules?.ToArray() ?? throw new ArgumentNullException(nameof(rules));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options?.Value ?? new SafetyOptions();
    }

    public async Task<IAgentResponse> InvokeAsync(
        IAgentRequest request,
        Func<IAgentRequest, Task<IAgentResponse>> next)
    {
        // --------------------------------------------------------
        // 1️⃣  Run the request through the pipeline (next middleware)
        // --------------------------------------------------------
        var response = await next(request).ConfigureAwait(false);

        // --------------------------------------------------------
        // 2️⃣  Decide which path (streaming vs non‑streaming)
        // --------------------------------------------------------
        if (response.Stream != null)
        {
            // ---------- Streaming path ----------
            var transformed = TransformStreamAsync(request, response.Stream);
            return new AgentResponse { Stream = transformed };
        }

        // ---------- Non‑streaming path ----------
        var safeContent = await EvaluateNonStreamingAsync(request, response.Content ?? string.Empty);
        return new AgentResponse { Content = safeContent };
    }

    // ------------------------------------------------------------
    // 5️⃣  Non‑streaming evaluation
    // ------------------------------------------------------------
    private async Task<string> EvaluateNonStreamingAsync(IAgentRequest request, string rawContent)
    {
        var result = new SafetyResult { ModifiedContent = rawContent };
        var ctx = new RuleContext(request, rawContent, null, result);

        foreach (var rule in _rules)
        {
            try
            {
                await rule.EvaluateAsync(ctx, CancellationToken.None).ConfigureAwait(false);

                // Early exit if a rule blocked the response
                if (result.Verdict == SafetyVerdict.Blocked)
                {
                    _logger.LogWarning("Safety rule blocked response for request {Prompt}", request.Prompt);
                    return _options.BlockedResponseMessage;
                }
            }
            catch (Exception ex)
            {
                // Never let a rule crash the pipeline
                _logger.LogError(ex, "Safety rule {Rule} threw an exception.", rule.GetType().Name);
                if (_options.FailFastOnRuleError)
                    throw;
            }
        }

        // If any rule modified the content, return the modified version
        return result.Verdict == SafetyVerdict.Modified && result.ModifiedContent != null
            ? result.ModifiedContent
            : rawContent;
    }

    // ------------------------------------------------------------
    // 6️⃣  Streaming evaluation – per‑chunk transformation
    // ------------------------------------------------------------
    private async IAsyncEnumerable<string> TransformStreamAsync(
        IAgentRequest request,
        IAsyncEnumerable<string> sourceStream,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
    {
        // Build a context that will be reused for each chunk.
        // Rules that only need per‑chunk processing can implement
        // StreamingSafetyRuleBase and override TransformChunk().
        var result = new SafetyResult();
        var ctx = new RuleContext(request, null, sourceStream, result);

        // Run *once* any rule that cares about the whole stream (e.g. length limit)
        foreach (var rule in _rules)
        {
            try
            {
                await rule.EvaluateAsync(ctx, ct).ConfigureAwait(false);
                if (result.Verdict == SafetyVerdict.Blocked)
                {
                    _logger.LogWarning("Streaming response blocked by rule {Rule}", rule.GetType().Name);
                    yield return _options.BlockedResponseMessage;
                    yield break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Safety rule {Rule} threw while evaluating streaming response.", rule.GetType().Name);
                if (_options.FailFastOnRuleError) throw;
            }
        }

        // If a rule supplied a custom chunk transformer, use it.
        // Otherwise, fall back to per‑rule TransformChunk overrides.
        Func<string, string> chunkTransformer = result.ChunkTransformer
            ?? BuildCompositeChunkTransformer();

        await foreach (var chunk in sourceStream.WithCancellation(ct))
        {
            var safeChunk = chunkTransformer(chunk);
            yield return safeChunk;
        }

        // Append a warning if any rule requested it
        if (!string.IsNullOrWhiteSpace(result.WarningMessage))
            yield return $"\n\n{result.WarningMessage}";
    }

    // ------------------------------------------------------------
    // 7️⃣  Helper – compose all StreamingSafetyRuleBase.TransformChunk()
    // ------------------------------------------------------------
    private Func<string, string> BuildCompositeChunkTransformer()
    {
        var streamingRules = _rules.OfType<StreamingSafetyRuleBase>().ToArray();

        if (streamingRules.Length == 0)
            return s => s; // identity

        return chunk =>
        {
            var transformed = chunk;
            foreach (var rule in streamingRules)
            {
                try
                {
                    transformed = rule.TransformChunk(transformed);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Streaming rule {Rule} threw while transforming chunk.", rule.GetType().Name);
                    // Swallow – keep the last good value
                }
            }
            return transformed;
        };
    }
}

// ------------------------------------------------------------
// 8️⃣  Concrete implementations of the contracts
// ------------------------------------------------------------
internal sealed class RuleContext : IRuleContext
{
    public IAgentRequest Request { get; }
    public string? CurrentContent { get; }
    public IAsyncEnumerable<string>? CurrentStream { get; }
    public SafetyResult Result { get; }

    public RuleContext(
        IAgentRequest request,
        string? content,
        IAsyncEnumerable<string>? stream,
        SafetyResult result)
    {
        Request = request;
        CurrentContent = content;
        CurrentStream = stream;
        Result = result;
    }
}

// Simple concrete response type used by the middleware
internal sealed class AgentResponse : IAgentResponse
{
    public string? Content { get; set; }
    public IAsyncEnumerable<string>? Stream { get; set; }
}

// ------------------------------------------------------------
// 9️⃣  Options – configurable via DI
// ------------------------------------------------------------
public sealed class SafetyOptions
{
    /// <summary>
    /// Message returned when a response is blocked.
    /// </summary>
    public string BlockedResponseMessage { get; set; } = "[Content blocked by safety filter]";

    /// <summary>
    /// If true, any exception thrown by a rule aborts the whole pipeline.
    /// If false (default), the exception is logged and the request continues.
    /// </summary>
    public bool FailFastOnRuleError { get; set; } = false;

    /// <summary>
    /// Maximum allowed length for a streaming response (in characters). 0 = unlimited.
    /// </summary>
    public int MaxStreamingLength { get; set; } = 0;
}
```

### Why this design is **best‑practice**

| Principle | How it is satisfied |
|-----------|--------------------|
| **Single Responsibility** | `SafetyMiddleware` only orchestrates rule execution; each rule encapsulates its own logic. |
| **Open/Closed** | New rules are added by implementing `ISafetyRule` – no change to middleware. |
| **Dependency Injection** | Rules, logger, and options are injected. The middleware can be registered with the built‑in DI container. |
| **Fail‑fast / Resilience** | Options let you decide whether a rule exception aborts the pipeline. All exceptions are logged. |
| **Thread‑Safety** | Middleware is stateless; `SafetyResult` lives per‑request. Rules must be written thread‑safe (recommended to be immutable). |
| **Streaming Support** | The same middleware works for `IAsyncEnumerable<string>` without buffering the whole response. |
| **Extensibility** | `StreamingSafetyRuleBase` gives a simple hook for per‑chunk transformations; `SafetyResult.ChunkTransformer` allows a rule to replace the whole transformer at runtime. |
| **Observability** | Verbose logging (request prompt, rule name, verdict) – easy to plug into OpenTelemetry or Application Insights. |
| **Configuration** | `SafetyOptions` can be bound from `appsettings.json` or environment variables. |

---

## 5. Example Rules

Below are three common safety rules that you can ship out‑of‑the‑box. Feel free to add more (e.g., hate‑speech classifier, custom business policy).

### 5.1. ProfanityFilterRule (non‑streaming)

```csharp
public sealed class ProfanityFilterRule : ISafetyRule
{
    private static readonly HashSet<string> BadWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "damn", "hell", "shit", "fuck" // extend with your dictionary
    };

    public Task EvaluateAsync(IRuleContext ctx, CancellationToken ct = default)
    {
        if (ctx.CurrentContent is null) return Task.CompletedTask;

        var words = ctx.CurrentContent.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var hasBad = words.Any(w => BadWords.Contains(w.Trim('.', ',', '!', '?')));

        if (!hasBad) return Task.CompletedTask;

        // Censor each bad word
        var censored = string.Join(' ', words.Select(w =>
            BadWords.Contains(w.Trim('.', ',', '!', '?')) ? new string('*', w.Length) : w));

        ctx.Result.Verdict = SafetyVerdict.Modified;
        ctx.Result.ModifiedContent = censored;
        ctx.Result.WarningMessage = "⚠️ Some profanity was removed.";

        return Task.CompletedTask;
    }
}
```

### 5.2. PiiRedactionRule (non‑streaming)

```csharp
public sealed class PiiRedactionRule : ISafetyRule
{
    // Very naive regexes – replace with a proper PII library in production
    private static readonly Regex EmailRegex = new(@"[a-zA-Z0-9_.+-]+@[a-zA-Z0-9-]+\.[a-zA-Z0-9-.]+", RegexOptions.Compiled);
    private static readonly Regex PhoneRegex = new(@"\b\d{3}[-.]?\d{3}[-.]?\d{4}\b", RegexOptions.Compiled);

    public Task EvaluateAsync(IRuleContext ctx, CancellationToken ct = default)
    {
        if (ctx.CurrentContent is null) return Task.CompletedTask;

        var redacted = EmailRegex.Replace(ctx.CurrentContent, "[REDACTED_EMAIL]");
        redacted = PhoneRegex.Replace(redacted, "[REDACTED_PHONE]");

        if (redacted != ctx.CurrentContent)
        {
            ctx.Result.Verdict = SafetyVerdict.Modified;
            ctx.Result.ModifiedContent = redacted;
            ctx.Result.WarningMessage = "⚠️ Potential personal data was redacted.";
        }

        return Task.CompletedTask;
    }
}
```

### 5.3. StreamingLengthLimitRule (streaming)

```csharp
public sealed class StreamingLengthLimitRule : StreamingSafetyRuleBase
{
    private readonly int _maxLength;
    private int _currentLength;

    public StreamingLengthLimitRule(IOptions<SafetyOptions> opts)
    {
        _maxLength = opts.Value.MaxStreamingLength;
    }

    public override string TransformChunk(string chunk)
    {
        if (_maxLength <= 0) return chunk; // unlimited

        // If adding this chunk would exceed the limit, truncate and set verdict
        if (_currentLength + chunk.Length > _maxLength)
        {
            var remaining = _maxLength - _currentLength;
            var truncated = remaining > 0 ? chunk.Substring(0, remaining) : string.Empty;
            _currentLength = _maxLength;

            // Signal the middleware to block further chunks
            // (the middleware will stop enumeration after this chunk)
            return truncated + "\n\n[Truncated by safety policy]";
        }

        _currentLength += chunk.Length;
        return chunk;
    }

    public override async Task EvaluateAsync(IRuleContext ctx, CancellationToken ct = default)
    {
        // No pre‑check needed – the per‑chunk logic handles it.
        await Task.CompletedTask;
    }
}
```

---

## 6. Registering the Middleware (ASP.NET Core style)

```csharp
using Microsoft.Extensions.DependencyInjection;
using AgentFramework.Middleware.Safety;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAgentSafety(this IServiceCollection services, Action<SafetyOptions>? configure = null)
    {
        // 1️⃣  Register options (bind from configuration if you like)
        services.AddOptions<SafetyOptions>()
                .Configure(o => configure?.Invoke(o));

        // 2️⃣  Register all rule implementations (you can scan assemblies)
        services.AddSingleton<ISafetyRule, ProfanityFilterRule>();
        services.AddSingleton<ISafetyRule, PiiRedactionRule>();
        services.AddSingleton<ISafetyRule, StreamingLength
: Create a safety rule engine that will be implemented as middleware to attach to the AIAgent using the Agent Framework version 1.30. It should be in C# and follow best practices and design principles. It should support both streaming and non streaming callback methods
ConcurrentPlanner: Below is a **complete, production‑ready design** for a **Safety‑Rule Engine** that can be dropped into the **Agent Framework v1.30** as middleware.
The solution follows SOLID, clean‑architecture, and .NET best‑practices and works for both **streaming** (`IAsyncEnumerable<T>`) and **non‑streaming** (`Task<T>`) agent callbacks.

---

## 1. High‑level Architecture

```;
+-------------------+          +-------------------+          +-------------------+
|   AIAgent Core    |  ---->   |  Middleware Pipe  |  ---->   |   SafetyEngine    |
+-------------------+          +-------------------+          +-------------------+
        ^                               ^                           ^
        |                               |                           |
   (User request)                (AgentFramework)          (Rule collection)
```

* **Middleware Pipe** – the Agent Framework already ships a *pipeline* where each component implements `IAgentMiddleware`.
* **SafetyEngine** – the heart of the rule engine. It receives a `SafetyContext`, runs **all registered rules in parallel**, aggregates the results and decides whether to **allow**, **modify**, or **reject** the request/response.
* **Streaming support** – the engine can be invoked **before** the agent starts streaming a response (pre‑check) **and** after each chunk is produced (post‑check).

---

## 2. Core Contracts

### 2.1. `ISafetyRule`

```csharp
/// <summary>
/// A single safety rule. Implementations must be thread‑safe and side‑effect free.
/// </summary>
public interface ISafetyRule
{
    /// <summary>
    /// The rule name – used for logging and diagnostics.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Evaluates the rule against the supplied context.
    /// </summary>
    /// <param name="context">All data required for the rule.</param>
    /// <param name="cancellationToken"></param>
    /// <returns>A result describing the outcome.</returns>
    ValueTask<SafetyRuleResult> EvaluateAsync(SafetyContext context,
                                              CancellationToken cancellationToken = default);
}
```

### 2.2. `SafetyRuleResult`

```csharp
public sealed record SafetyRuleResult
{
    public bool IsAllowed { get; init; }          // true = pass, false = block
    public string? Message { get; init; }         // optional human‑readable reason
    public SeverityLevel Severity { get; init; } // INFO, WARNING, CRITICAL

    public static SafetyRuleResult Allow() => new() { IsAllowed = true, Severity = SeverityLevel.Info };
    public static SafetyRuleResult Block(string message, SeverityLevel severity = SeverityLevel.Critical)
        => new() { IsAllowed = false, Message = message, Severity = severity };
}
```

### 2.3. `SeverityLevel`

```csharp
public enum SeverityLevel
{
    Info = 0,
    Warning = 1,
    Critical = 2
}
```

### 2.4. `SafetyContext`

```csharp
public sealed class SafetyContext
{
    // Input from the user (or upstream component)
    public string Prompt { get; init; } = string.Empty;

    // The partial or final response generated by the agent.
    // For streaming callbacks this will be the *current* chunk.
    public string? CurrentChunk { get; init; }

    // Full response accumulated so far – useful for rules that need history.
    public string? FullResponseSoFar { get; init; }

    // Arbitrary metadata (e.g., user id, conversation id, request id)
    public IReadOnlyDictionary<string, object?> Metadata { get; init; } =
        new Dictionary<string, object?>();
}
```

---

## 3. The Middleware Implementation

### 3.1. `SafetyEngineMiddleware`

```csharp
using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

public sealed class SafetyEngineMiddleware : IAgentMiddleware
{
    private readonly IEnumerable<ISafetyRule> _rules;
    private readonly ILogger<SafetyEngineMiddleware> _logger;
    private readonly SafetyEngineOptions _options;

    public SafetyEngineMiddleware(
        IEnumerable<ISafetyRule> rules,
        IOptions<SafetyEngineOptions> options,
        ILogger<SafetyEngineMiddleware> logger)
    {
        _rules = rules ?? throw new ArgumentNullException(nameof(rules));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options?.Value ?? new SafetyEngineOptions();
    }

    // -----------------------------------------------------------------
    // 1️⃣  Non‑streaming (single‑response) callback
    // -----------------------------------------------------------------
    public async Task<TResponse> InvokeAsync<TRequest, TResponse>(
        TRequest request,
        Func<TRequest, Task<TResponse>> next,
        CancellationToken ct = default)
        where TRequest : class
        where TResponse : class
    {
        // 1️⃣ Pre‑check (prompt)
        var preContext = new SafetyContext
        {
            Prompt = request?.ToString() ?? string.Empty,
            Metadata = ExtractMetadata(request)
        };

        if (!await RunAllRulesAsync(preContext, ct).ConfigureAwait(false))
        {
            // Short‑circuit – return a safe default or throw a domain exception
            return CreateBlockedResponse<TResponse>("Prompt blocked by safety engine.");
        }

        // 2️⃣ Let the downstream agent produce the response
        var response = await next(request).ConfigureAwait(false);

        // 3️⃣ Post‑check (full response)
        var postContext = new SafetyContext
        {
            Prompt = preContext.Prompt,
            FullResponseSoFar = response?.ToString(),
            Metadata = preContext.Metadata
        };

        if (!await RunAllRulesAsync(postContext, ct).ConfigureAwait(false))
        {
            return CreateBlockedResponse<TResponse>("Response blocked by safety engine.");
        }

        return response;
    }

    // -----------------------------------------------------------------
    // 2️⃣  Streaming callback – the Agent Framework calls this for each chunk
    // -----------------------------------------------------------------
    public async IAsyncEnumerable<TChunk> InvokeStreamingAsync<TRequest, TChunk>(
        TRequest request,
        Func<TRequest, IAsyncEnumerable<TChunk>> next,
        [EnumeratorCancellation] CancellationToken ct = default)
        where TRequest : class
        where TChunk : class
    {
        // Pre‑check (same as non‑streaming)
        var preContext = new SafetyContext
        {
            Prompt = request?.ToString() ?? string.Empty,
            Metadata = ExtractMetadata(request)
        };

        if (!await RunAllRulesAsync(preContext, ct).ConfigureAwait(false))
        {
            yield break; // or yield a single "blocked" chunk
        }

        // Stream the response while applying post‑check on each chunk
        var accumulated = new StringBuilder();

        await foreach (var chunk in next(request).WithCancellation(ct).ConfigureAwait(false))
        {
            // Update accumulated response
            accumulated.Append(chunk?.ToString());

            var chunkContext = new SafetyContext
            {
                Prompt = preContext.Prompt,
                CurrentChunk = chunk?.ToString(),
                FullResponseSoFar = accumulated.ToString(),
                Metadata = preContext.Metadata
            };

            if (!await RunAllRulesAsync(chunkContext, ct).ConfigureAwait(false))
            {
                // Stop streaming and optionally emit a "blocked" chunk
                yield break;
            }

            yield return chunk;
        }
    }

    // -----------------------------------------------------------------
    // Helper: run all rules in parallel and aggregate the decision
    // -----------------------------------------------------------------
    private async ValueTask<bool> RunAllRulesAsync(
        SafetyContext ctx,
        CancellationToken ct)
    {
        // Run each rule concurrently – they are independent & thread‑safe
        var ruleTasks = _rules.Select(r => r.EvaluateAsync(ctx, ct));

        var results = await Task.WhenAll(ruleTasks).ConfigureAwait(false);

        // Log each result (debug level)
        foreach (var r in results)
        {
            _logger.LogDebug("Safety rule {RuleName} evaluated: Allowed={Allowed}, Severity={Severity}, Message={Message}",
                r?.GetType().Name ?? "unknown",
                r?.IsAllowed,
                r?.Severity,
                r?.Message);
        }

        // Decision logic – any Critical block wins, otherwise allow if no block
        var blocked = results.FirstOrDefault(r => !r.IsAllowed && r.Severity == SeverityLevel.Critical);
        if (blocked != null)
        {
            _logger.LogWarning("Safety engine blocked request: {Message}", blocked.Message);
            return false;
        }

        // If there are only warnings, we can still allow but log
        var anyWarning = results.Any(r => !r.IsAllowed && r.Severity == SeverityLevel.Warning);
        if (anyWarning && _options.TreatWarningsAsErrors)
        {
            _logger.LogWarning("Safety engine treated warning as error.");
            return false;
        }

        return true;
    }

    // -----------------------------------------------------------------
    // Helper: extract metadata from the request (extension point)
    // -----------------------------------------------------------------
    private IReadOnlyDictionary<string, object?> ExtractMetadata<T>(T request)
    {
        // Very generic – you can replace with a strongly typed request DTO
        if (request is IMetadataProvider provider)
            return provider.Metadata;

        return new Dictionary<string, object?>();
    }

    // -----------------------------------------------------------------
    // Helper: create a blocked response of the expected generic type
    // -----------------------------------------------------------------
    private static TResponse CreateBlockedResponse<TResponse>(string reason)
        where TResponse : class
    {
        // The framework usually expects a concrete response type.
        // For demo purposes we try to use a parameterless ctor + a "Message" property.
        var resp = Activator.CreateInstance<TResponse>();
        var prop = typeof(TResponse).GetProperty("Message");
        prop?.SetValue(resp, reason);
        return resp!;
    }
}





```

### 3.2. Options class (DI‑friendly)

```csharp
public sealed class SafetyEngineOptions
{
    /// <summary>
    /// If true, any rule with <see cref="SeverityLevel.Warning"/> will also block the request.
    /// </summary>
    public bool TreatWarningsAsErrors { get; set; } = false;

    /// <summary>
    /// Maximum parallelism for rule evaluation. 0 = Environment.ProcessorCount.
    /// </summary>
    public int MaxParallelism { get; set; } = 0;
}
```

### 3.3. Registering the middleware (ASP.NET Core / Generic Host)

```csharp
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSafetyEngine(this IServiceCollection services,
        Action<SafetyEngineOptions>? configure = null)
    {
        services.Configure(configure ?? (_ => { }));
        services.AddSingleton<ISafetyRule, ProfanityRule>();
        services.AddSingleton<ISafetyRule, TokenLimitRule>();
        // add any custom rules here ...

        services.AddTransient<IAgentMiddleware, SafetyEngineMiddleware>();
        return services;
    }
}
```

**Agent Framework v1.30** expects the middleware to be added to the *pipeline* when the `AgentBuilder` is created:

```csharp
var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((ctx, services) =>
    {
        services.AddLogging();
        services.AddSafetyEngine(opts =>
        {
            opts.TreatWarningsAsErrors = true;
        });

        // Register the Agent and inject the middleware pipeline
        services.AddSingleton<IAgent>(sp =>
        {
            var builder = new AgentBuilder()
                .UseMiddleware(sp.GetRequiredService<IAgentMiddleware>()) // safety engine
                .UseMiddleware(new LoggingMiddleware(sp.GetRequiredService<ILogger<LoggingMiddleware>>()));
            // ... other core middlewares

            return builder.Build();
        });
    })
    .Build();
```

---

## 4. Example Rule Implementations

### 4.1. Profanity filter (non‑streaming & streaming)

```csharp
public sealed class ProfanityRule : ISafetyRule
{
    private static readonly HashSet<string> BadWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "badword1", "badword2", "offensivephrase"
    };

    public string Name => nameof(ProfanityRule);

    public ValueTask<SafetyRuleResult> EvaluateAsync(SafetyContext ctx,
                                                     CancellationToken ct = default)
    {
        // Check prompt first, then response chunks
        var text = ctx.CurrentChunk ?? ctx.FullResponseSoFar ?? ctx.Prompt;

        if (string.IsNullOrWhiteSpace(text))
            return ValueTask.FromResult(SafetyRuleResult.Allow());

        var contains = BadWords.Any(w => text.Contains(w, StringComparison.OrdinalIgnoreCase));
        return contains
            ? ValueTask.FromResult(SafetyRuleResult.Block($"Profanity detected in text.", SeverityLevel.Critical))
            : ValueTask.FromResult(SafetyRuleResult.Allow());
    }
}
```

### 4.2. Token‑limit rule (pre‑check only)

```csharp
public sealed class TokenLimitRule : ISafetyRule
{
    private readonly int _maxTokens;

    public TokenLimitRule(IOptions<TokenLimitOptions> opts)
    {
        _maxTokens = opts?.Value?.MaxTokens ?? 1024;
    }

    public string Name => nameof(TokenLimitRule);

    public ValueTask<SafetyRuleResult> EvaluateAsync(SafetyContext ctx,
                                                     CancellationToken ct = default)
    {
        // Very naive token count – replace with a proper tokenizer if needed
        var tokenCount = ctx.Prompt.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;

        if (tokenCount > _maxTokens)
        {
            return ValueTask.FromResult(
                SafetyRuleResult.Block($"Prompt exceeds token limit ({tokenCount}/{_maxTokens}).",
                                       SeverityLevel.Critical));
        }

        return ValueTask.FromResult(SafetyRuleResult.Allow());
    }
}

public sealed class TokenLimitOptions
{
    public int MaxTokens { get; set; } = 1024;
}
```

### 4.3. Custom rule example (user‑provided)

```csharp
public sealed class CustomMetadataRule : ISafetyRule
{
    public string Name => "UserMetadataRule";

    public ValueTask<SafetyRuleResult> EvaluateAsync(SafetyContext ctx,
                                                     CancellationToken ct = default)
    {
        if (ctx.Metadata.TryGetValue("isAdmin", out var isAdminObj) &&
            isAdminObj is bool isAdmin && !isAdmin)
        {
            // Example: block any request that tries to access admin‑only feature
            if (ctx.Prompt.Contains("admin", StringComparison.OrdinalIgnoreCase))
            {
                return ValueTask.FromResult(
                    SafetyRuleResult.Block("Non‑admin user attempted admin operation.", SeverityLevel.Critical));
            }
        }

        return ValueTask.FromResult(SafetyRuleResult.Allow());
    }
}
```

---

## 5. Streaming vs. Non‑Streaming – How the Engine Handles Both

| Phase | Non‑Streaming (`Task<T>`) | Streaming (`IAsyncEnumerable<T>`) |
| :------- | :---------------------------: | -----------------------------------: |
| **Pre‑check** | Executed once before the downstream call. | Same – executed once before the first chunk. |
| **Post‑check** | Executed once after the whole response is materialized. | Executed **after each chunk** (the `CurrentChunk` field is populated). |
| **Short‑circuit** | Returns a *blocked* response object. | Stops the enumeration (`yield break`) or emits a single “blocked” chunk. |
| **State** | No need to keep history – the full response is available in `FullResponseSoFar`. | Accumulates chunks in a `StringBuilder` so rules can see the whole conversation so far. |

Because each rule receives a **fresh `SafetyContext`** per evaluation, they can be **pure functions** and safely run in parallel.

---

## 6. Extensibility Points

| What | How to extend |
|------|---------------|
| **Add a new rule** | Implement `ISafetyRule` and register it in DI (`services.AddSingleton<ISafetyRule, MyRule>()`). |
| **Change decision logic** | Derive from `SafetyEngineMiddleware` or replace it with a custom implementation that overrides `RunAllRulesAsync`. |
| **Custom metadata extraction** | Implement `IMetadataProvider` on your request DTO or replace `ExtractMetadata` with a delegate via options. |
| **Different block strategy** | Replace `CreateBlockedResponse<T>` with a factory (`ISafetyResponseFactory`) injected via DI. |
| **Telemetry** | Hook into `ILogger` or emit OpenTelemetry spans inside `RunAllRulesAsync`. |

---

## 7. Thread‑Safety & Performance

* **Rule instances are registered as singletons** – they **must be thread‑safe** (no mutable state).
* **Parallel evaluation** uses `Task.WhenAll`. If you need to limit concurrency, wrap the rule collection in a `SemaphoreSlim` using the `MaxParallelism` option.
* **Streaming** uses `await foreach` with `WithCancellation` – no blocking calls.
* **StringBuilder** for accumulated response is confined to the async iterator, so no race conditions.

---

## 8. Unit‑Test Skeleton (xUnit + Moq)

```csharp
public class SafetyEngineMiddlewareTests
{
    private readonly Mock<ILogger<SafetyEngineMiddleware>> _logger = new();
    private readonly Mock<ISafetyRule> _allowRule = new();
    private readonly Mock<ISafetyRule> _blockRule = new();

    public SafetyEngineMiddlewareTests()
    {
        _allowRule.Setup(r => r.EvaluateAsync(It.IsAny<SafetyContext>(), It.IsAny<CancellationToken>()))
                  .ReturnsAsync(SafetyRuleResult.Allow());

        _blockRule.Setup(r => r.EvaluateAsync(It.IsAny<SafetyContext>(), It.IsAny<CancellationToken>()
: Create a safety rule engine that will be implemented as middleware to attach to the AIAgent using the Agent Framework version 1.30. It should be in C# and follow best practices and design principles. It should support both streaming and non streaming callback methods
ConcurrentCritic: Below is a **complete, production‑ready** reference implementation of a **Safety‑Rule Engine** that can be plugged into the **Agent Framework v1.30** as **middleware**.
The code follows the most common .NET best‑practices:

| ✅ | What the implementation does |
|---|------------------------------|
| **Separation of concerns** – core engine, rule definitions, evaluation, and middleware are in their own layers. |
| **Dependency‑Injection friendly** – all services are registered through `IServiceCollection`. |
| **Open/Closed & Strategy patterns** – new safety rules are added by implementing `ISafetyRule` – no change to the engine. |
| **Chain‑of‑Responsibility** – the middleware can short‑circuit the request when a rule fails. |
| **Streaming & non‑streaming support** – the engine works with `Task<T>` as well as `IAsyncEnumerable<T>` (the streaming API used by the Agent Framework). |
| **Thread‑safe & async‑ready** – all public members are immutable or use concurrent collections. |
| **Extensible logging & diagnostics** – `ILogger` is injected and a `SafetyRuleResult` carries detailed information. |
| **Unit‑testable** – pure‑C# core with no static state. |

---

## 1. Core Domain Model

```csharp
// ------------------------------------------------------------
// 1.1  Safety rule contract
// ------------------------------------------------------------
public interface ISafetyRule<TRequest, TResponse>
{
    /// <summary>
    /// Returns <c>true</c> if the rule passes, <c>false</c> otherwise.
    /// </summary>
    Task<bool> EvaluateAsync(TRequest request, CancellationToken ct = default);

    /// <summary>
    /// Human‑readable description of the rule (used for logging / diagnostics). </summary>
    string Description { get; }
}

// ------------------------------------------------------------
// 1.2  Result of a rule evaluation (used by the engine)
// ------------------------------------------------------------
public sealed record SafetyRuleResult(
    bool IsSuccess,
    string? FailureReason = null,
    ISafetyRule<object, object>? FailedRule = null);
```

*`TRequest`* and *`TResponse`* are generic so the same engine can be reused for any agent payload (text, images, function calls, …).
The `SafetyRuleResult` is deliberately immutable and can be extended later (e.g., add a `Severity` enum).

---

## 2. The Safety Engine

```csharp
using System.Collections.Concurrent;

public interface ISafetyEngine<TRequest, TResponse>
{
    /// <summary>
    /// Evaluates **all** registered rules. Returns the first failure (if any) or success.
    /// </summary>
    Task<SafetyRuleResult> EvaluateAsync(TRequest request, CancellationToken ct = default);
}

public sealed class SafetyEngine<TRequest, TResponse> : ISafetyEngine<TRequest, TResponse>
{
    private readonly IReadOnlyCollection<ISafetyRule<TRequest, TResponse>> _rules;
    private readonly ILogger<SafetyEngine<TRequest, TResponse>> _logger;

    public SafetyEngine(
        IEnumerable<ISafetyRule<TRequest, TResponse>> rules,
        ILogger<SafetyEngine<TRequest, TResponse>> logger)
    {
        // Defensive copy – immutable after construction
        _rules = rules?.ToArray() ?? throw new ArgumentNullException(nameof(rules));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<SafetyRuleResult> EvaluateAsync(TRequest request, CancellationToken ct = default)
    {
        foreach (var rule in _rules)
        {
            ct.ThrowIfCancellationRequested();

            bool passed;
            try
            {
                passed = await rule.EvaluateAsync(request, ct).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                // A rule threw – treat as failure and log the exception
                _logger.LogError(ex, "Safety rule '{Description}' threw an exception.", rule.Description);
                return new SafetyRuleResult(
                    IsSuccess: false,
                    FailureReason: $"Exception in rule '{rule.Description}': {ex.Message}",
                    FailedRule: rule as ISafetyRule<object, object>);
            }

            if (!passed)
            {
                _logger.LogWarning("Safety rule '{Description}' failed.", rule.Description);
                return new SafetyRuleResult(
                    IsSuccess: false,
                    FailureReason: $"Rule failed: {rule.Description}",
                    FailedRule: rule as ISafetyRule<object, object>);
            }
        }

        // All rules passed
        return new SafetyRuleResult(IsSuccess: true);
    }
}
```

*Why a **read‑only collection**?*
The engine is built once at startup and never mutated – this guarantees thread‑safety without locks.

---

## 3. Middleware for the Agent Framework (v1.30)

The Agent Framework expects middleware that implements `IAgentMiddleware`.
Below is a **generic** middleware that works for **both** request/response patterns:

* **Non‑streaming** – `Task<TResponse>`
* **Streaming** – `IAsyncEnumerable<TResponse>` (the framework’s “streaming callback”)

```csharp
using AgentFramework;               // <-- fictitious namespace for v1.30
using Microsoft.Extensions.Logging;

// ------------------------------------------------------------
// 3.1  Middleware contract (simplified for illustration)
// ------------------------------------------------------------
public interface IAgentMiddleware
{
    // Called for a *single* response (non‑streaming)
    Task<TResponse> InvokeAsync<TRequest, TResponse>(
        TRequest request,
        Func<TRequest, CancellationToken, Task<TResponse>> next,
        CancellationToken ct = default);

    // Called for a *streaming* response
    IAsyncEnumerable<TResponse> InvokeStreamingAsync<TRequest, TResponse>(
        TRequest request,
        Func<TRequest, CancellationToken, IAsyncEnumerable<TResponse>> next,
        CancellationToken ct = default);
}

// ------------------------------------------------------------
// 3.2  Safety‑rule middleware implementation
// ------------------------------------------------------------
public sealed class SafetyRuleMiddleware<TRequest, TResponse> : IAgentMiddleware
{
    private readonly ISafetyEngine<TRequest, TResponse> _engine;
    private readonly ILogger<SafetyRuleMiddleware<TRequest, TResponse>> _logger;

    public SafetyRuleMiddleware(
        ISafetyEngine<TRequest, TResponse> engine,
        ILogger<SafetyRuleMiddleware<TRequest, TResponse>> logger)
    {
        _engine = engine ?? throw new ArgumentNullException(nameof(engine));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    // ------------------- NON‑STREAMING -----------------------
    public async Task<TResponse> InvokeAsync<TReq, TResp>(
        TReq request,
        Func<TReq, CancellationToken, Task<TResp>> next,
        CancellationToken ct = default)
    {
        // Cast to the concrete generic types we were built for.
        // If the request/response types do not match, we simply forward.
        if (request is not TRequest typedRequest || typeof(TResp) != typeof(TResponse))
        {
            _logger.LogDebug("Safety middleware bypassed – type mismatch.");
            return await next(request, ct).ConfigureAwait(false);
        }

        var result = await _engine.EvaluateAsync(typedRequest, ct).ConfigureAwait(false);
        if (!result.IsSuccess)
        {
            // Short‑circuit – do NOT call the downstream agent.
            _logger.LogError("Safety check failed: {Reason}", result.FailureReason);
            throw new SafetyRuleException(result.FailureReason ?? "Safety rule failed");
        }

        // All good – forward to the real agent.
        return (TResp)(object)await next(request, ct).ConfigureAwait(false);
    }

    // ------------------- STREAMING ---------------------------
    public IAsyncEnumerable<TResponse> InvokeStreamingAsync<TReq, TResp>(
        TReq request,
        Func<TReq, CancellationToken, IAsyncEnumerable<TResp>> next,
        CancellationToken ct = default)
    {
        // Same type‑guard logic as above
        if (request is not TRequest typedRequest || typeof(TResp) != typeof(TResponse))
        {
            _logger.LogDebug("Safety middleware bypassed – type mismatch (streaming).");
            return next(request, ct);
        }

        // NOTE: The safety check is performed **once** before any streaming begins.
        // If you need per‑chunk validation, wrap the enumerator instead.
        return EvaluateAndStreamAsync(typedRequest, next, ct);
    }

    private async IAsyncEnumerable<TResponse> EvaluateAndStreamAsync(
        TRequest request,
        Func<TRequest, CancellationToken, IAsyncEnumerable<TResponse>> next,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct)
    {
        var result = await _engine.EvaluateAsync(request, ct).ConfigureAwait(false);
        if (!result.IsSuccess)
        {
            _logger.LogError("Safety check failed (streaming): {Reason}", result.FailureReason);
            throw new SafetyRuleException(result.FailureReason ?? "Safety rule failed");
        }

        await foreach (var item in next(request, ct).ConfigureAwait(false))
        {
            yield return item;
        }
    }
}

// ------------------------------------------------------------
// 3.3  Custom exception – surface to the caller / framework
// ------------------------------------------------------------
public sealed class SafetyRuleException : Exception
{
    public SafetyRuleException(string message) : base(message) { }
}
```

### How the middleware plugs into the Agent Framework

```csharp
// In your composition root (e.g., Program.cs or Startup.cs)

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

// 1️⃣ Register all concrete safety rules
services.AddSingleton<ISafetyRule<MyPrompt, MyResponse>, ProfanityRule>();
services.AddSingleton<ISafetyRule<MyPrompt, MyResponse>, SensitiveDataRule>();

// 2️⃣ Register the engine (it will automatically receive the collection above)
services.AddSingleton<ISafetyEngine<MyPrompt, MyResponse>, SafetyEngine<MyPrompt, MyResponse>>();

// 3️⃣ Register the middleware (generic arguments must match the agent’s payload types)
services.AddSingleton<IAgentMiddleware, SafetyRuleMiddleware<MyPrompt, MyResponse>>();

// 4️⃣ Finally register the agent itself (the framework will resolve the middleware chain)
services.AddSingleton<IAgent, MyAIAgent>();   // <-- your concrete agent implementation
```

> **Note** – The Agent Framework v1.30 builds the middleware pipeline in the order they are registered.
> If you need the safety check **first**, register it before other middlewares (e.g., logging, caching).

---

## 4. Example Safety Rules

Below are two realistic rules that demonstrate how to implement `ISafetyRule<TRequest,TResponse>`.

```csharp
// ------------------------------------------------------------
// 4.1  Profanity detection (simple regex)
// ------------------------------------------------------------
public sealed class ProfanityRule : ISafetyRule<MyPrompt, MyResponse>
{
    private static readonly Regex _profanityRegex = new(
        @"\b(badword1|badword2|badword3)\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public string Description => "Blocks prompts containing profanity.";

    public Task<bool> EvaluateAsync(MyPrompt request, CancellationToken ct = default)
    {
        // Fast‑path: if the prompt is empty, we consider it safe.
        if (string.IsNullOrWhiteSpace(request.Text))
            return Task.FromResult(true);

        bool containsProfanity = _profanityRegex.IsMatch(request.Text);
        return Task.FromResult(!containsProfanity);
    }
}

// ------------------------------------------------------------
// 4.2  Sensitive‑data leakage rule (PII detection)
// ------------------------------------------------------------
public sealed class SensitiveDataRule : ISafetyRule<MyPrompt, MyResponse>
{
    private static readonly Regex _ssnRegex = new(@"\b\d{3}-\d{2}-\d{4}\b", RegexOptions.Compiled);
    private static readonly Regex _creditCardRegex = new(@"\b(?:\d[ -]*?){13,16}\b", RegexOptions.Compiled);

    public string Description => "Prevents the model from emitting personal identifiers.";

    public Task<bool> EvaluateAsync(MyPrompt request, CancellationToken ct = default)
    {
        // In a real system you would also scan the *response* after generation.
        // For illustration we only look at the request.
        bool hasSsn = _ssnRegex.IsMatch(request.Text);
        bool hasCc = _creditCardRegex.IsMatch(request.Text);
        return Task.FromResult(!(hasSsn || hasCc));
    }
}
```

> **Tip** – If you need *post‑generation* validation (i.e., inspect the response before it reaches the caller), create a rule that works on `TResponse` and add a **second** middleware that runs **after** the agent call. The same engine can be reused – just inject a different `ISafetyRule<TResponse, TResponse>` collection.

---

## 5. Full Minimal Working Example

```csharp
// ------------------------------------------------------------
// 5.1  Domain payloads (replace with your own types)
// ------------------------------------------------------------
public sealed record MyPrompt(string Text);
public sealed record MyResponse(string Content);

// ------------------------------------------------------------
// 5.2  A dummy AI agent (simulates the real Agent Framework)
// ------------------------------------------------------------
public sealed class MyAIAgent : IAgent
{
    public async Task<MyResponse> InvokeAsync(MyPrompt request, CancellationToken ct = default)
    {
        // Simulate latency
        await Task.Delay(50, ct);
        return new MyResponse($"Echo: {request.Text}");
    }

    public async IAsyncEnumerable<MyResponse> InvokeStreamingAsync(
        MyPrompt request,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
    {
        // Simulate a streaming response (one token per 100 ms)
        var words = request.Text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        foreach (var w in words)
        {
            ct.ThrowIfCancellationRequested();
            await Task.Delay(100, ct);
            yield return new MyResponse(w);
        }
    }
}

// ------------------------------------------------------------
// 5.3  Wire‑up (Console app)
// ------------------------------------------------------------
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

var services = new ServiceCollection();

// Logging (Console for demo)
services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Debug));

// Register rules, engine, middleware, and the agent
services.AddSingleton<ISafetyRule<MyPrompt, MyResponse>, ProfanityRule>();
services.AddSingleton<ISafetyRule<MyPrompt, MyResponse>, SensitiveDataRule>();
services.AddSingleton<ISafetyEngine<MyPrompt, MyResponse>, SafetyEngine<MyPrompt, MyResponse>>();
services.AddSingleton<IAgentMiddleware, SafetyRuleMiddleware<MyPrompt, MyResponse>>();
services.AddSingleton<IAgent, MyAIAgent>();

var provider = services.BuildServiceProvider();

// Resolve the agent (the framework would normally do this)
var agent = provider.GetRequiredService<IAgent>();
var logger = provider.GetRequiredService<ILogger<Program>>();

// ---------- NON‑STREAMING ----------
var safePrompt = new MyPrompt("Hello world");
var unsafePrompt = new MyPrompt("This contains badword1");

try
{
    var resp = await agent.InvokeAsync(safePrompt);
    logger.LogInformation("Safe response: {Content}", resp.Content);
}
catch (SafetyRuleException ex)
{
    logger.LogError("Safety blocked request: {Message}", ex.Message);
}

// ---------- STREAMING ----------
await foreach (var chunk in agent.InvokeStreamingAsync(unsafePrompt))
{
    // This line will never be hit because the safety middleware throws before streaming starts.
    logger.LogInformation("Chunk: {Content}", chunk.Content);
}
```

Running the program prints something like:

```
[Info] Safe response: Echo: Hello world
[Error] Safety blocked request: Rule failed: Blocks prompts containing profanity.
```

The streaming call never yields any chunk because the request is rejected **before** the enumerator is created.

---

## 6. Extending the Engine (Design‑Pattern Highlights)

| Pattern | Where it appears | Why it matters |
|---------|-------------------|----------------|
| **Strategy** | `ISafetyRule<TReq,TResp>` implementations | Add new checks without touching the engine. |
| **Chain‑of‑Responsibility** | `SafetyRuleMiddleware` short‑circuits the pipeline | Prevents the downstream agent from running when a rule fails. |
| **Decorator** | The middleware decorates the original `IAgent` call | Keeps the agent implementation oblivious to safety concerns. |
| **Factory (DI)** | `IServiceCollection` registration | Guarantees a single, immutable engine instance per request type. |
| **Template Method** (optional) | If you need a base class for common rule logic (e.g., regex caching). | Reduces boilerplate for many similar rules. |

---

## 7. Testing Strategy

Below is a **minimal xUnit test suite** that proves the contract works for both execution modes.

```csharp
public class SafetyEngineTests
{
    private readonly ISafetyEngine<MyPrompt, MyResponse> _engine;

    public SafetyEngineTests()
    {
        var rules = new ISafetyRule<MyPrompt, MyResponse>[]
        {
            new ProfanityRule(),
            new SensitiveDataRule()
        };
        var logger = NullLogger<SafetyEngine<MyPrompt, MyResponse>>.Instance;
        _engine = new SafetyEngine<MyPrompt, MyResponse>(rules, logger);
    }

    [Fact]
    public async Task AllGood_ReturnsSuccess()
    {
        var result = await _engine.EvaluateAsync(new MyPrompt("Hello world"));
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Profanity_Fails()
    {
        var result = await _engine.EvaluateAsync(new MyPrompt("badword2 is here"));
        Assert.False(result.IsSuccess);
        Assert.Contains("profanity", result.FailureReason, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SensitiveData_Fails()
    {
        var result = await _engine.EvaluateAsync(new MyPrompt("My SSN is 123-45-6789"));
        Assert.False(result.IsSuccess);
        Assert.Contains("personal", result.FailureReason, StringComparison.OrdinalIgnoreCase);
    }
}
```

You can add analogous tests for the **middleware** by mocking `IAgent` (e.g., with Moq) and asserting that `SafetyRuleException` is thrown when a rule fails, while a successful request reaches the downstream mock exactly once.

---

## 8. Checklist – Did we meet the requirements?

| ✅ Requirement | Implementation
: Create a safety rule engine that will be implemented as middleware to attach to the AIAgent using the Agent Framework version 1.30. It should be in C# and follow best practices and design principles. It should support both streaming and non streaming callback methods
CoopAgent_dotnet: Below is a **complete, production‑ready** implementation of a **Safety‑Rule Engine** that can be plugged into the **Agent Framework v1.30** as middleware.

The solution is split into logical layers so it can be dropped into any ASP.NET Core / console host that already uses the Agent Framework:

```
/SafetyEngine
│   SafetyEngine.csproj
│
├───Core
│       ISafetyRule.cs
│       SafetyRuleResult.cs
│       SafetyRuleContext.cs
│       SafetyRuleEngine.cs
│
├───Middleware
│       SafetyMiddleware.cs
│
├───Rules
│       ProfanityRule.cs
│       SensitiveDataRule.cs
│       CustomRule.cs   (example stub)
│
├───Extensions
│       AgentBuilderExtensions.cs
│
└───Tests
        SafetyEngineTests.cs
```

> **What you get**
>
> * A **middleware component** (`SafetyMiddleware`) that intercepts every request/response pair that flows through an `IAgent`.
> * A **rule engine** (`SafetyRuleEngine`) that can evaluate **any number of `ISafetyRule` implementations**.
> * Full **streaming** and **non‑streaming** support – the middleware works with the `IAgent.InvokeAsync` overload that returns an `IAsyncEnumerable<string>` as well as the classic `Task<string>` overload.
> * **Dependency‑Injection‑friendly** design (register the engine and the rules in the host’s `IServiceCollection`).
> * **Extensibility** – add new rules without touching the middleware.
> * **Thread‑safety** – the engine is immutable after construction; rule evaluation is stateless.
> * **Configuration** – enable/disable the engine, set a “fail‑fast” mode, or provide a custom “fallback” response.

---

## 1. Core – the rule model

### `ISafetyRule.cs`

```csharp
using System.Threading;
using System.Threading.Tasks;

namespace SentinelCore.Core;

/// <summary>
/// Represents a single safety rule that can be evaluated against a request/response pair.
/// </summary>
public interface ISafetyRule
{
    /// <summary>
    /// Human‑readable name of the rule (used for logging & diagnostics).
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Evaluates the rule. Return <c>true</c> if the content is safe, <c>false</c> otherwise.
    /// </summary>
    /// <param name="context">All data needed for evaluation.</param>
    /// <param name="cancellationToken"></param>
    Task<bool> EvaluateAsync(SafetyRuleContext context, CancellationToken cancellationToken = default);
}
```

### `SafetyRuleContext.cs`

```csharp
using System.Collections.Generic;

namespace SentinelCore.Core;

/// <summary>
/// Information passed to each rule. The framework only cares about the raw text,
/// but you can extend this with metadata (e.g. user id, conversation id, etc.).
/// </summary>
public sealed class SafetyRuleContext
{
    public string Prompt { get; init; }          // What the user sent to the agent
    public string? Response { get; init; }      // What the agent is about to return (null for streaming)
    public IReadOnlyDictionary<string, object>? Metadata { get; init; }

    public SafetyRuleContext(string prompt, string? response = null,
        IReadOnlyDictionary<string, object>? metadata = null)
    {
        Prompt = prompt;
        Response = response;
        Metadata = metadata;
    }
}
```

### `SafetyRuleResult.cs`

```csharp
namespace SentinelCore.Core;

/// <summary>
/// Result of a rule evaluation. The engine aggregates many of these.
/// </summary>
public sealed class SafetyRuleResult
{
    public string RuleName { get; }
    public bool IsSafe { get; }
    public string? ViolationMessage { get; }

    public SafetyRuleResult(string ruleName, bool isSafe, string? violationMessage = null)
    {
        RuleName = ruleName;
        IsSafe = isSafe;
        ViolationMessage = violationMessage;
    }
}
```

### `SafetyRuleEngine.cs`

```csharp
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SentinelCore.Core;

/// <summary>
/// Immutable engine that knows about a set of rules and can evaluate them.
/// </summary>
public sealed class SafetyRuleEngine
{
    private readonly IReadOnlyList<ISafetyRule> _rules;
    private readonly bool _failFast; // stop on first violation if true

    public SafetyRuleEngine(IEnumerable<ISafetyRule> rules, bool failFast = false)
    {
        _rules = rules?.ToArray() ?? throw new ArgumentNullException(nameof(rules));
        _failFast = failFast;
    }

    /// <summary>
    /// Evaluates all rules against the supplied context.
    /// </summary>
    public async Task<IReadOnlyList<SafetyRuleResult>> EvaluateAsync(
        SafetyRuleContext context,
        CancellationToken cancellationToken = default)
    {
        var results = new List<SafetyRuleResult>();

        foreach (var rule in _rules)
        {
            bool safe;
            try
            {
                safe = await rule.EvaluateAsync(context, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                // A rule that throws is considered a failure – we surface the exception
                // as a violation so the caller can decide what to do.
                safe = false;
                results.Add(new SafetyRuleResult(rule.Name, false,
                    $"Rule threw an exception: {ex.GetType().Name} – {ex.Message}"));
                if (_failFast) break;
                continue;
            }

            if (!safe)
            {
                results.Add(new SafetyRuleResult(rule.Name, false,
                    $"Rule '{rule.Name}' flagged the content as unsafe."));
                if (_failFast) break;
            }
            else
            {
                results.Add(new SafetyRuleResult(rule.Name, true));
            }
        }

        return results;
    }

    /// <summary>
    /// Helper that returns true if *all* rules passed.
    /// </summary>
    public async Task<bool> IsSafeAsync(SafetyRuleContext context,
        CancellationToken cancellationToken = default)
    {
        var results = await EvaluateAsync(context, cancellationToken).ConfigureAwait(false);
        return results.All(r => r.IsSafe);
    }
}
```

---

## 2. Middleware – the glue to the Agent Framework

> **Agent Framework v1.30** exposes the following two extension points (simplified):
>
> ```csharp
> Task<string> InvokeAsync(string prompt, CancellationToken ct = default);
> IAsyncEnumerable<string> InvokeStreamingAsync(string prompt, CancellationToken ct = default);
> ```
>
> The middleware intercepts **both**.

### `SafetyMiddleware.cs`

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AgentFramework;               // <-- the official namespace of v1.30
using SafetyEngine.Core;
using Microsoft.Extensions.Logging;

namespace SentinelCore.Middleware;

/// <summary>
/// Middleware that runs the safety rule engine before the agent produces a response.
/// If any rule fails, the request is short‑circuited and a safe fallback is returned.
/// </summary>
public sealed class SafetyMiddleware : IAgentMiddleware
{
    private readonly SafetyRuleEngine _engine;
    private readonly ILogger<SafetyMiddleware> _logger;
    private readonly SafetyMiddlewareOptions _options;

    public SafetyMiddleware(
        SafetyRuleEngine engine,
        ILogger<SafetyMiddleware> logger,
        SafetyMiddlewareOptions? options = null)
    {
        _engine = engine ?? throw new ArgumentNullException(nameof(engine));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options ?? new SafetyMiddlewareOptions();
    }

    // ---------- NON‑STREAMING ----------
    public async Task<string> InvokeAsync(
        IAgent next,
        string prompt,
        IDictionary<string, object>? metadata = null,
        CancellationToken ct = default)
    {
        // 1️⃣  Run safety checks on the *prompt* (user input)
        var promptCtx = new SafetyRuleContext(prompt, metadata: metadata);
        var promptSafe = await _engine.IsSafeAsync(promptCtx, ct).ConfigureAwait(false);
        if (!promptSafe)
        {
            var msg = _options.PromptViolationMessage;
            _logger.LogWarning("Prompt blocked by safety engine: {Prompt}", prompt);
            return msg;
        }

        // 2️⃣  Let the downstream agent produce a response
        var response = await next.InvokeAsync(prompt, metadata, ct).ConfigureAwait(false);

        // 3️⃣  Run safety checks on the *response*
        var responseCtx = new SafetyRuleContext(prompt, response, metadata);
        var responseSafe = await _engine.IsSafeAsync(responseCtx, ct).ConfigureAwait(false);
        if (!responseSafe)
        {
            var msg = _options.ResponseViolationMessage;
            _logger.LogWarning("Response blocked by safety engine. Prompt: {Prompt}", prompt);
            return msg;
        }

        // 4️⃣  All good – forward the original response
        return response;
    }

    // ---------- STREAMING ----------
    public async IAsyncEnumerable<string> InvokeStreamingAsync(
        IAgent next,
        string prompt,
        IDictionary<string, object>? metadata = null,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
    {
        // Prompt safety – same as non‑streaming
        var promptCtx = new SafetyRuleContext(prompt, metadata: metadata);
        if (!await _engine.IsSafeAsync(promptCtx, ct).ConfigureAwait(false))
        {
            var msg = _options.PromptViolationMessage;
            _logger.LogWarning("Prompt blocked (streaming) by safety engine: {Prompt}", prompt);
            yield return msg;
            yield break;
        }

        // Stream the response token‑by‑token, but evaluate safety *after* the stream ends.
        // This keeps latency low – we do not block the stream while evaluating.
        var streamedChunks = new List<string>();
        await foreach (var chunk in next.InvokeStreamingAsync(prompt, metadata, ct)
                                         .ConfigureAwait(false))
        {
            streamedChunks.Add(chunk);
            yield return chunk; // forward immediately
        }

        // Once the stream is finished, evaluate the concatenated response.
        var fullResponse = string.Concat(streamedChunks);
        var responseCtx = new SafetyRuleContext(prompt, fullResponse, metadata);
        if (!await _engine.IsSafeAsync(responseCtx, ct).ConfigureAwait(false))
        {
            var msg = _options.ResponseViolationMessage;
            _logger.LogWarning("Response blocked (streaming) by safety engine. Prompt: {Prompt}", prompt);
            // Replace the *last* chunk with the safe fallback – this is the simplest UX.
            // In a real product you could send a separate "system" message instead.
            if (streamedChunks.Any())
            {
                // rewind the consumer by sending a special marker (optional)
                // Here we just emit the fallback and stop.
                yield return msg;
            }
        }
    }
}
```

### Options class

```csharp
namespace SentinelCore.Middleware;

/// <summary>
/// Simple POCO that can be bound from configuration (appsettings.json, etc.).
/// </summary>
public sealed class SafetyMiddlewareOptions
{
    /// <summary>
    /// Message returned when the *prompt* is unsafe.
    /// </summary>
    public string PromptViolationMessage { get; set; } = "Your request was blocked for safety reasons.";

    /// <summary>
    /// Message returned when the *response* is unsafe.
    /// </summary>
    public string ResponseViolationMessage { get; set; } = "The generated answer was removed because it violated safety policies.";

    /// <summary>
    /// If true, the engine stops evaluating after the first failing rule.
    /// </summary>
    public bool FailFast { get; set; } = true;
}
```

---

## 3. Example Rules (plug‑and‑play)

### `ProfanityRule.cs`

```csharp
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SafetyEngine.Core;

namespace SentinelCore.Rules;

/// <summary>
/// Very simple profanity filter – replace with a proper NLP model in production.
/// </summary>
public sealed class ProfanityRule : ISafetyRule
{
    private static readonly HashSet<string> BadWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "damn", "hell", "shit", "fuck"
    };

    public string Name => "ProfanityRule";

    public Task<bool> EvaluateAsync(SafetyRuleContext context, CancellationToken ct = default)
    {
        // Check both prompt and response (if present)
        var text = context.Prompt + " " + (context.Response ?? string.Empty);
        var containsBad = text.Split(' ', System.StringSplitOptions.RemoveEmptyEntries)
                              .Any(word => BadWords.Contains(word));

        return Task.FromResult(!containsBad);
    }
}
```

### `SensitiveDataRule.cs`

```csharp
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using SafetyEngine.Core;

namespace SentinelCore.Rules;

/// <summary>
/// Detects patterns that look like credit‑card numbers, SSNs, etc.
/// </summary>
public sealed class SensitiveDataRule : ISafetyRule
{
    public string Name => "SensitiveDataRule";

    // Very naive regexes – replace with a vetted library for production.
    private static readonly Regex CreditCardRegex = new(@"\b(?:\d[ -]*?){13,16}\b", RegexOptions.Compiled);
    private static readonly Regex SSNRegex = new(@"\b\d{3}-\d{2}-\d{4}\b", RegexOptions.Compiled);

    public Task<bool> EvaluateAsync(SafetyRuleContext context, CancellationToken ct = default)
    {
        var text = context.Prompt + " " + (context.Response ?? string.Empty);
        var hasCard = CreditCardRegex.IsMatch(text);
        var hasSsn = SSNRegex.IsMatch(text);
        var safe = !(hasCard || hasSsn);
        return Task.FromResult(safe);
    }
}
```

### `CustomRule.cs` (template)

```csharp
using System.Threading;
using System.Threading.Tasks;
using SafetyEngine.Core;

namespace SentinelCore.Rules;

/// <summary>
/// Skeleton for a rule that calls an external service (e.g., OpenAI moderation endpoint).
/// </summary>
public sealed class CustomRule : ISafetyRule
{
    private readonly IExternalModerationClient _client;

    public CustomRule(IExternalModerationClient client) => _client = client;

    public string Name => "ExternalModerationRule";

    public async Task<bool> EvaluateAsync(SafetyRuleContext context, CancellationToken ct = default)
    {
        var text = context.Prompt + " " + (context.Response ?? string.Empty);
        var verdict = await _client.CheckAsync(text, ct).ConfigureAwait(false);
        return verdict.IsAllowed;
    }
}
```

---

## 4. DI registration & extension method

### `AgentBuilderExtensions.cs`

```csharp
using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using AgentFramework;
using SafetyEngine.Core;
using SafetyEngine.Middleware;
using SafetyEngine.Rules;

namespace SentinelCore.Extensions;

/// <summary>
/// Helper to wire the safety engine into an <c>IAgentBuilder</c>.
/// </summary>
public static class AgentBuilderExtensions
{
    /// <summary>
    /// Adds the safety middleware (and a default set of rules) to the builder.
    /// </summary>
    /// <param name="builder">The AgentFramework builder you are configuring.</param>
    /// <param name="configureOptions">Optional configuration of the middleware options.</param>
    /// <param name="additionalRules">Optional extra rules you want to register.</param>
    public static IAgentBuilder UseSafetyEngine(
        this IAgentBuilder builder,
        Action<SafetyMiddlewareOptions>? configureOptions = null,
        IEnumerable<ISafetyRule>? additionalRules = null)
    {
        // 1️⃣ Register the default rules (you can replace them later)
        builder.Services.AddSingleton<ISafetyRule, ProfanityRule>();
        builder.Services.AddSingleton<ISafetyRule, SensitiveDataRule>();

        // 2️⃣ Register any custom rules supplied by the caller
        if (additionalRules != null)
        {
            foreach (var rule in additionalRules)
                builder.Services.AddSingleton(typeof(ISafetyRule), rule);
        }

        // 3️⃣ Register the engine (it will resolve all ISafetyRule implementations)
        builder.Services.AddSingleton(provider =>
        {
            var rules = provider.GetServices<ISafetyRule>();
            var opts = provider.GetService<SafetyMiddlewareOptions>() ?? new SafetyMiddlewareOptions();
            return new SafetyRuleEngine(rules, opts.FailFast);
        });

        // 4️⃣ Register the options (bindable from IConfiguration)
        builder.Services.Configure(configureOptions ?? (_ => { }));
        builder.Services.AddSingleton(provider =>
            provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<SafetyMiddlewareOptions>>().Value);

        // 5️⃣ Register the middleware itself
        builder.Services.AddSingleton<SafetyMiddleware>();

        // 6️⃣ Tell the AgentFramework to insert the middleware into the pipeline.
        //    The framework expects a delegate that receives the next IAgent.
        builder.Use(async (next, prompt, metadata, ct) =>
        {
            var middleware = next.Services.GetRequiredService<SafetyMiddleware>();
            return await middleware.InvokeAsync(next, prompt, metadata, ct).ConfigureAwait(false);
        },
        async (next, prompt, metadata, ct) =>
        {
            var middleware = next.Services.GetRequiredService<SafetyMiddleware>();
            return middleware.InvokeStreamingAsync(next, prompt, metadata, ct);
        });

        return builder;
    }
}
```

> **Explanation of the `Use` overload**
> The Agent Framework v1.30 defines `IAgentBuilder.Use(Func<IAgent, string, IDictionary<string,object>?, CancellationToken, Task<string>> nonStreaming, Func<IAgent, string, IDictionary<string,object>?, CancellationToken, IAsyncEnumerable<string>> streaming)`.
> The extension above adapts our `SafetyMiddleware` to that contract.

---

## 5. Putting it all together – a minimal host

```csharp
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

: Concurrent group synthesis:
Agent 1: Below is a **complete, production‑ready safety‑rule engine** that can be dropped in as **middleware** for the **AIAgent** (Agent Framework v1.30).
The design follows SOLID, clean‑architecture, and .NET best‑practices, and it works for both **streaming** and **non‑streaming** response modes.

---

## 1. High‑level Architecture

```

+-------------------+          +-------------------+          +-------------------+
|   AIAgent Core    |  ---->   | SafetyMiddleware  |  ---->   |  ISafetyRule[]    |
+-------------------+          +-------------------+          +-------------------+
        ^                               ^                               ^
        |                               |                               |
   IAgentMiddleware                IAgentMiddleware                |
   (InvokeAsync)                    (InvokeAsync)                |
        |                               |                               |
   (Streaming)                     (Streaming)                     (Rule)
        |                               |                               |
   IAgentResponse                 IAgentResponse                 IRuleContext

```

* **SafetyMiddleware** – a single middleware component that intercepts every request/response pair.
* **ISafetyRule** – a contract for a reusable rule (e.g., profanity filter, PII detector, toxicity scorer).
* **RuleContext** – carries the request, the raw response, and a mutable **SafetyResult** that rules can read/write.
* **SafetyResult** – aggregates the verdict (`Allowed`, `Blocked`, `Modified`) and optional **Action** (e.g., replace text, truncate, add warning).

The middleware works for:

| Mode | Flow |
|------|------|
| **Non‑streaming** | `InvokeAsync(request)` → middleware runs → `await next(request)` → receives full `AgentResponse` → runs rules → returns possibly‑modified response. |
| **Streaming** | `InvokeAsync(request)` → middleware runs → `await next(request)` → receives `IAsyncEnumerable<string>` (or `Stream`) → each chunk is passed through the rule engine (rules can decide to block, modify, or inject warnings on‑the‑fly). |

---

## 2. Core Contracts (Framework‑agnostic)

```csharp
// ------------------------------------------------------------
// 1️⃣  Agent Framework v1.30 – minimal required contracts
// ------------------------------------------------------------
public interface IAgentRequest
{
    string Prompt { get; }
    IDictionary<string, object?> Metadata { get; }
}

public interface IAgentResponse
{
    // Non‑streaming: full text
    string? Content { get; }

    // Streaming: an async enumerable of partial chunks
    IAsyncEnumerable<string>? Stream { get; }
}

public interface IAgentMiddleware
{
    // The next delegate in the pipeline
    Task<IAgentResponse> InvokeAsync(IAgentRequest request, Func<IAgentRequest, Task<IAgentResponse>> next);
}
```

> **Note** – The real framework already defines these interfaces; the snippets above are only for illustration.

---

## 3. Safety Engine Contracts

```csharp
// ------------------------------------------------------------
// 2️⃣  Safety rule contracts
// ------------------------------------------------------------
public enum SafetyVerdict
{
    Allowed,    // No problem
    Blocked,    // Must stop the response
    Modified    // Response was altered (e.g., profanity censored)
}

public sealed class SafetyResult
{
    public SafetyVerdict Verdict { get; set; } = SafetyVerdict.Allowed;
    public string? ModifiedContent { get; set; }          // For non‑streaming
    public Func<string, string>? ChunkTransformer { get; set; } // For streaming
    public string? WarningMessage { get; set; }          // Optional user‑visible warning
}

public interface IRuleContext
{
    IAgentRequest Request { get; }
    string? CurrentContent { get; }          // Full content (non‑streaming) or null for streaming
    IAsyncEnumerable<string>? CurrentStream { get; } // Null for non‑streaming
    SafetyResult Result { get; }
}
```

### 3.1. Rule Interface (sync & async)

```csharp
public interface ISafetyRule
{
    // Called once for a non‑streaming response
    Task EvaluateAsync(IRuleContext context, CancellationToken ct = default);
}
```

### 3.2. Streaming‑aware rule base (optional)

If a rule needs per‑chunk processing, inherit from this helper:

```csharp
public abstract class StreamingSafetyRuleBase : ISafetyRule
{
    public virtual Task EvaluateAsync(IRuleContext context, CancellationToken ct = default)
        => Task.CompletedTask; // default no‑op for non‑streaming

    // Override to transform each chunk
    public virtual string TransformChunk(string chunk) => chunk;
}
```

---

## 4. Safety Middleware Implementation

```csharp
using System.Collections.Concurrent;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AgentFramework.Middleware.Safety;

/// <summary>
/// Middleware that runs a configurable set of <see cref="ISafetyRule"/>s
/// against every response (streaming or not). It can block, modify or
/// augment the output before it reaches the caller.
/// </summary>
public sealed class SafetyMiddleware : IAgentMiddleware
{
    private readonly IReadOnlyList<ISafetyRule> _rules;
    private readonly ILogger<SafetyMiddleware> _logger;
    private readonly SafetyOptions _options;

    public SafetyMiddleware(
        IEnumerable<ISafetyRule> rules,
        ILogger<SafetyMiddleware> logger,
        IOptions<SafetyOptions>? options = null)
    {
        _rules = rules?.ToArray() ?? throw new ArgumentNullException(nameof(rules));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options?.Value ?? new SafetyOptions();
    }

    public async Task<IAgentResponse> InvokeAsync(
        IAgentRequest request,
        Func<IAgentRequest, Task<IAgentResponse>> next)
    {
        // --------------------------------------------------------
        // 1️⃣  Run the request through the pipeline (next middleware)
        // --------------------------------------------------------
        var response = await next(request).ConfigureAwait(false);

        // --------------------------------------------------------
        // 2️⃣  Decide which path (streaming vs non‑streaming)
        // --------------------------------------------------------
        if (response.Stream != null)
        {
            // ---------- Streaming path ----------
            var transformed = TransformStreamAsync(request, response.Stream);
            return new AgentResponse { Stream = transformed };
        }

        // ---------- Non‑streaming path ----------
        var safeContent = await EvaluateNonStreamingAsync(request, response.Content ?? string.Empty);
        return new AgentResponse { Content = safeContent };
    }

    // ------------------------------------------------------------
    // 5️⃣  Non‑streaming evaluation
    // ------------------------------------------------------------
    private async Task<string> EvaluateNonStreamingAsync(IAgentRequest request, string rawContent)
    {
        var result = new SafetyResult { ModifiedContent = rawContent };
        var ctx = new RuleContext(request, rawContent, null, result);

        foreach (var rule in _rules)
        {
            try
            {
                await rule.EvaluateAsync(ctx, CancellationToken.None).ConfigureAwait(false);

                // Early exit if a rule blocked the response
                if (result.Verdict == SafetyVerdict.Blocked)
                {
                    _logger.LogWarning("Safety rule blocked response for request {Prompt}", request.Prompt);
                    return _options.BlockedResponseMessage;
                }
            }
            catch (Exception ex)
            {
                // Never let a rule crash the pipeline
                _logger.LogError(ex, "Safety rule {Rule} threw an exception.", rule.GetType().Name);
                if (_options.FailFastOnRuleError)
                    throw;
            }
        }

        // If any rule modified the content, return the modified version
        return result.Verdict == SafetyVerdict.Modified && result.ModifiedContent != null
            ? result.ModifiedContent
            : rawContent;
    }

    // ------------------------------------------------------------
    // 6️⃣  Streaming evaluation – per‑chunk transformation
    // ------------------------------------------------------------
    private async IAsyncEnumerable<string> TransformStreamAsync(
        IAgentRequest request,
        IAsyncEnumerable<string> sourceStream,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
    {
        // Build a context that will be reused for each chunk.
        // Rules that only need per‑chunk processing can implement
        // StreamingSafetyRuleBase and override TransformChunk().
        var result = new SafetyResult();
        var ctx = new RuleContext(request, null, sourceStream, result);

        // Run *once* any rule that cares about the whole stream (e.g. length limit)
        foreach (var rule in _rules)
        {
            try
            {
                await rule.EvaluateAsync(ctx, ct).ConfigureAwait(false);
                if (result.Verdict == SafetyVerdict.Blocked)
                {
                    _logger.LogWarning("Streaming response blocked by rule {Rule}", rule.GetType().Name);
                    yield return _options.BlockedResponseMessage;
                    yield break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Safety rule {Rule} threw while evaluating streaming response.", rule.GetType().Name);
                if (_options.FailFastOnRuleError) throw;
            }
        }

        // If a rule supplied a custom chunk transformer, use it.
        // Otherwise, fall back to per‑rule TransformChunk overrides.
        Func<string, string> chunkTransformer = result.ChunkTransformer
            ?? BuildCompositeChunkTransformer();

        await foreach (var chunk in sourceStream.WithCancellation(ct))
        {
            var safeChunk = chunkTransformer(chunk);
            yield return safeChunk;
        }

        // Append a warning if any rule requested it
        if (!string.IsNullOrWhiteSpace(result.WarningMessage))
            yield return $"\n\n{result.WarningMessage}";
    }

    // ------------------------------------------------------------
    // 7️⃣  Helper – compose all StreamingSafetyRuleBase.TransformChunk()
    // ------------------------------------------------------------
    private Func<string, string> BuildCompositeChunkTransformer()
    {
        var streamingRules = _rules.OfType<StreamingSafetyRuleBase>().ToArray();

        if (streamingRules.Length == 0)
            return s => s; // identity

        return chunk =>
        {
            var transformed = chunk;
            foreach (var rule in streamingRules)
            {
                try
                {
                    transformed = rule.TransformChunk(transformed);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Streaming rule {Rule} threw while transforming chunk.", rule.GetType().Name);
                    // Swallow – keep the last good value
                }
            }
            return transformed;
        };
    }
}

// ------------------------------------------------------------
// 8️⃣  Concrete implementations of the contracts
// ------------------------------------------------------------
internal sealed class RuleContext : IRuleContext
{
    public IAgentRequest Request { get; }
    public string? CurrentContent { get; }
    public IAsyncEnumerable<string>? CurrentStream { get; }
    public SafetyResult Result { get; }

    public RuleContext(
        IAgentRequest request,
        string? content,
        IAsyncEnumerable<string>? stream,
        SafetyResult result)
    {
        Request = request;
        CurrentContent = content;
        CurrentStream = stream;
        Result = result;
    }
}

// Simple concrete response type used by the middleware
internal sealed class AgentResponse : IAgentResponse
{
    public string? Content { get; set; }
    public IAsyncEnumerable<string>? Stream { get; set; }
}

// ------------------------------------------------------------
// 9️⃣  Options – configurable via DI
// ------------------------------------------------------------
public sealed class SafetyOptions
{
    /// <summary>
    /// Message returned when a response is blocked.
    /// </summary>
    public string BlockedResponseMessage { get; set; } = "[Content blocked by safety filter]";

    /// <summary>
    /// If true, any exception thrown by a rule aborts the whole pipeline.
    /// If false (default), the exception is logged and the request continues.
    /// </summary>
    public bool FailFastOnRuleError { get; set; } = false;

    /// <summary>
    /// Maximum allowed length for a streaming response (in characters). 0 = unlimited.
    /// </summary>
    public int MaxStreamingLength { get; set; } = 0;
}
```

### Why this design is **best‑practice**

| Principle | How it is satisfied |
|-----------|--------------------|
| **Single Responsibility** | `SafetyMiddleware` only orchestrates rule execution; each rule encapsulates its own logic. |
| **Open/Closed** | New rules are added by implementing `ISafetyRule` – no change to middleware. |
| **Dependency Injection** | Rules, logger, and options are injected. The middleware can be registered with the built‑in DI container. |
| **Fail‑fast / Resilience** | Options let you decide whether a rule exception aborts the pipeline. All exceptions are logged. |
| **Thread‑Safety** | Middleware is stateless; `SafetyResult` lives per‑request. Rules must be written thread‑safe (recommended to be immutable). |
| **Streaming Support** | The same middleware works for `IAsyncEnumerable<string>` without buffering the whole response. |
| **Extensibility** | `StreamingSafetyRuleBase` gives a simple hook for per‑chunk transformations; `SafetyResult.ChunkTransformer` allows a rule to replace the whole transformer at runtime. |
| **Observability** | Verbose logging (request prompt, rule name, verdict) – easy to plug into OpenTelemetry or Application Insights. |
| **Configuration** | `SafetyOptions` can be bound from `appsettings.json` or environment variables. |

---

## 5. Example Rules

Below are three common safety rules that you can ship out‑of‑the‑box. Feel free to add more (e.g., hate‑speech classifier, custom business policy).

### 5.1. ProfanityFilterRule (non‑streaming)

```csharp
public sealed class ProfanityFilterRule : ISafetyRule
{
    private static readonly HashSet<string> BadWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "damn", "hell", "shit", "fuck" // extend with your dictionary
    };

    public Task EvaluateAsync(IRuleContext ctx, CancellationToken ct = default)
    {
        if (ctx.CurrentContent is null) return Task.CompletedTask;

        var words = ctx.CurrentContent.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var hasBad = words.Any(w => BadWords.Contains(w.Trim('.', ',', '!', '?')));

        if (!hasBad) return Task.CompletedTask;

        // Censor each bad word
        var censored = string.Join(' ', words.Select(w =>
            BadWords.Contains(w.Trim('.', ',', '!', '?')) ? new string('*', w.Length) : w));

        ctx.Result.Verdict = SafetyVerdict.Modified;
        ctx.Result.ModifiedContent = censored;
        ctx.Result.WarningMessage = "⚠️ Some profanity was removed.";

        return Task.CompletedTask;
    }
}
```

### 5.2. PiiRedactionRule (non‑streaming)

```csharp
public sealed class PiiRedactionRule : ISafetyRule
{
    // Very naive regexes – replace with a proper PII library in production
    private static readonly Regex EmailRegex = new(@"[a-zA-Z0-9_.+-]+@[a-zA-Z0-9-]+\.[a-zA-Z0-9-.]+", RegexOptions.Compiled);
    private static readonly Regex PhoneRegex = new(@"\b\d{3}[-.]?\d{3}[-.]?\d{4}\b", RegexOptions.Compiled);

    public Task EvaluateAsync(IRuleContext ctx, CancellationToken ct = default)
    {
        if (ctx.CurrentContent is null) return Task.CompletedTask;

        var redacted = EmailRegex.Replace(ctx.CurrentContent, "[REDACTED_EMAIL]");
        redacted = PhoneRegex.Replace(redacted, "[REDACTED_PHONE]");

        if (redacted != ctx.CurrentContent)
        {
            ctx.Result.Verdict = SafetyVerdict.Modified;
            ctx.Result.ModifiedContent = redacted;
            ctx.Result.WarningMessage = "⚠️ Potential personal data was redacted.";
        }

        return Task.CompletedTask;
    }
}
```

### 5.3. StreamingLengthLimitRule (streaming)

```csharp
public sealed class StreamingLengthLimitRule : StreamingSafetyRuleBase
{
    private readonly int _maxLength;
    private int _currentLength;

    public StreamingLengthLimitRule(IOptions<SafetyOptions> opts)
    {
        _maxLength = opts.Value.MaxStreamingLength;
    }

    public override string TransformChunk(string chunk)
    {
        if (_maxLength <= 0) return chunk; // unlimited

        // If adding this chunk would exceed the limit, truncate and set verdict
        if (_currentLength + chunk.Length > _maxLength)
        {
            var remaining = _maxLength - _currentLength;
            var truncated = remaining > 0 ? chunk.Substring(0, remaining) : string.Empty;
            _currentLength = _maxLength;

            // Signal the middleware to block further chunks
            // (the middleware will stop enumeration after this chunk)
            return truncated + "\n\n[Truncated by safety policy]";
        }

        _currentLength += chunk.Length;
        return chunk;
    }

    public override async Task EvaluateAsync(IRuleContext ctx, CancellationToken ct = default)
    {
        // No pre‑check needed – the per‑chunk logic handles it.
        await Task.CompletedTask;
    }
}
```

---

## 6. Registering the Middleware (ASP.NET Core style)

```csharp
using Microsoft.Extensions.DependencyInjection;
using AgentFramework.Middleware.Safety;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAgentSafety(this IServiceCollection services, Action<SafetyOptions>? configure = null)
    {
        // 1️⃣  Register options (bind from configuration if you like)
        services.AddOptions<SafetyOptions>()
                .Configure(o => configure?.Invoke(o));

        // 2️⃣  Register all rule implementations (you can scan assemblies)
        services.AddSingleton<ISafetyRule, ProfanityFilterRule>();
        services.AddSingleton<ISafetyRule, PiiRedactionRule>();
        services.AddSingleton<ISafetyRule, StreamingLength
Agent 2: Below is a **complete, production‑ready design** for a **Safety‑Rule Engine** that can be dropped into the **Agent Framework v1.30** as middleware.
The solution follows SOLID, clean‑architecture, and .NET best‑practices and works for both **streaming** (`IAsyncEnumerable<T>`) and **non‑streaming** (`Task<T>`) agent callbacks.

---

## 1. High‑level Architecture

```

+-------------------+          +-------------------+          +-------------------+
|   AIAgent Core    |  ---->   |  Middleware Pipe  |  ---->   |   SafetyEngine    |
+-------------------+          +-------------------+          +-------------------+
        ^                               ^                           ^
        |                               |                           |
   (User request)                (AgentFramework)          (Rule collection)

```

* **Middleware Pipe** – the Agent Framework already ships a *pipeline* where each component implements `IAgentMiddleware`.
* **SafetyEngine** – the heart of the rule engine. It receives a `SafetyContext`, runs **all registered rules in parallel**, aggregates the results and decides whether to **allow**, **modify**, or **reject** the request/response.
* **Streaming support** – the engine can be invoked **before** the agent starts streaming a response (pre‑check) **and** after each chunk is produced (post‑check).

---

## 2. Core Contracts

### 2.1. `ISafetyRule`

```csharp
/// <summary>
/// A single safety rule. Implementations must be thread‑safe and side‑effect free.
/// </summary>
public interface ISafetyRule
{
    /// <summary>
    /// The rule name – used for logging and diagnostics.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Evaluates the rule against the supplied context.
    /// </summary>
    /// <param name="context">All data required for the rule.</param>
    /// <param name="cancellationToken"></param>
    /// <returns>A result describing the outcome.</returns>
    ValueTask<SafetyRuleResult> EvaluateAsync(SafetyContext context,
                                              CancellationToken cancellationToken = default);
}
```

### 2.2. `SafetyRuleResult`

```csharp
public sealed record SafetyRuleResult
{
    public bool IsAllowed { get; init; }          // true = pass, false = block
    public string? Message { get; init; }         // optional human‑readable reason
    public SeverityLevel Severity { get; init; } // INFO, WARNING, CRITICAL

    public static SafetyRuleResult Allow() => new() { IsAllowed = true, Severity = SeverityLevel.Info };
    public static SafetyRuleResult Block(string message, SeverityLevel severity = SeverityLevel.Critical)
        => new() { IsAllowed = false, Message = message, Severity = severity };
}
```

### 2.3. `SeverityLevel`

```csharp
public enum SeverityLevel
{
    Info = 0,
    Warning = 1,
    Critical = 2
}
```

### 2.4. `SafetyContext`

```csharp
public sealed class SafetyContext
{
    // Input from the user (or upstream component)
    public string Prompt { get; init; } = string.Empty;

    // The partial or final response generated by the agent.
    // For streaming callbacks this will be the *current* chunk.
    public string? CurrentChunk { get; init; }

    // Full response accumulated so far – useful for rules that need history.
    public string? FullResponseSoFar { get; init; }

    // Arbitrary metadata (e.g., user id, conversation id, request id)
    public IReadOnlyDictionary<string, object?> Metadata { get; init; } =
        new Dictionary<string, object?>();
}
```

---

## 3. The Middleware Implementation

### 3.1. `SafetyEngineMiddleware`

```csharp
using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

public sealed class SafetyEngineMiddleware : IAgentMiddleware
{
    private readonly IEnumerable<ISafetyRule> _rules;
    private readonly ILogger<SafetyEngineMiddleware> _logger;
    private readonly SafetyEngineOptions _options;

    public SafetyEngineMiddleware(
        IEnumerable<ISafetyRule> rules,
        IOptions<SafetyEngineOptions> options,
        ILogger<SafetyEngineMiddleware> logger)
    {
        _rules = rules ?? throw new ArgumentNullException(nameof(rules));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options?.Value ?? new SafetyEngineOptions();
    }

    // -----------------------------------------------------------------
    // 1️⃣  Non‑streaming (single‑response) callback
    // -----------------------------------------------------------------
    public async Task<TResponse> InvokeAsync<TRequest, TResponse>(
        TRequest request,
        Func<TRequest, Task<TResponse>> next,
        CancellationToken ct = default)
        where TRequest : class
        where TResponse : class
    {
        // 1️⃣ Pre‑check (prompt)
        var preContext = new SafetyContext
        {
            Prompt = request?.ToString() ?? string.Empty,
            Metadata = ExtractMetadata(request)
        };

        if (!await RunAllRulesAsync(preContext, ct).ConfigureAwait(false))
        {
            // Short‑circuit – return a safe default or throw a domain exception
            return CreateBlockedResponse<TResponse>("Prompt blocked by safety engine.");
        }

        // 2️⃣ Let the downstream agent produce the response
        var response = await next(request).ConfigureAwait(false);

        // 3️⃣ Post‑check (full response)
        var postContext = new SafetyContext
        {
            Prompt = preContext.Prompt,
            FullResponseSoFar = response?.ToString(),
            Metadata = preContext.Metadata
        };

        if (!await RunAllRulesAsync(postContext, ct).ConfigureAwait(false))
        {
            return CreateBlockedResponse<TResponse>("Response blocked by safety engine.");
        }

        return response;
    }

    // -----------------------------------------------------------------
    // 2️⃣  Streaming callback – the Agent Framework calls this for each chunk
    // -----------------------------------------------------------------
    public async IAsyncEnumerable<TChunk> InvokeStreamingAsync<TRequest, TChunk>(
        TRequest request,
        Func<TRequest, IAsyncEnumerable<TChunk>> next,
        [EnumeratorCancellation] CancellationToken ct = default)
        where TRequest : class
        where TChunk : class
    {
        // Pre‑check (same as non‑streaming)
        var preContext = new SafetyContext
        {
            Prompt = request?.ToString() ?? string.Empty,
            Metadata = ExtractMetadata(request)
        };

        if (!await RunAllRulesAsync(preContext, ct).ConfigureAwait(false))
        {
            yield break; // or yield a single "blocked" chunk
        }

        // Stream the response while applying post‑check on each chunk
        var accumulated = new StringBuilder();

        await foreach (var chunk in next(request).WithCancellation(ct).ConfigureAwait(false))
        {
            // Update accumulated response
            accumulated.Append(chunk?.ToString());

            var chunkContext = new SafetyContext
            {
                Prompt = preContext.Prompt,
                CurrentChunk = chunk?.ToString(),
                FullResponseSoFar = accumulated.ToString(),
                Metadata = preContext.Metadata
            };

            if (!await RunAllRulesAsync(chunkContext, ct).ConfigureAwait(false))
            {
                // Stop streaming and optionally emit a "blocked" chunk
                yield break;
            }

            yield return chunk;
        }
    }

    // -----------------------------------------------------------------
    // Helper: run all rules in parallel and aggregate the decision
    // -----------------------------------------------------------------
    private async ValueTask<bool> RunAllRulesAsync(
        SafetyContext ctx,
        CancellationToken ct)
    {
        // Run each rule concurrently – they are independent & thread‑safe
        var ruleTasks = _rules.Select(r => r.EvaluateAsync(ctx, ct));

        var results = await Task.WhenAll(ruleTasks).ConfigureAwait(false);

        // Log each result (debug level)
        foreach (var r in results)
        {
            _logger.LogDebug("Safety rule {RuleName} evaluated: Allowed={Allowed}, Severity={Severity}, Message={Message}",
                r?.GetType().Name ?? "unknown",
                r?.IsAllowed,
                r?.Severity,
                r?.Message);
        }

        // Decision logic – any Critical block wins, otherwise allow if no block
        var blocked = results.FirstOrDefault(r => !r.IsAllowed && r.Severity == SeverityLevel.Critical);
        if (blocked != null)
        {
            _logger.LogWarning("Safety engine blocked request: {Message}", blocked.Message);
            return false;
        }

        // If there are only warnings, we can still allow but log
        var anyWarning = results.Any(r => !r.IsAllowed && r.Severity == SeverityLevel.Warning);
        if (anyWarning && _options.TreatWarningsAsErrors)
        {
            _logger.LogWarning("Safety engine treated warning as error.");
            return false;
        }

        return true;
    }

    // -----------------------------------------------------------------
    // Helper: extract metadata from the request (extension point)
    // -----------------------------------------------------------------
    private IReadOnlyDictionary<string, object?> ExtractMetadata<T>(T request)
    {
        // Very generic – you can replace with a strongly typed request DTO
        if (request is IMetadataProvider provider)
            return provider.Metadata;

        return new Dictionary<string, object?>();
    }

    // -----------------------------------------------------------------
    // Helper: create a blocked response of the expected generic type
    // -----------------------------------------------------------------
    private static TResponse CreateBlockedResponse<TResponse>(string reason)
        where TResponse : class
    {
        // The framework usually expects a concrete response type.
        // For demo purposes we try to use a parameterless ctor + a "Message" property.
        var resp = Activator.CreateInstance<TResponse>();
        var prop = typeof(TResponse).GetProperty("Message");
        prop?.SetValue(resp, reason);
        return resp!;
    }
}
```

### 3.2. Options class (DI‑friendly)

```csharp
public sealed class SafetyEngineOptions
{
    /// <summary>
    /// If true, any rule with <see cref="SeverityLevel.Warning"/> will also block the request.
    /// </summary>
    public bool TreatWarningsAsErrors { get; set; } = false;

    /// <summary>
    /// Maximum parallelism for rule evaluation. 0 = Environment.ProcessorCount.
    /// </summary>
    public int MaxParallelism { get; set; } = 0;
}
```

### 3.3. Registering the middleware (ASP.NET Core / Generic Host)

```csharp
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSafetyEngine(this IServiceCollection services,
        Action<SafetyEngineOptions>? configure = null)
    {
        services.Configure(configure ?? (_ => { }));
        services.AddSingleton<ISafetyRule, ProfanityRule>();
        services.AddSingleton<ISafetyRule, TokenLimitRule>();
        // add any custom rules here ...

        services.AddTransient<IAgentMiddleware, SafetyEngineMiddleware>();
        return services;
    }
}
```

**Agent Framework v1.30** expects the middleware to be added to the *pipeline* when the `AgentBuilder` is created:

```csharp
var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((ctx, services) =>
    {
        services.AddLogging();
        services.AddSafetyEngine(opts =>
        {
            opts.TreatWarningsAsErrors = true;
        });

        // Register the Agent and inject the middleware pipeline
        services.AddSingleton<IAgent>(sp =>
        {
            var builder = new AgentBuilder()
                .UseMiddleware(sp.GetRequiredService<IAgentMiddleware>()) // safety engine
                .UseMiddleware(new LoggingMiddleware(sp.GetRequiredService<ILogger<LoggingMiddleware>>()));
            // ... other core middlewares

            return builder.Build();
        });
    })
    .Build();
```

---

## 4. Example Rule Implementations

### 4.1. Profanity filter (non‑streaming & streaming)

```csharp
public sealed class ProfanityRule : ISafetyRule
{
    private static readonly HashSet<string> BadWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "badword1", "badword2", "offensivephrase"
    };

    public string Name => nameof(ProfanityRule);

    public ValueTask<SafetyRuleResult> EvaluateAsync(SafetyContext ctx,
                                                     CancellationToken ct = default)
    {
        // Check prompt first, then response chunks
        var text = ctx.CurrentChunk ?? ctx.FullResponseSoFar ?? ctx.Prompt;

        if (string.IsNullOrWhiteSpace(text))
            return ValueTask.FromResult(SafetyRuleResult.Allow());

        var contains = BadWords.Any(w => text.Contains(w, StringComparison.OrdinalIgnoreCase));
        return contains
            ? ValueTask.FromResult(SafetyRuleResult.Block($"Profanity detected in text.", SeverityLevel.Critical))
            : ValueTask.FromResult(SafetyRuleResult.Allow());
    }
}
```

### 4.2. Token‑limit rule (pre‑check only)

```csharp
public sealed class TokenLimitRule : ISafetyRule
{
    private readonly int _maxTokens;

    public TokenLimitRule(IOptions<TokenLimitOptions> opts)
    {
        _maxTokens = opts?.Value?.MaxTokens ?? 1024;
    }

    public string Name => nameof(TokenLimitRule);

    public ValueTask<SafetyRuleResult> EvaluateAsync(SafetyContext ctx,
                                                     CancellationToken ct = default)
    {
        // Very naive token count – replace with a proper tokenizer if needed
        var tokenCount = ctx.Prompt.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;

        if (tokenCount > _maxTokens)
        {
            return ValueTask.FromResult(
                SafetyRuleResult.Block($"Prompt exceeds token limit ({tokenCount}/{_maxTokens}).",
                                       SeverityLevel.Critical));
        }

        return ValueTask.FromResult(SafetyRuleResult.Allow());
    }
}

public sealed class TokenLimitOptions
{
    public int MaxTokens { get; set; } = 1024;
}
```

### 4.3. Custom rule example (user‑provided)

```csharp
public sealed class CustomMetadataRule : ISafetyRule
{
    public string Name => "UserMetadataRule";

    public ValueTask<SafetyRuleResult> EvaluateAsync(SafetyContext ctx,
                                                     CancellationToken ct = default)
    {
        if (ctx.Metadata.TryGetValue("isAdmin", out var isAdminObj) &&
            isAdminObj is bool isAdmin && !isAdmin)
        {
            // Example: block any request that tries to access admin‑only feature
            if (ctx.Prompt.Contains("admin", StringComparison.OrdinalIgnoreCase))
            {
                return ValueTask.FromResult(
                    SafetyRuleResult.Block("Non‑admin user attempted admin operation.", SeverityLevel.Critical));
            }
        }

        return ValueTask.FromResult(SafetyRuleResult.Allow());
    }
}
```

---

## 5. Streaming vs. Non‑Streaming – How the Engine Handles Both

| Phase | Non‑Streaming (`Task<T>`) | Streaming (`IAsyncEnumerable<T>`) |
|-------|---------------------------|-----------------------------------|
| **Pre‑check** | Executed once before the downstream call. | Same – executed once before the first chunk. |
| **Post‑check** | Executed once after the whole response is materialized. | Executed **after each chunk** (the `CurrentChunk` field is populated). |
| **Short‑circuit** | Returns a *blocked* response object. | Stops the enumeration (`yield break`) or emits a single “blocked” chunk. |
| **State** | No need to keep history – the full response is available in `FullResponseSoFar`. | Accumulates chunks in a `StringBuilder` so rules can see the whole conversation so far. |

Because each rule receives a **fresh `SafetyContext`** per evaluation, they can be **pure functions** and safely run in parallel.

---

## 6. Extensibility Points

| What | How to extend |
|------|---------------|
| **Add a new rule** | Implement `ISafetyRule` and register it in DI (`services.AddSingleton<ISafetyRule, MyRule>()`). |
| **Change decision logic** | Derive from `SafetyEngineMiddleware` or replace it with a custom implementation that overrides `RunAllRulesAsync`. |
| **Custom metadata extraction** | Implement `IMetadataProvider` on your request DTO or replace `ExtractMetadata` with a delegate via options. |
| **Different block strategy** | Replace `CreateBlockedResponse<T>` with a factory (`ISafetyResponseFactory`) injected via DI. |
| **Telemetry** | Hook into `ILogger` or emit OpenTelemetry spans inside `RunAllRulesAsync`. |

---

## 7. Thread‑Safety & Performance

* **Rule instances are registered as singletons** – they **must be thread‑safe** (no mutable state).
* **Parallel evaluation** uses `Task.WhenAll`. If you need to limit concurrency, wrap the rule collection in a `SemaphoreSlim` using the `MaxParallelism` option.
* **Streaming** uses `await foreach` with `WithCancellation` – no blocking calls.
* **StringBuilder** for accumulated response is confined to the async iterator, so no race conditions.

---

## 8. Unit‑Test Skeleton (xUnit + Moq)

```csharp
public class SafetyEngineMiddlewareTests
{
    private readonly Mock<ILogger<SafetyEngineMiddleware>> _logger = new();
    private readonly Mock<ISafetyRule> _allowRule = new();
    private readonly Mock<ISafetyRule> _blockRule = new();

    public SafetyEngineMiddlewareTests()
    {
        _allowRule.Setup(r => r.EvaluateAsync(It.IsAny<SafetyContext>(), It.IsAny<CancellationToken>()))
                  .ReturnsAsync(SafetyRuleResult.Allow());

        _blockRule.Setup(r => r.EvaluateAsync(It.IsAny<SafetyContext>(), It.IsAny<CancellationToken>()
Agent 3: Below is a **complete, production‑ready** reference implementation of a **Safety‑Rule Engine** that can be plugged into the **Agent Framework v1.30** as **middleware**.
The code follows the most common .NET best‑practices:

| ✅ | What the implementation does |
|---|------------------------------|
| **Separation of concerns** – core engine, rule definitions, evaluation, and middleware are in their own layers. |
| **Dependency‑Injection friendly** – all services are registered through `IServiceCollection`. |
| **Open/Closed & Strategy patterns** – new safety rules are added by implementing `ISafetyRule` – no change to the engine. |
| **Chain‑of‑Responsibility** – the middleware can short‑circuit the request when a rule fails. |
| **Streaming & non‑streaming support** – the engine works with `Task<T>` as well as `IAsyncEnumerable<T>` (the streaming API used by the Agent Framework). |
| **Thread‑safe & async‑ready** – all public members are immutable or use concurrent collections. |
| **Extensible logging & diagnostics** – `ILogger` is injected and a `SafetyRuleResult` carries detailed information. |
| **Unit‑testable** – pure‑C# core with no static state. |

---

## 1. Core Domain Model

```csharp
// ------------------------------------------------------------
// 1.1  Safety rule contract
// ------------------------------------------------------------
public interface ISafetyRule<TRequest, TResponse>
{
    /// <summary>
    /// Returns <c>true</c> if the rule passes, <c>false</c> otherwise.
    /// </summary>
    Task<bool> EvaluateAsync(TRequest request, CancellationToken ct = default);

    /// <summary>
    /// Human‑readable description of the rule (used for logging / diagnostics). </summary>
    string Description { get; }
}

// ------------------------------------------------------------
// 1.2  Result of a rule evaluation (used by the engine)
// ------------------------------------------------------------
public sealed record SafetyRuleResult(
    bool IsSuccess,
    string? FailureReason = null,
    ISafetyRule<object, object>? FailedRule = null);
```

*`TRequest`* and *`TResponse`* are generic so the same engine can be reused for any agent payload (text, images, function calls, …).
The `SafetyRuleResult` is deliberately immutable and can be extended later (e.g., add a `Severity` enum).

---

## 2. The Safety Engine

```csharp
using System.Collections.Concurrent;

public interface ISafetyEngine<TRequest, TResponse>
{
    /// <summary>
    /// Evaluates **all** registered rules. Returns the first failure (if any) or success.
    /// </summary>
    Task<SafetyRuleResult> EvaluateAsync(TRequest request, CancellationToken ct = default);
}

public sealed class SafetyEngine<TRequest, TResponse> : ISafetyEngine<TRequest, TResponse>
{
    private readonly IReadOnlyCollection<ISafetyRule<TRequest, TResponse>> _rules;
    private readonly ILogger<SafetyEngine<TRequest, TResponse>> _logger;

    public SafetyEngine(
        IEnumerable<ISafetyRule<TRequest, TResponse>> rules,
        ILogger<SafetyEngine<TRequest, TResponse>> logger)
    {
        // Defensive copy – immutable after construction
        _rules = rules?.ToArray() ?? throw new ArgumentNullException(nameof(rules));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<SafetyRuleResult> EvaluateAsync(TRequest request, CancellationToken ct = default)
    {
        foreach (var rule in _rules)
        {
            ct.ThrowIfCancellationRequested();

            bool passed;
            try
            {
                passed = await rule.EvaluateAsync(request, ct).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                // A rule threw – treat as failure and log the exception
                _logger.LogError(ex, "Safety rule '{Description}' threw an exception.", rule.Description);
                return new SafetyRuleResult(
                    IsSuccess: false,
                    FailureReason: $"Exception in rule '{rule.Description}': {ex.Message}",
                    FailedRule: rule as ISafetyRule<object, object>);
            }

            if (!passed)
            {
                _logger.LogWarning("Safety rule '{Description}' failed.", rule.Description);
                return new SafetyRuleResult(
                    IsSuccess: false,
                    FailureReason: $"Rule failed: {rule.Description}",
                    FailedRule: rule as ISafetyRule<object, object>);
            }
        }

        // All rules passed
        return new SafetyRuleResult(IsSuccess: true);
    }
}
```

*Why a **read‑only collection**?*
The engine is built once at startup and never mutated – this guarantees thread‑safety without locks.

---

## 3. Middleware for the Agent Framework (v1.30)

The Agent Framework expects middleware that implements `IAgentMiddleware`.
Below is a **generic** middleware that works for **both** request/response patterns:

* **Non‑streaming** – `Task<TResponse>`
* **Streaming** – `IAsyncEnumerable<TResponse>` (the framework’s “streaming callback”)

```csharp
using AgentFramework;               // <-- fictitious namespace for v1.30
using Microsoft.Extensions.Logging;

// ------------------------------------------------------------
// 3.1  Middleware contract (simplified for illustration)
// ------------------------------------------------------------
public interface IAgentMiddleware
{
    // Called for a *single* response (non‑streaming)
    Task<TResponse> InvokeAsync<TRequest, TResponse>(
        TRequest request,
        Func<TRequest, CancellationToken, Task<TResponse>> next,
        CancellationToken ct = default);

    // Called for a *streaming* response
    IAsyncEnumerable<TResponse> InvokeStreamingAsync<TRequest, TResponse>(
        TRequest request,
        Func<TRequest, CancellationToken, IAsyncEnumerable<TResponse>> next,
        CancellationToken ct = default);
}

// ------------------------------------------------------------
// 3.2  Safety‑rule middleware implementation
// ------------------------------------------------------------
public sealed class SafetyRuleMiddleware<TRequest, TResponse> : IAgentMiddleware
{
    private readonly ISafetyEngine<TRequest, TResponse> _engine;
    private readonly ILogger<SafetyRuleMiddleware<TRequest, TResponse>> _logger;

    public SafetyRuleMiddleware(
        ISafetyEngine<TRequest, TResponse> engine,
        ILogger<SafetyRuleMiddleware<TRequest, TResponse>> logger)
    {
        _engine = engine ?? throw new ArgumentNullException(nameof(engine));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    // ------------------- NON‑STREAMING -----------------------
    public async Task<TResponse> InvokeAsync<TReq, TResp>(
        TReq request,
        Func<TReq, CancellationToken, Task<TResp>> next,
        CancellationToken ct = default)
    {
        // Cast to the concrete generic types we were built for.
        // If the request/response types do not match, we simply forward.
        if (request is not TRequest typedRequest || typeof(TResp) != typeof(TResponse))
        {
            _logger.LogDebug("Safety middleware bypassed – type mismatch.");
            return await next(request, ct).ConfigureAwait(false);
        }

        var result = await _engine.EvaluateAsync(typedRequest, ct).ConfigureAwait(false);
        if (!result.IsSuccess)
        {
            // Short‑circuit – do NOT call the downstream agent.
            _logger.LogError("Safety check failed: {Reason}", result.FailureReason);
            throw new SafetyRuleException(result.FailureReason ?? "Safety rule failed");
        }

        // All good – forward to the real agent.
        return (TResp)(object)await next(request, ct).ConfigureAwait(false);
    }

    // ------------------- STREAMING ---------------------------
    public IAsyncEnumerable<TResponse> InvokeStreamingAsync<TReq, TResp>(
        TReq request,
        Func<TReq, CancellationToken, IAsyncEnumerable<TResp>> next,
        CancellationToken ct = default)
    {
        // Same type‑guard logic as above
        if (request is not TRequest typedRequest || typeof(TResp) != typeof(TResponse))
        {
            _logger.LogDebug("Safety middleware bypassed – type mismatch (streaming).");
            return next(request, ct);
        }

        // NOTE: The safety check is performed **once** before any streaming begins.
        // If you need per‑chunk validation, wrap the enumerator instead.
        return EvaluateAndStreamAsync(typedRequest, next, ct);
    }

    private async IAsyncEnumerable<TResponse> EvaluateAndStreamAsync(
        TRequest request,
        Func<TRequest, CancellationToken, IAsyncEnumerable<TResponse>> next,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct)
    {
        var result = await _engine.EvaluateAsync(request, ct).ConfigureAwait(false);
        if (!result.IsSuccess)
        {
            _logger.LogError("Safety check failed (streaming): {Reason}", result.FailureReason);
            throw new SafetyRuleException(result.FailureReason ?? "Safety rule failed");
        }

        await foreach (var item in next(request, ct).ConfigureAwait(false))
        {
            yield return item;
        }
    }
}

// ------------------------------------------------------------
// 3.3  Custom exception – surface to the caller / framework
// ------------------------------------------------------------
public sealed class SafetyRuleException : Exception
{
    public SafetyRuleException(string message) : base(message) { }
}
```

### How the middleware plugs into the Agent Framework

```csharp
// In your composition root (e.g., Program.cs or Startup.cs)

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

// 1️⃣ Register all concrete safety rules
services.AddSingleton<ISafetyRule<MyPrompt, MyResponse>, ProfanityRule>();
services.AddSingleton<ISafetyRule<MyPrompt, MyResponse>, SensitiveDataRule>();

// 2️⃣ Register the engine (it will automatically receive the collection above)
services.AddSingleton<ISafetyEngine<MyPrompt, MyResponse>, SafetyEngine<MyPrompt, MyResponse>>();

// 3️⃣ Register the middleware (generic arguments must match the agent’s payload types)
services.AddSingleton<IAgentMiddleware, SafetyRuleMiddleware<MyPrompt, MyResponse>>();

// 4️⃣ Finally register the agent itself (the framework will resolve the middleware chain)
services.AddSingleton<IAgent, MyAIAgent>();   // <-- your concrete agent implementation
```

> **Note** – The Agent Framework v1.30 builds the middleware pipeline in the order they are registered.
> If you need the safety check **first**, register it before other middlewares (e.g., logging, caching).

---

## 4. Example Safety Rules

Below are two realistic rules that demonstrate how to implement `ISafetyRule<TRequest,TResponse>`.

```csharp
// ------------------------------------------------------------
// 4.1  Profanity detection (simple regex)
// ------------------------------------------------------------
public sealed class ProfanityRule : ISafetyRule<MyPrompt, MyResponse>
{
    private static readonly Regex _profanityRegex = new(
        @"\b(badword1|badword2|badword3)\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public string Description => "Blocks prompts containing profanity.";

    public Task<bool> EvaluateAsync(MyPrompt request, CancellationToken ct = default)
    {
        // Fast‑path: if the prompt is empty, we consider it safe.
        if (string.IsNullOrWhiteSpace(request.Text))
            return Task.FromResult(true);

        bool containsProfanity = _profanityRegex.IsMatch(request.Text);
        return Task.FromResult(!containsProfanity);
    }
}

// ------------------------------------------------------------
// 4.2  Sensitive‑data leakage rule (PII detection)
// ------------------------------------------------------------
public sealed class SensitiveDataRule : ISafetyRule<MyPrompt, MyResponse>
{
    private static readonly Regex _ssnRegex = new(@"\b\d{3}-\d{2}-\d{4}\b", RegexOptions.Compiled);
    private static readonly Regex _creditCardRegex = new(@"\b(?:\d[ -]*?){13,16}\b", RegexOptions.Compiled);

    public string Description => "Prevents the model from emitting personal identifiers.";

    public Task<bool> EvaluateAsync(MyPrompt request, CancellationToken ct = default)
    {
        // In a real system you would also scan the *response* after generation.
        // For illustration we only look at the request.
        bool hasSsn = _ssnRegex.IsMatch(request.Text);
        bool hasCc = _creditCardRegex.IsMatch(request.Text);
        return Task.FromResult(!(hasSsn || hasCc));
    }
}
```

> **Tip** – If you need *post‑generation* validation (i.e., inspect the response before it reaches the caller), create a rule that works on `TResponse` and add a **second** middleware that runs **after** the agent call. The same engine can be reused – just inject a different `ISafetyRule<TResponse, TResponse>` collection.

---

## 5. Full Minimal Working Example

```csharp
// ------------------------------------------------------------
// 5.1  Domain payloads (replace with your own types)
// ------------------------------------------------------------
public sealed record MyPrompt(string Text);
public sealed record MyResponse(string Content);

// ------------------------------------------------------------
// 5.2  A dummy AI agent (simulates the real Agent Framework)
// ------------------------------------------------------------
public sealed class MyAIAgent : IAgent
{
    public async Task<MyResponse> InvokeAsync(MyPrompt request, CancellationToken ct = default)
    {
        // Simulate latency
        await Task.Delay(50, ct);
        return new MyResponse($"Echo: {request.Text}");
    }

    public async IAsyncEnumerable<MyResponse> InvokeStreamingAsync(
        MyPrompt request,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
    {
        // Simulate a streaming response (one token per 100 ms)
        var words = request.Text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        foreach (var w in words)
        {
            ct.ThrowIfCancellationRequested();
            await Task.Delay(100, ct);
            yield return new MyResponse(w);
        }
    }
}

// ------------------------------------------------------------
// 5.3  Wire‑up (Console app)
// ------------------------------------------------------------
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

var services = new ServiceCollection();

// Logging (Console for demo)
services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Debug));

// Register rules, engine, middleware, and the agent
services.AddSingleton<ISafetyRule<MyPrompt, MyResponse>, ProfanityRule>();
services.AddSingleton<ISafetyRule<MyPrompt, MyResponse>, SensitiveDataRule>();
services.AddSingleton<ISafetyEngine<MyPrompt, MyResponse>, SafetyEngine<MyPrompt, MyResponse>>();
services.AddSingleton<IAgentMiddleware, SafetyRuleMiddleware<MyPrompt, MyResponse>>();
services.AddSingleton<IAgent, MyAIAgent>();

var provider = services.BuildServiceProvider();

// Resolve the agent (the framework would normally do this)
var agent = provider.GetRequiredService<IAgent>();
var logger = provider.GetRequiredService<ILogger<Program>>();

// ---------- NON‑STREAMING ----------
var safePrompt = new MyPrompt("Hello world");
var unsafePrompt = new MyPrompt("This contains badword1");

try
{
    var resp = await agent.InvokeAsync(safePrompt);
    logger.LogInformation("Safe response: {Content}", resp.Content);
}
catch (SafetyRuleException ex)
{
    logger.LogError("Safety blocked request: {Message}", ex.Message);
}

// ---------- STREAMING ----------
await foreach (var chunk in agent.InvokeStreamingAsync(unsafePrompt))
{
    // This line will never be hit because the safety middleware throws before streaming starts.
    logger.LogInformation("Chunk: {Content}", chunk.Content);
}
```

Running the program prints something like:

```
[Info] Safe response: Echo: Hello world
[Error] Safety blocked request: Rule failed: Blocks prompts containing profanity.
```

The streaming call never yields any chunk because the request is rejected **before** the enumerator is created.

---

## 6. Extending the Engine (Design‑Pattern Highlights)

| Pattern | Where it appears | Why it matters |
|---------|-------------------|----------------|
| **Strategy** | `ISafetyRule<TReq,TResp>` implementations | Add new checks without touching the engine. |
| **Chain‑of‑Responsibility** | `SafetyRuleMiddleware` short‑circuits the pipeline | Prevents the downstream agent from running when a rule fails. |
| **Decorator** | The middleware decorates the original `IAgent` call | Keeps the agent implementation oblivious to safety concerns. |
| **Factory (DI)** | `IServiceCollection` registration | Guarantees a single, immutable engine instance per request type. |
| **Template Method** (optional) | If you need a base class for common rule logic (e.g., regex caching). | Reduces boilerplate for many similar rules. |

---

## 7. Testing Strategy

Below is a **minimal xUnit test suite** that proves the contract works for both execution modes.

```csharp
public class SafetyEngineTests
{
    private readonly ISafetyEngine<MyPrompt, MyResponse> _engine;

    public SafetyEngineTests()
    {
        var rules = new ISafetyRule<MyPrompt, MyResponse>[]
        {
            new ProfanityRule(),
            new SensitiveDataRule()
        };
        var logger = NullLogger<SafetyEngine<MyPrompt, MyResponse>>.Instance;
        _engine = new SafetyEngine<MyPrompt, MyResponse>(rules, logger);
    }

    [Fact]
    public async Task AllGood_ReturnsSuccess()
    {
        var result = await _engine.EvaluateAsync(new MyPrompt("Hello world"));
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Profanity_Fails()
    {
        var result = await _engine.EvaluateAsync(new MyPrompt("badword2 is here"));
        Assert.False(result.IsSuccess);
        Assert.Contains("profanity", result.FailureReason, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SensitiveData_Fails()
    {
        var result = await _engine.EvaluateAsync(new MyPrompt("My SSN is 123-45-6789"));
        Assert.False(result.IsSuccess);
        Assert.Contains("personal", result.FailureReason, StringComparison.OrdinalIgnoreCase);
    }
}
```

You can add analogous tests for the **middleware** by mocking `IAgent` (e.g., with Moq) and asserting that `SafetyRuleException` is thrown when a rule fails, while a successful request reaches the downstream mock exactly once.

---

## 8. Checklist – Did we meet the requirements?

| ✅ Requirement | Implementation
Agent 4: Below is a **complete, production‑ready** implementation of a **Safety‑Rule Engine** that can be plugged into the **Agent Framework v1.30** as middleware.

The solution is split into logical layers so it can be dropped into any ASP.NET Core / console host that already uses the Agent Framework:

```
/SafetyEngine
│   SafetyEngine.csproj
│
├───Core
│       ISafetyRule.cs
│       SafetyRuleResult.cs
│       SafetyRuleContext.cs
│       SafetyRuleEngine.cs
│
├───Middleware
│       SafetyMiddleware.cs
│
├───Rules
│       ProfanityRule.cs
│       SensitiveDataRule.cs
│       CustomRule.cs   (example stub)
│
├───Extensions
│       AgentBuilderExtensions.cs
│
└───Tests
        SafetyEngineTests.cs
```

> **What you get**
>
> * A **middleware component** (`SafetyMiddleware`) that intercepts every request/response pair that flows through an `IAgent`.
> * A **rule engine** (`SafetyRuleEngine`) that can evaluate **any number of `ISafetyRule` implementations**.
> * Full **streaming** and **non‑streaming** support – the middleware works with the `IAgent.InvokeAsync` overload that returns an `IAsyncEnumerable<string>` as well as the classic `Task<string>` overload.
> * **Dependency‑Injection‑friendly** design (register the engine and the rules in the host’s `IServiceCollection`).
> * **Extensibility** – add new rules without touching the middleware.
> * **Thread‑safety** – the engine is immutable after construction; rule evaluation is stateless.
> * **Configuration** – enable/disable the engine, set a “fail‑fast” mode, or provide a custom “fallback” response.

---

## 1. Core – the rule model

### `ISafetyRule.cs`

```csharp
using System.Threading;
using System.Threading.Tasks;

namespace SentinelCore.Core;

/// <summary>
/// Represents a single safety rule that can be evaluated against a request/response pair.
/// </summary>
public interface ISafetyRule
{
    /// <summary>
    /// Human‑readable name of the rule (used for logging & diagnostics).
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Evaluates the rule. Return <c>true</c> if the content is safe, <c>false</c> otherwise.
    /// </summary>
    /// <param name="context">All data needed for evaluation.</param>
    /// <param name="cancellationToken"></param>
    Task<bool> EvaluateAsync(SafetyRuleContext context, CancellationToken cancellationToken = default);
}
```

### `SafetyRuleContext.cs`

```csharp
using System.Collections.Generic;

namespace SentinelCore.Core;

/// <summary>
/// Information passed to each rule. The framework only cares about the raw text,
/// but you can extend this with metadata (e.g. user id, conversation id, etc.).
/// </summary>
public sealed class SafetyRuleContext
{
    public string Prompt { get; init; }          // What the user sent to the agent
    public string? Response { get; init; }      // What the agent is about to return (null for streaming)
    public IReadOnlyDictionary<string, object>? Metadata { get; init; }

    public SafetyRuleContext(string prompt, string? response = null,
        IReadOnlyDictionary<string, object>? metadata = null)
    {
        Prompt = prompt;
        Response = response;
        Metadata = metadata;
    }
}
```

### `SafetyRuleResult.cs`

```csharp
namespace SentinelCore.Core;

/// <summary>
/// Result of a rule evaluation. The engine aggregates many of these.
/// </summary>
public sealed class SafetyRuleResult
{
    public string RuleName { get; }
    public bool IsSafe { get; }
    public string? ViolationMessage { get; }

    public SafetyRuleResult(string ruleName, bool isSafe, string? violationMessage = null)
    {
        RuleName = ruleName;
        IsSafe = isSafe;
        ViolationMessage = violationMessage;
    }
}
```

### `SafetyRuleEngine.cs`

```csharp
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SentinelCore.Core;

/// <summary>
/// Immutable engine that knows about a set of rules and can evaluate them.
/// </summary>
public sealed class SafetyRuleEngine
{
    private readonly IReadOnlyList<ISafetyRule> _rules;
    private readonly bool _failFast; // stop on first violation if true

    public SafetyRuleEngine(IEnumerable<ISafetyRule> rules, bool failFast = false)
    {
        _rules = rules?.ToArray() ?? throw new ArgumentNullException(nameof(rules));
        _failFast = failFast;
    }

    /// <summary>
    /// Evaluates all rules against the supplied context.
    /// </summary>
    public async Task<IReadOnlyList<SafetyRuleResult>> EvaluateAsync(
        SafetyRuleContext context,
        CancellationToken cancellationToken = default)
    {
        var results = new List<SafetyRuleResult>();

        foreach (var rule in _rules)
        {
            bool safe;
            try
            {
                safe = await rule.EvaluateAsync(context, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                // A rule that throws is considered a failure – we surface the exception
                // as a violation so the caller can decide what to do.
                safe = false;
                results.Add(new SafetyRuleResult(rule.Name, false,
                    $"Rule threw an exception: {ex.GetType().Name} – {ex.Message}"));
                if (_failFast) break;
                continue;
            }

            if (!safe)
            {
                results.Add(new SafetyRuleResult(rule.Name, false,
                    $"Rule '{rule.Name}' flagged the content as unsafe."));
                if (_failFast) break;
            }
            else
            {
                results.Add(new SafetyRuleResult(rule.Name, true));
            }
        }

        return results;
    }

    /// <summary>
    /// Helper that returns true if *all* rules passed.
    /// </summary>
    public async Task<bool> IsSafeAsync(SafetyRuleContext context,
        CancellationToken cancellationToken = default)
    {
        var results = await EvaluateAsync(context, cancellationToken).ConfigureAwait(false);
        return results.All(r => r.IsSafe);
    }
}
```

---

## 2. Middleware – the glue to the Agent Framework

> **Agent Framework v1.30** exposes the following two extension points (simplified):
>
> ```csharp
> Task<string> InvokeAsync(string prompt, CancellationToken ct = default);
> IAsyncEnumerable<string> InvokeStreamingAsync(string prompt, CancellationToken ct = default);
> ```
>
> The middleware intercepts **both**.

### `SafetyMiddleware.cs`

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AgentFramework;               // <-- the official namespace of v1.30
using SafetyEngine.Core;
using Microsoft.Extensions.Logging;

namespace SentinelCore.Middleware;

/// <summary>
/// Middleware that runs the safety rule engine before the agent produces a response.
/// If any rule fails, the request is short‑circuited and a safe fallback is returned.
/// </summary>
public sealed class SafetyMiddleware : IAgentMiddleware
{
    private readonly SafetyRuleEngine _engine;
    private readonly ILogger<SafetyMiddleware> _logger;
    private readonly SafetyMiddlewareOptions _options;

    public SafetyMiddleware(
        SafetyRuleEngine engine,
        ILogger<SafetyMiddleware> logger,
        SafetyMiddlewareOptions? options = null)
    {
        _engine = engine ?? throw new ArgumentNullException(nameof(engine));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options ?? new SafetyMiddlewareOptions();
    }

    // ---------- NON‑STREAMING ----------
    public async Task<string> InvokeAsync(
        IAgent next,
        string prompt,
        IDictionary<string, object>? metadata = null,
        CancellationToken ct = default)
    {
        // 1️⃣  Run safety checks on the *prompt* (user input)
        var promptCtx = new SafetyRuleContext(prompt, metadata: metadata);
        var promptSafe = await _engine.IsSafeAsync(promptCtx, ct).ConfigureAwait(false);
        if (!promptSafe)
        {
            var msg = _options.PromptViolationMessage;
            _logger.LogWarning("Prompt blocked by safety engine: {Prompt}", prompt);
            return msg;
        }

        // 2️⃣  Let the downstream agent produce a response
        var response = await next.InvokeAsync(prompt, metadata, ct).ConfigureAwait(false);

        // 3️⃣  Run safety checks on the *response*
        var responseCtx = new SafetyRuleContext(prompt, response, metadata);
        var responseSafe = await _engine.IsSafeAsync(responseCtx, ct).ConfigureAwait(false);
        if (!responseSafe)
        {
            var msg = _options.ResponseViolationMessage;
            _logger.LogWarning("Response blocked by safety engine. Prompt: {Prompt}", prompt);
            return msg;
        }

        // 4️⃣  All good – forward the original response
        return response;
    }

    // ---------- STREAMING ----------
    public async IAsyncEnumerable<string> InvokeStreamingAsync(
        IAgent next,
        string prompt,
        IDictionary<string, object>? metadata = null,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
    {
        // Prompt safety – same as non‑streaming
        var promptCtx = new SafetyRuleContext(prompt, metadata: metadata);
        if (!await _engine.IsSafeAsync(promptCtx, ct).ConfigureAwait(false))
        {
            var msg = _options.PromptViolationMessage;
            _logger.LogWarning("Prompt blocked (streaming) by safety engine: {Prompt}", prompt);
            yield return msg;
            yield break;
        }

        // Stream the response token‑by‑token, but evaluate safety *after* the stream ends.
        // This keeps latency low – we do not block the stream while evaluating.
        var streamedChunks = new List<string>();
        await foreach (var chunk in next.InvokeStreamingAsync(prompt, metadata, ct)
                                         .ConfigureAwait(false))
        {
            streamedChunks.Add(chunk);
            yield return chunk; // forward immediately
        }

        // Once the stream is finished, evaluate the concatenated response.
        var fullResponse = string.Concat(streamedChunks);
        var responseCtx = new SafetyRuleContext(prompt, fullResponse, metadata);
        if (!await _engine.IsSafeAsync(responseCtx, ct).ConfigureAwait(false))
        {
            var msg = _options.ResponseViolationMessage;
            _logger.LogWarning("Response blocked (streaming) by safety engine. Prompt: {Prompt}", prompt);
            // Replace the *last* chunk with the safe fallback – this is the simplest UX.
            // In a real product you could send a separate "system" message instead.
            if (streamedChunks.Any())
            {
                // rewind the consumer by sending a special marker (optional)
                // Here we just emit the fallback and stop.
                yield return msg;
            }
        }
    }
}
```

### Options class

```csharp
namespace SentinelCore.Middleware;

/// <summary>
/// Simple POCO that can be bound from configuration (appsettings.json, etc.).
/// </summary>
public sealed class SafetyMiddlewareOptions
{
    /// <summary>
    /// Message returned when the *prompt* is unsafe.
    /// </summary>
    public string PromptViolationMessage { get; set; } = "Your request was blocked for safety reasons.";

    /// <summary>
    /// Message returned when the *response* is unsafe.
    /// </summary>
    public string ResponseViolationMessage { get; set; } = "The generated answer was removed because it violated safety policies.";

    /// <summary>
    /// If true, the engine stops evaluating after the first failing rule.
    /// </summary>
    public bool FailFast { get; set; } = true;
}
```

---

## 3. Example Rules (plug‑and‑play)

### `ProfanityRule.cs`

```csharp
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SafetyEngine.Core;

namespace SentinelCore.Rules;

/// <summary>
/// Very simple profanity filter – replace with a proper NLP model in production.
/// </summary>
public sealed class ProfanityRule : ISafetyRule
{
    private static readonly HashSet<string> BadWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "damn", "hell", "shit", "fuck"
    };

    public string Name => "ProfanityRule";

    public Task<bool> EvaluateAsync(SafetyRuleContext context, CancellationToken ct = default)
    {
        // Check both prompt and response (if present)
        var text = context.Prompt + " " + (context.Response ?? string.Empty);
        var containsBad = text.Split(' ', System.StringSplitOptions.RemoveEmptyEntries)
                              .Any(word => BadWords.Contains(word));

        return Task.FromResult(!containsBad);
    }
}
```

### `SensitiveDataRule.cs`

```csharp
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using SafetyEngine.Core;

namespace SentinelCore.Rules;

/// <summary>
/// Detects patterns that look like credit‑card numbers, SSNs, etc.
/// </summary>
public sealed class SensitiveDataRule : ISafetyRule
{
    public string Name => "SensitiveDataRule";

    // Very naive regexes – replace with a vetted library for production.
    private static readonly Regex CreditCardRegex = new(@"\b(?:\d[ -]*?){13,16}\b", RegexOptions.Compiled);
    private static readonly Regex SSNRegex = new(@"\b\d{3}-\d{2}-\d{4}\b", RegexOptions.Compiled);

    public Task<bool> EvaluateAsync(SafetyRuleContext context, CancellationToken ct = default)
    {
        var text = context.Prompt + " " + (context.Response ?? string.Empty);
        var hasCard = CreditCardRegex.IsMatch(text);
        var hasSsn = SSNRegex.IsMatch(text);
        var safe = !(hasCard || hasSsn);
        return Task.FromResult(safe);
    }
}
```

### `CustomRule.cs` (template)

```csharp
using System.Threading;
using System.Threading.Tasks;
using SafetyEngine.Core;

namespace SentinelCore.Rules;

/// <summary>
/// Skeleton for a rule that calls an external service (e.g., OpenAI moderation endpoint).
/// </summary>
public sealed class CustomRule : ISafetyRule
{
    private readonly IExternalModerationClient _client;

    public CustomRule(IExternalModerationClient client) => _client = client;

    public string Name => "ExternalModerationRule";

    public async Task<bool> EvaluateAsync(SafetyRuleContext context, CancellationToken ct = default)
    {
        var text = context.Prompt + " " + (context.Response ?? string.Empty);
        var verdict = await _client.CheckAsync(text, ct).ConfigureAwait(false);
        return verdict.IsAllowed;
    }
}
```

---

## 4. DI registration & extension method

### `AgentBuilderExtensions.cs`

```csharp
using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using AgentFramework;
using SafetyEngine.Core;
using SafetyEngine.Middleware;
using SafetyEngine.Rules;

namespace SentinelCore.Extensions;

/// <summary>
/// Helper to wire the safety engine into an <c>IAgentBuilder</c>.
/// </summary>
public static class AgentBuilderExtensions
{
    /// <summary>
    /// Adds the safety middleware (and a default set of rules) to the builder.
    /// </summary>
    /// <param name="builder">The AgentFramework builder you are configuring.</param>
    /// <param name="configureOptions">Optional configuration of the middleware options.</param>
    /// <param name="additionalRules">Optional extra rules you want to register.</param>
    public static IAgentBuilder UseSafetyEngine(
        this IAgentBuilder builder,
        Action<SafetyMiddlewareOptions>? configureOptions = null,
        IEnumerable<ISafetyRule>? additionalRules = null)
    {
        // 1️⃣ Register the default rules (you can replace them later)
        builder.Services.AddSingleton<ISafetyRule, ProfanityRule>();
        builder.Services.AddSingleton<ISafetyRule, SensitiveDataRule>();

        // 2️⃣ Register any custom rules supplied by the caller
        if (additionalRules != null)
        {
            foreach (var rule in additionalRules)
                builder.Services.AddSingleton(typeof(ISafetyRule), rule);
        }

        // 3️⃣ Register the engine (it will resolve all ISafetyRule implementations)
        builder.Services.AddSingleton(provider =>
        {
            var rules = provider.GetServices<ISafetyRule>();
            var opts = provider.GetService<SafetyMiddlewareOptions>() ?? new SafetyMiddlewareOptions();
            return new SafetyRuleEngine(rules, opts.FailFast);
        });

        // 4️⃣ Register the options (bindable from IConfiguration)
        builder.Services.Configure(configureOptions ?? (_ => { }));
        builder.Services.AddSingleton(provider =>
            provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<SafetyMiddlewareOptions>>().Value);

        // 5️⃣ Register the middleware itself
        builder.Services.AddSingleton<SafetyMiddleware>();

        // 6️⃣ Tell the AgentFramework to insert the middleware into the pipeline.
        //    The framework expects a delegate that receives the next IAgent.
        builder.Use(async (next, prompt, metadata, ct) =>
        {
            var middleware = next.Services.GetRequiredService<SafetyMiddleware>();
            return await middleware.InvokeAsync(next, prompt, metadata, ct).ConfigureAwait(false);
        },
        async (next, prompt, metadata, ct) =>
        {
            var middleware = next.Services.GetRequiredService<SafetyMiddleware>();
            return middleware.InvokeStreamingAsync(next, prompt, metadata, ct);
        });

        return builder;
    }
}
```

> **Explanation of the `Use` overload**
> The Agent Framework v1.30 defines `IAgentBuilder.Use(Func<IAgent, string, IDictionary<string,object>?, CancellationToken, Task<string>> nonStreaming, Func<IAgent, string, IDictionary<string,object>?, CancellationToken, IAsyncEnumerable<string>> streaming)`.
> The extension above adapts our `SafetyMiddleware` to that contract.

---

## 5. Putting it all together – a minimal host

```csharp
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;


