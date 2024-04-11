FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER $APP_UID
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["SteamStorageAPI/SteamStorageAPI.csproj", "SteamStorageAPI/"]
RUN dotnet restore "SteamStorageAPI/SteamStorageAPI.csproj"
COPY . .
WORKDIR "/src/SteamStorageAPI"
RUN dotnet build "SteamStorageAPI.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "SteamStorageAPI.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "SteamStorageAPI.dll"]
