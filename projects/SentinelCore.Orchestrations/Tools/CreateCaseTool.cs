// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         CreateCaseTool.cs
// Author: Kyle L. Crowder
// Build Num:  081312



using System.ComponentModel;

using SentinelCore.CaseEngine;
using SentinelCore.CaseFlow;




namespace SentinelCore.Tools;





public class CreateCaseTool : AITool
{

    private readonly ICaseFlowEngine _engine;








    public CreateCaseTool(ICaseFlowEngine engine)
    {
        _engine = engine ?? throw new ArgumentNullException(nameof(engine));
    }








    public override string Description { get; } = "A tool for creating a new case in the Sentinel Core platform from the provided signal. " + "The signal should be a string that describes the issue or anomaly that needs to be investigated.";

    public override string Name { get; } = "Create_Case";








    [Description("This is an AI tool for creating new investigative cases for the Sentinel Core platform.")]
    public async Task<ToolResult> ExecuteAsync([Description("The description of the signal in natural language.")] string signal)
    {
        Signal rawSignal = new(signal, "CaseGen");

        Guid caseId = await _engine.CreateCaseAsync(rawSignal);
        if (caseId == Guid.Empty)
        {
            return ToolResult.Fail("Failed to create case. The case ID returned was empty.");
        }

        return ToolResult.Ok(caseId.ToString());
    }
}