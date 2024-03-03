using System;

namespace Kivikko.PipeAppRunner.Samples.WPF.Client;

public class ClientLogic
{
    public event EventHandler<string>? MessageReceived;
    
    private readonly PipeClient _pipeClient;

    public ClientLogic(PipeClient pipeClient)
    {
        _pipeClient = pipeClient;
        _pipeClient.MessageReceived += (sender, message) => MessageReceived?.Invoke(sender, message);
    }

    public async void SendMessage(string message)
    {
        await _pipeClient.SendMessage(message);
    }
}