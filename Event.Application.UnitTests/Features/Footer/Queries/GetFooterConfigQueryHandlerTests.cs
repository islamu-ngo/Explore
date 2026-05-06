// ABOUTME: Unit tests for GetFooterConfigQueryHandler public footer configuration behavior.
// ABOUTME: Covers tenant-scoped setting resolution, link-group mapping, and empty public footer groups.

using AutoMapper;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.Footer;
using Explore.Application.Features.Footer.Handlers.Queries;
using Explore.Application.Features.Footer.Requests.Queries;
using Explore.Application.Settings;
using Explore.Application.Settings.Groups;
using Explore.Domain;
using Explore.Domain.Constants;
using Explore.Domain.Settings;
using NSubstitute;

namespace Event.Application.UnitTests.Features.Footer.Queries;

public sealed class GetFooterConfigQueryHandlerTests
{
    private readonly IHierarchicalSettingsResolver _settingsResolver;
    private readonly IFooterLinkGroupRepository _footerLinkGroupRepository;
    private readonly ITenantContext _tenantContext;
    private readonly IMapper _mapper;
    private readonly GetFooterConfigQueryHandler _handler;

    public GetFooterConfigQueryHandlerTests()
    {
        _settingsResolver = Substitute.For<IHierarchicalSettingsResolver>();
        _footerLinkGroupRepository = Substitute.For<IFooterLinkGroupRepository>();
        _tenantContext = Substitute.For<ITenantContext>();
        _mapper = Substitute.For<IMapper>();

        _handler = new GetFooterConfigQueryHandler(
            _settingsResolver,
            _footerLinkGroupRepository,
            _tenantContext,
            _mapper);
    }

    [Test]
    public async Task Handle_WhenFooterSettingsAndLinkGroupsExist_ReturnsResolvedPublicConfig()
    {
        var tenantId = Guid.NewGuid();
        _tenantContext.TenantId.Returns(tenantId);
        var settingGroup = CreateConfiguredFooterSettings();
        var groups = new List<TenantFooterLinkGroup>
        {
            CreateFooterLinkGroup(tenantId, "Community", 1),
            CreateFooterLinkGroup(tenantId, "Legal", 2)
        };
        var expectedGroups = new List<FooterLinkGroupDto>
        {
            CreateLinkGroupDto(groups[0], "Events", "/events"),
            CreateLinkGroupDto(groups[1], "Privacy", "/privacy")
        };
        _settingsResolver.ResolveGroupAsync<FooterSettingGroup>(
                Arg.Is<SettingContext>(context => context.TenantId == tenantId),
                Arg.Any<CancellationToken>())
            .Returns(settingGroup);
        _footerLinkGroupRepository.GetResolvedGroupsForTenantAsync(tenantId, Arg.Any<CancellationToken>())
            .Returns(groups);
        _mapper.Map<List<FooterLinkGroupDto>>(groups).Returns(expectedGroups);

        var result = await _handler.Handle(new GetFooterConfigQuery(), CancellationToken.None);

        await Assert.That(result.Settings.Enabled).IsFalse();
        await Assert.That(result.Settings.Template).IsEqualTo("community");
        await Assert.That(result.Settings.ShowDescription).IsTrue();
        await Assert.That(result.Settings.DescriptionText).IsEqualTo("Community events and local services.");
        await Assert.That(result.Settings.ShowSocialLinks).IsTrue();
        await Assert.That(result.Settings.SocialLinks.Count).IsEqualTo(1);
        await Assert.That(result.Settings.SocialLinks[0].Platform).IsEqualTo("github");
        await Assert.That(result.Settings.CopyrightText).IsEqualTo("© 2026 ISLAMU");
        await Assert.That(result.Settings.ShowCookieSettingsLink).IsFalse();
        await Assert.That(result.LinkGroups.Count).IsEqualTo(2);
        await Assert.That(result.LinkGroups[0].Title).IsEqualTo("Community");
        await Assert.That(result.LinkGroups[1].Links[0].Label).IsEqualTo("Privacy");
        await _settingsResolver.Received(1).ResolveGroupAsync<FooterSettingGroup>(
            Arg.Is<SettingContext>(context => context.TenantId == tenantId),
            Arg.Any<CancellationToken>());
        await _footerLinkGroupRepository.Received(1).GetResolvedGroupsForTenantAsync(tenantId, Arg.Any<CancellationToken>());
        _mapper.Received(1).Map<List<FooterLinkGroupDto>>(groups);
    }

    [Test]
    public async Task Handle_WhenNoLinkGroupsExist_ReturnsEmptyMappedGroupsWithResolvedSettings()
    {
        var tenantId = Guid.NewGuid();
        _tenantContext.TenantId.Returns(tenantId);
        var settingGroup = CreateConfiguredFooterSettings();
        var groups = new List<TenantFooterLinkGroup>();
        var expectedGroups = new List<FooterLinkGroupDto>();
        _settingsResolver.ResolveGroupAsync<FooterSettingGroup>(Arg.Any<SettingContext>(), Arg.Any<CancellationToken>())
            .Returns(settingGroup);
        _footerLinkGroupRepository.GetResolvedGroupsForTenantAsync(tenantId, Arg.Any<CancellationToken>())
            .Returns(groups);
        _mapper.Map<List<FooterLinkGroupDto>>(groups).Returns(expectedGroups);

        var result = await _handler.Handle(new GetFooterConfigQuery(), CancellationToken.None);

        await Assert.That(result.Settings.Template).IsEqualTo("community");
        await Assert.That(result.LinkGroups).IsNotNull();
        await Assert.That(result.LinkGroups.Count).IsEqualTo(0);
        await _footerLinkGroupRepository.Received(1).GetResolvedGroupsForTenantAsync(tenantId, Arg.Any<CancellationToken>());
        _mapper.Received(1).Map<List<FooterLinkGroupDto>>(groups);
    }

    [Test]
    public async Task Handle_UsesTenantContext_ForSettingsAndResolvedGroups()
    {
        var tenantId = Guid.NewGuid();
        var otherTenantId = Guid.NewGuid();
        _tenantContext.TenantId.Returns(tenantId);
        var settingGroup = new FooterSettingGroup();
        var groups = new List<TenantFooterLinkGroup>();
        _settingsResolver.ResolveGroupAsync<FooterSettingGroup>(Arg.Any<SettingContext>(), Arg.Any<CancellationToken>())
            .Returns(settingGroup);
        _footerLinkGroupRepository.GetResolvedGroupsForTenantAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(groups);
        _mapper.Map<List<FooterLinkGroupDto>>(groups).Returns(new List<FooterLinkGroupDto>());

        await _handler.Handle(new GetFooterConfigQuery(), CancellationToken.None);

        await _settingsResolver.Received(1).ResolveGroupAsync<FooterSettingGroup>(
            Arg.Is<SettingContext>(context => context.TenantId == tenantId),
            Arg.Any<CancellationToken>());
        await _footerLinkGroupRepository.Received(1).GetResolvedGroupsForTenantAsync(
            Arg.Is<Guid>(requestedTenantId => requestedTenantId == tenantId),
            Arg.Any<CancellationToken>());
        await _footerLinkGroupRepository.DidNotReceive().GetResolvedGroupsForTenantAsync(
            otherTenantId,
            Arg.Any<CancellationToken>());
    }

    private static FooterSettingGroup CreateConfiguredFooterSettings()
    {
        var group = new FooterSettingGroup();
        group.Populate(new Dictionary<string, ResolvedSetting>
        {
            [GovernanceSettingKeys.Footer.Enabled] = CreateResolvedSetting(false),
            [GovernanceSettingKeys.Footer.Template] = CreateResolvedSetting("community"),
            [GovernanceSettingKeys.Footer.ShowDescription] = CreateResolvedSetting(true),
            [GovernanceSettingKeys.Footer.DescriptionText] = CreateResolvedSetting("Community events and local services."),
            [GovernanceSettingKeys.Footer.ShowSocialLinks] = CreateResolvedSetting(true),
            [GovernanceSettingKeys.Footer.SocialLinks] = new()
            {
                Value = "[{\"platform\":\"github\",\"url\":\"https://github.com/islamu\",\"label\":\"GitHub\"}]"
            },
            [GovernanceSettingKeys.Footer.CopyrightText] = CreateResolvedSetting("© 2026 ISLAMU"),
            [GovernanceSettingKeys.Footer.ShowCookieSettingsLink] = CreateResolvedSetting(false)
        });

        return group;
    }

    private static ResolvedSetting CreateResolvedSetting<T>(T value) => new()
    {
        Value = SettingValueSerializer.Serialize(value)
    };

    private static TenantFooterLinkGroup CreateFooterLinkGroup(Guid tenantId, string title, int order) => new()
    {
        Id = Guid.NewGuid(),
        TenantId = tenantId,
        Title = title,
        Order = order,
        IsActive = true
    };

    private static FooterLinkGroupDto CreateLinkGroupDto(TenantFooterLinkGroup group, string linkLabel, string linkUrl) => new()
    {
        Id = group.Id,
        Title = group.Title,
        Order = group.Order,
        Links =
        [
            new FooterLinkItemDto
            {
                Id = Guid.NewGuid(),
                Label = linkLabel,
                Url = linkUrl,
                OpenInNewTab = false,
                Order = 1
            }
        ]
    };
}
