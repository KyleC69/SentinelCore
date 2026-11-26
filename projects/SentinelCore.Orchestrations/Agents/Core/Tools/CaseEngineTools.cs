// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         CaseEngineTools.cs
// Author: Kyle L. Crowder
// Build Num:  112518



using System.ComponentModel;

using SentinelCore.CaseFlowEngine.CaseFlow;
using SentinelCore.Contracts.CaseFlow;




namespace SentinelCore.Orchestrations.Agents.Core.Tools;





/// <summary>
///     In-App CaseEngine tools for agents
/// </summary>
public class CaseEngineTools


{

    private readonly ICaseFlowEngine _engine;








    public CaseEngineTools(ICaseFlowEngine engine)
    {
        _engine = engine ?? throw new ArgumentNullException(nameof(engine));
    }








    /// <summary>
    ///     Creates a new investigative case for the Sentinel Core platform.
    /// </summary>
    /// <param name="signal">The description of the signal in natural language.</param>
    /// <returns>A result whose message is the new case id, or a failure result.</returns>
    [Description("This is an AITool for creating new investigative cases for the Sentinel Core platform.")]
    public ToolResult CreateCase([Description("The description of the signal in natural language.")] string signal)
    {
        Signal rawSignal = new(signal, "CaseGen");

        try
        {
            int caseId = _engine.CreateCase(rawSignal);


            return ToolResult.Ok(caseId.ToString());
        }
        catch (Exception e)
        {
            return ToolResult.Fail($"Failed to create case. Exception: {e.Message}");
        }


    }








    /// <summary>
    ///     Inserts the summaries extracted from the final report into the database.
    /// </summary>
    /// <param name="caseID">The case id to insert into.</param>
    /// <param name="finalReport">The extracted report information.</param>
    /// <returns>A result describing the outcome of the insert.</returns>
    [Description("Inserts the summaries extracted from the final report")]
    public Task<ToolResult> InsertPatternSummaries([Description("The case id to insert into")] Guid caseID, [Description("Extracted report info wrapped in ReportInfoDto")] ReportInfoDto finalReport)
    {
        // Persistence for extracted summaries is not implemented yet; the tool reports the same
        // no-op result as InsertReport so that an agent can call it without failing.
        return Task.FromResult(ToolResult.Ok("No results"));
    }








    /// <summary>
    ///     Inserts the final investigation report for a case into the database.
    /// </summary>
    /// <param name="caseID">The case id to insert into.</param>
    /// <param name="finalReport">The final investigation report text.</param>
    /// <returns>A result describing the outcome of the insert.</returns>
    [Description("Inserts the final investigation report for a case")]
    public Task<ToolResult> InsertReport([Description("The case id to insert into")] Guid caseID, [Description("The final investigation report text")] string finalReport)
    {
        // Persistence for the final report is not implemented yet; the tool reports a no-op result
        // so that an agent can call it without failing.
        return Task.FromResult(ToolResult.Ok("No results"));
    }








    /// <summary>
    ///     Carries the summaries extracted from an investigation report so that an agent can pass them to
    ///     <see cref="InsertPatternSummaries" />.
    /// </summary>
    public class ReportInfoDto
    {
        /// <summary>
        ///     Gets or sets the conclusion summary of the investigation.
        /// </summary>
        public string ConclusionSummary { get; set; } = string.Empty;

        /// <summary>
        ///     Gets or sets the findings summary of the investigation.
        /// </summary>
        public string FindingsSummary { get; set; } = string.Empty;

        /// <summary>
        ///     Gets or sets the hypothesis the investigation confirmed or rejected.
        /// </summary>
        public string Hypothesis { get; set; } = string.Empty;
    }
}