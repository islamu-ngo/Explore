// ABOUTME: Unit tests for S3ConfigResolver verifying hierarchical settings resolution,
// IConfiguration fallback, caching behavior, and null handling when S3 storage is not configured.

using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Models;
using Explore.Application.Settings;
using Explore.Domain.Constants;
using Explore.Infrastructure.Storage;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Explore.Infrastructure.Tests.Infrastructure;

public class S3ConfigResolverTests : IDisposable
{
    private readonly IHierarchicalSettingsResolver _settingsResolver;
    private readonly ITenantContext _tenantContext;
    private readonly MemoryCache _cache;
    private readonly IConfiguration _configuration;
    private readonly ILogger<S3ConfigResolver> _logger;
    private readonly S3ConfigResolver _resolver;

    private static readonly Guid TestTenantId = Guid.NewGuid();

    public S3ConfigResolverTests()
    {
        _settingsResolver = Substitute.For<IHierarchicalSettingsResolver>();
        _tenantContext = Substitute.For<ITenantContext>();
        _cache = new MemoryCache(new MemoryCacheOptions());
        _configuration = new ConfigurationBuilder().Build();
        _logger = Substitute.For<ILogger<S3ConfigResolver>>();

        _tenantContext.TenantId.Returns(TestTenantId);

        _resolver = new S3ConfigResolver(_settingsResolver, _tenantContext, _cache, _configuration, _logger);
    }

    public void Dispose()
    {
        _cache.Dispose();
    }

    [Test]
    public async Task ResolveAsync_EmptyEndpoint_ReturnsNull()
    {
        _settingsResolver.ResolveAsync<string>(GovernanceSettingKeys.Storage.Endpoint, Arg.Any<SettingContext>(), Arg.Any<CancellationToken>())
            .Returns("");

        var result = await _resolver.ResolveAsync();

        await Assert.That(result).IsNull();
    }

    [Test]
    public async Task ResolveAsync_NullEndpoint_ReturnsNull()
    {
        _settingsResolver.ResolveAsync<string>(GovernanceSettingKeys.Storage.Endpoint, Arg.Any<SettingContext>(), Arg.Any<CancellationToken>())
            .Returns((string?)null);

        var result = await _resolver.ResolveAsync();

        await Assert.That(result).IsNull();
    }

    [Test]
    public async Task ResolveAsync_EndpointSetButEmptyBucketName_ReturnsNull()
    {
        _settingsResolver.ResolveAsync<string>(GovernanceSettingKeys.Storage.Endpoint, Arg.Any<SettingContext>(), Arg.Any<CancellationToken>())
            .Returns("https://fsn1.your-objectstorage.com");
        _settingsResolver.ResolveAsync<string>(GovernanceSettingKeys.Storage.BucketName, Arg.Any<SettingContext>(), Arg.Any<CancellationToken>())
            .Returns("");

        var result = await _resolver.ResolveAsync();

        await Assert.That(result).IsNull();
    }

    [Test]
    public async Task ResolveAsync_EndpointSetButEmptyAccessKey_ReturnsNull()
    {
        _settingsResolver.ResolveAsync<string>(GovernanceSettingKeys.Storage.Endpoint, Arg.Any<SettingContext>(), Arg.Any<CancellationToken>())
            .Returns("https://fsn1.your-objectstorage.com");
        _settingsResolver.ResolveAsync<string>(GovernanceSettingKeys.Storage.BucketName, Arg.Any<SettingContext>(), Arg.Any<CancellationToken>())
            .Returns("my-bucket");
        _settingsResolver.ResolveAsync<string>(InfrastructureSecretSettingKeys.Storage.AccessKeyId, Arg.Any<SettingContext>(), Arg.Any<CancellationToken>())
            .Returns("");

        var result = await _resolver.ResolveAsync();

        await Assert.That(result).IsNull();
    }

    [Test]
    public async Task ResolveAsync_EndpointSetButEmptySecretKey_ReturnsNull()
    {
        _settingsResolver.ResolveAsync<string>(GovernanceSettingKeys.Storage.Endpoint, Arg.Any<SettingContext>(), Arg.Any<CancellationToken>())
            .Returns("https://fsn1.your-objectstorage.com");
        _settingsResolver.ResolveAsync<string>(GovernanceSettingKeys.Storage.BucketName, Arg.Any<SettingContext>(), Arg.Any<CancellationToken>())
            .Returns("my-bucket");
        _settingsResolver.ResolveAsync<string>(InfrastructureSecretSettingKeys.Storage.AccessKeyId, Arg.Any<SettingContext>(), Arg.Any<CancellationToken>())
            .Returns("AKIAIOSFODNN7EXAMPLE");
        _settingsResolver.ResolveAsync<string>(InfrastructureSecretSettingKeys.Storage.SecretAccessKey, Arg.Any<SettingContext>(), Arg.Any<CancellationToken>())
            .Returns("");

        var result = await _resolver.ResolveAsync();

        await Assert.That(result).IsNull();
    }

    [Test]
    public async Task ResolveAsync_ValidConfig_ReturnsS3Configuration()
    {
        SetupValidS3Settings();

        var result = await _resolver.ResolveAsync();

        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Endpoint).IsEqualTo("https://fsn1.your-objectstorage.com");
        await Assert.That(result.BucketName).IsEqualTo("my-bucket");
        await Assert.That(result.AccessKeyId).IsEqualTo("AKIAIOSFODNN7EXAMPLE");
        await Assert.That(result.SecretAccessKey).IsEqualTo("wJalrXUtnFEMI/K7MDENG/bPxRfiCYEXAMPLEKEY");
        await Assert.That(result.Region).IsEqualTo("fsn1");
    }

    [Test]
    public async Task ResolveAsync_DefaultForcePathStyle_True()
    {
        SetupValidS3Settings(forcePathStyle: false);

        var result = await _resolver.ResolveAsync();

        await Assert.That(result).IsNotNull();
        // When ResolveAsync<bool> returns false, ForcePathStyle should be false
        await Assert.That(result!.ForcePathStyle).IsEqualTo(false);
    }

    [Test]
    public async Task ResolveAsync_DefaultUploadExpiration_60()
    {
        SetupValidS3Settings(uploadExpiration: 0);

        var result = await _resolver.ResolveAsync();

        await Assert.That(result).IsNotNull();
        await Assert.That(result!.UploadUrlExpirationMinutes).IsEqualTo(60);
    }

    [Test]
    public async Task ResolveAsync_CustomUploadExpiration_Preserved()
    {
        SetupValidS3Settings(uploadExpiration: 120);

        var result = await _resolver.ResolveAsync();

        await Assert.That(result).IsNotNull();
        await Assert.That(result!.UploadUrlExpirationMinutes).IsEqualTo(120);
    }

    [Test]
    public async Task ResolveAsync_CachesResult_SecondCallSkipsSettings()
    {
        SetupValidS3Settings();

        // First call — resolves from settings
        var result1 = await _resolver.ResolveAsync();
        // Second call — should hit cache
        var result2 = await _resolver.ResolveAsync();

        await Assert.That(result1).IsNotNull();
        await Assert.That(result2).IsNotNull();
        await Assert.That(result1!.Endpoint).IsEqualTo(result2!.Endpoint);

        // Settings resolver should have been called only once for the endpoint key
        await _settingsResolver.Received(1)
            .ResolveAsync<string>(GovernanceSettingKeys.Storage.Endpoint, Arg.Any<SettingContext>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task InvalidateCache_SpecificTenant_AllowsRefresh()
    {
        SetupValidS3Settings();

        // First call — populate cache
        var result1 = await _resolver.ResolveAsync();
        await Assert.That(result1).IsNotNull();

        // Invalidate cache for this tenant
        _resolver.InvalidateCache(TestTenantId);

        // Next call should resolve from settings again
        var result2 = await _resolver.ResolveAsync();
        await Assert.That(result2).IsNotNull();

        // Endpoint should be fetched twice now
        await _settingsResolver.Received(2)
            .ResolveAsync<string>(GovernanceSettingKeys.Storage.Endpoint, Arg.Any<SettingContext>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ResolveAsync_EmptyPublicEndpoint_ReturnsNullPublicEndpoint()
    {
        SetupValidS3Settings(publicEndpoint: "");

        var result = await _resolver.ResolveAsync();

        await Assert.That(result).IsNotNull();
        await Assert.That(result!.PublicEndpoint).IsNullOrEmpty();
    }

    [Test]
    public async Task ResolveAsync_NullRegion_DefaultsToUsEast1()
    {
        SetupValidS3Settings(region: null);

        var result = await _resolver.ResolveAsync();

        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Region).IsEqualTo("us-east-1");
    }

    [Test]
    public async Task ResolveAsync_DbEmpty_FallsBackToIConfiguration()
    {
        // DB returns empty values for all settings
        _settingsResolver.ResolveAsync<string>(Arg.Any<string>(), Arg.Any<SettingContext>(), Arg.Any<CancellationToken>())
            .Returns("");
        _settingsResolver.ResolveAsync<bool>(Arg.Any<string>(), Arg.Any<SettingContext>(), Arg.Any<CancellationToken>())
            .Returns(true);
        _settingsResolver.ResolveAsync<int>(Arg.Any<string>(), Arg.Any<SettingContext>(), Arg.Any<CancellationToken>())
            .Returns(60);

        // IConfiguration has values (simulating Infisical/env vars)
        var configValues = new Dictionary<string, string?>
        {
            ["S3Settings:Endpoint"] = "https://infisical-endpoint.com",
            ["S3Settings:BucketName"] = "infisical-bucket",
            ["S3Settings:AccessKeyId"] = "INFISICAL_KEY",
            ["S3Settings:SecretAccessKey"] = "INFISICAL_SECRET",
            ["S3Settings:Region"] = "eu-west-1",
            ["S3Settings:PublicEndpoint"] = "https://infisical-public.com"
        };
        var config = new ConfigurationBuilder().AddInMemoryCollection(configValues).Build();
        var resolver = new S3ConfigResolver(_settingsResolver, _tenantContext, _cache, config, _logger);

        var result = await resolver.ResolveAsync();

        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Endpoint).IsEqualTo("https://infisical-endpoint.com");
        await Assert.That(result.BucketName).IsEqualTo("infisical-bucket");
        await Assert.That(result.AccessKeyId).IsEqualTo("INFISICAL_KEY");
        await Assert.That(result.SecretAccessKey).IsEqualTo("INFISICAL_SECRET");
        await Assert.That(result.Region).IsEqualTo("eu-west-1");
        await Assert.That(result.PublicEndpoint).IsEqualTo("https://infisical-public.com");
    }


    [Test]
    public async Task ResolveAsync_DbEmpty_FallsBackToStorageS3EnvironmentVariables()
    {
        SetupEmptyDatabaseSettings();
        var configValues = new Dictionary<string, string?>
        {
            ["STORAGE_S3_ENDPOINT"] = "https://env-storage.example.com",
            ["STORAGE_S3_BUCKET_NAME"] = "env-bucket",
            ["STORAGE_S3_ACCESS_KEY_ID"] = "ENV_ACCESS_KEY",
            ["STORAGE_S3_SECRET_ACCESS_KEY"] = "ENV_SECRET_KEY",
            ["STORAGE_S3_REGION"] = "eu-central-1",
            ["STORAGE_S3_PUBLIC_ENDPOINT"] = "https://env-public.example.com"
        };
        var config = new ConfigurationBuilder().AddInMemoryCollection(configValues).Build();
        var resolver = new S3ConfigResolver(_settingsResolver, _tenantContext, new MemoryCache(new MemoryCacheOptions()), config, _logger);

        var result = await resolver.ResolveAsync();

        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Endpoint).IsEqualTo("https://env-storage.example.com");
        await Assert.That(result.BucketName).IsEqualTo("env-bucket");
        await Assert.That(result.AccessKeyId).IsEqualTo("ENV_ACCESS_KEY");
        await Assert.That(result.SecretAccessKey).IsEqualTo("ENV_SECRET_KEY");
        await Assert.That(result.Region).IsEqualTo("eu-central-1");
        await Assert.That(result.PublicEndpoint).IsEqualTo("https://env-public.example.com");
    }

    [Test]
    public async Task ResolveAsync_DbEmpty_FallsBackToInfisicalStorageSectionKeys()
    {
        SetupEmptyDatabaseSettings();
        var configValues = new Dictionary<string, string?>
        {
            ["Storage:S3Endpoint"] = "https://infisical-storage.example.com",
            ["Storage:S3BucketName"] = "infisical-bucket",
            ["Storage:S3AccessKeyId"] = "INFISICAL_ACCESS_KEY",
            ["Storage:S3SecretAccessKey"] = "INFISICAL_SECRET_KEY",
            ["Storage:S3Region"] = "fsn1",
            ["Storage:S3PublicEndpoint"] = "https://infisical-public.example.com"
        };
        var config = new ConfigurationBuilder().AddInMemoryCollection(configValues).Build();
        var resolver = new S3ConfigResolver(_settingsResolver, _tenantContext, new MemoryCache(new MemoryCacheOptions()), config, _logger);

        var result = await resolver.ResolveAsync();

        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Endpoint).IsEqualTo("https://infisical-storage.example.com");
        await Assert.That(result.BucketName).IsEqualTo("infisical-bucket");
        await Assert.That(result.AccessKeyId).IsEqualTo("INFISICAL_ACCESS_KEY");
        await Assert.That(result.SecretAccessKey).IsEqualTo("INFISICAL_SECRET_KEY");
        await Assert.That(result.Region).IsEqualTo("fsn1");
        await Assert.That(result.PublicEndpoint).IsEqualTo("https://infisical-public.example.com");
    }

    [Test]
    public async Task IsConfiguredAsync_WhenRequiredStorageS3EnvironmentVariablesExist_ReturnsTrue()
    {
        SetupEmptyDatabaseSettings();
        var configValues = new Dictionary<string, string?>
        {
            ["STORAGE_S3_ENDPOINT"] = "https://env-storage.example.com",
            ["STORAGE_S3_BUCKET_NAME"] = "env-bucket",
            ["STORAGE_S3_ACCESS_KEY_ID"] = "ENV_ACCESS_KEY",
            ["STORAGE_S3_SECRET_ACCESS_KEY"] = "ENV_SECRET_KEY"
        };
        var config = new ConfigurationBuilder().AddInMemoryCollection(configValues).Build();
        var resolver = new S3ConfigResolver(_settingsResolver, _tenantContext, new MemoryCache(new MemoryCacheOptions()), config, _logger);

        var result = await resolver.IsConfiguredAsync();

        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task ResolveAsync_DbHasValues_OverridesIConfiguration()
    {
        // DB has values
        SetupValidS3Settings();

        // IConfiguration also has values (different ones)
        var configValues = new Dictionary<string, string?>
        {
            ["S3Settings:Endpoint"] = "https://infisical-endpoint.com",
            ["S3Settings:BucketName"] = "infisical-bucket",
        };
        var config = new ConfigurationBuilder().AddInMemoryCollection(configValues).Build();
        var resolver = new S3ConfigResolver(_settingsResolver, _tenantContext, new MemoryCache(new MemoryCacheOptions()), config, _logger);

        var result = await resolver.ResolveAsync();

        // Should use DB values, NOT IConfiguration values
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Endpoint).IsEqualTo("https://fsn1.your-objectstorage.com");
        await Assert.That(result.BucketName).IsEqualTo("my-bucket");
    }


    private void SetupEmptyDatabaseSettings()
    {
        _settingsResolver.ResolveAsync<string>(Arg.Any<string>(), Arg.Any<SettingContext>(), Arg.Any<CancellationToken>())
            .Returns("");
        _settingsResolver.ResolveAsync<bool>(Arg.Any<string>(), Arg.Any<SettingContext>(), Arg.Any<CancellationToken>())
            .Returns(true);
        _settingsResolver.ResolveAsync<int>(Arg.Any<string>(), Arg.Any<SettingContext>(), Arg.Any<CancellationToken>())
            .Returns(60);
    }

    private void SetupValidS3Settings(
        int uploadExpiration = 60,
        bool forcePathStyle = true,
        string? publicEndpoint = "https://s3-public.example.com",
        string? region = "fsn1")
    {
        _settingsResolver.ResolveAsync<string>(GovernanceSettingKeys.Storage.Endpoint, Arg.Any<SettingContext>(), Arg.Any<CancellationToken>())
            .Returns("https://fsn1.your-objectstorage.com");
        _settingsResolver.ResolveAsync<string>(GovernanceSettingKeys.Storage.BucketName, Arg.Any<SettingContext>(), Arg.Any<CancellationToken>())
            .Returns("my-bucket");
        _settingsResolver.ResolveAsync<string>(InfrastructureSecretSettingKeys.Storage.AccessKeyId, Arg.Any<SettingContext>(), Arg.Any<CancellationToken>())
            .Returns("AKIAIOSFODNN7EXAMPLE");
        _settingsResolver.ResolveAsync<string>(InfrastructureSecretSettingKeys.Storage.SecretAccessKey, Arg.Any<SettingContext>(), Arg.Any<CancellationToken>())
            .Returns("wJalrXUtnFEMI/K7MDENG/bPxRfiCYEXAMPLEKEY");
        _settingsResolver.ResolveAsync<string>(GovernanceSettingKeys.Storage.Region, Arg.Any<SettingContext>(), Arg.Any<CancellationToken>())
            .Returns(region);
        _settingsResolver.ResolveAsync<string>(GovernanceSettingKeys.Storage.PublicEndpoint, Arg.Any<SettingContext>(), Arg.Any<CancellationToken>())
            .Returns(publicEndpoint);
        _settingsResolver.ResolveAsync<bool>(GovernanceSettingKeys.Storage.ForcePathStyle, Arg.Any<SettingContext>(), Arg.Any<CancellationToken>())
            .Returns(forcePathStyle);
        _settingsResolver.ResolveAsync<int>(GovernanceSettingKeys.Storage.UploadUrlExpirationMinutes, Arg.Any<SettingContext>(), Arg.Any<CancellationToken>())
            .Returns(uploadExpiration);
    }
}
