// Copyright (c) Kyle L. Crowder. All rights reserved.

using System.Reflection;

using Microsoft.Extensions.AI;

using SentinelCoreLib.Tools;

namespace SentinelCore.Tests;

/// <summary>
/// Contract and smoke tests for every deterministic read tool in SentinelCoreLib.Tools.
/// These tests validate instantiation, AIFunction discovery, and that each public operation
/// returns a well-formed <see cref="ToolResult"/> for both success and failure paths.
/// </summary>
[TestClass]
public sealed class ToolsTests
{
    private static IEnumerable<AITool> AllTools =>
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
        new WmiQueryTool(),
    ];

    [TestMethod]
    public void AllToolsExposeAtLeastOneFunction()
    {
        foreach (var tool in AllTools)
        {
            var functions = EnumerateFunctions(tool);
            Assert.IsTrue(functions.Any(), $"{tool.GetType().Name} should expose at least one tool method.");
        }
    }

    private static IEnumerable<MethodInfo> EnumerateFunctions(AITool tool)
    {
        return tool.GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(m => m.GetCustomAttribute<System.ComponentModel.DescriptionAttribute>() is not null && m.ReturnType == typeof(Task<ToolResult>));
    }

    [TestMethod]
    public void AccessibilityReadTool_ListsSettings()
    {
        var tool = new AccessibilityReadTool();
        var result = Invoke(tool, "accessibility_read_settings");
        Assert.IsNotNull(result);
        Assert.IsTrue(result.Success, result.FailReason);
    }

    [TestMethod]
    public void AppLockerReadTool_ListsRuleCollections()
    {
        var tool = new AppLockerReadTool();
        var result = Invoke(tool, "applocker_list_rule_collections");
        Assert.IsNotNull(result);
        Assert.IsFalse(string.IsNullOrWhiteSpace(result.FailReason) && !result.Success, "AppLocker tool failed without a reason.");
    }

    [TestMethod]
    public void AudioDeviceReadTool_ListsDevices()
    {
        var tool = new AudioDeviceReadTool();
        var result = Invoke(tool, "audio_list_devices");
        Assert.IsNotNull(result);
        Assert.IsFalse(string.IsNullOrWhiteSpace(result.FailReason) && !result.Success, "Audio device tool failed without a reason.");
    }

    [TestMethod]
    public void BootConfigurationReadTool_ListsEntries()
    {
        var tool = new BootConfigurationReadTool();
        var result = Invoke(tool, "bcdedit_enum");
        Assert.IsNotNull(result);
        Assert.IsFalse(string.IsNullOrWhiteSpace(result.FailReason) && !result.Success, "Boot configuration tool failed without a reason.");
    }

    [TestMethod]
    public void CertificateStoreReadTool_ListsCertificates()
    {
        var tool = new CertificateStoreReadTool();
        var result = Invoke(tool, "certificate_list", "Root");
        Assert.IsNotNull(result);
        Assert.IsTrue(result.Success, result.FailReason);
    }

    [TestMethod]
    public void CertificateStoreReadTool_InvalidStoreFailsGracefully()
    {
        var tool = new CertificateStoreReadTool();
        var result = Invoke(tool, "certificate_list", "__nonexistent__");
        Assert.IsNotNull(result);
        Assert.IsFalse(result.Success);
        Assert.IsFalse(string.IsNullOrWhiteSpace(result.FailReason));
    }

    [TestMethod]
    public void DcomReadTool_ListsApplications()
    {
        var tool = new DcomReadTool();
        var result = Invoke(tool, "dcom_list_applications");
        Assert.IsNotNull(result);
        Assert.IsTrue(result.Success, result.FailReason);
    }

    [TestMethod]
    public void EnvironmentVariablesReadTool_ListsEnvironment()
    {
        var tool = new EnvironmentVariablesReadTool();
        var result = Invoke(tool, "environment_list");
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
        var tool = new EnvironmentVariablesReadTool();
        var result = Invoke(tool, "environment_read_value", "SystemRoot");
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
        var tool = new EventLogReadTool();
        var result = Invoke(tool, "event_log_list_channels");
        Assert.IsNotNull(result);
        Assert.IsTrue(result.Success, result.FailReason);
    }

    [TestMethod]
    public void FileSystemReadTool_ListsTempDirectory()
    {
        var tool = new FileSystemReadTool();
        var result = Invoke(tool, "file_system_list_directory", Path.GetTempPath());
        Assert.IsNotNull(result);
        Assert.IsTrue(result.Success, result.FailReason);
    }

    [TestMethod]
    public void FileSystemReadTool_MissingDirectoryFails()
    {
        var tool = new FileSystemReadTool();
        var result = Invoke(tool, "file_system_list_directory", "C:\\__sentinel_missing_dir__");
        Assert.IsNotNull(result);
        Assert.IsFalse(result.Success);
        Assert.IsFalse(string.IsNullOrWhiteSpace(result.FailReason));
    }

    [TestMethod]
    public void FirewallReadTool_ListsRules()
    {
        var tool = new FirewallReadTool();
        var result = Invoke(tool, "firewall_list_rules");
        Assert.IsNotNull(result);
        Assert.IsFalse(string.IsNullOrWhiteSpace(result.FailReason) && !result.Success, "Firewall tool failed without a reason.");
    }

    [TestMethod]
    public void GroupPolicyReadTool_ListsResults()
    {
        var tool = new GroupPolicyReadTool();
        var result = Invoke(tool, "group_policy_list");
        Assert.IsNotNull(result);
        Assert.IsFalse(string.IsNullOrWhiteSpace(result.FailReason) && !result.Success, "GroupPolicy tool failed without a reason.");
    }

    [TestMethod]
    public void HyperVReadTool_ListsVirtualMachines()
    {
        var tool = new HyperVReadTool();
        var result = Invoke(tool, "hyperv_list_vms");
        Assert.IsNotNull(result);
        Assert.IsFalse(string.IsNullOrWhiteSpace(result.FailReason) && !result.Success, "HyperV tool failed without a reason.");
    }

    [TestMethod]
    public void LocalAccountsReadTool_ListsUsers()
    {
        var tool = new LocalAccountsReadTool();
        var result = Invoke(tool, "local_user_list");
        Assert.IsNotNull(result);
        Assert.IsTrue(result.Success, result.FailReason);
    }

    [TestMethod]
    public void NetworkReadTool_ListsInterfaces()
    {
        var tool = new NetworkReadTool();
        var result = Invoke(tool, "network_list_interfaces");
        Assert.IsNotNull(result);
        Assert.IsTrue(result.Success, result.FailReason);
    }

    [TestMethod]
    public void PnpDeviceReadTool_ListsDevices()
    {
        var tool = new PnpDeviceReadTool();
        var result = Invoke(tool, "pnp_list_devices");
        Assert.IsNotNull(result);
        Assert.IsTrue(result.Success, result.FailReason);
    }

    [TestMethod]
    public void PowerSettingsReadTool_ListsPlans()
    {
        var tool = new PowerSettingsReadTool();
        var result = Invoke(tool, "power_list_plans");
        Assert.IsNotNull(result);
        Assert.IsFalse(string.IsNullOrWhiteSpace(result.FailReason) && !result.Success, "Power settings tool failed without a reason.");
    }

    [TestMethod]
    public void PrinterReadTool_ListsPrinters()
    {
        var tool = new PrinterReadTool();
        var result = Invoke(tool, "printer_list");
        Assert.IsNotNull(result);
        Assert.IsFalse(string.IsNullOrWhiteSpace(result.FailReason) && !result.Success, "Printer tool failed without a reason.");
    }

    [TestMethod]
    public void RegistryReadTool_ReadsKnownValue()
    {
        var tool = new RegistryReadTool();
        var result = Invoke(tool, "registry_read_value", "HKLM", "SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion", "CurrentVersion");
        Assert.IsNotNull(result);
        Assert.IsTrue(result.Success, result.FailReason);
    }

    [TestMethod]
    public void RegistryReadTool_MissingKeyFails()
    {
        var tool = new RegistryReadTool();
        var result = Invoke(tool, "registry_list_key", "HKLM", "SOFTWARE\\__sentinel_missing_key__");
        Assert.IsNotNull(result);
        Assert.IsFalse(result.Success);
        Assert.IsFalse(string.IsNullOrWhiteSpace(result.FailReason));
    }

    [TestMethod]
    public void RemoteDesktopReadTool_ReportsSettings()
    {
        var tool = new RemoteDesktopReadTool();
        var result = Invoke(tool, "rdp_read_settings");
        Assert.IsNotNull(result);
        Assert.IsTrue(result.Success, result.FailReason);
    }

    [TestMethod]
    public void ScheduledTaskReadTool_ListsTasks()
    {
        var tool = new ScheduledTaskReadTool();
        var result = Invoke(tool, "scheduled_task_list");
        Assert.IsNotNull(result);
        Assert.IsFalse(string.IsNullOrWhiteSpace(result.FailReason) && !result.Success, "Scheduled task tool failed without a reason.");
    }

    [TestMethod]
    public void SearchIndexingReadTool_ReportsSettings()
    {
        var tool = new SearchIndexingReadTool();
        var result = Invoke(tool, "search_indexing_read_settings");
        Assert.IsNotNull(result);
        Assert.IsTrue(result.Success, result.FailReason);
    }

    [TestMethod]
    public void ShellExplorerReadTool_ReportsSettings()
    {
        var tool = new ShellExplorerReadTool();
        var result = Invoke(tool, "shell_explorer_read_settings");
        Assert.IsNotNull(result);
        Assert.IsTrue(result.Success, result.FailReason);
    }

    [TestMethod]
    public void WindowsServiceReadTool_ListsServices()
    {
        var tool = new WindowsServiceReadTool();
        var result = Invoke(tool, "service_list");
        Assert.IsNotNull(result);
        Assert.IsTrue(result.Success, result.FailReason);
    }

    [TestMethod]
    public void WindowsServiceReadTool_MissingServiceFails()
    {
        var tool = new WindowsServiceReadTool();
        var result = Invoke(tool, "service_read", "__sentinel_missing_service__");
        Assert.IsNotNull(result);
        Assert.IsFalse(result.Success);
        Assert.IsFalse(string.IsNullOrWhiteSpace(result.FailReason));
    }

    [TestMethod]
    public void WindowsUpdateReadTool_ReportsHistory()
    {
        var tool = new WindowsUpdateReadTool();
        var result = Invoke(tool, "windows_update_list_history");
        Assert.IsNotNull(result);
        Assert.IsFalse(string.IsNullOrWhiteSpace(result.FailReason) && !result.Success, "WindowsUpdate tool failed without a reason.");
    }

    [TestMethod]
    public void WmiQueryTool_ListsOperatingSystem()
    {
        var tool = new WmiQueryTool();
        var result = Invoke(tool, "wmi_query", "SELECT * FROM Win32_OperatingSystem");
        Assert.IsNotNull(result);
        Assert.IsFalse(string.IsNullOrWhiteSpace(result.FailReason) && !result.Success, "WMI query tool failed without a reason.");
        if (result.Success && !string.IsNullOrWhiteSpace(result.Results))
        {
            StringAssert.Contains(result.Results, "Caption");
        }
    }

    [TestMethod]
    public void WmiQueryTool_EmptyQueryFails()
    {
        var tool = new WmiQueryTool();
        var result = Invoke(tool, "wmi_query", "   ");
        Assert.IsNotNull(result);
        Assert.IsFalse(result.Success);
        Assert.IsFalse(string.IsNullOrWhiteSpace(result.FailReason));
    }

    /// <summary>
    /// Invokes a public method on an AITool by name. Matches by exact method name first,
    /// then falls back to a DescriptionAttribute text match. Optional positional arguments
    /// are passed to the method in order.
    /// </summary>
    private static ToolResult Invoke(AITool tool, string functionName, params object?[] args)
    {
        var methods = tool.GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(m => m.GetCustomAttributes(typeof(System.ComponentModel.DescriptionAttribute), inherit: false).Any())
            .ToList();

        var method = methods.FirstOrDefault(m => m.Name.Equals(functionName, StringComparison.OrdinalIgnoreCase));

        if (method is null)
        {
            method = methods.FirstOrDefault(m =>
            {
                var attr = m.GetCustomAttribute<System.ComponentModel.DescriptionAttribute>();
                return attr?.Description.Contains(functionName, StringComparison.OrdinalIgnoreCase) == true;
            });
        }

        Assert.IsNotNull(method, $"No public method found on {tool.GetType().Name} for function '{functionName}'.");

        var parameters = method.GetParameters();
        var invokeArgs = new object?[parameters.Length];
        for (var i = 0; i < parameters.Length; i++)
        {
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
        }

        var task = method.Invoke(tool, invokeArgs) as Task<ToolResult>;
        Assert.IsNotNull(task, $"Method {method.Name} on {tool.GetType().Name} did not return Task<ToolResult>.");
        return task.GetAwaiter().GetResult();
    }
}
