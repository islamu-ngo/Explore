// ABOUTME: Component tests for Phase 9.7 + 9.9 custom-property governance admin UI sections.
// ABOUTME: Verifies exposure grid, governance report, and projection status loading/error/success states.

using Explore.Blazor.Client.Contracts.Services.CustomProperties;
using Explore.Blazor.Client.Models;
using Explore.Blazor.Client.Models.CustomProperties;
using Explore.Blazor.Client.Models.Responses;
using Explore.Domain.Enums;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Explore.Blazor.Client.Tests.Pages.Admin;

public class CustomPropertyGovernanceTests : IDisposable
{
    private readonly BlazorTestContext _ctx;
    private readonly ICustomPropertyAdminService _adminService;

    public CustomPropertyGovernanceTests()
    {
        _ctx = new BlazorTestContext();
        _adminService = Substitute.For<ICustomPropertyAdminService>();

        _ctx.Services.AddSingleton(_adminService);
        _ctx.Services.AddSingleton(Substitute.For<IDialogService>());
        _ctx.Services.AddSingleton(Substitute.For<ISnackbar>());

        _ctx.SetAuthenticatedUser(Guid.NewGuid(), "Admin User", "admin@example.com");
    }

    public void Dispose() => _ctx.Dispose();

    private IRenderedComponent<DynamicComponent> Render(string typeName)
    {
        var type = typeof(Explore.Blazor.Client.Services.CustomPropertyAdminService).Assembly
            .GetType($"Explore.Blazor.Client.Pages.Admin.CustomProperties.Components.{typeName}")
            ?? throw new InvalidOperationException($"Component {typeName} not found");

        return _ctx.RenderMudComponent<DynamicComponent>(p => p.Add(x => x.Type, type));
    }

    private static CustomPropertyDefinitionListModel SampleDefinition(string key = "venue_capacity") => new()
    {
        Id = Guid.NewGuid(),
        EntityTypeName = EntityTypeName.Event,
        Namespace = "tenant",
        Key = key,
        DisplayName = "Venue Capacity",
        PropertyType = PropertyType.Number,
        IsRequired = false,
        IsMulti = false,
        IsActive = true,
        SortOrder = 0,
        ExposureLevel = ExposureLevel.Public,
        IsSearchable = true,
        IsFilterable = true,
        IsExportable = false,
        IsModerationRelevant = false,
        IsAnalyticsRelevant = true,
        IsSystemOwned = false,
        OptionCount = 0
    };

    // ── ExposureGovernanceSection (Task 9.7 / 9.9) ──

    [Test]
    public async Task ExposureSection_ShowsLoadingState_WhilePending()
    {
        var pending = new TaskCompletionSource<PaginatedResult<CustomPropertyDefinitionListModel>>();
        _adminService.GetDefinitionsAsync(Arg.Any<EntityTypeName>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(pending.Task);

        var cut = Render("ExposureGovernanceSection");

        await Assert.That(cut.Markup).Contains("Loading definitions");

        pending.TrySetResult(PaginatedResult<CustomPropertyDefinitionListModel>.Empty());
    }

    [Test]
    public async Task ExposureSection_RendersDefinitions_AfterLoad()
    {
        var items = new List<CustomPropertyDefinitionListModel> { SampleDefinition() };
        _adminService.GetDefinitionsAsync(Arg.Any<EntityTypeName>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new PaginatedResult<CustomPropertyDefinitionListModel>
            {
                Items = items,
                PageNumber = 1,
                PageSize = 200,
                TotalCount = 1
            });

        var cut = Render("ExposureGovernanceSection");
        cut.WaitForState(() => cut.Markup.Contains("Venue Capacity"), TimeSpan.FromSeconds(3));

        await Assert.That(cut.Markup).Contains("Venue Capacity");
        await Assert.That(cut.Markup).Contains("venue_capacity");
    }

    [Test]
    public async Task ExposureSection_ShowsErrorAlert_WhenLoadFails()
    {
        _adminService.GetDefinitionsAsync(Arg.Any<EntityTypeName>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("network down"));

        var cut = Render("ExposureGovernanceSection");
        cut.WaitForState(() => cut.Markup.Contains("Failed to load definitions", StringComparison.OrdinalIgnoreCase), TimeSpan.FromSeconds(3));

        await Assert.That(cut.Markup).Contains("Failed to load definitions: network down");
    }

    // ── GovernanceReportSection ──

    [Test]
    public async Task GovernanceReport_ShowsLoadingState_WhilePending()
    {
        var pending = new TaskCompletionSource<PaginatedResult<CustomPropertyGovernanceRowModel>>();
        _adminService.GetGovernanceReportAsync(Arg.Any<Guid?>(), Arg.Any<string?>(), Arg.Any<PromotionRecommendation?>(),
            Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(pending.Task);

        var cut = Render("GovernanceReportSection");

        await Assert.That(cut.Markup).Contains("Generating governance report");

        pending.TrySetResult(PaginatedResult<CustomPropertyGovernanceRowModel>.Empty());
    }

    [Test]
    public async Task GovernanceReport_ShowsErrorAlert_WhenRequestFails()
    {
        _adminService.GetGovernanceReportAsync(Arg.Any<Guid?>(), Arg.Any<string?>(), Arg.Any<PromotionRecommendation?>(),
            Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>()).ThrowsAsync(new InvalidOperationException("boom"));

        var cut = Render("GovernanceReportSection");
        cut.WaitForState(() => cut.Markup.Contains("Failed to load", StringComparison.OrdinalIgnoreCase), TimeSpan.FromSeconds(3));

        await Assert.That(cut.Markup).Contains("boom");
    }

    [Test]
    public async Task GovernanceReport_RendersRow_AfterLoad()
    {
        var row = new CustomPropertyGovernanceRowModel
        {
            TenantId = Guid.NewGuid(),
            Namespace = "tenant",
            Key = "venue_capacity",
            DisplayName = "Venue Capacity",
            EntityScope = "Event",
            PropertyType = "Number",
            ExposureLevel = ExposureLevel.Public,
            IsSearchable = true,
            IsFilterable = true,
            IsExportable = false,
            IsModerationRelevant = false,
            IsAnalyticsRelevant = false,
            IsSystemOwned = false,
            ActiveInstanceCount = 42,
            LastUsedAt = DateTimeOffset.UtcNow,
            Recommendation = PromotionRecommendation.ConsiderLayer2Promotion
        };

        _adminService.GetGovernanceReportAsync(Arg.Any<Guid?>(), Arg.Any<string?>(), Arg.Any<PromotionRecommendation?>(),
            Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new PaginatedResult<CustomPropertyGovernanceRowModel>
            {
                Items = new List<CustomPropertyGovernanceRowModel> { row },
                PageNumber = 1,
                PageSize = 50,
                TotalCount = 1
            });

        var cut = Render("GovernanceReportSection");
        cut.WaitForState(() => cut.Markup.Contains("Venue Capacity"), TimeSpan.FromSeconds(3));

        await Assert.That(cut.Markup).Contains("venue_capacity");
        await Assert.That(cut.Markup).Contains("42");
    }

    // ── ProjectionStatusSection ──

    [Test]
    public async Task ProjectionStatus_ShowsBothProjectionCards_WhenLoaded()
    {
        _adminService.GetEventProjectionStatusAsync(Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(new List<ProjectionStatusModel>
            {
                new()
                {
                    ProjectionName = "event_custom_property_projection",
                    ProjectionVersion = 1,
                    State = 0,
                    RowsProcessed = 100
                }
            });
        _adminService.GetSessionProjectionStatusAsync(Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(new List<ProjectionStatusModel>
            {
                new()
                {
                    ProjectionName = "event_session_custom_property_projection",
                    ProjectionVersion = 1,
                    State = 0,
                    RowsProcessed = 50
                }
            });
        _adminService.GetDirtyScopesAsync(Arg.Any<Guid?>(), Arg.Any<string?>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(PaginatedResult<ProjectionDirtyScopeModel>.Empty());

        var cut = Render("ProjectionStatusSection");
        cut.WaitForState(() => cut.Markup.Contains("Event Projection") && cut.Markup.Contains("Session Projection"),
            TimeSpan.FromSeconds(3));

        await Assert.That(cut.Markup).Contains("Event Projection");
        await Assert.That(cut.Markup).Contains("Session Projection");
    }

    [Test]
    public async Task ProjectionStatus_ShowsErrorAlert_WhenStatusFails()
    {
        _adminService.GetEventProjectionStatusAsync(Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("status unavailable"));
        _adminService.GetSessionProjectionStatusAsync(Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(new List<ProjectionStatusModel>());
        _adminService.GetDirtyScopesAsync(Arg.Any<Guid?>(), Arg.Any<string?>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(PaginatedResult<ProjectionDirtyScopeModel>.Empty());

        var cut = Render("ProjectionStatusSection");
        cut.WaitForState(() => cut.Markup.Contains("status unavailable", StringComparison.OrdinalIgnoreCase),
            TimeSpan.FromSeconds(3));

        await Assert.That(cut.Markup).Contains("status unavailable");
    }
}
