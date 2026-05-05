// ABOUTME: Authenticated integration tests for persisted external API key management endpoints.
// ABOUTME: Verifies policy updates stay owner-scoped and only mutate safe editable metadata.

using System.Net;
using System.Net.Http.Json;
using Event.Api.IntegrationTests.Fixtures;
using Explore.Application.DTOs.ExternalApiKey;
using Explore.Application.Responses;
using Explore.Domain.Enums;
using Explore.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Event.Api.IntegrationTests.Features;

[ClassDataSource<SingleTenantAuthenticatedApiTestFixture>(Shared = SharedType.PerAssembly)]
public class ExternalApiKeyIntegrationTests
{
    private readonly SingleTenantAuthenticatedApiTestFixture _fixture;

    public ExternalApiKeyIntegrationTests(SingleTenantAuthenticatedApiTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Test]
    public async Task UpdateExternalApiKeyPolicy_WithOwnerRequest_ShouldUpdateEditableFields()
    {
        var userId = Guid.NewGuid();
        var apiKeyId = await CreateExternalApiKeyAsync(userId, "Build Bot", ["events:read"]);
        var expiresAt = DateTime.UtcNow.AddDays(30);

        var payload = new UpdateExternalApiKeyPolicyDto
        {
            Id = apiKeyId,
            Name = "Deploy Bot",
            Scopes = ["events:write", "events:read", "events:write"],
            ExpiresAt = expiresAt
        };

        using var request = _fixture.CreateAuthenticatedRequest(HttpMethod.Put, $"/api/externalapikey/{apiKeyId}", userId);
        request.Content = JsonContent.Create(payload);

        var response = await _fixture.Client.SendAsync(request);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<BaseCommandResponse<Guid>>();
        await Assert.That(body).IsNotNull();
        await Assert.That(body!.Success).IsTrue();
        await Assert.That(body.Id).IsEqualTo(apiKeyId);

        using var scope = _fixture.Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ExploreDbContext>();
        var stored = await dbContext.ExternalApiKeys.SingleAsync(x => x.Id == apiKeyId);

        await Assert.That(stored.Name).IsEqualTo("Deploy Bot");
        await Assert.That(stored.Scopes).IsEqualTo("events:read events:write");
        await Assert.That(stored.ExpiresAt).IsEqualTo(expiresAt);
        await Assert.That(stored.OwnerId).IsEqualTo(userId);
        await Assert.That(stored.ExternalApiKeyStatusId).IsEqualTo((int)ExternalApiKeyStatusEnum.Active);
        await Assert.That(stored.UpdatedAt).IsNotNull();
    }

    [Test]
    public async Task GetExternalApiKeyDetails_WithOwnerRequest_ShouldReturnVisibleMetadata()
    {
        var userId = Guid.NewGuid();
        var apiKeyId = await CreateExternalApiKeyAsync(userId, "Reader Bot", ["events:read", "events:write"]);

        using var request = _fixture.CreateAuthenticatedRequest(HttpMethod.Get, $"/api/externalapikey/{apiKeyId}", userId);
        var response = await _fixture.Client.SendAsync(request);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<ExternalApiKeyListDto>(TestJsonOptions.Default);
        await Assert.That(body).IsNotNull();
        await Assert.That(body!.Id).IsEqualTo(apiKeyId);
        await Assert.That(body.Name).IsEqualTo("Reader Bot");
        await Assert.That(body.ExternalApiKeyOwnerTypeId).IsEqualTo((int)ExternalApiKeyOwnerType.User);
        await Assert.That(body.ExternalApiKeyOwnerTypeCode).IsEqualTo("USER");
        await Assert.That(body.OwnerId).IsEqualTo(userId);
        await Assert.That(body.Scopes).IsEquivalentTo(["events:read", "events:write"]);
        await Assert.That(body.KeyId.Length).IsEqualTo(16);
    }

    [Test]
    public async Task GetExternalApiKeyDetails_WithDifferentUser_ShouldReturnNotFound()
    {
        var ownerUserId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var apiKeyId = await CreateExternalApiKeyAsync(ownerUserId, "Private Bot", ["events:read"]);

        using var request = _fixture.CreateAuthenticatedRequest(HttpMethod.Get, $"/api/externalapikey/{apiKeyId}", otherUserId);
        var response = await _fixture.Client.SendAsync(request);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task UpdateExternalApiKeyPolicy_WithDifferentUser_ShouldReturnNotFoundAndLeaveKeyUnchanged()
    {
        var ownerUserId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var apiKeyId = await CreateExternalApiKeyAsync(ownerUserId, "Owner Key", ["events:read"]);

        var payload = new UpdateExternalApiKeyPolicyDto
        {
            Id = apiKeyId,
            Name = "Hijacked Key",
            Scopes = ["events:write"],
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        };

        using var request = _fixture.CreateAuthenticatedRequest(HttpMethod.Put, $"/api/externalapikey/{apiKeyId}", otherUserId);
        request.Content = JsonContent.Create(payload);

        var response = await _fixture.Client.SendAsync(request);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.NotFound);

        using var scope = _fixture.Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ExploreDbContext>();
        var stored = await dbContext.ExternalApiKeys.SingleAsync(x => x.Id == apiKeyId);

        await Assert.That(stored.Name).IsEqualTo("Owner Key");
        await Assert.That(stored.Scopes).IsEqualTo("events:read");
        await Assert.That(stored.ExpiresAt).IsNull();
        await Assert.That(stored.OwnerId).IsEqualTo(ownerUserId);
    }

    [Test]
    public async Task DeleteExternalApiKey_WithOwnerRequest_ShouldRevokeKeyAndPopulateAuditFields()
    {
        var userId = Guid.NewGuid();
        var apiKeyId = await CreateExternalApiKeyAsync(userId, "Ops Bot", ["events:read"]);

        using var request = _fixture.CreateAuthenticatedRequest(HttpMethod.Delete, $"/api/externalapikey/{apiKeyId}", userId);
        var response = await _fixture.Client.SendAsync(request);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.NoContent);

        using var scope = _fixture.Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ExploreDbContext>();
        var stored = await dbContext.ExternalApiKeys.SingleAsync(x => x.Id == apiKeyId);

        await Assert.That(stored.ExternalApiKeyStatusId).IsEqualTo((int)ExternalApiKeyStatusEnum.Revoked);
        await Assert.That(stored.UpdatedAt).IsNotNull();
    }

    private async Task<Guid> CreateExternalApiKeyAsync(Guid userId, string name, List<string> scopes)
    {
        var payload = new CreateExternalApiKeyDto
        {
            Name = name,
            Scopes = scopes,
            ExternalApiKeyOwnerTypeId = (int)ExternalApiKeyOwnerType.User
        };

        using var request = _fixture.CreateAuthenticatedRequest(HttpMethod.Post, "/api/externalapikey", userId);
        request.Content = JsonContent.Create(payload);

        var response = await _fixture.Client.SendAsync(request);
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<CreateExternalApiKeyCommandResponse>();
        await Assert.That(body).IsNotNull();
        await Assert.That(body!.Success).IsTrue();

        using var scope = _fixture.Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ExploreDbContext>();
        var stored = await dbContext.ExternalApiKeys.SingleAsync(x => x.Id == body.Id);
        await Assert.That(stored.CreatedAt).IsNotEqualTo(default(DateTime));

        return body.Id;
    }
}
