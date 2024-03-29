﻿namespace Kivikko.PipeAppRunner;

internal class PipeOut
{
    private readonly string _pipeName;
    private readonly NamedPipeClientStream _pipeClientStream;

    internal PipeOut(string pipeName)
    {
        _pipeName = pipeName;
        _pipeClientStream = new NamedPipeClientStream(".", pipeName, PipeDirection.Out, PipeOptions.Asynchronous);
    }

    internal bool IsConnected => _pipeClientStream?.IsConnected ?? false;
    
    internal event EventHandler Connected;
    internal event EventHandler Disconnected;
    
    private void OnConnected() => Connected?.Invoke(this, EventArgs.Empty);
    private void OnDisconnected() => Disconnected?.Invoke(this, EventArgs.Empty);
    
    internal async void Start()
    {
        await _pipeClientStream.ConnectAsync();
        var handshakeMessage = await SendMessage(_pipeName);
        
        if (handshakeMessage.IsSuccess)
            OnConnected();
        else
            OnDisconnected();
    }

    internal void Stop()
    {
        try
        {
            if (!_pipeClientStream.IsConnected)
                return;
            
            _pipeClientStream.WaitForPipeDrain();
            OnDisconnected();
        }
        finally
        {
            _pipeClientStream.Close();
            _pipeClientStream.Dispose();
        }
    }
    
    internal async Task<TaskResult> SendMessage(string message)
    {
        var taskCompletionSource = new TaskCompletionSource<TaskResult>();
        
        try
        {
            if (!_pipeClientStream.IsConnected || !IsConnected)
                return new TaskResult
                {
                    IsSuccess = false,
                    Status = PipeBase.Status.ServiceUnavailable,
                    ErrorMessage = $"[.\\{_pipeName}]: Cannot send message, pipe is not connected."
                };

            var buffer = Encoding.UTF8.GetBytes(message);
        
            _pipeClientStream.BeginWrite(buffer, 0, buffer.Length, asyncResult =>
            {
                try
                {
                    taskCompletionSource.SetResult(EndWriteCallBack(asyncResult));
                }
                catch (Exception exception)
                {
                    taskCompletionSource.SetException(exception);
                }

            }, null);

            Debug.WriteLine($"[{_pipeName}] send a message: {message}");
            
            return await taskCompletionSource.Task;
        }
        catch (Exception exception)
        {
            taskCompletionSource.SetException(exception);
            return new TaskResult
            {
                IsSuccess = false,
                Status = PipeBase.Status.InternalError,
                ErrorMessage = $"{exception.Message}\n\t{exception.StackTrace}"
            };
        }
    }
    
    private TaskResult EndWriteCallBack(IAsyncResult asyncResult)
    {
        try
        {
            _pipeClientStream.EndWrite(asyncResult);
            _pipeClientStream.Flush();
            return new TaskResult
            {
                IsSuccess = true,
                Status = PipeBase.Status.Success
            };
        }
        catch (Exception exception)
        {
            return new TaskResult
            {
                IsSuccess = false,
                Status = PipeBase.Status.InternalError,
                ErrorMessage = exception.Message
            };
        }
    }
}