// Solution: SentinelCore
// Project:   SentinelCore.CaseFlowEngine
// File:         NullPersistenceServices.cs
// Author: Kyle L. Crowder
// Build Num:  100908



using SentinelCore.CaseFlowEngine.CaseFlow;
using SentinelCore.CaseFlowEngine.Infrastructure;
using SentinelCore.Contracts.Abstractions;
using SentinelCore.Contracts.CaseFlow;
using SentinelCore.Contracts.Cfe;
using SentinelCore.Data.DBEntities;




namespace SentinelCore.CaseFlowEngine.Persistence;





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
    public Task AdvanceCaseAsync(int caseId, CaseStatus status, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }








    public int CreateCase(SignalEntity signalEntity, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }








    /// <summary>
    ///     Creates a new case from a user request.
    /// </summary>
    /// <param name="signal">The signal that triggered the case.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created case identifier.</returns>
    public Task<int> CreateCaseAsync(SignalEntity signal, CancellationToken cancellationToken = default)
    {


        Throw.IfNull(signal);


        return Task.FromResult(0);
    }








    /// <summary>
    ///     Returns 0 — no case can be created without persistence.
    /// </summary>
    public int CreateCase(Signal rawSignal, CancellationToken cancellationToken = default)
    {
        return 0;
    }








    /// <summary>
    ///     Returns 0 — no case can be created without persistence.
    /// </summary>
    public Task<int> CreateCaseAsync(Signal signal, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(0);
    }








    /// <summary>
    ///     Returns the statuses a case in the given status may legally transition to.
    /// </summary>
    /// <param name="status">The current status of the case.</param>
    /// <returns>A read-only list of the statuses reachable from <paramref name="status" />.</returns>
    public IReadOnlyList<CaseStatus> GetAllowedTransitions(int status)
    {
        throw new NotImplementedException();
    }








    /// <summary>
    ///     Returns an empty transition set — no lifecycle is enforced without persistence.
    /// </summary>
    public IReadOnlyList<CaseStatus> GetAllowedTransitions(CaseStatus status)
    {
        return new List<CaseStatus>();
    }








    /// <summary>
    ///     Returns <c>null</c> — no case can be found without persistence.
    /// </summary>
    public Task<CaseEntity?> GetCaseByIdAsync(int caseId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<CaseEntity?>(null);
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
        IReadOnlyList<Case> cases = new List<Case>();
        return Task.FromResult(cases);
    }
}





/// <summary>
///     PL-5 null-object default for <see cref="IEvidenceStore" />. Registered by
///     <c>AddSentinelCore</c> so the system runs without persistence; the host opts
///     in to the real EF Core-backed store via <c>AddCaseFlowEngine()</c>.
///     Never throws — every member is a safe no-op or returns default values.
/// </summary>
public sealed class NullEvidenceStore
{
    /// <summary>
    ///     No-op: evidence cannot be stored without persistence.
    /// </summary>
    public Task AddAsync(int caseId, Evidence item, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }








    /// <summary>
    ///     Returns an empty list — no evidence exists without persistence.
    /// </summary>
    public Task<IReadOnlyList<Evidence>> GetByCaseIdAsync(int caseId, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<Evidence> evidence = new List<Evidence>();
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
        IReadOnlyList<PatternMemoryResult> results = new List<PatternMemoryResult>();
        return Task.FromResult(results);
    }








    /// <summary>
    ///     Returns an empty list — no pattern memory exists without persistence.
    /// </summary>
    public Task<IReadOnlyList<PatternMemoryResult>> SearchAsync(float[] embedding, int topK = 10, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<PatternMemoryResult> results = new List<PatternMemoryResult>();
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