// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         FirewallReadTool.cs
// Author: Kyle L. Crowder
// Build Num:  080801



using System.Collections;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;

using SentinelCore.Tools.Interop;




namespace SentinelCore.Tools;





/// <summary>
///     Read-only tool for querying Windows Firewall rules and profiles using the HNetCfg COM API.
/// </summary>
public sealed class FirewallReadTool : AITool
{

    private const int NetFwProfile2Domain = 1;
    private const int NetFwProfile2Private = 2;
    private const int NetFwProfile2Public = 4;
    private static readonly Guid ClsidNetFwPolicy2 = new("E2B3C97F-6AE1-41AC-817A-F6F92166D7DD");
    public override string Description { get; } = "Read-only tool for querying Windows Firewall rules and profiles.";
    public override string Name { get; } = "Firewall_Read";








    private static void AppendProfile(INetFwPolicy2 policy, int profileType, string name, StringBuilder sb)
    {
        policy.get_FirewallEnabled(profileType, out bool enabled);
        policy.get_DefaultInboundAction(profileType, out int inbound);
        policy.get_DefaultOutboundAction(profileType, out int outbound);

        sb.AppendLine($"Profile={name}; " + $"Enabled={enabled}; " + $"DefaultInboundAction={inbound}; " + $"DefaultOutboundAction={outbound}");
    }








    private static void AppendRule(INetFwRule rule, StringBuilder sb, int desiredDirection, int desiredProfile)
    {
        rule.get_Direction(out int direction);
        if (desiredDirection != 0 && direction != desiredDirection) return;

        rule.get_Profiles(out int profiles);
        if (desiredProfile != 0 && (profiles & desiredProfile) == 0) return;

        rule.get_Name(out var name);
        rule.get_Enabled(out bool enabled);
        rule.get_Action(out int action);
        rule.get_Protocol(out int protocol);
        rule.get_LocalPorts(out var localPorts);

        sb.AppendLine($"Name={name ?? string.Empty}; " + $"Direction={direction}; " + $"Enabled={enabled}; " + $"Action={action}; " + $"Protocol={protocol}; " + $"LocalPorts={localPorts ?? string.Empty}");
    }








    private static int ParseDirection(string? direction)
    {
        return direction?.Trim().ToUpperInvariant() switch
        {
                "INBOUND" => 1, // NET_FW_RULE_DIR_IN
                "OUTBOUND" => 2, // NET_FW_RULE_DIR_OUT
                _ => 0
        };
    }








    private static int ParseProfile(string? profile)
    {
        return profile?.Trim().ToUpperInvariant() switch
        {
                "DOMAIN" => NetFwProfile2Domain,
                "PRIVATE" => NetFwProfile2Private,
                "PUBLIC" => NetFwProfile2Public,
                _ => 0
        };
    }








    [Description("Lists Windows Firewall rules with optional profile and direction filters.")]
    public Task<ToolResult> firewall_list_rules([Description("Optional direction filter: Inbound or Outbound.")] string? direction = null, [Description("Optional profile filter: Domain, Private, Public.")] string? profile = null)
    {
        try
        {
            using SafeComObject com = new(ClsidNetFwPolicy2);
            if (com.Instance is not INetFwPolicy2 policy)
                return Task.FromResult(ToolResult.Fail("Unable to create NetFwPolicy2."));

            policy.get_Rules(out var rules);
            if (rules is null) return Task.FromResult(ToolResult.Fail("Unable to retrieve firewall rules."));

            StringBuilder sb = new();

            rules.get_Count(out int count);
            sb.AppendLine($"FirewallRules={count}");

            int desiredDirection = ParseDirection(direction);
            int desiredProfile = ParseProfile(profile);

            foreach (INetFwRule rule in rules) AppendRule(rule, sb, desiredDirection, desiredProfile);

            Marshal.ReleaseComObject(rules);
            return Task.FromResult(ToolResult.Ok(sb.ToString()));
        }
        catch (Exception ex)
        {
            return Task.FromResult(ToolResult.Fail($"Firewall rule listing failed: {ex.Message}"));
        }
    }








    [Description("Reads the current firewall profile settings.")]
    public Task<ToolResult> firewall_read_profiles()
    {
        try
        {
            using SafeComObject com = new(ClsidNetFwPolicy2);
            if (com.Instance is not INetFwPolicy2 policy)
                return Task.FromResult(ToolResult.Fail("Unable to create NetFwPolicy2."));

            StringBuilder sb = new();

            AppendProfile(policy, NetFwProfile2Domain, "Domain", sb);
            AppendProfile(policy, NetFwProfile2Private, "Private", sb);
            AppendProfile(policy, NetFwProfile2Public, "Public", sb);

            return Task.FromResult(ToolResult.Ok(sb.ToString()));
        }
        catch (Exception ex)
        {
            return Task.FromResult(ToolResult.Fail($"Firewall profile read failed: {ex.Message}"));
        }
    }








    [ComImport]
    [Guid("E2B3C97F-6AE1-41AC-817A-F6F92166D7DD")]
    [ClassInterface(ClassInterfaceType.None)]
    private class NetFwPolicy2
    {
    }





    // INetFwPolicy2 inherits from IDispatch in the official API.
    [ComImport]
    [Guid("98325047-C671-4174-8D81-DEFCD3F03186")]
    [InterfaceType(ComInterfaceType.InterfaceIsIDispatch)]
    private interface INetFwPolicy2
    {
        int get_CurrentProfileTypes(out int profileTypes);


        int get_FirewallEnabled(int profileType, out bool enabled);


        int get_DefaultInboundAction(int profileType, out int action);


        int get_DefaultOutboundAction(int profileType, out int action);


        int get_Rules(out INetFwRules rules);
    }





    [ComImport]
    [Guid("AF230D27-BABA-4E56-8685-7D7852AD4197")]
    [InterfaceType(ComInterfaceType.InterfaceIsIDispatch)]
    private interface INetFwRules : IEnumerable
    {
        int get_Count(out int count);


        int Item([MarshalAs(UnmanagedType.BStr)] string name, out INetFwRule rule);








        // _NewEnum is exposed as DispId -4 and maps to IEnumerable in C#.
        [DispId(-4)]
        new IEnumerator GetEnumerator();
    }





    // Correct GUID for INetFwRule per Windows Firewall API.
    [ComImport]
    [Guid("2C5BC43E-3369-4C33-AB0C-BE9469677AF4")]
    [InterfaceType(ComInterfaceType.InterfaceIsIDispatch)]
    private interface INetFwRule
    {
        int get_Name([MarshalAs(UnmanagedType.BStr)] out string name);


        int get_Description([MarshalAs(UnmanagedType.BStr)] out string description);


        int get_ApplicationName([MarshalAs(UnmanagedType.BStr)] out string applicationName);


        int get_ServiceName([MarshalAs(UnmanagedType.BStr)] out string serviceName);


        int get_Protocol(out int protocol);


        int get_LocalPorts([MarshalAs(UnmanagedType.BStr)] out string localPorts);


        int get_RemotePorts([MarshalAs(UnmanagedType.BStr)] out string remotePorts);


        int get_LocalAddresses([MarshalAs(UnmanagedType.BStr)] out string localAddresses);


        int get_RemoteAddresses([MarshalAs(UnmanagedType.BStr)] out string remoteAddresses);


        int get_IcmpTypesAndCodes([MarshalAs(UnmanagedType.BStr)] out string icmpTypesAndCodes);


        int get_Direction(out int direction);


        int get_Interfaces([MarshalAs(UnmanagedType.Struct)] out object interfaces);


        int get_InterfaceTypes([MarshalAs(UnmanagedType.BStr)] out string interfaceTypes);


        int get_Enabled(out bool enabled);


        int get_Grouping([MarshalAs(UnmanagedType.BStr)] out string grouping);


        int get_Profiles(out int profiles);


        int get_EdgeTraversal(out bool edgeTraversal);


        int get_Action(out int action);


        int get_EdgeTraversalOptions(out int edgeTraversalOptions);
    }
}