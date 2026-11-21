// Solution: Sentinel-MCP-Projects
// Project:   SentinelCore-mcp
// File:         ToolCollection.cs
// Author: Kyle L. Crowder
// Build Num:  110719



using System;




namespace SentinelCoreMCP.Tools;





/// <summary>
///     Holds one eagerly created instance of every tool class. Access the tool instance, then call
///     the tool method directly, passing only what that method requires, for example
///     <c>tools.AppLockerReadTool.ApplockerGetEffectivePolicyAsync()</c>.
/// </summary>
public sealed class ToolCollection
{
    private readonly McpToolFactory _factory;








    /// <summary>
    ///     Initializes a new instance of the <see cref="ToolCollection" /> class.
    /// </summary>
    /// <param name="factory">The factory that has already created every tool instance.</param>
    public ToolCollection(McpToolFactory factory)
    {
        _factory = factory ?? throw new ArgumentNullException(nameof(factory));
    }








    // Sysinternals-backed tool classes.
    public AccessChkReadTool AccessChkReadTool => _factory.Create<AccessChkReadTool>();
    public AutorunsReadTool AutorunsReadTool => _factory.Create<AutorunsReadTool>();
    public HandleReadTool HandleReadTool => _factory.Create<HandleReadTool>();
    public ListDllsReadTool ListDllsReadTool => _factory.Create<ListDllsReadTool>();
    public PsInfoReadTool PsInfoReadTool => _factory.Create<PsInfoReadTool>();
    public PsListReadTool PsListReadTool => _factory.Create<PsListReadTool>();
    public PsServiceReadTool PsServiceReadTool => _factory.Create<PsServiceReadTool>();
    public RegDelNullReadTool RegDelNullReadTool => _factory.Create<RegDelNullReadTool>();
    public SigCheckReadTool SigCheckReadTool => _factory.Create<SigCheckReadTool>();
    public ProcessExplorerReadTool ProcessExplorerReadTool => _factory.Create<ProcessExplorerReadTool>();
    public ShareEnumReadTool ShareEnumReadTool => _factory.Create<ShareEnumReadTool>();

    // Managed Windows reconnaissance tool classes.
    public ActiveDirectoryReadTool ActiveDirectoryReadTool => _factory.Create<ActiveDirectoryReadTool>();
    public AppLockerReadTool AppLockerReadTool => _factory.Create<AppLockerReadTool>();
    public AudioDeviceReadTool AudioDeviceReadTool => _factory.Create<AudioDeviceReadTool>();
    public AuditingTool AuditingTool => _factory.Create<AuditingTool>();
    public BatteryReadTool BatteryReadTool => _factory.Create<BatteryReadTool>();
    public BitlockerReadTool BitlockerReadTool => _factory.Create<BitlockerReadTool>();
    public BootConfigurationReadTool BootConfigurationReadTool => _factory.Create<BootConfigurationReadTool>();
    public BrowserConfigReadTool BrowserConfigReadTool => _factory.Create<BrowserConfigReadTool>();
    public BrowserExtendedReadTool BrowserExtendedReadTool => _factory.Create<BrowserExtendedReadTool>();
    public CertificateExtendedReadTool CertificateExtendedReadTool => _factory.Create<CertificateExtendedReadTool>();
    public CertificateStoreReadTool CertificateStoreReadTool => _factory.Create<CertificateStoreReadTool>();
    public CoreInfoReadTool CoreInfoReadTool => _factory.Create<CoreInfoReadTool>();
    public CredentialsReadTool CredentialsReadTool => _factory.Create<CredentialsReadTool>();
    public DcomReadTool DcomReadTool => _factory.Create<DcomReadTool>();
    public DefenderExtendedReadTool DefenderExtendedReadTool => _factory.Create<DefenderExtendedReadTool>();
    public DefenderReadTool DefenderReadTool => _factory.Create<DefenderReadTool>();
    public DisplayReadTool DisplayReadTool => _factory.Create<DisplayReadTool>();
    public DriversReadTool DriversReadTool => _factory.Create<DriversReadTool>();
    public EnvironmentExtendedReadTool EnvironmentExtendedReadTool => _factory.Create<EnvironmentExtendedReadTool>();
    public EnvironmentVariablesReadTool EnvironmentVariablesReadTool => _factory.Create<EnvironmentVariablesReadTool>();
    public EventLogExtendedReadTool EventLogExtendedReadTool => _factory.Create<EventLogExtendedReadTool>();
    public EventLogReadTool EventLogReadTool => _factory.Create<EventLogReadTool>();
    public FileSystemExtendedReadTool FileSystemExtendedReadTool => _factory.Create<FileSystemExtendedReadTool>();
    public FileSystemReadTool FileSystemReadTool => _factory.Create<FileSystemReadTool>();
    public FirewallReadTool FirewallReadTool => _factory.Create<FirewallReadTool>();
    public GroupPolicyExtendedReadTool GroupPolicyExtendedReadTool => _factory.Create<GroupPolicyExtendedReadTool>();
    public GroupPolicyReadTool GroupPolicyReadTool => _factory.Create<GroupPolicyReadTool>();
    public HyperVExtendedReadTool HyperVExtendedReadTool => _factory.Create<HyperVExtendedReadTool>();
    public HyperVReadTool HyperVReadTool => _factory.Create<HyperVReadTool>();
    public InstalledAppsReadTool InstalledAppsReadTool => _factory.Create<InstalledAppsReadTool>();
    public LocalAccountsReadTool LocalAccountsReadTool => _factory.Create<LocalAccountsReadTool>();
    public LogonSessionsReadTool LogonSessionsReadTool => _factory.Create<LogonSessionsReadTool>();
    public NetworkDnsReadTool NetworkDnsReadTool => _factory.Create<NetworkDnsReadTool>();
    public NetworkExtendedReadTool NetworkExtendedReadTool => _factory.Create<NetworkExtendedReadTool>();
    public NetworkReadTool NetworkReadTool => _factory.Create<NetworkReadTool>();
    public NotificationsReadTool NotificationsReadTool => _factory.Create<NotificationsReadTool>();
    public NtfsInfoReadTool NtfsInfoReadTool => _factory.Create<NtfsInfoReadTool>();
    public PendMovesReadTool PendMovesReadTool => _factory.Create<PendMovesReadTool>();
    public PerformanceReadTool PerformanceReadTool => _factory.Create<PerformanceReadTool>();
    public PipeListReadTool PipeListReadTool => _factory.Create<PipeListReadTool>();
    public PnpDeviceReadTool PnpDeviceReadTool => _factory.Create<PnpDeviceReadTool>();
    public PnpExtendedReadTool PnpExtendedReadTool => _factory.Create<PnpExtendedReadTool>();
    public PowerSettingsReadTool PowerSettingsReadTool => _factory.Create<PowerSettingsReadTool>();
    public PowerShellReadTool PowerShellReadTool => _factory.Create<PowerShellReadTool>();
    public PrinterExtendedReadTool PrinterExtendedReadTool => _factory.Create<PrinterExtendedReadTool>();
    public PrinterReadTool PrinterReadTool => _factory.Create<PrinterReadTool>();
    public ProcDumpReadTool ProcDumpReadTool => _factory.Create<ProcDumpReadTool>();
    public ProcessesReadTool ProcessesReadTool => _factory.Create<ProcessesReadTool>();
    public ProxyReadTool ProxyReadTool => _factory.Create<ProxyReadTool>();
    public PsLogListReadTool PsLogListReadTool => _factory.Create<PsLogListReadTool>();
    public RdpExtendedReadTool RdpExtendedReadTool => _factory.Create<RdpExtendedReadTool>();
    public RegistryExtendedReadTool RegistryExtendedReadTool => _factory.Create<RegistryExtendedReadTool>();
    public RegistryReadTool RegistryReadTool => _factory.Create<RegistryReadTool>();
    public RemoteDesktopReadTool RemoteDesktopReadTool => _factory.Create<RemoteDesktopReadTool>();
    public ScheduledTaskReadTool ScheduledTaskReadTool => _factory.Create<ScheduledTaskReadTool>();
    public SearchIndexingReadTool SearchIndexingReadTool => _factory.Create<SearchIndexingReadTool>();
    public SecurityExtendedReadTool SecurityExtendedReadTool => _factory.Create<SecurityExtendedReadTool>();
    public SensorsReadTool SensorsReadTool => _factory.Create<SensorsReadTool>();
    public ServiceExtendedReadTool ServiceExtendedReadTool => _factory.Create<ServiceExtendedReadTool>();
    public SessionsReadTool SessionsReadTool => _factory.Create<SessionsReadTool>();
    public ShellExplorerReadTool ShellExplorerReadTool => _factory.Create<ShellExplorerReadTool>();
    public SystemReadTool SystemReadTool => _factory.Create<SystemReadTool>();
    public TcpViewReadTool TcpViewReadTool => _factory.Create<TcpViewReadTool>();
    public UacReadTool UacReadTool => _factory.Create<UacReadTool>();
    public VpnReadTool VpnReadTool => _factory.Create<VpnReadTool>();
    public WindowsServiceReadTool WindowsServiceReadTool => _factory.Create<WindowsServiceReadTool>();
    public WindowsUpdateExtendedReadTool WindowsUpdateExtendedReadTool => _factory.Create<WindowsUpdateExtendedReadTool>();
    public WindowsUpdateReadTool WindowsUpdateReadTool => _factory.Create<WindowsUpdateReadTool>();
    public WirelessExtendedReadTool WirelessExtendedReadTool => _factory.Create<WirelessExtendedReadTool>();
    public WirelessReadTool WirelessReadTool => _factory.Create<WirelessReadTool>();
    public WmiQueryTool WmiQueryTool => _factory.Create<WmiQueryTool>();
}
