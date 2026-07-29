// ABOUTME: Integration coverage for actor subscription endpoints and HAL affordances.
// ABOUTME: Seeds tenant-local membership and verifies subscription links flow through the API surface.

using System.Linq;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Event.Api.IntegrationTests.Builders;
using Event.Api.IntegrationTests.Fixtures;
using Explore.Application.DTOs.ActorSubscription;
using Explore.Application.Responses;
using Explore.Domain;
using Explore.Domain.Constants;
using Explore.Domain.Enums;
using Explore.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Event.Api.IntegrationTests.Features.Hateoas;

[Category(TestCategories.Fast)]
[Category("ActorSubscription")]
[NotInParallel("AuthenticatedApiFixture")]
[ClassDataSource<AuthenticatedApiTestFixture>(Shared = SharedType.PerAssembly)]
public class ActorSubscriptionHateoasTests(AuthenticatedApiTestFixture fixture)
{
    private readonly AuthenticatedApiTestFixture _fixture = fixture;

    [Test]
    public async Task SubscribeToActor_WithOrganizationTarget_PersistsSubscriptionAndReturnsHalAffordances()
    {
        var scenario = await SeedSubscriptionScenarioAsync();

        var subscribeResponse = await PostSubscribeAsync(scenario);

        await Assert.That(subscribeResponse.StatusCode).IsEqualTo(HttpStatusCode.OK);
        var commandResponse = await subscribeResponse.Content.ReadFromJsonAsync<BaseCommandResponse<Guid>>();
        await Assert.That(commandResponse).IsNotNull();
        await Assert.That(commandResponse!.Success).IsTrue();
        await Assert.That(commandResponse.Id).IsNotEqualTo(Guid.Empty);

        await using (var verifyScope = _fixture.Factory.Services.CreateAsyncScope())
        {
            var context = verifyScope.ServiceProvider.GetRequiredService<ExploreDbContext>();
            var subscription = await context.ActorSubscriptions
                .IgnoreQueryFilters()
                .SingleAsync(row => row.Id == commandResponse.Id);

            await Assert.That(subscription.TenantId).IsEqualTo(scenario.TenantId);
            await Assert.That(subscription.SubscriberTenantUserId).IsEqualTo(scenario.TenantUserId);
            await Assert.That(subscription.SubscriberUserId).IsEqualTo(scenario.UserId);
            await Assert.That(subscription.TargetActorId).IsEqualTo(scenario.TargetActorId);
            await Assert.That(subscription.StatusId).IsEqualTo((int)ActorSubscriptionStatusEnum.Active);
            await Assert.That(subscription.NotificationLevelId).IsEqualTo((int)ActorSubscriptionNotificationLevelEnum.All);
        }

        using var listRequest = _fixture.CreateAuthenticatedRequest(
            HttpMethod.Get,
            "/api/actor-subscriptions?pageNumber=1&pageSize=10",
            scenario.UserId);
        var listResponse = await _fixture.Client.SendAsync(listRequest);

        await Assert.That(listResponse.StatusCode).IsEqualTo(HttpStatusCode.OK);
        using var json = JsonDocument.Parse(await listResponse.Content.ReadAsStringAsync());
        var root = json.RootElement;

        await Assert.That(root.GetProperty("totalCount").GetInt32()).IsGreaterThanOrEqualTo(1);
        await Assert.That(root.GetProperty("_links").TryGetProperty("create", out _)).IsTrue();

        var items = root.GetProperty("_embedded").GetProperty("items");
        var item = items.EnumerateArray().Single(element =>
            element.GetProperty("targetActorId").GetGuid() == scenario.TargetActorId);
        var links = item.GetProperty("_links");

        await Assert.That(links.TryGetProperty("self", out _)).IsTrue();
        await Assert.That(links.TryGetProperty("actor", out _)).IsTrue();
        await Assert.That(links.TryGetProperty("update-notification-level", out _)).IsTrue();
        await Assert.That(links.TryGetProperty("unsubscribe", out _)).IsTrue();
    }

    [Test]
    public async Task SubscribeToActor_WithUserTarget_ReturnsBadRequestAndDoesNotPersistSubscription()
    {
        var scenario = await SeedSubscriptionScenarioAsync(targetActorType: ActorTypeEnum.User);

        var subscribeResponse = await PostSubscribeAsync(scenario);

        await Assert.That(subscribeResponse.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
        using var document = JsonDocument.Parse(await subscribeResponse.Content.ReadAsStringAsync());
        var root = document.RootElement;
        await Assert.That(root.GetProperty("code").GetString()).IsEqualTo("validation_failed");
        var errors = root.GetProperty("errors").GetProperty("actorSubscription").EnumerateArray().Select(e => e.GetString()).ToArray();
        await Assert.That(errors).Contains("Target actor must be an organization or group in the current tenant.");

        await using var verifyScope = _fixture.Factory.Services.CreateAsyncScope();
        var context = verifyScope.ServiceProvider.GetRequiredService<ExploreDbContext>();
        var persisted = await context.ActorSubscriptions
            .IgnoreQueryFilters()
            .AnyAsync(row => row.SubscriberTenantUserId == scenario.TenantUserId && row.TargetActorId == scenario.TargetActorId);
        await Assert.That(persisted).IsFalse();
    }

    [Test]
    public async Task SubscribeToActor_WithInactiveTenantUser_ReturnsBadRequestAndDoesNotPersistSubscription()
    {
        var scenario = await SeedSubscriptionScenarioAsync(tenantUserStatus: TenantUserStatusEnum.Suspended);

        var subscribeResponse = await PostSubscribeAsync(scenario);

        await Assert.That(subscribeResponse.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
        using var document2 = JsonDocument.Parse(await subscribeResponse.Content.ReadAsStringAsync());
        var root2 = document2.RootElement;
        await Assert.That(root2.GetProperty("code").GetString()).IsEqualTo("validation_failed");
        var errors2 = root2.GetProperty("errors").GetProperty("actorSubscription").EnumerateArray().Select(e => e.GetString()).ToArray();
        await Assert.That(errors2).Contains("An active tenant-local user is required before subscribing.");

        await using var verifyScope = _fixture.Factory.Services.CreateAsyncScope();
        var context = verifyScope.ServiceProvider.GetRequiredService<ExploreDbContext>();
        var persisted = await context.ActorSubscriptions
            .IgnoreQueryFilters()
            .AnyAsync(row => row.SubscriberTenantUserId == scenario.TenantUserId && row.TargetActorId == scenario.TargetActorId);
        await Assert.That(persisted).IsFalse();
    }

    [Test]
    public async Task NotificationLevelPatch_WithExpectedConcurrencyStamp_UpdatesSubscriptionLevel()
    {
        var scenario = await SeedSubscriptionScenarioAsync();
        var subscribeResponse = await PostSubscribeAsync(scenario);
        var subscribeCommand = await subscribeResponse.Content.ReadFromJsonAsync<BaseCommandResponse<Guid>>();
        await Assert.That(subscribeCommand).IsNotNull();

        var concurrencyStamp = await GetSubscriptionConcurrencyStampAsync(subscribeCommand!.Id);
        using var patchRequest = _fixture.CreateAuthenticatedRequest(
            HttpMethod.Patch,
            $"/api/actor-subscriptions/actors/{scenario.TargetActorId}/notification-level",
            scenario.UserId);
        patchRequest.Content = JsonContent.Create(new UpdateActorSubscriptionNotificationLevelDto
        {
            NotificationLevel = new UpdateActorSubscriptionNotificationLevelValueDto
            {
                Id = (int)ActorSubscriptionNotificationLevelEnum.None
            },
            ExpectedConcurrencyStamp = concurrencyStamp
        });

        var patchResponse = await _fixture.Client.SendAsync(patchRequest);

        await Assert.That(patchResponse.StatusCode).IsEqualTo(HttpStatusCode.OK);
        var patchCommand = await patchResponse.Content.ReadFromJsonAsync<BaseCommandResponse<Guid>>();
        await Assert.That(patchCommand).IsNotNull();
        await Assert.That(patchCommand!.Success).IsTrue();
        await Assert.That(patchCommand.Id).IsEqualTo(subscribeCommand.Id);

        await using var verifyScope = _fixture.Factory.Services.CreateAsyncScope();
        var context = verifyScope.ServiceProvider.GetRequiredService<ExploreDbContext>();
        var subscription = await context.ActorSubscriptions
            .IgnoreQueryFilters()
            .SingleAsync(row => row.Id == subscribeCommand.Id);
        await Assert.That(subscription.NotificationLevelId).IsEqualTo((int)ActorSubscriptionNotificationLevelEnum.None);
    }

    [Test]
    public async Task UnsubscribeThenResubscribe_WithExpectedConcurrencyStamp_ReactivatesDurableRow()
    {
        var scenario = await SeedSubscriptionScenarioAsync();
        var subscribeResponse = await PostSubscribeAsync(scenario);
        var subscribeCommand = await subscribeResponse.Content.ReadFromJsonAsync<BaseCommandResponse<Guid>>();
        await Assert.That(subscribeCommand).IsNotNull();

        var concurrencyStamp = await GetSubscriptionConcurrencyStampAsync(subscribeCommand!.Id);
        using var unsubscribeRequest = _fixture.CreateAuthenticatedRequest(
            HttpMethod.Delete,
            $"/api/actor-subscriptions/actors/{scenario.TargetActorId}",
            scenario.UserId);
        unsubscribeRequest.Content = JsonContent.Create(new UnsubscribeFromActorDto
        {
            TargetActorId = Guid.NewGuid(),
            ExpectedConcurrencyStamp = concurrencyStamp
        });

        var unsubscribeResponse = await _fixture.Client.SendAsync(unsubscribeRequest);

        await Assert.That(unsubscribeResponse.StatusCode).IsEqualTo(HttpStatusCode.OK);
        var unsubscribeCommand = await unsubscribeResponse.Content.ReadFromJsonAsync<BaseCommandResponse<Guid>>();
        await Assert.That(unsubscribeCommand).IsNotNull();
        await Assert.That(unsubscribeCommand!.Success).IsTrue();
        await Assert.That(unsubscribeCommand.Id).IsEqualTo(subscribeCommand.Id);

        await using (var verifyScope = _fixture.Factory.Services.CreateAsyncScope())
        {
            var context = verifyScope.ServiceProvider.GetRequiredService<ExploreDbContext>();
            var subscription = await context.ActorSubscriptions
                .IgnoreQueryFilters()
                .SingleAsync(row => row.Id == subscribeCommand.Id);
            await Assert.That(subscription.StatusId).IsEqualTo((int)ActorSubscriptionStatusEnum.Unsubscribed);
            await Assert.That(subscription.UnsubscribedAt).IsNotNull();
        }

        var resubscribeResponse = await PostSubscribeAsync(scenario);

        await Assert.That(resubscribeResponse.StatusCode).IsEqualTo(HttpStatusCode.OK);
        var resubscribeCommand = await resubscribeResponse.Content.ReadFromJsonAsync<BaseCommandResponse<Guid>>();
        await Assert.That(resubscribeCommand).IsNotNull();
        await Assert.That(resubscribeCommand!.Success).IsTrue();
        await Assert.That(resubscribeCommand.Id).IsEqualTo(subscribeCommand.Id);

        await using var finalScope = _fixture.Factory.Services.CreateAsyncScope();
        var finalContext = finalScope.ServiceProvider.GetRequiredService<ExploreDbContext>();
        var reactivated = await finalContext.ActorSubscriptions
            .IgnoreQueryFilters()
            .SingleAsync(row => row.Id == subscribeCommand.Id);
        await Assert.That(reactivated.StatusId).IsEqualTo((int)ActorSubscriptionStatusEnum.Active);
        await Assert.That(reactivated.NotificationLevelId).IsEqualTo((int)ActorSubscriptionNotificationLevelEnum.All);
        await Assert.That(reactivated.UnsubscribedAt).IsNull();
    }

    private async Task<HttpResponseMessage> PostSubscribeAsync(SubscriptionScenario scenario)
    {
        using var subscribeRequest = _fixture.CreateAuthenticatedRequest(
            HttpMethod.Post,
            "/api/actor-subscriptions",
            scenario.UserId);
        subscribeRequest.Content = JsonContent.Create(new SubscribeToActorDto
        {
            TargetActorId = scenario.TargetActorId
        });

        return await _fixture.Client.SendAsync(subscribeRequest);
    }

    private async Task<Guid> GetSubscriptionConcurrencyStampAsync(Guid subscriptionId)
    {
        await using var verifyScope = _fixture.Factory.Services.CreateAsyncScope();
        var context = verifyScope.ServiceProvider.GetRequiredService<ExploreDbContext>();
        var subscription = await context.ActorSubscriptions
            .IgnoreQueryFilters()
            .SingleAsync(row => row.Id == subscriptionId);
        return subscription.ConcurrencyStamp;
    }

    private async Task<SubscriptionScenario> SeedSubscriptionScenarioAsync(
        ActorTypeEnum targetActorType = ActorTypeEnum.Organization,
        TenantUserStatusEnum tenantUserStatus = TenantUserStatusEnum.Active)
    {
        await using var scope = _fixture.Factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<ExploreDbContext>();

        var tenant = await context.Tenants
            .IgnoreQueryFilters()
            .SingleOrDefaultAsync(row => row.Id == PlatformDefaults.DefaultTenantId);
        if (tenant is null)
        {
            tenant = new TenantBuilder()
                .WithId(PlatformDefaults.DefaultTenantId)
                .WithFullName("Default Test Tenant")
                .WithSlug($"default-test-{Guid.NewGuid():N}")
                .Build();
            context.Tenants.Add(tenant);
        }

        var user = new UserBuilder().Build();
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var userActor = new ActorBuilder()
            .WithUserId(user.Id)
            .WithDisplayName("Subscription Test User")
            .Build();
        var organizationActor = new ActorBuilder()
            .WithActorType(targetActorType)
            .WithDisplayName($"Subscription Target {Guid.NewGuid():N}")
            .Build();

        if (targetActorType == ActorTypeEnum.Organization)
        {
            var organization = new Organization
            {
                Id = Guid.CreateVersion7(),
                Pii = new OrganizationPii { FullName = "Subscription Target" },
                Actor = organizationActor
            };
            organizationActor.OrganizationId = organization.Id;
            organizationActor.Organization = organization;
            context.Organizations.Add(organization);
            context.OrganizationTenants.Add(new OrganizationTenant
            {
                Id = Guid.CreateVersion7(),
                TenantId = tenant.Id,
                Tenant = tenant,
                OrganizationId = organization.Id,
                Organization = organization,
                ApprovalStatusId = (int)ApprovalStatusEnum.Approved,
                ApprovalStatus = null!,
                IsVisible = true
            });
        }
        else if (targetActorType == ActorTypeEnum.Group)
        {
            var group = new Group
            {
                Id = Guid.CreateVersion7(),
                FullName = "Subscription Target",
                Actor = organizationActor
            };
            organizationActor.GroupId = group.Id;
            organizationActor.Group = group;
            context.Groups.Add(group);
            context.GroupTenants.Add(new GroupTenant
            {
                Id = Guid.CreateVersion7(),
                TenantId = tenant.Id,
                Tenant = tenant,
                GroupId = group.Id,
                Group = group,
                ApprovalStatusId = (int)ApprovalStatusEnum.Approved,
                ApprovalStatus = null!,
                IsVisible = true
            });
        }

        context.Actors.AddRange(userActor, organizationActor);
        await context.SaveChangesAsync();

        var tenantUser = new TenantUser
        {
            Id = Guid.NewGuid(),
            TenantId = tenant.Id,
            Tenant = tenant,
            UserId = user.Id,
            User = user,
            ActorId = userActor.Id,
            Actor = userActor,
            StatusId = (int)tenantUserStatus,
            JoinedAt = DateTime.UtcNow
        };
        context.TenantUsers.Add(tenantUser);
        await context.SaveChangesAsync();

        return new SubscriptionScenario(tenant.Id, user.Id, tenantUser.Id, organizationActor.Id);
    }

    private sealed record SubscriptionScenario(
        Guid TenantId,
        Guid UserId,
        Guid TenantUserId,
        Guid TargetActorId);
}
