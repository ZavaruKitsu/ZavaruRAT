#region

using System.Net.Sockets;
using ZavaruRAT.Proto;
using ZavaruRAT.Shared.Models.Client;

#endregion

namespace ZavaruRAT.Shared;

public static class ZavaruExtensions
{
    /// <summary>
    ///     Determines if the current <see cref="TcpClient" /> connected to remote
    /// </summary>
    /// <param name="tcpClient">The TCP client</param>
    /// <returns>The connection state</returns>
    public static bool IsConnected(this TcpClient tcpClient)
    {
        try
        {
            if (!tcpClient.Client.Poll(0, SelectMode.SelectRead))
            {
                return true;
            }

            var buff = new byte[1];
            return tcpClient.Client.Receive(buff, SocketFlags.Peek) != 0;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    ///     Maps <see cref="DeviceInfo" /> to <see cref="ClientDeviceInfo" />
    /// </summary>
    /// <param name="deviceInfo">The original device info</param>
    /// <returns>Mapped proto object</returns>
    public static ClientDeviceInfo ToProto(this DeviceInfo deviceInfo)
    {
        return new ClientDeviceInfo
        {
            OS = deviceInfo.OS,
            Motherboard = deviceInfo.Motherboard,
            CPU = deviceInfo.CPU,
            GPU = deviceInfo.GPU,
            RAM = deviceInfo.RAM,
            Drives =
            {
                deviceInfo.Drives
            }
        };
    }
}
