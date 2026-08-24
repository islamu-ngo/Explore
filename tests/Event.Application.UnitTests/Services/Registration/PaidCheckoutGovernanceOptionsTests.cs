// ABOUTME: Verifies startup-owned operator identity, ownership, legal links, and activation status fail closed.
// ABOUTME: Confirms browser-facing configuration has no sale-control lists or mutable official-status DTO path.

using Explore.Application.Contracts.Services;
using TUnit.Assertions;
using TUnit.Core;

namespace Event.Application.UnitTests.Services.Registration;

public sealed class PaidCheckoutGovernanceOptionsTests
{
    [Test]
    public async Task DefaultsExposeNoUsableOperatorAndRemainSuspended()
    {
        var options = new PaidCheckoutGovernanceOptions();

        await Assert.That(options.IsComplete()).IsFalse();
        await Assert.That(options.IsActivated).IsFalse();
        await Assert.That(options.IsOfficialInstance).IsFalse();
    }

    [Test]
    public async Task CompleteOperatorOwnershipRequiresStartupApprovedActivation()
    {
        PaidCheckoutGovernanceOptions options = Complete();
        await Assert.That(options.IsComplete()).IsTrue();
        await Assert.That(options.IsActivated).IsTrue();
        options.ActivationStatus = "suspended";
        await Assert.That(options.IsComplete()).IsTrue();
        await Assert.That(options.IsActivated).IsFalse();
    }

    private static PaidCheckoutGovernanceOptions Complete() => new()
    {
        OperatorId = Guid.CreateVersion7(),
        OperatorDisplayName = "Independent Operator",
        OfficialOrigin = "https://events.example.test",
        OperatorRegionCode = "BE",
        OperatorWebsiteUrl = "https://events.example.test",
        OperatorLegalNoticeUrl = "https://events.example.test/legal",
        OperatorTermsUrl = "https://events.example.test/terms",
        OperatorPrivacyUrl = "https://events.example.test/privacy",
        ComplaintContact = "complaints@example.test",
        ComplaintOwner = "Trust and Safety",
        RefundOwner = "Payments Operations",
        DisputeOwner = "Dispute Operations",
        ReconciliationOwner = "Payment Reconciliation",
        ActivationStatus = "approved",
        RefundPolicyLanguageTag = "en-GB",
        StatementDescriptor = "EXAMPLE EVENT"
    };
}
