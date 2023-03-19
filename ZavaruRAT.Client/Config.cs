namespace ZavaruRAT.Client;

// ReSharper disable ConvertToConstant.Global
public static class Config
{
    public static readonly string IP = "127.0.0.1";

    public static readonly int Port = 13441;

    #region Features

    public static readonly bool AutoStart = true;

    #endregion

    public static readonly string MutexName = "Unknown Application";
    public static readonly string AutoStartName = "Unknown Application";
}
