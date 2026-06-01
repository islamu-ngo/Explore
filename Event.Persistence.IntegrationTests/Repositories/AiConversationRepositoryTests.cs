// ABOUTME: PostgreSQL-backed tests for AI conversation repository persistence and tenant filtering.
// ABOUTME: Verifies migrated AI tables, aggregate updates, message ordering, quota counts, and action lookup.

using Event.Persistence.IntegrationTests.Fixtures;
using Explore.Application.Contracts.Infrastructure;
using Explore.Domain;
using Explore.Domain.Ai;
using Explore.Domain.Enums;
using Explore.Persistence;
using Explore.Persistence.Repositories;
using TUnit.Core;

namespace Event.Persistence.IntegrationTests.Repositories;

[ClassDataSource<PostgreSqlContainerFixture>(Shared = SharedType.PerAssembly)]
[NotInParallel("PersistenceDb")]
public sealed class AiConversationRepositoryTests(PostgreSqlContainerFixture fixture)
{
    [Test]
    public async Task CreateAndUpdate_WithTrackedAggregate_PersistsMessagesRunsAndActions()
    {
        await fixture.ResetAsync();
        var scope = await SeedTenantAndUserAsync("ai-repository-update");

        await using (var createContext = fixture.CreateTenantFilteredDbContext(new TestTenantContext(scope.TenantId)))
        {
            var repository = new AiConversationRepository(createContext);
            await repository.Create(NewConversation(scope, "Planning"));
        }

        await using (var updateContext = fixture.CreateTenantFilteredDbContext(new TestTenantContext(scope.TenantId)))
        {
            var repository = new AiConversationRepository(updateContext);
            var conversation = await repository.GetByIdForUpdateAsync(scope.ConversationId, CancellationToken.None);
            await Assert.That(conversation).IsNotNull();

            var userMessage = conversation!.AddMessage(AiMessageRole.User, "Plan the event", scope.UserId, DateTime.UtcNow);
            var assistantMessage = conversation.AddMessage(AiMessageRole.Assistant, "Here is a plan", null, DateTime.UtcNow.AddSeconds(1));
            var run = conversation.QueueRun("fake", "fake-ai-assistant-v1", DateTime.UtcNow.AddSeconds(2));
            run.Start(DateTime.UtcNow.AddSeconds(3));
            conversation.ProposeAction(
                AiProposedActionKind.CreateEventDraft,
                "{\"title\":\"AI event\"}",
                assistantMessage.Id,
                scope.UserId,
                DateTime.UtcNow.AddSeconds(4));
            conversation.CompleteRun(run, DateTime.UtcNow.AddSeconds(5));

            await repository.Update(conversation);
        }

        await using var verifyContext = fixture.CreateTenantFilteredDbContext(new TestTenantContext(scope.TenantId));
        var verifyRepository = new AiConversationRepository(verifyContext);
        var saved = await verifyRepository.GetByIdWithDetailsAsync(scope.ConversationId, CancellationToken.None);

        await Assert.That(saved).IsNotNull();
        await Assert.That(saved!.Status).IsEqualTo(AiConversationStatus.Active);
        await Assert.That(saved.Messages.Select(message => message.Sequence)).IsEquivalentTo([1L, 2L]);
        await Assert.That(saved.Messages.Select(message => message.Role)).IsEquivalentTo([AiMessageRole.User, AiMessageRole.Assistant]);
        await Assert.That(saved.Runs.Single().Status).IsEqualTo(AiRunStatus.Succeeded);
        await Assert.That(saved.ProposedActions.Single().Status).IsEqualTo(AiProposedActionStatus.Proposed);
        await Assert.That(saved.ProposedActions.Single().PayloadJson).Contains("AI event");
    }

    [Test]
    public async Task ListRecentForUserAsync_ReturnsOnlyCurrentTenantRows()
    {
        await fixture.ResetAsync();
        var tenantA = await SeedTenantAndUserAsync("ai-list-a");
        var tenantB = await SeedTenantAndUserAsync("ai-list-b", tenantA.UserId);

        await using (var seedContext = fixture.CreateDbContext())
        {
            seedContext.AiConversations.Add(NewConversation(tenantA, "Tenant A"));
            seedContext.AiConversations.Add(NewConversation(tenantB, "Tenant B"));
            await seedContext.SaveChangesAsync();
        }

        await using var context = fixture.CreateTenantFilteredDbContext(new TestTenantContext(tenantA.TenantId));
        var repository = new AiConversationRepository(context);

        var conversations = await repository.ListRecentForUserAsync(tenantA.UserId, limit: 10, CancellationToken.None);

        await Assert.That(conversations.Select(conversation => conversation.Id)).IsEquivalentTo([tenantA.ConversationId]);
    }

    [Test]
    public async Task GetByIdWithDetailsAsync_WhenTenantDoesNotMatch_ReturnsNull()
    {
        await fixture.ResetAsync();
        var tenantA = await SeedTenantAndUserAsync("ai-detail-a");
        var tenantB = await SeedTenantAndUserAsync("ai-detail-b", tenantA.UserId);

        await using (var seedContext = fixture.CreateDbContext())
        {
            seedContext.AiConversations.Add(NewConversation(tenantA, "Tenant A"));
            await seedContext.SaveChangesAsync();
        }

        await using var wrongTenantContext = fixture.CreateTenantFilteredDbContext(new TestTenantContext(tenantB.TenantId));
        var repository = new AiConversationRepository(wrongTenantContext);

        var conversation = await repository.GetByIdWithDetailsAsync(tenantA.ConversationId, CancellationToken.None);

        await Assert.That(conversation).IsNull();
    }

    [Test]
    public async Task CountUserMessagesSinceAsync_CountsOnlyCurrentTenantUserMessages()
    {
        await fixture.ResetAsync();
        var tenantA = await SeedTenantAndUserAsync("ai-quota-a");
        var tenantB = await SeedTenantAndUserAsync("ai-quota-b", tenantA.UserId);
        var since = DateTime.UtcNow.AddHours(-1);

        await using (var seedContext = fixture.CreateDbContext())
        {
            var conversationA = NewConversation(tenantA, "Tenant A");
            conversationA.AddMessage(AiMessageRole.User, "Count me", tenantA.UserId, DateTime.UtcNow);
            conversationA.AddMessage(AiMessageRole.Assistant, "Do not count assistant", null, DateTime.UtcNow.AddSeconds(1));

            var conversationB = NewConversation(tenantB, "Tenant B");
            conversationB.AddMessage(AiMessageRole.User, "Wrong tenant", tenantB.UserId, DateTime.UtcNow);

            seedContext.AiConversations.AddRange(conversationA, conversationB);
            await seedContext.SaveChangesAsync();
        }

        await using var context = fixture.CreateTenantFilteredDbContext(new TestTenantContext(tenantA.TenantId));
        var repository = new AiConversationRepository(context);

        var count = await repository.CountUserMessagesSinceAsync(tenantA.UserId, since, CancellationToken.None);

        await Assert.That(count).IsEqualTo(1);
    }

    [Test]
    public async Task GetProposedActionForUpdateAsync_RespectsTenantFilter()
    {
        await fixture.ResetAsync();
        var tenantA = await SeedTenantAndUserAsync("ai-action-a");
        var tenantB = await SeedTenantAndUserAsync("ai-action-b", tenantA.UserId);
        Guid actionId;

        await using (var seedContext = fixture.CreateDbContext())
        {
            var conversation = NewConversation(tenantA, "Tenant A");
            actionId = conversation.ProposeAction(
                AiProposedActionKind.CreateEventDraft,
                "{\"title\":\"Tenant A event\"}",
                messageId: null,
                tenantA.UserId,
                DateTime.UtcNow).Id;
            seedContext.AiConversations.Add(conversation);
            await seedContext.SaveChangesAsync();
        }

        await using var wrongTenantContext = fixture.CreateTenantFilteredDbContext(new TestTenantContext(tenantB.TenantId));
        var wrongTenantRepository = new AiConversationRepository(wrongTenantContext);
        var hiddenAction = await wrongTenantRepository.GetProposedActionForUpdateAsync(actionId, CancellationToken.None);

        await using var tenantContext = fixture.CreateTenantFilteredDbContext(new TestTenantContext(tenantA.TenantId));
        var repository = new AiConversationRepository(tenantContext);
        var action = await repository.GetProposedActionForUpdateAsync(actionId, CancellationToken.None);

        await Assert.That(hiddenAction).IsNull();
        await Assert.That(action).IsNotNull();
        await Assert.That(action!.Conversation).IsNotNull();
        await Assert.That(action.PayloadJson).Contains("Tenant A event");
    }

    private async Task<AiTestScope> SeedTenantAndUserAsync(string slugPrefix, Guid? userId = null)
    {
        await using var context = fixture.CreateDbContext();
        var tenant = new Tenant
        {
            Id = Guid.NewGuid(),
            FullName = $"AI Test {slugPrefix}",
            Slug = $"ai-test-{slugPrefix}-{Guid.NewGuid().ToString("N")[..8]}",
            TenantStatusId = (int)TenantStatusEnum.Active,
            TenantStatus = null!,
        };
        context.Tenants.Add(tenant);

        User user;
        if (userId is { } existingUserId)
        {
            user = (await context.Users.FindAsync(existingUserId))!;
        }
        else
        {
            user = new User
            {
                Id = Guid.NewGuid(),
                Pii = new UserPii
                {
                    Email = $"ai-{slugPrefix}-{Guid.NewGuid():N}@example.com",
                    FirstName = "AI",
                    LastName = "Tester",
                },
                EmailVerified = true,
                CreatedAt = DateTime.UtcNow,
            };
            context.Users.Add(user);
        }

        await context.SaveChangesAsync();
        return new AiTestScope(tenant.Id, user.Id, Guid.CreateVersion7());
    }

    private static AiConversation NewConversation(AiTestScope scope, string title) =>
        new()
        {
            Id = scope.ConversationId,
            TenantId = scope.TenantId,
            UserId = scope.UserId,
            Status = AiConversationStatus.Active,
            Title = title,
            Provider = "fake",
            ModelId = "fake-ai-assistant-v1",
            CreatedAt = DateTime.UtcNow,
            CreatedBy = scope.UserId,
            ConcurrencyStamp = Guid.CreateVersion7(),
        };

    private sealed record TestTenantContext(Guid TenantId) : ITenantContext;

    private sealed record AiTestScope(Guid TenantId, Guid UserId, Guid ConversationId);
}
