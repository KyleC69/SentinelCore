// Solution: SentinelCore
// Project:   SentinelCoreAdmin
// File:         UserViewModel.cs
// Author: Kyle L. Crowder
// Build Num:  082808



using System.Windows.Media.Imaging;

using CommunityToolkit.Mvvm.ComponentModel;

using JetBrains.Annotations;




namespace SentinelCoreAdmin.ViewModels;





public class UserViewModel : ObservableObject
{
    private string? _name;
    private BitmapImage? _photo;
    private string? _userPrincipalName;

    [CanBeNull]
    public string Name
    {
        get => _name;
        set => this.SetProperty(ref _name, value);
    }

    [CanBeNull]
    public BitmapImage Photo
    {
        get => _photo;
        set => this.SetProperty(ref _photo, value);
    }

    [CanBeNull]
    public string UserPrincipalName
    {
        get => _userPrincipalName;
        set => this.SetProperty(ref _userPrincipalName, value);
    }
}