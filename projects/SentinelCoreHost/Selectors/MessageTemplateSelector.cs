// Solution: SentinelCoreLib
// Project:   SentinelCoreHost
// File:         MessageTemplateSelector.cs
// Author: Kyle L. Crowder
// Build Date: 2026/07/07



using System.Windows;
using System.Windows.Controls;

using Microsoft.Extensions.AI;




namespace SentinelCoreHost.Selectors;





/// <summary>
///     Routes each ChatMessage to the correct DataTemplate based on the sender's role.
/// </summary>
public sealed class MessageTemplateSelector : DataTemplateSelector
{

    public override DataTemplate? SelectTemplate(object item, DependencyObject container)
    {
        ChatMessage? msg = item as ChatMessage;

        switch (msg.Role.Value)
        {
            case "User":
                return UserTemplate;
            case "Assistant":
                return AssistantTemplate;

            default:
                return UserTemplate;
        }



    }








    public DataTemplate? AssistantTemplate { get; set; }
    public DataTemplate? ErrorTemplate { get; set; }
    public DataTemplate? UserTemplate { get; set; }
}