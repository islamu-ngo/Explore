# ABOUTME: Builds the Explore Blazor container image with deterministic restore inputs and pinned bases.
# ABOUTME: Supports production deployment while keeping runtime secrets injected only at startup.
# Secrets are loaded at runtime via AddSecretManagement() - no CLI needed
# Supports zero-downtime deployments with Coolify

FROM mcr.microsoft.com/dotnet/aspnet:10.0@sha256:ddcf70ad1ab963a4fcd41fbd722a6b660e404e87567cfbd46fd2809c21b02088 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:10.0@sha256:548d93f8a18a1acbe6cc127bc4f47281430d34a9e35c18afa80a8d6741c2adc3 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy restore inputs and project graph first for deterministic locked restore caching
COPY ["global.json", "Directory.Build.props", "Directory.Packages.props", "dotnet-tools.json", "./"]
COPY ["Explore.Blazor/Explore.Blazor.csproj", "Explore.Blazor/packages.lock.json", "Explore.Blazor/"]
COPY ["Explore.Blazor.Client/Explore.Blazor.Client.csproj", "Explore.Blazor.Client/packages.lock.json", "Explore.Blazor.Client/"]
COPY ["Explore.Application/Explore.Application.csproj", "Explore.Application/packages.lock.json", "Explore.Application/"]
COPY ["Explore.Domain/Explore.Domain.csproj", "Explore.Domain/packages.lock.json", "Explore.Domain/"]
COPY ["Explore.Infrastructure/Explore.Infrastructure.csproj", "Explore.Infrastructure/packages.lock.json", "Explore.Infrastructure/"]
COPY ["Explore.Persistence/Explore.Persistence.csproj", "Explore.Persistence/packages.lock.json", "Explore.Persistence/"]
COPY ["Explore.Secrets/Explore.Secrets.csproj", "Explore.Secrets/packages.lock.json", "Explore.Secrets/"]
COPY ["Explore.ServiceDefaults/Explore.ServiceDefaults.csproj", "Explore.ServiceDefaults/packages.lock.json", "Explore.ServiceDefaults/"]

RUN dotnet restore "./Explore.Blazor/Explore.Blazor.csproj" --locked-mode
RUN dotnet tool restore
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
