// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         CaseEngineTools.cs
// Author: Kyle L. Crowder
// Build Num:  111504



using System.ComponentModel;

using SentinelCore.CaseFlowEngine.CaseFlow;
using SentinelCore.CaseFlowEngine.Persistence;
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








    [Description("This is an AITool for creating new investigative cases for the Sentinel Core platform.")]
    public ToolResult CreateCase([Description("The description of the signal in natural language.")] string signal)
    {
        Signal rawSignal = new(signal, "CaseGen");

        try
        {
            int caseId = _engine.CreateCase(rawSignal.ToEntity());


            return ToolResult.Ok(caseId.ToString());
        }
        catch (Exception e)
        {
            return ToolResult.Fail($"Failed to create case. Exception: {e.Message}");
        }


    }








    [Description("Inserts the summaries extracted from the final report")]
    public Task<ToolResult> InsertPatternSummaries([Description("The case id to insert into")] Guid caseID, [Description("Extracted report info wrapped in ReportInfoDto")] ReportInfoDto finalReport)
    {
        //Inserts final report into database
        try
        {




        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);

        }


        return Task.FromResult(ToolResult.Ok("No results"));
    }








    public Task<ToolResult> InsertReport(Guid caseID, string finalReport)
    {
        //Inserts final report into database



        return Task.FromResult(ToolResult.Ok("No results"));
    }








    public class ReportInfoDto
    {
        public string ConclusionSummary { get; set; } = string.Empty;
        public string FindingsSummary { get; set; } = string.Empty;

        public string Hypothesis { get; set; } = string.Empty;
    }
}