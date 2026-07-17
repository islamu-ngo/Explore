ABOUTME: Canonical entry point for authentication, authorization, and trust-boundary documentation.
ABOUTME: Points readers to the maintained security model while preserving documentation index links.

# Security

> **Audience:** Operators | Contributors | AI agents
> **Status:** Mixed
> **Owner:** Security
> **Last Verified:** 2026-05-12
> **Source Anchors:** `docs/SECURITY-MODEL.md`, `Explore.Blazor/Extensions/YarpProxyExtensions.cs`, `Explore.Blazor/Services/CircuitTokenStore.cs`, `Explore.Blazor/Extensions/BffStorageEndpoints.cs`, `Explore.Blazor/Extensions/BffPreferenceEndpoints.cs`

The maintained security model lives in [SECURITY-MODEL.md](SECURITY-MODEL.md).

Use that document for the current BFF trust boundaries, token-forwarding model, antiforgery contract, auth diagnostic safety, support-access boundary, upload-session binding, and circuit token lifecycle notes.

The approved lifecycle-email plan adds dispatch-time privacy authorization without changing SMTP into an authority. Email may target only a current verified tenant member or an authorization-bound same-tenant managed tenant-administrator invitation. Incompatible pre-1.0 delivery ledgers are reset instead of receiving a synthetic legacy authority; inbox notifications and unrelated business/audit/settings data remain preserved. Consent, preference, disclosure, deletion, and supersession may narrow a snapshotted policy before provider handoff, never broaden it.
