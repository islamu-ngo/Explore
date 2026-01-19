# Explore.Blazor/Dockerfile
# Multi-stage build with Infisical CLI for secret management
# Supports zero-downtime deployments with Coolify

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy all project files for restore
COPY ["Explore.Blazor/Explore.Blazor.csproj", "Explore.Blazor/"]
COPY ["Explore.Blazor.Client/Explore.Blazor.Client.csproj", "Explore.Blazor.Client/"]
COPY ["Explore.ServiceDefaults/Explore.ServiceDefaults.csproj", "Explore.ServiceDefaults/"]

RUN dotnet restore "./Explore.Blazor/Explore.Blazor.csproj"
COPY . .
WORKDIR "/src/Explore.Blazor"
RUN dotnet build "./Explore.Blazor.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Explore.Blazor.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app

# 1. Install dos2unix along with Infisical
USER root
RUN apt-get update && apt-get install -y bash curl ca-certificates dos2unix && \
    curl -1sLf 'https://dl.cloudsmith.io/public/infisical/infisical-cli/setup.deb.sh' | bash && \
    apt-get update && apt-get install -y infisical && \
    apt-get clean && rm -rf /var/lib/apt/lists/*

# Copy published application
COPY --from=publish /app/publish .

# Copy entrypoint script
COPY Explore.Blazor/entrypoint.sh /app/entrypoint.sh

# 2. FIX LINE ENDINGS
RUN dos2unix /app/entrypoint.sh && chmod +x /app/entrypoint.sh

# Switch back to non-root user
USER $APP_UID

ENTRYPOINT ["/app/entrypoint.sh"]