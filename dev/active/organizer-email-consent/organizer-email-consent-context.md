# Organizer Email Consent — Context

> Last Updated: 2026-04-28 Europe/Brussels

## SESSION PROGRESS (2026-04-28)

### ✅ COMPLETED — Implementation Fully Integrated

The feature is **fully implemented and verified**. Most of the codebase already existed from prior work (domain, persistence, application service, controller, Cerbos policy, Blazor client services, Connected Apps UI, Organization Shared Contacts UI). This session closed the critical integration gaps and fixed two Oracle-identified blockers.

#### What Already Existed (verified, no changes needed)
- **Domain**: `ConsentStatus` enum, `ConsentPurposeCodes`, `ConsentUiVersions`, `EventContactShareConsent` entity, `EventContactShareExport`, `EventContactShareExportItem` — all complete
- **Persistence**: EF configurations for all 3 entities, `EventContactShareConsentRepository` (4 methods), `EventContactShareExportRepository`, DI registrations in `PersistenceServicesRegistration`
- **Application**: `ContactShareConsentService` (ProcessRegistrationConsent, HasGrantedConsentForOrganizer, WithdrawConsent, GetUserConsents), all CQRS handlers (Withdraw, GetMy, GetOrg, Export), DTOs, DI in `ApplicationServicesRegistration`
- **API**: `ContactShareShareConsentController` (5 endpoints), Cerbos `event_contact_share_consent.yaml`, RouteNames constants
- **Blazor Client**: `IContactShareConsentService` + impl, `SettingsConnectedApps.razor`, `OrganizationSharedContacts.razor`, DI in `ServiceCollectionExtensions`

#### What This Session Changed

##### 1. Added consent fields to registration DTO
- **File**: `Explore.Application/DTOs/EventRegistration/CreateEventRegistrationDto.cs`
- Added `ShareEmailWithOrganizer` (bool), `ConsentTextAcknowledged` (string?), `ConsentUiVersion` (string?)

##### 2. Wired consent service into registration handler
- **File**: `Explore.Application/Features/EventRegistrations/Handlers/Commands/CreateEventRegistrationCommandHandler.cs`
- Injected `IContactShareConsentService` + `ILogger<CreateEventRegistrationCommandHandler>`
- After `CreateWithChildrenAsync`, calls `ProcessRegistrationConsent` if `ShareEmailWithOrganizer == true`
- Fail-safe: wrapped in try/catch, logs warning, registration never fails

##### 3. Fixed consent audit FK (Oracle blocker #1)
- **Problem**: Consent entity had `SourceEventRegistrationId` FK to `EventRegistration`, but handler passed `EventRegistrationIntent.Id` — FK violation at runtime
- **Fix**: Renamed to `SourceEventRegistrationIntentId` → `EventRegistrationIntent` (parent aggregate). One intent can create multiple child registrations, so the intent is the correct audit reference.
- **Files changed**:
  - `Explore.Domain/EventContactShareConsent.cs` — replaced FK property + navigation
  - `Explore.Persistence/Configurations/Entities/EventContactShareConsentConfiguration.cs` — updated FK config
  - `Explore.Application/Contracts/Services/IContactShareConsentService.cs` — renamed param
  - `Explore.Application/Services/ContactShareConsentService.cs` — updated create/reactivate assignments
- **Migration**: `Explore.Persistence/Migrations/Appearance/20260428193206_UseRegistrationIntentForContactShareConsent.cs` — surgical rename of column + FK

##### 4. Fixed org contact authorization context (Oracle blocker #2)
- **Problem**: `GetOrganizationSharedContactsQuery` and `ExportSharedContactsCommand` used `RecipientActorId` as both `ResourceId` and `organizationId` attribute. Cerbos expects actual `OrganizationId`, not actor id. `TenantId` was missing from attributes.
- **Fix**: Controller resolves `recipientActorId → Actor.OrganizationId` server-side before sending to mediator. Queries/commands now carry actual `OrganizationId` for authorization.
- **Files changed**:
  - `Explore.Application/Features/ContactShareConsents/Requests/Queries/GetOrganizationSharedContactsQuery.cs` — added `OrganizationId`, updated `ISecureRequest` attrs
  - `Explore.Application/Features/ContactShareConsents/Requests/Commands/ExportSharedContactsCommand.cs` — same
  - `Explore.API/Controllers/ContactShareConsentController.cs` — injected `IActorRepository`, added `ResolveOrganizationId` helper, resolves before mediator send

##### 5. Added DbSets for export entities
- **File**: `Explore.Persistence/ExploreDbContext.DbSets.cs`
- Added `DbSet<EventContactShareExport>` and `DbSet<EventContactShareExportItem>`

##### 6. Updated generated NSwag client
- **File**: `Explore.Blazor.Client/Clients/EventApiClient.g.cs`
- Added 3 nullable properties to generated `CreateEventRegistrationDto` class

##### 7. Added consent UI to EventRegistration.razor
- **File**: `Explore.Blazor.Client/Pages/Events/Components/EventRegistration.razor`
- Injected `IContactShareConsentService`, added `RecipientActorId`/`PublisherOrganizationName` parameters
- Checks existing consent on init, shows info notice if already sharing, checkbox otherwise
- Submits consent fields with registration

##### 8. Wired parent components to pass consent parameters
- **File**: `Explore.Blazor.Client/Pages/Events/EventDetail.razor.cs` — passes `_eventDetails.ActorId` + `_eventDetails.ActorDisplayName`
- **File**: `Explore.Blazor.Client/Pages/Events/EventList.razor.cs` — passes `evt.ActorId` + `evt.ActorDisplayName`

##### 9. Wired Connected Apps into SettingsLayout navigation
- **File**: `Explore.Blazor.Client/Pages/User/Components/SettingsLayout.razor`
- Added Connected Apps nav item, content section, `/settings?section=connected-apps` query param support
- Custom `ParseSectionFromQuery` method (no WebUtilities dependency for WASM compat)

##### 10. Updated tests
- `Event.Application.UnitTests/Services/ContactShareConsentServiceTests.cs` — renamed `_registrationId` → `_registrationIntentId`, updated assertions
- `Event.Domain.UnitTests/Entities/EventContactShareConsentTests.cs` — updated nullable FK/navigation assertions
- `Event.Application.UnitTests/Features/ContactShareConsents/Queries/GetOrganizationSharedContactsQueryHandlerTests.cs` — added `OrganizationId`, new `SecureRequest_UsesOrganizationIdAndTenantId_ForAuthorizationContext` test
- `Event.Application.UnitTests/Features/ContactShareConsents/Commands/ExportSharedContactsCommandHandlerTests.cs` — same pattern

##### 11. Documentation
- `schemas/islamu-event.md` — added DBML notes for consent/export tables, updated FK references
- `dev/_journal/MAJOR_DECISIONS.md` — documented consent scope, snapshot lifecycle, approved-org restriction decisions

### ⚠️ Known Unrelated Issues
- `Explore.Blazor.Client.Tests`: 28 failures in ThemeQuickSwitcher/MainLayout/NavMenu tests due to `IAppearanceThemeService` not registered in test context — pre-existing, unrelated to consent
- No commit was created (user did not request commit)

---

## Key Architectural Decisions

### D1: Consent scope is organizer-level (not event-level)
**Actual**: Unique index `(TenantId, UserId, RecipientActorId, PurposeCode)` — no `EventId`. SourceEventId is informational audit only.
**Why**: User-facing promise is "share with this organizer" not "share for this event." Prevents duplicate prompts across events by same org.

### D2: Schema uses `recipient_actor_id` (not `recipient_organization_id`)
**Why**: Future-proof for adding group/user recipients. Business logic restricts to approved org actors.

### D3: Email snapshot, never live
**Why**: Consent records must reflect what was shared at grant time, independent of later email changes.

### D4: No `ISoftDeletable` on consent entity
**Why**: Audit integrity. `ConsentStatus.Withdrawn` is the lifecycle mechanism.

### D5: Registration does NOT fail if consent grant fails
**Why**: Registration is the primary action. Consent is secondary. Try/catch + warning log.

### D6: Registration-intent FK (not child-registration FK)
**Why**: One `EventRegistrationIntent` can create multiple child `EventRegistration` rows. Consent audits the parent intent, not an arbitrary child row.
**Changed from original plan**: Original had `SourceEventRegistrationId` → Oracle flagged FK mismatch → renamed to `SourceEventRegistrationIntentId`.

### D7: Authorization uses server-resolved OrganizationId
**Why**: Cerbos policies check org membership. Client-sent `RecipientActorId` could differ from actual org id. Controller resolves actor → org before authorization.
**Changed from original plan**: Original used `RecipientActorId` as both resource id and org id → Oracle flagged policy mismatch → controller now resolves actual org id.

---

## Build & Test Verification (2026-04-28)

| Project | Errors | Warnings | Notes |
|---|---|---|---|
| Explore.Application | 0 | 135 | Pre-existing CA analyzer warnings |
| Explore.Persistence | 0 | 168 | Pre-existing CA warnings |
| Explore.API | 0 | 12 | NU1902 MailKit, NU1510 pruning |
| Explore.Blazor.Client | 0 | 1 | ASPDEPR001 |
| Explore.Blazor | 0 | 72 | Pre-existing NU/CS/CA warnings |
| Event.Domain.UnitTests | — | — | 218/218 passed |
| Event.Application.UnitTests | — | — | 976/976 passed |
| Explore.Blazor.Client.Tests | 28 fails | — | Pre-existing ThemeQuickSwitcher setup |

---

## Files Modified This Session

| File | Change |
|---|---|
| `Explore.Application/DTOs/EventRegistration/CreateEventRegistrationDto.cs` | Added 3 consent properties |
| `Explore.Application/Features/EventRegistrations/Handlers/Commands/CreateEventRegistrationCommandHandler.cs` | Injected consent service + fail-safe call |
| `Explore.Domain/EventContactShareConsent.cs` | Renamed FK to SourceEventRegistrationIntentId |
| `Explore.Persistence/Configurations/Entities/EventContactShareConsentConfiguration.cs` | Updated FK config |
| `Explore.Application/Contracts/Services/IContactShareConsentService.cs` | Renamed param |
| `Explore.Application/Services/ContactShareConsentService.cs` | Updated assignments |
| `Explore.Application/Features/ContactShareConsents/Requests/Queries/GetOrganizationSharedContactsQuery.cs` | Added OrganizationId, fixed auth attrs |
| `Explore.Application/Features/ContactShareConsents/Requests/Commands/ExportSharedContactsCommand.cs` | Same |
| `Explore.API/Controllers/ContactShareConsentController.cs` | Injected IActorRepository, resolve org id |
| `Explore.Persistence/ExploreDbContext.DbSets.cs` | Added 2 export DbSets |
| `Explore.Blazor.Client/Clients/EventApiClient.g.cs` | Added 3 DTO properties |
| `Explore.Blazor.Client/Pages/Events/Components/EventRegistration.razor` | Consent checkbox UI |
| `Explore.Blazor.Client/Pages/Events/EventDetail.razor.cs` | Pass consent params |
| `Explore.Blazor.Client/Pages/Events/EventList.razor.cs` | Pass consent params |
| `Explore.Blazor.Client/Pages/User/Components/SettingsLayout.razor` | Connected Apps nav + query params |
| `Explore.Persistence/Migrations/Appearance/20260428193206_UseRegistrationIntentForContactShareConsent.cs` | FK rename migration |
| `Event.Application.UnitTests/Services/ContactShareConsentServiceTests.cs` | Updated FK assertions |
| `Event.Domain.UnitTests/Entities/EventContactShareConsentTests.cs` | Updated FK assertions |
| `Event.Application.UnitTests/Features/ContactShareConsents/Queries/GetOrganizationSharedContactsQueryHandlerTests.cs` | Added auth context test |
| `Event.Application.UnitTests/Features/ContactShareConsents/Commands/ExportSharedContactsCommandHandlerTests.cs` | Added auth context test |
| `schemas/islamu-event.md` | DBML notes + FK reference |
| `dev/_journal/MAJOR_DECISIONS.md` | Consent decisions |

---

## Quick Resume Instructions

1. The feature is **fully implemented and verified** — no remaining implementation work
2. To commit: review `git status --short` (many unrelated dirty-tree changes present from concurrent workstreams)
3. To test: `dotnet test --project Event.Application.UnitTests` (976/976) + `dotnet test --project Event.Domain.UnitTests` (218/218)
4. Known gap: `Explore.Blazor.Client.Tests` has 28 pre-existing failures in ThemeQuickSwitcher tests — not consent-related
5. NSwag client was directly edited (not regenerated via swagger.json) — next API startup in Development will regenerate `EventApiClient.g.cs`
6. Migration `20260428193206_UseRegistrationIntentForContactShareConsent.cs` is ready to apply
