// ABOUTME: Persistence-contract tests for authoritative inbound and outbound webhook payload bytes.
// ABOUTME: Verifies bytea mapping, byte/hash identity, immutable evidence metadata, and honest legacy provenance.

using System.Security.Cryptography;
using System.Text;
using Explore.Domain;
using Explore.Persistence;
using Microsoft.EntityFrameworkCore;
using TUnit.Core;

namespace Event.Persistence.IntegrationTests.Repositories;

public sealed class WebhookExactPayloadPersistenceTests
{
    [Test]
    public async Task DomainPayloadEvidence_PreservesExactBytesAndDetectsOneByteMutation()
    {
        var exactBytes = Encoding.UTF8.GetBytes("{ \"b\":2, \"a\": 1 }\n");
        var mutatedBytes = exactBytes.ToArray();
        mutatedBytes[^2] = (byte)'2';
        var now = new DateTime(2026, 7, 13, 19, 0, 0, DateTimeKind.Utc);

        var outbound = WebhookMessage.Create(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            "event.updated",
            "evt-exact",
            "event",
            Guid.CreateVersion7(),
            null,
            exactBytes,
            "application/json",
            "utf-8",
            now.AddMinutes(-1),
            now.AddDays(14),
            now);
        var inbound = IncomingWebhookMessage.CreateVerified(
            outbound.TenantId,
            "svix",
            "msg-exact",
            "idem-exact",
            "event.updated",
            exactBytes,
            Sha256(exactBytes),
            "application/json",
            "utf-8",
            null,
            now,
            now,
            now.AddDays(14));

        await Assert.That(outbound.GetPayloadBytes()).IsEquivalentTo(exactBytes);
        await Assert.That(inbound.PayloadBytes.ToArray()).IsEquivalentTo(exactBytes);
        await Assert.That(outbound.PayloadByteLength).IsEqualTo(exactBytes.LongLength);
        await Assert.That(inbound.PayloadByteLength).IsEqualTo(exactBytes.LongLength);
        await Assert.That(outbound.PayloadHash).IsEqualTo(Sha256(exactBytes));
        await Assert.That(inbound.PayloadHash).IsEqualTo(Sha256(exactBytes));
        await Assert.That(Sha256(mutatedBytes)).IsNotEqualTo(outbound.PayloadHash);
        await Assert.That(outbound.PayloadProvenanceId).IsEqualTo((int)WebhookPayloadProvenance.ExactBytes);
        await Assert.That(inbound.PayloadProvenanceId).IsEqualTo((int)WebhookPayloadProvenance.ExactBytes);
    }

    [Test]
    public async Task EfModel_MapsBothPayloadAuthoritiesToPostgresByteaWithEvidenceFields()
    {
        await using var context = CreateModelContext();

        foreach (var entityType in new[] { typeof(WebhookMessage), typeof(IncomingWebhookMessage) })
        {
            var entity = context.Model.FindEntityType(entityType)!;
            await Assert.That(entity.FindProperty("_payloadBytes")!.GetColumnType()).IsEqualTo("bytea");
            await Assert.That(entity.FindProperty(nameof(WebhookMessage.PayloadByteLength))!.ClrType).IsEqualTo(typeof(long));
            await Assert.That(entity.FindProperty(nameof(WebhookMessage.PayloadProvenanceId))!.IsNullable).IsFalse();
            await Assert.That(entity.FindProperty(nameof(WebhookMessage.ContentType))!.GetMaxLength()).IsEqualTo(200);
            await Assert.That(entity.FindProperty(nameof(WebhookMessage.ContentEncoding))!.GetMaxLength()).IsEqualTo(50);
        }
    }

    [Test]
    public async Task Migration_ClassifiesCanonicalizedJsonWithoutClaimingExactLegacyBytes()
    {
        var root = FindRepositoryRoot();
        var bindingMigration = await File.ReadAllTextAsync(Path.Combine(
            root,
            "src/Explore.Persistence/Migrations/20260713185132_AddWebhookProviderBindingFoundation.cs"));
        var freezeMigration = await File.ReadAllTextAsync(Directory.GetFiles(
            Path.Combine(root, "src/Explore.Persistence/Migrations"),
            "*FreezeWebhookDeliverySchema.cs").Single());

        await Assert.That(bindingMigration).Contains("convert_to(payload_json::text, 'UTF8')");
        await Assert.That(bindingMigration).Contains("payload_provenance_id = 2");
        await Assert.That(freezeMigration).Contains("{ 2, \"LEGACY_JSON_CANONICALIZED\"");
        await Assert.That(freezeMigration).Contains("payload_byte_length = octet_length(payload_bytes)");
        await Assert.That(freezeMigration).Contains("payload_provenance_id = 2");
    }

    private static ExploreDbContext CreateModelContext()
    {
        var options = new DbContextOptionsBuilder<ExploreDbContext>()
            .UseNpgsql("Host=localhost;Database=webhook_model;Username=unused;Password=unused")
            .UseSnakeCaseNamingConvention()
            .Options;
        return new ExploreDbContext(options);
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "AGENTS.md")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName
            ?? throw new DirectoryNotFoundException("Repository root containing AGENTS.md was not found.");
    }

    private static string Sha256(ReadOnlySpan<byte> bytes) =>
        $"sha256:{Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant()}";
}
