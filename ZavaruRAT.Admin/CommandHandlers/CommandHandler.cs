#region

using Grpc.Core;
using ZavaruRAT.Proto;

#endregion

namespace ZavaruRAT.Admin.CommandHandlers;

public abstract class CommandHandler
{
    public abstract bool Match(string command);
    public abstract Task Handle(string hashId, string clientId, AsyncServerStreamingCall<CommandExecutedEvent> results);
}
