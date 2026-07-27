// ABOUTME: RealRuntime API tests for registration creation, promotion, cancellation, replay, and authorization.
// ABOUTME: Verifies PostgreSQL-backed parent transitions and atomic recipient-notification delivery graphs.

using System.Net;
using System.Net.Http.Json;
using Event.Api.IntegrationTests.Builders;
using Event.Api.IntegrationTests.Fixtures;
using Event.Api.IntegrationTests.Seeds;
using Explore.Application.DTOs.EventRegistration;
using Explore.Application.Models.Common;
using Explore.Application.Responses;
using Explore.Application.Services;
using Explore.Domain;
using Explore.Domain.Enums;
using Explore.Domain.Services.Scheduling;
using Explore.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Event.Api.IntegrationTests.Features;

[ClassDataSource<RealRuntimeApiFixture>(Shared = SharedType.PerAssembly)]
[NotInParallel("RealRuntimeDb")]
public sealed class EventRegistrationRealRuntimeTests(RealRuntimeApiFixture fixture)
{
    private readonly RealRuntimeApiFixture _fixture = fixture;

    [Test]
    public async Task Create_WhenSameSessionSelectionIsSubmittedTwice_PersistsOneIntentAndOneOutbox()
    {
        await _fixture.ResetDatabaseAsync();

        RegistrationScenario scenario;
        await using (var scope = _fixture.Factory.Services.CreateAsyncScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ExploreDbContext>();
            scenario = await SeedSessionSelectionRegistrationScenarioAsync(context);
        }

        var dto = new CreateEventRegistrationDto
        {
            EventId = scenario.EventId,
            RegistrationScopeId = (int)RegistrationScopeEnum.SessionSelection,
            SelectedSessionIds = [scenario.SessionId]
        };

        using var firstRequest = _fixture.CreateAuthenticatedRequest(
            HttpMethod.Post,
            "/api/eventregistration",
            scenario.UserId);
        firstRequest.Content = JsonContent.Create(dto);

        var firstResponse = await _fixture.Client.SendAsync(firstRequest);
        var firstBody = await firstResponse.Content.ReadFromJsonAsync<BaseCommandResponse<Guid>>();

        using var secondRequest = _fixture.CreateAuthenticatedRequest(
            HttpMethod.Post,
            "/api/eventregistration",
            scenario.UserId);
        secondRequest.Content = JsonContent.Create(dto);

        var secondResponse = await _fixture.Client.SendAsync(secondRequest);
        var secondBody = await secondResponse.Content.ReadFromJsonAsync<BaseCommandResponse<Guid>>();

        await Assert.That(firstResponse.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(firstBody).IsNotNull();
        await Assert.That(firstBody!.Success).IsTrue();
        await Assert.That(firstBody.Id).IsNotEqualTo(Guid.Empty);
        await Assert.That(firstBody.Message).IsEqualTo("Event Registration created successfully.");

        await Assert.That(secondResponse.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(secondBody).IsNotNull();
        await Assert.That(secondBody!.Success).IsTrue();
        await Assert.That(secondBody.Id).IsEqualTo(firstBody.Id);
        await Assert.That(secondBody.Message).IsEqualTo("Event Registration already exists.");

        await using var verifyScope = _fixture.Factory.Services.CreateAsyncScope();
        var verifyContext = verifyScope.ServiceProvider.GetRequiredService<ExploreDbContext>();

        var intentCount = await verifyContext.EventRegistrationIntents
            .IgnoreQueryFilters()
            .CountAsync(intent => intent.EventId == scenario.EventId
                && intent.UserId == scenario.UserId
                && intent.RegistrationScopeId == (int)RegistrationScopeEnum.SessionSelection);

        var registrationCount = await verifyContext.EventRegistrations
            .IgnoreQueryFilters()
            .CountAsync(registration => registration.EventId == scenario.EventId
                && registration.UserId == scenario.UserId
                && registration.EventSessionId == scenario.SessionId);

        var outboxRows = await verifyContext.EmailDispatchOutbox
            .IgnoreQueryFilters()
            .Where(outbox => outbox.EventId == scenario.EventId
                && outbox.RecipientUserId == scenario.UserId
                && outbox.Kind == EmailDispatchKind.RegistrationConfirmation)
            .ToListAsync();

        var sessionAttendees = await verifyContext.EventSessions
            .IgnoreQueryFilters()
            .Where(session => session.Id == scenario.SessionId)
            .Select(session => session.CurrentAudienceAttendees)
            .SingleAsync();

        await Assert.That(intentCount).IsEqualTo(1);
        await Assert.That(registrationCount).IsEqualTo(1);
        await Assert.That(outboxRows.Count).IsEqualTo(1);

        var outbox = outboxRows.Single();
        await Assert.That(outbox.SourceType).IsEqualTo(EventLifecycleEmailOutboxFactory.RegistrationIntentSourceType);
        await Assert.That(outbox.SourceId).IsEqualTo(firstBody.Id);
        await Assert.That(outbox.RegistrationIntentId).IsEqualTo(firstBody.Id);
        await Assert.That(outbox.CorrelationId).IsEqualTo(firstBody.Id.ToString());
        await Assert.That(sessionAttendees).IsEqualTo(1);
    }

    [Test]
    public async Task Create_WhenSelectedSessionIsFull_ReturnsWaitlistAndDoesNotIncrementCapacity()
    {
        await _fixture.ResetDatabaseAsync();

        RegistrationScenario scenario;
        await using (var scope = _fixture.Factory.Services.CreateAsyncScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ExploreDbContext>();
            scenario = await SeedSessionSelectionRegistrationScenarioAsync(
                context,
                "Full Session Registration Event",
                maxAudienceAttendees: 1,
                currentAudienceAttendees: 1);
        }

        using var request = _fixture.CreateAuthenticatedRequest(
            HttpMethod.Post,
            "/api/eventregistration",
            scenario.UserId);
        request.Content = JsonContent.Create(CreateSessionSelectionDto(scenario));

        var response = await _fixture.Client.SendAsync(request);
        var body = await response.Content.ReadFromJsonAsync<BaseCommandResponse<Guid>>();

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(body).IsNotNull();
        await Assert.That(body!.Success).IsTrue();
        await Assert.That(body.Id).IsNotEqualTo(Guid.Empty);
        await Assert.That(body.Message).IsEqualTo("Event Registration added to the waitlist.");

        await using var verifyScope = _fixture.Factory.Services.CreateAsyncScope();
        var verifyContext = verifyScope.ServiceProvider.GetRequiredService<ExploreDbContext>();

        var persisted = await verifyContext.EventRegistrations
            .IgnoreQueryFilters()
            .Where(registration => registration.EventId == scenario.EventId
                && registration.UserId == scenario.UserId
                && registration.EventSessionId == scenario.SessionId)
            .Select(registration => new
            {
                registration.ApprovalStatusId,
                registration.EventRegistrationIntentId
            })
            .SingleAsync();

        var intentStatus = await verifyContext.EventRegistrationIntents
            .IgnoreQueryFilters()
            .Where(intent => intent.Id == persisted.EventRegistrationIntentId)
            .Select(intent => intent.ApprovalStatusId)
            .SingleAsync();

        var sessionAttendees = await verifyContext.EventSessions
            .IgnoreQueryFilters()
            .Where(session => session.Id == scenario.SessionId)
            .Select(session => session.CurrentAudienceAttendees)
            .SingleAsync();

        var outboxCount = await verifyContext.EmailDispatchOutbox
            .IgnoreQueryFilters()
            .CountAsync(outbox => outbox.EventId == scenario.EventId
                && outbox.RecipientUserId == scenario.UserId
                && outbox.Kind == EmailDispatchKind.RegistrationConfirmation);

        await Assert.That(persisted.ApprovalStatusId).IsEqualTo((int)ApprovalStatusEnum.Waitlisted);
        await Assert.That(intentStatus).IsEqualTo((int)ApprovalStatusEnum.Waitlisted);
        await Assert.That(sessionAttendees).IsEqualTo(1);
        await Assert.That(outboxCount).IsEqualTo(1);
    }

    [Test]
    public async Task Create_WhenRequestIsUnauthenticated_ReturnsUnauthorizedAndDoesNotPersistRegistration()
    {
        await _fixture.ResetDatabaseAsync();

        RegistrationScenario scenario;
        await using (var scope = _fixture.Factory.Services.CreateAsyncScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ExploreDbContext>();
            scenario = await SeedSessionSelectionRegistrationScenarioAsync(
                context,
                "Unauthenticated Registration Event",
                maxAudienceAttendees: 5,
                currentAudienceAttendees: 0);
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/eventregistration")
        {
            Content = JsonContent.Create(CreateSessionSelectionDto(scenario))
        };

        var response = await _fixture.Client.SendAsync(request);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);

        await using var verifyScope = _fixture.Factory.Services.CreateAsyncScope();
        var verifyContext = verifyScope.ServiceProvider.GetRequiredService<ExploreDbContext>();

        var intentCount = await verifyContext.EventRegistrationIntents
            .IgnoreQueryFilters()
            .CountAsync(intent => intent.EventId == scenario.EventId);

        var registrationCount = await verifyContext.EventRegistrations
            .IgnoreQueryFilters()
            .CountAsync(registration => registration.EventId == scenario.EventId);

        var outboxCount = await verifyContext.EmailDispatchOutbox
            .IgnoreQueryFilters()
            .CountAsync(outbox => outbox.EventId == scenario.EventId);

        await Assert.That(intentCount).IsEqualTo(0);
        await Assert.That(registrationCount).IsEqualTo(0);
        await Assert.That(outboxCount).IsEqualTo(0);
    }

    [Test]
    public async Task Update_WhenWaitlistedRegistrationIsPromoted_PersistsOnePromotionGraphAndReplayIsSilent()
    {
        await _fixture.ResetDatabaseAsync();

        RegistrationScenario scenario;
        await using (var scope = _fixture.Factory.Services.CreateAsyncScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ExploreDbContext>();
            scenario = await SeedSessionSelectionRegistrationScenarioAsync(
                context,
                "Waitlist Promotion Event",
                maxAudienceAttendees: 1,
                currentAudienceAttendees: 1);
        }

        using (var createRequest = _fixture.CreateAuthenticatedRequest(
                   HttpMethod.Post,
                   "/api/eventregistration",
                   scenario.UserId))
        {
            createRequest.Content = JsonContent.Create(CreateSessionSelectionDto(scenario));
            var createResponse = await _fixture.Client.SendAsync(createRequest);
            await Assert.That(createResponse.StatusCode).IsEqualTo(HttpStatusCode.OK);
        }

        Guid registrationId;
        Guid concurrencyStamp;
        await using (var scope = _fixture.Factory.Services.CreateAsyncScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ExploreDbContext>();
            var registration = await context.EventRegistrations
                .IgnoreQueryFilters()
                .SingleAsync(value => value.EventId == scenario.EventId && !value.IsDeleted);
            registrationId = registration.Id;
            concurrencyStamp = registration.ConcurrencyStamp;
            await context.EventSessions
                .Where(value => value.Id == scenario.SessionId)
                .ExecuteUpdateAsync(update => update.SetProperty(value => value.CurrentAudienceAttendees, 0));
        }

        using var promoteRequest = _fixture.CreateAuthenticatedRequest(
            HttpMethod.Patch,
            $"/api/eventregistration/{registrationId}",
            scenario.UserId);
        promoteRequest.Headers.TryAddWithoutValidation("If-Match", $"\"{concurrencyStamp:D}\"");
        promoteRequest.Content = JsonContent.Create(new UpdateEventRegistrationDto
        {
            ApprovalStatus = new UpdateEventRegistrationApprovalStatusDto
            {
                ApprovalStatusId = OptionalUpdate<int?>.Set((int)ApprovalStatusEnum.Approved)
            }
        });
        var promoteResponse = await _fixture.Client.SendAsync(promoteRequest);
        var promoteBody = await promoteResponse.Content.ReadFromJsonAsync<BaseCommandResponse<Guid>>();

        await Assert.That(promoteResponse.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(promoteBody).IsNotNull();
        await Assert.That(promoteBody!.Success).IsTrue();
        await Assert.That(promoteBody.Id).IsNotEqualTo(Guid.Empty);

        Guid promotedConcurrencyStamp;
        await using (var scope = _fixture.Factory.Services.CreateAsyncScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ExploreDbContext>();
            promotedConcurrencyStamp = await context.EventRegistrations
                .IgnoreQueryFilters()
                .Where(value => value.Id == promoteBody.Id)
                .Select(value => value.ConcurrencyStamp)
                .SingleAsync();
        }

        using var replayRequest = _fixture.CreateAuthenticatedRequest(
            HttpMethod.Patch,
            $"/api/eventregistration/{promoteBody.Id}",
            scenario.UserId);
        replayRequest.Headers.TryAddWithoutValidation("If-Match", $"\"{promotedConcurrencyStamp:D}\"");
        replayRequest.Content = JsonContent.Create(new UpdateEventRegistrationDto
        {
            ApprovalStatus = new UpdateEventRegistrationApprovalStatusDto
            {
                ApprovalStatusId = OptionalUpdate<int?>.Set((int)ApprovalStatusEnum.Approved)
            }
        });
        var replayResponse = await _fixture.Client.SendAsync(replayRequest);

        await Assert.That(replayResponse.StatusCode).IsEqualTo(HttpStatusCode.OK);

        await using var verifyScope = _fixture.Factory.Services.CreateAsyncScope();
        var verifyContext = verifyScope.ServiceProvider.GetRequiredService<ExploreDbContext>();
        var promotionOutboxCount = await verifyContext.EmailDispatchOutbox
            .IgnoreQueryFilters()
            .CountAsync(value => value.EventId == scenario.EventId
                && value.RecipientUserId == scenario.UserId
                && value.Kind == EmailDispatchKind.WaitlistPromoted);
        var promotionIntentCount = await verifyContext.NotificationIntents
            .IgnoreQueryFilters()
            .CountAsync(value => value.EventId == scenario.EventId
                && value.RecipientUserId == scenario.UserId
                && value.TemplateKey == "registration.waitlist-promoted");

        await Assert.That(promotionOutboxCount).IsEqualTo(1);
        await Assert.That(promotionIntentCount).IsEqualTo(1);
    }

    [Test]
    public async Task Delete_WhenAttendeeCancels_PersistsOneCancellationGraphAndReplayCreatesNothing()
    {
        await _fixture.ResetDatabaseAsync();

        RegistrationScenario scenario;
        await using (var scope = _fixture.Factory.Services.CreateAsyncScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ExploreDbContext>();
            scenario = await SeedSessionSelectionRegistrationScenarioAsync(
                context,
                "Registration Cancellation Event");
        }

        using (var createRequest = _fixture.CreateAuthenticatedRequest(
                   HttpMethod.Post,
                   "/api/eventregistration",
                   scenario.UserId))
        {
            createRequest.Content = JsonContent.Create(CreateSessionSelectionDto(scenario));
            var createResponse = await _fixture.Client.SendAsync(createRequest);
            await Assert.That(createResponse.StatusCode).IsEqualTo(HttpStatusCode.OK);
        }

        Guid registrationId;
        await using (var scope = _fixture.Factory.Services.CreateAsyncScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ExploreDbContext>();
            registrationId = await context.EventRegistrations
                .IgnoreQueryFilters()
                .Where(value => value.EventId == scenario.EventId && !value.IsDeleted)
                .Select(value => value.Id)
                .SingleAsync();
        }

        using var cancelRequest = _fixture.CreateAuthenticatedRequest(
            HttpMethod.Delete,
            $"/api/eventregistration/{registrationId}",
            scenario.UserId);
        var cancelResponse = await _fixture.Client.SendAsync(cancelRequest);
        using var replayRequest = _fixture.CreateAuthenticatedRequest(
            HttpMethod.Delete,
            $"/api/eventregistration/{registrationId}",
            scenario.UserId);
        var replayResponse = await _fixture.Client.SendAsync(replayRequest);

        await Assert.That(cancelResponse.StatusCode).IsEqualTo(HttpStatusCode.NoContent);
        await Assert.That(replayResponse.StatusCode).IsEqualTo(HttpStatusCode.Forbidden);

        await using var verifyScope = _fixture.Factory.Services.CreateAsyncScope();
        var verifyContext = verifyScope.ServiceProvider.GetRequiredService<ExploreDbContext>();
        var cancellationOutboxCount = await verifyContext.EmailDispatchOutbox
            .IgnoreQueryFilters()
            .CountAsync(value => value.EventId == scenario.EventId
                && value.RecipientUserId == scenario.UserId
                && value.Kind == EmailDispatchKind.RegistrationCancelled);
        var cancellationIntentCount = await verifyContext.NotificationIntents
            .IgnoreQueryFilters()
            .CountAsync(value => value.EventId == scenario.EventId
                && value.RecipientUserId == scenario.UserId
                && value.TemplateKey == "registration.cancelled");

        await Assert.That(cancellationOutboxCount).IsEqualTo(1);
        await Assert.That(cancellationIntentCount).IsEqualTo(1);
    }

    private static CreateEventRegistrationDto CreateSessionSelectionDto(RegistrationScenario scenario) => new()
    {
        EventId = scenario.EventId,
        RegistrationScopeId = (int)RegistrationScopeEnum.SessionSelection,
        SelectedSessionIds = [scenario.SessionId]
    };

    private static async Task<RegistrationScenario> SeedSessionSelectionRegistrationScenarioAsync(
        ExploreDbContext context,
        string title = "Repeat Submit Registration Event",
        int? maxAudienceAttendees = 5,
        int currentAudienceAttendees = 0)
    {
        var tenant = await TenantScenarioSeed.SeedActiveTenantWithUserAsync(context);
        var user = await context.Users.FindAsync([tenant.UserId], CancellationToken.None);
        user!.EmailVerified = true;
        var startsAt = DateTimeOffset.UtcNow.AddDays(14);
        var localDate = DateOnly.FromDateTime(startsAt.UtcDateTime);

        var @event = new EventBuilder()
            .WithTitle(title)
            .WithActorId(tenant.ActorId)
            .WithTenantId(tenant.TenantId)
            .WithStatus(EventStatusEnum.Published)
            .WithVisibility(VisibilityTypeEnum.Public)
            .WithSessionDates(localDate, localDate)
            .Build();

        @event.ParticipationConfiguration = EventParticipationConfiguration.Create(
            @event.Id,
            tenant.TenantId,
            (int)ParticipationHandlingModeEnum.PlatformManaged,
            (int)AdvanceRegistrationObligationEnum.Required,
            (int)IdentityAccessModeEnum.AccountRequired,
            guestRecoveryPolicy: null,
            DateTime.UtcNow);
        @event.RegistrationPolicyId = (int)EventRegistrationPolicyEnum.SessionSelectionOnly;

        var day = new EventDay
        {
            Id = Guid.CreateVersion7(),
            EventId = @event.Id,
            Event = @event,
            LocalDate = localDate,
            Label = "Day 1",
            IsPublished = true,
            SortOrder = 1,
            AllowsDayScopeRegistration = true,
            TenantId = tenant.TenantId,
            Tenant = null!,
            CreatedAt = DateTime.UtcNow,
            ConcurrencyStamp = Guid.CreateVersion7()
        };

        var session = new EventSession
        {
            Id = Guid.CreateVersion7(),
            EventId = @event.Id,
            Event = @event,
            EventDayId = day.Id,
            EventDay = day,
            TenantId = tenant.TenantId,
            Tenant = null!,
            Title = "Repeat Submit Session",
            EventSessionStatusId = (int)EventSessionStatusEnum.Published,
            SortOrder = 1,
            EventSessionKindId = (int)EventSessionKindEnum.Talk,
            RegistrationModeId = (int)RegistrationModeEnum.Open,
            MaxAudienceAttendees = maxAudienceAttendees,
            CurrentAudienceAttendees = currentAudienceAttendees,
            ConcurrencyStamp = Guid.CreateVersion7()
        };
        session.Reschedule(
            startsAt,
            startsAt.AddHours(1),
            "UTC",
            new EventScheduleProjectionCalculator());

        @event.Days.Add(day);
        @event.Sessions.Add(session);
        @event.RecalculateScheduleSummaryFromSessions();

        context.Events.Add(@event);
        await context.SaveChangesAsync();

        return new RegistrationScenario(tenant.TenantId, tenant.UserId, @event.Id, session.Id);
    }

    private sealed record RegistrationScenario(Guid TenantId, Guid UserId, Guid EventId, Guid SessionId);
}
