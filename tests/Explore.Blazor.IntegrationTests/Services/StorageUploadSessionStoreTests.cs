// ABOUTME: Unit-style tests for BFF storage upload session binding.
// ABOUTME: Proves upload destinations must be server-issued, user-bound, and content-type-bound.

using System.Security.Claims;
using System.Text.Json;
using Explore.Blazor.Client.Clients;
using Explore.Blazor.Services;
using FluentAssertions;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;

using Options = Microsoft.Extensions.Options.Options;

namespace Explore.Blazor.IntegrationTests.Services;

public sealed class StorageUploadSessionStoreTests
{
    private readonly MemoryDistributedCache _cache = new(Options.Create(new MemoryDistributedCacheOptions()));
    private readonly StorageUploadSessionStore _store;

    public StorageUploadSessionStoreTests()
    {
        _store = new StorageUploadSessionStore(_cache);
    }

    [Test]
    public async Task IssueAsync_WithMissingApiUploadSessionId_ReturnsFailure()
    {
        var result = await _store.IssueAsync(
            CreateUser("user-1"),
            new StorageUploadSessionDto
            {
                Id = Guid.Empty,
                Provider = "local",
                ExpectedSizeBytes = 4,
                ReservedBytes = 4,
                ContentType = "image/png",
                SafeDisplayName = "probe.png",
                Purpose = "legacy_image",
                Visibility = "public_image",
                Status = "reserved",
                ExpiresAt = DateTime.UtcNow.AddMinutes(15)
            },
            "image/png");

        result.Success.Should().BeFalse();
        result.FailureCode.Should().Be("missing_upload_session_id");
    }

    [Test]
    public async Task IssueAsync_WithExpiredApiUploadSession_ReturnsFailure()
    {
        var result = await _store.IssueAsync(
            CreateUser("user-1"),
            new StorageUploadSessionDto
            {
                Id = ApiUploadSessionId,
                Provider = "local",
                ExpectedSizeBytes = 4,
                ReservedBytes = 4,
                ContentType = "image/png",
                SafeDisplayName = "probe.png",
                Purpose = "legacy_image",
                Visibility = "public_image",
                Status = "reserved",
                ExpiresAt = DateTime.UtcNow.AddMinutes(-1)
            },
            "image/png");

        result.Success.Should().BeFalse();
        result.FailureCode.Should().Be("upload_session_expired");
    }

    [Test]
    public async Task ResolveAsync_WithDifferentUser_ReturnsFailure()
    {
        var issued = await IssueTrustedSessionAsync("user-1", "image/png");

        var resolved = await _store.ResolveAsync(CreateUser("user-2"), issued.SessionId!, "image/png");

        resolved.Success.Should().BeFalse();
        resolved.FailureCode.Should().Be("session_owner_mismatch");
    }

    [Test]
    public async Task ResolveAsync_WithDifferentContentType_ReturnsFailure()
    {
        var issued = await IssueTrustedSessionAsync("user-1", "image/png");

        var resolved = await _store.ResolveAsync(CreateUser("user-1"), issued.SessionId!, "image/jpeg");

        resolved.Success.Should().BeFalse();
        resolved.FailureCode.Should().Be("content_type_mismatch");
    }

    [Test]
    public async Task ResolveAsync_WithSameUserAndContentType_ReturnsApiUploadSession()
    {
        var issued = await IssueTrustedSessionAsync("user-1", "image/png");

        var resolved = await _store.ResolveAsync(CreateUser("user-1"), issued.SessionId!, "image/png");

        resolved.Success.Should().BeTrue();
        resolved.Session.Should().NotBeNull();
        resolved.Session!.ApiUploadSessionId.Should().Be(ApiUploadSessionId);
        resolved.Session.ExpectedSizeBytes.Should().Be(4);
    }

    [Test]
    public async Task ResolveAsync_WithExpiredCachedSession_ReturnsFailureAndConsumesSession()
    {
        const string sessionId = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
        var expiredSession = new StorageUploadSession(
            sessionId,
            "user-1",
            ApiUploadSessionId,
            "image/png",
            4,
            DateTimeOffset.UtcNow.AddMinutes(-1));
        var payload = JsonSerializer.Serialize(expiredSession, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        await _cache.SetStringAsync("storage-upload-session:" + sessionId, payload);

        var resolved = await _store.ResolveAsync(CreateUser("user-1"), sessionId, "image/png");
        var secondResolve = await _store.ResolveAsync(CreateUser("user-1"), sessionId, "image/png");

        resolved.Success.Should().BeFalse();
        resolved.FailureCode.Should().Be("session_expired");
        secondResolve.Success.Should().BeFalse();
        secondResolve.FailureCode.Should().Be("session_not_found");
    }

    private static readonly Guid ApiUploadSessionId = Guid.CreateVersion7();

    private Task<StorageUploadSessionIssueResult> IssueTrustedSessionAsync(string userId, string contentType) =>
        _store.IssueAsync(
            CreateUser(userId),
            new StorageUploadSessionDto
            {
                Id = ApiUploadSessionId,
                Provider = "local",
                ExpectedSizeBytes = 4,
                ReservedBytes = 4,
                ContentType = contentType,
                SafeDisplayName = "probe.png",
                Purpose = "legacy_image",
                Visibility = "public_image",
                Status = "reserved",
                ExpiresAt = DateTime.UtcNow.AddMinutes(15)
            },
            contentType);

    private static ClaimsPrincipal CreateUser(string userId) =>
        new(new ClaimsIdentity(
        [
            new Claim("sub", userId),
            new Claim(ClaimTypes.Name, "Storage Tester")
        ], "Test"));
}
