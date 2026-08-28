// Solution: SentinelCore
// Project:   SentinelCoreAdmin.Core
// File:         StreamExtensions.cs
// Author: Kyle L. Crowder
// Build Num:  082808



namespace SentinelCoreAdmin.Core.Helpers;





public static class StreamExtensions
{
    public static string ToBase64String(this Stream stream)
    {
        using (MemoryStream memoryStream = new())
        {
            stream.CopyTo(memoryStream);
            return Convert.ToBase64String(memoryStream.ToArray());
        }
    }
}