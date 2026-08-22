<!-- ABOUTME: Clean-room provenance record for Cerbos onboarding policy sync credential modes. -->
<!-- ABOUTME: Records public interface facts and the repository-native implementation boundary. -->

# Cerbos Onboarding Policy Sync — Clean-Room Record

Date (Europe/Brussels): 2026-08-20  
Intent / workstream: `external-infrastructure-bootstrap` / Cerbos onboarding policy sync  
Feature: deployment-secret and one-time Admin API credential modes

## Source register

- Cerbos Hub, “Admin API,” https://docs.cerbos.dev/cerbos/latest/api/admin_api.html, accessed 2026-08-20 through public documentation. Observed facts only: policy/schema management uses the Admin HTTP API and Basic authentication; it is distinct from runtime decision checks.
- Cerbos Hub, “Server configuration,” https://docs.cerbos.dev/cerbos/latest/configuration/server.html, accessed 2026-08-20 through public documentation. Observed facts only: the Admin API is optional and its availability depends on server/store configuration.

No third-party source code, snippets, tests, schemas, migrations, comments, prose, screenshots, or assets were accessed or retained.

## Functional specification

- Operators may synchronize the bundled repository policy package with deployment credentials, or provide a complete one-time username/password pair for one explicit request.
- Deployment credentials are the default when both are available; the UI progressively discloses the one-time override. Without a complete deployment pair, the UI opens the one-time form.
- One-time values must not be written to settings, responses, logs, traces, support artifacts, or background jobs. Partial pairs fail validation.
- Runtime authorization must depend only on the configured gRPC PDP. Admin HTTP is restricted to explicit package sync/status operations and must fail within a bounded timeout.
- Existing policy/schema upload remains additive. This work adds no delete/reimport operation.

## Independent design / SSO

- ISLAMU anchors: MediatR command handling, Application-owned DTO validation, Infrastructure-owned provider transport, NSwag-generated Blazor client, native `<details>` disclosure, and current Cerbos policy package service.
- Independent decisions: an empty sync request selects deployment credentials; a complete request pair overrides only credentials while the server retains endpoint authority; credentials were removed from saved provider configuration instead of adding another secret store.
- Constrained/commonplace elements: HTTP Basic authentication and gRPC/HTTP endpoint separation are protocol/interface constraints. Request-scoped values, bounded timeout, password input, and complete-pair validation are security conventions.
- Reviewer: Codex, 2026-08-20. Decision: pass.

## Dependency decision

None. No package, asset, font, dataset, or generated external artifact was added.

## Evidence

- Handoff: this file.
- Verification: repository build and intent-required test projects recorded in the task handoff.
- Journal entry: not required; canonical operational behavior is documented in configuration, secrets, self-hosting, authorization, operations, API changelog, and troubleshooting docs.
