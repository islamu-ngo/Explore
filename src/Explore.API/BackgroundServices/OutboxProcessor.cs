// ABOUTME: Background processor that polls and dispatches generic outbox messages for reliable side-effect delivery.
// ABOUTME: Mirrors PdsSyncWorker pattern: poll loop, optimistic lock, dispatch via IOutboxMessageDispatcher, retry with backoff.

using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Domain;
using Explore.Infrastructure;
using Microsoft.Extensions.Options;

namespace Explore.API.BackgroundServices;

/// <summary>
/// Background worker that polls <see cref="OutboxMessage"/> entries and dispatches them
/// via <see cref="IOutboxMessageDispatcher"/>. Implements at-least-once delivery with
/// optimistic locking, exponential backoff, and dead-lettering.
/// </summary>
public sealed class OutboxProcessor(
    IServiceProvider serviceProvider,
    IOptions<OutboxProcessorSettings> settings,
    ILogger<OutboxProcessor> logger) : BackgroundService
{
    private readonly OutboxProcessorSettings _settings = settings.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_settings.Enabled)
        {
            logger.LogInformation("Outbox processor is disabled");
            return;
        }

        logger.LogInformation(
            "Outbox processor starting with {Interval}s polling interval, batch size {BatchSize}",
            _settings.PollingIntervalSeconds, _settings.BatchSize);

        // Initial delay to let the application fully start
        await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessOutboxBatchAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in outbox processor loop");
            }

            try
            {
                await Task.Delay(TimeSpan.FromSeconds(_settings.PollingIntervalSeconds), stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }

        logger.LogInformation("Outbox processor stopped");
    }

    private async Task ProcessOutboxBatchAsync(CancellationToken stoppingToken)
    {
        await using var scope = serviceProvider.CreateAsyncScope();

        var outboxRepository = scope.ServiceProvider.GetRequiredService<IOutboxRepository>();
        var dispatcher = scope.ServiceProvider.GetRequiredService<IOutboxMessageDispatcher>();

        var pendingMessages = await outboxRepository.GetPendingBatch(_settings.BatchSize, stoppingToken);

        if (pendingMessages.Count == 0)
        {
            if (_settings.VerboseLogging)
            {
                logger.LogDebug("No pending outbox messages");
            }
            return;
        }

        logger.LogInformation("Processing {Count} outbox messages", pendingMessages.Count);

        foreach (var message in pendingMessages)
        {
            if (stoppingToken.IsCancellationRequested)
                break;

            await ProcessSingleMessageAsync(message, outboxRepository, dispatcher, stoppingToken);
        }
    }

    private async Task ProcessSingleMessageAsync(
        OutboxMessage message,
        IOutboxRepository outboxRepository,
        IOutboxMessageDispatcher dispatcher,
        CancellationToken stoppingToken)
    {
        try
        {
            // Optimistic lock — another processor may have claimed this message
            var locked = await outboxRepository.TryMarkAsProcessing(message.Id, stoppingToken);
            if (!locked)
            {
                if (_settings.VerboseLogging)
                {
                    logger.LogDebug("Message {Id} already being processed by another worker", message.Id);
                }
                return;
            }

            if (_settings.VerboseLogging)
            {
                logger.LogDebug(
                    "Dispatching message {Id}: {EventType} for {AggregateType}/{AggregateId}",
                    message.Id, message.EventType, message.AggregateType, message.AggregateId);
            }

            // Dispatch to consumer — idempotent consumers required (at-least-once delivery)
            await dispatcher.DispatchAsync(message, stoppingToken);

            await outboxRepository.MarkAsCompleted(message.Id, stoppingToken);

            logger.LogInformation(
                "Successfully dispatched message {Id}: {EventType} for {AggregateType}/{AggregateId}",
                message.Id, message.EventType, message.AggregateType, message.AggregateId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Exception dispatching message {Id}", message.Id);

            var retryDelay = _settings.CalculateRetryDelay(message.RetryCount);

            await outboxRepository.MarkAsFailed(
                message.Id,
                ex.Message,
                isRetryable: true,
                retryDelay,
                _settings.MaxRetryCount,
                stoppingToken);

            if (message.RetryCount + 1 < _settings.MaxRetryCount)
            {
                logger.LogWarning(
                    "Message {Id} failed (retry {Retry}/{Max}): {Error}. Next retry in {Delay}s",
                    message.Id, message.RetryCount + 1, _settings.MaxRetryCount, ex.Message, retryDelay);
            }
            else
            {
                logger.LogError(
                    "Message {Id} dead-lettered after {Retries} attempts: {Error}",
                    message.Id, message.RetryCount + 1, ex.Message);
            }
        }
    }
}
