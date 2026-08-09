// ABOUTME: Exercises authenticated and capability-scoped native registration submission routes through the HTTP host.
// ABOUTME: Verifies authorization, safe validation problems, token transport, and HAL-owned affordance contracts.

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Event.Api.IntegrationTests.Fixtures;
using Explore.API.Services;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Services.Registration;
using Explore.Application.Contracts.Webhooks;
using Explore.Application.DTOs.RegistrationOrders;
using Explore.Application.DTOs.RegistrationSubmissions;
using Explore.Application.Features.RegistrationOrders.Requests.Commands;
using Explore.Application.Features.RegistrationSubmissions.Commands;
using Explore.Domain;
using Explore.Domain.Constants;
using Explore.Domain.Enums;
using Explore.Persistence;
using MediatR;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using NSubstitute;

namespace Event.Api.IntegrationTests.Features;

[NotInParallel("ApiTestFixture")]
public sealed class NativeRegistrationSubmissionHttpTests
{
    private const string OrderCapabilityHeader = "X-Registration-Order-Capability";
    private const string AttemptCapabilityHeader = "X-Registration-Attempt-Capability";

    [Test]
    public async Task GuestStartReplayRestoresProtectedCapability()
    {
        const string capability = "guest-order-capability";
        var mediator = Substitute.For<IMediator>();
        Guid orderId = Guid.CreateVersion7();
        mediator.Send(Arg.Any<StartGuestRegistrationOrderCommand>(), Arg.Any<CancellationToken>())
            .Returns(new GuestRegistrationOrderStartDto
            {
                Id = orderId,
                Success = true,
                GuestCapabilityToken = capability
            });
        await using WebApplicationFactory<Program> factory = CreateFactory(mediator);
        using HttpClient client = factory.CreateClient();
        Guid eventId = Guid.CreateVersion7();
        string idempotencyKey = Guid.CreateVersion7().ToString("D");
        object body = new
        {
            ticketCatalogVersionId = Guid.CreateVersion7(),
            bookingPartyType = "Individual",
            lines = Array.Empty<object>()
        };

        using HttpResponseMessage first = await PostAsync(
            client, $"/api/events/{eventId:D}/registration-orders/guest", body, false,
            idempotencyKey: idempotencyKey);
        using HttpResponseMessage replay = await PostAsync(
            client, $"/api/events/{eventId:D}/registration-orders/guest", body, false,
            idempotencyKey: idempotencyKey);

        await Assert.That(first.StatusCode).IsEqualTo(HttpStatusCode.Created);
        await Assert.That(replay.StatusCode).IsEqualTo(HttpStatusCode.Created);
        await Assert.That(first.Headers.GetValues(OrderCapabilityHeader).Single()).IsEqualTo(capability);
        await Assert.That(replay.Headers.GetValues(OrderCapabilityHeader).Single()).IsEqualTo(capability);
        await Assert.That(replay.Headers.GetValues("X-Idempotency-Replay").Single()).IsEqualTo("true");
        await mediator.Received(1).Send(Arg.Any<StartGuestRegistrationOrderCommand>(), Arg.Any<CancellationToken>());

        using IServiceScope scope = factory.Services.CreateScope();
        ExploreDbContext db = scope.ServiceProvider.GetRequiredService<ExploreDbContext>();
        IdempotencyRecord record = await db.IdempotencyRecords.SingleAsync(item => item.Key == idempotencyKey);
        await Assert.That(record.ResponseBody!.StartsWith("dp:v1:", StringComparison.Ordinal)).IsTrue();
        await Assert.That(record.ResponseBody.Contains(capability, StringComparison.Ordinal)).IsFalse();
    }

    [Test]
    public async Task AuthenticatedLaunchRequiresAuthenticationAndReturnsBoundedAttemptCapability()
    {
        var mediator = Substitute.For<IMediator>();
        Guid eventId = Guid.CreateVersion7();
        Guid orderId = Guid.CreateVersion7();
        Guid requirementId = Guid.CreateVersion7();
        Guid channelId = Guid.CreateVersion7();
        Guid formId = Guid.CreateVersion7();
        Guid versionId = Guid.CreateVersion7();
        Guid attemptId = Guid.CreateVersion7();
        const string attemptToken = "attempt-token-secret";
        mediator.Send(Arg.Any<LaunchAuthenticatedNativeRegistrationAttemptCommand>(), Arg.Any<CancellationToken>())
            .Returns(CreateAttemptResult(
                attemptId, requirementId, channelId, formId, versionId, attemptToken));
        await using WebApplicationFactory<Program> factory = CreateFactory(mediator);
        using HttpClient client = factory.CreateClient();
        object body = new { requirementId, channelId, formId, formVersionId = versionId };

        using HttpResponseMessage anonymous = await PostAsync(
            client, $"/api/events/{eventId:D}/registration-orders/{orderId:D}/attempts", body, authenticated: false);
        await Assert.That(anonymous.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);

        using HttpResponseMessage response = await PostAsync(
            client, $"/api/events/{eventId:D}/registration-orders/{orderId:D}/attempts", body, authenticated: true);
        string payload = await response.Content.ReadAsStringAsync();

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Created);
        await Assert.That(response.Headers.GetValues(AttemptCapabilityHeader)).Contains(attemptToken);
        await Assert.That(payload).Contains(attemptToken);
        await Assert.That(payload).Contains("requirement-progress");
        await Assert.That(payload).Contains("submit");
        await Assert.That(payload).Contains("skip");
    }

    [Test]
    public async Task GuestLaunchRequiresMatchingOrderCapability()
    {
        const string validCapability = "valid-order-capability";
        var mediator = Substitute.For<IMediator>();
        mediator.Send(Arg.Any<LaunchGuestNativeRegistrationAttemptCommand>(), Arg.Any<CancellationToken>())
            .Returns(call => call.Arg<LaunchGuestNativeRegistrationAttemptCommand>().CapabilityToken == validCapability
                ? new NativeRegistrationAttemptResult(
                    true, Guid.CreateVersion7(), call.Arg<LaunchGuestNativeRegistrationAttemptCommand>().RequirementId,
                    call.Arg<LaunchGuestNativeRegistrationAttemptCommand>().ChannelId,
                    call.Arg<LaunchGuestNativeRegistrationAttemptCommand>().FormId,
                    call.Arg<LaunchGuestNativeRegistrationAttemptCommand>().FormVersionId,
                    new DateTime(2026, 8, 3, 0, 0, 0, DateTimeKind.Utc),
                    new NativeRegistrationFormDefinitionDto(Guid.CreateVersion7(), 1, "en", "hash", [], []),
                    [], new NativeRegistrationRequirementProgressDto(1, 0, 0, 1, false), true, "attempt-token")
                : new NativeRegistrationAttemptResult(
                    false, Guid.Empty, call.Arg<LaunchGuestNativeRegistrationAttemptCommand>().RequirementId,
                    call.Arg<LaunchGuestNativeRegistrationAttemptCommand>().ChannelId,
                    call.Arg<LaunchGuestNativeRegistrationAttemptCommand>().FormId,
                    call.Arg<LaunchGuestNativeRegistrationAttemptCommand>().FormVersionId,
                    default, null, [], null, false, null, "registration_order_not_found"));
        await using WebApplicationFactory<Program> factory = CreateFactory(mediator);
        using HttpClient client = factory.CreateClient();
        Guid eventId = Guid.CreateVersion7();
        Guid orderId = Guid.CreateVersion7();
        object body = new
        {
            requirementId = Guid.CreateVersion7(),
            channelId = Guid.CreateVersion7(),
            formId = Guid.CreateVersion7(),
            formVersionId = Guid.CreateVersion7()
        };

        using HttpResponseMessage missing = await PostAsync(
            client, $"/api/events/{eventId:D}/registration-orders/guest/{orderId:D}/attempts", body, false);
        using HttpResponseMessage invalid = await PostAsync(
            client, $"/api/events/{eventId:D}/registration-orders/guest/{orderId:D}/attempts", body, false, "invalid");
        using HttpResponseMessage valid = await PostAsync(
            client, $"/api/events/{eventId:D}/registration-orders/guest/{orderId:D}/attempts", body, false, validCapability);

        await Assert.That(missing.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
        await Assert.That(invalid.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
        await Assert.That(valid.StatusCode).IsEqualTo(HttpStatusCode.Created);
    }

    [Test]
    public async Task ValidationProblemContainsOnlyIssueCodesAndFieldKeys()
    {
        var mediator = Substitute.For<IMediator>();
        mediator.Send(Arg.Any<SubmitAuthenticatedNativeRegistrationAttemptCommand>(), Arg.Any<CancellationToken>())
            .Returns(new NativeRegistrationSubmissionResult(
                false, Guid.CreateVersion7(), [new("INVALID_TEXT", "profile.display_name")],
                "registration_submission_invalid"));
        await using WebApplicationFactory<Program> factory = CreateFactory(mediator);
        using HttpClient client = factory.CreateClient();
        Guid eventId = Guid.CreateVersion7();
        Guid orderId = Guid.CreateVersion7();
        Guid attemptId = Guid.CreateVersion7();
        const string rawAnswer = "raw-answer-must-never-leak";
        object body = new
        {
            requirementId = Guid.CreateVersion7(),
            answers = new[]
            {
                new
                {
                    fieldId = Guid.CreateVersion7(),
                    subjectType = RegistrationAnswerSubjectTypeEnum.RegistrationOrder,
                    subjectId = orderId,
                    ticketAssignmentOrderLineId = (Guid?)null,
                    value = JsonSerializer.Deserialize<JsonElement>($"\"{rawAnswer}\"")
                }
            }
        };

        using HttpResponseMessage response = await PostAsync(
            client,
            $"/api/events/{eventId:D}/registration-orders/{orderId:D}/attempts/{attemptId:D}/submissions",
            body,
            authenticated: true,
            attemptCapability: "attempt-token");
        string payload = await response.Content.ReadAsStringAsync();

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
        await Assert.That(payload).Contains("INVALID_TEXT");
        await Assert.That(payload).Contains("profile.display_name");
        await Assert.That(payload).DoesNotContain(rawAnswer);
    }

    [Test]
    public async Task AuthenticatedAndGuestSubmissionsReturnSuccessForValidCapabilities()
    {
        const string orderCapability = "valid-order-capability";
        const string attemptCapability = "valid-attempt-capability";
        var mediator = Substitute.For<IMediator>();
        Guid submissionId = Guid.CreateVersion7();
        mediator.Send(Arg.Any<SubmitAuthenticatedNativeRegistrationAttemptCommand>(), Arg.Any<CancellationToken>())
            .Returns(call => call.Arg<SubmitAuthenticatedNativeRegistrationAttemptCommand>().AttemptCapabilityToken == attemptCapability
                ? new NativeRegistrationSubmissionResult(true, submissionId, [])
                : new NativeRegistrationSubmissionResult(false, Guid.Empty, [], "registration_attempt_not_found"));
        mediator.Send(Arg.Any<SubmitGuestNativeRegistrationAttemptCommand>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                SubmitGuestNativeRegistrationAttemptCommand command = call.Arg<SubmitGuestNativeRegistrationAttemptCommand>();
                return command.CapabilityToken == orderCapability && command.AttemptCapabilityToken == attemptCapability
                    ? new NativeRegistrationSubmissionResult(true, submissionId, [])
                    : new NativeRegistrationSubmissionResult(false, Guid.Empty, [], "registration_attempt_not_found");
            });
        await using WebApplicationFactory<Program> factory = CreateFactory(mediator);
        using HttpClient client = factory.CreateClient();
        Guid eventId = Guid.CreateVersion7();
        Guid orderId = Guid.CreateVersion7();
        Guid attemptId = Guid.CreateVersion7();
        object body = new { requirementId = Guid.CreateVersion7(), answers = Array.Empty<object>() };

        using HttpResponseMessage authenticated = await PostAsync(
            client,
            $"/api/events/{eventId:D}/registration-orders/{orderId:D}/attempts/{attemptId:D}/submissions",
            body,
            authenticated: true,
            attemptCapability: attemptCapability);
        using HttpResponseMessage guest = await PostAsync(
            client,
            $"/api/events/{eventId:D}/registration-orders/guest/{orderId:D}/attempts/{attemptId:D}/submissions",
            body,
            authenticated: false,
            orderCapability: orderCapability,
            attemptCapability: attemptCapability);
        using HttpResponseMessage invalidGuestAttempt = await PostAsync(
            client,
            $"/api/events/{eventId:D}/registration-orders/guest/{orderId:D}/attempts/{attemptId:D}/submissions",
            body,
            authenticated: false,
            orderCapability: orderCapability,
            attemptCapability: "invalid");

        await Assert.That(authenticated.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(guest.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(invalidGuestAttempt.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task AuthenticatedProgressAndLaunchUseRealHandlersAndPersistPinnedAttempt()
    {
        await using var factory = new AuthenticatedWebApplicationFactory();
        using HttpClient client = factory.CreateClient();
        RealNativeFlow scenario = await SeedRealNativeFlowAsync(factory, publishedFormCount: 1);
        Guid eventId = scenario.EventId;
        Guid orderId = scenario.OrderId;
        Guid userId = scenario.UserId;
        Guid requirementId = scenario.RequirementId;
        Guid channelId = scenario.ChannelId;
        Guid formId = scenario.FormId;
        Guid versionId = scenario.VersionId;
        Guid fieldId = scenario.FieldId;

        using var progressRequest = new HttpRequestMessage(
            HttpMethod.Get, $"/api/events/{eventId:D}/registration-orders/{orderId:D}/requirement-progress");
        progressRequest.Headers.Add(TestAuthHandler.AuthHeaderName, TestAuthHandler.CreateAuthHeaderValue(userId));
        using HttpResponseMessage progressResponse = await client.SendAsync(progressRequest);
        string progressPayload = await progressResponse.Content.ReadAsStringAsync();
        using JsonDocument progressJson = JsonDocument.Parse(progressPayload);
        JsonElement descriptor = progressJson.RootElement.GetProperty("requirements")[0];

        await Assert.That(progressResponse.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(descriptor.GetProperty("requirementId").GetGuid()).IsEqualTo(requirementId);
        await Assert.That(descriptor.GetProperty("channelId").GetGuid()).IsEqualTo(channelId);
        await Assert.That(descriptor.GetProperty("formVersionId").GetGuid()).IsEqualTo(versionId);
        await Assert.That(progressPayload).Contains("launch-attempt");
        await Assert.That(progressPayload).Contains($"REGISTRATION_ORDER:{orderId:D}");

        object launchBody = new { requirementId, channelId, formId, formVersionId = versionId };
        string launchIdempotencyKey = Guid.CreateVersion7().ToString("D");
        using HttpResponseMessage launchResponse = await PostAsync(
            client, $"/api/events/{eventId:D}/registration-orders/{orderId:D}/attempts",
            launchBody, authenticated: true, authenticatedUserId: userId,
            idempotencyKey: launchIdempotencyKey);
        string launchPayload = await launchResponse.Content.ReadAsStringAsync();
        using JsonDocument launchJson = JsonDocument.Parse(launchPayload);
        Guid attemptId = launchJson.RootElement.GetProperty("attemptId").GetGuid();
        string rawAttemptCapability = launchJson.RootElement.GetProperty("attemptCapabilityToken").GetString()!;

        await Assert.That(launchResponse.StatusCode).IsEqualTo(HttpStatusCode.Created);
        await Assert.That(rawAttemptCapability).IsNotEmpty();
        await Assert.That(launchJson.RootElement.GetProperty("form").GetProperty("sections")[0]
            .GetProperty("fields")[0].GetProperty("consentText").GetString())
            .IsEqualTo("I agree to receive event updates by email.");
        await Assert.That(launchPayload).Contains("submit");
        await Assert.That(launchPayload).Contains("skip");

        using HttpResponseMessage replayResponse = await PostAsync(
            client, $"/api/events/{eventId:D}/registration-orders/{orderId:D}/attempts",
            launchBody, authenticated: true, authenticatedUserId: userId,
            idempotencyKey: launchIdempotencyKey);
        string replayPayload = await replayResponse.Content.ReadAsStringAsync();
        await Assert.That(replayResponse.StatusCode).IsEqualTo(HttpStatusCode.Created);
        await Assert.That(replayResponse.Headers.GetValues("X-Idempotency-Replay").Single()).IsEqualTo("true");
        await Assert.That(replayResponse.Headers.GetValues(AttemptCapabilityHeader).Single())
            .IsEqualTo(rawAttemptCapability);
        await Assert.That(replayPayload).IsEqualTo(launchPayload);

        using HttpResponseMessage skipResponse = await PostAsync(
            client,
            $"/api/events/{eventId:D}/registration-orders/{orderId:D}/attempts/{attemptId:D}/skip",
            new { requirementId }, authenticated: true, attemptCapability: rawAttemptCapability,
            authenticatedUserId: userId);
        string skipPayload = await skipResponse.Content.ReadAsStringAsync();
        await Assert.That(skipResponse.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(skipPayload).Contains("\"isComplete\":true");

        object wrongLineageBody = new
        {
            requirementId,
            channelId = Guid.CreateVersion7(),
            formId,
            formVersionId = versionId
        };
        using HttpResponseMessage wrongLineageResponse = await PostAsync(
            client, $"/api/events/{eventId:D}/registration-orders/{orderId:D}/attempts",
            wrongLineageBody, authenticated: true, authenticatedUserId: userId);
        string wrongLineagePayload = await wrongLineageResponse.Content.ReadAsStringAsync();
        await Assert.That(wrongLineageResponse.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
        await Assert.That(wrongLineagePayload).DoesNotContain(rawAttemptCapability);
        await Assert.That(wrongLineagePayload).DoesNotContain(fieldId.ToString("D"));

        using (IServiceScope scope = factory.Services.CreateScope())
        {
            ExploreDbContext db = scope.ServiceProvider.GetRequiredService<ExploreDbContext>();
            RegistrationAttempt persisted = await db.RegistrationAttempts.SingleAsync();
            await Assert.That(await db.RegistrationAttempts.CountAsync()).IsEqualTo(1);
            await Assert.That(persisted.RegistrationRequirementId).IsEqualTo(requirementId);
            await Assert.That(persisted.RegistrationChannelId).IsEqualTo(channelId);
            await Assert.That(persisted.RegistrationFormVersionId).IsEqualTo(versionId);
            await Assert.That(persisted.CapabilityTokenHash.Value).IsNotEqualTo(rawAttemptCapability);
            await Assert.That(await db.RegistrationRequirementFulfillments.CountAsync()).IsEqualTo(1);
            IdempotencyRecord replayRecord = await db.IdempotencyRecords.SingleAsync(record =>
                record.Key == launchIdempotencyKey);
            await Assert.That(replayRecord.ResponseBody!.StartsWith("dp:v1:", StringComparison.Ordinal)).IsTrue();
            await Assert.That(replayRecord.ResponseBody.Contains(rawAttemptCapability, StringComparison.Ordinal)).IsFalse();
        }
    }

    [Test]
    public async Task TwoPublishedFormsYieldNoDescriptorAndDirectLaunchFailsClosed()
    {
        await using var factory = new AuthenticatedWebApplicationFactory();
        using HttpClient client = factory.CreateClient();
        RealNativeFlow scenario = await SeedRealNativeFlowAsync(factory, publishedFormCount: 2);

        using var progressRequest = new HttpRequestMessage(HttpMethod.Get,
            $"/api/events/{scenario.EventId:D}/registration-orders/{scenario.OrderId:D}/requirement-progress");
        progressRequest.Headers.Add(TestAuthHandler.AuthHeaderName,
            TestAuthHandler.CreateAuthHeaderValue(scenario.UserId));
        using HttpResponseMessage progressResponse = await client.SendAsync(progressRequest);
        using JsonDocument progressJson = JsonDocument.Parse(await progressResponse.Content.ReadAsStringAsync());
        await Assert.That(progressResponse.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(progressJson.RootElement.GetProperty("requirements").GetArrayLength()).IsEqualTo(0);

        object launchBody = new
        {
            scenario.RequirementId,
            scenario.ChannelId,
            scenario.FormId,
            formVersionId = scenario.VersionId
        };
        using HttpResponseMessage launchResponse = await PostAsync(
            client,
            $"/api/events/{scenario.EventId:D}/registration-orders/{scenario.OrderId:D}/attempts",
            launchBody,
            authenticated: true,
            authenticatedUserId: scenario.UserId);
        await Assert.That(launchResponse.StatusCode).IsEqualTo(HttpStatusCode.NotFound);

        using IServiceScope scope = factory.Services.CreateScope();
        ExploreDbContext db = scope.ServiceProvider.GetRequiredService<ExploreDbContext>();
        await Assert.That(await db.RegistrationAttempts.CountAsync()).IsEqualTo(0);
    }

    [Test]
    public async Task ProviderCallback_DuplicateValidPost_IsAcceptedTwiceAndCreatesOneMessageAndEffect()
    {
        await using WebApplicationFactory<Program> factory = CreateCallbackFactory();
        Guid bindingId = await SeedRegistrationProviderBindingAsync(factory, "external-form");
        using HttpClient client = factory.CreateClient();
        object body = new
        {
            attemptId = Guid.CreateVersion7(),
            providerSubmissionId = "provider-submission-1",
            providerResponseRevision = "revision-1"
        };

        using HttpResponseMessage first = await PostCallbackAsync(client, "external-form", bindingId, body);
        using HttpResponseMessage second = await PostCallbackAsync(client, "external-form", bindingId, body);

        await Assert.That(first.StatusCode).IsEqualTo(HttpStatusCode.Accepted);
        await Assert.That(second.StatusCode).IsEqualTo(HttpStatusCode.Accepted);

        using IServiceScope scope = factory.Services.CreateScope();
        ExploreDbContext db = scope.ServiceProvider.GetRequiredService<ExploreDbContext>();
        var messageRepository = scope.ServiceProvider.GetRequiredService<IIncomingWebhookMessageRepository>();
        IReadOnlyList<IncomingWebhookClaim> claims = await messageRepository.ClaimDueAsync(
            new IncomingWebhookClaimRequest("test", 10, DateTime.UtcNow, TimeSpan.FromMinutes(5)),
            CancellationToken.None);
        foreach (IncomingWebhookClaim claim in claims)
        {
            using IServiceScope processingScope = factory.Services.CreateScope();
            var processingService = processingScope.ServiceProvider.GetRequiredService<IIncomingWebhookProcessingService>();
            _ = await processingService.ProcessAsync(claim, CancellationToken.None);
        }

        string providerMessageId = $"{bindingId:N}:provider-submission-1";
        await Assert.That(await db.IncomingWebhookMessages.CountAsync(message => message.ProviderMessageId == providerMessageId)).IsEqualTo(1);
        await Assert.That(await db.IncomingWebhookEffectOutboxes.CountAsync(pointer => pointer.ProviderDecisionId == providerMessageId)).IsEqualTo(1);
        IncomingWebhookMessage message = await db.IncomingWebhookMessages.SingleAsync(message => message.ProviderMessageId == providerMessageId);
        await Assert.That(message.HeadersJson).DoesNotContain("super-secret-signature");
        await Assert.That(message.HeadersJson).DoesNotContain("raw-provider-receipt");
        await Assert.That(message.HeadersJson).Contains("X-Registration-Verification-Receipt");

        Dictionary<string, string> headers = JsonSerializer.Deserialize<Dictionary<string, string>>(message.HeadersJson!)!;
        var receiptProtector = scope.ServiceProvider.GetRequiredService<IRegistrationProviderCallbackReceiptProtector>();
        RegistrationProviderCallbackReceipt receipt = receiptProtector.Unprotect(headers[IncomingWebhookIntakeService.VerificationReceiptHeader]);
        await Assert.That(receipt.BindingId).IsEqualTo(bindingId);
        await Assert.That(receipt.Provider).IsEqualTo("external-form");
    }

    [Test]
    public async Task ProviderCallback_MalformedJsonOrMissingSubmissionId_IsAcceptedWithoutCapture()
    {
        await using WebApplicationFactory<Program> factory = CreateCallbackFactory();
        Guid bindingId = await SeedRegistrationProviderBindingAsync(factory, "external-form");
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage malformed = await PostRawCallbackAsync(
            client, "external-form", bindingId, "{", includeSignature: true);
        using HttpResponseMessage missingSubmissionId = await PostRawCallbackAsync(
            client, "external-form", bindingId,
            JsonSerializer.Serialize(new { attemptId = Guid.CreateVersion7(), providerResponseRevision = "revision-1" }),
            includeSignature: true);

        await Assert.That(malformed.StatusCode).IsEqualTo(HttpStatusCode.Accepted);
        await Assert.That(missingSubmissionId.StatusCode).IsEqualTo(HttpStatusCode.Accepted);

        using IServiceScope scope = factory.Services.CreateScope();
        ExploreDbContext db = scope.ServiceProvider.GetRequiredService<ExploreDbContext>();
        await Assert.That(await db.IncomingWebhookMessages.CountAsync()).IsEqualTo(0);
        await Assert.That(await db.IncomingWebhookEffectOutboxes.CountAsync()).IsEqualTo(0);
    }

    [Test]
    public async Task ProviderCallback_VerifierFormatException_IsAcceptedWithoutTenantDisclosure()
    {
        await using WebApplicationFactory<Program> factory = CreateCallbackFactory<ThrowingRegistrationProviderCallbackVerifier>();
        Guid bindingId = await SeedRegistrationProviderBindingAsync(factory, "external-form");
        using HttpClient client = factory.CreateClient();
        object body = new
        {
            attemptId = Guid.CreateVersion7(),
            providerSubmissionId = "provider-submission-1",
            providerResponseRevision = "revision-1"
        };

        using HttpResponseMessage response = await PostCallbackAsync(client, "external-form", bindingId, body);
        string payload = await response.Content.ReadAsStringAsync();

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Accepted);
        await Assert.That(payload).DoesNotContain(PlatformDefaults.DefaultTenantId.ToString("D"));
    }

    private static WebApplicationFactory<Program> CreateFactory(IMediator mediator) =>
        new AuthenticatedWebApplicationFactory().WithWebHostBuilder(builder =>
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IMediator>();
                services.AddSingleton(mediator);
            }));

    private static WebApplicationFactory<Program> CreateCallbackFactory() =>
        CreateCallbackFactory<FakeRegistrationProviderCallbackVerifier>();

    private static WebApplicationFactory<Program> CreateCallbackFactory<TVerifier>()
        where TVerifier : class, IRegistrationProviderCallbackVerifier =>
        new AuthenticatedWebApplicationFactory().WithWebHostBuilder(builder =>
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IRegistrationProviderCallbackVerifier>();
                services.RemoveAll<IHostedService>();
                services.AddSingleton<IRegistrationProviderCallbackVerifier, TVerifier>();
            }));

    private static NativeRegistrationAttemptResult CreateAttemptResult(
        Guid attemptId,
        Guid requirementId,
        Guid channelId,
        Guid formId,
        Guid formVersionId,
        string attemptToken) => new(
        true,
        attemptId,
        requirementId,
        channelId,
        formId,
        formVersionId,
        new DateTime(2026, 8, 3, 0, 0, 0, DateTimeKind.Utc),
        new NativeRegistrationFormDefinitionDto(formVersionId, 1, "en", "hash", [], []),
        [],
        new NativeRegistrationRequirementProgressDto(1, 0, 0, 1, false),
        true,
        attemptToken);

    private static async Task<RealNativeFlow> SeedRealNativeFlowAsync(
        AuthenticatedWebApplicationFactory factory,
        int publishedFormCount)
    {
        Guid tenantId = PlatformDefaults.DefaultTenantId;
        Guid eventId = Guid.CreateVersion7();
        Guid orderId = Guid.CreateVersion7();
        Guid userId = Guid.CreateVersion7();
        Guid workflowId = Guid.CreateVersion7();
        Guid requirementId = Guid.CreateVersion7();
        Guid channelId = Guid.CreateVersion7();
        Guid formId = Guid.CreateVersion7();
        Guid versionId = Guid.CreateVersion7();
        Guid fieldId = Guid.CreateVersion7();
        DateTime now = DateTime.UtcNow;

        using IServiceScope scope = factory.Services.CreateScope();
        ExploreDbContext db = scope.ServiceProvider.GetRequiredService<ExploreDbContext>();
        RegistrationWorkflow workflow = RegistrationWorkflow.Create(
            workflowId, tenantId, eventId, "registration", now);
        RegistrationRequirement requirement = RegistrationRequirement.Create(
            requirementId, workflow, 1, RegistrationRequirementCriticalityEnum.Optional, true,
            RegistrationRequirementCompletionEffectEnum.EnrichesRegistration,
            RegistrationAnswerSyncModeEnum.FULL_CANONICAL,
            RegistrationRequirementSubjectTypeEnum.AllOrders, null, now);
        RegistrationChannel channel = RegistrationChannel.Create(channelId, requirement, 1, true, null, now);
        requirement.AddChannel(channel);
        workflow.AddRequirement(requirement);

        for (int index = 0; index < publishedFormCount; index++)
        {
            Guid currentFormId = index == 0 ? formId : Guid.CreateVersion7();
            Guid currentVersionId = index == 0 ? versionId : Guid.CreateVersion7();
            RegistrationForm form = RegistrationForm.Create(
                currentFormId, tenantId, eventId, "platform.registration", $"attendee_{index}", $"Attendee {index}", now);
            RegistrationFormVersion version = RegistrationFormVersion.Create(
                currentVersionId, form, 1, "en", null, null, now);
            RegistrationFormSection section = RegistrationFormSection.Create(
                Guid.CreateVersion7(), version, 1, "Contact permissions", now);
            RegistrationFormField field = RegistrationFormField.Create(
                index == 0 ? fieldId : Guid.CreateVersion7(), section, 1, "registration", "event_updates",
                "Send event updates", RegistrationFieldTypeEnum.Consent, 1,
                RegistrationOrganizerVisibilityEnum.Hidden, true, false, now,
                "EVENT_UPDATES", "2026-08", "I agree to receive event updates by email.");
            version.AddSection(section);
            version.AddField(section, field);
            form.AddVersion(version);
            db.RegistrationForms.Add(form);
            db.Entry(version).Property(value => value.StatusId).CurrentValue = (int)RegistrationFormStatusEnum.Published;
            db.Entry(version).Property(value => value.SchemaHash).CurrentValue = $"pinned-schema-hash-{index}";
            db.Entry(version).Property(value => value.PublishedAt).CurrentValue = now;
        }

        RegistrationOrder order = RegistrationOrder.Create(
            orderId, tenantId, eventId, userId, null, BookingPartyTypeEnum.Individual,
            Guid.CreateVersion7(), RegistrationParticipationSnapshot.Create(
                Guid.CreateVersion7(), (int)ParticipationHandlingModeEnum.PlatformManaged,
                (int)AdvanceRegistrationObligationEnum.Required,
                (int)IdentityAccessModeEnum.AccountRequired,
                GuestRecoveryPolicyEnum.VerifiedEmailRequired),
            workflowId, null, "EUR", now, now.AddHours(2));
        order.TransitionTo(RegistrationOrderStatusEnum.AwaitingIdentity, now);
        order.TransitionTo(RegistrationOrderStatusEnum.AwaitingParticipantDetails, now);
        order.TransitionTo(RegistrationOrderStatusEnum.AwaitingRequirements, now);

        db.RegistrationWorkflows.Add(workflow);
        db.RegistrationOrders.Add(order);
        await db.SaveChangesAsync();
        return new(eventId, orderId, userId, requirementId, channelId, formId, versionId, fieldId);
    }

    private static async Task<Guid> SeedRegistrationProviderBindingAsync(
        WebApplicationFactory<Program> factory,
        string provider)
    {
        Guid tenantId = PlatformDefaults.DefaultTenantId;
        Guid eventId = Guid.CreateVersion7();
        DateTime now = DateTime.UtcNow;
        using IServiceScope scope = factory.Services.CreateScope();
        ExploreDbContext db = scope.ServiceProvider.GetRequiredService<ExploreDbContext>();
        RegistrationProviderConnection connection = RegistrationProviderConnection.Create(
            tenantId, "Provider", RegistrationProviderKindEnum.ExternalForm,
            RegistrationProviderDeploymentKindEnum.HostedSaas, null, null, now);
        RegistrationProviderBinding binding = RegistrationProviderBinding.Create(
            tenantId, connection.Id, Guid.CreateVersion7(), Guid.CreateVersion7(),
            RegistrationProviderPresentationModeEnum.Redirect,
            RegistrationProviderCollectionModeEnum.ProviderHosted,
            RegistrationProviderCompletionModeEnum.Callback,
            RegistrationProviderTrustLevelEnum.FullCanonical,
            now);
        binding.AddCapability(RegistrationProviderCapability.Create(
            binding, provider, "hosted", "v1", "policy", "evidence",
            RegistrationProviderCapabilityCodes.CallbackVerification));
        binding.Publish(RegistrationEvidenceHash.Create(Convert.ToBase64String(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes("mapping")))), now);
        db.RegistrationProviderConnections.Add(connection);
        db.RegistrationProviderBindings.Add(binding);
        await db.SaveChangesAsync();
        return binding.Id;
    }

    private sealed record RealNativeFlow(
        Guid EventId,
        Guid OrderId,
        Guid UserId,
        Guid RequirementId,
        Guid ChannelId,
        Guid FormId,
        Guid VersionId,
        Guid FieldId);

    private static async Task<HttpResponseMessage> PostAsync(
        HttpClient client,
        string path,
        object body,
        bool authenticated,
        string? orderCapability = null,
        string? attemptCapability = null,
        Guid? authenticatedUserId = null,
        string? idempotencyKey = null)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, path)
        {
            Content = JsonContent.Create(body)
        };
        request.Headers.Add("Idempotency-Key", idempotencyKey ?? Guid.CreateVersion7().ToString("D"));
        if (authenticated)
        {
            request.Headers.Add(TestAuthHandler.AuthHeaderName,
                TestAuthHandler.CreateAuthHeaderValue(authenticatedUserId ?? Guid.CreateVersion7()));
        }
        if (orderCapability is not null)
        {
            request.Headers.Add(OrderCapabilityHeader, orderCapability);
        }
        if (attemptCapability is not null)
        {
            request.Headers.Add(AttemptCapabilityHeader, attemptCapability);
        }
        return await client.SendAsync(request);
    }

    private static async Task<HttpResponseMessage> PostCallbackAsync(
        HttpClient client,
        string provider,
        Guid bindingId,
        object body)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"/api/integrations/registration/{provider}/{bindingId:D}/callback")
        {
            Content = JsonContent.Create(body)
        };
        request.Headers.Add("X-Provider-Signature", "super-secret-signature");
        return await client.SendAsync(request);
    }

    private static async Task<HttpResponseMessage> PostRawCallbackAsync(
        HttpClient client,
        string provider,
        Guid bindingId,
        string body,
        bool includeSignature)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"/api/integrations/registration/{provider}/{bindingId:D}/callback")
        {
            Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json")
        };
        if (includeSignature)
        {
            request.Headers.Add("X-Provider-Signature", "super-secret-signature");
        }

        return await client.SendAsync(request);
    }

    private sealed class FakeRegistrationProviderCallbackVerifier : IRegistrationProviderCallbackVerifier
    {
        public Task<RegistrationProviderCallbackVerificationResult> VerifyCallbackAsync(
            RegistrationProviderCallbackVerificationRequest request,
            CancellationToken cancellationToken) => Task.FromResult(
            request.Headers.ContainsKey("X-Provider-Signature")
                ? new RegistrationProviderCallbackVerificationResult(true, Receipt: "raw-provider-receipt")
                : new RegistrationProviderCallbackVerificationResult(false, "missing_signature"));
    }

    private sealed class ThrowingRegistrationProviderCallbackVerifier : IRegistrationProviderCallbackVerifier
    {
        public Task<RegistrationProviderCallbackVerificationResult> VerifyCallbackAsync(
            RegistrationProviderCallbackVerificationRequest request,
            CancellationToken cancellationToken) => throw new System.Security.Cryptography.CryptographicException("bad signature envelope");
    }
}
