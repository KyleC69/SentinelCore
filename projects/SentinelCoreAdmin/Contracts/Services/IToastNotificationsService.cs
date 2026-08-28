// Solution: SentinelCore
// Project:   SentinelCoreAdmin
// File:         IToastNotificationsService.cs
// Author: Kyle L. Crowder
// Build Num:  082808



using Windows.UI.Notifications;




namespace SentinelCoreAdmin.Contracts.Services;





public interface IToastNotificationsService
{
    public void ShowToastNotification(ToastNotification toastNotification);


    public void ShowToastNotificationSample();
}