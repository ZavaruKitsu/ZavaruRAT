#region

using ZavaruRAT.Shared;
using ZavaruRAT.Shared.Models.Client;

#endregion

namespace ZavaruRAT.Node.Runtime;

public sealed class ZavaruStoredClient
{
    public ZavaruStoredClient(ZavaruClient zavaruClient, DeviceInfo deviceInfo)
    {
        Id = Guid.NewGuid();
        Client = zavaruClient;
        DeviceInfo = deviceInfo;
    }

    public Guid Id { get; }
    public ZavaruClient Client { get; }
    public DeviceInfo DeviceInfo { get; }

    public override string ToString()
    {
        return Client + $" (stored with id {Id})";
    }
}
