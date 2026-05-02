// ABOUTME: Contract-profile API tests for public event visibility rules.
// ABOUTME: Verifies hidden event states stay out of anonymous list/detail responses.

using System.Net;
using Event.Api.IntegrationTests.Builders;
using Event.Api.IntegrationTests.Fixtures;
using Explore.Domain.Constants;
using Explore.Domain.Enums;
using Explore.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Event.Api.IntegrationTests.Features;

[ClassDataSource<ContractApiFixture>(Shared = SharedType.PerAssembly)]
[NotInParallel("ContractEventVisibility")]
public class EventVisibilityContractTests(ContractApiFixture fixture)
{
    private readonly ContractApiFixture _fixture = fixture;

    [Test]
    public async Task GetAllWithoutStatusFilterHidesDraftAndArchivedEvents()
    {
        var marker = Guid.NewGuid().ToString("N");
        var publishedTitle = $"Published Visibility Event {marker}";
        var draftTitle = $"Draft Visibility Event {marker}";
        var archivedTitle = $"Archived Visibility Event {marker}";

        await using (var scope = _fixture.Factory.Services.CreateAsyncScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ExploreDbContext>();
            var seed = await EnsureDefaultTenantActorAsync(context);

            context.Events.Add(new EventBuilder()
                .WithTitle(publishedTitle)
                .WithActorId(seed.ActorId)
                .WithTenantId(seed.TenantId)
                .WithStatus(EventStatusEnum.Published)
                .WithVisibility(VisibilityTypeEnum.Public)
                .Build());

            context.Events.Add(new EventBuilder()
                .WithTitle(draftTitle)
                .WithActorId(seed.ActorId)
                .WithTenantId(seed.TenantId)
                .WithStatus(EventStatusEnum.Draft)
                .WithVisibility(VisibilityTypeEnum.Public)
                .Build());

            context.Events.Add(new EventBuilder()
                .WithTitle(archivedTitle)
                .WithActorId(seed.ActorId)
                .WithTenantId(seed.TenantId)
                .WithStatus(EventStatusEnum.Archived)
                .WithVisibility(VisibilityTypeEnum.Public)
                .Build());

            await context.SaveChangesAsync();
        }

        var response = await _fixture.Client.GetAsync("/api/event?pageNumber=1&pageSize=50");
        var content = await response.Content.ReadAsStringAsync();

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(content).Contains(publishedTitle);
        await Assert.That(content).DoesNotContain(draftTitle);
        await Assert.That(content).DoesNotContain(archivedTitle);
    }

    [Test]
    public async Task GetAllWithActorIdFiltersByActorAndKeepsPublicDiscoverability()
    {
        var marker = Guid.NewGuid().ToString("N");
        var matchingTitle = $"Actor Matching Published {marker}";
        var otherActorTitle = $"Actor Other Published {marker}";
        var hiddenTitle = $"Actor Matching Draft {marker}";
        var privateTitle = $"Actor Matching Private {marker}";
        var membersOnlyTitle = $"Actor Matching Members Only {marker}";
        var crossTenantTitle = $"Actor Cross Tenant Published {marker}";
        Guid actorId;
        Guid crossTenantActorId;

        await using (var scope = _fixture.Factory.Services.CreateAsyncScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ExploreDbContext>();
            var seed = await EnsureDefaultTenantActorAsync(context);

            var user = new UserBuilder().Build();
            context.Users.Add(user);
            await context.SaveChangesAsync();

            var actor = new ActorBuilder()
                .WithTenantId(seed.TenantId)
                .WithUserId(user.Id)
                .WithDisplayName("Actor Filter Owner")
                .Build();
            context.Actors.Add(actor);
            await context.SaveChangesAsync();
            actorId = actor.Id;

            var crossTenant = new TenantBuilder()
                .WithFullName($"Cross Tenant {marker}")
                .WithSlug($"cross-tenant-{marker}")
                .Build();
            var crossTenantUser = new UserBuilder().Build();
            context.Tenants.Add(crossTenant);
            context.Users.Add(crossTenantUser);
            await context.SaveChangesAsync();

            var crossTenantActor = new ActorBuilder()
                .WithTenantId(crossTenant.Id)
                .WithUserId(crossTenantUser.Id)
                .WithDisplayName("Cross Tenant Actor Filter Owner")
                .Build();
            context.Actors.Add(crossTenantActor);
            await context.SaveChangesAsync();
            crossTenantActorId = crossTenantActor.Id;

            context.Events.Add(new EventBuilder()
                .WithTitle(matchingTitle)
                .WithActorId(actorId)
                .WithTenantId(seed.TenantId)
                .WithStatus(EventStatusEnum.Published)
                .WithVisibility(VisibilityTypeEnum.Public)
                .Build());

            context.Events.Add(new EventBuilder()
                .WithTitle(hiddenTitle)
                .WithActorId(actorId)
                .WithTenantId(seed.TenantId)
                .WithStatus(EventStatusEnum.Draft)
                .WithVisibility(VisibilityTypeEnum.Public)
                .Build());

            context.Events.Add(new EventBuilder()
                .WithTitle(privateTitle)
                .WithActorId(actorId)
                .WithTenantId(seed.TenantId)
                .WithStatus(EventStatusEnum.Published)
                .WithVisibility(VisibilityTypeEnum.Private)
                .Build());

            context.Events.Add(new EventBuilder()
                .WithTitle(membersOnlyTitle)
                .WithActorId(actorId)
                .WithTenantId(seed.TenantId)
                .WithStatus(EventStatusEnum.Published)
                .WithVisibility(VisibilityTypeEnum.MembersOnly)
                .Build());

            context.Events.Add(new EventBuilder()
                .WithTitle(otherActorTitle)
                .WithActorId(seed.ActorId)
                .WithTenantId(seed.TenantId)
                .WithStatus(EventStatusEnum.Published)
                .WithVisibility(VisibilityTypeEnum.Public)
                .Build());

            context.Events.Add(new EventBuilder()
                .WithTitle(crossTenantTitle)
                .WithActorId(crossTenantActorId)
                .WithTenantId(crossTenant.Id)
                .WithStatus(EventStatusEnum.Published)
                .WithVisibility(VisibilityTypeEnum.Public)
                .Build());

            await context.SaveChangesAsync();
        }

        var response = await _fixture.Client.GetAsync($"/api/event?pageNumber=1&pageSize=50&actorId={actorId}");
        var content = await response.Content.ReadAsStringAsync();

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(content).Contains(matchingTitle);
        await Assert.That(content).DoesNotContain(otherActorTitle);
        await Assert.That(content).DoesNotContain(hiddenTitle);
        await Assert.That(content).DoesNotContain(privateTitle);
        await Assert.That(content).DoesNotContain(membersOnlyTitle);
        await Assert.That(content).DoesNotContain(crossTenantTitle);

        var crossTenantResponse = await _fixture.Client.GetAsync($"/api/event?pageNumber=1&pageSize=50&actorId={crossTenantActorId}");
        var crossTenantContent = await crossTenantResponse.Content.ReadAsStringAsync();

        await Assert.That(crossTenantResponse.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(crossTenantContent).DoesNotContain(crossTenantTitle);
    }

    [Test]
    public async Task GetByIdForDraftEventReturnsNotFoundForAnonymousUser()
    {
        var eventId = await SeedHiddenEventAsync(EventStatusEnum.Draft);

        var response = await _fixture.Client.GetAsync($"/api/event/{eventId}");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task GetByIdForArchivedEventReturnsNotFound()
    {
        var eventId = await SeedHiddenEventAsync(EventStatusEnum.Archived);

        var response = await _fixture.Client.GetAsync($"/api/event/{eventId}");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
    }

    private async Task<Guid> SeedHiddenEventAsync(EventStatusEnum status)
    {
        await using var scope = _fixture.Factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<ExploreDbContext>();
        var seed = await EnsureDefaultTenantActorAsync(context);

        var hiddenEvent = new EventBuilder()
            .WithTitle($"{status} Hidden Event {Guid.NewGuid():N}")
            .WithActorId(seed.ActorId)
            .WithTenantId(seed.TenantId)
            .WithStatus(status)
            .WithVisibility(VisibilityTypeEnum.Public)
            .Build();

        context.Events.Add(hiddenEvent);
        await context.SaveChangesAsync();
        return hiddenEvent.Id;
    }

    private static async Task<DefaultTenantSeed> EnsureDefaultTenantActorAsync(ExploreDbContext context)
    {
        var tenantId = PlatformDefaults.DefaultTenantId;
        var tenant = await context.Tenants.FindAsync(tenantId);

        if (tenant is null)
        {
            tenant = new TenantBuilder()
                .WithId(tenantId)
                .WithFullName("Default Test Tenant")
                .WithSlug("default-test")
                .Build();
            context.Tenants.Add(tenant);
            await context.SaveChangesAsync();
        }

        var actor = await context.Actors
            .Where(candidate => candidate.TenantId == tenantId && !candidate.IsDeleted)
            .OrderBy(candidate => candidate.Id)
            .FirstOrDefaultAsync();

        if (actor is not null)
        {
            return new DefaultTenantSeed(tenantId, actor.Id);
        }

        var user = new UserBuilder().Build();
        context.Users.Add(user);
        await context.SaveChangesAsync();

        actor = new ActorBuilder()
            .WithTenantId(tenantId)
            .WithUserId(user.Id)
            .WithDisplayName("Default Visibility Actor")
            .Build();
        context.Actors.Add(actor);
        await context.SaveChangesAsync();

        return new DefaultTenantSeed(tenantId, actor.Id);
    }

    private sealed record DefaultTenantSeed(Guid TenantId, Guid ActorId);
}
