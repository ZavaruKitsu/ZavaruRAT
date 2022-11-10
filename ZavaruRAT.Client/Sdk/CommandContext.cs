#region

using ZavaruRAT.Shared;

#endregion

namespace ZavaruRAT.Client.Sdk;

public sealed class CommandContext
{
    public ZavaruClient Client { get; init; } = null!;
}
