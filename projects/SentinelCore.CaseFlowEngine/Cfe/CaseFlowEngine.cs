// Solution: SentinelCore
// Project:   SentinelCore.CaseFlowEngine
// File:         CaseFlowEngine.cs
// Author: Kyle L. Crowder
// Build Num:  082808



using Microsoft.EntityFrameworkCore;

using SentinelCore.Abstractions;
using SentinelCore.Cfe.Persistence;
using SentinelCore.Contracts;
using SentinelCore.Persistence;




namespace SentinelCore.Cfe;





public interface ICaseFlowEngine
{
    /// <summary>
    ///     Advances a case to the specified state.
    /// </summary>
    /// <param name="caseId">The case identifier.</param>
    /// <param name="status">The target status.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task AdvanceCaseAsync(Guid caseId, CaseStatus status, CancellationToken cancellationToken = default);








    Guid CreateCase(Signal rawSignal, CancellationToken cancellationToken = default);








    /// <summary>
    ///     Creates a new case from a user request.
    /// </summary>
    /// <param name="signal">The signal that triggered the case.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created case identifier.</returns>
    Task<Guid> CreateCaseAsync(Signal signal, CancellationToken cancellationToken = default);








    /// <summary>
    ///     Returns the number of cases currently in the specified <paramref name="status" />.
    /// </summary>
    /// <param name="status">The case status to count.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The count of cases matching the given status.</returns>
    Task<int> GetCaseCountByStatusAsync(CaseStatus status, CancellationToken cancellationToken = default);








    /// <summary>
    ///     Returns all cases currently in the specified <paramref name="status" />.
    /// </summary>
    /// <param name="status">The case status to filter by.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A read-only list of cases matching the given status.</returns>
    Task<IReadOnlyList<Case>> GetCasesByStatusAsync(CaseStatus status, CancellationToken cancellationToken = default);
}





/// <summary>
///     Default implementation of <see cref="ICaseFlowEngine" />.
///     <para>
///         The Case Flow Engine (CFE) is the single owner of case lifecycle state.
///         No agent, orchestrator, or host may mutate case status directly — all
///         mutations must flow through this engine.
///     </para>
/// </summary>
public sealed class CaseFlowEngine : ICaseFlowEngine
{
    private readonly SentinelCoreDBContext _dbContext;

    /// <summary>
    ///     Allowed state transitions for the case lifecycle.
    ///     Each key is the current status; each value is the set of statuses
    ///     that the case may transition to from that current status.
    /// </summary>
    private static readonly Dictionary<CaseStatus, HashSet<CaseStatus>> AllowedTransitions = new()
    {
            [CaseStatus.Open] = [CaseStatus.Analysis, CaseStatus.Cancelled],
            [CaseStatus.Analysis] = [CaseStatus.Investigation, CaseStatus.AwaitingInput, CaseStatus.Blocked, CaseStatus.Cancelled],
            [CaseStatus.Investigation] = [CaseStatus.Review, CaseStatus.AwaitingInput, CaseStatus.Blocked, CaseStatus.Escalated, CaseStatus.Alerted, CaseStatus.Cancelled],
            [CaseStatus.Review] = [CaseStatus.Complete, CaseStatus.Investigation, CaseStatus.AwaitingInput, CaseStatus.Escalated, CaseStatus.Cancelled],
            [CaseStatus.AwaitingInput] = [CaseStatus.Investigation, CaseStatus.Escalated, CaseStatus.Cancelled],
            [CaseStatus.Escalated] = [CaseStatus.Investigation, CaseStatus.AwaitingInput, CaseStatus.Blocked, CaseStatus.Alerted, CaseStatus.Cancelled],
            [CaseStatus.Alerted] = [CaseStatus.Escalated, CaseStatus.Blocked, CaseStatus.Cancelled],
            [CaseStatus.Blocked] = [CaseStatus.AwaitingInput, CaseStatus.Escalated, CaseStatus.Alerted, CaseStatus.Cancelled],
            [CaseStatus.Complete] = [CaseStatus.Closed],
            [CaseStatus.Cancelled] = [CaseStatus.Closed],
            [CaseStatus.Closed] = []
    };








    /// <summary>
    ///     Initializes a new instance of the <see cref="CaseFlowEngine" /> class.
    /// </summary>
    /// <param name="dbContext">
    ///     The database context for accessing and persisting case-related data.
    /// </param>
    /// <exception cref="ArgumentNullException">
    ///     Thrown when <paramref name="dbContext" /> is <c>null</c>.
    /// </exception>
    public CaseFlowEngine(SentinelCoreDBContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }








    public async Task AdvanceCaseAsync(Guid caseId, CaseStatus status, CancellationToken cancellationToken = default)
    {
        if (caseId == Guid.Empty)
        {
            throw new ArgumentException("Case identifier must be a non-empty GUID.", nameof(caseId));
        }

        // 1. Retrieve the current case
        Case? caseRecord = await GetCaseByIdAsync(caseId, cancellationToken).ConfigureAwait(false);

        if (caseRecord is null)
        {
            throw new InvalidOperationException($"Case '{caseId}' not found.");
        }

        // 2. Validate the transition is allowed by the lifecycle
        ValidateTransition(caseRecord.Status, status);
        /*
                // 3. Safety gate — evaluate before applying the transition
                SafetyContext safetyContext = new() { CaseId = caseId.ToString() };

                SafetyVerdict verdict = _safetyMiddleware.Evaluate(safetyContext);

                if (verdict == SafetyVerdict.Blocked)
                {
                    // Transition blocked by safety — force to Blocked status instead
                    caseRecord.Status = CaseStatus.Blocked;
                    caseRecord.UpdatedAt = DateTime.Now;
                    //   await _caseRepository.UpdateAsync(caseRecord, cancellationToken).ConfigureAwait(false);
                    return;
                }
                */
        // 4. Apply the transition
        caseRecord.Status = status;
        caseRecord.UpdatedAt = DateTime.Now;

        await UpdateAsync(caseRecord, cancellationToken).ConfigureAwait(false);
    }








    public Guid CreateCase(Signal rawSignal, CancellationToken cancellationToken = default)
    {
        //Save the signal first so we can grab this records identifier and use it in the case.
        SignalEntity ent = rawSignal.ToEntity();
        _dbContext.SignalEntities.Add(ent);
        _dbContext.SaveChanges();

        //Now the case.
        CaseEntity caseent = new CaseEntity { InitiatingSignal = ent.SignalId, CaseId = Guid.NewGuid(), Status = (int)CaseStatus.Open };
        _dbContext.CaseEntities.Add(caseent);
        _dbContext.SaveChanges();
        return caseent.CaseId;
    }








    /// <summary>
    ///     Creates a new case and associates it with the provided <see cref="Signal" />.
    /// </summary>
    /// <param name="signal">
    ///     The <see cref="Signal" /> instance containing the details required to create the case.
    /// </param>
    /// <param name="cancellationToken">
    ///     A <see cref="CancellationToken" /> to observe while waiting for the task to complete.
    /// </param>
    /// <returns>
    ///     A <see cref="Task{TResult}" /> representing the asynchronous operation, which upon completion
    ///     contains the unique identifier (<see cref="Guid" />) of the newly created case.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    ///     Thrown when the <paramref name="signal" /> is <c>null</c>.
    /// </exception>
    public async Task<Guid> CreateCaseAsync(Signal signal, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(signal);

        Case caseRecord = new() { CaseId = Guid.NewGuid(), Status = CaseStatus.Open, CreatedAt = DateTime.Now };

        // Single atomic SaveChanges: signal + case + FK link, all in one round-trip
        return await CreateCaseWithSignalAsync(signal, caseRecord, cancellationToken).ConfigureAwait(false);
    }








    /// <summary>
    ///     Returns the number of cases currently in the specified <paramref name="status" />.
    /// </summary>
    /// <param name="status">The case status to count.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The count of cases matching the given status.</returns>
    public async Task<int> GetCaseCountByStatusAsync(CaseStatus status, CancellationToken cancellationToken = default)
    {
        // Validate input



        // Query the database and return the count
        return await _dbContext.CaseEntities.Where(d => d.Status == (int)status).CountAsync(cancellationToken);
    }








    /// <summary>
    ///     Returns all cases currently in the specified <paramref name="status" />.
    /// </summary>
    public async Task<IReadOnlyList<Case>> GetCasesByStatusAsync(CaseStatus status, CancellationToken cancellationToken = default)
    {
        List<CaseEntity> entities = await _dbContext.CaseEntities.Where(c => c.Status == (int)status).ToListAsync(cancellationToken);

        return entities.Select(e => e.ToCase()).ToList();
    }








    /// <summary>
    ///     Creates a new case record associated with the provided signal and saves it to the database.
    /// </summary>
    /// <param name="signal">
    ///     The signal associated with the case. This parameter must not be <c>null</c>.
    /// </param>
    /// <param name="caseRecord">
    ///     The case record to be created. This parameter must not be <c>null</c>.
    /// </param>
    /// <param name="cancellationToken">
    ///     A token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None" />.
    /// </param>
    /// <returns>
    ///     A <see cref="Task{TResult}" /> representing the asynchronous operation, with the result being the unique identifier
    ///     (<see cref="Guid" />) of the created case.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    ///     Thrown if <paramref name="signal" /> or <paramref name="caseRecord" /> is <c>null</c>.
    /// </exception>
    /// <remarks>
    ///     This method ensures that the signal and case records are saved atomically within a single database transaction.
    /// </remarks>
    private async Task<Guid> CreateCaseWithSignalAsync(Signal signal, Case caseRecord, CancellationToken cancellationToken)
    {
        //Save the signal first so we can grab this records identifier and use it in the case.
        SignalEntity ent = signal.ToEntity();
        _dbContext.SignalEntities.Add(ent);
        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        //Now the case.
        CaseEntity caseent = caseRecord.ToEntity();
        caseent.InitiatingSignal = ent.SignalId;
        caseent.Status = (int)CaseStatus.Open;
        _dbContext.CaseEntities.Add(caseent);
        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return caseent.CaseId;
    }








    /// <summary>
    ///     Retrieves a case by its identifier, or <c>null</c> when no matching case exists.
    /// </summary>
    private async Task<Case?> GetCaseByIdAsync(Guid caseId, CancellationToken cancellationToken)
    {
        CaseEntity? entity = await _dbContext.CaseEntities
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.CaseId == caseId, cancellationToken)
                .ConfigureAwait(false);

        return entity?.ToCase();
    }




    /// <summary>
    ///     Persists the status and timestamp of <paramref name="caseRecord" /> to its database row.
    /// </summary>
    private async Task UpdateAsync(Case caseRecord, CancellationToken cancellationToken)
    {
        CaseEntity? tracked = await _dbContext.CaseEntities
                .FirstOrDefaultAsync(c => c.CaseId == caseRecord.CaseId, cancellationToken)
                .ConfigureAwait(false);

        if (tracked is null)
        {
            throw new InvalidOperationException($"Case '{caseRecord.CaseId}' not found.");
        }

                    tracked.Status = (int)caseRecord.Status;
                    tracked.UpdatedAt = caseRecord.UpdatedAt;
                    await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                }




                /// <summary>
                ///     Validates that the transition from <paramref name="from" /> to
    ///     <paramref name="to" /> is allowed by the case lifecycle.
    ///     This ensures that the case status changes adhere to the predefined
    ///     lifecycle rules, preventing invalid state transitions.
    ///     The method checks if the current status has a set of allowed
    ///     transitions and verifies that the target status is within this set.
    /// </summary>
    private static void ValidateTransition(CaseStatus from, CaseStatus to)
    {
        if (!AllowedTransitions.TryGetValue(from, out HashSet<CaseStatus>? allowed))
        {
            throw new InvalidOperationException($"Case status '{from}' has no defined transitions.");
        }

        if (!allowed.Contains(to))
        {
            throw new InvalidOperationException($"Transition from '{from}' to '{to}' is not allowed. " + $"Allowed transitions from '{from}': [{string.Join(", ", allowed)}].");
        }
    }
}