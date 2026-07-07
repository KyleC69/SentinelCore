// Solution: SentinelCoreLib
// Project:   SentinelCoreLib
// File:         TheCoreOrchestration.cs
// Author: Kyle L. Crowder
// Build Date: 2026/07/07



using System.Text.Json;

using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using SentinelCore.Contracts;

using SentinelCoreLib.Agents.Core;
using SentinelCoreLib.Application.Abstractions;
using SentinelCoreLib.Contracts;

using CaseStatus = SentinelCoreLib.Application.Abstractions.Persistence.CaseStatus;




namespace SentinelCoreLib.Orchestration;





/// <summary>
///     Represents the orchestration layer involving The Core AI agent and the Magnetic Workflow sub-workflow.
/// </summary>
/// <remarks>
///     This class is responsible for creating and managing core agents, as well as building workflows.
///     It integrates with various components such as <see cref="CoreAgentFactory" /> and <see cref="AIAgent" />.
/// </remarks>
public sealed class TheCoreOrchestration
{
    private readonly ILogger<TheCoreOrchestration> _logger;

    private MagneticOrchestration _magneticOrchestration;
    public CoreAgentFactory Core;
    public AIAgent TheCore;








    public TheCoreOrchestration(IOptions<SentinelCoreSettings> options, MagneticOrchestration magneticOrchestration, ILoggerFactory loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(loggerFactory);

        _logger = loggerFactory.CreateLogger<TheCoreOrchestration>();
        Core = new(options, loggerFactory);
        _magneticOrchestration = magneticOrchestration;
        TheCore = Core.Create();

    }








    private string BuildExecutionPrompt(string signal, string caseId)
    {
        return $$"""

                 You need to create a list of atomic evidence gathering actions to be executed in to identify the root cause of the signal below. Each action must be an EvidenceOperation, which is a machine-oriented action that gathers raw data from system surfaces.
                 EvidenceOperations NEVER include reasoning, interpretation, remediation, or human-style troubleshooting. They exist solely to collect evidence for later analysis.

                 Output must be in valid JSON matching this schema and the only output of your response must be valid JSON, no other text or commentary is allowed. The schema is as follows:

                     "CaseId": {{caseId}},
                         "EvidenceOperations": [
                         {
                             "OperationId": "string",
                             "Surface": "string",
                             "Instruction": "string",
                             "Result": "string"
                         }
                         ]

                 The user has provided the following signal or security event for investigation: {{signal}}
                 """;
    }








    /// <summary>
    ///     Initiates the orchestration process for a given case.
    /// </summary>
    /// <param name="theActiveCase">
    ///     The active case to be processed, containing details such as the initiating prompt and case metadata.
    /// </param>
    /// <returns>
    ///     A task that represents the asynchronous operation of initiating the orchestration process.
    /// </returns>
    /// <remarks>
    ///     This method creates an AI agent session, generates an investigation plan based on the provided case,
    ///     and executes the Magnetic Workflow using the generated plan. It updates the case status and prepares
    ///     it for the next state in the workflow.
    /// </remarks>
    /// <exception cref="ArgumentNullException">
    ///     Thrown if <paramref name="theActiveCase" /> is null.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    ///     Thrown if the response from the AI agent is not of type <see cref="InvestigationPlan" />.
    /// </exception>
    public async Task InitiateAsync(CaseRecord theActiveCase)
    {
        _logger.LogInformation("Begin InitiateAsync for case {CaseId}", theActiveCase.CaseId);


        ChatMessage msg = new(ChatRole.User, BuildExecutionPrompt(theActiveCase.InitiatingPrompt, theActiveCase.CaseId));

        JsonSerializerOptions jOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, WriteIndented = true, PropertyNameCaseInsensitive = true };

        AgentSession agentSession = await TheCore.CreateSessionAsync();
        //Create the plan
        AgentResponse response = await TheCore.RunAsync(msg, agentSession, null, CancellationToken.None);

        EvidenceActions? evidenceActions = JsonSerializer.Deserialize<EvidenceActions>(response.Text, jOptions);
        InvestigationPlan plan = evidenceActions?.ToInvestigationPlan(evidenceActions) ?? new InvestigationPlan { CaseId = theActiveCase.CaseId, Steps = new List<InvestigationPlanStep>() };






        //Use the plan to execute the Magnetic Workflow and get results
        await _magneticOrchestration.ExecuteTasksAsync(plan.Steps.ToList(), theActiveCase.CaseId, CancellationToken.None);




        // Update the case with the plan results and transition to the next state
        theActiveCase.Update(CaseStatus.Open);
        //TODO: Persist updatedCase to ICaseRepository
        _logger.LogInformation("End InitiateAsync for case {CaseId}", theActiveCase.CaseId);
    }
}