// ABOUTME: Unit tests for tenant branding typed settings document query mapping.
// ABOUTME: Verifies resolver metadata and payload are projected without scalar fallback behavior.

using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Services;
using Explore.Application.Features.TenantSettingsDocuments.Handlers.Queries;
using Explore.Application.Features.TenantSettingsDocuments.Requests.Queries;
using Explore.Application.Settings;
using Explore.Domain.Settings.Documents;
using Explore.Domain.Settings.Documents.Payloads;
using NSubstitute;
using TUnit.Core;

namespace Event.Application.UnitTests.Features.TenantSettingsDocuments.Queries;

public sealed class GetTenantBrandingSettingsDocumentQueryHandlerTests
{
    private readonly ITenantContext _tenantContext;
    private readonly ITypedSettingsDocumentResolver _resolver;
    private readonly ITenantBrandingSettingsDocumentProvisioningService _provisioningService;
    private readonly ITenantBrandingSettingsDocumentLockService _lockService;
    private readonly GetTenantBrandingSettingsDocumentQueryHandler _handler;

    public GetTenantBrandingSettingsDocumentQueryHandlerTests()
    {
        _tenantContext = Substitute.For<ITenantContext>();
        _resolver = Substitute.For<ITypedSettingsDocumentResolver>();
        _provisioningService = Substitute.For<ITenantBrandingSettingsDocumentProvisioningService>();
        _lockService = Substitute.For<ITenantBrandingSettingsDocumentLockService>();
        _lockService.GetLockStateAsync(Arg.Any<CancellationToken>())
            .Returns(TenantBrandingSettingsDocumentLockState.AllowAll);
        _handler = new GetTenantBrandingSettingsDocumentQueryHandler(
            _tenantContext,
            _resolver,
            _provisioningService,
            _lockService);
    }

    [Test]
    public async Task Handle_ShouldMapResolvedBrandingDocument()
    {
        var tenantId = Guid.NewGuid();
        var updatedAt = DateTime.UtcNow;
        _tenantContext.TenantId.Returns(tenantId);
        _resolver.ResolveTenantDocumentAsync<BrandingSettings>(
                Arg.Is<SettingsResolutionContext>(context =>
                    context.TenantId == tenantId &&
                    context.RequestsDocument(SettingsDocumentKeys.Tenant.Branding)),
                SettingsDocumentKeys.Tenant.Branding,
                Arg.Any<CancellationToken>())
            .Returns(new ResolvedSettingsDocument<BrandingSettings>
            {
                DocumentKey = SettingsDocumentKeys.Tenant.Branding,
                SchemaVersion = 2,
                DefaultsVersion = "2026-05-branding",
                Payload = new BrandingSettings
                {
                    DisplayName = "Open Islamu",
                    LogoUrl = "https://cdn.example.test/logo.svg",
                    FaviconUrl = "https://cdn.example.test/favicon.ico",
                    CustomCssUrl = "https://cdn.example.test/tenant.css"
                },
                Source = SettingsDocumentSource.Tenant,
                SourceScopeId = tenantId,
                ConcurrencyStamp = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                UpdatedAt = updatedAt
            });

        _lockService.GetLockStateAsync(Arg.Any<CancellationToken>())
            .Returns(new TenantBrandingSettingsDocumentLockState(
                CanChangeDisplayName: true,
                CanChangeLogoUrl: false,
                CanChangeFaviconUrl: true,
                CanChangeCustomCssUrl: true));

        var result = await _handler.Handle(new GetTenantBrandingSettingsDocumentQuery(), CancellationToken.None);

        await Assert.That(result).IsNotNull();
        await Assert.That(result!.DocumentKey).IsEqualTo(SettingsDocumentKeys.Tenant.Branding);
        await Assert.That(result.SchemaVersion).IsEqualTo(2);
        await Assert.That(result.DefaultsVersion).IsEqualTo("2026-05-branding");
        await Assert.That(result.Source).IsEqualTo(SettingsDocumentSource.Tenant.ToString());
        await Assert.That(result.SourceScopeId).IsEqualTo(tenantId);
        await Assert.That(result.ConcurrencyStamp).IsEqualTo(Guid.Parse("11111111-1111-1111-1111-111111111111"));
        await Assert.That(result.IsLockedByInstance).IsTrue();
        await Assert.That(result.CanChangeDisplayName).IsTrue();
        await Assert.That(result.CanChangeLogoUrl).IsFalse();
        await Assert.That(result.CanChangeFaviconUrl).IsTrue();
        await Assert.That(result.CanChangeCustomCssUrl).IsTrue();
        await Assert.That(result.UpdatedAt).IsEqualTo(updatedAt);
        await Assert.That(result.Payload.DisplayName).IsEqualTo("Open Islamu");
        await Assert.That(result.Payload.LogoUrl).IsEqualTo("https://cdn.example.test/logo.svg");
        await Assert.That(result.Payload.FaviconUrl).IsEqualTo("https://cdn.example.test/favicon.ico");
        await Assert.That(result.Payload.CustomCssUrl).IsEqualTo("https://cdn.example.test/tenant.css");
    }

    [Test]
    public async Task Handle_ShouldProvisionAndMapDocument_WhenResolverHasNoTypedBrandingDocument()
    {
        var tenantId = Guid.NewGuid();
        var provisioned = TenantBrandingSettingsDocumentDefaults.Create(tenantId, "Provisioned Brand");
        provisioned.Id = Guid.NewGuid();
        provisioned.ConcurrencyStamp = Guid.Parse("22222222-2222-2222-2222-222222222222");
        _tenantContext.TenantId.Returns(tenantId);
        _resolver.ResolveTenantDocumentAsync<BrandingSettings>(
                Arg.Any<SettingsResolutionContext>(),
                SettingsDocumentKeys.Tenant.Branding,
                Arg.Any<CancellationToken>())
            .Returns((ResolvedSettingsDocument<BrandingSettings>?)null);
        _provisioningService.EnsureTenantBrandingDocumentAsync(tenantId, null, Arg.Any<CancellationToken>())
            .Returns(provisioned);

        var result = await _handler.Handle(new GetTenantBrandingSettingsDocumentQuery(), CancellationToken.None);

        await Assert.That(result).IsNotNull();
        await Assert.That(result!.DocumentKey).IsEqualTo(SettingsDocumentKeys.Tenant.Branding);
        await Assert.That(result.Source).IsEqualTo(SettingsDocumentSource.Tenant.ToString());
        await Assert.That(result.SourceScopeId).IsEqualTo(tenantId);
        await Assert.That(result.ConcurrencyStamp).IsEqualTo(Guid.Parse("22222222-2222-2222-2222-222222222222"));
        await Assert.That(result.CanChangeDisplayName).IsTrue();
        await Assert.That(result.CanChangeLogoUrl).IsTrue();
        await Assert.That(result.CanChangeFaviconUrl).IsTrue();
        await Assert.That(result.CanChangeCustomCssUrl).IsTrue();
        await Assert.That(result.Payload.DisplayName).IsEqualTo("Provisioned Brand");
        await _provisioningService.Received(1)
            .EnsureTenantBrandingDocumentAsync(tenantId, null, Arg.Any<CancellationToken>());
    }
}
