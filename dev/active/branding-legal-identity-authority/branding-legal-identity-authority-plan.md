<!-- ABOUTME: Canonical implementation plan for tenant directory-operator identity and role-correct legal attribution. -->
<!-- ABOUTME: Separates cosmetic branding from tenant, instance, and organizer authority across public and paid flows. -->

# Branding And Legal-Identity Authority — Implementation Plan

Last Updated: 2026-08-28 Europe/Brussels

## 0. Planning Metadata

- **Original request:** Resolve instance-versus-tenant branding semantics, then
  fully implement the enterprise-grade legal-identity model without backward
  compatibility.
- **Task directory:**
  `dev/active/branding-legal-identity-authority/`
- **Planning status:** In implementation; the user approved the full
  legal-identity scope on 2026-08-28.
- **Change classification:** Behavioral Delta with breaking API, configuration,
  persistence, public-rendering, and operator-readiness changes.
- **Primary intent:** `legal-identity-authority-change`, the Tier 0
  cross-cutting contract that owns the complete approved blast radius.
- **Related intents:** `external-infrastructure-bootstrap` for instance
  operator startup governance and `registration-data-collection` for immutable
  paid acceptance and multi-party disclosure.
- **Supporting intents:** `add-cqrs-handler`, `add-get-endpoint`,
  `add-write-endpoint`, `openapi-contract-change`, `add-hal-link`,
  `blazor-component-affordance`, and `add-ef-migration`.
- **Criticality:** Tier 1 Security for tenancy, lifecycle, and migration
  boundaries; Tier 0 Sovereign where paid checkout and immutable acceptance
  evidence change.
- **Primary layers:** Domain, Application, Persistence, API, generated client,
  Blazor Client, startup configuration, tests, operations, and documentation.
- **Complexity:** XL. The change establishes two new authority contracts,
  touches tenant activation and paid checkout, intentionally breaks existing
  wire/configuration contracts, and requires generated provider migrations and
  generated-client refresh.
- **Relevant skills:** `implementation-plan`, `criticality-guardrail`,
  `clean-architecture-rules`, `dotnet-efcore-guidelines`,
  `cqrs-mediatr-guidelines`, `blazor-ui-conventions`, `accessibility`,
  `footer-management`, `error-tracking`, `ip-clean-room`, `i-vsd`.
- **Relevant rules:** `domain`, `application-layer`, `efcore-persistence`,
  `efcore-migrations`, `api-controllers`, `payments-commerce`,
  `blazor-client`, and `tests`.
- **I-VSD document:**
  [i-vsd-branding-legal-identity-authority.md](../../../islamic-value-sensitive-design/i-vsd-branding-legal-identity-authority.md)
- **I-VSD reviewed input revision:**
  `sha256:2364e821f8455789cc00fe1c5f6c134c07b57e1db861a1ac6aaea607db2bfcb5`
- **I-VSD status / disposition:** Current / plan-aligned.
- **CTO review:** Not reviewed.
- **User approval:** Approved for the full legal-identity workstream described
  here, including intentional breaking changes.
- **Grill-Me intake:** The user selected the recommended full scope:
  first-class tenant directory-operator identity, distinct instance operator
  identity, preserved organizer merchant authority, readiness gates, and no
  compatibility layer.

## 1. Executive Summary

ISLAMU Event will stop treating cosmetic branding as proof of legal or
operational authority. Every tenant will own a first-class directory-operator
identity document. Every deployment will expose a distinct startup-governed
instance operator identity. Paid checkout will continue to preserve the
organizer merchant as the commercial counterparty while recording structured,
immutable tenant and instance disclosures in buyer acceptance evidence.

Tenant creation will atomically create branding and directory-operator
documents. A tenant cannot become active until the accountable operator
identity is complete. Public surfaces will show tenant and instance roles
separately. Paid capability will fail closed when any required tenant,
instance, merchant, or policy disclosure is incomplete. Missing identity will
never silently fall back to the instance brand, cosmetic tenant brand, or a
literal product name.

The implementation intentionally removes obsolete scalar branding semantics,
the prose-only paid-event directory disclaimer, and old startup configuration
keys. There will be no dual read, dual write, deprecated property, compatibility
alias, or transitional API contract.

### Explicit Non-Goals

- Determining jurisdiction-specific legal wording or legal-controller status.
- Treating the platform as organizer, merchant of record, or recipient of
  organizer proceeds.
- Requiring cosmetic instance co-branding where white-label policy allows it.
- Introducing a generic runtime schema or custom-property model.
- Persisting private credentials, payment secrets, or raw legal documents.
- Reviving the stale `tenant-onboarding-enterprise` workstream.

## 2. Source-Grounded Current State Report

### 2.0 Pre-Flight Structural Context

The repository's code-review-graph tools were not registered in this session.
The bounded structural slice was therefore assembled from exact source reads,
AST search, LSP attempts, and two read-only repository scouts.

```yaml
Target: Tenant identity creation, lifecycle activation, public settings, and paid acceptance
Callers:
  - CreateTenantCommandHandler
  - CompleteInstanceOnboardingCommandHandler
  - EnsureManagedProviderClientProvisionedCommandHandler
  - TransitionControlPlaneTenantLifecycleCommandHandler
  - TenantSettingsDocumentsController
  - GetPublicExperienceSettingsQueryHandler
  - GetRegistrationCheckoutCompositionQueryHandler
  - PaidOrderAcceptanceService
Callees:
  - TenantCreationService
  - ITenantSettingsDocumentRepository
  - ITypedSettingsDocumentResolver
  - TenantActivationCapacityPolicy
  - IPaidCheckoutGovernance
  - PaidOrderAcceptanceSnapshot.Create
Impacted flows:
  - Tenant creation and onboarding (Tier 1)
  - Tenant activation and isolation (Tier 1)
  - Anonymous public/footer rendering (Tier 3)
  - Paid checkout composition and acceptance (Tier 0)
Test coverage:
  - Event.Domain.UnitTests
  - Event.Application.UnitTests
  - Event.Persistence.IntegrationTests
  - Event.API.IntegrationTests
  - Explore.Infrastructure.Tests
  - Explore.Blazor.Client.Tests
  - Event.Architecture.Tests
```

### 2.1 Evidence Log

| Claim | Evidence | Confidence | Notes |
|---|---|---:|---|
| Tenant creation already writes branding atomically | `TenantCreationService`, `CreateTenantCommandHandler`, `TenantCreationServiceTests` | High | Branding is a required typed document seed |
| Single-tenant onboarding creates and synchronizes default-tenant branding | `CompleteInstanceOnboardingCommandHandler`, `UpdateInstanceSubResourceHandlers` | High | Instance and tenant values may match while scopes stay separate |
| Typed documents do not fall back to scalar or instance settings | `TypedSettingsDocumentResolver`, `GetTenantBrandingSettingsDocumentQueryHandlerTests` | High | The migration is intentionally additive/no-dual-read |
| Public fallback behavior is inconsistent | `GetPublicExperienceSettingsQueryHandler`, `PaidEventDisclaimerFormatter`, `MULTI_TENANCY.md` | High | Empty string, literal `ISLAMU`, and documented instance fallback disagree |
| Branding locks control editability, not legal authority | `TenantBrandingSettingsDocumentLockService` | High | A locked brand is not a tenant legal actor |
| Tenant activation currently gates only capacity and transition validity | `TransitionControlPlaneTenantLifecycleCommandHandler`, `TenantActivationCapacityPolicy` | High | No legal-identity readiness seam exists |
| Paid disclosure includes a branding-derived tenant disclaimer, but the persisted acceptance snapshot does not store it | `PaidOrderAcceptanceSnapshot`, `PaidOrderAcceptanceService` | High | The snapshot persists merchant prose and structured instance-operator facts, but no tenant identity |
| Instance paid operator identity is startup-governed | `IPaidCheckoutGovernance`, `PaidCheckoutGovernanceOptions` | High | General instance identity and payment-specific governance are conflated |
| Public footer consumes a server-authored disclaimer | `Footer.razor`, `FooterTests` | High | Tests pin behavior through a prose field |
| No existing tenant legal-identity document was found | `SettingsDocumentKeys`, source/test search, lifecycle scout | High | A new canonical typed document is required |
| Active status bypasses the lifecycle handler in three creation paths | `CreateTenantCommandHandler`, `EnsureManagedProviderClientProvisionedCommandHandler`, `CompleteInstanceOnboardingCommandHandler` | High | The common creation boundary must enforce readiness for Active |
| Tenant onboarding marks `Identity` complete after provisioning branding only | `CompleteTenantOnboardingCommandHandler` | High | The live onboarding request/UI/tests must collect and validate directory identity |

### 2.2 Existing Implementation

#### Domain

- `TenantSettingsDocument` provides additive tenant-owned JSON document
  persistence.
- `BrandingSettings` contains display name and visual assets only.
- `PaidOrderAcceptanceSnapshot` stores immutable buyer-accepted commercial,
  operator, provider, and prose disclosure facts.

#### Application

- `TenantCreationService` writes one required branding document beside a new
  tenant in the caller-owned transaction.
- `TenantBrandingSettingsDocumentProvisioningService` repairs or aligns the
  branding display name.
- `TransitionControlPlaneTenantLifecycleCommandHandler` validates state and
  capacity before Active.
- Public and paid flows separately resolve branding, producing inconsistent
  missing-value behavior.

#### Infrastructure And Persistence

- `TypedSettingsDocumentResolver` deserializes tenant-owned payloads and caches
  them for five minutes.
- Paid acceptance is a persisted aggregate with provider-generated migrations;
  expanding immutable structured evidence changes the relational model.

#### API And Blazor

- `TenantSettingsDocumentsController` exposes typed branding GET/PATCH routes.
- HAL assemblers/policies gate edit affordances.
- `TenantBrandingSection` uses autosave, concurrency stamps, accessible status,
  and generated-client services.
- `Footer` renders the paid directory disclaimer supplied by public settings.

### 2.3 Existing Tests And Verification Coverage

Protected seams include tenant+branding atomic creation, branding provisioning,
presence-aware patches, lock enforcement, concurrency conflict handling,
typed-document query mapping, branding administration, footer disclaimer
rendering, paid acceptance, and provider migration lifecycle tests.

Missing coverage includes:

- canonical directory-operator value semantics and readiness;
- atomic creation of both mandatory tenant documents;
- activation rejection for incomplete identity;
- cross-tenant legal-identity read/write denial;
- structured public tenant/instance disclosure;
- structured paid acceptance and replay;
- missing/corrupt identity fail-closed behavior;
- startup validation for general instance operator identity;
- generated-client and accessibility coverage for the new admin section;
- removal of obsolete fallback/prose properties.

### 2.4 Existing Documentation And Contracts

Authoritative inputs include `MULTI_TENANCY.md`, `PAYMENTS.md`,
`FOOTER_MANAGEMENT.md`, `CONFIGURATION.md`, `SECURITY-MODEL.md`, ADR-022,
OpenAPI/NSwag generated contracts, `.env.example`, and the active
configuration-manifest authority model.

The paused tenant-onboarding plan contains stale broader assumptions and will
be referenced only as superseded context, not implementation authority.

### 2.5 Current Pain Points

- A presentation string currently names an accountable directory actor.
- Missing tenant identity can become blank or silently become `ISLAMU`.
- Instance operator identity is bundled into paid checkout startup options and
  is unavailable as a general platform disclosure contract.
- Tenant activation cannot prove the directory operator is accountable.
- Public and paid surfaces transport prose instead of role-typed disclosure
  facts.
- Existing tests assert prose rather than machine-consumed role fields.
- Documentation describes a fallback the typed runtime does not implement.

### 2.6 Resolved Engineering Decisions

- Activation and anonymous public disclosure use the same required-field
  profile so an Active tenant cannot immediately become publicly unavailable.
- Paid commerce adds a required terms URL to that profile.
- Instance identity is mandatory non-secret startup configuration and uses
  fail-fast option validation before onboarding is reachable.
- The five current `Init` migrations are retained. The final model receives one
  new generated corrective migration per application provider; existing
  development databases must be recreated before applying this breaking cut.
- Exact localized labels and counsel-approved wording remain deferrable
  presentation details and do not change machine contracts.

## 3. Proposed Future State: Behavioral Contract And Scenarios

### Requirement BLIA-R1: Distinct authority identities

The system SHALL represent cosmetic tenant branding, tenant directory-operator
identity, instance operator identity, and organizer merchant identity as
separate authoritative facts.

#### Scenario: A single organization holds multiple roles

- **GIVEN** a self-hosted single-tenant deployment uses the same public name for
  its instance and default tenant
- **WHEN** public or paid disclosures are composed
- **THEN** the same value MAY appear more than once, but each occurrence MUST be
  labeled and stored under its distinct role

#### Scenario: Branding is locked by the instance

- **GIVEN** instance governance locks the tenant's cosmetic display name
- **WHEN** the tenant's legal/operator disclosure is rendered
- **THEN** the accountable tenant identity MUST remain tenant-owned and MUST NOT
  be replaced by the locked cosmetic brand

### Requirement BLIA-R2: Atomic tenant identity provisioning

The system SHALL create the tenant, cosmetic branding, and directory-operator
identity atomically.

#### Scenario: New tenant creation succeeds

- **GIVEN** a valid tenant name and slug
- **WHEN** tenant creation commits
- **THEN** branding and directory-operator documents MUST exist with the tenant
  public name seeded from the submitted tenant name

#### Scenario: Mandatory document persistence fails

- **GIVEN** either required document cannot be persisted
- **WHEN** tenant creation runs
- **THEN** the tenant and all required documents MUST roll back atomically

### Requirement BLIA-R3: Capability-specific identity readiness

The system SHALL evaluate directory-operator completeness for tenant activation,
anonymous public disclosure, and paid commerce using closed reason codes.

#### Scenario: Activation is incomplete

- **GIVEN** a provisioning tenant lacks any activation-required identity field
- **WHEN** an authorized operator requests Active status
- **THEN** the transition MUST fail without state mutation and MUST return a
  stable failure code plus the bounded missing-field codes

#### Scenario: Corrupted identity on an active tenant

- **GIVEN** an active tenant's identity document is missing, malformed, or
  unsupported
- **WHEN** anonymous public settings or checkout composition is requested
- **THEN** the affected tenant flow MUST fail closed without substituting any
  instance, brand, or product identity

#### Capability field matrix

| Field | Activation | Public disclosure | Paid commerce |
|---|:---:|:---:|:---:|
| `PublicName` | Required | Required | Required |
| `LegalName` | Required | Required | Required |
| `OperatorKindCode` | Required | Required | Required |
| `JurisdictionCountryCode` | Required | Required | Required |
| `RegistrationIdentifier` | Optional | Optional | Optional |
| `PublicContactEmail` | Required | Required | Required |
| `LegalNoticeUrl` | Required | Required | Required |
| `TermsUrl` | Optional | Optional | Required |
| `PrivacyUrl` | Required | Required | Required |

Every supplied value remains structurally valid in draft state. Missing and
malformed fields return separate stable field codes. Legal and privacy links
are absolute HTTPS URLs; registration identifiers are never synthesized.

### Requirement BLIA-R4: Tenant-isolated mutation

The system SHALL expose tenant directory-operator identity through authenticated
HAL-driven CQRS endpoints with optimistic concurrency.

#### Scenario: Authorized tenant update

- **GIVEN** the current tenant has the edit relation and a current concurrency
  stamp
- **WHEN** one identity field is patched
- **THEN** unspecified fields MUST be preserved, the revision MUST advance, and
  public caches MUST be invalidated once

#### Scenario: Cross-tenant or stale update

- **GIVEN** an actor targets another tenant or submits a stale stamp
- **WHEN** the patch is handled
- **THEN** the request MUST be rejected with no cross-tenant disclosure and no
  partial write

### Requirement BLIA-R5: Structured public disclosure

The system SHALL expose machine-consumed, role-labeled tenant and instance
operator disclosures independently from visual branding.

#### Scenario: Tenant public footer

- **GIVEN** an active tenant and a configured instance operator
- **WHEN** the public footer renders
- **THEN** it MUST identify the tenant directory operator and instance platform
  operator separately and render each role's applicable legal/privacy links

#### Scenario: White-label presentation

- **GIVEN** white-labeling hides cosmetic instance co-branding
- **WHEN** the instance materially owns platform, privacy, security, complaint,
  or payment duties
- **THEN** the role-labeled instance operator disclosure MUST remain available

### Requirement BLIA-R6: Structured immutable paid acceptance

The system SHALL persist the exact tenant, instance, organizer, provider,
policy, and money facts accepted by the buyer.

#### Scenario: Paid acceptance succeeds

- **GIVEN** complete tenant and instance identities, an organizer merchant, and
  a current paid policy
- **WHEN** the buyer accepts checkout composition
- **THEN** immutable acceptance MUST snapshot structured role identities, legal
  links, source revisions, provider facts, policies, and line money

#### Scenario: Identity changes after acceptance

- **GIVEN** an acceptance snapshot already exists
- **WHEN** tenant branding, tenant legal identity, or instance operator
  configuration changes later
- **THEN** historical acceptance MUST retain the exact previously accepted facts

#### Scenario: Paid event publication

- **GIVEN** tenant, instance, organizer, provider, or paid-policy authority is
  incomplete
- **WHEN** paid publication preflight or the publication command runs
- **THEN** publication MUST fail closed and the command MUST re-evaluate
  readiness rather than trusting a prior preflight or HAL affordance

### Requirement BLIA-R7: General instance operator startup governance

The system SHALL require a complete non-secret instance operator identity
independently from payment-specific checkout governance.

#### Scenario: Valid startup

- **GIVEN** instance operator identity and any enabled payment governance are
  complete
- **WHEN** the host starts and readiness is evaluated
- **THEN** general operator disclosure MUST be available to global and tenant
  public surfaces

#### Scenario: Incomplete operator configuration

- **GIVEN** required operator identity is missing
- **WHEN** the API, Standalone host, or Blazor host starts
- **THEN** startup option validation MUST fail before onboarding is served,
  without exposing partial operator data or activating paid checkout

Instance onboarding continues to collect the separate default-tenant identity;
self-hosters must configure instance operator identity in `.env` or Infisical
before first-run onboarding.

### Requirement BLIA-R8: No compatibility fallback

The system SHALL remove obsolete scalar branding identity reads/writes, prose
directory-disclaimer contracts, and old checkout-governance operator keys.

#### Scenario: Contract cutover

- **GIVEN** the new source, migrations, OpenAPI, and generated client are built
- **WHEN** obsolete symbols or configuration keys are searched
- **THEN** no runtime caller, dual-write path, compatibility property, or
  deprecated alias MUST remain

### Requirement BLIA-R9: PII-minimized observability

The system SHALL record readiness and corruption events without logging identity
payloads, legal names, registration identifiers, email addresses, or URLs.

#### Scenario: Readiness failure is observed

- **GIVEN** a tenant identity fails readiness
- **WHEN** telemetry is emitted
- **THEN** logs and metrics MUST contain only approved identifiers, capability
  context, reason codes, and correlation data

## 4. Non-Negotiable Constraints

- Repositories return entities and typed documents, never DTOs or `IQueryable`.
- Tenant filters and explicit tenant context remain active on every identity
  read and write.
- Validators are manually instantiated in handlers.
- Domain values own normalization and closed-code validation; controllers only
  dispatch, assemble HAL, and shape responses.
- GET remains anonymous only for already-composed public experience; identity
  administration GET/PATCH remains authorized.
- HAL links are the sole UI authority for edit affordances.
- Browser clients use generated clients through the BFF boundary.
- Generated OpenAPI clients and EF migration snapshots are never hand-edited.
- Every new file starts with two `ABOUTME:` lines.
- UUIDv7 identifies new aggregates/documents; `int` remains lookup identity.
- No secret or operator configuration value is hard-coded. Non-secret schema is
  documented in `.env.example`; secrets remain Infisical or `.env` only.
- OrganizerDirect, direct-charge account fencing, immutable acceptance,
  integer minor-unit money, and payment idempotency remain unchanged.
- Tests cannot pin prose or use timing sleeps/polling.
- No backward-compatibility alias, dual read, dual write, or old configuration
  fallback is allowed.
- External source expression and incompatible dependencies are forbidden; no
  new dependency is expected.

## 5. Architecture And Design Decisions

### D1. Model the tenant role as directory-operator identity

- **Decision:** Add a canonical `tenant.directory_operator_identity` typed
  settings document containing accountable public/legal identity and explicit
  legal links.
- **Why:** The role is broader and more accurate than “legal entity”; it can
  represent incorporated organizations, individuals, unincorporated
  communities, or public bodies without conflating branding.
- **Alternatives considered:** Reuse `BrandingSettings`; create a generic custom
  property bag; create a new relational aggregate.
- **Consequences:** Typed-document infrastructure is reused without a new tenant
  identity table. Payload validation and readiness remain explicit.
- **Affected layers:** Domain settings, Application services/CQRS, API, Blazor,
  public experience, tests, docs.

### D2. Use a closed, minimal public identity payload

- **Decision:** The payload will contain `PublicName`, `LegalName`,
  `OperatorKindCode`, `JurisdictionCountryCode`, optional
  `RegistrationIdentifier`, `PublicContactEmail`, `LegalNoticeUrl`, `TermsUrl`,
  and `PrivacyUrl`.
- **Why:** These fields support accountable role disclosure without storing
  legal documents, private addresses, or payment credentials.
- **Alternatives considered:** Require a street address; store free-form legal
  prose; use arbitrary JSON.
- **Consequences:** Some jurisdictions may later require an additive,
  counsel-approved schema revision. Public contact fields are intentionally
  disclosed and receive bounded validation.
- **Affected layers:** Domain payload/value, DTOs, validators, public and paid
  disclosures.

### D3. Separate persisted payload from validated capability profile

- **Decision:** Typed settings preserve draft/incomplete values; a Domain value
  factory produces a normalized `TenantDirectoryOperatorIdentity` only for a
  named readiness capability.
- **Why:** Provisioning must create a draft before all details exist, while
  activation and commerce must parse into valid state.
- **Alternatives considered:** Permit only complete payload persistence; scatter
  null checks through handlers.
- **Consequences:** One readiness evaluator owns missing/invalid reason codes.
- **Affected layers:** Domain, Application readiness service, lifecycle and paid
  flows.

### D4. Create all mandatory tenant documents atomically

- **Decision:** Refactor `TenantCreationRequest` to carry two explicit typed
  seeds: branding and directory-operator identity.
- **Why:** Exactly two known mandatory documents need atomic ownership; a
  generic collection would hide the invariant and add unnecessary abstraction.
- **Alternatives considered:** Call provisioning after tenant creation; add
  another parallel constructor field group.
- **Consequences:** All creation callers change in one cut. No partial tenant is
  possible.
- **Affected layers:** Application contracts/service/callers and tests.

### D5. Add capability-specific readiness to creation, lifecycle, and commerce

- **Decision:** One `ITenantDirectoryOperatorReadinessEvaluator` returns
  immutable structured readiness for `Activation`, `PublicDisclosure`, and
  `PaidCommerce`. `TenantCreationService` becomes the only production tenant
  creation boundary and rejects an Active request unless its supplied identity
  seed passes Activation readiness. Existing lifecycle activation rechecks the
  same policy transactionally.
- **Why:** Field requirements differ by capability, and closed reasons must be
  reusable without duplicating policy.
- **Alternatives considered:** Validate only in controller; validate only at
  checkout; infer readiness from tenant status.
- **Consequences:** Direct creation, managed provisioning, configuration
  manifest creation, single-tenant onboarding, and later lifecycle transitions
  cannot bypass readiness. Tenant onboarding cannot mark `Identity` complete
  until the same profile passes.
- **Affected layers:** Domain readiness contract, Application lifecycle/public/
  paid handlers, tests.

### D6. Split general instance identity from payment governance

- **Decision:** Introduce startup-bound `IInstanceOperatorIdentity`; remove
  general operator fields from `PaidCheckoutGovernanceOptions`; compose both
  contracts when paid checkout validates or builds acceptance.
- **Why:** Global platform attribution exists even when payments are disabled.
  Payment ownership/status remains startup-governed but is not the general
  identity source.
- **Alternatives considered:** Reuse payment options everywhere; store instance
  identity in tenant branding; make the general identity tenant-editable.
- **Consequences:** Existing environment keys break intentionally. The new
  general identity options use `ValidateOnStart`; `.env.example`, every host
  registration, onboarding prerequisites, tests, and operations docs change
  together.
- **Affected layers:** Application contracts, hosting/configuration,
  public/paid composition, docs/tests.

### D7. Transport structured disclosure, not canonical prose

- **Decision:** Remove `PaidEventDirectoryDisclaimer`; expose nested,
  role-labeled tenant and instance disclosures. UI-localized components own
  explanatory labels.
- **Why:** Machine-consumed values are stable and testable; legal prose requires
  localization and counsel review and must not be pinned by tests.
- **Alternatives considered:** Keep the string and add structured fields; add
  compatibility aliases.
- **Consequences:** Breaking OpenAPI/client change; formatter and prose tests are
  deleted only after structured tests fail and pass.
- **Affected layers:** Domain snapshot, Application DTOs/services, API/OpenAPI,
  generated client, Blazor/footer, tests/docs.

### D8. Persist immutable multi-party disclosure in paid acceptance

- **Decision:** Add normalized tenant operator facts, legal links, identity
  document ID/revision, and structured organizer merchant facts plus their
  immutable source identity to `PaidOrderAcceptanceSnapshot`.
- **Why:** Historical buyer acceptance cannot depend on mutable current
  settings or free-form `MerchantDisclosureText`.
- **Alternatives considered:** Store only current document ID; store one JSON
  blob; keep one prose string.
- **Consequences:** Provider-generated migrations and persistence lifecycle
  coverage are required. `OrganizerPaymentRecipientSnapshot` remains the
  immutable OrganizerDirect/provider authority and is extended only with the
  structured merchant disclosure facts needed by paid composition; tenant
  identity never becomes a payment recipient. Existing development databases
  are reset rather than backfilled through compatibility logic.
- **Affected layers:** Domain aggregate, EF configuration/migrations,
  Application mapping, tests.

### D9. Preserve explicit tenant ownership and HAL mutation authority

- **Decision:** Directory-operator identity has dedicated GET/PATCH handlers,
  DTOs, HAL policy/assembler, API route, generated client service, and admin
  section. Instance branding locks do not overwrite or lock tenant legal facts.
- **Why:** Tenant identity is accountable tenant data; instance authority is
  suspension/readiness governance, not impersonation.
- **Alternatives considered:** Add fields to branding endpoint; expose one broad
  settings PATCH; let instance branding locks substitute values.
- **Consequences:** More explicit files, but clear ownership, test seams, and
  consumer contracts.
- **Affected layers:** Application, API, generated client, Blazor, tests.

### D10. Use forward-fix cutover with no compatibility period

- **Decision:** Remove obsolete symbols/configuration in the same verified
  increment that introduces replacements.
- **Why:** The repository is in development mode and the user explicitly
  rejects compatibility cost.
- **Alternatives considered:** Dual read/write; obsolete aliases; staged
  deprecation.
- **Consequences:** All affected hosts and clients must deploy from the same
  revision; generated migrations remain reversible, but application rollback
  requires restoring the prior development database or deploying a forward fix.
- **Affected layers:** Entire workstream.

### D11. Define one public unavailability contract

- **Decision:** `/api/PublicExperience/settings` and
  `/api/PublicExperience/shell` return non-cacheable RFC 7807 `503` responses
  with stable code `tenant_identity_unavailable` when tenant or instance public
  identity is missing, malformed, or unsupported.
- **Why:** Anonymous consumers need one deterministic fail-closed contract
  rather than a blank DTO, a literal fallback, or a cacheable partial shell.
- **Consequences:** Query handlers return explicit results, controllers map the
  failure through the repository ProblemDetails policy, output cache stores
  successful `200` responses only, and BFF/Blazor render an unavailable state
  without substituting identity.

### D12. Keep merged migration history and add generated corrective migrations

- **Decision:** Retain every current `Init` migration and generate one new
  application migration for PostgreSQL, SQLite, SQL Server, MariaDB, and MySQL
  after the final model stabilizes.
- **Why:** Repository policy forbids rewriting merged history. Development
  databases with old acceptance rows are explicitly recreated for this
  breaking cut, so no nullable legacy model, inferred backfill, or runtime
  fallback is added.
- **Consequences:** Data Protection and privacy-authority catalogs are
  untouched. `schemas/islamu-event.md`, provider migration lifecycle tests,
  pending-model checks, and MigrationService second-run evidence are required.

## 6. Implementation Phases

### Phase 1: Authority Contracts And Red Specifications

- **Goal:** Establish compile-safe contract shells, then behavioral Red tests
  and the smallest Domain/startup implementations that satisfy them.
- **Depends on:** Approved plan and clean baseline.
- **Relevant files:** Domain settings/payloads/value objects; Application
  contracts; Domain/Application tests; startup options.
- **Related skills/rules:** `criticality-guardrail`, `clean-architecture-rules`,
  `tests`, `domain`, `application-layer`.
- **Acceptance criteria:**
  - Closed operator-kind and readiness reason codes exist.
  - Draft payload and validated capability identity are separate.
  - Instance and tenant identities expose no payment secrets.
  - Red tests fail for missing implementation rather than fixture mistakes.
- **Phase-end verification:** `dotnet test --project
  tests/Event.Domain.UnitTests/Event.Domain.UnitTests.csproj --configuration
  Release --verbosity quiet`
- **Rollback / failure handling:** Remove only unverified new contracts/tests;
  do not weaken the red invariants.

### Phase 2: Atomic Provisioning And Activation Readiness

- **Goal:** Create both tenant documents atomically and enforce capability
  readiness before Active.
- **Depends on:** Phase 1.
- **Relevant files:** Tenant creation contracts/service/callers; lifecycle
  handler; readiness service; Application tests.
- **Related skills/rules:** `cqrs-mediatr-guidelines`, `application-layer`,
  `tests`.
- **Acceptance criteria:**
  - Every production creation caller routes through `TenantCreationService`
    with the two explicit typed seeds.
  - Direct, managed-provider, configuration-manifest, and single-tenant Active
    requests prove readiness before any write.
  - Live tenant onboarding collects identity and cannot falsely complete its
    `Identity` step.
  - Incomplete activation returns stable bounded reasons and performs no write.
  - Cross-tenant reads cannot influence readiness.
- **Phase-end verification:** `dotnet test --project
  tests/Event.Application.UnitTests/Event.Application.UnitTests.csproj
  --configuration Release --verbosity quiet`
- **Rollback / failure handling:** Creation and activation changes revert
  together; no partially provisioned tenant is accepted.

### Phase 3: CQRS, HAL, API, And Generated Client

- **Goal:** Expose isolated optimistic-concurrency administration and regenerate
  the public client contract.
- **Depends on:** Phase 2.
- **Relevant files:** Application DTOs/validators/handlers/HAL; API controller;
  authorization descriptors/registry; JSON source-generation context; HAL
  schema/DI catalogs; OpenAPI document; NSwag generated client;
  API/architecture tests.
- **Related skills/rules:** `cqrs-mediatr-guidelines`, `api-controllers`,
  `blazor-ui-conventions`, `tests`.
- **Acceptance criteria:**
  - GET and PATCH honor tenant context, authorization, stamp conflicts, and HAL.
  - Domain validation is not duplicated in the controller.
  - Generated client is produced by repository tooling with no hand edits.
  - Authorization, JSON source generation, HAL schema, route, and generated
    client registries remain in parity.
- **Phase-end verification:** `dotnet test --project
  tests/Event.API.IntegrationTests/Event.API.IntegrationTests.csproj
  --configuration Release --verbosity quiet`
- **Rollback / failure handling:** Revert source DTO/API changes and regenerate;
  never patch generated output.

### Phase 4: Public And Paid Structured Disclosure

- **Goal:** Compose distinct tenant, instance, organizer, provider, and policy
  facts and persist immutable paid evidence.
- **Depends on:** Phases 1-3.
- **Relevant files:** Public experience DTO/handler; paid composition and
  acceptance services; snapshot aggregate/configuration; generated provider
  migrations; footer DTO/service; tests.
- **Related skills/rules:** `payments-commerce`, `dotnet-efcore-guidelines`,
  `criticality-guardrail`, `footer-management`, `tests`.
- **Acceptance criteria:**
  - Public output contains structured tenant and instance disclosures.
  - Missing/corrupt identity fails closed without fallback.
  - Settings and shell use the same non-cacheable `503` contract on failure.
  - Paid publication, checkout, and acceptance each re-evaluate readiness.
  - Paid acceptance snapshots exact tenant, organizer, instance, provider,
    policy, and money identities plus immutable source revisions.
  - Provider migrations are generator-produced and reversible.
  - OrganizerDirect and money/idempotency invariants remain unchanged.
- **Phase-end verification:** `dotnet test --project
  tests/Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj
  --configuration Release --verbosity quiet`
- **Rollback / failure handling:** Fix model/configuration and regenerate
  unapplied development migrations; never hand-edit generated artifacts.

### Phase 5: Tenant Administration And Accessible Public Presentation

- **Goal:** Provide an accessible HAL-gated admin form and role-labeled footer.
- **Depends on:** Phases 3-4.
- **Relevant files:** Blazor admin service/model/component/CSS; public footer;
  localization resources; component/integration tests.
- **Related skills/rules:** `blazor-ui-conventions`, `accessibility`,
  `footer-management`, `blazor-client`, `tests`.
- **Acceptance criteria:**
  - Editable fields follow HAL relations and concurrency refresh.
  - Labels, descriptions, errors, focus, keyboard behavior, LTR URL/email
    islands, RTL layout, and status announcements meet WCAG 2.2 AA.
  - Footer presents roles separately without relying on prose DTOs.
  - Component tests assert machine-consumed values and semantics, not prose.
- **Phase-end verification:** `dotnet test --project
  tests/Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj
  --configuration Release --verbosity quiet`
- **Rollback / failure handling:** UI can revert independently only while the
  new API remains unused; once generated consumers ship, use a forward fix.

### Phase 6: Contract Cutover, Documentation, Release Evidence, And Review

- **Goal:** Remove obsolete paths, update operational truth, and produce
  release/review evidence.
- **Depends on:** Phases 1-5.
- **Relevant files:** Obsolete branding/formatter/configuration symbols;
  `.env.example`; canonical docs; API changelog; release change fragment;
  workstream/I-VSD artifacts.
- **Related skills/rules:** `review-work`, `review-pr`, `ip-clean-room`,
  `i-vsd`, `implementation-plan`.
- **Acceptance criteria:**
  - Repository-wide absence verification finds no old scalar identity, literal
    fallback, prose DTO, or old operator configuration key.
  - Documentation matches runtime sources and self-hosted behavior.
  - I-VSD mappings are current/plan-aligned and legal/scholarly boundaries are
    explicit.
  - Tier 2 breaking change fragment validates.
  - Build and all affected project gates pass once.
- **Phase-end verification:** `dotnet build --configuration Release --verbosity
  quiet`
- **Rollback / failure handling:** Do not restore compatibility shims; use a
  complete forward fix or restore the pre-change development revision/database.

## 7. Verification Strategy

### Baseline

Before the first product edit:

```bash
dotnet build --configuration Release --verbosity quiet
```

Run only focused TUnit class slices during Red/Green work. At each phase exit,
run one Release build and at most the single owning project gate named above.
At PR completion run affected full projects once:

```bash
dotnet test --project tests/Event.Domain.UnitTests/Event.Domain.UnitTests.csproj --configuration Release --verbosity quiet
dotnet test --project tests/Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet
dotnet test --project tests/Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj --configuration Release --verbosity quiet
dotnet test --project tests/Explore.Infrastructure.Tests/Explore.Infrastructure.Tests.csproj --configuration Release --verbosity quiet
dotnet test --project tests/Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet
dotnet test --project tests/Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --verbosity quiet
dotnet test --project tests/Explore.Blazor.IntegrationTests/Explore.Blazor.IntegrationTests.csproj --configuration Release --verbosity quiet
dotnet test --project tests/Explore.Secrets.UnitTests/Explore.Secrets.UnitTests.csproj --configuration Release --verbosity quiet
dotnet test --project tests/Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet
```

Tier 0/1 completion also requires real provider-migration lifecycle evidence,
cross-tenant Invariant-Breaker tests, generated OpenAPI/client diff review,
payment acceptance replay coverage, mutation testing above the repository
threshold for changed critical logic, and Epistemic MAD review.

Manual QA must exercise API help/error/happy paths, tenant activation failure and
success, admin keyboard/focus behavior, anonymous footer output, paid checkout
composition, and one malformed identity case without timing waits.

## 8. Release And Changelog Strategy

This is Tier 2 because it intentionally breaks configuration, API, generated
client, and persisted acceptance shape. The public capability scope is
`onboarding`; payment-specific increments use `registration`. The final closing
task creates an append-only
`docs/releases/changes/CHG-2026-NNNN.yaml`, validates it through
`ReleaseInputPolicy`, and records the same `Change-Id` in the terminal commit
footer if the user later authorizes commits. The terminal commit also requires a
`BREAKING CHANGE:` footer explaining removed operator keys and prose contracts.

`docs/API_CHANGELOG.md` records the externally observable OpenAPI break. Internal
architecture-only commits use `Changelog: skip` with an explicit reason.

## 9. Islamic Value-Sensitive Design And Moral Boundaries

The mapped report is
[i-vsd-branding-legal-identity-authority.md](../../../islamic-value-sensitive-design/i-vsd-branding-legal-identity-authority.md).
The selected safeguards are:

- `IVSD-BLIA-M001`: mandatory tenant identity/readiness;
- `IVSD-BLIA-M002`: observable fail-closed corruption handling;
- `IVSD-BLIA-M003`: explicit tenant/instance/organizer roles;
- `IVSD-BLIA-M004`: role-based instance disclosure despite white-labeling;
- `IVSD-BLIA-M005`: paid activation/checkout fail-closed;
- `IVSD-BLIA-M006`: one clean cut without dual semantics;
- `IVSD-BLIA-M007`: adversarial scenario coverage.

Implementation must not claim legal or religious authority. Final disclosure
wording remains subject to qualified counsel and scholarly review; the software
enforces truthful role structure and configurable links.

## 10. Documentation Impact

Update:

- `docs/MULTI_TENANCY.md` for identity scopes, provisioning, readiness, and no
  fallback;
- `docs/PAYMENTS.md` and ADR-022 for structured immutable disclosures;
- `docs/FOOTER_MANAGEMENT.md` for tenant and instance operator sections;
- `docs/CONFIGURATION.md`, `.env.example`, and operations guidance for the new
  instance operator section and removed checkout keys;
- `docs/DOMAIN.md` for tenant document and acceptance snapshot shape;
- `schemas/islamu-event.md` for generated relational identity and acceptance
  fields;
- `docs/AUTHORIZATION.md` for the identity resource and HAL/Cerbos actions;
- `docs/API.md`, `docs/API_CHANGELOG.md`, and contract inventory for routes and
  breaking DTO changes;
- I-VSD report and this triad for final evidence.

## 11. Security, Privacy, And Tenant Isolation

- Identity documents are resolved only through current tenant context and
  tenant-filtered repositories.
- Public output contains only explicitly public fields.
- Registration identifiers and contact addresses are never logged.
- Patch requests carry no target tenant ID from the browser; server tenant
  context owns scope.
- Cross-tenant and header-spoofing tests are mandatory.
- Startup operator identity is non-secret configuration; secret providers remain
  unchanged.
- Public and paid flows fail closed on missing, malformed, unsupported, or
  cross-tenant identity.

## 12. Performance And Reliability

- Reuse typed-document cache and tag invalidation; do not add per-render
  repository fan-out.
- Public experience resolves branding, tenant identity, and instance identity
  once per composed response.
- Readiness returns immutable bounded results and performs no writes.
- Paid acceptance captures current identity once inside the existing
  transaction/revision checks.
- Telemetry uses stable reason-code dimensions and avoids unbounded names/URLs.

## 13. Accessibility, Localization, And RTL

- Admin fields use native/MudBlazor semantics, persistent labels, descriptions,
  required-state indication, and explicit validation summaries.
- Saving, conflict, and readiness states use accessible live regions and focus
  transfer without timing sleeps.
- Public URLs, email addresses, identifiers, and codes render in isolated
  `dir="ltr"` spans inside logical RTL-safe layout.
- Role labels and explanatory text are localizable UI resources; API contracts
  contain facts, not English legal prose.
- Manual QA covers keyboard-only use, 200% zoom/reflow, light/dark themes, and
  one RTL locale.

## 14. Deployment, Cutover, And Rollback

- All application hosts, generated clients, and provider migrations deploy from
  one revision.
- Self-hosters must replace removed environment keys before startup.
- Current merged `Init` migrations remain immutable. Generate one corrective
  migration per application provider after the model is final. Existing
  development databases must be recreated before this breaking cut; preserved
  old acceptance data is outside scope and receives no inferred backfill or
  runtime fallback.
- Generated migrations keep valid `Down` operations, but production-style
  rollback after the contract cut is unsupported; use a forward fix or restore
  the previous application/database pair.
- Readiness prevents traffic when instance operator configuration is incomplete.

## 15. Definition Of Done

The workstream is complete only when:

- all requirements BLIA-R1 through BLIA-R9 are observable;
- every Red task failed for the named scenario before its Green task passed;
- tenant creation and activation are atomic and fail closed;
- public and paid outputs contain structured role identities;
- paid acceptance preserves immutable historical evidence;
- no obsolete fallback, prose DTO, scalar identity, or old configuration key
  remains;
- generated migrations, OpenAPI, and NSwag client are current;
- affected builds/tests, mutation threshold, MAD review, and manual QA pass;
- canonical docs, change fragment, I-VSD report, tasks, and context are current;
- no unrelated workspace changes were modified.

## 16. Implementation-Agent Responsibilities

The implementation agent must use this plan's architectural decisions and the
task ledger's Red/Green order. It must inspect before editing, use LSP for
symbols/diagnostics, use `apply_patch` for every file edit, regenerate rather
than hand-edit migrations/clients, update task status immediately, and stop on
any repository-rule conflict. It must teach the final architecture, data flow,
failure semantics, and verification evidence rather than reporting only file
names.

## 17. Progress And Handoff Contract

Dynamic status, baseline results, blockers, modified files, and dated handoffs
belong only in
`branding-legal-identity-authority-context.md`. Granular Red/Green tasks and
verification checkboxes belong only in
`branding-legal-identity-authority-tasks.md`. This plan changes only when
strategy, scope, architecture, phase order, acceptance, risk, or validation
changes.

Completion reporting must state:

```text
Completed: implemented behavior and verified evidence
Remaining: explicit unfinished work
Next: recommended next slice
Docs updated: plan/context/tasks/I-VSD and canonical docs status
```

## 18. Potential Risks And Unknowns

The hardest risk is not storage; it is preserving truthful role attribution
without accidentally making the tenant or instance look like the organizer or
merchant. The second risk is the intentional configuration/API cut: startup
operator identity, public DTOs, generated clients, and paid snapshots must move
together or deployments become unavailable. Legal-field completeness is
capability-dependent and final jurisdiction-specific wording remains outside
engineering authority. No implementation-scope question remains, but qualified
legal review may require an additive schema revision later.
