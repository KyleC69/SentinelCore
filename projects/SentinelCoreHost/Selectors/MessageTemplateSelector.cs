// Solution: SentinelCore
// Project:   SentinelCoreHost
// File:         MessageTemplateSelector.cs
// Author: Kyle L. Crowder
// Build Num:  080801



using System.Windows;
using System.Windows.Controls;

using Microsoft.Extensions.AI;




namespace SentinelCoreHost.Selectors;





/// <summary>
///     Routes each ChatMessage to the correct DataTemplate based on the sender's role.
/// </summary>
public sealed class MessageTemplateSelector : DataTemplateSelector
{

    public DataTemplate? AssistantTemplate { get; set; }
    public DataTemplate? ErrorTemplate { get; set; }
    public DataTemplate? UserTemplate { get; set; }








    public override DataTemplate? SelectTemplate(object item, DependencyObject container)
    {
        ChatMessage msg = item as ChatMessage ?? throw new ArgumentException("Item must be a ChatMessage.", nameof(item));

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
}