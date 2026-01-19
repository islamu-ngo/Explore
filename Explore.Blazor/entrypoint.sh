#!/bin/bash
set -e

echo "Starting Explore.Blazor container..."

# Check if Infisical credentials are provided
if [ -n "$INFISICAL_CLIENT_ID" ] && [ -n "$INFISICAL_CLIENT_SECRET" ]; then
    echo "Infisical credentials detected, fetching secrets..."

    # Set Infisical API URL if provided
    [ -n "$INFISICAL_SITE_URL" ] && export INFISICAL_API_URL="$INFISICAL_SITE_URL"

    # Authenticate
    export INFISICAL_TOKEN=$(infisical login \
        --method=universal-auth \
        --client-id="$INFISICAL_CLIENT_ID" \
        --client-secret="$INFISICAL_CLIENT_SECRET" \
        --silent --plain)

    # Load secrets
    echo "Loading secrets..."
    eval "$(infisical export --env="$INFISICAL_ENV" --projectId="$INFISICAL_PROJECT_ID" --path="/keycloak" --format=dotenv-export)"
    eval "$(infisical export --env="$INFISICAL_ENV" --projectId="$INFISICAL_PROJECT_ID" --path="/blazor" --format=dotenv-export)"

    echo "Secrets loaded."
else
    echo "No Infisical credentials found, using existing environment variables."
fi

echo "Starting .NET application..."
exec dotnet Explore.Blazor.dll "$@"