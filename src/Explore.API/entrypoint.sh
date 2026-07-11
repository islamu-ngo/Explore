#!/bin/bash
set -e

echo "Starting Explore.API container..."

# Check if Infisical credentials are provided
if [ -n "$INFISICAL_CLIENT_ID" ] && [ -n "$INFISICAL_CLIENT_SECRET" ]; then
    echo "Infisical credentials detected, fetching secrets..."

    # Set Infisical API URL if provided (for self-hosted instances)
    [ -n "$INFISICAL_SITE_URL" ] && export INFISICAL_API_URL="$INFISICAL_SITE_URL"

    # Authenticate with Infisical using universal auth (machine identity)
    export INFISICAL_TOKEN=$(infisical login \
        --method=universal-auth \
        --client-id="$INFISICAL_CLIENT_ID" \
        --client-secret="$INFISICAL_CLIENT_SECRET" \
        --silent --plain)

    # Export secrets from required paths
    echo "Loading /postgresql secrets..."
    eval "$(infisical export --env="$INFISICAL_ENV" --projectId="$INFISICAL_PROJECT_ID" --path="/postgresql" --format=dotenv-export)"

    echo "Loading /keycloak secrets..."
    eval "$(infisical export --env="$INFISICAL_ENV" --projectId="$INFISICAL_PROJECT_ID" --path="/keycloak" --format=dotenv-export)"

    echo "Loading /api secrets..."
    eval "$(infisical export --env="$INFISICAL_ENV" --projectId="$INFISICAL_PROJECT_ID" --path="/api" --format=dotenv-export)"

    echo "All secrets loaded successfully."
else
    echo "No Infisical credentials found, using existing environment variables."
fi

echo "Starting .NET application..."
exec dotnet Explore.API.dll "$@"
