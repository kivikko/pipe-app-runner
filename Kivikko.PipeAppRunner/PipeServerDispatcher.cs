namespace Kivikko.PipeAppRunner;

public class PipeServerDispatcher
{
    private readonly List<PipeServer> _pipeServers = new();
    public PipeServer Create()
    {
        var pipeServer = PipeServer.Create();
        pipeServer.Connected    += PipeServerOnConnected;
        pipeServer.Disconnected += PipeServerOnDisconnected;
        return pipeServer;
    }

    private void PipeServerOnConnected(object sender, EventArgs e)
    {
        if (sender is PipeServer pipeServer) _pipeServers.Add(pipeServer);
    }

    private void PipeServerOnDisconnected(object sender, EventArgs e)
    {
        if (sender is PipeServer pipeServer) Remove(pipeServer);
    }

    public void Remove(PipeServer pipeServer)
    {
        _pipeServers.Remove(pipeServer);
        pipeServer.Connected    -= PipeServerOnConnected;
        pipeServer.Disconnected -= PipeServerOnDisconnected;
    }

    public void SendMessageAll(string message)
    {
        _pipeServers.ForEach(SendMessage);
        return;
        async void SendMessage(PipeServer x) => await x.SendMessage(message);
    }
    public void SendAll(string endpoint, string parameter = null)
    {
        Task.Run(() => _pipeServers.ForEach(SendMessage));
        return;
        async void SendMessage(PipeServer x) => await x.Send(endpoint, parameter);
    }
}