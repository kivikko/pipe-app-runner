namespace Kivikko.PipeAppRunner.Samples.WPF.Server;

public class ServerViewModel : ViewModelBase
{
    private readonly ServerLogic _logic;
    public ServerViewModel(ServerLogic logic)
    {
        _logic = logic;
        _logic.MessageReceived += (_, message) => Chat += $"\nClient: {message}";
    }

    public string? Chat { get; set; }
    public string? Message { get; set; }
    public bool ClientOnTop { get; set; }
    public Command SendCommand => new(() =>
    {
        if (string.IsNullOrWhiteSpace(Message))
            return;

        _logic.SendMessage(Message);
        Chat += $"\nServer: {Message}";
        Message = string.Empty;
    });
    public Command StartClientCommand => new(() => _logic.StartClient(ClientOnTop));
}