// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         SafeComObject.cs
// Author: Kyle L. Crowder
// Build Num:  081312



using System.Runtime.InteropServices;




namespace SentinelCore.Tools.Interop;





/// <summary>
///     Disposable wrapper for a COM object obtained through Type.GetTypeFromCLSID / Activator.CreateInstance.
///     Ensures deterministic release without weakening the read-only safety contract.
/// </summary>
internal sealed class SafeComObject : IDisposable
{
    public SafeComObject(Guid clsid)
    {
        Type? type = Type.GetTypeFromCLSID(clsid);
        if (type is not null)
        {
            Instance = Activator.CreateInstance(type);
        }
    }








    public object? Instance { get; private set; }








    public void Dispose()
    {
        if (Instance is not null)
        {
            Marshal.ReleaseComObject(Instance);
            Instance = null;
        }
    }
}