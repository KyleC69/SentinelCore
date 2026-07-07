// Solution: SentinelCoreLib
// Project:   SentinelCoreLib
// File:         ICaseFlowEngine.cs
// Author: Kyle L. Crowder
// Build Date: 2026/07/07



using SentinelCoreLib.Application.Abstractions;
using SentinelCoreLib.Application.Abstractions.Persistence;
using SentinelCoreLib.Domain.Contracts;




namespace SentinelCoreLib.CaseFlow;





/// <summary>
///     Owns the deterministic lifecycle of a case.
/// </summary>
public interface ICaseFlowEngine
{

    /// <summary>
    ///     Aborts a case.
    /// </summary>
    /// <param name="caseId">The case identifier.</param>
    /// <param name="reason">The abort reason.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task AbortCaseAsync(CaseId caseId, string reason, CancellationToken cancellationToken = default);








    /// <summary>
    ///     Records evidence against a case.
    /// </summary>
    /// <param name="caseId">The case identifier.</param>
    /// <param name="item">The evidence item.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task AddEvidenceAsync(CaseId caseId, EvidenceItem item, CancellationToken cancellationToken = default);








    /// <summary>
    ///     Advances a case to the specified state.
    /// </summary>
    /// <param name="caseId">The case identifier.</param>
    /// <param name="status">The target status.</param>
    /// <param name="stateJson">Optional JSON state payload.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task AdvanceCaseAsync(CaseId caseId, CaseStatus status, string? stateJson = null, CancellationToken cancellationToken = default);








    /// <summary>
    ///     Creates a new case from a user request.
    /// </summary>
    /// <param name="title">The case title.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created case identifier.</returns>
    Task<CaseId> CreateCaseAsync(string title, CancellationToken cancellationToken = default);








    /// <summary>
    ///     Evaluates a user message for safety and emits an investigation work item for the Core agent.
    ///     The CFE does not run agents directly; it returns a work item that the orchestrator executes.
    /// </summary>
    /// <param name="caseId">The case identifier.</param>
    /// <param name="message">The user message.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A work item for the orchestrator, or a blocked result if safety rules reject the message.</returns>
    Task<InvestigationWorkItem> EmitInvestigationWorkAsync(CaseId caseId, string message, CancellationToken cancellationToken = default);
}