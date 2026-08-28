// Solution: SentinelCore
// Project:   SentinelCoreAdmin
// File:         IPageService.cs
// Author: Kyle L. Crowder
// Build Num:  082808



using System.Windows.Controls;




namespace SentinelCoreAdmin.Contracts.Services;





public interface IPageService
{

    Page GetPage(string key);


    Type GetPageType(string key);
}