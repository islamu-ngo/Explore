// ABOUTME: Component tests for the event registration dialog submission boundary.
// ABOUTME: Verifies consent-bearing registrations use the safe registration service contract.

using System.Reflection;
using Explore.Blazor.Client.Pages.Events.Components;
using MudBlazor;

namespace Explore.Blazor.Client.Tests.Pages.Event;

public sealed class EventRegistrationTests : IDisposable
{
    private readonly BlazorTestContext _ctx = new();
    private readonly IEventRegistrationService _registrationService = Substitute.For<IEventRegistrationService>();
    private readonly IUserService _userService = Substitute.For<IUserService>();
    private readonly IContactShareConsentService _consentService = Substitute.For<IContactShareConsentService>();

    public EventRegistrationTests()
    {
        _ctx.Services.AddSingleton(_registrationService);
        _ctx.Services.AddSingleton(_userService);
        _ctx.Services.AddSingleton(_consentService);
        _ctx.Services.AddSingleton(Substitute.For<ISnackbar>());
    }

    public void Dispose()
    {
        _ctx.Dispose();
    }

    [Test]
    public async Task OnInitialized_WhenUserHasEventRegistration_SetsAlreadyRegisteredState()
    {
        var eventId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        _userService.GetCurrentUserAsync().Returns(new UserDto
        {
            Id = userId,
            FirstName = "Amina",
            LastName = "Rahman",
            Email = "amina@example.test"
        });
        _registrationService.GetRegistrationsByUserAsync(userId).Returns(
        [
            new EventRegistrationListDto
            {
                Id = Guid.NewGuid(),
                EventId = eventId,
                EventTitle = "Annual Conference"
            }
        ]);

        var cut = _ctx.RenderMudComponent<EventRegistration>(parameters => parameters
            .Add(component => component.EventId, eventId)
            .Add(component => component.RegistrationPolicyId, 1));

        await _registrationService.Received(1).GetRegistrationsByUserAsync(userId);
        await Assert.That(GetPrivateField<bool>(cut.Instance, "isAlreadyRegistered")).IsTrue();
    }

    [Test]
    public async Task SubmitWithShareEmailUsesRegistrationServiceWithConsentSnapshot()
    {
        var eventId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        _userService.GetCurrentUserAsync().Returns(new UserDto
        {
            Id = userId,
            FirstName = "Amina",
            LastName = "Rahman",
            Email = "amina@example.test"
        });
        _consentService.CheckConsentForOrganizerAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(false);
        _registrationService.RegisterForSessionAsync(Arg.Any<CreateEventRegistrationDto>())
            .Returns(Task.FromResult<BaseCommandResponseOfGuid?>(new BaseCommandResponseOfGuid
            {
                Success = true,
                Message = "Event Registration created successfully."
            }));

        var cut = _ctx.RenderMudComponent<EventRegistration>(parameters => parameters
            .Add(component => component.EventId, eventId)
            .Add(component => component.EventSessionId, sessionId)
            .Add(component => component.RegistrationPolicyId, 3)
            .Add(component => component.RecipientActorId, Guid.NewGuid())
            .Add(component => component.PublisherOrganizationName, "Community Organizer")
            .Add(component => component.Sessions, new List<EventSessionListDto>
            {
                new() { Id = sessionId, Title = "Main Session" }
            }));

        SetPrivateField(cut.Instance, "currentUser", new UserDto
        {
            Id = userId,
            FirstName = "Amina",
            LastName = "Rahman",
            Email = "amina@example.test"
        });
        SetPrivateField(cut.Instance, "registrationModel", new CreateEventRegistrationDto
        {
            EventId = eventId,
            UserId = userId
        });
        SetPrivateField(cut.Instance, "_selectedScopeId", 3);
        SetPrivateField(cut.Instance, "_selectedSessionIds", new HashSet<Guid> { sessionId });
        SetPrivateField(cut.Instance, "_shareEmailWithOrganizer", true);

        await InvokePrivateTaskAsync(cut, "HandleSubmit");

        await _registrationService.Received(1).RegisterForSessionAsync(Arg.Is<CreateEventRegistrationDto>(dto =>
            dto != null
            && dto.EventId == eventId
            && dto.UserId == userId
            && dto.RegistrationScopeId == 3
            && dto.SelectedSessionIds != null
            && dto.SelectedSessionIds.SequenceEqual(new[] { sessionId })
            && dto.ShareEmailWithOrganizer == true
            && dto.ConsentTextAcknowledged != null
            && dto.ConsentTextAcknowledged.Contains("Community Organizer", StringComparison.Ordinal)
            && dto.ConsentUiVersion == "v1"));
    }

    [Test]
    public async Task SubmitWithWholeEventPolicyUsesEventScopeWithConsentSnapshot()
    {
        var eventId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        _userService.GetCurrentUserAsync().Returns(new UserDto
        {
            Id = userId,
            FirstName = "Amina",
            LastName = "Rahman",
            Email = "amina@example.test"
        });
        _consentService.CheckConsentForOrganizerAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(false);
        _registrationService.RegisterForSessionAsync(Arg.Any<CreateEventRegistrationDto>())
            .Returns(Task.FromResult<BaseCommandResponseOfGuid?>(new BaseCommandResponseOfGuid
            {
                Success = true,
                Message = "Event Registration created successfully."
            }));

        var cut = _ctx.RenderMudComponent<EventRegistration>(parameters => parameters
            .Add(component => component.EventId, eventId)
            .Add(component => component.RegistrationPolicyId, 1)
            .Add(component => component.RecipientActorId, Guid.NewGuid())
            .Add(component => component.PublisherOrganizationName, "Community Organizer"));

        SetPrivateField(cut.Instance, "currentUser", new UserDto
        {
            Id = userId,
            FirstName = "Amina",
            LastName = "Rahman",
            Email = "amina@example.test"
        });
        SetPrivateField(cut.Instance, "registrationModel", new CreateEventRegistrationDto
        {
            EventId = eventId,
            UserId = userId
        });
        SetPrivateField(cut.Instance, "_selectedScopeId", 1);
        SetPrivateField(cut.Instance, "_shareEmailWithOrganizer", true);

        await InvokePrivateTaskAsync(cut, "HandleSubmit");

        await _registrationService.Received(1).RegisterForSessionAsync(Arg.Is<CreateEventRegistrationDto>(dto =>
            dto != null
            && dto.EventId == eventId
            && dto.UserId == userId
            && dto.RegistrationScopeId == 1
            && dto.SelectedSessionIds == null
            && dto.SelectedEventDayId == null
            && dto.ShareEmailWithOrganizer == true
            && dto.ConsentTextAcknowledged != null
            && dto.ConsentTextAcknowledged.Contains("Community Organizer", StringComparison.Ordinal)
            && dto.ConsentUiVersion == "v1"));
    }

    [Test]
    public async Task Submit_WhenApiReturnsAlreadyExists_ShowsAlreadyRegisteredState()
    {
        var eventId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var snackbar = _ctx.Services.GetRequiredService<ISnackbar>();

        _userService.GetCurrentUserAsync().Returns(new UserDto
        {
            Id = userId,
            FirstName = "Amina",
            LastName = "Rahman",
            Email = "amina@example.test"
        });
        _registrationService.RegisterForSessionAsync(Arg.Any<CreateEventRegistrationDto>())
            .Returns(Task.FromResult<BaseCommandResponseOfGuid?>(new BaseCommandResponseOfGuid
            {
                Success = true,
                Message = "Event Registration already exists."
            }));

        var cut = _ctx.RenderMudComponent<EventRegistration>(parameters => parameters
            .Add(component => component.EventId, eventId)
            .Add(component => component.EventSessionId, sessionId)
            .Add(component => component.RegistrationPolicyId, 3)
            .Add(component => component.Sessions, new List<EventSessionListDto>
            {
                new() { Id = sessionId, Title = "Main Session" }
            }));

        SetPrivateField(cut.Instance, "currentUser", new UserDto
        {
            Id = userId,
            FirstName = "Amina",
            LastName = "Rahman",
            Email = "amina@example.test"
        });
        SetPrivateField(cut.Instance, "registrationModel", new CreateEventRegistrationDto
        {
            EventId = eventId,
            UserId = userId
        });
        SetPrivateField(cut.Instance, "_selectedScopeId", 3);
        SetPrivateField(cut.Instance, "_selectedSessionIds", new HashSet<Guid> { sessionId });

        await InvokePrivateTaskAsync(cut, "HandleSubmit");

        await Assert.That(GetPrivateField<bool>(cut.Instance, "isAlreadyRegistered")).IsTrue();
        await Assert.That(GetPrivateField<bool>(cut.Instance, "isRegistered")).IsFalse();
        snackbar.Received().Add("You are already registered for this event.", Severity.Info);
    }

    private static async Task InvokePrivateTaskAsync(IRenderedComponent<EventRegistration> cut, string methodName)
    {
        var method = typeof(EventRegistration)
            .GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException($"{methodName} not found.");

        await cut.InvokeAsync(async () =>
        {
            var task = method.Invoke(cut.Instance, []) as Task
                ?? throw new InvalidOperationException($"{methodName} did not return a Task.");

            await task;
        });
    }

    private static void SetPrivateField<T>(EventRegistration instance, string fieldName, T value)
    {
        var field = typeof(EventRegistration)
            .GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException($"{fieldName} not found.");

        field.SetValue(instance, value);
    }

    private static T GetPrivateField<T>(EventRegistration instance, string fieldName)
    {
        var field = typeof(EventRegistration)
            .GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException($"{fieldName} not found.");

        return (T?)field.GetValue(instance)
            ?? throw new InvalidOperationException($"{fieldName} returned null.");
    }
}
