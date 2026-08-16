// Solution: SentinelCore
// Project:   SentinelCoreAdmin
// File:         PersistAndRestoreService.cs
// Author: Kyle L. Crowder
// Build Num:  081602



using System.Collections;
using System.IO;

using JetBrains.Annotations;

using Microsoft.Extensions.Options;

using SentinelCoreAdmin.Contracts.Services;
using SentinelCoreAdmin.Core.Contracts.Services;
using SentinelCoreAdmin.Models;




namespace SentinelCoreAdmin.Services;





public class PersistAndRestoreService : IPersistAndRestoreService
{
    private readonly AppConfig _appConfig;
    private readonly IFileService _fileService;
    private readonly string _localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);








    public PersistAndRestoreService([CanBeNull] IFileService fileService, [NotNull] IOptions<AppConfig> appConfig)
    {
        _fileService = fileService;
        _appConfig = appConfig.Value;
    }








    public void PersistData()
    {
        if (App.Current.Properties != null)
        {
            string folderPath = Path.Combine(_localAppData, _appConfig.ConfigurationsFolder);
            string fileName = _appConfig.AppPropertiesFileName;
            _fileService.Save(folderPath, fileName, App.Current.Properties);
        }
    }








    public void RestoreData()
    {
        string folderPath = Path.Combine(_localAppData, _appConfig.ConfigurationsFolder);
        string fileName = _appConfig.AppPropertiesFileName;
        IDictionary properties = _fileService.Read<IDictionary>(folderPath, fileName);
        if (properties != null)
        {
            foreach (DictionaryEntry property in properties) App.Current.Properties.Add(property.Key, property.Value);
        }
    }
}