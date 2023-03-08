#region

using System.Diagnostics;
using System.Reflection;

#endregion

Console.WriteLine("ZavaruRAT Cleaner");
Console.WriteLine();
Console.WriteLine();

if (args.Length == 0)
{
    Console.WriteLine("[-] RAT assembly path is not specified");
    Environment.Exit(1);
}

var fullPath = Path.GetFullPath(args[0]);

Console.WriteLine("[*] Loading RAT assembly");
var rat = Assembly.LoadFile(fullPath);
Console.WriteLine("[+] Loaded");

Console.WriteLine("[*] Removing startup");

try
{
    rat.GetType("ZavaruRAT.Client.Installation")!.GetMethod("ForceRemoveAutoStart")!.Invoke(null, null);
}
catch (NullReferenceException)
{
    Console.WriteLine("[-] Wrong RAT version or it has been obfuscated");
    Environment.Exit(1);
}
catch (Exception e)
{
    Console.WriteLine("[-] Something went wrong");
    Console.WriteLine(e);
    Console.WriteLine(e.InnerException);
    Environment.Exit(1);
}

Console.WriteLine("[+] Autostart removed");

foreach (var process in Process.GetProcessesByName(Path.GetFileNameWithoutExtension(fullPath)))
{
    var pid = process.Id;

    try
    {
        process.Kill();
        Console.WriteLine($"[+] Process with PID {pid} killed");
    }
    catch
    {
        Console.WriteLine($"[-] Failed to kill process with PID {pid}");
    }
}
