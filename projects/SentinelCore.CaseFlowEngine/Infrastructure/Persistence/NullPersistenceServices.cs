// Solution: SentinelCore
// Project:   SentinelCore.CaseFlowEngine
// File:         NullPersistenceServices.cs
// Author: Kyle L. Crowder
// Build Num:  092308



using SentinelCore.CaseFlowEngine.Cfe;
using SentinelCore.Contracts.Abstractions;
using SentinelCore.Contracts.CaseFlow;
using SentinelCore.Contracts.Cfe;




namespace SentinelCore.CaseFlowEngine.Infrastructure.Persistence;





/// <summary>
///     PL-5 null-object default for <see cref="ICaseFlowEngine" />. Registered by
///     <c>AddSentinelCore</c> so the system runs without persistence; the host opts
///     in to the real EF Core-backed engine via <c>AddCaseFlowEngine()</c>.
///     Never throws — every member is a safe no-op or returns default values.
/// </summary>
public sealed class NullCaseFlowEngine : ICaseFlowEngine
{
    /// <summary>
    ///     No-op: without persistence there is no case to advance.
    /// </summary>
    public Task AdvanceCaseAsync(Guid caseId, CaseStatus status, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }








    /// <summary>
    ///     Returns <see cref="Guid.Empty" /> — no case can be created without persistence.
    /// </summary>
    public Guid CreateCase(Signal rawSignal, CancellationToken cancellationToken = default)
    {
        return Guid.Empty;
    }








    /// <summary>
    ///     Returns <see cref="Guid.Empty" /> — no case can be created without persistence.
    /// </summary>
    public Task<Guid> CreateCaseAsync(Signal signal, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Guid.Empty);
    }








    /// <summary>
    ///     Returns an empty transition set — no lifecycle is enforced without persistence.
    /// </summary>
    public IReadOnlyList<CaseStatus> GetAllowedTransitions(CaseStatus status)
    {
        return [];
    }








    /// <summary>
    ///     Returns <c>null</c> — no case can be found without persistence.
    /// </summary>
    public Task<Case?> GetCaseByIdAsync(Guid caseId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<Case?>(null);
    }








    /// <summary>
    ///     Returns zero — no cases exist without persistence.
    /// </summary>
    public Task<int> GetCaseCountByStatusAsync(CaseStatus status, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(0);
    }








    /// <summary>
    ///     Returns zero for every status — no cases exist without persistence.
    /// </summary>
    public Task<IReadOnlyDictionary<CaseStatus, int>> GetCaseStatusCountsAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyDictionary<CaseStatus, int> counts = new Dictionary<CaseStatus, int>();
        return Task.FromResult(counts);
    }








    /// <summary>
    ///     Returns an empty list — no cases exist without persistence.
    /// </summary>
    public Task<IReadOnlyList<Case>> GetCasesByStatusAsync(CaseStatus status, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<Case> cases = [];
        return Task.FromResult(cases);
    }
}





/// <summary>
///     PL-5 null-object default for <see cref="IEvidenceStore" />. Registered by
///     <c>AddSentinelCore</c> so the system runs without persistence; the host opts
///     in to the real EF Core-backed store via <c>AddCaseFlowEngine()</c>.
///     Never throws — every member is a safe no-op or returns default values.
/// </summary>
public sealed class NullEvidenceStore : IEvidenceStore
{
    /// <summary>
    ///     No-op: evidence cannot be stored without persistence.
    /// </summary>
    public Task AddAsync(string caseId, Evidence item, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }








    /// <summary>
    ///     Returns an empty list — no evidence exists without persistence.
    /// </summary>
    public Task<IReadOnlyList<Evidence>> GetByCaseIdAsync(string caseId, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<Evidence> evidence = [];
        return Task.FromResult(evidence);
    }
}





/// <summary>
///     PL-5 null-object default for <see cref="IPatternMemoryStore" />. Registered by
///     <c>AddSentinelCore</c> so the system runs without persistence; the host opts
///     in to the real EF Core-backed store via <c>AddCaseFlowEngine()</c>.
///     Never throws — every member is a safe no-op or returns default values.
/// </summary>
public sealed class NullPatternMemoryStore : IPatternMemoryStore
{
    /// <summary>
    ///     Returns an empty list — no pattern memory exists without persistence.
    /// </summary>
    public Task<IReadOnlyList<PatternMemoryResult>> GetByCaseIdAsync(string caseId, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<PatternMemoryResult> results = [];
        return Task.FromResult(results);
    }








    /// <summary>
    ///     Returns an empty list — no pattern memory exists without persistence.
    /// </summary>
    public Task<IReadOnlyList<PatternMemoryResult>> SearchAsync(float[] embedding, int topK = 10, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<PatternMemoryResult> results = [];
        return Task.FromResult(results);
    }








    /// <summary>
    ///     No-op: pattern memory cannot be stored without persistence.
    /// </summary>
    public Task StoreAsync(string caseId, string summary, float[] signalEmbedding, float[] summaryEmbedding, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}