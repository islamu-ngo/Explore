// ABOUTME: Unit tests for tenant storage settings mapping through the generated Event API client.
// ABOUTME: Ensures generated HAL edit affordances gate updates and generated DTOs carry saved values.

namespace Explore.Blazor.Client.Tests.Services;

public sealed class TenantStorageSettingsAdminServiceTests
{
    private readonly IEventApiClient _api = Substitute.For<IEventApiClient>();
    private readonly TenantStorageSettingsAdminService _service;

    public TenantStorageSettingsAdminServiceTests()
    {
        _service = new TenantStorageSettingsAdminService(
            _api,
            Substitute.For<ILogger<TenantStorageSettingsAdminService>>());
    }

    [Test]
    public async Task GetAsync_MapsEditableHalResource()
    {
        _api.GetTenantStorageSettingsAsync(
                Arg.Any<string?>(),
                Arg.Any<string?>(),
                Arg.Any<CancellationToken>())
            .Returns(new HalResourceOfTenantStorageSettingsDto
            {
                TenantId = Guid.NewGuid(),
                Provider = "local",
                MaxUploadBytes = 10 * 1024 * 1024,
                TenantQuotaBytes = 1024L * 1024 * 1024,
                IsReadOnly = false,
                TenantOverridesAllowed = true,
                TenantStorageLocked = false,
                EffectivePolicy = new EffectivePolicy2
                {
                    Provider = "local",
                    MaxUploadBytes = 10 * 1024 * 1024,
                    TenantQuotaBytes = 1024L * 1024 * 1024,
                    InstanceMaxUploadBytes = 100L * 1024 * 1024
                },
                Usage = new Usage2
                {
                    Provider = "local",
                    UsedBytes = 1024,
                    AvailableBytes = 2048,
                    ObjectCount = 2
                },
                _links = new Dictionary<string, HalLink>
                {
                    ["edit"] = new() { Href = "/api/tenant/settings/storage", Method = "PATCH" }
                }
            });

        var result = await _service.GetAsync();

        await Assert.That(result.IsEditable).IsTrue();
        await Assert.That(result.Usage!.UsedBytes).IsEqualTo(1024);
        await Assert.That(result.EffectivePolicy!.InstanceMaxUploadBytes).IsEqualTo(100L * 1024 * 1024);
    }

    [Test]
    public async Task SaveAsync_DoesNotCallApi_WhenEditAffordanceMissing()
    {
        var result = await _service.SaveAsync(new HalResourceOfTenantStorageSettingsDto());

        await Assert.That(result.Success).IsFalse();
        await _api.DidNotReceive().PatchTenantStorageSettingsAsync(
            Arg.Any<PatchTenantStorageSettingsDto>(),
            Arg.Any<string?>(),
            Arg.Any<string?>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task SaveAsync_ForwardsGeneratedTenantStorageDto_WhenEditable()
    {
        _api.PatchTenantStorageSettingsAsync(
                Arg.Any<PatchTenantStorageSettingsDto>(),
                Arg.Any<string?>(),
                Arg.Any<string?>(),
                Arg.Any<CancellationToken>())
            .Returns(new BaseCommandResponseOfGuid { Success = true, Message = "Updated" });
        var model = new HalResourceOfTenantStorageSettingsDto
        {
            TenantId = Guid.NewGuid(),
            IsReadOnly = false,
            TenantOverridesAllowed = true,
            TenantStorageLocked = false,
            Provider = StorageProviderOptions.Local,
            MaxUploadBytes = 10 * 1024 * 1024,
            TenantQuotaBytes = 1024L * 1024 * 1024,
            S3AccessKeyConfigured = true,
            S3SecretAccessKeyConfigured = true,
            _links = new Dictionary<string, HalLink>
            {
                ["edit"] = new() { Href = "/api/tenant/settings/storage", Method = "PATCH" }
            }
        };

        var result = await _service.SaveAsync(model);

        await Assert.That(result.Success).IsTrue();
        await _api.Received(1).PatchTenantStorageSettingsAsync(
            Arg.Is<PatchTenantStorageSettingsDto>(request =>
                request != null &&
                request.Policy != null &&
                request.Policy.Provider != null &&
                request.Policy.Provider.HasValue == true &&
                request.Policy.Provider.Value == "local" &&
                request.Policy.MaxUploadBytes != null &&
                request.Policy.MaxUploadBytes.HasValue == true &&
                request.Policy.MaxUploadBytes.Value == 10 * 1024 * 1024 &&
                request.S3 != null &&
                request.S3.AccessKeyId == null &&
                request.S3.SecretAccessKey == null),
            Arg.Any<string?>(),
            Arg.Any<string?>(),
            Arg.Any<CancellationToken>());
    }
}
