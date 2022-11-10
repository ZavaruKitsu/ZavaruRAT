namespace ZavaruRAT.Client;

public static class ZavaruExtensions
{
    public static string Commandify(this string command)
    {
        return command.Trim(' ', '\n', '\r').Replace("\n", " & ");
    }
}
