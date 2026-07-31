// ABOUTME: Unit-style tests for the API-hosted registration inventory-hold expiry cycle.
// ABOUTME: Proves discovery and every conditional expiry/recovery order use separate dependency-injection scopes.

using Explore.API.BackgroundServices;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Services;
using Explore.Application.DTOs.RegistrationOrders;
using Explore.Domain;
using Explore.Domain.Enums;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using TUnit.Core;

namespace ApiIntegrationTests.Features;

public sealed class InventoryHoldExpiryWorkerTests
{
    [Test]
    public async Task RunOnceAsync_WithTwoDueHolds_UsesFreshScopeForEachConditionalExpiry()
    {
        DateTime createdAt = DateTime.UtcNow.AddMinutes(-2);
        RegistrationInventoryHold first = CreateHold(createdAt);
        RegistrationInventoryHold second = CreateHold(createdAt);
        var itemRepositories = new List<IRegistrationInventoryRepository>();
        var itemLifecycles = new List<IRegistrationOrderLifecycleService>();
        var itemTenantAccessors = new List<ITenantContextAccessor>();
        int repositoryCount = 0;
        var services = new ServiceCollection();
        services.AddScoped<ITenantContextAccessor>(_ =>
        {
            var tenantAccessor = Substitute.For<ITenantContextAccessor>();
            itemTenantAccessors.Add(tenantAccessor);
            return tenantAccessor;
        });
        services.AddScoped<IRegistrationInventoryRepository>(_ =>
        {
            var repository = Substitute.For<IRegistrationInventoryRepository>();
            if (Interlocked.Increment(ref repositoryCount) == 1)
            {
                repository.GetExpiredActiveHoldsAsync(
                        Arg.Any<DateTime>(),
                        Arg.Any<int>(),
                        Arg.Any<CancellationToken>())
                    .Returns([first, second]);
                repository.GetHoldExpiryRecoveryTargetsAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
                    .Returns([]);
            }
            else
            {
                itemRepositories.Add(repository);
                repository.TryExpireDueHoldAsync(Arg.Any<Guid>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
                    .Returns(true);
                repository.TryExpireDueHoldAsync(
                        Arg.Any<Guid>(),
                        Arg.Any<DateTime>(),
                        Arg.Any<CancellationToken>())
                    .Returns(true);
            }

            return repository;
        });
        services.AddScoped<IRegistrationOrderLifecycleService>(_ =>
        {
            var lifecycle = Substitute.For<IRegistrationOrderLifecycleService>();
            lifecycle.RecoverExpiredHoldAsync(
                    Arg.Any<Guid>(),
                    Arg.Any<Guid>(),
                    Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(new RegistrationOrderLifecycleResponseDto()));
            itemLifecycles.Add(lifecycle);
            return lifecycle;
        });

        await using ServiceProvider provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true
        });
        var worker = new InventoryHoldExpiryWorker(provider, NullLogger<InventoryHoldExpiryWorker>.Instance);

        await worker.RunOnceAsync(CancellationToken.None);

        await Assert.That(repositoryCount).IsEqualTo(3);
        await Assert.That(itemRepositories).Count().IsEqualTo(2);
        await Assert.That(itemLifecycles).Count().IsEqualTo(2);
        await Assert.That(itemTenantAccessors).Count().IsEqualTo(2);
        await itemRepositories[0].Received(1).TryExpireDueHoldAsync(
            first.Id,
            Arg.Any<DateTime>(),
            CancellationToken.None);
        await itemRepositories[1].Received(1).TryExpireDueHoldAsync(
            second.Id,
            Arg.Any<DateTime>(),
            CancellationToken.None);
        await itemLifecycles[0].Received(1).RecoverExpiredHoldAsync(
            first.RegistrationOrderId,
            first.TenantId,
            CancellationToken.None);
        await itemLifecycles[1].Received(1).RecoverExpiredHoldAsync(
            second.RegistrationOrderId,
            second.TenantId,
            CancellationToken.None);
        itemTenantAccessors[0].Received(1).SetTenant(first.TenantId);
        itemTenantAccessors[1].Received(1).SetTenant(second.TenantId);
        itemTenantAccessors[0].Received(1).Clear();
        itemTenantAccessors[1].Received(1).Clear();
    }

    [Test]
    public async Task RunOnceAsync_WithTwoDueHoldsForOneOrder_ExpiresBothAndRecoversOnceInOneFreshScope()
    {
        DateTime createdAt = DateTime.UtcNow.AddMinutes(-2);
        Guid tenantId = Guid.CreateVersion7();
        Guid orderId = Guid.CreateVersion7();
        RegistrationInventoryHold first = CreateHold(createdAt, tenantId, orderId);
        RegistrationInventoryHold second = CreateHold(createdAt, tenantId, orderId);
        var itemRepositories = new List<IRegistrationInventoryRepository>();
        var itemLifecycles = new List<IRegistrationOrderLifecycleService>();
        int repositoryCount = 0;
        var services = new ServiceCollection();
        services.AddScoped<ITenantContextAccessor>(_ => Substitute.For<ITenantContextAccessor>());
        services.AddScoped<IRegistrationInventoryRepository>(_ =>
        {
            var repository = Substitute.For<IRegistrationInventoryRepository>();
            if (Interlocked.Increment(ref repositoryCount) == 1)
            {
                repository.GetExpiredActiveHoldsAsync(Arg.Any<DateTime>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
                    .Returns([first, second]);
                repository.GetHoldExpiryRecoveryTargetsAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
                    .Returns([]);
            }
            else
            {
                itemRepositories.Add(repository);
                repository.TryExpireDueHoldAsync(Arg.Any<Guid>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
                    .Returns(true);
                repository.TryExpireDueHoldAsync(
                        Arg.Any<Guid>(),
                        Arg.Any<DateTime>(),
                        Arg.Any<CancellationToken>())
                    .Returns(true);
            }

            return repository;
        });
        services.AddScoped<IRegistrationOrderLifecycleService>(_ =>
        {
            var lifecycle = Substitute.For<IRegistrationOrderLifecycleService>();
            lifecycle.RecoverExpiredHoldAsync(orderId, tenantId, CancellationToken.None)
                .Returns(Task.FromResult(new RegistrationOrderLifecycleResponseDto()));
            itemLifecycles.Add(lifecycle);
            return lifecycle;
        });

        await using ServiceProvider provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true
        });
        var worker = new InventoryHoldExpiryWorker(provider, NullLogger<InventoryHoldExpiryWorker>.Instance);

        await worker.RunOnceAsync(CancellationToken.None);

        await Assert.That(repositoryCount).IsEqualTo(2);
        await Assert.That(itemRepositories).HasSingleItem();
        await Assert.That(itemLifecycles).HasSingleItem();
        await itemRepositories.Single().Received(1).TryExpireDueHoldAsync(first.Id, Arg.Any<DateTime>(), CancellationToken.None);
        await itemRepositories.Single().Received(1).TryExpireDueHoldAsync(second.Id, Arg.Any<DateTime>(), CancellationToken.None);
        await itemLifecycles.Single().Received(1).RecoverExpiredHoldAsync(orderId, tenantId, CancellationToken.None);
    }

    [Test]
    public async Task RunOnceAsync_WithPersistedReconciliationTarget_RecoversAfterAnInterruptedPriorExpiry()
    {
        Guid tenantId = Guid.CreateVersion7();
        Guid orderId = Guid.CreateVersion7();
        var itemLifecycles = new List<IRegistrationOrderLifecycleService>();
        int repositoryCount = 0;
        var services = new ServiceCollection();
        services.AddScoped<ITenantContextAccessor>(_ => Substitute.For<ITenantContextAccessor>());
        services.AddScoped<IRegistrationInventoryRepository>(_ =>
        {
            var repository = Substitute.For<IRegistrationInventoryRepository>();
            if (Interlocked.Increment(ref repositoryCount) == 1)
            {
                repository.GetExpiredActiveHoldsAsync(Arg.Any<DateTime>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
                    .Returns([]);
                repository.GetHoldExpiryRecoveryTargetsAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
                    .Returns([new RegistrationHoldExpiryRecoveryTarget(tenantId, orderId)]);
            }

            return repository;
        });
        services.AddScoped<IRegistrationOrderLifecycleService>(_ =>
        {
            var lifecycle = Substitute.For<IRegistrationOrderLifecycleService>();
            lifecycle.RecoverExpiredHoldAsync(orderId, tenantId, CancellationToken.None)
                .Returns(Task.FromResult(new RegistrationOrderLifecycleResponseDto()));
            itemLifecycles.Add(lifecycle);
            return lifecycle;
        });

        await using ServiceProvider provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true
        });
        var worker = new InventoryHoldExpiryWorker(provider, NullLogger<InventoryHoldExpiryWorker>.Instance);

        await worker.RunOnceAsync(CancellationToken.None);

        await Assert.That(repositoryCount).IsEqualTo(2);
        await Assert.That(itemLifecycles).HasSingleItem();
        await itemLifecycles.Single().Received(1).RecoverExpiredHoldAsync(orderId, tenantId, CancellationToken.None);
    }

    private static RegistrationInventoryHold CreateHold(DateTime createdAt, Guid? tenantId = null, Guid? orderId = null) => RegistrationInventoryHold.Create(
        Guid.CreateVersion7(),
        orderId ?? Guid.CreateVersion7(),
        Guid.CreateVersion7(),
        Guid.CreateVersion7(),
        tenantId ?? Guid.CreateVersion7(),
        quantity: 1,
        createdAt,
        createdAt.AddMinutes(1));
}
