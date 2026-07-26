// ABOUTME: Unit tests for the typed public-experience shell query handler.
// ABOUTME: Verifies tenant-local OrganizationCentric settings and primary organization safety states.

using System.Text.Json;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Services;
using Explore.Application.DTOs.Footer;
using Explore.Application.DTOs.Onboarding;
using Explore.Application.DTOs.PublicExperience;
using Explore.Application.DTOs.Tenant;
using Explore.Application.Features.PublicExperience.Handlers.Queries;
using Explore.Application.Features.PublicExperience.Requests.Queries;
using Explore.Application.Features.Tenants.Requests.Queries;
using Explore.Application.Models;
using Explore.Application.Models.PublicExperience;
using Explore.Application.Settings;
using Explore.Domain;
using Explore.Domain.Constants;
using Explore.Domain.Enums;
using MediatR;
using NSubstitute;

namespace Event.Application.UnitTests.Features.PublicExperience.Queries;

public class GetPublicExperienceShellQueryHandlerTests
{
    private readonly IRequestHandler<GetPublicExperienceSettingsQuery, PublicExperienceSettingsDto> _settingsHandler;
    private readonly IRequestHandler<GetTenantNavLinksQuery, List<TenantNavigationLinkDto>> _navigationLinksHandler;
    private readonly ITenantContext _tenantContext;
    private readonly IHierarchicalSettingsResolver _settingsResolver;
    private readonly IOrganizationRepository _organizationRepository;
    private readonly GetPublicExperienceShellQueryHandler _handler;

    public GetPublicExperienceShellQueryHandlerTests()
    {
        _settingsHandler = Substitute.For<IRequestHandler<GetPublicExperienceSettingsQuery, PublicExperienceSettingsDto>>();
        _navigationLinksHandler = Substitute.For<IRequestHandler<GetTenantNavLinksQuery, List<TenantNavigationLinkDto>>>();
        _tenantContext = Substitute.For<ITenantContext>();
        _settingsResolver = Substitute.For<IHierarchicalSettingsResolver>();
        _organizationRepository = Substitute.For<IOrganizationRepository>();

        _settingsHandler.Handle(Arg.Any<GetPublicExperienceSettingsQuery>(), Arg.Any<CancellationToken>())
            .Returns(CreateSettings(Guid.Empty));
        _navigationLinksHandler.Handle(Arg.Any<GetTenantNavLinksQuery>(), Arg.Any<CancellationToken>())
            .Returns([]);
        _settingsResolver.ResolveAsync<string>(
                GovernanceSettingKeys.PublicExperience.Mode,
                Arg.Any<SettingContext>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<string?>(null));
        _settingsResolver.ResolveAsync<string>(
                GovernanceSettingKeys.PublicExperience.EventCatalogLabel,
                Arg.Any<SettingContext>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<string?>(null));
        _settingsResolver.ResolveAsync<string>(
                GovernanceSettingKeys.PublicExperience.PrimaryOrganizationId,
                Arg.Any<SettingContext>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<string?>(null));
        _settingsResolver.ResolveAsync<string>(
                GovernanceSettingKeys.PublicExperience.HomeBlocks,
                Arg.Any<SettingContext>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<string?>(null));
        _settingsResolver.ResolveAsync<string>(
                GovernanceSettingKeys.PublicExperience.Ctas,
                Arg.Any<SettingContext>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<string?>(null));
        _settingsResolver.ResolveAsync<string>(
                GovernanceSettingKeys.PublicExperience.EventSectionPresets,
                Arg.Any<SettingContext>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<string?>(null));
        _settingsResolver.ResolveAsync<string>(
                GovernanceSettingKeys.UiShell.RailPublicVisibility,
                Arg.Any<SettingContext>(),
                Arg.Any<CancellationToken>())
            .Returns("AuthenticatedOnly");

        _handler = new GetPublicExperienceShellQueryHandler(
            _settingsHandler,
            _navigationLinksHandler,
            _tenantContext,
            _settingsResolver,
            _organizationRepository);
    }

    [Test]
    public async Task Handle_WhenPublicExperienceSettingsAreMissing_ReturnsDiscoveryCentricNeutralShell()
    {
        var tenantId = Guid.NewGuid();
        _tenantContext.TenantId.Returns(tenantId);
        _settingsHandler.Handle(Arg.Any<GetPublicExperienceSettingsQuery>(), Arg.Any<CancellationToken>())
            .Returns(CreateSettings(tenantId));

        PublicExperienceShellDto result = await _handler.Handle(new GetPublicExperienceShellQuery(), CancellationToken.None);

        await Assert.That(result.SchemaVersion).IsEqualTo(1);
        await Assert.That(result.Mode).IsEqualTo(PublicExperienceMode.DiscoveryCentric);
        await Assert.That(result.EventCatalog.Label).IsEqualTo("Events");
        await Assert.That(result.EventCatalog.Url).IsEqualTo("/events");
        await Assert.That(result.PrimaryOrganization.State).IsEqualTo(PublicExperiencePrimaryOrganizationState.NotConfigured);
        await Assert.That(result.RailPublicVisibility).IsEqualTo("AuthenticatedOnly");
        await Assert.That(result.Footer.Settings.Template).IsEqualTo("minimal");
        await _organizationRepository.DidNotReceive().GetOrganizationWithDetails(Arg.Any<Guid>());
    }

    [Test]
    public async Task Handle_WhenRailVisibilityIsAlways_ReturnsResolvedPublicRailPolicy()
    {
        var tenantId = Guid.NewGuid();
        _tenantContext.TenantId.Returns(tenantId);
        _settingsHandler.Handle(Arg.Any<GetPublicExperienceSettingsQuery>(), Arg.Any<CancellationToken>())
            .Returns(CreateSettings(tenantId));
        _settingsResolver.ResolveAsync<string>(
                GovernanceSettingKeys.UiShell.RailPublicVisibility,
                Arg.Is<SettingContext>(context => context.TenantId == tenantId),
                Arg.Any<CancellationToken>())
            .Returns("Always");

        PublicExperienceShellDto result = await _handler.Handle(new GetPublicExperienceShellQuery(), CancellationToken.None);

        await Assert.That(result.RailPublicVisibility).IsEqualTo("Always");
        await Assert.That(result.Revision).Contains(":Always:");
    }

    [Test]
    public async Task Handle_WhenOrganizationCentricPrimaryOrganizationIsAvailable_ReturnsActorBackedReference()
    {
        var tenantId = Guid.NewGuid();
        var organizationId = Guid.NewGuid();
        var actorId = Guid.NewGuid();
        _tenantContext.TenantId.Returns(tenantId);
        _settingsHandler.Handle(Arg.Any<GetPublicExperienceSettingsQuery>(), Arg.Any<CancellationToken>())
            .Returns(CreateSettings(tenantId));
        _settingsResolver.ResolveAsync<string>(
                GovernanceSettingKeys.PublicExperience.Mode,
                Arg.Any<SettingContext>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<string?>("OrganizationCentric"));
        _settingsResolver.ResolveAsync<string>(
                GovernanceSettingKeys.PublicExperience.EventCatalogLabel,
                Arg.Any<SettingContext>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<string?>("Programs"));
        _settingsResolver.ResolveAsync<string>(
                GovernanceSettingKeys.PublicExperience.PrimaryOrganizationId,
                Arg.Any<SettingContext>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<string?>(organizationId.ToString()));
        _organizationRepository.GetOrganizationWithDetails(organizationId)
            .Returns(CreateOrganization(tenantId, organizationId, actorId));

        PublicExperienceShellDto result = await _handler.Handle(new GetPublicExperienceShellQuery(), CancellationToken.None);

        await Assert.That(result.Mode).IsEqualTo(PublicExperienceMode.OrganizationCentric);
        await Assert.That(result.EventCatalog.Label).IsEqualTo("Programs");
        await Assert.That(result.PrimaryOrganization.State).IsEqualTo(PublicExperiencePrimaryOrganizationState.Available);
        await Assert.That(result.PrimaryOrganization.OrganizationId).IsEqualTo(organizationId);
        await Assert.That(result.PrimaryOrganization.ActorId).IsEqualTo(actorId);
        await Assert.That(result.PrimaryOrganization.DisplayName).IsEqualTo("Primary organizer");
        await Assert.That(result.PrimaryOrganization.Handle).IsEqualTo("primary.example");
        await Assert.That(result.PrimaryOrganization.WebsiteUrl).IsEqualTo("https://primary.example");
        await Assert.That(result.PrimaryOrganization.ProfilePictureUri).IsEqualTo("https://cdn.example/avatar.png");
        await Assert.That(result.Revision).Contains(organizationId.ToString("N"));
    }

    [Test]
    public async Task Handle_WhenOrganizationCentricConfigIsEmpty_ReturnsReadOnlyPrimaryOrganizationDefaults()
    {
        var tenantId = Guid.NewGuid();
        var organizationId = Guid.NewGuid();
        var actorId = Guid.NewGuid();
        _tenantContext.TenantId.Returns(tenantId);
        _settingsHandler.Handle(Arg.Any<GetPublicExperienceSettingsQuery>(), Arg.Any<CancellationToken>())
            .Returns(CreateSettings(tenantId));
        _settingsResolver.ResolveAsync<string>(
                GovernanceSettingKeys.PublicExperience.Mode,
                Arg.Any<SettingContext>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<string?>("OrganizationCentric"));
        _settingsResolver.ResolveAsync<string>(
                GovernanceSettingKeys.PublicExperience.EventCatalogLabel,
                Arg.Any<SettingContext>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<string?>("Programs"));
        ConfigurePrimaryOrganizationId(organizationId);
        _organizationRepository.GetOrganizationWithDetails(organizationId)
            .Returns(CreateOrganization(tenantId, organizationId, actorId));

        PublicExperienceShellDto result = await _handler.Handle(new GetPublicExperienceShellQuery(), CancellationToken.None);

        await Assert.That(result.Home.Blocks.Count).IsEqualTo(1);
        await Assert.That(result.Home.Blocks[0].Key).IsEqualTo("primary-organization-summary");
        await Assert.That(result.Home.Blocks[0].Kind).IsEqualTo(PublicExperienceHomeBlockKind.OrganizationSummary);
        await Assert.That(result.Home.Blocks[0].Title).IsEqualTo("Primary organizer");
        await Assert.That(result.Home.Blocks[0].ImageUrl).IsEqualTo("https://cdn.example/avatar.png");
        await Assert.That(result.Home.Blocks[0].LinkUrl).Contains($"ActorId={actorId:D}");

        await Assert.That(result.EventSections.Count).IsEqualTo(1);
        await Assert.That(result.EventSections[0].Key).IsEqualTo("primary-organization-events");
        await Assert.That(result.EventSections[0].Label).IsEqualTo("Programs");
        await Assert.That(result.EventSections[0].Url).Contains($"ActorId={actorId:D}");

        await Assert.That(result.Ctas.Count).IsEqualTo(1);
        await Assert.That(result.Ctas[0].Key).IsEqualTo("primary-organization-events");
        await Assert.That(result.Ctas[0].Label).IsEqualTo("Browse Programs");
        await Assert.That(result.Ctas[0].Url).Contains($"ActorId={actorId:D}");
        await Assert.That(result.Revision).Contains("primary-organization-summary");
        await Assert.That(result.Revision).Contains("primary-organization-events");
    }

    [Test]
    public async Task Handle_WhenPrimaryOrganizationMetadataChanges_ChangesRevision()
    {
        var tenantId = Guid.NewGuid();
        var organizationId = Guid.NewGuid();
        var actorId = Guid.NewGuid();
        _tenantContext.TenantId.Returns(tenantId);
        _settingsHandler.Handle(Arg.Any<GetPublicExperienceSettingsQuery>(), Arg.Any<CancellationToken>())
            .Returns(CreateSettings(tenantId));
        ConfigurePrimaryOrganizationId(organizationId);

        _organizationRepository.GetOrganizationWithDetails(organizationId)
            .Returns(
                CreateOrganization(tenantId, organizationId, actorId, displayName: "Primary organizer"),
                CreateOrganization(tenantId, organizationId, actorId, displayName: "Renamed organizer"));

        PublicExperienceShellDto original = await _handler.Handle(new GetPublicExperienceShellQuery(), CancellationToken.None);
        PublicExperienceShellDto renamed = await _handler.Handle(new GetPublicExperienceShellQuery(), CancellationToken.None);

        await Assert.That(original.PrimaryOrganization.DisplayName).IsEqualTo("Primary organizer");
        await Assert.That(renamed.PrimaryOrganization.DisplayName).IsEqualTo("Renamed organizer");
        await Assert.That(original.Revision).IsNotEqualTo(renamed.Revision);
    }


    [Test]
    public async Task Handle_WhenPrimaryOrganizationBelongsToDifferentTenant_ReturnsCrossTenantInvalidWithoutLeakingDetails()
    {
        var tenantId = Guid.NewGuid();
        var otherTenantId = Guid.NewGuid();
        var organizationId = Guid.NewGuid();
        _tenantContext.TenantId.Returns(tenantId);
        _settingsHandler.Handle(Arg.Any<GetPublicExperienceSettingsQuery>(), Arg.Any<CancellationToken>())
            .Returns(CreateSettings(tenantId));
        _settingsResolver.ResolveAsync<string>(
                GovernanceSettingKeys.PublicExperience.PrimaryOrganizationId,
                Arg.Any<SettingContext>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<string?>(organizationId.ToString()));
        _organizationRepository.GetOrganizationWithDetails(organizationId)
            .Returns(CreateOrganization(otherTenantId, organizationId, Guid.NewGuid()));

        PublicExperienceShellDto result = await _handler.Handle(new GetPublicExperienceShellQuery(), CancellationToken.None);

        await Assert.That(result.PrimaryOrganization.State).IsEqualTo(PublicExperiencePrimaryOrganizationState.CrossTenantInvalid);
        await Assert.That(result.PrimaryOrganization.OrganizationId).IsEqualTo(organizationId);
        await Assert.That(result.PrimaryOrganization.ActorId).IsNull();
        await Assert.That(result.PrimaryOrganization.DisplayName).IsEqualTo(string.Empty);
        await Assert.That(result.PrimaryOrganization.Handle).IsEqualTo(string.Empty);
    }

    [Test]
    public async Task Handle_WhenEventSectionPresetConfigExists_ReturnsGeneratedEventUrls()
    {
        var tenantId = Guid.NewGuid();
        var actorId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        var tagId = Guid.NewGuid();
        _tenantContext.TenantId.Returns(tenantId);
        _settingsHandler.Handle(Arg.Any<GetPublicExperienceSettingsQuery>(), Arg.Any<CancellationToken>())
            .Returns(CreateSettings(tenantId));

        var presetConfig = new PublicEventSectionPresetsConfig(
            Presets:
            [
                new PublicEventSectionPresetConfig(
                    Id: "featured",
                    Label: "Featured programs",
                    Owners: new PublicEventSectionOwnerFilter(ActorIds: [actorId]),
                    Filters: new PublicEventSectionEventFilter(
                        CategoryIds: [categoryId],
                        TagIds: [tagId],
                        AudienceGenderIds: [1],
                        AudienceAgeIds: [2],
                        EventTypeIds: [3],
                        EventFormatIds: [4],
                        Date: new PublicEventSectionDateFilter(
                            PublicEventSectionDateWindow.Custom,
                            new DateOnly(2026, 5, 1),
                            new DateOnly(2026, 5, 31)),
                        CustomProperties:
                        [
                            new PublicEventSectionCustomPropertyFilter(
                                Namespace: "tenant.event",
                                Key: "track",
                                Operator: PublicEventSectionCustomPropertyOperator.Equals,
                                Values: ["youth"])
                        ]),
                    Icon: "calendar-star",
                    SortOrder: 10,
                    Limit: 6),
                new PublicEventSectionPresetConfig(
                    Id: "disabled",
                    Label: "Disabled",
                    IsEnabled: false)
            ]);
        _settingsResolver.ResolveAsync<string>(
                GovernanceSettingKeys.PublicExperience.EventSectionPresets,
                Arg.Any<SettingContext>(),
                Arg.Any<CancellationToken>())
            .Returns(JsonSerializer.Serialize(presetConfig, new JsonSerializerOptions(JsonSerializerDefaults.Web)));

        PublicExperienceShellDto result = await _handler.Handle(new GetPublicExperienceShellQuery(), CancellationToken.None);

        await Assert.That(result.EventSections.Count).IsEqualTo(1);
        PublicExperienceEventSectionDto section = result.EventSections[0];
        await Assert.That(section.Key).IsEqualTo("featured");
        await Assert.That(section.Label).IsEqualTo("Featured programs");
        await Assert.That(section.Icon).IsEqualTo("calendar-star");
        await Assert.That(section.SortOrder).IsEqualTo(10);
        await Assert.That(section.Url).StartsWith("/events?");
        await Assert.That(section.Url).Contains($"ActorId={actorId:D}");
        await Assert.That(section.Url).Contains($"IncludedCategoryIds={categoryId:D}");
        await Assert.That(section.Url).Contains($"IncludedTagIds={tagId:D}");
        await Assert.That(section.Url).Contains("AudienceGenderIds=1");
        await Assert.That(section.Url).Contains("AudienceAgeIds=2");
        await Assert.That(section.Url).Contains("EventTypeIds=3");
        await Assert.That(section.Url).Contains("FormatIds=4");
        await Assert.That(section.Url).Contains("DateFrom=2026-05-01");
        await Assert.That(section.Url).Contains("DateTo=2026-05-31");
        await Assert.That(section.Url).Contains("CustomPropertyFilters%5B0%5D.Namespace=tenant.event");
        await Assert.That(section.Url).Contains("CustomPropertyFilters%5B0%5D.Key=track");
        await Assert.That(section.Url).Contains("CustomPropertyFilters%5B0%5D.Value=youth");
        await Assert.That(section.Url).Contains("PageSize=6");
        await Assert.That(result.Revision).Contains("featured");
    }

    [Test]
    public async Task Handle_WhenHomeBlocksAndCtasConfigured_ReturnsBoundedShellProjection()
    {
        var tenantId = Guid.NewGuid();
        _tenantContext.TenantId.Returns(tenantId);
        _settingsHandler.Handle(Arg.Any<GetPublicExperienceSettingsQuery>(), Arg.Any<CancellationToken>())
            .Returns(CreateSettings(tenantId));

        var homeConfig = new PublicExperienceHomeBlocksConfig(
            Blocks:
            [
                new PublicExperienceHomeBlockConfig(
                    Id: "hero",
                    Kind: PublicExperienceHomeBlockKind.Hero,
                    Title: "Welcome home",
                    Subtitle: "Programs for the community",
                    Body: "Plain bounded copy",
                    ImageUrl: "/media/hero.png",
                    LinkText: "See programs",
                    LinkUrl: "/events",
                    SortOrder: 2),
                new PublicExperienceHomeBlockConfig(
                    Id: "disabled",
                    Kind: PublicExperienceHomeBlockKind.RichText,
                    Title: "Hidden",
                    IsEnabled: false)
            ]);
        var ctaConfig = new PublicExperienceCtasConfig(
            Ctas:
            [
                new PublicExperienceCtaConfig(
                    Id: "donate",
                    Label: "Donate",
                    Url: "/donate",
                    Placement: PublicExperienceCtaPlacement.Hero,
                    Style: PublicExperienceCtaStyle.Secondary,
                    SortOrder: 3),
                new PublicExperienceCtaConfig(
                    Id: "disabled",
                    Label: "Hidden",
                    Url: "/hidden",
                    Placement: PublicExperienceCtaPlacement.Footer,
                    IsEnabled: false)
            ]);
        var serializerOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        _settingsResolver.ResolveAsync<string>(
                GovernanceSettingKeys.PublicExperience.HomeBlocks,
                Arg.Any<SettingContext>(),
                Arg.Any<CancellationToken>())
            .Returns(JsonSerializer.Serialize(homeConfig, serializerOptions));
        _settingsResolver.ResolveAsync<string>(
                GovernanceSettingKeys.PublicExperience.Ctas,
                Arg.Any<SettingContext>(),
                Arg.Any<CancellationToken>())
            .Returns(JsonSerializer.Serialize(ctaConfig, serializerOptions));

        PublicExperienceShellDto result = await _handler.Handle(new GetPublicExperienceShellQuery(), CancellationToken.None);

        await Assert.That(result.Home.Blocks.Count).IsEqualTo(1);
        PublicExperienceHomeBlockDto block = result.Home.Blocks[0];
        await Assert.That(block.Key).IsEqualTo("hero");
        await Assert.That(block.Kind).IsEqualTo(PublicExperienceHomeBlockKind.Hero);
        await Assert.That(block.Title).IsEqualTo("Welcome home");
        await Assert.That(block.Subtitle).IsEqualTo("Programs for the community");
        await Assert.That(block.Body).IsEqualTo("Plain bounded copy");
        await Assert.That(block.ImageUrl).IsEqualTo("/media/hero.png");
        await Assert.That(block.LinkText).IsEqualTo("See programs");
        await Assert.That(block.LinkUrl).IsEqualTo("/events");

        await Assert.That(result.Ctas.Count).IsEqualTo(1);
        PublicExperienceCtaDto cta = result.Ctas[0];
        await Assert.That(cta.Key).IsEqualTo("donate");
        await Assert.That(cta.Label).IsEqualTo("Donate");
        await Assert.That(cta.Url).IsEqualTo("/donate");
        await Assert.That(cta.Placement).IsEqualTo(PublicExperienceCtaPlacement.Hero);
        await Assert.That(cta.Style).IsEqualTo(PublicExperienceCtaStyle.Secondary);
        await Assert.That(result.Revision).Contains("hero");
        await Assert.That(result.Revision).Contains("donate");
    }

    [Test]
    public async Task Handle_WhenTenantNavigationLinksExist_ReturnsShellNavigationAndRevisionInput()
    {
        var tenantId = Guid.NewGuid();
        _tenantContext.TenantId.Returns(tenantId);
        _settingsHandler.Handle(Arg.Any<GetPublicExperienceSettingsQuery>(), Arg.Any<CancellationToken>())
            .Returns(CreateSettings(tenantId));
        _navigationLinksHandler.Handle(Arg.Any<GetTenantNavLinksQuery>(), Arg.Any<CancellationToken>())
            .Returns([
                new TenantNavigationLinkDto { Label = "Donate", Url = "/donate", Order = 20 },
                new TenantNavigationLinkDto { Label = "Programs", Url = "/events", Order = 10 },
                new TenantNavigationLinkDto { Label = "", Url = "/hidden", Order = 0 }
            ]);

        PublicExperienceShellDto result = await _handler.Handle(new GetPublicExperienceShellQuery(), CancellationToken.None);

        await Assert.That(result.Navigation.Links.Count).IsEqualTo(2);
        await Assert.That(result.Navigation.Links[0].Label).IsEqualTo("Programs");
        await Assert.That(result.Navigation.Links[0].Url).IsEqualTo("/events");
        await Assert.That(result.Navigation.Links[0].SortOrder).IsEqualTo(10);
        await Assert.That(result.Navigation.Links[1].Label).IsEqualTo("Donate");
        await Assert.That(result.Revision).Contains("Programs:/events");
    }

    [Test]
    public async Task Handle_WhenPrimaryOrganizationIsMissing_ReturnsMissingState()
    {
        var tenantId = Guid.NewGuid();
        var organizationId = Guid.NewGuid();
        _tenantContext.TenantId.Returns(tenantId);
        _settingsHandler.Handle(Arg.Any<GetPublicExperienceSettingsQuery>(), Arg.Any<CancellationToken>())
            .Returns(CreateSettings(tenantId));
        ConfigurePrimaryOrganizationId(organizationId);
        _organizationRepository.GetOrganizationWithDetails(organizationId).Returns((Organization?)null);

        PublicExperienceShellDto result = await _handler.Handle(new GetPublicExperienceShellQuery(), CancellationToken.None);

        await Assert.That(result.PrimaryOrganization.State).IsEqualTo(PublicExperiencePrimaryOrganizationState.Missing);
        await Assert.That(result.PrimaryOrganization.OrganizationId).IsEqualTo(organizationId);
    }

    [Test]
    public async Task Handle_WhenPrimaryOrganizationIsDeleted_ReturnsDeletedStateWithoutLeakingDetails()
    {
        var tenantId = Guid.NewGuid();
        var organizationId = Guid.NewGuid();
        _tenantContext.TenantId.Returns(tenantId);
        _settingsHandler.Handle(Arg.Any<GetPublicExperienceSettingsQuery>(), Arg.Any<CancellationToken>())
            .Returns(CreateSettings(tenantId));
        ConfigurePrimaryOrganizationId(organizationId);
        _organizationRepository.GetOrganizationWithDetails(organizationId)
            .Returns(CreateOrganization(tenantId, organizationId, Guid.NewGuid(), isDeleted: true));

        PublicExperienceShellDto result = await _handler.Handle(new GetPublicExperienceShellQuery(), CancellationToken.None);

        await Assert.That(result.PrimaryOrganization.State).IsEqualTo(PublicExperiencePrimaryOrganizationState.Deleted);
        await Assert.That(result.PrimaryOrganization.DisplayName).IsEqualTo(string.Empty);
    }

    [Test]
    public async Task Handle_WhenPrimaryOrganizationIsNotApproved_ReturnsHiddenOrInactiveState()
    {
        var tenantId = Guid.NewGuid();
        var organizationId = Guid.NewGuid();
        _tenantContext.TenantId.Returns(tenantId);
        _settingsHandler.Handle(Arg.Any<GetPublicExperienceSettingsQuery>(), Arg.Any<CancellationToken>())
            .Returns(CreateSettings(tenantId));
        ConfigurePrimaryOrganizationId(organizationId);
        _organizationRepository.GetOrganizationWithDetails(organizationId)
            .Returns(CreateOrganization(
                tenantId,
                organizationId,
                Guid.NewGuid(),
                approvalStatusId: (int)ApprovalStatusEnum.Pending));

        PublicExperienceShellDto result = await _handler.Handle(new GetPublicExperienceShellQuery(), CancellationToken.None);

        await Assert.That(result.PrimaryOrganization.State).IsEqualTo(PublicExperiencePrimaryOrganizationState.HiddenOrInactive);
        await Assert.That(result.PrimaryOrganization.DisplayName).IsEqualTo(string.Empty);
    }

    [Test]
    public async Task Handle_WhenPrimaryOrganizationActorIsUnavailable_ReturnsActorUnavailableState()
    {
        var tenantId = Guid.NewGuid();
        var organizationId = Guid.NewGuid();
        _tenantContext.TenantId.Returns(tenantId);
        _settingsHandler.Handle(Arg.Any<GetPublicExperienceSettingsQuery>(), Arg.Any<CancellationToken>())
            .Returns(CreateSettings(tenantId));
        ConfigurePrimaryOrganizationId(organizationId);
        _organizationRepository.GetOrganizationWithDetails(organizationId)
            .Returns(CreateOrganization(tenantId, organizationId, Guid.NewGuid(), actorDeleted: true));

        PublicExperienceShellDto result = await _handler.Handle(new GetPublicExperienceShellQuery(), CancellationToken.None);

        await Assert.That(result.PrimaryOrganization.State).IsEqualTo(PublicExperiencePrimaryOrganizationState.ActorUnavailable);
        await Assert.That(result.PrimaryOrganization.DisplayName).IsEqualTo(string.Empty);
    }

    private static PublicExperienceSettingsDto CreateSettings(Guid tenantId)
    {
        return new PublicExperienceSettingsDto
        {
            TenantId = tenantId,
            PreferredHomePage = "EventList",
            BrandDisplayName = "Tenant brand",
            BrandLogoUrl = "/brand.svg",
            BrandFaviconUrl = "/favicon.ico",
            FooterConfig = new FooterConfigDto
            {
                Settings = new FooterSettingsDto { Template = "minimal" },
                LinkGroups = []
            }
        };
    }

    private void ConfigurePrimaryOrganizationId(Guid organizationId)
    {
        _settingsResolver.ResolveAsync<string>(
                GovernanceSettingKeys.PublicExperience.PrimaryOrganizationId,
                Arg.Any<SettingContext>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<string?>(organizationId.ToString()));
    }

    private static Organization CreateOrganization(
        Guid tenantId,
        Guid organizationId,
        Guid actorId,
        int approvalStatusId = (int)ApprovalStatusEnum.Approved,
        bool isDeleted = false,
        bool actorDeleted = false,
        string displayName = "Primary organizer")
    {
        var tenant = new Tenant
        {
            Id = tenantId,
            FullName = "Tenant",
            Slug = "tenant",
            TenantStatusId = (int)TenantStatusEnum.Active,
            TenantStatus = new TenantStatus
            {
                Id = (int)TenantStatusEnum.Active,
                MasterCode = "Active",
                FullName = "Active",
                IsActiveState = true
            }
        };

        var actor = new Actor
        {
            Id = actorId,
            ActorTypeId = 2,
            ActorType = new ActorType { Id = 2, MasterCode = "Organization", FullName = "Organization" },
            IsDeleted = actorDeleted,
            Pii = new ActorPii
            {
                ActorId = actorId,
                DisplayName = displayName,
                ProfilePictureUri = "https://cdn.example/avatar.png"
            }
        };
        actor.AtprotoIdentities.Add(new AtprotoIdentity
        {
            Id = Guid.CreateVersion7(),
            Did = "did:plc:primary",
            ActorId = actorId,
            Actor = actor,
            Handle = "primary.example",
            PdsHost = "https://pds.example.com",
            IsActive = true,
            LastResolvedAt = DateTime.UtcNow,
            ConcurrencyStamp = Guid.CreateVersion7()
        });

        var organization = new Organization
        {
            Id = organizationId,
            Actor = actor,
            WebsiteUrl = "https://primary.example",
            IsDeleted = isDeleted,
            Pii = new OrganizationPii
            {
                OrganizationId = organizationId,
                FullName = displayName
            }
        };
        organization.TenantParticipations.Add(new OrganizationTenant
        {
            Id = Guid.CreateVersion7(),
            TenantId = tenantId,
            Tenant = tenant,
            OrganizationId = organizationId,
            Organization = organization,
            ApprovalStatusId = approvalStatusId,
            ApprovalStatus = new ApprovalStatus
            {
                Id = approvalStatusId,
                MasterCode = ((ApprovalStatusEnum)approvalStatusId).ToString(),
                FullName = ((ApprovalStatusEnum)approvalStatusId).ToString()
            },
            IsVisible = true
        });

        actor.OrganizationId = organizationId;
        actor.Organization = organization;
        return organization;
    }
}
