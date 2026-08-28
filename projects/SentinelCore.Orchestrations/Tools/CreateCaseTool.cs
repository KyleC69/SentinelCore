// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         CreateCaseTool.cs
// Author: Kyle L. Crowder
// Build Num:  082808



using System.ComponentModel;

using SentinelCore.Cfe;
using SentinelCore.Cfe.Persistence;
using SentinelCore.Infrastructure.Persistence;




namespace SentinelCore.Tools;





public class CaseTool : AITool
{

    private readonly ICaseFlowEngine _engine = new CaseFlowEngine(new SentinelCoreDBContext());

    public override string Description { get; } = "A tool for creating a new case in the Sentinel Core platform from the provided signal. " + "The signal should be a string that describes the issue or anomaly that needs to be investigated.";

    public override string Name { get; } = "CreateCase";








    [Description("This is an AITool for creating new investigative cases for the Sentinel Core platform.")]
    public ToolResult CreateCase([Description("The description of the signal in natural language.")] string signal)
    {
        Signal rawSignal = new(signal, "CaseGen");

        try
        {
            Guid caseId = _engine.CreateCase(rawSignal);
            if (caseId == Guid.Empty)
            {
                return ToolResult.Fail("Failed to create case. The case ID returned was empty.");
            }

            return ToolResult.Ok(caseId.ToString());
        }
        catch (Exception e)
        {
            return ToolResult.Fail($"Failed to create case. Exception: {e.Message}");
        }


    }
}