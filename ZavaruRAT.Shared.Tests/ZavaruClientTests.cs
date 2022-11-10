namespace ZavaruRAT.Shared.Tests;

public sealed class ZavaruClientTests : IDisposable
{
    private readonly int _port;
    private readonly TcpListener _tcpListener;

    public ZavaruClientTests()
    {
        _tcpListener = TcpListener.Create(0);
        _tcpListener.Start();

        Task.Factory.StartNew(async () =>
        {
            while (true)
            {
                await _tcpListener.AcceptTcpClientAsync();
            }
        });

        _port = ((IPEndPoint)_tcpListener.LocalEndpoint).Port;
    }

    public void Dispose()
    {
        _tcpListener.Stop();
    }

    private async Task<ZavaruClient> CreateAsync()
    {
        return await ZavaruClient.CreateAsync("localhost", _port);
    }

    [Fact]
    public async Task TestBasicConnection()
    {
        var client = await CreateAsync();

        Assert.True(client.Connected);
    }

    [Fact]
    public async Task TestManualConnection()
    {
        var client = new TcpClient();
        await client.ConnectAsync("localhost", _port);

        Assert.True(client.Connected);
    }

    [Fact]
    public async Task TestSending()
    {
        var client = await CreateAsync();
        await client.SendAsync(new Dictionary<string, string>
        {
            { "ZavaruKitsu", "is a top tier coder" },
            { "AlexeyZavar", "is my ex. nickname" }
        });

        Assert.True(client.Connected);
    }
}
