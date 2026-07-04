# ABOUTME: Builds the separate Event control-plane Blazor BFF container image.
# ABOUTME: Supports runtime configuration through environment variables and Infisical bootstrap settings.
# Secrets are loaded by the application at runtime via IConfiguration and Explore.Secrets.
# Do not bake Keycloak, Infisical, API, or provider secret values into this image.

FROM mcr.microsoft.com/dotnet/aspnet:10.0@sha256:7644f992230d35cf230017189d4038c0ae0f7388b13f4f7ae1900a155bafb597 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:10.0@sha256:ea8bde36c11b6e7eec2656d0e59101d4462f6bd630730f2c8201ed0572b295d5 AS build
ARG BUILD_CONFIGURATION=Release
ARG USE_COMMERCIAL_LUCKYPENNY_LIBS=false
WORKDIR /src

COPY ["global.json", "Directory.Build.props", "Directory.Packages.props", "dotnet-tools.json", "./"]
COPY ["Event.ControlPlane.Blazor/Event.ControlPlane.Blazor.csproj", "Event.ControlPlane.Blazor/packages.lock.json", "Event.ControlPlane.Blazor/"]
COPY ["Event.ControlPlane.Client/Event.ControlPlane.Client.csproj", "Event.ControlPlane.Client/packages.lock.json", "Event.ControlPlane.Client/"]
COPY ["Event.Web.BffHosting/Event.Web.BffHosting.csproj", "Event.Web.BffHosting/packages.lock.json", "Event.Web.BffHosting/"]
COPY ["Explore.Secrets/Explore.Secrets.csproj", "Explore.Secrets/packages.lock.json", "Explore.Secrets/"]
COPY ["Explore.Application/Explore.Application.csproj", "Explore.Application/packages.lock.json", "Explore.Application/"]
COPY ["Explore.Domain/Explore.Domain.csproj", "Explore.Domain/packages.lock.json", "Explore.Domain/"]
COPY ["Explore.ServiceDefaults/Explore.ServiceDefaults.csproj", "Explore.ServiceDefaults/packages.lock.json", "Explore.ServiceDefaults/"]

RUN if [ "$USE_COMMERCIAL_LUCKYPENNY_LIBS" = "true" ]; then \
      dotnet restore "./Event.ControlPlane.Blazor/Event.ControlPlane.Blazor.csproj" \
        -p:UseCommercialLuckyPennyLibraries=true; \
    else \
      dotnet restore "./Event.ControlPlane.Blazor/Event.ControlPlane.Blazor.csproj" \
        --locked-mode \
        -p:UseCommercialLuckyPennyLibraries=false; \
    fi

RUN dotnet tool restore
COPY . .
WORKDIR "/src/Event.ControlPlane.Blazor"
RUN dotnet build "./Event.ControlPlane.Blazor.csproj" -c $BUILD_CONFIGURATION -o /app/build \
      --no-restore \
      -p:UseCommercialLuckyPennyLibraries=$USE_COMMERCIAL_LUCKYPENNY_LIBS

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
ARG USE_COMMERCIAL_LUCKYPENNY_LIBS=false
RUN dotnet publish "./Event.ControlPlane.Blazor.csproj" -c $BUILD_CONFIGURATION -o /app/publish \
      /p:UseAppHost=false \
      --no-restore \
      -p:UseCommercialLuckyPennyLibraries=$USE_COMMERCIAL_LUCKYPENNY_LIBS

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Runtime configuration examples:
#   Bff__Authentication__Authority=https://auth.example.org/realms/ISLAMU
#   Bff__Authentication__ClientId=islamu-event-control-plane
#   Bff__Authentication__ClientSecret=<runtime secret>
#   ExploreApi__BaseUrl=https://api.internal:7039
#   SecretProvider__Provider=None
#   SecretProvider__Provider=Infisical
#   Infisical__ProjectId=<runtime secret>
#   Infisical__ClientId=<runtime secret>
#   Infisical__ClientSecret=<runtime secret>
#   Infisical__Environment=prod

ENTRYPOINT ["dotnet", "Event.ControlPlane.Blazor.dll"]
