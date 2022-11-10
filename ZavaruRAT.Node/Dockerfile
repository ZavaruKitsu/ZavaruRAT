FROM mcr.microsoft.com/dotnet/runtime:6.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["ZavaruRAT.Node/ZavaruRAT.Node.csproj", "ZavaruRAT.Node/"]
RUN dotnet restore "ZavaruRAT.Node/ZavaruRAT.Node.csproj"
COPY . .
WORKDIR "/src/ZavaruRAT.Node"
RUN dotnet build "ZavaruRAT.Node.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ZavaruRAT.Node.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ZavaruRAT.Node.dll"]
