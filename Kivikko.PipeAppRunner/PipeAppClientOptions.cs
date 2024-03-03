namespace Kivikko.PipeAppRunner;

public class PipeAppClientOptions
{
    public PipeClient PipeClient { get; set; }
    public string MutexName { get; set; }
    public string OwnerWindowHandle { get; set; }
    public string PipeName { get; set; }
    public string[] Args { get; set; }
}