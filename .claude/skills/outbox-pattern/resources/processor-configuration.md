ABOUTME: OutboxProcessor background service configuration and retry strategy.
ABOUTME: Covers settings, exponential backoff, dead-letter, and monitoring.

# OutboxProcessor Configuration

## Settings (Section: `OutboxProcessor`)

| Key | Type | Default | Description |
|-----|------|---------|-------------|
| Enabled | bool | true | Master switch |
| PollingIntervalSeconds | int | 5 | Poll frequency |
| BatchSize | int | 100 | Messages per poll cycle |
| MaxRetryCount | int | 5 | Retries before dead-letter |
| InitialRetryDelaySeconds | int | 1 | First retry delay |
| MaxRetryDelaySeconds | int | 3600 | Backoff ceiling (1 hour) |
| VerboseLogging | bool | false | Log each message processed |

## Retry Strategy

Exponential backoff with ceiling:

```
delay = min(InitialRetryDelaySeconds × 2^retryCount, MaxRetryDelaySeconds)
```

Example with defaults (Initial=1s, Max=3600s):
- Retry 0: 1s
- Retry 1: 2s
- Retry 2: 4s
- Retry 3: 8s
- Retry 4: 16s (then dead-letter if MaxRetryCount=5)

## Processing Lifecycle

1. `PeriodicTimer` fires every `PollingIntervalSeconds`.
2. Fetch batch of Pending messages where `NextRetryAt` is null or past.
3. For each message, call `TryMarkAsProcessing(id)` — returns false if another instance claimed it.
4. Call `IOutboxMessageDispatcher.DispatchAsync(message)`.
5. On success: `MarkAsCompleted(id)` — sets `ProcessedAt`.
6. On failure: `MarkAsFailed(id, error, isRetryable, retryDelay, maxRetries)`.
7. After `MaxRetries` exhausted: status → `DeadLettered`, `DeadLetteredAt` set.

## Dead-Letter Handling

Dead-lettered messages remain in the database indefinitely. They are queryable via `GetFailedEntries(limit)` which returns both Failed and DeadLettered statuses. No automatic cleanup — operational decision.

Completed messages can be cleaned up via `DeleteCompletedOlderThan(cutoff)`.

## DI Registration

| Layer | Registration |
|-------|-------------|
| Infrastructure | `OutboxProcessorSettings` (config bind), `LoggingOutboxMessageDispatcher` (default no-op) |
| Persistence | `OutboxRepository` → `IOutboxRepository` |
| API | `OutboxProcessor` as `IHostedService` |

## Related

- `resources/entity-model.md` — message fields and status enum
- `resources/dispatcher-patterns.md` — implementing real dispatchers
