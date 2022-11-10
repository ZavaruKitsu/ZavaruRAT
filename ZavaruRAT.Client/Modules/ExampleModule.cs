#region

using ZavaruRAT.Client.Sdk;

#endregion

namespace ZavaruRAT.Client.Modules;

public sealed class ExampleModule : ModuleBase
{
    public Task<ExecutionResult> Ping()
    {
        return Task.FromResult(new ExecutionResult
        {
            Result = "Pong!"
        });
    }
}
