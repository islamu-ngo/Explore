// ABOUTME: Unit tests for full and leaf-specific tenant module capability synchronization.
// ABOUTME: Proves PATCH updates only the requested Islamic or Tech capability and forwards cancellation.

using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Services;
using Explore.Application.Services;
using Explore.Domain.Modules;
using NSubstitute;

namespace Event.Application.UnitTests.Services;

public sealed class ModuleCapabilityServiceTests
{
    private readonly ITenantCapabilityRepository _tenantCapabilities = Substitute.For<ITenantCapabilityRepository>();
    private readonly IModuleDefinitionRepository _moduleDefinitions = Substitute.For<IModuleDefinitionRepository>();
    private readonly IModuleCapabilityService _service;

    public ModuleCapabilityServiceTests()
    {
        _tenantCapabilities.Create(Arg.Any<TenantCapability>(), Arg.Any<CancellationToken>())
            .Returns(call => Task.FromResult(call.Arg<TenantCapability>()));
        _service = new ModuleCapabilityService(_tenantCapabilities, _moduleDefinitions);
    }

    [Test]
    public async Task SyncTenantModuleCapabilityPatchAsync_WhenIslamicLeafIsProvided_CreatesOnlyIslamicCapability()
    {
        var tenantId = Guid.CreateVersion7();
        var actorUserId = Guid.CreateVersion7();
        using var cancellation = new CancellationTokenSource();
        _moduleDefinitions.GetByKey("Mod_Islamic", cancellation.Token)
            .Returns(CreateModule("Mod_Islamic"));
        _tenantCapabilities.GetByTenantAndModuleKey(tenantId, "Mod_Islamic", cancellation.Token)
            .Returns((TenantCapability?)null);

        await _service.SyncTenantModuleCapabilityPatchAsync(
            tenantId,
            enableIslamic: false,
            enableTech: null,
            actorUserId,
            cancellation.Token);

        await _moduleDefinitions.Received(1).GetByKey("Mod_Islamic", cancellation.Token);
        await _moduleDefinitions.DidNotReceive().GetByKey("Mod_Core", Arg.Any<CancellationToken>());
        await _moduleDefinitions.DidNotReceive().GetByKey("Mod_Tech", Arg.Any<CancellationToken>());
        await _tenantCapabilities.Received(1).Create(
            Arg.Is<TenantCapability>(capability => capability.TenantId == tenantId
                && capability.ModuleId != Guid.Empty
                && !capability.IsEnabled
                && capability.EnabledBy == actorUserId),
            cancellation.Token);
        await _tenantCapabilities.DidNotReceive().Update(
            Arg.Any<TenantCapability>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task SyncTenantModuleCapabilityPatchAsync_WhenTechLeafIsProvided_UpdatesOnlyTechCapability()
    {
        var tenantId = Guid.CreateVersion7();
        var actorUserId = Guid.CreateVersion7();
        using var cancellation = new CancellationTokenSource();
        var capability = new TenantCapability
        {
            TenantId = tenantId,
            Tenant = null!,
            ModuleId = Guid.CreateVersion7(),
            Module = null!,
            IsEnabled = false
        };
        _moduleDefinitions.GetByKey("Mod_Tech", cancellation.Token)
            .Returns(CreateModule("Mod_Tech"));
        _tenantCapabilities.GetByTenantAndModuleKey(tenantId, "Mod_Tech", cancellation.Token)
            .Returns(capability);

        await _service.SyncTenantModuleCapabilityPatchAsync(
            tenantId,
            enableIslamic: null,
            enableTech: true,
            actorUserId,
            cancellation.Token);

        await _moduleDefinitions.Received(1).GetByKey("Mod_Tech", cancellation.Token);
        await _moduleDefinitions.DidNotReceive().GetByKey("Mod_Core", Arg.Any<CancellationToken>());
        await _moduleDefinitions.DidNotReceive().GetByKey("Mod_Islamic", Arg.Any<CancellationToken>());
        await _tenantCapabilities.Received(1).Update(
            Arg.Is<TenantCapability>(updated => updated == capability
                && updated.IsEnabled
                && updated.EnabledBy == actorUserId),
            cancellation.Token);
        await _tenantCapabilities.DidNotReceive().Create(
            Arg.Any<TenantCapability>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task SyncTenantModuleCapabilitiesAsync_ProvisionsCoreIslamicAndTechCapabilities()
    {
        var tenantId = Guid.CreateVersion7();
        using var cancellation = new CancellationTokenSource();
        _moduleDefinitions.GetByKey(Arg.Any<string>(), cancellation.Token)
            .Returns(call => CreateModule(call.Arg<string>()));

        await _service.SyncTenantModuleCapabilitiesAsync(
            tenantId,
            enableIslamic: true,
            enableTech: false,
            actorUserId: null,
            cancellation.Token);

        await _moduleDefinitions.Received(1).GetByKey("Mod_Core", cancellation.Token);
        await _moduleDefinitions.Received(1).GetByKey("Mod_Islamic", cancellation.Token);
        await _moduleDefinitions.Received(1).GetByKey("Mod_Tech", cancellation.Token);
    }

    private static ModuleDefinition CreateModule(string moduleKey) => new()
    {
        Id = Guid.CreateVersion7(),
        ModuleKey = moduleKey,
        Name = moduleKey
    };
}
