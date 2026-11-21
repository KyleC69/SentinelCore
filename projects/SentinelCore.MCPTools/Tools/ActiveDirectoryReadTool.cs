// Solution: Sentinel-MCP-Projects
// Project:   SentinelCore-mcp
// File:         ActiveDirectoryReadTool.cs
// Author: Kyle L. Crowder
// Build Num:  110104



using System.ComponentModel;
using System.DirectoryServices;
using System.DirectoryServices.ActiveDirectory;
using System.Runtime.Versioning;
using System.Text;

using ModelContextProtocol.Server;




namespace SentinelCoreMCP.Tools;





/// <summary>
///     Read-only tools for Active Directory reconnaissance, mirroring the
///     Sysinternals ADExplorer and ADInsight workflows.
///     Both tools are guarded: when the machine is not domain-joined or the AD
///     directory services are unavailable, they return a structured failure
///     instead of crashing (spec §2.4, §6.2).
/// </summary>
[McpServerToolType]
[SupportedOSPlatform("windows")]
public sealed class ActiveDirectoryReadTool
{


    /// <summary>
    ///     Reports whether the host is joined to an Active Directory domain and, when
    ///     it is, returns the domain context that ADExplorer would browse.
    /// </summary>
    /// <returns>A <see cref="ToolResult" /> containing the domain availability record.</returns>
    [SupportedOSPlatform("windows")]
    [McpServerTool(Name = "sc_identity_check_domain_availability", ReadOnly = true, Destructive = false)]
    [Description("Checks whether this host is joined to an Active Directory domain. ADExplorer and ADInsight require a domain-joined host with reachable domain controllers.")]
    public async Task<ToolResult> ActiveDirectoryAvailabilityAsync()
    {
        return await Task.Run(() =>
                {
                    if (!TryGetDomain(out Domain? domain))
                    {
                        return ToolResult.Fail("This host is not joined to an Active Directory domain.", "Active Directory availability");
                    }

                    var result = new { DomainJoined = true, domain!.Name, ForestName = domain.Forest.Name, DomainControllers = domain.DomainControllers.OfType<DomainController>().Take(10).Select(dc => dc.Name).ToList() };

                    return ToolResult.Ok(result, "Active Directory domain detected.");
                })
                .ConfigureAwait(false);
    }








    /// <summary>
    ///     Browses an Active Directory container and lists its child objects,
    ///     mirroring the ADExplorer tree view in a read-only manner.
    /// </summary>
    /// <param name="ldapPath">
    ///     The LDAP path of the container to browse, e.g. LDAP://DC=contoso,DC=com. Defaults to the root
    ///     naming context.
    /// </param>
    /// <param name="maxRecords">Maximum number of child objects to return. Defaults to 50.</param>
    /// <returns>A <see cref="ToolResult" /> containing the child object listing.</returns>
    [SupportedOSPlatform("windows")]
    [McpServerTool(Name = "sc_identity_browse_ad_container", ReadOnly = true, Destructive = false)]
    [Description("Browses an Active Directory container and lists child objects (ADExplorer-style tree read). Requires a domain-joined host.")]
    public async Task<ToolResult> ActiveDirectoryBrowseContainerAsync([Description("The LDAP path of the container to browse, e.g. LDAP://DC=contoso,DC=com. Defaults to the root naming context.")] string? ldapPath = null, [Description("Maximum number of child objects to return. Defaults to 50.")] int maxRecords = 50)
    {
        ToolResult? maxValidation = InputValidator.ValidateMaxRecords(maxRecords);
        if (maxValidation is not null)
        {
            return maxValidation;
        }

        if (ldapPath is not null && !ldapPath.StartsWith("LDAP://", StringComparison.OrdinalIgnoreCase) && !ldapPath.StartsWith("GC://", StringComparison.OrdinalIgnoreCase))
        {
            return ToolResult.Fail("ldapPath must start with LDAP:// or GC://.", "Active Directory browse");
        }

        if (!TryGetDomain(out Domain? domain))
        {
            return ToolResult.Ok("This host is not joined to an Active Directory domain.", "Active Directory browse");
        }

        return await Task.Run(() =>
                {
                    try
                    {
                        string path = string.IsNullOrWhiteSpace(ldapPath) ? $"LDAP://{domain!.Name}" : ldapPath;

                        using DirectoryEntry container = new(path);
                        StringBuilder sb = new();
                        sb.AppendLine($"Container: {path}");
                        sb.AppendLine($"Name: {container.Name}");
                        sb.AppendLine();

                        int count = 0;
                        foreach (DirectoryEntry child in container.Children)
                        {
                            using (child)
                            {
                                if (count >= maxRecords)
                                {
                                    break;
                                }

                                sb.AppendLine($"  {child.SchemaClassName}: {child.Name}");
                                count++;
                            }
                        }

                        sb.AppendLine();
                        sb.AppendLine($"Child objects returned: {count}");

                        return ToolResult.Ok(sb.ToString(), "Active Directory container browse complete.");
                    }
                    catch (DirectoryServicesCOMException ex)
                    {
                        return ToolResult.Fail($"Active Directory browse failed: {ex.Message}", "Active Directory browse");
                    }
                    catch (Exception ex)
                    {
                        return ToolResult.Fail(ex.Message, "Active Directory browse");
                    }
                })
                .ConfigureAwait(false);
    }








    /// <summary>
    ///     Determines whether the machine is joined to an Active Directory domain.
    /// </summary>
    /// <param name="domain">The detected domain, when domain-joined.</param>
    /// <returns>True when the machine is domain-joined; otherwise false.</returns>
    private static bool TryGetDomain(out Domain? domain)
    {
        try
        {
            Domain currentDomain = Domain.GetComputerDomain();
            if (currentDomain is null)
            {
                // Defensive: the API can return null on misconfigured hosts.
                currentDomain = null!;
            }

            // Force a property read so a non-domain-joined machine throws here
            // rather than in the caller.
            _ = currentDomain.Name;

            domain = currentDomain;
            return true;
        }
        catch (ActiveDirectoryObjectNotFoundException)
        {
            // Machine is not domain-joined.
            domain = null;
            return false;
        }
        catch (ActiveDirectoryOperationException)
        {
            // Domain controller could not be contacted.
            domain = null;
            return false;
        }

    }
}
