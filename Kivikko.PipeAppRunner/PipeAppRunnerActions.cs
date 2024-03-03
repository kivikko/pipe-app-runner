namespace Kivikko.PipeAppRunner;

public class PipeAppRunnerActions
{
    public Action<PipeServer> AddServices { get; set; }
    public Action<PipeServer> OnConnected { get; set; }
    public Action<PipeServer> OnDisconnected { get; set; }
    public Action<PipeServer> OnNewCreated { get; set; }
}