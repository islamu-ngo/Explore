<!-- ABOUTME: Task checklist for maintaining the AI Context Disclosure Policy workstream. -->
<!-- ABOUTME: Tracks matrix, gateway, MCP, consent, and architecture-test verification work. -->

# AI Context Disclosure Policy - Task Checklist

Last Updated: 2026-07-04 Europe/Brussels

## Status Summary

- [x] Restore active workstream files referenced by `update-ai-context-disclosure`.
- [x] Recreate the field matrix from `AiContextDisclosureRegistry.CreateDefault()`.
- [ ] Reconcile `docs/AI_CONTEXT_SECURITY.md` summary counts with the restored matrix if they drift.
- [ ] Keep `mcp-tool-audit.md` updated when MCP tools change.

## Phase 1 - Registry And Matrix

- [x] Classify every persisted public property on `UserPii`, `OrganizationPii`, `ActorPii`, and `LocationPii`.
- [x] Exclude navigation properties from registry and matrix rows.
- [ ] Add a matrix row and registry entry before adding any new `*Pii` property.
- [ ] Run `Event.Architecture.Tests` after any field classification change.

## Phase 2 - Gateway And Consent

- [ ] Keep AI prompt construction behind `IAiContextGateway`.
- [ ] Keep consent unable to override instance or tenant deny.
- [ ] Keep provider trust tier `Unknown` as the most restrictive outcome.

## Phase 3 - Bypass Prevention

- [ ] Keep `AiContextGatewayBypassTests` green for `Features/AiAssistant/**`.
- [ ] Keep `AiContextGatewayBypassTests` green for `Explore.API/Mcp/**`.
- [ ] Avoid widening DTOs or repositories as an alternative to gateway envelopes.

## Phase 4 - Enablement Gate

- [ ] Verify transcript persistence max-sensitivity behavior.
- [ ] Verify log redaction for confidential/restricted/special values.
- [ ] Verify deletion and revocation propagation to AI transcript context.
- [ ] Enable `PiiDisclosureEnabled` only after the preceding tasks pass.

## Phase 5 - MCP Tool Audit

- [ ] Record every MCP tool that can expose AI context in `mcp-tool-audit.md`.
- [ ] Record disclosed fields, denied fields, consent behavior, and provider trust tier for each tool.
- [ ] Ensure MCP responses never return raw PII outside the gateway envelope.

## Verification Checklist

- [ ] `dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet`
- [ ] `rg -n "field-classification-matrix|ai-context-disclosure-policy-plan" docs .claude dev/active`
