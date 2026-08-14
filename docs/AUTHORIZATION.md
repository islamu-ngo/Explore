ABOUTME: Consolidated authorization architecture, provider routing, and CQRS request patterns.
ABOUTME: Covers server-side enforcement, Cerbos/fallback behavior, and claim-related authorization notes.

# Authorization

This document consolidates all authorization-related knowledge for the platform.

## Table of Contents

1.  [Overview](#1-overview)
2.  [Authentication vs. Authorization](#2-authentication-vs-authorization)
3.  [Core Authorization Components](#3-core-authorization-components)
    *   [Endpoint-Level Authorization](#31-endpoint-level-authorization)
    *   [Resource-Level Authorization (MediatR)](#32-resource-level-authorization-mediatr)
    *   [Runtime Authorization Provider](#33-runtime-authorization-provider)
    *   [HATEOAS Link Authorization](#34-hateoas-link-authorization)
4.  [Authorization Providers](#4-authorization-providers)
    *   [Cerbos](#41-cerbos)
    *   [Fallback RBAC Service](#42-fallback-rbac-service)
    *   [Provider Resolution Flow](#43-provider-resolution-flow)
    *   [Failure Modes](#44-failure-modes)
5.  [Roles and Permissions](#5-roles-and-permissions)
    *   [Administrative Hierarchy](#51-administrative-hierarchy)
    *   [Permission Boundaries](#52-permission-boundaries)
6.  [Implementation Patterns](#6-implementation-patterns)
    *   [CQRS Authorization Patterns](#61-cqrs-authorization-patterns)
    *   [Claim-Based Authorization](#62-claim-based-authorization)
7.  [Related Documentation](#7-related-documentation)

---

## 1. Overview

The platform employs a multi-layered authorization strategy to ensure robust and flexible access control. It combines endpoint-level checks, fine-grained resource-level authorization within the application's business logic, and a runtime-pluggable provider model that supports both a sophisticated policy engine (Cerbos) and a local role-based access control (RBAC) fallback. This ensures security is enforced at multiple depths, from the web request down to individual data access.

## 2. Authentication vs. Authorization

Per [ADR-021](adr/ADR-021-keycloak-authentication-standard.md) and [ADR-001](adr/ADR-001-authorization-provider-architecture.md), the platform enforces a strict separation of concerns between authentication and authorization:

-   **Authentication** (Keycloak): The process of verifying who a user is. Per ADR-021, Keycloak is the mandatory identity authority across all SaaS, BYOC, and on-premise deployments. Browser sign-in is handled by the Blazor BFF (`Event.Web.BffHosting`) through Keycloak OIDC Code Flow + PKCE (`S256`). Keycloak access tokens remain provider-issued; a verified ATProtocol login receives a short-lived first-party API session JWT.
-   **Authorization** (Cerbos / Fallback): The process of determining whether an authenticated user has the permission to perform a specific action on a specific resource. Cerbos acts as the policy decision point (PDP), evaluating fine-grained business resource policies against principal, resource, action, and tenant context.


ATProto does not introduce a second authorization model. The API first independently restores the submitted CarpaNet OAuth session and verifies `com.atproto.server.getSession` against the expected DID, PDS, tenant, and an exact pre-existing `UserExternalLogin`. It then issues a purpose-separated ES256 session JWT whose `sub` is the existing platform user `Guid`. Existing MediatR authorization, tenant isolation, Cerbos/local fallback policies, and HAL affordance filtering therefore operate unchanged. An ATProto DID, handle, or PDS response can never create a local user or authorize a resource by itself.

ATProto credential operations remain server-private. The bootstrap/current-session/delete bridge is excluded from API discovery and generated browser contracts; generic raw-token, `UserExternalLogin`, and `IndexedDid` CRUD routes do not exist. Public session reads return only the current user's safe metadata (`id`, `provider`, `pdsHost`, `expiresAt`), and deletion is authorized, self-scoped, and idempotent. Direct `AtprotoRecord` mutations are likewise absent: verified authentication, lifecycle-owned outboxes, and canonical Jetstream ingress are the only identity and record-write authorities. If a client renders a resource action, it must use the returned HAL relation rather than infer authority from the ATProto provider, DID, roles, or claims.

## 3. Core Authorization Components

For authentication, JWT validation, and security-header behavior, see [SECURITY.md](SECURITY.md).

### 3.1. Endpoint-Level Authorization

-   **Mechanism**: Standard ASP.NET Core `[Authorize]` and `[AllowAnonymous]` attributes on API controllers.
-   **Convention**:
    -   `GET` requests are generally `[AllowAnonymous]` to support public discovery.
    -   `POST`, `PUT`, `DELETE`, and `PATCH` requests are `[Authorize]` by default, requiring an authenticated user.
-   **Purpose**: A coarse, first-line defense at the entry point of the API.

### 3.2. Resource-Level Authorization (MediatR)

This is the core of the fine-grained authorization system, enforced within the MediatR request pipeline.

-   **Enforcement Point**: `AuthorizationBehavior<TRequest, TResponse>`. This pipeline behavior intercepts CQRS requests before they reach their handlers.
-   **Denial Behavior**: If authorization fails, the behavior throws an `AuthorizationException`. This is caught by the `GlobalExceptionHandler`, which returns an HTTP `403 Forbidden` response.
-   **Trigger Patterns**: The behavior is triggered by decorating CQRS request objects with specific interfaces or attributes. (See Implementation Patterns section below).

### 3.3. Runtime Authorization Provider

The actual logic of "is this user allowed to do this?" is delegated to a runtime provider. This allows the authorization engine to be swappable.

-   **Wrapper**: `RuntimeAuthorizationProvider` is injected into the `AuthorizationBehavior` and decides which concrete provider to use.
-   **Providers**:
    -   `CerbosAuthorizationService`: Offloads decision-making to an external Cerbos Policy Decision Point (PDP).
    -   `FallbackAuthorizationService`: Uses a local, database-backed RBAC implementation when local authorization is selected.

### 3.4. HATEOAS Link Authorization

The API uses a Hypermedia as the Engine of Application State (HATEOAS) model. HAL `_links` are the browser/client source of truth for action availability; Blazor and other clients must not recreate action gates from roles, claims, or cached local state.

-   **Mechanism**: `HateoasAuthorizationEvaluator` is used by resource assemblers and manual sync controllers before links are materialized.
-   **Behavior**: It evaluates the permissions required to execute each potential link. If the current user is not authorized, the link is omitted from the response. Permission-bound links fail closed when authorization evaluation fails; non-permission navigation links may remain when they only require authentication or static conditions.
-   **Metadata**: Link permission metadata includes resource kind, resource id, action, optional `AuthorizationScope`, and resource attributes. Descriptor-based links propagate scope and attributes from `ResourceDescriptors`; API-only links use explicit `AuthorizationActions` + `ResourceKinds` constants.
-   **Batching & Performance**: To avoid $N+1$ performance issues, the evaluator implements a **4-Phase Capability Planning Pipeline** (Candidate → Normalize → Batch Decision → Materialize). Deduplication includes resource kind/id/action, scope, and canonicalized attributes so scoped or attribute-sensitive links do not collapse into the wrong decision.
-   **Provider Optimizations**:
    -   **Cerbos**: Uses the official gRPC SDK to send deduplicated checks in a **single batch request** (`CheckResourcesAsync`).
    -   **Fallback (Local)**: Resolves the user's **Authority Profile** (admin status, tenant membership) **exactly once** per batch to eliminate redundant database/async overhead during individual link evaluation.
-   **Collection Support**: For "Get All" endpoints, all link definitions for all items in the paginated result are flattened into a single massive batch, ensuring high-scale efficiency.

Registration-form authoring uses the scoped `islamuevent_registration_form` resource with `view`, `create`, `update`, `delete`, `preflight`, `publish`, and `manage-requirements` actions. Its trusted resource context is enriched from the persisted parent Event; request bodies cannot author tenant or organizer identity. The event-level `manage-registration-workflow` entry relation and all form-level actions share the same authority: a verified organizer controller or exact tenant/event `event.registration_manager` assignment carrying `event_registration:manage`. Contributors, listing submitters, tenant-only curators, instance administrators, machine principals, missing/ambiguous organizer state, and unrelated tenant/event assignments fail closed in both Cerbos and fallback authorization.

`HateoasAuthorizationEvaluator` performs that enrichment through one bounded persisted-Event lookup per batch, applies the ambient tenant filter, removes caller-supplied authority attributes, rebuilds trusted actor/organizer context, and denies before Cerbos or fallback evaluation for missing, cross-tenant, missing-ID, or over-bound inputs. The resulting HAL relation set—not local claims or role checks—is the client action boundary.

## 4. Authorization Providers

### 4.1. Cerbos

-   **Description**: A powerful, open-source, stateless authorization service that allows policies to be defined in human-readable YAML files.
-   **Layering**: Application owns provider-neutral catalogs and checks (`AuthorizationActions`, `ResourceKinds`, `AuthorizationCheck`, `ResourceDescriptors`). Infrastructure owns Cerbos gRPC, Admin API, ZIP package export, client caching, and package publishing details.
-   **Usage**: When configured, the `CerbosAuthorizationService` translates the application's authorization request into a Cerbos `CheckResources` API call. Cerbos policy resource kinds are namespaced, for example `islamuevent_custom_property_template` and `islamuevent_custom_property_projection`.
-   **Scoped checks**: Resource descriptors always send tenant context as resource attributes. The shared instance PDP does not send a Cerbos resource `scope` by default, so bundled root policies work without tenant-scoped policy files. Set `Cerbos:UsePolicyScope=true` only when the target PDP has a complete scoped-policy chain and `engine.lenientScopeSearch=true`.
-   **BYO (Bring Your Own) Cerbos**: The platform supports a multi-tenant model where each tenant can optionally provide their own Cerbos PDP and Admin API configuration.

Event-session creation uses the `islamuevent_event_session` Cerbos resource even before a session row exists. The create check authorizes against the parent event id with `tenantId`, `eventId`, and `authorizationPhase=pre_create` attributes. Instance admins can always create sessions; tenant admins can create sessions within their tenant; event owners/managers can create sessions for their assigned event; authenticated users remain view-only. In the shared instance-provider path, pre-create session checks are evaluated through the local parity provider even when they appear inside mixed HATEOAS batches, while tenant BYO Cerbos remains authoritative when configured.

Event moderation uses explicit event actions rather than edit authority. `moderate-light`, `moderate-heavy`, and `unmoderate` are granted to instance administrators and tenant administrators in scope while `update`/`delete` remain denied to those admin roles unless a separate edit policy grants them. Organization administrators, owners, and event-role members can receive normal management/edit affordances for events they control, but they do not inherit admin moderation actions from that relationship. The active HAL/API surface emits `moderate-light`, `moderate-heavy`, and eligible `unmoderate` affordances independently from edit authority. Heavy redaction is irreversible and is advertised only when the backend can redact event-owned content, detach and delete event images through provider-backed retryable storage deletion, and send generic attendee notifications. Unmoderation is advertised only when the latest moderation record is reversible light moderation.

Global Actor and exact ATProto identity moderation use a separate instance-setting boundary. Both commands authorize `AuthorizationActions.InstanceSettings.Update` on `ResourceKinds.InstanceSetting` with resource id `global-actor-moderation`, then recheck the authenticated operator with `IAdminContext.IsInstanceAdminAsync` before loading the target. Tenant administrators have no authority over global Actor or identity state. Tenant moderation remains limited to `TenantUser`, `OrganizationTenant`, `GroupTenant`, and tenant federation or import policy. Event moderation remains content-local.

Moderated-event reads also use explicit management authorization. Public event detail and public discovery fail closed for moderated events. Authorized management callers use `view-management` through `GET /api/event/{id}/management-detail`, `GET /api/event/management/by-actor/{actorId}`, and `GET /api/event/{id}/moderation/history`. The moderation-history route returns safe audit metadata only; it must not expose original event text, slugs, URLs, image identifiers, storage object keys/paths, or raw provider errors.

Public Event eligibility is evaluated independently from management authorization. Anonymous event, program, session, group, assignment, and agenda reads inherit the parent Event eligibility gate. An ineligible Event remains available through authorized `view-management` reads, but its HAL representation uses the management self link and omits public report, claim, participation, external-action, and other public affordances. Clients must not infer public eligibility from management access.

Event reporting keeps reporter and moderator authority separate. `GET /api/event-reports/events/{eventId}/options` remains anonymous for published-event reportability discovery, while `POST /api/event-reports`, `GET /api/event-reports/my`, and `GET /api/event-reports/my/{reportId}` require the authenticated current user and return only reporter-owned, limited status metadata. Moderator queue/detail reads and triage/assign/decide/execute commands authorize against the parent `islamuevent_event` resource with the explicit event moderation action, then the handlers recheck tenant, report, event, case, assignment, and expected case concurrency stamp before mutating state. Moderation read projections are intentionally data-minimized: they do not expose stable reporter user/actor identifiers, evidence creator identifiers, decision moderator identifiers, raw provider case/signal identifiers, provider URLs, or provider correlation identifiers. UI affordances must come from HAL relations: `report-event`, `moderation-reports`, `triage-report`, `assign-report`, `decide-report`, and `execute-report-decision`.

Event provenance separates listing contribution from organizer authority. A submitter or importer does not gain registration, attendee-data, ticketing, or commercial authority; those capabilities require an assigned organizer actor and the relevant event authorization. Public-action management authorizes against `islamuevent_event`, while every event-bound organizer-claim list/detail/submit/withdraw/review check uses `islamuevent_event_organizer_claim` with parent-event metadata preserved server-side. Withdrawal uses the dedicated `withdraw-organizer-claim` action. Before provider evaluation, the authorization behavior loads the persisted claim and claimant actor, then supplies claimant user, organization, or group ownership as server-only resource attributes; route/body ownership is never trusted and the attributes are excluded from public JSON/OpenAPI. Only an authenticated non-machine principal controlling that claimant actor may withdraw: personal actors require the same user, while organization/group actors require `PermissionCodes.EventCreate` in that exact organization/group. Separate Cerbos principal attributes carry those permission-derived IDs without broadening admin memberships or derived roles. Unrelated users, curators without claimant control, and instance administrators are denied. Event policy does not accept claim actions, and both providers deny unsupported future registration, ticket, and attendee actions until their owning phases implement them. Authenticated humans may submit claims only for claimant actors they control in the current tenant; the actor must be globally active, with an active tenant user or approved organizer-eligible organization/group participation. Withdrawal intentionally checks persisted claimant ownership rather than current tenant eligibility, so a legitimate controller can revoke an active claim after the claimant becomes ineligible; approval still revalidates tenant eligibility inside the decision transaction. Machine principals and unsupported claim actions fail closed. Approval assigns `Event.OrganizerActorId` transactionally but never grants access to historical attendee data. HAL relations remain the client authority for `claim-event`, source/action links, correction suggestions, unsafe-link reports, withdrawal, and review.

Event ticket management is implemented through `AuthorizationActions.Events.ManageTickets` on the parent Event. The event HAL may expose `manage-ticket-types` and `manage-capacity-pools` independently. The ticket catalog HAL then applies the same parent-event permission metadata to `create-draft`, `clone-draft`, `create-type`, `create-pool`, `publish`, and item-level `edit` or `delete`. Community contributors and callers without exact ticket authority fail closed. Clients must not turn the two event relations into broader local authority.

Platform monetization uses `ResourceKinds.InstanceSetting` with setting key `platform-monetization` and `InstanceSettings.View` or `InstanceSettings.Update`. Endpoint classification, MediatR authorization metadata, and HAL filtering provide the normal server boundary. The query and command handlers add defense in depth by calling `IAdminContext.IsInstanceAdminAsync` before repository access. Tenant administrators, organizers, curators, and regular users cannot read or update this management resource, and the Blazor save controls appear only when the HAL document contains `edit`.

Paid-event authority follows ADR-022 through ADR-024 and is not implied by administration. `AuthorizationActions.Events.ManagePaidEventCommerce` (`manage-paid-event-commerce`) authorizes against `ResourceKinds.Event` using trusted persisted Event organizer attributes only: exactly one of `organizerUserId`, `organizerOrganizationId`, or `organizerGroupId` must accompany `organizerActorId`. The action allows only authenticated human principals who directly control that organizer actor: same canonical `userId` for user organizers, or `event:manage-finance` in the exact organizer organization/group via `eventFinanceOrganizations` or `eventFinanceGroups`. Listing contributors, event owners/managers/ticket-role assignees, tenant administrators, instance administrators, machine callers, missing organizer context, and ambiguous organizer context receive no fallback merchant authority. Future connect, paid-publish, promotion, checkout, cancel, refund, reconcile, ticket, check-in, transfer, waitlist, and add-on actions require dedicated resource decisions and matching HAL relations. Scanner capabilities are separate, narrow, expiring authorities and do not grant roster, order, payment, refund, or attendee-answer access. `ProtectedDelayedPayout` cannot be authorized until its external approval evidence gate is current.

Managed reporting routing actions use settings resources rather than event resources. Tenant routing-state reads, tenant routing updates, tenant provider readiness tests, and tenant moderation-reporting dashboard reads authorize as `AuthorizationActions.TenantSettings.View` or `.Update` on `ResourceKinds.TenantSetting` with resource id `<tenantId>:moderation-reporting` and tenant scope. Instance reporting-provider lock updates authorize as `AuthorizationActions.InstanceSettings.Update` on `ResourceKinds.InstanceSetting` with resource id `moderation-reporting-locks`, then the handler rechecks instance-admin authority. UI affordances must come from HAL rels (`routing-state`, `edit`, `test-osprey-provider`, `test-coop-provider`); hidden links stay hidden and clients must not recreate them from role claims.

Control-plane tenant fleet governance uses the instance-setting authorization boundary, not tenant membership authority. Fleet list and detail requests authorize with `ResourceKinds.InstanceSetting` and `AuthorizationActions.InstanceSettings.View` for resource id `control-plane.tenants`; activate, suspend, archive, reactivate, and schedule-purge commands use the same resource with `AuthorizationActions.InstanceSettings.Update`. Bundled Cerbos policy for `islamuevent_instance_setting` and the local fallback both allow these checks only for instance administrators, so regular users and tenant administrators are denied direct API reads as well as lifecycle writes. Tenant HAL resources evaluate the same kind, id, and action metadata before emitting lifecycle links; clients must use `_links` as the action source of truth and must not reconstruct controls from roles or claims.

Moderation provider callbacks are machine-authenticated, not browser-user authenticated. `POST /api/integrations/moderation/osprey/callback` uses the `ModerationIntegration.OspreyCallback` policy and `POST /api/integrations/moderation/coop/callback` uses the `ModerationIntegration.CoopCallback` policy. Both require authenticated API-key principals whose scopes pass `MachineScopeMapping` for event moderation authority; the Coop endpoint also verifies the configured HMAC-SHA256 webhook signature before retaining callback bytes and a unique effect pointer. Deferred execution runs under a tenant-bound internal principal limited to `webhook:process-incoming`. Effect status requires `webhook:view-delivery`; operator redrive requires the distinct `webhook:redrive-incoming` action, an authenticated actor, current processing generation, and a replayable retained callback. HAL exposes redrive only for dead-lettered state.

Registration provider callbacks are public-ingestion endpoints where provider proof is the authentication boundary. `POST /api/integrations/registration/{provider}/{bindingId}/callback` must not disclose tenant existence: binding resolution, verifier failure, duplicate delivery, stale evidence, malformed evidence, and parked outcomes acknowledge with `202 Accepted`. The intake controller does not authorize or mutate registration aggregates. A later fenced worker validates the protected receipt and executes under the Application registration-submission path.

Registration provider management is event- and tenant-scoped. Connection CRUD and approved-origin replacement authorize against tenant update authority because connections are tenant-owned secret-binding metadata. Binding, mapping, channel, health queue, manual import, retry/resolve, launch descriptor, and reconciliation routes authorize against the parent Event using `manage-registration-channels` or `view-registration-provider-health`. Event HAL emits `manage-registration-channels` and `view-registration-provider-health`; item/collection HAL emits `provider-create`, `origins`, `mappings`, `publish`, `manual-import`, `poll`, `retry`, `resolve`, and `launch-descriptor`. Studio and clients must render only those relations and must not infer actions from roles, provider names, capability codes, drift class, channel mode, or local status.

Outgoing webhook management uses the `islamuevent_webhook` resource kind. The namespaced catalog includes `webhook:view`, `webhook:create`, `webhook:update`, `webhook:delete`, `webhook:rotate-secret`, `webhook:test`, `webhook:retry`, `webhook:view-delivery`, `webhook:view-payload`, `webhook:pause`, `webhook:resume`, `webhook:reconcile-publication`, `webhook:abandon-publication`, `webhook:bulk-replay`, `webhook:manage-provider`, and `webhook:open-provider-portal`. The Svix App Portal route uses `webhook:open-provider-portal`, sensitive retained bytes use `webhook:view-payload`, and bulk replay preview/list/detail/schedule/cancel requests use `webhook:bulk-replay` through the MediatR authorization pipeline with tenant and operation attributes supplied by `ISecureRequest`. Bundled Cerbos policy grants instance admins all webhook actions and tenant admins webhook actions within their tenant scope. Organization admins remain limited to delegated organization-owned endpoint actions and cannot view payloads, reconcile/abandon provider uncertainty, manage provider configuration, or bulk replay. The dedicated incoming webhook worker can only use `webhook:process-incoming`; a tenant-admin machine needs `admin:tenant` for bulk replay. Local fallback and machine-scope mapping mirror those boundaries. HAL exposes replay scheduling/preview only on the authorized collection and cancellation only on an authorized queued operation, so clients must not infer these controls from roles or claims.

EmailDispatch admin status and tenant delivery controls use the `islamuevent_email_dispatch` resource kind. Status reads require `view`, tenant pause/resume requires `manage_tenant`, row parking requires `park`, row replay requires `replay`, terminal abandonment requires `resolve`, and an explicit `Unknown` delivered/not-delivered decision requires `reconcile`. The API controller remains `[Authorize]`, but MediatR request metadata is the enforcement boundary and supplies tenant/outbox context through `ISecureRequest`. Bundled Cerbos and local fallback deny regular users and bound tenant admins to their tenant. Global drain pause/resume and SMTP rate override instead use `islamuevent_instance_setting:view|update` for `email-dispatch.processor`; only instance administrators receive those permissions. HAL uses the same split actions so clients gate every affordance from `_links`.

Tenant role grant management uses the `islamuevent_tenant_user_role_grant` resource kind. The action catalog is `view`, `create`, and `delete`. Read projections intentionally include tenant-local user and grant identity metadata, so they are administrative APIs: list/detail requests are authenticated, carry the resolved `tenantId` through `ISecureRequest`, and require action `view` on `islamuevent_tenant_user_role_grant`. Grant and revoke commands also carry the resolved tenant id for `create` and `delete` checks. Bundled Cerbos and local fallback both deny regular authenticated users and allow tenant admins only for their resolved tenant, with instance administrators retaining cross-tenant administrative authority.

Footer management writes use the existing tenant-resource update convention rather than a separate footer resource family. Link-group, link, reorder, and footer-settings commands are authenticated, carry the resolved tenant id from `ITenantContext` through `ISecureRequest`, and require `[AuthorizeResource(ResourceKinds.Tenant, AuthorizationActions.Update)]` before handlers mutate tenant footer configuration. This keeps Cerbos and local fallback aligned with tenant administration authority; regular authenticated users are denied, tenant admins are limited to their resolved tenant, and a future distinct `Footer.Manage` permission must add resource kind/action metadata, Cerbos policy/schema, local fallback, HAL metadata, and tests in one slice.

Support-access session management uses the `islamuevent_support_access_session` resource kind. The foundational action catalog is `view`, `list`, `start`, `stop`, `view_audit`, and `force_stop`. Runtime support-access authorization preserves actor identity, validates persisted sessions per request when the BFF/server forwards a trusted support-access session id, matches the target tenant, and gates write behavior by explicit write-mode policy rather than by tenant role grants or browser-visible claims. `RuntimeAuthorizationProvider` applies this boundary before routing to local RBAC, instance Cerbos, tenant BYO Cerbos, or HAL batch evaluation: inactive forwarded sessions deny tenant-scoped resources, read-only sessions deny mutation actions, and write sessions deny mismatched tenant resources. Boundary denials emit warning logs, bounded `Explore.Business` metrics, and trace tags/events so HAL affordance filtering and MediatR command authorization remain observable through the same provider path. The provider forwards only bounded support metadata such as session id, actor id, target tenant/user id, mode, and `supportAccessAllowsWrites`; ticket and reason text stay out of policy attributes. Local fallback and bundled Cerbos policy/schema files both understand this resource kind; new support-access actions must update both paths plus the HAL link policy so clients continue to gate affordances from `_links`.

User profile updates use the `islamuevent_user` resource with the target domain user id as the resource id. The static Cerbos policy mirrors local fallback RBAC: instance admins can manage all users, tenant admins can manage users in their tenant, and an authenticated human user can `update` only the resource whose id matches either the Cerbos principal id or `principal.attr.userId`. That `userId` attribute carries the internal domain user id so self-service account settings still work when the OIDC subject is an external provider identifier. The `actorId` user-resource attribute is optional because self-service account settings updates authorize before the actor/profile-picture context has to be loaded. In the shared instance-provider path, `islamuevent_user:update` checks are evaluated through local parity so a stale shared PDP package cannot block the canonical self-service user handler; tenant BYO Cerbos remains authoritative when configured.

### 4.1.1 Cerbos Package Upload

For the hosted production path, `.github/workflows/cerbos-policy-check.yml` publishes schemas and policies to the Cerbos Admin API only after `Cerbos Policy Validation` succeeds on a `push` to `main`. The publish job uses the protected `production` GitHub Environment approval gate and the repository secrets documented in [CONFIGURATION.md](CONFIGURATION.md#deployment-cicd-secrets).

For a Coolify-managed Cerbos PDP, use [CERBOS_COOLIFY.md](CERBOS_COOLIFY.md). That runbook covers the pinned Docker image, PostgreSQL policy-store schema, Admin API password hash, Traefik gRPC `h2c` label, and direct `cerbosctl` upload from this repository's `cerbos/policies/` folder.

When CI/CD publishing or Admin API sync is unavailable, operators can still push local policy and schema files with `cerbosctl` directly:

```bash
docker run --rm -it -v "/home/{user}/ISLAMU/Github/Event/cerbos/policies/_schemas:/schemas:ro" ghcr.io/cerbos/cerbosctl:0.53.0 --server={cerbos.example.com:443} --username={username} --password={password} put schema -R /schemas

docker run --rm -it -v "/home/{user}/ISLAMU/Github/Event/cerbos/policies:/policies:ro" ghcr.io/cerbos/cerbosctl:0.53.0 --server={cerbos.example.com:443} --username={username} --password={password} put policy -R /policies
```

If the password contains spaces, wrap it in quotes:

```bash
--password="password with spaces"
```

### 4.2. Fallback RBAC Service (`FallbackAuthorizationService`)

-   **Description**: A local, in-database implementation of Role-Based Access Control (RBAC) and Attribute-Based Access Control (ABAC). It serves as the primary authorization engine when running without an external Cerbos PDP container (e.g., local development, single-tenant deployments, ATProto/PDS standalone nodes). It is not used as an automatic fallback when the instance-level Cerbos provider is selected and unavailable.
-   **Class Architecture**: The service is decomposed into four partial classes in `src/Explore.Infrastructure/Services/`:
    -   `FallbackAuthorizationService.cs`: Entry point, primary switch dispatcher, safe-mode latch, and Activity correlation logging.
    -   `FallbackAuthorizationService.Evaluators.cs`: Granular evaluator logic across all 40 domain resource kinds (`organization`, `event`, `storage_object`, `user`, `webhook`, `support_access_session`, etc.).
    -   `FallbackAuthorizationService.Batch.cs`: High-throughput batch evaluation engine utilizing single-pass `AuthorityProfile` pre-resolution and `EventAuthoritySnapshotService` batch querying.
    -   `FallbackAuthorizationService.MachineCaller.cs`: Scope-ceiling and owner-type evaluation for API key machine principals.
-   **Notable Evaluation Rules**:
    -   **Instance Admin Bypass**: Instance administrators bypass standard checks except for direct event authority requirements (e.g., `event:manage-tickets` requires explicit event authority).
    -   **Tenant Settings & Governance Locks**: Updates check `isLockedByInstance == true`. If locked by infrastructure operators, non-instance admin update attempts are denied. `tenant.branding` document updates are explicitly exempted from instance locks.
    -   **Event Moderation**: `moderate-light`, `moderate-heavy`, and `unmoderate` actions require Instance Admin or Tenant Admin authority in scope. Event managers, owners, and organization admins cannot moderate events.
    -   **User Profiles**: Authenticated users can view/update their own profile (`targetUserId == currentUserId`); other targets require Tenant Admin or Instance Admin authority.
    -   **Storage Objects**: Downloads are allowed if active and visibility is `PublicImage` or `AuthenticatedTenant`, or if private and `createdBy == currentUserId`.
    -   **Support Access Sessions**: Active support sessions tag OpenTelemetry traces, enforce tenant isolation (`support_access_target_tenant_mismatch`), and block write actions when in read-only mode (`support_access_read_only`).

### 4.3. Provider Resolution Flow (`RuntimeAuthorizationProvider`)

The `RuntimeAuthorizationProvider` selects the authorization engine for a given check in the following order:

1.  **Tenant BYO Cerbos**: If the current tenant has a specific "Bring Your Own" Cerbos instance configured via `ICerbosConfigResolver`, all resource checks route to that endpoint.
2.  **Handler-Owned Local Parity Bypasses**: Specific requests (self-service `user:update`, pre-create `event:create`, `organization:create`, `event_session:create`, `ai_conversation`) are identified via `GetHandlerOwnedLocalCheckIndexes()` and routed directly to `FallbackAuthorizationService`. This guarantees that stale external PDP policy packages cannot block canonical self-service or pre-create handlers.
3.  **Instance-Level Setting**: If not bypassed, the system checks the instance-wide `AuthorizationProvider` setting (`SystemSetting` key `GovernanceSettingKeys.Security.AuthorizationProvider`, cached for 1 minute):
    -   If `"cerbos"`, it routes to the shared instance `CerbosAuthorizationService` and fails closed if the PDP is unavailable.
    -   If any other value (or null / `"local"`), it routes to `FallbackAuthorizationService`.

If reading the instance provider setting fails, runtime authorization uses the Cerbos fail-closed path and logs only safe `FailureType` metadata. It does not default open to local RBAC.

### 4.4. Failure Modes & The One-Way Safe-Mode Latch

The system is designed to fail safely — deny by default when the configured provider is unavailable.

-   **Instance Cerbos Failure**: If the connection to the instance-level Cerbos PDP fails (e.g., network error, timeout), all authorization checks are denied. The operator explicitly chose Cerbos; falling back to a potentially more permissive local RBAC would silently bypass intended policies. Restore Cerbos connectivity or explicitly switch the authorization provider setting to local RBAC through instance administration to recover without Cerbos.
-   **BYO Cerbos Failure & The Safe-Mode Latch**:
    -   If the tenant's BYO configuration has `failure_mode=closed`, `FallbackAuthorizationService.ActivateSafeMode()` is triggered. Safe mode is a **one-way, thread-safe latch** for the provider instance: once activated, it logs a critical alert and denies all non-instance-admin requests to prevent bypassing stricter tenant policies.
    -   If `failure_mode=open`, the fallback provider runs its standard RBAC logic.
-   **BYO Configuration Failure**: If tenant BYO configuration cannot be resolved, runtime authorization activates provider-instance safe mode instead of silently using local RBAC.
-   **Blank BYO PDP Endpoint**: If a tenant explicitly sets `cerbos.mode=custom_endpoint` but leaves the custom PDP endpoint blank, the resolver preserves BYO mode, failure mode, and explicit BYO Admin API config. Runtime authorization then applies the configured `failure_mode`; it does not fall back to the instance PDP.
-   **Safe Logging**: Runtime failure logs avoid raw endpoints, Admin API credentials, JWTs/tokens, response bodies, and exception objects/messages. They keep safe operational metadata such as failure type, action, mode, counts, request id, and correlation id.

### 4.5. Machine Principal (API Key) Security Architecture

API key machine callers evaluate authorization through `EvaluateMachineCallerAccessAsync`:

1.  **Registration Workflow Prohibition**: Machine callers are strictly barred from modifying registration forms, registration workflows, or managing event tickets.
2.  **Scope Ceiling (`MachineScopeMapping`)**: External API key scopes (`events:write`, `organizations:read`, `admin:tenant`, `mcp:propose`, etc.) establish a maximum capability ceiling. A machine caller must satisfy this scope ceiling in addition to owner-type authority.
3.  **Owner-Type Boundaries (`ExternalApiKeyOwnerType`)**:
    -   `InstanceAdmin`: Unrestricted platform-wide access.
    -   `Tenant`: Bound to the key's `TenantId`. Cannot access instance settings, ATProto records, or platform namespaces.
    -   `Organization` / `Group`: Bound strictly to resources owned by `context.OwnerId`.
    -   `User`: Bound to user-owned resources or tenant resources where the user holds Tenant Admin or Organization Admin authority.

### 4.6. Batch Capability Planning Engine

To prevent $N+1$ database queries during HATEOAS link evaluation for paginated resource lists:

1.  **Authority Profile Pre-Resolution**: `FallbackAuthorizationService.Batch.cs` resolves an immutable `AuthorityProfile` (Instance Admin, Tenant Admin, Admin Org IDs, Admin Group IDs, Event Create Org/Group IDs) in **a single pass** at the start of a batch check.
2.  **Batch Event Authority Snapshots**: `IEventAuthoritySnapshotService.GetForUserAndEventsAsync()` extracts distinct event IDs from all event-scoped checks and loads active `EventRoleAssignment` records in **a single SQL query**.
3.  **In-Memory Evaluation Loop**: `EvaluateWithProfile()` evaluates all checks in CPU memory against the pre-resolved `AuthorityProfile` and event authority snapshot, executing batch checks in **$O(1)$ database calls**.

## 5. Roles and Permissions

### 5.1. Administrative Hierarchy

The platform defines a clear hierarchy of roles with distinct boundaries. See [ADMIN_HIERARCHY.md](ADMIN_HIERARCHY.md) for a detailed breakdown.

-   **Instance Administrator**: Operates the infrastructure. Can manage tenants but cannot access tenant business data.
-   **Tenant Administrator**: Manages a specific community (tenant). Can configure the tenant, manage users and content within it, but cannot override instance-locked policies.
-   **Organization Administrator**: A user with elevated privileges within a specific organization inside a tenant.
-   **Standard User**: A regular platform user.

### 5.2. Permission Boundaries

Strict boundaries are enforced to protect tenant autonomy and platform integrity. For example, an Instance Admin cannot read tenant business data, and a Tenant Admin cannot disable globally enforced security policies.

Tenant user participation is tenant-local. A global `User` authenticates the person or external identity, but tenant-admin-controlled lifecycle and moderation state lives in `TenantUser`/`TenantUserProfile`. Tenant role authority lives in `TenantUserRoleGrant`, an auditable child of `TenantUser`. Local membership checks require an active tenant-local user record plus an unrevoked tenant-scoped grant, so a suspension, ban, removal, or profile moderation action in one tenant does not affect the same external identity in another tenant.

Managed-provider provisioning follows the same boundary. Provider/operator automation must authenticate through instance-admin authority before it can create customer tenants. The provisioned ERP customer/admin receives tenant-local `TenantUser`, `TenantUserProfile`, user actor, external-login binding, and `TenantUserRoleGrant` tenant-admin authority for that tenant only; this flow must not create `PlatformUserRole` rows or `InstanceAdmin` API keys for customer/admin identities.

Organization membership authority is also resource scoped. `OrganizationMember` list/detail reads use `ISecureRequest` plus `[AuthorizeResource(ResourceKinds.OrganizationMember, AuthorizationActions.OrganizationMembers.View)]`; list reads send the resolved tenant id and organization id, while detail reads send the member id and are enriched by `AuthorizationBehavior` with tenant, organization, and user attributes before the provider decision. The Cerbos resource kind is `islamuevent_organization_member`. Local fallback mirrors the policy by allowing tenant administrators in the resolved tenant and organization administrators for the target organization, while denying regular authenticated users. HAL collection/item affordances must use the same resource/action metadata instead of local role or claim checks.

Event participation configuration uses the event `manage-registrations` action, not generic event update. Cerbos and local fallback allow only a controller of the verified `OrganizerActorId` or an event assignment carrying `EventRegistrationManage`. A community listing contributor, unrelated actor controller, tenant administrator, instance administrator, or machine receives no implicit participation-management authority. The MediatR command and `configure-participation` HAL affordance use the same action metadata.

Event attendee contact export is a separate consent-resource decision. A management Event can emit `export-attendees` only when it has a verified organization organizer; the candidate relation requires `ExportSharedContacts` on `event_contact_share_consent` with exact tenant and organizer-organization attributes. The export command independently carries the same resource/action metadata. Blazor checks only the relation and does not infer export authority from `view-participants`, roles, claims, contributor status, or instance/tenant administration.

Footer configuration authority is tenant-scoped. Footer management writes reuse `ResourceKinds.Tenant` with `AuthorizationActions.Update`, and each command sends the resolved `tenantId` as the resource id and authorization attribute. Local fallback allows tenant administrators only for the ambient tenant and denies create/delete tenant-resource actions for non-instance administrators; Cerbos evaluates the same tenant update action. Footer UI affordances must be emitted from the same resource/action decision if HAL links are added later.

Email dispatch row operations are tenant-scoped operator actions. The status, tenant pause/resume, park, replay, resolve, and reconcile requests use `ResourceKinds.EmailDispatch` with `tenantId` and row-level `outboxId` where applicable. Global processor requests use `ResourceKinds.InstanceSetting` for `email-dispatch.processor`. EmailDispatch DTOs and logs remain bounded and never expose recipient email, message body, subject, provider message ids, raw provider errors, pause actors, or reconciliation evidence text.

## 6. Implementation Patterns

### 6.1. CQRS Authorization Patterns

Authorization is triggered in the MediatR pipeline based on one of three patterns applied to a command or query request class.

1.  **`IAuthorizedRequest` Interface**:
    -   **Use When**: The resource kind, ID, and action are dynamic and depend on the request's properties.
    -   **Implementation**: The request class implements `IAuthorizedRequest` and provides the `ResourceKind`, `ResourceId`, and `Action`.

2.  **`[AuthorizeResource]` Attribute**:
    -   **Use When**: The resource kind and action are static for all requests of this type.
    -   **Implementation**: The request class is decorated with `[AuthorizeResource(ResourceKind, Action)]`.

3.  **`[AuthorizeResource]` Attribute + `ISecureRequest` Interface**:
    -   **Use When**: The resource kind and action are static, but the resource ID or other attributes needed for the policy are determined at runtime.
    -   **Implementation**: A combination of the attribute and the interface. The behavior prefers the dynamic values from `ISecureRequest` at runtime.

Notification preference organization and group queries/commands use this pattern with `ResourceKinds.Organization` or `ResourceKinds.Group` and `AuthorizationActions.View`/`AuthorizationActions.Update`. Current-user preference endpoints are authenticated user-self endpoints; organization/group preference endpoints still pass through the resource authorization pipeline before handlers run.

Participation requirement writes use `[AuthorizeResource(ResourceKinds.RegistrationForm, Attach|Detach)]` with `ISecureRequest` event and requirement attributes. Persisted Event enrichment determines verified organizer control and explicit `event.registration_manager` authority before handlers load the attachment graph. Cerbos and local fallback use the same action catalog; tenant-only administration, listing contribution, machines, and unrelated assignments do not authorize these writes.

### 6.2. Claim-Based Authorization

-   **User ID Extraction**: The standard fallback chain for user identity extraction is `sub` -> `nameidentifier` -> `sid`.
-   **`internal_user_id`**: This is a separate BFF-enriched local-user claim used by some UI/admin helpers after external identity resolution. It is not part of the general fallback chain.
-   **Admin Claims**: A `BffAdminClaimsTransformation` service enriches the user's principal with specific `admin` claims after authentication, which can be used for UI-level authorization checks.

## 7. Related Documentation

-   [SECURITY.md](SECURITY.md): Covers the broader security model, including authentication and JWT configuration.
-   [AUTHORIZATION_PATTERNS.md](AUTHORIZATION_PATTERNS.md): Quick reference for MediatR request-shape choices and provider fallback rules.
-   [ADMIN_HIERARCHY.md](ADMIN_HIERARCHY.md): Details the roles and responsibilities of different administrative levels.
-   [API.md](API.md): Describes the MediatR pipeline and how authorization fits into the request flow.
-   [adr/ADR-001-authorization-provider-architecture.md](adr/ADR-001-authorization-provider-architecture.md): The architectural decision record for Cerbos PDP authorization provider integration.
-   [adr/ADR-021-keycloak-authentication-standard.md](adr/ADR-021-keycloak-authentication-standard.md): The architectural decision record standardizing Keycloak as the mandatory identity plane.
