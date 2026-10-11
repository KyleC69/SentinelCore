// Solution: SentinelCore
// Project:   SentinelCore.CaseFlowEngine
// File:         CaseFlowEngine.cs
// Author: Kyle L. Crowder
// Build Num:  100908



using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

using SentinelCore.CaseFlowEngine.Persistence;
using SentinelCore.Cfe.Persistence;
using SentinelCore.Contracts.CaseFlow;
using SentinelCore.Contracts.Cfe;
using SentinelCore.Persistence;




namespace SentinelCore.CaseFlowEngine.Cfe;





public interface ICaseFlowEngine
{
    /// <summary>
    ///     Advances a case to the specified state.
    /// </summary>
    /// <param name="caseId">The case identifier.</param>
    /// <param name="status">The target status.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task AdvanceCaseAsync(int caseId, CaseStatus status, CancellationToken cancellationToken = default);








    int CreateCase(Signal rawSignal, CancellationToken cancellationToken = default);








    /// <summary>
    ///     Creates a new case from a user request.
    /// </summary>
    /// <param name="signal">The signal that triggered the case.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created case identifier.</returns>
    Task<int> CreateCaseAsync(Signal signal, CancellationToken cancellationToken = default);








    /// <summary>
    ///     Returns the statuses a case in the given status may legally transition to.
    /// </summary>
    /// <param name="status">The current status of the case.</param>
    /// <returns>A read-only list of the statuses reachable from <paramref name="status" />.</returns>
    IReadOnlyList<CaseStatus> GetAllowedTransitions(CaseStatus status);








    /// <summary>
    ///     Returns the case with the given business identifier, or <c>null</c> when no
    ///     matching case exists.
    /// </summary>
    /// <param name="caseId">The business identifier of the case.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The matching case, or <c>null</c> when not found.</returns>
    Task<Case?> GetCaseByIdAsync(int caseId, CancellationToken cancellationToken = default);








    /// <summary>
    ///     Returns the number of cases currently in the specified <paramref name="status" />.
    /// </summary>
    /// <param name="status">The case status to count.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The count of cases matching the given status.</returns>
    Task<int> GetCaseCountByStatusAsync(CaseStatus status, CancellationToken cancellationToken = default);








    /// <summary>
    ///     Returns the count of cases in every status in a single grouped query.
    ///     Statuses with no cases are included with a count of zero.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A read-only dictionary mapping each case status to its case count.</returns>
    Task<IReadOnlyDictionary<CaseStatus, int>> GetCaseStatusCountsAsync(CancellationToken cancellationToken = default);





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
    private readonly IDbContextFactory<SentinelCoreDBContext> _dbContextFactory;

    /// <summary>
    ///     Allowed state transitions for the case lifecycle.
    ///     Each key is the current status; each value is the set of statuses
    ///     that the case may transition to from that current status.
    /// </summary>
    private static readonly Dictionary<CaseStatus, HashSet<CaseStatus>> AllowedTransitions = new()
    {
        [CaseStatus.Open] = new HashSet<CaseStatus> { CaseStatus.Analysis, CaseStatus.Cancelled },
        [CaseStatus.Analysis] = new HashSet<CaseStatus> { CaseStatus.Investigation, CaseStatus.AwaitingInput, CaseStatus.Blocked, CaseStatus.Cancelled },
        [CaseStatus.Investigation] = new HashSet<CaseStatus> { CaseStatus.Review, CaseStatus.AwaitingInput, CaseStatus.Blocked, CaseStatus.Escalated, CaseStatus.Alerted, CaseStatus.Cancelled },
        [CaseStatus.Review] = new HashSet<CaseStatus> { CaseStatus.Complete, CaseStatus.Investigation, CaseStatus.AwaitingInput, CaseStatus.Escalated, CaseStatus.Cancelled },
        [CaseStatus.AwaitingInput] = new HashSet<CaseStatus> { CaseStatus.Investigation, CaseStatus.Escalated, CaseStatus.Cancelled },
        [CaseStatus.Escalated] = new HashSet<CaseStatus> { CaseStatus.Investigation, CaseStatus.AwaitingInput, CaseStatus.Blocked, CaseStatus.Alerted, CaseStatus.Cancelled },
        [CaseStatus.Alerted] = new HashSet<CaseStatus> { CaseStatus.Escalated, CaseStatus.Blocked, CaseStatus.Cancelled },
        [CaseStatus.Blocked] = new HashSet<CaseStatus> { CaseStatus.AwaitingInput, CaseStatus.Escalated, CaseStatus.Alerted, CaseStatus.Cancelled },
        [CaseStatus.Complete] = new HashSet<CaseStatus> { CaseStatus.Closed },
        [CaseStatus.Cancelled] = new HashSet<CaseStatus> { CaseStatus.Closed },
        [CaseStatus.Closed] = new HashSet<CaseStatus>()
    };








    /// <summary>
    ///     Initializes a new instance of the <see cref="CaseFlowEngine" /> class.
    /// </summary>
    /// <param name="dbContextFactory">
    ///     Factory for creating database contexts on demand.
    /// </param>
    public CaseFlowEngine(IDbContextFactory<SentinelCoreDBContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory ?? throw new ArgumentNullException(nameof(dbContextFactory));
    }








    public async Task AdvanceCaseAsync(int caseId, CaseStatus status, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(caseId);

        await using SentinelCoreDBContext db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        // 1. Retrieve the current case
        CaseEntity entity = await db.CaseEntities.FindAsync(new object[] { caseId }, cancellationToken);

        if (entity is null)
        {
            throw new InvalidOperationException($"Case '{caseId}' not found.");
        }

        CaseStatus currentStatus = (CaseStatus)entity.Status;

        // 2. Validate the transition is allowed by the lifecycle
        ValidateTransition(currentStatus, status);
        /*
                // 3. Safety gate — evaluate before applying the transition
                SafetyContext safetyContext = new() { CaseId = caseId.ToString() };

                SafetyVerdict verdict = _safetyMiddleware.Evaluate(safetyContext);

                if (verdict == SafetyVerdict.Blocked)
                {
                    // Transition blocked by safety — force to Blocked status instead
                    entity.Status = (int)CaseStatus.Blocked;
                    entity.UpdatedAt = DateTime.Now;
                    await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                    return;
                }
                */
        // 4. Apply the transition
        entity.Status = (int)status;
        entity.UpdatedAt = DateTime.Now;

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }








    public int CreateCase(Signal rawSignal, CancellationToken cancellationToken = default)
    {
        using SentinelCoreDBContext db = _dbContextFactory.CreateDbContext();
        using IDbContextTransaction transaction = db.Database.BeginTransaction();

        //Save the signal first so we can grab this records identifier and use it in the case.
        SignalEntity ent = rawSignal.ToEntity();
        db.SignalEntities!.Add(ent);
        db.SaveChanges();

        //Now the case.
        CaseEntity caseent = new() { InputSignal = ent, Status = (int)CaseStatus.Open };
        db.CaseEntities!.Add(caseent);
        db.SaveChanges();

        transaction.Commit();
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
    public async Task<int> CreateCaseAsync(Signal signal, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(signal);


        // Single atomic SaveChanges: signal + case + FK link, all in one round-trip
        return await CreateCaseWithSignalAsync(signal, cancellationToken).ConfigureAwait(false);
    }








    /// <summary>
    ///     Returns the statuses a case in the given status may legally transition to.
    /// </summary>
    /// <param name="status">The current status of the case.</param>
    /// <returns>A read-only list of the statuses reachable from <paramref name="status" />.</returns>
    public IReadOnlyList<CaseStatus> GetAllowedTransitions(CaseStatus status)
    {
        return AllowedTransitions.TryGetValue(status, out HashSet<CaseStatus>? allowed) ? allowed.ToList() : [];
    }










    /// <summary>
    ///     Returns the number of cases currently in the specified <paramref name="status" />.
    /// </summary>
    /// <param name="status">The case status to count.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The count of cases matching the given status.</returns>
    public async Task<int> GetCaseCountByStatusAsync(CaseStatus status, CancellationToken cancellationToken = default)
    {
        await using SentinelCoreDBContext db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await db.CaseEntities!.Where(d => d.Status == (int)status).CountAsync(cancellationToken);
    }








    /// <summary>
    ///     Returns the count of cases in every status in a single grouped query.
    ///     Statuses with no cases are included with a count of zero.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A read-only dictionary mapping each case status to its case count.</returns>
    public async Task<IReadOnlyDictionary<CaseStatus, int>> GetCaseStatusCountsAsync(CancellationToken cancellationToken = default)
    {
        await using SentinelCoreDBContext db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        Dictionary<CaseStatus, int> counts = Enum.GetValues<CaseStatus>().ToDictionary(status => status, _ => 0);

        List<StatusCount> grouped = await db.CaseEntities!.GroupBy(c => c.Status).Select(g => new StatusCount(g.Key, g.Count())).ToListAsync(cancellationToken).ConfigureAwait(false);

        foreach (StatusCount group in grouped) counts[(CaseStatus)group.Status] = group.Count;

        return counts;
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
    private async Task<int> CreateCaseWithSignalAsync(Signal signal, CancellationToken cancellationToken)
    {
        await using SentinelCoreDBContext db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        await using IDbContextTransaction transaction = await db.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);

        //Save the signal first so we can grab this records identifier and use it in the case.
        SignalEntity ent = signal.ToEntity();
        db.SignalEntities!.Add(ent);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        //Now the case.
        CaseEntity caseent = new CaseEntity
        {
            InputSignal = ent,
            Status = (int)CaseStatus.Open,
            CreatedAt = DateTime.Now
        };
        db.CaseEntities!.Add(caseent);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
        return caseent.CaseId;
    }



    private async Task<Case> PullNewCase()
    {
        await using SentinelCoreDBContext db = await _dbContextFactory.CreateDbContextAsync();
        CaseEntity? entity = await db.CaseEntities!.OrderBy(c => c.CreatedAt).FirstOrDefaultAsync();
        if (entity is null)
        {
            throw new InvalidOperationException("No cases available to pull.");
        }
        return entity;
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








    /// <summary>
    ///     A single grouped count row: the raw status ordinal and its case count.
    /// </summary>
    /// <param name="Status">The raw database status ordinal.</param>
    /// <param name="Count">The number of cases in that status.</param>
    private sealed record StatusCount(int Status, int Count);
}