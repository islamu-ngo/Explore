// ABOUTME: Unit tests for tenant module command handlers.
// ABOUTME: Covers application-layer mutation delegation and audit user resolution.

using Explore.Application.Contracts.Identity;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Features.Modules.Handlers.Commands;
using Explore.Application.Features.Modules.Requests.Commands;
using NSubstitute;

namespace Event.Application.UnitTests.Features.Modules.Commands;

public sealed class TenantModuleCommandHandlerTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid UserId = Guid.NewGuid();

    private readonly IModuleService _moduleService = Substitute.For<IModuleService>();
    private readonly IAdminContext _adminContext = Substitute.For<IAdminContext>();

    [Test]
    public async Task EnableHandler_WhenModuleCanBeEnabled_DelegatesWithResolvedAuditUser()
    {
        _adminContext.ResolveUserIdAsync(Arg.Any<CancellationToken>()).Returns(UserId);
        _moduleService.EnableModuleAsync(TenantId, "Mod_Tech", UserId, Arg.Any<CancellationToken>())
            .Returns(true);
        var handler = new EnableTenantModuleCommandHandler(_moduleService, _adminContext);

        var response = await handler.Handle(new EnableTenantModuleCommand
        {
            TenantId = TenantId,
            ModuleKey = "Mod_Tech"
        }, CancellationToken.None);

        await Assert.That(response.IsSuccess).IsTrue();
        await Assert.That(response.Id).IsEqualTo(TenantId);
        await _moduleService.Received(1).EnableModuleAsync(
            TenantId,
            "Mod_Tech",
            UserId,
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task EnableHandler_WhenModuleIsUnknown_ReturnsCommandFailure()
    {
        _adminContext.ResolveUserIdAsync(Arg.Any<CancellationToken>()).Returns(UserId);
        _moduleService.EnableModuleAsync(TenantId, "Mod_Unknown", UserId, Arg.Any<CancellationToken>())
            .Returns(false);
        var handler = new EnableTenantModuleCommandHandler(_moduleService, _adminContext);

        var response = await handler.Handle(new EnableTenantModuleCommand
        {
            TenantId = TenantId,
            ModuleKey = "Mod_Unknown"
        }, CancellationToken.None);

        await Assert.That(response.IsSuccess).IsFalse();
        await Assert.That(response.Errors).Contains("Module 'Mod_Unknown' not found or not active.");
    }

    [Test]
    public async Task DisableHandler_WhenModuleCanBeDisabled_DelegatesToModuleService()
    {
        _moduleService.DisableModuleAsync(TenantId, "Mod_Islamic", Arg.Any<CancellationToken>())
            .Returns(true);
        var handler = new DisableTenantModuleCommandHandler(_moduleService);

        var response = await handler.Handle(new DisableTenantModuleCommand
        {
            TenantId = TenantId,
            ModuleKey = "Mod_Islamic"
        }, CancellationToken.None);

        await Assert.That(response.IsSuccess).IsTrue();
        await Assert.That(response.Id).IsEqualTo(TenantId);
        await _moduleService.Received(1).DisableModuleAsync(
            TenantId,
            "Mod_Islamic",
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task DisableHandler_WhenCapabilityIsMissing_ReturnsCommandFailure()
    {
        _moduleService.DisableModuleAsync(TenantId, "Mod_Tech", Arg.Any<CancellationToken>())
            .Returns(false);
        var handler = new DisableTenantModuleCommandHandler(_moduleService);

        var response = await handler.Handle(new DisableTenantModuleCommand
        {
            TenantId = TenantId,
            ModuleKey = "Mod_Tech"
        }, CancellationToken.None);

        await Assert.That(response.IsSuccess).IsFalse();
        await Assert.That(response.Errors).Contains("Module 'Mod_Tech' is not enabled for this tenant.");
    }
}
