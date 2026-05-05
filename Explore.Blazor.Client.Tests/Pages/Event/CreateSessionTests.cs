using Explore.Blazor.Client.Pages.Events.Sessions;
using System.Reflection;

namespace Explore.Blazor.Client.Tests.Pages.Event;

public sealed class CreateSessionTests : IDisposable
{
    private readonly BlazorTestContext _ctx;
    private readonly IEventService _eventService;

    public CreateSessionTests()
    {
        _ctx = new BlazorTestContext();
        _eventService = Substitute.For<IEventService>();
        _ctx.Services.AddSingleton(_eventService);
    }

    [Test]
    public async Task SaveSessionAsync_CreatesSessionForDraftEventAndReturnsToDraft()
    {
        var eventId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        _eventService.GetEventByIdAsync(eventId).Returns(new EventDto
        {
            Id = eventId,
            TenantId = tenantId,
            Title = "Program launch",
            ActorDisplayName = "ISLAMU",
            ActorTypeFullName = "Organization",
            EventStatusFullName = "Draft",
            EventStatusMasterCode = "DRAFT",
            VisibilityTypeFullName = "Public",
            VisibilityTypeMasterCode = "PUBLIC",
            EventFormatFullName = "In person",
            EventFormatMasterCode = "IN_PERSON"
        });
        _eventService.CreateSessionAsync(Arg.Any<CreateEventSessionDto>()).Returns(new BaseCommandResponseOfGuid
        {
            Success = true,
            Id = sessionId
        });

        var cut = _ctx.Render<CreateSession>(parameters => parameters.Add(component => component.EventId, eventId));
        var session = GetPrivateField<CreateEventSessionDto>(cut.Instance, "_session");
        session.Title = "Opening talk";
        session.Description = "A focused opening session.";
        SetPrivateField(cut.Instance, "_sessionDate", new DateTime(2026, 6, 1));
        SetPrivateField(cut.Instance, "_startTime", new TimeSpan(9, 30, 0));
        SetPrivateField(cut.Instance, "_endTime", new TimeSpan(10, 30, 0));

        await InvokePrivateAsync(cut.Instance, "SaveSessionAsync");

        await _eventService.Received(1).CreateSessionAsync(Arg.Is<CreateEventSessionDto>(dto =>
            dto.EventId == eventId
            && dto.TenantId == tenantId
            && dto.Title == "Opening talk"
            && dto.Description == "A focused opening session."
            && dto.StartTime == new DateTimeOffset(new DateTime(2026, 6, 1, 9, 30, 0))
            && dto.EndTime == new DateTimeOffset(new DateTime(2026, 6, 1, 10, 30, 0))));
        await Assert.That(_ctx.Services.GetRequiredService<NavigationManager>().Uri.EndsWith($"/events/{eventId}/edit", StringComparison.Ordinal)).IsTrue();
    }

    [Test]
    public async Task SaveSessionAsync_WhenEndTimeIsBeforeStartTime_DoesNotCallApi()
    {
        var eventId = Guid.NewGuid();
        _eventService.GetEventByIdAsync(eventId).Returns(new EventDto
        {
            Id = eventId,
            TenantId = Guid.NewGuid(),
            Title = "Program launch",
            ActorDisplayName = "ISLAMU",
            ActorTypeFullName = "Organization",
            EventStatusFullName = "Draft",
            EventStatusMasterCode = "DRAFT",
            VisibilityTypeFullName = "Public",
            VisibilityTypeMasterCode = "PUBLIC",
            EventFormatFullName = "In person",
            EventFormatMasterCode = "IN_PERSON"
        });

        var cut = _ctx.Render<CreateSession>(parameters => parameters.Add(component => component.EventId, eventId));
        var session = GetPrivateField<CreateEventSessionDto>(cut.Instance, "_session");
        session.Title = "Opening talk";
        SetPrivateField(cut.Instance, "_startTime", new TimeSpan(11, 0, 0));
        SetPrivateField(cut.Instance, "_endTime", new TimeSpan(10, 0, 0));

        await InvokePrivateAsync(cut.Instance, "SaveSessionAsync");

        await _eventService.DidNotReceive().CreateSessionAsync(Arg.Any<CreateEventSessionDto>());
        var errorMessage = GetPrivateField<string?>(cut.Instance, "_errorMessage");
        await Assert.That(errorMessage).IsEqualTo("End time must be after start time.");
    }

    public void Dispose() => _ctx.Dispose();

    private static T GetPrivateField<T>(object instance, string fieldName)
    {
        var field = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException($"Field {fieldName} was not found.");
        return (T)(field.GetValue(instance) ?? throw new InvalidOperationException($"Field {fieldName} was null."));
    }

    private static void SetPrivateField<T>(object instance, string fieldName, T value)
    {
        var field = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException($"Field {fieldName} was not found.");
        field.SetValue(instance, value);
    }

    private static async Task InvokePrivateAsync(object instance, string methodName)
    {
        var method = instance.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException($"Method {methodName} was not found.");
        if (method.Invoke(instance, null) is Task task)
        {
            await task;
        }
    }
}
