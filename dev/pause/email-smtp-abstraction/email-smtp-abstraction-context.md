# Email SMTP Abstraction — Context

> Key files, decisions, and dependencies for resuming work.
>
> Last Updated: 2026-02-10

---

## SESSION PROGRESS (2026-02-10)

### ✅ COMPLETED
- Research: SMTP provider-agnostic patterns, MailKit docs, project architecture
- **Phase 1**: Application contracts & models (6 files)
- **Phase 2**: Infrastructure MailKit implementation (3 files + DI registration)
- **Phase 3**: Seed data — GovernanceSettingKeys, SeedIds, LookupTableSeeder (9 email settings)
- **Phase 4**: Consumer cleanup — confirmed zero old references, updated CODEBASE_STRUCTURE.md
- **Phase 5**: Unit tests — 29 tests across 3 files, all passing (190 total in project)

### 🟡 REMAINING
- Phase 6: Admin API endpoints (test-connection, send-test) — optional
- User needs to delete 4 obsolete files (see below)

### ⚠️ BLOCKERS
- None

---

## Files to Delete (User Action Required)

| Obsolete File | Replaced By |
|---|---|
| `Explore.Application/Models/Email.cs` | `EmailMessage.cs` |
| `Explore.Application/Models/EmailSettings.cs` | DB-stored config via `SmtpConfiguration.cs` |
| `Explore.Infrastructure/Mail/EmailSender.cs` | `SmtpEmailService.cs` |
| `Explore.Application/Contracts/Infrastructure/IEmailSender.cs` | `IEmailService.cs` |

---

## Key Design Decisions

### Decision 1: MailKit over FluentEmail
- **Choice**: MailKit
- **Why**: Microsoft-recommended replacement for deprecated `System.Net.Mail.SmtpClient`, actively maintained, .NET Foundation project, 300M+ downloads.

### Decision 2: Database-stored config via existing Settings Engine
- **Choice**: Leverage `ISettingsResolver` + `SystemSetting`/`TenantSetting` for all SMTP settings
- **Why**: No new tables needed. Cascading engine handles System → Tenant resolution with `IsLocked` support.
- **Note**: SMTP password is stored via ISettingsResolver (same as other settings). Can be migrated to AppSetting (AES-256-GCM) later for enhanced encryption.

### Decision 3: Create-per-send SmtpClient (no pooling)
- **Choice**: New `MailKit.Net.Smtp.SmtpClient` per send, disposed after use
- **Why**: MailKit's SmtpClient is NOT thread-safe. For transactional email volumes, overhead is negligible.

### Decision 4: Polly retry on transient SMTP errors only
- **Choice**: 3 retries with exponential backoff + jitter for SMTP 421/451/452 and connection errors
- **Why**: Standard SMTP error classification. Permanent errors (5xx, auth failure) not retried.

### Decision 5: GovernanceSettingKeys for all email setting keys
- **Choice**: Constants in `GovernanceSettingKeys.cs`, not hardcoded strings
- **Why**: Single source of truth. Used by SmtpConfigResolver, seed data, and admin UI.

---

## Key Files — Created

### Application Layer
| File | Purpose |
|---|---|
| `Explore.Application/Models/EmailMessage.cs` | Rich email DTO (To, CC, BCC, HTML, attachments, custom headers) |
| `Explore.Application/Models/EmailAttachment.cs` | Attachment DTO with inline image support |
| `Explore.Application/Models/EmailResult.cs` | Result type with static `Ok`/`Fail` factories |
| `Explore.Application/Models/SmtpConfiguration.cs` | SMTP config POCO + `SmtpSecurityMode` enum |
| `Explore.Application/Contracts/Infrastructure/IEmailService.cs` | `SendAsync` + `TestConnectionAsync` |
| `Explore.Application/Contracts/Infrastructure/ISmtpConfigResolver.cs` | `ResolveAsync` + `InvalidateCache` |

### Infrastructure Layer
| File | Purpose |
|---|---|
| `Explore.Infrastructure/Mail/SmtpEmailService.cs` | MailKit SMTP sender with Polly retry |
| `Explore.Infrastructure/Mail/SmtpConfigResolver.cs` | Resolves SMTP config from cascading settings per tenant |
| `Explore.Infrastructure/Mail/EmailResiliencePipelines.cs` | Polly v8 retry pipeline (3 retries, exponential backoff) |

### Persistence / Domain
| File | Change |
|---|---|
| `Explore.Domain/Constants/GovernanceSettingKeys.cs` | Added 9 `email.*` setting key constants |
| `Explore.Persistence/Seed/SeedIds.cs` | Added 9 email setting IDs (0520-0528) |
| `Explore.Persistence/Seed/LookupTableSeeder.cs` | Added 9 email SystemSetting seed entries |

### Tests
| File | Tests |
|---|---|
| `Event.Application.UnitTests/Infrastructure/EmailResiliencePipelinesTests.cs` | 13 tests (transient classification) |
| `Event.Application.UnitTests/Infrastructure/SmtpConfigResolverTests.cs` | 11 tests (config resolution, caching) |
| `Event.Application.UnitTests/Infrastructure/EmailResultTests.cs` | 5 tests (static factories) |

### Modified
| File | Change |
|---|---|
| `Explore.Infrastructure/InfrastructureServicesRegistration.cs` | Replaced old email DI with new services |
| `Directory.Packages.props` | Added MailKit 4.12.0 |
| `Explore.Infrastructure/Explore.Infrastructure.csproj` | Added MailKit + Polly references |
| `docs/CODEBASE_STRUCTURE.md` | Updated email file listings |

---

## Multi-Tenant SaaS Email Scenarios

The implementation supports all three SaaS email scenarios via `ISettingsResolver`:

1. **Instance admin locks SMTP** (`IsLocked=true`) → All tenants use SaaS provider's SMTP
2. **Instance admin sets default, unlocked** → Tenants inherit default but can override with own SMTP
3. **Tenant provides own SMTP** → Tenant admin configures `email.smtp_*` settings in their settings

**No code changes needed** — the `IsLocked` mechanism in `ISettingsResolver` handles all three cases transparently.
## Context Reset Session Update (2026-02-15 21:25 Europe/Brussels)

- Current implementation state: No new implementation changes in this session for this track.
- Key decisions made this session: Priority shifted to analytics implementation completion and verification.
- Files modified and why: None in this track during this session.
- Blockers/issues discovered: None newly discovered for this track.
- Next immediate steps: Continue from highest-priority unchecked items in `email-smtp-abstraction-tasks.md`.
