// ABOUTME: Specifies the sole v1alpha1 instance document and canonical paid-policy revision authority.
// ABOUTME: Proves sovereign fields, caller-selected revisions, and cross-scope broadening fail safely.

namespace Event.Application.UnitTests.Features.ConfigurationManifest;

using System.Reflection;
using System.Text.Json;
using Event.Application.UnitTests.Features.ConfigurationManifest;
using Explore.Application.Features.ConfigurationManifest.Catalog;
using Explore.Application.Features.ConfigurationManifest.Contracts;
using Explore.Application.Features.ConfigurationManifest.Serialization;
using Explore.Application.Features.PaidEventPolicies;
using Explore.Application.Features.ConfigurationManifest.Compilation;
using Explore.Application.Features.ConfigurationManifest.Preflight;
using Explore.Application.Features.ConfigurationManifest.Validation;

public sealed class ConfigurationManifestPaidEventPolicyAuthorityTests
{
    private const string SuppliedValue = "sovereign-value-sentinel";

    [Test]
    public async Task InstanceDocumentCatalog_ContainsOnlyCanonicalPaidEventPolicy()
    {
        PropertyInfo? property = typeof(ConfigurationManifestCatalog).GetProperty(
            "InstanceDocuments",
            BindingFlags.Public | BindingFlags.Static);

        await Assert.That(property).IsNotNull();
        if (property is null)
            return;

        var documents = property.GetValue(null)
            as IReadOnlyDictionary<string, ConfigurationManifestDocumentCatalogEntry>;
        await Assert.That(documents).IsNotNull();
        if (documents is null)
            return;

        await Assert.That(documents.Keys.SequenceEqual(
        [
            ConfigurationManifestDocumentKeys.InstancePaidEventPolicy
        ])).IsTrue();
        ConfigurationManifestDocumentCatalogEntry entry =
            documents[ConfigurationManifestDocumentKeys.InstancePaidEventPolicy];
        await Assert.That(entry.Scope)
            .IsEqualTo(ConfigurationManifestScope.Instance);
        await Assert.That(entry.SchemaVersion).IsEqualTo(1);
        await Assert.That(entry.PayloadType)
            .IsEqualTo(typeof(ConfigurationManifestPaidEventPolicyPayloadV1Alpha1));
        await Assert.That(entry.Storage)
            .IsEqualTo(ConfigurationManifestDocumentStorage.PaidEventPolicy);
    }

    [Test]
    [Arguments("instance.arbitrary_json")]
    [Arguments(ConfigurationManifestDocumentKeys.TenantPaidEventPolicy)]
    public async Task Validate_UnapprovedInstanceDocument_FailsClosed(string key)
    {
        ConfigurationManifestValidationResult result =
            ConfigurationManifestValidator.Validate(
                ConfigurationManifestTestData.Valid(
                    instanceDocuments: new Dictionary<
                        string,
                        ConfigurationManifestDocumentV1Alpha1>(
                        StringComparer.Ordinal)
                    {
                        [key] = Document(InstancePolicyPayload())
                    }));

        ConfigurationManifestValidationError? error = result.Errors
            .SingleOrDefault(candidate =>
                candidate.Code
                    == ConfigurationManifestFailureCodes.DocumentInvalid);

        await Assert.That(error).IsNotNull();
        if (error is null)
            return;

        await Assert.That(error.Path)
            .IsEqualTo($"$.spec.instance.documents.{key}");
    }

    [Test]
    public async Task Validate_NullInstanceDocument_FailsClosed()
    {
        var documents =
            new Dictionary<string, ConfigurationManifestDocumentV1Alpha1>(
                StringComparer.Ordinal)
            {
                [ConfigurationManifestDocumentKeys.InstancePaidEventPolicy] = null!
            };

        ConfigurationManifestValidationResult result =
            ConfigurationManifestValidator.Validate(
                ConfigurationManifestTestData.Valid(
                    instanceDocuments: documents));

        await Assert.That(result.Errors.Any(error =>
            error.Code == ConfigurationManifestFailureCodes.DocumentInvalid
            && error.Path
                == "$.spec.instance.documents.instance.paid_event_policy")).IsTrue();
    }

    [Test]
    public async Task Validate_NullTenantPolicyWithProposedInstancePolicy_FailsClosed()
    {
        var instanceDocuments =
            new Dictionary<string, ConfigurationManifestDocumentV1Alpha1>(
                StringComparer.Ordinal)
            {
                [ConfigurationManifestDocumentKeys.InstancePaidEventPolicy] =
                    Document(InstancePolicyPayload())
            };
        var tenantDocuments =
            new Dictionary<string, ConfigurationManifestDocumentV1Alpha1>(
                StringComparer.Ordinal)
            {
                [ConfigurationManifestDocumentKeys.TenantPaidEventPolicy] = null!
            };

        ConfigurationManifestValidationResult result =
            ConfigurationManifestValidator.Validate(
                ConfigurationManifestTestData.Valid(
                    documents: tenantDocuments,
                    instanceDocuments: instanceDocuments));

        await Assert.That(result.Errors.Any(error =>
            error.Code == ConfigurationManifestFailureCodes.DocumentInvalid
            && error.Path
                == "$.spec.tenants[0].spec.documents['tenant.paid_event_policy']"))
            .IsTrue();
    }

    [Test]
    public async Task Validate_NullSovereignLockedFields_FailsClosed()
    {
        ConfigurationManifestV1Alpha1 valid =
            ConfigurationManifestTestData.Valid();
        ConfigurationManifestV1Alpha1 manifest = valid with
        {
            Metadata = valid.Metadata with
            {
                Export = new ConfigurationManifestExportMetadataV1Alpha1
                {
                    View = ConfigurationManifestExportMetadataValues.OverridesView,
                    EffectiveValuesFlattened = false,
                    SensitiveValuesOmitted = true,
                    AuthorityScope = ConfigurationManifestExportMetadataValues
                        .InstanceAndTenantsAuthorityScope,
                    SovereignValuesOmitted = true,
                    SovereignLockedFields = null!
                }
            }
        };

        ConfigurationManifestValidationResult result =
            ConfigurationManifestValidator.Validate(manifest);

        await Assert.That(result.Errors.Any(error =>
            error.Code == ConfigurationManifestFailureCodes.ContractInvalid
            && error.Path == "$.metadata.export")).IsTrue();
    }

    [Test]
    public async Task PaidPolicyPayload_ContainsOnlyManifestOwnedPolicyFields()
    {
        string[] actual = typeof(ConfigurationManifestPaidEventPolicyPayloadV1Alpha1)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Select(property => property.Name)
            .Order(StringComparer.Ordinal)
            .ToArray();
        string[] expected =
        [
            "AllowedCurrencyCodes",
            "AllowedOrganizerKindIds",
            "CurrencyRiskLimits",
            "DefaultCurrencyCode",
            "FarFutureReviewThresholdDays",
            "IsPaymentsEnabled",
            "RefundProtectionIds",
            "RequiresFirstPaidEventReview",
            "RequiresLocalVerification"
        ];

        await Assert.That(actual.SequenceEqual(expected, StringComparer.Ordinal))
            .IsTrue();
    }

    [Test]
    public async Task CurrencyRiskLimit_ContainsOnlyPortableCeilingFields()
    {
        string[] actual =
            typeof(ConfigurationManifestPaidEventPolicyCurrencyRiskLimitV1Alpha1)
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Select(property => property.Name)
                .Order(StringComparer.Ordinal)
                .ToArray();
        string[] expected =
        [
            "CurrencyCode",
            "HighValueReviewThresholdMinor",
            "PerEventSalesCeilingMinor",
            "PerEventSalesCountCeiling",
            "RollingOrganizerSalesCeilingMinor",
            "RollingOrganizerSalesCountCeiling",
            "RollingOrganizerWindowDays"
        ];

        await Assert.That(actual.SequenceEqual(expected, StringComparer.Ordinal))
            .IsTrue();
    }

    [Test]
    public async Task PaidPolicyPayload_RejectsCallerSelectedInstanceRevision()
    {
        string payload = AddProperty(
            InstancePolicyPayload(),
            "instancePolicyVersion",
            "777");
        JsonException? exception = null;

        try
        {
            _ = JsonSerializer.Deserialize(
                payload,
                ConfigurationManifestJsonContext.Default
                    .ConfigurationManifestPaidEventPolicyPayloadV1Alpha1);
        }
        catch (JsonException caught)
        {
            exception = caught;
        }

        await Assert.That(exception).IsNotNull();
        if (exception is null)
            return;

        await Assert.That(exception.Message.Contains(
            "777",
            StringComparison.Ordinal)).IsFalse();
    }

    [Test]
    [Arguments("saleControl")]
    [Arguments("reviewDecision")]
    [Arguments("paymentHandoff")]
    [Arguments("reconciliationState")]
    [Arguments("buyerAcceptance")]
    [Arguments("liabilityAllocation")]
    [Arguments("disputeHandling")]
    [Arguments("negativeBalanceLiability")]
    [Arguments("refundExecution")]
    public async Task Validate_SovereignInstancePolicyField_RejectsWithoutValueReflection(
        string field)
    {
        string payload = AddProperty(
            InstancePolicyPayload(),
            field,
            $"\"{SuppliedValue}\"");
        ConfigurationManifestValidationResult result =
            ConfigurationManifestValidator.Validate(
                ManifestWithPolicies(instancePayload: payload));

        await Assert.That(JsonSerializer.Serialize(result).Contains(
            SuppliedValue,
            StringComparison.Ordinal)).IsFalse();
        await Assert.That(result.Errors.Any(candidate =>
            candidate.Code
                == ConfigurationManifestFailureCodes.DocumentInvalid)).IsTrue();
    }

    [Test]
    public async Task Validate_InstanceAndNarrowingTenantPolicies_UsesProposedInstance()
    {
        ConfigurationManifestValidationResult result =
            ConfigurationManifestValidator.Validate(
                ManifestWithPolicies(
                    instancePayload: InstancePolicyPayload(),
                    tenantPayload: TenantPolicyPayload()));

        await Assert.That(result.IsValid).IsTrue();
    }

    [Test]
    public async Task Validate_TenantBroadeningProposedInstancePolicy_FailsBeforePersistence()
    {
        string disabledInstance = InstancePolicyPayload()
            .Replace(
                "\"isPaymentsEnabled\": true",
                "\"isPaymentsEnabled\": false",
                StringComparison.Ordinal);
        ConfigurationManifestValidationResult result =
            ConfigurationManifestValidator.Validate(
                ManifestWithPolicies(
                    instancePayload: disabledInstance,
                    tenantPayload: TenantPolicyPayload()));

        ConfigurationManifestValidationError? error = result.Errors
            .SingleOrDefault(candidate =>
                candidate.Code
                    == ConfigurationManifestFailureCodes.CrossReferenceInvalid);

        await Assert.That(error).IsNotNull();
        if (error is null)
            return;

        await Assert.That(error.ReasonCode)
            .IsEqualTo(
                ConfigurationManifestApplicationFailureCodes
                    .PaidPolicyBroadening);
    }

    [Test]
    public async Task ApplyPlan_StaleInstanceRevisionIsFencedByExpectedActiveVersion()
    {
        PropertyInfo? instanceProperty =
            typeof(ConfigurationManifestApplyPlan).GetProperty(
                nameof(ConfigurationManifestApplyPlan.Instance),
                BindingFlags.Public | BindingFlags.Instance);

        await Assert.That(instanceProperty).IsNotNull();
        if (instanceProperty is null)
            return;

        PropertyInfo? policyProperty =
            instanceProperty.PropertyType.GetProperty(
                nameof(ConfigurationManifestInstancePlan.PaidEventPolicy),
                BindingFlags.Public | BindingFlags.Instance);
        await Assert.That(policyProperty).IsNotNull();
        if (policyProperty is null)
            return;

        await Assert.That(policyProperty.PropertyType.GetProperty(
            "ExpectedActivePolicyVersion",
            BindingFlags.Public | BindingFlags.Instance)).IsNotNull();
    }

    [Test]
    public async Task PaidPolicyBoundary_ConcurrentInstanceRevisionUsesExpectedVersionFence()
    {
        MethodInfo? method = typeof(IPaidEventPolicyMutationBoundary).GetMethod(
            "ReviseInstanceInCurrentTransactionAsync",
            BindingFlags.Public | BindingFlags.Instance);

        await Assert.That(method).IsNotNull();
        if (method is null)
            return;

        ParameterInfo input = method.GetParameters()[0];
        await Assert.That(input.ParameterType.GetProperty(
            "ExpectedActivePolicyVersion",
            BindingFlags.Public | BindingFlags.Instance)).IsNotNull();
    }

    private static ConfigurationManifestV1Alpha1 ManifestWithPolicies(
        string instancePayload,
        string? tenantPayload = null)
    {
        var instanceDocuments =
            new Dictionary<string, ConfigurationManifestDocumentV1Alpha1>(
                StringComparer.Ordinal)
            {
                [ConfigurationManifestDocumentKeys.InstancePaidEventPolicy] =
                    Document(instancePayload)
            };
        IReadOnlyDictionary<string, ConfigurationManifestDocumentV1Alpha1>
            tenantDocuments = tenantPayload is null
                ? new Dictionary<
                    string,
                    ConfigurationManifestDocumentV1Alpha1>(
                    StringComparer.Ordinal)
                : new Dictionary<
                    string,
                    ConfigurationManifestDocumentV1Alpha1>(
                    StringComparer.Ordinal)
                {
                    [ConfigurationManifestDocumentKeys.TenantPaidEventPolicy] =
                        Document(tenantPayload)
                };

        return ConfigurationManifestTestData.Valid(
            documents: tenantDocuments,
            instanceDocuments: instanceDocuments);
    }

    private static ConfigurationManifestDocumentV1Alpha1 Document(
        string payload) =>
        new()
        {
            SchemaVersion = 1,
            Payload = ConfigurationManifestTestData.Json(payload)
        };

    private static string InstancePolicyPayload() =>
        """
        {
          "isPaymentsEnabled": true,
          "allowedOrganizerKindIds": [2],
          "requiresLocalVerification": true,
          "allowedCurrencyCodes": ["USD"],
          "defaultCurrencyCode": "USD",
          "refundProtectionIds": [1, 2, 3, 4, 5, 6, 7],
          "currencyRiskLimits": [
            {
              "currencyCode": "USD",
              "perEventSalesCeilingMinor": 10000,
              "perEventSalesCountCeiling": 100,
              "rollingOrganizerSalesCeilingMinor": 50000,
              "rollingOrganizerSalesCountCeiling": 500,
              "rollingOrganizerWindowDays": 30,
              "highValueReviewThresholdMinor": 5000
            }
          ],
          "requiresFirstPaidEventReview": true,
          "farFutureReviewThresholdDays": 90
        }
        """;

    private static string TenantPolicyPayload() =>
        """
        {
          "isPaymentsEnabled": true,
          "allowedOrganizerKindIds": [2],
          "requiresLocalVerification": true,
          "allowedCurrencyCodes": ["USD"],
          "defaultCurrencyCode": "USD",
          "refundProtectionIds": [1, 2, 3, 4, 5, 6, 7],
          "currencyRiskLimits": [
            {
              "currencyCode": "USD",
              "perEventSalesCeilingMinor": 8000,
              "perEventSalesCountCeiling": 80,
              "rollingOrganizerSalesCeilingMinor": 40000,
              "rollingOrganizerSalesCountCeiling": 400,
              "rollingOrganizerWindowDays": 30,
              "highValueReviewThresholdMinor": 4000
            }
          ],
          "requiresFirstPaidEventReview": true,
          "farFutureReviewThresholdDays": 60
        }
        """;

    private static string AddProperty(
        string payload,
        string propertyName,
        string jsonValue)
    {
        int closingBrace = payload.LastIndexOf('}');
        if (closingBrace < 0)
        {
            throw new InvalidOperationException(
                "Paid-policy test payload is not a JSON object.");
        }

        return payload.Insert(
            closingBrace,
            $",\n  \"{propertyName}\": {jsonValue}\n");
    }
}
