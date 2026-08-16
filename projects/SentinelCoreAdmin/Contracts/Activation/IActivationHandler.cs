// Solution: SentinelCore
// Project:   SentinelCoreAdmin
// File:         IActivationHandler.cs
// Author: Kyle L. Crowder
// Build Num:  081602



namespace SentinelCoreAdmin.Contracts.Activation;





public interface IActivationHandler
{
    bool CanHandle();


    Task HandleAsync();
}