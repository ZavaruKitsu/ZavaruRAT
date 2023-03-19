#region

using System.Net.Sockets;

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
}
