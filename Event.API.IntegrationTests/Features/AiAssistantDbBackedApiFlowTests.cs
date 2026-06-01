// ABOUTME: PostgreSQL-backed API flow tests for persisted AI assistant conversations.
// ABOUTME: Verifies the real EF repositories behind authenticated create/send/detail endpoints.

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Event.Api.IntegrationTests.Fixtures;
using Event.Api.IntegrationTests.Seeds;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Infrastructure.Ai;
using Explore.Application.DTOs.Ai;
using Explore.Application.Responses;
using Explore.Application.Settings;
using Explore.Application.Settings.Groups;
using Explore.Domain.Ai;
using Explore.Domain.Constants;
using Explore.Domain.Settings;
using Explore.Infrastructure.Ai;
using Explore.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Event.Api.IntegrationTests.Features;

[ClassDataSource<AiAssistantDbBackedApiFixture>(Shared = SharedType.PerAssembly)]
[NotInParallel("RealRuntimeDb")]
public sealed class AiAssistantDbBackedApiFlowTests(AiAssistantDbBackedApiFixture fixture)
{
    private readonly AiAssistantDbBackedApiFixture _fixture = fixture;

    [Test]
    public async Task CreateSendAndDetail_WithPostgreSqlPersistence_PersistsConversationMessagesAndRun()
    {
        var seeded = await ResetAndSeedAsync();
        var conversationId = await CreateConversationAsync(seeded.UserId, "DB-backed planning");
        var runId = await SendMessageAsync(seeded.UserId, conversationId, "db-flow", "Plan the opening night.");

        using var detailRequest = _fixture.CreateAuthenticatedRequest(
            HttpMethod.Get,
            $"/api/ai/assistant/conversations/{conversationId}",
            seeded.UserId);
        var response = await _fixture.Client.SendAsync(detailRequest);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        var json = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        var root = json.RootElement;
        await Assert.That(root.GetProperty("id").GetGuid()).IsEqualTo(conversationId);
        await Assert.That(root.GetProperty("messages").GetArrayLength()).IsEqualTo(2);
        await Assert.That(root.GetProperty("runs").GetArrayLength()).IsEqualTo(1);
        await Assert.That(root.GetProperty("runs")[0].GetProperty("id").GetGuid()).IsEqualTo(runId);
        await Assert.That(root.GetProperty("_links").TryGetProperty("send-message", out _)).IsTrue();

        await using var scope = _fixture.Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ExploreDbContext>();
        var persistedConversation = await db.AiConversations.FindAsync(conversationId);
        await Assert.That(persistedConversation).IsNotNull();
        await Assert.That(persistedConversation!.TenantId).IsEqualTo(seeded.TenantId);
    }

    [Test]
    public async Task SendMessage_WithSameIdempotencyKeyAndPayload_ReplaysRunIdFromRepository()
    {
        var seeded = await ResetAndSeedAsync();
        var conversationId = await CreateConversationAsync(seeded.UserId, "Idempotent planning");
        var firstRunId = await SendMessageAsync(seeded.UserId, conversationId, "db-replay", "Plan the event.");

        var replayRunId = await SendMessageAsync(seeded.UserId, conversationId, "db-replay", "Plan the event.");

        await Assert.That(replayRunId).IsEqualTo(firstRunId);
    }

    [Test]
    public async Task SendMessage_WithSameIdempotencyKeyAndDifferentPayload_ReturnsApplicationConflict()
    {
        var seeded = await ResetAndSeedAsync();
        var conversationId = await CreateConversationAsync(seeded.UserId, "Conflict planning");
        await SendMessageAsync(seeded.UserId, conversationId, "db-conflict", "Plan the event.");
        using var request = _fixture.CreateAuthenticatedRequest(
            HttpMethod.Post,
            $"/api/ai/assistant/conversations/{conversationId}/messages",
            seeded.UserId);
        request.Headers.Add("Idempotency-Key", "db-conflict");
        request.Content = JsonContent.Create(new SendAiMessageRequestDto { Content = "Plan another event." });

        var response = await _fixture.Client.SendAsync(request);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Conflict);
        var json = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        await Assert.That(json.RootElement.GetProperty("code").GetString()).IsEqualTo("idempotency_key_conflict");
    }

    [Test]
    public async Task Detail_WhenRequestedByDifferentUser_ReturnsNotFoundWithPostgreSqlPersistence()
    {
        var seeded = await ResetAndSeedAsync();
        var conversationId = await CreateConversationAsync(seeded.UserId, "Private planning");
        using var request = _fixture.CreateAuthenticatedRequest(
            HttpMethod.Get,
            $"/api/ai/assistant/conversations/{conversationId}",
            Guid.NewGuid());

        var response = await _fixture.Client.SendAsync(request);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
    }

    private async Task<TenantScenarioSeed.TenantScenarioResult> ResetAndSeedAsync()
    {
        await _fixture.ResetDatabaseAsync();
        await using var scope = _fixture.Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ExploreDbContext>();
        return await TenantScenarioSeed.SeedActiveTenantWithUserAsync(db);
    }

    private async Task<Guid> CreateConversationAsync(Guid userId, string title)
    {
        using var request = _fixture.CreateAuthenticatedRequest(HttpMethod.Post, "/api/ai/assistant/conversations", userId);
        request.Content = JsonContent.Create(new CreateAiConversationRequestDto { Title = title });

        var response = await _fixture.Client.SendAsync(request);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<BaseCommandResponse<Guid>>();
        await Assert.That(body).IsNotNull();
        await Assert.That(body!.Success).IsTrue();
        return body.Id;
    }

    private async Task<Guid> SendMessageAsync(Guid userId, Guid conversationId, string idempotencyKey, string content)
    {
        using var request = _fixture.CreateAuthenticatedRequest(
            HttpMethod.Post,
            $"/api/ai/assistant/conversations/{conversationId}/messages",
            userId);
        request.Headers.Add("Idempotency-Key", idempotencyKey);
        request.Content = JsonContent.Create(new SendAiMessageRequestDto { Content = content });

        var response = await _fixture.Client.SendAsync(request);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Accepted);
        var body = await response.Content.ReadFromJsonAsync<BaseCommandResponse<Guid>>();
        await Assert.That(body).IsNotNull();
        await Assert.That(body!.Success).IsTrue();
        return body.Id;
    }
}

public sealed class AiAssistantDbBackedApiFixture : RealRuntimeApiFixture
{
    protected override void ConfigureAdditionalTestServices(IServiceCollection services)
    {
        services.RemoveAll<IHierarchicalSettingsResolver>();
        services.RemoveAll<IAiChatProvider>();
        services.RemoveAll<IAiModelCatalog>();

        services.AddSingleton<IHierarchicalSettingsResolver>(new FixedAiSettingsResolver(CreateFakeSettings()));
        services.AddSingleton<FakeAiChatProvider>();
        services.AddSingleton<IAiChatProvider>(sp => sp.GetRequiredService<FakeAiChatProvider>());
        services.AddSingleton<IAiModelCatalog>(sp => sp.GetRequiredService<FakeAiChatProvider>());
    }

    private static AiAssistantSettingGroup CreateFakeSettings()
    {
        var values = new Dictionary<string, object?>
        {
            [GovernanceSettingKeys.AiAssistant.Enabled] = true,
            [GovernanceSettingKeys.AiAssistant.Provider] = AiProviderDefaults.ProviderFake,
            [GovernanceSettingKeys.AiAssistant.DailyMessageLimit] = 50,
            [GovernanceSettingKeys.AiAssistant.ToolProposalsEnabled] = true
        };

        var resolved = values.ToDictionary(
            pair => pair.Key,
            pair => new ResolvedSetting
            {
                Key = pair.Key,
                Value = JsonSerializer.Serialize(pair.Value),
                Source = SettingSource.SystemDefault,
                IsLocked = false
            });

        var group = new AiAssistantSettingGroup();
        group.Populate(resolved);
        return group;
    }

    private sealed class FixedAiSettingsResolver(AiAssistantSettingGroup settings) : IHierarchicalSettingsResolver
    {
        public Task<T?> ResolveAsync<T>(string key, SettingContext context, CancellationToken ct = default)
            => Task.FromResult(default(T));

        public Task<ResolvedSetting?> ResolveWithMetadataAsync(string key, SettingContext context, CancellationToken ct = default)
            => Task.FromResult<ResolvedSetting?>(null);

        public Task<IReadOnlyList<ResolvedSetting>> ResolveBatchAsync(
            IEnumerable<string> keys,
            SettingContext context,
            CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<ResolvedSetting>>([]);

        public Task<TGroup> ResolveGroupAsync<TGroup>(SettingContext context, CancellationToken ct = default)
            where TGroup : ISettingGroup, new()
        {
            if (typeof(TGroup) == typeof(AiAssistantSettingGroup))
            {
                return Task.FromResult((TGroup)(object)settings);
            }

            return Task.FromResult(new TGroup());
        }

        public Task SetValueAsync(
            string key,
            string value,
            SettingScope scope,
            Guid scopeId,
            Guid actorId,
            CancellationToken ct = default)
            => Task.CompletedTask;

        public Task RemoveOverrideAsync(
            string key,
            SettingScope scope,
            Guid scopeId,
            Guid actorId,
            CancellationToken ct = default)
            => Task.CompletedTask;

        public Task LockAsync(
            string key,
            SettingScope scope,
            Guid scopeId,
            Guid actorId,
            CancellationToken ct = default)
            => Task.CompletedTask;

        public Task UnlockAsync(
            string key,
            SettingScope scope,
            Guid scopeId,
            Guid actorId,
            CancellationToken ct = default)
            => Task.CompletedTask;

        public void InvalidateCache(SettingScope? scope = null, Guid? scopeId = null)
        {
        }

        public void InvalidateUserCache(Guid tenantId, Guid userId)
        {
        }
    }
}
