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
    public async Task PatchPolicyAsync_DoesNotCallApi_WhenEditAffordanceMissing()
    {
        var result = await _service.PatchPolicyAsync(new HalResourceOfTenantStorageSettingsDto());

        await Assert.That(result.Success).IsFalse();
        await _api.DidNotReceive().PatchTenantStorageSettingsAsync(
            Arg.Any<PatchTenantStorageSettingsDto>(),
            Arg.Any<string?>(),
            Arg.Any<string?>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task PatchPolicyAsync_ForwardsOnlyCompletePolicyGroup_WhenEditable()
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

        var result = await _service.PatchPolicyAsync(model);

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
                request.S3 == null),
            Arg.Any<string?>(),
            Arg.Any<string?>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task PatchS3Async_ForwardsOnlyNonSecretS3Leaves_AndAlwaysOmitsCredentials()
    {
        _api.PatchTenantStorageSettingsAsync(
                Arg.Any<PatchTenantStorageSettingsDto>(),
                Arg.Any<string?>(),
                Arg.Any<string?>(),
                Arg.Any<CancellationToken>())
            .Returns(new BaseCommandResponseOfGuid { Success = true });
        var model = CreateEditableModel();
        model.S3Endpoint = " https://storage.example.test ";
        model.S3Region = "eu-central-1";
        model.S3ForcePathStyle = false;
        model.S3UploadUrlExpirationMinutes = 90;
        model.S3AccessKeyId = "typed-access-key";
        model.S3SecretAccessKey = "typed-secret-key";
        model.S3AccessKeyConfigured = true;
        model.S3SecretAccessKeyConfigured = true;

        var result = await _service.PatchS3Async(model);

        await Assert.That(result.Success).IsTrue();
        await _api.Received(1).PatchTenantStorageSettingsAsync(
            Arg.Is<PatchTenantStorageSettingsDto>(request => IsS3OnlyRequestWithOmittedCredentials(request!)),
            Arg.Any<string?>(),
            Arg.Any<string?>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task PatchS3CredentialsAsync_ForwardsExactlyBothCredentialLeaves_InOneS3Group()
    {
        _api.PatchTenantStorageSettingsAsync(
                Arg.Any<PatchTenantStorageSettingsDto>(),
                Arg.Any<string?>(),
                Arg.Any<string?>(),
                Arg.Any<CancellationToken>())
            .Returns(new BaseCommandResponseOfGuid { Success = true });
        var model = CreateEditableModel();
        model.S3Endpoint = "https://storage.example.test";
        model.S3AccessKeyId = " typed-access-key ";
        model.S3SecretAccessKey = " typed-secret-key ";

        var result = await _service.PatchS3CredentialsAsync(model);

        await Assert.That(result.Success).IsTrue();
        await _api.Received(1).PatchTenantStorageSettingsAsync(
            Arg.Is<PatchTenantStorageSettingsDto>(request => IsCredentialOnlyRequest(request!)),
            Arg.Any<string?>(),
            Arg.Any<string?>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task TestProviderAsync_WhenAffordanceExists_ReturnsApiPreflight()
    {
        var expected = new InstanceStorageProviderStatusDto
        {
            IsAvailable = true,
            Preflight = new S3PreflightResult { IsSuccess = true, CanWrite = true }
        };
        _api.TestTenantStorageConnectionAsync(
                Arg.Any<string?>(),
                Arg.Any<string?>(),
                Arg.Any<CancellationToken>())
            .Returns(expected);

        var result = await _service.TestProviderAsync(CreateEditableModel());

        await Assert.That(result).IsSameReferenceAs(expected);
        await Assert.That(result.Preflight?.CanWrite).IsTrue();
    }

    [Test]
    public async Task TestProviderAsync_WhenAffordanceMissing_DoesNotCallApi()
    {
        var result = await _service.TestProviderAsync(new HalResourceOfTenantStorageSettingsDto());

        await Assert.That(result.FailureCode).IsEqualTo("provider_test_not_allowed");
        await _api.DidNotReceive().TestTenantStorageConnectionAsync(
            Arg.Any<string?>(),
            Arg.Any<string?>(),
            Arg.Any<CancellationToken>());
    }

    private static HalResourceOfTenantStorageSettingsDto CreateEditableModel() => new()
    {
        TenantOverridesAllowed = true,
        TenantStorageLocked = false,
        IsReadOnly = false,
        Provider = StorageProviderOptions.S3Compatible,
        MaxUploadBytes = 10 * 1024 * 1024,
        TenantQuotaBytes = 1024L * 1024 * 1024,
        _links = new Dictionary<string, HalLink>
        {
            ["edit"] = new() { Href = "/api/tenant/settings/storage", Method = "PATCH" },
            ["provider-test"] = new() { Href = "/api/tenant/settings/storage/test", Method = "POST" }
        }
    };

    private static bool IsS3OnlyRequestWithOmittedCredentials(PatchTenantStorageSettingsDto request)
    {
        PatchTenantStorageS3Dto? s3 = request.S3;
        return request.Policy is null
            && s3?.Endpoint?.Value == "https://storage.example.test"
            && s3.Region?.Value == "eu-central-1"
            && s3.ForcePathStyle?.Value == false
            && s3.UploadUrlExpirationMinutes?.Value == 90
            && s3.AccessKeyId is null
            && s3.SecretAccessKey is null;
    }

    private static bool IsCredentialOnlyRequest(PatchTenantStorageSettingsDto request)
    {
        PatchTenantStorageS3Dto? s3 = request.S3;
        return request.Policy is null
            && s3?.AccessKeyId?.Value == "typed-access-key"
            && s3.SecretAccessKey?.Value == "typed-secret-key"
            && s3.Endpoint is null
            && s3.PublicEndpoint is null
            && s3.BucketName is null
            && s3.Region is null
            && s3.ForcePathStyle is null
            && s3.UploadUrlExpirationMinutes is null;
    }
}
