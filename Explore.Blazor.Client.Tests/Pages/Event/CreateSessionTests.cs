using Explore.Blazor.Client.Pages.Events.Sessions;
using Explore.Blazor.Client.Helpers;
using System.Reflection;
using System.Text.Json;

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
        _eventService.GetEventByIdAsync(eventId).Returns(CreateDraftEvent(eventId, tenantId));
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
            && dto.StartTime == DateTimeHelper.ConvertLocalToUtc(new DateTime(2026, 6, 1, 9, 30, 0))
            && dto.EndTime == DateTimeHelper.ConvertLocalToUtc(new DateTime(2026, 6, 1, 10, 30, 0))
            && dto.StartTime.Value.Offset == TimeSpan.Zero
            && dto.EndTime.Value.Offset == TimeSpan.Zero));
        await Assert.That(_ctx.Services.GetRequiredService<NavigationManager>().Uri.EndsWith($"/events/{eventId}/edit", StringComparison.Ordinal)).IsTrue();
    }

    [Test]
    public async Task SaveSessionAsync_WhenEndTimeIsBeforeStartTime_DoesNotCallApi()
    {
        var eventId = Guid.NewGuid();
        _eventService.GetEventByIdAsync(eventId).Returns(CreateDraftEvent(eventId, Guid.NewGuid()));

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

    [Test]
    public async Task SaveSessionAsync_WhenCapacityIsZero_NormalizesCapacityBeforeCreate()
    {
        var eventId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        _eventService.GetEventByIdAsync(eventId).Returns(CreateDraftEvent(eventId, tenantId));
        _eventService.CreateSessionAsync(Arg.Any<CreateEventSessionDto>()).Returns(new BaseCommandResponseOfGuid
        {
            Success = true,
            Id = Guid.NewGuid()
        });

        var cut = _ctx.Render<CreateSession>(parameters => parameters.Add(component => component.EventId, eventId));
        var session = GetPrivateField<CreateEventSessionDto>(cut.Instance, "_session");
        session.Title = "Opening talk";
        session.MaxAudienceAttendees = 0;

        await InvokePrivateAsync(cut.Instance, "SaveSessionAsync");

        await _eventService.Received(1).CreateSessionAsync(Arg.Is<CreateEventSessionDto>(dto => dto.MaxAudienceAttendees == null));
    }

    [Test]
    public async Task SaveSessionAsync_WhenAddSessionLinkIsMissing_DoesNotCallApi()
    {
        var eventId = Guid.NewGuid();
        _eventService.GetEventByIdAsync(eventId).Returns(CreateDraftEvent(eventId, Guid.NewGuid(), canAddSession: false));

        var cut = _ctx.Render<CreateSession>(parameters => parameters.Add(component => component.EventId, eventId));
        var session = GetPrivateField<CreateEventSessionDto>(cut.Instance, "_session");
        session.Title = "Opening talk";

        await InvokePrivateAsync(cut.Instance, "SaveSessionAsync");

        await _eventService.DidNotReceive().CreateSessionAsync(Arg.Any<CreateEventSessionDto>());
        var errorMessage = GetPrivateField<string?>(cut.Instance, "_errorMessage");
        await Assert.That(errorMessage).IsEqualTo("You do not currently have permission to add sessions to this event draft.");
    }

    public void Dispose() => _ctx.Dispose();

    private static EventDto CreateDraftEvent(Guid eventId, Guid tenantId, bool canAddSession = true)
    {
        var dto = new EventDto
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
            EventFormatMasterCode = "IN_PERSON",
            AdditionalProperties = new Dictionary<string, object>()
        };

        if (canAddSession)
        {
            using var doc = JsonDocument.Parse(
                "{\"self\":{\"href\":\"/api/event/1\"},\"add-session\":{\"href\":\"/api/event-session\",\"method\":\"POST\"}}");
            dto.AdditionalProperties["_links"] = doc.RootElement.Clone();
        }

        return dto;
    }

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
