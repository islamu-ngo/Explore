// ABOUTME: Verifies shared tenant creation atomically writes both mandatory typed documents.
// ABOUTME: Proves Active creation fails before persistence when legal identity is incomplete.

using System.Text.Json;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Services;
using Explore.Application.Services;
using Explore.Domain;
using Explore.Domain.Enums;
using Explore.Domain.Settings.Documents;
using Explore.Domain.Settings.Documents.Payloads;
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
            "Primary Community",
            "primary",
            (int)TenantStatusEnum.Provisioning,
            actorId,
            occurredAt,
            BrandingSeed(
                documentId,
                """{"displayName":"Primary Community","logoUrl":null,"faviconUrl":null,"customCssUrl":null}"""),
            IdentitySeed(tenantId, "Primary Community"));

        TenantCreationOutcome outcome = await service.CreateInCurrentTransactionAsync(
            request,
            CancellationToken.None);

        await Assert.That(outcome.Tenant.Id).IsEqualTo(tenantId);
        await Assert.That(outcome.Tenant.CreatedAt).IsEqualTo(occurredAt);
        await Assert.That(outcome.Tenant.CreatedBy).IsEqualTo(actorId);
        await Assert.That(outcome.BrandingDocument.TenantId).IsEqualTo(tenantId);
        await Assert.That(outcome.BrandingDocument.Id).IsEqualTo(documentId);
        await Assert.That(outcome.BrandingDocument.PayloadJson).IsEqualTo(request.Branding.PayloadJson);
        await tenants.Received(1).Create(Arg.Is<Tenant>(tenant =>
            tenant.Id == tenantId && tenant.Slug == "primary"));
        await documents.Received(1).Create(Arg.Is<TenantSettingsDocument>(document =>
            document.TenantId == tenantId
            && document.DocumentKey == SettingsDocumentKeys.Tenant.Branding));
    }

    [Test]
    public async Task CreateInCurrentTransactionAsync_CreatesBothCanonicalDocumentsAtomically()
    {
        ITenantRepository tenants = Substitute.For<ITenantRepository>();
        ITenantSettingsDocumentRepository documents =
            Substitute.For<ITenantSettingsDocumentRepository>();
        var createdDocuments = new List<TenantSettingsDocument>();
        tenants.Create(Arg.Any<Tenant>()).Returns(call => call.Arg<Tenant>());
        documents.Create(Arg.Do<TenantSettingsDocument>(createdDocuments.Add))
            .Returns(call => call.Arg<TenantSettingsDocument>());
        var service = new TenantCreationService(tenants, documents);
        Guid tenantId = Guid.CreateVersion7();
        var request = new TenantCreationRequest(
            tenantId,
            "  Community Events  ",
            "community-events",
            (int)TenantStatusEnum.Provisioning,
            Guid.CreateVersion7(),
            new DateTime(2026, 8, 28, 12, 0, 0, DateTimeKind.Utc),
            BrandingSeed(
                Guid.CreateVersion7(),
                """{"displayName":"Community Events","logoUrl":null,"faviconUrl":null,"customCssUrl":null}"""),
            IdentitySeed(tenantId, "Community Events"));

        await service.CreateInCurrentTransactionAsync(request, CancellationToken.None);

        await Assert.That(createdDocuments).Count().IsEqualTo(2);
        TenantSettingsDocument identityDocument = createdDocuments.Single(
            document => document.DocumentKey == SettingsDocumentKeys.Tenant.DirectoryOperatorIdentity);
        TenantDirectoryOperatorIdentitySettings? payload =
            JsonSerializer.Deserialize<TenantDirectoryOperatorIdentitySettings>(
                identityDocument.PayloadJson,
                new JsonSerializerOptions(JsonSerializerDefaults.Web));
        await Assert.That(identityDocument.TenantId).IsEqualTo(tenantId);
        await Assert.That(payload).IsNotNull();
        await Assert.That(payload!.PublicName).IsEqualTo("Community Events");
        await Assert.That(payload.LegalName).IsNull();
    }

    [Test]
    public async Task CreateInCurrentTransactionAsync_RejectsActiveWithoutCompleteIdentityBeforeWriting()
    {
        ITenantRepository tenants = Substitute.For<ITenantRepository>();
        ITenantSettingsDocumentRepository documents =
            Substitute.For<ITenantSettingsDocumentRepository>();
        var service = new TenantCreationService(tenants, documents);
        Guid tenantId = Guid.CreateVersion7();
        var request = new TenantCreationRequest(
            tenantId,
            "Community Events",
            "community-events",
            (int)TenantStatusEnum.Active,
            Guid.CreateVersion7(),
            new DateTime(2026, 8, 28, 12, 0, 0, DateTimeKind.Utc),
            BrandingSeed(
                Guid.CreateVersion7(),
                """{"displayName":"Community Events","logoUrl":null,"faviconUrl":null,"customCssUrl":null}"""),
            IdentitySeed(tenantId, "Community Events"));

        await Assert.That(() => service.CreateInCurrentTransactionAsync(request, CancellationToken.None))
            .Throws<InvalidOperationException>();
        await tenants.DidNotReceiveWithAnyArgs().Create(default!);
        await documents.DidNotReceiveWithAnyArgs().Create(default!);
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
                    "Primary",
                    "primary",
                    (int)TenantStatusEnum.Provisioning,
                    ActorUserId: null,
                    occurredAt,
                    new TenantBrandingDocumentSeed(
                        Guid.CreateVersion7(),
                        SchemaVersion: 99,
                        TenantBrandingSettingsDocumentDefaults.DefaultsVersion,
                        "{}"),
                    IdentitySeed(Guid.CreateVersion7(), "Primary")),
                CancellationToken.None))
            .Throws<ArgumentException>();
    }

    private static TenantBrandingDocumentSeed BrandingSeed(Guid documentId, string payloadJson) =>
        new(
            documentId,
            TenantBrandingSettingsDocumentDefaults.SchemaVersion,
            TenantBrandingSettingsDocumentDefaults.DefaultsVersion,
            payloadJson);

    private static TenantDirectoryOperatorIdentityDocumentSeed IdentitySeed(
        Guid tenantId,
        string publicName)
    {
        TenantSettingsDocument document =
            TenantDirectoryOperatorIdentityDocumentDefaults.Create(tenantId, publicName);
        return new(
            Guid.CreateVersion7(),
            document.SchemaVersion,
            document.DefaultsVersion,
            document.PayloadJson);
    }
}
