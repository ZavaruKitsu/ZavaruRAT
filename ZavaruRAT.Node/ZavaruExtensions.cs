#region

using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ZavaruRAT.Node.Runtime;
using ZavaruRAT.Proto;

#endregion

namespace ZavaruRAT.Node;

public static class ZavaruExtensions
{
    public static IServiceCollection AddMainServerClient(this IServiceCollection services)
    {
        return services.AddSingleton(provider =>
        {
            var configuration = provider.GetRequiredService<IConfiguration>();

            var channel = CreateChannel(configuration);

            var actionClient = new ActionHub.ActionHubClient(channel);
            var eventClient = new EventHub.EventHubClient(channel);

            return new MainServerClient(actionClient, eventClient, configuration,
                                        provider.GetRequiredService<ILogger<MainServerClient>>());
        });
    }

    public static GrpcChannel CreateChannel(IConfiguration configuration)
    {
        var credentials = CallCredentials.FromInterceptor((context, metadata) =>
        {
            metadata.Add("Authorization", $"{configuration["Grpc:Token"]}:{configuration["NodeId"]}");
            return Task.CompletedTask;
        });

        var channel = GrpcChannel.ForAddress(configuration["Grpc:Host"]!, new GrpcChannelOptions
        {
            Credentials = ChannelCredentials.Create(new SslCredentials(), credentials)
        });

        return channel;
    }
}
