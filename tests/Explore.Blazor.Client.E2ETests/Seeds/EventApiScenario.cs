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
        int? registrationPolicyId = null,
        int startsInDays = 14)
    {
        var formats = await api.GetEventFormatOptionsAsync();
        var visibilities = await api.GetVisibilityTypesAsync();
        var sessionKinds = await api.GetEventSessionKindsAsync();
        var registrationModes = await api.GetRegistrationModesAsync();
        var start = DateTimeOffset.UtcNow.AddDays(startsInDays);

        return SuccessfulId(
            await api.CreateEventAsync(new CreateEventDraftRequestDto
            {
                Title = title,
                Slug = slug,
                Description = $"E2E event created through the API for {title}.",
                EventFormatId = FindLookup(formats, value => value.MasterCode, value => value.Id, "local"),
                VisibilityTypeId = FindLookup(visibilities, value => value.MasterCode, value => value.Id, "public"),
                EventStatusId = 2,
                Timezone = "Europe/Brussels",
                EventTimeZoneId = "Europe/Brussels",
                IsRegistrationRequired = registrationRequired,
                RegistrationPolicyId = registrationPolicyId,
                CategoryIds = [],
                TagIds = [],
                Locations = [],
                Sessions =
                [
                    new CreateEventSessionRequest
                    {
                        Title = $"{title} Session",
                        Slug = $"{slug}-session",
                        Description = $"Scheduled session for {title}.",
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
                            "open"),
                        LanguageIds = [],
                        SpeakerActorIds = []
                    }
                ],
                Days = [],
                Rooms = [],
                AgendaItems = []
            }),
            "creating a published E2E event");
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
        BaseCommandResponseOfGuid response;
        try
        {
            response = await api.CreateEventAsync(new CreateEventDraftRequestDto
            {
                Title = title,
                Slug = slug,
                Description = $"E2E event created through the API for {title}.",
                EventFormatId = FindLookup(formats, value => value.MasterCode, value => value.Id, "local"),
                VisibilityTypeId = FindLookup(visibilities, value => value.MasterCode, value => value.Id, "public"),
                EventStatusId = 1,
                Timezone = "Europe/Brussels",
                EventTimeZoneId = "Europe/Brussels",
                IsRegistrationRequired = registrationRequired,
                RegistrationPolicyId = registrationPolicyId,
                CategoryIds = [],
                TagIds = [],
                Locations = [],
                Sessions = [],
                Days = [],
                Rooms = [],
                AgendaItems = []
            });
        }
        catch (ApiException<ValidationProblemDetails> exception)
        {
            var errors = exception.Result.Errors?
                .SelectMany(pair => pair.Value.Select(message => $"{pair.Key}: {message}"))
                .ToArray() ?? [];
            throw new InvalidOperationException(
                "API rejected the E2E event draft. " +
                $"Title={exception.Result.Title}. Detail={exception.Result.Detail}. " +
                $"Errors={string.Join(" | ", errors)}",
                exception);
        }
        catch (ApiException<ProblemDetails> exception)
        {
            throw new InvalidOperationException(
                "API denied the E2E event draft. " +
                $"Status={exception.StatusCode}. Title={exception.Result.Title}. " +
                $"Detail={exception.Result.Detail}. Type={exception.Result.Type}.",
                exception);
        }

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
