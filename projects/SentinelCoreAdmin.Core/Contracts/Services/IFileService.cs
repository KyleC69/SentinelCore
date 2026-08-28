// Solution: SentinelCore
// Project:   SentinelCoreAdmin.Core
// File:         IFileService.cs
// Author: Kyle L. Crowder
// Build Num:  082808



namespace SentinelCoreAdmin.Core.Contracts.Services;





public interface IFileService
{

    void Delete(string folderPath, string fileName);


    T? Read<T>(string folderPath, string fileName);


    void Save<T>(string folderPath, string fileName, T content);
}