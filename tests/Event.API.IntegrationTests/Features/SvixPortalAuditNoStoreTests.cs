// ABOUTME: API contract tests for audited no-store Svix portal issuance.
// ABOUTME: Ensures audit failures and safe metadata handling never expose a portal URL.

using System.Net;
using System.Net.Http.Json;
using Event.Api.IntegrationTests.Fixtures;
using Explore.API.Controllers;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Webhooks;
using Explore.Application.Features.Webhooks.Handlers.Commands;
using Explore.Application.Features.Webhooks.Requests.Commands;
using Explore.Domain;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;

namespace Event.API.IntegrationTests.Features;

public sealed class SvixPortalAuditNoStoreTests
{
    [Test]
    public async Task PortalEndpoint_DeclaresNoStoreAndWriteMiddlewareEnforcesIt()
    {
        var method = typeof(WebhooksController).GetMethod(nameof(WebhooksController.OpenSvixAppPortal));
        var responseCache = method!
            .GetCustomAttributes(typeof(ResponseCacheAttribute), inherit: true)
            .Cast<ResponseCacheAttribute>()
            .Single();
        await using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/webhooks/svix/app-portal")
        {
            Content = JsonContent.Create(new
            {
                consumerId = Guid.CreateVersion7(),
                expiresInSeconds = 300
            })
        };
        using var response = await client.SendAsync(request);

        await Assert.That(responseCache.NoStore).IsTrue();
        await Assert.That(responseCache.Location).IsEqualTo(ResponseCacheLocation.None);
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
        await Assert.That(response.Headers.CacheControl?.NoStore).IsTrue();
        await Assert.That(response.Headers.Pragma.Any(value => value.Name == "no-cache")).IsTrue();
    }

    [Test]
    public async Task AuditFailure_ProducesNoPortalResponse()
    {
        var portalService = Substitute.For<IWebhookProviderPortalService>();
        var auditRepository = Substitute.For<IAuditLogRepository>();
        var currentUserService = Substitute.For<ICurrentUserService>();
        portalService.CreateAccessAsync(Arg.Any<WebhookProviderPortalAccessInput>(), Arg.Any<CancellationToken>())
            .Returns(WebhookProviderPortalAccessResult.Success(
                "https://svix.example/sensitive-portal",
                "sensitive-token",
                DateTimeOffset.UtcNow.AddMinutes(15),
                Guid.CreateVersion7(),
                "policy-v1"));
        auditRepository.Create(Arg.Any<AuditLog>())
            .Returns(Task.FromException<AuditLog>(new InvalidOperationException("audit unavailable")));
        var handler = new OpenSvixAppPortalCommandHandler(
            portalService,
            auditRepository,
            currentUserService);

        await Assert.ThrowsAsync<InvalidOperationException>(() => handler.Handle(
            CreateCommand(),
            CancellationToken.None));
    }

    [Test]
    public async Task SuccessfulAudit_ContainsSafeMetadataOnly()
    {
        const string portalUrl = "https://svix.example/sensitive-portal";
        const string portalToken = "sensitive-token";
        var bindingId = Guid.CreateVersion7();
        var portalService = Substitute.For<IWebhookProviderPortalService>();
        var auditRepository = Substitute.For<IAuditLogRepository>();
        var currentUserService = Substitute.For<ICurrentUserService>();
        portalService.CreateAccessAsync(Arg.Any<WebhookProviderPortalAccessInput>(), Arg.Any<CancellationToken>())
            .Returns(WebhookProviderPortalAccessResult.Success(
                portalUrl,
                portalToken,
                DateTimeOffset.UtcNow.AddMinutes(15),
                bindingId,
                "policy-v1"));
        AuditLog? auditLog = null;
        auditRepository.Create(Arg.Do<AuditLog>(log => auditLog = log))
            .Returns(call => Task.FromResult(call.Arg<AuditLog>()!));
        var handler = new OpenSvixAppPortalCommandHandler(
            portalService,
            auditRepository,
            currentUserService);

        var result = await handler.Handle(CreateCommand(), CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(auditLog).IsNotNull();
        await Assert.That(auditLog!.NewValues).Contains(bindingId.ToString("D"), StringComparison.OrdinalIgnoreCase);
        await Assert.That(auditLog.NewValues).Contains("correlationId", StringComparison.Ordinal);
        await Assert.That(auditLog.NewValues).Contains("webhook_provider_portal", StringComparison.Ordinal);
        await Assert.That(auditLog.NewValues).DoesNotContain(portalUrl, StringComparison.Ordinal);
        await Assert.That(auditLog.NewValues).DoesNotContain(portalToken, StringComparison.Ordinal);
    }

    [Test]
    public async Task ProviderFailure_IsAuditedWithSafeCategoryBeforeResponse()
    {
        var portalService = Substitute.For<IWebhookProviderPortalService>();
        var auditRepository = Substitute.For<IAuditLogRepository>();
        var currentUserService = Substitute.For<ICurrentUserService>();
        portalService.CreateAccessAsync(Arg.Any<WebhookProviderPortalAccessInput>(), Arg.Any<CancellationToken>())
            .Returns(WebhookProviderPortalAccessResult.Failure(
                "svix_provider_unavailable",
                isRetryable: true,
                "SvixApi:503"));
        AuditLog? auditLog = null;
        auditRepository.Create(Arg.Do<AuditLog>(log => auditLog = log))
            .Returns(call => Task.FromResult(call.Arg<AuditLog>()!));
        var handler = new OpenSvixAppPortalCommandHandler(
            portalService,
            auditRepository,
            currentUserService);

        var result = await handler.Handle(CreateCommand(), CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(auditLog).IsNotNull();
        await Assert.That(auditLog!.NewValues).Contains("provider_failure", StringComparison.Ordinal);
        await Assert.That(auditLog.NewValues).Contains("svix_provider_unavailable", StringComparison.Ordinal);
        await Assert.That(auditLog.NewValues).DoesNotContain("SvixApi:503", StringComparison.Ordinal);
    }

    private static OpenSvixAppPortalCommand CreateCommand() => new()
    {
        TenantId = Guid.CreateVersion7(),
        ConsumerId = Guid.CreateVersion7(),
        SessionId = "session-1"
    };
}
