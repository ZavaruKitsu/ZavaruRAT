#region

using Microsoft.Extensions.Logging;
using ZavaruRAT.Shared;
using ZavaruRAT.Shared.Models.Client;

#endregion

namespace ZavaruRAT.Node.Runtime;

public sealed class ZavaruClientsStorage
{
    private readonly ILogger<ZavaruClientsStorage> _logger;

    private readonly List<ZavaruStoredClient> _clients;

    public ZavaruClientsStorage(ILogger<ZavaruClientsStorage> logger)
    {
        _logger = logger;

        _clients = new List<ZavaruStoredClient>();
    }

    public IEnumerable<ZavaruStoredClient> Clients => _clients.AsReadOnly();

    public ZavaruStoredClient AddClient(ZavaruClient zavaruClient, DeviceInfo deviceInfo)
    {
        ArgumentNullException.ThrowIfNull(zavaruClient);

        var storedClient = new ZavaruStoredClient(zavaruClient, deviceInfo);

        _clients.Add(storedClient);
        _logger.LogInformation("Client added {StoredClient}", storedClient);

        return storedClient;
    }

    public void RemoveClient(ZavaruStoredClient storedClient)
    {
        ArgumentNullException.ThrowIfNull(storedClient);

        _clients.Remove(storedClient);
        _logger.LogInformation("Client removed {StoredClient}", storedClient);
    }

    public ZavaruStoredClient? Find(Guid id)
    {
        return _clients.FirstOrDefault(x => x.Id == id);
    }
}
