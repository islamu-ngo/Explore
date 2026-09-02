<!-- ABOUTME: Active working memory for configured-administrator headless instance onboarding. -->
<!-- ABOUTME: Records review state, resume order, blockers, decisions, baseline, and planning handoff. -->

# Headless Instance Onboarding — Context

Last Updated: 2026-09-01 Europe/Brussels

## Review State

- **Planning status:** Approved; implementation complete and verified
- **I-VSD report:**
  [i-vsd-headless-instance-onboarding.md](../../../islamic-value-sensitive-design/i-vsd-headless-instance-onboarding.md)
- **I-VSD reviewed input revision:**
  `sha256:5c67de2a4b210f6d57239a6eb7f4c7ae38cf6bf4a0b9321e32c77410065a9ca9`
- **I-VSD status / disposition:** current and plan-aligned
- **CTO review:** Not reviewed
- **User approval:** Approved in full on 2026-09-01
- **Allocated change identities:**
  - migration: `CHG-01M1ETX06HRETFBJTK6SCZGBZ6`
  - provider identity: `CHG-01M1ETXMS84KS8ASDW4GR22Q3J`
  - activation: `CHG-01M1EQWDAHHXQ3AD29B4Y0645B`

## SESSION PROGRESS (2026-09-01 Europe/Brussels)

### COMPLETED

- Classified the work as Tier 1 `external-infrastructure-bootstrap` with
  supporting CQRS, BFF-auth, and additive OpenAPI contract obligations.
- Verified the current interactive onboarding, Keycloak synchronization,
  ATProto verified-session, BFF routing, bootstrap persistence, provider-login
  uniqueness, ConfigurationManifest, and operator-documentation flows.
- Resolved the architecture with the user:
  - Setup Assistant remains offline-only.
  - ConfigurationManifest remains identity-free and portable.
  - deployment-local configuration names the exact initial administrator.
  - Keycloak matches configured issuer plus `sub`.
  - ATProto matches the DID returned by verified OAuth.
  - the pending claim completes atomically only after authentication proof.
  - no backward compatibility is required.
- Created the I-VSD planning report and mapped all five accepted findings and
  mitigations.
- Created the canonical ten-phase implementation plan.
- Created the 28-task invariant-first execution ledger with exact phase
  verification and commit contracts.
- Allocated collision-safe release identities for the independently breaking
  migration, provider-identity replacement, and runtime activation commits.
- Completed Phase 1 offline configuration contract:
  - seven exact configured-administrator bootstrap keys
  - value-safe composer validation and exact-key diagnostics
  - canonical 353-definition JSON and generated configuration documentation
  - 72/72 Setup Core tests
  - atomic commit `5896449f3ae7f78f302cc8f4d85e29574f74a2a5`
- Completed and activated Phases 2–10:
  - typed bootstrap status/mode/provider lifecycle and generation finality
  - five-provider generated migrations with deterministic legacy backfill
  - provider-native row locks and serializable claim convergence
  - shared provider-neutral completion transaction
  - authority-qualified OIDC subject keys and canonical ATProto DIDs
  - verified ATProto first claim and closed BFF provider routing
  - generated status client and startup gate consumption
  - environment-backed preparation in Split and Standalone topology
  - operator, schema, release-fragment, and API contract evidence
  - implementation commit `02e024a3e023a998209462610c48ed52f058d85b`
  - host/API/generated integration commit
    `584be9624aa5c5367fe3d918d24866fd297acb07`
- Final verification:
  - solution Release build passed with zero errors
  - exact changed test surface passed AssuranceAudit with zero diagnostics
  - focused Domain, Application, Persistence, API, BFF, Client,
    Infrastructure, Standalone, and Architecture suites passed
  - reproduced and fixed every Tier 1 MAD finding across generated status,
    cookie/self-call trust boundaries, replay effects, durable setup-secret
    revocation, provider-link isolation, selector-removal finality, migration
    downgrade preservation, and empty-table replica convergence
  - real provider convergence evidence includes PostgreSQL existing-row
    races plus empty-table SQLite, MariaDB, and MySQL startup

### IN PROGRESS

- Final anonymized Tier 1 security/operations review and closure evidence only.

### NEXT

1. Record weighted MAD review disposition.
2. Commit only the final verified identity/BFF corrections and synchronized
   closure artifacts, excluding unrelated shared-tree changes.
3. Close the active goal.

### BLOCKERS

- None for the implemented capability. Unrelated Setup Assistant and workflow
  changes remain in the shared worktree and are excluded from closure commits.

## Quick Resume

The workstream is implemented. If closure is interrupted, resume from the
latest MAD review result and the scoped uncommitted identity/BFF corrections;
do not restart implementation or touch unrelated shared-tree paths.

## Key Files And Responsibilities

| Path | Existing/New | Layer | Purpose | Notes |
|---|---|---|---|---|
| `src/Explore.Domain/InstanceBootstrapState.cs` | Existing | Domain | First-run state lifecycle | Replace binary mutation with explicit transitions |
| `src/Explore.Application/Features/InstanceOnboarding/Handlers/Commands/CompleteInstanceOnboardingCommandHandler.cs` | Existing | Application | Interactive completion | Refactor onto shared completion operation |
| `src/Explore.Application/Features/InstanceOnboarding/Services/InstanceOnboardingCompletionOperation.cs` | New | Application | Deep atomic completion module | One transaction for interactive/configured modes |
| `src/Explore.Application/Authentication/ProviderAccountKey.cs` | New | Application | Authority-qualified provider identity | Keycloak/Google issuer+subject; ATProto DID |
| `src/Explore.Application/Features/Users/Handlers/Commands/SyncUserCommandHandler.cs` | Existing | Application | Keycloak post-login sync seam | Must bypass email fallback for initial claim |
| `src/Explore.Application/Features/Authentication/Atproto/Handlers/Commands/BootstrapAtprotoSessionCommandHandler.cs` | Existing | Application | Verified ATProto session seam | Narrow exact-DID branch after verification |
| `src/Explore.Persistence/Configurations/Entities/InstanceBootstrapStateConfiguration.cs` | Existing | Persistence | Bootstrap schema/constraints | Source for generated provider migrations |
| `src/Explore.API/Controllers/UserController.cs` | Existing | API | Authenticated user synchronization | No new bootstrap endpoint planned |
| `src/Explore.Application/DTOs/Onboarding/InstanceOnboardingStatusDto.cs` | Existing | Application/API contract | Bounded bootstrap state/provider | Never exposes identity values |
| `src/Explore.Blazor/Extensions/BffAuthEndpoints.cs` | Existing | BFF | Provider challenge routing | Permit only configured provider while pending |
| `src/Explore.Blazor/Services/BffAdminClaimsTransformation.cs` | Existing | BFF | Post-login sync/admin refresh | Stop skipping configured pending mode |
| `src/Event.Setup.Core/Environment/CanonicalEnvironmentMetadata.cs` | Existing | Offline setup | Closed `.env` metadata | No runtime connectivity |
| `src/Event.Setup.Core/Dotenv/DotenvComposer.cs` | Existing | Offline setup | Execute value and matrix validation | Added to Phase 1 after confirmed Red proved metadata-only scope insufficient |
| `docs/CONFIGURATION.md` | Existing | Operator docs | Generator-owned environment catalogue block | Added to Phase 1 because canonical generation updates it atomically with machine JSON |
| `src/Explore.Infrastructure/Services/ConfiguredAdministratorBootstrapProvider.cs` | New | Infrastructure | Server-only runtime configuration | Implemented in Phase 8; activated only in Phase 10 |
| `docs/releases/changes/CHG-01M1EQWDAHHXQ3AD29B4Y0645B.yaml` | New | Release | Public structured impact | Created in owning activation phase |
| `docs/releases/changes/CHG-01M1ETX06HRETFBJTK6SCZGBZ6.yaml` | New | Release | Bootstrap-state migration impact | Created in Phase 3 |
| `docs/releases/changes/CHG-01M1ETXMS84KS8ASDW4GR22Q3J.yaml` | New | Release | Provider identity replacement impact | Created in Phase 5 |

## Key Decisions

1. **Pending first-auth claim, not startup completion:** authentication proof
   precedes role grant and completion, preventing typo lockout.
2. **Manifest identity exclusion:** manifest supplies portable configuration;
   deployment-local authority supplies the administrator selector.
3. **Exact provider identity:** Keycloak issuer plus `sub`; ATProto verified
   DID. Email, username, handle, display name, provider role, and arrival order
   are forbidden.
4. **Authority-qualified provider account key:** replace raw realm-scoped keys
   without legacy readers.
5. **One Application transaction:** identity, actor, login, roles, tenant,
   settings, generation, and completion commit together.
6. **No new write route:** Keycloak uses authenticated SyncUser; ATProto uses
   its verified-session handler.
7. **Closed BFF state:** interactive pending, configured pending, completed,
   invalid; unknown fails closed.
8. **Runtime activation last:** phases 1–9 remain dormant; Phase 10 registers
   environment-backed authority and ships docs/release evidence.
9. **Setup authority finality:** setup secret remains recoverable while pending
   and locks only after committed completion.
10. **No automatic transfer:** configuration cannot revoke or transfer
    administrator authority after completion.

## Constraints And Rules To Remember

- Tier 1 requires adversarial state/race/security tests and anonymized weighted
  MAD review.
- Use advanced model capability for implementation/review.
- Use graph tools first if they become available; otherwise preserve the
  source evidence ledger.
- Repositories return entities; validators are manually instantiated.
- Tokens stay server-side in the BFF.
- UI never authorizes from roles/claims; no HAL change is planned.
- Current tenant/user authority never comes from request bodies.
- Raw subject, DID, issuer, email, fingerprint, token, and secret values are
  forbidden in diagnostics and public contracts.
- Generated migrations/snapshots/OpenAPI/client/catalogue are never hand
  edited.
- Tests use exact signals and bounded timeouts, not sleeps or polling.
- Internal repositories/handlers are not mocked.
- Backward compatibility is rejected; no aliases, dual readers, or fallback
  paths.
- Shared-tree unrelated paths and hunks remain untouched.

## Validation Baseline

- **Planning verification:** Markdown/diff/link checks only, per repository
  docs-only policy.
- **Product baseline:** established before Phase 1 and recorded in the
  execution ledger.
- **Per-phase implementation policy:** one Release build and the bounded
  affected test set once after all phase tasks, followed immediately by the
  exact phase-owned commit. Phase 2 is cross-layer and runs every affected
  project plus AssuranceAudit.
- **Phase 1:** Release build had one proven unrelated agent-workflow CS0122 and
  zero attributable errors; Setup Core tests passed 72/72; commit
  `5896449f3ae7f78f302cc8f4d85e29574f74a2a5`.
- **Closure:** final solution Release build passed with zero errors; exact
  changed test surface passed AssuranceAudit with zero diagnostics.
- **Tier 1 evidence path:**
  `.omo/evidence/20260901-headless-instance-onboarding/`

## Current Known Risks / Unknowns

- ATProto exact-DID claim must compose with verified-session persistence and
  token issuance without moving either before commit.
- Several planned shared paths currently have unrelated changes; path-level
  ownership may not be enough if files contain mixed hunks.
- EF migration timestamps are generated during Task 3.2; restrictive named
  pathspecs are planned, and exact generated paths must be recorded before
  staging.
- Authority-qualified provider-key replacement has a broad caller set and no
  compatibility fallback; all callers must migrate atomically.
- Operator comprehension of pending recovery has no stakeholder evidence yet;
  implementation documentation must be explicit and value-free.

## Handoff Notes

### Closure Handoff — 2026-09-01 Europe/Brussels

- **Current state:** all ten phases implemented and verified.
- **Implementation commits:** `5896449f3`, `02e024a3e`, `584be9624`.
- **Remaining action:** record MAD disposition, commit final verified
  identity/BFF corrections plus synchronized closure artifacts, then close the
  active goal.
- **Ownership:** unrelated Setup Assistant, workflow, and release-fragment
  work remains outside this workstream and must not be staged.

### Handoff — 2026-09-01 Europe/Brussels

- **Current state:** Planning and I-VSD are complete; no runtime implementation
  has started.
- **Next action:** User review/approval, then one-time baseline and Task 1.1.
- **Blockers:** Implementation approval and shared-tree ownership conflicts.
- **Modified files:**
  - `islamic-value-sensitive-design/i-vsd-headless-instance-onboarding.md`
  - `dev/active/headless-instance-onboarding/headless-instance-onboarding-plan.md`
  - `dev/active/headless-instance-onboarding/headless-instance-onboarding-tasks.md`
  - `dev/active/headless-instance-onboarding/headless-instance-onboarding-context.md`
- **Validation:** Planning-only Markdown/diff/link checks; no product build or
  tests by policy.
- **Documentation impact:** Plan identifies exact configuration, manifest,
  secrets, self-hosting, troubleshooting, operations, backup/restore, schema,
  API changelog/inventory, and release-fragment updates.
- **Risks:** ATProto transaction ordering, Keycloak authority qualification,
  concurrent claims, identity privacy, and shared-tree collisions.
- **Notes for next contributor/agent:** Do not reinterpret the user-approved
  Setup Assistant boundary. It generates offline artifacts only and never
  connects to the instance.
