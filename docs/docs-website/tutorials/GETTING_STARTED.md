# Tutorial: Getting Started with ISLAMU Event

This guide will walk you through setting up a local development environment for the ISLAMU Event platform.

## Prerequisites

*   **Docker Desktop** (latest version)
*   **.NET 10 SDK** (if running outside Docker)
*   **Git**

## Option 1: Quick Start (Docker Compose)

The easiest way to run the full stack (API, Blazor, Database, Keycloak, Cerbos) is via Docker Compose.

1.  **Clone the repository:**
    ```bash
    git clone https://github.com/islamu-ngo/Explore.git
    cd Explore
    ```

2.  **Start the services:**
    ```bash
    docker-compose up -d
    ```
    *This pulls images for PostgreSQL, Keycloak, Cerbos, and builds the API/Blazor apps.*

3.  **Wait for startup:**
    The `migration-service` container will automatically apply database migrations and seed default data. Wait for it to complete (check logs with `docker-compose logs -f migration-service`).

4.  **Access the application:**
    *   **Main UI:** [https://localhost:7001](https://localhost:7001)
    *   **Keycloak Admin:** [http://localhost:8080](http://localhost:8080) (user: `admin`, pass: `admin`)
    *   **Mailpit (Email):** [http://localhost:8025](http://localhost:8025)

## Option 2: Local Development (.NET Aspire)

For active development, use the .NET Aspire orchestration project.

1.  **Open the solution:**
    Open `Explore.sln` in Visual Studio 2022+ or VS Code.

2.  **Set Startup Project:**
    Set `Explore.AppHost` as the startup project.

3.  **Run (F5):**
    Aspire will spin up the necessary containers (Postgres, Keycloak) and run the .NET apps (API, Blazor) locally for debugging.

4.  **Aspire Dashboard:**
    A dashboard will open showing all running services, logs, and traces.

## Default Credentials

### System Admin
*   **Email:** `admin@example.com`
*   **Password:** `Password123!`

### Demo Tenant Admin
*   **Email:** `tenant-admin@example.com`
*   **Password:** `Password123!`

## Troubleshooting Common Issues

### Database Connection Failed
If the API crashes with connection errors, ensure the `postgres` container is healthy.
```bash
docker-compose restart postgres
```

### Keycloak Redirect Loop
Ensure you are accessing via `https://localhost:7001`. The OIDC configuration requires HTTPS.

### "Tenant Not Found"
Ensure you are using the correct header or domain. In local dev, the default tenant is automatically resolved for `localhost`.
