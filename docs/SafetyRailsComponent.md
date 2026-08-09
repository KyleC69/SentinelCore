---
title: "Safety Rails Component"
status: Active
component: SafetyRails
last_updated: 2026-07-19
version: v1.0
---

# SentinelCore Safety Rails Component

**Project:** `SentinelCore.Contracts` (abstractions) + `SentinelCore.Orchestrations` (implementation)
**Namespaces:** `SentinelCore.Contracts.SafetyEngine`, `SentinelCore.Orchestrations.Agents.Middleware`
**Dependencies:** `Microsoft.Extensions.AI`, `Microsoft.Agents.AI`
**Consumers:** `SentinelCore.Orchestrations` (AgentBuilder for Core agent), `SentinelCoreHost` (via DI)

---

## Purpose

The Safety Rails component provides **defense-in-depth safety controls** for AI agents in SentinelCore. It implements a middleware-based safety pipeline that intercepts agent interactions, applies configurable safety rules, and can block, modify, or flag unsafe content before it reaches the model or the user.

**Key Principles:**
- **Middleware-based** — integrates into the `ChatClient` pipeline via `IChatClient` decorator
- **Rule-based** — composable `ISafetyRule` implementations for different safety concerns
- **Role-aware** — applied selectively (currently only to `AgentRole.Core`)
- **Fail-safe** — defaults to `NullSafetyMiddleware` (no-op) if not configured
- **Observable** — emits safety events via `ISentinelCoreEvents`

---

## Architecture Position

```
┌─────────────────────────────────────────────────────────────────┐
│                    SentinelCore.Orchestrations                   │
│  ┌───────────────────────────────────────────────────────────┐  │
│  │                  AgentBuilder Pipeline                     │  │
│  │  OllamaApiClient                                           │  │
│  │       │                                                    │  │
│  │       ▼                                                    │  │
│  │  LoggingChatClient                                         │  │
│  │       │                                                    │  │
│  │       ▼                                                    │  │
│  │  EventPublishingChatClient                                 │  │
│  │       │                                                    │  │
│  │       ▼                                                    │  │
│  │  ┌─────────────────────────────────────────────────────┐  │  │
│  │  │         SafetyMiddleware (if Core agent)            │  │  │
│  │  │  ┌─────────────────────────────────────────────┐   │  │  │
│  │  │  │ ISafetyMiddleware                            │   │  │  │
│  │  │  │  - GetResponseAsync(messages, options)      │   │  │  │
│  │  │  │  - GetStreamingResponseAsync(...)           │   │  │  │
│  │  │  └─────────────────────────────────────────────┘   │  │  │
│  │  │         │                    │                      │  │  │
│  │  │         ▼                    ▼                      │  │  │
│  │  │  ┌─────────────┐      ┌─────────────┐              │  │  │
│  │  │  │ SafetyRule1 │      │ SafetyRule2 │  ...         │  │  │
│  │  │  │ (e.g., PII) │      │ (e.g., Harm)│              │  │  │
│  │  │  └─────────────┘      └─────────────┘              │  │  │
│  │  └─────────────────────────────────────────────────────┘  │  │
│  │       │                                                    │  │
│  │       ▼                                                    │  │
│  │  ChatClientAgent                                           │  │
│  └───────────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────────┘
                              ▲
                              │ implements
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│                    SentinelCore.Contracts                        │
│  ┌───────────────────────────────────────────────────────────┐  │
│  │  SafetyEngine Abstractions                                 │  │
│  │  ┌──────────────────┐  ┌──────────────┐  ┌──────────────┐  │  │
│  │  │ ISafetyMiddleware│  │ ISafetyRule  │  │ SafetyContext│  │  │
│  │  │                  │  │              │  │              │  │  │
│  │  │ GetResponseAsync │  │ EvaluateAsync│  │ Messages     │  │  │
│  │  │ GetStreaming...  │  │              │  │ Options      │  │  │
│  │  └──────────────────┘  └──────────────┘  │ AgentRole    │  │  │
│  │         ▲                    ▲           │ Metadata     │  │  │
│  │         │                    │           └──────────────┘  │  │
│  │         │                    │                    ▲        │  │
│  │         │                    │                    │        │  │
│  │  ┌──────┴──────┐    ┌────────┴────────┐  ┌───────┴───────┐  │  │
│  │  │SafetyResult │    │ SafetyVerdict   │  │ NullSafety    │  │  │
│  │  │             │    │ (Allow/Block/   │  │ Middleware    │  │  │
│  │  │ Verdict     │    │  Modify/Flag)   │  │ (default)     │  │  │
│  │  │ Reason      │    └─────────────────┘  └───────────────┘  │  │
│  │  │ Metadata    │                                          │  │
│  │  └─────────────┘                                          │  │
│  └───────────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────────┘
```

---

## 1. Abstractions (Contracts)

### ISafetyMiddleware

**File:** `SentinelCore.Contracts/SafetyEngine/ISafetyMiddleware.cs`

```csharp
public interface ISafetyMiddleware : IChatClient
{
    /// <summary>
    /// Gets the inner chat client this middleware wraps.
    /// </summary>
    IChatClient InnerClient { get; }

    /// <summary>
    /// The safety rules to apply in order.
    /// </summary>
    IReadOnlyList<ISafetyRule> Rules { get; }
}
```

**Purpose:** Decorates an `IChatClient` to intercept requests/responses and apply safety rules. Implements `IChatClient` so it can be chained in the pipeline.

### ISafetyRule

**File:** `SentinelCore.Contracts/SafetyEngine/ISafetyRule.cs`

```csharp
public interface ISafetyRule
{
    /// <summary>
    /// Unique identifier for this rule.
    /// </summary>
    string RuleId { get; }

    /// <summary>
    /// Human-readable name.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Evaluates the safety context and returns a verdict.
    /// </summary>
    /// <param name="context">The safety evaluation context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The safety result with verdict and optional modifications.</returns>
    Task<SafetyResult> EvaluateAsync(SafetyContext context, CancellationToken cancellationToken = default);
}
```

### SafetyContext

**File:** `SentinelCore.Contracts/SafetyEngine/SafetyContext.cs`

```csharp
public sealed record SafetyContext
{
    /// <summary>
    /// The chat messages being evaluated.
    /// </summary>
    public required IReadOnlyList<ChatMessage> Messages { get; init; }

    /// <summary>
    /// The chat options for this request.
    /// </summary>
    public required ChatOptions? Options { get; init; }

    /// <summary>
    /// The role of the agent being evaluated.
    /// </summary>
    public required AgentRole AgentRole { get; init; }

    /// <summary>
    /// Additional metadata for rule evaluation.
    /// </summary>
    public IReadOnlyDictionary<string, object?> Metadata { get; init; }
        = new Dictionary<string, object?>();
}
```

### SafetyResult

**File:** `SentinelCore.Contracts/SafetyEngine/SafetyResult.cs`

```csharp
public sealed record SafetyResult
{
    /// <summary>
    /// The safety verdict.
    /// </summary>
    public required SafetyVerdict Verdict { get; init; }

    /// <summary>
    /// Human-readable reason for the verdict.
    /// </summary>
    public string Reason { get; init; } = string.Empty;

    /// <summary>
    /// The rule that produced this result.
    /// </summary>
    public string RuleId { get; init; } = string.Empty;

    /// <summary>
    /// Modified messages if verdict is Modified.
    /// </summary>
    public IReadOnlyList<ChatMessage>? ModifiedMessages { get; init; }

    /// <summary>
    /// Additional metadata about the evaluation.
    /// </summary>
    public IReadOnlyDictionary<string, object?> Metadata { get; init; }
        = new Dictionary<string, object?>();

    /// <summary>
    /// Creates an Allow result.
    /// </summary>
    public static SafetyResult Allow(string ruleId, string reason = "Allowed")
        => new() { Verdict = SafetyVerdict.Allow, RuleId = ruleId, Reason = reason };

    /// <summary>
    /// Creates a Block result.
    /// </summary>
    public static SafetyResult Block(string ruleId, string reason)
        => new() { Verdict = SafetyVerdict.Block, RuleId = ruleId, Reason = reason };

    /// <summary>
    /// Creates a Modify result with sanitized messages.
    /// </summary>
    public static SafetyResult Modify(string ruleId, string reason, IReadOnlyList<ChatMessage> messages)
        => new() { Verdict = SafetyVerdict.Modify, RuleId = ruleId, Reason = reason, ModifiedMessages = messages };

    /// <summary>
    /// Creates a Flag result (allow but record).
    /// </summary>
    public static SafetyResult Flag(string ruleId, string reason, IReadOnlyDictionary<string, object?>? metadata = null)
        => new() { Verdict = SafetyVerdict.Flag, RuleId = ruleId, Reason = reason, Metadata = metadata ?? new Dictionary<string, object?>() };
}
```

### SafetyVerdict

**File:** `SentinelCore.Contracts/SafetyEngine/SafetyVerdict.cs`

```csharp
public enum SafetyVerdict
{
    /// <summary>Content is safe, proceed normally.</summary>
    Allow = 0,

    /// <summary>Content violates policy, block the request.</summary>
    Block = 1,

    /// <summary>Content needs modification, replace with sanitized version.</summary>
    Modify = 2,

    /// <summary>Content is allowed but flagged for review/monitoring.</summary>
    Flag = 3
}
```

---

## 2. Implementation (Orchestrations)

### NullSafetyMiddleware (Default)

**File:** `SentinelCore.Orchestrations/Agents/Middleware/NullSafetyMiddleware.cs`

```csharp
public sealed class NullSafetyMiddleware : ISafetyMiddleware
{
    public IChatClient InnerClient { get; }
    public IReadOnlyList<ISafetyRule> Rules => [];

    public NullSafetyMiddleware(IChatClient innerClient)
    {
        InnerClient = innerClient ?? throw new ArgumentNullException(nameof(innerClient));
    }

    // Pass-through implementations for all IChatClient methods
    public Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
        => InnerClient.GetResponseAsync(messages, options, cancellationToken);

    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (var update in InnerClient.GetStreamingResponseAsync(messages, options, cancellationToken))
            yield return update;
    }

    // ... other IChatClient methods delegate to InnerClient
}
```

**Purpose:** No-op safety middleware used when no safety rules are configured. Registered as default in DI.

### SafetyMiddleware (Active)

**File:** `SentinelCore.Orchestrations/Agents/Middleware/SafetyMiddleware.cs`

```csharp
public sealed class SafetyMiddleware : ISafetyMiddleware
{
    private readonly IChatClient _innerClient;
    private readonly IReadOnlyList<ISafetyRule> _rules;
    private readonly ILogger<SafetyMiddleware> _logger;
    private readonly ISentinelCoreEvents _events;

    public IChatClient InnerClient => _innerClient;
    public IReadOnlyList<ISafetyRule> Rules => _rules;

    public SafetyMiddleware(
        IChatClient innerClient,
        IEnumerable<ISafetyRule> rules,
        ILogger<SafetyMiddleware> logger,
        ISentinelCoreEvents events)
    {
        _innerClient = innerClient ?? throw new ArgumentNullException(nameof(innerClient));
        _rules = rules?.ToList().AsReadOnly() ?? throw new ArgumentNullException(nameof(rules));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _events = events ?? throw new ArgumentNullException(nameof(events));
    }

    public async Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var messageList = messages.ToList();
        var context = new SafetyContext
        {
            Messages = messageList,
            Options = options,
            AgentRole = options?.GetAgentRole() ?? AgentRole.General
        };

        // Evaluate all rules in order
        var result = await EvaluateRulesAsync(context, cancellationToken);

        switch (result.Verdict)
        {
            case SafetyVerdict.Allow:
                return await _innerClient.GetResponseAsync(messageList, options, cancellationToken);

            case SafetyVerdict.Block:
                await _events.RaiseSafetyViolationAsync(result.RuleId, result.Reason, context.AgentRole, cancellationToken);
                throw new SafetyViolationException(result.RuleId, result.Reason);

            case SafetyVerdict.Modify:
                if (result.ModifiedMessages != null)
                {
                    return await _innerClient.GetResponseAsync(result.ModifiedMessages, options, cancellationToken);
                }
                goto case SafetyVerdict.Block;

            case SafetyVerdict.Flag:
                await _events.RaiseSafetyFlagAsync(result.RuleId, result.Reason, context.AgentRole, result.Metadata, cancellationToken);
                return await _innerClient.GetResponseAsync(messageList, options, cancellationToken);

            default:
                throw new InvalidOperationException($"Unknown safety verdict: {result.Verdict}");
        }
    }

    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // For streaming, evaluate once before streaming
        var messageList = messages.ToList();
        var context = new SafetyContext
        {
            Messages = messageList,
            Options = options,
            AgentRole = options?.GetAgentRole() ?? AgentRole.General
        };

        var result = await EvaluateRulesAsync(context, cancellationToken);

        if (result.Verdict == SafetyVerdict.Block)
        {
            await _events.RaiseSafetyViolationAsync(result.RuleId, result.Reason, context.AgentRole, cancellationToken);
            throw new SafetyViolationException(result.RuleId, result.Reason);
        }

        if (result.Verdict == SafetyVerdict.Flag)
        {
            await _events.RaiseSafetyFlagAsync(result.RuleId, result.Reason, context.AgentRole, result.Metadata, cancellationToken);
        }

        var effectiveMessages = result.Verdict == SafetyVerdict.Modify && result.ModifiedMessages != null
            ? result.ModifiedMessages
            : messageList;

        await foreach (var update in _innerClient.GetStreamingResponseAsync(effectiveMessages, options, cancellationToken))
            yield return update;
    }

    private async Task<SafetyResult> EvaluateRulesAsync(SafetyContext context, CancellationToken ct)
    {
        foreach (var rule in _rules)
        {
            var result = await rule.EvaluateAsync(context, ct);
            if (result.Verdict != SafetyVerdict.Allow)
            {
                _logger.LogWarning("Safety rule {RuleId} returned {Verdict}: {Reason}",
                    rule.RuleId, result.Verdict, result.Reason);
                return result;
            }
        }
        return SafetyResult.Allow("composite", "All rules passed");
    }
}
```

### SafetyViolationException

**File:** `SentinelCore.Orchestrations/Agents/Middleware/SafetyViolationException.cs`

```csharp
public sealed class SafetyViolationException : Exception
{
    public string RuleId { get; }
    public SafetyVerdict Verdict { get; }

    public SafetyViolationException(string ruleId, string reason, SafetyVerdict verdict = SafetyVerdict.Block)
        : base($"Safety violation [{ruleId}]: {reason}")
    {
        RuleId = ruleId;
        Verdict = verdict;
    }
}
```

---

## 3. AgentBuilder Integration

**File:** `SentinelCore.Orchestrations/Agents/AgentBuilder.cs`

```csharp
public AIAgent Build(AgentSpec spec)
{
    // ... create OllamaApiClient, LoggingChatClient, EventPublishingChatClient ...

    IChatClient client = new ChatClientAgent(spec.AgentName, spec.Persona.Instructions, spec.Tools);

    // Apply safety middleware ONLY for Core agent
    if (spec.Role == AgentRole.Core)
    {
        var safetyMiddleware = _serviceProvider.GetRequiredService<ISafetyMiddleware>();
        client = safetyMiddleware with { InnerClient = client };
    }

    // Apply pattern memory injector for Core agent
    if (spec.Role == AgentRole.Core)
    {
        var patternInjector = _serviceProvider.GetRequiredService<PatternMemoryInjector>();
        client = client.WithMiddleware(patternInjector);
    }

    return new AIAgent(client, spec.AgentName);
}
```

**Key Points:**
- Safety middleware only applied to `AgentRole.Core`
- Uses DI to resolve `ISafetyMiddleware` (defaults to `NullSafetyMiddleware`)
- Applied **after** event publishing, **before** pattern memory injector
- Order: `OllamaApiClient` → `LoggingChatClient` → `EventPublishingChatClient` → `SafetyMiddleware` → `PatternMemoryInjector` → `ChatClientAgent`

---

## 4. DI Registration

**File:** `SentinelCore.Orchestrations/Infrastructure/DI/SentinelCoreServiceExtensions.cs`

```csharp
public static IServiceCollection AddSentinelCoreOrchestrations(this IServiceCollection services, IConfiguration configuration)
{
    // ... other registrations ...

    // Safety middleware - defaults to NullSafetyMiddleware
    services.AddScoped<ISafetyMiddleware>(sp =>
    {
        var rules = sp.GetServices<ISafetyRule>().ToList();
        var innerClient = sp.GetRequiredService<IChatClient>(); // This would be the decorated client
        var logger = sp.GetRequiredService<ILogger<SafetyMiddleware>>();
        var events = sp.GetRequiredService<ISentinelCoreEvents>();

        if (rules.Count == 0)
        {
            return new NullSafetyMiddleware(innerClient);
        }

        return new SafetyMiddleware(innerClient, rules, logger, events);
    });

    // Register safety rules (example - not yet implemented)
    // services.AddSingleton<ISafetyRule, PiiDetectionRule>();
    // services.AddSingleton<ISafetyRule, HarmfulContentRule>();
    // services.AddSingleton<ISafetyRule, PromptInjectionRule>();

    return services;
}
```

**Configuration Pattern:** Safety rules are registered as `ISafetyRule` implementations. The middleware factory collects all registered rules via `GetServices<ISafetyRule>()`.

---

## 5. Example Safety Rules (Not Yet Implemented)

### PII Detection Rule

```csharp
public sealed class PiiDetectionRule : ISafetyRule
{
    public string RuleId => "pii-detection";
    public string Name => "PII Detection";

    private static readonly Regex[] PiiPatterns =
    [
        new(@"\b\d{3}-\d{2}-\d{4}\b", RegexOptions.Compiled), // SSN
        new(@"\b\d{4}[\s-]?\d{4}[\s-]?\d{4}[\s-]?\d{4}\b", RegexOptions.Compiled), // Credit card
        new(@"\b[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Z|a-z]{2,}\b", RegexOptions.Compiled), // Email
    ];

    public Task<SafetyResult> EvaluateAsync(SafetyContext context, CancellationToken ct)
    {
        var userMessages = context.Messages.Where(m => m.Role == ChatRole.User);

        foreach (var message in userMessages)
        {
            var text = message.Text ?? string.Empty;
            foreach (var pattern in PiiPatterns)
            {
                if (pattern.IsMatch(text))
                {
                    return Task.FromResult(SafetyResult.Modify(
                        RuleId,
                        "PII detected in user message",
                        SanitizeMessages(context.Messages)));
                }
            }
        }

        return Task.FromResult(SafetyResult.Allow(RuleId));
    }

    private IReadOnlyList<ChatMessage> SanitizeMessages(IReadOnlyList<ChatMessage> messages)
    {
        return messages.Select(m => m with { Text = SanitizeText(m.Text) }).ToList();
    }

    private string SanitizeText(string? text)
    {
        if (string.IsNullOrEmpty(text)) return text ?? string.Empty;
        var result = text;
        foreach (var pattern in PiiPatterns)
        {
            result = pattern.Replace(result, "[REDACTED]");
        }
        return result;
    }
}
```

### Harmful Content Rule

```csharp
public sealed class HarmfulContentRule : ISafetyRule
{
    public string RuleId => "harmful-content";
    public string Name => "Harmful Content Detection";

    private readonly IContentSafetyClient _safetyClient; // Azure Content Safety, etc.

    public async Task<SafetyResult> EvaluateAsync(SafetyContext context, CancellationToken ct)
    {
        var userText = string.Join("\n", context.Messages
            .Where(m => m.Role == ChatRole.User)
            .Select(m => m.Text ?? string.Empty));

        if (string.IsNullOrWhiteSpace(userText))
            return SafetyResult.Allow(RuleId);

        var result = await _safetyClient.AnalyzeTextAsync(userText, ct);

        if (result.HateSeverity > 3 || result.ViolenceSeverity > 3 ||
            result.SelfHarmSeverity > 3 || result.SexualSeverity > 3)
        {
            return SafetyResult.Block(RuleId, "Harmful content detected");
        }

        if (result.HateSeverity > 0 || result.ViolenceSeverity > 0)
        {
            return SafetyResult.Flag(RuleId, "Potentially harmful content flagged",
                new Dictionary<string, object?> { ["analysis"] = result });
        }

        return SafetyResult.Allow(RuleId);
    }
}
```

### Prompt Injection Rule

```csharp
public sealed class PromptInjectionRule : ISafetyRule
{
    public string RuleId => "prompt-injection";
    public string Name => "Prompt Injection Detection";

    private static readonly string[] InjectionPatterns =
    [
        "ignore previous instructions",
        "disregard the above",
        "system prompt",
        "you are now",
        "act as",
        "pretend to be",
        "override",
        "bypass",
        "jailbreak"
    ];

    public Task<SafetyResult> EvaluateAsync(SafetyContext context, CancellationToken ct)
    {
        var userText = string.Join(" ", context.Messages
            .Where(m => m.Role == ChatRole.User)
            .Select(m => m.Text ?? string.Empty))
            .ToLowerInvariant();

        foreach (var pattern in InjectionPatterns)
        {
            if (userText.Contains(pattern))
            {
                return Task.FromResult(SafetyResult.Block(
                    RuleId,
                    $"Potential prompt injection detected: '{pattern}'"));
            }
        }

        return Task.FromResult(SafetyResult.Allow(RuleId));
    }
}
```

---

## 6. Event Integration

**File:** `SentinelCore.Contracts/Events/ISentinelCoreEvents.cs`

```csharp
public interface ISentinelCoreEvents
{
    // ... existing events ...

    /// <summary>
    /// Raised when a safety rule blocks a request.
    /// </summary>
    Task RaiseSafetyViolationAsync(string ruleId, string reason, AgentRole agentRole, CancellationToken ct = default);

    /// <summary>
    /// Raised when a safety rule flags content for review.
    /// </summary>
    Task RaiseSafetyFlagAsync(string ruleId, string reason, AgentRole agentRole, IReadOnlyDictionary<string, object?> metadata, CancellationToken ct = default);
}
```

**Implementation:** `EventPublishingChatClient` routes these to the host via the appropriate event channel.

---

## 7. Pattern-Lock Compliance

| Rule | Status | Notes |
|------|--------|-------|
| Abstractions in Contracts | ✅ | `ISafetyMiddleware`, `ISafetyRule`, `SafetyContext`, `SafetyResult`, `SafetyVerdict` |
| Middleware-based | ✅ | Implements `IChatClient` decorator pattern |
| Role-aware (Core only) | ✅ | Applied in `AgentBuilder` for `AgentRole.Core` |
| Fail-safe default | ✅ | `NullSafetyMiddleware` when no rules registered |
| Composable rules | ✅ | `IEnumerable<ISafetyRule>` evaluated in order |
| Event integration | ✅ | `ISentinelCoreEvents` for violations/flags |
| No circular dependencies | ✅ | Contracts → Orchestrations only |

---

## 8. Open Items / TODOs

| Item | Location | Status |
|------|----------|--------|
| `ISafetyRule` implementations | Orchestrations | ❌ None implemented yet |
| `IContentSafetyClient` abstraction | Contracts | ❌ Not defined |
| Safety rule configuration | Settings | ❌ Not in `SentinelCoreSettings` |
| Rule ordering/priority | Middleware | ⚠️ Registration order only |
| Streaming safety evaluation | Middleware | ⚠️ Evaluates once pre-stream |
| Safety metrics/telemetry | Events | ❌ Not implemented |
| Per-role rule sets | Middleware | ❌ Single rule set for all |
| Unit tests for safety pipeline | Tests | ❌ Not written |

---

## 9. Related Documentation

| Document | Description |
|----------|-------------|
| `DynamicAgentsComponent.md` | AgentBuilder pipeline, Core agent construction |
| `ContractsComponent.md` | Safety abstractions, ISentinelCoreEvents |
| `OrchestrationComponent.md` | Agent construction pipeline, middleware ordering |
| `PatternLock.md` | Architectural constraints |

---

## Version History

| Version | Date | Author | Changes |
|---------|------|--------|---------|
| v1.0 | 2025-07-18 | Kyle | Initial documentation from source code analysis |
