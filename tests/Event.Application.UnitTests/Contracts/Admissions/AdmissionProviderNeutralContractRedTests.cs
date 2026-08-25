// ABOUTME: Ratchets the explicit repository-native Task 20.2 issuance contract against provider leakage.
// ABOUTME: Later revocation and recovery service contracts remain owned by Tasks 20.4 and 20.6.

using ApplicationUnitTests.Contracts.Admissions.Support;

namespace ApplicationUnitTests.Contracts.Admissions;

public sealed class AdmissionProviderNeutralContractRedTests
{
    private static readonly string[] ExplicitContractTypes =
    [
        "AdmissionIssuanceService",
        "AdmissionIssuanceRequest",
        "AdmissionIssuanceContext",
        "AdmissionAssignmentFact",
        "AdmissionIssuancePersistenceRequest",
        "AdmissionIssuanceResult",
        "AdmissionIssuanceOutcome",
        "AdmissionDeliveryOutcome",
        "AdmissionDeliveryFailure",
        "AdmissionCredentialDeliveryEnvelope",
        "AdmissionCredentialDirectDeliveryRequest",
        "AdmissionCredentialDirectDeliveryOutcome",
        "AdmissionCredentialDirectDeliveryResult",
        "AdmissionCredentialCreateRequest",
        "AdmissionCredentialVerificationRequest",
        "AdmissionCredentialVerificationOutcome",
        "AdmissionCredentialMaterial",
        "AdmissionProtectedDeliveryMaterial",
        "AdmissionDeliveryIntent",
        "AdmissionDeliveryDispatchRequest",
        "AdmissionDeliveryDispatchResult",
        "IAdmissionIssuanceRepository",
        "IAdmissionCredentialDigestService",
        "IAdmissionDeliveryEnvelopeProtector",
        "IAdmissionDeliveryDispatcher",
        "IAdmissionCredentialDirectDeliveryChannel",
        "IAdmissionCredentialDeliveryOutboxHandler"
    ];

    [Test]
    public async Task ExplicitAdmissionServiceSignaturesContainNoPaymentProviderOrSdkTypes()
    {
        Type nestedSentinel = typeof(Tuple<PaymentIntentSdkSentinel?, IReadOnlyList<StripeSdkSentinel[]>>);
        IReadOnlyCollection<Type> sentinelGraph = ProviderNeutralTypeGraph.Closure([nestedSentinel]);
        await Assert.That(sentinelGraph.Contains(typeof(PaymentIntentSdkSentinel))).IsTrue();
        await Assert.That(sentinelGraph.Contains(typeof(StripeSdkSentinel))).IsTrue();
        await Assert.That(sentinelGraph.Contains(typeof(ProviderSentinelBase))).IsTrue();
        await Assert.That(sentinelGraph.Contains(typeof(IStripeProviderSentinel))).IsTrue();

        IReadOnlyCollection<Type> constructorGraph = ProviderNeutralTypeGraph.Closure(
            AdmissionContractRuntime.PublicSignatureTypes(typeof(AdditionalPublicConstructorSentinel)));
        await Assert.That(constructorGraph.Contains(typeof(PaymentIntentSdkSentinel))).IsTrue();
        await Assert.That(constructorGraph.Contains(typeof(StripeSdkSentinel))).IsTrue();
        await Assert.That(() => AdmissionContractRuntime.ResolveServiceConstructor(
            typeof(AdditionalPublicConstructorSentinel),
            new HashSet<string>(StringComparer.Ordinal))).Throws<InvalidOperationException>();

        Type[] contracts = ExplicitContractTypes.Select(AdmissionContractRuntime.ApplicationType).ToArray();
        IReadOnlyCollection<Type> publicGraph = ProviderNeutralTypeGraph.Closure(
            contracts.SelectMany(AdmissionContractRuntime.PublicSignatureTypes));
        Type[] leaked = publicGraph
            .Where(ProviderNeutralTypeGraph.IsProviderSpecific)
            .Distinct()
            .ToArray();

        await Assert.That(leaked).IsEmpty();
    }

    private interface IStripeProviderSentinel;
    private abstract class ProviderSentinelBase;
    private sealed class StripeSdkSentinel : ProviderSentinelBase, IStripeProviderSentinel;
    private readonly record struct PaymentIntentSdkSentinel(int Value);

    private sealed class AdditionalPublicConstructorSentinel
    {
        public AdditionalPublicConstructorSentinel()
        {
        }

        public AdditionalPublicConstructorSentinel(
            Tuple<PaymentIntentSdkSentinel?, IReadOnlyList<StripeSdkSentinel[]>> providerShape)
        {
            GC.KeepAlive(providerShape);
        }
    }
}
