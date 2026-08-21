// ABOUTME: Locks the public registration-payment projection to bounded operational fields only.
// ABOUTME: Prevents provider, capability, idempotency, PII, and raw-error fields from entering generated contracts.

using Explore.Application.DTOs.RegistrationOrders;

namespace ApplicationUnitTests.Features.RegistrationOrders;

public sealed class RegistrationPaymentDtoPrivacyTests
{
    [Test]
    public async Task PaymentProjectionContainsOnlyApprovedFields()
    {
        string[] fields = typeof(RegistrationPaymentDto).GetProperties().Select(property => property.Name).Order().ToArray();

        await Assert.That(fields).IsEquivalentTo(new[]
        {
            "CreatedAt", "ExpiresAt", "FailureCode", "HostedRedirectAvailable", "Id", "LastUpdatedAt",
            "RegistrationOrderId", "RetryAvailable", "StatusCode", "StatusName"
        });
    }
}
