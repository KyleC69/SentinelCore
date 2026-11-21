
using System;
using System.Collections.Concurrent;

using Microsoft.Extensions.DependencyInjection;




namespace SentinelCoreMCP.Tools;





/// <summary>
///     Creates every tool instance eagerly when it is constructed. Nothing is created on demand, so
///     a tool is always present and a test can call its methods directly.
/// </summary>
public sealed class McpToolFactory
{
    private readonly ConcurrentDictionary<Type, object> _instances = new();

    private readonly IServiceProvider _serviceProvider;








    /// <summary>
    ///     Initializes a new instance of the <see cref="McpToolFactory" /> class.
    /// </summary>
    /// <param name="serviceProvider">The provider used to resolve tool constructor dependencies.</param>
    public McpToolFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));

        Create<AccessChkReadTool>();
        Create<AutorunsReadTool>();
        Create<HandleReadTool>();
        Create<ListDllsReadTool>();
        Create<PsInfoReadTool>();
        Create<PsListReadTool>();
        Create<PsServiceReadTool>();
        Create<RegDelNullReadTool>();
        Create<SigCheckReadTool>();
        Create<ProcessExplorerReadTool>();
        Create<ShareEnumReadTool>();
        Create<ActiveDirectoryReadTool>();
        Create<AppLockerReadTool>();
        Create<AudioDeviceReadTool>();
        Create<AuditingTool>();
        Create<BatteryReadTool>();
        Create<BitlockerReadTool>();
        Create<BootConfigurationReadTool>();
        Create<BrowserConfigReadTool>();
        Create<BrowserExtendedReadTool>();
        Create<CertificateExtendedReadTool>();
        Create<CertificateStoreReadTool>();
        Create<CoreInfoReadTool>();
        Create<CredentialsReadTool>();
        Create<DcomReadTool>();
        Create<DefenderExtendedReadTool>();
        Create<DefenderReadTool>();
        Create<DisplayReadTool>();
        Create<DriversReadTool>();
        Create<EnvironmentExtendedReadTool>();
        Create<EnvironmentVariablesReadTool>();
        Create<EventLogExtendedReadTool>();
        Create<EventLogReadTool>();
        Create<FileSystemExtendedReadTool>();
        Create<FileSystemReadTool>();
        Create<FirewallReadTool>();
        Create<GroupPolicyExtendedReadTool>();
        Create<GroupPolicyReadTool>();
        Create<HyperVExtendedReadTool>();
        Create<HyperVReadTool>();
        Create<InstalledAppsReadTool>();
        Create<LocalAccountsReadTool>();
        Create<LogonSessionsReadTool>();
        Create<NetworkDnsReadTool>();
        Create<NetworkExtendedReadTool>();
        Create<NetworkReadTool>();
        Create<NotificationsReadTool>();
        Create<NtfsInfoReadTool>();
        Create<PendMovesReadTool>();
        Create<PerformanceReadTool>();
        Create<PipeListReadTool>();
        Create<PnpDeviceReadTool>();
        Create<PnpExtendedReadTool>();
        Create<PowerSettingsReadTool>();
        Create<PowerShellReadTool>();
        Create<PrinterExtendedReadTool>();
        Create<PrinterReadTool>();
        Create<ProcDumpReadTool>();
        Create<ProcessesReadTool>();
        Create<ProxyReadTool>();
        Create<PsLogListReadTool>();
        Create<RdpExtendedReadTool>();
        Create<RegistryExtendedReadTool>();
        Create<RegistryReadTool>();
        Create<RemoteDesktopReadTool>();
        Create<ScheduledTaskReadTool>();
        Create<SearchIndexingReadTool>();
        Create<SecurityExtendedReadTool>();
        Create<SensorsReadTool>();
        Create<ServiceExtendedReadTool>();
        Create<SessionsReadTool>();
        Create<ShellExplorerReadTool>();
        Create<SystemReadTool>();
        Create<TcpViewReadTool>();
        Create<UacReadTool>();
        Create<VpnReadTool>();
        Create<WindowsServiceReadTool>();
        Create<WindowsUpdateExtendedReadTool>();
        Create<WindowsUpdateReadTool>();
        Create<WirelessExtendedReadTool>();
        Create<WirelessReadTool>();
        Create<WmiQueryTool>();
    }








    /// <summary>Returns the tool instance for <typeparamref name="T" />, creating it once.</summary>
    /// <typeparam name="T">The tool class to create.</typeparam>
    /// <returns>The already created tool.</returns>
    public T Create<T>() where T : class => (T)_instances.GetOrAdd(typeof(T), static (type, provider) => ActivatorUtilities.CreateInstance(provider, type), _serviceProvider);








    /// <summary>
    ///     Returns a collection over the tool instances this factory already created.
    /// </summary>
    /// <returns>A collection exposing every tool class by its real instance.</returns>
    public ToolCollection CreateAllTools() => new(this);



}
