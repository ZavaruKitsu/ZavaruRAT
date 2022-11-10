FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["ZavaruRAT.Main/ZavaruRAT.Main.csproj", "ZavaruRAT.Main/"]
RUN dotnet restore "ZavaruRAT.Main/ZavaruRAT.Main.csproj"
COPY . .
WORKDIR "/src/ZavaruRAT.Main"
RUN dotnet build "ZavaruRAT.Main.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ZavaruRAT.Main.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ZavaruRAT.Main.dll"]
