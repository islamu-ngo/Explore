// ABOUTME: Component tests for tenant organization approvals section loading/error/success states.
// ABOUTME: Verifies organization request summaries render correctly after admin page consolidation.

using Explore.Blazor.Client.Services;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Explore.Blazor.Client.Tests.Pages.Admin;

public class AdminListTests : IDisposable
{
    private readonly BlazorTestContext _ctx;
    private readonly IAdminService _adminService;

    public AdminListTests()
    {
        _ctx = new BlazorTestContext();
        _adminService = Substitute.For<IAdminService>();

        _ctx.Services.AddSingleton(_adminService);
        _ctx.Services.AddSingleton(Substitute.For<IDialogService>());

        _ctx.SetAuthenticatedUser(Guid.NewGuid(), "Admin User", "admin@example.com");
    }

    public void Dispose()
    {
        _ctx.Dispose();
    }

    private IRenderedComponent<DynamicComponent> RenderOrganizationsSection()
    {
        var componentType = typeof(IAdminService).Assembly.GetType("Explore.Blazor.Client.Pages.Admin.Tenant.Components.TenantOrganizationsSection")
                            ?? throw new InvalidOperationException("TenantOrganizationsSection component type not found");

        return _ctx.RenderMudComponent<DynamicComponent>(p => p.Add(x => x.Type, componentType));
    }

    [Test]
    public async Task AdminList_ShowsLoadingState_WhileRequestsPending()
    {
        // Arrange
        var pending = new TaskCompletionSource<ICollection<OrganizationListDto>>();
        _adminService.GetOrganizationRequestsAsync().Returns(pending.Task);

        // Act
        var cut = RenderOrganizationsSection();

        // Assert
        await Assert.That(cut.Markup).Contains("Loading organization requests...");

        // Cleanup
        pending.TrySetResult(new List<OrganizationListDto>());
    }

    [Test]
    public async Task AdminList_ShowsErrorAlert_WhenRequestLoadFails()
    {
        // Arrange
        _adminService.GetOrganizationRequestsAsync().ThrowsAsync(new InvalidOperationException("boom"));

        // Act
        var cut = RenderOrganizationsSection();
        cut.WaitForState(() => cut.Markup.Contains("Failed to load organizations", StringComparison.OrdinalIgnoreCase), TimeSpan.FromSeconds(3));

        // Assert
        await Assert.That(cut.Markup).Contains("Failed to load organizations: boom");
    }

    [Test]
    public async Task AdminList_ShowsSummaryAndTabs_WhenRequestsLoaded()
    {
        // Arrange
        _adminService.GetOrganizationRequestsAsync().Returns(
        [
            new OrganizationListDto
            {
                Id = Guid.NewGuid(),
                FullName = "Community Org",
                Email = "org@example.com",
                ApprovalStatusId = 1,
                CreatedAt = TestTime.UtcNow
            }
        ]);

        // Act
        var cut = RenderOrganizationsSection();
        cut.WaitForState(() => cut.Markup.Contains("Organization Approvals", StringComparison.OrdinalIgnoreCase), TimeSpan.FromSeconds(3));

        // Assert
        await Assert.That(cut.Markup).Contains("Organization Approvals");
        await Assert.That(cut.Markup).Contains("All (1)");
        await Assert.That(cut.Markup).Contains("Pending (1)");
        await Assert.That(cut.Markup).Contains("Approved (0)");
        await Assert.That(cut.Markup).Contains("Rejected (0)");
    }
}
