namespace Kivikko.PipeAppRunner;

public abstract class PipeBase
{
    public static class Status
    {
        public const int Success            = 200;
        public const int BadRequest         = 400;
        public const int NotFound           = 404;
        public const int InternalError      = 500;
        public const int ServiceUnavailable = 503;
    }

    protected int RequestId;
    
    private readonly PipeIn  _pipeIn;
    private readonly PipeOut _pipeOut;
    private readonly Dictionary<string, Action>                     _actionServices = new();
    private readonly Dictionary<string, Action<string>>             _parameterActionServices = new();
    private readonly Dictionary<string, Func<string>>               _funcServices = new();
    private readonly Dictionary<string, Func<Task<string>>>         _taskFuncServices = new();
    private readonly Dictionary<string, Func<string, string>>       _parameterServices = new();
    private readonly Dictionary<string, Func<string, Task<string>>> _taskParameterServices = new();
    private readonly Dictionary<int, Response>                      _responses = new();
    private bool _pipeInConnected;
    private bool _pipeOutConnected;

    protected PipeBase(string pipeInName, string pipeOutName)
    {
        _pipeIn = new PipeIn(pipeInName);
        _pipeIn.MessageReceived += OnMessageReceived;
        _pipeIn.Connected += OnConnected;
        _pipeIn.Disconnected += OnDisconnected;
        
        _pipeOut = new PipeOut(pipeOutName);
        _pipeOut.Connected += OnConnected;
        _pipeOut.Disconnected += OnDisconnected;
    }

    protected abstract int NewRequestId();
    protected abstract void RequestHandle(int requestId, string data);
    
    public string Name { get; protected set; }
    public bool IsConnected => _pipeInConnected && _pipeOutConnected;
    
    public PipeBase AddService(string endpoint, Action action)
    {
        _actionServices[endpoint] = action;
        return this;
    }
    public PipeBase AddService(string endpoint, Action<string> action)
    {
        _parameterActionServices[endpoint] = action;
        return this;
    }
    public PipeBase AddService(string endpoint, Func<string> func)
    {
        _funcServices[endpoint] = func;
        return this;
    }
    public PipeBase AddService(string endpoint, Func<Task<string>> func)
    {
        _taskFuncServices[endpoint] = func;
        return this;
    }
    public PipeBase AddService(string endpoint, Func<string, string> func)
    {
        _parameterServices[endpoint] = func;
        return this;
    }
    public PipeBase AddService(string endpoint, Func<string, Task<string>> func)
    {
        _taskParameterServices[endpoint] = func;
        return this;
    }
    
    public void Start()
    {
        _pipeIn.Start();
        _pipeOut.Start();
    }

    public void Stop()
    {
        _pipeOut.Stop();
        _pipeIn.Stop();
    }

    public event EventHandler Connected;
    public event EventHandler Disconnected;
    public event EventHandler<string> MessageReceived;

    private void OnConnected(object sender, EventArgs e)
    {
        switch (sender)
        {
            case PipeIn pipeIn:   _pipeInConnected = pipeIn.IsConnected;   break;
            case PipeOut pipeOut: _pipeOutConnected = pipeOut.IsConnected; break;
        }

        if (_pipeInConnected && _pipeOutConnected)
            Connected?.Invoke(sender, EventArgs.Empty);
    }

    private void OnDisconnected(object sender, EventArgs e)
    {
        switch (sender)
        {
            case PipeIn:  _pipeOut.Stop(); _pipeInConnected = false;  break;
            case PipeOut: _pipeIn.Stop();  _pipeOutConnected = false; break;
        }
        
        if (!_pipeInConnected && !_pipeOutConnected)
            Disconnected?.Invoke(sender, EventArgs.Empty);
    }

    private void OnMessageReceived(object sender, string message)
    {
        MessageReceived?.Invoke(sender, message);
        
        message.Split(':', out var requestId, out var data);
        
        if (string.IsNullOrWhiteSpace(data) ||
            !int.TryParse(requestId, out var requestIdAsInt))
            return;
        
        RequestHandle(requestIdAsInt, data);
    }

    protected void InputRequest(int requestId, string data)
    {
        data.Split(':', out var endpoint, out var parameter);
        
        Task.Run(async () =>
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(parameter))
                {
                    if (_parameterActionServices.TryGetValue(endpoint, out var action))
                    {
                        action(parameter);
                        await SendMessage($"{requestId}:{Status.Success}");
                    }

                    else if (_parameterServices.TryGetValue(endpoint, out var parameterResponseFunc))
                        await SendMessage($"{requestId}:{Status.Success}:{parameterResponseFunc.Invoke(parameter)}");

                    else if (_taskParameterServices.TryGetValue(endpoint, out var taskParameterResponseFunc))
                        await SendMessage($"{requestId}:{Status.Success}:{await taskParameterResponseFunc.Invoke(parameter)}");

                    else if (_funcServices.TryGetValue(endpoint, out var responseFunc))
                        await SendMessage($"{requestId}:{Status.Success}:{responseFunc.Invoke()}");

                    else if (_taskFuncServices.TryGetValue(endpoint, out var taskResponseFunc))
                        await SendMessage($"{requestId}:{Status.Success}:{await taskResponseFunc.Invoke()}");

                    else
                        await SendMessage($"{requestId}:{Status.BadRequest}:endpoint '{endpoint}' not found.");
                }
                
                else if (_actionServices.TryGetValue(endpoint, out var action))
                {
                    action();
                    await SendMessage($"{requestId}:{Status.Success}");
                }
                
                else if (_funcServices.TryGetValue(endpoint, out var responseFunc))
                    await SendMessage($"{requestId}:{Status.Success}:{responseFunc.Invoke()}");

                else if (_taskFuncServices.TryGetValue(endpoint, out var taskResponseFunc))
                    await SendMessage($"{requestId}:{Status.Success}:{await taskResponseFunc.Invoke()}");

                else if (_parameterServices.TryGetValue(endpoint, out var parameterResponseFunc))
                    await SendMessage($"{requestId}:{Status.Success}:{parameterResponseFunc.Invoke(null)}");

                else if (_taskParameterServices.TryGetValue(endpoint, out var taskParameterResponseFunc))
                    await SendMessage($"{requestId}:{Status.Success}:{await taskParameterResponseFunc.Invoke(null)}");

                else
                    await SendMessage($"{requestId}:{Status.NotFound}:endpoint '{endpoint}' not found.");
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
                await SendMessage($"{requestId}:{Status.BadRequest}:{exception.Message}, {exception.StackTrace}");
            }
        });
    }

    protected void OutputResponse(int requestId, string data)
    {
        data.Split(':', out var status, out var content);
        
        if (_responses.ContainsKey(requestId))
            _responses[requestId] = new Response(status, content);
        else
            _responses.Add(requestId, new Response(status, content));
    }

    public async Task<TaskResult> SendMessage(string message) => await _pipeOut.SendMessage(message);
    public async Task<TaskResult> SendErrorMessage(Exception exception) => await _pipeOut.SendMessage($"ERROR:{exception.Message}\n{exception.StackTrace}");
    public async Task<TaskResult> Send(string pipeEndpoint, string parameter = null)
    {
        var response = await Request<string>(pipeEndpoint, parameter);

        return new TaskResult
        {
            Status = response.Status,
            IsSuccess = response.IsSuccess,
            Content = response.Content,
            ErrorMessage = response.ErrorMessage
        };
    }

    public async Task<T> Get<T>(string endpoint, string parameter = null, Func<string, T> fromJson = null) where T : class =>
        (await Request(endpoint, parameter, fromJson)).Content;

    private async Task<TaskResult<T>> Request<T>(string endpoint, string parameter, Func<string, T> fromJson = null) where T : class
    {
        if (!IsConnected)
            return DisconnectedResult<T>();
        
        var requestId = NewRequestId();
        
        try
        {
            var request = string.Join(":", new[] { requestId.ToString(), endpoint, parameter }.Where(x => x is not null));
            await SendMessage(request);
            
            while (true)
            {
                if (!IsConnected)
                    return DisconnectedResult<T>();
                
                if (!_responses.TryGetValue(requestId, out var response))
                {
                    await Task.Delay(100);
                    continue;
                }
            
                _responses.Remove(requestId);
                
                return response.StatusCode switch
                {
                    >= 200 and < 300 => new TaskResult<T>
                    {
                        Status = response.StatusCode,
                        IsSuccess = true,
                        Content = typeof(T) != typeof(string)
                            ? fromJson?.Invoke(response.Content) ?? JsonUtils.FromJson<T>(response.Content)
                            : response.Content as T,
                    },
                    >= 400 => new TaskResult<T>
                    {
                        Status = response.StatusCode,
                        IsSuccess = false,
                        ErrorMessage = response.Content,
                    },
                    _ => throw new ArgumentOutOfRangeException(nameof(response.StatusCode), response.StatusCode, null)
                };
            }
        }
        finally
        {
            _responses.Remove(requestId);
        }
    }

    private TaskResult<T> DisconnectedResult<T>() where T : class => new()
    {
        IsSuccess = false,
        Status = Status.ServiceUnavailable,
        ErrorMessage = $"{GetType().Name} '{Name}' is not connected."
    };

    private class Response
    {
        public Response(string status, string content) :
            this(int.TryParse(status, out var statusAsInt) ? statusAsInt : -1, content) { }

        public Response(int statusCode, string content)
        {
            StatusCode = statusCode;
            Content = content;
        }

        public int StatusCode { get; }
        public string Content { get; }
    }
}