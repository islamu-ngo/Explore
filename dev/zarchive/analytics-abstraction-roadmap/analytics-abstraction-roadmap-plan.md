ABOUTME: Strategic roadmap for hardening the existing analytics abstraction for self-hostable deployments.
ABOUTME: Focuses on provider capability tiers, privacy-safe defaults, transport reliability, and operator-facing rollout guidance.

# Analytics Abstraction Roadmap - Plan

Last Updated: 2026-03-08

## Executive Summary

This roadmap no longer treats analytics abstraction as greenfield work. The repository already contains a working runtime-selectable analytics layer for PostHog, Plausible, Rybbit, RudderStack, and None across Application, Infrastructure, and Blazor. The real work now is to harden that abstraction for a self-hostable open-source platform that many operators will deploy under very different constraints: some will disable analytics entirely, some will use lightweight privacy-first web analytics, and some will want richer product analytics behind reverse proxies and stricter compliance controls.

The roadmap therefore shifts from "build provider abstraction" to "make the existing abstraction safe, explicit, operator-friendly, and honest about capability differences." The highest priorities are: fix configuration drift, define the supported capability tiers, introduce privacy/consent defaults that work for self-hosters, add proxy/CSP/ad-blocker guidance, and only then chase deeper provider parity.

## Verified Current State

### Existing implementation already in repo

- Domain and governance:
  - `Explore.Domain/AnalyticsProvider.cs`
  - `Explore.Domain/Enums/AnalyticsProviderEnum.cs`
  - `Explore.Domain/Settings/Definitions/AnalyticsSettingDefinitions.cs`
  - `Explore.Domain/Constants/GovernanceSettingKeys.cs`
- Application contracts and models:
  - `Explore.Application/Contracts/Infrastructure/IAnalyticsProvider.cs`
  - `Explore.Application/Contracts/Infrastructure/IAnalyticsFeatureFlagProvider.cs`
  - `Explore.Application/Contracts/Infrastructure/IAnalyticsConfigResolver.cs`
  - `Explore.Application/Models/AnalyticsConfiguration.cs`
  - `Explore.Application/Settings/Groups/AnalyticsSettingGroup.cs`
- Infrastructure:
  - `Explore.Infrastructure/Analytics/RuntimeAnalyticsProvider.cs`
  - `Explore.Infrastructure/Analytics/AnalyticsConfigResolver.cs`
  - `Explore.Infrastructure/Analytics/PostHogAnalyticsProvider.cs`
  - `Explore.Infrastructure/Analytics/PlausibleAnalyticsProvider.cs`
  - `Explore.Infrastructure/Analytics/RybbitAnalyticsProvider.cs`
  - `Explore.Infrastructure/Analytics/RudderStackAnalyticsProvider.cs`
  - `Explore.Infrastructure/Analytics/NullAnalyticsProvider.cs`
  - `Explore.Infrastructure/InfrastructureServicesRegistration.cs`
- Blazor bootstrap and interop:
  - `Explore.Blazor.Client/Shared/AnalyticsInitializer.razor`
  - `Explore.Blazor.Client/Contracts/Interop/IAnalyticsInterop.cs`
  - `Explore.Blazor.Client/Services/AnalyticsInterop.cs`
  - `Explore.Blazor.Client/wwwroot/js/analytics-bridge.js`
  - `Explore.Blazor/Services/ServerAnalyticsInterop.cs`
- Test coverage:
  - `Event.Application.UnitTests/Infrastructure/RuntimeAnalyticsProviderTests.cs`
  - `Event.Application.UnitTests/Infrastructure/AnalyticsConfigResolverTests.cs`
  - `Event.Application.UnitTests/Infrastructure/AnalyticsProviderEdgeCaseTests.cs`
  - `Event.Application.UnitTests/Infrastructure/NullAnalyticsProviderTests.cs`
  - `Explore.Blazor.Client.Tests/Components/AnalyticsInitializerTests.cs`

### Verified provider capability snapshot

| Provider | Track | Identify | PageView | GroupIdentify | Feature Flags | Current status |
|---|---|---|---|---|---|---|
| `None` | Yes (no-op) | Yes (no-op) | Yes (no-op) | Yes (no-op) | Safe defaults | Production-safe fallback |
| `PostHog` | Yes | Yes | Yes | Yes | Yes | Most complete provider |
| `Plausible` | Yes | No-op | Yes | No-op | No | Intentionally thin web analytics tier |
| `Rybbit` | Yes | No-op | Yes | No-op | No | Partial implementation despite richer client surface |
| `RudderStack` | Yes | Yes | Yes | Yes | No | Implemented in code, but rollout expectations not yet validated in roadmap docs |

### Verified repo-aligned constraints

1. Runtime config follows the standard governance cascade: system setting -> tenant override -> default fallback.
2. Instance admins can lock settings; tenants can BYO provider only when settings are unlocked.
3. Analytics is already treated as non-critical infrastructure: provider failures are caught, logged, and swallowed.
4. Blazor prerender requires a server-side no-op interop implementation, already present in `ServerAnalyticsInterop`.
5. Public bootstrap payload already computes analytics readiness before initializing the client bridge.
6. The localization/TMS system is the closest mature pattern for multi-provider, self-hoster-tiered external service abstraction in this repo.

## Confirmed Gaps

1. `AnalyticsSettingGroup` previously used legacy keys (`analytics.endpoint`, `analytics.site_id`); canonical governance keys are `analytics.endpoint_url` and no `site_id` key exists.
2. The roadmap currently over-emphasizes provider parity and under-emphasizes self-hosting realities like reverse proxies, CSP, ad blockers, first-party routing, and no-analytics deployments.
3. No canonical event taxonomy, event ownership policy, or property allowlist exists yet.
4. No explicit consent model or privacy mode contract exists, even though the UI already signals future analytics/privacy controls.
5. Browser transport assumptions are under-specified: direct script loading, proxied first-party loading, and server-relay fallback are not distinguished.
6. Operator docs are missing for CSP headers, reverse proxy patterns, disable switches, endpoint requirements, and rollout troubleshooting.
7. Integration tests verify routing and degradation but do not yet validate realistic provider transport scenarios or self-hosted endpoint assumptions.

## External Research That Changes Priorities

### PostHog

- Reverse proxying is recommended to reduce tracking-blocker losses.
- Self-hosted and proxied deployments require explicit `api_host` plus `ui_host` handling.
- CSP and proxy requirements are non-trivial and must be documented rather than inferred.

### Plausible

- Direct script installs can lose a meaningful portion of traffic to ad blockers.
- Plausible supports server-side tracking through the Events API.
- Proxying through first-party paths is an important accuracy and operability feature.
- Plausible's custom properties are intentionally constrained and must not include PII.

### Rybbit

- The script and proxy docs show a richer client surface than the current provider implementation uses.
- Proxying through first-party paths is a first-class pattern, not an optional afterthought.
- Session replay, metrics, and identify-style endpoints increase capability but also increase privacy and infrastructure burden.

### Cross-provider implication

The abstraction must optimize for honest capability tiers, privacy-safe defaults, and first-party/proxied transport. It should not pretend every provider supports the same semantics.

## Target Future State

### Product and architectural goals

1. Analytics remains optional infrastructure, never a platform dependency.
2. Operators can choose among disabled, lightweight, and richer analytics modes without code changes.
3. The abstraction exposes a lowest-common-denominator core contract and clearly documents advanced capability opt-ins.
4. Default deployments are privacy-safe: no accidental PII leakage, no hidden identify semantics, and clear consent hooks.
5. Client-side transport supports direct, proxied, and degraded/no-op modes explicitly.
6. Self-hosters get concrete operator guidance for reverse proxying, CSP, endpoint exposure, and troubleshooting.
7. OpenTelemetry and business metrics remain separate from tenant/operator analytics.

### Self-hoster tiers to optimize for

| Tier | Operator posture | Expected analytics mode | Roadmap implication |
|---|---|---|---|
| Tier 0 | Privacy-first or no need | Disabled / `None` | Must be first-class and frictionless |
| Tier 1 | Lightweight self-hosting | Web analytics only (`Plausible`-style) | Prioritize pageview/custom event support and low operational burden |
| Tier 2 | Product analytics | `PostHog`, `Rybbit`, or `RudderStack` | Support richer event/identify/group semantics where verified |
| Tier 3 | Accuracy-sensitive operators | First-party proxied analytics | Ship reverse proxy/CSP/runbook guidance and fallback patterns |

### Capability model to make explicit

| Capability tier | Required? | Notes |
|---|---|---|
| `track` | Yes | Core contract across all non-null providers |
| `pageview` | Yes | Core contract across all web-facing providers |
| `identify` | Optional | Disabled by default in privacy-safe mode unless provider + policy allow it |
| `groupIdentify` | Optional | Useful for tenant/org analytics, not assumed everywhere |
| `featureFlags` | Optional | PostHog only today |
| `sessionReplay` / advanced client features | Deferred/explicit | Never implied by the base abstraction |

## Roadmap Principles

1. **Harden before expanding**: fix correctness and operator clarity before adding new provider features.
2. **Privacy-safe by default**: zero-cookie or pseudonymous mode should be the baseline expectation.
3. **Proxy-first for client analytics**: design around first-party/proxied delivery instead of assuming direct vendor script loads.
4. **Capability honesty**: expose optional capabilities explicitly; do not simulate parity where none exists.
5. **Keep business flows clean**: analytics never blocks command/query paths.
6. **Governance-first rollout**: instance lock, tenant override, and kill-switch behavior must be obvious and testable.

## Phased Implementation Plan

### Phase 0 - Re-baseline the Existing Abstraction

- Scope:
  - Finish naming/config drift cleanup around canonical analytics keys.
  - Publish a verified capability matrix and self-hoster-tier framing.
  - Reclassify current implementation as "existing foundation" rather than planned net-new work.
- Acceptance criteria:
  - All analytics configuration references use canonical keys.
  - Plan/docs explicitly distinguish disabled, lightweight, and full analytics modes.
  - RudderStack and Rybbit expectations are called out as verified vs not-yet-validated.
- Effort: 0.5-1 day.

### Phase 1 - Event Contract, Privacy, and Governance Baseline

- Scope:
  - Define event taxonomy ownership, naming, and property governance.
  - Introduce an allowlist/denylist approach for event properties.
  - Define privacy/consent modes for anonymous, pseudonymous, and identified analytics.
- Acceptance criteria:
  - Canonical event catalog exists for server and client emitters.
  - PII/consent rules are documented and reflected in the abstraction contract.
  - Identify/group calls are policy-gated rather than assumed globally valid.
- Effort: 1-1.5 days.

### Phase 2 - Transport and Self-Hosting Hardening

- Scope:
  - Add explicit transport modes: direct vendor endpoint, first-party proxy, server relay fallback.
  - Document CSP, reverse proxy, and ad-blocker mitigation expectations.
  - Define failure strategy for blocked scripts and unavailable endpoints.
- Implementation status (2026-03-08): implemented in code/docs with relay path `POST /api/a/t`, dedicated relay rate limiting, bootstrap readiness for relay-without-public-key, and documented direct/proxy/relay degradation behavior.
- Acceptance criteria:
  - Operators have concrete setup guidance for proxied first-party delivery.
  - Client initialization documents what happens when script load fails or CSP blocks external code.
  - Server relay fallback is explicitly planned for operators who cannot or do not want client script loading.
- Effort: 1-2 days.

### Phase 3 - Provider Capability Hardening

- Scope:
  - Keep PostHog as the richest validated provider.
  - Preserve Plausible as a deliberate basic web analytics tier.
  - Validate and extend Rybbit only where external docs and actual behavior support it.
  - Reassess RudderStack positioning before promising parity in operator docs.
- Research status (2026-03-08): validated that PostHog is the only rich provider in this set with native feature flags and documented server/browser parity; Plausible intentionally excludes identify/group semantics; Rybbit docs justify browser event/pageview/identify but not server/group/flag parity; RudderStack supports the broader event spec but behaves more like a CDP/router than a first-party analytics backend.
- Acceptance criteria:
  - Provider docs and tests align with the real supported capability surface.
  - Unsupported operations are clearly documented as no-op/safe-default behavior.
  - Provider-specific expectations are reflected in the capability matrix.
- Effort: 1.5-2.5 days.

### Phase 4 - Blazor, BFF, and Server Event Integration

- Scope:
  - Align public bootstrap, client interop, and server-side emitters with the new contract.
  - Clarify correlation rules for client pageviews vs server business events.
  - Add explicit initialization behavior for disabled, proxied, blocked, and consent-denied states.
- Acceptance criteria:
  - Client and server event responsibilities are documented and testable.
  - `AnalyticsInitializer` and JS bridge degrade safely across all transport modes.
  - Privacy policy and settings surface can evolve from the documented contract rather than ad hoc behavior.
- Implementation status (2026-03-08): completed with pageview ownership in `AnalyticsInitializer`, automatic normalized-path pageviews on initial load and route changes, verified Blazor component coverage for initialization/disabled-state/navigation behavior, and documented client-vs-server event boundaries plus privacy-UX contract notes.
- Effort: 1-1.5 days.

### Phase 5 - Reliability, Ops, and Rollout

- Scope:
  - Add operator runbooks, kill-switch procedures, and rollout validation steps.
  - Decide whether buffered/outbox delivery belongs in the first hardening pass or a follow-up milestone.
  - Add integration and smoke-test coverage around runtime switching and transport failure scenarios.
- Acceptance criteria:
  - Operators can disable analytics globally or per tenant without redeploying.
  - A runbook exists for provider switch, script failure, proxy misconfiguration, and CSP blocking.
  - Deferred items are clearly marked instead of being implicitly included.
- Decision status (2026-03-08): buffered/outbox delivery is deferred to a follow-up milestone. The current hardening pass keeps browser telemetry and server analytics best-effort, with kill-switch and first-party relay/proxy runbooks documented for operators who need fast rollback without turning analytics into a transactional dependency.
- Effort: 1-2 days.

## Detailed Workstreams

### Workstream A - Configuration Correctness

- Align `AnalyticsSettingGroup` with canonical governance keys.
- Audit batch/group-loading code paths for stale key names.
- Reconcile old roadmap language that still treats provider abstraction as net-new work.

### Workstream B - Capability and Taxonomy Governance

- Define a canonical `AnalyticsEvent` or equivalent event catalog.
- Separate basic web analytics from richer product analytics semantics.
- Introduce explicit rules for property naming, cardinality, and PII exclusion.

### Workstream C - Privacy and Consent Defaults

- Define default anonymous or pseudonymous mode.
- Make identify/group semantics opt-in, not silently assumed.
- Tie future user-facing privacy controls to concrete backend/client behavior.

### Workstream D - Self-Hosting Transport

- Document first-party proxy patterns for PostHog, Plausible, and Rybbit.
- Capture CSP requirements, path naming cautions, header forwarding, and HTTPS assumptions.
- Add a server-relay path to keep lightweight self-hosters viable even when browser script loading is undesirable.

### Workstream E - Provider Hardening

- Keep PostHog feature flags isolated as an optional capability.
- Treat Plausible's intentionally limited surface as a feature, not a bug.
- Validate whether Rybbit identify/group APIs should be implemented or intentionally documented as unsupported.
- Decide whether RudderStack remains first-class or becomes documented as advanced/experimental until stronger validation exists.

### Workstream F - Testing and Rollout Evidence

- Add tests around key mismatch fixes, capability gating, and transport-mode selection.
- Add smoke coverage for blocked/failed client initialization and disabled-provider bootstrap.
- Add operator verification checklists for self-hosted deployment modes.

## Risks and Mitigations

| Risk | Why it matters | Mitigation |
|---|---|---|
| Capability over-promising | Self-hosters expect parity that providers do not offer | Publish explicit capability matrix and safe-default behavior |
| Privacy regressions | Open-source operators may deploy in strict legal contexts | Default to privacy-safe mode and document identify as opt-in |
| Ad-blocker/CSP data loss | Direct script loads are often blocked | Prioritize proxy-first guidance and server-relay fallback |
| Operator burden | Heavy analytics stacks raise the self-hosting floor | Keep disabled and lightweight modes first-class |
| Config drift | Legacy key names already exist | Fix canonical keys before deeper implementation work |
| Testing blind spots | Current tests focus more on routing than deployment realism | Add transport and rollout verification scenarios |

## Success Metrics

1. Canonical analytics keys resolve consistently across system and tenant scope.
2. The roadmap and tasks explicitly describe disabled, lightweight, and richer analytics deployment modes.
3. Provider docs/tests accurately reflect supported capabilities without hidden assumptions.
4. Operators get reverse proxy, CSP, and disable/runbook guidance before provider expansion work is declared complete.
5. Client and server analytics degrade safely when scripts are blocked, endpoints fail, or analytics is disabled.

## Deliberate Non-Goals For This Hardening Pass

1. Replacing OpenTelemetry or business metrics with tenant analytics.
2. Forcing deep feature parity across all providers.
3. Shipping session replay, surveys, or other high-burden client features as part of the base abstraction contract.
4. Making analytics mandatory for any deployment mode.

## External Sources Used For Priority Shifts

- PostHog JS docs: `https://posthog.com/docs/libraries/js`
- PostHog proxy/self-host guidance: `https://posthog.com/docs/advanced/proxy/proxy-reference`
- PostHog self-host docs: `https://posthog.com/docs/self-host`
- PostHog proxy runtime requirements: `https://posthog.com/docs/self-host/configure/running-behind-proxy`
- Plausible proxy guidance: `https://plausible.io/docs/proxy/introduction`
- Plausible Events API: `https://plausible.io/docs/events-api`
- Plausible custom properties guidance: `https://plausible.io/docs/custom-props/introduction`
- Rybbit script docs: `https://rybbit.com/docs/script`
- Rybbit proxy docs: `https://rybbit.com/docs/proxy-guide`
- Rybbit architecture docs: `https://rybbit.com/docs/architecture`
- Rybbit self-hosting docs: `https://rybbit.com/docs/self-hosting`

## Overall Estimate

- Planning and hardening work represented here: 5-8 engineering days for one engineer, including tests and operator docs.
- If buffered/outbox delivery becomes part of the same milestone instead of a follow-up: add 1.5-2.5 days.

## Final Planning Position

The abstraction exists. The remaining challenge is making it trustworthy for a heterogeneous self-hosting ecosystem. That means configuration correctness, privacy defaults, transport realism, and operator guidance come before any claim of provider parity.
