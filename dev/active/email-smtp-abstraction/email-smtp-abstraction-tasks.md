# Email SMTP Abstraction — Task Checklist

> Last Updated: 2026-02-10

---

## Phase 1: Application Layer — Contracts & Models ✅ COMPLETE
**Effort: S (~1h) | No external dependencies | Pure C#**

- [x] **1.1** Create `EmailMessage` model (`Explore.Application/Models/EmailMessage.cs`)
- [x] **1.2** Create `EmailAttachment` model (`Explore.Application/Models/EmailAttachment.cs`)
- [x] **1.3** Create `EmailResult` model (`Explore.Application/Models/EmailResult.cs`)
- [x] **1.4** Create `SmtpConfiguration` model + `SmtpSecurityMode` enum (`Explore.Application/Models/SmtpConfiguration.cs`)
- [x] **1.5** Create `IEmailService` interface (`Explore.Application/Contracts/Infrastructure/IEmailService.cs`)
- [x] **1.6** Create `ISmtpConfigResolver` interface (`Explore.Application/Contracts/Infrastructure/ISmtpConfigResolver.cs`)

---

## Phase 2: Infrastructure Layer — MailKit Implementation ✅ COMPLETE
**Effort: M (~3h) | Depends on: Phase 1**

- [x] **2.1** Add MailKit NuGet package (Directory.Packages.props + csproj)
- [x] **2.2** Implement `SmtpConfigResolver` (`Explore.Infrastructure/Mail/SmtpConfigResolver.cs`)
- [x] **2.3** Implement `EmailResiliencePipelines` (`Explore.Infrastructure/Mail/EmailResiliencePipelines.cs`)
- [x] **2.4** Implement `SmtpEmailService` (`Explore.Infrastructure/Mail/SmtpEmailService.cs`)
- [x] **2.5** Update `InfrastructureServicesRegistration.cs` — replaced old email DI

---

## Phase 3: Persistence — Settings Seed Data ✅ COMPLETE
**Effort: S (~30min) | Depends on: Phase 1**

- [x] **3.1** Add email setting keys to `GovernanceSettingKeys.cs` (9 keys: email.smtp_*)
- [x] **3.2** Add seed IDs to `SeedIds.cs` (IDs 0520-0528)
- [x] **3.3** Add email SystemSetting seed entries to `LookupTableSeeder.cs` (9 settings, Category="Email")
- [x] **3.4** Update `SmtpConfigResolver` to use `GovernanceSettingKeys` constants (no hardcoded strings)

---

## Phase 4: Update Existing Consumers ✅ COMPLETE
**Effort: S (~30min) | Depends on: Phase 2**

- [x] **4.1** Verify no handlers/services reference old `IEmailSender` (confirmed: zero consumers)
- [x] **4.2** Update `docs/CODEBASE_STRUCTURE.md` to reflect new email files
- [x] **4.3** Report obsolete files for user deletion (see below)

**Files to delete (user action required):**
- `Explore.Application/Models/Email.cs` → replaced by `EmailMessage.cs`
- `Explore.Application/Models/EmailSettings.cs` → replaced by DB-stored config via `SmtpConfiguration.cs`
- `Explore.Infrastructure/Mail/EmailSender.cs` → replaced by `SmtpEmailService.cs`
- `Explore.Application/Contracts/Infrastructure/IEmailSender.cs` → replaced by `IEmailService.cs`

---

## Phase 5: Testing 🟡 IN PROGRESS
**Effort: M (~2h) | Depends on: Phase 2**

- [ ] **5.1** Unit tests for `EmailResiliencePipelines` (IsTransient classification)
- [ ] **5.2** Unit tests for `SmtpConfigResolver` (config resolution, caching, null handling)
- [ ] **5.3** Unit tests for `EmailResult` (static factories, properties)

---

## Phase 6: Admin API Endpoint ⏳ NOT STARTED
**Effort: S (~30min) | Depends on: Phase 2**

- [ ] **6.1** Add `POST /api/v1/admin/email/test-connection` endpoint
- [ ] **6.2** Add `POST /api/v1/admin/email/send-test` endpoint (optional)

---

## Summary

| Phase | Status | Tasks | Effort |
|-------|--------|-------|--------|
| 1. Application Contracts | ✅ Complete | 6 tasks | S (~1h) |
| 2. Infrastructure Implementation | ✅ Complete | 5 tasks | M (~3h) |
| 3. Persistence Seed Data | ✅ Complete | 4 tasks | S (~30min) |
| 4. Update Consumers | ✅ Complete | 3 tasks | S (~30min) |
| 5. Testing | 🟡 In Progress | 3 tasks | M (~1h) |
| 6. Admin Endpoint | ⏳ Not Started | 2 tasks | S (~30min) |
| **Total** | | **23 tasks** | **~6-7h** |
