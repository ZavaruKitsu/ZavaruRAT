#region

using MessagePack;

#endregion

namespace ZavaruRAT.Shared.Models.Client;

[MessagePackObject]
public sealed class DeviceInfo
{
    public static readonly DeviceInfo Empty = new();

    [Key(0)] public string OS { get; set; } = null!;

    [Key(1)] public string Motherboard { get; set; } = null!;
    [Key(2)] public string CPU { get; set; } = null!;
    [Key(3)] public string GPU { get; set; } = null!;

    [Key(4)] public float RAM { get; set; } // in gb
    [Key(5)] public string[] Drives { get; set; } = null!;

    [Key(100)] public string InstallationPath { get; set; } = null!;
    [Key(101)] public bool AutoStart { get; set; }
}
