// ABOUTME: Component tests verifying OrganizationDetails page surfaces Edit controls only when the API
// ABOUTME: returns an `_links.edit` HAL affordance, and never pre-fetches members data on load.

using System.Text.Json;
using Blazouter.Services;
using Explore.Blazor.Client.Pages.Organizations;
using MudBlazor;

namespace Explore.Blazor.Client.Tests.Pages.Organizations;

public class OrganizationDetailsHateoasTests : IDisposable
{
    private readonly BlazorTestContext _ctx;
    private readonly IOrganizationService _organizationService;
    private readonly IEventService _eventService;
    private readonly IOrganizationMemberService _organizationMemberService;

    public OrganizationDetailsHateoasTests()
    {
        _ctx = new BlazorTestContext();
        _organizationService = Substitute.For<IOrganizationService>();
        _eventService = Substitute.For<IEventService>();
        _organizationMemberService = Substitute.For<IOrganizationMemberService>();

        _ctx.Services.AddSingleton(_organizationService);
        _ctx.Services.AddSingleton(_eventService);
        _ctx.Services.AddSingleton(_organizationMemberService);
        _ctx.Services.AddScoped<RouterStateService>();
        _ctx.Services.AddSingleton(Substitute.For<ISnackbar>());
        _ctx.Services.AddSingleton(Substitute.For<IDialogService>());
        _ctx.Services.AddSingleton(Substitute.For<ILogger<OrganizationDetails>>());

        _eventService.GetPublicEventsByActorAsync(Arg.Any<Guid>()).Returns(new List<EventListDto>());
    }

    public void Dispose()
    {
        _ctx.Dispose();
    }

    [Test]
    public async Task WhenApiReturnsEditLink_ShowsOrganizationActionAffordances()
    {
        _organizationService.GetOrganizationByIdAsync(Arg.Any<Guid>())
            .Returns(CreateOrganization(withEditLink: true));

        var cut = _ctx.RenderMudComponent<OrganizationDetails>();
        cut.WaitForState(() => !cut.Markup.Contains("Loading", StringComparison.OrdinalIgnoreCase), TimeSpan.FromSeconds(3));

        await Assert.That(cut.Markup).Contains("Create Event");
        await Assert.That(cut.Markup).Contains("Members");
        await Assert.That(cut.Markup).Contains("Edit");
    }

    [Test]
    public async Task WhenApiDoesNotReturnEditLink_HidesOrganizationActionAffordances()
    {
        _organizationService.GetOrganizationByIdAsync(Arg.Any<Guid>())
            .Returns(CreateOrganization(withEditLink: false));

        var cut = _ctx.RenderMudComponent<OrganizationDetails>();
        cut.WaitForState(() => !cut.Markup.Contains("Loading", StringComparison.OrdinalIgnoreCase), TimeSpan.FromSeconds(3));

        await Assert.That(cut.Markup.Contains("Create Event", StringComparison.Ordinal)).IsFalse();
        await Assert.That(cut.Markup.Contains("Members", StringComparison.Ordinal)).IsFalse();
        await Assert.That(cut.Markup.Contains("Edit", StringComparison.Ordinal)).IsFalse();
    }

    [Test]
    public async Task DoesNotCallOrganizationMembersService_OnPageLoad()
    {
        _organizationService.GetOrganizationByIdAsync(Arg.Any<Guid>())
            .Returns(CreateOrganization(withEditLink: true));

        var cut = _ctx.RenderMudComponent<OrganizationDetails>();
        cut.WaitForState(() => !cut.Markup.Contains("Loading", StringComparison.OrdinalIgnoreCase), TimeSpan.FromSeconds(3));

        await _organizationMemberService.DidNotReceiveWithAnyArgs().GetMembersAsync(default);
    }

    private static OrganizationDto CreateOrganization(bool withEditLink)
    {
        var dto = new OrganizationDto
        {
            Id = Guid.NewGuid(),
            FullName = "Test Organization",
            Email = "test@example.com",
            ApprovalStatusId = 2,
            ApprovalStatusFullName = "Approved",
            AdditionalProperties = new Dictionary<string, object>()
        };

        if (withEditLink)
        {
            using var doc = JsonDocument.Parse(
                "{\"self\":{\"href\":\"/api/organization/1\"},\"edit\":{\"href\":\"/api/organization/1\",\"method\":\"PUT\"}}");
            dto.AdditionalProperties["_links"] = doc.RootElement.Clone();
        }

        return dto;
    }
}
