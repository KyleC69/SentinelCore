// Solution: SentinelCoreLib
// Project:   SentinelCoreLib
// File:         InvestigationControl.cs
// Author: Kyle L. Crowder
// Build Date: 2026/07/07



using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

using SentinelCoreLib.Agents.Core;
using SentinelCoreLib.Application.Abstractions;
using SentinelCoreLib.Orchestration;




namespace SentinelCoreLib.Application;





public class InvestigationControl
{
    private readonly AIAgent _coreAgent;
    private readonly CoreAgentFactory _coreAgentFactory;
    private AgentSession? agentSession;
    private TheCoreOrchestration orchestration;








    public InvestigationControl(CoreAgentFactory coreAgent, TheCoreOrchestration orch)
    {


        _coreAgentFactory = coreAgent;
        _coreAgent = _coreAgentFactory.Create();
        orchestration = orch;


    }








    //Method not fully implemented yet, but it will be used to start the case orchestration process.
    public async Task StartCaseOrchestration(ChatMessage promptSignal)
    {
        try
        {


            //TODO: Add CFE Case Flow Engine Orchestration initiation logic here.
            CaseRecord theActiveCase = new(promptSignal.Text);



            await orchestration.InitiateAsync(theActiveCase);

        }
        catch (Exception)
        {

            throw new SentinelCorePlatformException("Fatal error during Initialization");
        }
    }
}