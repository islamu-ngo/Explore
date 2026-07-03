// ABOUTME: API integration tests for the provider-neutral incoming webhook framework.
// ABOUTME: Verifies verifier lookup, Svix signature verification, route metadata, and raw-body capture behavior.

using System.Diagnostics.Metrics;
using System.Reflection;
using System.Text;
using Explore.API.Attributes;
using Explore.API.Controllers;
using Explore.API.Extensions;
using Explore.API.Hateoas;
using Explore.API.Services;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Secrets;
using Explore.Application.Contracts.Webhooks;
using Explore.Application.Telemetry;
using Explore.Domain;
using Explore.Domain.Enums;
using Explore.Infrastructure.Configuration;
using Explore.Infrastructure.Webhooks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Event.Api.IntegrationTests.Features;

public sealed class IncomingWebhookFrameworkTests
{
    [Test]
    public async Task SvixOperationalRoute_UsesStableAnonymousSignedCallbackMetadata()
    {
        var controllerType = typeof(IncomingWebhooksController);
        var action = controllerType.GetMethod(nameof(IncomingWebhooksController.RecordSvixOperationalCallback))!;

        await Assert.That(controllerType.GetCustomAttribute<EndpointClassificationAttribute>()?.Class).IsEqualTo(EndpointClass.Public);
        await Assert.That(controllerType.GetCustomAttribute<RouteAttribute>()?.Template).IsEqualTo("api/integrations");
        await AssertRoute(action, typeof(HttpPostAttribute), "svix/operational", RouteNames.IntegrationSvixOperationalCallback);
        await Assert.That(action.GetCustomAttribute<AllowAnonymousAttribute>()).IsNotNull();
        await Assert.That(action.GetCustomAttribute<EnableRateLimitingAttribute>()?.PolicyName).IsEqualTo(RateLimitingExtensions.WritePolicy);
    }

    [Test]
    public async Task CoopIncomingWebhookVerifier_WithValidTimestampedHmac_ReturnsVerifiedResult()
    {
        const string secret = "coop-secret";
        const string payload = "{\"tenant_id\":\"018f0000-0000-7000-8000-000000000001\"}";
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
        var verifier = CreateCoopVerifier(secret);
        var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["X-Coop-Timestamp"] = timestamp,
            ["X-Coop-Signature"] = $"sha256={ComputeCoopSignature(secret, timestamp, payload)}"
        };

        var result = await verifier.VerifyAsync(
            new IncomingWebhookContext("coop", payload, headers, DateTimeOffset.UtcNow),
            CancellationToken.None);

        await Assert.That(result.IsVerified).IsTrue();
        await Assert.That(result.EventType).IsEqualTo("moderation.coop.decision");
        await Assert.That(result.ProviderMessageId).StartsWith("sha256:");
    }

    [Test]
    public async Task SvixIncomingWebhookVerifier_WithValidSignature_ReturnsSvixMessageId()
    {
        var secretBytes = Enumerable.Range(1, 32).Select(value => (byte)value).ToArray();
        var secret = "whsec_" + Convert.ToBase64String(secretBytes);
        const string secretRef = "webhooks.svix.operational_webhook_secret";
        const string payload = "{\"type\":\"endpoint.created\"}";
        const string messageId = "msg_test_123";
        var signatureService = new WebhookSignatureService();
        var signatureHeaders = signatureService.Sign(
            messageId,
            DateTimeOffset.UtcNow,
            payload,
            new WebhookSecretMaterial(secret, 1));
        var secretResolver = Substitute.For<ISecretResolver>();
        secretResolver.ResolveAsync(secretRef, null, Arg.Any<CancellationToken>())
            .Returns(new ResolvedSecret(
                secretRef,
                secret,
                SecretSourceType.EnvironmentVariable,
                SecretScope.Instance,
                null,
                DateTimeOffset.UtcNow));
        var verifier = new SvixIncomingWebhookVerifier(
            new StaticOptionsMonitor<WebhookOptions>(new WebhookOptions
            {
                Svix = new WebhookSvixOptions
                {
                    OperationalWebhookSecretRef = secretRef
                }
            }),
            secretResolver,
            signatureService,
            NullLogger<SvixIncomingWebhookVerifier>.Instance);
        var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["svix-id"] = signatureHeaders.SvixId,
            ["svix-timestamp"] = signatureHeaders.SvixTimestamp,
            ["svix-signature"] = signatureHeaders.SvixSignature,
            ["svix-event-type"] = "endpoint.created"
        };

        var result = await verifier.VerifyAsync(
            new IncomingWebhookContext("svix", payload, headers, DateTimeOffset.UtcNow),
            CancellationToken.None);

        await Assert.That(result.IsVerified).IsTrue();
        await Assert.That(result.ProviderMessageId).IsEqualTo(messageId);
        await Assert.That(result.EventType).IsEqualTo("endpoint.created");
    }

    [Test]
    public async Task IncomingWebhookIntakeService_ReadsRawBodyResetsStreamAndCapturesBeforeProcessing()
    {
        const string rawPayload = "{\"tenant_id\":\"018f0000-0000-7000-8000-000000000001\"}";
        var tenantId = Guid.Parse("018f0000-0000-7000-8000-000000000001");
        var verifier = Substitute.For<IIncomingWebhookVerifier>();
        verifier.Provider.Returns("test");
        verifier.VerifyAsync(Arg.Any<IncomingWebhookContext>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                var context = call.Arg<IncomingWebhookContext>();
                return IncomingWebhookVerificationResult.Verified(
                    "provider-msg-1",
                    "test.received",
                    "provider-msg-1:" + context.RawPayload.Length);
            });
        var repository = Substitute.For<IIncomingWebhookMessageRepository>();
        repository.TryCreateAsync(Arg.Any<IncomingWebhookMessage>(), Arg.Any<CancellationToken>())
            .Returns(true);
        var service = new IncomingWebhookIntakeService(
            new IncomingWebhookVerifierRegistry([verifier]),
            repository,
            NullLogger<IncomingWebhookIntakeService>.Instance);
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(rawPayload));
        httpContext.Request.Headers["X-Test"] = "safe";
        httpContext.Request.Headers["X-Test-Signature"] = "secret";

        var read = await service.ReadAndVerifyAsync(httpContext.Request, "test", 65_536, CancellationToken.None);
        var capture = await service.CaptureAsync(read, tenantId, null, null, null, CancellationToken.None);

        await Assert.That(read.Succeeded).IsTrue();
        await Assert.That(httpContext.Request.Body.Position).IsEqualTo(0);
        await Assert.That(capture.Succeeded).IsTrue();
        await repository.Received(1).TryCreateAsync(
            Arg.Is<IncomingWebhookMessage>(message =>
                message.TenantId == tenantId &&
                message.Provider == "test" &&
                message.ProviderMessageId == "provider-msg-1" &&
                message.EventType == "test.received" &&
                message.PayloadJson == null &&
                !message.HeadersJson!.Contains("X-Test-Signature", StringComparison.Ordinal)),
            Arg.Any<CancellationToken>());
    }

    private static async Task AssertRoute(MethodInfo method, Type attributeType, string? template, string routeName)
    {
        var attribute = method.GetCustomAttributes().Single(value => value.GetType() == attributeType) as HttpMethodAttribute;
        await Assert.That(attribute).IsNotNull();
        await Assert.That(attribute!.Template).IsEqualTo(template);
        await Assert.That(attribute.Name).IsEqualTo(routeName);
    }

    private static CoopIncomingWebhookVerifier CreateCoopVerifier(string secret) => new(
        new StaticOptionsMonitor<CoopProviderOptions>(new CoopProviderOptions
        {
            WebhookSecret = secret
        }),
        NullLogger<CoopIncomingWebhookVerifier>.Instance);

    private static string ComputeCoopSignature(string secret, string timestamp, string body)
    {
        using var hmac = new System.Security.Cryptography.HMACSHA256(Encoding.UTF8.GetBytes(secret));
        return Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes($"{timestamp}.{body}"))).ToLowerInvariant();
    }

    private sealed class StaticOptionsMonitor<TOptions>(TOptions currentValue) : IOptionsMonitor<TOptions>
    {
        public TOptions CurrentValue => currentValue;

        public TOptions Get(string? name) => currentValue;

        public IDisposable? OnChange(Action<TOptions, string?> listener) => null;
    }
}
