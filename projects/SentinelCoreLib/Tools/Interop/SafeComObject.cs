// Solution: SentinelCoreLib
// Project:   SentinelCoreLib
// File:         SafeComObject.cs
// Author: Kyle L. Crowder
// Build Date: 2026/07/07



using System.Runtime.InteropServices;




namespace SentinelCoreLib.Tools.Interop;





/// <summary>
///     Disposable wrapper for a COM object obtained through Type.GetTypeFromCLSID / Activator.CreateInstance.
///     Ensures deterministic release without weakening the read-only safety contract.
/// </summary>
internal sealed class SafeComObject : IDisposable
{
    private object? _instance;








    public SafeComObject(Guid clsid)
    {
        Type? type = Type.GetTypeFromCLSID(clsid);
        if (type is not null)
        {
            _instance = Activator.CreateInstance(type);
        }
    }








    public object? Instance
    {
        get => _instance;
    }








    public void Dispose()
    {
        if (_instance is not null)
        {
            Marshal.ReleaseComObject(_instance);
            _instance = null;
        }
    }
}