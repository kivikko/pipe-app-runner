using System.Windows;
using System.Windows.Input;

namespace Kivikko.PipeAppRunner.Samples.WPF.Client;

public partial class ClientWindow
{
    public ClientWindow() => InitializeComponent();
    public ClientWindow(ClientLogic clientLogic) : this() =>
        DataContext = new ClientViewModel(clientLogic);

    private void UIElement_OnKeyDown(object sender, KeyEventArgs e)
    {
        switch (e)
        {
            case { Key: Key.Enter } when
                DataContext is ClientViewModel viewModel:
                viewModel.SendCommand.Execute(null);
                break;
        }
    }

    private void FocusMessageInputTextBox(object sender, RoutedEventArgs e) =>
        MessageInputTextBox.Focus();
}