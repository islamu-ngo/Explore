// ABOUTME: Integration tests for runtime database seeding against PostgreSQL.
// ABOUTME: Verifies development catalog seeding remains idempotent and preserves user state across API startups.

using Event.Persistence.IntegrationTests.Fixtures;
using Explore.Domain;
using Explore.Domain.Constants;
using Explore.Domain.Enums;
using Explore.Domain.Secrets;
using Explore.Persistence.Seed;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using TUnit.Assertions;
using TUnit.Core;

namespace Event.Persistence.IntegrationTests.Seed;

[ClassDataSource<PostgreSqlContainerFixture>(Shared = SharedType.PerAssembly)]
[NotInParallel("PersistenceDb")]
public class DatabaseSeederTests(PostgreSqlContainerFixture fixture)
{
    private static readonly IHostEnvironment DevelopmentEnvironment = new TestHostEnvironment();

    [Test]
    public async Task SeedAsync_InDevelopment_IsIdempotentAcrossStartups()
    {
        await fixture.ResetAsync();

        await using (var context = fixture.CreateDbContext())
        {
            await DatabaseSeeder.SeedAsync(context, DevelopmentEnvironment);
        }

        await using (var context = fixture.CreateDbContext())
        {
            await DatabaseSeeder.SeedAsync(context, DevelopmentEnvironment);
        }

        await using var verifyContext = fixture.CreateDbContext();
        var visibleCatalogCount = await verifyContext.Events
            .Where(e => SeedIds.IslamicEventCatalogIds.Contains(e.Id))
            .CountAsync();
        var unfilteredCatalogCount = await verifyContext.Events
            .IgnoreQueryFilters()
            .Where(e => SeedIds.IslamicEventCatalogIds.Contains(e.Id))
            .CountAsync();
        var softDeletedCatalogCount = await verifyContext.Events
            .IgnoreQueryFilters()
            .Where(e => SeedIds.IslamicEventCatalogIds.Contains(e.Id) && e.IsDeleted)
            .CountAsync();

        await Assert.That(visibleCatalogCount).IsEqualTo(SeedIds.IslamicEventCatalogIds.Length);
        await Assert.That(unfilteredCatalogCount).IsEqualTo(SeedIds.IslamicEventCatalogIds.Length);
        await Assert.That(softDeletedCatalogCount).IsEqualTo(0);
    }

    [Test]
    public async Task SeedAsync_InDevelopment_RepairsLaunchCatalogDiscoveryFieldsAcrossStartups()
    {
        await fixture.ResetAsync();

        await using (var context = fixture.CreateDbContext())
        {
            await DatabaseSeeder.SeedAsync(context, DevelopmentEnvironment);
        }

        await using (var staleContext = fixture.CreateDbContext())
        {
            var catalogEvents = await staleContext.Events
                .IgnoreQueryFilters()
                .Where(e => SeedIds.IslamicEventCatalogIds.Contains(e.Id))
                .ToListAsync();
            foreach (var catalogEvent in catalogEvents)
            {
                catalogEvent.LastSessionEndUtc = null;
            }

            var catalogSessions = await staleContext.EventSessions
                .IgnoreQueryFilters()
                .Where(session => SeedIds.IslamicEventCatalogIds.Contains(session.EventId))
                .ToListAsync();
            foreach (var catalogSession in catalogSessions)
            {
                catalogSession.EventSessionStatusId = (int)EventSessionStatusEnum.Draft;
            }

            await staleContext.SaveChangesAsync();
        }

        await using (var context = fixture.CreateDbContext())
        {
            await DatabaseSeeder.SeedAsync(context, DevelopmentEnvironment);
        }

        await using var verifyContext = fixture.CreateDbContext();
        var repairedEvents = await verifyContext.Events
            .IgnoreQueryFilters()
            .Where(e => SeedIds.IslamicEventCatalogIds.Contains(e.Id))
            .Select(e => new { e.Id, e.FirstSessionStartUtc, e.LastSessionEndUtc })
            .ToListAsync();
        var repairedSessions = await verifyContext.EventSessions
            .IgnoreQueryFilters()
            .Where(session => SeedIds.IslamicEventCatalogIds.Contains(session.EventId))
            .Select(session => new { session.Id, session.EventSessionStatusId })
            .ToListAsync();

        await Assert.That(repairedEvents.Count).IsEqualTo(SeedIds.IslamicEventCatalogIds.Length);
        await Assert.That(repairedSessions.Count).IsEqualTo(SeedData.IslamicEventSessions.Count);
        await Assert.That(repairedEvents.All(e =>
            e.FirstSessionStartUtc.HasValue
            && e.LastSessionEndUtc.HasValue
            && e.LastSessionEndUtc.Value > e.FirstSessionStartUtc.Value)).IsTrue();
        await Assert.That(repairedSessions.All(session =>
            session.EventSessionStatusId == (int)EventSessionStatusEnum.Published)).IsTrue();
    }

    [Test]
    public async Task SeedAsync_InDevelopment_PreservesRegistrationAndConsentAcrossStartups()
    {
        await fixture.ResetAsync();

        await using (var context = fixture.CreateDbContext())
        {
            await DatabaseSeeder.SeedAsync(context, DevelopmentEnvironment);
        }

        var intentId = Guid.Parse("018e4e5c-7f00-7001-8000-000000010001");
        var registrationId = Guid.Parse("018e4e5c-7f00-7001-8000-000000010002");
        var consentId = Guid.Parse("018e4e5c-7f00-7001-8000-000000010003");

        await using (var context = fixture.CreateDbContext())
        {
            var eventId = SeedIds.QuranTafsirWomenEventId;
            var sessionId = await context.EventSessions
                .IgnoreQueryFilters()
                .Where(session => session.EventId == eventId)
                .Select(session => session.Id)
                .FirstAsync();
            var recipientActorId = await context.Events
                .IgnoreQueryFilters()
                .Where(e => e.Id == eventId)
                .Select(e => e.ActorId)
                .SingleAsync();

            context.Set<EventRegistrationIntent>().Add(new EventRegistrationIntent
            {
                Id = intentId,
                EventId = eventId,
                Event = null!,
                UserId = SeedIds.RegularUserId,
                User = null!,
                RegistrationScopeId = (int)RegistrationScopeEnum.SessionSelection,
                RegistrationScope = null!,
                ApprovalStatusId = (int)ApprovalStatusEnum.Approved,
                TenantId = SeedIds.DefaultTenantId,
                Tenant = null!,
                CreatedAt = DateTime.UtcNow,
                ConcurrencyStamp = Guid.Parse("018e4e5c-7f00-7001-8000-000000010004")
            });
            context.Set<EventRegistration>().Add(new EventRegistration
            {
                Id = registrationId,
                EventId = eventId,
                Event = null!,
                UserId = SeedIds.RegularUserId,
                User = null!,
                EventSessionId = sessionId,
                EventSession = null!,
                EventRegistrationIntentId = intentId,
                ApprovalStatusId = (int)ApprovalStatusEnum.Approved,
                TenantId = SeedIds.DefaultTenantId,
                Tenant = null!,
                CreatedAt = DateTime.UtcNow
            });
            context.Set<EventContactShareConsent>().Add(new EventContactShareConsent
            {
                Id = consentId,
                TenantId = SeedIds.DefaultTenantId,
                SourceEventId = eventId,
                UserId = SeedIds.RegularUserId,
                RecipientActorId = recipientActorId,
                SourceEventRegistrationIntentId = intentId,
                PurposeCode = ConsentPurposeCodes.OrganizerFutureCommunications,
                Status = ConsentStatus.Granted,
                EmailSnapshot = "user@example.test",
                EmailNormalizedSnapshot = "user@example.test",
                ConsentTextSnapshot = "Share my email with the organizer.",
                ConsentUiVersion = "v1",
                GrantedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            });
            await context.SaveChangesAsync();
        }

        await using (var context = fixture.CreateDbContext())
        {
            await DatabaseSeeder.SeedAsync(context, DevelopmentEnvironment);
        }

        await using var verifyContext = fixture.CreateDbContext();
        var intentExists = await verifyContext.Set<EventRegistrationIntent>()
            .IgnoreQueryFilters()
            .AnyAsync(intent => intent.Id == intentId && !intent.IsDeleted);
        var registrationExists = await verifyContext.Set<EventRegistration>()
            .IgnoreQueryFilters()
            .AnyAsync(registration => registration.Id == registrationId && !registration.IsDeleted);
        var consentExists = await verifyContext.Set<EventContactShareConsent>()
            .AnyAsync(consent => consent.Id == consentId && consent.Status == ConsentStatus.Granted);

        await Assert.That(intentExists).IsTrue();
        await Assert.That(registrationExists).IsTrue();
        await Assert.That(consentExists).IsTrue();
    }

    [Test]
    public async Task SeedAsync_InDevelopment_SeedsSmtpSettingsFromConfiguration()
    {
        await fixture.ResetAsync();

        var environmentKeys = new[]
        {
            "MAIL_SMTP_HOST",
            "MAIL_SMTP_PORT",
            "MAIL_SMTP_ENCRYPTION",
            "MAIL_SMTP_FROM_ADDRESS",
            "MAIL_SMTP_FROM_NAME",
            "SMTP_HOST",
            "SMTP_PORT",
            "SMTP_SECURITY",
            "SMTP_FROM_ADDRESS",
            "SMTP_FROM_NAME"
        };
        var originalEnvironment = environmentKeys.ToDictionary(key => key, Environment.GetEnvironmentVariable);

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["MAIL_SMTP_HOST"] = "mailpit",
                ["MAIL_SMTP_PORT"] = "1025",
                ["MAIL_SMTP_ENCRYPTION"] = "None",
                ["MAIL_SMTP_FROM_ADDRESS"] = "noreply@localhost",
                ["MAIL_SMTP_FROM_NAME"] = "ISLAMU Event Dev"
            })
            .Build();

        try
        {
            foreach (var key in environmentKeys)
            {
                Environment.SetEnvironmentVariable(key, null);
            }

            await using (var resetContext = fixture.CreateDbContext())
            {
                var smtpSettingKeys = new[]
                {
                    GovernanceSettingKeys.Email.SmtpHost,
                    GovernanceSettingKeys.Email.SmtpPort,
                    GovernanceSettingKeys.Email.SmtpSecurity,
                    GovernanceSettingKeys.Email.FromAddress,
                    GovernanceSettingKeys.Email.FromName
                };
                var smtpSettings = await resetContext.Set<SystemSetting>()
                    .Where(setting => smtpSettingKeys.Contains(setting.SettingKey))
                    .ToListAsync();

                foreach (var setting in smtpSettings)
                {
                    setting.Value = setting.SettingKey == GovernanceSettingKeys.Email.SmtpPort
                        ? "587"
                        : "\"\"";
                }

                await resetContext.SaveChangesAsync();
            }

            await using (var context = fixture.CreateDbContext())
            {
                await DatabaseSeeder.SeedAsync(context, DevelopmentEnvironment, configuration: configuration);
            }

            await using var verifyContext = fixture.CreateDbContext();
            var settings = await verifyContext.Set<SystemSetting>()
                .Where(setting => new[]
                {
                    GovernanceSettingKeys.Email.SmtpHost,
                    GovernanceSettingKeys.Email.SmtpPort,
                    GovernanceSettingKeys.Email.SmtpSecurity,
                    GovernanceSettingKeys.Email.FromAddress,
                    GovernanceSettingKeys.Email.FromName
                }.Contains(setting.SettingKey))
                .ToDictionaryAsync(setting => setting.SettingKey, setting => setting.Value);

            await Assert.That(settings[GovernanceSettingKeys.Email.SmtpHost]).IsEqualTo("\"mailpit\"");
            await Assert.That(settings[GovernanceSettingKeys.Email.SmtpPort]).IsEqualTo("1025");
            await Assert.That(settings[GovernanceSettingKeys.Email.SmtpSecurity]).IsEqualTo("\"None\"");
            await Assert.That(settings[GovernanceSettingKeys.Email.FromAddress]).IsEqualTo("\"noreply@localhost\"");
            await Assert.That(settings[GovernanceSettingKeys.Email.FromName]).IsEqualTo("\"ISLAMU Event Dev\"");
        }
        finally
        {
            foreach (var (key, value) in originalEnvironment)
            {
                Environment.SetEnvironmentVariable(key, value);
            }
        }
    }

    [Test]
    public async Task SeedAsync_InDevelopment_SeedsSvixSecretBindingsFromEnvironment()
    {
        await fixture.ResetAsync();

        var environmentKeys = new[]
        {
            "WEBHOOKS_SVIX_AUTH_TOKEN",
            "WEBHOOKS_SVIX_OPERATIONAL_WEBHOOK_SECRET"
        };
        var originalEnvironment = environmentKeys.ToDictionary(key => key, Environment.GetEnvironmentVariable);

        try
        {
            Environment.SetEnvironmentVariable("WEBHOOKS_SVIX_AUTH_TOKEN", "local-test-svix-token");
            Environment.SetEnvironmentVariable(
                "WEBHOOKS_SVIX_OPERATIONAL_WEBHOOK_SECRET",
                "whsec_local_test_operational_secret");

            await using (var context = fixture.CreateDbContext())
            {
                await DatabaseSeeder.SeedAsync(context, DevelopmentEnvironment);
            }

            await using var verifyContext = fixture.CreateDbContext();
            var bindings = await verifyContext.Set<SecretBinding>()
                .Where(binding => new[]
                {
                    SecretDefinitionRegistry.Keys.Webhooks.SvixAuthToken,
                    SecretDefinitionRegistry.Keys.Webhooks.SvixOperationalWebhookSecret
                }.Contains(binding.SettingKey))
                .ToDictionaryAsync(binding => binding.SettingKey);

            await Assert.That(bindings.Count).IsEqualTo(2);
            await Assert.That(bindings[SecretDefinitionRegistry.Keys.Webhooks.SvixAuthToken].SourceType)
                .IsEqualTo(SecretSourceType.EnvironmentVariable);
            await Assert.That(bindings[SecretDefinitionRegistry.Keys.Webhooks.SvixAuthToken].EnvironmentVariableName)
                .IsEqualTo("WEBHOOKS_SVIX_AUTH_TOKEN");
            await Assert.That(bindings[SecretDefinitionRegistry.Keys.Webhooks.SvixOperationalWebhookSecret].SourceType)
                .IsEqualTo(SecretSourceType.EnvironmentVariable);
            await Assert.That(bindings[SecretDefinitionRegistry.Keys.Webhooks.SvixOperationalWebhookSecret].EnvironmentVariableName)
                .IsEqualTo("WEBHOOKS_SVIX_OPERATIONAL_WEBHOOK_SECRET");
        }
        finally
        {
            foreach (var (key, value) in originalEnvironment)
            {
                Environment.SetEnvironmentVariable(key, value);
            }
        }
    }

    private sealed class TestHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Development;
        public string ApplicationName { get; set; } = "Event.Persistence.IntegrationTests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
