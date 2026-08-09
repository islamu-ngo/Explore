# ABOUTME: Builds the Event.Standalone combined API and Blazor host as a deterministic runtime image.
# ABOUTME: Publishes locked dependencies, exposes HTTP on 8080, and provides app-owned SQLite storage.

FROM mcr.microsoft.com/dotnet/sdk:10.0.301@sha256:548d93f8a18a1acbe6cc127bc4f47281430d34a9e35c18afa80a8d6741c2adc3 AS build
WORKDIR /src

COPY ["global.json", "Directory.Build.props", "Directory.Packages.props", "./"]
COPY ["src/Event.Standalone/Event.Standalone.csproj", "src/Event.Standalone/packages.lock.json", "src/Event.Standalone/"]
COPY ["src/Explore.API/Explore.API.csproj", "src/Explore.API/packages.lock.json", "src/Explore.API/"]
COPY ["src/Explore.Application/Explore.Application.csproj", "src/Explore.Application/packages.lock.json", "src/Explore.Application/"]
COPY ["src/Explore.Atproto.Transport/Explore.Atproto.Transport.csproj", "src/Explore.Atproto.Transport/packages.lock.json", "src/Explore.Atproto.Transport/"]
COPY ["src/Explore.Blazor/Explore.Blazor.csproj", "src/Explore.Blazor/packages.lock.json", "src/Explore.Blazor/"]
COPY ["src/Explore.Blazor.Client/Explore.Blazor.Client.csproj", "src/Explore.Blazor.Client/packages.lock.json", "src/Explore.Blazor.Client/"]
COPY ["src/Explore.Domain/Explore.Domain.csproj", "src/Explore.Domain/packages.lock.json", "src/Explore.Domain/"]
COPY ["src/Explore.Infrastructure/Explore.Infrastructure.csproj", "src/Explore.Infrastructure/packages.lock.json", "src/Explore.Infrastructure/"]
COPY ["src/Explore.Persistence/Explore.Persistence.csproj", "src/Explore.Persistence/packages.lock.json", "src/Explore.Persistence/"]
COPY ["src/Explore.Secrets/Explore.Secrets.csproj", "src/Explore.Secrets/packages.lock.json", "src/Explore.Secrets/"]
COPY ["src/Explore.ServiceDefaults/Explore.ServiceDefaults.csproj", "src/Explore.ServiceDefaults/packages.lock.json", "src/Explore.ServiceDefaults/"]
COPY ["src/Event.Web.BffHosting/Event.Web.BffHosting.csproj", "src/Event.Web.BffHosting/packages.lock.json", "src/Event.Web.BffHosting/"]
RUN dotnet restore "src/Event.Standalone/Event.Standalone.csproj" --locked-mode --force-evaluate /p:StandaloneContainerPublish=true

COPY . .
RUN dotnet publish "src/Event.Standalone/Event.Standalone.csproj" -c Release -o /app/publish /p:StandaloneContainerPublish=true /p:DebugType=none /p:ErrorOnDuplicatePublishOutputFiles=false --no-restore && cp src/Event.Standalone/appsettings.json /app/publish/appsettings.json && mkdir /app/data

FROM mcr.microsoft.com/dotnet/runtime-deps:10.0-noble-chiseled-extra@sha256:a0428e029a1026daa75e68d00ee464c78d8979fb10f289257b143e3ea997196b AS final
WORKDIR /app
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
COPY --from=build /app/publish .
COPY --chown=$APP_UID:$APP_UID --from=build /app/data /app/data
USER $APP_UID
ENTRYPOINT ["./Event.Standalone"]
