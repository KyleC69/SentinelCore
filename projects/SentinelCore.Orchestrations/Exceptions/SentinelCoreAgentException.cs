// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         SentinelCoreAgentException.cs
// Author: Kyle L. Crowder
// Build Num:  100919



namespace SentinelCore.Orchestrations.Exceptions;





public sealed class SentinelCoreAgentException : Exception
{
    public SentinelCoreAgentException(string message, Exception? innerException = null) : base(ValidateMessage(message), innerException)
    {
        HResult = GenerateHResult(); // Assign a meaningful HResult for categorization
    }








    private static int GenerateHResult()
    {
        // Generate or assign a specific HResult value for this exception type
        return unchecked((int)0x80131500); // Example: COR_E_APPLICATION
    }








    private static string ValidateMessage(string message)
    {
        return string.IsNullOrWhiteSpace(message) ? "A SentinelCore agent error occurred. Please provide additional context for debugging." : message;
    }
}