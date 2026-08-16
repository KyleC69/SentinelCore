// Solution: SentinelCore
// Project:   SentinelCoreAdmin
// File:         UserDataService.cs
// Author: Kyle L. Crowder
// Build Num:  081602



using System.IO;
using System.Windows.Media.Imaging;

using JetBrains.Annotations;

using Microsoft.Extensions.Options;

using SentinelCoreAdmin.Contracts.Services;
using SentinelCoreAdmin.Core.Contracts.Services;
using SentinelCoreAdmin.Core.Models;
using SentinelCoreAdmin.Helpers;
using SentinelCoreAdmin.Models;
using SentinelCoreAdmin.ViewModels;




namespace SentinelCoreAdmin.Services;





public class UserDataService : IUserDataService
{
    private readonly AppConfig _appConfig;
    private readonly IFileService _fileService;
    private readonly IIdentityService _identityService;
    private readonly string _localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
    private readonly IMicrosoftGraphService _microsoftGraphService;
    private UserViewModel _user;








    public UserDataService([CanBeNull] IFileService fileService, [CanBeNull] IIdentityService identityService, [CanBeNull] IMicrosoftGraphService microsoftGraphService, [NotNull] IOptions<AppConfig> appConfig)
    {
        _fileService = fileService;
        _identityService = identityService;
        _microsoftGraphService = microsoftGraphService;
        _appConfig = appConfig.Value;
    }








    [CanBeNull]
    public UserViewModel GetUser()
    {
        if (_user == null)
        {
            _user = GetUserFromCache();
            if (_user == null)
            {
                _user = GetDefaultUserData();
            }
        }

        return _user;
    }








    public void Initialize()
    {
        _identityService.LoggedIn += OnLoggedIn;
        _identityService.LoggedOut += OnLoggedOut;
    }








    public event EventHandler<UserViewModel> UserDataUpdated;








    private UserViewModel GetDefaultUserData()
    {
        return new UserViewModel { Name = _identityService.GetAccountUserName(), Photo = ImageHelper.ImageFromAssetsFile("DefaultIcon.png") };
    }








    [CanBeNull]
    private UserViewModel GetUserFromCache()
    {
        string folderPath = Path.Combine(_localAppData, _appConfig.ConfigurationsFolder);
        string fileName = _appConfig.UserFileName;
        User cacheData = _fileService.Read<User>(folderPath, fileName);
        return GetUserViewModelFromData(cacheData);
    }








    [ItemCanBeNull]
    private async Task<UserViewModel> GetUserFromGraphApiAsync()
    {
        string accessToken = await _identityService.GetAccessTokenForGraphAsync();
        if (string.IsNullOrEmpty(accessToken))
        {
            return null;
        }

        User userData = await _microsoftGraphService.GetUserInfoAsync(accessToken);
        if (userData != null)
        {
            userData.Photo = await _microsoftGraphService.GetUserPhoto(accessToken);
            string folderPath = Path.Combine(_localAppData, _appConfig.ConfigurationsFolder);
            string fileName = _appConfig.UserFileName;
            _fileService.Save(folderPath, fileName, userData);
        }

        return GetUserViewModelFromData(userData);
    }








    [CanBeNull]
    private UserViewModel GetUserViewModelFromData([CanBeNull] User userData)
    {
        if (userData == null)
        {
            return null;
        }

        BitmapImage userPhoto = string.IsNullOrEmpty(userData.Photo) ? ImageHelper.ImageFromAssetsFile("DefaultIcon.png") : ImageHelper.ImageFromString(userData.Photo);

        return new UserViewModel { Name = userData.DisplayName, UserPrincipalName = userData.UserPrincipalName, Photo = userPhoto };
    }








    private async void OnLoggedIn([CanBeNull] object sender, [CanBeNull] EventArgs e)
    {
        _user = await GetUserFromGraphApiAsync();
        UserDataUpdated?.Invoke(this, _user);
    }








    private void OnLoggedOut([CanBeNull] object sender, [CanBeNull] EventArgs e)
    {
        _user = null;
        string folderPath = Path.Combine(_localAppData, _appConfig.ConfigurationsFolder);
        string fileName = _appConfig.UserFileName;
        _fileService.Save<User>(folderPath, fileName, null);
    }
}