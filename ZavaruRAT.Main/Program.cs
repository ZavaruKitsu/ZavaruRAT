#region

using ZavaruRAT.Main.Authentication;
using ZavaruRAT.Main.Runtime;
using ZavaruRAT.Main.Services;

#endregion

var builder = WebApplication.CreateBuilder(args);

builder.Services
       .AddAuthentication(options => options.DefaultScheme = XAuthSchemeConstants.SchemeName)
       .AddScheme<XAuthSchemeOptions, XAuthHandler>(XAuthSchemeConstants.SchemeName, _ => { });
builder.Services.AddAuthorization();

builder.Services.AddSingleton<ClientsStorage>();

builder.Services.AddGrpc(options => options.MaxReceiveMessageSize = 128 * 1024 * 1024); // 128 mb
builder.Services.AddCors();

var app = builder.Build();

app.UseCors(x => x.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());

app.UseAuthentication();
app.UseAuthorization();

app.MapGrpcService<EventService>();
app.MapGrpcService<ActionService>();
app.MapGrpcService<AdminService>();

app.MapGet("/", () => "Communication with gRPC endpoints must be made through a gRPC client.");

app.Run();
