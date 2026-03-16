# Organizer Email Consent — Context

> Last Updated: 2026-03-12

## SESSION PROGRESS (2026-03-12)

### ✅ COMPLETED
- Repository inspection (entities, registration flow, org approval, RBAC, Cerbos policies, PII tables, Blazor UI structure)
- Plan files created under `dev/active/organizer-email-consent/`
- SQL todos populated in session DB

### 🟡 IN PROGRESS
- Planning only — implementation not started

### ⚠️ BLOCKERS
- None at planning stage

---

## Key Architectural Decisions

### D1: Consent scope is event-level (not session-level)
**Why**: A user may register for multiple sessions of the same event. Asking for consent
once per event (not once per session) is the correct UX and prevents duplicate active rows.
**Unique index**: `(tenant_id, event_id, user_id, recipient_actor_id, purpose_code)`

### D2: Schema uses `recipient_actor_id` (not `recipient_organization_id`)
**Why**: Future-proofs for adding group/user recipients later without schema changes.
**Business logic restriction**: Validated in `ConsentService` — only approved org actors pass.

### D3: Email snapshot, never live
**Why**: If a user later changes their account email, the organiser still only has
the email that was explicitly shared with them at consent time.
**Implementation**: Read `User.Pii.Email` at the moment of grant, store in `EmailSnapshot`.

### D4: No `ISoftDeletable` on consent entity
**Why**: Audit integrity. Consent rows must never disappear. `ConsentStatus.Withdrawn` is
the lifecycle mechanism. Adding `IsDeleted` would create ambiguity.

### D5: Registration does NOT fail if consent grant fails
**Why**: The user chose to register. A consent processing error (e.g. org not found)
must not prevent their registration. Log warning, skip consent.

### D6: Show checkbox only once (first registration for this event)
**Why**: If the user already gave consent (Status = Granted), don't ask again — show an
info notice. If previously withdrawn, show checkbox again to allow re-consent.

### D7: Connected Apps page as the single consent/integration hub
**Why**: User wants this to be the canonical privacy management page — future SSO/OAuth
grants from Keycloak will also land here. Design it as a hub, implement only email
consent for now.

### D8: Export is in-memory StringBuilder (no external CSV library)
**Why**: Volume is expected to be small (event attendees). No new dependency needed.
`System.Text.StringBuilder` + proper escaping is sufficient. CSV column escape rule:
wrap in `"` if value contains comma, newline, or `"`.

---

## Key Files

### Domain
| File | Purpose |
|---|---|
| `Explore.Domain/Enums/ConsentStatus.cs` | **NEW** — `Granted = 1, Withdrawn = 2` |
| `Explore.Domain/EventContactShareConsent.cs` | **NEW** — main consent entity |
| `Explore.Domain/EventContactShareExport.cs` | **NEW** — export audit header |
| `Explore.Domain/EventContactShareExportItem.cs` | **NEW** — export audit items |
| `Explore.Domain/Constants/ConsentPurposeCodes.cs` | **NEW** — `ORGANIZER_FUTURE_COMMUNICATIONS` |
| `Explore.Domain/Constants/ConsentUiVersions.cs` | **NEW** — `V1 = "v1"` |

### Persistence
| File | Purpose |
|---|---|
| `Explore.Persistence/Configurations/Entities/EventContactShareConsentConfiguration.cs` | **NEW** — EF config with indexes |
| `Explore.Persistence/Configurations/Entities/EventContactShareExportConfiguration.cs` | **NEW** |
| `Explore.Persistence/Configurations/Entities/EventContactShareExportItemConfiguration.cs` | **NEW** |
| `Explore.Persistence/ExploreDbContext.cs` | **MODIFY** — add 3 DbSets |
| `Explore.Persistence/Repositories/EventContactShareConsentRepository.cs` | **NEW** |
| `Explore.Persistence/Repositories/EventContactShareExportRepository.cs` | **NEW** |

### Application
| File | Purpose |
|---|---|
| `Explore.Application/Contracts/Persistence/IEventContactShareConsentRepository.cs` | **NEW** |
| `Explore.Application/Contracts/Persistence/IEventContactShareExportRepository.cs` | **NEW** |
| `Explore.Application/Contracts/Application/IConsentService.cs` | **NEW** |
| `Explore.Application/Services/ConsentService.cs` | **NEW** — core consent business logic |
| `Explore.Application/DTOs/EventRegistration/CreateEventRegistrationDto.cs` | **MODIFY** — add 3 fields |
| `Explore.Application/DTOs/EventContactShareConsent/*.cs` | **NEW** — response DTOs |
| `Explore.Application/Features/EventRegistrations/.../CreateEventRegistrationCommandHandler.cs` | **MODIFY** — call consent service |
| `Explore.Application/Features/EventContactShareConsents/**` | **NEW** — commands, queries, handlers |
| `Explore.Application/Profiles/EventContactShareConsentProfile.cs` | **NEW** — AutoMapper |

### API
| File | Purpose |
|---|---|
| `Explore.API/Controllers/EventContactShareConsentController.cs` | **NEW** — 5 endpoints |
| `cerbos/policies/event_contact_share_consent.yaml` | **NEW** — Cerbos policy |
| `Explore.API/Utilities/RouteNames.cs` | **MODIFY** — add 5 new route name constants |

### Blazor
| File | Purpose |
|---|---|
| `Explore.Blazor.Client/Pages/Events/Components/EventRegistration.razor` | **MODIFY** — consent checkbox/notice |
| `Explore.Blazor.Client/Pages/Account/ConnectedApps.razor` | **NEW** — user consent management hub |
| `Explore.Blazor.Client/Pages/Account/ConnectedApps.razor.css` | **NEW** — BEM scoped CSS |
| `Explore.Blazor.Client/Pages/Organizations/OrgContactsPage.razor` | **NEW** — organiser view |
| `Explore.Blazor.Client/Pages/Organizations/OrgContactsPage.razor.css` | **NEW** |
| `Explore.Blazor.Client/Services/IConsentService.cs` | **NEW** |
| `Explore.Blazor.Client/Services/ConsentService.cs` | **NEW** |
| `Explore.Blazor.Client/Extensions/ServiceCollectionExtensions.cs` | **MODIFY** — register ConsentService |

### Tests
| File | Purpose |
|---|---|
| `Event.Application.UnitTests/Features/EventContactShareConsents/ConsentServiceTests.cs` | **NEW** — 8 unit tests |
| `Event.Application.UnitTests/Features/EventContactShareConsents/ExportConsentsHandlerTests.cs` | **NEW** — 4 unit tests |
| `Event.API.IntegrationTests/Controllers/EventContactShareConsentControllerTests.cs` | **NEW** — 5 integration tests |

---

## Critical Implementation Details

### Resolving the publisher org from an event session

```csharp
// In ConsentService.GrantAsync:
var session = await _eventSessionRepository.GetById(sessionId); // includes Event
var actor = await _actorRepository.GetById(session.Event.ActorId); // includes Organization + Pii
if (actor.OrganizationId == null) return GrantConsentResult.NotOrganisation();
var org = actor.Organization!;
if (org.ApprovalStatusId != (int)ApprovalStatusEnum.Approved)
    return GrantConsentResult.NotApproved();
```

### Upsert logic (prevent duplicate consents)

```csharp
var existing = await _consentRepo.GetByScope(tenantId, eventId, userId, actor.Id, purposeCode, ct);
if (existing is not null)
{
    if (existing.Status == ConsentStatus.Granted) return GrantConsentResult.AlreadyGranted(existing.Id);
    // Reactivate withdrawn consent
    existing.Status = ConsentStatus.Granted;
    existing.EmailSnapshot = email;
    existing.EmailNormalizedSnapshot = email.ToLowerInvariant();
    existing.GrantedAt = DateTime.UtcNow;
    existing.WithdrawnAt = null;
    existing.SourceEventRegistrationId = registrationId;
    await _consentRepo.Update(existing);
    return GrantConsentResult.Reactivated(existing.Id);
}
// Create new
```

### Cerbos resource attributes for organiser access

```csharp
// In GetOrgConsentsRequestHandler / ExportConsentsRequestHandler:
// The command must set organizationId in ResourceAttributes for org_admin check:
IDictionary<string, object>? ISecureRequest.ResourceAttributes =>
    new Dictionary<string, object>
    {
        ["organizationId"] = OrganizationId.ToString(),
        ["tenantId"] = TenantId.ToString()
    };
```

### Consent text template

```csharp
// In ConsentService:
private static string BuildConsentText(string orgName) =>
    $"Share my email address with {orgName} so they can contact me about future events and related updates. Optional.";
```

### CSV/TSV escape helper

```csharp
private static string EscapeCsvField(string? value)
{
    if (value is null) return string.Empty;
    if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
        return $"\"{value.Replace("\"", "\"\"")}\"";
    return value;
}
```

### Withdrawal warning UI text

```
Withdrawing your consent will prevent [Org Name] from seeing your email address on this platform 
going forward. Note: this does not guarantee that they will remove your email address from any 
mailing lists they may have already compiled.
```

---

## Existing Patterns to Follow

| Pattern | Where to find example |
|---|---|
| Entity with ITenantEntity + IAuditableEntity | `Explore.Domain/EventRegistration.cs` |
| EF config with FK + indexes | `Explore.Persistence/Configurations/Entities/EventRegistrationConfiguration.cs` |
| Command with AuthorizeResource + ISecureRequest | `Explore.Application/Features/EventRegistrations/Requests/Commands/CreateEventRegistrationCommand.cs` |
| Handler with multiple repo injections | `Explore.Application/Features/EventRegistrations/Handlers/Commands/CreateEventRegistrationCommandHandler.cs` |
| Cerbos policy with org_admin rule | `cerbos/policies/event_registration.yaml` |
| Cerbos derived roles structure | `cerbos/policies/derived_roles.yaml` |
| AutoMapper profile | Check `Explore.Application/Profiles/` folder |
| MudTable/MudDataGrid Blazor page | Existing org or event list pages |

---

## Assumptions Made

1. `Event.ActorId` → `Actor.OrganizationId` is the canonical publisher-org resolution path
2. "Approved organisation" = `Organization.ApprovalStatusId == 2` (from `ApprovalStatusEnum.Approved`)
3. The `IGenericRepository<T, TId>` base interface exists in `Explore.Application/Contracts/Persistence/`
4. `IEventSessionRepository.GetById()` can be configured to include `Event` navigation (or a dedicated method created)
5. `IActorRepository` exists for resolving actor details with org navigation
6. The NSwag regeneration step is part of normal build; `EventApiClient.g.cs` will be regenerated after DTO changes
7. Permission `HasData` seeding is done in the persistence configuration, not in a separate migration
8. The `RoleScopeEnum.Organization` value exists already in `Explore.Domain`
9. Account navigation component can be identified and extended (to add Connected Apps link)
10. `IJSRuntime` is used for file downloads in existing Blazor pages (check `StorageObjectController` pattern)

---

## Quick Resume Instructions

1. Read `organizer-email-consent-plan.md` for full phase breakdown
2. Check `organizer-email-consent-tasks.md` for current checklist state
3. Run build baseline first: `dotnet build --configuration Release --verbosity quiet`
4. Start with Phase 1 (Domain) — pure entity additions, no external dependencies
5. After Phase 2 migration runs: verify schema in pgAdmin or migration SQL preview
6. After Phase 3: run unit tests to verify consent logic before adding API layer
7. After Phase 4: manually test with `curl` or Swagger UI
8. After Phase 5: use Playwriter MCP for visual UI verification
9. Final: run all 7 test projects per `CLAUDE.md` instructions
