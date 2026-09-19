// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         HumanOperatorExecutor.cs
// Author: Kyle L. Crowder
// Build Num:  091418

namespace SentinelCore.Orchestrations.Workflows.Executors;





public partial class HumanOperatorExecutor() : Executor("HumanOperator")
{










    /// <summary>
    /// Handles the escalation of a signal to a human operator.
    /// </summary>
    /// <param name="input">The signal hypothesis containing information about the signal.</param>
    /// <param name="context">The workflow context providing state and execution information.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A ValueTask representing the asynchronous operation.</returns>
    [MessageHandler]
    public async ValueTask HandleEscalationAsync(SignalHypothesis input, IWorkflowContext context, CancellationToken cancellationToken = new())
    {
        await Task.Delay(300);
        throw new NotImplementedException();
    }



}

