// Solution: SentinelCoreLib
// Project:   SentinelCoreLib
// File:         CaseFlowEngine.cs
// Author: Kyle L. Crowder
// Build Date: 2026/07/07



using Microsoft.Extensions.AI;

using SentinelCoreLib.Agents.Middleware;
using SentinelCoreLib.Application.Abstractions;
using SentinelCoreLib.Application.Abstractions.Persistence;
using SentinelCoreLib.CaseFlow;




namespace SentinelCoreLib.Domain.Contracts;





/// <summary>
///     Strongly-typed case identifier.
/// </summary>
public sealed record CaseId(string Value)
{
    /// <summary>
    ///     Returns the underlying string value.
    /// </summary>
    public override string ToString() => Value;
}





/// <summary>
///     Default implementation of <see cref="ICaseFlowEngine" />.
/// </summary>
public sealed class CaseFlowEngine : ICaseFlowEngine
{
    private readonly ICaseRepository _caseRepository;
    private readonly IEvidenceStore _evidenceStore;
    private readonly ISafetyMiddleware _safety;
    private readonly IToolRegistry _toolRegistry;








    /// <summary>
    ///     Initializes a new instance of the <see cref="CaseFlowEngine" /> class.
    /// </summary>
    public CaseFlowEngine(ICaseRepository caseRepository, IEvidenceStore evidenceStore, ISafetyMiddleware safety, IToolRegistry toolRegistry)
    {
        _caseRepository = caseRepository ?? throw new ArgumentNullException(nameof(caseRepository));
        _evidenceStore = evidenceStore ?? throw new ArgumentNullException(nameof(evidenceStore));
        _safety = safety ?? throw new ArgumentNullException(nameof(safety));
        _toolRegistry = toolRegistry ?? throw new ArgumentNullException(nameof(toolRegistry));
    }








    /// <inheritcheck />
    public async Task AbortCaseAsync(CaseId caseId, string reason, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(caseId);
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);

        CaseRecord? record = await _caseRepository.GetByIdAsync(caseId.Value, cancellationToken).ConfigureAwait(false);
        if (record is null)
        {
            return;
        }

        record.Update(CaseStatus.Blocked);
        await _caseRepository.UpdateAsync(record, cancellationToken).ConfigureAwait(false);
    }








    /// <inheritcheck />
    public async Task AddEvidenceAsync(CaseId caseId, EvidenceItem item, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(caseId);
        ArgumentNullException.ThrowIfNull(item);

        await _evidenceStore.AddAsync(caseId.Value, item, cancellationToken).ConfigureAwait(false);
    }








    /// <inheritcheck />
    public async Task AdvanceCaseAsync(CaseId caseId, CaseStatus status, string? stateJson = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(caseId);

        CaseRecord? record = await _caseRepository.GetByIdAsync(caseId.Value, cancellationToken).ConfigureAwait(false);
        if (record is null)
        {
            return;
        }

        record.Update(status);
        await _caseRepository.UpdateAsync(record, cancellationToken).ConfigureAwait(false);
    }








    /// <inheritcheck />
    public async Task<CaseId> CreateCaseAsync(string title, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);

        CaseId caseId = new(Guid.NewGuid().ToString("N"));
        CaseRecord record = new(caseId.Value, title, CaseStatus.Open, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, "{}");

        await _caseRepository.CreateAsync(record, cancellationToken).ConfigureAwait(false);
        return caseId;
    }








    /// <inheritcheck />
    public async Task<InvestigationWorkItem> EmitInvestigationWorkAsync(CaseId caseId, string message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(caseId);
        ArgumentException.ThrowIfNullOrWhiteSpace(message);

        CaseRecord? record = await _caseRepository.GetByIdAsync(caseId.Value, cancellationToken).ConfigureAwait(false);
        if (record is null)
        {
            return new(caseId, "Case not found.", isBlocked: true, "Case not found.");
        }



        SafetyContext safetyContext = new()
        {
            CaseId = caseId.Value,
            Message = new(ChatRole.User, message),
            RegisteredToolNames = null, //TODO: _toolRegistry.GetRegisteredToolNames(),
            MutatingToolNames = new HashSet<string>()
        };

        SafetyVerdict verdict = _safety.Evaluate(safetyContext);
        if (!verdict.IsAllowed)
        {
            record.Update(CaseStatus.Blocked);
            await _caseRepository.UpdateAsync(record, cancellationToken).ConfigureAwait(false);
            return new(caseId, message, isBlocked: true, $"Blocked by safety rule '{verdict.RuleName}': {verdict.Reason}");
        }

        return new(caseId, message);
    }
}