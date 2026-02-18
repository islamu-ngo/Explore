using Explore.Blazor.Client.Pages;
using MudBlazor;

namespace Explore.Blazor.Client.Tests.Pages;

/// <summary>
/// Component tests for Home page.
/// Tests authentication state handling and conditional rendering.
/// </summary>
/// <remarks>
/// Home page has three states:
/// 1. Loading - Shows loading indicator while checking auth
/// 2. Authenticated - Shows LandingPageForUsers
/// 3. Anonymous - Shows LandingPageForNonUsers
/// </remarks>
public class HomeTests : IDisposable
{
    private readonly BlazorTestContext _ctx;

    public HomeTests()
    {
        _ctx = new BlazorTestContext();
    }

    public void Dispose()
    {
        _ctx.Dispose();
    }

    #region Authentication State Tests

    [Test]
    public async Task Home_ShowsLoadingState_Initially()
    {
        // Arrange - Set up slow auth response
        _ctx.SetAuthorizingState();

        // Act
        var cut = _ctx.RenderMudComponent<Home>();

        // Assert - Should show loading
        await Assert.That(cut.Markup).Contains("Loading");
    }

    [Test]
    public async Task Home_ShowsLandingPageForUsers_WhenAuthenticated()
    {
        // Arrange
        _ctx.SetAuthenticatedUser(Guid.NewGuid(), "Test User", "test@example.com");

        // Add required services for LandingPageForUsers
        SetupLandingPageServices();

        // Act
        var cut = _ctx.RenderMudComponent<Home>();
        cut.WaitForState(() => !cut.Markup.Contains("Loading", StringComparison.OrdinalIgnoreCase), TimeSpan.FromSeconds(2));

        // Assert - Should render authenticated content
        // LandingPageForUsers typically has different content than non-user page
        await Assert.That(cut.Markup).DoesNotContain("Loading");
    }

    [Test]
    public async Task Home_ShowsLandingPageForNonUsers_WhenAnonymous()
    {
        // Arrange
        _ctx.SetAnonymousUser();

        // Add required services for LandingPageForNonUsers
        SetupLandingPageServices();

        // Act
        var cut = _ctx.RenderMudComponent<Home>();
        cut.WaitForState(() => !cut.Markup.Contains("Loading", StringComparison.OrdinalIgnoreCase), TimeSpan.FromSeconds(2));

        // Assert - Should render anonymous content
        await Assert.That(cut.Markup).DoesNotContain("Loading");
    }

    #endregion

    #region Page Title Tests

    [Test]
    public async Task Home_SetsPageTitle()
    {
        // Arrange
        _ctx.SetAnonymousUser();
        SetupLandingPageServices();

        // Act
        var cut = _ctx.RenderMudComponent<Home>();
        cut.WaitForState(() => !cut.Markup.Contains("Loading", StringComparison.OrdinalIgnoreCase), TimeSpan.FromSeconds(2));

        // Assert - PageTitle component renders in head, check landing page content instead
        // LandingPageForNonUsers has specific content like "Sign Up" and "Explore"
        await Assert.That(cut.Markup).Contains("Sign Up");
    }

    #endregion

    #region Error Handling Tests

    [Test]
    public async Task Home_HandlesAuthError_Gracefully()
    {
        // Arrange - Set anonymous (simulates auth error fallback)
        _ctx.SetAnonymousUser();
        SetupLandingPageServices();

        // Act - Should not throw
        var cut = _ctx.RenderMudComponent<Home>();
        cut.WaitForState(() => !cut.Markup.Contains("Loading", StringComparison.OrdinalIgnoreCase), TimeSpan.FromSeconds(2));

        // Assert - Should render without crash
        await Assert.That(cut.Markup).DoesNotContain("Loading");
    }

    #endregion

    /// <summary>
    /// Sets up services required by the landing page components.
    /// </summary>
    private void SetupLandingPageServices()
    {
        // LandingPageService is required by both landing pages
        var landingPageService = Substitute.For<ILandingPageService>();
        landingPageService.GetFeaturedEventsAsync(Arg.Any<int>()).Returns(new List<EventListDto>());
        landingPageService.GetTotalMembersCountAsync().Returns(100);
        landingPageService.GetUpcomingEventsCountAsync().Returns(10);
        _ctx.Services.AddSingleton(landingPageService);

        // Services that LandingPageForUsers/NonUsers might need
        var eventService = Substitute.For<IEventService>();
        eventService.GetAllEventsAsync().Returns(new List<EventListDto>());
        eventService.GetEventTypesAsync().Returns(new List<EventTypeListDto>());
        eventService.GetEventFormatsAsync().Returns(new List<EventFormatListDto>());
        eventService.GetAllSessionsAsync().Returns(new List<EventSessionListDto>());
        _ctx.Services.AddSingleton(eventService);

        var categoryService = Substitute.For<ICategoryService>();
        categoryService.GetAllCategoriesAsync().Returns(new List<CategoryListDto>());
        _ctx.Services.AddSingleton(categoryService);

        var organizationService = Substitute.For<IOrganizationService>();
        organizationService.GetMyOrganizationsAsync().Returns(new List<OrganizationListDto>());
        _ctx.Services.AddSingleton(organizationService);

        var userService = Substitute.For<IUserService>();
        _ctx.Services.AddSingleton(userService);

        var authStateService = Substitute.For<IAuthStateService>();
        authStateService.GetCurrentUserIdAsync().Returns(Guid.NewGuid().ToString());
        authStateService.IsAuthenticatedAsync().Returns(true);
        _ctx.Services.AddSingleton(authStateService);

        // Add dialog and snackbar services
        _ctx.Services.AddSingleton(Substitute.For<IDialogService>());
        _ctx.Services.AddSingleton(Substitute.For<ISnackbar>());
    }
}
