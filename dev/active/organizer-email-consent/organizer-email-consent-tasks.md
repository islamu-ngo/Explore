# Organizer Email Consent — Task Checklist

> Last Updated: 2026-03-12

---

## Phase 0: Pre-flight Baseline ⏳ NOT STARTED

- [ ] **0.1** Build baseline — `dotnet build --configuration Release --verbosity quiet` (0 errors expected)
- [ ] **0.2** Test baseline — run all 7 test projects individually, document pass/fail state

---

## Phase 1: Domain Layer ⏳ NOT STARTED

- [ ] **1.1** `Explore.Domain/Enums/ConsentStatus.cs` — `Granted = 1, Withdrawn = 2`
- [ ] **1.2** `Explore.Domain/EventContactShareConsent.cs` — main consent entity (ITenantEntity + IAuditableEntity, no ISoftDeletable)
- [ ] **1.3** `Explore.Domain/EventContactShareExport.cs` — export audit header entity (ITenantEntity)
- [ ] **1.4** `Explore.Domain/EventContactShareExportItem.cs` — export audit item entity (composite PK)
- [ ] **1.5** `Explore.Domain/Constants/ConsentPurposeCodes.cs` — `OrganizerFutureCommunications = "ORGANIZER_FUTURE_COMMUNICATIONS"`
- [ ] **1.6** `Explore.Domain/Constants/ConsentUiVersions.cs` — `V1 = "v1"`
- [ ] Build passes after Phase 1

---

## Phase 2: Persistence Layer ⏳ NOT STARTED

- [ ] **2.1** `Explore.Persistence/Configurations/Entities/EventContactShareConsentConfiguration.cs`
  - [ ] Table name `event_contact_share_consents`
  - [ ] Unique index `ix_eventcontactshare_scope_unique` on `(TenantId, EventId, UserId, RecipientActorId, PurposeCode)`
  - [ ] Index `ix_eventcontactshare_recipient_status` on `(TenantId, RecipientActorId, Status, EventId)`
  - [ ] Index `ix_eventcontactshare_user_status` on `(TenantId, UserId, Status)`
  - [ ] FK: SourceEventRegistrationId = SetNull, others = Restrict
  - [ ] Column lengths: EmailSnapshot/Normalized = 320, PurposeCode = 100, ConsentUiVersion = 20, ConsentTextSnapshot = text
  - [ ] Tenant query filter applied

- [ ] **2.2** `Explore.Persistence/Configurations/Entities/EventContactShareExportConfiguration.cs`
  - [ ] Table name `event_contact_share_exports`
  - [ ] Index on `(TenantId, RecipientActorId, CreatedAt)`

- [ ] **2.3** `Explore.Persistence/Configurations/Entities/EventContactShareExportItemConfiguration.cs`
  - [ ] Table name `event_contact_share_export_items`
  - [ ] Composite PK `(ExportId, ConsentId)`
  - [ ] FK ExportId → cascade, ConsentId → restrict

- [ ] **2.4** `Explore.Persistence/ExploreDbContext.cs` — add 3 DbSet + ApplyConfiguration calls

- [ ] **2.5** `Explore.Application/Contracts/Persistence/IEventContactShareConsentRepository.cs`
  - [ ] `GetByScope(...)` method signature
  - [ ] `GetGrantedForOrganiser(...)` method signature
  - [ ] `GetByUser(...)` method signature

- [ ] **2.6** `Explore.Persistence/Repositories/EventContactShareConsentRepository.cs` — implement interface

- [ ] **2.7** `Explore.Application/Contracts/Persistence/IEventContactShareExportRepository.cs` + implementation

- [ ] **2.8** Permission seed data — add `event_contact_share_consent:view_contacts` and `event_contact_share_consent:export_contacts`

- [ ] **2.9** Migration: `dotnet ef migrations add AddEventContactShareConsent --project Explore.Persistence --startup-project Explore.API`
  - [ ] Review generated SQL
  - [ ] Confirm all tables, indexes, FK constraints correct

- [ ] Build passes after Phase 2

---

## Phase 3: Application Layer ⏳ NOT STARTED

- [ ] **3.1** `Explore.Application/DTOs/EventRegistration/CreateEventRegistrationDto.cs` — add `ShareEmailWithOrganizer`, `ConsentTextAcknowledged`, `ConsentUiVersion`

- [ ] **3.2** New DTOs:
  - [ ] `Explore.Application/DTOs/EventContactShareConsent/EventContactShareConsentDto.cs`
  - [ ] `Explore.Application/DTOs/EventContactShareConsent/EventContactShareConsentListDto.cs`
  - [ ] `Explore.Application/DTOs/EventContactShareConsent/ConsentStatusForEventDto.cs`

- [ ] **3.3** `Explore.Application/Contracts/Application/IConsentService.cs` + `Explore.Application/Services/ConsentService.cs`
  - [ ] `GrantAsync` — resolves publisher org, validates approved, snapshots email, upserts consent
  - [ ] `GetStatusForEventAsync` — resolves publisher org + existing consent status
  - [ ] Fails safe — never fails the registration

- [ ] **3.4** Modify `CreateEventRegistrationCommandHandler` — inject `IConsentService`, call on success when `ShareEmailWithOrganizer == true`

- [ ] **3.5** `WithdrawConsentCommand` + `WithdrawConsentCommandHandler`
  - [ ] Verifies `consent.UserId == command.UserId` (own consent only)
  - [ ] Sets `Status = Withdrawn, WithdrawnAt = DateTime.UtcNow`

- [ ] **3.6** `GetMyConsentsRequest` + `GetMyConsentsRequestHandler` — returns all consents for current user

- [ ] **3.7** `GetConsentStatusForEventRequest` + `GetConsentStatusForEventRequestHandler`
  - [ ] Returns `ConsentStatusForEventDto` including `PublisherIsApprovedOrg` and existing consent status

- [ ] **3.8** `GetOrgConsentsRequest` + `GetOrgConsentsRequestHandler`
  - [ ] `[AuthorizeResource("event_contact_share_consent", "view_contacts")]` + `ISecureRequest` with `organizationId`
  - [ ] Returns paginated `PaginatedResult<EventContactShareConsentListDto>` (Granted only)

- [ ] **3.9** `ExportConsentsRequest` + `ExportConsentsRequestHandler`
  - [ ] `[AuthorizeResource("event_contact_share_consent", "export_contacts")]` + `ISecureRequest`
  - [ ] Builds CSV/TSV in-memory with StringBuilder
  - [ ] Writes `EventContactShareExport` + `EventContactShareExportItem` audit rows
  - [ ] Returns `ExportConsentsResult` with file bytes + metadata

- [ ] **3.10** `Explore.Application/Profiles/EventContactShareConsentProfile.cs` — AutoMapper mapping

- [ ] Register `IConsentService` in DI (`Explore.Application` or `Explore.Persistence` service registration extension)

- [ ] Build passes after Phase 3

---

## Phase 4: API Layer ⏳ NOT STARTED

- [ ] **4.1** `Explore.API/Controllers/EventContactShareConsentController.cs`
  - [ ] `GET /api/eventcontactshareconsent/my-consents` — `[Authorize]`
  - [ ] `GET /api/eventcontactshareconsent/for-event/{eventId}` — `[Authorize]`
  - [ ] `POST /api/eventcontactshareconsent/{id}/withdraw` — `[Authorize]`, `[EnableRateLimiting("write")]`
  - [ ] `GET /api/eventcontactshareconsent/org/{orgId}/contacts` — `[Authorize]`
  - [ ] `GET /api/eventcontactshareconsent/org/{orgId}/export` — `[Authorize]`, `[RequestTimeout("Complex")]`
  - [ ] Export returns `FileContentResult` with `Content-Disposition: attachment`
  - [ ] All routes named in `RouteNames`
  - [ ] All actions have `[ProducesResponseType]`

- [ ] **4.2** `cerbos/policies/event_contact_share_consent.yaml`
  - [ ] `instance_admin` → all
  - [ ] `tenant_admin` → view_contacts, export_contacts, manage
  - [ ] `org_admin` → view_contacts, export_contacts (resource.attr.organizationId enforced)
  - [ ] `authenticated_user` → update (withdraw own consent, condition: ownerUserId == principal.id)

- [ ] **4.3** `Explore.API/Utilities/RouteNames.cs` — add 5 new constants

- [ ] Build passes after Phase 4

---

## Phase 5: Blazor UI ⏳ NOT STARTED

- [ ] **5.1** Modify `Explore.Blazor.Client/Pages/Events/Components/EventRegistration.razor`
  - [ ] Load `ConsentStatusForEventDto` on component init
  - [ ] If `PublisherIsApprovedOrg == false`: no consent UI at all
  - [ ] If `Status == Granted`: show info notice with org name and link to Connected Apps
  - [ ] If `Status == null or Withdrawn`: show unchecked `MudCheckBox` with consent text naming the org
  - [ ] On form submit: include `ShareEmailWithOrganizer`, `ConsentTextAcknowledged`, `ConsentUiVersion`
  - [ ] `EventRegistration.razor.css`: `.consent-section`, `.consent-info-notice` styles

- [ ] **5.2** `Explore.Blazor.Client/Services/IConsentService.cs` + `ConsentService.cs`
  - [ ] `GetConsentStatusForEventAsync(Guid eventId)`
  - [ ] `GetMyConsentsAsync()`
  - [ ] `WithdrawConsentAsync(Guid consentId)`

- [ ] **5.3** Register `IConsentService` in `ServiceCollectionExtensions.AddSharedApplicationServices()`

- [ ] **5.4** `Explore.Blazor.Client/Pages/Account/ConnectedApps.razor`
  - [ ] Route: `/account/connected-apps`
  - [ ] `MudDataGrid` with Organisation, Event, Granted At, Status columns
  - [ ] Status badges: green = Granted, grey = Withdrawn
  - [ ] Withdraw button → MudDialog with explicit warning text
  - [ ] Warning: "Does not guarantee removal from mailing lists"
  - [ ] Future integrations section stub: "More connected apps will appear here in the future"
  - [ ] Empty state shown when no consents
  - [ ] `ConnectedApps.razor.css`: `.connected-apps-page`, `.status-badge--granted`, `.status-badge--withdrawn`, `.withdrawal-warning`

- [ ] **5.5** `Explore.Blazor.Client/Pages/Organizations/OrgContactsPage.razor`
  - [ ] Route: `/org/{OrgId:guid}/contacts`
  - [ ] Event filter dropdown
  - [ ] `MudDataGrid` with Email, Event, Granted At, Purpose columns
  - [ ] "Export CSV" and "Export TSV" buttons triggering file download
  - [ ] Empty state for no consents
  - [ ] `OrgContactsPage.razor.css`

- [ ] **5.6** Add "Connected Apps" link to account navigation sidebar/menu

- [ ] Visual verification via Playwriter (see CLAUDE.md for workflow)

---

## Phase 6: Tests ⏳ NOT STARTED

### Unit Tests (Event.Application.UnitTests)

- [ ] **6.1** `ConsentServiceTests.cs`
  - [ ] `Registration_WithUncheckedCheckbox_DoesNotCreateConsent`
  - [ ] `Registration_WithCheckedCheckbox_CreatesGrantedConsentWithEmailSnapshot`
  - [ ] `EmailSnapshot_NotUpdated_WhenUserChangesEmail`
  - [ ] `ReRegistration_SameEvent_DoesNotCreateDuplicate`
  - [ ] `WithdrawConsent_SetsWithdrawnStatus`
  - [ ] `NonOrganisationActor_CannotReceiveConsent`
  - [ ] `UnapprovedOrganisation_CannotReceiveConsent`
  - [ ] `ConsentText_IncludesOrganisationName`

- [ ] **6.2** `ExportConsentsHandlerTests.cs`
  - [ ] `Export_OnlyIncludesGrantedConsents`
  - [ ] `Export_CsvFormat_HeaderAndRows`
  - [ ] `Export_TsvFormat_HeaderAndRows`
  - [ ] `Export_WritesAuditRows`

### Integration Tests (Event.API.IntegrationTests)

- [ ] **6.3** `EventContactShareConsentControllerTests.cs`
  - [ ] `UnauthorisedOrgMember_CannotViewContacts` → 403
  - [ ] `AuthorisedOrgMember_CanViewOwnOrgContacts` → 200
  - [ ] `AuthorisedOrgMember_CannotViewOtherOrgContacts` → 403
  - [ ] `User_CanWithdrawOwnConsent` → 200, status Withdrawn
  - [ ] `User_CannotWithdrawOtherUsersConsent` → 403

- [ ] All 7 test projects pass individually after Phase 6

---

## Phase 7: Schema Docs + Journal ⏳ NOT STARTED

- [ ] **7.1** Create `schema/islamu-event.md` — full DB schema reference including 3 new tables
- [ ] **7.2** Update `dev/_journal/MAJOR_DECISIONS.md` — document consent scope, actor-based schema, email snapshot rationale

---

## Post-Implementation Verification

- [ ] `dotnet build --configuration Release --verbosity quiet` → 0 errors
- [ ] All 7 test projects pass
- [ ] NSwag regeneration: `EventApiClient.g.cs` updated with new endpoints
- [ ] Cerbos policy validated (run `cerbos compile cerbos/policies` if cerbos CLI available)
- [ ] Blazor visual test: registration checkbox appears for approved org events
- [ ] Blazor visual test: checkbox not shown after consent granted (info notice instead)
- [ ] Blazor visual test: Connected Apps page shows consent rows + withdrawal dialog

---

## Summary Counts

| Phase | Tasks | New Files | Modified Files |
|---|---|---|---|
| 0 | 2 | 0 | 0 |
| 1 | 6 | 6 | 0 |
| 2 | 9 | 8 | 1 |
| 3 | 10 | 14 | 2 |
| 4 | 3 | 2 | 1 |
| 5 | 6 | 7 | 2 |
| 6 | 3 | 3 | 0 |
| 7 | 2 | 1 | 1 |
| **Total** | **41** | **41** | **7** |
