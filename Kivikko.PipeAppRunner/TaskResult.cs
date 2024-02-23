namespace Kivikko.PipeAppRunner;

public class TaskResult : TaskResult<object> { }

public class TaskResult<T> where T : class
{
    public int Status { get; set; }
    public bool IsSuccess { get; set; }
    public string ErrorMessage { get; set; }
    public T Content { get; set; }
}