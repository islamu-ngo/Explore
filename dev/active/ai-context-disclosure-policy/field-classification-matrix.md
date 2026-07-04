<!-- ABOUTME: Field-level AI disclosure matrix for persisted PII extension entities. -->
<!-- ABOUTME: Mirrors AiContextDisclosureRegistry so AI prompt, transcript, and MCP disclosure stays reviewable. -->

# AI Context Disclosure Policy - Field Classification Matrix

Last Updated: 2026-07-04 Europe/Brussels

## Purpose

This matrix is the human review artifact for `Explore.Application/Features/AiAssistant/Disclosure/AiContextDisclosureRegistry.cs`.
Every persisted public property on the supported `*Pii` entities must appear here and in the registry. Navigation properties are intentionally excluded.

## Policy Rules

- `Public` and `Internal` fields keep their local rule across provider trust tiers.
- `Confidential` and `Restricted` fields use the local rule only for `LocalInProcessOrSameNetworkModel`; all other provider trust tiers downgrade to `Deny`.
- `Special` fields are denied at every provider trust tier.
- `Phase4Gated = true` forces `Deny` until PII disclosure is explicitly enabled after the Phase 4 prerequisites in the plan are complete.
- Unregistered fields fail closed through `AiContextDisclosureRegistry.ResolveEffectiveRule`.

## Summary

| Entity | Persisted classified fields | Public | Internal | Confidential | Restricted | Phase-4 gated |
|---|---:|---:|---:|---:|---:|---:|
| `UserPii` | 4 | 0 | 1 | 0 | 3 | 3 |
| `OrganizationPii` | 7 | 1 | 4 | 1 | 1 | 2 |
| `ActorPii` | 5 | 4 | 1 | 0 | 0 | 0 |
| `LocationPii` | 5 | 0 | 2 | 0 | 3 | 3 |
| **Total** | **21** | **5** | **8** | **1** | **7** | **8** |

## Field Matrix

| Entity | Field | Sensitivity | Local model rule | Phase-4 gated | Rationale |
|---|---|---|---|---|---|
| `UserPii` | `UserId` | `Internal` | `Allow` | No | Opaque foreign key; safe as an uncorrelated reference at every tier. |
| `UserPii` | `Email` | `Restricted` | `Allow` | Yes | Direct PII. Local-model disclosure requires owner self-consent. |
| `UserPii` | `FirstName` | `Restricted` | `Allow` | Yes | Direct PII. Local-model disclosure requires owner self-consent. |
| `UserPii` | `LastName` | `Restricted` | `Allow` | Yes | Direct PII. Local-model disclosure requires owner self-consent. |
| `OrganizationPii` | `OrganizationId` | `Internal` | `Allow` | No | Opaque foreign key. |
| `OrganizationPii` | `FullName` | `Public` | `Allow` | No | Public-facing display name; intentionally indexed. |
| `OrganizationPii` | `Email` | `Confidential` | `Allow` | Yes | Organization contact email. Local-model disclosure requires organization-admin consent. |
| `OrganizationPii` | `Country` | `Internal` | `Allow` | No | Coarse jurisdiction metadata. |
| `OrganizationPii` | `City` | `Internal` | `Allow` | No | Coarse jurisdiction metadata. |
| `OrganizationPii` | `Address` | `Restricted` | `Redact` | Yes | Physical address. Local model redacts to city plus postcode; external tiers deny. |
| `OrganizationPii` | `Postcode` | `Internal` | `Allow` | No | Coarse jurisdiction metadata at postal-area granularity. |
| `ActorPii` | `ActorId` | `Internal` | `Allow` | No | Opaque foreign key. |
| `ActorPii` | `DisplayName` | `Public` | `Allow` | No | Public-facing actor display name; intentionally indexed. |
| `ActorPii` | `Did` | `Public` | `Allow` | No | W3C DID; pseudonymous by design unless external resolver authority links it. |
| `ActorPii` | `Handle` | `Public` | `Allow` | No | Public handle; intentionally indexed. |
| `ActorPii` | `ProfilePictureUri` | `Public` | `Allow` | No | Public CDN or media URL. |
| `LocationPii` | `LocationId` | `Internal` | `Allow` | No | Opaque foreign key. |
| `LocationPii` | `Address` | `Restricted` | `Redact` | Yes | Physical address. Local model redacts to city plus postcode; external tiers deny. |
| `LocationPii` | `Postcode` | `Internal` | `Allow` | No | Coarse jurisdiction metadata. |
| `LocationPii` | `Latitude` | `Restricted` | `Aggregate` | Yes | Precise geo. Local model bins or aggregates; external tiers deny. |
| `LocationPii` | `Longitude` | `Restricted` | `Aggregate` | Yes | Precise geo. Local model bins or aggregates; external tiers deny. |

## Drift Control

- Update this matrix, `AiContextDisclosureRegistry`, and `docs/AI_CONTEXT_SECURITY.md` together when a PII field is added, removed, or reclassified.
- Run `dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet` after any disclosure-policy change.
- Do not set `PiiDisclosureEnabled` to `true` until the Phase 4 persistence, logging, deletion-propagation, and operator-review tasks are complete.
