# Organizer Email Consent — Implementation Plan

> Last Updated: 2026-03-12

## Executive Summary

Add explicit, opt-in, event-scoped email-sharing consent so a registering user may
voluntarily allow a **verified organisation** (the event publisher) to contact them
about future events and related communications.

Key product decisions that shape the implementation:

| Decision | Rationale |
|---|---|
| Consent is **event-level**, not session-level | A user registers for sessions but consents once per event+org pair |
| Checkbox shown **only once** (first-time, unchecked) | After consent is granted, replace with a notice; avoid re-asking |
| Re-show checkbox **only if previously Withdrawn** | Allow the user to reconsent, but never silently re-grant |
| Email stored as **snapshot** at grant time | Organiser always sees the email that was shared; never live PII |
| Only **approved organisations** may receive consents | Validated in business logic; schema is actor-based for extensibility |
| **Connected Apps** page is the single privacy hub | Houses consent management and future OAuth/SSO integrations |
| **Withdrawal warning** is explicit | "Does not guarantee removal from existing email lists" |
| Export tables audit every export event | Who exported what, when, for which org |

---

## Repo-Specific Implementation Summary

From codebase inspection:

| What | Where |
|---|---|
| Event publisher | `Event.ActorId → Actor.OrganizationId → Organization` |
| Approved org check | `Organization.ApprovalStatusId == (int)ApprovalStatusEnum.Approved (2)` |
| Org membership | `OrganizationMember.OrganizationId + UserId + RoleId` |
| User email | `User.Pii.Email` (via `[NotMapped]` wrapper on `User`) |
| Registration handler | `CreateEventRegistrationCommandHandler` (no external service dependencies yet) |
| Authorization model | `[AuthorizeResource]` + Cerbos `org_admin` derived role (resource.attr.organizationId) |
| Permission entity | `Permission.MasterCode` = `"resource_kind:action"` |
| Cerbos principal | `attr.orgMemberships = { "<orgId>": "admin" }` |
| Registration UI | `EventRegistration.razor` (user-facing) + `RegistrationManagerDialog.razor` (admin) |
| Existing unique index | `ix_eventregistrations_session_user` on `(event_session_id, user_id)` |
| No CSV export today | First CSV/TSV export feature in codebase |

---

## Current State

- `EventContactShareConsent` entity: **does not exist**
- Email-sharing checkbox on registration form: **does not exist**
- Connected Apps / Privacy hub page: **does not exist**
- Organiser contacts view/export: **does not exist**
- Relevant permissions: **do not exist**

---

## Proposed Future State

```
Registration Form
  └─ if publisher == approved org AND no prior Granted consent for this event:
       show consent checkbox (unchecked, explicit label naming the org)
  └─ if prior consent == Granted:
       show: "ℹ️ You previously shared your email with [Org Name]."
  └─ if prior consent == Withdrawn:
       show checkbox again (allows re-consent)

Account > Connected Apps (/account/connected-apps)
  └─ table of all granted consents across events
       columns: Organisation, Event, Granted At, Status
       action: Withdraw (per row, with warning modal)
  └─ future: OAuth/SSO grants from Keycloak (deferred)

Organisation > Shared Contacts (/org/{orgId}/contacts)  [org members only]
  └─ list of email snapshots for their events
       filter by event
       export CSV / export TSV buttons
       shows EmailSnapshot, GrantedAt, EventTitle, PurposeCode
```

---

## Implementation Phases

---

### Phase 0 — Pre-flight Baseline

**Goal:** Establish a known-good build and test baseline before touching anything.

#### Task 0.1 — Build baseline
- Run `dotnet build --configuration Release --verbosity quiet`
- Confirm 0 errors (pre-existing errors in CreateEvent.razor re EventSeriesId are known and noted in memories; do not fix in this task)
- **Effort**: S

#### Task 0.2 — Test baseline
- Run each test project individually per `CLAUDE.md`
- Document which tests are currently passing/failing
- **Effort**: S

---

### Phase 1 — Domain Layer

**Layer**: `Explore.Domain`
**Skills**: `clean-architecture-rules`

#### Task 1.1 — Add `ConsentStatus` enum
- **File**: `Explore.Domain/Enums/ConsentStatus.cs`
- Values: `Granted = 1`, `Withdrawn = 2`
- Add `ABOUTME` header (two-line)
- File-scoped namespace
- **Effort**: XS
- **Acceptance criteria**:
  - [ ] File compiles cleanly
  - [ ] Values match spec exactly

#### Task 1.2 — Add `EventContactShareConsent` entity
- **File**: `Explore.Domain/EventContactShareConsent.cs`
- Implements: `ITenantEntity`, `IAuditableEntity` (no `ISoftDeletable` — audit row must never be hard-deleted or soft-deleted; use `Status` field instead)
- **Properties**:

```csharp
public Guid Id { get; set; }
public Guid TenantId { get; set; }
public required Tenant Tenant { get; set; }

public Guid EventId { get; set; }
public required Event Event { get; set; }

public Guid UserId { get; set; }
public required User User { get; set; }

public Guid RecipientActorId { get; set; }
public required Actor RecipientActor { get; set; }

public Guid? SourceEventRegistrationId { get; set; }
public EventRegistration? SourceEventRegistration { get; set; }

public required string PurposeCode { get; set; }          // "ORGANIZER_FUTURE_COMMUNICATIONS"
public ConsentStatus Status { get; set; }

public required string EmailSnapshot { get; set; }          // max 320
public required string EmailNormalizedSnapshot { get; set; } // lower-cased, max 320

public required string ConsentTextSnapshot { get; set; }    // exact text shown to user
public required string ConsentUiVersion { get; set; }       // "v1"

public DateTime GrantedAt { get; set; }
public DateTime? WithdrawnAt { get; set; }

// IAuditableEntity
public DateTime CreatedAt { get; set; }
public Guid? CreatedBy { get; set; }
public DateTime? UpdatedAt { get; set; }
public Guid? UpdatedBy { get; set; }
```

- **No** `IsDeleted` — consent history must be preserved; `Status` is the lifecycle field
- **Effort**: S
- **Acceptance criteria**:
  - [ ] All properties present
  - [ ] Implements correct interfaces
  - [ ] No default values set (set in handler/EF config per CLAUDE.md rule 5)

#### Task 1.3 — Add `EventContactShareExport` audit entity
- **File**: `Explore.Domain/EventContactShareExport.cs`
- Implements: `ITenantEntity`
- **Properties**:

```csharp
public Guid Id { get; set; }
public Guid TenantId { get; set; }
public required Tenant Tenant { get; set; }

public Guid RecipientActorId { get; set; }
public required Actor RecipientActor { get; set; }

public Guid? EventId { get; set; }
public Event? Event { get; set; }

public Guid ExportedByUserId { get; set; }
public required User ExportedByUser { get; set; }

public required string Format { get; set; }   // "csv" or "tsv"
public int RowCount { get; set; }

public DateTime CreatedAt { get; set; }
public Guid? CreatedBy { get; set; }

public ICollection<EventContactShareExportItem>? Items { get; set; }
```

- **Effort**: S

#### Task 1.4 — Add `EventContactShareExportItem` entity
- **File**: `Explore.Domain/EventContactShareExportItem.cs`
- Composite PK: `(ExportId, ConsentId)`
- **Properties**:

```csharp
public Guid ExportId { get; set; }
public required EventContactShareExport Export { get; set; }

public Guid ConsentId { get; set; }
public required EventContactShareConsent Consent { get; set; }

public required string EmailSnapshot { get; set; }   // snapshot copied at export time
```

- **Effort**: XS

#### Task 1.5 — Add `ConsentPurposeCodes` constants
- **File**: `Explore.Domain/Constants/ConsentPurposeCodes.cs`
- Static class, no DI, extensible:

```csharp
public static class ConsentPurposeCodes
{
    public const string OrganizerFutureCommunications = "ORGANIZER_FUTURE_COMMUNICATIONS";
}
```

- **Effort**: XS

#### Task 1.6 — Add `ConsentUiVersions` constants
- **File**: `Explore.Domain/Constants/ConsentUiVersions.cs`

```csharp
public static class ConsentUiVersions
{
    /// <summary>v1 — initial consent text: "Share my email address with {OrgName} so they can
    /// contact me about future events and related updates. Optional."</summary>
    public const string V1 = "v1";
}
```

- **Effort**: XS

---

### Phase 2 — Persistence Layer

**Layer**: `Explore.Persistence`
**Skills**: `dotnet-efcore-guidelines`

#### Task 2.1 — EF configuration for `EventContactShareConsent`
- **File**: `Explore.Persistence/Configurations/Entities/EventContactShareConsentConfiguration.cs`
- Table name: `event_contact_share_consents`
- Column/constraint rules:
  - `Id` — UUIDv7 default via `HasDefaultValueSql("gen_random_uuid()")`  (or `newid()` stub — check existing pattern in other configs)
  - `PurposeCode` — `varchar(100)` not null
  - `Status` — `int` not null, no default (set in handler)
  - `EmailSnapshot` — `varchar(320)` not null
  - `EmailNormalizedSnapshot` — `varchar(320)` not null
  - `ConsentTextSnapshot` — `text` not null
  - `ConsentUiVersion` — `varchar(20)` not null
  - `GrantedAt` — timestamptz not null
  - `WithdrawnAt` — timestamptz nullable
  - `SourceEventRegistrationId` — FK restrict (set null on delete — `OnDelete(DeleteBehavior.SetNull)`)
  - `EventId` → events.id restrict
  - `UserId` → users.id restrict
  - `TenantId` → tenants.id restrict
  - `RecipientActorId` → actors.id restrict
- **Unique index**: `(TenantId, EventId, UserId, RecipientActorId, PurposeCode)` — name: `ix_eventcontactshare_scope_unique`
- **Index 1**: `(TenantId, RecipientActorId, Status, EventId)` — name: `ix_eventcontactshare_recipient_status`
- **Index 2**: `(TenantId, UserId, Status)` — name: `ix_eventcontactshare_user_status`
- **Named query filter**: `HasQueryFilter(name: "Tenant", ...)` — matches ExploreDbContext pattern for tenant-scoped entities
- No soft-delete filter (consent rows are permanent)
- **Effort**: M
- **Acceptance criteria**:
  - [ ] All FK behaviours match spec
  - [ ] Unique index present
  - [ ] Two additional indexes present

#### Task 2.2 — EF configuration for `EventContactShareExport`
- **File**: `Explore.Persistence/Configurations/Entities/EventContactShareExportConfiguration.cs`
- Table: `event_contact_share_exports`
- Index: `(TenantId, RecipientActorId, CreatedAt)` — name: `ix_eventcontactshareexport_recipient_date`
- **Effort**: S

#### Task 2.3 — EF configuration for `EventContactShareExportItem`
- **File**: `Explore.Persistence/Configurations/Entities/EventContactShareExportItemConfiguration.cs`
- Table: `event_contact_share_export_items`
- Composite PK: `(ExportId, ConsentId)`
- `ExportId` → `event_contact_share_exports.id` cascade delete
- `ConsentId` → `event_contact_share_consents.id` restrict
- **Effort**: S

#### Task 2.4 — Add DbSets to `ExploreDbContext`
- **File**: `Explore.Persistence/ExploreDbContext.cs` (modify)
- Add three `DbSet<>` properties
- Wire up `modelBuilder.ApplyConfiguration(new EventContactShareConsentConfiguration())` etc.
- **Effort**: S

#### Task 2.5 — Repository interface: `IEventContactShareConsentRepository`
- **File**: `Explore.Application/Contracts/Persistence/IEventContactShareConsentRepository.cs`
- Extends `IGenericRepository<EventContactShareConsent, Guid>` (or whatever base is in the project)
- Additional methods:

```csharp
Task<EventContactShareConsent?> GetByScope(
    Guid tenantId, Guid eventId, Guid userId, Guid recipientActorId, string purposeCode,
    CancellationToken ct);

Task<List<EventContactShareConsent>> GetGrantedForOrganiser(
    Guid tenantId, Guid recipientActorId, Guid? eventId, CancellationToken ct);

Task<List<EventContactShareConsent>> GetByUser(
    Guid tenantId, Guid userId, CancellationToken ct);
```

- **Effort**: S

#### Task 2.6 — Repository implementation
- **File**: `Explore.Persistence/Repositories/EventContactShareConsentRepository.cs`
- Implements `IEventContactShareConsentRepository`
- `GetByScope` uses a direct index on `(TenantId, EventId, UserId, RecipientActorId, PurposeCode)` — SingleOrDefaultAsync
- `GetGrantedForOrganiser` filters `Status == ConsentStatus.Granted`, optionally filters by eventId
- `GetByUser` returns all consents (any status) for the connected apps page
- Includes navigations: `RecipientActor.Organization.Pii`, `Event` (for display name), `User`
- **Effort**: M

#### Task 2.7 — Repository interface and impl for exports
- **Files**:
  - `Explore.Application/Contracts/Persistence/IEventContactShareExportRepository.cs`
  - `Explore.Persistence/Repositories/EventContactShareExportRepository.cs`
- Used only in the export handler (create audit row + items)
- **Effort**: S

#### Task 2.8 — Add new permissions to seed data
- **File**: Locate the permission seeding file (check `PermissionConfiguration.cs` or `ExploreDbContext.HasData` calls)
- Add two new `Permission` entries:

```
MasterCode: "event_contact_share_consent:view_contacts"
ResourceKind: "event_contact_share_consent"
Action: "view_contacts"
GroupName: "Contacts"
Scope: RoleScopeEnum.Organization
FullName: "View Shared Registrant Contacts"
IsSystem: true, IsActive: true

MasterCode: "event_contact_share_consent:export_contacts"
ResourceKind: "event_contact_share_consent"
Action: "export_contacts"
GroupName: "Contacts"
Scope: RoleScopeEnum.Organization
FullName: "Export Shared Registrant Contacts"
IsSystem: true, IsActive: true
```

- **Effort**: S

#### Task 2.9 — EF Core migration
- Run: `dotnet ef migrations add AddEventContactShareConsent --project Explore.Persistence --startup-project Explore.API`
- Review generated SQL, confirm:
  - [ ] Three tables created with correct columns and types
  - [ ] Unique index and two additional indexes
  - [ ] Foreign key constraints correct
- **Effort**: S

---

### Phase 3 — Application Layer

**Layer**: `Explore.Application`
**Skills**: `cqrs-mediatr-guidelines`, `clean-architecture-rules`

#### Task 3.1 — Extend `CreateEventRegistrationDto`
- **File**: `Explore.Application/DTOs/EventRegistration/CreateEventRegistrationDto.cs` (modify)
- Add:

```csharp
public bool ShareEmailWithOrganizer { get; set; }
public string? ConsentTextAcknowledged { get; set; }   // the exact text shown to the user
public string? ConsentUiVersion { get; set; }          // "v1" etc.
```

- These fields are optional — if `ShareEmailWithOrganizer == false` they are ignored
- **Effort**: XS

#### Task 3.2 — Add consent DTOs
- `Explore.Application/DTOs/EventContactShareConsent/EventContactShareConsentDto.cs`

```csharp
public Guid Id { get; set; }
public Guid EventId { get; set; }
public string? EventTitle { get; set; }
public Guid RecipientActorId { get; set; }
public string? OrganisationName { get; set; }
public string PurposeCode { get; set; }
public ConsentStatus Status { get; set; }
public string EmailSnapshot { get; set; }
public DateTime GrantedAt { get; set; }
public DateTime? WithdrawnAt { get; set; }
```

- `Explore.Application/DTOs/EventContactShareConsent/EventContactShareConsentListDto.cs` (minimal — for org contacts list)

```csharp
public Guid Id { get; set; }
public string EmailSnapshot { get; set; }
public DateTime GrantedAt { get; set; }
public Guid EventId { get; set; }
public string? EventTitle { get; set; }
public string PurposeCode { get; set; }
```

- `Explore.Application/DTOs/EventContactShareConsent/ConsentStatusForEventDto.cs`

```csharp
public bool HasActiveConsent { get; set; }
public Guid? ConsentId { get; set; }
public ConsentStatus? Status { get; set; }
public string? OrganisationName { get; set; }      // for showing in UI
public Guid? RecipientActorId { get; set; }        // for the form binding
public bool PublisherIsApprovedOrg { get; set; }   // whether checkbox should be shown at all
```

- **Effort**: S

#### Task 3.3 — Add `ConsentService` (internal application service)
- **File**: `Explore.Application/Services/ConsentService.cs` (+ interface `IConsentService.cs` in `Contracts/Application/`)
- Responsible for:
  1. Resolving the publisher org from `EventSession.Event.ActorId → Actor → Organization`
  2. Validating org is approved (`ApprovalStatusId == (int)ApprovalStatusEnum.Approved`)
  3. Snapshotting the user email from `User.Pii.Email`
  4. Building the consent text: `"Share my email address with {orgName} so they can contact me about future events and related updates. Optional."`
  5. Upsert logic: find existing by scope → if Withdrawn, reactivate; if none, create new
- **Methods**:

```csharp
Task<GrantConsentResult> GrantAsync(
    Guid tenantId, Guid eventId, Guid userId, Guid sourceRegistrationId,
    string consentTextAcknowledged, string consentUiVersion, CancellationToken ct);

Task<ConsentStatusForEventDto> GetStatusForEventAsync(
    Guid tenantId, Guid eventId, Guid userId, CancellationToken ct);
```

- Fail safe rules:
  - If user has no email → return `GrantConsentResult.Failed("User has no email")`
  - If publisher actor not found → return `GrantConsentResult.Failed("Publisher not found")`
  - If org not approved → return `GrantConsentResult.Failed("Organisation not approved")`
  - Failures do NOT fail the registration — only the consent part is skipped
- **Effort**: L

#### Task 3.4 — Modify `CreateEventRegistrationCommandHandler`
- **File**: `Explore.Application/Features/EventRegistrations/Handlers/Commands/CreateEventRegistrationCommandHandler.cs` (modify)
- After successful registration creation, if `dto.ShareEmailWithOrganizer == true`:
  - Call `IConsentService.GrantAsync(...)`
  - If grant fails: log a warning but do NOT fail the registration response
  - If grant succeeds: no change to response (consent is a side-effect)
- Inject `IConsentService` into constructor
- **Effort**: S
- **Acceptance criteria**:
  - [ ] Registration still succeeds if consent grant fails
  - [ ] No duplicate consents created for same scope

#### Task 3.5 — `WithdrawConsentCommand` + handler
- **Files**:
  - `Explore.Application/Features/EventContactShareConsents/Requests/Commands/WithdrawConsentCommand.cs`
  - `Explore.Application/Features/EventContactShareConsents/Handlers/Commands/WithdrawConsentCommandHandler.cs`
- Command:

```csharp
[AuthorizeResource("event_contact_share_consent", PermissionAction.Update)]
public class WithdrawConsentCommand : IRequest<Unit>, ISecureRequest
{
    public Guid ConsentId { get; set; }
    public Guid UserId { get; set; }  // extracted from JWT claims in controller

    string? ISecureRequest.ResourceId => ConsentId.ToString();
}
```

- Handler:
  - Load consent by ID, verify `consent.UserId == command.UserId` (own consent only)
  - Set `Status = ConsentStatus.Withdrawn`, `WithdrawnAt = DateTime.UtcNow`
  - Update via repository
  - Return `Unit`
- **Effort**: S

#### Task 3.6 — `GetMyConsentsRequest` + handler
- **Files**:
  - `Explore.Application/Features/EventContactShareConsents/Requests/Queries/GetMyConsentsRequest.cs`
  - `Explore.Application/Features/EventContactShareConsents/Handlers/Queries/GetMyConsentsRequestHandler.cs`
- Returns all consents (any status) for the current user, mapped to `EventContactShareConsentDto`
- Includes: org name, event title, granted at, withdrawn at, current status
- **Effort**: S

#### Task 3.7 — `GetConsentStatusForEventRequest` + handler
- **Files**:
  - `Explore.Application/Features/EventContactShareConsents/Requests/Queries/GetConsentStatusForEventRequest.cs`
  - `Explore.Application/Features/EventContactShareConsents/Handlers/Queries/GetConsentStatusForEventRequestHandler.cs`
- Given `eventId + userId (from JWT)`, returns `ConsentStatusForEventDto`:
  - Resolves the publisher actor for the event
  - Checks if publisher is an approved org → sets `PublisherIsApprovedOrg`
  - Checks existing consent by scope
  - Returns consent status or null
- `[AllowAnonymous]` endpoint (GET) — but actual consent check requires auth
- **Effort**: M

#### Task 3.8 — `GetOrgConsentsRequest` + handler (organiser view)
- **Files**:
  - `Explore.Application/Features/EventContactShareConsents/Requests/Queries/GetOrgConsentsRequest.cs`
  - `Explore.Application/Features/EventContactShareConsents/Handlers/Queries/GetOrgConsentsRequestHandler.cs`
- `[AuthorizeResource("event_contact_share_consent", "view_contacts")]` + `ISecureRequest` with `organizationId`
- Returns paginated `PaginatedResult<EventContactShareConsentListDto>` — only `Status == Granted`
- Optional filter by `EventId`
- **Effort**: M

#### Task 3.9 — `ExportConsentsRequest` + handler
- **Files**:
  - `Explore.Application/Features/EventContactShareConsents/Requests/Queries/ExportConsentsRequest.cs`
  - `Explore.Application/Features/EventContactShareConsents/Handlers/Queries/ExportConsentsRequestHandler.cs`
- `[AuthorizeResource("event_contact_share_consent", "export_contacts")]` + `ISecureRequest` with `organizationId`
- Returns `ExportConsentsResult` containing:

```csharp
public byte[] FileBytes { get; set; }
public string FileName { get; set; }        // e.g. "contacts_20260312.csv"
public string ContentType { get; set; }     // "text/csv" or "text/tab-separated-values"
public int RowCount { get; set; }
public Guid ExportId { get; set; }          // audit row ID
```

- Handler:
  1. Fetch only `Status == Granted` consents for `(tenantId, recipientActorId, optionalEventId)`
  2. Build CSV/TSV in-memory using `System.Text.StringBuilder` (no external library)
  3. Columns: `Email,GrantedAtUtc,EventId,EventTitle,OrganisationId,OrganisationName,PurposeCode`
  4. UTF-8 bytes
  5. Create `EventContactShareExport` audit row + `EventContactShareExportItem` rows
  6. Return `ExportConsentsResult`
- **Effort**: L
- **Acceptance criteria**:
  - [ ] Only Granted consents exported
  - [ ] Header row present
  - [ ] UTF-8 encoding
  - [ ] Audit rows created
  - [ ] CSV and TSV both work via `format` parameter

#### Task 3.10 — AutoMapper profile
- **File**: `Explore.Application/Profiles/EventContactShareConsentProfile.cs`
- Maps `EventContactShareConsent → EventContactShareConsentDto` and `EventContactShareConsentListDto`
- Includes: `Event.Title`, `RecipientActor.Organization.Pii.FullName`
- **Effort**: S

---

### Phase 4 — API Layer

**Layer**: `Explore.API`
**Skills**: `cqrs-mediatr-guidelines`, `auth-patterns`

#### Task 4.1 — `EventContactShareConsentController`
- **File**: `Explore.API/Controllers/EventContactShareConsentController.cs`
- Route: `[Route("api/eventcontactshareconsent")]`
- Versioned: `[ApiVersion("0.1")]`
- Named routes in `RouteNames` constants (add new constants there)

**Endpoints**:

| Verb | Route | Auth | Purpose |
|---|---|---|---|
| `GET` | `/my-consents` | `[Authorize]` | User's own consents (for Connected Apps page) |
| `GET` | `/for-event/{eventId}` | `[Authorize]` | Consent status for current user + this event |
| `POST` | `/{id}/withdraw` | `[Authorize]` | User withdraws own consent |
| `GET` | `/org/{orgId}/contacts` | `[Authorize]` | Organiser views shared contacts (paginated) |
| `GET` | `/org/{orgId}/export` | `[Authorize]` | Organiser exports (CSV/TSV stream) |

- **Export endpoint** uses `Complex` timeout policy: `[RequestTimeout("Complex")]`
- Export endpoint returns `FileContentResult` with correct content-type header
- Export endpoint accepts query param: `?format=csv` (default) or `?format=tsv`
- **Effort**: M
- **Acceptance criteria**:
  - [ ] All 5 endpoints present
  - [ ] `GET` endpoints `[AllowAnonymous]` if appropriate or `[Authorize]` per table above
  - [ ] Export streams the file with `Content-Disposition: attachment` header
  - [ ] Named routes added to `RouteNames`
  - [ ] `[ProducesResponseType]` on all actions

#### Task 4.2 — Cerbos policy: `event_contact_share_consent.yaml`
- **File**: `cerbos/policies/event_contact_share_consent.yaml`

```yaml
# ABOUTME: Cerbos resource policy for event_contact_share_consent resources.
# Organiser contacts: org admins may view/export consents for their own org's events.

apiVersion: api.cerbos.dev/v1
resourcePolicy:
  resource: "event_contact_share_consent"
  version: "default"
  importDerivedRoles:
    - explore_admin_roles
  rules:
    - actions: ["*"]
      effect: EFFECT_ALLOW
      derivedRoles: [instance_admin]

    - actions: ["view_contacts", "export_contacts", "manage"]
      effect: EFFECT_ALLOW
      derivedRoles: [tenant_admin]

    - actions: ["view_contacts", "export_contacts"]
      effect: EFFECT_ALLOW
      derivedRoles: [org_admin]
      # resource.attr.organizationId must be set by the handler to the recipient org's ID

    # Users can withdraw their own consent (update action on the specific consent row)
    - actions: ["update"]
      effect: EFFECT_ALLOW
      roles: [authenticated_user]
      condition:
        match:
          expr: request.resource.attr.ownerUserId == request.principal.id
```

- **Effort**: S
- **Note**: The resource `attr.organizationId` must be populated by the handler from `Actor.OrganizationId`. The `attr.ownerUserId` is `EventContactShareConsent.UserId`.

#### Task 4.3 — Update `RouteNames`
- **File**: `Explore.API/Utilities/RouteNames.cs` (or wherever it lives)
- Add:
  ```csharp
  public const string GetMyConsents = "GetMyConsents";
  public const string GetConsentStatusForEvent = "GetConsentStatusForEvent";
  public const string WithdrawConsent = "WithdrawConsent";
  public const string GetOrgConsents = "GetOrgConsents";
  public const string ExportOrgConsents = "ExportOrgConsents";
  ```
- **Effort**: XS

---

### Phase 5 — Blazor UI

**Layer**: `Explore.Blazor.Client` (WASM)
**Skills**: `blazor-ui-conventions`, `blazor-css-isolation`, `blazor-bff-patterns`

#### Task 5.1 — Extend `EventRegistration.razor` (consent checkbox)
- **File**: `Explore.Blazor.Client/Pages/Events/Components/EventRegistration.razor` (modify)

**Logic changes**:
1. On component init, after loading current user and checking existing registration:
   - Call `IConsentService.GetConsentStatusForEventAsync(eventId)` → returns `ConsentStatusForEventDto`
   - Store result in `_consentStatus`
2. In the registration form markup:
   - If `_consentStatus?.PublisherIsApprovedOrg == true`:
     - If `_consentStatus.Status == Granted` → render info notice (not a checkbox):
       ```
       ℹ️ You previously shared your email with {OrganisationName}. You can manage this in Connected Apps.
       ```
     - Else (no consent or Withdrawn) → render consent checkbox:
       ```html
       <MudCheckBox @bind-Value="_shareEmail" Label="Share my email address with [OrgName] so they can contact me about future events and related updates. Optional." />
       ```
       (unchecked by default)
3. On form submit, include:
   - `ShareEmailWithOrganizer = _shareEmail`
   - `ConsentTextAcknowledged = BuildConsentText(_consentStatus.OrganisationName)`
   - `ConsentUiVersion = ConsentUiVersions.V1`

**CSS** (`EventRegistration.razor.css`):
- `.consent-section` — subtle separator, slightly muted text
- `.consent-info-notice` — info-coloured banner

- **Effort**: M
- **Acceptance criteria**:
  - [ ] Checkbox is unchecked by default
  - [ ] Checkbox only shown if publisher is approved org
  - [ ] If prior Granted consent: shows info notice, no checkbox
  - [ ] If prior Withdrawn consent: shows checkbox again
  - [ ] Consent text includes org name

#### Task 5.2 — `IConsentService` + `ConsentService` (Blazor Client)
- **Files**:
  - `Explore.Blazor.Client/Services/IConsentService.cs`
  - `Explore.Blazor.Client/Services/ConsentService.cs`
- Methods:

```csharp
Task<ConsentStatusForEventDto?> GetConsentStatusForEventAsync(Guid eventId);
Task<IReadOnlyList<EventContactShareConsentDto>> GetMyConsentsAsync();
Task WithdrawConsentAsync(Guid consentId);
```

- Uses `EventApiClient` (NSwag generated) — after NSwag regeneration
- **Effort**: S

#### Task 5.3 — Register `ConsentService` in DI
- **File**: `Explore.Blazor.Client/Extensions/ServiceCollectionExtensions.cs` (modify — in `AddSharedApplicationServices()`)
- Add: `services.AddScoped<IConsentService, ConsentService>();`
- **Effort**: XS

#### Task 5.4 — `ConnectedApps.razor` page
- **File**: `Explore.Blazor.Client/Pages/Account/ConnectedApps.razor`
- Route: `@page "/account/connected-apps"`
- Render mode: `@rendermode InteractiveWebAssembly` (matches account pages pattern)
- **Content**:
  - Page title: "Connected Apps & Sharing"
  - Subtitle: "Manage what you've shared with third parties and connected organisations."
  - `MudTable` or `MudDataGrid` with columns:
    | Organisation | Event | Granted | Status | Actions |
  - Status badge: green = Granted, grey = Withdrawn
  - "Withdraw" button per Granted row → opens `MudDialog` confirmation:
    - Warning: "This will prevent [Org Name] from seeing your email on this platform going forward. Note: this does not guarantee that they will remove your email address from any mailing lists they may have already compiled."
    - Two buttons: "Cancel", "Withdraw Consent"
  - Empty state: "You haven't shared your email with any organisations."
  - Section header note: "More connected apps will appear here in the future (e.g. SSO integrations)."

- **File**: `Explore.Blazor.Client/Pages/Account/ConnectedApps.razor.css`
  - `.connected-apps-page` — container
  - `.status-badge` — shared with BEM modifier: `status-badge--granted`, `status-badge--withdrawn`
  - `.withdrawal-warning` — amber/warning colour for the notice

- **Effort**: M
- **Acceptance criteria**:
  - [ ] Shows all consents (any status) for current user
  - [ ] Withdraw button only on Granted rows
  - [ ] Confirmation dialog shows warning text
  - [ ] Withdraw action calls `IConsentService.WithdrawConsentAsync`
  - [ ] Page reloads/refreshes list after withdrawal
  - [ ] Link to this page from account navigation or registration info notice

#### Task 5.5 — `OrgContactsPage.razor` (organiser view)
- **File**: `Explore.Blazor.Client/Pages/Organizations/OrgContactsPage.razor`
- Route: `@page "/org/{OrgId:guid}/contacts"`
- Render mode: `InteractiveWebAssembly`
- Guard: only show if user is org member (use existing org membership check pattern)
- **Content**:
  - Page title: "Shared Registrant Contacts"
  - Event filter dropdown (all events for this org)
  - `MudDataGrid` with columns: Email, Event, Granted At, Purpose
  - Export buttons: "Export CSV", "Export TSV"
  - Export triggers file download via `IJSRuntime` (or `NavigationManager.NavigateTo` with download URL)
  - Empty state: "No registrants have shared their email address yet."

- **File**: `Explore.Blazor.Client/Pages/Organizations/OrgContactsPage.razor.css`

- **Effort**: M

#### Task 5.6 — Add link to Connected Apps from account navigation
- **Action**: Identify existing account/settings navigation component and add a "Connected Apps" link
- Check for `AccountNav.razor` or settings sidebar component
- **Effort**: S

---

### Phase 6 — Tests

**Test projects**: `Event.Application.UnitTests`, `Event.API.IntegrationTests`
**Skills**: TDD — write failing test first, then minimal code to pass

#### Task 6.1 — Unit tests: Consent lifecycle (Application.UnitTests)
- **File**: `Event.Application.UnitTests/Features/EventContactShareConsents/ConsentServiceTests.cs`

| Test | Scenario |
|---|---|
| `Registration_WithUncheckedCheckbox_DoesNotCreateConsent` | `ShareEmailWithOrganizer = false` → no consent row |
| `Registration_WithCheckedCheckbox_CreatesGrantedConsentWithEmailSnapshot` | `ShareEmailWithOrganizer = true` → consent row with email snapshot |
| `EmailSnapshot_NotUpdated_WhenUserChangesEmail` | Change user email after consent → old snapshot unchanged |
| `ReRegistration_SameEvent_DoesNotCreateDuplicate` | Second registration for another session of same event → reuses or skips consent row |
| `WithdrawConsent_SetsWithdrawnStatus` | Call withdraw → status = Withdrawn, withdrawnAt set |
| `NonOrganisationActor_CannotReceiveConsent` | Recipient actor has no OrganizationId → fail safe, no consent |
| `UnapprovedOrganisation_CannotReceiveConsent` | Org.ApprovalStatusId != Approved → fail safe, no consent |
| `ConsentText_IncludesOrganisationName` | Snapshot text contains org display name |

- **Effort**: L

#### Task 6.2 — Unit tests: Export (Application.UnitTests)
- **File**: `Event.Application.UnitTests/Features/EventContactShareConsents/ExportConsentsHandlerTests.cs`

| Test | Scenario |
|---|---|
| `Export_OnlyIncludesGrantedConsents` | Withdrawn consents not in export file |
| `Export_CsvFormat_HeaderAndRows` | CSV has correct header, UTF-8, columns |
| `Export_TsvFormat_HeaderAndRows` | TSV has correct delimiter |
| `Export_WritesAuditRows` | `EventContactShareExport` + `Items` created |

- **Effort**: M

#### Task 6.3 — Integration tests: API (API.IntegrationTests)
- **File**: `Event.API.IntegrationTests/Controllers/EventContactShareConsentControllerTests.cs`

| Test | Scenario |
|---|---|
| `UnauthorisedOrgMember_CannotViewContacts` | 403 when user not in org |
| `AuthorisedOrgMember_CanViewOwnOrgContacts` | 200 with contacts |
| `AuthorisedOrgMember_CannotViewOtherOrgContacts` | 403 when querying wrong org |
| `User_CanWithdrawOwnConsent` | Withdraw → 200, status Withdrawn |
| `User_CannotWithdrawOtherUsersConsent` | 403 when userId mismatch |

- **Effort**: M

---

### Phase 7 — Schema Documentation + Journal

#### Task 7.1 — Create `schema/islamu-event.md`
- **File**: `schema/islamu-event.md`
- Documents all tables including the three new ones in DBML-style markdown
- **Effort**: M

#### Task 7.2 — Update `dev/_journal/MAJOR_DECISIONS.md`
- Document: consent scope decision (event-level not session-level), actor-based schema choice, email snapshot rationale
- **Effort**: S

---

## Risk Assessment

| Risk | Likelihood | Impact | Mitigation |
|---|---|---|---|
| NSwag regeneration breaks existing DTOs | Medium | High | Strictly additive changes to `CreateEventRegistrationDto`; validate client build after |
| EF unique constraint violated during re-registration | Low | Medium | Upsert logic in `ConsentService.GrantAsync` handles this (find-then-update vs insert) |
| Export performance on large datasets | Low | Medium | Use `Complex` (60s) timeout; add `AsNoTracking()` in export query; paginate if needed |
| Cerbos `orgMemberships` attribute not set for consent resource | Medium | High | Follow exact pattern from `event_registration.yaml` — test with Cerbos principal builder |
| `ActorId` on Event pointing to a user (not org) — org check returns null | High | Low | `ConsentService` fails safe (returns "publisher not org" → skip consent silently) |
| Connected Apps page exposes all future integrations, scope creep | Low | Low | Page is a stub with a "more coming soon" notice; only consent list is functional now |

---

## Success Metrics

- [ ] Consent checkbox appears only when publisher is an approved org
- [ ] Checkbox is never pre-checked
- [ ] Consent is event-scoped (not session-scoped); no duplicates
- [ ] Email snapshot is stored; live email never exposed to organiser
- [ ] User can withdraw from Connected Apps page
- [ ] Withdrawal warning is visible and explicit
- [ ] Organiser can export CSV and TSV
- [ ] Export audit rows are persisted
- [ ] All 13 unit tests pass
- [ ] All 5 integration tests pass
- [ ] Build clean (0 errors)

---

## Deferred Follow-ups

| Item | Why Deferred |
|---|---|
| listmonk / Mailchimp / Brevo direct sync | Out of scope per product decision |
| Platform relay (sending emails on behalf of org) | Schema supports it; business logic intentionally blocks until ready |
| OAuth/SSO grants display on Connected Apps page | Keycloak scope management is a separate feature |
| PostgreSQL RLS for new tables | Deferred to post-v1 RLS global task |
| Per-session consent variation | Deliberately over-engineered; event-level scope is sufficient |
| Bulk "share with all my orgs" global preference | Rejected — must remain explicit, opt-in, per-event |
| Cerbos test files for new policy | Should be added post-implementation as `event_contact_share_consent_test.yaml` |
