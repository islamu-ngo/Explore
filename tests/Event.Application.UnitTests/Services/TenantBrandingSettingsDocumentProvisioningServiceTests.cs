// ABOUTME: Unit tests for tenant branding typed-document provisioning behavior.
// ABOUTME: Verifies onboarding can align seeded tenant.branding rows with chosen display names.

using System.Text.Json;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Services;
using Explore.Domain.Settings.Documents;
using NSubstitute;

namespace Event.Application.UnitTests.Services;

public sealed class TenantBrandingSettingsDocumentProvisioningServiceTests
{
    private readonly ITenantRepository _tenantRepository = Substitute.For<ITenantRepository>();
    private readonly ITenantSettingsDocumentRepository _documentRepository = Substitute.For<ITenantSettingsDocumentRepository>();
    private readonly ITypedSettingsDocumentResolver _typedSettingsDocumentResolver = Substitute.For<ITypedSettingsDocumentResolver>();
    private readonly TenantBrandingSettingsDocumentProvisioningService _service;

    public TenantBrandingSettingsDocumentProvisioningServiceTests()
    {
        _service = new TenantBrandingSettingsDocumentProvisioningService(
            _tenantRepository,
            _documentRepository,
            _typedSettingsDocumentResolver);
    }

    [Test]
    public async Task EnsureTenantBrandingDocumentAsync_WhenExistingAndDisplayNameProvided_UpdatesDisplayNameAndInvalidatesCache()
    {
        var tenantId = Guid.NewGuid();
        var existing = TenantSettingsDocument.Create(
            tenantId,
            SettingsDocumentKeys.Tenant.Branding,
            schemaVersion: 3,
            defaultsVersion: "2026-05-branding-v3",
            payloadJson: "{\"displayName\":\"ISLAMU Default Tenant\",\"logoUrl\":\"https://cdn.example.test/logo.svg\",\"faviconUrl\":\"https://cdn.example.test/favicon.ico\",\"customCssUrl\":\"https://cdn.example.test/site.css\"}");
        _documentRepository.GetTrackedByTenantAndDocumentKey(
                tenantId,
                SettingsDocumentKeys.Tenant.Branding,
                Arg.Any<CancellationToken>())
            .Returns(existing);

        var result = await _service.EnsureTenantBrandingDocumentAsync(
            tenantId,
            "Community Events",
            CancellationToken.None);

        await Assert.That(result).IsSameReferenceAs(existing);
        using var payload = JsonDocument.Parse(existing.PayloadJson);
        await Assert.That(payload.RootElement.GetProperty("displayName").GetString()).IsEqualTo("Community Events");
        await Assert.That(payload.RootElement.GetProperty("logoUrl").GetString()).IsEqualTo("https://cdn.example.test/logo.svg");
        await Assert.That(existing.SchemaVersion).IsEqualTo(3);
        await Assert.That(existing.DefaultsVersion).IsEqualTo("2026-05-branding-v3");
        await _documentRepository.Received(1).Update(existing);
        _typedSettingsDocumentResolver.Received(1).InvalidateTenantDocumentCache(tenantId, SettingsDocumentKeys.Tenant.Branding);
    }

    [Test]
    public async Task EnsureTenantBrandingDocumentAsync_WhenExistingAndDisplayNameMissing_DoesNotRewriteDocument()
    {
        var tenantId = Guid.NewGuid();
        var existing = TenantBrandingSettingsDocumentDefaults.Create(tenantId, "Existing Brand");
        _documentRepository.GetTrackedByTenantAndDocumentKey(
                tenantId,
                SettingsDocumentKeys.Tenant.Branding,
                Arg.Any<CancellationToken>())
            .Returns(existing);

        var result = await _service.EnsureTenantBrandingDocumentAsync(tenantId, cancellationToken: CancellationToken.None);

        await Assert.That(result).IsSameReferenceAs(existing);
        await _documentRepository.DidNotReceive().Update(Arg.Any<TenantSettingsDocument>());
        _typedSettingsDocumentResolver.DidNotReceive().InvalidateTenantDocumentCache(Arg.Any<Guid>(), Arg.Any<string?>());
    }
}
