#region

using MessagePack;

#endregion

namespace ZavaruRAT.Shared.Models.Client;

[MessagePackObject]
public sealed class Command
{
    [Key(0)] public string Id { get; set; } = null!;

    [Key(1)] public string CommandName { get; set; } = null!;

    [Key(2)] public byte[]? Args { get; set; }
}
