// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         RateLimitRule.cs
// Author: Kyle L. Crowder
// Build Num:  091418



using System.Collections.Concurrent;




namespace SentinelCore.Orchestrations.SafetyEngine.Rules;





/// <summary>
///     A safety rule that enforces per-agent rate limiting to prevent abuse and ensure
///     fair resource allocation across agents.
/// </summary>
public sealed class RateLimitRule : ISafetyRule
{
    private readonly bool _allowBurst;
    private readonly ConcurrentDictionary<string, RateLimitBucket> _buckets;
    private readonly int _maxRequests;
    private readonly SafetySeverity _severity;
    private readonly TimeSpan _window;








    /// <summary>
    ///     Initializes a new instance of the <see cref="RateLimitRule" /> with default settings.
    ///     Default: 100 requests per minute per agent.
    /// </summary>
    public RateLimitRule()
    {
        Name = "RateLimit";
        _maxRequests = 100;
        _window = TimeSpan.FromMinutes(1);
        _severity = SafetySeverity.High;
        _allowBurst = true;
        _buckets = new ConcurrentDictionary<string, RateLimitBucket>(StringComparer.OrdinalIgnoreCase);
        Description = "Enforces rate limiting of 100 requests per 60 seconds.";
    }








    /// <summary>
    ///     Initializes a new instance of the <see cref="RateLimitRule" /> with custom settings.
    /// </summary>
    /// <param name="name">The unique name of this rule.</param>
    /// <param name="maxRequests">Maximum number of requests allowed within the time window.</param>
    /// <param name="window">The time window for rate limiting.</param>
    /// <param name="severity">The severity when rate limit is exceeded.</param>
    /// <param name="allowBurst">Whether to allow initial burst of requests before rate limiting takes full effect.</param>
    /// <param name="description">A description of what this rule checks.</param>
    public RateLimitRule(string name, int maxRequests, TimeSpan window, SafetySeverity severity, bool allowBurst, string? description)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));

        if (maxRequests <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxRequests), "Max requests must be greater than zero.");
        }

        _maxRequests = maxRequests;
        _window = window;
        _severity = severity;
        _allowBurst = allowBurst;
        _buckets = new ConcurrentDictionary<string, RateLimitBucket>(StringComparer.OrdinalIgnoreCase);
        Description = description ?? $"Enforces rate limiting of {maxRequests} requests per {window.TotalSeconds} seconds.";
    }








 
    public string Description { get; }








 
    public Task<SafetyRuleResult> EvaluateAsync(SafetyEvaluationContext context, CancellationToken cancellationToken = default)
    {
        // Use agent name from context if available, otherwise use a default bucket
        string agentId = context.AgentName ?? "default";
        DateTimeOffset now = DateTimeOffset.UtcNow;

        RateLimitBucket bucket = _buckets.GetOrAdd(agentId, _ => new RateLimitBucket(_maxRequests, _window, _allowBurst));

        bool isAllowed = bucket.TryConsume(now);

        if (isAllowed)
        {
            return Task.FromResult(SafetyRuleResult.Allow(Name, $"Request allowed. Bucket: {bucket.Remaining}/{_maxRequests} remaining."));
        }

        TimeSpan? retryAfter = bucket.GetRetryAfter(now);
        string reason = retryAfter.HasValue ? $"Rate limit exceeded for agent '{agentId}'. Retry after {retryAfter.Value.TotalSeconds:F0} seconds." : $"Rate limit exceeded for agent '{agentId}'. No more requests allowed in current window.";

        return Task.FromResult(SafetyRuleResult.Block(Name, _severity, reason));
    }








 
    public string Name { get; }








    /// <summary>
    ///     Gets the current rate limit status for an agent.
    /// </summary>
    /// <param name="agentId">The agent identifier.</param>
    /// <returns>A tuple containing (isAllowed, remainingRequests, retryAfter).</returns>
    public (bool IsAllowed, int Remaining, TimeSpan? RetryAfter) GetStatus(string agentId)
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;

        if (_buckets.TryGetValue(agentId, out RateLimitBucket? bucket))
        {
            return (bucket.TryPeek(now), bucket.Remaining, bucket.GetRetryAfter(now));
        }

        return (true, _maxRequests, null);
    }








    /// <summary>
    ///     Resets the rate limit for a specific agent.
    /// </summary>
    /// <param name="agentId">The agent identifier to reset.</param>
    public void Reset(string agentId)
    {
        _buckets.TryRemove(agentId, out _);
    }








    /// <summary>
    ///     Resets all rate limit buckets.
    /// </summary>
    public void ResetAll()
    {
        _buckets.Clear();
    }








    /// <summary>
    ///     Internal token bucket implementation for rate limiting.
    /// </summary>
    private sealed class RateLimitBucket
    {
        private readonly bool _allowBurst;
        private readonly int _capacity;
        private readonly object _lock = new();
        private readonly Queue<DateTimeOffset> _requests;
        private readonly TimeSpan _window;








        public RateLimitBucket(int capacity, TimeSpan window, bool allowBurst)
        {
            _capacity = capacity;
            _window = window;
            _allowBurst = allowBurst;
            _requests = new Queue<DateTimeOffset>();
        }








        public int Remaining
        {
            get
            {
                lock (_lock)
                {
                    PruneOldRequests(DateTimeOffset.UtcNow);
                    return Math.Max(0, _capacity - _requests.Count);
                }
            }
        }








        public TimeSpan? GetRetryAfter(DateTimeOffset now)
        {
            lock (_lock)
            {
                if (_requests.Count < _capacity)
                {
                    return null;
                }

                DateTimeOffset oldest = _requests.Peek();
                DateTimeOffset windowEnd = oldest.Add(_window);

                if (windowEnd > now)
                {
                    return windowEnd - now;
                }

                return TimeSpan.Zero;
            }
        }








        private void PruneOldRequests(DateTimeOffset now)
        {
            DateTimeOffset cutoff = now - _window;

            while (_requests.Count > 0 && _requests.Peek() < cutoff)
            {
                _requests.Dequeue();
            }
        }








        public bool TryConsume(DateTimeOffset now)
        {
            lock (_lock)
            {
                PruneOldRequests(now);
                int effectiveCapacity = _allowBurst ? _capacity + 10 : _capacity; // Allow 10 extra burst requests

                if (_requests.Count < effectiveCapacity)
                {
                    _requests.Enqueue(now);
                    return true;
                }

                return false;
            }
        }








        public bool TryPeek(DateTimeOffset now)
        {
            lock (_lock)
            {
                PruneOldRequests(now);
                return _requests.Count < _capacity;
            }
        }
    }
}