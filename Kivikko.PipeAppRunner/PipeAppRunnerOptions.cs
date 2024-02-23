namespace Kivikko.PipeAppRunner;

public class PipeAppRunnerOptions
{
    public Action<PipeServer> AddEndpoints { get; set; }
    public Action<PipeServer> OnConnected { get; set; }
    public Action<PipeServer> OnDisconnected { get; set; }
    public Action<PipeServer> OnNewCreated { get; set; }
}