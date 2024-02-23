namespace Kivikko.PipeAppRunner;

internal class PipeIn
{
    private readonly string _pipeName;
    private readonly NamedPipeServerStream _pipeServer;
    private bool _isStopping;
    private readonly object _lockingObject = new();
    private const int BufferSize = 2048;
    
    internal PipeIn(string pipeName)
    {
        _pipeName = pipeName;
        _pipeServer = new NamedPipeServerStream(
            pipeName,
            PipeDirection.In,
            1,
            PipeTransmissionMode.Message,
            PipeOptions.Asynchronous);
    }

    internal bool IsConnected => _pipeServer?.IsConnected ?? false;
    
    internal event EventHandler Connected;
    internal event EventHandler Disconnected;
    internal event EventHandler<string> MessageReceived;
    
    private void OnMessageReceived(string message) => MessageReceived?.Invoke(this, message);
    private void OnConnected() => Connected?.Invoke(this, EventArgs.Empty);
    private void OnDisconnected() => Disconnected?.Invoke(this, EventArgs.Empty);
    
    internal void Start() => _pipeServer.BeginWaitForConnection(WaitForConnectionCallBack, null);

    internal void Stop()
    {
        try
        {
            if (!_pipeServer.IsConnected)
                return;
                
            _isStopping = true;
            _pipeServer.Disconnect();
            OnDisconnected();
        }
        finally
        {
            _pipeServer.Close();
            _pipeServer.Dispose();
            _isStopping = false;
        }
    }
    
    private void WaitForConnectionCallBack(IAsyncResult result)
    {
        if (_isStopping) return;
            
        lock (_lockingObject)
        {
            if (_isStopping) return;
                
            _pipeServer.EndWaitForConnection(result);
            OnConnected();
            BeginRead(new PipeMessage());
        }
    }
    
    private void BeginRead(PipeMessage pipeMessage)
    {
        _pipeServer.BeginRead(pipeMessage.Buffer, 0, BufferSize, EndReadCallBack, pipeMessage);
    }
    
    private void EndReadCallBack(IAsyncResult result)
    {
        var readBytes = _pipeServer.EndRead(result);
        
        if (readBytes > 0)
        {
            var info = (PipeMessage) result.AsyncState;
            info.StringBuilder.Append(Encoding.UTF8.GetString(info.Buffer, 0, readBytes));

            if (!_pipeServer.IsMessageComplete)
            {
                BeginRead(info);
            }
            else
            {
                var message = info.StringBuilder.ToString().TrimEnd('\0');
                
                Debug.WriteLine($"[{_pipeName}] received a message: {message}");
                
                if (!IsHandshake(message))
                    OnMessageReceived(message);
                
                BeginRead(new PipeMessage());
            }
        }
        else
        {
            if (_isStopping) return;
                
            lock (_lockingObject)
            {
                if (_isStopping) return;
                Stop();
            }
        }
    }

    private bool IsHandshake(string message) => message == _pipeName;

    private class PipeMessage
    {
        public readonly byte[] Buffer;
        public readonly StringBuilder StringBuilder;

        public PipeMessage()
        {
            Buffer = new byte[BufferSize];
            StringBuilder = new StringBuilder();
        }
    }
}