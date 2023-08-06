#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["TwitchActivityBot/TwitchActivityBot.csproj", "TwitchActivityBot/"]
RUN dotnet restore "TwitchActivityBot/TwitchActivityBot.csproj"
COPY . .
WORKDIR "/src/TwitchActivityBot"
RUN dotnet build "TwitchActivityBot.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "TwitchActivityBot.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "TwitchActivityBot.dll"]