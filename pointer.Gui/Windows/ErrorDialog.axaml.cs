using Avalonia.Controls;
using Avalonia.Interactivity;
using pointer.Gui.Utils;

namespace pointer.Gui.Windows;

public partial class ErrorDialog : Window
{
    public ErrorDialog()
    {
        InitializeComponent();
        SoundEffects.Play("overlay-pop-in");
        Closed += (_, _) => SoundEffects.Play("overlay-pop-out");
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
