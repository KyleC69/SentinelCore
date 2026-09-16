// Solution: SentinelCore
// Project:   SentinelCore.UI
// File:         AgentAssignmentRow.cs
// Author: Kyle L. Crowder
// Build Num:  091418



using CommunityToolkit.Mvvm.ComponentModel;




namespace SentinelCore.UI.Models;





/// <summary>
///     Represents one logical agent in the per-server assignment editor shown on the
///     <see cref="SentinelCore.UI.Views.McpServersPage" />. An unchecked row means the
///     agent does not receive the server's tools; when no row is checked the server is
///     available to all agents.
/// </summary>
public sealed partial class AgentAssignmentRow : ObservableObject
{

    /// <summary>
    ///     Gets or sets a value indicating whether this agent is assigned to use the server.
    ///     Observable so programmatic resets (form cancel/reopen) refresh bound checkboxes.
    /// </summary>
    [ObservableProperty] private bool _isAssigned;

    /// <summary>
    ///     The logical agent name.
    /// </summary>
    public string AgentName { get; set; } = string.Empty;
}