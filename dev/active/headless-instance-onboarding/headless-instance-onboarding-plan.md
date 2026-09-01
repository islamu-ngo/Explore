<!-- ABOUTME: Canonical implementation plan for configured-administrator headless instance onboarding. -->
<!-- ABOUTME: Defines behavior, architecture, security invariants, phases, and verification without implementing runtime code. -->

# Headless Instance Onboarding — Implementation Plan

Last Updated: 2026-09-01 Europe/Brussels

## 0. Planning Metadata

- **Original request:** Replace mandatory first-run onboarding UI with an
  enterprise-grade configured-administrator path. An offline-generated
  `.env` plus ConfigurationManifest configures the instance. The exact
  configured AT Protocol account or existing Keycloak realm user becomes the
  initial platform administrator only after authenticating successfully.
- **Task directory:** `dev/active/headless-instance-onboarding/`
- **Planning status:** Draft; ready for user review
- **Change classification:** Behavioral Delta
- **Primary intent:** `external-infrastructure-bootstrap` — Tier 1 Security,
  system-level, exhaustive exploration, mandatory adversarial tests, and
  anonymized Epistemic MAD review
- **Supporting intents:**
  - `add-cqrs-handler` for the provider-neutral claim command and handler
  - `bff-auth-bug` for fail-closed pre-completion authentication routing
  - `openapi-contract-change` for additive onboarding status fields and
    regenerated clients
  - `add-ef-migration` for generated multi-provider bootstrap-state schema
    replacement
- **Explicitly not matched:**
  - `add-write-endpoint`: no new public write endpoint is planned; existing
    authenticated synchronization and verified ATProto seams are reused
- **Relevant skills:** implementation-plan, i-vsd, grill-me,
  criticality-guardrail, auth-patterns, blazor-bff-patterns,
  clean-architecture-rules, cqrs-mediatr-guidelines,
  dotnet-efcore-guidelines, blazor-ui-conventions, ip-clean-room,
  conventional-commit
- **Relevant rules:** API controllers, Application layer, auth trust
  boundaries, Blazor server, Blazor client, Domain, EF Core persistence,
  generated migrations, privacy/PII, tests, IP clean-room
- **Primary layers:** Event.Setup.Core offline configuration; Domain bootstrap
  state; Application identity/claim orchestration; Persistence and generated
  provider migrations; API contracts; Blazor BFF authentication; generated
  client and startup routing; Infrastructure startup; operator documentation
- **Complexity:** XL — one Tier 1 cross-layer state transition, two provider
  authentication protocols, multi-replica concurrency, five database
  providers, BFF routing, generated contracts, privacy-sensitive
  configuration, operator recovery, and release evidence
- **I-VSD document:**
  [islamic-value-sensitive-design/i-vsd-headless-instance-onboarding.md](../../../islamic-value-sensitive-design/i-vsd-headless-instance-onboarding.md)
- **I-VSD reviewed input revision:**
  `sha256:5c67de2a4b210f6d57239a6eb7f4c7ae38cf6bf4a0b9321e32c77410065a9ca9`
- **I-VSD status / disposition:** current and plan-aligned after triad
  revalidation
- **CTO review:** Not reviewed
- **User approval:** Awaiting approval for this exact workstream revision
- **Grill-Me intake:** Resolved. Setup Assistant remains an offline generator
  only. ConfigurationManifest owns portable non-secret instance/tenant
  configuration. Deployment-local environment/secret authority names the
  intended provider account. Keycloak binds exact configured issuer plus
  `sub`; ATProto binds the DID returned by verified OAuth. No email, username,
  handle, first-login order, provider role, browser header, or manifest field
  may select the initial administrator. Backward compatibility is explicitly
  rejected; clean replacement is approved.
- **Reviewed evidence revision:**
  `sha256:5c67de2a4b210f6d57239a6eb7f4c7ae38cf6bf4a0b9321e32c77410065a9ca9`
- **Allocated change identities:**
  - migration: `CHG-01M1ETX06HRETFBJTK6SCZGBZ6`
  - authority-qualified provider identity:
    `CHG-01M1ETXMS84KS8ASDW4GR22Q3J`
  - runtime activation: `CHG-01M1EQWDAHHXQ3AD29B4Y0645B`

## 1. Executive Summary

The platform will gain a second first-run mode named
`ConfiguredAdministrator`. It is disabled unless a closed, complete,
server-only configuration is present and ConfigurationManifest bootstrap has
succeeded. While pending, the instance does not render `/setup`; it routes the
browser directly to the configured provider and permits only that provider's
authentication path. The instance remains healthy so authentication can
finish.

The first exact, cryptographically verified match claims a durable pending
bootstrap generation. One serializable transaction creates or resolves the
local user, personal actor, authority-qualified external login, platform
administrator grant, optional default-tenant administrator state, canonical
onboarding settings, and completed bootstrap marker. Only after commit does
the platform lock the setup secret, invalidate caches, refresh authority, and
resume normal routing.

Keycloak uses configured issuer plus exact `sub`. ATProto uses the DID returned
by the verified OAuth security gateway and gains one narrow exception to its
existing "account must already be linked" rule. The provider-neutral claim
operation is shared; provider adapters remain separate.

ConfigurationManifest remains identity-free and portable. The offline Setup
Assistant may generate the new `.env` keys and manifest but gains no runtime
client, token, endpoint, or live authority.

### Intended outcome

- Automated deployments never show the first-run onboarding UI when valid
  configured-administrator inputs are supplied.
- An existing Keycloak realm user or configured ATProto DID becomes the local
  initial administrator after proving control through normal authentication.
- Wrong, stale, raced, or nonmatching identities cannot create users, grant
  roles, complete onboarding, or disclose the configured selector.
- Operators can correct a pending binding explicitly before completion;
  configuration can never transfer authority after completion.

### Explicit non-goals

- Setup Assistant runtime connectivity, live apply, token storage, or instance
  mutation
- Administrator identity, PII, provider binding, secret reference, role grant,
  or completion state in ConfigurationManifest
- Importing Keycloak users through a management API
- Creating an administrator password, temporary provider credential, or
  Keycloak realm role
- First-login-wins behavior
- Email, username, handle, display name, or provider-role authorization
- Automatic administrator transfer or revocation after onboarding completion
- New public write endpoint or browser-submitted identity selector
- Backward-compatibility aliases for old identity-key representation or status
  contracts

## 2. Source-Grounded Current State Report

### 2.0 Pre-Flight Structural Context

The repository knowledge-graph database exists, but no callable
code-review-graph tool was exposed in this session. Roslyn workspace-symbol
queries timed out while loading the solution. The following bounded impact
slice is therefore verified from owning source files, focused text search, and
two independent read-only architecture investigations.

```yaml
# Injected Structural Context (Pre-Flight Blast Radius)
Target: Explore.Application.Features.InstanceOnboarding.CompleteInstanceOnboardingCommandHandler.Handle
Callers (Upstream):
  - Explore.API.Controllers.InstanceOnboardingController.Complete
  - Explore.Blazor.Client.Services.InstanceOnboardingService.CompleteAsync
  - Explore.Blazor.Client.Pages.Onboarding.InstanceOnboarding.CompleteOnboardingAsync
Identity Entry Points:
  - Explore.API.Controllers.UserController.SyncUser
  - Explore.Application.Features.Authentication.Atproto.BootstrapAtprotoSessionCommandHandler.Handle
  - Explore.Blazor.Services.BffAdminClaimsTransformation.EnrichPrincipalAsync
Routing Gates:
  - Explore.Blazor.Extensions.MiddlewareExtensions.HandleStartupRedirectAsync
  - Explore.Blazor.Extensions.BffAuthEndpoints.ShouldGateForOnboardingAsync
Callees (Downstream):
  - IInstanceBootstrapStateRepository.GetCurrent/Create/Update
  - IUserExternalLoginRepository.GetByProviderAndKey/Create
  - IPlatformUserRoleRepository.GetByUserAndRole/Create
  - ITenantCreationService.CreateInCurrentTransactionAsync
  - ISetupSecretProvider.Lock
Impacted Flows:
  - Flow: Interactive instance onboarding (Tier 1 Security)
  - Flow: Keycloak post-login user synchronization (Tier 1 Security)
  - Flow: ATProto verified OAuth session bootstrap (Tier 1 Security)
  - Flow: BFF startup and authentication routing (Tier 1 Security)
  - Flow: ConfigurationManifest post-migration bootstrap (Tier 1 Security)
Test Coverage:
  - tests/Event.API.IntegrationTests/Features/InstanceOnboardingControllerTests.cs
  - tests/Event.API.IntegrationTests/Features/SetupSecretFlowTests.cs
  - tests/Event.API.IntegrationTests/Features/UserExternalLoginIntegrationTests.cs
  - tests/Explore.Blazor.IntegrationTests/Services/BffAdminClaimsTransformationTests.cs
  - tests/Explore.Blazor.IntegrationTests/Endpoints/AtprotoAuthenticationFlowTests.cs
  - tests/Event.Persistence.IntegrationTests/TenantIsolation/UserExternalLoginRepositoryBypassTests.cs
  - tests/Event.Architecture.Tests/InstanceOnboardingOpenApiContractTests.cs
```

### 2.1 Evidence Log

| Claim | Evidence | Confidence | Notes |
|---|---|---:|---|
| `InstanceBootstrapState.IsCompleted` is the launch marker | `src/Explore.Domain/InstanceBootstrapState.cs`; `GetInstanceOnboardingStatusQueryHandler.cs` | High | Null or false is incomplete |
| Only interactive completion currently marks the instance complete | `CompleteInstanceOnboardingCommandHandler.cs` and `InstanceOnboardingController.Complete` | High | ConfigurationManifest never invokes this path |
| Completion already creates local identity and authority atomically | `CompleteInstanceOnboardingCommandHandler.Handle` | High | User, Actor, external login, role, tenant state, settings, bootstrap row |
| BFF routes incomplete root/login requests to `/setup` | `MiddlewareExtensions.HandleStartupRedirectAsync`; `BffAuthEndpoints.ShouldGateForOnboardingAsync` | High | Protected setup cookie is the only bypass |
| BFF skips user synchronization while onboarding is incomplete | `BffAdminClaimsTransformation.EnrichPrincipalAsync` | High | Configured claim cannot currently start |
| Keycloak external identity is represented by provider plus subject | `PlatformIdentityPrincipalExtensions`; `UserController.SyncUser` | High | Current storage does not bind issuer |
| ATProto uses verified DID but rejects unknown links | `BootstrapAtprotoSessionCommandHandler.Handle` | High | Exact-link lookup follows cryptographic verification |
| External logins are globally unique by provider plus key | `UserExternalLoginConfiguration.cs`; `UserExternalLoginRepository.cs` | High | Tenant filter is deliberately bypassed for authentication |
| Current Keycloak sync may auto-match verified email | `SyncUserCommandHandler.Handle`; `ResolveCurrentUserIdByIdentityRequestHandler` | High | Forbidden for initial admin selection |
| Manifest portability excludes PII and provider binding | `ConfigurationPortabilityRegistry.cs`; `docs/CONFIGURATION_MANIFEST.md` | High | Identity must remain deployment-local |
| Setup Assistant currently owns offline configuration artifacts | User correction plus active setup workstream evidence | High | Runtime connectivity is explicitly out of scope |
| No configured-admin bootstrap implementation exists | Focused searches for bootstrap admin mode/subject/headless completion | High | New contracts and behavior required |
| Core analyzed paths were clean after analysis | Bounded `git status --short` returned no entries for the analyzed paths | High | Shared tree still contains substantial unrelated work elsewhere |

### 2.2 Existing Implementation

#### Domain

`InstanceBootstrapState` is a mutable persistence entity with a binary
`IsCompleted` flag, completion timestamp/user, creation timestamp, and selected
deployment mode. It has no explicit pending configured-identity state,
generation, selector fingerprint, completion method, or transition methods.

`User` owns a required `UserPii` extension and optional provider summary.
`UserExternalLogin` is the authoritative external-provider-to-local-user
binding. `PlatformUserRole` stores local platform administrator authority.

#### Application

`CompleteInstanceOnboardingCommandHandler` validates the profile, resolves
authoritative deployment mode, creates the default tenant for single-tenant
mode, creates a missing user/actor/login, persists settings, grants platform
and tenant roles, and writes completed bootstrap state in one transaction.
Setup-secret lock, cache invalidation, deployment-cache refresh, JWT authority
reload, and bounded bootstrap audit happen after commit.

`SyncUserCommandHandler` performs ordinary provider synchronization. It checks
exact provider login first, then a caller-projected local ID, then verified
email for Keycloak/Google. ATProto without email requires a pre-existing exact
link.

`PlatformIdentityPrincipalExtensions` owns provider subject, provider
classification, provider ID, email, names, and email-verification projection.
`CurrentUserResolutionExtensions` resolves local identity through exact
external login and provider-specific fallback logic.

#### ATProto

`BootstrapAtprotoSessionCommandHandler` verifies the OAuth/PDS session through
`IAtprotoOAuthSecurityGateway`, obtains the verified DID, requires an exact
`UserExternalLogin("atproto", did)`, then persists ATProto subject/session state
inside a serializable transaction before token issuance. An unknown configured
administrator DID cannot currently reach local session issuance.

#### API

`InstanceOnboardingController.Complete` is authenticated and setup-secret
gated. It builds identity only from the authenticated principal, runs
preflight, and dispatches the completion command. `UserController.SyncUser` is
the existing authenticated post-login synchronization seam. No new endpoint is
needed for configured claim.

#### Blazor BFF

Startup middleware and auth endpoints independently gate root and provider
challenge routes while onboarding is incomplete. `BffAdminClaimsTransformation`
normally synchronizes a user and reloads persisted admin authority at sign-in,
but explicitly skips both while incomplete.

#### ConfigurationManifest and offline configuration

ConfigurationManifest bootstrap runs after migrations and seeding, applies
portable instance/tenant settings atomically, and records its own operation.
It does not own authentication topology, provider binding, PII, role grants,
or `InstanceBootstrapState`.

Event.Setup.Core owns the generated offline environment catalogue and dotenv
contract. It is the correct place to teach Setup Assistant about new
deployment-local keys without creating runtime connectivity.

### 2.3 Existing Tests And Verification Coverage

Existing protection includes:

- interactive completion, preflight, identity creation, and deployment-mode
  authority in `InstanceOnboardingControllerTests`
- setup-secret authentication and completed-state behavior in
  `SetupSecretFlowTests`, `SetupSecretAuthorizationMatrixTests`, and
  `SetupSecretProviderTests`
- exact external-login uniqueness and tenant-filter bypass in persistence/API
  integration tests
- Keycloak/Google/ATProto synchronization behavior in user controller and
  Application tests
- ATProto OAuth verification/session behavior in
  `AtprotoAuthenticationFlowTests` and Application tests
- BFF admin-claims and setup-secret forwarding behavior in
  `Explore.Blazor.IntegrationTests`
- generated onboarding OpenAPI shape in
  `InstanceOnboardingOpenApiContractTests`
- startup routing and onboarding pages in Blazor Client tests

Missing high-leverage coverage:

- configured binding state transitions and generation mismatch
- exact Keycloak issuer plus `sub` claim
- identical `sub` from a different issuer
- verified ATProto DID first claim
- submitted/expected DID mismatch after provider verification
- first-login/email/username/handle/provider-role takeover attempts
- simultaneous exact and nonmatching claims against a real relational engine
- rollback after identity creation but before bootstrap completion
- multi-replica selector-digest mismatch
- pending BFF route matrix and unknown-status fail-closed behavior
- zero identifier/PII disclosure across logs, status, health, and errors
- manifest/export proof that administrator binding remains absent

### 2.4 Existing Documentation And Contracts

- `docs/CONFIGURATION_MANIFEST.md` defines strict non-secret portability and
  explicitly excludes provider and operator authority.
- `docs/SELF_HOSTING.md` documents the current interactive setup wizard,
  setup-secret lifecycle, Keycloak configuration, and managed provisioning.
- `docs/CONFIGURATION.md` and the Event.Setup.Core generated catalogue document
  environment metadata.
- `docs/SECRETS.md` owns source-of-truth and no-fallback secret behavior.
- `docs/TROUBLESHOOTING.md` owns operator recovery.
- `docs/OPERATIONS.md` owns startup/diagnostic behavior.
- `schemas/openapi_islamu-event.json`,
  `docs/API_CONTRACT_INVENTORY.md`, and
  `src/Explore.Blazor.Client/Clients/EventApiClient.g.cs` are generated
  contract artifacts.
- `docs/API_CHANGELOG.md` must record the additive status-contract change.
- Public security/configuration/operator/migration impact requires
  `docs/releases/changes/CHG-01M1EQWDAHHXQ3AD29B4Y0645B.yaml`.

### 2.5 Current Pain Points / Improvement Areas

- Onboarding state is binary even though configured bootstrap needs a durable
  pending security state.
- BFF status and routing cannot distinguish interactive setup from configured
  administrator authentication.
- Keycloak provider keys omit issuer/realm authority and can collide after
  authority replacement.
- Ordinary verified-email auto-linking is too permissive for an initial
  platform privilege grant.
- ATProto exact-link enforcement creates a circular first-account problem.
- Completion orchestration is coupled to interactive request shape and cannot
  consume authoritative startup/manifest state.
- Concurrent completion relies on a filtered index but does not map the losing
  transaction to bounded idempotent/conflict behavior.
- Setup Secret flags for managed provisioning do not represent configured
  administrator authority and must not be overloaded.
- Current unknown-status BFF paths can fail open rather than presenting a
  bounded authentication-unavailable outcome.

### 2.6 Unknowns After Investigation

No scope-, architecture-, API-, or task-changing questions remain. The
following implementation detail is deferrable and owned by a named task:

- **Exact generated migration timestamps:** EF tooling chooses them during
  Task 3.2. The task and phase commit contract use restrictive Git pathspecs
  bound to the exact migration name
  `AddConfiguredAdministratorBootstrapState`, and the ledger must record the
  generated paths before staging.

## 3. Proposed Future State: Behavioral Contract & Scenarios

### Requirement 3.1: Configured mode bypasses onboarding UI

When configured-administrator mode is valid and pending, the system **SHALL**
route first-run users directly to the configured authentication provider and
**MUST NOT** render or require the interactive onboarding UI.

#### Scenario 3.1A: Pending Keycloak administrator

- **GIVEN** valid configured-administrator inputs select Keycloak and the
  instance is pending
- **WHEN** a browser requests the instance root
- **THEN** it is routed through the configured Keycloak challenge without
  visiting `/setup`

#### Scenario 3.1B: Pending ATProto administrator

- **GIVEN** valid configured-administrator inputs select ATProto and the
  instance is pending
- **WHEN** a browser requests the instance root
- **THEN** it reaches only the ATProto authentication entry flow and no setup
  secret or onboarding wizard is requested

#### Scenario 3.1C: Wrong provider challenge

- **GIVEN** configured mode is pending for one provider
- **WHEN** a caller requests another provider or status is unknown
- **THEN** authentication fails closed with a bounded unavailable/forbidden
  outcome and no configured identity data is disclosed

### Requirement 3.2: Exact authenticated identity owns the claim

The system **MUST** grant initial administrator authority only when the
cryptographically authenticated provider, provider authority, and stable
subject exactly match the configured binding.

#### Scenario 3.2A: Existing Keycloak realm user

- **GIVEN** a configured Keycloak issuer and exact subject identify an existing
  realm user
- **WHEN** that issuer validates a token carrying that exact `sub`
- **THEN** the platform creates or resolves one local user and external login,
  grants initial administrator authority, and completes onboarding

#### Scenario 3.2B: Same subject from a different issuer

- **GIVEN** a token has the configured subject but a different issuer
- **WHEN** it attempts the initial claim
- **THEN** the system denies it without creating, linking, granting, or
  completing anything

#### Scenario 3.2C: Verified ATProto DID

- **GIVEN** the configured selector is an ATProto DID
- **WHEN** the OAuth security gateway verifies a session for that exact DID
- **THEN** the system may materialize the configured local account and proceed
  through the shared claim transaction before issuing the platform session

#### Scenario 3.2D: Mutable or indirect identifier attack

- **GIVEN** a caller matches email, username, handle, display name, provider
  role, expected DID input, or login order but not the exact verified binding
- **WHEN** it authenticates
- **THEN** it receives no bootstrap write, external login, role, tenant
  membership, or completion

### Requirement 3.3: Portable configuration remains identity-free

ConfigurationManifest **MUST** remain non-secret, identity-free, export-safe,
and incapable of granting or selecting initial administrator authority.

#### Scenario 3.3A: Manifest and environment compose safely

- **GIVEN** a valid manifest supplies instance/tenant configuration and
  deployment-local authority supplies the configured administrator selector
- **WHEN** startup preparation succeeds
- **THEN** the pending binding references only value-free fingerprints while
  the manifest contains no identity selector, PII, provider binding, role
  grant, secret reference, or completion state

#### Scenario 3.3B: Identity smuggling

- **GIVEN** a manifest attempts to carry administrator identity or provider
  binding
- **WHEN** it is validated, applied, or exported
- **THEN** it is rejected or omitted by the existing closed portability
  contract and produces no pending authority

### Requirement 3.4: Completion is atomic and race-safe

The exact claim **MUST** be one serializable, idempotent state transition. A
failed or losing attempt **MUST NOT** leave partial identity, role, tenant,
setting, or bootstrap state.

#### Scenario 3.4A: Successful claim

- **GIVEN** one exact authenticated identity and a matching pending generation
- **WHEN** claim completion commits
- **THEN** exactly one local user, personal actor, authority-qualified external
  login, platform administrator grant, required tenant authority, completed
  bootstrap generation, and canonical settings exist

#### Scenario 3.4B: Concurrent exact callbacks

- **GIVEN** two exact callbacks subscribe to a deterministic transaction gate
  before either commits
- **WHEN** both attempt the same pending generation
- **THEN** one transaction performs the transition and the other converges on
  idempotent success for the same completed identity

#### Scenario 3.4C: Exact and nonmatching race

- **GIVEN** an exact callback races a different authenticated identity
- **WHEN** both reach the claim boundary
- **THEN** only the exact identity may complete; the other leaves zero
  authority and zero partial state

#### Scenario 3.4D: Transaction failure

- **GIVEN** persistence fails after an intermediate identity write but before
  completion
- **WHEN** the transaction rolls back
- **THEN** no user/login/actor/role/tenant/settings/bootstrap residue remains
  and setup authority remains unlocked

### Requirement 3.5: Pending recovery is explicit and completion is final

The system **SHALL** allow deliberate correction before completion and **MUST
NOT** transfer initial authority from configuration after completion.

#### Scenario 3.5A: Replica/configuration mismatch

- **GIVEN** a persisted pending generation and a replica presenting a different
  selector digest without a generation increment
- **WHEN** startup validation runs
- **THEN** that replica fails startup with a value-free reason code

#### Scenario 3.5B: Deliberate pending correction

- **GIVEN** the configured selector is wrong and no claim completed
- **WHEN** the operator increments the binding generation and supplies a valid
  replacement
- **THEN** the prior pending generation is superseded and the new generation
  becomes the only claimable binding

#### Scenario 3.5C: Configuration changes after completion

- **GIVEN** onboarding is complete
- **WHEN** bootstrap selector values are removed, retained, or changed
- **THEN** removal or the same selector is an idempotent steady state, while a
  different selector cannot grant, revoke, or transfer authority

#### Scenario 3.5D: Setup-secret lifecycle

- **GIVEN** a configured claim is still pending or its transaction fails
- **WHEN** setup authority is inspected
- **THEN** it remains recoverable; only successful committed completion locks
  and deletes generated setup-secret material

### Requirement 3.6: Observability is value-free

Status, health, logs, metrics, traces, errors, support evidence, and release
artifacts **MUST NOT** disclose subject, DID, email, issuer URL, identity
fingerprint, token claim, profile value, secret, or raw configuration.

#### Scenario 3.6A: Successful and rejected claims

- **GIVEN** matching, nonmatching, malformed, and concurrent attempts
- **WHEN** diagnostics are captured
- **THEN** they contain only bounded state, provider kind, generation,
  operation outcome, and stable reason codes

### Requirement 3.7: Setup Assistant remains offline-only

Setup Assistant **MAY** generate and validate `.env` and manifest artifacts,
but **MUST NOT** connect to, authenticate against, inspect, or mutate the
running instance.

#### Scenario 3.7A: Offline generation

- **GIVEN** an operator selects configured-administrator mode
- **WHEN** Setup Assistant generates deployment artifacts
- **THEN** it produces only local files and diagnostics and introduces no API
  client, access token, credential handle, or live control-plane dependency

### Requirement 3.8: Interactive setup remains an explicit mode

Deployments without a complete configured-administrator contract **SHALL**
either use explicit interactive mode or fail startup; they **MUST NOT**
silently infer automation or grant authority.

#### Scenario 3.8A: Explicit interactive mode

- **GIVEN** interactive mode is selected
- **WHEN** the instance is incomplete
- **THEN** the existing setup-secret and onboarding UI contract remains the
  only completion path

#### Scenario 3.8B: Partial configured mode

- **GIVEN** configured mode is selected but provider, subject, authority,
  profile, manifest, or required topology is incomplete
- **WHEN** startup validation runs
- **THEN** startup fails closed with value-free diagnostics and never falls
  back to first-login or anonymous setup

## 4. Non-Negotiable Constraints

1. Initial authority is exact provider + authority + subject/DID only.
2. Provider subject/DID, issuer, email, and fingerprints never enter
   ConfigurationManifest, generated catalogue output values, browser DTOs,
   logs, metrics, status detail, support evidence, or release prose.
3. Setup Assistant remains offline-only.
4. Browser-supplied headers, body fields, return URLs, expected DIDs, and
   handles are never bootstrap authority.
5. Keycloak token validation and ATProto OAuth verification happen before
   claim matching.
6. Email auto-match is forbidden for the initial administrator transition.
7. Local platform roles remain the authorization source; provider roles and
   client claims do not become database authority.
8. Tokens stay inside the BFF/server boundary.
9. Completion, roles, identity linkage, tenant state, settings, and bootstrap
   state commit atomically.
10. Setup-secret lock and cache/session side effects happen only after commit.
11. Pending state must remain healthy enough to authenticate.
12. Unknown or inconsistent status fails closed.
13. Repositories return entities, validators are manually instantiated, and
    Clean Architecture dependencies point inward.
14. Generated OpenAPI/client/catalogue/migration artifacts are never hand
    edited.
15. All five database provider migrations and snapshots are generated from the
    model.
16. Fixed sleeps, polling waits, mock-mirroring, source scraping, and
    framework-behavior tests are forbidden.
17. No compatibility aliases, dual identity-key readers, deprecated status
    members, or legacy fallback logic.
18. Shared-tree unrelated changes remain untouched and unstaged.

## 5. Architecture And Design Decisions

### 5.1 Offline configuration and runtime authority remain separate

- **Decision:** Event.Setup.Core adds a closed offline catalogue for bootstrap
  mode, provider, subject, optional profile fallback, and generation. Runtime
  reads the same documented keys through an Infrastructure provider. A
  convergence test prevents metadata drift. Setup Assistant receives no
  runtime reference.
- **Why:** The offline tool should help operators prepare artifacts without
  gaining credentials or control-plane authority.
- **Alternatives considered:** Setup Assistant live connection, startup HTTP
  callback, or manifest-owned identity; all rejected.
- **Consequences:** Configuration keys are duplicated only as a deliberately
  tested contract between offline generation and runtime configuration.
- **Affected:** Event.Setup.Core, Infrastructure, `.env.example`, generated
  environment catalogue, configuration docs, architecture tests.

### 5.2 ConfigurationManifest supplies configuration, not identity

- **Decision:** Configured mode requires successful manifest `Bootstrap`
  preparation for portable instance settings. Single-tenant legal/operator
  facts come from existing startup-owned `INSTANCE__OPERATORIDENTITY__*`
  configuration. Administrator identity remains deployment-local.
- **Why:** Manifest export/import must not reproduce a role grant or personal
  provider binding.
- **Alternatives considered:** raw subject/DID in manifest, a manifest secret
  reference, or a portable role assignment; rejected by portability and I-VSD.
- **Consequences:** No ConfigurationManifest wire/schema version bump is
  required. Validation gains a regression test that administrator keys cannot
  enter registered portable sections.
- **Affected:** manifest startup ordering/validation tests and operator docs;
  no manifest identity field.

### 5.3 Bootstrap becomes an explicit Domain state machine

- **Decision:** Replace binary mutation with explicit transitions for
  interactive pending, configured pending, superseded, and completed
  generations. Persist provider kind and fixed-length keyed/value-free
  fingerprints, generation, manifest/configuration digest, completion method,
  selected `DeploymentMode`, timestamps, and completed user.
- **Why:** Multi-replica convergence, deliberate correction, replay handling,
  auditability, and finality cannot be represented safely by `IsCompleted`
  alone.
- **Alternatives considered:** process-local configuration only, SystemSetting
  storage, a second shallow bootstrap table, or startup-time completion.
- **Consequences:** Phase 2 atomically cuts Domain, persistence schema, active
  server callers, and fixtures to the typed state; no Domain compatibility
  reader or dual column remains. Existing wire DTO names may remain temporary
  projections of typed state until their owning generated-client phase.
- **Affected:** Domain entity/enums, active Application/Infrastructure/API
  readers and writers, repository/configuration, generated
  migrations/snapshots, release fragment, and affected tests.

### 5.4 Provider account keys become authority-qualified

- **Decision:** Canonicalize the external account key before persistence:
  Keycloak uses normalized configured issuer plus `sub`; ATProto uses canonical
  DID; Google follows issuer plus subject. Keep the existing unique
  `(Provider, ProviderKey)` database index, but replace raw realm-scoped keys
  with authority-qualified keys.
- **Why:** A Keycloak `sub` is unique only inside one issuer/realm.
- **Alternatives considered:** adding a nullable authority column, trusting one
  realm forever, or dual old/new readers. The canonical-key replacement is
  smaller, keeps the unique index portable, and embraces greenfield breaking
  change freedom.
- **Consequences:** All provider-key constructors and repository callers
  migrate in one Application slice. Existing development data is not adapted;
  no compatibility fallback is planned.
- **Affected:** canonical identity extensions/value, sync/resolution/admin
  callers, managed provisioning, federation callers, tests.

### 5.5 One deep provider-neutral claim operation owns completion

- **Decision:** Extract the transactional core from interactive completion into
  an Application-owned operation invoked by both interactive completion and
  configured provider adapters. A new command claims the current configured
  generation from trusted server-derived identity; it accepts no browser
  identity selector.
- **Why:** Role/tenant/settings/bootstrap invariants need one transaction and
  one implementation. Controllers and authentication adapters should remain
  thin.
- **Alternatives considered:** duplicate Keycloak/ATProto handlers,
  controller-to-controller HTTP, middleware mutation, GET/status mutation, or
  a shallow repository facade.
- **Consequences:** Interactive and configured modes share completion
  semantics without sharing authority adapters. Validators remain manual.
- **Affected:** Application request/handler/service contracts, current
  completion handler, cache/audit boundaries, tests.

### 5.6 Keycloak claims through existing authenticated synchronization

- **Decision:** `UserController.SyncUser` continues deriving identity from the
  validated principal. In configured pending mode, synchronization dispatches
  the shared claim operation with exact issuer/subject-derived account key.
  Email fallback remains ordinary account behavior only and is bypassed for
  initial administration.
- **Why:** BFF already invokes SyncUser at the trusted post-login boundary.
- **Alternatives considered:** new bootstrap endpoint, Keycloak management API,
  realm-role mirroring, or email lookup.
- **Consequences:** No new write route, rate limiter, HAL relation, or
  browser-visible request shape is introduced.
- **Affected:** principal extensions, SyncUser command/handler/controller,
  current-user resolution, admin resolution, API tests.

### 5.7 ATProto claims only after cryptographic verification

- **Decision:** Immediately after the OAuth security gateway returns a verified
  session and before `account_not_linked`, the ATProto handler may invoke the
  shared claim operation only when configured mode selects the exact verified
  DID. Session persistence and token issuance retain their current ordering.
- **Why:** ATProto otherwise has a circular requirement: a link is required
  before the first platform session, but configured bootstrap is the authority
  to create that first link.
- **Alternatives considered:** expected DID input, handle matching, PDS URL
  matching, pre-created privileged fake user, or anonymous linking.
- **Consequences:** The exception remains narrow, one-time, and provider-
  verified. Required local email/profile fallback comes from server-only
  configuration.
- **Affected:** ATProto session handler, subject onboarding operation,
  Application tests, BFF flow tests.

### 5.8 BFF status is a closed routing state

- **Decision:** Replace boolean-only BFF decisions with a closed state:
  `InteractivePending`, `ConfiguredAdministratorPending`, `Completed`, or
  `Invalid`, plus required provider kind only. Pending configured mode permits
  exactly that provider's challenge and causes sign-in enrichment to sync,
  claim, refresh onboarding status, and load admin authority.
- **Why:** Authentication must remain reachable without exposing arbitrary
  provider choice or setup UI.
- **Alternatives considered:** setup-cookie bypass, direct UI page, fail-open
  unknown status, or global unready health.
- **Consequences:** Status DTO/OpenAPI/client add bounded fields. Subject,
  issuer, email, and fingerprints never leave the API process.
- **Affected:** status query/DTO, BFF status provider/middleware/auth endpoints,
  claims transformation, generated client, client startup routing, tests.

### 5.9 Runtime activation is last

- **Decision:** Internal contracts, state, claim logic, provider adapters,
  BFF/client consumers, and architecture ratchets land while runtime
  configuration remains disabled. The final phase registers the environment-
  backed provider, startup preparation, docs, and release fragment.
- **Why:** Every intermediate commit remains buildable and cannot expose a
  partially wired privilege path.
- **Alternatives considered:** activate keys in the first phase or maintain a
  long-lived feature flag.
- **Consequences:** There is no compatibility flag after activation; explicit
  `Interactive` and `ConfiguredAdministrator` are permanent product modes.
- **Affected:** Infrastructure registration/startup, API/Standalone startup,
  operator docs, release evidence.

## 6. Implementation Phases

### Phase 1: Offline Configuration Contract

- **Goal:** Define and validate the closed deployment-local configuration
  contract without adding runtime connectivity or activation.
- **Depends on:** approved plan and I-VSD; current Event.Setup.Core environment
  catalogue.
- **Relevant files:**
  - existing `src/Event.Setup.Core/Environment/CanonicalEnvironmentCatalogue.cs`
  - existing `src/Event.Setup.Core/Environment/CanonicalEnvironmentMetadata.cs`
  - existing `src/Event.Setup.Core/Dotenv/DotenvComposer.cs`
  - existing `eng/setup-assistant/generated/environment-catalogue.json`
  - generator-owned `docs/CONFIGURATION.md` environment catalogue block
  - existing `.env.example`
  - existing/new focused Event.Setup.Core environment tests
- **Phase-owned paths:**
  - `.env.example`
  - `src/Event.Setup.Core/Environment/CanonicalEnvironmentCatalogue.cs`
  - `src/Event.Setup.Core/Environment/CanonicalEnvironmentMetadata.cs`
  - `src/Event.Setup.Core/Dotenv/DotenvComposer.cs`
  - `eng/setup-assistant/generated/environment-catalogue.json`
  - `docs/CONFIGURATION.md`
  - `tests/Event.Setup.Core.Tests/Environment/EnvironmentCatalogueInvariantTests.cs`
  - `tests/Event.Setup.Core.Tests/Environment/DotenvContractTests.cs`
- **Related skills/rules:** ip-clean-room; Event.Setup.Core architecture;
  tests; offline Setup Assistant boundary
- **Acceptance criteria:**
  - closed keys represent mode, provider, subject, generation, and bounded
    profile fallback with correct sensitivity and restart metadata
  - generated dotenv never fabricates or logs administrator values
  - machine catalogue and generated configuration documentation travel with
    the source contract and converge through the same canonical generator
  - configured mode readiness requires the exact key matrix; interactive mode
    does not require it
  - the production composer executes catalogue validators, rejects known
    inactive keys, and reports invalid cross-key matrices without values
  - no live API/client dependency enters Setup projects
- **Phase-end verification:**
  - `dotnet build --configuration Release --verbosity quiet`
  - `dotnet test --project tests/Event.Setup.Core.Tests/Event.Setup.Core.Tests.csproj --configuration Release --verbosity quiet`
- **Phase-close commit outcome:** Define an offline, value-safe configured
  administrator bootstrap contract without enabling runtime behavior.
- **Rollback / failure handling:** Remove the offline key catalogue and
  composer-validation changes; runtime behavior remains unchanged.

### Phase 2: Atomic Typed Bootstrap Lifecycle Cutover

- **Goal:** Replace the binary marker atomically with one typed lifecycle
  authority across Domain, schema, active server callers, and fixtures.
- **Depends on:** Phase 1 key semantics.
- **Relevant files:**
  - existing `src/Explore.Domain/InstanceBootstrapState.cs`
  - new status, mode, and provider-kind Domain enums
  - active bootstrap writers/readers in Application, Infrastructure, and API
  - entity configuration plus generator-produced five-provider migration and
    snapshots
  - affected Domain/Application/Infrastructure/API/Persistence tests
  - migration change fragment `CHG-01M1ETX06HRETFBJTK6SCZGBZ6`
- **Phase-owned paths:**
  - `src/Explore.Domain/InstanceBootstrapState.cs`
  - `src/Explore.Domain/Enums/InstanceBootstrapStatus.cs`
  - `src/Explore.Domain/Enums/InstanceBootstrapMode.cs`
  - `src/Explore.Domain/Enums/InstanceBootstrapProviderKind.cs`
  - active `InstanceBootstrapState` writers/readers identified by LSP in
    `Explore.Application`, `Explore.Infrastructure`, and `Explore.API`
  - `src/Explore.Persistence/Configurations/Entities/InstanceBootstrapStateConfiguration.cs`
  - five provider model snapshots and restrictive generated
    `*AddConfiguredAdministratorBootstrapState*.cs` pathspecs
  - directly affected Domain, Application, Infrastructure, API, Persistence,
    Blazor, and architecture tests recorded before staging
  - `docs/releases/changes/CHG-01M1ETX06HRETFBJTK6SCZGBZ6.yaml`
- **Related skills/rules:** criticality-guardrail,
  clean-architecture-rules, dotnet-efcore-guidelines, Domain, Application,
  Infrastructure, API, EF migration, and test rules
- **Acceptance criteria:**
  - invalid transitions, generation regression, selector drift, duplicate
    completion, and post-completion transfer fail explicitly
  - provider kind, selected `DeploymentMode`, generation, fingerprints,
    timestamps, and completed local user are typed persisted state
  - fingerprints are fixed-length value data and raw external identifiers
    cannot enter the entity
  - exact replay of the completed generation is idempotent
  - Domain `IsCompleted` and `SelectedDeploymentMode` aliases are removed;
    active server decisions use `Status` and `DeploymentMode`
  - typed tests pass AssuranceAudit with no runtime-selected behavior dispatch
  - generated schema and active callers cut over in the same breaking commit
    with no dual reader
- **Phase-end verification:**
  - `dotnet build --configuration Release --verbosity quiet`
  - full affected Domain, Application, Infrastructure, API, Persistence,
    Blazor Client, Blazor Integration, and Architecture test projects
  - AssuranceAudit over the Phase 2 changed test set
- **Phase-close commit outcome:** Replace the binary bootstrap marker with one
  typed lifecycle, generated schema, and active-caller authority.
- **Rollback / failure handling:** Revert the complete typed lifecycle and
  generated schema commit before later claim phases consume it; never leave a
  mixed schema/runtime reader.

### Phase 3: Multi-Replica Locking And Convergence

- **Goal:** Add row locking and serializable convergence over the typed schema
  committed by Phase 2.
- **Depends on:** Phase 2 typed state and schema.
- **Relevant files:**
  - existing `InstanceBootstrapStateRepository`
  - real relational atomicity/concurrency tests
- **Phase-owned paths:**
  - `src/Explore.Persistence/Repositories/InstanceBootstrapStateRepository.cs`
  - `tests/Event.Persistence.IntegrationTests/Onboarding/InstanceBootstrapStatePersistenceTests.cs`
  - `tests/Event.Persistence.IntegrationTests/Onboarding/InstanceOnboardingConcurrencyTests.cs`
- **Related skills/rules:** dotnet-efcore-guidelines,
  criticality-guardrail, EF persistence rules, tests
- **Acceptance criteria:**
  - same-digest replica startup is idempotent; mismatched same-generation
    startup fails
  - deterministic real relational races converge without sleeps or polling
  - loser classification distinguishes exact replay, generation drift,
    different identity, and rollback without schema changes
- **Phase-end verification:**
  - `dotnet build --configuration Release --verbosity quiet`
  - `dotnet test --project tests/Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj --configuration Release --verbosity quiet`
- **Phase-close commit outcome:** Serialize claim/convergence behavior over the
  typed bootstrap schema.
- **Rollback / failure handling:** Revert repository locking/convergence logic
  and race tests together; Phase 2 schema remains authoritative.

### Phase 4: Provider-Neutral Claim Orchestrator

- **Goal:** Extract one deep Application transaction for interactive and
  configured completion and lock it with invariant-first tests.
- **Depends on:** Phase 3 persistence.
- **Relevant files:**
  - existing interactive completion command/handler
  - new configured binding provider contract and immutable model
  - new claim command/handler and completion operation
  - Application registration
  - focused Application tests
- **Phase-owned paths:**
  - `src/Explore.Application/Contracts/Services/IConfiguredAdministratorBootstrapProvider.cs`
  - `src/Explore.Application/Authentication/ProviderAccountKey.cs`
  - `src/Explore.Application/Models/ConfiguredAdministratorBootstrapBinding.cs`
  - `src/Explore.Application/Features/InstanceOnboarding/Requests/Commands/ClaimConfiguredInstanceAdministratorCommand.cs`
  - `src/Explore.Application/Features/InstanceOnboarding/Handlers/Commands/ClaimConfiguredInstanceAdministratorCommandHandler.cs`
  - `src/Explore.Application/Features/InstanceOnboarding/Services/InstanceOnboardingCompletionOperation.cs`
  - `src/Explore.Application/Features/InstanceOnboarding/Handlers/Commands/CompleteInstanceOnboardingCommandHandler.cs`
  - `src/Explore.Application/ApplicationServicesRegistration.cs`
  - `tests/Event.Application.UnitTests/Features/InstanceOnboarding/ConfiguredAdministratorClaimInvariantTests.cs`
  - `tests/Event.Application.UnitTests/Features/InstanceOnboarding/InstanceOnboardingCompletionOperationTests.cs`
- **Related skills/rules:** cqrs-mediatr-guidelines, auth-patterns,
  clean-architecture-rules, criticality-guardrail, Application rules, tests
- **Acceptance criteria:**
  - Red tests bind directly to Scenarios 3.2, 3.4, 3.5, and 3.6 before
    production logic
  - trusted provider identity and pending binding are re-read inside one
    serializable transaction
  - interactive completion delegates to the same operation without weakening
    setup-secret authority
  - configured claim has no browser identity input and never invokes email
    fallback
  - post-commit side effects remain post-commit and value-free
- **Phase-end verification:**
  - `dotnet build --configuration Release --verbosity quiet`
  - `dotnet test --project tests/Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet`
- **Phase-close commit outcome:** Centralize initial authority assignment in
  one provider-neutral, atomic Application operation.
- **Rollback / failure handling:** Runtime provider remains disabled; revert
  the internal orchestration without exposing partial behavior.

### Phase 5: Verified Provider Adapters And API Status

- **Goal:** Bind Keycloak and ATProto verified identity seams to the shared
  claim operation while keeping runtime configured mode disabled.
- **Depends on:** Phase 4 claim operation.
- **Relevant files:**
  - canonical principal/provider identity extensions
  - current-user and admin resolution
  - User sync controller/command/handler
  - ATProto verified session handler
  - onboarding status DTO/query
  - fail-closed disabled Infrastructure provider
  - API and Application integration tests
- **Phase-owned paths:**
  - `src/Explore.Application/Authentication/PlatformIdentityPrincipalExtensions.cs`
  - `src/Explore.Application/Authentication/CurrentUserResolutionExtensions.cs`
  - `src/Explore.Application/Features/Users/Requests/Commands/SyncUserCommand.cs`
  - `src/Explore.Application/Features/Users/Handlers/Commands/SyncUserCommandHandler.cs`
  - `src/Explore.Application/Features/Users/Handlers/Queries/ResolveCurrentUserIdByIdentityRequestHandler.cs`
  - `src/Explore.Application/Features/Authentication/Atproto/Handlers/Commands/BootstrapAtprotoSessionCommandHandler.cs`
  - `src/Explore.Application/Features/Authentication/Atproto/Services/AtprotoSubjectOnboardingOperation.cs`
  - `src/Explore.Application/Features/ManagedProviderProvisioning/Handlers/Commands/EnsureManagedProviderClientProvisionedCommandHandler.cs`
  - `src/Explore.Application/Features/Federation/Atproto/Services/AtprotoEventPublicationPlanner.cs`
  - `src/Explore.Application/Contracts/Persistence/IUserExternalLoginRepository.cs`
  - `src/Explore.Infrastructure/Identity/AdminClaimsTransformation.cs`
  - `src/Explore.Infrastructure/Identity/AdminContext.cs`
  - `src/Explore.Infrastructure/Services/DisabledConfiguredAdministratorBootstrapProvider.cs`
  - `src/Explore.Infrastructure/InfrastructureServicesRegistration.cs`
  - `src/Explore.API/Controllers/UserController.cs`
  - `src/Explore.Application/DTOs/Onboarding/InstanceOnboardingStatusDto.cs`
  - `src/Explore.Application/Features/InstanceOnboarding/Handlers/Queries/GetInstanceOnboardingStatusQueryHandler.cs`
  - `tests/Event.API.IntegrationTests/Features/ConfiguredAdministratorBootstrapTests.cs`
  - `tests/Event.API.IntegrationTests/Features/UserControllerTests.cs`
  - `tests/Event.API.IntegrationTests/Features/UserExternalLoginIntegrationTests.cs`
  - `docs/releases/changes/CHG-01M1ETXMS84KS8ASDW4GR22Q3J.yaml`
- **Related skills/rules:** auth-patterns, cqrs-mediatr-guidelines,
  criticality-guardrail, API controller/application/auth trust-boundary rules,
  privacy/PII, tests
- **Acceptance criteria:**
  - Keycloak claim requires exact validated issuer plus subject
  - ATProto exception occurs only after verified DID and before exact-link
    rejection
  - all provider-key constructors use one authority-qualified canonicalizer
  - same email/username/handle/role and wrong issuer/DID attempts produce no
    bootstrap writes
  - status exposes bounded state and required provider only
  - no new public write route, identity request body, HAL affordance, or
    provider management call is added
  - provider identity, Security, OpenAPI, and operator impact is recorded in
    `CHG-01M1ETXMS84KS8ASDW4GR22Q3J`
- **Phase-end verification:**
  - `dotnet build --configuration Release --verbosity quiet`
  - `dotnet test --project tests/Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet`
- **Phase-close commit outcome:** Replace realm-ambiguous provider keys with
  authority-qualified identities and connect verified providers to the dormant
  claim path.
- **Rollback / failure handling:** Disabled provider prevents activation;
  revert provider adapters together to preserve canonical identity semantics.

### Phase 6: BFF Pending Authentication Routing

- **Goal:** Route configured pending instances through exactly one provider
  and refresh authority after successful claim.
- **Depends on:** Phase 5 status and provider adapters.
- **Relevant files:**
  - BFF status contract/provider
  - startup redirect middleware
  - auth endpoints and return URL handling
  - admin claims transformation/session refresh
  - BFF integration tests
- **Phase-owned paths:**
  - `src/Explore.Blazor/Services/BffOnboardingStatusProvider.cs`
  - `src/Explore.Blazor/Extensions/MiddlewareExtensions.cs`
  - `src/Explore.Blazor/Extensions/BffAuthEndpoints.cs`
  - `src/Explore.Blazor/Services/BffAdminClaimsTransformation.cs`
  - `src/Explore.Blazor/Services/Auth/BffSessionRefreshService.cs`
  - `tests/Explore.Blazor.IntegrationTests/Endpoints/ConfiguredAdministratorRoutingTests.cs`
  - `tests/Explore.Blazor.IntegrationTests/Endpoints/AtprotoAuthenticationFlowTests.cs`
  - `tests/Explore.Blazor.IntegrationTests/Services/BffAdminClaimsTransformationTests.cs`
  - `tests/Explore.Blazor.IntegrationTests/Services/BffSessionRefreshServiceTests.cs`
- **Related skills/rules:** blazor-bff-patterns, auth-patterns,
  criticality-guardrail, Blazor server/auth trust-boundary rules, tests
- **Acceptance criteria:**
  - root/setup/login/challenge routing matches Scenario 3.1 for every closed
    state
  - only the configured provider is reachable while pending
  - unknown/invalid status fails closed without redirect loops
  - sign-in synchronization claims, refreshes status, then loads persisted
    admin authority
  - cookies, antiforgery, SameSite, return URL, and token-forwarding invariants
    remain intact
- **Phase-end verification:**
  - `dotnet build --configuration Release --verbosity quiet`
  - `dotnet test --project tests/Explore.Blazor.IntegrationTests/Explore.Blazor.IntegrationTests.csproj --configuration Release --verbosity quiet`
- **Phase-close commit outcome:** Make pre-completion authentication
  provider-specific, fail-closed, and invisible to the onboarding UI.
- **Rollback / failure handling:** Runtime mode remains disabled; revert BFF
  closed-state consumers without changing API identity behavior.

### Phase 7: Generated Client And Startup Route Consumption

- **Goal:** Regenerate the additive status contract and consume the closed
  routing state in client-capable startup services without rendering new UI.
- **Depends on:** Phase 6 BFF contract.
- **Relevant files:**
  - canonical OpenAPI and API inventory
  - generated Blazor client
  - onboarding service/status mapping
  - startup routing/HomeStart/StartupGate behavior
  - API changelog and client tests
- **Phase-owned paths:**
  - `schemas/openapi_islamu-event.json`
  - `docs/API_CONTRACT_INVENTORY.md`
  - `docs/API_CHANGELOG.md`
  - `src/Explore.Blazor.Client/Clients/EventApiClient.g.cs`
  - `src/Explore.Blazor.Client/Services/InstanceOnboardingService.cs`
  - `src/Explore.Blazor.Client/Services/StartupRoutingService.cs`
  - `src/Explore.Blazor.Client/Pages/HomeStart.razor`
  - `src/Explore.Blazor.Client/Pages/Onboarding/StartupGate.razor`
  - `tests/Explore.Blazor.Client.Tests/Services/InstanceOnboardingServiceTests.cs`
  - `tests/Explore.Blazor.Client.Tests/Services/StartupRoutingServiceTests.cs`
  - `tests/Explore.Blazor.Client.Tests/Pages/Onboarding/StartupGateTests.cs`
- **Related skills/rules:** blazor-ui-conventions, auth-patterns, generated
  client ownership, Blazor client rules, tests
- **Acceptance criteria:**
  - OpenAPI/client generation is canonical and no generated file is hand edited
  - added status fields carry only state/provider and no identity values
  - client startup never renders the onboarding wizard in configured pending
    mode
  - interactive mode and completed routing remain explicit product behavior,
    not compatibility fallback
  - no component gates authority by local roles/claims
- **Phase-end verification:**
  - `dotnet build --configuration Release --verbosity quiet`
  - `dotnet test --project tests/Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --verbosity quiet`
- **Phase-close commit outcome:** Keep generated and client startup contracts
  aligned with the server's closed bootstrap state.
- **Rollback / failure handling:** Regenerate from the reverted API contract;
  never patch the generated client manually.

### Phase 8: Environment-Backed Runtime Preparation

- **Goal:** Implement the server-only configuration provider, pending
  generation preparation, recovery, and value-free diagnostics while DI still
  resolves the disabled provider.
- **Depends on:** Phases 1–7.
- **Relevant files:** new Infrastructure provider/startup runner and focused
  Infrastructure tests.
- **Phase-owned paths:**
  - `src/Explore.Infrastructure/Services/ConfiguredAdministratorBootstrapProvider.cs`
  - `src/Explore.Infrastructure/Services/ConfiguredAdministratorBootstrapStartupRunner.cs`
  - `tests/Explore.Infrastructure.Tests/Infrastructure/ConfiguredAdministratorBootstrapProviderTests.cs`
- **Related skills/rules:** criticality-guardrail, auth-patterns,
  secrets/privacy rules, Infrastructure tests
- **Acceptance criteria:**
  - exact offline catalogue semantics are parsed and validated server-side
  - same-generation same-digest startup is idempotent; drift fails closed
  - pending correction requires a higher generation; completed state is final
  - valid pending remains authentication-capable and diagnostics are value-free
  - no composition root registers this provider yet
- **Phase-end verification:**
  - `dotnet build --configuration Release --verbosity quiet`
  - `dotnet test --project tests/Explore.Infrastructure.Tests/Explore.Infrastructure.Tests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/*/*[Category!=Runtime]" --minimum-expected-tests 1`
- **Phase-close commit outcome:** Prepare the environment-backed bootstrap
  authority and recovery logic without activating it.
- **Rollback / failure handling:** Revert the unregistered provider and tests;
  the disabled runtime provider remains authoritative.

### Phase 9: Split And Standalone Startup Composition

- **Goal:** Wire preparation after migrations/manifest completion in both API
  Split and Standalone composition while the disabled provider still makes the
  new call a no-op.
- **Depends on:** Phase 8.
- **Relevant files:** API startup extension, Standalone composition, and
  Standalone integration tests.
- **Phase-owned paths:**
  - `src/Explore.API/Hosting/ApiHostStartupExtensions.cs`
  - `src/Event.Standalone/Program.cs`
  - `tests/Event.Standalone.IntegrationTests/ConfiguredAdministratorBootstrapStartupTests.cs`
- **Related skills/rules:** clean-architecture-rules, auth-patterns,
  Standalone topology contract, tests
- **Acceptance criteria:**
  - Split ordering is migration dependency, manifest bootstrap, binding
    preparation, then HTTP
  - Standalone ordering is migration/seed, manifest bootstrap, binding
    preparation, then HTTP
  - preparation failure blocks startup; disabled preparation is a no-op
  - token, cookie, API, and tenant trust boundaries remain identical in both
    topologies
- **Phase-end verification:**
  - `dotnet build --configuration Release --verbosity quiet`
  - `dotnet test --project tests/Event.Standalone.IntegrationTests/Event.Standalone.IntegrationTests.csproj --configuration Release --verbosity quiet`
- **Phase-close commit outcome:** Give Split and Standalone one dormant,
  verified bootstrap preparation sequence.
- **Rollback / failure handling:** Revert composition calls; migrations and
  manifest behavior remain unchanged.

### Phase 10: Runtime Activation, Architecture, Operations, And Release Evidence

- **Goal:** Replace the disabled registration with environment-backed
  authority only after all consumers are green, then close architecture,
  operator, privacy, and release obligations.
- **Depends on:** Phases 1–9, current I-VSD, Tier 1 evidence, and no unresolved
  shared-tree path collision.
- **Relevant files:** Infrastructure registration, architecture ratchets,
  operator/configuration/security/backup documentation, DBML, and activation
  change fragment.
- **Phase-owned paths:**
  - `src/Explore.Infrastructure/InfrastructureServicesRegistration.cs`
  - `tests/Event.Architecture.Tests/ConfiguredAdministratorBootstrapArchitectureTests.cs`
  - `docs/CONFIGURATION.md`
  - `docs/CONFIGURATION_MANIFEST.md`
  - `docs/SECRETS.md`
  - `docs/SELF_HOSTING.md`
  - `docs/TROUBLESHOOTING.md`
  - `docs/OPERATIONS.md`
  - `docs/BACKUP_RESTORE_UPGRADE.md`
  - `schemas/islamu-event.md`
  - `docs/releases/changes/CHG-01M1EQWDAHHXQ3AD29B4Y0645B.yaml`
- **Related skills/rules:** criticality-guardrail, ip-clean-room,
  auth-patterns, error/privacy rules, architecture tests, release governance
- **Acceptance criteria:**
  - environment-backed provider is the only configured-mode activation
    authority
  - interactive/default behavior remains explicit and no first-login fallback
    exists
  - architecture ratchets prove no runtime Setup dependency, no outward
    Application dependency, and generator ownership
  - operator docs cover valid matrices, first sign-in, correction, restart,
    completion, selector removal, backup/restore, and break-glass boundaries
  - activation fragment records Security, Configuration, OpenAPI, and Operator
    impacts without identity values
  - zero-PII evidence and anonymized weighted Tier 1 MAD review are captured
    under `.omo/evidence/20260901-headless-instance-onboarding/`
- **Phase-end verification:**
  - `dotnet build --configuration Release --verbosity quiet`
  - `dotnet test --project tests/Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet`
- **Phase-close commit outcome:** Enable recoverable, exact-identity headless
  onboarding with complete architecture, operator, and release evidence.
- **Rollback / failure handling:** Keep disabled registration until every gate
  passes. After activation, forward-fix attributable defects; an uncompleted
  instance may return to explicit interactive mode before a claim completes.

## 7. Testing Strategy

### 7.1 Invariant anchors

| Phase | Selected project | Primary protection |
|---|---|---|
| 1 | `Event.Setup.Core.Tests` | Closed key matrix, sensitivity, no fabricated values, offline-only dependencies |
| 2 | `Event.Domain.UnitTests` | Bootstrap state machine and finality |
| 3 | `Event.Persistence.IntegrationTests` | Real relational atomicity, concurrency, multi-replica convergence, provider parity |
| 4 | `Event.Application.UnitTests` | Exact claim contract, shared completion transaction, failure mapping |
| 5 | `Event.API.IntegrationTests` | Verified Keycloak/ATProto identity, no indirect matching, bounded status |
| 6 | `Explore.Blazor.IntegrationTests` | BFF routing, cookies, token boundary, refresh ordering |
| 7 | `Explore.Blazor.Client.Tests` | Generated status mapping and no-wizard startup routing |
| 8 | `Explore.Infrastructure.Tests` | Environment authority, generation recovery, and value-free diagnostics |
| 9 | `Event.Standalone.IntegrationTests` | Split/Standalone preparation ordering and topology parity |
| 10 | `Event.Architecture.Tests` | Clean Architecture, generated ownership, no Setup runtime dependency, contract convergence |

### 7.2 High-leverage adversarial scenarios

- exact issuer/subject versus identical subject from another authority
- verified DID versus expected/submitted DID and mutable handle
- concurrent exact callbacks and exact-versus-attacker race
- rollback after identity materialization but before completion
- same-generation replica configuration mismatch
- post-completion selector drift
- verified-email/username/handle/provider-role takeover
- unknown BFF status and wrong-provider challenge
- identity values injected into logs, exceptions, status, support evidence, and
  manifest export

Tests subscribe to exact barriers/events before triggering concurrency and use
bounded timeouts. Fixed sleeps and polling are forbidden. Internal
repositories/handlers are not mocked; real domain and relational seams are
used. External OAuth/PDS verification may use behavior-preserving gateway
fakes because it is an external boundary.

### 7.3 Phase verification lane

Each phase runs one Release build and exactly one selected non-browser test
project after all phase tasks. No application, browser, Docker Compose,
Aspire, Playwright, or live identity provider starts. A broad shared-tree
failure may be classified as unrelated only with exact external-path evidence
and a green phase-owned lane.

## 8. Documentation, Configuration, And Operations Impact

### Configuration

Add closed keys through the Event.Setup.Core catalogue and generated
`.env.example`:

- `INSTANCE_BOOTSTRAP_MODE=Interactive|ConfiguredAdministrator`
- `INSTANCE_BOOTSTRAP_ADMIN_PROVIDER=keycloak|atproto`
  - `INSTANCE_BOOTSTRAP_ADMIN_SUBJECT=did-or-provider-subject`
  - `INSTANCE_BOOTSTRAP_BINDING_GENERATION=1`
  - `INSTANCE_BOOTSTRAP_ADMIN_EMAIL=admin-profile-email`
- bounded optional first/last name fallback only if provider claims cannot
  populate required local profile fields

Keycloak authority is derived from the existing trusted Keycloak configuration
and compared to authenticated `iss`; it is not duplicated as an untrusted
browser value. ATProto authority is the verified DID namespace.

### Documentation

Update exact operator contracts named in Phase 10. ConfigurationManifest docs
must explicitly state that it may be required for configured startup but still
never owns administrator identity. API changelog documents additive state and
provider fields. Backup/restore docs state that completed authority is database
state and selector configuration cannot replay or transfer it.

### Operations

Emit bounded state/reason codes, provider kind, generation, and outcome only.
Pending is healthy; invalid configuration is a startup blocker. Setup secret
locks only after commit. Removing completed selector values is recommended.
Recovery before completion uses an explicit generation increment. Recovery
after completion uses normal administrator governance, backup restore, or a
separately approved one-shot database/operator-authority procedure—never an
HTTP or Setup Assistant backdoor.

### 8.1 Release, Changelog, And Phase Commit Strategy

- **Release tier:** Tier 2 — security, configuration, migration, OpenAPI, and
  operator impact
- **Migration change fragment:**
  `docs/releases/changes/CHG-01M1ETX06HRETFBJTK6SCZGBZ6.yaml`, owned by
  Phase 2 with its breaking typed-lifecycle/schema commit
- **Provider identity change fragment:**
  `docs/releases/changes/CHG-01M1ETXMS84KS8ASDW4GR22Q3J.yaml`, owned by
  Phase 5 with its breaking authority-qualified identity commit
- **Runtime activation change fragment:**
  `docs/releases/changes/CHG-01M1EQWDAHHXQ3AD29B4Y0645B.yaml`, owned by
  Phase 10
- **Public scope:** `onboarding`
- **Breaking classification:** Phase 2 replaces bootstrap persistence and
  Phase 5 replaces canonical provider account keys. Each carries its own
  `BREAKING CHANGE:` and `Change-Id`. Other pre-activation phases use
  `Changelog: skip` with exact reasons.
- **Activation:** Phase 10 only; its fragment and docs ship in the owning
  commit
- **No final catch-all:** every phase verifies and commits immediately using
  the exact contract in `headless-instance-onboarding-tasks.md`

## 9. Islamic Value-Sensitive Design & Moral Boundaries

- **Report:**
  [i-vsd-headless-instance-onboarding.md](../../../islamic-value-sensitive-design/i-vsd-headless-instance-onboarding.md)
- **Reviewed input revision:**
  `sha256:5c67de2a4b210f6d57239a6eb7f4c7ae38cf6bf4a0b9321e32c77410065a9ca9`
- **Status / disposition:** current and plan-aligned

| I-VSD ID | Finding / mitigation status | Scenario and task mapping | Disposition |
|---|---|---|---|
| `IVSD-F001` / `IVSD-M001` | Accepted | Scenarios 3.2A–3.2D; Tasks 4.1, 4.2, 5.1, 5.2 | Implement exact authenticated authority |
| `IVSD-F002` / `IVSD-M002` | Accepted | Scenarios 3.3A–3.3B; Tasks 1.1, 1.2, 8.1 | Keep manifest identity-free |
| `IVSD-F003` / `IVSD-M003` | Accepted | Scenarios 3.4A–3.5D; Tasks 2.1, 3.1, 3.2, 4.2, 8.2 | Pending, atomic, recoverable, final |
| `IVSD-F004` / `IVSD-M004` | Accepted | Scenario 3.6A; Tasks 1.2, 4.1, 5.1, 8.1 | Value-free observability |
| `IVSD-F005` / `IVSD-M005` | Accepted | Scenario 3.7A; Tasks 1.1 and 10.1 | Preserve offline-only Setup boundary |

No scholarly escalation is required for the approved technical scope. The
report becomes stale if identity enters the manifest, Setup Assistant gains
runtime connectivity, exact matching weakens, completion moves before proof,
post-completion transfer becomes automatic, or telemetry disclosure expands.

## 10. Security, Authorization, Privacy, And Abuse Considerations

- **Authentication:** Keycloak token and ATProto OAuth/PDS validation precede
  claim matching.
- **Authorization:** local `platform.admin` role remains authoritative.
  Provider roles, browser claims, and HAL are not mutation authority.
- **Identity:** exact authority-qualified provider key; no email or mutable
  profile matching for bootstrap.
- **BFF:** tokens stay server-side; cookies, SameSite, antiforgery, return URL,
  and YARP forwarding remain unchanged.
- **Tenant isolation:** external login lookup remains global only for the
  documented authentication bypass. Tenant membership and role grants are
  explicit and transactional.
- **Replay/idempotency:** generation and completed identity are re-read under
  lock/transaction. Same exact completion converges; different identity fails.
- **Abuse:** failed attempts create no durable attacker-controlled rows and use
  bounded diagnostics/rate behavior already owned by authentication.
- **Privacy:** subject, DID, issuer, email, and fingerprints are sensitive.
  They remain server-only and are excluded from logs/status/export/evidence.
- **Secrets:** no new password or provider credential. Identity selectors use
  the selected server configuration/secret authority and never fall back.
- **Setup authority:** remains available pending and locks post-commit.
- **Auditability:** durable generation/completion state plus bounded bootstrap
  audit; no parallel generic audit subsystem.

## 11. Multi-Tenancy, Federation, Localization, Accessibility, And Product

- **Multi-tenancy — Applicable:** initial platform admin is system-level;
  single-tenant default membership and multi-tenant control-plane access must
  remain exact and transactional.
- **Federation — Applicable:** ATProto DID verification and publication callers
  must migrate to the authority-qualified provider key.
- **Localization — Not applicable:** no new prose UI is rendered. Operator
  documentation remains English under current repository convention.
- **Accessibility — Not applicable:** configured mode suppresses UI rather
  than adding forms/dialogs. Existing interactive setup remains unchanged.
- **Product — Applicable:** explicit modes avoid surprising self-hosters;
  pending status and recovery are documented.
- **HAL — Not applicable to claim authority:** no new action affordance or
  client-side authorization decision.

## 12. Observability And Operations

- Structured state: `interactive_pending`,
  `configured_administrator_pending`, `completed`, `invalid`
- Bounded outcomes: `prepared`, `matched`, `nonmatching_identity`,
  `authority_mismatch`, `generation_mismatch`, `concurrent_completed`,
  `transaction_failed`, `completed`
- Metrics: attempt count by provider/outcome; current bootstrap-state gauge;
  startup configuration failure count
- Health: pending is healthy; invalid is unhealthy; no identity values
- Logs/traces: operation/binding UUID, provider kind, generation, stable reason
  code only
- Post-commit: setup-secret lock, admin/onboarding cache invalidation,
  deployment/JWT authority refresh, BFF session refresh
- Recovery: correct pending configuration with generation increment; never
  transfer completed authority from environment

## 13. Migration And Compatibility Plan

The project is pre-release and no external compatibility is preserved.

1. Update the Domain/entity configuration.
2. Generate `AddConfiguredAdministratorBootstrapState` for PostgreSQL, SQLite,
   SQL Server, MariaDB, and MySQL using repository EF tooling.
3. Inspect generated model/SQL and reversible `Down` output; never edit it.
4. Backfill legacy rows deterministically into `Completed` or
   `InteractivePending` based on existing completion state, then remove the
   obsolete binary representation in the same generated development
   migration where provider tooling supports it.
5. Replace all status and provider-key callers directly; add no aliases or
   dual readers.
6. Deploy migrations before runtime activation. Runtime registration remains
   disabled until Phase 10.
7. Backup/restore preserves completed state and cannot re-run configured claim.

If any migration is already applied or merged before correction, generate a
new corrective migration; never rewrite history.

## 14. Risk Register

| Risk | Likelihood | Impact | Mitigation | Detection signal | Owner/task |
|---|---:|---:|---|---|---|
| Wrong account receives initial authority | Low | Critical | Exact provider+authority+subject match; no indirect matching | Nonmatching claim tests and audit code | 4.1, 4.2, 5.1 |
| Keycloak realm replacement collides on subject | Medium | Critical | Authority-qualified provider account key | Wrong-issuer test | 4.1, 5.1 |
| ATProto bootstrap bypasses verification | Low | Critical | Branch only after verified gateway result | Expected-DID/handle adversarial tests | 5.2 |
| Concurrent callbacks create split authority | Medium | Critical | Lock plus serializable transaction and idempotent reread | Real relational race | 3.2, 4.2 |
| Selector typo locks the instance | Medium | High | Pending until proof; generation correction; setup authority retained | Pending recovery test | 2.1, 8.2 |
| Different replicas use different selectors | Medium | High | Persisted digest/generation startup convergence | Replica mismatch startup failure | 3.1, 8.2 |
| Identity leaks into logs/status/export | Medium | High | Bounded models and log-capture scans | Zero-PII evidence | 1.2, 5.1, 8.1 |
| Manifest becomes privilege authority | Low | Critical | Closed portability exclusion tests | Manifest smuggling test | 1.1, 8.1 |
| BFF unknown status fails open | Medium | High | Closed state and bounded unavailable result | BFF unknown-state tests | 6.1 |
| Shared dirty tree contaminates commits | High | High | Exact phase paths, path-limited commits, post-commit file check | Git inspection | Every phase commit |
| Generated artifact drift | Medium | High | Generator-owned regeneration and architecture ratchets | Architecture/client tests | 3.2, 7.1, 8.3 |

## 15. Success Metrics And Definition Of Done

- Valid configured deployments render no onboarding UI.
- Exact Keycloak issuer/subject and exact verified ATProto DID both complete
  onboarding through the same Application operation.
- At least one deterministic real relational concurrent exact-claim test and
  one exact-versus-nonmatching race pass.
- Wrong issuer, subject, DID, email, username, handle, role, and provider
  attempts produce zero writes.
- All five provider models/migrations agree.
- Status, logs, metrics, health, errors, manifest export, and support evidence
  contain zero configured identity values.
- Setup secret locks only after committed completion.
- Configuration changes after completion cannot transfer authority.
- Setup Assistant has no runtime project/API dependency.
- OpenAPI, API inventory, generated client, environment catalogue, docs, and
  change fragment match shipped behavior.
- Each phase has one green Release build, its bounded affected test set, and an
  exact verified phase-owned commit. Phase 2 is intentionally cross-layer and
  therefore verifies every affected project plus AssuranceAudit.
- Final Tier 1 MAD review passes with anonymized weighted approval and evidence
  is recorded.
- Plan, tasks, and context remain synchronized.

## 16. Implementation Agent Contract — KEEP DEV DOCS CURRENT

1. At first implementation start or cold resume, read task-owned context and
   the current task first, then retrieve only the plan heading needed for the
   current phase or changed decision.
2. Keep a path + heading/symbol + revision ledger. Do not reread unchanged
   artifacts in one uninterrupted session.
3. Start from the highest-priority unchecked task unless the user overrides.
4. Treat `tasks.md` as the hot execution ledger; check substantial tasks
   immediately and small tasks no later than phase end.
5. Keep implementation, phase verification, and phase commit checkboxes
   separate.
6. Update context after a phase, decision, blocker, validation failure,
   material discovery, or handoff.
7. Update this plan only when scope, architecture, phase order, acceptance,
   risk, or validation strategy changes.
8. Never edit generated migrations, snapshots, OpenAPI, generated client, API
   inventory, or generated environment catalogue by hand.
9. Run no build/tests after individual tasks. At phase end run one Release
   build and the one selected project test exactly once.
10. Phase-attributable failure blocks commit. Proven unrelated shared-tree
    failure must name exact external path/ownership evidence and leave it
    untouched.
11. Reconcile phase-owned paths against the dirty tree and index before every
    commit.
12. Use the approved commit contract directly when truthful. Load
    `conventional-commit` only for permitted material divergence, then record a
    complete replacement packet before committing.
13. Stage and commit exact phase-owned paths only; restrictive generated
    migration pathspecs may match only the named migration.
14. Verify the resulting commit file list and record its hash before completing
    the phase.
15. Keep Setup Assistant offline-only and ConfigurationManifest identity-free
    even if a shorter implementation path appears.
16. Preserve exact identity, transaction, privacy, and fail-closed invariants;
    never add compatibility fallbacks.
17. Before pause, compaction, transfer, or PR creation, reconcile tasks and add
    a dated context handoff.
18. Do not report completion when repository reality, verification evidence,
    commit contents, and task ledger disagree.

Every implementation summary teaches:

- what changed and why;
- Domain state transitions and provider authority;
- CQRS/Application orchestration and transaction flow;
- Keycloak/OIDC and ATProto verification seams;
- BFF routing and token/cookie boundaries;
- persistence/migration/provider behavior;
- configuration/manifest/Setup Assistant separation;
- privacy, recovery, observability, and release evidence;
- exact verification and remaining work.

## 17. Progress Reporting Contract

After each implementation slice:

```text
Implemented: developer teaching summary
Verified: exact command/evidence
Remaining: incomplete or deferred work
Next: recommended next slice
Docs updated: tasks reconciled; context/plan updated or unchanged with reason
```

## 18. Potential Risks & Unknowns

The hardest implementation boundary is ATProto first claim: cryptographic
verification currently precedes a mandatory existing-link check, while local
session/token issuance follows a serializable subject-onboarding transaction.
The new branch must create the configured identity only from the verified DID,
compose with that transaction without issuing a token early, and converge
under concurrent callbacks. The second-largest risk is shared-tree ownership:
current unrelated Setup, generated-client, persistence, migration, and
OpenAPI changes can make a correct phase unsafe to stage unless every path and
hunk is inspected.
