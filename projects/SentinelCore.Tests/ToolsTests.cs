// Solution: SentinelCoreLib
// Project:   SentinelCore.Tests
// File:         ToolsTests.cs
// Author: Kyle L. Crowder
// Build Date: 2026/07/07



using System.Reflection;

using Microsoft.Extensions.AI;

using SentinelCoreLib.Tools;

using DescriptionAttribute = System.ComponentModel.DescriptionAttribute;




namespace SentinelCore.Tests;





/// <summary>
///     Contract and smoke tests for every deterministic read tool in SentinelCoreLib.Tools.
///     These tests validate instantiation, AIFunction discovery, and that each public operation
///     returns a well-formed <see cref="ToolResult" /> for both success and failure paths.
/// </summary>
[TestClass]
public sealed class ToolsTests
{
    private static IEnumerable<AITool> AllTools
    {
        get =>
        [
                new AccessibilityReadTool(),
                new AppLockerReadTool(),
                new AudioDeviceReadTool(),
                new BootConfigurationReadTool(),
                new CertificateStoreReadTool(),
                new DcomReadTool(),
                new EnvironmentVariablesReadTool(),
                new EventLogReadTool(),
                new FileSystemReadTool(),
                new FirewallReadTool(),
                new GroupPolicyReadTool(),
                new HyperVReadTool(),
                new LocalAccountsReadTool(),
                new NetworkReadTool(),
                new PnpDeviceReadTool(),
                new PowerSettingsReadTool(),
                new PrinterReadTool(),
                new RegistryReadTool(),
                new RemoteDesktopReadTool(),
                new ScheduledTaskReadTool(),
                new SearchIndexingReadTool(),
                new ShellExplorerReadTool(),
                new WindowsServiceReadTool(),
                new WindowsUpdateReadTool(),
                new WmiQueryTool()
        ];
    }








    [TestMethod]
    public void AccessibilityReadTool_ListsSettings()
    {
        AccessibilityReadTool tool = new AccessibilityReadTool();
        ToolResult result = Invoke(tool, "accessibility_read_settings");
        Assert.IsNotNull(result);
        Assert.IsTrue(result.Success, result.FailReason);
    }








    [TestMethod]
    public void AllToolsExposeAtLeastOneFunction()
    {
        foreach (AITool tool in AllTools)
        {
            IEnumerable<MethodInfo> functions = EnumerateFunctions(tool);
            Assert.IsTrue(functions.Any(), $"{tool.GetType().Name} should expose at least one tool method.");
        }
    }








    [TestMethod]
    public void AppLockerReadTool_ListsRuleCollections()
    {
        AppLockerReadTool tool = new AppLockerReadTool();
        ToolResult result = Invoke(tool, "applocker_list_rule_collections");
        Assert.IsNotNull(result);
        Assert.IsFalse(string.IsNullOrWhiteSpace(result.FailReason) && !result.Success, "AppLocker tool failed without a reason.");
    }








    [TestMethod]
    public void AudioDeviceReadTool_ListsDevices()
    {
        AudioDeviceReadTool tool = new AudioDeviceReadTool();
        ToolResult result = Invoke(tool, "audio_list_devices");
        Assert.IsNotNull(result);
        Assert.IsFalse(string.IsNullOrWhiteSpace(result.FailReason) && !result.Success, "Audio device tool failed without a reason.");
    }








    [TestMethod]
    public void BootConfigurationReadTool_ListsEntries()
    {
        BootConfigurationReadTool tool = new BootConfigurationReadTool();
        ToolResult result = Invoke(tool, "bcdedit_enum");
        Assert.IsNotNull(result);
        Assert.IsFalse(string.IsNullOrWhiteSpace(result.FailReason) && !result.Success, "Boot configuration tool failed without a reason.");
    }








    [TestMethod]
    public void CertificateStoreReadTool_InvalidStoreFailsGracefully()
    {
        CertificateStoreReadTool tool = new CertificateStoreReadTool();
        ToolResult result = Invoke(tool, "certificate_list", "__nonexistent__");
        Assert.IsNotNull(result);
        Assert.IsFalse(result.Success);
        Assert.IsFalse(string.IsNullOrWhiteSpace(result.FailReason));
    }








    [TestMethod]
    public void CertificateStoreReadTool_ListsCertificates()
    {
        CertificateStoreReadTool tool = new CertificateStoreReadTool();
        ToolResult result = Invoke(tool, "certificate_list", "Root");
        Assert.IsNotNull(result);
        Assert.IsTrue(result.Success, result.FailReason);
    }








    [TestMethod]
    public void DcomReadTool_ListsApplications()
    {
        DcomReadTool tool = new DcomReadTool();
        ToolResult result = Invoke(tool, "dcom_list_applications");
        Assert.IsNotNull(result);
        Assert.IsTrue(result.Success, result.FailReason);
    }








    private static IEnumerable<MethodInfo> EnumerateFunctions(AITool tool)
    {
        return tool.GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly).Where(m => m.GetCustomAttribute<System.ComponentModel.DescriptionAttribute>() is not null && m.ReturnType == typeof(Task<ToolResult>));
    }








    [TestMethod]
    public void EnvironmentVariablesReadTool_ListsEnvironment()
    {
        EnvironmentVariablesReadTool tool = new EnvironmentVariablesReadTool();
        ToolResult result = Invoke(tool, "environment_list");
        Assert.IsNotNull(result);
        Assert.IsFalse(string.IsNullOrWhiteSpace(result.FailReason) && !result.Success, "Environment variable tool failed without a reason.");
        if (result.Success && !string.IsNullOrWhiteSpace(result.Results))
        {
            StringAssert.Contains(result.Results, "Path");
        }
    }








    [TestMethod]
    public void EnvironmentVariablesReadTool_ReadsSpecificVariable()
    {
        EnvironmentVariablesReadTool tool = new EnvironmentVariablesReadTool();
        ToolResult result = Invoke(tool, "environment_read_value", "SystemRoot");
        Assert.IsNotNull(result);
        Assert.IsFalse(string.IsNullOrWhiteSpace(result.FailReason) && !result.Success, "Environment variable tool failed without a reason.");
        if (result.Success && !string.IsNullOrWhiteSpace(result.Results))
        {
            StringAssert.Contains(result.Results, "C:\\", StringComparison.OrdinalIgnoreCase);
        }
    }








    [TestMethod]
    public void EventLogReadTool_ListsLogChannels()
    {
        EventLogReadTool tool = new EventLogReadTool();
        ToolResult result = Invoke(tool, "event_log_list_channels");
        Assert.IsNotNull(result);
        Assert.IsTrue(result.Success, result.FailReason);
    }








    [TestMethod]
    public void FileSystemReadTool_ListsTempDirectory()
    {
        FileSystemReadTool tool = new FileSystemReadTool();
        ToolResult result = Invoke(tool, "file_system_list_directory", Path.GetTempPath());
        Assert.IsNotNull(result);
        Assert.IsTrue(result.Success, result.FailReason);
    }








    [TestMethod]
    public void FileSystemReadTool_MissingDirectoryFails()
    {
        FileSystemReadTool tool = new FileSystemReadTool();
        ToolResult result = Invoke(tool, "file_system_list_directory", "C:\\__sentinel_missing_dir__");
        Assert.IsNotNull(result);
        Assert.IsFalse(result.Success);
        Assert.IsFalse(string.IsNullOrWhiteSpace(result.FailReason));
    }








    [TestMethod]
    public void FirewallReadTool_ListsRules()
    {
        FirewallReadTool tool = new FirewallReadTool();
        ToolResult result = Invoke(tool, "firewall_list_rules");
        Assert.IsNotNull(result);
        Assert.IsFalse(string.IsNullOrWhiteSpace(result.FailReason) && !result.Success, "Firewall tool failed without a reason.");
    }








    [TestMethod]
    public void GroupPolicyReadTool_ListsResults()
    {
        GroupPolicyReadTool tool = new GroupPolicyReadTool();
        ToolResult result = Invoke(tool, "group_policy_list");
        Assert.IsNotNull(result);
        Assert.IsFalse(string.IsNullOrWhiteSpace(result.FailReason) && !result.Success, "GroupPolicy tool failed without a reason.");
    }








    [TestMethod]
    public void HyperVReadTool_ListsVirtualMachines()
    {
        HyperVReadTool tool = new HyperVReadTool();
        ToolResult result = Invoke(tool, "hyperv_list_vms");
        Assert.IsNotNull(result);
        Assert.IsFalse(string.IsNullOrWhiteSpace(result.FailReason) && !result.Success, "HyperV tool failed without a reason.");
    }








    /// <summary>
    ///     Invokes a public method on an AITool by name. Matches by exact method name first,
    ///     then falls back to a DescriptionAttribute text match. Optional positional arguments
    ///     are passed to the method in order.
    /// </summary>
    private static ToolResult Invoke(AITool tool, string functionName, params object?[] args)
    {
        List<MethodInfo> methods = tool.GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly).Where(m => m.GetCustomAttributes(typeof(System.ComponentModel.DescriptionAttribute), inherit: false).Any()).ToList();

        MethodInfo? method = methods.FirstOrDefault(m => m.Name.Equals(functionName, StringComparison.OrdinalIgnoreCase));

        if (method is null)
        {
            method = methods.FirstOrDefault(m =>
            {
                DescriptionAttribute? attr = m.GetCustomAttribute<System.ComponentModel.DescriptionAttribute>();
                return attr?.Description.Contains(functionName, StringComparison.OrdinalIgnoreCase) == true;
            });
        }

        Assert.IsNotNull(method, $"No public method found on {tool.GetType().Name} for function '{functionName}'.");

        ParameterInfo[] parameters = method.GetParameters();
        object?[] invokeArgs = new object?[parameters.Length];
        for (int i = 0; i < parameters.Length; i++)
            if (i < args.Length)
            {
                invokeArgs[i] = args[i];
            }
            else if (parameters[i].HasDefaultValue)
            {
                invokeArgs[i] = parameters[i].DefaultValue;
            }
            else
            {
                invokeArgs[i] = null;
            }

        Task<ToolResult>? task = method.Invoke(tool, invokeArgs) as Task<ToolResult>;
        Assert.IsNotNull(task, $"Method {method.Name} on {tool.GetType().Name} did not return Task<ToolResult>.");
        return task.GetAwaiter().GetResult();
    }








    [TestMethod]
    public void LocalAccountsReadTool_ListsUsers()
    {
        LocalAccountsReadTool tool = new LocalAccountsReadTool();
        ToolResult result = Invoke(tool, "local_user_list");
        Assert.IsNotNull(result);
        Assert.IsTrue(result.Success, result.FailReason);
    }








    [TestMethod]
    public void NetworkReadTool_ListsInterfaces()
    {
        NetworkReadTool tool = new NetworkReadTool();
        ToolResult result = Invoke(tool, "network_list_interfaces");
        Assert.IsNotNull(result);
        Assert.IsTrue(result.Success, result.FailReason);
    }








    [TestMethod]
    public void PnpDeviceReadTool_ListsDevices()
    {
        PnpDeviceReadTool tool = new PnpDeviceReadTool();
        ToolResult result = Invoke(tool, "pnp_list_devices");
        Assert.IsNotNull(result);
        Assert.IsTrue(result.Success, result.FailReason);
    }








    [TestMethod]
    public void PowerSettingsReadTool_ListsPlans()
    {
        PowerSettingsReadTool tool = new PowerSettingsReadTool();
        ToolResult result = Invoke(tool, "power_list_plans");
        Assert.IsNotNull(result);
        Assert.IsFalse(string.IsNullOrWhiteSpace(result.FailReason) && !result.Success, "Power settings tool failed without a reason.");
    }








    [TestMethod]
    public void PrinterReadTool_ListsPrinters()
    {
        PrinterReadTool tool = new PrinterReadTool();
        ToolResult result = Invoke(tool, "printer_list");
        Assert.IsNotNull(result);
        Assert.IsFalse(string.IsNullOrWhiteSpace(result.FailReason) && !result.Success, "Printer tool failed without a reason.");
    }








    [TestMethod]
    public void RegistryReadTool_MissingKeyFails()
    {
        RegistryReadTool tool = new RegistryReadTool();
        ToolResult result = Invoke(tool, "registry_list_key", "HKLM", "SOFTWARE\\__sentinel_missing_key__");
        Assert.IsNotNull(result);
        Assert.IsFalse(result.Success);
        Assert.IsFalse(string.IsNullOrWhiteSpace(result.FailReason));
    }








    [TestMethod]
    public void RegistryReadTool_ReadsKnownValue()
    {
        RegistryReadTool tool = new RegistryReadTool();
        ToolResult result = Invoke(tool, "registry_read_value", "HKLM", "SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion", "CurrentVersion");
        Assert.IsNotNull(result);
        Assert.IsTrue(result.Success, result.FailReason);
    }








    [TestMethod]
    public void RemoteDesktopReadTool_ReportsSettings()
    {
        RemoteDesktopReadTool tool = new RemoteDesktopReadTool();
        ToolResult result = Invoke(tool, "rdp_read_settings");
        Assert.IsNotNull(result);
        Assert.IsTrue(result.Success, result.FailReason);
    }








    [TestMethod]
    public void ScheduledTaskReadTool_ListsTasks()
    {
        ScheduledTaskReadTool tool = new ScheduledTaskReadTool();
        ToolResult result = Invoke(tool, "scheduled_task_list");
        Assert.IsNotNull(result);
        Assert.IsFalse(string.IsNullOrWhiteSpace(result.FailReason) && !result.Success, "Scheduled task tool failed without a reason.");
    }








    [TestMethod]
    public void SearchIndexingReadTool_ReportsSettings()
    {
        SearchIndexingReadTool tool = new SearchIndexingReadTool();
        ToolResult result = Invoke(tool, "search_indexing_read_settings");
        Assert.IsNotNull(result);
        Assert.IsTrue(result.Success, result.FailReason);
    }








    [TestMethod]
    public void ShellExplorerReadTool_ReportsSettings()
    {
        ShellExplorerReadTool tool = new ShellExplorerReadTool();
        ToolResult result = Invoke(tool, "shell_explorer_read_settings");
        Assert.IsNotNull(result);
        Assert.IsTrue(result.Success, result.FailReason);
    }








    [TestMethod]
    public void WindowsServiceReadTool_ListsServices()
    {
        WindowsServiceReadTool tool = new WindowsServiceReadTool();
        ToolResult result = Invoke(tool, "service_list");
        Assert.IsNotNull(result);
        Assert.IsTrue(result.Success, result.FailReason);
    }








    [TestMethod]
    public void WindowsServiceReadTool_MissingServiceFails()
    {
        WindowsServiceReadTool tool = new WindowsServiceReadTool();
        ToolResult result = Invoke(tool, "service_read", "__sentinel_missing_service__");
        Assert.IsNotNull(result);
        Assert.IsFalse(result.Success);
        Assert.IsFalse(string.IsNullOrWhiteSpace(result.FailReason));
    }








    [TestMethod]
    public void WindowsUpdateReadTool_ReportsHistory()
    {
        WindowsUpdateReadTool tool = new WindowsUpdateReadTool();
        ToolResult result = Invoke(tool, "windows_update_list_history");
        Assert.IsNotNull(result);
        Assert.IsFalse(string.IsNullOrWhiteSpace(result.FailReason) && !result.Success, "WindowsUpdate tool failed without a reason.");
    }








    [TestMethod]
    public void WmiQueryTool_EmptyQueryFails()
    {
        WmiQueryTool tool = new WmiQueryTool();
        ToolResult result = Invoke(tool, "wmi_query", "   ");
        Assert.IsNotNull(result);
        Assert.IsFalse(result.Success);
        Assert.IsFalse(string.IsNullOrWhiteSpace(result.FailReason));
    }








    [TestMethod]
    public void WmiQueryTool_ListsOperatingSystem()
    {
        WmiQueryTool tool = new WmiQueryTool();
        ToolResult result = Invoke(tool, "wmi_query", "SELECT * FROM Win32_OperatingSystem");
        Assert.IsNotNull(result);
        Assert.IsFalse(string.IsNullOrWhiteSpace(result.FailReason) && !result.Success, "WMI query tool failed without a reason.");
        if (result.Success && !string.IsNullOrWhiteSpace(result.Results))
        {
            StringAssert.Contains(result.Results, "Caption");
        }
    }
}