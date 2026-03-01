// ABOUTME: Tests for HierarchicalSettingsResolver covering cascade, locks, batch loading, and scope validation.
// ABOUTME: Uses NSubstitute mocks for repository dependencies.

namespace Event.Application.UnitTests.Settings;

using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Settings;
using Explore.Domain;
using Explore.Domain.Settings;
using Explore.Infrastructure.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using NSubstitute;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;

public class HierarchicalSettingsResolverTests : IDisposable
{
    private readonly ISystemSettingRepository _systemRepo;
    private readonly ITenantSettingRepository _tenantRepo;
    private readonly IOrganizationSettingRepository _orgRepo;
    private readonly IGroupSettingRepository _groupRepo;
    private readonly IUserPreferenceRepository _userPrefRepo;
    private readonly MemoryCache _cache;
    private readonly ILogger<HierarchicalSettingsResolver> _logger;
    private readonly HierarchicalSettingsResolver _resolver;

    public HierarchicalSettingsResolverTests()
    {
        _systemRepo = Substitute.For<ISystemSettingRepository>();
        _tenantRepo = Substitute.For<ITenantSettingRepository>();
        _orgRepo = Substitute.For<IOrganizationSettingRepository>();
        _groupRepo = Substitute.For<IGroupSettingRepository>();
        _userPrefRepo = Substitute.For<IUserPreferenceRepository>();
        _cache = new MemoryCache(new MemoryCacheOptions());
        _logger = Substitute.For<ILogger<HierarchicalSettingsResolver>>();
        _resolver = new HierarchicalSettingsResolver(
            _systemRepo, _tenantRepo, _orgRepo, _groupRepo, _userPrefRepo, _cache, _logger);
    }

    public void Dispose()
    {
        _cache.Dispose();
        GC.SuppressFinalize(this);
    }

    // --- Basic resolution ---

    [Test]
    public async Task ResolveAsync_ReturnsSystemDefault_WhenNoTenantContext()
    {
        SetupSystemSettings(new SystemSetting
        {
            SettingKey = "email.smtp_port",
            Value = "587",
            ValueType = SettingValueType.Integer,
            IsLocked = false
        });

        var result = await _resolver.ResolveAsync<int>("email.smtp_port", new SettingContext());
        await Assert.That(result).IsEqualTo(587);
    }

    [Test]
    public async Task ResolveAsync_ReturnsTenantOverride_WhenNotLocked()
    {
        SetupSystemSettings(new SystemSetting
        {
            SettingKey = "email.smtp_port",
            Value = "587",
            ValueType = SettingValueType.Integer,
            IsLocked = false
        });
        SetupTenantSettings(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), new TenantSetting
        {
            TenantId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            Tenant = null!,
            SettingKey = "email.smtp_port",
            Value = "465"
        });

        var context = new SettingContext(TenantId: Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));
        var result = await _resolver.ResolveAsync<int>("email.smtp_port", context);
        await Assert.That(result).IsEqualTo(465);
    }

    [Test]
    public async Task ResolveAsync_ReturnsSystemValue_WhenLocked()
    {
        SetupSystemSettings(new SystemSetting
        {
            SettingKey = "deployment.mode",
            Value = "\"MultiTenant\"",
            ValueType = SettingValueType.String,
            IsLocked = true
        });
        var tenantId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        SetupTenantSettings(tenantId, new TenantSetting
        {
            TenantId = tenantId,
            Tenant = null!,
            SettingKey = "deployment.mode",
            Value = "\"SingleTenant\""
        });

        var context = new SettingContext(TenantId: tenantId);
        var result = await _resolver.ResolveAsync<string>("deployment.mode", context);
        await Assert.That(result).IsEqualTo("MultiTenant");
    }

    [Test]
    public async Task ResolveAsync_ReturnsNull_WhenKeyNotFound()
    {
        _systemRepo.GetAllSettings(Arg.Any<string?>()).Returns(new List<SystemSetting>());

        var result = await _resolver.ResolveAsync<string>("nonexistent.key", new SettingContext());
        await Assert.That(result).IsNull();
    }

    // --- Batch resolution ---

    [Test]
    public async Task ResolveBatchAsync_ReturnsMultipleSettings()
    {
        SetupSystemSettings(
            new SystemSetting { SettingKey = "email.smtp_host", Value = "\"smtp.test.com\"", ValueType = SettingValueType.String, IsLocked = false },
            new SystemSetting { SettingKey = "email.smtp_port", Value = "587", ValueType = SettingValueType.Integer, IsLocked = false }
        );

        var results = await _resolver.ResolveBatchAsync(
            ["email.smtp_host", "email.smtp_port"],
            new SettingContext());

        await Assert.That(results.Count).IsEqualTo(2);
        await Assert.That(results[0].Key).IsEqualTo("email.smtp_host");
        await Assert.That(results[1].Key).IsEqualTo("email.smtp_port");
    }

    [Test]
    public async Task ResolveBatchAsync_WithEmptyKeys_ReturnsEmpty()
    {
        var results = await _resolver.ResolveBatchAsync([], new SettingContext());
        await Assert.That(results.Count).IsEqualTo(0);
    }

    // --- Metadata ---

    [Test]
    public async Task ResolveWithMetadata_ReturnsSourceSystemDefault()
    {
        SetupSystemSettings(new SystemSetting
        {
            SettingKey = "branding.display_name",
            Value = "\"ISLAMU\"",
            ValueType = SettingValueType.String,
            IsLocked = false,
            Description = "Brand name",
            Category = "Branding"
        });

        var result = await _resolver.ResolveWithMetadataAsync("branding.display_name", new SettingContext());
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Source).IsEqualTo(SettingSource.SystemDefault);
        await Assert.That(result.Category).IsEqualTo("Branding");
    }

    [Test]
    public async Task ResolveWithMetadata_ReturnsSourceSystemLocked_WhenLocked()
    {
        SetupSystemSettings(new SystemSetting
        {
            SettingKey = "deployment.mode",
            Value = "\"MultiTenant\"",
            ValueType = SettingValueType.String,
            IsLocked = true
        });

        var result = await _resolver.ResolveWithMetadataAsync("deployment.mode", new SettingContext());
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Source).IsEqualTo(SettingSource.SystemLocked);
        await Assert.That(result.IsLocked).IsTrue();
    }

    [Test]
    public async Task ResolveWithMetadata_ReturnsSourceTenantOverride()
    {
        SetupSystemSettings(new SystemSetting
        {
            SettingKey = "branding.display_name",
            Value = "\"Default\"",
            ValueType = SettingValueType.String,
            IsLocked = false
        });
        var tenantId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        SetupTenantSettings(tenantId, new TenantSetting
        {
            TenantId = tenantId,
            Tenant = null!,
            SettingKey = "branding.display_name",
            Value = "\"Tenant Brand\""
        });

        var result = await _resolver.ResolveWithMetadataAsync(
            "branding.display_name",
            new SettingContext(TenantId: tenantId));

        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Source).IsEqualTo(SettingSource.TenantOverride);
        await Assert.That(result.Value).IsEqualTo("\"Tenant Brand\"");
    }

    // --- SetValue scope validation ---

    [Test]
    public async Task SetValueAsync_ThrowsWhenScopeOutOfRange()
    {
        // deployment.mode is Instance-only (MinScope=Instance, MaxScope=Instance)
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await _resolver.SetValueAsync(
                "deployment.mode", "\"SingleTenant\"",
                SettingScope.Tenant, Guid.NewGuid(), Guid.NewGuid()));

        await Assert.That(ex.Message).Contains("cannot be set at scope");
    }

    [Test]
    public async Task SetValueAsync_SucceedsAtInstanceScope()
    {
        _systemRepo.GetByKey("email.smtp_port").Returns((SystemSetting?)null);

        await _resolver.SetValueAsync(
            "email.smtp_port", "465",
            SettingScope.Instance, Guid.Empty, Guid.NewGuid());

        await _systemRepo.Received(1).Create(Arg.Is<SystemSetting>(s =>
            s.SettingKey == "email.smtp_port" && s.Value == "465"));
    }

    [Test]
    public async Task SetValueAsync_SucceedsAtTenantScope()
    {
        var tenantId = Guid.NewGuid();
        _tenantRepo.GetByTenantAndKey(tenantId, "email.smtp_port").Returns((TenantSetting?)null);

        await _resolver.SetValueAsync(
            "email.smtp_port", "465",
            SettingScope.Tenant, tenantId, Guid.NewGuid());

        await _tenantRepo.Received(1).Create(Arg.Is<TenantSetting>(s =>
            s.SettingKey == "email.smtp_port" && s.Value == "465" && s.TenantId == tenantId));
    }

    // --- Lock ---

    [Test]
    public async Task LockAsync_SetsIsLockedToTrue()
    {
        var setting = new SystemSetting
        {
            SettingKey = "email.smtp_port",
            Value = "587",
            ValueType = SettingValueType.Integer,
            IsLocked = false
        };
        _systemRepo.GetByKey("email.smtp_port").Returns(setting);

        await _resolver.LockAsync("email.smtp_port", SettingScope.Instance, Guid.Empty, Guid.NewGuid());

        await Assert.That(setting.IsLocked).IsTrue();
        await _systemRepo.Received(1).Update(setting);
    }

    [Test]
    public async Task LockAsync_ThrowsForNonInstanceScope()
    {
        await Assert.ThrowsAsync<NotSupportedException>(async () =>
            await _resolver.LockAsync("email.smtp_port", SettingScope.Tenant, Guid.NewGuid(), Guid.NewGuid()));
    }

    // --- Cache invalidation ---

    [Test]
    public async Task InvalidateCache_RemovesSystemCache()
    {
        SetupSystemSettings(new SystemSetting
        {
            SettingKey = "email.smtp_port",
            Value = "587",
            ValueType = SettingValueType.Integer,
            IsLocked = false
        });

        // Prime the cache
        await _resolver.ResolveAsync<int>("email.smtp_port", new SettingContext());

        // Invalidate
        _resolver.InvalidateCache(SettingScope.Instance);

        // Next call should hit the repo again
        _systemRepo.ClearReceivedCalls();
        await _resolver.ResolveAsync<int>("email.smtp_port", new SettingContext());
        await _systemRepo.Received(1).GetAllSettings(Arg.Any<string?>());
    }

    // --- Organization scope ---

    [Test]
    public async Task ResolveAsync_ReturnsOrgOverride_WhenNotLocked()
    {
        SetupSystemSettings(new SystemSetting
        {
            SettingKey = "branding.display_name",
            Value = "\"Platform\"",
            ValueType = SettingValueType.String,
            IsLocked = false
        });
        var tenantId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var orgId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        SetupTenantSettings(tenantId, new TenantSetting
        {
            TenantId = tenantId,
            Tenant = null!,
            SettingKey = "branding.display_name",
            Value = "\"Tenant Brand\""
        });
        SetupOrgSettings(orgId, new OrganizationSetting
        {
            OrganizationId = orgId,
            Organization = null!,
            Tenant = null!,
            SettingKey = "branding.display_name",
            Value = "\"Org Brand\""
        });

        var context = new SettingContext(TenantId: tenantId, OrganizationId: orgId);
        var result = await _resolver.ResolveWithMetadataAsync("branding.display_name", context);
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Source).IsEqualTo(SettingSource.OrganizationOverride);
        await Assert.That(result.Value).IsEqualTo("\"Org Brand\"");
    }

    // --- Group scope ---

    [Test]
    public async Task ResolveAsync_ReturnsGroupOverride_WhenNotLocked()
    {
        SetupSystemSettings(new SystemSetting
        {
            SettingKey = "branding.display_name",
            Value = "\"Platform\"",
            ValueType = SettingValueType.String,
            IsLocked = false
        });
        var tenantId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var groupId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
        SetupGroupSettings(groupId, new GroupSetting
        {
            GroupId = groupId,
            Group = null!,
            Tenant = null!,
            SettingKey = "branding.display_name",
            Value = "\"Group Brand\""
        });

        var context = new SettingContext(TenantId: tenantId, GroupId: groupId);
        var result = await _resolver.ResolveWithMetadataAsync("branding.display_name", context);
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Source).IsEqualTo(SettingSource.GroupOverride);
    }

    // --- SetValue at organization/group scopes ---

    [Test]
    public async Task SetValueAsync_SucceedsAtOrganizationScope()
    {
        var orgId = Guid.NewGuid();
        _orgRepo.GetByOrganizationAndKey(orgId, "custom.org_name").Returns((OrganizationSetting?)null);

        await _resolver.SetValueAsync(
            "custom.org_name", "\"Org Brand\"",
            SettingScope.Organization, orgId, Guid.NewGuid());

        await _orgRepo.Received(1).Create(Arg.Is<OrganizationSetting>(s =>
            s.SettingKey == "custom.org_name" && s.OrganizationId == orgId));
    }

    [Test]
    public async Task SetValueAsync_SucceedsAtGroupScope()
    {
        var groupId = Guid.NewGuid();
        _groupRepo.GetByGroupAndKey(groupId, "custom.group_name").Returns((GroupSetting?)null);

        await _resolver.SetValueAsync(
            "custom.group_name", "\"Group Brand\"",
            SettingScope.Group, groupId, Guid.NewGuid());

        await _groupRepo.Received(1).Create(Arg.Is<GroupSetting>(s =>
            s.SettingKey == "custom.group_name" && s.GroupId == groupId));
    }

    // --- Helpers ---

    private void SetupSystemSettings(params SystemSetting[] settings)
    {
        _systemRepo.GetAllSettings(Arg.Any<string?>()).Returns(settings.ToList());
    }

    private void SetupTenantSettings(Guid tenantId, params TenantSetting[] settings)
    {
        _tenantRepo.GetAllForTenant(tenantId).Returns(settings.ToList());
    }

    private void SetupOrgSettings(Guid orgId, params OrganizationSetting[] settings)
    {
        _orgRepo.GetAllForOrganization(orgId).Returns(settings.ToList());
    }

    private void SetupGroupSettings(Guid groupId, params GroupSetting[] settings)
    {
        _groupRepo.GetAllForGroup(groupId).Returns(settings.ToList());
    }

    private void SetupUserPreferences(Guid tenantId, Guid userId, params UserPreference[] prefs)
    {
        _userPrefRepo.GetAllForUser(tenantId, userId).Returns(prefs.ToList());
    }
}
