// ABOUTME: Component tests for MyRegistrations loading/error/empty/success states.
// ABOUTME: Verifies user-scoped registration rendering and event/session enrichment flow.

using Explore.Blazor.Client.Pages.User;
using MudBlazor;

namespace Explore.Blazor.Client.Tests.Pages.User;

public class MyRegistrationsTests : IDisposable
{
    private readonly BlazorTestContext _ctx;
    private readonly IEventService _eventService;
    private readonly IUserService _userService;
    private readonly ISnackbar _snackbar;

    public MyRegistrationsTests()
    {
        _ctx = new BlazorTestContext();
        _eventService = Substitute.For<IEventService>();
        _userService = Substitute.For<IUserService>();
        _snackbar = Substitute.For<ISnackbar>();

        _ctx.Services.AddSingleton(_eventService);
        _ctx.Services.AddSingleton(_userService);
        _ctx.Services.AddSingleton(_snackbar);
        _ctx.Services.AddSingleton(Substitute.For<IDialogService>());

        _ctx.SetAuthenticatedUser(Guid.NewGuid(), "Test User", "test@example.com");
    }

    public void Dispose()
    {
        _ctx.Dispose();
    }

    private IRenderedComponent<DynamicComponent> RenderMyRegistrations()
    {
        var componentType = typeof(UserProfile).Assembly.GetType("Explore.Blazor.Client.Pages.User.MyRegistrations")
                            ?? throw new InvalidOperationException("MyRegistrations component type not found");

        return _ctx.RenderMudComponent<DynamicComponent>(p => p.Add(x => x.Type, componentType));
    }

    [Test]
    public async Task MyRegistrations_ShowsLoadingState_WhileUserLookupIsPending()
    {
        // Arrange
        var pendingUser = new TaskCompletionSource<UserDto?>();
        _userService.GetCurrentUserAsync().Returns(pendingUser.Task);

        // Act
        var cut = RenderMyRegistrations();

        // Assert
        await Assert.That(cut.Markup).Contains("Loading your registrations...");

        // Cleanup
        pendingUser.TrySetResult(new UserDto { Id = Guid.NewGuid(), FirstName = "Test", LastName = "User" });
    }

    [Test]
    public async Task MyRegistrations_ShowsEmptyState_WhenUserHasNoRegistrations()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _userService.GetCurrentUserAsync().Returns(new UserDto { Id = userId, FirstName = "Test", LastName = "User" });
        _eventService.GetRegistrationsByUserAsync(userId).Returns(new List<EventRegistrationListDto>());

        // Act
        var cut = RenderMyRegistrations();
        cut.WaitForState(() => cut.Markup.Contains("No registrations yet", StringComparison.OrdinalIgnoreCase), TimeSpan.FromSeconds(3));

        // Assert
        await Assert.That(cut.Markup).Contains("No registrations yet");
        await Assert.That(cut.Markup).Contains("Browse Events");
        await Assert.That(cut.Markup).Contains("href=\"/events\"");
    }

    [Test]
    public async Task MyRegistrations_ShowsRegistrationCards_WhenRegistrationsExist()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var registrationId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var eventId = Guid.NewGuid();

        _userService.GetCurrentUserAsync().Returns(new UserDto { Id = userId, FirstName = "Test", LastName = "User" });
        _eventService.GetRegistrationsByUserAsync(userId).Returns(
        [
            new EventRegistrationListDto
            {
                Id = registrationId,
                EventSessionId = sessionId,
                ApprovalStatusId = 2,
                ApprovalStatusFullName = "Approved"
            }
        ]);

        _eventService.GetSessionByIdAsync(sessionId).Returns(new EventSessionDto
        {
            Id = sessionId,
            EventId = eventId,
            EventTitle = "Session Title",
            StartTime = DateTimeOffset.UtcNow
        });

        _eventService.GetEventByIdAsync(eventId).Returns(new EventDto
        {
            Id = eventId,
            Title = "Annual Conference",
            FeaturedImageUri = "https://example.test/image.png"
        });

        // Act
        var cut = RenderMyRegistrations();
        cut.WaitForState(() => cut.Markup.Contains("Annual Conference", StringComparison.OrdinalIgnoreCase), TimeSpan.FromSeconds(3));

        // Assert
        await Assert.That(cut.Markup).Contains("Annual Conference");
        await Assert.That(cut.Markup).Contains("Approved");
        await Assert.That(cut.Markup).Contains("Cancel");
    }

    [Test]
    public async Task MyRegistrations_HandlesLoadError_AndShowsEmptyState()
    {
        // Arrange
        _userService.GetCurrentUserAsync().ThrowsAsync(new InvalidOperationException("boom"));

        // Act
        var cut = RenderMyRegistrations();
        cut.WaitForState(() => cut.Markup.Contains("No registrations yet", StringComparison.OrdinalIgnoreCase), TimeSpan.FromSeconds(3));

        // Assert
        await Assert.That(cut.Markup).Contains("No registrations yet");
        _snackbar.Received().Add(Arg.Is<string>(s => s != null && s.Contains("Error loading registrations", StringComparison.OrdinalIgnoreCase)), Severity.Error);
    }
}
