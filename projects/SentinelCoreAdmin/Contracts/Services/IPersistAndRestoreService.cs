// Solution: SentinelCore
// Project:   SentinelCoreAdmin
// File:         IPersistAndRestoreService.cs
// Author: Kyle L. Crowder
// Build Num:  082808



namespace SentinelCoreAdmin.Contracts.Services;





public interface IPersistAndRestoreService
{

    void PersistData();


    void RestoreData();
}