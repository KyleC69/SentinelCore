// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         AudioDeviceReadTool.cs
// Author: Kyle L. Crowder
// Build Num:  080801



using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;

using SentinelCore.Tools.Interop;




namespace SentinelCore.Tools;





/// <summary>
///     Read-only tool for querying multimedia and audio device settings via the Core Audio MMDevice API.
/// </summary>
public sealed class AudioDeviceReadTool : AITool
{

    private const int DeviceStateActive = 0x00000001;
    private const int EDataFlowCapture = 1;
    private const int EDataFlowRender = 0;
    private static readonly Guid MmDeviceEnumeratorClsid = new("BCDE0395-E52F-467C-8E3D-C4579291692E");
    public override string Description { get; } = "Read-only tool for querying multimedia and audio device settings.";
    public override string Name { get; } = "Audio_Device_Read";








    private static void AppendDevicesForFlow(StringBuilder sb, IMMDeviceEnumerator enumerator, int dataFlow, string flowLabel)
    {
        int hr = enumerator.EnumAudioEndpoints(dataFlow, DeviceStateActive, out IMMDeviceCollection collection);
        if (hr < 0)
        {
            return;
        }

        using MarshalReleaseScope collectionScope = new(collection);
        collection.GetCount(out int count);
        for (int i = 0; i < count; i++)
        {
            collection.Item(i, out IMMDevice device);
            using MarshalReleaseScope deviceScope = new(device);
            device.GetId(out string id);
            device.GetState(out int state);
            sb.AppendLine($"Flow={flowLabel} Id={id} State={state}");
        }
    }








    [Description("Lists active audio playback and recording devices using the Core Audio MMDevice API.")]
    public Task<ToolResult> audio_list_devices()
    {
        try
        {
            StringBuilder sb = new();
            using SafeComObject com = new(MmDeviceEnumeratorClsid);
            if (com.Instance is not IMMDeviceEnumerator enumerator)
            {
                return Task.FromResult(ToolResult.Fail("Unable to create MMDeviceEnumerator."));
            }

            AppendDevicesForFlow(sb, enumerator, EDataFlowRender, "Render");
            AppendDevicesForFlow(sb, enumerator, EDataFlowCapture, "Capture");

            return Task.FromResult(ToolResult.Ok(sb.ToString()));
        }
        catch (Exception ex)
        {
            return Task.FromResult(ToolResult.Fail($"Audio device listing failed: {ex.Message}"));
        }
    }








    [Description("Reads the default audio playback device using the Core Audio MMDevice API.")]
    public Task<ToolResult> audio_read_default_device()
    {
        try
        {
            using SafeComObject com = new(MmDeviceEnumeratorClsid);
            if (com.Instance is not IMMDeviceEnumerator enumerator)
            {
                return Task.FromResult(ToolResult.Fail("Unable to create MMDeviceEnumerator."));
            }

            int hr = enumerator.GetDefaultAudioEndpoint(EDataFlowRender, 0 /* eConsole */, out IMMDevice device);
            if (hr < 0)
            {
                return Task.FromResult(ToolResult.Fail("No default render endpoint found."));
            }

            using MarshalReleaseScope deviceCom = new(device);
            device.GetId(out string id);
            device.GetState(out int state);
            return Task.FromResult(ToolResult.Ok($"DefaultRenderEndpoint Id={id} State={state}"));
        }
        catch (Exception ex)
        {
            return Task.FromResult(ToolResult.Fail($"Default audio device read failed: {ex.Message}"));
        }
    }








    [ComImport]
    [Guid("A95664D2-9614-4F35-A746-DE8DB63617E6")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IMMDeviceEnumerator
    {
        [PreserveSig]
        int EnumAudioEndpoints(int dataFlow, int dwStateMask, out IMMDeviceCollection ppDevices);








        [PreserveSig]
        int GetDefaultAudioEndpoint(int dataFlow, int role, out IMMDevice ppEndpoint);
    }





    [ComImport]
    [Guid("0BD7A1BE-7A1A-44DB-8397-CC5392387B5E")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IMMDeviceCollection
    {
        [PreserveSig]
        int GetCount(out int pcDevices);








        [PreserveSig]
        int Item(int nDevice, out IMMDevice ppDevice);
    }





    [ComImport]
    [Guid("D666063F-1587-4E43-81F1-B948E807363F")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IMMDevice
    {
        [PreserveSig]
        int Activate(ref Guid iid, int dwClsCtx, IntPtr pActivationParams, [MarshalAs(UnmanagedType.IUnknown)] out object ppInterface);








        [PreserveSig]
        int GetId([MarshalAs(UnmanagedType.LPWStr)] out string ppstrId);








        [PreserveSig]
        int GetState(out int pdwState);
    }





    private sealed class MarshalReleaseScope : IDisposable
    {
        private object? _obj;








        public MarshalReleaseScope(object obj)
        {
            _obj = obj;
        }








        public void Dispose()
        {
            if (_obj is not null)
            {
                Marshal.ReleaseComObject(_obj);
                _obj = null;
            }
        }
    }
}