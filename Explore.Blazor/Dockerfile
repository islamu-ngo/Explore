# Explore.Blazor/Dockerfile
# Multi-stage build for production deployment
# Secrets are loaded at runtime via AddSecretManagement() - no CLI needed
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
COPY ["Explore.Secrets/Explore.Secrets.csproj", "Explore.Secrets/"]
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

# Copy published application
COPY --from=publish /app/publish .

# Secret management is handled by the application via AddSecretManagement()
# Configure via environment variables:
#   SECRET_PROVIDER=infisical (or "none" for env vars only)
#   Infisical__ProjectId, Infisical__ClientId, Infisical__ClientSecret, etc.
# See docs/CONFIGURATION.md for full list of options

ENTRYPOINT ["dotnet", "Explore.Blazor.dll"]
