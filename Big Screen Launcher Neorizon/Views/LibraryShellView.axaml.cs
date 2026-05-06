using System;
using System.ComponentModel;
using Avalonia.Controls;
using Big_Screen_Launcher_Neorizon.ViewModels;

namespace Big_Screen_Launcher_Neorizon.Views;

public partial class LibraryShellView : UserControl
{
    private LibraryShellViewModel? _vm;

    public LibraryShellView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (_vm is not null)
        {
            _vm.PropertyChanged -= OnVmPropertyChanged;
        }

        _vm = DataContext as LibraryShellViewModel;
        if (_vm is not null)
        {
            _vm.PropertyChanged += OnVmPropertyChanged;
        }
    }

    private void OnVmPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(LibraryShellViewModel.SelectedIndex) || _vm is null)
        {
            return;
        }

        Avalonia.Threading.Dispatcher.UIThread.Post(ScrollSelectedIntoView);
    }

    private void ScrollSelectedIntoView()
    {
        if (_vm is null || _vm.SelectedIndex < 0 || _vm.SelectedIndex >= GameRailList.ItemCount)
        {
            return;
        }

        var selectedItem = GameRailList.Items[_vm.SelectedIndex];
        if (selectedItem is not null)
        {
            GameRailList.ScrollIntoView(selectedItem);
        }
    }
}
