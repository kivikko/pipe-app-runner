namespace Kivikko.PipeAppRunner;

public static class PipeAppRunner
{
    private const string DefaultPipeName  = "BE870F6E-ECFF-4E6F-80A1-31D90D4105D6";
    
    public static void StartFromOwner(string path, string mutexName = null, PipeAppRunnerOptions options = null, params string[] args) =>
        StartFromOwner(PipeServer.Create(), path, mutexName, options, args);
    
    public static void StartFromOwner(PipeServer pipeServer, string path, string mutexName = null, PipeAppRunnerOptions options = null, params string[] args)
    {
        if (string.IsNullOrWhiteSpace(path)) throw new ArgumentNullException(nameof(path));
        if (!File.Exists(path)) throw new FileNotFoundException($"{path} not found");
        
        pipeServer.Connected    += PipeServerOnConnected;
        pipeServer.Disconnected += PipeServerOnDisconnected;
        
        options?.AddEndpoints?.Invoke(pipeServer);
        
        if (!string.IsNullOrWhiteSpace(mutexName))
        {
            using var mutex = new Mutex(initiallyOwned: true, mutexName, out var createdNew);
            if (createdNew) options?.OnNewCreated?.Invoke(pipeServer);
        }
        
        pipeServer.Start();

        var currentProcess = Process.GetCurrentProcess();
        var clientArguments = string.Join(" ", new[] { mutexName ?? "-", pipeServer.Name, currentProcess.MainWindowHandle.ToString() }.Concat(args));
        var clientProcess = new Process
        {
            StartInfo =
            {
                FileName = path,
                Arguments = clientArguments,
            },
        };
        
        clientProcess.Start();
        return;

        void PipeServerOnConnected(object sender, EventArgs e)
        {
            options?.OnConnected?.Invoke(pipeServer);
        }

        void PipeServerOnDisconnected(object sender, EventArgs e)
        {
            options?.OnDisconnected?.Invoke(pipeServer);
            pipeServer.Connected    -= PipeServerOnConnected;
            pipeServer.Disconnected -= PipeServerOnDisconnected;
        }
    }
    
    public static void StartClientApp(
        Action<string[]> startApp,
        string[] args,
        Action activateWindow,
        Action<string> whenWindowActivated = null)
    {
        var mutexName = args.Any() ? args[0] : null;
        
        if (!string.IsNullOrWhiteSpace(mutexName) && mutexName is not "-")
        {
            using var mutex = new Mutex(initiallyOwned: true, mutexName, out var createdNew);
            
            if (!createdNew)
            {
                using var client = new NamedPipeClientStream(mutexName);
                client.Connect(timeout: 3000);

                if (!client.IsConnected || args.Length <= 3)
                    return;
                
                var appArgs = args[3];
                using var streamWriter = new StreamWriter(client);
                streamWriter.AutoFlush = true;
                streamWriter.WriteLine(appArgs);
            }
            else
            {
                Task.Run(() => RestartListenerThread(mutexName, activateWindow, whenWindowActivated));
                startApp(args);
            }
        }
        else
        {
            Task.Run(() => RestartListenerThread(DefaultPipeName, activateWindow, whenWindowActivated));
            startApp(args);
        }
    }
    
    // ReSharper disable once FunctionNeverReturns
    private static async void RestartListenerThread(
        string pipeName,
        Action activateWindow,
        Action<string> windowActivatedCallback)
    {
        while(true)
        {
            using var server = new NamedPipeServerStream(pipeName);
            await server.WaitForConnectionAsync();
            using var reader = new StreamReader(server);
            var arg = await reader.ReadLineAsync();
            activateWindow();
            windowActivatedCallback?.Invoke(arg);
        }
    }

    public static PipeAppClientOptions GetPipeClientAppOptions(string[] args)
    {
        var options = new PipeAppClientOptions
        {
            PipeName          = args?.Length > 1 ? args[1] : null,
            OwnerWindowHandle = args?.Length > 2 ? args[2] : null,
            Args              = args?.Length > 3 ? args.Skip(3).ToArray() : null,
        };
        options.PipeClient = !string.IsNullOrWhiteSpace(options.PipeName) ? PipeClient.CreateAndStart(options.PipeName) : null;
        return options;
    }
}