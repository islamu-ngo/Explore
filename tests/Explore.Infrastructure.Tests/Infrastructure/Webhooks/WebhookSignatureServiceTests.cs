// ABOUTME: Unit tests for Svix-compatible webhook signing and verification.
// ABOUTME: Covers raw-body integrity, timestamp tolerance, fixed header names, and secret rotation support.

using System.Text;
using Explore.Application.Contracts.Webhooks;
using Explore.Infrastructure.Webhooks;

namespace Explore.Infrastructure.Tests.Infrastructure.Webhooks;

public sealed class WebhookSignatureServiceTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 7, 2, 12, 0, 0, TimeSpan.Zero);

    [Test]
    public async Task Verify_WhenSignatureMatchesRawBody_ReturnsSuccess()
    {
        var service = CreateService();
        var secret = CreateSecret("current-secret");
        const string payload = "{\"id\":\"msg_1\",\"data\":{\"value\":1}}";

        var headers = service.Sign("msg_1", FixedNow, System.Text.Encoding.UTF8.GetBytes(payload), secret);

        var result = service.Verify(Encoding.UTF8.GetBytes(payload), ToDictionary(headers), secret);

        await Assert.That(result.IsValid).IsTrue();
        await Assert.That(result.Timestamp).IsEqualTo(FixedNow);
    }

    [Test]
    public async Task Verify_WhenPayloadChanges_ReturnsSignatureMismatch()
    {
        var service = CreateService();
        var secret = CreateSecret("current-secret");
        var headers = service.Sign("msg_1", FixedNow, "{\"value\":1}"u8, secret);

        var result = service.Verify(Encoding.UTF8.GetBytes("{\"value\":2}"), ToDictionary(headers), secret);

        await Assert.That(result.IsValid).IsFalse();
        await Assert.That(result.FailureCategory).IsEqualTo("signature_mismatch");
    }

    [Test]
    public async Task Verify_WhenTimestampOutsideTolerance_ReturnsTimestampFailure()
    {
        var service = CreateService();
        var secret = CreateSecret("current-secret");
        var headers = service.Sign("msg_1", FixedNow.AddMinutes(-10), "{}"u8, secret);

        var result = service.Verify(Encoding.UTF8.GetBytes("{}"), ToDictionary(headers), secret);

        await Assert.That(result.IsValid).IsFalse();
        await Assert.That(result.FailureCategory).IsEqualTo("timestamp_outside_tolerance");
    }

    [Test]
    public async Task Verify_WhenPreviousSecretStillValid_AcceptsSignature()
    {
        var service = CreateService();
        var previousOnly = CreateSecret("previous-secret");
        var rotatedSecret = new WebhookSecretMaterial(
            CurrentSecret: CreateWhsec("current-secret"),
            CurrentSecretVersion: 2,
            PreviousSecret: previousOnly.CurrentSecret,
            PreviousSecretValidUntil: FixedNow.AddMinutes(1));

        var headers = service.Sign("msg_1", FixedNow, "{}"u8, previousOnly);

        var result = service.Verify(Encoding.UTF8.GetBytes("{}"), ToDictionary(headers), rotatedSecret);

        await Assert.That(result.IsValid).IsTrue();
    }

    private static WebhookSignatureService CreateService() =>
        new(new FixedTimeProvider(FixedNow));

    private static WebhookSecretMaterial CreateSecret(string value) =>
        new(CreateWhsec(value), CurrentSecretVersion: 1);

    private static string CreateWhsec(string value) =>
        $"whsec_{Convert.ToBase64String(Encoding.UTF8.GetBytes(value))}";

    private static Dictionary<string, string> ToDictionary(WebhookSignatureHeaders headers) => new(StringComparer.OrdinalIgnoreCase)
    {
        ["svix-id"] = headers.SvixId,
        ["svix-timestamp"] = headers.SvixTimestamp,
        ["svix-signature"] = headers.SvixSignature
    };

    private sealed class FixedTimeProvider : TimeProvider
    {
        private readonly DateTimeOffset _now;

        public FixedTimeProvider(DateTimeOffset now)
        {
            _now = now;
        }

        public override DateTimeOffset GetUtcNow() => _now;
    }
}
