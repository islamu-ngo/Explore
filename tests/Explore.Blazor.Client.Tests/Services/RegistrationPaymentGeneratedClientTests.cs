// ABOUTME: Verifies generated authenticated payment mutations transmit the required idempotency header.
// ABOUTME: Prevents OpenAPI or NSwag drift from silently dropping payment replay protection.

using System.Net;
using System.Reflection;
using Explore.Blazor.Client.Clients;

namespace Explore.Blazor.Client.Tests.Services;

public sealed class RegistrationPaymentGeneratedClientTests
{
    [Test]
    public async Task AuthenticatedPaymentMutationsSendGeneratedIdempotencyHeader()
    {
        var handler = new RecordingHandler();
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://api.example.test") };
        var client = new EventApiClient(httpClient);
        Guid eventId = Guid.CreateVersion7();
        Guid orderId = Guid.CreateVersion7();

        await client.StartAuthenticatedRegistrationPaymentAsync(
            eventId, orderId, idempotency_Key: "start-key", cancellationToken: CancellationToken.None);
        await client.RetryAuthenticatedRegistrationPaymentAsync(
            eventId, orderId, idempotency_Key: "retry-key", cancellationToken: CancellationToken.None);
        await client.StartGuestRegistrationPaymentAsync(
            eventId, orderId, "guest-start-key", "guest-capability", cancellationToken: CancellationToken.None);
        await client.RetryGuestRegistrationPaymentAsync(
            eventId, orderId, "guest-retry-key", "guest-capability", cancellationToken: CancellationToken.None);

        await Assert.That(handler.Requests.Count).IsEqualTo(4);
        await Assert.That(handler.Requests[0].Headers.GetValues("Idempotency-Key").Single()).IsEqualTo("start-key");
        await Assert.That(handler.Requests[1].Headers.GetValues("Idempotency-Key").Single()).IsEqualTo("retry-key");
        await Assert.That(handler.Requests[2].Headers.GetValues("Idempotency-Key").Single()).IsEqualTo("guest-start-key");
        await Assert.That(handler.Requests[3].Headers.GetValues("Idempotency-Key").Single()).IsEqualTo("guest-retry-key");

        foreach (string methodName in new[]
                 {
                     nameof(IEventApiClient.StartAuthenticatedRegistrationPaymentAsync),
                     nameof(IEventApiClient.RetryAuthenticatedRegistrationPaymentAsync),
                     nameof(IEventApiClient.StartGuestRegistrationPaymentAsync),
                     nameof(IEventApiClient.RetryGuestRegistrationPaymentAsync)
                 })
        {
            ParameterInfo parameter = typeof(IEventApiClient).GetMethods().Single(method => method.Name == methodName)
                .GetParameters().Single(candidate => candidate.Name == "idempotency_Key");
            await Assert.That(parameter.HasDefaultValue).IsFalse();
            await Assert.That(new NullabilityInfoContext().Create(parameter).ReadState).IsEqualTo(NullabilityState.NotNull);
        }
    }

    private sealed class RecordingHandler : HttpMessageHandler
    {
        public List<HttpRequestMessage> Requests { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add(request);
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{}", System.Text.Encoding.UTF8, "application/json")
            });
        }
    }
}
