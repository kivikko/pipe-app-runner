namespace Kivikko.PipeAppRunner.Samples.WPF.Client;

public class ClientViewModel : ViewModelBase
{
    private readonly ClientLogic _logic;
    public ClientViewModel(ClientLogic logic)
    {
        _logic = logic;
        _logic.MessageReceived += (_, message) => Chat += $"\nServer: {message}";
    }

    public string? Chat { get; set; }
    public string? Message { get; set; }
    public Command SendCommand => new(() =>
    {
        if (string.IsNullOrWhiteSpace(Message))
            return;
        
        _logic.SendMessage(Message);
        Chat += $"\nClient: {Message}";
        Message = string.Empty;
    });
}