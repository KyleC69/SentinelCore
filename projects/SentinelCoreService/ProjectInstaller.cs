// Solution: SentinelCore
// Project:   SentinelCoreService
// File:         ProjectInstaller.cs
// Author: Kyle L. Crowder
// Build Num:  082808



using System.ComponentModel;




namespace SentinelCore;





[RunInstaller(true)]
public partial class ProjectInstaller : System.Configuration.Install.Installer
{
    public ProjectInstaller()
    {
        InitializeComponent();
    }
}