## Technical Insights
- [2026-02-15 21:28 Europe/Brussels] Runtime provider selection should be centralized in `RuntimeAnalyticsProvider`; concrete provider `IsActive(...)` checks should validate local prerequisites (enabled/key presence) but avoid provider-id coupling to prevent stale-cache/provider mismatch behavior.
- [2026-02-15 21:28 Europe/Brussels] Public bootstrap payload must compute analytics readiness (`enabled && providerId > 0 && apiKey present`) to prevent first-load UI script churn and no-op/fail races.

## Architectural Decisions
- [2026-02-15 21:28 Europe/Brussels] Keep analytics provider abstraction thin (`Identify`, `Track`, `PageView`, `GroupIdentify`) and isolate feature flags via a separate capability interface with safe defaults.
- [2026-02-15 21:28 Europe/Brussels] JS analytics bridge enforces no-op initialization when API key is empty, independent of provider flag, to preserve graceful degradation.

## Failed Approaches
- [2026-02-15 21:28 Europe/Brussels] Attempted to filter TUnit tests via standard `--filter` flow; this runner uses different option handling and rejected the argument. Use project runs and targeted suite partitioning instead.

## Deferred Fixes
- [2026-02-15 21:28 Europe/Brussels] Add CSP documentation and validation for analytics script hosts (PostHog/Plausible/RudderStack) before production rollout.
- [2026-02-15 21:28 Europe/Brussels] Add integration tests for runtime provider switch SLA (within 60s cache window) and UI-level graceful degradation checks.

## Technical Insights
- [2026-02-16 01:55 Europe/Brussels] In Blazor `InteractiveAuto`, components in client assembly can be instantiated during server prerender; any injected service must exist in server DI too. Added server no-op `IAnalyticsInterop` implementation to prevent prerender resolution failures.
