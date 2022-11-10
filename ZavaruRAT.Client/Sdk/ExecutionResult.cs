namespace ZavaruRAT.Client.Sdk;

public sealed class ExecutionResult
{
    public static readonly ExecutionResult Empty = new();

    public object? Result { get; set; }
}
