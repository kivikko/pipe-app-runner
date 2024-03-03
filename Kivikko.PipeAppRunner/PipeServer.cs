namespace Kivikko.PipeAppRunner;

public class PipeServer : PipeBase
{
    public static PipeServer Create()
    {
        var pipeName = Guid.NewGuid().ToString();
        return new PipeServer(pipeName);
    }
    
    public PipeServer(string pipeName) :
        base(pipeInName: $"{pipeName}-0", pipeOutName: $"{pipeName}-1") =>
        Name = pipeName;

    protected override int NewRequestId() => --RequestId;

    protected override void RequestHandle(int requestId, string data)
    {
        if (requestId > 0)
            InputRequest(requestId, data);
        else
            OutputResponse(requestId, data);
    }
    
    public new PipeServer AddService(string endpoint, Func<string> func)
    {
        base.AddService(endpoint, func);
        return this;
    }
    
    public new PipeServer AddService(string endpoint, Func<Task<string>> func)
    {
        base.AddService(endpoint, func);
        return this;
    }

    public new PipeServer AddService(string endpoint, Func<string, string> func)
    {
        base.AddService(endpoint, func);
        return this;
    }

    public new PipeServer AddService(string endpoint, Func<string, Task<string>> func)
    {
        base.AddService(endpoint, func);
        return this;
    }
}