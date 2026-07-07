// Solution: SentinelCoreLib
// Project:   SentinelCoreLib
// File:         AccessibilityReadTool.cs
// Author: Kyle L. Crowder
// Build Date: 2026/07/07



using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;

using Microsoft.Extensions.AI;
using Microsoft.Win32;

using SentinelCoreLib.Tools.Interop;




namespace SentinelCoreLib.Tools;





/// <summary>
///     Read-only tool for querying accessibility and ease-of-access settings via the UI Automation COM API.
/// </summary>
public sealed class AccessibilityReadTool : AITool
{
    private const string AccessibilityKey = "Control Panel\\Accessibility";
    private const int UIA_ControlTypePropertyId = 30003;

    private const int UIA_NamePropertyId = 30005;
    private static readonly Guid CuiAutomationClsid = new("FF48DBDA-A5CA-44D2-830A-E3AFC40F06DE");








    [Description("Reads specific ease-of-access feature configuration such as high contrast or sticky keys.")]
    public Task<ToolResult> accessibility_read_feature([Description("The feature subkey name, e.g. HighContrast, StickyKeys, ToggleKeys, MouseKeys.")] string featureName)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(featureName))
            {
                return Task.FromResult(ToolResult.FailureResult("featureName is required."));
            }

            string keyPath = $"{AccessibilityKey}\\{featureName}";
            using RegistryKey? key = Registry.CurrentUser.OpenSubKey(keyPath, writable: false);
            if (key is null)
            {
                return Task.FromResult(ToolResult.FailureResult($"Accessibility feature key not found: {keyPath}"));
            }

            StringBuilder sb = new();
            sb.AppendLine($"[{keyPath}]");
            foreach (string valueName in key.GetValueNames()) sb.AppendLine($"  {valueName}={key.GetValue(valueName)}");

            return Task.FromResult(ToolResult.SuccessResult(sb.ToString()));
        }
        catch (Exception ex)
        {
            return Task.FromResult(ToolResult.FailureResult($"Accessibility feature read failed: {ex.Message}"));
        }
    }








    [Description("Reads accessibility settings from the registry.")]
    public Task<ToolResult> accessibility_read_settings()
    {
        try
        {
            StringBuilder sb = new();
            using RegistryKey? key = Registry.CurrentUser.OpenSubKey(AccessibilityKey, writable: false);
            if (key is null)
            {
                return Task.FromResult(ToolResult.FailureResult($"Accessibility registry key not found: {AccessibilityKey}"));
            }

            sb.AppendLine($"[{AccessibilityKey}]");
            foreach (string valueName in key.GetValueNames()) sb.AppendLine($"  {valueName}={key.GetValue(valueName)}");

            return Task.FromResult(ToolResult.SuccessResult(sb.ToString()));
        }
        catch (Exception ex)
        {
            return Task.FromResult(ToolResult.FailureResult($"Accessibility settings read failed: {ex.Message}"));
        }
    }








    [Description("Reads the root UI Automation element name and control type to confirm the UI Automation API is reachable.")]
    public Task<ToolResult> accessibility_read_uia_root()
    {
        try
        {
            using SafeComObject com = new(CuiAutomationClsid);
            if (com.Instance is not IUIAutomation automation)
            {
                return Task.FromResult(ToolResult.FailureResult("Unable to create CUIAutomation."));
            }

            int hr = automation.GetRootElement(out var root);
            if (hr < 0 || root is null)
            {
                return Task.FromResult(ToolResult.FailureResult("Could not retrieve UI Automation root element."));
            }

            using MarshalReleaseScope rootScope = new(root);
            root.GetCurrentPropertyValue(UIA_NamePropertyId, out var nameValue);
            root.GetCurrentPropertyValue(UIA_ControlTypePropertyId, out var controlTypeValue);
            string name = nameValue?.ToString() ?? string.Empty;
            string controlType = controlTypeValue?.ToString() ?? string.Empty;

            return Task.FromResult(ToolResult.SuccessResult($"RootName={name} ControlType={controlType}"));
        }
        catch (Exception ex)
        {
            return Task.FromResult(ToolResult.FailureResult($"UI Automation root read failed: {ex.Message}"));
        }
    }








    [ComImport]
    [Guid("30CBE57D-D9D0-452B-AB13-7AC5AC4825EE"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IUIAutomation
    {
        [PreserveSig]
        int GetRootElement(out IUIAutomationElement root);








        [PreserveSig]
        int CreatePropertyCondition(int propertyId, object value, out IUIAutomationCondition condition);
    }





    [ComImport]
    [Guid("D22108AA-8AC5-49A5-837B-37BBB3D7591E"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IUIAutomationElement
    {
        [PreserveSig]
        int GetCurrentPropertyValue(int propertyId, out object value);








        [PreserveSig]
        int GetRuntimeId(out object runtimeId);








        // IUnknown v-table placeholders for unused methods.
        void Get_BoundingRectangle();


        void Get_LabeledBy();


        void Get_AriaRole();


        void Get_AriaProperties();


        void Get_ProviderDescription();


        void Get_ClickablePoint();
    }





    [ComImport]
    [Guid("3526BAE7-0970-431C-9204-4B49373050AF"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IUIAutomationCondition
    {
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