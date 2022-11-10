#region

using ZavaruRAT.Proto;

#endregion

namespace ZavaruRAT.Main.Runtime;

public sealed class RemoteClient
{
    public RemoteClient(ClientConnectedEvent ev)
    {
        Id = Guid.Parse(ev.Id);
        DeviceInfo = ev.DeviceInfo;
    }

    public Guid Id { get; }
    public ClientDeviceInfo DeviceInfo { get; }
}
