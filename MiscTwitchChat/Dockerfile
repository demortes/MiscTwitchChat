FROM mcr.microsoft.com/dotnet/core/aspnet:2.2-stretch-slim AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/core/sdk:2.2-stretch AS build
WORKDIR /src
COPY ["MiscTwitchChat/MiscTwitchChat.csproj", "MiscTwitchChat/"]
RUN dotnet restore "MiscTwitchChat/MiscTwitchChat.csproj"
COPY . .
WORKDIR "/src/MiscTwitchChat"
RUN dotnet build "MiscTwitchChat.csproj" -c Release -o /app

FROM build AS publish
RUN dotnet publish "MiscTwitchChat.csproj" -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "MiscTwitchChat.dll"]