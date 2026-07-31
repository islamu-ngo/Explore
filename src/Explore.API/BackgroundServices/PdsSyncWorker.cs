// ABOUTME: Claims due ATProto PDS outbox rows and runs each through the fenced delivery processor.
// ABOUTME: Uses bounded parallelism, per-claim scopes, expiring leases, and cancellation-safe polling.

using Explore.Application.Contracts.Persistence;
using Explore.Application.Features.Federation.Atproto.Services;
using Microsoft.Extensions.Options;

namespace Explore.API.BackgroundServices;

public sealed class PdsSyncWorker(
    IServiceScopeFactory scopeFactory,
    IOptions<PdsSyncWorkerOptions> options,
    TimeProvider timeProvider,
    ILogger<PdsSyncWorker> logger) : BackgroundService
{
    private readonly PdsSyncWorkerOptions _options = options.Value;
    private readonly string _leaseOwner = BuildLeaseOwner();
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            logger.LogInformation("ATProto PDS sync worker is disabled");
            return;
        }

        logger.LogInformation(
            "ATProto PDS sync worker started with batch size {BatchSize} and concurrency {MaxConcurrency}",
            _options.BatchSize,
            _options.MaxConcurrency);

        while (!stoppingToken.IsCancellationRequested)
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
                logger.LogError(exception, "ATProto PDS sync polling failed");
            }

            try
            {
                await Task.Delay(
                    TimeSpan.FromSeconds(_options.PollingIntervalSeconds),
                    timeProvider,
                    stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }

        logger.LogInformation("ATProto PDS sync worker stopped");
    }

    internal async Task RunOnceAsync(CancellationToken cancellationToken)
    {
        IReadOnlyList<PdsSyncClaim> claims;
        using (IServiceScope claimScope = scopeFactory.CreateScope())
        {
            var repository = claimScope.ServiceProvider.GetRequiredService<IPdsSyncOutboxRepository>();
            claims = await repository.ClaimDueAsync(
                _options.BatchSize,
                _leaseOwner,
                timeProvider.GetUtcNow().UtcDateTime,
                TimeSpan.FromSeconds(_options.LeaseDurationSeconds),
                cancellationToken);
        }

        await Parallel.ForEachAsync(
            claims,
            new ParallelOptions
            {
                CancellationToken = cancellationToken,
                MaxDegreeOfParallelism = _options.MaxConcurrency
            },
            async (claim, token) =>
            {
                try
                {
                    using IServiceScope scope = scopeFactory.CreateScope();
                    var processor = scope.ServiceProvider.GetRequiredService<AtprotoPdsDeliveryProcessor>();
                    AtprotoPdsClaimResult result = await processor.ProcessAsync(
                        claim,
                        TimeSpan.FromSeconds(_options.LeaseDurationSeconds),
                        token);
                    if (result.Outcome == AtprotoPdsClaimOutcome.DeliveryFailed)
                    {
                        logger.LogWarning(
                            "ATProto PDS outbox {OutboxId} failed with {FailureCode}; disposition {FailureDisposition}",
                            claim.OutboxId,
                            result.FailureCode,
                            result.FailureDisposition);
                    }
                    else
                    {
                        logger.LogInformation(
                            "ATProto PDS outbox {OutboxId} completed worker pass with {Outcome}",
                            claim.OutboxId,
                            result.Outcome);
                    }
                }
                catch (OperationCanceledException) when (token.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception exception)
                {
                    logger.LogError(
                        exception,
                        "ATProto PDS outbox {OutboxId} worker pass failed",
                        claim.OutboxId);
                }
            });
    }

    private static string BuildLeaseOwner()
    {
        var value = $"pds-{Environment.MachineName}-{Environment.ProcessId}-{Guid.CreateVersion7():N}";
        return value.Length <= 200 ? value : value[..200];
    }
}
