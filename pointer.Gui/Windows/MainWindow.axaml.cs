using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using pointer.Gui.ViewModels;

namespace pointer.Gui.Windows;

public partial class MainWindow : Window
{
    private readonly MainWindowViewModel _viewModel = new();

    public MainWindow()
    {
        InitializeComponent();

        DataContext = _viewModel;
        _viewModel.ErrorOccurred += OnErrorOccurred;
    }

    private async void OnErrorOccurred(object? sender, string message)
    {
        var dialog = new ErrorDialog(message);
        await dialog.ShowDialog(this);
    }

    private void OpenHardLinkInfo(object sender, RoutedEventArgs args)
    {
        Launcher.LaunchUriAsync(new System.Uri("https://osu.ppy.sh/wiki/en/Client/Release_stream/Lazer/File_storage#via-hard-links"));
    }
}