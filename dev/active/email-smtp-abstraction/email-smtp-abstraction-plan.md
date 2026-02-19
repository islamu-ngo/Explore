# Email SMTP Abstraction — Implementation Plan

> **Provider-Agnostic Email Service for Self-Hosted Multi-Tenant Platform**
>
> Last Updated: 2026-02-10

---

## Executive Summary

Replace the current stub email implementation (`EmailSender` returns `false`) with a **provider-agnostic SMTP email service** powered by **MailKit**. Self-hosters can configure **any SMTP server** (SendGrid, Mailgun, Amazon SES, Office 365, Gmail, Postfix, etc.) through the existing cascading settings engine — no code changes needed. SMTP credentials are stored encrypted in the database using the existing `AppSetting` AES-256-GCM infrastructure. Per-tenant SMTP overrides are supported via `SystemSetting`/`TenantSetting`.

**Why MailKit?**
- Microsoft officially recommends MailKit as the replacement for the deprecated `System.Net.Mail.SmtpClient`
- Actively maintained, 300M+ NuGet downloads, .NET Foundation project
- Full SMTP protocol support: STARTTLS, SSL/TLS, OAuth2, PLAIN, LOGIN, XOAUTH2
- Works with every SMTP provider through standard protocol — no vendor lock-in

**Why NOT FluentEmail?** Abandoned (3+ years no updates, no .NET 10 support).
**Why NOT System.Net.Mail?** Officially deprecated by Microsoft.

---

## Current State Analysis

### Existing Email Infrastructure

| File | Purpose | Status |
|------|---------|--------|
| `Explore.Application/Contracts/Infrastructure/IEmailSender.cs` | Interface: `Task<bool> SendEmail(Email email)` | Minimal — single recipient, no CC/BCC/HTML |
| `Explore.Application/Models/Email.cs` | DTO: `To`, `Subject`, `Body` (all `required string`) | Too simple — no HTML, attachments, headers |
| `Explore.Application/Models/EmailSettings.cs` | Config: `ApiKey`, `FromAddress`, `FromName` | Coupled to SendGrid — not generic SMTP |
| `Explore.Infrastructure/Mail/EmailSender.cs` | Implementation: **returns `false`** (commented-out SendGrid code) | Non-functional stub |
| `InfrastructureServicesRegistration.cs:23-24` | Registration: `Configure<EmailSettings>` + `AddTransient<IEmailSender, EmailSender>` | Reads from `appsettings.json` only |

### Existing Infrastructure We Will Leverage

| Component | What It Does | How We Use It |
|-----------|-------------|---------------|
| **`ISettingsResolver`** | 3-tier cascading: System → Tenant (respects `IsLocked`) | Resolve SMTP host, port, from-address, TLS mode per-tenant |
| **`SystemSetting`** | Global defaults with `IsLocked`, `Category`, `AllowedValues` | Seed email.* settings with sensible defaults |
| **`TenantSetting`** | Tenant-specific overrides (ITenantEntity) | Tenants can bring their own SMTP provider |
| **`AppSetting`** | AES-256-GCM encrypted config with key versioning | Store SMTP passwords securely |
| **`IAppSettingRepository`** | CRUD + decryption for AppSetting | Read/write encrypted SMTP credentials |
| **Polly 8.6.5** | Already in `Directory.Packages.props` | Retry with exponential backoff on transient failures |
| **MemoryCache** | Already registered in DI (`services.AddMemoryCache()`) | Cache resolved SMTP config to avoid DB reads per send |

---

## Proposed Architecture

### Clean Architecture Layer Placement

```
┌─────────────────────────────────────────────────────────────────┐
│  Explore.Application (Contracts + Models)                       │
│  ┌─────────────────────────────────────────────────────────────┐│
│  │ Contracts/Infrastructure/IEmailService.cs       (interface) ││
│  │ Contracts/Infrastructure/ISmtpConfigResolver.cs  (interface) ││
│  │ Models/EmailMessage.cs                          (DTO)       ││
│  │ Models/EmailAttachment.cs                       (DTO)       ││
│  │ Models/EmailResult.cs                           (result)    ││
│  │ Models/SmtpConfiguration.cs                     (config)    ││
│  └─────────────────────────────────────────────────────────────┘│
├─────────────────────────────────────────────────────────────────┤
│  Explore.Infrastructure (Implementations)                       │
│  ┌─────────────────────────────────────────────────────────────┐│
│  │ Mail/SmtpEmailService.cs       (MailKit SMTP send)         ││
│  │ Mail/SmtpConfigResolver.cs     (ISettingsResolver + cache) ││
│  │ Mail/EmailResiliencePipelines.cs (Polly retry policies)    ││
│  └─────────────────────────────────────────────────────────────┘│
├─────────────────────────────────────────────────────────────────┤
│  Explore.Persistence (Seed Data)                                │
│  ┌─────────────────────────────────────────────────────────────┐│
│  │ Seed/EmailSettingsSeedData.cs   (SystemSetting seed)       ││
│  │ Migration: SeedEmailSmtpSettings                           ││
│  └─────────────────────────────────────────────────────────────┘│
└─────────────────────────────────────────────────────────────────┘
```

### Data Flow

```
1. Handler calls IEmailService.SendAsync(EmailMessage)
         │
2. SmtpEmailService resolves SMTP config
         │  ├── ISmtpConfigResolver.ResolveAsync()
         │  │   ├── ISettingsResolver.GetSettingAsync("email.smtp_host", tenantId)
         │  │   ├── ISettingsResolver.GetSettingAsync("email.smtp_port", tenantId)
         │  │   ├── ISettingsResolver.GetSettingAsync("email.smtp_security", tenantId)
         │  │   ├── ISettingsResolver.GetSettingAsync("email.from_address", tenantId)
         │  │   ├── ISettingsResolver.GetSettingAsync("email.from_name", tenantId)
         │  │   └── (cached for 5 min per tenant)
         │  └── IAppSettingRepository for encrypted password
         │
3. SmtpEmailService builds MimeMessage (MailKit)
         │  ├── From, To, CC, BCC, Subject
         │  ├── HTML + PlainText multipart body
         │  └── Attachments + inline images
         │
4. SmtpEmailService sends via MailKit SmtpClient
         │  ├── ConnectAsync(host, port, SecureSocketOptions)
         │  ├── AuthenticateAsync(username, password) — if credentials exist
         │  ├── SendAsync(mimeMessage)
         │  └── DisconnectAsync(quit: true)
         │
5. Polly retry pipeline wraps step 4
         │  ├── 3 retries on transient failures (421, 451, timeout)
         │  ├── Exponential backoff: 2s → 4s → 8s
         │  └── No retry on permanent errors (550, 553, auth failure)
         │
6. Returns EmailResult (Success/Fail + timing + error details)
```

### SMTP Configuration Storage

Two storage mechanisms, each for its purpose:

| Setting | Storage | Why |
|---------|---------|-----|
| `email.smtp_host` | SystemSetting/TenantSetting | Non-sensitive, cascading resolution |
| `email.smtp_port` | SystemSetting/TenantSetting | Non-sensitive, cascading resolution |
| `email.smtp_security` | SystemSetting/TenantSetting | Non-sensitive, enum: None/StartTls/SslOnConnect/Auto |
| `email.from_address` | SystemSetting/TenantSetting | Non-sensitive, per-tenant customizable |
| `email.from_name` | SystemSetting/TenantSetting | Non-sensitive, per-tenant customizable |
| `email.smtp_username` | SystemSetting/TenantSetting | Low-sensitivity (often public like "apikey") |
| `email.smtp_timeout_seconds` | SystemSetting (**IsLocked=true**) | Security — tenants should not change |
| `email.smtp_skip_cert_validation` | SystemSetting (**IsLocked=true**) | Security — never allow tenants to bypass TLS |
| `email.smtp_password` | **AppSetting** (AES-256-GCM encrypted) | High-sensitivity — encrypted at rest |

### Provider Compatibility Matrix

Every major provider works with standard SMTP — no code changes, just database settings:

| Provider | Host | Port | Security | Username | Password |
|----------|------|------|----------|----------|----------|
| **SendGrid** | `smtp.sendgrid.net` | 587 | StartTls | `apikey` (literal) | API key |
| **Amazon SES** | `email-smtp.{region}.amazonaws.com` | 587 | StartTls | IAM SMTP credentials | IAM SMTP password |
| **Mailgun** | `smtp.mailgun.org` | 587 | StartTls | `postmaster@{domain}` | Mailgun SMTP password |
| **Microsoft 365** | `smtp.office365.com` | 587 | StartTls | Email address | App password |
| **Google Workspace** | `smtp.gmail.com` | 587 | StartTls | Email address | App password |
| **Postfix (self-hosted)** | `mail.yourdomain.com` | 25/587 | StartTls/None | (optional) | (optional) |
| **Mailtrap (testing)** | `sandbox.smtp.mailtrap.io` | 587 | StartTls | API token | API token |

---

## Implementation Phases

### Phase 1: Application Layer — Contracts & Models (Effort: S)

Define the email abstractions that the rest of the system depends on. Zero external dependencies.

**Relevant Skills**: `clean-architecture-rules`

#### Task 1.1: Create `EmailMessage` model
- **File**: `Explore.Application/Models/EmailMessage.cs`
- **What**: Replace the minimal `Email` class with a full-featured DTO
- **Properties**:
  - `string To` (required) — primary recipient
  - `List<string> Cc` — carbon copy recipients
  - `List<string> Bcc` — blind carbon copy recipients
  - `string Subject` (required)
  - `string? HtmlBody` — HTML content
  - `string? PlainTextBody` — plain text fallback
  - `string? FromName` — override per-message (null = use SMTP config)
  - `string? FromAddress` — override per-message (null = use SMTP config)
  - `string? ReplyTo` — reply-to address
  - `List<EmailAttachment> Attachments`
  - `Dictionary<string, string> CustomHeaders`
- **Acceptance Criteria**:
  - [ ] File-scoped namespace: `Explore.Application.Models`
  - [ ] ABOUTME comment at top
  - [ ] No external dependencies — plain C# only
  - [ ] `required` keyword on `To` and `Subject`
  - [ ] Collection properties initialized with `= []` (C# 12+)

#### Task 1.2: Create `EmailAttachment` model
- **File**: `Explore.Application/Models/EmailAttachment.cs`
- **What**: Attachment DTO supporting file attachments and inline images
- **Properties**:
  - `string FileName` (required)
  - `byte[] Content` (required)
  - `string ContentType` (required) — MIME type
  - `bool IsInline` — for embedded images
  - `string? ContentId` — CID for inline images
- **Acceptance Criteria**:
  - [ ] File-scoped namespace
  - [ ] ABOUTME comment
  - [ ] All required fields use `required` keyword

#### Task 1.3: Create `EmailResult` model
- **File**: `Explore.Application/Models/EmailResult.cs`
- **What**: Result type for email send operations
- **Properties**:
  - `bool Success`
  - `string? Message`
  - `string? ErrorMessage`
  - `TimeSpan? Duration`
- **Static factories**: `EmailResult.Ok(...)`, `EmailResult.Fail(...)`
- **Acceptance Criteria**:
  - [ ] File-scoped namespace
  - [ ] ABOUTME comment
  - [ ] Static factory methods for construction

#### Task 1.4: Create `SmtpConfiguration` model
- **File**: `Explore.Application/Models/SmtpConfiguration.cs`
- **What**: Strongly-typed SMTP connection parameters resolved from settings
- **Properties**:
  - `string Host` (required)
  - `int Port` (default: 587)
  - `string? Username`
  - `string? Password` (decrypted — never logged)
  - `SmtpSecurityMode Security` (enum)
  - `string FromAddress` (required)
  - `string FromName` (default: "Explore")
  - `int TimeoutSeconds` (default: 30)
  - `bool SkipCertificateValidation` (default: false)
- **Enum**: `SmtpSecurityMode { None = 0, StartTls = 1, SslOnConnect = 2, Auto = 3 }`
- **Acceptance Criteria**:
  - [ ] File-scoped namespace
  - [ ] ABOUTME comment
  - [ ] Enum defined in same file (small, tightly coupled)
  - [ ] `required` on Host and FromAddress

#### Task 1.5: Create `IEmailService` interface
- **File**: `Explore.Application/Contracts/Infrastructure/IEmailService.cs`
- **What**: Replace `IEmailSender` with a richer contract
- **Methods**:
  - `Task<EmailResult> SendAsync(EmailMessage message, CancellationToken ct = default)`
  - `Task<EmailResult> TestConnectionAsync(CancellationToken ct = default)`
- **Acceptance Criteria**:
  - [ ] File-scoped namespace: `Explore.Application.Contracts.Infrastructure`
  - [ ] ABOUTME comment
  - [ ] XML documentation on interface and methods
  - [ ] Uses `EmailMessage`, `EmailResult` from Models
  - [ ] CancellationToken on all async methods

#### Task 1.6: Create `ISmtpConfigResolver` interface
- **File**: `Explore.Application/Contracts/Infrastructure/ISmtpConfigResolver.cs`
- **What**: Contract for resolving SMTP config from the cascading settings engine
- **Methods**:
  - `Task<SmtpConfiguration?> ResolveAsync(CancellationToken ct = default)`
  - `void InvalidateCache(Guid? tenantId = null)`
- **Acceptance Criteria**:
  - [ ] File-scoped namespace
  - [ ] ABOUTME comment
  - [ ] XML documentation explaining cascading resolution
  - [ ] Returns `null` when SMTP is not configured (host empty)

---

### Phase 2: Infrastructure Layer — MailKit Implementation (Effort: M)

Implement the actual SMTP sending using MailKit and wire up configuration resolution.

**Relevant Skills**: `clean-architecture-rules`, `dotnet-efcore-guidelines`

#### Task 2.1: Add MailKit NuGet package
- **File**: `Directory.Packages.props` (add version) + `Explore.Infrastructure/Explore.Infrastructure.csproj` (add reference)
- **What**: Add MailKit as a centrally managed package
- **Version**: Latest stable (4.x)
- **Acceptance Criteria**:
  - [ ] `<PackageVersion Include="MailKit" Version="4.x.x" />` in `Directory.Packages.props`
  - [ ] `<PackageReference Include="MailKit" />` in `Explore.Infrastructure.csproj`
  - [ ] `dotnet build` passes

#### Task 2.2: Implement `SmtpConfigResolver`
- **File**: `Explore.Infrastructure/Mail/SmtpConfigResolver.cs`
- **What**: Resolves SMTP configuration from `ISettingsResolver` + `IAppSettingRepository`
- **Dependencies**: `ISettingsResolver`, `IAppSettingRepository` (for encrypted password), `ITenantContext`, `IMemoryCache`
- **Logic**:
  1. Check memory cache first (key: `SmtpConfig:{tenantId}`, 5 min TTL)
  2. Resolve non-sensitive settings via `ISettingsResolver.GetSettingAsync<T>(key, tenantId)`
  3. Resolve password from `IAppSettingRepository` (encrypted) — use tenant-scoped key
  4. Return `SmtpConfiguration` or `null` if host is empty
- **Setting Keys** (constants in the class):
  - `email.smtp_host`, `email.smtp_port`, `email.smtp_username`
  - `email.smtp_security`, `email.from_address`, `email.from_name`
  - `email.smtp_timeout_seconds`, `email.smtp_skip_cert_validation`
- **Acceptance Criteria**:
  - [ ] File-scoped namespace: `Explore.Infrastructure.Mail`
  - [ ] ABOUTME comment
  - [ ] Caches resolved config per-tenant (5 min)
  - [ ] Returns null when host is empty/not configured
  - [ ] Never logs password values
  - [ ] `InvalidateCache` clears per-tenant or all

#### Task 2.3: Implement `EmailResiliencePipelines`
- **File**: `Explore.Infrastructure/Mail/EmailResiliencePipelines.cs`
- **What**: Polly v8 resilience pipeline for email send operations
- **Logic**:
  - 3 retry attempts with exponential backoff (2s base, jitter)
  - Retry on: timeout, connection errors, SMTP 421/451 (transient)
  - No retry on: auth failure, SMTP 5xx permanent errors
- **Acceptance Criteria**:
  - [ ] File-scoped namespace
  - [ ] ABOUTME comment
  - [ ] Uses Polly v8 `ResiliencePipelineBuilder`
  - [ ] Static factory method: `CreateSendPipeline()`
  - [ ] Clear separation of transient vs permanent failures

#### Task 2.4: Implement `SmtpEmailService`
- **File**: `Explore.Infrastructure/Mail/SmtpEmailService.cs`
- **What**: The core MailKit-based email sender
- **Dependencies**: `ISmtpConfigResolver`, `ILogger<SmtpEmailService>`
- **Logic**:
  1. Resolve SMTP config via `ISmtpConfigResolver`
  2. Build `MimeMessage` from `EmailMessage` (From, To, CC, BCC, Subject, Body, Attachments)
  3. Create new `MailKit.Net.Smtp.SmtpClient` per send (not shared — MailKit is not thread-safe)
  4. `ConnectAsync(host, port, secureSocketOptions)` — map `SmtpSecurityMode` to `SecureSocketOptions`
  5. `AuthenticateAsync(username, password)` — only if credentials are provided
  6. `SendAsync(mimeMessage)`
  7. `DisconnectAsync(quit: true)`
  8. Wrap in Polly retry pipeline
  9. Return `EmailResult` with timing and error details
- **Error Handling**:
  - `SmtpCommandException` → inspect `StatusCode` for transient (4xx) vs permanent (5xx)
  - `SmtpProtocolException` → protocol-level error
  - `TimeoutException`, `IOException` → connection error (retryable)
  - `AuthenticationException` → bad credentials (not retryable)
- **TestConnection**:
  - Connect + Authenticate + Disconnect (no send)
  - Returns `EmailResult` with connection timing
- **Acceptance Criteria**:
  - [ ] File-scoped namespace: `Explore.Infrastructure.Mail`
  - [ ] ABOUTME comment
  - [ ] Creates new SmtpClient per send operation (disposes after)
  - [ ] Maps `SmtpSecurityMode` → `SecureSocketOptions` correctly
  - [ ] Handles optional authentication (some SMTP servers don't require it)
  - [ ] Supports `SkipCertificateValidation` for self-signed certs (dev/self-hosted)
  - [ ] Never logs password or sensitive config
  - [ ] Structured logging with correlation (recipient, host:port, duration)
  - [ ] `CancellationToken` propagated to all MailKit async methods

#### Task 2.5: Update `InfrastructureServicesRegistration`
- **File**: `Explore.Infrastructure/InfrastructureServicesRegistration.cs`
- **What**: Replace old email registration with new services
- **Changes**:
  - Remove: `services.Configure<EmailSettings>(...)` and `services.AddTransient<IEmailSender, EmailSender>()`
  - Add: `services.AddScoped<ISmtpConfigResolver, SmtpConfigResolver>()`
  - Add: `services.AddScoped<IEmailService, SmtpEmailService>()`
- **Acceptance Criteria**:
  - [ ] Old `IEmailSender`/`EmailSender` registration removed
  - [ ] Old `EmailSettings` configuration removed
  - [ ] New services use `AddScoped` (depends on tenant context)
  - [ ] `dotnet build` passes

---

### Phase 3: Persistence — Settings Seed Data (Effort: S)

Seed the `SystemSetting` table with email SMTP defaults so the feature is configurable from day one.

**Relevant Skills**: `dotnet-efcore-guidelines`

#### Task 3.1: Create email settings seed data
- **File**: `Explore.Persistence/Seed/EmailSettingsSeedData.cs`
- **What**: Static class providing `SystemSetting` seed objects for email config
- **Settings to seed**:

| SettingKey | Value | ValueType | IsLocked | Category | Description |
|-----------|-------|-----------|----------|----------|-------------|
| `email.smtp_host` | `""` | String | false | Email | SMTP server hostname (e.g., smtp.sendgrid.net) |
| `email.smtp_port` | `"587"` | Integer | false | Email | SMTP server port (587=STARTTLS, 465=SSL) |
| `email.smtp_username` | `""` | String | false | Email | SMTP authentication username |
| `email.smtp_security` | `"StartTls"` | String | false | Email | TLS mode: None, StartTls, SslOnConnect, Auto |
| `email.from_address` | `""` | String | false | Email | Default sender email address |
| `email.from_name` | `"Explore"` | String | false | Email | Default sender display name |
| `email.smtp_timeout_seconds` | `"30"` | Integer | **true** | Email | Connection timeout (locked) |
| `email.smtp_skip_cert_validation` | `"false"` | Boolean | **true** | Email | Skip TLS cert validation (locked for security) |

- **Acceptance Criteria**:
  - [ ] File-scoped namespace: `Explore.Persistence.Seed`
  - [ ] ABOUTME comment
  - [ ] Returns `List<SystemSetting>` with pre-generated GUIDs (deterministic for idempotent seeding)
  - [ ] `IsLocked = true` on security-sensitive settings
  - [ ] Category = "Email" for all settings
  - [ ] DisplayOrder set sequentially

#### Task 3.2: Create EF Core migration for seed data
- **What**: Migration that inserts the email SystemSetting rows
- **Command**: `dotnet ef migrations add SeedEmailSmtpSettings --project Explore.Persistence --startup-project Explore.API`
- **Acceptance Criteria**:
  - [ ] Migration creates the seed data in the `Up` method
  - [ ] Migration removes the seed data in the `Down` method
  - [ ] `dotnet ef database update` succeeds

---

### Phase 4: Update Existing Consumers (Effort: S)

Find and update any code that references the old `IEmailSender` / `Email` / `EmailSettings`.

#### Task 4.1: Search and update all `IEmailSender` references
- **What**: Replace `IEmailSender` with `IEmailService` across the codebase
- **Expected locations**: Handlers, controllers, test mocks
- **Acceptance Criteria**:
  - [ ] No remaining references to `IEmailSender` (except the file itself, to be deleted)
  - [ ] No remaining references to `EmailSettings` model (except the file itself, to be deleted)
  - [ ] All consumers use `EmailMessage` instead of `Email`
  - [ ] `dotnet build` passes

#### Task 4.2: Report obsolete files for deletion
- **What**: Identify files that should be removed (per project rules — report, don't delete)
- **Files to remove**:
  - `Explore.Application/Models/EmailSettings.cs` — replaced by `SmtpConfiguration.cs`
  - `Explore.Infrastructure/Mail/EmailSender.cs` — replaced by `SmtpEmailService.cs`
  - Possibly `Explore.Application/Models/Email.cs` — replaced by `EmailMessage.cs`
  - Possibly `Explore.Application/Contracts/Infrastructure/IEmailSender.cs` — replaced by `IEmailService.cs`
- **Acceptance Criteria**:
  - [ ] All obsolete files identified and listed
  - [ ] User confirms deletion before proceeding

---

### Phase 5: Testing (Effort: M)

**Relevant Skills**: `cqrs-mediatr-guidelines`

#### Task 5.1: Unit tests for `SmtpConfigResolver`
- **File**: `Event.Application.UnitTests/Infrastructure/SmtpConfigResolverTests.cs`
- **What**: Test config resolution logic
- **Test cases**:
  - Returns null when SMTP host is empty
  - Returns valid config when all settings are present
  - Caches config and returns from cache on second call
  - Invalidates cache correctly
  - Falls back to defaults for optional settings (port, timeout)
  - Respects locked settings
- **Acceptance Criteria**:
  - [ ] Uses TUnit test framework (project convention)
  - [ ] Mocks `ISettingsResolver` and `IAppSettingRepository`
  - [ ] No network or database calls

#### Task 5.2: Unit tests for `SmtpEmailService`
- **File**: `Event.Application.UnitTests/Infrastructure/SmtpEmailServiceTests.cs`
- **What**: Test email service logic (mocking the config resolver)
- **Test cases**:
  - Returns failure when SMTP is not configured (null config)
  - Returns failure with descriptive message on missing required fields
  - MimeMessage is correctly built from EmailMessage (From, To, CC, BCC, Subject, Body)
  - Attachments are correctly added to MimeMessage
  - Inline attachments use ContentId
- **Acceptance Criteria**:
  - [ ] Uses TUnit
  - [ ] Mocks `ISmtpConfigResolver`
  - [ ] Does NOT connect to any SMTP server
  - [ ] Tests MimeMessage building logic only

#### Task 5.3: Unit tests for `EmailResiliencePipelines`
- **File**: `Event.Application.UnitTests/Infrastructure/EmailResiliencePipelinesTests.cs`
- **What**: Test retry policy behavior
- **Test cases**:
  - Retries on transient failure (timeout, 421)
  - Does NOT retry on permanent failure (550, auth error)
  - Respects max retry count
  - Returns final result after all retries exhausted
- **Acceptance Criteria**:
  - [ ] Uses TUnit
  - [ ] Tests pipeline behavior, not actual SMTP

#### Task 5.4: Integration test — connection test endpoint (optional)
- **File**: `Event.API.IntegrationTests/Features/EmailConnectionTests.cs`
- **What**: Test that the `/api/admin/email/test-connection` endpoint works
- **Acceptance Criteria**:
  - [ ] Uses WebApplicationFactory
  - [ ] Mocks SMTP (no real connection)
  - [ ] Returns 200 with EmailResult on success

---

### Phase 6: Admin API Endpoint (Effort: S)

Optional but recommended: An admin endpoint to test SMTP connectivity from the UI.

#### Task 6.1: Add email test connection endpoint
- **File**: Add to existing admin controller or create `Explore.API/Controllers/EmailController.cs`
- **What**: `POST /api/admin/email/test-connection` — tests current SMTP configuration
- **Authorization**: `[Authorize(Roles = "Admin")]`
- **Response**: `EmailResult` (success/failure + timing)
- **Acceptance Criteria**:
  - [ ] Admin-only access
  - [ ] Uses `IEmailService.TestConnectionAsync()`
  - [ ] Returns descriptive error on failure
  - [ ] Includes `[EndpointSummary]` and `[ProducesResponseType]` attributes

#### Task 6.2: Add send test email endpoint (optional)
- **File**: Same controller
- **What**: `POST /api/admin/email/send-test` — sends a test email to verify end-to-end
- **Authorization**: `[Authorize(Roles = "Admin")]`
- **Body**: `{ "to": "admin@example.com" }`
- **Response**: `EmailResult`
- **Acceptance Criteria**:
  - [ ] Admin-only access
  - [ ] Sends a simple test email with subject "Test Email from Explore"
  - [ ] Returns result with timing

---

## Risk Assessment

| Risk | Likelihood | Impact | Mitigation |
|------|-----------|--------|------------|
| MailKit version conflict | Low | Medium | Use central package management, pin version |
| SMTP server unreachable in production | Medium | High | Polly retry + clear error messages + TestConnection endpoint |
| Password decryption failure | Low | High | Graceful fallback + clear error logging (no secrets logged) |
| Self-signed cert rejection | Medium | Medium | `SkipCertificateValidation` flag (locked to system admin) |
| Tenant misconfiguring SMTP | Medium | Low | `IsLocked` on security settings, `TestConnection` endpoint |
| Email sending performance | Low | Low | Create-per-send is fine for transactional email volumes |

---

## Success Metrics

- [ ] `dotnet build` passes with zero warnings in new code
- [ ] All existing tests still pass
- [ ] New unit tests pass (min 15 test cases)
- [ ] Can send email to Mailtrap with only database configuration changes
- [ ] `TestConnection` endpoint returns success with valid SMTP config
- [ ] SMTP password is never visible in logs or API responses
- [ ] Tenant can override SMTP provider via TenantSetting
- [ ] Locked settings cannot be overridden by tenants

---

## Effort Estimates

| Phase | Effort | Description |
|-------|--------|-------------|
| Phase 1: Application Contracts | S (~1h) | Pure C# models and interfaces |
| Phase 2: Infrastructure Implementation | M (~3h) | MailKit integration, config resolver, retry |
| Phase 3: Persistence Seed Data | S (~30min) | Seed SystemSettings, migration |
| Phase 4: Update Consumers | S (~30min) | Find/replace references |
| Phase 5: Testing | M (~2h) | Unit tests for all new code |
| Phase 6: Admin Endpoint | S (~30min) | Optional test connection API |
| **Total** | **~7-8 hours** | |

---

## Dependencies

- **MailKit** NuGet package (new dependency)
- **Polly 8.6.5** (already in `Directory.Packages.props`)
- **ISettingsResolver** (already implemented)
- **IAppSettingRepository** (already implemented)
- **ITenantContext** (already implemented)
- **IMemoryCache** (already registered)

---

## Files Created (New)

| File | Layer | Purpose |
|------|-------|---------|
| `Explore.Application/Models/EmailMessage.cs` | Application | Full-featured email DTO |
| `Explore.Application/Models/EmailAttachment.cs` | Application | Attachment DTO |
| `Explore.Application/Models/EmailResult.cs` | Application | Send result type |
| `Explore.Application/Models/SmtpConfiguration.cs` | Application | SMTP config DTO + enum |
| `Explore.Application/Contracts/Infrastructure/IEmailService.cs` | Application | Email service interface |
| `Explore.Application/Contracts/Infrastructure/ISmtpConfigResolver.cs` | Application | Config resolver interface |
| `Explore.Infrastructure/Mail/SmtpEmailService.cs` | Infrastructure | MailKit implementation |
| `Explore.Infrastructure/Mail/SmtpConfigResolver.cs` | Infrastructure | Settings resolver |
| `Explore.Infrastructure/Mail/EmailResiliencePipelines.cs` | Infrastructure | Polly retry policies |
| `Explore.Persistence/Seed/EmailSettingsSeedData.cs` | Persistence | Seed data |
| `Event.Application.UnitTests/Infrastructure/SmtpConfigResolverTests.cs` | Tests | Config resolver tests |
| `Event.Application.UnitTests/Infrastructure/SmtpEmailServiceTests.cs` | Tests | Email service tests |
| `Event.Application.UnitTests/Infrastructure/EmailResiliencePipelinesTests.cs` | Tests | Retry tests |

## Files Modified

| File | Change |
|------|--------|
| `Directory.Packages.props` | Add MailKit version |
| `Explore.Infrastructure/Explore.Infrastructure.csproj` | Add MailKit reference |
| `Explore.Infrastructure/InfrastructureServicesRegistration.cs` | Replace email registration |

## Files to Remove (Report Only)

| File | Replaced By |
|------|-------------|
| `Explore.Application/Models/Email.cs` | `EmailMessage.cs` |
| `Explore.Application/Models/EmailSettings.cs` | `SmtpConfiguration.cs` (config from DB, not appsettings) |
| `Explore.Infrastructure/Mail/EmailSender.cs` | `SmtpEmailService.cs` |
| `Explore.Application/Contracts/Infrastructure/IEmailSender.cs` | `IEmailService.cs` |

---

## Related Skills

- `clean-architecture-rules` — Interface in Application, implementation in Infrastructure
- `cqrs-mediatr-guidelines` — Email sending from command handlers
- `dotnet-efcore-guidelines` — SystemSetting seed data, migrations
- `auth-patterns` — Authorize on admin endpoints

## References

- [MailKit GitHub](https://github.com/jstedfast/MailKit) — Official documentation
- [Microsoft: SmtpClient Deprecation](https://learn.microsoft.com/en-us/dotnet/api/system.net.mail.smtpclient) — Use MailKit
- [Polly v8 Docs](https://www.pollydocs.org/) — Retry strategies
- [RFC 5321 (SMTP)](https://datatracker.ietf.org/doc/html/rfc5321) — SMTP protocol
- [IANA SMTP Status Codes](https://www.iana.org/assignments/smtp-enhanced-status-codes/) — Error classification
