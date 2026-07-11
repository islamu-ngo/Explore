// ABOUTME: Unit tests for SmtpConfigResolver verifying hierarchical settings resolution,
// caching behavior, and null handling when SMTP is not configured.

using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Models;
using Explore.Application.Settings;
using Explore.Domain.Constants;
using Explore.Infrastructure.Mail;
using Explore.Infrastructure.Tests.Fixtures;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.Core;

namespace Explore.Infrastructure.Tests.Infrastructure;

[Category(InfrastructureTestCategories.Email)]
public class SmtpConfigResolverTests : IDisposable
{
    private readonly IHierarchicalSettingsResolver _settingsResolver;
    private readonly ITenantContext _tenantContext;
    private readonly MemoryCache _cache;
    private readonly ILogger<SmtpConfigResolver> _logger;
    private readonly SmtpConfigResolver _resolver;

    private static readonly Guid TestTenantId = Guid.NewGuid();

    public SmtpConfigResolverTests()
    {
        _settingsResolver = Substitute.For<IHierarchicalSettingsResolver>();
        _tenantContext = Substitute.For<ITenantContext>();
        _cache = new MemoryCache(new MemoryCacheOptions());
        _logger = Substitute.For<ILogger<SmtpConfigResolver>>();

        _tenantContext.TenantId.Returns(TestTenantId);

        _resolver = new SmtpConfigResolver(_settingsResolver, _tenantContext, _cache, _logger);
    }

    public void Dispose()
    {
        _cache.Dispose();
    }

    [Test]
    public async Task ResolveAsync_EmptyHost_ReturnsNull()
    {
        _settingsResolver.ResolveAsync<string>(GovernanceSettingKeys.Email.SmtpHost, Arg.Any<SettingContext>(), Arg.Any<CancellationToken>())
            .Returns("");

        var result = await _resolver.ResolveAsync();

        await Assert.That(result).IsNull();
    }

    [Test]
    public async Task ResolveAsync_NullHost_ReturnsNull()
    {
        _settingsResolver.ResolveAsync<string>(GovernanceSettingKeys.Email.SmtpHost, Arg.Any<SettingContext>(), Arg.Any<CancellationToken>())
            .Returns((string?)null);

        var result = await _resolver.ResolveAsync();

        await Assert.That(result).IsNull();
    }

    [Test]
    public async Task ResolveAsync_HostSetButEmptyFromAddress_ReturnsNull()
    {
        _settingsResolver.ResolveAsync<string>(GovernanceSettingKeys.Email.SmtpHost, Arg.Any<SettingContext>(), Arg.Any<CancellationToken>())
            .Returns("smtp.example.com");
        _settingsResolver.ResolveAsync<string>(GovernanceSettingKeys.Email.FromAddress, Arg.Any<SettingContext>(), Arg.Any<CancellationToken>())
            .Returns("");

        var result = await _resolver.ResolveAsync();

        await Assert.That(result).IsNull();
    }

    [Test]
    public async Task ResolveAsync_ValidConfig_ReturnsSmtpConfiguration()
    {
        SetupValidSmtpSettings();

        var result = await _resolver.ResolveAsync();

        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Host).IsEqualTo("smtp.example.com");
        await Assert.That(result.Port).IsEqualTo(587);
        await Assert.That(result.FromAddress).IsEqualTo("noreply@example.com");
        await Assert.That(result.FromName).IsEqualTo("Test Platform");
        await Assert.That(result.Username).IsEqualTo("user@example.com");
        await Assert.That(result.Security).IsEqualTo(SmtpSecurityMode.StartTls);
    }

    [Test]
    public async Task ResolveAsync_UsesTenantContextForSettingsCascade()
    {
        SetupValidSmtpSettings();

        var result = await _resolver.ResolveAsync();

        await Assert.That(result).IsNotNull();
        await _settingsResolver.Received(1)
            .ResolveAsync<string>(
                GovernanceSettingKeys.Email.SmtpHost,
                Arg.Is<SettingContext>(ctx => ctx.TenantId == TestTenantId),
                Arg.Any<CancellationToken>());
        await _settingsResolver.Received(1)
            .ResolveAsync<string>(
                InfrastructureSecretSettingKeys.Email.SmtpPassword,
                Arg.Is<SettingContext>(ctx => ctx.TenantId == TestTenantId),
                Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ResolveAsync_WhenTenantChanges_UsesSeparateCacheEntry()
    {
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        var activeTenantId = tenantA;
        _tenantContext.TenantId.Returns(_ => activeTenantId);
        SetupTenantAwareSmtpSettings(
            tenantB,
            systemHost: "smtp.system.example.com",
            tenantOverrideHost: "smtp.tenant.example.com");

        var systemDefaultConfig = await _resolver.ResolveAsync();
        activeTenantId = tenantB;
        var tenantOverrideConfig = await _resolver.ResolveAsync();
        activeTenantId = tenantA;
        var cachedSystemDefaultConfig = await _resolver.ResolveAsync();

        await Assert.That(systemDefaultConfig).IsNotNull();
        await Assert.That(tenantOverrideConfig).IsNotNull();
        await Assert.That(cachedSystemDefaultConfig).IsNotNull();
        await Assert.That(systemDefaultConfig!.Host).IsEqualTo("smtp.system.example.com");
        await Assert.That(tenantOverrideConfig!.Host).IsEqualTo("smtp.tenant.example.com");
        await Assert.That(cachedSystemDefaultConfig!.Host).IsEqualTo("smtp.system.example.com");
        await _settingsResolver.Received(2)
            .ResolveAsync<string>(
                GovernanceSettingKeys.Email.SmtpHost,
                Arg.Any<SettingContext>(),
                Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ResolveAsync_PortZero_DefaultsTo587()
    {
        SetupValidSmtpSettings(port: 0);

        var result = await _resolver.ResolveAsync();

        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Port).IsEqualTo(587);
    }

    [Test]
    public async Task ResolveAsync_TimeoutZero_DefaultsTo30()
    {
        SetupValidSmtpSettings(timeout: 0);

        var result = await _resolver.ResolveAsync();

        await Assert.That(result).IsNotNull();
        await Assert.That(result!.TimeoutSeconds).IsEqualTo(30);
    }

    [Test]
    public async Task ResolveAsync_InvalidSecurityMode_DefaultsToStartTls()
    {
        SetupValidSmtpSettings(security: "InvalidMode");

        var result = await _resolver.ResolveAsync();

        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Security).IsEqualTo(SmtpSecurityMode.StartTls);
    }

    [Test]
    public async Task ResolveAsync_SslOnConnect_ParsesCorrectly()
    {
        SetupValidSmtpSettings(security: "SslOnConnect");

        var result = await _resolver.ResolveAsync();

        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Security).IsEqualTo(SmtpSecurityMode.SslOnConnect);
    }

    [Test]
    public async Task ResolveAsync_CachesResult_SecondCallSkipsSettings()
    {
        SetupValidSmtpSettings();

        // First call — resolves from settings
        var result1 = await _resolver.ResolveAsync();
        // Second call — should hit cache
        var result2 = await _resolver.ResolveAsync();

        await Assert.That(result1).IsNotNull();
        await Assert.That(result2).IsNotNull();
        await Assert.That(result1!.Host).IsEqualTo(result2!.Host);

        // Settings resolver should have been called only once for the host key
        await _settingsResolver.Received(1)
            .ResolveAsync<string>(GovernanceSettingKeys.Email.SmtpHost, Arg.Any<SettingContext>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task InvalidateCache_SpecificTenant_AllowsRefresh()
    {
        SetupValidSmtpSettings();

        // First call — populate cache
        var result1 = await _resolver.ResolveAsync();
        await Assert.That(result1).IsNotNull();

        // Invalidate cache for this tenant
        _resolver.InvalidateCache(TestTenantId);

        // Next call should resolve from settings again
        var result2 = await _resolver.ResolveAsync();
        await Assert.That(result2).IsNotNull();

        // Host should be fetched twice now
        await _settingsResolver.Received(2)
            .ResolveAsync<string>(GovernanceSettingKeys.Email.SmtpHost, Arg.Any<SettingContext>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ResolveAsync_NullFromName_DefaultsToExplore()
    {
        _settingsResolver.ResolveAsync<string>(GovernanceSettingKeys.Email.SmtpHost, Arg.Any<SettingContext>(), Arg.Any<CancellationToken>())
            .Returns("smtp.example.com");
        _settingsResolver.ResolveAsync<string>(GovernanceSettingKeys.Email.FromAddress, Arg.Any<SettingContext>(), Arg.Any<CancellationToken>())
            .Returns("noreply@example.com");
        _settingsResolver.ResolveAsync<int>(GovernanceSettingKeys.Email.SmtpPort, Arg.Any<SettingContext>(), Arg.Any<CancellationToken>())
            .Returns(587);
        _settingsResolver.ResolveAsync<string>(GovernanceSettingKeys.Email.SmtpSecurity, Arg.Any<SettingContext>(), Arg.Any<CancellationToken>())
            .Returns("StartTls");
        _settingsResolver.ResolveAsync<int>(GovernanceSettingKeys.Email.SmtpTimeoutSeconds, Arg.Any<SettingContext>(), Arg.Any<CancellationToken>())
            .Returns(30);
        _settingsResolver.ResolveAsync<string>(GovernanceSettingKeys.Email.FromName, Arg.Any<SettingContext>(), Arg.Any<CancellationToken>())
            .Returns((string?)null);

        var result = await _resolver.ResolveAsync();

        await Assert.That(result).IsNotNull();
        await Assert.That(result!.FromName).IsEqualTo("Explore");
    }

    private void SetupValidSmtpSettings(
        int port = 587,
        int timeout = 30,
        string security = "StartTls")
    {
        _settingsResolver.ResolveAsync<string>(GovernanceSettingKeys.Email.SmtpHost, Arg.Any<SettingContext>(), Arg.Any<CancellationToken>())
            .Returns("smtp.example.com");
        _settingsResolver.ResolveAsync<string>(GovernanceSettingKeys.Email.FromAddress, Arg.Any<SettingContext>(), Arg.Any<CancellationToken>())
            .Returns("noreply@example.com");
        _settingsResolver.ResolveAsync<int>(GovernanceSettingKeys.Email.SmtpPort, Arg.Any<SettingContext>(), Arg.Any<CancellationToken>())
            .Returns(port);
        _settingsResolver.ResolveAsync<string>(GovernanceSettingKeys.Email.SmtpSecurity, Arg.Any<SettingContext>(), Arg.Any<CancellationToken>())
            .Returns(security);
        _settingsResolver.ResolveAsync<int>(GovernanceSettingKeys.Email.SmtpTimeoutSeconds, Arg.Any<SettingContext>(), Arg.Any<CancellationToken>())
            .Returns(timeout);
        _settingsResolver.ResolveAsync<string>(InfrastructureSecretSettingKeys.Email.SmtpUsername, Arg.Any<SettingContext>(), Arg.Any<CancellationToken>())
            .Returns("user@example.com");
        _settingsResolver.ResolveAsync<string>(InfrastructureSecretSettingKeys.Email.SmtpPassword, Arg.Any<SettingContext>(), Arg.Any<CancellationToken>())
            .Returns("secret123");
        _settingsResolver.ResolveAsync<string>(GovernanceSettingKeys.Email.FromName, Arg.Any<SettingContext>(), Arg.Any<CancellationToken>())
            .Returns("Test Platform");
        _settingsResolver.ResolveAsync<bool>(GovernanceSettingKeys.Email.SmtpSkipCertValidation, Arg.Any<SettingContext>(), Arg.Any<CancellationToken>())
            .Returns(false);
    }

    private void SetupTenantAwareSmtpSettings(
        Guid tenantOverrideTenantId,
        string systemHost,
        string tenantOverrideHost)
    {
        _settingsResolver
            .ResolveAsync<string>(
                GovernanceSettingKeys.Email.SmtpHost,
                Arg.Any<SettingContext>(),
                Arg.Any<CancellationToken>())
            .Returns(call => ResolveTenantValue(call, tenantOverrideTenantId, systemHost, tenantOverrideHost));
        _settingsResolver
            .ResolveAsync<string>(
                GovernanceSettingKeys.Email.FromAddress,
                Arg.Any<SettingContext>(),
                Arg.Any<CancellationToken>())
            .Returns("noreply@example.com");
        _settingsResolver
            .ResolveAsync<int>(
                GovernanceSettingKeys.Email.SmtpPort,
                Arg.Any<SettingContext>(),
                Arg.Any<CancellationToken>())
            .Returns(587);
        _settingsResolver
            .ResolveAsync<string>(
                GovernanceSettingKeys.Email.SmtpSecurity,
                Arg.Any<SettingContext>(),
                Arg.Any<CancellationToken>())
            .Returns("StartTls");
        _settingsResolver
            .ResolveAsync<int>(
                GovernanceSettingKeys.Email.SmtpTimeoutSeconds,
                Arg.Any<SettingContext>(),
                Arg.Any<CancellationToken>())
            .Returns(30);
        _settingsResolver
            .ResolveAsync<string>(
                InfrastructureSecretSettingKeys.Email.SmtpUsername,
                Arg.Any<SettingContext>(),
                Arg.Any<CancellationToken>())
            .Returns("user@example.com");
        _settingsResolver
            .ResolveAsync<string>(
                InfrastructureSecretSettingKeys.Email.SmtpPassword,
                Arg.Any<SettingContext>(),
                Arg.Any<CancellationToken>())
            .Returns("secret123");
        _settingsResolver
            .ResolveAsync<string>(
                GovernanceSettingKeys.Email.FromName,
                Arg.Any<SettingContext>(),
                Arg.Any<CancellationToken>())
            .Returns("Test Platform");
        _settingsResolver
            .ResolveAsync<bool>(
                GovernanceSettingKeys.Email.SmtpSkipCertValidation,
                Arg.Any<SettingContext>(),
                Arg.Any<CancellationToken>())
            .Returns(false);
    }

    private static string ResolveTenantValue(
        CallInfo call,
        Guid tenantOverrideTenantId,
        string systemValue,
        string tenantOverrideValue)
    {
        var context = call.ArgAt<SettingContext>(1);
        return context.TenantId == tenantOverrideTenantId
            ? tenantOverrideValue
            : systemValue;
    }
}
