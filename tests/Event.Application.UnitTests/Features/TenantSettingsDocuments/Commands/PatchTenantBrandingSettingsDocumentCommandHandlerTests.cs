// ABOUTME: Unit tests for presence-aware tenant branding typed settings document patches.
// ABOUTME: Verifies merge preservation, explicit clears, concurrency, governance atomicity, and single side effects.

using System.Text.Json;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.TenantSettingsDocuments;
using Explore.Application.Exceptions;
using Explore.Application.Features.TenantSettingsDocuments.Handlers.Commands;
using Explore.Application.Features.TenantSettingsDocuments.Requests.Commands;
using Explore.Application.Models.Common;
using Explore.Application.Services;
using Explore.Domain;
using Explore.Domain.Constants;
using Explore.Domain.Settings.Documents;
using NSubstitute;
using TUnit.Assertions;
using TUnit.Core;

namespace Event.Application.UnitTests.Features.TenantSettingsDocuments.Commands;

public sealed class PatchTenantBrandingSettingsDocumentCommandHandlerTests
{
    private readonly ITenantSettingsDocumentRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITypedSettingsDocumentResolver _resolver;
    private readonly ISystemSettingRepository _systemSettingRepository;
    private readonly PatchTenantBrandingSettingsDocumentCommandHandler _handler;

    public PatchTenantBrandingSettingsDocumentCommandHandlerTests()
    {
        _repository = Substitute.For<ITenantSettingsDocumentRepository>();
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _resolver = Substitute.For<ITypedSettingsDocumentResolver>();
        _systemSettingRepository = Substitute.For<ISystemSettingRepository>();
        _unitOfWork
            .ExecuteInTransactionAsync(Arg.Any<Func<CancellationToken, Task>>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var operation = callInfo.Arg<Func<CancellationToken, Task>>();
                return operation(CancellationToken.None);
            });
        _handler = new PatchTenantBrandingSettingsDocumentCommandHandler(
            _repository,
            _unitOfWork,
            _resolver,
            new TenantBrandingSettingsDocumentLockService(_systemSettingRepository));
    }

    [Test]
    public async Task Handle_WhenConcurrencyStampIsEmpty_ReturnsValidationFailure()
    {
        var command = CreateCommand(Guid.NewGuid(), Guid.Empty, DisplayName("Brand"));

        var result = await _handler.Handle(command, CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.Errors).IsNotNull();
        await Assert.That(result.Errors!.Single()).IsEqualTo("Expected Concurrency Stamp is required.");
        await _repository.DidNotReceive().Update(Arg.Any<TenantSettingsDocument>());
        _resolver.DidNotReceive().InvalidateTenantDocumentCache(Arg.Any<Guid>(), Arg.Any<string?>());
    }

    [Test]
    public async Task Handle_WhenNoMutationGroupIsSupplied_ReturnsValidationFailure()
    {
        var command = CreateCommand(Guid.NewGuid(), Guid.NewGuid());

        var result = await _handler.Handle(command, CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.Errors).IsNotNull();
        await Assert.That(result.Errors!.Single()).Contains("At least one tenant branding mutation group");
        await _repository.DidNotReceive().Update(Arg.Any<TenantSettingsDocument>());
        _resolver.DidNotReceive().InvalidateTenantDocumentCache(Arg.Any<Guid>(), Arg.Any<string?>());
    }

    [Test]
    [Arguments("displayName")]
    [Arguments("assets")]
    public async Task Handle_WhenMutationGroupIsEmpty_ReturnsValidationFailure(string group)
    {
        var command = CreateCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            group == "displayName" ? new PatchTenantBrandingDisplayNameDto() : null,
            group == "assets" ? new PatchTenantBrandingAssetsDto() : null);

        var result = await _handler.Handle(command, CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.Errors).IsNotNull();
        await Assert.That(result.Errors!.Single()).Contains("must include at least one field");
        await _repository.DidNotReceive().Update(Arg.Any<TenantSettingsDocument>());
        _resolver.DidNotReceive().InvalidateTenantDocumentCache(Arg.Any<Guid>(), Arg.Any<string?>());
    }

    [Test]
    public async Task Handle_WhenBrandingDocumentIsMissing_ReturnsNotFoundFailure()
    {
        var tenantId = Guid.NewGuid();
        var command = CreateCommand(tenantId, Guid.NewGuid(), DisplayName("Brand"));
        _repository.GetTrackedByTenantAndDocumentKey(tenantId, SettingsDocumentKeys.Tenant.Branding, Arg.Any<CancellationToken>())
            .Returns((TenantSettingsDocument?)null);

        var result = await _handler.Handle(command, CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.Message).IsEqualTo("Tenant branding settings document not found.");
        await _repository.DidNotReceive().Update(Arg.Any<TenantSettingsDocument>());
        _resolver.DidNotReceive().InvalidateTenantDocumentCache(Arg.Any<Guid>(), Arg.Any<string?>());
    }

    [Test]
    public async Task Handle_WhenConcurrencyStampIsStale_ThrowsConcurrencyConflict()
    {
        var tenantId = Guid.NewGuid();
        var existingStamp = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var staleStamp = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var document = CreateDocument(tenantId, existingStamp, schemaVersion: 2, defaultsVersion: "2026-05-branding");
        _repository.GetTrackedByTenantAndDocumentKey(tenantId, SettingsDocumentKeys.Tenant.Branding, Arg.Any<CancellationToken>())
            .Returns(document);

        var exception = await Assert.ThrowsAsync<ConcurrencyConflictException>(() =>
            _handler.Handle(CreateCommand(tenantId, staleStamp, DisplayName("Changed Brand")), CancellationToken.None));

        await Assert.That(exception!.Code).IsEqualTo(ConcurrencyConflictException.ConcurrentUpdate);
        await Assert.That(exception.EntityType).IsEqualTo("tenant_settings_document");
        await Assert.That(exception.EntityId).IsEqualTo(document.Id.ToString());
        await _repository.DidNotReceive().Update(Arg.Any<TenantSettingsDocument>());
        _resolver.DidNotReceive().InvalidateTenantDocumentCache(Arg.Any<Guid>(), Arg.Any<string?>());
    }

    [Test]
    public async Task Handle_WhenPersistedPayloadHasIncompatibleTypes_FailsClosedWithoutSideEffects()
    {
        var tenantId = Guid.NewGuid();
        var expectedStamp = Guid.NewGuid();
        const string incompatiblePayload =
            "{\"displayName\":{\"unexpected\":true},\"logoUrl\":\"https://cdn.example.test/original.svg\"}";
        var document = CreateDocument(tenantId, expectedStamp, 1, "2026-05-branding", incompatiblePayload);
        _repository.GetTrackedByTenantAndDocumentKey(tenantId, SettingsDocumentKeys.Tenant.Branding, Arg.Any<CancellationToken>())
            .Returns(document);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _handler.Handle(CreateCommand(tenantId, expectedStamp, DisplayName("Updated Brand")), CancellationToken.None));

        await Assert.That(exception!.Message).IsEqualTo("Document 'tenant.branding' payload could not be deserialized.");
        await Assert.That(document.PayloadJson).IsEqualTo(incompatiblePayload);
        await _unitOfWork.DidNotReceive()
            .ExecuteInTransactionAsync(Arg.Any<Func<CancellationToken, Task>>(), Arg.Any<CancellationToken>());
        await _repository.DidNotReceive().Update(Arg.Any<TenantSettingsDocument>());
        _resolver.DidNotReceive().InvalidateTenantDocumentCache(Arg.Any<Guid>(), Arg.Any<string?>());
    }

    [Test]
    public async Task Handle_WhenDisplayNameOnlyIsSupplied_PreservesAssetsAndReturnsFreshDocumentWithSingleSideEffects()
    {
        var tenantId = Guid.NewGuid();
        var expectedStamp = Guid.Parse("33333333-3333-3333-3333-333333333333");
        var refreshedStamp = Guid.Parse("77777777-7777-7777-7777-777777777777");
        var updatedAt = DateTime.UtcNow;
        var document = CreateDocument(
            tenantId,
            expectedStamp,
            schemaVersion: 7,
            defaultsVersion: "2026-05-branding-v7",
            payloadJson: FullPayloadJson);
        _repository.GetTrackedByTenantAndDocumentKey(tenantId, SettingsDocumentKeys.Tenant.Branding, Arg.Any<CancellationToken>())
            .Returns(document);
        _repository.Update(document).Returns(_ =>
        {
            document.ConcurrencyStamp = refreshedStamp;
            document.UpdatedAt = updatedAt;
            return Task.CompletedTask;
        });

        var command = CreateCommand(tenantId, expectedStamp, DisplayName("  Updated Brand  "));

        var result = await _handler.Handle(command, CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Id).IsNotNull();
        await Assert.That(result.Id!.ConcurrencyStamp).IsEqualTo(refreshedStamp);
        await Assert.That(result.Id.UpdatedAt).IsEqualTo(updatedAt);
        await Assert.That(result.Id.Payload.DisplayName).IsEqualTo("Updated Brand");
        await Assert.That(result.Id.Payload.LogoUrl).IsEqualTo("https://cdn.example.test/original.svg");
        await Assert.That(result.Id.Payload.FaviconUrl).IsEqualTo("https://cdn.example.test/original.ico");
        await Assert.That(result.Id.Payload.CustomCssUrl).IsEqualTo("https://cdn.example.test/original.css");
        await Assert.That(result.Id.CanChangeDisplayName).IsTrue();
        await Assert.That(result.Id.CanChangeLogoUrl).IsTrue();
        await Assert.That(result.Id.CanChangeFaviconUrl).IsTrue();
        await Assert.That(result.Id.CanChangeCustomCssUrl).IsTrue();
        await Assert.That(document.SchemaVersion).IsEqualTo(7);
        await Assert.That(document.DefaultsVersion).IsEqualTo("2026-05-branding-v7");

        using var payloadJson = JsonDocument.Parse(document.PayloadJson);
        await Assert.That(payloadJson.RootElement.GetProperty("displayName").GetString()).IsEqualTo("Updated Brand");
        await Assert.That(payloadJson.RootElement.GetProperty("logoUrl").GetString()).IsEqualTo("https://cdn.example.test/original.svg");
        await Assert.That(payloadJson.RootElement.GetProperty("faviconUrl").GetString()).IsEqualTo("https://cdn.example.test/original.ico");
        await Assert.That(payloadJson.RootElement.GetProperty("customCssUrl").GetString()).IsEqualTo("https://cdn.example.test/original.css");
        await _repository.Received(1).Update(document);
        await _unitOfWork.Received(1)
            .ExecuteInTransactionAsync(Arg.Any<Func<CancellationToken, Task>>(), Arg.Any<CancellationToken>());
        _resolver.Received(1).InvalidateTenantDocumentCache(tenantId, SettingsDocumentKeys.Tenant.Branding);
    }

    [Test]
    public async Task Handle_WhenOneAssetIsSupplied_PreservesDisplayNameAndOtherAssets()
    {
        var tenantId = Guid.NewGuid();
        var expectedStamp = Guid.NewGuid();
        var document = CreateDocument(tenantId, expectedStamp, 1, "2026-05-branding", FullPayloadJson);
        _repository.GetTrackedByTenantAndDocumentKey(tenantId, SettingsDocumentKeys.Tenant.Branding, Arg.Any<CancellationToken>())
            .Returns(document);

        var result = await _handler.Handle(
            CreateCommand(tenantId, expectedStamp, assets: Assets(logoUrl: "  https://cdn.example.test/updated.svg  ")),
            CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        using var payloadJson = JsonDocument.Parse(document.PayloadJson);
        await Assert.That(payloadJson.RootElement.GetProperty("displayName").GetString()).IsEqualTo("Original Brand");
        await Assert.That(payloadJson.RootElement.GetProperty("logoUrl").GetString()).IsEqualTo("https://cdn.example.test/updated.svg");
        await Assert.That(payloadJson.RootElement.GetProperty("faviconUrl").GetString()).IsEqualTo("https://cdn.example.test/original.ico");
        await Assert.That(payloadJson.RootElement.GetProperty("customCssUrl").GetString()).IsEqualTo("https://cdn.example.test/original.css");
    }

    [Test]
    public async Task Handle_WhenAssetIsExplicitlyCleared_PersistsNullAndPreservesOtherLeaves()
    {
        var tenantId = Guid.NewGuid();
        var expectedStamp = Guid.NewGuid();
        var document = CreateDocument(tenantId, expectedStamp, 1, "2026-05-branding", FullPayloadJson);
        _repository.GetTrackedByTenantAndDocumentKey(tenantId, SettingsDocumentKeys.Tenant.Branding, Arg.Any<CancellationToken>())
            .Returns(document);

        var result = await _handler.Handle(
            CreateCommand(
                tenantId,
                expectedStamp,
                assets: new PatchTenantBrandingAssetsDto
                {
                    CustomCssUrl = OptionalUpdate<string?>.Set(null)
                }),
            CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        using var payloadJson = JsonDocument.Parse(document.PayloadJson);
        await Assert.That(payloadJson.RootElement.GetProperty("displayName").GetString()).IsEqualTo("Original Brand");
        await Assert.That(payloadJson.RootElement.GetProperty("logoUrl").GetString()).IsEqualTo("https://cdn.example.test/original.svg");
        await Assert.That(payloadJson.RootElement.GetProperty("customCssUrl").ValueKind).IsEqualTo(JsonValueKind.Null);
    }

    [Test]
    public async Task Handle_WhenMergedPayloadIsInvalid_ReturnsValidationFailureWithoutSideEffects()
    {
        var tenantId = Guid.NewGuid();
        var expectedStamp = Guid.NewGuid();
        var payloadJson = JsonSerializer.Serialize(new
        {
            displayName = new string('x', 201),
            logoUrl = "https://cdn.example.test/original.svg"
        });
        var document = CreateDocument(tenantId, expectedStamp, 1, "2026-05-branding", payloadJson);
        _repository.GetTrackedByTenantAndDocumentKey(tenantId, SettingsDocumentKeys.Tenant.Branding, Arg.Any<CancellationToken>())
            .Returns(document);

        var result = await _handler.Handle(
            CreateCommand(tenantId, expectedStamp, assets: Assets(logoUrl: "https://cdn.example.test/updated.svg")),
            CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.Errors).IsNotNull();
        await Assert.That(result.Errors!.Single()).Contains("Display Name must not exceed 200 characters");
        await Assert.That(document.PayloadJson).IsEqualTo(payloadJson);
        await _repository.DidNotReceive().Update(Arg.Any<TenantSettingsDocument>());
        _resolver.DidNotReceive().InvalidateTenantDocumentCache(Arg.Any<Guid>(), Arg.Any<string?>());
    }

    [Test]
    public async Task Handle_WhenMultiTenantAndDisplayNameLockedAndChanged_ReturnsGovernanceFailure()
    {
        var tenantId = Guid.NewGuid();
        var expectedStamp = Guid.Parse("44444444-4444-4444-4444-444444444444");
        var document = CreateDocument(tenantId, expectedStamp, schemaVersion: 1, defaultsVersion: "2026-05-branding");
        _repository.GetTrackedByTenantAndDocumentKey(tenantId, SettingsDocumentKeys.Tenant.Branding, Arg.Any<CancellationToken>())
            .Returns(document);
        _systemSettingRepository.GetByKey(GovernanceSettingKeys.Deployment.Mode)
            .Returns(CreateSystemSetting(GovernanceSettingKeys.Deployment.Mode, "\"MultiTenant\""));
        _systemSettingRepository.GetByKey(GovernanceSettingKeys.Tenants.WhiteLabelingEnabled)
            .Returns(CreateSystemSetting(GovernanceSettingKeys.Tenants.WhiteLabelingEnabled, "true"));
        _systemSettingRepository.GetByKey(GovernanceSettingKeys.Branding.DisplayName)
            .Returns(CreateSystemSetting(GovernanceSettingKeys.Branding.DisplayName, "\"Instance Brand\"", isLocked: true));

        var result = await _handler.Handle(
            CreateCommand(tenantId, expectedStamp, DisplayName("Changed Brand")),
            CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.Errors).IsNotNull();
        await Assert.That(result.Errors!.Single()).Contains("Display name cannot be changed");
        await _repository.DidNotReceive().Update(Arg.Any<TenantSettingsDocument>());
        _resolver.DidNotReceive().InvalidateTenantDocumentCache(Arg.Any<Guid>(), Arg.Any<string?>());
    }


    [Test]
    public async Task Handle_WhenMultiTenantWhiteLabelingDisabledAndOnlyDisplayNameChanges_AllowsPreservedLockedMediaFields()
    {
        var tenantId = Guid.NewGuid();
        var expectedStamp = Guid.Parse("55555555-5555-5555-5555-555555555555");
        var document = CreateDocument(
            tenantId,
            expectedStamp,
            schemaVersion: 1,
            defaultsVersion: "2026-05-branding",
            payloadJson: FullPayloadJson);
        _repository.GetTrackedByTenantAndDocumentKey(tenantId, SettingsDocumentKeys.Tenant.Branding, Arg.Any<CancellationToken>())
            .Returns(document);
        ConfigureMultiTenantBrandingGovernance(whiteLabelingEnabled: false);

        var result = await _handler.Handle(
            CreateCommand(tenantId, expectedStamp, DisplayName("Updated Brand")),
            CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        using var payloadJson = JsonDocument.Parse(document.PayloadJson);
        await Assert.That(payloadJson.RootElement.GetProperty("displayName").GetString()).IsEqualTo("Updated Brand");
        await Assert.That(payloadJson.RootElement.GetProperty("logoUrl").GetString()).IsEqualTo("https://cdn.example.test/original.svg");
        await Assert.That(payloadJson.RootElement.GetProperty("faviconUrl").GetString()).IsEqualTo("https://cdn.example.test/original.ico");
        await Assert.That(payloadJson.RootElement.GetProperty("customCssUrl").GetString()).IsEqualTo("https://cdn.example.test/original.css");
        await _repository.Received(1).Update(document);
        _resolver.Received(1).InvalidateTenantDocumentCache(tenantId, SettingsDocumentKeys.Tenant.Branding);
    }

    [Test]
    public async Task Handle_WhenMultiTenantWhiteLabelingDisabledAndLockedMediaFieldChanges_ReturnsGovernanceFailure()
    {
        var tenantId = Guid.NewGuid();
        var expectedStamp = Guid.Parse("66666666-6666-6666-6666-666666666666");
        var document = CreateDocument(
            tenantId,
            expectedStamp,
            schemaVersion: 1,
            defaultsVersion: "2026-05-branding",
            payloadJson: FullPayloadJson);
        _repository.GetTrackedByTenantAndDocumentKey(tenantId, SettingsDocumentKeys.Tenant.Branding, Arg.Any<CancellationToken>())
            .Returns(document);
        ConfigureMultiTenantBrandingGovernance(whiteLabelingEnabled: false);

        var result = await _handler.Handle(
            CreateCommand(
                tenantId,
                expectedStamp,
                DisplayName("Updated Brand"),
                Assets(logoUrl: "https://cdn.example.test/changed.svg")),
            CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.Errors).IsNotNull();
        await Assert.That(result.Errors!.Single()).Contains("Logo URL cannot be changed");
        using var payloadJson = JsonDocument.Parse(document.PayloadJson);
        await Assert.That(payloadJson.RootElement.GetProperty("displayName").GetString()).IsEqualTo("Original Brand");
        await Assert.That(payloadJson.RootElement.GetProperty("logoUrl").GetString()).IsEqualTo("https://cdn.example.test/original.svg");
        await _repository.DidNotReceive().Update(Arg.Any<TenantSettingsDocument>());
        _resolver.DidNotReceive().InvalidateTenantDocumentCache(Arg.Any<Guid>(), Arg.Any<string?>());
    }

    [Test]
    public async Task Handle_WhenEveryLockedFieldIsRequestedWithoutChangingValues_ReturnsEveryGovernanceFailure()
    {
        var tenantId = Guid.NewGuid();
        var expectedStamp = Guid.NewGuid();
        var document = CreateDocument(tenantId, expectedStamp, 1, "2026-05-branding", FullPayloadJson);
        _repository.GetTrackedByTenantAndDocumentKey(tenantId, SettingsDocumentKeys.Tenant.Branding, Arg.Any<CancellationToken>())
            .Returns(document);
        ConfigureMultiTenantBrandingGovernance(whiteLabelingEnabled: false);
        _systemSettingRepository.GetByKey(GovernanceSettingKeys.Branding.DisplayName)
            .Returns(CreateSystemSetting(GovernanceSettingKeys.Branding.DisplayName, "\"Instance Brand\"", isLocked: true));

        var result = await _handler.Handle(
            CreateCommand(
                tenantId,
                expectedStamp,
                DisplayName("Original Brand"),
                new PatchTenantBrandingAssetsDto
                {
                    LogoUrl = OptionalUpdate<string?>.Set("https://cdn.example.test/original.svg"),
                    FaviconUrl = OptionalUpdate<string?>.Set("https://cdn.example.test/original.ico"),
                    CustomCssUrl = OptionalUpdate<string?>.Set("https://cdn.example.test/original.css")
                }),
            CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.Errors).IsNotNull();
        await Assert.That(result.Errors!).Count().IsEqualTo(4);
        await Assert.That(result.Errors!.Any(error => error.Contains("Display name cannot be changed"))).IsTrue();
        await Assert.That(result.Errors.Any(error => error.Contains("Logo URL cannot be changed"))).IsTrue();
        await Assert.That(result.Errors.Any(error => error.Contains("Favicon URL cannot be changed"))).IsTrue();
        await Assert.That(result.Errors.Any(error => error.Contains("Custom CSS URL cannot be changed"))).IsTrue();
        await Assert.That(document.PayloadJson).IsEqualTo(FullPayloadJson);
        await _repository.DidNotReceive().Update(Arg.Any<TenantSettingsDocument>());
        _resolver.DidNotReceive().InvalidateTenantDocumentCache(Arg.Any<Guid>(), Arg.Any<string?>());
    }

    private const string FullPayloadJson =
        "{\"displayName\":\"Original Brand\",\"logoUrl\":\"https://cdn.example.test/original.svg\",\"faviconUrl\":\"https://cdn.example.test/original.ico\",\"customCssUrl\":\"https://cdn.example.test/original.css\"}";

    private static PatchTenantBrandingSettingsDocumentCommand CreateCommand(
        Guid tenantId,
        Guid expectedStamp,
        PatchTenantBrandingDisplayNameDto? displayName = null,
        PatchTenantBrandingAssetsDto? assets = null) => new()
        {
            TenantId = tenantId,
            IsLockedByInstance = false,
            Patch = new PatchTenantBrandingSettingsDocumentDto
            {
                ExpectedConcurrencyStamp = expectedStamp,
                DisplayName = displayName,
                Assets = assets
            }
        };

    private static PatchTenantBrandingDisplayNameDto DisplayName(string? value)
        => new() { Value = OptionalUpdate<string?>.Set(value) };

    private static PatchTenantBrandingAssetsDto Assets(string? logoUrl = null)
        => new()
        {
            LogoUrl = OptionalUpdate<string?>.Set(logoUrl)
        };

    private static TenantSettingsDocument CreateDocument(
        Guid tenantId,
        Guid concurrencyStamp,
        int schemaVersion,
        string defaultsVersion,
        string payloadJson = "{\"displayName\":\"Original Brand\"}")
    {
        var document = TenantSettingsDocument.Create(
            tenantId,
            SettingsDocumentKeys.Tenant.Branding,
            schemaVersion,
            defaultsVersion,
            payloadJson);
        document.Id = Guid.NewGuid();
        document.Tenant = null!;
        document.ConcurrencyStamp = concurrencyStamp;
        return document;
    }

    private void ConfigureMultiTenantBrandingGovernance(bool whiteLabelingEnabled)
    {
        _systemSettingRepository.GetByKey(GovernanceSettingKeys.Deployment.Mode)
            .Returns(CreateSystemSetting(GovernanceSettingKeys.Deployment.Mode, "\"MultiTenant\""));
        _systemSettingRepository.GetByKey(GovernanceSettingKeys.Tenants.WhiteLabelingEnabled)
            .Returns(CreateSystemSetting(GovernanceSettingKeys.Tenants.WhiteLabelingEnabled, whiteLabelingEnabled ? "true" : "false"));
        _systemSettingRepository.GetByKey(GovernanceSettingKeys.Branding.DisplayName)
            .Returns(CreateSystemSetting(GovernanceSettingKeys.Branding.DisplayName, "\"Instance Brand\""));
        _systemSettingRepository.GetByKey(GovernanceSettingKeys.Branding.LogoUrl)
            .Returns(CreateSystemSetting(GovernanceSettingKeys.Branding.LogoUrl, "\"https://cdn.example.test/instance.svg\""));
        _systemSettingRepository.GetByKey(GovernanceSettingKeys.Branding.FaviconUrl)
            .Returns(CreateSystemSetting(GovernanceSettingKeys.Branding.FaviconUrl, "\"https://cdn.example.test/instance.ico\""));
        _systemSettingRepository.GetByKey(GovernanceSettingKeys.Branding.CustomCssUrl)
            .Returns(CreateSystemSetting(GovernanceSettingKeys.Branding.CustomCssUrl, "\"https://cdn.example.test/instance.css\""));
    }

    private static SystemSetting CreateSystemSetting(string key, string value, bool isLocked = false)
        => new()
        {
            Id = Guid.NewGuid(),
            SettingKey = key,
            Value = value,
            IsLocked = isLocked,
            SettingValueTypeLookup = null!
        };
}
