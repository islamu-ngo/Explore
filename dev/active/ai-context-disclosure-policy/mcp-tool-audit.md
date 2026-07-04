<!-- ABOUTME: MCP tool disclosure audit for AI context responses and prompt-adjacent data. -->
<!-- ABOUTME: Records gateway, consent, provider trust, and redaction expectations for MCP-facing AI tools. -->

# AI Context Disclosure Policy - MCP Tool Audit

Last Updated: 2026-07-04 Europe/Brussels

## Purpose

This audit tracks MCP tools that can expose AI assistant context, prompt-adjacent data, or sanitized platform references. It is intentionally conservative: tools must route through `IAiContextGateway` before exposing any data derived from `*Pii` entities.

## Current Posture

| Area | Expected boundary | Status |
|---|---|---|
| AI assistant MCP tools | Use sanitized envelopes from `IAiContextGateway`; no raw `*Pii` entity dependencies. | Guarded by `AiContextGatewayBypassTests`. |
| Provider trust metadata | Resolve from evidence and treat `Unknown` as most restrictive. | Required by ADR-012. |
| Consent metadata | Include consent/denial state for PII fields once Phase 4 enables disclosure. | Not enabled yet. |
| Raw prompt/transcript data | Do not expose confidential/restricted/special values without gateway approval. | Phase-gated. |

## Tool Review Checklist

For every new or changed MCP tool:

- [ ] Name the source handler/tool file.
- [ ] Identify every field that can be disclosed.
- [ ] Confirm `IAiContextGateway` is used for any `*Pii`-derived data.
- [ ] Confirm provider trust tier is explicit and fail-closed.
- [ ] Confirm missing consent denies PII fields.
- [ ] Confirm logs, telemetry, and errors do not contain raw PII, prompts, provider bodies, or secrets.
- [ ] Add or update API/Application/architecture tests for the changed boundary.

## Current Audit Notes

- `Explore.API/Mcp/**` remains under architecture-test enforcement for direct PII references.
- PII disclosure remains disabled by default; MCP tools should expose only `Public` and `Internal` fields unless the Phase 4 gate is completed and enabled.
