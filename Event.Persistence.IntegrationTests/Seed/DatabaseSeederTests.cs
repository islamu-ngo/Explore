// ABOUTME: Integration tests for runtime database seeding against PostgreSQL.
// ABOUTME: Verifies development catalog seeding remains idempotent and preserves user state across API startups.

using Event.Persistence.IntegrationTests.Fixtures;
using Explore.Domain;
using Explore.Domain.Constants;
using Explore.Domain.Enums;
using Explore.Persistence.Seed;
using Microsoft.EntityFrameworkCore;
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

    private sealed class TestHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Development;
        public string ApplicationName { get; set; } = "Event.Persistence.IntegrationTests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
