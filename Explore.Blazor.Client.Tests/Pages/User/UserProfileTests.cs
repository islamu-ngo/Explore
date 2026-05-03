// ABOUTME: Component tests for UserProfile auth-sensitive loading/error/fallback/success states.
// ABOUTME: Verifies sync fallback and stats/review rendering from service data.

using Explore.Blazor.Client.Pages.User;

namespace Explore.Blazor.Client.Tests.Pages.User;

public class UserProfileTests : IDisposable
{
    private readonly BlazorTestContext _ctx;
    private readonly IUserService _userService;
    private readonly IEventService _eventService;
    private readonly IOrganizationReviewService _reviewService;

    public UserProfileTests()
    {
        _ctx = new BlazorTestContext();
        _userService = Substitute.For<IUserService>();
        _eventService = Substitute.For<IEventService>();
        _reviewService = Substitute.For<IOrganizationReviewService>();

        _ctx.Services.AddSingleton(_userService);
        _ctx.Services.AddSingleton(_eventService);
        _ctx.Services.AddSingleton(_reviewService);
        _ctx.Services.AddSingleton(Substitute.For<ILogger<UserProfile>>());

        _ctx.SetAuthenticatedUser(Guid.NewGuid(), "Test User", "test@example.com");
    }

    public void Dispose()
    {
        _ctx.Dispose();
    }

    [Test]
    public async Task UserProfile_ShowsLoadingState_WhileUserLookupIsPending()
    {
        // Arrange
        var pendingUser = new TaskCompletionSource<UserDto?>();
        _userService.GetCurrentUserAsync().Returns(pendingUser.Task);

        // Act
        var cut = _ctx.RenderMudComponent<UserProfile>();

        // Assert
        await Assert.That(cut.Markup).Contains("Loading profile...");

        // Cleanup
        pendingUser.TrySetResult(new UserDto { Id = Guid.NewGuid(), FirstName = "Test", LastName = "User" });
    }

    [Test]
    public async Task UserProfile_ShowsErrorState_WhenLoadThrows()
    {
        // Arrange
        _userService.GetCurrentUserAsync().ThrowsAsync(new InvalidOperationException("boom"));

        // Act
        var cut = _ctx.RenderMudComponent<UserProfile>();
        cut.WaitForState(() => cut.Markup.Contains("An error occurred while loading your profile", StringComparison.OrdinalIgnoreCase), TimeSpan.FromSeconds(3));

        // Assert
        await Assert.That(cut.Markup).Contains("An error occurred while loading your profile");
        await Assert.That(cut.Markup).Contains("Retry");
    }

    [Test]
    public async Task UserProfile_ShowsFallbackError_WhenUserStillNullAfterSync()
    {
        // Arrange
        _userService.GetCurrentUserAsync().Returns((UserDto?)null);
        _userService.SyncUserAsync().Returns(new BaseCommandResponseOfGuid { Success = false, Message = "sync failed" });

        // Act
        var cut = _ctx.RenderMudComponent<UserProfile>();
        cut.WaitForState(() => cut.Markup.Contains("Unable to load user profile", StringComparison.OrdinalIgnoreCase), TimeSpan.FromSeconds(3));

        // Assert
        await Assert.That(cut.Markup).Contains("Unable to load user profile. Please try refreshing the page.");
    }

    [Test]
    public async Task UserProfile_ShowsUserAndStats_WhenDataLoadsSuccessfully()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _userService.GetCurrentUserAsync().Returns(new UserDto
        {
            Id = userId,
            FirstName = "Amina",
            LastName = "Rahman",
            Username = "amina",
            Email = "amina@example.com",
            EmailVerified = true
        });

        _eventService.GetRegistrationsByUserAsync(userId).Returns(
        [
            new EventRegistrationListDto { Id = Guid.NewGuid() },
            new EventRegistrationListDto { Id = Guid.NewGuid() }
        ]);

        _reviewService.GetReviewsByUserId(userId).Returns(
        [
            new OrganizationReviewDto
            {
                Id = Guid.NewGuid(),
                OrganizationFullName = "Community Center",
                Comment = "Great event",
                Rating = 5,
                CreatedAt = DateTimeOffset.UtcNow
            }
        ]);

        // Act
        var cut = _ctx.RenderMudComponent<UserProfile>();
        cut.WaitForState(() => cut.Markup.Contains("Amina Rahman", StringComparison.OrdinalIgnoreCase), TimeSpan.FromSeconds(3));

        // Assert
        await Assert.That(cut.Markup).Contains("Amina Rahman");
        await Assert.That(cut.Markup).Contains("amina@example.com");
        await Assert.That(cut.Markup).Contains("Email Verified");

        cut.FindAll("[role='tab']")
            .First(tab => tab.TextContent.Contains("Reviews", StringComparison.OrdinalIgnoreCase))
            .Click();

        await Assert.That(cut.Markup).Contains("Community Center");
    }
}
