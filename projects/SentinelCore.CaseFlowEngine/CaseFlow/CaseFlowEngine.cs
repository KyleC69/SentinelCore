// Solution: SentinelCore
// Project:   SentinelCore.CaseFlowEngine
// File:         CaseFlowEngine.cs
// Author: Kyle L. Crowder
// Build Num:  100908



using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

using SentinelCore.CaseFlowEngine.Infrastructure;
using SentinelCore.Contracts.CaseFlow;
using SentinelCore.Contracts.Cfe;
using SentinelCore.Data.DBEntities;





namespace SentinelCore.CaseFlowEngine.CaseFlow;





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


    int CreateCase(SignalEntity signalEntity, CancellationToken cancellationToken = default);


    /// <summary>
    ///     Creates a new case from a user request.
    /// </summary>
    /// <param name="signal">The signal that triggered the case.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created case identifier.</returns>
    Task<int> CreateCaseAsync(SignalEntity signal, CancellationToken cancellationToken = default);


    /// <summary>
    ///     Returns the statuses a case in the given status may legally transition to.
    /// </summary>
    /// <param name="status">The current status of the case.</param>
    /// <returns>A read-only list of the statuses reachable from <paramref name="status" />.</returns>
    IReadOnlyList<CaseStatus> GetAllowedTransitions(int status);













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








    Task<CaseEntity?> GetCaseByIdAsync(int caseId, CancellationToken cancellationToken = default);
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
    private static readonly Dictionary<int, HashSet<int>> AllowedTransitions = new()
    {
        [(int)CaseStatus.Open] = new HashSet<int> { (int)CaseStatus.Analysis, (int)CaseStatus.Cancelled },
        [(int)CaseStatus.Analysis] = new HashSet<int> { (int)CaseStatus.Investigation, (int)CaseStatus.AwaitingInput, (int)CaseStatus.Blocked, (int)CaseStatus.Cancelled },
        [(int)CaseStatus.Investigation] = new HashSet<int> { (int)CaseStatus.Review, (int)CaseStatus.AwaitingInput, (int)CaseStatus.Blocked, (int)CaseStatus.Escalated, (int)CaseStatus.Alerted, (int)CaseStatus.Cancelled },
        [(int)CaseStatus.Review] = new HashSet<int> { (int)CaseStatus.Complete, (int)CaseStatus.Investigation, (int)CaseStatus.AwaitingInput, (int)CaseStatus.Escalated, (int)CaseStatus.Cancelled },
        [(int)CaseStatus.AwaitingInput] = new HashSet<int> { (int)CaseStatus.Investigation, (int)CaseStatus.Escalated, (int)CaseStatus.Cancelled },
        [(int)CaseStatus.Escalated] = new HashSet<int> { (int)CaseStatus.Investigation, (int)CaseStatus.AwaitingInput, (int)CaseStatus.Blocked, (int)CaseStatus.Alerted, (int)CaseStatus.Cancelled },
        [(int)CaseStatus.Alerted] = new HashSet<int> { (int)CaseStatus.Escalated, (int)CaseStatus.Blocked, (int)CaseStatus.Cancelled },
        [(int)CaseStatus.Blocked] = new HashSet<int> { (int)CaseStatus.AwaitingInput, (int)CaseStatus.Escalated, (int)CaseStatus.Alerted, (int)CaseStatus.Cancelled },
        [(int)CaseStatus.Complete] = new HashSet<int> { (int)CaseStatus.Closed },
        [(int)CaseStatus.Cancelled] = new HashSet<int> { (int)CaseStatus.Closed },
        [(int)CaseStatus.Closed] = new HashSet<int>()
    };








    /// <summary>
    /// Initializes a new instance of the <see cref="CaseFlowEngine"/> class.
    /// </summary>
    /// <param name="dbContextFactory">
    /// A factory for creating instances of <see cref="SentinelCoreDBContext"/> on demand.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="dbContextFactory"/> is <c>null</c>.
    /// </exception>
    public CaseFlowEngine(IDbContextFactory<SentinelCoreDBContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory ?? throw new ArgumentNullException(nameof(dbContextFactory));
    }







    /// <summary>
    /// Advances a case to the specified status.
    /// </summary>
    /// <remarks>The transition must be permitted by the case lifecycle.</remarks>
    /// <param name="caseId">The identifier of the case to advance.</param>
    /// <param name="status">The status to assign to the case.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    /// <exception cref="KeyNotFoundException">The case with the specified identifier does not exist.</exception>
    /// <exception cref="CaseFlowException">The case could not be advanced to the specified status.</exception>
    public async Task AdvanceCaseAsync(int caseId, CaseStatus status, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(caseId);

        try
        {
            await using SentinelCoreDBContext db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

            // 1. Retrieve the current case
            CaseEntity entity = await db.CaseEntities.FindAsync(new object[] { caseId }, cancellationToken);

            if (entity is null)
            {
                throw new KeyNotFoundException($"Case with ID '{caseId}' not found.");
            }

            CaseStatus currentStatus = (CaseStatus)entity.CaseStatus;

            // 2. Validate the transition is allowed by the lifecycle
            ValidateTransition((int)currentStatus, (int)status);

            // 4. Apply the transition
            entity.CaseStatus = (CaseStatus)status;
            entity.UpdatedAt = DateTime.Now;

            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            // In a real scenario, we would use IErrorReporter here.
            // For now, we wrap in a domain-specific exception if it's not already one.
            throw new CaseFlowException($"Failed to advance case {caseId} to {status}.", ex);
        }
    }







    /// <summary>
    /// Creates an open case for the specified signal and returns the case identifier.
    /// </summary>
    /// <param name="signalEntity">The signal to associate with the new case.</param>
    /// <param name="cancellationToken">A token intended to cancel the operation; it is not observed.</param>
    /// <returns>The identifier of the created case.</returns>
    /// <exception cref="CaseFlowException">The case could not be created.</exception>
    public int CreateCase(SignalEntity signalEntity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(signalEntity);

        try
        {
            using SentinelCoreDBContext db = _dbContextFactory.CreateDbContext();
            using IDbContextTransaction transaction = db.Database.BeginTransaction();

            //Save the signal first so we can grab this records identifier and use it in the case.
            SignalEntity ent = signalEntity;
            db.SignalEntities!.Add(ent);
            db.SaveChanges();

            //Now the case.
            CaseEntity caseent = new() { SignalEntity = ent, CaseStatus = CaseStatus.Open };
            db.CaseEntities!.Add(caseent);
            db.SaveChanges();

            transaction.Commit();
            return caseent.Id;
        }
        catch (Exception ex)
        {
            throw new CaseFlowException("Failed to create case synchronously.", ex);
        }
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
    public async Task<int> CreateCaseAsync(SignalEntity signal, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(signal);

        try
        {
            // Single atomic SaveChanges: signal + case + FK link, all in one round-trip
            return await CreateCaseWithSignalAsync(signal, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new CaseFlowException("Failed to create case asynchronously.", ex);
        }
    }








    /// <summary>
    ///     Returns the statuses a case in the given status may legally transition to.
    /// </summary>
    /// <param name="status">The current status of the case.</param>
    /// <returns>A read-only list of the statuses reachable from <paramref name="status" />.</returns>
    public IReadOnlyList<CaseStatus> GetAllowedTransitions(int status)
    {
        return AllowedTransitions.TryGetValue(status, out HashSet<int>? allowed) ? allowed.Select(s => (CaseStatus)s).ToList() : [];
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
        return await db.CaseEntities!.Where(d => d.CaseStatus == status).CountAsync(cancellationToken);
    }








    /// <summary>
    ///     Returns the count of cases in every status in a single grouped query.
    ///     Statuses with no cases are included with a count of zero.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A read-only dictionary mapping each case status to its case count.</returns>
    public async Task<IReadOnlyDictionary<CaseStatus, int>> GetCaseStatusCountsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await using SentinelCoreDBContext db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

            if (db.CaseEntities is null)
            {
                throw new InvalidOperationException("CaseEntities DbSet is not initialized in the database context.");
            }

            Dictionary<CaseStatus, int> counts = Enum.GetValues<CaseStatus>().ToDictionary(status => status, _ => 0);

            List<StatusCount> grouped = await db.CaseEntities
                .GroupBy(c => c.CaseStatus)
                .Select(g => new StatusCount((int)g.Key, g.Count()))
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            foreach (StatusCount group in grouped)
            {
                counts[(CaseStatus)group.Status] = group.Count;
            }

            return counts;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            // Wrap in domain-specific exception for consistency with other methods in this class
            throw new CaseFlowException("Failed to retrieve case status counts.", ex);
        }
    }







    /// <summary>
    ///     Retrieves a case by its unique identifier.
    /// </summary>
    /// <param name="caseId">The case identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The matching case, or <c>null</c> if not found.</returns>
    public async Task<CaseEntity?> GetCaseByIdAsync(int caseId, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(caseId);

        try
        {
            await using SentinelCoreDBContext db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
            CaseEntity? entity = await db.CaseEntities!.FirstOrDefaultAsync(c => c.Id == caseId, cancellationToken).ConfigureAwait(false);

            return entity;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new CaseFlowException($"Failed to retrieve case {caseId} by ID.", ex);
        }
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
    private async Task<int> CreateCaseWithSignalAsync(SignalEntity signal, CancellationToken cancellationToken)
    {

        await using SentinelCoreDBContext db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        await using IDbContextTransaction transaction = await db.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);




        try
        {
            db.Attach(signal);


            var parent = new CaseEntity
            {

                SignalEntity = signal,
                CaseStatus = CaseStatus.Open,
                CreatedAt = DateTime.Now,


            };

            db.Attach(parent);
            //TEST
            // Save the parent with the child attached 
            db.CaseEntities!.Add(parent);


            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);

            Throw.IfLessThanOrEqual(parent.Id, 0, "Id");

            return parent.Id;
        }
        catch (Exception ex)
        {
            throw new CaseFlowException("Failed to create case with signal.", ex);
        }
    }


    /// <summary>
    /// Retrieves the oldest case from the queue.
    /// </summary>
    /// <param name="cancellationToken">The token to monitor for cancellation.</param>
    /// <returns>The oldest case in the queue.</returns>
    /// <exception cref="KeyNotFoundException"></exception>
    /// <exception cref="CaseFlowException">The case could not be retrieved. A <see cref="KeyNotFoundException"/> is wrapped as the inner exception when the
    /// queue contains no cases.</exception>
    private async Task<CaseEntity> PullNewCase(CancellationToken cancellationToken = default)
    {
        try
        {
            await using SentinelCoreDBContext db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
            CaseEntity? entity = await db.CaseEntities!.OrderBy(c => c.CreatedAt).FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);

            if (entity is null)
            {
                throw new KeyNotFoundException("No cases available to pull from the queue.");
            }
            return entity;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new CaseFlowException("Failed to pull a new case from the queue.", ex);
        }
    }




    /// <summary>
    ///     Validates that the transition from <paramref name="from" /> to
    ///     <paramref name="to" /> is allowed by the case lifecycle.
    ///     This ensures that the case status changes adhere to the predefined
    ///     lifecycle rules, preventing invalid state transitions.
    ///     The method checks if the current status has a set of allowed
    ///     transitions and verifies that the target status is within this set.
    /// </summary>
    private static void ValidateTransition(int from, int to)
    {
        if (!AllowedTransitions.TryGetValue(from, out HashSet<int>? allowed))
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
