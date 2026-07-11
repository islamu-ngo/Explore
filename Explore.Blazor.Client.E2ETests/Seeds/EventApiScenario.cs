// ABOUTME: Generated-client helpers for creating realistic E2E event data through the public API.
// ABOUTME: Resolves lookup identifiers from API contracts before creating and publishing events.

using Explore.Blazor.Client.Clients;

namespace Explore.Blazor.Client.E2ETests.Seeds;

internal static class EventApiScenario
{
    public static async Task<Guid> CreatePublishedEventAsync(
        IEventApiClient api,
        string title,
        string slug,
        bool registrationRequired = false,
        int? registrationPolicyId = null)
    {
        var eventId = await CreateDraftEventAsync(
            api,
            title,
            slug,
            registrationRequired,
            registrationPolicyId);
        await CreatePublishedSessionAsync(api, eventId, title, slug);
        await PublishEventAsync(api, eventId);
        return eventId;
    }

    public static async Task<Guid> CreateDraftEventAsync(
        IEventApiClient api,
        string title,
        string slug,
        bool registrationRequired = false,
        int? registrationPolicyId = null)
    {
        var formats = await api.GetEventFormatOptionsAsync();
        var visibilities = await api.GetVisibilityTypesAsync();
        var response = await api.CreateEventAsync(new CreateEventDraftRequestDto
        {
            Title = title,
            Slug = slug,
            Description = $"E2E event created through the API for {title}.",
            EventFormatId = FindLookup(formats, value => value.MasterCode, value => value.Id, "local"),
            VisibilityTypeId = FindLookup(visibilities, value => value.MasterCode, value => value.Id, "public"),
            Timezone = "Europe/Brussels",
            EventTimeZoneId = "Europe/Brussels",
            IsRegistrationRequired = registrationRequired,
            RegistrationPolicyId = registrationPolicyId
        });
        return SuccessfulId(response, "creating an E2E event");
    }

    public static async Task PublishEventAsync(IEventApiClient api, Guid eventId)
    {
        var created = await api.GetEventByIdAsync(eventId);
        EnsureSuccess(
            await api.PublishEventAsync(eventId, new PublishEventRequestDto
            {
                ExpectedConcurrencyStamp = created.ConcurrencyStamp
            }),
            "publishing an E2E event");
    }

    private static async Task CreatePublishedSessionAsync(
        IEventApiClient api,
        Guid eventId,
        string eventTitle,
        string eventSlug)
    {
        var sessionKinds = await api.GetEventSessionKindsAsync();
        var registrationModes = await api.GetRegistrationModesAsync();
        var start = DateTimeOffset.UtcNow.AddDays(14);
        var sessionId = SuccessfulId(
            await api.CreateEventSessionAsync(new CreateEventSessionDto
            {
                EventId = eventId,
                Title = $"{eventTitle} Session",
                Slug = $"{eventSlug}-session",
                Description = $"Scheduled session for {eventTitle}.",
                StartTime = start,
                EndTime = start.AddHours(2),
                EndTimeType = SessionEndTimeType.Fixed,
                SortOrder = 1,
                EventSessionKindId = FindLookup(
                    sessionKinds,
                    value => value.MasterCode,
                    value => value.Id,
                    "talk"),
                RegistrationModeId = FindLookup(
                    registrationModes,
                    value => value.MasterCode,
                    value => value.Id,
                    "open")
            }),
            "creating an E2E event session");
        var session = await api.GetEventSessionByIdAsync(sessionId);
        EnsureSuccess(
            await api.PublishEventSessionAsync(sessionId, new PublishEventSessionRequestDto
            {
                ExpectedConcurrencyStamp = session.ConcurrencyStamp
            }),
            "publishing an E2E event session");
    }

    public static int FindLookup<T>(
        IEnumerable<T> values,
        Func<T, string> getMasterCode,
        Func<T, int?> getId,
        string masterCode)
    {
        foreach (var value in values)
        {
            if (string.Equals(getMasterCode(value), masterCode, StringComparison.OrdinalIgnoreCase))
            {
                return getId(value)
                    ?? throw new InvalidOperationException($"Lookup '{masterCode}' did not expose an id.");
            }
        }

        throw new InvalidOperationException($"Lookup '{masterCode}' was not returned by the API.");
    }

    public static Guid SuccessfulId(BaseCommandResponseOfGuid response, string operation)
    {
        EnsureSuccess(response, operation);
        return response.Id is { } id && id != Guid.Empty
            ? id
            : throw new InvalidOperationException($"API returned no id while {operation}.");
    }

    public static void EnsureSuccess(BaseCommandResponseOfGuid response, string operation)
    {
        if (response.Success != true)
        {
            throw new InvalidOperationException($"API failed while {operation}: {response.Message}");
        }
    }
}
