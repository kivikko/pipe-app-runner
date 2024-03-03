using System;
using System.IO;
using System.Windows;

namespace Kivikko.PipeAppRunner.Samples.WPF.Server;

public class ServerLogic
{
    public event EventHandler<string>? MessageReceived;
    
    private readonly PipeServerDispatcher _pipeServerDispatcher = new();
    private readonly string _mutexName = Guid.NewGuid().ToString();
    private readonly string _clientPath = Path.Combine(
        "..", "..", "..", "..",
        "Kivikko.PipeAppRunner.Samples.WPF.Client",
        "bin", "debug", "net7.0-windows", "Kivikko.PipeAppRunner.Samples.WPF.Client.exe");
    
    public void StartClient(bool clientOnTop)
    {
        if (!File.Exists(_clientPath))
        {
            MessageBox.Show("The client file wasn't found.\nBuild the 'Kivikko.PipeAppRunner.Samples.WPF.Client' project.");
            return;
        }
        
        PipeAppRunner.StartFromOwner(
            _pipeServerDispatcher.Create(),
            _clientPath,
            new PipeAppRunnerOptions
            {
                ClientWindowOnTopOfOwner = clientOnTop,
                ClientsNumber = 1,
                MutexName = _mutexName
            },
            new PipeAppRunnerActions
            {
                AddServices = pipeServer =>
                {
                    pipeServer.MessageReceived += (sender, message) => MessageReceived?.Invoke(sender, message);
                } 
            });
    }

    public void SendMessage(string message)
    {
        _pipeServerDispatcher.SendMessageAll(message);
    }
}