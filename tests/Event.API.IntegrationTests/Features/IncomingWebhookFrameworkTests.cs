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
using Microsoft.Extensions.Logging;
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
        await Assert.That(action.GetCustomAttribute<EnableRateLimitingAttribute>()?.PolicyName).IsEqualTo(RateLimitingExtensions.PublicIngestionPolicy);
    }

    [Test]
    public async Task RecordSvixOperationalCallback_WhenVerificationFails_ReturnsSafeProblemDetails()
    {
        const string unsafePayload = "{\"tenantId\":\"018f0000-0000-7000-8000-000000000001\",\"secret\":\"raw-provider-secret\"}";
        var intakeService = Substitute.For<IIncomingWebhookIntakeService>();
        intakeService.ReadAndVerifyAsync(
                Arg.Any<HttpRequest>(),
                "svix",
                Arg.Any<long>(),
                Arg.Any<CancellationToken>())
            .Returns(IncomingWebhookReadResult.Failure(
                "svix",
                StatusCodes.Status401Unauthorized,
                "Incoming webhook verification failed",
                "The Svix operational webhook signature could not be verified.",
                "svix_webhook_signature_mismatch"));
        var controller = CreateController(intakeService);

        var result = await controller.RecordSvixOperationalCallback();

        var objectResult = result as ObjectResult;
        await Assert.That(objectResult).IsNotNull();
        await Assert.That(objectResult!.StatusCode).IsEqualTo(StatusCodes.Status401Unauthorized);
        var problem = objectResult.Value as ProblemDetails;
        await Assert.That(problem).IsNotNull();
        await Assert.That(problem!.Status).IsEqualTo(StatusCodes.Status401Unauthorized);
        await Assert.That(problem.Title).IsEqualTo("Incoming webhook verification failed");
        await Assert.That(problem.Detail).IsEqualTo("The Svix operational webhook signature could not be verified.");
        await Assert.That(problem.Extensions["code"]).IsEqualTo("svix_webhook_signature_mismatch");
        await Assert.That(problem.Detail).DoesNotContain(unsafePayload);
        await Assert.That(problem.Detail).DoesNotContain("raw-provider-secret");
        await intakeService.DidNotReceive().CaptureAsync(
            Arg.Any<IncomingWebhookReadResult>(),
            Arg.Any<Guid>(),
            Arg.Any<string?>(),
            Arg.Any<string?>(),
            Arg.Any<string?>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task RecordSvixOperationalCallback_WhenDuplicateTenantCapture_AcknowledgesWithoutProcessingAgain()
    {
        var tenantId = Guid.Parse("018f0000-0000-7000-8000-000000000001");
        var existingMessageId = Guid.Parse("018f0000-0000-7000-8000-000000000002");
        var rawPayload = $"{{\"tenantId\":\"{tenantId}\"}}";
        var verification = IncomingWebhookVerificationResult.Verified(
            "msg_duplicate_1",
            "endpoint.updated",
            "msg_duplicate_1");
        var read = IncomingWebhookReadResult.Success(
            "svix",
            rawPayload,
            Encoding.UTF8.GetBytes(rawPayload),
            DateTimeOffset.UtcNow,
            ComputePayloadHash(rawPayload),
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase),
            verification);
        var intakeService = Substitute.For<IIncomingWebhookIntakeService>();
        intakeService.ReadAndVerifyAsync(
                Arg.Any<HttpRequest>(),
                "svix",
                Arg.Any<long>(),
                Arg.Any<CancellationToken>())
            .Returns(read);
        intakeService.CaptureAsync(
                read,
                tenantId,
                "msg_duplicate_1",
                "endpoint.updated",
                "msg_duplicate_1",
                Arg.Any<CancellationToken>())
            .Returns(IncomingWebhookCaptureResult.Duplicate(
                existingMessageId,
                "msg_duplicate_1",
                "msg_duplicate_1"));
        var controller = CreateController(intakeService);

        var result = await controller.RecordSvixOperationalCallback();

        await Assert.That(result).IsTypeOf<AcceptedResult>();
        await intakeService.Received(1).CaptureAsync(
            read,
            tenantId,
            "msg_duplicate_1",
            "endpoint.updated",
            "msg_duplicate_1",
            Arg.Any<CancellationToken>());
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
            Encoding.UTF8.GetBytes(payload),
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
    public async Task IncomingWebhookIntakeService_WithMissingSvixSignature_ReturnsUnauthorizedWithoutUnsafeDetail()
    {
        const string payload = "{\"tenantId\":\"018f0000-0000-7000-8000-000000000001\",\"secret\":\"raw-provider-secret\"}";
        var secret = CreateSvixSecret();
        var request = CreateSvixRequest(payload);
        request.Headers["svix-id"] = "msg_missing_signature";
        request.Headers["svix-timestamp"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
        var service = CreateSvixIntakeService(secret);

        var result = await service.ReadAndVerifyAsync(request, "svix", 65_536, CancellationToken.None);

        await Assert.That(result.Succeeded).IsFalse();
        await Assert.That(result.StatusCode).IsEqualTo(StatusCodes.Status401Unauthorized);
        await Assert.That(result.Code).IsEqualTo("svix_webhook_missing_header");
        await Assert.That(result.Detail).IsEqualTo("The Svix operational webhook signature could not be verified.");
        await Assert.That(result.Detail).DoesNotContain(payload);
        await Assert.That(result.Detail).DoesNotContain("raw-provider-secret");
        await Assert.That(result.Detail).DoesNotContain("msg_missing_signature");
    }

    [Test]
    public async Task IncomingWebhookIntakeService_WithInvalidSvixSignature_ReturnsUnauthorizedWithoutUnsafeDetail()
    {
        const string payload = "{\"tenantId\":\"018f0000-0000-7000-8000-000000000001\",\"secret\":\"raw-provider-secret\"}";
        var secret = CreateSvixSecret();
        var request = CreateSvixRequest(payload);
        request.Headers["svix-id"] = "msg_invalid_signature";
        request.Headers["svix-timestamp"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
        request.Headers["svix-signature"] = "v1," + Convert.ToBase64String(new byte[32]);
        var service = CreateSvixIntakeService(secret);

        var result = await service.ReadAndVerifyAsync(request, "svix", 65_536, CancellationToken.None);

        await Assert.That(result.Succeeded).IsFalse();
        await Assert.That(result.StatusCode).IsEqualTo(StatusCodes.Status401Unauthorized);
        await Assert.That(result.Code).IsEqualTo("svix_webhook_signature_mismatch");
        await Assert.That(result.Detail).IsEqualTo("The Svix operational webhook signature could not be verified.");
        await Assert.That(result.Detail).DoesNotContain(payload);
        await Assert.That(result.Detail).DoesNotContain("raw-provider-secret");
        await Assert.That(result.Detail).DoesNotContain("msg_invalid_signature");
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
            Options.Create(new WebhookOptions()),
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
                message.PayloadBytes.ToArray().SequenceEqual(Encoding.UTF8.GetBytes(rawPayload)) &&
                !message.HeadersJson!.Contains("X-Test-Signature", StringComparison.Ordinal)),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task IncomingWebhookIntakeService_WhenBodyExceedsLimit_ReturnsPayloadTooLargeBeforeVerification()
    {
        var verifier = Substitute.For<IIncomingWebhookVerifier>();
        verifier.Provider.Returns("test");
        var repository = Substitute.For<IIncomingWebhookMessageRepository>();
        var service = new IncomingWebhookIntakeService(
            new IncomingWebhookVerifierRegistry([verifier]),
            repository,
            Options.Create(new WebhookOptions()),
            NullLogger<IncomingWebhookIntakeService>.Instance);
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes("{\"oversized\":true}"));

        var result = await service.ReadAndVerifyAsync(httpContext.Request, "test", maxBodyBytes: 8, CancellationToken.None);

        await Assert.That(result.Succeeded).IsFalse();
        await Assert.That(result.StatusCode).IsEqualTo(StatusCodes.Status413PayloadTooLarge);
        await Assert.That(result.Code).IsEqualTo("test_webhook_body_too_large");
        await verifier.DidNotReceive().VerifyAsync(
            Arg.Any<IncomingWebhookContext>(),
            Arg.Any<CancellationToken>());
        await repository.DidNotReceive().TryCreateAsync(
            Arg.Any<IncomingWebhookMessage>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task IncomingWebhookIntakeService_WhenDuplicateCapture_LogsWithoutTenantOrProviderMessageId()
    {
        var tenantId = Guid.Parse("018f0000-0000-7000-8000-000000000001");
        const string rawPayload = "{\"tenantId\":\"018f0000-0000-7000-8000-000000000001\"}";
        var payloadHash = ComputePayloadHash(rawPayload);
        var read = IncomingWebhookReadResult.Success(
            "svix",
            rawPayload,
            Encoding.UTF8.GetBytes(rawPayload),
            DateTimeOffset.UtcNow,
            payloadHash,
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["svix-signature"] = "v1,sensitive",
                ["authorization"] = "Bearer sensitive",
                ["x-safe"] = "safe"
            },
            IncomingWebhookVerificationResult.Verified(
                "msg_sensitive_provider_id",
                "endpoint.updated",
                "msg_sensitive_provider_id"));
        var repository = Substitute.For<IIncomingWebhookMessageRepository>();
        repository.TryCreateAsync(Arg.Any<IncomingWebhookMessage>(), Arg.Any<CancellationToken>())
            .Returns(false);
        var existing = IncomingWebhookMessage.CreateVerified(
            tenantId,
            "svix",
            "msg_sensitive_provider_id",
            "msg_sensitive_provider_id",
            "endpoint.updated",
            Encoding.UTF8.GetBytes(rawPayload),
            payloadHash,
            "application/json",
            "utf-8",
            headersJson: null,
            DateTime.UtcNow.AddMinutes(-1),
            DateTime.UtcNow.AddMinutes(-1),
            DateTime.UtcNow.AddDays(14));
        repository.GetByProviderMessageIdForUpdateAsync(
                tenantId,
                "svix",
                "msg_sensitive_provider_id",
                Arg.Any<CancellationToken>())
            .Returns(existing);
        var logger = new ListLogger<IncomingWebhookIntakeService>();
        var service = new IncomingWebhookIntakeService(
            new IncomingWebhookVerifierRegistry([]),
            repository,
            Options.Create(new WebhookOptions()),
            logger);

        var result = await service.CaptureAsync(read, tenantId, null, null, null, CancellationToken.None);

        await Assert.That(result.Succeeded).IsTrue();
        await Assert.That(result.IsDuplicate).IsTrue();
        await Assert.That(result.MessageId).IsEqualTo(existing.Id);
        var logOutput = string.Join('\n', logger.Messages);
        await Assert.That(logOutput).Contains("Incoming webhook duplicate captured for provider svix");
        await Assert.That(logOutput).DoesNotContain(tenantId.ToString());
        await Assert.That(logOutput).DoesNotContain("msg_sensitive_provider_id");
        await repository.Received(1).TryCreateAsync(
            Arg.Is<IncomingWebhookMessage>(message =>
                message.PayloadBytes.ToArray().SequenceEqual(Encoding.UTF8.GetBytes(rawPayload)) &&
                message.HeadersJson!.Contains("x-safe", StringComparison.Ordinal) &&
                !message.HeadersJson.Contains("svix-signature", StringComparison.OrdinalIgnoreCase) &&
                !message.HeadersJson.Contains("authorization", StringComparison.OrdinalIgnoreCase)),
            Arg.Any<CancellationToken>());
    }

    private static async Task AssertRoute(MethodInfo method, Type attributeType, string? template, string routeName)
    {
        var attribute = method.GetCustomAttributes().Single(value => value.GetType() == attributeType) as HttpMethodAttribute;
        await Assert.That(attribute).IsNotNull();
        await Assert.That(attribute!.Template).IsEqualTo(template);
        await Assert.That(attribute.Name).IsEqualTo(routeName);
    }

    private static IncomingWebhooksController CreateController(IIncomingWebhookIntakeService intakeService) => new(
        intakeService,
        new StaticOptionsMonitor<WebhookOptions>(new WebhookOptions
        {
            Svix = new WebhookSvixOptions
            {
                OperationalWebhookMaxBodyBytes = 65_536
            }
        }),
        CreateMetrics(),
        NullLogger<IncomingWebhooksController>.Instance)
    {
        ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        }
    };

    private static BusinessMetrics CreateMetrics()
    {
        var meterFactory = Substitute.For<IMeterFactory>();
        meterFactory.Create(Arg.Any<MeterOptions>()).Returns(new Meter(BusinessMetrics.MeterName));

        return new BusinessMetrics(meterFactory);
    }

    private static CoopIncomingWebhookVerifier CreateCoopVerifier(string secret) => new(
        new StaticOptionsMonitor<CoopProviderOptions>(new CoopProviderOptions
        {
            WebhookSecret = secret
        }),
        NullLogger<CoopIncomingWebhookVerifier>.Instance);

    private static IncomingWebhookIntakeService CreateSvixIntakeService(string secret)
    {
        const string secretRef = "webhooks.svix.operational_webhook_secret";
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
            new WebhookSignatureService(),
            NullLogger<SvixIncomingWebhookVerifier>.Instance);

        return new IncomingWebhookIntakeService(
            new IncomingWebhookVerifierRegistry([verifier]),
            Substitute.For<IIncomingWebhookMessageRepository>(),
            Options.Create(new WebhookOptions()),
            NullLogger<IncomingWebhookIntakeService>.Instance);
    }

    private static HttpRequest CreateSvixRequest(string body)
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(body));
        httpContext.Request.ContentType = HateoasConstants.JsonMediaType;
        return httpContext.Request;
    }

    private static string CreateSvixSecret()
    {
        var secretBytes = Enumerable.Range(1, 32).Select(value => (byte)value).ToArray();
        return "whsec_" + Convert.ToBase64String(secretBytes);
    }

    private static string ComputeCoopSignature(string secret, string timestamp, string body)
    {
        using var hmac = new System.Security.Cryptography.HMACSHA256(Encoding.UTF8.GetBytes(secret));
        return Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes($"{timestamp}.{body}"))).ToLowerInvariant();
    }

    private static string ComputePayloadHash(string payload) =>
        $"sha256:{Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(Encoding.UTF8.GetBytes(payload))).ToLowerInvariant()}";

    private sealed class StaticOptionsMonitor<TOptions>(TOptions currentValue) : IOptionsMonitor<TOptions>
    {
        public TOptions CurrentValue => currentValue;

        public TOptions Get(string? name) => currentValue;

        public IDisposable? OnChange(Action<TOptions, string?> listener) => null;
    }

    private sealed class ListLogger<T> : ILogger<T>
    {
        public List<string> Messages { get; } = [];

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            Messages.Add(formatter(state, exception));
        }
    }
}
