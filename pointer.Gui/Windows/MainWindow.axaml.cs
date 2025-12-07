using System.Threading.Tasks;
using Avalonia.Controls;
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
}