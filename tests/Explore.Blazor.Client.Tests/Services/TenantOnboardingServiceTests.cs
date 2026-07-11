// ABOUTME: Unit tests for tenant onboarding delegation through the generated Event API client.
// ABOUTME: Verifies generated contract mapping, command results, and resilient failure behavior.

namespace Explore.Blazor.Client.Tests.Services;

public class TenantOnboardingServiceTests
{
    private readonly IEventApiClient _api;
    private readonly TenantOnboardingService _service;

    public TenantOnboardingServiceTests()
    {
        _api = Substitute.For<IEventApiClient>();
        _service = new TenantOnboardingService(
            _api,
            Substitute.For<ILogger<TenantOnboardingService>>());
    }

    [Test]
    public async Task GetStatusAsync_ReturnsMappedStatus_WhenApiSucceeds()
    {
        var tenantId = Guid.NewGuid();
        _api.GetTenantOnboardingStatusAsync(
                Arg.Any<string?>(),
                Arg.Any<string?>(),
                Arg.Any<CancellationToken>())
            .Returns(new TenantOnboardingStatusDto
            {
                IsCompleted = true,
                IsAuthenticated = true,
                IsCurrentUserTenantAdministrator = true,
                IsCurrentUserPlatformAdministrator = false,
                TenantId = tenantId
            });

        var result = await _service.GetStatusAsync();

        await Assert.That(result).IsNotNull();
        await Assert.That(result!.IsCompleted).IsTrue();
        await Assert.That(result.TenantId).IsEqualTo(tenantId);
        await _api.Received(1).GetTenantOnboardingStatusAsync(
            Arg.Any<string?>(),
            Arg.Any<string?>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task GetStatusAsync_ReturnsNull_WhenApiThrows()
    {
        _api.GetTenantOnboardingStatusAsync(
                Arg.Any<string?>(),
                Arg.Any<string?>(),
                Arg.Any<CancellationToken>())
            .Returns<Task<TenantOnboardingStatusDto>>(_ => throw new HttpRequestException("boom"));

        var result = await _service.GetStatusAsync();

        await Assert.That(result).IsNull();
    }

    [Test]
    public async Task GetSettingsAsync_ReturnsMappedSettings_WhenApiSucceeds()
    {
        _api.GetTenantOnboardingPolicySettingsAsync(
                Arg.Any<string?>(),
                Arg.Any<string?>(),
                Arg.Any<CancellationToken>())
            .Returns(new TenantPolicySettingsDto
            {
                AllowUserSubmittedEvents = false,
                RequireEventApproval = true,
                PreferredHomePage = "Dashboard"
            });

        var result = await _service.GetSettingsAsync();

        await Assert.That(result.RequireEventApproval).IsTrue();
        await Assert.That(result.PreferredHomePage).IsEqualTo("Dashboard");
    }

    [Test]
    public async Task GetSettingsAsync_ReturnsDefaultSettings_WhenApiThrows()
    {
        _api.GetTenantOnboardingPolicySettingsAsync(
                Arg.Any<string?>(),
                Arg.Any<string?>(),
                Arg.Any<CancellationToken>())
            .Returns<Task<TenantPolicySettingsDto>>(_ => throw new HttpRequestException("boom"));

        var result = await _service.GetSettingsAsync();

        await Assert.That(result).IsNotNull();
        await Assert.That(result.PreferredHomePage).IsEqualTo("EventList");
    }

    [Test]
    public async Task CompleteAsync_ForwardsMappedSettingsAndReturnsSuccess()
    {
        _api.CompleteTenantOnboardingAsync(
                Arg.Any<UpdateTenantPolicyRequest>(),
                Arg.Any<string?>(),
                Arg.Any<string?>(),
                Arg.Any<CancellationToken>())
            .Returns(new BaseCommandResponseOfGuid
            {
                Success = true,
                Message = "OK"
            });
        var settings = new TenantPolicySettingsDto
        {
            PreferredHomePage = "Dashboard",
            RequireEventApproval = true
        };

        var result = await _service.CompleteAsync(settings);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Message).IsEqualTo("OK");
        await _api.Received(1).CompleteTenantOnboardingAsync(
            Arg.Is<UpdateTenantPolicyRequest>(request =>
                request.PreferredHomePage == "Dashboard" && request.RequireEventApproval == true),
            Arg.Any<string?>(),
            Arg.Any<string?>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task CompleteAsync_ReturnsFailure_WhenApiThrows()
    {
        _api.CompleteTenantOnboardingAsync(
                Arg.Any<UpdateTenantPolicyRequest>(),
                Arg.Any<string?>(),
                Arg.Any<string?>(),
                Arg.Any<CancellationToken>())
            .Returns<Task<BaseCommandResponseOfGuid>>(_ => throw new HttpRequestException("network failed"));

        var result = await _service.CompleteAsync(new TenantPolicySettingsDto());

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.Message).IsEqualTo("Request failed.");
        await Assert.That(result.Errors).Contains("network failed");
    }

    [Test]
    public async Task UpdateSettingsAsync_ReturnsSuccess_WhenApiSucceeds()
    {
        _api.UpdateTenantOnboardingPolicySettingsAsync(
                Arg.Any<UpdateTenantPolicyRequest>(),
                Arg.Any<string?>(),
                Arg.Any<string?>(),
                Arg.Any<CancellationToken>())
            .Returns(new BaseCommandResponseOfGuid
            {
                Success = true,
                Message = "Updated"
            });

        var result = await _service.UpdateSettingsAsync(new TenantPolicySettingsDto());

        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Message).IsEqualTo("Updated");
    }

    [Test]
    public async Task UpdateSettingsAsync_ReturnsStatusFailure_WhenApiRejectsRequest()
    {
        _api.UpdateTenantOnboardingPolicySettingsAsync(
                Arg.Any<UpdateTenantPolicyRequest>(),
                Arg.Any<string?>(),
                Arg.Any<string?>(),
                Arg.Any<CancellationToken>())
            .Returns<Task<BaseCommandResponseOfGuid>>(_ => throw new ApiException(
                "Bad Request",
                400,
                string.Empty,
                new Dictionary<string, IEnumerable<string>>(),
                null));

        var result = await _service.UpdateSettingsAsync(new TenantPolicySettingsDto());

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.Message).IsEqualTo("Request failed with status 400.");
    }
}
