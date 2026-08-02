// ABOUTME: Verifies provider commands, failure results, transaction guards, and real SQLite named-lock leases.
// ABOUTME: Covers the provider-neutral lock boundary without claiming unexecuted server-engine behavior.

using System.Data.Common;
using Explore.Persistence;
using Explore.Persistence.Database;
using Explore.Secrets.Database;
using FluentAssertions;
using Microsoft.Data.SqlClient;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using MySqlConnector;
using Npgsql;
using TUnit.Core;

namespace Event.Persistence.IntegrationTests.Database;

public sealed class RelationalNamedLockTests
{
    [Test]
    public async Task ProviderCommands_UseRequiredOwnershipAndBoundedMySqlName()
    {
        const string resource = "notification-fanout-precedence:tenant:event";
        using DbCommand postgresTransaction = RelationalNamedLock.CreateAcquireCommand(
            new NpgsqlConnection(), null, RelationalNamedLock.PostgreSqlProvider, resource, true);
        using DbCommand postgresSession = RelationalNamedLock.CreateAcquireCommand(
            new NpgsqlConnection(), null, RelationalNamedLock.PostgreSqlProvider, resource, false);
        using DbCommand sqlServerTransaction = RelationalNamedLock.CreateAcquireCommand(
            new SqlConnection(), null, RelationalNamedLock.SqlServerProvider, resource, true);
        using DbCommand sqlServerSession = RelationalNamedLock.CreateAcquireCommand(
            new SqlConnection(), null, RelationalNamedLock.SqlServerProvider, resource, false);
        string mySqlResource = RelationalNamedLock.NormalizeProviderResource(
            RelationalNamedLock.MySqlProvider,
            new string('x', 500));
        using DbCommand mySql = RelationalNamedLock.CreateAcquireCommand(
            new MySqlConnection(), null, RelationalNamedLock.MySqlProvider, mySqlResource, true);
        using DbCommand postgresRelease = RelationalNamedLock.CreateReleaseCommand(
            new NpgsqlConnection(), RelationalNamedLock.PostgreSqlProvider, resource);
        using DbCommand sqlServerRelease = RelationalNamedLock.CreateReleaseCommand(
            new SqlConnection(), RelationalNamedLock.SqlServerProvider, resource);
        using DbCommand mySqlRelease = RelationalNamedLock.CreateReleaseCommand(
            new MySqlConnection(), RelationalNamedLock.MySqlProvider, mySqlResource);

        postgresTransaction.CommandText.Should().Be("SELECT pg_advisory_xact_lock(@key)");
        postgresSession.CommandText.Should().Be("SELECT pg_advisory_lock(@key)");
        sqlServerTransaction.CommandText.Should().Contain("@LockOwner = 'Transaction'");
        sqlServerSession.CommandText.Should().Contain("@LockOwner = 'Session'");
        mySql.CommandText.Should().Be("SELECT GET_LOCK(@resource, -1)");
        postgresRelease.CommandText.Should().Be("SELECT pg_advisory_unlock(@key)");
        sqlServerRelease.CommandText.Should().Contain("sys.sp_releaseapplock");
        mySqlRelease.CommandText.Should().Be("SELECT RELEASE_LOCK(@resource)");
        mySqlResource.Should().HaveLength(64);
        mySqlResource.Should().StartWith("explore:");
        await Assert.That(mySql.Parameters[0].Value).IsEqualTo(mySqlResource);
    }

    [Test]
    public async Task ProviderResults_FailClosedOnAcquireAndReleaseFailure()
    {
        RelationalNamedLock.EnsureAcquireSucceeded(RelationalNamedLock.SqlServerProvider, 0);
        RelationalNamedLock.EnsureAcquireSucceeded(RelationalNamedLock.MySqlProvider, 1L);
        RelationalNamedLock.EnsureReleaseSucceeded(RelationalNamedLock.PostgreSqlProvider, true);
        RelationalNamedLock.EnsureReleaseSucceeded(RelationalNamedLock.SqlServerProvider, 0);
        RelationalNamedLock.EnsureReleaseSucceeded(RelationalNamedLock.MySqlProvider, 1L);

        Assert.Throws<InvalidOperationException>(() =>
            RelationalNamedLock.EnsureAcquireSucceeded(RelationalNamedLock.SqlServerProvider, -1));
        Assert.Throws<InvalidOperationException>(() =>
            RelationalNamedLock.EnsureAcquireSucceeded(RelationalNamedLock.MySqlProvider, 0));
        Assert.Throws<InvalidOperationException>(() =>
            RelationalNamedLock.EnsureReleaseSucceeded(RelationalNamedLock.PostgreSqlProvider, false));
        Assert.Throws<InvalidOperationException>(() =>
            RelationalNamedLock.EnsureReleaseSucceeded(RelationalNamedLock.MySqlProvider, null));

        await Task.CompletedTask;
    }

    [Test]
    public async Task MySqlReleaseFailure_ClosesConnectionBeforeItCanReturnToPool()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using DbTransaction transaction = await connection.BeginTransactionAsync();
        var interceptor = new MySqlNamedLockTransactionInterceptor();
        interceptor.Track(transaction, "explore:test-lock");

        Assert.Throws<SqliteException>(() => interceptor.ReleaseTracked(transaction));

        await Assert.That(connection.State).IsEqualTo(System.Data.ConnectionState.Closed);
    }

    [Test]
    [Arguments(PrimaryDatabaseProvider.PostgreSql)]
    [Arguments(PrimaryDatabaseProvider.Sqlite)]
    [Arguments(PrimaryDatabaseProvider.SqlServer)]
    [Arguments(PrimaryDatabaseProvider.MariaDb)]
    [Arguments(PrimaryDatabaseProvider.MySql)]
    public async Task TransactionLock_RequiresActiveTransactionForEveryRelationalProvider(
        PrimaryDatabaseProvider provider)
    {
        var options = new DbContextOptionsBuilder<ExploreDbContext>();
        PrimaryDatabaseProviderComposition.ConfigureApplication(options, CreateOptions(provider));
        await using var context = new ExploreDbContext(options.Options);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            RelationalNamedLock.AcquireTransactionAsync(context, "test-lock", CancellationToken.None));
    }

    [Test]
    public async Task SqliteSessionLease_SerializesAcrossContextsAndReleases()
    {
        await using ExploreDbContext firstContext = CreateSqliteContext();
        await using ExploreDbContext secondContext = CreateSqliteContext();
        var firstLock = new RelationalAtprotoSessionRefreshLock(firstContext);
        var secondLock = new RelationalAtprotoSessionRefreshLock(secondContext);
        Guid tenantId = Guid.CreateVersion7();
        Guid userId = Guid.CreateVersion7();

        IAsyncDisposable firstLease = await firstLock.AcquireAsync(
            tenantId, userId, "pds", "did:plc:sqlite-lock", CancellationToken.None);
        Task<IAsyncDisposable> waiting = secondLock.AcquireAsync(
            tenantId, userId, "pds", "did:plc:sqlite-lock", CancellationToken.None);

        await Task.Delay(100);
        await Assert.That(waiting.IsCompleted).IsFalse();
        await firstLease.DisposeAsync();
        await using IAsyncDisposable secondLease = await waiting.WaitAsync(TimeSpan.FromSeconds(2));
    }

    [Test]
    public async Task SqliteSettingLock_SerializesConcurrentOperations()
    {
        await using ExploreDbContext firstContext = CreateSqliteContext();
        await using ExploreDbContext secondContext = CreateSqliteContext();
        var firstLock = new RelationalSettingMutationLock(firstContext, new EfCoreUnitOfWork(firstContext));
        var secondLock = new RelationalSettingMutationLock(secondContext, new EfCoreUnitOfWork(secondContext));
        var firstEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseFirst = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        bool secondEntered = false;

        Task first = firstLock.ExecuteAsync("governance.lock", async _ =>
        {
            firstEntered.SetResult();
            await releaseFirst.Task;
            return true;
        });
        await firstEntered.Task.WaitAsync(TimeSpan.FromSeconds(2));
        Task second = secondLock.ExecuteAsync("governance.lock", _ =>
        {
            secondEntered = true;
            return Task.FromResult(true);
        });

        await Task.Delay(100);
        await Assert.That(secondEntered).IsFalse();
        releaseFirst.SetResult();
        await Task.WhenAll(first, second).WaitAsync(TimeSpan.FromSeconds(2));
        await Assert.That(secondEntered).IsTrue();
    }

    private static ExploreDbContext CreateSqliteContext()
    {
        var options = new DbContextOptionsBuilder<ExploreDbContext>()
            .UseSqlite("Data Source=:memory:")
            .Options;
        return new ExploreDbContext(options);
    }

    private static PrimaryDatabaseConnectionOptions CreateOptions(PrimaryDatabaseProvider provider) => new()
    {
        Role = PrimaryDatabaseRole.Runtime,
        Provider = provider,
        Host = provider == PrimaryDatabaseProvider.Sqlite ? null : "localhost",
        Port = provider switch
        {
            PrimaryDatabaseProvider.PostgreSql => 5432,
            PrimaryDatabaseProvider.SqlServer => 1433,
            PrimaryDatabaseProvider.MariaDb or PrimaryDatabaseProvider.MySql => 3306,
            _ => null,
        },
        Database = provider == PrimaryDatabaseProvider.Sqlite ? "lock-tests.db" : "event",
        Username = provider == PrimaryDatabaseProvider.Sqlite ? null : "event",
        Password = provider == PrimaryDatabaseProvider.Sqlite ? null : "password",
        TlsMode = provider == PrimaryDatabaseProvider.Sqlite
            ? PrimaryDatabaseTlsMode.Prefer
            : PrimaryDatabaseTlsMode.Disabled,
        ServerFlavor = provider switch
        {
            PrimaryDatabaseProvider.MariaDb => PrimaryDatabaseServerFlavor.MariaDb,
            PrimaryDatabaseProvider.MySql => PrimaryDatabaseServerFlavor.MySql,
            _ => null,
        },
        ServerVersion = provider switch
        {
            PrimaryDatabaseProvider.MariaDb => new Version(11, 4),
            PrimaryDatabaseProvider.MySql => new Version(8, 4),
            _ => null,
        },
    };
}
