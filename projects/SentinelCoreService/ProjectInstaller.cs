// Solution: SentinelCore
// Project:   SentinelCoreService
// File:         ProjectInstaller.cs
// Author: Kyle L. Crowder
// Build Num:  092308



using System.ComponentModel;
using System.Configuration.Install;




namespace SentinelCore;





[RunInstaller(true)]
public partial class ProjectInstaller : Installer
{
    public ProjectInstaller()
    {
        InitializeComponent();
    }
}