// Solution: Sentinel-MCP-Projects
// Project:   SentinelCore-mcp
// File:         LocalAccountsReadTool.cs
// Author: Kyle L. Crowder
// Build Num:  110104



using System.ComponentModel;
using System.DirectoryServices.AccountManagement;
using System.Runtime.Versioning;

using ModelContextProtocol.Server;




namespace SentinelCoreMCP.Tools;





/// <summary>
///     Read-only tool for querying local users and groups.
/// </summary>
[McpServerToolType]
public sealed class LocalAccountsReadTool
{

    [SupportedOSPlatform("windows")]
    [McpServerTool(Name = "sc_identity_list_groups", ReadOnly = true, Destructive = false)]
    [Description("Lists local groups and their members.")]
    public async Task<ToolResult> LocalGroupListAsync([Description("Optional group name to filter. If provided, members of that group are listed.")] string? groupName = null, [Description("Maximum number of groups to return. Defaults to 50.")] int maxRecords = 50)
    {
        try
        {
            List<object> results = new();

            using PrincipalContext context = new(ContextType.Machine);
            using GroupPrincipal filter = new(context);
            using PrincipalSearcher searcher = new(filter);

            PrincipalSearchResult<Principal> searchResults = searcher.FindAll();
            int count = 0;
            foreach (GroupPrincipal group in searchResults.OfType<GroupPrincipal>())
            {
                if (!string.IsNullOrWhiteSpace(groupName) && !group.Name.Equals(groupName, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (count >= maxRecords)
                {
                    break;
                }

                List<string> members = new();
                PrincipalSearchResult<Principal> groupMembers = group.GetMembers();
                foreach (Principal member in groupMembers)
                {
                    members.Add(Helpers.CleanValue($"{member.Name} ({member.StructuralObjectClass})"));
                }

                results.Add(new { Group = Helpers.CleanValue(group.Name), Description = Helpers.CleanValue(group.Description), Members = members });
                count++;
            }

            return ToolResult.Ok(results, "LocalAccountsReadTool");
        }
        catch (Exception ex)
        {
            return ToolResult.Fail(ex.Message, "LocalAccountsReadTool");
        }
    }








    [SupportedOSPlatform("windows")]
    [McpServerTool(Name = "sc_identity_list_users", ReadOnly = true, Destructive = false)]
    [Description("Lists local user accounts on the system.")]
    public async Task<ToolResult> LocalUserListAsync([Description("Maximum number of users to return. Defaults to 50.")] int maxRecords = 50)
    {
        try
        {
            List<object> results = new();

            using PrincipalContext context = new(ContextType.Machine);
            using UserPrincipal filter = new(context);
            using PrincipalSearcher searcher = new(filter);

            PrincipalSearchResult<Principal> searchResults = searcher.FindAll();
            int count = 0;
            foreach (UserPrincipal user in searchResults.OfType<UserPrincipal>())
            {
                if (count >= maxRecords)
                {
                    break;
                }

                results.Add(new
                {
                        Name = Helpers.CleanValue(user.Name),
                        user.Enabled,
                        LastLogon = user.LastLogon?.ToString(),
                        user.PasswordNeverExpires,
                        user.UserCannotChangePassword
                });
                count++;
            }

            return ToolResult.Ok(results, "LocalAccountsReadTool");
        }
        catch (Exception ex)
        {
            return ToolResult.Fail(ex.Message, "LocalAccountsReadTool");
        }
    }
}
