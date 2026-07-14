<!-- ABOUTME: Reconciles broad API regression failures observed while verifying inbound webhook Wave 3. -->
<!-- ABOUTME: Separates webhook-owned evidence from protected unrelated failures in the shared dirty worktree. -->

# Webhook Wave 3 API Baseline Reconciliation

Verified: 2026-07-14 Europe/Brussels

The full API project executed 1,762 tests: 1,751 passed, 8 failed, and 3 were skipped.
The failures are outside the webhook implementation and remain unmodified:

| Failure | Evidence | Attribution |
|---|---|---|
| Public GET smoke | `/api/management/capabilities` returned 404 | Management routing; test and route surface are clean in the webhook diff |
| Instance-admin control plane | Tenant list returned 403 instead of 200 | Control-plane authorization; test and policy surface are clean in the webhook diff |
| Two production authorization guardrails | Disposed `IServiceProvider` while resolving the in-memory test DbContext | Test-factory lifecycle; fixture and infrastructure registration are clean in the webhook diff |
| Two event-registration runtime tests | HTTP 500 from missing TickerQ relation/notification lookup fixture state | Registration/runtime fixture; source and tests are clean in the webhook diff |
| Cerbos generic resource matrix | Regular user expected allow for `islamuevent_instance_setting`, policy returned deny | Instance-setting policy contract; webhook change adds a separate method and touches only `islamuevent_webhook` policy |
| Resolver-config governance route | Regular user expected 403, route returned 200 | Existing instance-setting HTTP authorization contract; opposite direction from the generic Cerbos assertion |

Focused webhook API redrive tests, Local authorization, and the live Cerbos webhook
machine-scope parity test pass. No unrelated test, controller, fixture, policy, or runtime
registration was changed to force the broad project green.

This reconciliation permits webhook implementation to proceed, but the full API suite is
reported as baseline-red until the owning workstreams resolve these eight failures.
