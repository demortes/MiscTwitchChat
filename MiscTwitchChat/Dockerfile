#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["MiscTwitchChat/MiscTwitchChat.csproj", "MiscTwitchChat/"]
COPY ["MiscTwitchChat.Classlib/MiscTwitchChat.Classlib.csproj", "MiscTwitchChat.Classlib/"]
RUN dotnet restore "MiscTwitchChat/MiscTwitchChat.csproj"
COPY . .
WORKDIR "/src/MiscTwitchChat"
RUN dotnet build "MiscTwitchChat.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "MiscTwitchChat.csproj" -c Release -o /app/publish

FROM base AS final
# Please select the corresponding download for your Linux containers https://github.com/DataDog/dd-trace-dotnet/releases/latest

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
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "MiscTwitchChat.dll"]