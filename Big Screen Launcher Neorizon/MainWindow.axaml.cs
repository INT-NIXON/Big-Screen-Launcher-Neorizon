using Avalonia.Controls;
using PCL.Core.App.IoC;
using PCL.Core.Logging;
using PCL.Core.App;

namespace Big_Screen_Launcher_Neorizon;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        Lifecycle.OnWindowCreated();
        LogWrapper.Info("MainWindow opened, lifecycle WindowCreated triggered");
    }
}