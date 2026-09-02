---
description: >-
  Set up the .NET, Docker, and Aspire development environment and verify the
  local stack.
---

# Local Development

Run the complete development topology through .NET Aspire so service discovery, dependencies, health checks, and local infrastructure match the repository's supported workflow.

## Prerequisites

Install:

* .NET 10 SDK
* Docker with Compose support
* .NET Aspire CLI
* Git

## Start the stack

```bash
git clone <repository-url>
cd Event
cp .env.example .env
aspire run --apphost Explore.AppHost/Explore.AppHost.csproj
```

Treat `.env.example` as the configuration schema. Replace development placeholders through the approved local secret authority; never commit populated `.env` files or credentials.

## Development flow

1. Wait for Aspire to report required resources as healthy.
2. Open the BFF user interface from the Aspire dashboard.
3. Verify API `/alive` and `/health` endpoints.
4. Use Mailpit for local SMTP inspection where email resources are enabled.
5. Make changes in the owning architectural layer, then run the smallest relevant build and test slice.

## Generated artifacts

OpenAPI contracts, generated clients, EF Core migrations, and model snapshots are generated outputs. Change their source definitions and regenerate them; do not hand-edit generated files.

## Before opening a pull request

* Build the affected projects in Release configuration.
* Run the relevant TUnit project and exact test-class slice.
* Confirm configuration and secret changes are documented without secret values.
* Update API change records when a public contract changes.
* Perform the repository's intent-specific review checklist.
