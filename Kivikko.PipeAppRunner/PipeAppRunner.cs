namespace Kivikko.PipeAppRunner;

public static class PipeAppRunner
{
    private const string DefaultPipeName  = "BE870F6E-ECFF-4E6F-80A1-31D90D4105D6";
    private static Mutex _mutex; // https://stackoverflow.com/questions/52925517/mutex-initialization-inside-a-c-sharp-method-always-returns-positive-creatednew#answer-63210934
    
    public static void StartFromOwner(
        string path,
        PipeAppRunnerOptions options = null,
        PipeAppRunnerActions actions = null,
        params string[] args) =>
        StartFromOwner(PipeServer.Create(), path, options, actions, args);
    
    public static void StartFromOwner(
        PipeServer pipeServer,
        string path,
        PipeAppRunnerOptions options = null,
        PipeAppRunnerActions actions = null,
        params string[] args)
    {
        if (string.IsNullOrWhiteSpace(path)) throw new ArgumentNullException(nameof(path));
        if (!File.Exists(path)) throw new FileNotFoundException($"{path} not found");
        
        pipeServer.Connected    += PipeServerOnConnected;
        pipeServer.Disconnected += PipeServerOnDisconnected;
        
        actions?.AddServices?.Invoke(pipeServer);
        
        if (!string.IsNullOrWhiteSpace(options?.MutexName))
        {
            using var mutex = new Mutex(initiallyOwned: true, options.MutexName, out var createdNew);
            if (createdNew) actions?.OnNewCreated?.Invoke(pipeServer);
        }
        
        pipeServer.Start();

        var mainWindowHandle = options?.ClientWindowOnTopOfOwner ?? false ? Process.GetCurrentProcess().MainWindowHandle.ToString() : null;
        var pipeAppClientOptions = new PipeAppClientOptions
        {
            MutexName = options?.MutexName,
            OwnerWindowHandle = mainWindowHandle,
            PipeName = pipeServer.Name,
            Args = args
        };
        var clientProcess = new Process
        {
            StartInfo =
            {
                FileName = path,
                Arguments = JsonUtils.ToJson(pipeAppClientOptions)
            },
        };
        
        clientProcess.Start();
        return;

        void PipeServerOnConnected(object sender, EventArgs e)
        {
            actions?.OnConnected?.Invoke(pipeServer);
        }

        void PipeServerOnDisconnected(object sender, EventArgs e)
        {
            actions?.OnDisconnected?.Invoke(pipeServer);
            pipeServer.Connected    -= PipeServerOnConnected;
            pipeServer.Disconnected -= PipeServerOnDisconnected;
        }
    }
    
    public static bool StartClientApp(
        Action<PipeAppClientOptions> startApp,
        string[] args,
        Action activateExistingClient,
        Action<string> whenExistingClientActivated = null)
    {
        var options = args.Any() ? JsonUtils.FromJson<PipeAppClientOptions>(args.FirstOrDefault()) : null;
        var mutexName = options?.MutexName;
        
        if (!string.IsNullOrWhiteSpace(mutexName))
        {
            _mutex = new Mutex(initiallyOwned: true, mutexName, out var createdNew);
            
            if (!createdNew)
                return ActivateExistingClient(options);

            options.PipeClient = PipeClient.CreateAndStart(options.PipeName);
            Task.Run(() => RestartListenerThread(mutexName, activateExistingClient, whenExistingClientActivated));
            startApp?.Invoke(options);
        }
        else
        {
            if (options is not null)
                options.PipeClient = PipeClient.CreateAndStart(options.PipeName);
            
            Task.Run(() => RestartListenerThread(DefaultPipeName, activateExistingClient, whenExistingClientActivated));
            startApp?.Invoke(options);
        }

        return true;
    }

    private static bool ActivateExistingClient(PipeAppClientOptions options)
    {
        using var client = new NamedPipeClientStream(options.MutexName);
                
        try
        {
            client.Connect(timeout: 3000);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return false;
        }
                
        if (!client.IsConnected)
            return false;
                
        var appArgs = options.Args;
        using var streamWriter = new StreamWriter(client);
        streamWriter.AutoFlush = true;
        streamWriter.WriteLine(appArgs);
        
        return false;
    }

    // ReSharper disable once FunctionNeverReturns
    private static async void RestartListenerThread(
        string pipeName,
        Action activateExistingClient,
        Action<string> existingClientActivatedCallback)
    {
        try
        {
            while(true)
            {
                using var server = new NamedPipeServerStream(pipeName);
                await server.WaitForConnectionAsync();
                using var reader = new StreamReader(server);
                var arg = await reader.ReadLineAsync();
                activateExistingClient();
                existingClientActivatedCallback?.Invoke(arg);
            }
        }
        catch (Exception exception)
        {
            Console.WriteLine(exception);
        }
    }
}