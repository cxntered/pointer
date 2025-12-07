using Avalonia.Controls;
using Avalonia.Interactivity;

namespace pointer.Gui.Windows;

public partial class ErrorDialog : Window
{
    public ErrorDialog()
    {
        InitializeComponent();
    }

    public ErrorDialog(string message) : this()
    {
        MessageText.Text = message;
    }

    private void OnButtonClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}
