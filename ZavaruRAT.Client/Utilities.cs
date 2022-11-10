#region

using System.Management;
using ZavaruRAT.Shared.Models.Client;

#endregion

namespace ZavaruRAT.Client;

public static class Utilities
{
    public static IEnumerable<FileSystemInfo> EnumerateDirectories(string root, string pattern,
                                                                   List<string>? ignore = null)
    {
        return EnumerateDirectories(new DirectoryInfo(root), pattern, ignore);
    }

    public static IEnumerable<FileSystemInfo> EnumerateDirectories(DirectoryInfo root, string pattern,
                                                                   List<string>? ignore = null)
    {
        ignore ??= new List<string>(0);

        if (root is not { Exists: true })
        {
            yield break;
        }

        if (ignore.Any(x => root.Name.IndexOf(x, StringComparison.InvariantCultureIgnoreCase) != -1))
        {
            yield break;
        }

        IEnumerable<FileSystemInfo> matches = new List<FileSystemInfo>();
        try
        {
            matches = matches.Concat(root.EnumerateDirectories(pattern, SearchOption.TopDirectoryOnly))
                             .Concat(root.EnumerateFiles(pattern, SearchOption.TopDirectoryOnly));
        }
        catch (SystemException)
        {
            yield break;
        }

        foreach (var file in matches)
        {
            yield return file;
        }

        foreach (var dir in root.EnumerateDirectories("*", SearchOption.TopDirectoryOnly))
        {
            var fileSystemInfos = EnumerateDirectories(dir, pattern, ignore);
            foreach (var match in fileSystemInfos)
            {
                yield return match;
            }
        }
    }

    public static DeviceInfo GatherDeviceInfo()
    {
        var os = new DeviceInfo();

        os.OS = new ManagementObjectSearcher("SELECT Caption FROM Win32_OperatingSystem")
                .Get()
                .Cast<ManagementObject>()
                .Select(x => x.GetPropertyValue("Caption"))
                .FirstOrDefault()
                ?.ToString() ?? "<unknown>";

        os.Motherboard = new ManagementObjectSearcher("SELECT Manufacturer, Product FROM Win32_BaseBoard")
                         .Get()
                         .Cast<ManagementObject>()
                         .Select(x => x.GetPropertyValue("Manufacturer") + " " + x.GetPropertyValue("Product"))
                         .FirstOrDefault()
                         ?.ToString() ?? "<unknown>";

        os.CPU = new ManagementObjectSearcher("SELECT Name FROM Win32_Processor")
                 .Get()
                 .Cast<ManagementObject>()
                 .Select(x => x.GetPropertyValue("Name"))
                 .FirstOrDefault()
                 ?.ToString() ?? "<unknown>";

        os.GPU = new ManagementObjectSearcher("SELECT Name, AdapterRAM FROM Win32_VideoController")
                 .Get()
                 .Cast<ManagementObject>()
                 .Select(x => x.GetPropertyValue("Name") + ", " + x.GetPropertyValue("AdapterRAM") + " bytes")
                 .FirstOrDefault()
                 ?.ToString() ?? "<unknown>";

        os.RAM = GC.GetGCMemoryInfo().TotalAvailableMemoryBytes / 1048576.0f / 1024.0f;

        os.Drives = DriveInfo.GetDrives().Where(x => x.IsReady)
                             .Select(x =>
                                         $"{x.VolumeLabel} ({x.Name[..2]}) : {x.AvailableFreeSpace / 1048576.0f / 1024.0f:F1} / {x.TotalSize / 1048576.0f / 1024.0f:F1} GB")
                             .ToArray();

        return os;
    }

    public static void EnsureOneInstance()
    {
        var mutex = new Mutex(true, Config.MutexName);

        if (!mutex.WaitOne(TimeSpan.Zero, true))
        {
            Environment.Exit(1);
        }
    }
}
