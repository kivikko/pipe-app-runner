namespace Kivikko.PipeAppRunner;

public class PipeClient : PipeBase
{
    public static PipeClient CreateAndStart(string pipeName)
    {
        if (string.IsNullOrWhiteSpace(pipeName))
            return null;
        
        var pipeClient = new PipeClient(pipeName);
        Task.Run(() => pipeClient.Start());
        const int attempts = 100;
        var i = 0;
        while (!pipeClient.IsConnected && i++ < attempts) Thread.Sleep(10);
        return pipeClient;
    }
    
    public PipeClient(string pipeName) :
        base(pipeInName: $"{pipeName}-1", pipeOutName: $"{pipeName}-0") =>
        Name = pipeName;

    protected override int NewRequestId() => ++RequestId;
    
    protected override void RequestHandle(int requestId, string data)
    {
        if (requestId < 0)
            InputRequest(requestId, data);
        else
            OutputResponse(requestId, data);
    }
}