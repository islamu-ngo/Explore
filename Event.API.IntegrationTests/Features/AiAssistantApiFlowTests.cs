// ABOUTME: Host-backed API flow tests for AI assistant conversation and message endpoints.
// ABOUTME: Exercises auth gates, fake-provider send flow, idempotency, and HAL over the in-memory test host.

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Event.Api.IntegrationTests.Fixtures;
using Explore.API;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Infrastructure.Ai;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.Ai;
using Explore.Application.Responses;
using Explore.Application.Settings;
using Explore.Application.Settings.Groups;
using Explore.Domain;
using Explore.Domain.Ai;
using Explore.Domain.Constants;
using Explore.Domain.Settings;
using Explore.Infrastructure.Ai;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Event.Api.IntegrationTests.Features;

public sealed class AiAssistantApiFlowTests
{
    private static readonly Guid DefaultTenantId = Guid.Parse("018e4e5c-7f00-7000-8000-000000000001");
    private static readonly Guid AlternateTenantId = Guid.Parse("018e4e5c-7f00-7000-8000-000000000002");
    private const string TenantHeaderName = "X-Test-Tenant";

    [Test]
    public async Task Conversations_WithoutAuthentication_ReturnsUnauthorized()
    {
        await using var factory = new AiAssistantApiFlowFactory(CreateFakeSettings());
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/ai/assistant/conversations");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task CreateConversation_WhenAssistantDisabled_ReturnsForbiddenProblemDetails()
    {
        await using var factory = new AiAssistantApiFlowFactory(CreateDisabledSettings());
        using var client = factory.CreateClient();
        using var request = CreateAuthenticatedRequest(HttpMethod.Post, "/api/ai/assistant/conversations");
        request.Content = JsonContent.Create(new CreateAiConversationRequestDto { Title = "Planning" });

        var response = await client.SendAsync(request);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Forbidden);
        var json = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        await Assert.That(json.RootElement.GetProperty("code").GetString()).IsEqualTo("disabled");
    }

    [Test]
    public async Task FakeProviderFlow_CreatesConversationSendsMessageAndReturnsHalDetail()
    {
        await using var factory = new AiAssistantApiFlowFactory(CreateFakeSettings());
        using var client = factory.CreateClient();
        var userId = Guid.NewGuid();
        var conversationId = await CreateConversationAsync(client, userId, "Planning");
        var runId = await SendMessageAsync(client, userId, conversationId, "idem-flow", "Plan the opening night.");

        using var detailRequest = CreateAuthenticatedRequest(
            HttpMethod.Get,
            $"/api/ai/assistant/conversations/{conversationId}",
            userId);
        var response = await client.SendAsync(detailRequest);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        var json = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        var root = json.RootElement;
        await Assert.That(root.GetProperty("id").GetGuid()).IsEqualTo(conversationId);
        await Assert.That(root.GetProperty("messages").GetArrayLength()).IsEqualTo(2);
        await Assert.That(root.GetProperty("runs").GetArrayLength()).IsEqualTo(1);
        await Assert.That(root.GetProperty("runs")[0].GetProperty("id").GetGuid()).IsEqualTo(runId);
        await Assert.That(root.GetProperty("_links").TryGetProperty("send-message", out _)).IsTrue();
    }

    [Test]
    public async Task SendMessage_WithSameIdempotencyKeyAndPayload_ReplaysRunId()
    {
        await using var factory = new AiAssistantApiFlowFactory(CreateFakeSettings());
        using var client = factory.CreateClient();
        var userId = Guid.NewGuid();
        var conversationId = await CreateConversationAsync(client, userId, "Planning");
        var firstRunId = await SendMessageAsync(client, userId, conversationId, "idem-replay", "Plan the event.");

        var replayRunId = await SendMessageAsync(client, userId, conversationId, "idem-replay", "Plan the event.");

        await Assert.That(replayRunId).IsEqualTo(firstRunId);
    }

    [Test]
    public async Task SendMessage_WithSameIdempotencyKeyAndDifferentPayload_ReturnsConflictProblemDetails()
    {
        await using var factory = new AiAssistantApiFlowFactory(CreateFakeSettings());
        using var client = factory.CreateClient();
        var userId = Guid.NewGuid();
        var conversationId = await CreateConversationAsync(client, userId, "Planning");
        await SendMessageAsync(client, userId, conversationId, "idem-conflict", "Plan the event.");

        using var request = CreateAuthenticatedRequest(
            HttpMethod.Post,
            $"/api/ai/assistant/conversations/{conversationId}/messages",
            userId);
        request.Headers.Add("Idempotency-Key", "idem-conflict");
        request.Content = JsonContent.Create(new SendAiMessageRequestDto { Content = "Plan a different event." });

        var response = await client.SendAsync(request);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Conflict);
        var json = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        await Assert.That(json.RootElement.GetProperty("code").GetString()).IsEqualTo("idempotency_key_reuse");
    }

    [Test]
    public async Task Detail_WhenRequestedByDifferentUser_ReturnsNotFound()
    {
        await using var factory = new AiAssistantApiFlowFactory(CreateFakeSettings());
        using var client = factory.CreateClient();
        var ownerId = Guid.NewGuid();
        var conversationId = await CreateConversationAsync(client, ownerId, "Planning");

        using var request = CreateAuthenticatedRequest(
            HttpMethod.Get,
            $"/api/ai/assistant/conversations/{conversationId}",
            Guid.NewGuid());

        var response = await client.SendAsync(request);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task Detail_WhenRequestedFromDifferentTenant_ReturnsNotFound()
    {
        await using var factory = new AiAssistantApiFlowFactory(CreateFakeSettings());
        using var client = factory.CreateClient();
        var userId = Guid.NewGuid();
        var conversationId = await CreateConversationAsync(client, userId, "Planning", DefaultTenantId);

        using var request = CreateAuthenticatedRequest(
            HttpMethod.Get,
            $"/api/ai/assistant/conversations/{conversationId}",
            userId,
            AlternateTenantId);

        var response = await client.SendAsync(request);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task SendMessage_WhenProviderFails_ReturnsSafeServiceUnavailableProblemDetails()
    {
        await using var factory = new AiAssistantApiFlowFactory(CreateFakeSettings(), providerFails: true);
        using var client = factory.CreateClient();
        var userId = Guid.NewGuid();
        var conversationId = await CreateConversationAsync(client, userId, "Planning");
        using var request = CreateAuthenticatedRequest(
            HttpMethod.Post,
            $"/api/ai/assistant/conversations/{conversationId}/messages",
            userId);
        request.Headers.Add("Idempotency-Key", "provider-failure");
        request.Content = JsonContent.Create(new SendAiMessageRequestDto { Content = "Plan the event." });

        var response = await client.SendAsync(request);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.ServiceUnavailable);
        var json = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        var root = json.RootElement;
        await Assert.That(root.GetProperty("code").GetString()).IsEqualTo("provider_unreachable");
        await Assert.That(root.GetProperty("title").GetString()).IsEqualTo("AI provider unavailable");
        await Assert.That(root.GetProperty("detail").GetString()).DoesNotContain("Plan the event");
        await Assert.That(root.GetProperty("detail").GetString()).DoesNotContain("api_key");
    }

    private static async Task<Guid> CreateConversationAsync(
        HttpClient client,
        Guid userId,
        string title,
        Guid? tenantId = null)
    {
        using var request = CreateAuthenticatedRequest(
            HttpMethod.Post,
            "/api/ai/assistant/conversations",
            userId,
            tenantId);
        request.Content = JsonContent.Create(new CreateAiConversationRequestDto { Title = title });

        var response = await client.SendAsync(request);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<BaseCommandResponse<Guid>>();
        await Assert.That(body).IsNotNull();
        await Assert.That(body!.Success).IsTrue();
        return body.Id;
    }

    private static async Task<Guid> SendMessageAsync(
        HttpClient client,
        Guid userId,
        Guid conversationId,
        string idempotencyKey,
        string content,
        Guid? tenantId = null)
    {
        using var request = CreateAuthenticatedRequest(
            HttpMethod.Post,
            $"/api/ai/assistant/conversations/{conversationId}/messages",
            userId,
            tenantId);
        request.Headers.Add("Idempotency-Key", idempotencyKey);
        request.Content = JsonContent.Create(new SendAiMessageRequestDto { Content = content });

        var response = await client.SendAsync(request);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Accepted);
        var body = await response.Content.ReadFromJsonAsync<BaseCommandResponse<Guid>>();
        await Assert.That(body).IsNotNull();
        await Assert.That(body!.Success).IsTrue();
        return body.Id;
    }

    private static HttpRequestMessage CreateAuthenticatedRequest(
        HttpMethod method,
        string url,
        Guid? userId = null,
        Guid? tenantId = null)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.Add(
            TestAuthHandler.AuthHeaderName,
            TestAuthHandler.CreateAuthHeaderValue(userId ?? Guid.NewGuid()));
        request.Headers.Add(TenantHeaderName, (tenantId ?? DefaultTenantId).ToString());
        return request;
    }

    private static AiAssistantSettingGroup CreateFakeSettings()
        => CreateSettings(enabled: true, provider: AiProviderDefaults.ProviderFake, toolProposalsEnabled: true);

    private static AiAssistantSettingGroup CreateDisabledSettings()
        => CreateSettings(enabled: false, provider: AiProviderDefaults.ProviderNone);

    private static AiAssistantSettingGroup CreateSettings(
        bool enabled,
        string provider,
        bool toolProposalsEnabled = false)
    {
        var values = new Dictionary<string, object?>
        {
            [GovernanceSettingKeys.AiAssistant.Enabled] = enabled,
            [GovernanceSettingKeys.AiAssistant.Provider] = provider,
            [GovernanceSettingKeys.AiAssistant.DailyMessageLimit] = 50,
            [GovernanceSettingKeys.AiAssistant.ToolProposalsEnabled] = toolProposalsEnabled
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

    private sealed class AiAssistantApiFlowFactory(AiAssistantSettingGroup settings, bool providerFails = false)
        : AuthenticatedWebApplicationFactory
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            AuthorizationProviderOverride = new StubAuthorizationProvider();
            base.ConfigureWebHost(builder);

            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IHierarchicalSettingsResolver>();
                services.RemoveAll<ITenantContext>();
                services.AddSingleton<IHierarchicalSettingsResolver>(new FixedAiSettingsResolver(settings));
                services.AddScoped<ITenantContext, HeaderTenantContext>();

                services.RemoveAll<IAiChatProvider>();
                services.RemoveAll<IAiModelCatalog>();
                services.RemoveAll<IAiConversationRepository>();
                services.RemoveAll<IIdempotencyRepository>();

                services.AddSingleton<InMemoryAiConversationStore>();
                services.AddScoped<IAiConversationRepository, InMemoryAiConversationRepository>();
                services.AddSingleton<IIdempotencyRepository, InMemoryIdempotencyRepository>();

                if (providerFails)
                {
                    services.AddSingleton<FailingAiChatProvider>();
                    services.AddSingleton<IAiChatProvider>(sp => sp.GetRequiredService<FailingAiChatProvider>());
                    services.AddSingleton<IAiModelCatalog>(sp => sp.GetRequiredService<FailingAiChatProvider>());
                }
                else
                {
                    services.AddSingleton<FakeAiChatProvider>();
                    services.AddSingleton<IAiChatProvider>(sp => sp.GetRequiredService<FakeAiChatProvider>());
                    services.AddSingleton<IAiModelCatalog>(sp => sp.GetRequiredService<FakeAiChatProvider>());
                }
            });
        }
    }

    private sealed class HeaderTenantContext(IHttpContextAccessor httpContextAccessor) : ITenantContext
    {
        public Guid TenantId
        {
            get
            {
                var header = httpContextAccessor.HttpContext?.Request.Headers[TenantHeaderName].FirstOrDefault();
                return Guid.TryParse(header, out var tenantId) ? tenantId : DefaultTenantId;
            }
        }
    }

    private sealed class InMemoryAiConversationStore
    {
        public Dictionary<Guid, AiConversation> Conversations { get; } = [];
    }

    private sealed class InMemoryAiConversationRepository(
        InMemoryAiConversationStore store,
        ITenantContext tenantContext) : IAiConversationRepository
    {
        private IEnumerable<AiConversation> TenantConversations
            => store.Conversations.Values.Where(conversation => conversation.TenantId == tenantContext.TenantId);

        public Task<AiConversation?> GetById(Guid id)
            => Task.FromResult(TenantConversations.FirstOrDefault(conversation => conversation.Id == id));

        public Task<IReadOnlyList<AiConversation>> GetAll()
            => Task.FromResult<IReadOnlyList<AiConversation>>(TenantConversations.ToList());

        public Task<(IReadOnlyList<AiConversation> Items, int TotalCount)> GetAllPaged(int pageNumber, int pageSize)
        {
            var conversations = TenantConversations.ToList();
            var items = conversations
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return Task.FromResult<(
                IReadOnlyList<AiConversation> Items,
                int TotalCount)>((items, conversations.Count));
        }

        public Task<bool> Exists(Guid id)
            => Task.FromResult(TenantConversations.Any(conversation => conversation.Id == id));

        public Task<AiConversation> Create(AiConversation entity)
        {
            store.Conversations[entity.Id] = entity;
            return Task.FromResult(entity);
        }

        public Task Update(AiConversation entity)
        {
            store.Conversations[entity.Id] = entity;
            return Task.CompletedTask;
        }

        public Task Delete(AiConversation entity)
        {
            store.Conversations.Remove(entity.Id);
            return Task.CompletedTask;
        }

        public Task HardDelete(AiConversation entity)
            => Delete(entity);

        public Task<AiConversation?> GetByIdWithDetailsAsync(Guid conversationId, CancellationToken cancellationToken)
            => GetById(conversationId);

        public Task<AiConversation?> GetByIdForUpdateAsync(Guid conversationId, CancellationToken cancellationToken)
            => GetById(conversationId);

        public Task<IReadOnlyList<AiConversation>> ListRecentForUserAsync(
            Guid userId,
            int limit,
            CancellationToken cancellationToken)
        {
            var conversations = TenantConversations
                .Where(conversation => conversation.UserId == userId)
                .OrderByDescending(conversation => conversation.UpdatedAt ?? conversation.CreatedAt)
                .ThenByDescending(conversation => conversation.Id)
                .Take(Math.Max(0, limit))
                .ToList();

            return Task.FromResult<IReadOnlyList<AiConversation>>(conversations);
        }

        public Task<int> CountUserMessagesSinceAsync(Guid userId, DateTime sinceUtc, CancellationToken cancellationToken)
        {
            var count = TenantConversations
                .SelectMany(conversation => conversation.Messages)
                .Count(message => message.Role == AiMessageRole.User
                    && message.CreatedBy == userId
                    && message.CreatedAt >= sinceUtc);

            return Task.FromResult(count);
        }

        public Task<int> CountTenantMessagesSinceAsync(DateTime sinceUtc, CancellationToken cancellationToken)
        {
            var count = TenantConversations
                .SelectMany(conversation => conversation.Messages)
                .Count(message => message.Role == AiMessageRole.User && message.CreatedAt >= sinceUtc);

            return Task.FromResult(count);
        }

        public Task<int> CountRunningConversationsForUserAsync(Guid userId, CancellationToken cancellationToken)
        {
            var count = TenantConversations
                .Count(conversation => conversation.UserId == userId && conversation.Status == AiConversationStatus.Running);

            return Task.FromResult(count);
        }

        public Task<AiProposedAction?> GetProposedActionForUpdateAsync(Guid proposedActionId, CancellationToken cancellationToken)
        {
            var action = TenantConversations
                .SelectMany(conversation => conversation.ProposedActions)
                .FirstOrDefault(candidate => candidate.Id == proposedActionId);

            return Task.FromResult(action);
        }
    }

    private sealed class FailingAiChatProvider : IAiChatProvider, IAiModelCatalog
    {
        public Task<AiChatProviderResult> SendAsync(AiChatRequest request, CancellationToken cancellationToken = default)
            => Task.FromResult(AiChatProviderResult.Failure(
                "provider_unreachable",
                "AI provider is unavailable.",
                isTransient: true));

        public Task<IReadOnlyList<AiModelDescriptor>> ListAvailableModelsAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<AiModelDescriptor>>(
            [
                new AiModelDescriptor(
                    AiProviderDefaults.FakeModelId,
                    AiProviderDefaults.FakeModelDisplayName,
                    AiProviderDefaults.DefaultMaxInputTokens,
                    AiProviderDefaults.DefaultMaxOutputTokens,
                    SupportsToolProposals: true)
            ]);
    }

    private sealed class InMemoryIdempotencyRepository : IIdempotencyRepository
    {
        private readonly Dictionary<(Guid TenantId, string Key), IdempotencyRecord> _records = [];

        public Task<IdempotencyRecord?> FindAsync(string key, Guid tenantId, CancellationToken cancellationToken = default)
            => Task.FromResult(_records.GetValueOrDefault((tenantId, key)));

        public Task SaveAsync(IdempotencyRecord record, CancellationToken cancellationToken = default)
        {
            _records[(record.TenantId, record.Key)] = record;
            return Task.CompletedTask;
        }

        public Task<int> CountExpiredAsync(DateTime expiresBeforeUtc, int batchSize, CancellationToken cancellationToken = default)
        {
            var count = _records.Values.Count(record => record.ExpiresAt < expiresBeforeUtc);
            return Task.FromResult(Math.Min(count, batchSize));
        }

        public Task<int> DeleteExpiredAsync(DateTime expiresBeforeUtc, int batchSize, CancellationToken cancellationToken = default)
        {
            var expired = _records
                .Where(pair => pair.Value.ExpiresAt < expiresBeforeUtc)
                .Take(batchSize)
                .Select(pair => pair.Key)
                .ToList();

            foreach (var key in expired)
            {
                _records.Remove(key);
            }

            return Task.FromResult(expired.Count);
        }
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
