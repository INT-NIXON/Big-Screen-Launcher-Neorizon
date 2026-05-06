namespace Big_Screen_Launcher_Neorizon.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    public MainWindowViewModel(LibraryShellViewModel shell)
    {
        Shell = shell;
    }

    public LibraryShellViewModel Shell { get; }
}
