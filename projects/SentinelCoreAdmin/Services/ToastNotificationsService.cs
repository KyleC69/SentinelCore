// Solution: SentinelCore
// Project:   SentinelCoreAdmin
// File:         ToastNotificationsService.cs
// Author: Kyle L. Crowder
// Build Num:  081602



using Windows.UI.Notifications;

using CommunityToolkit.WinUI.Notifications;

using JetBrains.Annotations;

using SentinelCoreAdmin.Contracts.Services;




namespace SentinelCoreAdmin.Services;





public partial class ToastNotificationsService : IToastNotificationsService
{

    public void ShowToastNotification([CanBeNull] ToastNotification toastNotification)
    {
        ToastNotificationManagerCompat.CreateToastNotifier().Show(toastNotification);
    }
}