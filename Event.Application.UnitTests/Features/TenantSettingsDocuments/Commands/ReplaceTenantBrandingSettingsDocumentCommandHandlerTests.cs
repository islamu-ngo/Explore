// ABOUTME: Unit tests for tenant branding typed settings document replacement.
// ABOUTME: Verifies validation, optimistic concurrency, JSONB payload replacement, and typed cache invalidation.

using System.Text.Json;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.TenantSettingsDocuments;
using Explore.Application.Exceptions;
using Explore.Application.Features.TenantSettingsDocuments.Handlers.Commands;
using Explore.Application.Features.TenantSettingsDocuments.Requests.Commands;
using Explore.Application.Services;
using Explore.Domain;
using Explore.Domain.Constants;
using Explore.Domain.Settings.Documents;
using NSubstitute;
using TUnit.Assertions;
using TUnit.Core;

namespace Event.Application.UnitTests.Features.TenantSettingsDocuments.Commands;

public sealed class ReplaceTenantBrandingSettingsDocumentCommandHandlerTests
{
    private readonly ITenantSettingsDocumentRepository _repository;
    private readonly ITypedSettingsDocumentResolver _resolver;
    private readonly ISystemSettingRepository _systemSettingRepository;
    private readonly ReplaceTenantBrandingSettingsDocumentCommandHandler _handler;

    public ReplaceTenantBrandingSettingsDocumentCommandHandlerTests()
    {
        _repository = Substitute.For<ITenantSettingsDocumentRepository>();
        _resolver = Substitute.For<ITypedSettingsDocumentResolver>();
        _systemSettingRepository = Substitute.For<ISystemSettingRepository>();
        _handler = new ReplaceTenantBrandingSettingsDocumentCommandHandler(
            _repository,
            _resolver,
            new TenantBrandingSettingsDocumentLockService(_systemSettingRepository));
    }

    [Test]
    public async Task Handle_WhenConcurrencyStampIsEmpty_ReturnsValidationFailure()
    {
        var command = CreateCommand(Guid.NewGuid(), Guid.Empty);

        var result = await _handler.Handle(command, CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.Errors).IsNotNull();
        await Assert.That(result.Errors!.Single()).IsEqualTo("Expected Concurrency Stamp is required.");
        await _repository.DidNotReceive().Update(Arg.Any<TenantSettingsDocument>());
        _resolver.DidNotReceive().InvalidateTenantDocumentCache(Arg.Any<Guid>(), Arg.Any<string?>());
    }

    [Test]
    public async Task Handle_WhenBrandingDocumentIsMissing_ReturnsNotFoundFailure()
    {
        var tenantId = Guid.NewGuid();
        var command = CreateCommand(tenantId, Guid.NewGuid());
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
            _handler.Handle(CreateCommand(tenantId, staleStamp), CancellationToken.None));

        await Assert.That(exception!.Code).IsEqualTo(ConcurrencyConflictException.ConcurrentUpdate);
        await Assert.That(exception.EntityType).IsEqualTo("tenant_settings_document");
        await Assert.That(exception.EntityId).IsEqualTo(document.Id.ToString());
        await _repository.DidNotReceive().Update(Arg.Any<TenantSettingsDocument>());
        _resolver.DidNotReceive().InvalidateTenantDocumentCache(Arg.Any<Guid>(), Arg.Any<string?>());
    }

    [Test]
    public async Task Handle_WhenValid_ReplacesPayloadPreservesMetadataAndInvalidatesTypedCache()
    {
        var tenantId = Guid.NewGuid();
        var expectedStamp = Guid.Parse("33333333-3333-3333-3333-333333333333");
        var document = CreateDocument(tenantId, expectedStamp, schemaVersion: 7, defaultsVersion: "2026-05-branding-v7");
        _repository.GetTrackedByTenantAndDocumentKey(tenantId, SettingsDocumentKeys.Tenant.Branding, Arg.Any<CancellationToken>())
            .Returns(document);

        var command = CreateCommand(
            tenantId,
            expectedStamp,
            new TenantBrandingSettingsPayloadDto
            {
                DisplayName = "Updated Brand",
                LogoUrl = "https://cdn.example.test/updated.svg",
                FaviconUrl = "https://cdn.example.test/favicon.ico",
                CustomCssUrl = "https://cdn.example.test/tenant.css"
            });

        var result = await _handler.Handle(command, CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Id).IsEqualTo(document.Id);
        await Assert.That(document.SchemaVersion).IsEqualTo(7);
        await Assert.That(document.DefaultsVersion).IsEqualTo("2026-05-branding-v7");

        using var payloadJson = JsonDocument.Parse(document.PayloadJson);
        await Assert.That(payloadJson.RootElement.GetProperty("displayName").GetString()).IsEqualTo("Updated Brand");
        await Assert.That(payloadJson.RootElement.GetProperty("logoUrl").GetString()).IsEqualTo("https://cdn.example.test/updated.svg");
        await Assert.That(payloadJson.RootElement.GetProperty("faviconUrl").GetString()).IsEqualTo("https://cdn.example.test/favicon.ico");
        await Assert.That(payloadJson.RootElement.GetProperty("customCssUrl").GetString()).IsEqualTo("https://cdn.example.test/tenant.css");
        await _repository.Received(1).Update(document);
        _resolver.Received(1).InvalidateTenantDocumentCache(tenantId, SettingsDocumentKeys.Tenant.Branding);
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
            CreateCommand(tenantId, expectedStamp, new TenantBrandingSettingsPayloadDto { DisplayName = "Changed Brand" }),
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
            payloadJson: "{\"displayName\":\"Original Brand\",\"logoUrl\":\"https://cdn.example.test/original.svg\",\"faviconUrl\":\"https://cdn.example.test/original.ico\",\"customCssUrl\":\"https://cdn.example.test/original.css\"}");
        _repository.GetTrackedByTenantAndDocumentKey(tenantId, SettingsDocumentKeys.Tenant.Branding, Arg.Any<CancellationToken>())
            .Returns(document);
        ConfigureMultiTenantBrandingGovernance(whiteLabelingEnabled: false);

        var result = await _handler.Handle(
            CreateCommand(tenantId, expectedStamp, new TenantBrandingSettingsPayloadDto
            {
                DisplayName = "Updated Brand",
                LogoUrl = "https://cdn.example.test/original.svg",
                FaviconUrl = "https://cdn.example.test/original.ico",
                CustomCssUrl = "https://cdn.example.test/original.css"
            }),
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
            payloadJson: "{\"displayName\":\"Original Brand\",\"logoUrl\":\"https://cdn.example.test/original.svg\",\"faviconUrl\":\"https://cdn.example.test/original.ico\",\"customCssUrl\":\"https://cdn.example.test/original.css\"}");
        _repository.GetTrackedByTenantAndDocumentKey(tenantId, SettingsDocumentKeys.Tenant.Branding, Arg.Any<CancellationToken>())
            .Returns(document);
        ConfigureMultiTenantBrandingGovernance(whiteLabelingEnabled: false);

        var result = await _handler.Handle(
            CreateCommand(tenantId, expectedStamp, new TenantBrandingSettingsPayloadDto
            {
                DisplayName = "Original Brand",
                LogoUrl = "https://cdn.example.test/changed.svg",
                FaviconUrl = "https://cdn.example.test/original.ico",
                CustomCssUrl = "https://cdn.example.test/original.css"
            }),
            CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.Errors).IsNotNull();
        await Assert.That(result.Errors!.Single()).Contains("Logo URL cannot be changed");
        await _repository.DidNotReceive().Update(Arg.Any<TenantSettingsDocument>());
        _resolver.DidNotReceive().InvalidateTenantDocumentCache(Arg.Any<Guid>(), Arg.Any<string?>());
    }

    private static ReplaceTenantBrandingSettingsDocumentCommand CreateCommand(
        Guid tenantId,
        Guid expectedStamp,
        TenantBrandingSettingsPayloadDto? payload = null) => new()
        {
            TenantId = tenantId,
            IsLockedByInstance = false,
            Document = new ReplaceTenantBrandingSettingsDocumentDto
            {
                ExpectedConcurrencyStamp = expectedStamp,
                Payload = payload ?? new TenantBrandingSettingsPayloadDto
                {
                    DisplayName = "Brand",
                    LogoUrl = "https://cdn.example.test/logo.svg",
                    FaviconUrl = "https://cdn.example.test/favicon.ico",
                    CustomCssUrl = "https://cdn.example.test/tenant.css"
                }
            }
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
