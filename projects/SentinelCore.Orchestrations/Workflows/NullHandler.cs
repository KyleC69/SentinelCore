// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         NullHandler.cs
// Author: Kyle L. Crowder
// Build Num:  091200



namespace SentinelCore.Orchestrations.Workflows;





internal class NullHandler : Executor
{
    public NullHandler(string id, ExecutorOptions? options = null, bool declareCrossRunShareable = false) : base(id, options, declareCrossRunShareable)
    {
    }








    protected override ProtocolBuilder ConfigureProtocol(ProtocolBuilder protocolBuilder)
    {
        throw new NotImplementedException();
    }
}