using System;
using Avalonia.Controls;
using PCL.Core.App.IoC;
using PCL.Core.Logging;

namespace Big_Screen_Launcher_Neorizon;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        Lifecycle.OnLoading();
        Lifecycle.OnWindowCreated();
        LogWrapper.Info("MainWindow opened, lifecycle WindowCreated triggered");
    }
}