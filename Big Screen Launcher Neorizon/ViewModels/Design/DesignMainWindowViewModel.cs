namespace Big_Screen_Launcher_Neorizon.ViewModels.Design;

public sealed class DesignMainWindowViewModel : MainWindowViewModel
{
    public DesignMainWindowViewModel()
        : base(LibraryShellViewModel.CreateDesignViewModel())
    {
    }
}
