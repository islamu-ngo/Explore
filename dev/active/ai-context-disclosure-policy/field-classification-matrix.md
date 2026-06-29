<!-- ABOUTME: Authoritative field-level PII classification matrix for the AI Context Disclosure Policy. -->
<!-- ABOUTME: Every persisted public property on every *Pii entity is classified here; unclassified = Deny. -->

# AI Context Disclosure — Field Classification Matrix

**Status:** Authoritative input for Phase 1 Task 1.3 (`AiContextDisclosureRegistry`).
**Last Updated:** 2026-06-28
**Owner:** ISLAMU Event Platform — AI Context Disclosure workstream.

---

## 1. Purpose

This matrix is the **single source of truth** for the sensitivity classification of every persisted public property on every `*Pii` extension entity in `Explore.Domain`. It is consumed verbatim by:

- `AiContextDisclosureRegistry` (Task 1.3) — machine-readable seed.
- `Event.Architecture.Tests/AiContextDisclosureSchemaTests` (Task 1.5) — reflection-driven completeness check that fails the build if any `*Pii` property is missing a classification.
- `docs/AI_CONTEXT_SECURITY.md` (Task 1.4) — human-readable policy.

**Default rule:** Any `*Pii` property not listed here is `Deny`. Adding a new property without adding a row is a build failure (Task 1.5).

## 2. PII Entity Inventory (Verified)

`Explore.Domain/**/*Pii*.cs` resolves to exactly four entities, all using a **1:1 shared-primary-key** pattern with their parent aggregate so the PII rows can be hard-deleted independently of the soft-deletable parent:

| Entity | Parent Aggregate | Pattern | Verified |
|---|---|---|---|
| `UserPii` | `User` | `UserId (Guid) → User` | ✅ |
| `OrganizationPii` | `Organization` | `OrganizationId (Guid) → Organization` | ✅ |
| `ActorPii` | `Actor` | `ActorId (Guid) → Actor` | ✅ |
| `LocationPii` | `Location` | `LocationId (Guid) → Location` | ✅ |

No other `*Pii` entities exist in the domain. Adding a fifth entity requires adding a section here AND extending the registry seed AND updating the reflection test allowlist (Task 1.5).

## 3. Classification Framework

### 3.1 Data Sensitivity Tiers (`AiContextSensitivityEnum`, Task 1.2)

Aligned with **NIST SP 800-122** (PII taxonomy), **GDPR Art. 4(1)** (personal data definition), and **ISO/IEC 27001 A.5.12** (information classification). Five tiers, ordered from least to most restrictive:

| Value | Name | Definition | Examples |
|---|---|---|---|
| `0` | `Public` | Intentionally published; no harm from broad AI disclosure. | Actor `DisplayName`, organization `FullName`. |
| `1` | `Internal` | Non-regulated business metadata; safe for any authenticated/tenant-scoped context. | Jurisdiction (country, city, postcode), opaque foreign keys. |
| `2` | `Confidential` | Sensitive business contact data; not personal PII but not for broad disclosure. | Organization `Email`. |
| `3` | `Restricted` | Regulated personal PII (GDPR Art. 4(1)). Disclosure requires consent + transcript controls. | User `Email`, `FirstName`, `LastName`, physical `Address`, precise geo. |
| `4` | `Special` | GDPR Art. 9 special-category data or credentials/secrets. **Never disclosed to AI**, redacted even in local mode. | None currently present in `*Pii` entities — reserved for future biometric/health fields. |

### 3.2 Disclosure Rules (`AiContextDisclosureRuleEnum`, Task 1.2)

| Value | Name | Semantics |
|---|---|---|
| `0` | `Deny` | Field never reaches the AI prompt. Default for any unclassified field. |
| `1` | `Redact` | Field is disclosed with masking (e.g. `a***@example.com`, city-only for addresses). |
| `2` | `Aggregate` | Field is disclosed only as a count/sum/bin (e.g. geo coordinates → city-level bucket). |
| `3` | `Allow` | Field is disclosed in full, subject to consent + provider-trust + transcript controls. |

### 3.3 Provider Trust Tiers (`AiProviderTrustTierEnum`, Task 1.2 — CTO correction #6)

Evidence-based, **not** naming-based. The least-trusted tier wins when evidence is ambiguous.

| Value | Name | Evidence Required |
|---|---|---|
| `0` | `LocalInProcessOrSameNetworkModel` | Model endpoint resolves to loopback or a same-VPC private address; no egress to public internet. |
| `1` | `TenantControlledPrivateEndpoint` | Tenant-configured endpoint on a private network the tenant controls. |
| `2` | `TenantConfiguredExternalProcessor` | Tenant-configured external provider (e.g. tenant's OpenAI/Azure deployment) with explicit tenant opt-in. |
| `3` | `PlatformConfiguredExternalProcessor` | Platform-default external provider shared across tenants. |
| `4` | `Unknown` | Cannot establish evidence. **Most restrictive** — behaves as `PlatformConfiguredExternalProcessor` plus additional denials. |

### 3.4 Administrative Scope (`AiAdministrativeContextScopeEnum`, Task 1.2 — CTO correction #1)

Instance-admin AI access is **aggregate/redacted only**, scoped to one of:

| Value | Name | Semantics |
|---|---|---|
| `0` | `InstanceAggregate` | Instance-wide counts/totals; never row-level user PII. |
| `1` | `TenantAggregate` | Tenant-scoped counts/totals. |
| `2` | `OperationalDiagnostics` | Operational health (queue depth, error rates); no user content. |

## 4. Field-by-Field Matrix

> **Rule of precedence (policy hierarchy — CTO correction #7):**
> `Instance policy ∩ Tenant policy ∩ User consent`.
> The intersection is enforced. A user consent can NEVER override an instance or tenant deny. The matrix below is the **base** classification; effective disclosure is the intersection.

### 4.1 `UserPii` (5 public persisted properties)

| Property | Type | Sensitivity | Base Rule | Local | Tenant-External | Platform-External | Unknown | Notes |
|---|---|---|---|---|---|---|---|---|
| `UserId` | `Guid` | `Internal` | `Allow` | Allow | Allow | Allow | Allow | Opaque FK; safe as reference. |
| `User` | `User?` | n/a | n/a | n/a | n/a | n/a | n/a | Navigation property — not persisted as data. |
| `Email` | `string (req)` | `Restricted` | `Deny` | Deny¹ | Deny | Deny | Deny | Direct PII (GDPR Art. 4(1)). ¹Allowed only with **explicit owner consent** AND target is `LocalInProcessOrSameNetworkModel`. Phase-4-gated (Task 4.4). |
| `FirstName` | `string (req)` | `Restricted` | `Deny` | Deny¹ | Deny | Deny | Deny | Direct PII. Same consent+Phase-4 gate as `Email`. |
| `LastName` | `string (req)` | `Restricted` | `Deny` | Deny¹ | Deny | Deny | Deny | Direct PII. Same consent+Phase-4 gate as `Email`. |

¹ Per-owner self-disclosure (a user asking the AI about their own account) is **still Phase-4-gated**: requires Task 4.1 (`MaxSensitivity` on persistence), 4.2 (log redaction), 4.3 (deletion propagation) before Task 4.4 enables the flip.

### 4.2 `OrganizationPii` (7 public persisted properties)

| Property | Type | Sensitivity | Base Rule | Local | Tenant-External | Platform-External | Unknown | Notes |
|---|---|---|---|---|---|---|---|---|
| `OrganizationId` | `Guid` | `Internal` | `Allow` | Allow | Allow | Allow | Allow | Opaque FK. |
| `Organization` | `Organization?` | n/a | n/a | n/a | n/a | n/a | n/a | Navigation. |
| `FullName` | `string (req)` | `Public` | `Allow` | Allow | Allow | Allow | Allow | Organization's public display name; intentionally indexed. |
| `Email` | `string?` | `Confidential` | `Deny` | Allow² | Deny | Deny | Deny | Org contact email. ²Local model + org-admin consent permitted in Phase 4. |
| `Country` | `string?` | `Internal` | `Allow` | Allow | Allow | Allow | Allow | Jurisdiction metadata; coarse. |
| `City` | `string?` | `Internal` | `Allow` | Allow | Allow | Allow | Allow | Coarse jurisdiction. |
| `Address` | `string?` | `Restricted` | `Redact` | Redact³ | Deny | Deny | Deny | Physical address. ³Local model redacts to `City, Postcode`; full address requires Phase-4 consent. |
| `Postcode` | `string?` | `Internal` | `Allow` | Allow | Allow | Allow | Allow | Coarse jurisdiction (postal-area granularity). |

### 4.3 `ActorPii` (5 public persisted properties)

| Property | Type | Sensitivity | Base Rule | Local | Tenant-External | Platform-External | Unknown | Notes |
|---|---|---|---|---|---|---|---|---|
| `ActorId` | `Guid` | `Internal` | `Allow` | Allow | Allow | Allow | Allow | Opaque FK. |
| `Actor` | `Actor?` | n/a | n/a | n/a | n/a | n/a | n/a | Navigation. |
| `DisplayName` | `string (req)` | `Public` | `Allow` | Allow | Allow | Allow | Allow | Public-facing actor name; intentionally indexed. |
| `Did` | `string?` | `Public` | `Allow` | Allow | Allow | Allow | Allow | W3C DID — pseudonymous by design (no PII mapping without resolver authority). |
| `Handle` | `string?` | `Public` | `Allow` | Allow | Allow | Allow | Allow | Public handle (e.g. `@handle`); intentionally indexed. |
| `ProfilePictureUri` | `string?` | `Public` | `Allow` | Allow | Allow | Allow | Allow | Public CDN URL. |

### 4.4 `LocationPii` (5 public persisted properties)

| Property | Type | Sensitivity | Base Rule | Local | Tenant-External | Platform-External | Unknown | Notes |
|---|---|---|---|---|---|---|---|---|
| `LocationId` | `Guid` | `Internal` | `Allow` | Allow | Allow | Allow | Allow | Opaque FK. |
| `Location` | `Location?` | n/a | n/a | n/a | n/a | n/a | n/a | Navigation. |
| `Address` | `string (req)` | `Restricted` | `Redact` | Redact³ | Deny | Deny | Deny | Physical address. ³Local redacts to `City, Postcode`. |
| `Postcode` | `string (req)` | `Internal` | `Allow` | Allow | Allow | Allow | Allow | Coarse jurisdiction. |
| `Latitude` | `double?` | `Restricted` | `Aggregate` | Aggregate⁴ | Deny | Deny | Deny | Precise geo. ⁴Local model: bin to city centroid (~1km); raw value requires Phase-4 consent + operational purpose. |
| `Longitude` | `double?` | `Restricted` | `Aggregate` | Aggregate⁴ | Deny | Deny | Deny | Precise geo. Same rule as `Latitude`. |

## 5. Aggregation Count vs Row-Level PII

The disclosure gateway NEVER emits row-level attendee PII. Where the AI needs attendance insight (e.g. the event-context rich retrieval in compressed blocks b1–b12), only the following aggregates are permitted **without** individual consent:

- `EventRegistration` row counts grouped by `ApprovalStatus`.
- Session-level `CurrentAudienceAttendees` / `MaxAudienceAttendees` (already on `EventSession`).
- Event-level totals.

These aggregates are `Internal` sensitivity and follow the same provider-trust gating as other `Internal` fields.

## 6. Admin Boundary (Authoritative Quotes)

Quoted from `docs/SECURITY-MODEL.md` and `docs/AUTHORIZATION.md` so the registry seed and architecture tests stay anchored to the existing authorization contract:

> **`SECURITY-MODEL.md` line 137:** "`failure_mode=closed` -> provider-instance fallback `SafeMode` (deny all except instance admin path)."

> **`SECURITY-MODEL.md` line 153:** "**`derived_roles.yaml`**: Resolves instance admin, tenant admin, and org admin roles from principal attributes and resource context."

> **`SECURITY-MODEL.md` line 153 (cont.):** "Resource policies (`{kind}.yaml`): Each defines rules per derived role and `authenticated_user`. **Instance admin gets wildcard `"*"`**, tenant/org admin get CRUD, authenticated user gets `"view"`."

> **`SECURITY-MODEL.md` line 408:** "Current tenant isolation: EF Core named query filters (`HasQueryFilter(name: "Tenant", ...)`) ... EF tenant filters now **fail closed** when `TenantContext` is missing."

> **`AUTHORIZATION.md` line 124:** "Instance administrators bypass most checks." (Note: CTO correction #1 narrows this for AI flows — instance-admin AI = aggregate/redacted only.)

> **`AUTHORIZATION.md` line 145:** "If the tenant's BYO configuration has `failure_mode=closed`, the fallback provider runs in provider-instance-scoped `SafeMode`, denying all requests except for those from an instance administrator."

> **`AUTHORIZATION.md` line 166:** "Tenant user participation is tenant-local. A global `User` authenticates the person or external identity, but tenant-admin-controlled lifecycle and moderation state lives in `TenantUser`/`TenantUserProfile`."

## 7. Mapping User's Three-Tier Requirement → Existing Auth Model

The original user requirement (compressed block b1) specified three tiers:

| User Tier (requirement) | Maps To (codebase) | AI Disclosure Behavior |
|---|---|---|
| **Public users** | Unauthenticated principal OR authenticated user with no resolved actor context. | Base matrix rules (`Public`/`Internal` allowed; `Confidential`/`Restricted` denied). |
| **Instance administrators** | Cerbos `derived_roles.yaml` resolves `instance_admin` (wildcard `"*"`). | **Aggregate-only** (CTO #1) via `AiAdministrativeContextScope` — never row-level user PII through the general AI assistant. |
| **Event organizers / team** | `AiAssistantActorContextService.ResolveAuthorizedActorAsync` returns the acting `ActorId` (user/org/group). | Owner-scoped disclosure: events where `Event.ActorId == resolvedActorId`. Field-level matrix still applies; owner consent unlocks owner's own `Restricted` data in Phase 4. |

## 8. Phase Gating (CTO Correction #5)

PII disclosure (any field marked `Restricted` or `Confidential` reaching an AI prompt) is **disabled** until Phase 4 completes:

| Phase Gate | Required Tasks | Effect |
|---|---|---|
| Phases 1–3 | Tasks 1.1–3.4 | Only `Public` and `Internal` fields reach AI prompts. `Confidential`/`Restricted` always `Deny`. |
| Phase 4 (PREREQ) | 4.1 (`MaxSensitivity` on persistence) + 4.2 (log redaction) + 4.3 (deletion propagation) | PII disclosure becomes POSSIBLE. |
| Task 4.4 (FLIP) | Gated on 4.1–4.3 verified | PII disclosure ENABLED per matrix + consent + provider trust. |

## 9. Drift Control

- **Adding a new `*Pii` property** without a matrix row → Task 1.5 reflection test fails the build.
- **Changing a sensitivity classification** requires updating this matrix, the registry seed (Task 1.3), and the ADR (Task 1.4).
- **Adding a fifth `*Pii` entity** requires: new section §4.5 here, registry seed extension, reflection allowlist update in Task 1.5.

## 10. Cross-References

- `dev/active/ai-context-disclosure-policy/ai-context-disclosure-policy-plan.md` — strategic plan.
- `dev/active/ai-context-disclosure-policy/ai-context-disclosure-policy-tasks.md` — task checklist.
- `docs/SECURITY-MODEL.md` — runtime authorization providers, Cerbos topology.
- `docs/AUTHORIZATION.md` — endpoint/resource-level authorization, admin hierarchy.
- `Explore.Domain/{UserPii,OrganizationPii,ActorPii,LocationPii}.cs` — source entities.
- Compressed blocks b1–b12 (session history) — original event-context bug research that motivated this workstream.
