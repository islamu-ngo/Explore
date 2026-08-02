// ABOUTME: Rehearses a primary-only SQLite restore while the embedded erasure authority remains untouched.
// ABOUTME: Proves a restarted authority replays retained intent into restored primary state exactly once.

using Explore.Application.Configuration;
using Explore.Application.Contracts.PrivacyErasure;
using Explore.Application.Services;
using Explore.Domain;
using Explore.Domain.Enums;
using Explore.Persistence;
using Explore.Persistence.Database;
using Explore.Persistence.Privacy.ErasureAuthority;
using Explore.Persistence.Seed;
using Explore.Secrets.Database;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Event.Persistence.IntegrationTests.Privacy;

[NotInParallel("EmbeddedPrivacyErasureRecovery")]
public sealed class EmbeddedPrivacyErasureRecoveryTests
{
    [Test]
    [Timeout(300_000)]
    public async Task PrimaryOnlyRestore_RetainsEmbeddedAuthorityAndReplayConvergesExactlyOnce()
    {
        DirectoryInfo root = Directory.CreateTempSubdirectory("mdb-erasure-recovery-");
        string primaryDirectory = Directory.CreateDirectory(Path.Combine(root.FullName, "primary")).FullName;
        string authorityDirectory = Directory.CreateDirectory(Path.Combine(root.FullName, "authority")).FullName;
        string backupDirectory = Directory.CreateDirectory(Path.Combine(root.FullName, "backup")).FullName;
        string primaryPath = Path.Combine(primaryDirectory, "event.db");
        string authorityPath = Path.Combine(authorityDirectory, "privacy_erasure_authority.db");
        string backupPath = Path.Combine(backupDirectory, "event-before-erasure.db");
        string restoredPath = Path.Combine(primaryDirectory, "event-restored.db");

        try
        {
            GlobalLocationPrivacyErasureTests.ErasureGraph graph;
            await using (ExploreDbContext primary = CreatePrimaryContext(primaryPath))
            {
                await primary.Database.EnsureCreatedAsync();
                await LookupTableSeeder.SeedAsync(primary);
                graph = await GlobalLocationPrivacyErasureTests.SeedErasureGraphAsync(primary);
            }
            await CopyDatabaseAsync(primaryPath, backupPath);

            PrivacyErasureIntent retained;
            AuthorityFactSnapshot[] authorityBeforeRestore;
            await using (ServiceProvider firstProcess = await CreateAuthorityProviderAsync(authorityPath))
            {
                IPrivacyErasureAuthority authority =
                    firstProcess.GetRequiredService<IPrivacyErasureAuthority>();
                retained = await authority.AppendAsync(new PrivacyErasureRequest(
                    Guid.CreateVersion7(),
                    PrivacyErasureSubjectKind.User,
                    graph.OwnerUserId,
                    PrivacyErasureReasonCode.AccountDeletion,
                    1));
                authorityBeforeRestore = await ReadAuthoritySnapshotAsync(authority);
            }

            await CopyDatabaseAsync(backupPath, restoredPath);
            await using (ExploreDbContext restored = CreatePrimaryContext(restoredPath))
            {
                await Assert.That(await restored.UserPii
                    .AnyAsync(pii => pii.UserId == graph.OwnerUserId)).IsTrue();
                await Assert.That(await restored.PrivacyErasureReplayCheckpoints.CountAsync())
                    .IsEqualTo(0);
                await AssertPrimaryContainsNoAuthorityTablesAsync(restored);
            }

            int checkpointCount;
            int outboxCount;
            await using (ServiceProvider restartedProcess = await CreateAuthorityProviderAsync(authorityPath))
            {
                IPrivacyErasureAuthority authority =
                    restartedProcess.GetRequiredService<IPrivacyErasureAuthority>();
                await Assert.That(await ReadAuthoritySnapshotAsync(authority))
                    .IsEquivalentTo(authorityBeforeRestore);

                await using (ExploreDbContext replayContext = CreatePrimaryContext(restoredPath))
                await using (GlobalLocationPrivacyErasureTests.ErasureRuntime runtime =
                    GlobalLocationPrivacyErasureTests.CreateRuntime(replayContext, authority))
                {
                    await runtime.ReplayService.ReplayAsync(CancellationToken.None);
                }

                await using (ExploreDbContext verified = CreatePrimaryContext(restoredPath))
                {
                    await AssertErasedAsync(verified, graph);
                    PrivacyErasureReplayCheckpoint checkpoint = await verified
                        .PrivacyErasureReplayCheckpoints.SingleAsync();
                    checkpointCount = await verified.PrivacyErasureReplayCheckpoints.CountAsync();
                    outboxCount = await CountPrivacyOutboxAsync(verified);
                    await Assert.That(checkpoint.AuthoritySequence)
                        .IsEqualTo(retained.AuthoritySequence);
                    await Assert.That(checkpoint.IntentId).IsEqualTo(retained.IntentId);
                    await Assert.That(outboxCount).IsEqualTo(4);
                }

                await using (ExploreDbContext repeatedContext = CreatePrimaryContext(restoredPath))
                await using (GlobalLocationPrivacyErasureTests.ErasureRuntime runtime =
                    GlobalLocationPrivacyErasureTests.CreateRuntime(repeatedContext, authority))
                {
                    await runtime.ReplayService.ReplayAsync(CancellationToken.None);
                }

                await using (ExploreDbContext final = CreatePrimaryContext(restoredPath))
                {
                    await AssertErasedAsync(final, graph);
                    await Assert.That(await final.PrivacyErasureReplayCheckpoints.CountAsync())
                        .IsEqualTo(checkpointCount);
                    await Assert.That(await CountPrivacyOutboxAsync(final)).IsEqualTo(outboxCount);
                }

                await Assert.That(await ReadAuthoritySnapshotAsync(authority))
                    .IsEquivalentTo(authorityBeforeRestore);
            }

            await Assert.That(Path.GetDirectoryName(primaryPath))
                .IsNotEqualTo(Path.GetDirectoryName(authorityPath));
            await Assert.That(File.Exists(authorityPath)).IsTrue();
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            root.Delete(recursive: true);
        }
    }

    [Test]
    [Arguments("")]
    [Arguments("-wal")]
    [Arguments("-shm")]
    public async Task ExistingAuthorityFiles_WithUnsafeUnixPermissions_FailClosed(string suffix)
    {
        if (OperatingSystem.IsWindows())
        {
            return;
        }

        DirectoryInfo root = Directory.CreateTempSubdirectory("mdb-erasure-permissions-");
        string authorityPath = Path.Combine(root.FullName, "privacy_erasure_authority.db");
        string unsafePath = authorityPath + suffix;
        try
        {
            await File.WriteAllBytesAsync(unsafePath, [0]);
            File.SetUnixFileMode(
                unsafePath,
                UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.GroupRead);
            using var storage = new EmbeddedPrivacyErasureAuthorityStorage(
                EmbeddedOptions(authorityPath));

            InvalidOperationException? exception = await Assert.That(() =>
                    storage.EnsureReadyAsync())
                .Throws<InvalidOperationException>();

            await Assert.That(exception!.Message).Contains("permissions are unsafe");
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            root.Delete(recursive: true);
        }
    }

    [Test]
    public async Task ExistingAuthoritySymbolicLink_FailsClosed()
    {
        if (OperatingSystem.IsWindows())
        {
            return;
        }

        DirectoryInfo root = Directory.CreateTempSubdirectory("mdb-erasure-symlink-");
        string targetPath = Path.Combine(root.FullName, "target.db");
        string authorityPath = Path.Combine(root.FullName, "privacy_erasure_authority.db");
        try
        {
            await File.WriteAllBytesAsync(targetPath, [0]);
            File.SetUnixFileMode(targetPath, UnixFileMode.UserRead | UnixFileMode.UserWrite);
            File.CreateSymbolicLink(authorityPath, targetPath);
            using var storage = new EmbeddedPrivacyErasureAuthorityStorage(
                EmbeddedOptions(authorityPath));

            InvalidOperationException? exception = await Assert.That(() =>
                    storage.EnsureReadyAsync())
                .Throws<InvalidOperationException>();

            await Assert.That(exception!.Message).Contains("symbolic-link");
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            root.Delete(recursive: true);
        }
    }

    private static ExploreDbContext CreatePrimaryContext(string path)
    {
        var options = new PrimaryDatabaseConnectionOptions
        {
            Role = PrimaryDatabaseRole.Runtime,
            Provider = PrimaryDatabaseProvider.Sqlite,
            Database = path,
        };
        var builder = new DbContextOptionsBuilder<ExploreDbContext>();
        PrimaryDatabaseProviderComposition.ConfigureApplication(builder, options);
        var context = new ExploreDbContext(builder.Options);
        context.EnableTenantFilterBypass("Embedded authority primary-only recovery rehearsal.");
        return context;
    }

    private static async Task<ServiceProvider> CreateAuthorityProviderAsync(string authorityPath)
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["PrivacyErasure:Authority:Topology"] = "EmbeddedSqlite",
                ["PrivacyErasureAuthorityEmbedded:Path"] = authorityPath,
                ["PrivacyErasureAuthorityEmbedded:WriterReplicaCount"] = "1",
                ["PrivacyErasureAuthorityEmbedded:BusyTimeoutSeconds"] = "30",
            })
            .Build();
        var services = new ServiceCollection();
        services.AddOptions<PrivacyErasureOptions>();
        services.ConfigurePersistenceServices(
            configuration,
            skipDbContextRegistration: true,
            skipLookupCacheInitializer: true);
        ServiceProvider provider = services.BuildServiceProvider(validateScopes: true);

        await provider.GetRequiredService<EmbeddedPrivacyErasureAuthorityStorage>()
            .EnsureReadyAsync();
        IDbContextFactory<EmbeddedPrivacyErasureAuthorityDbContext> factory = provider
            .GetRequiredService<IDbContextFactory<EmbeddedPrivacyErasureAuthorityDbContext>>();
        await using EmbeddedPrivacyErasureAuthorityDbContext context =
            await factory.CreateDbContextAsync();
        await context.Database.EnsureCreatedAsync();
        return provider;
    }

    private static EmbeddedPrivacyErasureAuthorityOptions EmbeddedOptions(string path) => new()
    {
        Path = path,
        WriterReplicaCount = 1,
        BusyTimeoutSeconds = 30,
    };

    private static async Task CopyDatabaseAsync(string sourcePath, string destinationPath)
    {
        await using var source = new SqliteConnection(new SqliteConnectionStringBuilder
        {
            DataSource = sourcePath,
            Mode = SqliteOpenMode.ReadOnly,
            Pooling = false,
        }.ConnectionString);
        await using var destination = new SqliteConnection(new SqliteConnectionStringBuilder
        {
            DataSource = destinationPath,
            Mode = SqliteOpenMode.ReadWriteCreate,
            Pooling = false,
        }.ConnectionString);
        await source.OpenAsync();
        await destination.OpenAsync();
        source.BackupDatabase(destination);
    }

    private static async Task AssertPrimaryContainsNoAuthorityTablesAsync(ExploreDbContext context)
    {
        await context.Database.OpenConnectionAsync();
        try
        {
            var connection = (SqliteConnection)context.Database.GetDbConnection();
            await using SqliteCommand command = connection.CreateCommand();
            command.CommandText =
                "SELECT name FROM sqlite_master WHERE type = 'table' AND name IN ('authority_counter', 'erasure_intents', '__EFPrivacyErasureAuthorityMigrationsHistory')";
            await using SqliteDataReader reader = await command.ExecuteReaderAsync();
            await Assert.That(await reader.ReadAsync()).IsFalse();
        }
        finally
        {
            await context.Database.CloseConnectionAsync();
        }
    }

    private static async Task AssertErasedAsync(
        ExploreDbContext context,
        GlobalLocationPrivacyErasureTests.ErasureGraph graph)
    {
        await Assert.That(await context.UserPii
            .AnyAsync(pii => pii.UserId == graph.OwnerUserId)).IsFalse();
        await Assert.That(await context.UserPii
            .AnyAsync(pii => pii.UserId == graph.UnrelatedUserId)).IsTrue();
        Location[] homes = await context.Locations
            .IgnoreQueryFilters()
            .Include(location => location.Pii)
            .Where(location => graph.LocationIds.Contains(location.Id))
            .ToArrayAsync();
        await Assert.That(homes.All(home =>
            home.OwnerUserId is null
            && home.Pii is null
            && home.LocationPrivacyStateId == (int)LocationPrivacyStateEnum.Erased)).IsTrue();
    }

    private static Task<int> CountPrivacyOutboxAsync(ExploreDbContext context) =>
        context.OutboxMessages.CountAsync(message =>
            message.EventType == LocationPrivacyOutboxMessageFactory.LocationPiiErasedEventType
            || message.EventType == LocationPrivacyOutboxMessageFactory.LocationPrivacyCorrectionRequestedEventType);

    private static async Task<AuthorityFactSnapshot[]> ReadAuthoritySnapshotAsync(
        IPrivacyErasureAuthority authority)
    {
        IReadOnlyList<PrivacyErasureIntent> facts = await authority.ReadAfterAsync(0, 500);
        return facts.Select(fact => new AuthorityFactSnapshot(
            fact.AuthoritySequence,
            fact.IntentId,
            fact.SubjectKind,
            fact.SubjectId,
            fact.ReasonCode,
            fact.PolicyVersion,
            fact.RequestedAtUtc,
            fact.RecordedAtUtc,
            fact.RetentionExpiresAtUtc)).ToArray();
    }

    private sealed record AuthorityFactSnapshot(
        long AuthoritySequence,
        Guid IntentId,
        PrivacyErasureSubjectKind SubjectKind,
        Guid SubjectId,
        PrivacyErasureReasonCode ReasonCode,
        int PolicyVersion,
        DateTime RequestedAtUtc,
        DateTime RecordedAtUtc,
        DateTime RetentionExpiresAtUtc);
}
