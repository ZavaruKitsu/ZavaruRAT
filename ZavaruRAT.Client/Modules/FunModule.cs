#region

using Windows.Win32;
using ZavaruRAT.Client.Sdk;

#endregion

namespace ZavaruRAT.Client.Modules;

public class FunModule : ModuleBase
{
    public Task<ExecutionResult> HideMouse()
    {
        var res = PInvoke.ShowCursor(false);
        return Task.FromResult(new ExecutionResult
        {
            Result = res
        });
    }

    public Task<ExecutionResult> ShowMouse()
    {
        var res = PInvoke.ShowCursor(true);
        return Task.FromResult(new ExecutionResult
        {
            Result = res
        });
    }

    public Task<ExecutionResult> BlockInput()
    {
        var res = PInvoke.BlockInput(true);
        return Task.FromResult(new ExecutionResult
        {
            Result = res.Value
        });
    }

    public Task<ExecutionResult> UnblockInput()
    {
        var res = PInvoke.BlockInput(false);
        return Task.FromResult(new ExecutionResult
        {
            Result = res.Value
        });
    }
}
