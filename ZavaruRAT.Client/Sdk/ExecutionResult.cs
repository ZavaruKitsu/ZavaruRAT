namespace ZavaruRAT.Client.Sdk;

public class ExecutionResult
{
    public static readonly ExecutionResult Empty = new();

    public object? Result { get; set; }
}

public sealed class RealTimeExecutionResult
{
}
