// ABOUTME: Executes the canonical whole-instance manifest handler for Overrides and Portable exports.
// ABOUTME: Proves current entity materialization, closed-catalog resolution, deterministic bytes, and overflow safety.

namespace Event.Application.UnitTests.Features.ConfigurationManifest;

using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Text.Json;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Features.ConfigurationManifest.Catalog;
using Explore.Application.Features.ConfigurationManifest.Contracts;
using Explore.Application.Features.ConfigurationManifest.Handlers.Queries;
using Explore.Application.Features.ConfigurationManifest.Requests.Queries;
using Explore.Application.Features.ConfigurationManifest.Validation;
using Explore.Application.Settings;
using Explore.Domain;
using Explore.Domain.Enums;
using Explore.Domain.Settings;
using Explore.Domain.Settings.Definitions;
using Explore.Domain.Settings.Documents;
using Explore.Domain.Settings.Documents.Payloads;
using FluentValidation;
using NSubstitute;

public sealed class ExportConfigurationManifestQueryHandlerTests
{
    private static readonly Guid AlphaId =
        Guid.Parse("0199464e-e388-7f56-9281-cefabd6a5673");
    private static readonly Guid ZuluId =
        Guid.Parse("0199464e-e388-7f56-9281-cefabd6a5674");

    [Test]
    public async Task HandleOverridesExportsCurrentOwnedConfigurationInSlugOrder()
    {
        var fixture = new Fixture();
        fixture.Tenants.GetAllActiveForConfigurationManifestExportAsync(
                Arg.Any<int>(),
                Arg.Any<CancellationToken>())
            .Returns([Tenant(ZuluId, "z-community"), Tenant(AlphaId, "a-community")]);
        fixture.SystemSettings.GetAllSettings(null, Arg.Any<CancellationToken>())
            .Returns(
            [
                SystemSetting(BrandingSettingDefinitions.DisplayName.Key, "\"Current instance\""),
                SystemSetting("auth.google_client_secret", "\"must-never-export\"")
            ]);
        fixture.TenantSettings.GetByTenantAndKeys(
                Arg.Any<Guid>(),
                Arg.Any<IReadOnlyCollection<string>>(),
                Arg.Any<CancellationToken>())
            .Returns(call =>
            [
                TenantSetting(
                    call.Arg<Guid>(),
                    EventSettingDefinitions.RequireApproval.Key,
                    call.Arg<Guid>() == AlphaId ? "true" : "false"),
                TenantSetting(
                    call.Arg<Guid>() == AlphaId ? ZuluId : AlphaId,
                    EventSettingDefinitions.UserSubmissionEnabled.Key,
                    "true"),
                TenantSetting(call.Arg<Guid>(), "buyerEmail", "\"must-never-export\"")
            ]);
        fixture.TenantDocuments.GetManyForTenant(
                Arg.Any<Guid>(),
                Arg.Any<IEnumerable<string>>(),
                Arg.Any<CancellationToken>())
            .Returns(call =>
            [
                BrandingDocument(call.Arg<Guid>()),
                BrandingDocument(call.Arg<Guid>() == AlphaId ? ZuluId : AlphaId)
            ]);
        fixture.PaidPolicies.GetActiveTenantAsync(
                Arg.Any<Guid>(),
                Arg.Any<CancellationToken>())
            .Returns(call => TenantPolicy(call.Arg<Guid>()));
        fixture.Operations.GetLatestAppliedBootstrapAsync(Arg.Any<CancellationToken>())
            .Returns(AppliedOperation("primary-deployment"));

        ConfigurationManifestExportResult result = await fixture.Handler.Handle(
            new ExportConfigurationManifestQuery(ConfigurationManifestExportView.Overrides),
            CancellationToken.None);

        using JsonDocument export = JsonDocument.Parse(result.Utf8Json);
        JsonElement root = export.RootElement;
        JsonElement instance = root.GetProperty("spec").GetProperty("instance");
        JsonElement tenants = root.GetProperty("spec").GetProperty("tenants");

        await Assert.That(result.View)
            .IsEqualTo(ConfigurationManifestExportView.Overrides);
        await Assert.That(result.FileName)
            .IsEqualTo("configuration-manifest-overrides.json");
        await Assert.That(root.GetProperty("metadata").GetProperty("name").GetString())
            .IsEqualTo("primary-deployment");
        await Assert.That(instance.GetProperty("settings")
            .EnumerateObject().Select(property => property.Name).ToArray())
            .IsEquivalentTo([BrandingSettingDefinitions.DisplayName.Key]);
        await Assert.That(instance.GetProperty("documents")
            .TryGetProperty(ConfigurationManifestDocumentKeys.InstancePaidEventPolicy, out _))
            .IsTrue();
        await Assert.That(tenants[0].GetProperty("metadata").GetProperty("name").GetString())
            .IsEqualTo("a-community");
        await Assert.That(tenants[1].GetProperty("metadata").GetProperty("name").GetString())
            .IsEqualTo("z-community");

        foreach (JsonElement tenant in tenants.EnumerateArray())
        {
            JsonElement spec = tenant.GetProperty("spec");
            await Assert.That(spec.GetProperty("settings")
                .EnumerateObject().Select(property => property.Name).ToArray())
                .IsEquivalentTo([EventSettingDefinitions.RequireApproval.Key]);
            await Assert.That(spec.GetProperty("documents")
                .EnumerateObject().Select(property => property.Name).ToArray())
                .IsEquivalentTo(
                [
                    SettingsDocumentKeys.Tenant.Branding,
                    ConfigurationManifestDocumentKeys.TenantPaidEventPolicy
                ]);
            await Assert.That(spec.GetProperty("documents")
                    .GetProperty(ConfigurationManifestDocumentKeys.TenantPaidEventPolicy)
                    .GetProperty("payload")
                    .GetProperty("requiresLocalVerification")
                    .GetBoolean())
                .IsTrue();
        }

        await Assert.That(result.Utf8Json.AsSpan().IndexOf("must-never-export"u8))
            .IsEqualTo(-1);
        await fixture.Tenants.Received(1)
            .GetAllActiveForConfigurationManifestExportAsync(
                ConfigurationManifestValidator.MaximumTenantCount + 1,
                Arg.Any<CancellationToken>());
        await fixture.Resolver.DidNotReceiveWithAnyArgs()
            .ResolveBatchAsync(default!, default!, default);
        await fixture.TypedDocuments.DidNotReceiveWithAnyArgs()
            .ResolveTenantDocumentAsync<BrandingSettings>(default!, default!, default);
        await fixture.TenantDocuments.Received(2).GetManyForTenant(
            Arg.Any<Guid>(),
            Arg.Is<IEnumerable<string>>(keys =>
                keys.ToHashSet(StringComparer.Ordinal).SetEquals(
                new[]
                {
                    SettingsDocumentKeys.Tenant.Branding
                })),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task HandlePortableResolvesEveryClosedCatalogForEveryTenantDeterministically()
    {
        var fixture = new Fixture();
        fixture.Tenants.GetAllActiveForConfigurationManifestExportAsync(
                Arg.Any<int>(),
                Arg.Any<CancellationToken>())
            .Returns([Tenant(ZuluId, "z-community"), Tenant(AlphaId, "a-community")]);
        fixture.Resolver.ResolveBatchAsync(
                Arg.Any<IEnumerable<string>>(),
                Arg.Any<SettingContext>(),
                Arg.Any<CancellationToken>())
            .Returns(call => call.Arg<IEnumerable<string>>()!
                .Select(key => Resolved(key))
                .ToArray());
        fixture.TypedDocuments.ResolveTenantDocumentAsync<BrandingSettings>(
                Arg.Any<SettingsResolutionContext>(),
                SettingsDocumentKeys.Tenant.Branding,
                Arg.Any<CancellationToken>())
            .Returns(call => ResolvedBranding(
                call.Arg<SettingsResolutionContext>()!.TenantId));

        var query = new ExportConfigurationManifestQuery(
            ConfigurationManifestExportView.Portable);
        ConfigurationManifestExportResult first = await fixture.Handler.Handle(
            query,
            CancellationToken.None);
        ConfigurationManifestExportResult second = await fixture.Handler.Handle(
            query,
            CancellationToken.None);

        using JsonDocument export = JsonDocument.Parse(first.Utf8Json);
        JsonElement metadata = export.RootElement.GetProperty("metadata").GetProperty("export");
        JsonElement spec = export.RootElement.GetProperty("spec");
        string[] expectedInstanceKeys = ConfigurationManifestCatalog.InstanceSettings.Keys
            .Order(StringComparer.Ordinal).ToArray();
        string[] expectedTenantKeys = ConfigurationManifestCatalog.TenantSettings.Keys
            .Order(StringComparer.Ordinal).ToArray();

        await Assert.That(first.View)
            .IsEqualTo(ConfigurationManifestExportView.Portable);
        await Assert.That(first.FileName)
            .IsEqualTo("configuration-manifest-portable.json");
        await Assert.That(second.FileName).IsEqualTo(first.FileName);
        await Assert.That(metadata.GetProperty("view").GetString()).IsEqualTo("Portable");
        await Assert.That(metadata.GetProperty("effectiveValuesFlattened").GetBoolean()).IsTrue();
        await Assert.That(metadata.GetProperty("authorityScope").GetString())
            .IsEqualTo(ConfigurationManifestExportMetadataValues.InstanceAndTenantsAuthorityScope);
        await Assert.That(metadata.GetProperty("sovereignValuesOmitted").GetBoolean()).IsTrue();
        await Assert.That(metadata.GetProperty("sovereignLockedFields")
            .EnumerateArray().Select(value => value.GetString()).ToArray())
            .Contains("providerCredentials");
        await Assert.That(spec.GetProperty("instance").GetProperty("settings")
            .EnumerateObject().Select(property => property.Name).ToArray())
            .IsEquivalentTo(expectedInstanceKeys);
        foreach (JsonElement tenant in spec.GetProperty("tenants").EnumerateArray())
        {
            await Assert.That(tenant.GetProperty("spec").GetProperty("settings")
                .EnumerateObject().Select(property => property.Name).ToArray())
                .IsEquivalentTo(expectedTenantKeys);
            JsonElement documents = tenant.GetProperty("spec").GetProperty("documents");
            await Assert.That(documents
                    .GetProperty(SettingsDocumentKeys.Tenant.Branding)
                    .GetProperty("schemaVersion")
                    .GetInt32())
                .IsEqualTo(TenantBrandingSettingsDocumentDefaults.SchemaVersion);
            await Assert.That(documents
                    .GetProperty(SettingsDocumentKeys.Tenant.Branding)
                    .GetProperty("payload")
                    .GetProperty("displayName")
                    .GetString())
                .IsEqualTo("Community");
            await Assert.That(documents
                    .GetProperty(ConfigurationManifestDocumentKeys.TenantPaidEventPolicy)
                    .GetProperty("payload")
                    .GetProperty("requiresLocalVerification")
                    .GetBoolean())
                .IsFalse();
        }
        await Assert.That(first.Utf8Json).IsEquivalentTo(second.Utf8Json);

        await fixture.Resolver.Received(2).ResolveBatchAsync(
            Arg.Is<IEnumerable<string>>(keys =>
                keys != null && keys.SequenceEqual(expectedInstanceKeys)),
            Arg.Is<SettingContext>(context =>
                context != null && context.TenantId == null),
            Arg.Any<CancellationToken>());
        foreach (Guid tenantId in new[] { AlphaId, ZuluId })
        {
            await fixture.Resolver.Received(2).ResolveBatchAsync(
                Arg.Is<IEnumerable<string>>(keys =>
                    keys != null && keys.SequenceEqual(expectedTenantKeys)),
                Arg.Is<SettingContext>(context =>
                    context != null && context.TenantId == tenantId),
                Arg.Any<CancellationToken>());
            await fixture.TypedDocuments.Received(2)
                .ResolveTenantDocumentAsync<BrandingSettings>(
                    Arg.Is<SettingsResolutionContext>(context =>
                        context.TenantId == tenantId
                        && context.RequestedDocuments != null
                        && context.RequestedDocuments.SequenceEqual(
                        new[]
                        {
                            SettingsDocumentKeys.Tenant.Branding
                        },
                        StringComparer.Ordinal)),
                    SettingsDocumentKeys.Tenant.Branding,
                    Arg.Any<CancellationToken>());
        }
        await fixture.SystemSettings.DidNotReceiveWithAnyArgs()
            .GetAllSettings(default, default);
        await fixture.TenantSettings.DidNotReceiveWithAnyArgs()
            .GetByTenantAndKeys(default, default!, default);
        await fixture.TenantDocuments.DidNotReceiveWithAnyArgs()
            .GetManyForTenant(default, default!, default);
    }

    [Test]
    public async Task HandleRejectsTenantOverflowBeforeInstanceOrPerTenantReads()
    {
        var fixture = new Fixture();
        Tenant[] overflow = Enumerable.Range(
                0,
                ConfigurationManifestValidator.MaximumTenantCount + 1)
            .Select(index => Tenant(
                Guid.CreateVersion7(),
                $"tenant-{index:D3}"))
            .ToArray();
        fixture.Tenants.GetAllActiveForConfigurationManifestExportAsync(
                ConfigurationManifestValidator.MaximumTenantCount + 1,
                Arg.Any<CancellationToken>())
            .Returns(overflow);

        ConfigurationManifestExportTooLargeException? exception = null;
        try
        {
            _ = await fixture.Handler.Handle(
                new ExportConfigurationManifestQuery(),
                CancellationToken.None);
        }
        catch (ConfigurationManifestExportTooLargeException caught)
        {
            exception = caught;
        }

        await Assert.That(exception).IsNotNull();
        await Assert.That(exception!.FailureCode)
            .IsEqualTo(ConfigurationManifestExportContract.TooLargeFailureCode);
        await fixture.SystemSettings.DidNotReceiveWithAnyArgs()
            .GetAllSettings(default, default);
        await fixture.TenantSettings.DidNotReceiveWithAnyArgs()
            .GetByTenantAndKeys(default, default!, default);
        await fixture.TenantDocuments.DidNotReceiveWithAnyArgs()
            .GetManyForTenant(default, default!, default);
        await fixture.PaidPolicies.DidNotReceiveWithAnyArgs()
            .GetActiveInstanceAsync(default);
    }

    [Test]
    public async Task HandleInvalidViewRejectsBeforeRepositoryAccess()
    {
        var fixture = new Fixture();

        await Assert.That(() => fixture.Handler.Handle(
                new ExportConfigurationManifestQuery(
                    (ConfigurationManifestExportView)int.MaxValue),
                CancellationToken.None))
            .Throws<ValidationException>();

        await fixture.Tenants.DidNotReceiveWithAnyArgs()
            .GetAllActiveForConfigurationManifestExportAsync(default, default);
        await fixture.PaidPolicies.DidNotReceiveWithAnyArgs()
            .GetActiveInstanceAsync(default);
    }

    [Test]
    public async Task HandleRejectsEmptyActiveTenantSet()
    {
        var fixture = new Fixture();
        fixture.Tenants.GetAllActiveForConfigurationManifestExportAsync(
                Arg.Any<int>(),
                Arg.Any<CancellationToken>())
            .Returns([]);

        InvalidOperationException? exception = null;
        try
        {
            _ = await fixture.Handler.Handle(
                new ExportConfigurationManifestQuery(),
                CancellationToken.None);
        }
        catch (InvalidOperationException caught)
        {
            exception = caught;
        }

        await Assert.That(exception?.Message)
            .IsEqualTo(
                "Configuration manifest export requires at least one active tenant.");
        await fixture.PaidPolicies.DidNotReceiveWithAnyArgs()
            .GetActiveInstanceAsync(default);
    }

    [Test]
    public async Task HandleAcceptsExactlyMaximumTenantCount()
    {
        var fixture = new Fixture();
        Tenant[] maximum = Enumerable.Range(
                0,
                ConfigurationManifestValidator.MaximumTenantCount)
            .Select(index => Tenant(
                Guid.CreateVersion7(),
                $"tenant-{index:D3}"))
            .ToArray();
        fixture.Tenants.GetAllActiveForConfigurationManifestExportAsync(
                Arg.Any<int>(),
                Arg.Any<CancellationToken>())
            .Returns(maximum);

        ConfigurationManifestExportResult result = await fixture.Handler.Handle(
            new ExportConfigurationManifestQuery(),
            CancellationToken.None);

        using JsonDocument export = JsonDocument.Parse(result.Utf8Json);
        await Assert.That(export.RootElement
                .GetProperty("spec")
                .GetProperty("tenants")
                .GetArrayLength())
            .IsEqualTo(ConfigurationManifestValidator.MaximumTenantCount);
    }

    [Test]
    [Arguments("missing")]
    [Arguments("inactive")]
    [Arguments("tenant-scoped")]
    public async Task HandleRejectsInvalidInstancePolicyBeforeConfigurationReads(
        string scenario)
    {
        var fixture = new Fixture();
        fixture.Tenants.GetAllActiveForConfigurationManifestExportAsync(
                Arg.Any<int>(),
                Arg.Any<CancellationToken>())
            .Returns([Tenant(AlphaId, "a-community")]);
        PaidEventPolicyVersion? invalid = scenario switch
        {
            "missing" => null,
            "inactive" => RetiredInstancePolicy(),
            "tenant-scoped" => TenantPolicy(AlphaId),
            _ => throw new ArgumentOutOfRangeException(nameof(scenario))
        };
        fixture.PaidPolicies.GetActiveInstanceAsync(Arg.Any<CancellationToken>())
            .Returns(_ => invalid);

        InvalidOperationException? exception = null;
        try
        {
            _ = await fixture.Handler.Handle(
                new ExportConfigurationManifestQuery(),
                CancellationToken.None);
        }
        catch (InvalidOperationException caught)
        {
            exception = caught;
        }

        await Assert.That(exception?.Message)
            .IsEqualTo(
                "An active instance paid-event policy is required for configuration manifest export.");
        await fixture.SystemSettings.DidNotReceiveWithAnyArgs()
            .GetAllSettings(default, default);
        await fixture.TenantSettings.DidNotReceiveWithAnyArgs()
            .GetByTenantAndKeys(default, default!, default);
        await fixture.TenantDocuments.DidNotReceiveWithAnyArgs()
            .GetManyForTenant(default, default!, default);
        await fixture.Operations.DidNotReceiveWithAnyArgs()
            .GetLatestAppliedBootstrapAsync(default);
    }

    [Test]
    public async Task HandleRejectsTenantPolicyOwnedByDifferentTenant()
    {
        var fixture = new Fixture();
        fixture.Tenants.GetAllActiveForConfigurationManifestExportAsync(
                Arg.Any<int>(),
                Arg.Any<CancellationToken>())
            .Returns([Tenant(AlphaId, "a-community")]);
        fixture.PaidPolicies.GetActiveTenantAsync(
                AlphaId,
                Arg.Any<CancellationToken>())
            .Returns(TenantPolicy(ZuluId));

        InvalidOperationException? exception = null;
        try
        {
            _ = await fixture.Handler.Handle(
                new ExportConfigurationManifestQuery(),
                CancellationToken.None);
        }
        catch (InvalidOperationException caught)
        {
            exception = caught;
        }

        await Assert.That(exception?.Message)
            .IsEqualTo(
                "The paid-event policy repository returned a policy for a different tenant.");
        await fixture.Operations.DidNotReceiveWithAnyArgs()
            .GetLatestAppliedBootstrapAsync(default);
    }

    [Test]
    public async Task HandleRejectsBroadTenantPolicyBeforeLatestOperationRead()
    {
        var fixture = new Fixture();
        fixture.Tenants.GetAllActiveForConfigurationManifestExportAsync(
                Arg.Any<int>(),
                Arg.Any<CancellationToken>())
            .Returns([Tenant(AlphaId, "a-community")]);
        fixture.PaidPolicies.GetActiveTenantAsync(
                AlphaId,
                Arg.Any<CancellationToken>())
            .Returns(BroadTenantPolicy(AlphaId));

        InvalidOperationException? exception = null;
        try
        {
            _ = await fixture.Handler.Handle(
                new ExportConfigurationManifestQuery(),
                CancellationToken.None);
        }
        catch (InvalidOperationException caught)
        {
            exception = caught;
        }

        await Assert.That(exception?.Message)
            .IsEqualTo(
                "Tenant paid-event policy cannot add organizer kinds outside the instance ceiling.");
        await fixture.Operations.DidNotReceiveWithAnyArgs()
            .GetLatestAppliedBootstrapAsync(default);
    }

    [Test]
    [Arguments(true)]
    [Arguments(false)]
    public async Task HandleRejectsTenantDocumentWhenEitherCatalogVersionDiffers(
        bool schemaDiffers)
    {
        var fixture = new Fixture();
        fixture.Tenants.GetAllActiveForConfigurationManifestExportAsync(
                Arg.Any<int>(),
                Arg.Any<CancellationToken>())
            .Returns([Tenant(AlphaId, "a-community")]);
        fixture.TenantDocuments.GetManyForTenant(
                AlphaId,
                Arg.Any<IEnumerable<string>>(),
                Arg.Any<CancellationToken>())
            .Returns(
            [
                BrandingDocument(
                    AlphaId,
                    schemaVersion: schemaDiffers
                        ? TenantBrandingSettingsDocumentDefaults.SchemaVersion + 1
                        : TenantBrandingSettingsDocumentDefaults.SchemaVersion,
                    defaultsVersion: schemaDiffers
                        ? TenantBrandingSettingsDocumentDefaults.DefaultsVersion
                        : "unexpected")
            ]);

        InvalidOperationException? exception = null;
        try
        {
            _ = await fixture.Handler.Handle(
                new ExportConfigurationManifestQuery(),
                CancellationToken.None);
        }
        catch (InvalidOperationException caught)
        {
            exception = caught;
        }

        await Assert.That(exception?.Message)
            .IsEqualTo(
                "A tenant settings document does not match the configuration manifest catalog version.");
        await fixture.Operations.DidNotReceiveWithAnyArgs()
            .GetLatestAppliedBootstrapAsync(default);
    }

    [Test]
    public async Task HandleRejectsIncompleteResolvedSettingCatalog()
    {
        var fixture = new Fixture();
        fixture.Tenants.GetAllActiveForConfigurationManifestExportAsync(
                Arg.Any<int>(),
                Arg.Any<CancellationToken>())
            .Returns([Tenant(AlphaId, "a-community")]);
        fixture.Resolver.ResolveBatchAsync(
                Arg.Any<IEnumerable<string>>(),
                Arg.Any<SettingContext>(),
                Arg.Any<CancellationToken>())
            .Returns([]);

        InvalidOperationException? exception = null;
        try
        {
            _ = await fixture.Handler.Handle(
                new ExportConfigurationManifestQuery(
                    ConfigurationManifestExportView.Portable),
                CancellationToken.None);
        }
        catch (InvalidOperationException caught)
        {
            exception = caught;
        }

        await Assert.That(exception?.Message)
            .IsEqualTo(
                "The settings resolver returned an incomplete configuration manifest catalog.");
    }

    [Test]
    public async Task HandleRejectsUnexpectedResolvedSettingOrder()
    {
        var fixture = new Fixture();
        fixture.Tenants.GetAllActiveForConfigurationManifestExportAsync(
                Arg.Any<int>(),
                Arg.Any<CancellationToken>())
            .Returns([Tenant(AlphaId, "a-community")]);
        fixture.Resolver.ResolveBatchAsync(
                Arg.Any<IEnumerable<string>>(),
                Arg.Any<SettingContext>(),
                Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                string[] keys = call.Arg<IEnumerable<string>>().ToArray();
                return keys.Select((key, index) =>
                        Resolved(index == 0 ? keys[1] : key))
                    .ToArray();
            });

        InvalidOperationException? exception = null;
        try
        {
            _ = await fixture.Handler.Handle(
                new ExportConfigurationManifestQuery(
                    ConfigurationManifestExportView.Portable),
                CancellationToken.None);
        }
        catch (InvalidOperationException caught)
        {
            exception = caught;
        }

        await Assert.That(exception?.Message)
            .IsEqualTo(
                "The settings resolver returned an unexpected configuration manifest key.");
    }

    [Test]
    public async Task HandleRejectsDuplicateStoredTenantDocuments()
    {
        var fixture = new Fixture();
        fixture.Tenants.GetAllActiveForConfigurationManifestExportAsync(
                Arg.Any<int>(),
                Arg.Any<CancellationToken>())
            .Returns([Tenant(AlphaId, "a-community")]);
        fixture.TenantDocuments.GetManyForTenant(
                AlphaId,
                Arg.Any<IEnumerable<string>>(),
                Arg.Any<CancellationToken>())
            .Returns(
            [
                BrandingDocument(AlphaId),
                BrandingDocument(AlphaId)
            ]);

        InvalidOperationException? exception = null;
        try
        {
            _ = await fixture.Handler.Handle(
                new ExportConfigurationManifestQuery(),
                CancellationToken.None);
        }
        catch (InvalidOperationException caught)
        {
            exception = caught;
        }

        await Assert.That(exception?.Message)
            .IsEqualTo(
                "Duplicate tenant settings documents were returned for configuration manifest export.");
    }

    [Test]
    public async Task HandleRejectsDuplicateStoredInstanceSettings()
    {
        var fixture = new Fixture();
        fixture.Tenants.GetAllActiveForConfigurationManifestExportAsync(
                Arg.Any<int>(),
                Arg.Any<CancellationToken>())
            .Returns([Tenant(AlphaId, "a-community")]);
        fixture.SystemSettings.GetAllSettings(null, Arg.Any<CancellationToken>())
            .Returns(
            [
                SystemSetting(BrandingSettingDefinitions.DisplayName.Key, "\"One\""),
                SystemSetting(BrandingSettingDefinitions.DisplayName.Key, "\"Two\"")
            ]);

        InvalidOperationException? exception = null;
        try
        {
            _ = await fixture.Handler.Handle(
                new ExportConfigurationManifestQuery(),
                CancellationToken.None);
        }
        catch (InvalidOperationException caught)
        {
            exception = caught;
        }

        await Assert.That(exception?.Message)
            .IsEqualTo(
                "Duplicate settings were returned for configuration manifest export.");
    }

    [Test]
    public async Task HandleCancellationStopsBeforeFirstTenantExport()
    {
        var fixture = new Fixture();
        using var cancellation = new CancellationTokenSource();
        fixture.Tenants.GetAllActiveForConfigurationManifestExportAsync(
                Arg.Any<int>(),
                Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                cancellation.Cancel();
                return new[] { Tenant(AlphaId, "a-community") };
            });

        await Assert.That(() => fixture.Handler.Handle(
                new ExportConfigurationManifestQuery(),
                cancellation.Token))
            .Throws<OperationCanceledException>();

        await fixture.TenantSettings.DidNotReceiveWithAnyArgs()
            .GetByTenantAndKeys(default, default!, default);
        await fixture.TenantDocuments.DidNotReceiveWithAnyArgs()
            .GetManyForTenant(default, default!, default);
    }

    [Test]
    [Arguments("http://unsafe.example/logo.svg")]
    [Arguments("https://unsafe.example/logo.svg?token=export-secret")]
    [Arguments("https://unsafe.example/logo.svg#user@example.test")]
    [Arguments("https://user@unsafe.example/logo.svg")]
    public async Task HandleRejectsUnsafePersistedBrandingUrlBeforeSerialization(
        string unsafeUrl)
    {
        var fixture = new Fixture();
        fixture.Tenants.GetAllActiveForConfigurationManifestExportAsync(
                Arg.Any<int>(),
                Arg.Any<CancellationToken>())
            .Returns([Tenant(AlphaId, "a-community")]);
        fixture.TenantDocuments.GetManyForTenant(
                AlphaId,
                Arg.Any<IEnumerable<string>>(),
                Arg.Any<CancellationToken>())
            .Returns(
            [
                new TenantSettingsDocument
                {
                    Id = Guid.CreateVersion7(),
                    TenantId = AlphaId,
                    Tenant = null!,
                    DocumentKey = SettingsDocumentKeys.Tenant.Branding,
                    SchemaVersion = TenantBrandingSettingsDocumentDefaults.SchemaVersion,
                    DefaultsVersion = TenantBrandingSettingsDocumentDefaults.DefaultsVersion,
                    PayloadJson = JsonSerializer.Serialize(new { logoUrl = unsafeUrl }),
                    ConcurrencyStamp = Guid.CreateVersion7()
                }
            ]);

        InvalidOperationException? exception = null;
        try
        {
            _ = await fixture.Handler.Handle(
                new ExportConfigurationManifestQuery(),
                CancellationToken.None);
        }
        catch (InvalidOperationException caught)
        {
            exception = caught;
        }

        await Assert.That(exception?.Message)
            .IsEqualTo(
                "Current branding configuration contains a URL that is unsafe to export.");
    }

    [Test]
    public async Task CanonicalSerializerThrowsBeforeReturningOversizedBytes()
    {
        ConfigurationManifestV1Alpha1 manifest = OversizedManifest();
        byte[]? returned = null;

        await Assert.That(() => returned = Serialize(manifest))
            .Throws<ConfigurationManifestExportTooLargeException>();
        await Assert.That(returned).IsNull();
    }

    private static byte[] Serialize(ConfigurationManifestV1Alpha1 manifest)
    {
        Type serializer = typeof(ConfigurationManifestV1Alpha1).Assembly.GetTypes()
            .Single(type => type.Name == "ConfigurationManifestExportJsonSerializer");
        MethodInfo method = serializer.GetMethod(
            "Serialize",
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static,
            binder: null,
            types: [typeof(ConfigurationManifestV1Alpha1)],
            modifiers: null)!;
        try
        {
            return (byte[])method.Invoke(null, [manifest])!;
        }
        catch (TargetInvocationException exception) when (exception.InnerException is not null)
        {
            ExceptionDispatchInfo.Capture(exception.InnerException).Throw();
            throw;
        }
    }

    private static ConfigurationManifestV1Alpha1 OversizedManifest() =>
        new()
        {
            Schema = ConfigurationManifestContractMetadata.SchemaId,
            ApiVersion = ConfigurationManifestContractMetadata.ApiVersion,
            Kind = ConfigurationManifestContractMetadata.Kind,
            Metadata = new ConfigurationManifestMetadataV1Alpha1 { Name = "current-instance" },
            Spec = new ConfigurationManifestSpecV1Alpha1
            {
                Instance = new ConfigurationManifestInstanceV1Alpha1
                {
                    Settings = new Dictionary<string, JsonElement>(StringComparer.Ordinal)
                    {
                        [BrandingSettingDefinitions.DisplayName.Key] =
                            JsonSerializer.SerializeToElement(new string(
                                'x',
                                ConfigurationManifestExportContract.MaximumUtf8Bytes))
                    },
                    Documents = new Dictionary<string, ConfigurationManifestDocumentV1Alpha1>(
                        StringComparer.Ordinal)
                },
                Tenants =
                [
                    new ConfigurationManifestTenantV1Alpha1
                    {
                        Metadata = new ConfigurationManifestTenantMetadataV1Alpha1
                        {
                            Name = "primary"
                        },
                        Spec = new ConfigurationManifestTenantSpecV1Alpha1
                        {
                            DisplayName = "Primary",
                            Settings = new Dictionary<string, JsonElement>(StringComparer.Ordinal),
                            Documents = new Dictionary<string, ConfigurationManifestDocumentV1Alpha1>(
                                StringComparer.Ordinal)
                        }
                    }
                ]
            }
        };

    private static Tenant Tenant(Guid id, string slug) => new()
    {
        Id = id,
        Slug = slug,
        FullName = $"{slug} display name",
        TenantStatus = null!
    };

    private static SystemSetting SystemSetting(string key, string value) => new()
    {
        Id = Guid.CreateVersion7(),
        SettingKey = key,
        Value = value
    };

    private static TenantSetting TenantSetting(Guid tenantId, string key, string value) => new()
    {
        Id = Guid.CreateVersion7(),
        TenantId = tenantId,
        Tenant = null!,
        SettingKey = key,
        Value = value
    };

    private static TenantSettingsDocument BrandingDocument(
        Guid tenantId,
        int? schemaVersion = null,
        string? defaultsVersion = null) => new()
    {
        Id = Guid.CreateVersion7(),
        TenantId = tenantId,
        Tenant = null!,
        DocumentKey = SettingsDocumentKeys.Tenant.Branding,
        SchemaVersion =
            schemaVersion ?? TenantBrandingSettingsDocumentDefaults.SchemaVersion,
        DefaultsVersion =
            defaultsVersion ?? TenantBrandingSettingsDocumentDefaults.DefaultsVersion,
        PayloadJson = "{\"displayName\":\"Community\",\"logoUrl\":null,\"faviconUrl\":null,\"customCssUrl\":null}",
        ConcurrencyStamp = Guid.CreateVersion7()
    };

    private static ResolvedSetting Resolved(string key)
    {
        SettingDefinition definition = ConfigurationManifestCatalog.InstanceSettings
            .TryGetValue(key, out ConfigurationManifestSettingCatalogEntry? instance)
            ? instance.Definition
            : ConfigurationManifestCatalog.TenantSettings[key].Definition;
        return new ResolvedSetting
        {
            Key = key,
            Value = definition.DefaultValue,
            ValueType = definition.ValueType,
            Source = SettingSource.SystemDefault,
            IsLocked = false
        };
    }

    private static ResolvedSettingsDocument<BrandingSettings> ResolvedBranding(Guid tenantId) =>
        new()
        {
            DocumentKey = SettingsDocumentKeys.Tenant.Branding,
            SchemaVersion = TenantBrandingSettingsDocumentDefaults.SchemaVersion,
            DefaultsVersion = TenantBrandingSettingsDocumentDefaults.DefaultsVersion,
            Payload = new BrandingSettings { DisplayName = "Community" },
            Source = SettingsDocumentSource.Tenant,
            SourceScopeId = tenantId,
            ConcurrencyStamp = Guid.Parse("0199464e-e388-7f56-9281-cefabd6a5675")
        };

    private static PaidEventPolicyVersion InstancePolicy() =>
        PaidEventPolicyVersion.CreateDefaultInstance().CreateRevision(
            isPaymentsEnabled: true,
            allowedOrganizerKinds: [ActorTypeEnum.Organization],
            requiresLocalVerification: false,
            allowedCurrencyCodes: ["USD"],
            defaultCurrencyCode: "USD",
            refundProtections: Enum.GetValues<PaidEventRefundProtection>(),
            currencyRiskLimits: [],
            requiresFirstPaidEventReview: false,
            farFutureReviewThresholdDays: null);

    private static PaidEventPolicyVersion TenantPolicy(Guid tenantId) =>
        PaidEventPolicyVersion.CreateTenant(
            tenantId,
            isPaymentsEnabled: true,
            allowedOrganizerKinds: [ActorTypeEnum.Organization],
            requiresLocalVerification: true,
            allowedCurrencyCodes: ["USD"],
            defaultCurrencyCode: "USD",
            refundProtections: Enum.GetValues<PaidEventRefundProtection>(),
            currencyRiskLimits: [],
            requiresFirstPaidEventReview: false,
            farFutureReviewThresholdDays: null);

    private static PaidEventPolicyVersion BroadTenantPolicy(Guid tenantId) =>
        PaidEventPolicyVersion.CreateTenant(
            tenantId,
            isPaymentsEnabled: true,
            allowedOrganizerKinds:
            [
                ActorTypeEnum.Organization,
                ActorTypeEnum.Group
            ],
            requiresLocalVerification: true,
            allowedCurrencyCodes: ["USD"],
            defaultCurrencyCode: "USD",
            refundProtections: Enum.GetValues<PaidEventRefundProtection>(),
            currencyRiskLimits: [],
            requiresFirstPaidEventReview: false,
            farFutureReviewThresholdDays: null);

    private static PaidEventPolicyVersion RetiredInstancePolicy()
    {
        PaidEventPolicyVersion policy = InstancePolicy();
        policy.Retire();
        return policy;
    }

    private static ConfigurationManifestOperation AppliedOperation(string name)
    {
        DateTime occurredAt = new(2026, 8, 27, 12, 0, 0, DateTimeKind.Utc);
        return ConfigurationManifestOperation.Create(
            Guid.Parse("0199464e-e388-7f56-9281-cefabd6a5676"),
            ConfigurationManifestAuditMode.Bootstrap,
            ConfigurationManifestContractMetadata.ApiVersion,
            ConfigurationManifestContractMetadata.Kind,
            name,
            new string('a', 64),
            ConfigurationManifestOperationStatus.Applied,
            requestedTenantCount: 2,
            createdTenantCount: 2,
            skippedExistingTenantCount: 0,
            failedTenantCount: 0,
            reasonCode: null,
            reason: null,
            occurredAt,
            occurredAt,
            instanceSectionDigest: new string('b', 64),
            bootstrapGeneration: 1);
    }

    private sealed class Fixture
    {
        public Fixture()
        {
            SystemSettings.GetAllSettings(null, Arg.Any<CancellationToken>()).Returns([]);
            TenantSettings.GetByTenantAndKeys(
                    Arg.Any<Guid>(),
                    Arg.Any<IReadOnlyCollection<string>>(),
                    Arg.Any<CancellationToken>())
                .Returns([]);
            TenantDocuments.GetManyForTenant(
                    Arg.Any<Guid>(),
                    Arg.Any<IEnumerable<string>>(),
                    Arg.Any<CancellationToken>())
                .Returns([]);
            PaidPolicies.GetActiveInstanceAsync(Arg.Any<CancellationToken>())
                .Returns(InstancePolicy());
            Operations.GetLatestAppliedBootstrapAsync(Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<ConfigurationManifestOperation?>(null));

            Handler = new ExportConfigurationManifestQueryHandler(
                Tenants,
                SystemSettings,
                TenantSettings,
                TenantDocuments,
                Resolver,
                TypedDocuments,
                PaidPolicies,
                Operations);
        }

        public ITenantRepository Tenants { get; } = Substitute.For<ITenantRepository>();
        public ISystemSettingRepository SystemSettings { get; } =
            Substitute.For<ISystemSettingRepository>();
        public ITenantSettingRepository TenantSettings { get; } =
            Substitute.For<ITenantSettingRepository>();
        public ITenantSettingsDocumentRepository TenantDocuments { get; } =
            Substitute.For<ITenantSettingsDocumentRepository>();
        public IHierarchicalSettingsResolver Resolver { get; } =
            Substitute.For<IHierarchicalSettingsResolver>();
        public ITypedSettingsDocumentResolver TypedDocuments { get; } =
            Substitute.For<ITypedSettingsDocumentResolver>();
        public IPaidEventPolicyRepository PaidPolicies { get; } =
            Substitute.For<IPaidEventPolicyRepository>();
        public IConfigurationManifestOperationRepository Operations { get; } =
            Substitute.For<IConfigurationManifestOperationRepository>();
        public ExportConfigurationManifestQueryHandler Handler { get; }
    }
}
