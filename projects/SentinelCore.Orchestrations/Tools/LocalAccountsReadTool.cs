// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         LocalAccountsReadTool.cs
// Author: Kyle L. Crowder
// Build Num:  080801



using System.ComponentModel;
using System.DirectoryServices.AccountManagement;
using System.Text;




namespace SentinelCore.Tools;





/// <summary>
///     Read-only tool for querying local users and groups.
/// </summary>
public sealed class LocalAccountsReadTool : AITool
{
    public override string Description { get; } = "Read-only tool for querying local users and groups.";
    public override string Name { get; } = "Local_Accounts_Read";








    [Description("Lists local groups and their members.")]
    public Task<ToolResult> local_group_list([Description("Optional group name to filter. If provided, members of that group are listed.")] string? groupName = null)
    {
        try
        {
            StringBuilder sb = new();
            using PrincipalContext context = new(ContextType.Machine);
            GroupPrincipal groupPrincipal = new(context);
            using PrincipalSearcher searcher = new(groupPrincipal);
            foreach (GroupPrincipal? group in searcher.FindAll().Cast<GroupPrincipal>())
            {
                if (!string.IsNullOrWhiteSpace(groupName) && !group.Name.Equals(groupName, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                sb.AppendLine($"Group={group.Name}, Description={group.Description}");
                foreach (Principal? member in group.GetMembers())
                    sb.AppendLine($"  Member={member.Name} ({member.StructuralObjectClass})");
            }

            return Task.FromResult(ToolResult.Ok(sb.ToString()));
        }
        catch (Exception ex)
        {
            return Task.FromResult(ToolResult.Fail($"Local group listing failed: {ex.Message}"));
        }
    }








    [Description("Lists local user accounts on the system.")]
    public Task<ToolResult> local_user_list()
    {
        try
        {
            StringBuilder sb = new();
            using PrincipalContext context = new(ContextType.Machine);
            UserPrincipal userPrincipal = new(context);
            using PrincipalSearcher searcher = new(userPrincipal);
            foreach (UserPrincipal? user in searcher.FindAll().Cast<UserPrincipal>())
                sb.AppendLine($"Name={user.Name}, Enabled={user.Enabled}, LastLogon={user.LastLogon}, PasswordNeverExpires={user.PasswordNeverExpires}, UserCannotChangePassword={user.UserCannotChangePassword}");

            return Task.FromResult(ToolResult.Ok(sb.ToString()));
        }
        catch (Exception ex)
        {
            return Task.FromResult(ToolResult.Fail($"Local user listing failed: {ex.Message}"));
        }
    }
}