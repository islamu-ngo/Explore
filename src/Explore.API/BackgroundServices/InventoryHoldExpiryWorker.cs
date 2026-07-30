// ABOUTME: Hosted worker that conditionally expires overdue registration inventory holds.
// ABOUTME: Uses a fresh service scope per recovery order so pooled DbContexts never retain stale tracked state.

using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Services;

namespace Explore.API.BackgroundServices;

public sealed class InventoryHoldExpiryWorker(IServiceProvider serviceProvider, ILogger<InventoryHoldExpiryWorker> logger) : BackgroundService
{
    private static readonly TimeSpan PollingInterval = TimeSpan.FromMinutes(1);
    private const int BatchSize = 100;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(PollingInterval);
        do
        {
            try
            {
                await RunOnceAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Inventory-hold expiry batch failed");
            }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    internal async Task RunOnceAsync(CancellationToken cancellationToken)
    {
        IReadOnlyDictionary<(Guid TenantId, Guid OrderId), IReadOnlyList<Guid>> dueHoldIdsByOrder;
        IReadOnlyList<RegistrationHoldExpiryRecoveryTarget> recoveryTargets;
        DateTime utcNow = DateTime.UtcNow;
        await using (AsyncServiceScope discoveryScope = serviceProvider.CreateAsyncScope())
        {
            var repository = discoveryScope.ServiceProvider.GetRequiredService<IRegistrationInventoryRepository>();
            dueHoldIdsByOrder = (await repository.GetExpiredActiveHoldsAsync(utcNow, BatchSize, cancellationToken))
                .GroupBy(hold => (hold.TenantId, hold.RegistrationOrderId))
                .ToDictionary(
                    group => (TenantId: group.Key.TenantId, OrderId: group.Key.RegistrationOrderId),
                    group => (IReadOnlyList<Guid>)group.Select(hold => hold.Id).ToArray());
            recoveryTargets = await repository.GetHoldExpiryRecoveryTargetsAsync(BatchSize, cancellationToken);
        }

        var recoveryOrders = new Dictionary<(Guid TenantId, Guid OrderId), IReadOnlyList<Guid>>(dueHoldIdsByOrder);
        foreach (RegistrationHoldExpiryRecoveryTarget target in recoveryTargets)
        {
            recoveryOrders.TryAdd((target.TenantId, target.RegistrationOrderId), []);
        }

        foreach (((Guid tenantId, Guid orderId), IReadOnlyList<Guid> holdIds) in recoveryOrders)
        {
            await using AsyncServiceScope itemScope = serviceProvider.CreateAsyncScope();
            var repository = itemScope.ServiceProvider.GetRequiredService<IRegistrationInventoryRepository>();
            bool holdExpired = false;
            foreach (Guid holdId in holdIds)
            {
                holdExpired |= await repository.TryExpireDueHoldAsync(holdId, utcNow, cancellationToken);
            }

            if (holdExpired || recoveryTargets.Any(target => target.TenantId == tenantId && target.RegistrationOrderId == orderId))
            {
                var lifecycle = itemScope.ServiceProvider.GetRequiredService<IRegistrationOrderLifecycleService>();
                await lifecycle.RecoverExpiredHoldAsync(orderId, tenantId, cancellationToken);
            }
        }
    }
}
