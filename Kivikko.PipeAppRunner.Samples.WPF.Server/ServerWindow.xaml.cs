using System.Windows;
using System.Windows.Input;

namespace Kivikko.PipeAppRunner.Samples.WPF.Server;

public partial class ServerWindow
{
    public ServerWindow()
    {
        InitializeComponent();
        DataContext = new ServerViewModel(new ServerLogic());
    }

    private void UIElement_OnKeyDown(object sender, KeyEventArgs e)
    {
        switch (e)
        {
            case { Key: Key.Enter } when
                DataContext is ServerViewModel viewModel:
                viewModel.SendCommand.Execute(null);
                break;
        }
    }

    private void FocusMessageInputTextBox(object sender, RoutedEventArgs e) =>
        MessageInputTextBox.Focus();
}