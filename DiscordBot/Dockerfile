#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["DiscordBot/DiscordBot.csproj", "DiscordBot/"]
COPY ["MiscTwitchChat.Health/MiscTwitchChat.Health.csproj", "MiscTwitchChat.Health/"]
RUN dotnet restore "DiscordBot/DiscordBot.csproj"
COPY . .
WORKDIR "/src/DiscordBot"
RUN dotnet build "DiscordBot.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "DiscordBot.csproj" -c Release -o /app/publish

FROM base AS final
# Download and install the Tracer
RUN mkdir -p /opt/datadog \
    && mkdir -p /var/log/datadog \
    && TRACER_VERSION=$(curl -s https://api.github.com/repos/DataDog/dd-trace-dotnet/releases/latest | grep tag_name | cut -d '"' -f 4 | cut -c2-) \
    && curl -LO https://github.com/DataDog/dd-trace-dotnet/releases/download/v${TRACER_VERSION}/datadog-dotnet-apm_${TRACER_VERSION}_amd64.deb \
    && dpkg -i ./datadog-dotnet-apm_${TRACER_VERSION}_amd64.deb \
    && rm ./datadog-dotnet-apm_${TRACER_VERSION}_amd64.deb

# Enable the tracer
ENV CORECLR_ENABLE_PROFILING=1
ENV CORECLR_PROFILER={846F5F1C-F9AE-4B07-969E-05C26BC060D8}
ENV CORECLR_PROFILER_PATH=/opt/datadog/Datadog.Trace.ClrProfiler.Native.so
ENV DD_DOTNET_TRACER_HOME=/opt/datadog
ENV DD_IAST_ENABLED=true

# Health listener the bot starts alongside the Discord gateway connection. Override with Health__Port.
EXPOSE 8080

# Readiness covers the Discord gateway connection and reachability of the API the commands call.
ENV HEALTHCHECK_URL=http://127.0.0.1:8080/health/ready
HEALTHCHECK --interval=30s --timeout=10s --start-period=45s --retries=3 \
    CMD curl -fsS "$HEALTHCHECK_URL" > /dev/null || exit 1

WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "DiscordBot.dll"]