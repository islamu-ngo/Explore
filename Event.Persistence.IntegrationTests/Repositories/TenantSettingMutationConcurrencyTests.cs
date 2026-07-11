// ABOUTME: PostgreSQL concurrency tests for linearized system and tenant setting mutations.
// ABOUTME: Uses two DbContexts to prove value-only writes, expected-state CAS, and advisory-lock ordering.

using Event.Persistence.IntegrationTests.Fixtures;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Responses;
using Explore.Application.Settings;
using Explore.Domain;
using Explore.Domain.Constants;
using Explore.Domain.Enums;
using Explore.Domain.Settings;
using Explore.Persistence;
using Explore.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using TUnit.Assertions;
using TUnit.Core;

namespace Event.Persistence.IntegrationTests.Repositories;

[ClassDataSource<PostgreSqlContainerFixture>(Shared = SharedType.PerAssembly)]
[NotInParallel("PersistenceDb")]
public sealed class TenantSettingMutationConcurrencyTests(PostgreSqlContainerFixture fixture)
{
    private const string SettingKey = GovernanceSettingKeys.Email.SmtpHost;
    private readonly Guid _actorId = Guid.NewGuid();

    [Test]
    public async Task SetValueAsync_WhenOverrideIsLocked_ChangesValueWithoutUnlocking()
    {
        Guid tenantId = await SeedAsync(tenantLocked: true);
        await using ExploreDbContext context = fixture.CreateDbContext();
        var repository = new TenantSettingRepository(context);

        await repository.SetValueAsync(tenantId, SettingKey, "\"updated\"");

        TenantSetting saved = await ReadTenantSettingAsync(tenantId);
        await Assert.That(saved.Value).IsEqualTo("\"updated\"");
        await Assert.That(saved.IsLocked).IsTrue();
    }

    [Test]
    public async Task LockAndUnlockAsync_WhenExpectedStateDoesNotMatch_ReturnFalseWithoutMutation()
    {
        Guid tenantId = await SeedAsync(tenantLocked: true);
        Guid deniedLockActor = Guid.NewGuid();
        Guid unlockActor = Guid.NewGuid();
        Guid deniedUnlockActor = Guid.NewGuid();
        await using ExploreDbContext context = fixture.CreateDbContext();
        var repository = new TenantSettingRepository(context);

        bool lockApplied = await repository.LockAsync(tenantId, SettingKey, deniedLockActor);
        bool unlockApplied = await repository.UnlockAsync(tenantId, SettingKey, unlockActor);
        bool secondUnlockApplied = await repository.UnlockAsync(tenantId, SettingKey, deniedUnlockActor);
        TenantSetting saved = await ReadTenantSettingAsync(tenantId);

        await Assert.That(lockApplied).IsFalse();
        await Assert.That(unlockApplied).IsTrue();
        await Assert.That(secondUnlockApplied).IsFalse();
        await Assert.That(saved.UpdatedBy).IsEqualTo(unlockActor);
        await Assert.That(saved.UpdatedAt).IsNotNull();
    }

    [Test]
    public async Task UpsertManyForTenantAsync_StampsActorOnUpdatesAndInserts()
    {
        Guid tenantId = await SeedAsync(tenantLocked: false);
        Guid actorId = Guid.NewGuid();
        await using ExploreDbContext context = fixture.CreateDbContext();
        var repository = new TenantSettingRepository(context);

        await repository.UpsertManyForTenantAsync(
            tenantId,
            [
                new TenantSettingOverrideUpsert(SettingKey, "\"updated\"", true),
                new TenantSettingOverrideUpsert(GovernanceSettingKeys.Email.SmtpPort, "2525", false)
            ],
            actorId);

        List<TenantSetting> saved = await context.TenantSettingOverrides
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(setting => setting.TenantId == tenantId)
            .ToListAsync();
        TenantSetting updated = saved.Single(setting => setting.SettingKey == SettingKey);
        TenantSetting inserted = saved.Single(setting => setting.SettingKey == GovernanceSettingKeys.Email.SmtpPort);
        await Assert.That(updated.UpdatedBy).IsEqualTo(actorId);
        await Assert.That(updated.UpdatedAt).IsNotNull();
        await Assert.That(inserted.CreatedBy).IsEqualTo(actorId);
        await Assert.That(inserted.CreatedAt).IsNotEqualTo(default);
    }

    [Test]
    public async Task SetThenLock_AreSerializedWithoutLosingValueOrLock()
    {
        Guid tenantId = await SeedAsync(tenantLocked: false);
        await using ExploreDbContext firstContext = fixture.CreateDbContext();
        await using ExploreDbContext secondContext = fixture.CreateDbContext();
        var firstRepository = new TenantSettingRepository(firstContext);
        var secondRepository = new TenantSettingRepository(secondContext);
        PostgresSettingMutationLock firstLock = CreateMutationLock(firstContext);
        PostgresSettingMutationLock secondLock = CreateMutationLock(secondContext);

        bool lockApplied = await RunFirstThenSecondAsync(
            firstLock,
            secondContext,
            token => firstRepository.SetValueAsync(tenantId, SettingKey, "\"raced\"", token),
            () => secondLock.ExecuteAsync(
                SettingKey,
                token => secondRepository.LockAsync(tenantId, SettingKey, _actorId, token)));

        TenantSetting saved = await ReadTenantSettingAsync(tenantId);
        await Assert.That(lockApplied).IsTrue();
        await Assert.That(saved.Value).IsEqualTo("\"raced\"");
        await Assert.That(saved.IsLocked).IsTrue();
    }

    [Test]
    public async Task SetThenUnlock_AreSerializedWithoutRestoringStaleLockState()
    {
        Guid tenantId = await SeedAsync(tenantLocked: true);
        await using ExploreDbContext firstContext = fixture.CreateDbContext();
        await using ExploreDbContext secondContext = fixture.CreateDbContext();
        var firstRepository = new TenantSettingRepository(firstContext);
        var secondRepository = new TenantSettingRepository(secondContext);
        PostgresSettingMutationLock firstLock = CreateMutationLock(firstContext);
        PostgresSettingMutationLock secondLock = CreateMutationLock(secondContext);

        bool unlockApplied = await RunFirstThenSecondAsync(
            firstLock,
            secondContext,
            token => firstRepository.SetValueAsync(tenantId, SettingKey, "\"raced\"", token),
            () => secondLock.ExecuteAsync(
                SettingKey,
                token => secondRepository.UnlockAsync(tenantId, SettingKey, _actorId, token)));

        TenantSetting saved = await ReadTenantSettingAsync(tenantId);
        await Assert.That(unlockApplied).IsTrue();
        await Assert.That(saved.Value).IsEqualTo("\"raced\"");
        await Assert.That(saved.IsLocked).IsFalse();
    }

    [Test]
    public async Task LockThenUnlock_AreSerializedByExpectedState()
    {
        Guid tenantId = await SeedAsync(tenantLocked: false);
        await using ExploreDbContext firstContext = fixture.CreateDbContext();
        await using ExploreDbContext secondContext = fixture.CreateDbContext();
        var firstRepository = new TenantSettingRepository(firstContext);
        var secondRepository = new TenantSettingRepository(secondContext);
        PostgresSettingMutationLock firstLock = CreateMutationLock(firstContext);
        PostgresSettingMutationLock secondLock = CreateMutationLock(secondContext);

        bool unlockApplied = await RunFirstThenSecondAsync(
            firstLock,
            secondContext,
            async token =>
            {
                bool lockApplied = await firstRepository.LockAsync(tenantId, SettingKey, _actorId, token);
                await Assert.That(lockApplied).IsTrue();
            },
            () => secondLock.ExecuteAsync(
                SettingKey,
                token => secondRepository.UnlockAsync(tenantId, SettingKey, _actorId, token)));

        TenantSetting saved = await ReadTenantSettingAsync(tenantId);
        await Assert.That(unlockApplied).IsTrue();
        await Assert.That(saved.IsLocked).IsFalse();
    }

    [Test]
    public async Task ExecuteManyAsync_WithReverseOrder_UsesOneCanonicalWaitOrder()
    {
        await fixture.ResetAsync();
        await using ExploreDbContext firstContext = fixture.CreateDbContext();
        await using ExploreDbContext secondContext = fixture.CreateDbContext();
        PostgresSettingMutationLock firstLock = CreateMutationLock(firstContext);
        PostgresSettingMutationLock secondLock = CreateMutationLock(secondContext);
        int secondBackendPid = await GetBackendPidAsync(secondContext);
        var firstEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseFirst = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        Task<bool> firstTask = firstLock.ExecuteManyAsync(
            [GovernanceSettingKeys.Email.SmtpPort, SettingKey],
            async token =>
            {
                firstEntered.SetResult();
                await releaseFirst.Task.WaitAsync(token);
                return true;
            });
        await firstEntered.Task;
        Task<bool> secondTask = secondLock.ExecuteManyAsync(
            [SettingKey, GovernanceSettingKeys.Email.SmtpPort],
            _ => Task.FromResult(true));

        await WaitUntilAdvisoryLockIsWaitingAsync(secondBackendPid, secondTask);
        await Assert.That(secondTask.IsCompleted).IsFalse();
        releaseFirst.SetResult();

        await Assert.That(await firstTask).IsTrue();
        await Assert.That(await secondTask).IsTrue();
    }

    [Test]
    public async Task ExecuteManyAsync_WhenNested_ReusesActiveTransaction()
    {
        await fixture.ResetAsync();
        await using ExploreDbContext context = fixture.CreateDbContext();
        PostgresSettingMutationLock mutationLock = CreateMutationLock(context);

        bool result = await mutationLock.ExecuteManyAsync(
            [GovernanceSettingKeys.Email.SmtpPort, SettingKey],
            token => mutationLock.ExecuteManyAsync(
                [SettingKey, GovernanceSettingKeys.Email.SmtpPort],
                _ => Task.FromResult(true),
                token));

        await Assert.That(result).IsTrue();
    }

    [Test]
    [Arguments(TenantMutation.Set, false)]
    [Arguments(TenantMutation.Lock, false)]
    [Arguments(TenantMutation.Unlock, true)]
    public async Task SystemLockThenTenantMutation_RecheckRejectsWithoutTenantMutation(
        TenantMutation mutation,
        bool tenantInitiallyLocked)
    {
        Guid tenantId = await SeedAsync(tenantInitiallyLocked);
        await using ExploreDbContext firstContext = fixture.CreateDbContext();
        await using ExploreDbContext secondContext = fixture.CreateDbContext();
        PostgresSettingMutationLock firstLock = CreateMutationLock(firstContext);
        PostgresSettingMutationLock secondLock = CreateMutationLock(secondContext);
        var firstSystemRepository = new SystemSettingRepository(firstContext, firstLock);
        var secondSystemRepository = new SystemSettingRepository(secondContext, secondLock);
        var secondTenantRepository = new TenantSettingRepository(secondContext);

        BaseCommandResponse<Guid> result = await RunFirstThenSecondAsync(
            firstLock,
            secondContext,
            token => LockSystemSettingAsync(firstSystemRepository, token),
            () => ExecuteTenantMutationAsync(
                mutation,
                tenantId,
                secondTenantRepository,
                secondSystemRepository,
                secondLock));

        TenantSetting saved = await ReadTenantSettingAsync(tenantId);
        await Assert.That(result.FailureCode).IsEqualTo("setting_system_locked");
        await Assert.That(saved.Value).IsEqualTo("\"baseline\"");
        await Assert.That(saved.IsLocked).IsEqualTo(tenantInitiallyLocked);
    }

    private async Task<Guid> SeedAsync(bool tenantLocked)
    {
        await fixture.ResetAsync();
        await using ExploreDbContext context = fixture.CreateDbContext();
        SystemSetting systemSetting = await context.SystemSettings.SingleAsync(setting => setting.SettingKey == SettingKey);
        systemSetting.Value = "\"system\"";
        systemSetting.IsLocked = false;

        var tenant = new Tenant
        {
            FullName = "Setting Race Tenant",
            Slug = $"setting-race-{Guid.NewGuid():N}",
            TenantStatusId = (int)TenantStatusEnum.Active,
            TenantStatus = null!
        };
        context.Tenants.Add(tenant);
        await context.SaveChangesAsync();
        context.TenantSettingOverrides.Add(new TenantSetting
        {
            TenantId = tenant.Id,
            Tenant = null!,
            SettingKey = SettingKey,
            Value = "\"baseline\"",
            IsLocked = tenantLocked
        });
        await context.SaveChangesAsync();
        return tenant.Id;
    }

    private async Task<TenantSetting> ReadTenantSettingAsync(Guid tenantId)
    {
        await using ExploreDbContext context = fixture.CreateDbContext();
        return await context.TenantSettingOverrides
            .AsNoTracking()
            .SingleAsync(setting => setting.TenantId == tenantId && setting.SettingKey == SettingKey);
    }

    private static PostgresSettingMutationLock CreateMutationLock(ExploreDbContext context) =>
        new(context, new EfCoreUnitOfWork(context));

    private async Task<T> RunFirstThenSecondAsync<T>(
        ISettingMutationLock firstLock,
        ExploreDbContext secondContext,
        Func<CancellationToken, Task> firstMutation,
        Func<Task<T>> secondMutation)
    {
        int secondBackendPid = await GetBackendPidAsync(secondContext);
        var firstMutated = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseFirst = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        Task<bool> firstTask = firstLock.ExecuteAsync(
            SettingKey,
            async token =>
            {
                await firstMutation(token);
                firstMutated.SetResult();
                await releaseFirst.Task.WaitAsync(token);
                return true;
            });

        await firstMutated.Task;
        Task<T> secondTask = secondMutation();
        await WaitUntilAdvisoryLockIsWaitingAsync(secondBackendPid, secondTask);
        await Assert.That(secondTask.IsCompleted).IsFalse();
        releaseFirst.SetResult();
        await firstTask;
        return await secondTask;
    }

    private static async Task<int> GetBackendPidAsync(ExploreDbContext context)
    {
        await context.Database.OpenConnectionAsync();
        await using var command = context.Database.GetDbConnection().CreateCommand();
        command.CommandText = "SELECT pg_backend_pid()";
        object? result = await command.ExecuteScalarAsync();
        return Convert.ToInt32(result, System.Globalization.CultureInfo.InvariantCulture);
    }

    private async Task WaitUntilAdvisoryLockIsWaitingAsync(int backendPid, Task secondTask)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        await using var observer = new NpgsqlConnection(fixture.ConnectionString);
        await observer.OpenAsync(timeout.Token);

        while (!timeout.IsCancellationRequested)
        {
            if (secondTask.IsCompleted)
            {
                throw new InvalidOperationException(
                    "The competing setting mutation completed before waiting on the advisory lock.");
            }

            await using var command = observer.CreateCommand();
            command.CommandText = """
                SELECT EXISTS (
                    SELECT 1
                    FROM pg_locks
                    WHERE pid = @pid
                      AND locktype = 'advisory'
                      AND NOT granted)
                """;
            command.Parameters.AddWithValue("pid", backendPid);
            if (await command.ExecuteScalarAsync(timeout.Token) is true)
            {
                return;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(20), timeout.Token);
        }

        throw new TimeoutException(
            $"Backend {backendPid} did not report waiting on an advisory lock within 10 seconds.");
    }

    private static async Task LockSystemSettingAsync(
        ISystemSettingRepository repository,
        CancellationToken cancellationToken)
    {
        await repository.UpsertAsync(new SystemSetting
        {
            SettingKey = SettingKey,
            Value = "\"system\"",
            ValueType = SettingValueType.String,
            IsLocked = true,
            Description = "SMTP host",
            Category = "Email",
            DisplayOrder = 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        }, cancellationToken);
    }

    private Task<BaseCommandResponse<Guid>> ExecuteTenantMutationAsync(
        TenantMutation mutation,
        Guid tenantId,
        ITenantSettingRepository tenantRepository,
        ISystemSettingRepository systemRepository,
        ISettingMutationLock mutationLock)
        => mutationLock.ExecuteAsync(
            SettingKey,
            async cancellationToken =>
            {
                if (await systemRepository.IsLocked(SettingKey, cancellationToken))
                {
                    return new BaseCommandResponse<Guid>
                    {
                        Id = tenantId,
                        Success = false,
                        FailureCode = "setting_system_locked"
                    };
                }

                switch (mutation)
                {
                    case TenantMutation.Set:
                        await tenantRepository.SetValueAsync(
                            tenantId, SettingKey, "\"changed\"", cancellationToken);
                        break;
                    case TenantMutation.Lock:
                        await tenantRepository.LockAsync(tenantId, SettingKey, _actorId, cancellationToken);
                        break;
                    case TenantMutation.Unlock:
                        await tenantRepository.UnlockAsync(tenantId, SettingKey, _actorId, cancellationToken);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(mutation), mutation, null);
                }

                return new BaseCommandResponse<Guid> { Id = tenantId, Success = true };
            },
            CancellationToken.None);

    public enum TenantMutation
    {
        Set,
        Lock,
        Unlock
    }
}
