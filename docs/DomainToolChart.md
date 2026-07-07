| **Domain**     | **Recommended Safe API / System Interface**                              |
| -------------- | ------------------------------------------------------------------------ |
| registry       | Win32 Registry API (RegOpenKeyEx, RegQueryValueEx)                       |
| filesystem     | Win32 File APIs, NT Native APIs                                          |
| environment    | Win32 Environment APIs                                                   |
| bootconfig     | BCD API                                                                  |
| accessibility  | UIAutomation API                                                         |
| searchindexing | Windows Search API                                                       |
| shellexplorer  | Shell API (IShellFolder, IShellLink)                                     |
| certificates   | CertEnroll API, CryptoAPI                                                |
| eventlog       | WEVTAPI (Windows Event Log API)                                          |
| applocker      | AppLocker Policy API                                                     |
| windowsupdate  | Windows Update Agent (WUA) API                                           |
| pnpdevices     | Configuration Manager (CM) API                                           |
| hyperv         | Hyper‑V WMI v2 (CIM‑based, safe)                                         |
| audio          | Core Audio APIs (MMDevice API)                                           |
| printers       | Print Spooler API                                                        |
| grouppolicy    | LGPO API, GroupPolicy COM                                                |
| firewall       | Windows Firewall API (INetFwPolicy2)                                     |
| localaccounts  | LSA API                                                                  |
| rdp            | Terminal Services API                                                    |
| services       | Service Control Manager (SCM) API                                        |
| scheduledtasks | Task Scheduler 2.0 COM API                                               |
| power          | PowerCfg API                                                             |
| network        | NetCfg API, IP Helper API                                                |
| dcom           | COM/DCOM APIs                                                            |
| wmi            | CIM / MI APIs (NOT classic WMI)                                          |
| drivers        | SCM Driver APIs, CM API                                                  |
| processes      | NT Query APIs, ToolHelp32Snapshot                                        |
| performance    | PDH API                                                                  |
| installedapps  | MSI API, AppX Deployment API                                             |
| browserconfig  | Registry + Browser COM APIs                                              |
| fonts          | GDI Font APIs                                                            |
| notifications  | Windows Notification Platform API                                        |
| vpn            | RAS API                                                                  |
| wireless       | Native Wi‑Fi API                                                         |
| proxy          | WinHTTP API                                                              |
| sensors        | Sensor API                                                               |
| battery        | PowerCfg API                                                             |
| display        | DXGI APIs, Win32 Display APIs                                            |
| credentials    | Credential Manager API                                                   |
| UAC            | TokenElevation APIs, SecurityDescriptor APIs                             |
| defender       | Windows Security Center API                                              |
| bitlocker      | BitLocker WMI v2 (CIM‑based), Manage‑bde API |
