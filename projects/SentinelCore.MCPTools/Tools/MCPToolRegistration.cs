
using Microsoft.Extensions.DependencyInjection;




namespace SentinelCoreMCP.Tools;





/// <summary>
///     Extension methods for registering all MCPTools in the DI container.
///     Tools are resolved by <see cref="McpToolFactory" /> via ActivatorUtilities;
///     these registrations exist so tools can also be resolved directly from DI when needed.
/// </summary>
public static class MCPToolRegistrations
{

    /// <summary>
    ///     Registers all MCPTools. Each tool is registered as a transient service, allowing it to be resolved directly from the DI container when needed.
    /// </summary>
    /// <param name="services">The service collection to register into.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection RegisterTools(this IServiceCollection services)
    {
        // Factory used to compose the full ToolCollection via ActivatorUtilities.
        services.AddTransient<McpToolFactory>();

        // Sysinternals-backed tool classes.
        services.AddTransient<AccessChkReadTool>();
        services.AddTransient<AutorunsReadTool>();
        services.AddTransient<HandleReadTool>();
        services.AddTransient<ListDllsReadTool>();
        services.AddTransient<PsInfoReadTool>();
        services.AddTransient<PsListReadTool>();
        services.AddTransient<PsServiceReadTool>();
        services.AddTransient<RegDelNullReadTool>();
        services.AddTransient<SigCheckReadTool>();
        services.AddTransient<ProcessExplorerReadTool>();
        services.AddTransient<ShareEnumReadTool>();

        // Managed Windows reconnaissance tool classes.
        services.AddTransient<ActiveDirectoryReadTool>();
        services.AddTransient<AppLockerReadTool>();
        services.AddTransient<AudioDeviceReadTool>();
        services.AddTransient<AuditingTool>();
        services.AddTransient<BatteryReadTool>();
        services.AddTransient<BitlockerReadTool>();
        services.AddTransient<BootConfigurationReadTool>();
        services.AddTransient<BrowserConfigReadTool>();
        services.AddTransient<BrowserExtendedReadTool>();
        services.AddTransient<CertificateExtendedReadTool>();
        services.AddTransient<CertificateStoreReadTool>();
        services.AddTransient<CoreInfoReadTool>();
        services.AddTransient<CredentialsReadTool>();
        services.AddTransient<DcomReadTool>();
        services.AddTransient<DefenderExtendedReadTool>();
        services.AddTransient<DefenderReadTool>();
        services.AddTransient<DisplayReadTool>();
        services.AddTransient<DriversReadTool>();
        services.AddTransient<EnvironmentExtendedReadTool>();
        services.AddTransient<EnvironmentVariablesReadTool>();
        services.AddTransient<EventLogExtendedReadTool>();
        services.AddTransient<EventLogReadTool>();
        services.AddTransient<FileSystemExtendedReadTool>();
        services.AddTransient<FileSystemReadTool>();
        services.AddTransient<FirewallReadTool>();
        services.AddTransient<GroupPolicyExtendedReadTool>();
        services.AddTransient<GroupPolicyReadTool>();
        services.AddTransient<HyperVExtendedReadTool>();
        services.AddTransient<HyperVReadTool>();
        services.AddTransient<InstalledAppsReadTool>();
        services.AddTransient<LocalAccountsReadTool>();
        services.AddTransient<LogonSessionsReadTool>();
        services.AddTransient<NetworkDnsReadTool>();
        services.AddTransient<NetworkExtendedReadTool>();
        services.AddTransient<NetworkReadTool>();
        services.AddTransient<NotificationsReadTool>();
        services.AddTransient<NtfsInfoReadTool>();
        services.AddTransient<PendMovesReadTool>();
        services.AddTransient<PerformanceReadTool>();
        services.AddTransient<PipeListReadTool>();
        services.AddTransient<PnpDeviceReadTool>();
        services.AddTransient<PnpExtendedReadTool>();
        services.AddTransient<PowerSettingsReadTool>();
        services.AddTransient<PowerShellReadTool>();
        services.AddTransient<PrinterExtendedReadTool>();
        services.AddTransient<PrinterReadTool>();
        services.AddTransient<ProcDumpReadTool>();
        services.AddTransient<ProcessesReadTool>();
        services.AddTransient<ProxyReadTool>();
        services.AddTransient<PsLogListReadTool>();
        services.AddTransient<RdpExtendedReadTool>();
        services.AddTransient<RegistryExtendedReadTool>();
        services.AddTransient<RegistryReadTool>();
        services.AddTransient<RemoteDesktopReadTool>();
        services.AddTransient<ScheduledTaskReadTool>();
        services.AddTransient<SearchIndexingReadTool>();
        services.AddTransient<SecurityExtendedReadTool>();
        services.AddTransient<SensorsReadTool>();
        services.AddTransient<ServiceExtendedReadTool>();
        services.AddTransient<SessionsReadTool>();
        services.AddTransient<ShellExplorerReadTool>();
        services.AddTransient<SystemReadTool>();
        services.AddTransient<TcpViewReadTool>();
        services.AddTransient<UacReadTool>();
        services.AddTransient<VpnReadTool>();
        services.AddTransient<WindowsServiceReadTool>();
        services.AddTransient<WindowsUpdateExtendedReadTool>();
        services.AddTransient<WindowsUpdateReadTool>();
        services.AddTransient<WirelessExtendedReadTool>();
        services.AddTransient<WirelessReadTool>();
        services.AddTransient<WmiQueryTool>();

        return services;
    }
}
