// ABOUTME: Unit tests for UpdateTenantFooterSettingsCommandHandler governance-aware persistence.
// ABOUTME: Covers tenant-scoped footer writes, lock-based skips, and settings cache invalidation.

using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Features.Footer.Handlers.Commands;
using Explore.Application.Features.Footer.Requests.Commands;
using Explore.Application.Settings;
using Explore.Application.Settings.Groups;
using Explore.Domain.Constants;
using Explore.Domain.Settings;
using NSubstitute;

namespace Event.Application.UnitTests.Features.Footer.Commands;

public sealed class UpdateTenantFooterSettingsCommandHandlerTests
{
    private const string Template = "community";
    private const string DescriptionText = "Community events and local services.";
    private const string SocialLinksJson = "[{\"platform\":\"github\",\"url\":\"https://github.com/islamu\"}]";
    private const string CopyrightText = "© 2026 ISLAMU";

    private readonly IHierarchicalSettingsResolver _settingsResolver;
    private readonly ITenantContext _tenantContext;
    private readonly UpdateTenantFooterSettingsCommandHandler _handler;

    public UpdateTenantFooterSettingsCommandHandlerTests()
    {
        _settingsResolver = Substitute.For<IHierarchicalSettingsResolver>();
        _tenantContext = Substitute.For<ITenantContext>();
        _tenantContext.TenantId.Returns(Guid.NewGuid());

        _settingsResolver.ResolveGroupAsync<FooterSettingGroup>(
                Arg.Any<SettingContext>(),
                Arg.Any<CancellationToken>())
            .Returns(new FooterSettingGroup());

        _handler = new UpdateTenantFooterSettingsCommandHandler(_settingsResolver, _tenantContext);
    }

    [Test]
    public async Task Handle_WhenFooterSettingsAreUnlocked_PersistsAllProvidedTenantSettings()
    {
        var tenantId = Guid.NewGuid();
        _tenantContext.TenantId.Returns(tenantId);
        var command = CreateFullCommand();

        var result = await _handler.Handle(command, CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await _settingsResolver.Received(8).SetValueAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            SettingScope.Tenant,
            tenantId,
            command.UserId,
            Arg.Any<CancellationToken>());
        await ReceivedSetValue(GovernanceSettingKeys.Footer.Enabled, SettingValueSerializer.Serialize(false), tenantId, command.UserId);
        await ReceivedSetValue(GovernanceSettingKeys.Footer.Template, SettingValueSerializer.Serialize(Template), tenantId, command.UserId);
        await ReceivedSetValue(GovernanceSettingKeys.Footer.ShowDescription, SettingValueSerializer.Serialize(true), tenantId, command.UserId);
        await ReceivedSetValue(GovernanceSettingKeys.Footer.DescriptionText, SettingValueSerializer.Serialize(DescriptionText), tenantId, command.UserId);
        await ReceivedSetValue(GovernanceSettingKeys.Footer.ShowSocialLinks, SettingValueSerializer.Serialize(true), tenantId, command.UserId);
        await ReceivedSetValue(GovernanceSettingKeys.Footer.SocialLinks, SocialLinksJson, tenantId, command.UserId);
        await ReceivedSetValue(GovernanceSettingKeys.Footer.CopyrightText, SettingValueSerializer.Serialize(CopyrightText), tenantId, command.UserId);
        await ReceivedSetValue(GovernanceSettingKeys.Footer.ShowCookieSettingsLink, SettingValueSerializer.Serialize(false), tenantId, command.UserId);
        _settingsResolver.Received(1).InvalidateCache(SettingScope.Tenant, tenantId);
    }

    [Test]
    public async Task Handle_WhenGovernanceLocksAreEnabled_SkipsLockedTenantOverridesButPersistsUnlockedSettings()
    {
        var tenantId = Guid.NewGuid();
        _tenantContext.TenantId.Returns(tenantId);
        SetupFooterLocks(
            lockTemplate: true,
            lockDescription: true,
            lockSocialLinks: true,
            lockCopyright: true);
        var command = CreateFullCommand();

        var result = await _handler.Handle(command, CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await _settingsResolver.Received(2).SetValueAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            SettingScope.Tenant,
            tenantId,
            command.UserId,
            Arg.Any<CancellationToken>());
        await ReceivedSetValue(GovernanceSettingKeys.Footer.Enabled, SettingValueSerializer.Serialize(false), tenantId, command.UserId);
        await ReceivedSetValue(GovernanceSettingKeys.Footer.ShowCookieSettingsLink, SettingValueSerializer.Serialize(false), tenantId, command.UserId);
        await DidNotReceiveSetValue(GovernanceSettingKeys.Footer.Template);
        await DidNotReceiveSetValue(GovernanceSettingKeys.Footer.ShowDescription);
        await DidNotReceiveSetValue(GovernanceSettingKeys.Footer.DescriptionText);
        await DidNotReceiveSetValue(GovernanceSettingKeys.Footer.ShowSocialLinks);
        await DidNotReceiveSetValue(GovernanceSettingKeys.Footer.SocialLinks);
        await DidNotReceiveSetValue(GovernanceSettingKeys.Footer.CopyrightText);
        _settingsResolver.Received(1).InvalidateCache(SettingScope.Tenant, tenantId);
    }

    [Test]
    public async Task Handle_WhenOptionalSettingsAreNull_SkipsNullSettingsAndInvalidatesTenantCache()
    {
        var tenantId = Guid.NewGuid();
        _tenantContext.TenantId.Returns(tenantId);
        var command = new UpdateTenantFooterSettingsCommand
        {
            UserId = Guid.NewGuid(),
            Enabled = true,
            ShowCookieSettingsLink = true
        };

        var result = await _handler.Handle(command, CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await _settingsResolver.Received(2).SetValueAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            SettingScope.Tenant,
            tenantId,
            command.UserId,
            Arg.Any<CancellationToken>());
        await ReceivedSetValue(GovernanceSettingKeys.Footer.Enabled, SettingValueSerializer.Serialize(true), tenantId, command.UserId);
        await ReceivedSetValue(GovernanceSettingKeys.Footer.ShowCookieSettingsLink, SettingValueSerializer.Serialize(true), tenantId, command.UserId);
        await DidNotReceiveSetValue(GovernanceSettingKeys.Footer.Template);
        await DidNotReceiveSetValue(GovernanceSettingKeys.Footer.DescriptionText);
        await DidNotReceiveSetValue(GovernanceSettingKeys.Footer.SocialLinks);
        await DidNotReceiveSetValue(GovernanceSettingKeys.Footer.CopyrightText);
        _settingsResolver.Received(1).InvalidateCache(SettingScope.Tenant, tenantId);
    }

    private void SetupFooterLocks(
        bool lockTemplate = false,
        bool lockDescription = false,
        bool lockSocialLinks = false,
        bool lockCopyright = false)
    {
        var settings = new Dictionary<string, ResolvedSetting>
        {
            [GovernanceSettingKeys.Footer.LockTenantTemplate] = CreateResolvedSetting(lockTemplate),
            [GovernanceSettingKeys.Footer.LockTenantDescription] = CreateResolvedSetting(lockDescription),
            [GovernanceSettingKeys.Footer.LockTenantSocialLinks] = CreateResolvedSetting(lockSocialLinks),
            [GovernanceSettingKeys.Footer.LockTenantCopyright] = CreateResolvedSetting(lockCopyright)
        };
        var group = new FooterSettingGroup();
        group.Populate(settings);

        _settingsResolver.ResolveGroupAsync<FooterSettingGroup>(
                Arg.Any<SettingContext>(),
                Arg.Any<CancellationToken>())
            .Returns(group);
    }

    private async Task ReceivedSetValue(string key, string value, Guid tenantId, Guid userId)
        => await _settingsResolver.Received(1).SetValueAsync(
            key,
            value,
            SettingScope.Tenant,
            tenantId,
            userId,
            Arg.Any<CancellationToken>());

    private async Task DidNotReceiveSetValue(string key)
        => await _settingsResolver.DidNotReceive().SetValueAsync(
            key,
            Arg.Any<string>(),
            Arg.Any<SettingScope>(),
            Arg.Any<Guid>(),
            Arg.Any<Guid>(),
            Arg.Any<CancellationToken>());

    private static ResolvedSetting CreateResolvedSetting(bool value) => new()
    {
        Value = SettingValueSerializer.Serialize(value)
    };

    private static UpdateTenantFooterSettingsCommand CreateFullCommand() => new()
    {
        UserId = Guid.NewGuid(),
        Enabled = false,
        Template = Template,
        ShowDescription = true,
        DescriptionText = DescriptionText,
        ShowSocialLinks = true,
        SocialLinksJson = SocialLinksJson,
        CopyrightText = CopyrightText,
        ShowCookieSettingsLink = false
    };
}
