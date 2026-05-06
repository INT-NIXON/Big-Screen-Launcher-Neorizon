using System;
using Avalonia.Controls;
using Avalonia.Input;
using Big_Screen_Launcher_Neorizon.Input;
using Big_Screen_Launcher_Neorizon.Services;
using Big_Screen_Launcher_Neorizon.ViewModels;
using BSLN.Core.Domain;
using PCL.Core;
using PCL.Core.App.IoC;
using PCL.Core.Logging;


namespace Big_Screen_Launcher_Neorizon;

public partial class MainWindow : Window
{
    private readonly MainWindowViewModel? _viewModel;

    public MainWindow()
    {
        InitializeComponent();
    }

    public MainWindow(MainWindowViewModel viewModel, WindowPresentationService windowPresentationService)
        : this()
    {
        _viewModel = viewModel;
        DataContext = viewModel;
        windowPresentationService.ApplyBigScreenPresentation(this, fullscreen: true);
        Opened += OnOpened;
    }

    private async void OnOpened(object? sender, EventArgs e)
    {
        Lifecycle.OnWindowCreated();
        LogWrapper.Info("MainWindow opened, lifecycle WindowCreated triggered");

        if (_viewModel is null)
        {
            return;
        }

        _viewModel.Shell.ActivateRuntime();
        await _viewModel.Shell.InitializeAsync();
    }

    protected override async void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        if (_viewModel is null || !AvaloniaKeyInputAdapter.TryMap(e.Key, out var action))
        {
            return;
        }

        await _viewModel.Shell.SetInputFamilyAsync(InputDeviceFamily.KeyboardMouse);
        await _viewModel.Shell.HandleActionAsync(action);
        e.Handled = true;
    }
}
