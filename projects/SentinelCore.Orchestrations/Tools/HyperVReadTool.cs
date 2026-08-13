// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         HyperVReadTool.cs
// Author: Kyle L. Crowder
// Build Num:  081312



using System.ComponentModel;
using System.Text;
using System.Text.Json;

using Microsoft.Management.Infrastructure;




namespace SentinelCore.Tools;





/// <summary>
///     Read-only tool for querying Hyper-V virtual machines and settings via the CIM-based Hyper-V WMI v2 namespace.
/// </summary>
public sealed class HyperVReadTool : AITool
{

    private const string HyperVNamespace = @"root\virtualization\v2";
    public override string Description { get; } = "Read-only tool for querying Hyper-V virtual machines and settings.";
    public override string Name { get; } = "HyperV_Read";








    [Description("Lists Hyper-V virtual switches.")]
    public Task<ToolResult> hyperv_list_switches()
    {
        try
        {
            StringBuilder sb = new();
            using CimSession? session = CimSession.Create(null);
            const string query = "SELECT Name, ElementName FROM Msvm_VirtualEthernetSwitch";
            foreach (CimInstance? switchObj in session.QueryInstances(HyperVNamespace, "WQL", query))
            {
                string name = switchObj.CimInstanceProperties["Name"] != null ? switchObj.CimInstanceProperties["Name"]?.Value?.ToString() ?? string.Empty : string.Empty;
                string elementName = switchObj.CimInstanceProperties["ElementName"] != null ? switchObj.CimInstanceProperties["ElementName"]?.Value?.ToString() ?? string.Empty : string.Empty;
                sb.AppendLine($"Name={name}, ElementName={elementName}");
            }

            return Task.FromResult(ToolResult.Ok(sb.ToString()));
        }
        catch (Exception ex)
        {
            return Task.FromResult(ToolResult.Fail($"Hyper-V switch listing failed: {ex.Message}"));
        }
    }








    [Description("Lists Hyper-V virtual machines.")]
    public Task<ToolResult> hyperv_list_vms()
    {
        try
        {
            StringBuilder sb = new();
            using CimSession? session = CimSession.Create(null);
            const string query = "SELECT Name, ElementName, EnabledState, HealthState FROM Msvm_ComputerSystem WHERE Caption = 'Virtual Machine'";
            foreach (CimInstance? vm in session.QueryInstances(HyperVNamespace, "WQL", query))
            {
                string name = vm.CimInstanceProperties["Name"]?.Value?.ToString() ?? string.Empty;
                string elementName = vm.CimInstanceProperties["ElementName"]?.Value?.ToString() ?? string.Empty;
                string enabledState = vm.CimInstanceProperties["EnabledState"]?.Value?.ToString() ?? string.Empty;
                string healthState = vm.CimInstanceProperties["HealthState"]?.Value?.ToString() ?? string.Empty;
                sb.AppendLine($"Name={name}, ElementName={elementName}, EnabledState={enabledState}, HealthState={healthState}");
            }

            return Task.FromResult(ToolResult.Ok(sb.ToString()));
        }
        catch (Exception ex)
        {
            return Task.FromResult(ToolResult.Fail($"Hyper-V VM listing failed: {ex.Message}"));
        }
    }








    [Description("Reads settings of a specific Hyper-V virtual machine.")]
    public Task<ToolResult> hyperv_read_vm([Description("The VM name (ElementName).")] string vmName)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(vmName))
            {
                return Task.FromResult(ToolResult.Fail("vmName is required."));
            }

            string escaped = vmName.Replace("'", "''");
            string query = $"SELECT * FROM Msvm_ComputerSystem WHERE ElementName = '{escaped}' AND Caption = 'Virtual Machine'";
            List<Dictionary<string, object?>> results = new();
            using CimSession? session = CimSession.Create(null);
            foreach (CimInstance? vm in session.QueryInstances(HyperVNamespace, "WQL", query))
            {
                Dictionary<string, object?> record = new();
                foreach (CimProperty? property in vm.CimInstanceProperties)
                    record[property.Name] = property.Value switch
                    {
                            Array array => string.Join("|", array.Cast<object>().Select(x => x != null ? x.ToString() != null ? x.ToString() : string.Empty : string.Empty)),
                            _ => property.Value
                    };

                results.Add(record);
            }

            if (results.Count == 0)
            {
                return Task.FromResult(ToolResult.Fail($"Hyper-V VM not found: {vmName}"));
            }

            string json = JsonSerializer.Serialize(results, new JsonSerializerOptions { WriteIndented = true });
            return Task.FromResult(ToolResult.Ok(json));
        }
        catch (Exception ex)
        {
            return Task.FromResult(ToolResult.Fail($"Hyper-V VM read failed: {ex.Message}"));
        }
    }
}