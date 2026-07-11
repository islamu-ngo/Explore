// ABOUTME: API-driven registration journey seed for Playwright E2E coverage.
// ABOUTME: Creates and publishes a registration-ready event and session through generated contracts.

using Explore.Blazor.Client.Clients;

namespace Explore.Blazor.Client.E2ETests.Seeds;

public static class RegistrationScenarioSeed
{
    public sealed record Result(
        Guid TenantId,
        string TenantSlug,
        Guid EventId,
        Guid SessionId,
        string EventTitle,
        string SessionTitle);

    public static async Task<Result> SeedAsync(IEventApiClient api)
    {
        var tenant = (await api.GetTenantsAsync()).Single(candidate => candidate.IsActive == true);
        var policies = await api.GetEventRegistrationPoliciesAsync();
        var policyId = EventApiScenario.FindLookup(
            policies,
            value => value.MasterCode,
            value => value.Id,
            "session_selection_only");
        var eventTitle = $"Registration E2E Event {Guid.NewGuid():N}";
        var eventId = await EventApiScenario.CreateDraftEventAsync(
            api,
            eventTitle,
            $"registration-e2e-{Guid.NewGuid():N}",
            registrationRequired: true,
            registrationPolicyId: policyId);

        var sessionKinds = await api.GetEventSessionKindsAsync();
        var registrationModes = await api.GetRegistrationModesAsync();
        var start = DateTimeOffset.UtcNow.AddDays(14).AddHours(2);
        var sessionTitle = "Registration E2E Session";
        var sessionResponse = await api.CreateEventSessionAsync(new CreateEventSessionDto
        {
            EventId = eventId,
            Title = sessionTitle,
            Slug = $"registration-session-{Guid.NewGuid():N}",
            Description = "Open future registration session created through the API.",
            StartTime = start,
            EndTime = start.AddHours(2),
            EndTimeType = SessionEndTimeType.Fixed,
            SortOrder = 1,
            EventSessionKindId = EventApiScenario.FindLookup(
                sessionKinds,
                value => value.MasterCode,
                value => value.Id,
                "talk"),
            RegistrationModeId = EventApiScenario.FindLookup(
                registrationModes,
                value => value.MasterCode,
                value => value.Id,
                "open"),
            MaxAudienceAttendees = 50
        });
        var sessionId = EventApiScenario.SuccessfulId(sessionResponse, "creating the E2E event session");
        var session = await api.GetEventSessionByIdAsync(sessionId);
        EventApiScenario.EnsureSuccess(
            await api.PublishEventSessionAsync(sessionId, new PublishEventSessionRequestDto
            {
                ExpectedConcurrencyStamp = session.ConcurrencyStamp
            }),
            "publishing the E2E event session");
        await EventApiScenario.PublishEventAsync(api, eventId);

        return new Result(
            tenant.Id ?? throw new InvalidOperationException("The API tenant did not expose an id."),
            tenant.Slug,
            eventId,
            sessionId,
            eventTitle,
            sessionTitle);
    }
}
