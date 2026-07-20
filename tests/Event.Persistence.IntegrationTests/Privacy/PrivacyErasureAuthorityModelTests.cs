// ABOUTME: Verifies generalized privacy-erasure EF models, retained composition, and function-only ACL contracts.
// ABOUTME: Pins User-only fact retention, replay coverage keys, receipt hashing, and design-time isolation.

using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.PrivacyErasure;
using Explore.Domain;
using Explore.Infrastructure;
using Explore.Persistence;
using Explore.Persistence.Privacy.ErasureAuthority;
using Explore.Persistence.Privacy.ErasureAuthority.Repositories;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace Event.Persistence.IntegrationTests.Privacy;

[NotInParallel]
public sealed class PrivacyErasureAuthorityModelTests
{
    [Test]
    public async Task RetainedAuthorityModel_IsMinimizedTypedAndRetentionBounded()
    {
        var options = new DbContextOptionsBuilder<PrivacyErasureAuthorityDbContext>()
            .UseNpgsql("Host=localhost;Database=model_only;Username=unused;Password=unused")
            .UseSnakeCaseNamingConvention()
            .Options;
        await using var context = new PrivacyErasureAuthorityDbContext(options);

        IModel model = context.GetService<IDesignTimeModel>().Model;
        IEntityType intent = model.FindEntityType(typeof(PrivacyErasureIntent))
            ?? throw new InvalidOperationException("Retained intent is not mapped.");

        await Assert.That(model.GetEntityTypes().Select(entity => entity.ClrType))
            .IsEquivalentTo([typeof(PrivacyErasureIntent), typeof(PrivacyErasureCounter)]);
        await Assert.That(intent.GetSchema()).IsEqualTo("privacy_erasure_authority");
        await Assert.That(intent.GetProperties().Select(property => property.GetColumnName()))
            .IsEquivalentTo([
                "authority_sequence", "intent_id", "policy_version", "reason_code",
                "recorded_at_utc", "requested_at_utc", "retention_expires_at_utc",
                "subject_id", "subject_kind"
            ]);
        await Assert.That(intent.GetCheckConstraints().Select(check => check.Sql))
            .Contains(sql => sql == "subject_kind = 1");
        await Assert.That(intent.GetCheckConstraints().Select(check => check.Sql))
            .Contains(sql => sql == "retention_expires_at_utc > recorded_at_utc");
    }

    [Test]
    public async Task ExploreModel_MapsSagaReceiptAndPolicyCoverageByTypedIntentIdentity()
    {
        await using ExploreDbContext context = CreateExploreContext();
        IModel model = context.Model;
        IEntityType saga = model.FindEntityType(typeof(PrivacyErasureSaga))
            ?? throw new InvalidOperationException("Privacy erasure saga is not mapped.");
        IEntityType coverage = model.FindEntityType(typeof(PrivacyErasurePolicyCoverage))
            ?? throw new InvalidOperationException("Privacy erasure policy coverage is not mapped.");
        IEntityType checkpoint = model.FindEntityType(typeof(PrivacyErasureReplayCheckpoint))
            ?? throw new InvalidOperationException("Privacy erasure replay checkpoint is not mapped.");

        await Assert.That(saga.FindProperty(nameof(PrivacyErasureSaga.ReceiptHash))!.GetMaxLength())
            .IsEqualTo(32);
        await Assert.That(saga.GetIndexes().Single(index =>
                index.Properties.Count == 1
                && index.Properties[0].Name == nameof(PrivacyErasureSaga.ReceiptHash))
            .IsUnique)
            .IsTrue();
        await Assert.That(coverage.FindPrimaryKey()!.Properties.Select(property => property.Name))
            .IsEquivalentTo([
                nameof(PrivacyErasurePolicyCoverage.IntentId),
                nameof(PrivacyErasurePolicyCoverage.SubjectKind),
                nameof(PrivacyErasurePolicyCoverage.PolicyVersion)
            ]);
        await Assert.That(checkpoint.GetTableName())
            .IsEqualTo("privacy_erasure_replay_checkpoints");
        await Assert.That(model.FindEntityType(typeof(PrivacyErasureIntent))!.GetSchema())
            .IsEqualTo("privacy_erasure_authority");
    }

    [Test]
    public async Task DataProtectionModel_RemainsUnchanged()
    {
        var options = new DbContextOptionsBuilder<DataProtectionKeyContext>()
            .UseNpgsql("Host=localhost;Database=model_only;Username=unused;Password=unused")
            .UseSnakeCaseNamingConvention()
            .Options;
        await using var context = new DataProtectionKeyContext(options);
        IEntityType entity = context.Model.GetEntityTypes().Single();

        await Assert.That(entity.ClrType).IsEqualTo(typeof(DataProtectionKey));
        await Assert.That(entity.GetTableName()).IsEqualTo("data_protection_keys");
        await Assert.That(entity.GetProperties().Select(property => property.Name))
            .IsEquivalentTo(["FriendlyName", "Id", "Xml"]);
    }

    [Test]
    public async Task DefaultPersistenceComposition_DoesNotRegisterRetainedAuthority()
    {
        var services = new ServiceCollection();
        services.ConfigurePersistenceServices(
            new ConfigurationBuilder().AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["ConnectionStrings:PrivacyErasureAuthority"] =
                        "Host=unused;Database=unused;Username=unused"
                }).Build(),
            skipDbContextRegistration: true,
            skipLookupCacheInitializer: true);

        await Assert.That(services.Any(item =>
            item.ServiceType == typeof(PrivacyErasureAuthorityDbContext))).IsFalse();
        await Assert.That(services.Any(item =>
            item.ServiceType == typeof(IPrivacyErasureAuthority))).IsFalse();
        await Assert.That(services.Any(item =>
            item.ServiceType == typeof(IPrivacyErasureLedgerRepository))).IsTrue();
    }

    [Test]
    public async Task DefaultComposition_PoisonAuthorityProviderIsNeverReadOrResolved()
    {
        var provider = new PoisonAuthorityConfigurationProvider();
        using var configuration = new ConfigurationRoot([provider]);
        var services = new ServiceCollection();

        services.ConfigurePersistenceServices(
            configuration,
            skipDbContextRegistration: true,
            skipLookupCacheInitializer: true);
        services.ConfigureInfrastructureServices(configuration);

        await Assert.That(provider.AuthorityReadCount).IsEqualTo(0);
        await Assert.That(services.Any(item =>
            item.ServiceType == typeof(PrivacyErasureAuthorityDbContext))).IsFalse();
        await Assert.That(services.Any(item =>
            item.ServiceType == typeof(IPrivacyErasureAuthority))).IsFalse();
        await Assert.That(services.Any(item =>
            item.ServiceType.FullName?.Contains(
                "IPrivacyErasureReplayService",
                StringComparison.Ordinal) == true)).IsFalse();
    }

    [Test]
    public async Task RetainedPersistenceComposition_RegistersOnlyGeneralizedAuthoritySurface()
    {
        var services = new ServiceCollection();
        services.ConfigurePersistenceServices(
            new ConfigurationBuilder().AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["PrivacyErasure:Durability:Mode"] = "RetainedAuthority",
                    ["ConnectionStrings:PrivacyErasureAuthority"] =
                        "Host=localhost;Database=privacy_erasure;Username=runtime;Password=unused"
                }).Build(),
            skipDbContextRegistration: true,
            skipLookupCacheInitializer: true);

        await Assert.That(services.Any(item =>
            item.ServiceType == typeof(PrivacyErasureAuthorityDbContext))).IsTrue();
        await Assert.That(services.Any(item =>
            item.ServiceType == typeof(IPrivacyErasureAuthority))).IsTrue();
    }

    [Test]
    public async Task DesignTimeFactory_IgnoresAmbientAndHostileArgumentsUnlessExplicitlySelected()
    {
        const string key = "ConnectionStrings__PrivacyErasureAuthority";
        string? previousValue = Environment.GetEnvironmentVariable(key);
        try
        {
            Environment.SetEnvironmentVariable(
                key,
                "Host=127.0.0.1;Port=2;Database=hostile_ambient;Username=canary;Password=canary");

            await using PrivacyErasureAuthorityDbContext context =
                new PrivacyErasureAuthorityDbContextFactory().CreateDbContext(
                    ["--hostile-connection", "Host=127.0.0.1;Port=3;Database=hostile_argument;Username=canary"]);
            var target = new NpgsqlConnectionStringBuilder(context.Database.GetConnectionString());

            await Assert.That(target.Host).IsEqualTo("127.0.0.1");
            await Assert.That(target.Port).IsEqualTo(1);
            await Assert.That(target.Database)
                .IsEqualTo("privacy_erasure_authority_design_time");
        }
        finally
        {
            Environment.SetEnvironmentVariable(key, previousValue);
        }
    }

    [Test]
    public async Task RuntimeAuthorityContract_IsFunctionOnlyWithDefaultPrivilegeRevokes()
    {
        string acl = PrivacyErasureAuthorityDatabaseContract.RuntimeAclSql;
        string normalized = acl.ToUpperInvariant();

        await Assert.That(normalized).Contains("GRANT EXECUTE ON FUNCTION");
        await Assert.That(normalized).Contains("REVOKE ALL ON ALL TABLES");
        await Assert.That(normalized).Contains("REVOKE ALL ON ALL SEQUENCES");
        await Assert.That(normalized).Contains("ALTER DEFAULT PRIVILEGES");
        await Assert.That(normalized).DoesNotContain("GRANT SELECT ON");
        await Assert.That(normalized).DoesNotContain("GRANT INSERT ON");
        await Assert.That(normalized).DoesNotContain("GRANT UPDATE ON");
        await Assert.That(normalized).DoesNotContain("GRANT DELETE ON");
        await Assert.That(normalized).DoesNotContain("GRANT TRUNCATE ON");
        await Assert.That(typeof(EfCorePrivacyErasureAuthorityRepository)
            .GetMethods()
            .Where(method =>
                method.DeclaringType == typeof(EfCorePrivacyErasureAuthorityRepository))
            .Select(method => method.Name)
            .Distinct())
            .IsEquivalentTo(["AppendAsync", "ReadAfterAsync"]);
    }

    private static ExploreDbContext CreateExploreContext()
    {
        var options = new DbContextOptionsBuilder<ExploreDbContext>()
            .UseNpgsql("Host=localhost;Database=model_only;Username=unused;Password=unused")
            .UseSnakeCaseNamingConvention()
            .Options;
        return new ExploreDbContext(options);
    }

    private sealed class PoisonAuthorityConfigurationProvider : ConfigurationProvider
    {
        private static readonly string[] ForbiddenPrefixes =
        [
            "PrivacyErasure:Authority",
            "ConnectionStrings:PrivacyErasureAuthority",
            "ConnectionStrings:LocationPrivacyAuthority",
            "LocationPrivacy:ErasureAuthority",
            "LocationPrivacy:ErasureDurability"
        ];

        private int _authorityReadCount;

        public int AuthorityReadCount => Volatile.Read(ref _authorityReadCount);

        public override bool TryGet(string key, out string? value)
        {
            ThrowIfAuthorityKey(key);
            value = null;
            return false;
        }

        public override IEnumerable<string> GetChildKeys(
            IEnumerable<string> earlierKeys,
            string? parentPath)
        {
            if (parentPath is not null)
            {
                ThrowIfAuthorityKey(parentPath);
            }

            return earlierKeys;
        }

        private void ThrowIfAuthorityKey(string key)
        {
            if (!ForbiddenPrefixes.Any(prefix =>
                    key.Equals(prefix, StringComparison.OrdinalIgnoreCase)
                    || key.StartsWith($"{prefix}:", StringComparison.OrdinalIgnoreCase)))
            {
                return;
            }

            Interlocked.Increment(ref _authorityReadCount);
            throw new InvalidOperationException("Default composition read an authority configuration key.");
        }
    }
}
