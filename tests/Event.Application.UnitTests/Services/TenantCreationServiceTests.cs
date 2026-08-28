// ABOUTME: Verifies shared tenant creation writes the tenant and final branding document only.
// ABOUTME: Keeps transaction ownership and cache invalidation outside the reusable creation primitive.

using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Services;
using Explore.Application.Services;
using Explore.Domain;
using Explore.Domain.Enums;
using Explore.Domain.Settings.Documents;
using NSubstitute;

namespace Event.Application.UnitTests.Services;

public sealed class TenantCreationServiceTests
{
    [Test]
    public async Task CreateInCurrentTransactionAsync_PreservesPlannedIdentityAndFinalBranding()
    {
        ITenantRepository tenants = Substitute.For<ITenantRepository>();
        ITenantSettingsDocumentRepository documents =
            Substitute.For<ITenantSettingsDocumentRepository>();
        tenants.Create(Arg.Any<Tenant>()).Returns(call => call.Arg<Tenant>());
        documents.Create(Arg.Any<TenantSettingsDocument>())
            .Returns(call => call.Arg<TenantSettingsDocument>());
        var service = new TenantCreationService(tenants, documents);
        Guid tenantId = Guid.Parse("0198e2a4-5340-7f89-8abc-b8bdf43e0ea2");
        Guid documentId = Guid.Parse("0198e2a4-5340-7f89-8abc-b8bdf43e0ea4");
        Guid actorId = Guid.Parse("0198e2a4-5340-7f89-8abc-b8bdf43e0ea3");
        DateTime occurredAt = new(2026, 8, 25, 12, 0, 0, DateTimeKind.Utc);
        var request = new TenantCreationRequest(
            tenantId,
            documentId,
            "Primary Community",
            "primary",
            (int)TenantStatusEnum.Provisioning,
            actorId,
            occurredAt,
            SettingsDocumentKeys.Tenant.Branding,
            TenantBrandingSettingsDocumentDefaults.SchemaVersion,
            TenantBrandingSettingsDocumentDefaults.DefaultsVersion,
            """{"displayName":"Primary Community","logoUrl":null,"faviconUrl":null,"customCssUrl":null}""");

        TenantCreationOutcome outcome = await service.CreateInCurrentTransactionAsync(
            request,
            CancellationToken.None);

        await Assert.That(outcome.Tenant.Id).IsEqualTo(tenantId);
        await Assert.That(outcome.Tenant.CreatedAt).IsEqualTo(occurredAt);
        await Assert.That(outcome.Tenant.CreatedBy).IsEqualTo(actorId);
        await Assert.That(outcome.BrandingDocument.TenantId).IsEqualTo(tenantId);
        await Assert.That(outcome.BrandingDocument.Id).IsEqualTo(documentId);
        await Assert.That(outcome.BrandingDocument.PayloadJson).IsEqualTo(request.BrandingPayloadJson);
        await tenants.Received(1).Create(Arg.Is<Tenant>(tenant =>
            tenant.Id == tenantId && tenant.Slug == "primary"));
        await documents.Received(1).Create(Arg.Is<TenantSettingsDocument>(document =>
            document.TenantId == tenantId
            && document.DocumentKey == SettingsDocumentKeys.Tenant.Branding));
    }

    [Test]
    public async Task CreateInCurrentTransactionAsync_RejectsNonUuidV7AndMismatchedBrandingKey()
    {
        var service = new TenantCreationService(
            Substitute.For<ITenantRepository>(),
            Substitute.For<ITenantSettingsDocumentRepository>());
        DateTime occurredAt = new(2026, 8, 25, 12, 0, 0, DateTimeKind.Utc);

        await Assert.That(() => service.CreateInCurrentTransactionAsync(
                new TenantCreationRequest(
                    Guid.NewGuid(),
                    Guid.CreateVersion7(),
                    "Primary",
                    "primary",
                    (int)TenantStatusEnum.Provisioning,
                    ActorUserId: null,
                    occurredAt,
                    "unknown.document",
                    1,
                    "v1",
                    "{}"),
                CancellationToken.None))
            .Throws<ArgumentException>();
    }
}
