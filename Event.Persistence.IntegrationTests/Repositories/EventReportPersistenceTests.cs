// ABOUTME: PostgreSQL persistence tests for event-reporting moderation intake records.
// ABOUTME: Verifies tenant filters, soft-delete graph hiding, and composite tenant FK enforcement.

using Event.Persistence.IntegrationTests.Fixtures;
using Explore.Application.Contracts.Infrastructure;
using Explore.Domain;
using Explore.Domain.Enums;
using Explore.Persistence;
using Microsoft.EntityFrameworkCore;
using TUnit.Core;

namespace Event.Persistence.IntegrationTests.Repositories;

[ClassDataSource<PostgreSqlContainerFixture>(Shared = SharedType.PerAssembly)]
[NotInParallel("PersistenceDb")]
public sealed class EventReportPersistenceTests(PostgreSqlContainerFixture fixture)
{
    [Test]
    public async Task EventReportGraph_WhenSaved_PersistsTenantScopedRows()
    {
        await fixture.ResetAsync();
        await using var context = fixture.CreateDbContext();
        var (tenant, @event, user, actor) = await SetupEventAsync(context, "report-graph");
        var report = CreateReport(tenant.Id, @event.Id, user.Id, actor.Id);
        var reportCase = EventReportCase.Create(tenant.Id, report.Id, "default", EventReportPriority.Normal, DateTime.UtcNow.AddHours(24));
        var decision = EventReportDecision.Create(
            tenant.Id,
            reportCase.Id,
            report.Id,
            EventReportDecisionSource.LocalModerator,
            EventReportDecisionKind.NoViolation,
            "not_violation",
            "Reviewed safely.",
            user.Id,
            externalDecisionId: null);

        context.EventReports.Add(report);
        context.EventReportTargets.Add(EventReportTarget.CreateEventTarget(tenant.Id, report.Id, @event.Id));
        context.EventReportEvidenceItems.Add(EventReportEvidence.CreateReporterText(
            tenant.Id,
            report.Id,
            "encrypted:reporter-text",
            EventReportEvidenceClassification.Sensitive,
            DateTime.UtcNow.AddDays(30),
            user.Id));
        context.EventReportCases.Add(reportCase);
        context.EventReportSignals.Add(EventReportSignal.Create(
            tenant.Id,
            report.Id,
            @event.Id,
            EventReportSignalProvider.Local,
            "keyword_match",
            "community_spam",
            0.82m,
            EventReportSignalVerdict.NeedsReview,
            EventReportRecommendedAction.LightModerate,
            "Potential promotional abuse.",
            "signal-" + Guid.NewGuid().ToString("N"),
            "correlation-" + Guid.NewGuid().ToString("N")[..12]));
        context.EventReportDecisions.Add(decision);
        context.EventReportExternalLinks.Add(EventReportExternalLink.CreatePending(
            tenant.Id,
            report.Id,
            reportCase.Id,
            EventReportExternalProvider.Coop,
            "external-" + Guid.NewGuid().ToString("N")[..12]));
        await context.SaveChangesAsync();

        await using var verifyContext = fixture.CreateTenantFilteredDbContext(new StaticTenantContext(tenant.Id));
        var persisted = await verifyContext.EventReports
            .Include(e => e.Targets)
            .Include(e => e.EvidenceItems)
            .Include(e => e.Cases)
            .Include(e => e.Signals)
            .Include(e => e.Decisions)
            .Include(e => e.ExternalLinks)
            .SingleAsync(e => e.Id == report.Id);

        await Assert.That(persisted.Targets.Count).IsEqualTo(1);
        await Assert.That(persisted.EvidenceItems.Count).IsEqualTo(1);
        await Assert.That(persisted.Cases.Count).IsEqualTo(1);
        await Assert.That(persisted.Signals.Count).IsEqualTo(1);
        await Assert.That(persisted.Decisions.Count).IsEqualTo(1);
        await Assert.That(persisted.ExternalLinks.Count).IsEqualTo(1);
    }

    [Test]
    public async Task TenantFilter_HidesReportsAndEvidenceFromOtherTenants()
    {
        await fixture.ResetAsync();
        await using var setupContext = fixture.CreateDbContext();
        var (tenant, @event, user, actor) = await SetupEventAsync(setupContext, "report-filter");
        var report = CreateReport(tenant.Id, @event.Id, user.Id, actor.Id);
        setupContext.EventReports.Add(report);
        setupContext.EventReportEvidenceItems.Add(EventReportEvidence.CreateReporterText(
            tenant.Id,
            report.Id,
            "encrypted:tenant-filter",
            EventReportEvidenceClassification.Normal,
            retentionUntil: null,
            user.Id));
        await setupContext.SaveChangesAsync();

        await using var wrongTenantContext = fixture.CreateTenantFilteredDbContext(new StaticTenantContext(Guid.CreateVersion7()));

        await Assert.That(await wrongTenantContext.EventReports.CountAsync()).IsEqualTo(0);
        await Assert.That(await wrongTenantContext.EventReportEvidenceItems.CountAsync()).IsEqualTo(0);
    }

    [Test]
    public async Task SoftDeleteReport_HidesReportEvidenceGraphFromFilteredQueries()
    {
        await fixture.ResetAsync();
        await using var context = fixture.CreateDbContext();
        var (tenant, @event, user, actor) = await SetupEventAsync(context, "report-soft-delete");
        var report = CreateReport(tenant.Id, @event.Id, user.Id, actor.Id);
        context.EventReports.Add(report);
        context.EventReportEvidenceItems.Add(EventReportEvidence.CreateReporterText(
            tenant.Id,
            report.Id,
            "encrypted:soft-delete",
            EventReportEvidenceClassification.Sensitive,
            retentionUntil: null,
            user.Id));
        await context.SaveChangesAsync();

        context.EventReports.Remove(report);
        await context.SaveChangesAsync();

        await using var filteredContext = fixture.CreateTenantFilteredDbContext(new StaticTenantContext(tenant.Id));
        await Assert.That(await filteredContext.EventReports.CountAsync()).IsEqualTo(0);
        await Assert.That(await filteredContext.EventReportEvidenceItems.CountAsync()).IsEqualTo(0);

        await using var rawContext = fixture.CreateDbContext();
        var rawReport = await rawContext.EventReports.IgnoreQueryFilters().SingleAsync(e => e.Id == report.Id);
        var rawEvidenceCount = await rawContext.EventReportEvidenceItems.IgnoreQueryFilters().CountAsync(e => e.ReportId == report.Id);

        await Assert.That(rawReport.IsDeleted).IsTrue();
        await Assert.That(rawEvidenceCount).IsEqualTo(1);
    }

    [Test]
    public async Task SaveChanges_ShouldRejectCaseReferencingReportFromAnotherTenant()
    {
        await fixture.ResetAsync();
        await using var context = fixture.CreateDbContext();
        var (tenantA, eventA, userA, actorA) = await SetupEventAsync(context, "report-tenant-a");
        var (tenantB, _, _, _) = await SetupEventAsync(context, "report-tenant-b");
        var report = CreateReport(tenantA.Id, eventA.Id, userA.Id, actorA.Id);
        context.EventReports.Add(report);
        await context.SaveChangesAsync();

        context.EventReportCases.Add(EventReportCase.Create(
            tenantB.Id,
            report.Id,
            "default",
            EventReportPriority.Normal,
            slaDueAt: null));

        await Assert.ThrowsAsync<DbUpdateException>(async () => await context.SaveChangesAsync());
    }

    private static EventReport CreateReport(Guid tenantId, Guid eventId, Guid userId, Guid actorId)
    {
        return EventReport.Create(
            tenantId,
            eventId,
            userId,
            actorId,
            EventReporterKind.AuthenticatedUser,
            EventReportSourceKind.UserReport,
            "spam",
            "misleading",
            EventReportPriority.Normal,
            EventReportSeverityHint.Medium,
            reporterContactConsent: true,
            "en",
            "iphash-" + Guid.NewGuid().ToString("N")[..16],
            "uahash-" + Guid.NewGuid().ToString("N")[..16]);
    }

    private static async Task<(Tenant Tenant, Explore.Domain.Event Event, User User, Actor Actor)> SetupEventAsync(
        ExploreDbContext context,
        string slugPrefix)
    {
        var tenant = new Tenant
        {
            FullName = "Reporting Test Tenant " + slugPrefix,
            Slug = slugPrefix + "-" + Guid.NewGuid().ToString("N")[..8],
            TenantStatusId = (int)TenantStatusEnum.Active,
            TenantStatus = null!
        };
        context.Tenants.Add(tenant);

        var user = new User
        {
            Pii = new UserPii
            {
                Email = $"{slugPrefix}-{Guid.NewGuid():N}@example.com",
                FirstName = "Report",
                LastName = "User"
            }
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var actor = new Actor
        {
            Pii = new ActorPii { DisplayName = "Reporting Actor " + slugPrefix },
            ActorTypeId = (int)ActorTypeEnum.User,
            ActorType = null!,
            TenantId = tenant.Id,
            Tenant = null!,
            UserId = user.Id
        };
        context.Actors.Add(actor);
        await context.SaveChangesAsync();

        var @event = new Explore.Domain.Event
        {
            Id = Guid.CreateVersion7(),
            Title = "Reporting Event " + slugPrefix,
            ActorId = actor.Id,
            Actor = null!,
            TenantId = tenant.Id,
            Tenant = null!,
            VisibilityTypeId = (int)VisibilityTypeEnum.Public,
            VisibilityType = null!,
            EventStatusId = (int)EventStatusEnum.Published,
            EventStatus = null!,
            EventFormatId = (int)EventFormatEnum.Digital,
            EventFormat = null!
        };
        context.Events.Add(@event);
        await context.SaveChangesAsync();

        return (tenant, @event, user, actor);
    }

    private sealed record StaticTenantContext(Guid TenantId) : ITenantContext;
}
