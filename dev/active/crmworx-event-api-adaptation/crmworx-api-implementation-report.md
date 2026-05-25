<!-- ABOUTME: Implementation-level CRMWorx queue-first email/RabbitMQ report for ISLAMU Event planning. -->
<!-- ABOUTME: Complements the conceptual CRMWorx benefits report with exact classes, methods, schema, and translation notes. -->

# CRMWorx API Implementation Report: Queue-First Email, RabbitMQ, Outbox, SMTP, and Tests

> Companion to [`dev/active/crmworx-api-report.md`](crmworx-api-report.md).
>
> Scope: source-level implementation details from `/home/amir/Oppworx/Github/crmworx-api`, focused on what ISLAMU Event can implement or adapt. This report intentionally avoids ISLAMU Event application-code inspection and translates CRMWorx Java/Spring/JPA concepts into Event-friendly .NET/Clean Architecture guidance.

---

## 1. Executive Implementation Takeaway

CRMWorx's highest-value implementation pattern is not simply "use RabbitMQ for emails." It is a full queue-first state machine:

1. Persist a durable `email_outbox` intent row with recipient/body/provider/retry metadata.
2. A scheduled publish worker claims pending rows using a conditional database update.
3. The worker creates a pointer-only `EmailDispatchRequestedEvent` containing IDs and correlation metadata, not email bodies or secrets.
4. A RabbitMQ publisher sends the pointer event with mandatory routing and correlated publisher confirms.
5. Only after broker ack/no-return does CRMWorx mark the row `published`.
6. A manual-ack RabbitMQ consumer re-enters tenant context, loads the DB row, idempotently claims a receipt row, and dispatches SMTP.
7. SMTP success, retry, permanent failure, and unknown outcome are written back into database-owned delivery state.
8. DLQ replay validates DB truth before requeueing and parks invalid/already-sent events for operators.

For ISLAMU Event, the concrete translation is: implement a specialized Notification/Email Dispatch outbox beside the generic `OutboxMessage`, not a generic fire-and-forget broker publisher. Event should keep the business truth in PostgreSQL/EF Core, use RabbitMQ as transport, and treat publisher confirms, receipts, attempts, tenant validation, and DLQ replay as first-class implementation requirements.

---

## 2. Exact CRMWorx Files That Define The Queue-First Email Implementation

### 2.1 Application layer

| File | Role |
|---|---|
| `crmworx-application/src/main/java/com/oppworx/crmworx/application/features/email/publish/EmailOutboxPublishService.java` | Scheduled publisher orchestration: finds rows, claims publish, builds pointer event, calls publisher port, records publish success/retry. |
| `crmworx-application/src/main/java/com/oppworx/crmworx/application/features/email/consume/EmailDispatchConsumeService.java` | Consumer orchestration: validates event and tenant, claims receipt, enforces pause/throttle/circuit controls, calls SMTP relay port, records delivery outcome. |
| `crmworx-application/src/main/java/com/oppworx/crmworx/application/dto/notification/EmailDispatchRequestedEvent.java` | Pointer-only RabbitMQ event contract. |
| `crmworx-application/src/main/java/com/oppworx/crmworx/application/features/email/consume/EmailDispatchDisposition.java` | Consumer result enum: `ACK` or `REJECT_TO_DLQ`. |
| `crmworx-application/src/main/java/com/oppworx/crmworx/application/contracts/infrastructure/EmailDispatchPublisherPort.java` | Port for RabbitMQ publisher adapter. |
| `crmworx-application/src/main/java/com/oppworx/crmworx/application/contracts/infrastructure/EmailOutboxRelayPort.java` | Port for actual email provider dispatch, currently SMTP. |
| `crmworx-application/src/main/java/com/oppworx/crmworx/application/contracts/infrastructure/runtime/EmailDispatchProcessingControlPort.java` | Port for runtime kill-switch, pause, tenant circuit, and deferred retry settings. |

### 2.2 Infrastructure layer

| File | Role |
|---|---|
| `crmworx-infrastructure/src/main/java/com/oppworx/crmworx/infrastructure/messaging/RabbitMQConfig.java` | Declares RabbitMQ connection factory, template, exchanges, queues, bindings, manual-ack listener factories. |
| `crmworx-infrastructure/src/main/java/com/oppworx/crmworx/infrastructure/messaging/RabbitMQProperties.java` | Configuration model for broker connection, exchanges, queues, routing keys, confirms/returns, consumers, DLQ replay. |
| `crmworx-infrastructure/src/main/java/com/oppworx/crmworx/infrastructure/messaging/RabbitMQEmailDispatchPublisher.java` | Publisher adapter with JSON serialization, mandatory routing, correlated confirms, return checks, metrics. |
| `crmworx-infrastructure/src/main/java/com/oppworx/crmworx/infrastructure/messaging/RabbitMQEmailDispatchListener.java` | Standard queue listener with manual ack/reject/nack behavior and tenant context re-entry. |
| `crmworx-infrastructure/src/main/java/com/oppworx/crmworx/infrastructure/messaging/RabbitMQEmailDispatchDeadLetterReplayListener.java` | DLQ replay/parking listener. Validates DB row before replay. |
| `crmworx-infrastructure/src/main/java/com/oppworx/crmworx/infrastructure/messaging/EmailOutboxPublishScheduler.java` | `@Scheduled` wrapper that invokes `EmailOutboxPublishService.publish(batchSize)` every fixed delay. |
| `crmworx-infrastructure/src/main/java/com/oppworx/crmworx/infrastructure/messaging/EmailOutboxPublishProperties.java` | Scheduler config: enabled, batch size, fixed delay. |
| `crmworx-infrastructure/src/main/java/com/oppworx/crmworx/infrastructure/messaging/InMemoryEmailDispatchProcessingControlAdapter.java` | Runtime control adapter for global pause, tenant pause, and tenant failure circuit state. |
| `crmworx-infrastructure/src/main/java/com/oppworx/crmworx/infrastructure/notification/SmtpEmailOutboxRelayClient.java` | SMTP email provider implementation using Spring `JavaMailSender`, MIME messages, tenant SMTP fallback, SSRF/private-host guardrails. |

### 2.3 Persistence and schema

| File | Role |
|---|---|
| `crmworx-domain/src/main/java/com/oppworx/crmworx/domain/entity/EmailOutbox.java` | Domain object representing durable email intent plus publish/delivery state. |
| `crmworx-domain/src/main/java/com/oppworx/crmworx/domain/entity/EmailDispatchReceipt.java` | Consumer idempotency/audit receipt keyed by tenant + event ID. |
| `crmworx-domain/src/main/java/com/oppworx/crmworx/domain/entity/EmailOutboxDeliveryAttempt.java` | Per-SMTP-attempt audit row. |
| `crmworx-persistence/src/main/java/com/oppworx/crmworx/persistence/repository/JpaEmailOutboxRepository.java` | Spring Data queries for pending publish rows and publish state updates. |
| `crmworx-persistence/src/main/java/com/oppworx/crmworx/persistence/adapter/EmailOutboxRepositoryAdapter.java` | Application repository adapter wrapping JPA repository and MapStruct mapper. |
| `crmworx-persistence/src/main/java/com/oppworx/crmworx/persistence/repository/JpaEmailDispatchReceiptRepository.java` | Native `INSERT ... ON CONFLICT DO NOTHING` receipt claim. |
| `crmworx-persistence/src/main/java/com/oppworx/crmworx/persistence/adapter/EmailDispatchReceiptRepositoryAdapter.java` | Receipt persistence adapter. |
| `crmworx-persistence/src/main/resources/db/migration/V3_51__add_email_outbox_routing_and_attempt_history.sql` | Adds provider routing fields and `email_outbox_delivery_attempts`. |
| `crmworx-persistence/src/main/resources/db/migration/V3_52__add_email_dispatch_publish_and_receipt_contracts.sql` | Adds publish lifecycle fields and `email_dispatch_receipts`. |

---

## 3. Libraries And Frameworks Used In CRMWorx

CRMWorx uses Java/Spring equivalents. Event should translate the implementation shape into .NET primitives rather than copy library APIs.

| CRMWorx library | Evidence | Purpose | Event translation |
|---|---|---|---|
| Spring AMQP / RabbitMQ | `crmworx-infrastructure/pom.xml` includes `spring-boot-starter-amqp`; `RabbitMQConfig`, `RabbitMQEmailDispatchPublisher`, listeners | Broker topology, `RabbitTemplate`, `@RabbitListener`, manual ack, correlated confirms | `RabbitMQ.Client`, MassTransit, Wolverine, or a custom `BackgroundService` with `IModel`/confirm channels. Must preserve mandatory routing + publisher confirm semantics. |
| Spring Mail / Jakarta Mail | `spring-boot-starter-mail`; `SmtpEmailOutboxRelayClient` uses `JavaMailSender`, `MimeMessage`, `MimeMessageHelper` | SMTP dispatch and MIME composition | `MailKit`/`MimeKit` or existing Event SMTP abstraction. Preserve provider selection, timeout, correlation header, and fallback rules. |
| Spring Data JPA / Hibernate | JPA repositories and entities | SQL-backed state transitions | EF Core repositories returning entities, per Event rule. Use `ExecuteUpdateAsync` or raw SQL for conditional claim updates and `ON CONFLICT DO NOTHING`. |
| Flyway | root/persistence poms include Flyway | Versioned schema migrations | EF Core migrations or raw SQL migrations, but keep same additive schema discipline and indexes. |
| PostgreSQL | `postgresql` dependency and migrations | Durable outbox, receipt, attempts, RLS | PostgreSQL remains directly applicable; Event already uses PostgreSQL patterns. |
| MapStruct | persistence adapters map JPA entities to domain objects | Persistence mapping | Event should not need MapStruct; EF entities/domain mapping depends on Event's existing persistence style. Critical rule: Event repositories return entities, not DTOs. |
| Micrometer | `micrometer-core`, Prometheus registry | Business metrics and timings | Event translation: OpenTelemetry `Meter`, Prometheus exporter, structured logs to Loki. |
| Testcontainers | poms include Testcontainers | Rabbit/Postgres/Mail integration tests | .NET Testcontainers for RabbitMQ/PostgreSQL/Mailpit where needed. |

---

## 4. Database Schema: Exact CRMWorx Tables And Columns To Recreate Conceptually

### 4.1 Existing `email_outbox` is extended with routing and delivery fields

Migration: `V3_51__add_email_outbox_routing_and_attempt_history.sql`.

CRMWorx adds these columns to `email_outbox`:

```sql
ALTER TABLE email_outbox
    ADD COLUMN IF NOT EXISTS delivery_class VARCHAR(64),
    ADD COLUMN IF NOT EXISTS fallback_policy VARCHAR(64),
    ADD COLUMN IF NOT EXISTS requested_provider VARCHAR(64),
    ADD COLUMN IF NOT EXISTS resolved_provider VARCHAR(64),
    ADD COLUMN IF NOT EXISTS final_provider VARCHAR(64),
    ADD COLUMN IF NOT EXISTS fallback_used BOOLEAN NOT NULL DEFAULT false,
    ADD COLUMN IF NOT EXISTS fallback_reason VARCHAR(500),
    ADD COLUMN IF NOT EXISTS delivery_status VARCHAR(64);
```

It adds an operational lookup index:

```sql
CREATE INDEX IF NOT EXISTS idx_email_outbox_delivery_routing
    ON email_outbox (tenant_id, delivery_class, delivery_status, created_at);
```

**Event implementation mapping:** if Event creates `EmailDispatchOutbox`, include provider routing/fallback summary fields from the beginning. They make incident diagnosis possible without parsing logs: requested provider, resolved provider, final provider, fallback used, fallback reason, delivery class, delivery status.

### 4.2 Per-attempt audit table

Migration: `V3_51__add_email_outbox_routing_and_attempt_history.sql`.

```sql
CREATE TABLE IF NOT EXISTS email_outbox_delivery_attempts (
    id UUID PRIMARY KEY,
    email_outbox_id UUID NOT NULL REFERENCES email_outbox(id) ON DELETE CASCADE,
    tenant_id UUID NOT NULL,
    attempt_number INTEGER NOT NULL,
    provider_type VARCHAR(64) NOT NULL,
    provider_profile_id UUID,
    started_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    completed_at TIMESTAMPTZ,
    outcome VARCHAR(64) NOT NULL,
    smtp_status_code VARCHAR(32),
    error_category VARCHAR(128),
    sanitized_error_message VARCHAR(1000),
    correlation_id VARCHAR(120),
    created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    created_by UUID,
    updated_at TIMESTAMPTZ,
    updated_by UUID,
    CONSTRAINT uq_email_outbox_delivery_attempt UNIQUE (email_outbox_id, attempt_number)
);
```

Indexes:

```sql
CREATE INDEX IF NOT EXISTS idx_email_outbox_delivery_attempts_outbox
    ON email_outbox_delivery_attempts (email_outbox_id, attempt_number);

CREATE INDEX IF NOT EXISTS idx_email_outbox_delivery_attempts_tenant_created
    ON email_outbox_delivery_attempts (tenant_id, created_at DESC);
```

**Event implementation mapping:** add a dedicated attempts table rather than stuffing attempt history into JSON. The uniqueness constraint `(email_outbox_id, attempt_number)` is important because it turns operational history into an ordered ledger.

### 4.3 Publish lifecycle columns

Migration: `V3_52__add_email_dispatch_publish_and_receipt_contracts.sql`.

```sql
ALTER TABLE email_outbox
    ADD COLUMN IF NOT EXISTS publish_event_id UUID,
    ADD COLUMN IF NOT EXISTS publish_status VARCHAR(64) NOT NULL DEFAULT 'pending',
    ADD COLUMN IF NOT EXISTS publish_claimed_at TIMESTAMPTZ,
    ADD COLUMN IF NOT EXISTS published_at TIMESTAMPTZ,
    ADD COLUMN IF NOT EXISTS publish_attempt_count INTEGER NOT NULL DEFAULT 0,
    ADD COLUMN IF NOT EXISTS publish_last_error TEXT,
    ADD COLUMN IF NOT EXISTS publish_next_retry_at TIMESTAMPTZ;
```

Indexes:

```sql
CREATE UNIQUE INDEX IF NOT EXISTS uq_email_outbox_tenant_publish_event
    ON email_outbox (tenant_id, publish_event_id)
    WHERE publish_event_id IS NOT NULL;

CREATE INDEX IF NOT EXISTS idx_email_outbox_publish_pending
    ON email_outbox (publish_status, publish_next_retry_at, created_at);

CREATE INDEX IF NOT EXISTS idx_email_outbox_tenant_publish_status
    ON email_outbox (tenant_id, publish_status, publish_next_retry_at);
```

**Event implementation mapping:** the unique `(tenant_id, publish_event_id)` index is a concrete anti-duplicate guarantee. For Event, use UUIDv7 `Guid` for `PublishEventId`, preserve tenant scoping, and index pending rows by status/retry time/created time.

### 4.4 Consumer receipt/idempotency table

Migration: `V3_52__add_email_dispatch_publish_and_receipt_contracts.sql`.

```sql
CREATE TABLE IF NOT EXISTS email_dispatch_receipts (
    id UUID PRIMARY KEY,
    tenant_id UUID NOT NULL,
    event_id UUID NOT NULL,
    email_outbox_id UUID NOT NULL REFERENCES email_outbox(id) ON DELETE CASCADE,
    status VARCHAR(64) NOT NULL,
    consumer_id VARCHAR(120),
    first_seen_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    processing_started_at TIMESTAMPTZ,
    completed_at TIMESTAMPTZ,
    failed_at TIMESTAMPTZ,
    failure_code VARCHAR(128),
    failure_message TEXT,
    smtp_message_id VARCHAR(255),
    created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    created_by UUID,
    updated_at TIMESTAMPTZ,
    updated_by UUID
);

ALTER TABLE email_dispatch_receipts
    ADD CONSTRAINT uq_email_dispatch_receipts_tenant_event
    UNIQUE (tenant_id, event_id);
```

Indexes:

```sql
CREATE INDEX IF NOT EXISTS idx_email_dispatch_receipts_tenant_outbox
    ON email_dispatch_receipts (tenant_id, email_outbox_id);

CREATE INDEX IF NOT EXISTS idx_email_dispatch_receipts_status
    ON email_dispatch_receipts (status, first_seen_at);
```

**Event implementation mapping:** this table is the consumer idempotency mechanism. Do not rely only on RabbitMQ delivery tags or broker redelivery. Event should atomically insert a receipt for `(TenantId, EventId)` and treat conflict as duplicate delivery.

### 4.5 Tenant row-level security in CRMWorx

Both migrations conditionally enable PostgreSQL RLS when role `crmworx_app` exists:

```sql
ALTER TABLE email_dispatch_receipts ENABLE ROW LEVEL SECURITY;
ALTER TABLE email_dispatch_receipts FORCE ROW LEVEL SECURITY;

CREATE POLICY tenant_isolation_email_dispatch_receipts ON email_dispatch_receipts
    AS PERMISSIVE
    FOR ALL
    TO crmworx_app
    USING (tenant_id = NULLIF(current_setting('app.current_tenant_id', true), '')::uuid)
    WITH CHECK (tenant_id = NULLIF(current_setting('app.current_tenant_id', true), '')::uuid);
```

**Event implementation mapping:** Event docs already emphasize tenant isolation through EF filters. If Event has database-level tenant context available for background workers, this RLS pattern is worth adapting; otherwise, every repository query/update must include tenant predicates and background consumers must explicitly set tenant context before touching rows.

---

## 5. Publish Worker: `EmailOutboxPublishService`

File: `crmworx-application/src/main/java/com/oppworx/crmworx/application/features/email/publish/EmailOutboxPublishService.java`.

### 5.1 Public entry point

```java
@Transactional
public int publish(int batchSize) {
    OffsetDateTime now = appClock.now();
    List<EmailOutbox> entries = outboxRepository.findPendingForPublish(batchSize, now);
    int published = 0;
    for (EmailOutbox entry : entries) {
        published += publishEntry(entry, now);
    }
    return published;
}
```

Important details:

- The method is transactional.
- It asks the repository for rows eligible before `now`.
- It returns a count of successful publishes.
- Failures inside an entry schedule retry and return `0`, so one failed row does not prevent later scheduler cycles.

**Event implementation mapping:** use an `EmailDispatchPublishWorker : BackgroundService` or hosted service with a scoped Application service. Inside each tick, call `PublishAsync(batchSize, cancellationToken)`. Keep the application service transactional per batch or per row; if EF transaction boundaries become too broad, prefer per-row transactions so broker waits do not hold locks unnecessarily.

### 5.2 Stable event ID assignment

```java
UUID eventId = entry.getPublishEventId() != null
        ? entry.getPublishEventId()
        : uuidGenerator.generate();
entry.setPublishEventId(eventId);
```

CRMWorx never creates a new message ID for each retry once `publish_event_id` exists. Retries reuse the same event identity.

**Event implementation mapping:** `PublishEventId` should be generated once and then reused across publisher retries. This is the ID that the consumer receipt table uses for idempotency.

### 5.3 Attempt count increment

```java
int attemptCount = entry.getPublishAttemptCount() != null
        ? entry.getPublishAttemptCount() + 1
        : 1;
entry.setPublishAttemptCount(attemptCount);
```

**Event implementation mapping:** store `PublishAttemptCount` independently from SMTP `RetryCount`. Broker publish retry and provider delivery retry are separate lifecycle stages.

### 5.4 Optimistic publish claim

```java
if (!outboxRepository.tryMarkPublishClaimed(entry.getId(), eventId, attemptCount, now)) {
    return 0;
}
```

Repository implementation:

```java
@Query("""
        UPDATE JpaEmailOutbox o
        SET o.publishEventId = :publishEventId,
            o.publishAttemptCount = :publishAttemptCount,
            o.publishStatus = 'claimed',
            o.publishClaimedAt = :claimedAt
        WHERE o.id = :id
          AND o.publishStatus IN ('pending', 'publish_retry_scheduled')
        """)
int markPublishClaimed(...);
```

Adapter returns `updated > 0` for `tryMarkPublishClaimed`.

**Event implementation mapping:** in EF Core, implement a repository method like:

```csharp
Task<bool> TryMarkPublishClaimedAsync(Guid id, Guid eventId, int attemptCount, DateTimeOffset claimedAt, CancellationToken ct);
```

Use `ExecuteUpdateAsync` or raw SQL so contention returns `false`, not an exception. The `WHERE` clause must only allow `Pending` and `PublishRetryScheduled` rows.

### 5.5 Pointer-only event construction

```java
EmailDispatchRequestedEvent event = new EmailDispatchRequestedEvent(
        SCHEMA_VERSION,
        EVENT_TYPE,
        eventId,
        entry.getTenantId(),
        entry.getId(),
        entry.getIdempotencyKey(),
        SOURCE_TYPE,
        DEFAULT_PRIORITY,
        entry.getCorrelationId(),
        null,
        now
);
```

Constants:

- `SCHEMA_VERSION = 1`
- `EVENT_TYPE = "email.dispatch.requested"`
- `SOURCE_TYPE = "email_outbox"`
- `DEFAULT_PRIORITY = "NORMAL"`

**Event implementation mapping:** define a .NET record such as:

```csharp
public sealed record EmailDispatchRequestedEvent(
    int SchemaVersion,
    string EventType,
    Guid EventId,
    Guid TenantId,
    Guid EmailOutboxId,
    string? IdempotencyKey,
    string SourceType,
    string Priority,
    string? CorrelationId,
    string? CausationId,
    DateTimeOffset? OccurredAt);
```

Do not include `To`, `Subject`, `HtmlBody`, `TextBody`, SMTP credentials, or tenant provider settings in RabbitMQ payloads.

### 5.6 Confirmed publish then DB success

```java
try {
    publisherPort.publish(event);
} catch (Exception brokerEx) {
    scheduleRetry(entry, eventId, attemptCount, brokerEx.getMessage(), now);
    return 0;
}

try {
    outboxRepository.markPublishSucceeded(entry.getId(), eventId, now);
    entry.setPublishStatus(EmailOutboxPublishStatus.PUBLISHED);
    entry.setPublishedAt(now);
    return 1;
} catch (Exception persistEx) {
    scheduleRetry(entry, eventId, attemptCount, persistEx.getMessage(), now);
    return 0;
}
```

Subtle implementation issue: if broker publish succeeds but marking DB success fails, CRMWorx schedules retry. Because the same `publish_event_id` is reused and the consumer is idempotent, duplicate publish is safe.

**Event implementation mapping:** this is exactly why Event needs stable event IDs and a receipt table. Without them, DB failure after broker success can cause duplicate emails.

### 5.7 Publish retry scheduling

```java
private void scheduleRetry(EmailOutbox entry, UUID eventId, int attemptCount,
                           String rawError, OffsetDateTime now) {
    String redactedError = redact(rawError);
    OffsetDateTime nextRetryAt = now.plusSeconds(RETRY_DELAY_SECONDS);
    entry.setPublishStatus(EmailOutboxPublishStatus.RETRY_SCHEDULED);
    entry.setPublishLastError(redactedError);
    entry.setPublishNextRetryAt(nextRetryAt);
    outboxRepository.markPublishRetryScheduled(
            entry.getId(), eventId, attemptCount, redactedError, nextRetryAt
    );
}
```

Current CRMWorx publish retry delay is a constant `2` seconds. Delivery retry uses exponential backoff separately.

**Event implementation mapping:** Event can improve this by using the existing outbox skill's exponential retry pattern for publish stage too, but keep publish retry separate from SMTP retry.

---

## 6. Publish Scheduler: `EmailOutboxPublishScheduler`

File: `crmworx-infrastructure/src/main/java/com/oppworx/crmworx/infrastructure/messaging/EmailOutboxPublishScheduler.java`.

```java
@Component
@ConditionalOnProperty(
        name = {
                "crmworx.messaging.rabbitmq.enabled",
                "crmworx.email.outbox-publish.enabled"
        },
        havingValue = "true",
        matchIfMissing = false
)
public class EmailOutboxPublishScheduler {
    @Scheduled(fixedDelayString = "${crmworx.email.outbox-publish.fixed-delay-ms:5000}")
    public void run() {
        try {
            int published = publishService.publish(properties.getBatchSize());
            if (published > 0) {
                log.info("Email outbox publish dispatched {} events", published);
            }
        } catch (Exception ex) {
            log.error("Email outbox publish cycle failed: {}", ex.getMessage(), ex);
        }
    }
}
```

Properties:

```java
@ConfigurationProperties(prefix = "crmworx.email.outbox-publish")
public class EmailOutboxPublishProperties {
    private boolean enabled = true;
    private int batchSize = 50;
    private long fixedDelayMs = 5000;
}
```

Runtime config from `application.yml`:

```yaml
crmworx:
  email:
    outbox-publish:
      enabled: ${CRMWORX_EMAIL_OUTBOX_PUBLISH_ENABLED:true}
      batch-size: ${CRMWORX_EMAIL_OUTBOX_PUBLISH_BATCH_SIZE:50}
      fixed-delay-ms: ${CRMWORX_EMAIL_OUTBOX_PUBLISH_FIXED_DELAY_MS:5000}
```

**Event implementation mapping:** use `IOptions<EmailOutboxPublishOptions>` and a hosted `BackgroundService` with `PeriodicTimer`. Keep the scheduler thin; it should not contain publish logic.

---

## 7. RabbitMQ Topology: `RabbitMQConfig`

File: `crmworx-infrastructure/src/main/java/com/oppworx/crmworx/infrastructure/messaging/RabbitMQConfig.java`.

### 7.1 Connection factory with publisher confirms/returns

```java
ConnectionFactory rabbitConnectionFactory(RabbitMQProperties properties) {
    CachingConnectionFactory connectionFactory = new CachingConnectionFactory();
    connectionFactory.setHost(properties.getHost());
    connectionFactory.setPort(properties.getPort());
    connectionFactory.setUsername(properties.getUsername());
    connectionFactory.setPassword(properties.getPassword());
    connectionFactory.setVirtualHost(properties.getVirtualHost());
    connectionFactory.setPublisherConfirmType(properties.isPublisherConfirms()
            ? CachingConnectionFactory.ConfirmType.CORRELATED
            : CachingConnectionFactory.ConfirmType.NONE);
    connectionFactory.setPublisherReturns(properties.isPublisherReturns());
    return connectionFactory;
}
```

Rabbit template:

```java
RabbitTemplate rabbitTemplate(ConnectionFactory rabbitConnectionFactory) {
    RabbitTemplate rabbitTemplate = new RabbitTemplate(rabbitConnectionFactory);
    rabbitTemplate.setMandatory(true);
    return rabbitTemplate;
}
```

**Event implementation mapping:** if using raw `RabbitMQ.Client`, enable publisher confirms (`IModel.ConfirmSelect`) and mandatory publishing (`BasicPublish(..., mandatory: true, ...)`) with a return handler. If using MassTransit, verify equivalent guarantees explicitly; do not assume publish completion equals broker route success.

### 7.2 Exchanges and queues

CRMWorx declares:

```java
TopicExchange emailDispatchExchange(properties.getEmailDispatchExchange(), true, false);
TopicExchange emailDispatchDeadLetterExchange(properties.getEmailDispatchExchange() + ".dlx", true, false);
```

Standard queue:

```java
QueueBuilder.durable(properties.getEmailDispatchQueue())
    .withArgument("x-dead-letter-exchange", properties.getEmailDispatchExchange() + ".dlx")
    .withArgument("x-dead-letter-routing-key", properties.getEmailDispatchDeadLetterRoutingKey())
    .build();
```

Priority queue is also declared with the same DLX:

```java
QueueBuilder.durable(properties.getEmailDispatchPriorityQueue())
    .withArgument("x-dead-letter-exchange", properties.getEmailDispatchExchange() + ".dlx")
    .withArgument("x-dead-letter-routing-key", properties.getEmailDispatchDeadLetterRoutingKey())
    .build();
```

DLQ and parking queue:

```java
QueueBuilder.durable(properties.getEmailDispatchDeadLetterQueue()).build();
QueueBuilder.durable(properties.getEmailDispatchParkingQueue()).build();
```

Bindings:

- Standard queue binds to `emailDispatchRoutingKey`.
- Priority queue binds to `emailDispatchPriorityRoutingKey`.
- DLQ binds to `emailDispatchDeadLetterRoutingKey` on the `.dlx` exchange.
- Parking queue binds to `emailDispatchParkingRoutingKey` on the main exchange.

**Verified caution:** CRMWorx declares a priority queue and routes high/critical messages to it, but the standard listener annotation only references `email-dispatch-queue`. Event should either create a consumer for both standard and priority queues or avoid priority routing until it is fully consumed and tested.

### 7.3 Manual ack listener factory

```java
factory.setAcknowledgeMode(AcknowledgeMode.MANUAL);
factory.setDefaultRequeueRejected(false);
factory.setPrefetchCount(prefetch);       // default 20
factory.setConcurrentConsumers(concurrent); // default 2
factory.setMaxConcurrentConsumers(Math.max(concurrent, maxConcurrent)); // default max 4
```

DLQ replay factory also uses manual ack with defaults prefetch `5`, concurrent `1`, max `1`.

**Event implementation mapping:** consumers must not auto-ack. Ack only after DB receipt and delivery state are safe. Reject poison messages without requeue. Nack unexpected transient worker failures with requeue.

### 7.4 RabbitMQ config keys

From `application.yml`:

```yaml
crmworx:
  messaging:
    rabbitmq:
      enabled: ${CRMWORX_RABBITMQ_ENABLED:false}
      host: ${CRMWORX_RABBITMQ_HOST:localhost}
      port: ${CRMWORX_RABBITMQ_PORT:5672}
      username: ${CRMWORX_RABBITMQ_USERNAME:guest}
      password: ${CRMWORX_RABBITMQ_PASSWORD:guest}
      virtual-host: ${CRMWORX_RABBITMQ_VHOST:/}
      publisher-confirms: ${CRMWORX_RABBITMQ_PUBLISHER_CONFIRMS:true}
      publisher-returns: ${CRMWORX_RABBITMQ_PUBLISHER_RETURNS:true}
      email-dispatch-publisher-enabled: ${CRMWORX_RABBITMQ_EMAIL_DISPATCH_PUBLISHER_ENABLED:false}
      email-dispatch-exchange: ${CRMWORX_RABBITMQ_EMAIL_DISPATCH_EXCHANGE:crmworx.email.dispatch.v1.exchange}
      email-dispatch-queue: ${CRMWORX_RABBITMQ_EMAIL_DISPATCH_QUEUE:crmworx.email.dispatch.standard.q}
      email-dispatch-priority-queue: ${CRMWORX_RABBITMQ_EMAIL_DISPATCH_PRIORITY_QUEUE:crmworx.email.dispatch.priority.q}
      email-dispatch-routing-key: ${CRMWORX_RABBITMQ_EMAIL_DISPATCH_ROUTING_KEY:email.dispatch.standard}
      email-dispatch-priority-routing-key: ${CRMWORX_RABBITMQ_EMAIL_DISPATCH_PRIORITY_ROUTING_KEY:email.dispatch.priority}
      email-dispatch-dead-letter-queue: ${CRMWORX_RABBITMQ_EMAIL_DISPATCH_DLQ:crmworx.email.dispatch.dlq}
      email-dispatch-dead-letter-routing-key: ${CRMWORX_RABBITMQ_EMAIL_DISPATCH_DLQ_ROUTING_KEY:email.dispatch.standard.dlq}
      email-dispatch-parking-queue: ${CRMWORX_RABBITMQ_EMAIL_DISPATCH_PARKING_QUEUE:crmworx.email.dispatch.parking.q}
      email-dispatch-parking-routing-key: ${CRMWORX_RABBITMQ_EMAIL_DISPATCH_PARKING_ROUTING_KEY:email.dispatch.parking}
      email-dispatch-publish-confirm-timeout-ms: ${CRMWORX_RABBITMQ_EMAIL_DISPATCH_CONFIRM_TIMEOUT_MS:10000}
      email-dispatch-consumer-enabled: ${CRMWORX_RABBITMQ_EMAIL_DISPATCH_CONSUMER_ENABLED:false}
      email-dispatch-prefetch: ${CRMWORX_RABBITMQ_EMAIL_DISPATCH_PREFETCH:20}
      email-dispatch-concurrent-consumers: ${CRMWORX_RABBITMQ_EMAIL_DISPATCH_CONCURRENT_CONSUMERS:2}
      email-dispatch-max-concurrent-consumers: ${CRMWORX_RABBITMQ_EMAIL_DISPATCH_MAX_CONCURRENT_CONSUMERS:4}
      email-dispatch-consumer-id: ${CRMWORX_RABBITMQ_EMAIL_DISPATCH_CONSUMER_ID:email-dispatch-consumer}
      email-dispatch-dlq-replay-enabled: ${CRMWORX_RABBITMQ_EMAIL_DISPATCH_DLQ_REPLAY_ENABLED:false}
```

**Event implementation mapping:** keep publisher and consumer feature flags separate. This permits safe rollout: create schema, enable publisher only, observe queue depth, enable consumer, enable DLQ replay last.

---

## 8. RabbitMQ Publisher: `RabbitMQEmailDispatchPublisher`

File: `crmworx-infrastructure/src/main/java/com/oppworx/crmworx/infrastructure/messaging/RabbitMQEmailDispatchPublisher.java`.

### 8.1 Conditional activation

```java
@ConditionalOnProperty(
        name = {
                "crmworx.messaging.rabbitmq.enabled",
                "crmworx.messaging.rabbitmq.email-dispatch-publisher-enabled"
        },
        havingValue = "true",
        matchIfMissing = false
)
@ConditionalOnBean(RabbitTemplate.class)
public class RabbitMQEmailDispatchPublisher implements EmailDispatchPublisherPort
```

**Event implementation mapping:** register the Rabbit publisher only when broker settings are complete and the feature flag is enabled. Fail visibly if the publish worker is enabled but no real publisher is registered.

### 8.2 Publish method exact control flow

```java
public void publish(EmailDispatchRequestedEvent event) {
    var sample = metrics.startTimer();
    try {
        validate(event);
        log.info("email_dispatch_publish_requested {}", logContext(event));
        Message message = toMessage(event);
        CorrelationData correlationData = new CorrelationData(event.eventId().toString());
        rabbitTemplate.send(exchange, routingKey, message, correlationData);

        CorrelationData.Confirm confirm = correlationData.getFuture()
                .get(resolveConfirmTimeoutMillis(), TimeUnit.MILLISECONDS);
        if (confirm == null || !confirm.isAck()) {
            throw new IllegalStateException("RabbitMQ publish not acknowledged: " + reason);
        }

        ReturnedMessage returned = correlationData.getReturned();
        if (returned != null) {
            throw new IllegalStateException("RabbitMQ returned unroutable message: "
                    + returned.getReplyCode() + " " + returned.getReplyText());
        }
        metrics.recordRabbitMqPublishEvent("email_dispatch_published");
        metrics.recordEmailDispatchEvent("publish", "published", event.sourceType(), event.priority());
        metrics.recordRabbitMqPublishDuration(sample, "success");
    } catch (...) {
        // interrupted, execution failed, timeout, or runtime failure -> metrics + exception
    }
}
```

Key points:

- `validate(event)` rejects missing ID/tenant/outbox/schema/event/source/priority.
- `CorrelationData` ID is `event.eventId().toString()`.
- Confirms are waited synchronously for `emailDispatchPublishConfirmTimeoutMs`, default `10_000` ms.
- Any missing confirm, broker nack, timeout, or returned message becomes an exception.
- Exceptions are rethrown so `EmailOutboxPublishService` schedules a publish retry.
- Failure messages are sanitized for `password`, `passwd`, `secret`, `token`, `api-key` fragments.

**Event implementation mapping:** implement `IEmailDispatchPublisher.PublishAsync` so it only returns success after:

1. Payload serialized.
2. Message published with mandatory routing.
3. Broker confirm ack observed within timeout.
4. No return/unroutable signal observed.

If any condition fails, throw an exception and let the application publish service schedule retry.

### 8.3 Message headers and payload

Headers set in `toMessage`:

```java
properties.setContentType(MessageProperties.CONTENT_TYPE_JSON);
properties.setContentEncoding(StandardCharsets.UTF_8.name());
properties.setHeader("schemaVersion", event.schemaVersion());
properties.setHeader("eventType", event.eventType());
properties.setHeader("eventId", event.eventId().toString());
properties.setHeader("tenantId", event.tenantId().toString());
properties.setHeader("emailOutboxId", event.emailOutboxId().toString());
properties.setHeader("idempotencyKey", event.idempotencyKey());
properties.setHeader("sourceType", event.sourceType());
properties.setHeader("priority", event.priority());
properties.setHeader("correlationId", event.correlationId());
properties.setHeader("causationId", event.causationId());
properties.setMessageId(event.eventId().toString());
```

JSON body contains the same pointer metadata:

```json
{
  "schemaVersion": 1,
  "eventType": "email.dispatch.requested",
  "eventId": "...",
  "tenantId": "...",
  "emailOutboxId": "...",
  "idempotencyKey": "...",
  "sourceType": "email_outbox",
  "priority": "NORMAL",
  "correlationId": "...",
  "causationId": null,
  "occurredAt": "..."
}
```

**Event implementation mapping:** set both headers and JSON body. Headers make broker/diagnostic tooling easier; body keeps consumers portable.

### 8.4 Priority routing

```java
private String resolveRoutingKey(String priority) {
    String normalized = priority == null ? "" : priority.trim().toUpperCase(Locale.ROOT);
    if ("CRITICAL".equals(normalized) || "HIGH".equals(normalized) || "PRIORITY".equals(normalized)) {
        return requiredText(rabbitProperties.getEmailDispatchPriorityRoutingKey(), "emailDispatchPriorityRoutingKey");
    }
    return requiredText(rabbitProperties.getEmailDispatchRoutingKey(), "emailDispatchRoutingKey");
}
```

**Event implementation mapping:** if Event implements priority routing, test both publish and consume sides. CRMWorx's priority route is a useful idea but should not be copied incompletely.

---

## 9. Consumer: `RabbitMQEmailDispatchListener`

File: `crmworx-infrastructure/src/main/java/com/oppworx/crmworx/infrastructure/messaging/RabbitMQEmailDispatchListener.java`.

### 9.1 Listener binding

```java
@RabbitListener(
        queues = "${crmworx.messaging.rabbitmq.email-dispatch-queue:crmworx.email.dispatch.standard.q}",
        containerFactory = "crmworxEmailDispatchListenerContainerFactory"
)
public void onEmailDispatch(Message message, Channel channel) throws IOException
```

**Event implementation mapping:** consumer should bind to the explicit configured queue(s), not wildcard exchange consumption. If Event uses both standard and priority queues, register both consumers or a multi-queue consumer.

### 9.2 Manual ack/reject/nack behavior

Main flow:

```java
EmailDispatchRequestedEvent parsedEvent = parseEvent(message);
EmailDispatchDisposition disposition = TenantContext.runWithTenant(
        tenantContext(parsedEvent),
        () -> consumeService.consume(parsedEvent, properties.getEmailDispatchConsumerId())
);
if (disposition == EmailDispatchDisposition.REJECT_TO_DLQ) {
    channel.basicReject(deliveryTag, false);
    return;
}
channel.basicAck(deliveryTag, false);
```

Poison/validation errors:

```java
catch (EmailDispatchPoisonMessageException | IllegalArgumentException ex) {
    channel.basicReject(deliveryTag, false);
}
```

Unexpected errors:

```java
catch (Exception ex) {
    channel.basicNack(deliveryTag, false, true);
}
```

Semantics:

- `basicAck(..., false)` after successful or safely deferred processing.
- `basicReject(..., false)` sends poison messages to DLQ because queue has DLX configured.
- `basicNack(..., false, true)` requeues unexpected infrastructure errors.

**Event implementation mapping:** keep broker disposition outside application business logic but driven by application result. In .NET, this maps to `BasicAck`, `BasicReject(requeue:false)`, and `BasicNack(requeue:true)`.

### 9.3 Tenant context re-entry

```java
private TenantResolutionContext tenantContext(EmailDispatchRequestedEvent event) {
    if (event.tenantId() == null) {
        throw new EmailDispatchPoisonMessageException("email dispatch event tenantId is required");
    }
    return new TenantResolutionContext(event.tenantId(), null, TenantHintSource.INTERNAL_WORKER);
}
```

**Event implementation mapping:** Event consumers must set scoped `TenantContext` from the event before loading the outbox row. After loading, the application service must still verify the row tenant equals the event tenant.

---

## 10. Consumer Application State Machine: `EmailDispatchConsumeService`

File: `crmworx-application/src/main/java/com/oppworx/crmworx/application/features/email/consume/EmailDispatchConsumeService.java`.

### 10.1 Public consume method

```java
@Transactional
public EmailDispatchDisposition consume(EmailDispatchRequestedEvent event, String consumerId) {
    validateEvent(event);
    EmailOutbox outboxEntry = outboxRepository.findById(event.emailOutboxId())
            .orElseThrow(() -> new EmailDispatchPoisonMessageException("Outbox row not found for event"));
    assertTenantAndEventIdentity(event, outboxEntry);

    UUID claimId = uuidGenerator.generate();
    OffsetDateTime now = appClock.now();
    boolean claimed = receiptRepository.tryClaimReceived(...);

    EmailDispatchReceipt receipt = claimed
            ? loadClaimedReceipt(event.tenantId(), event.eventId())
            : loadExistingReceipt(event.tenantId(), event.eventId());

    if (isTerminalReceipt(receipt)) return ACK;
    if (isAlreadySent(outboxEntry)) { markReceiptCompleted(receipt, now); return ACK; }

    // pause/throttle/circuit checks
    // mark processing
    // execute delivery
    return ACK;
}
```

**Event implementation mapping:** this is a MediatR command handler candidate, e.g. `ConsumeEmailDispatchCommandHandler`, or an Application service invoked by the infrastructure consumer. It should remain application-layer state orchestration and depend on ports/repositories, not RabbitMQ client types.

### 10.2 Event validation

```java
if (event.schemaVersion() != 1) throw poison;
if (!"email.dispatch.requested".equalsIgnoreCase(event.eventType().trim())) throw poison;
if (event.eventId() == null) throw poison;
if (event.tenantId() == null) throw poison;
if (event.emailOutboxId() == null) throw poison;
```

**Event implementation mapping:** reject unsupported schema versions and event types to DLQ. Do not attempt best-effort parsing of unknown event contracts.

### 10.3 Tenant and event identity assertion

```java
if (outboxEntry.getTenantId() == null || !outboxEntry.getTenantId().equals(event.tenantId())) {
    throw new EmailDispatchPoisonMessageException("Tenant mismatch between event and outbox row");
}

if (outboxEntry.getPublishEventId() == null) {
    outboxEntry.setPublishEventId(event.eventId());
    outboxRepository.save(outboxEntry);
    return;
}

if (!outboxEntry.getPublishEventId().equals(event.eventId())) {
    throw new EmailDispatchPoisonMessageException("publish_event_id mismatch for dispatch event");
}
```

CRMWorx has a defensive backfill for fast consumers seeing a row before `publish_event_id` is persisted.

**Event implementation mapping:** ideally avoid the backfill race by setting `PublishEventId` before broker publish in the same database claim update, as CRMWorx does. If Event still has race windows, treat backfill as a compatibility/repair path, not the normal path.

### 10.4 Receipt claim idempotency

Repository native SQL:

```sql
INSERT INTO email_dispatch_receipts (...)
VALUES (...)
ON CONFLICT ON CONSTRAINT uq_email_dispatch_receipts_tenant_event DO NOTHING
```

Adapter returns `== 1` as claimed.

Terminal receipt logic:

```java
private boolean isTerminalReceipt(EmailDispatchReceipt receipt) {
    return receipt.getStatus() == EmailDispatchReceiptStatus.COMPLETED
            || receipt.getStatus() == EmailDispatchReceiptStatus.UNKNOWN;
}
```

Already-sent outbox logic:

```java
private boolean isAlreadySent(EmailOutbox outboxEntry) {
    return outboxEntry.getDeliveryStatus() == EmailOutboxDeliveryStatus.SENT;
}
```

**Event implementation mapping:** use a repository method like `TryClaimEmailDispatchReceiptAsync`. On duplicate, load the existing receipt. If receipt is `Completed` or `Unknown`, ack the broker message without SMTP. If row is already `Sent`, complete the receipt and ack.

### 10.5 Runtime safety gates before SMTP

CRMWorx checks, in order:

1. `!processingControl.isEnabled()`
2. `processingControl.isGlobalPaused()`
3. `processingControl.isTenantPaused(event.tenantId())`
4. `processingControl.isTenantCircuitOpen(event.tenantId(), now)`
5. `!throttlePort.tryAcquire(WORKLOAD, event.tenantId())`

Each of these schedules a deferred retry and returns `ACK`, meaning the current broker message is consumed and future processing is DB-scheduled.

```java
scheduleDeferredRetry(outboxEntry, receipt, now, "dispatch_tenant_paused", "Tenant email dispatch paused");
return EmailDispatchDisposition.ACK;
```

**Event implementation mapping:** avoid requeue storms when an operator pauses delivery. Persist a future retry timestamp and ack the broker message.

### 10.6 Mark processing before provider call

```java
receipt.setStatus(EmailDispatchReceiptStatus.PROCESSING);
receipt.setProcessingStartedAt(now);
receipt.setConsumerId(normalizeNullable(consumerId));
receiptRepository.save(receipt);

outboxEntry.setDeliveryStatus(EmailOutboxDeliveryStatus.DELIVERING);
outboxRepository.save(outboxEntry);
```

**Event implementation mapping:** record processing start before SMTP. If a worker dies mid-send, operators can see stuck `Processing/Delivering` rows.

### 10.7 Provider dispatch and outcome classification

```java
int attemptNumber = nextAttemptNumber(outboxEntry.getId());
boolean relayDispatched = false;
try {
    relayPort.dispatch(outboxEntry);
    relayDispatched = true;
    markDeliverySuccess(...);
    return;
} catch (RuntimeException ex) {
    if (relayDispatched) {
        markDeliveryUnknown(...);
        return;
    }
    DeliveryFailureDisposition failureDisposition = classifyFailure(ex);
    if (failureDisposition == PERMANENT) markDeliveryPermanentFailure(...);
    else if (failureDisposition == UNKNOWN) markDeliveryUnknown(...);
    else markDeliveryRetryScheduled(...);
}
```

Current classification:

```java
if (message contains "timeout") return UNKNOWN;
if (ex instanceof IllegalArgumentException) return PERMANENT;
return TRANSIENT;
```

**Event implementation mapping:** the most important state is `UNKNOWN`. SMTP timeout may mean the provider accepted the message but the app did not observe completion. Retrying unknown sends can duplicate email, so CRMWorx dead-letters/holds unknown outcomes instead of blindly retrying.

### 10.8 Success path

`markDeliverySuccess` sets:

- `statusId = 2` (`sent`)
- `deliveryStatus = SENT`
- `sentAt = completedAt`
- clears `lastError`, `nextRetryAt`, `publishNextRetryAt`, `publishLastError`
- `publishStatus = PUBLISHED`
- records an attempt with outcome `succeeded`
- `processingControl.recordDeliverySuccess(tenantId)` clears tenant circuit state
- receipt becomes `COMPLETED`

**Event implementation mapping:** success should clear both delivery and publish error fields and reset any tenant circuit/failure state.

### 10.9 Transient retry path

`markDeliveryRetryScheduled`:

- increments `retryCount`
- stores sanitized/truncated error
- if retry budget exhausted:
  - `statusId = 3`
  - `deliveryStatus = FAILED_PERMANENT`
  - `deadLetteredAt = completedAt`
  - `publishStatus = FAILED`
  - receipt failure code `smtp_retry_exhausted`
- else:
  - `statusId = 1`
  - `deliveryStatus = RETRY_SCHEDULED`
  - `nextRetryAt = completedAt + min(2^retryCount, 3600 seconds)`
  - `publishStatus = RETRY_SCHEDULED`
  - `publishNextRetryAt = nextRetryAt`
  - receipt failure code `smtp_retry_scheduled`

Then records attempt outcome `failed` and calls `processingControl.recordDeliveryFailure`.

**Event implementation mapping:** retry scheduling is database-owned, not RabbitMQ-owned. RabbitMQ redelivery is for infrastructure exceptions; known SMTP retry waits should become future DB eligibility.

### 10.10 Permanent failure path

`markDeliveryPermanentFailure`:

- `statusId = 3`
- `deliveryStatus = FAILED_PERMANENT`
- `deadLetteredAt = completedAt`
- `publishStatus = FAILED`
- records failed attempt
- receipt failure code `smtp_failed_permanent`
- records tenant delivery failure

### 10.11 Unknown outcome path

`markDeliveryUnknown`:

- `statusId = 3`
- `deliveryStatus = UNKNOWN`
- `deadLetteredAt = completedAt`
- `publishStatus = FAILED`
- records attempt outcome `unknown`
- receipt status `UNKNOWN`
- receipt failure code `smtp_outcome_unknown`
- records tenant delivery failure

**Event implementation mapping:** `Unknown` should be a first-class enum value. Do not collapse it into failed/retry.

### 10.12 Failure message sanitization

CRMWorx redacts:

```java
Pattern.compile("(?i)(password|passwd|secret|token|api[-_]?key)\\s*[:=]\\s*[^\\s,;]+")
```

and truncates to `1000` chars for persisted failure messages.

**Event implementation mapping:** sanitize exception messages before storing them in database or structured logs. Do not persist SMTP credentials, tokens, API keys, or tenant secret refs.

---

## 11. Runtime Controls: `InMemoryEmailDispatchProcessingControlAdapter`

File: `crmworx-infrastructure/src/main/java/com/oppworx/crmworx/infrastructure/messaging/InMemoryEmailDispatchProcessingControlAdapter.java`.

Properties:

```java
@ConfigurationProperties(prefix = "crmworx.email.dispatch-processing")
public class EmailDispatchProcessingProperties {
    private boolean enabled = true;
    private boolean globalPaused = false;
    private List<String> pausedTenantIds = new ArrayList<>();
    private int deferredRetryDelaySeconds = 10;
    private boolean circuitEnabled = true;
    private int circuitFailureThreshold = 5;
    private int circuitOpenDurationSeconds = 120;
}
```

Adapter behavior:

- `isEnabled()` returns the global enable flag.
- `isGlobalPaused()` returns the global pause flag.
- `isTenantPaused(UUID tenantId)` compares tenant ID strings against configured paused IDs.
- `isTenantCircuitOpen(UUID tenantId, OffsetDateTime asOf)` checks an in-memory `openUntil` timestamp.
- `recordDeliverySuccess(UUID tenantId)` clears that tenant's circuit state.
- `recordDeliveryFailure(UUID tenantId, OffsetDateTime occurredAt)` increments consecutive failures and opens circuit after threshold for configured duration.

Implementation detail:

```java
circuitsByTenant.compute(tenantId, (ignored, previous) -> {
    TenantCircuitState state = previous == null ? new TenantCircuitState() : previous;
    int failures = state.consecutiveFailures().incrementAndGet();
    if (failures >= threshold) {
        state.setOpenUntil(occurredAt.plusSeconds(openSeconds));
        state.consecutiveFailures().set(0);
    }
    return state;
});
```

**Event implementation mapping:** start with in-memory controls if only one app instance consumes email. For multi-instance deployments, move tenant circuit state to Redis/PostgreSQL so all workers share the same pause/circuit view.

---

## 12. SMTP Provider: `SmtpEmailOutboxRelayClient`

File: `crmworx-infrastructure/src/main/java/com/oppworx/crmworx/infrastructure/notification/SmtpEmailOutboxRelayClient.java`.

### 12.1 Dispatch method

```java
public void dispatch(EmailOutbox entry) {
    Timer.Sample sample = metrics.startTimer();
    try {
        if (entry.getRequestedProvider() == null) {
            entry.setRequestedProvider(EmailProviderType.PLATFORM_SMTP);
        }

        if (shouldUseTenantSmtp(entry)) {
            dispatchUsingTenantSmtpWithFallback(entry);
        } else {
            dispatchUsingPlatformSmtp(entry);
        }
        metrics.recordEmailDeliveryEvent("sent");
        metrics.recordEmailDeliveryDuration(sample, "sent");
    } catch (...) {
        metrics.recordEmailDeliveryEvent("failed");
        metrics.recordEmailDeliveryDuration(sample, "failed");
        throw ...;
    }
}
```

### 12.2 Tenant SMTP with fallback

```java
private void dispatchUsingTenantSmtpWithFallback(EmailOutbox entry) {
    Optional<TenantEmailDeliveryProfile> optionalProfile = tenantProfileRepository.findByTenantId(entry.getTenantId());
    if (optionalProfile.isEmpty()) { dispatchUsingPlatformSmtp(entry); return; }

    TenantEmailDeliveryProfile profile = optionalProfile.get();
    if (!isTenantSmtpReady(profile)) { dispatchUsingPlatformSmtp(entry); return; }

    entry.setResolvedProvider(EmailProviderType.TENANT_SMTP);
    try {
        validateTenantSmtpHost(profile.getHost());
        JavaMailSender tenantSender = buildTenantSmtpSender(profile);
        String fromAddress = isBlank(profile.getFromAddress()) ? properties.getFromAddress() : profile.getFromAddress();
        sendMessage(tenantSender, entry, fromAddress);
        entry.setFinalProvider(EmailProviderType.TENANT_SMTP);
        entry.setFallbackUsed(Boolean.FALSE);
        entry.setFallbackReason(null);
    } catch (RuntimeException ex) {
        if (!isFallbackEligible(entry)) throw ex;
        entry.setFallbackUsed(Boolean.TRUE);
        entry.setFallbackReason(truncate(sanitizeFailureMessage(ex.getMessage()), 500));
        dispatchUsingPlatformSmtp(entry, true);
    }
}
```

Fallback eligibility:

```java
return fallbackPolicy == PLATFORM_FALLBACK_ALLOWED
    || deliveryClass == SECURITY_CRITICAL
    || deliveryClass == ACCESS_CRITICAL
    || deliveryClass == BILLING_CRITICAL
    || deliveryClass == SYSTEM_OPERATIONAL;
```

**Event implementation mapping:** if Event supports tenant SMTP in the future, classify emails. Security-critical email may use platform fallback, while marketing/bulk email may fail instead of falling back.

### 12.3 MIME composition

```java
MimeMessage mimeMessage = sender.createMimeMessage();
MimeMessageHelper helper = new MimeMessageHelper(mimeMessage, true, "UTF-8");
helper.setTo(entry.getRecipientEmail());
helper.setSubject(entry.getSubject());
helper.setFrom(fromAddress.trim());
helper.setReplyTo(entry.getReplyToEmail().trim());

if (hasHtml && hasText) helper.setText(entry.getTextBody(), entry.getHtmlBody());
else if (hasHtml) helper.setText(entry.getHtmlBody(), true);
else helper.setText(entry.getTextBody(), false);

if (!isBlank(entry.getCorrelationId())) {
    mimeMessage.addHeader("X-Correlation-ID", entry.getCorrelationId().trim());
}
sender.send(mimeMessage);
```

**Event implementation mapping:** with MailKit/MimeKit, preserve multipart text/html behavior and add `X-Correlation-ID` headers to outbound emails.

### 12.4 Tenant SMTP host guardrail

CRMWorx rejects tenant SMTP hosts resolving to local/private/restricted ranges unless explicitly allowed:

- any-local
- loopback
- link-local
- site-local
- multicast
- IPv6 unique local `fc00::/7`
- IPv4 `0.0.0.0/8`
- carrier-grade NAT `100.64.0.0/10`

**Event implementation mapping:** if tenants can configure SMTP hostnames, implement DNS resolution guardrails to reduce SSRF/internal-network abuse risk.

---

## 13. DLQ Replay And Parking: `RabbitMQEmailDispatchDeadLetterReplayListener`

File: `crmworx-infrastructure/src/main/java/com/oppworx/crmworx/infrastructure/messaging/RabbitMQEmailDispatchDeadLetterReplayListener.java`.

### 13.1 Listener and feature flag

```java
@ConditionalOnProperty(
        name = {
                "crmworx.messaging.rabbitmq.enabled",
                "crmworx.messaging.rabbitmq.email-dispatch-dlq-replay-enabled"
        },
        havingValue = "true",
        matchIfMissing = false
)
@RabbitListener(
        queues = "${crmworx.messaging.rabbitmq.email-dispatch-dead-letter-queue:crmworx.email.dispatch.dlq}",
        containerFactory = "crmworxEmailDispatchDlqReplayListenerContainerFactory"
)
```

DLQ replay is disabled unless explicitly enabled.

### 13.2 Replay decision

```java
private ReplayDecision decideReplay(EmailDispatchRequestedEvent event) {
    EmailOutbox outbox = outboxRepository.findById(event.emailOutboxId()).orElse(null);
    if (outbox == null) return park("outbox_missing");
    if (tenant mismatch) return park("tenant_mismatch");
    if (publishEventId mismatch) return park("event_mismatch");
    if (outbox.getDeliveryStatus() == SENT) return park("already_sent");
    return allowReplay();
}
```

### 13.3 Parking reasons

CRMWorx parks messages for:

- invalid payload
- tenant missing
- outbox missing
- tenant mismatch
- event mismatch
- already sent

Parked messages are republished to the parking routing key with headers:

- `x-email-dispatch-replay-reason`
- `x-email-dispatch-replay-at`
- `x-email-dispatch-original-routing-key`
- `x-email-dispatch-source-type`
- `x-email-dispatch-priority`

**Event implementation mapping:** DLQ replay must not blindly requeue. It must check database truth first. Already-sent messages should be parked, not replayed.

---

## 14. Integration Outbox: Useful Contrast But Weaker Than Email Dispatch

CRMWorx also has a generic integration outbox relay. The previous conceptual report identified these exact source-level gaps:

- `RabbitMQOutboxRelayClient` publishes generic integration events through `rabbitTemplate.send(exchange, routingKey, message)` but does not wait for correlated publisher confirms or check returned messages like `RabbitMQEmailDispatchPublisher` does.
- `HttpOutboxRelayClient` defaults generic relay transport to HTTP and returns immediately when endpoint URL is blank.
- `IntegrationEventRelay` is a placeholder shell retained after cleanup.
- `IntegrationOutboxRelayService` and `OutboxRelayScheduler` implement a separate generic retry/dead-letter relay path, but the high-reliability queue-first details are more mature in the email path than in the generic integration path.
- `OutboxRelayProperties.fixedDelayMs` exists as configuration, but the scheduler cadence is controlled by the `@Scheduled` annotation expression. This is a documentation/runtime-consistency risk to avoid when porting the concept.

Exact generic-relay files:

| File | Implementation detail | Event guidance |
|---|---|---|
| `crmworx-application/src/main/java/com/oppworx/crmworx/application/features/integration/support/IntegrationOutboxPublisher.java` | Transactional writer for generic integration events. | Useful for Event's generic `OutboxMessage`, but not enough for email delivery reliability by itself. |
| `crmworx-application/src/main/java/com/oppworx/crmworx/application/features/integration/relay/IntegrationOutboxRelayService.java` | Polls pending generic outbox rows, relays through `OutboxRelayPort`, increments retry/dead-letter state. | Keep this concept for generic integrations, but add confirm/return guarantees if broker transport is used. |
| `crmworx-infrastructure/src/main/java/com/oppworx/crmworx/infrastructure/integration/OutboxRelayScheduler.java` | Scheduled wrapper around generic integration relay. | Keep schedulers thin; make delay options actually drive runtime behavior. |
| `crmworx-infrastructure/src/main/java/com/oppworx/crmworx/infrastructure/integration/HttpOutboxRelayClient.java` | HTTP POST relay with HMAC signature; returns if endpoint URL is blank. | Event should fail visibly on missing dispatcher configuration instead of silently succeeding. |
| `crmworx-infrastructure/src/main/java/com/oppworx/crmworx/infrastructure/messaging/RabbitMQOutboxRelayClient.java` | RabbitMQ relay with tenant-aware routing headers but no confirm/return wait. | Event should use the email publisher's confirm/return pattern for any high-value broker dispatch. |
| `crmworx-persistence/src/main/resources/db/migration/V3_27__create_integration_outbox.sql` | Generic integration outbox table, tenant idempotency, retry/dead-letter columns, tenant/source indexes, optional RLS. | Good generic outbox baseline, but specialized delivery workflows still need workflow-specific state tables. |

**Event implementation mapping:** when implementing Event's notification/email flow, copy the mature email-dispatch path, not the weaker generic integration relay. For Event's existing generic `OutboxMessage`, the CRMWorx comparison suggests a future improvement: real dispatchers should not silently no-op and broker transports should expose confirm/return semantics.

---

## 15. Tests: Exact CRMWorx Coverage To Mirror In Event

Source-level test outline found these concrete test files and methods.

### 15.1 Application consume service tests

File: `crmworx-application/src/test/java/com/oppworx/crmworx/application/features/email/EmailDispatchConsumeServiceTest.java`.

Important tests:

- `consume_marksSentAndCompletesReceipt_onSuccessfulDispatch`
- `consume_schedulesRetry_onTransientFailure`
- `consume_marksUnknown_onTimeoutFailure`
- `consume_defersRetry_whenTenantPaused`
- `consume_throwsPoison_onTenantMismatch`
- `consume_backfillsMissingPublishEventId_insteadOfPoisonReject`
- `consume_throwsPoison_whenOutboxRowMissing`
- `consume_ackNoop_whenDuplicateReceiptAlreadyCompleted`
- `consume_ackNoop_whenDuplicateReceiptAlreadyUnknown`
- `consume_marksPermanentFailure_onInvalidDeliveryRequest`
- `consume_marksPermanentFailure_whenRetryBudgetExhausted`
- `consume_marksUnknown_whenPersistenceFailsAfterRelayDispatch`
- `consume_redactsSensitiveFragments_whenFailureMessagesPersisted`
- `consume_throws_whenClaimedRowCannotTransitionToProcessing_beforeSmtpDispatch`

**Event test mapping:** create Application unit tests for every state transition. Mock repositories/ports and assert row status, receipt status, attempts, retry timestamps, and no SMTP invocation for duplicates/paused/poison cases.

### 15.2 RabbitMQ publisher tests

File: `crmworx-infrastructure/src/test/java/com/oppworx/crmworx/infrastructure/messaging/RabbitMQEmailDispatchPublisherTest.java`.

Important tests:

- `publish_succeedsWhenBrokerConfirmsAckAndNoReturn`
- `publish_throwsWhenBrokerNacks`
- `publish_throwsWhenMessageIsReturnedUnroutable`
- `publish_routesCriticalPriorityToPriorityRoutingKey`
- `publish_redactsSecretsFromRuntimeFailureMessage`
- `publish_redactsSecretsFromRuntimeFailureLogs`

**Event test mapping:** test publisher confirm ack, nack, timeout, unroutable return, priority routing, and redaction.

### 15.3 RabbitMQ listener tests

File: `crmworx-infrastructure/src/test/java/com/oppworx/crmworx/infrastructure/messaging/RabbitMQEmailDispatchListenerTest.java`.

Important tests:

- `onEmailDispatch_acksOnSuccessfulConsume`
- `onEmailDispatch_rejectsToDlq_whenConsumeDispositionRequestsReject`
- `onEmailDispatch_rejectsPoisonMessageToDlq`
- `onEmailDispatch_nacksForRequeueOnUnexpectedFailure`
- `onEmailDispatch_rejectsWhenPayloadCannotDeserialize`
- `onEmailDispatch_requeues_whenAckFailsAfterSuccessfulConsume`

**Event test mapping:** test exact broker disposition choices: ack, reject no requeue, nack requeue. These are not just infrastructure tests; they enforce duplicate and poison-message semantics.

### 15.4 DLQ replay tests

File: `crmworx-infrastructure/src/test/java/com/oppworx/crmworx/infrastructure/messaging/RabbitMQEmailDispatchDeadLetterReplayListenerTest.java`.

Important tests:

- `onDeadLetter_replaysValidMessage_toDispatchQueue`
- `onDeadLetter_parksMessage_whenAlreadySent`
- `onDeadLetter_parksMessage_whenOutboxMissing`
- `onDeadLetter_parksMessage_whenTenantMismatch`
- `onDeadLetter_parksMessage_whenPayloadInvalid`
- `onDeadLetter_nacksForRequeue_whenParkingPublishFails`

**Event test mapping:** DLQ replay should have its own tests and feature flag. Do not ship replay tooling that can resend already-sent email.

### 15.5 RabbitMQ topology tests

File: `crmworx-infrastructure/src/test/java/com/oppworx/crmworx/infrastructure/messaging/RabbitMQConfigTest.java`.

Important tests:

- `rabbitConnectionFactory_usesConfiguredProperties`
- `rabbitConnectionFactory_disablesConfirmsWhenConfigured`
- `rabbitTemplate_createsTemplateAndExposesAmqpTemplateContract`
- `emailDispatchTopologyBeans_useConfiguredNamesAndDeadLetterPolicy`
- `emailDispatchContainerFactory_usesManualAckAndConcurrencySettings`
- `emailDispatchDlqReplayContainerFactory_usesManualAckAndReplaySettings`

**Event test mapping:** topology should be tested. If Event declares RabbitMQ via Aspire or code, assert exchange/queue/routing/DLX/manual-ack options.

### 15.6 SMTP relay tests

File: `crmworx-infrastructure/src/test/java/com/oppworx/crmworx/infrastructure/notification/SmtpEmailOutboxRelayClientTest.java`.

Important tests:

- `dispatch_sendsMimeMessageAndRecordsSentMetrics`
- `dispatch_recordsFailedMetricsWhenMailSenderThrows`
- `dispatch_usesTenantSmtpWhenProfileConfigured`
- `dispatch_fallsBackToPlatformWhenTenantFailsAndFallbackEligible`
- `dispatch_redactsSecretsFromFallbackReason`
- `dispatch_rejectsPrivateTenantSmtpHostWhenGuardrailEnabled`

**Event test mapping:** provider fallback and tenant SMTP guardrails need tests if Event implements tenant-configured SMTP.

### 15.7 Testcontainers and end-to-end tests

CRMWorx also proves parts of the implementation against live infrastructure:

| File | Test methods / proof |
|---|---|
| `crmworx-infrastructure/src/test/java/com/oppworx/crmworx/infrastructure/messaging/RabbitMQDeadLetterContainerTest.java` | `failedConsumerMessage_routesToDeadLetterQueue` proves real RabbitMQ dead-letter routing when rejected/requeued behavior is configured correctly. |
| `crmworx-infrastructure/src/test/java/com/oppworx/crmworx/infrastructure/messaging/RabbitMQOutboxRelayClientContainerTest.java` | `dispatch_publishesToRabbitBrokerQueue` proves generic outbox messages reach a live RabbitMQ queue with headers/body. |
| `crmworx-infrastructure/src/test/java/com/oppworx/crmworx/infrastructure/notification/SmtpEmailOutboxRelayClientContainerTest.java` | `dispatch_deliversEmailToMailhogSmtpServer` proves SMTP delivery into MailHog and validates recipient/subject visibility. |
| `crmworx-api/src/test/java/com/oppworx/crmworx/api/integration/TenantEmailDeliveryE2EIT.java` | `testEndpoint_dispatchesEmailThroughOutboxRabbitMqAndSmtp`, `testEndpoint_whenSmtpUnavailable_transitionsToFailureStateWithFailureMetadata`, `testEndpoint_whenBrokerUnavailable_transitionsPublishToRetryScheduled`. This is the strongest system-level evidence for DB + RabbitMQ + SMTP state behavior. |
| `crmworx-api/src/test/java/com/oppworx/crmworx/api/integration/SequenceAutomationEmailChainE2EIT.java` | `sequenceTrigger_toAutomationExecution_toRabbitMqToSmtp_deliversEmail` proves the higher-level automation trigger chain reaches the same queue-first email pipeline. |

**Event test mapping:** use .NET Testcontainers for PostgreSQL, RabbitMQ, and Mailpit/MailHog. Unit tests should prove state transitions; container tests should prove RabbitMQ dead-letter routing, publisher confirm/return behavior where possible, and SMTP delivery visibility.

### 15.8 Exact test gaps CRMWorx exposes

These gaps are useful because Event can avoid inheriting them:

- No direct tests were found for `EmailOutboxPublishScheduler`, `OutboxRelayScheduler`, or scheduler cadence behavior.
- No direct tests were found for `HttpOutboxRelayClient` or `OutboxRelayProperties`.
- No broker-backed end-to-end test was found for the actual email DLQ replay path; CRMWorx has unit replay tests plus a generic dead-letter container test.
- No explicit `EmailDispatchConsumeService` tests were found for global pause, circuit-open, throttle-denied, or `isEnabled=false` branches, even though the code implements those gates.
- No `RabbitMQConfig` test was found for generic integration-outbox topology; the config tests focus on email-dispatch topology.
- No live SMTP/TLS/auth test was found for tenant SMTP; tenant SMTP behavior is unit-tested with fakes and MailHog covers platform SMTP delivery.
- No obvious publish-service test was found for empty/no-pending batches or multi-page publish loops.

**Event implementation mapping:** turn these gaps into acceptance criteria. If Event builds this workflow, the first test plan should include scheduler cadence/options, disabled/misconfigured transports, global pause/circuit/throttle branches, live DLQ replay safety, and priority queue consumption if priority routing is enabled.

---

## 16. Concrete Event Implementation Blueprint

This section translates CRMWorx implementation pieces into a .NET/Clean Architecture shape without reading Event source.

### 16.1 Domain / Persistence model

Create specialized entities/tables:

- `EmailDispatchOutbox`
  - `Id Guid` UUIDv7
  - `TenantId Guid`
  - `RecipientEmail`
  - `ReplyToEmail`
  - `Subject`
  - `TextBody`
  - `HtmlBody`
  - `CorrelationId`
  - `DeliveryClass`
  - `FallbackPolicy`
  - `RequestedProvider`
  - `ResolvedProvider`
  - `FinalProvider`
  - `FallbackUsed`
  - `FallbackReason`
  - `DeliveryStatus`
  - `StatusId` if Event has lookup status pattern
  - `RetryCount`
  - `MaxRetries`
  - `LastError`
  - `NextRetryAt`
  - `SentAt`
  - `DeadLetteredAt`
  - `CreatedAt`
  - `IdempotencyKey`
  - `PublishEventId`
  - `PublishStatus`
  - `PublishClaimedAt`
  - `PublishedAt`
  - `PublishAttemptCount`
  - `PublishLastError`
  - `PublishNextRetryAt`

- `EmailDispatchReceipt`
  - `Id Guid`
  - `TenantId Guid`
  - `EventId Guid`
  - `EmailOutboxId Guid`
  - `Status`
  - `ConsumerId`
  - `FirstSeenAt`
  - `ProcessingStartedAt`
  - `CompletedAt`
  - `FailedAt`
  - `FailureCode`
  - `FailureMessage`
  - `SmtpMessageId`

- `EmailOutboxDeliveryAttempt`
  - `Id Guid`
  - `EmailOutboxId Guid`
  - `TenantId Guid`
  - `AttemptNumber int`
  - `ProviderType`
  - `ProviderProfileId Guid?`
  - `StartedAt`
  - `CompletedAt`
  - `Outcome`
  - `SmtpStatusCode`
  - `ErrorCategory`
  - `SanitizedErrorMessage`
  - `CorrelationId`

Respect Event rules:

- Repositories return entities, not DTOs.
- Keep EF Core in Persistence.
- Application handlers/services orchestrate state and map DTOs.
- Use `Guid` UUIDv7 for aggregates/outbox IDs.

### 16.2 Application ports

Create ports equivalent to CRMWorx:

```csharp
public interface IEmailDispatchPublisher
{
    Task PublishAsync(EmailDispatchRequestedEvent message, CancellationToken cancellationToken);
}

public interface IEmailOutboxRelay
{
    Task DispatchAsync(EmailDispatchOutbox entry, CancellationToken cancellationToken);
}

public interface IEmailDispatchProcessingControl
{
    bool IsEnabled();
    bool IsGlobalPaused();
    bool IsTenantPaused(Guid tenantId);
    bool IsTenantCircuitOpen(Guid tenantId, DateTimeOffset asOf);
    int DeferredRetryDelaySeconds { get; }
    void RecordDeliverySuccess(Guid tenantId);
    void RecordDeliveryFailure(Guid tenantId, DateTimeOffset occurredAt);
}
```

### 16.3 Repository methods

Minimum repository methods:

```csharp
Task<IReadOnlyList<EmailDispatchOutbox>> FindPendingForPublishAsync(int limit, DateTimeOffset eligibleBefore, CancellationToken ct);
Task<bool> TryMarkPublishClaimedAsync(Guid id, Guid publishEventId, int publishAttemptCount, DateTimeOffset claimedAt, CancellationToken ct);
Task MarkPublishSucceededAsync(Guid id, Guid publishEventId, DateTimeOffset publishedAt, CancellationToken ct);
Task MarkPublishRetryScheduledAsync(Guid id, Guid publishEventId, int publishAttemptCount, string? error, DateTimeOffset nextRetryAt, CancellationToken ct);
Task<EmailDispatchOutbox?> GetByIdAsync(Guid id, CancellationToken ct);
Task<EmailDispatchOutbox> SaveAsync(EmailDispatchOutbox entity, CancellationToken ct);
```

Receipt repository:

```csharp
Task<bool> TryClaimReceivedAsync(Guid id, Guid tenantId, Guid eventId, Guid emailOutboxId, EmailDispatchReceiptStatus status, string? consumerId, DateTimeOffset firstSeenAt, CancellationToken ct);
Task<EmailDispatchReceipt?> FindByTenantIdAndEventIdAsync(Guid tenantId, Guid eventId, CancellationToken ct);
Task<EmailDispatchReceipt> SaveAsync(EmailDispatchReceipt receipt, CancellationToken ct);
```

Use PostgreSQL `ON CONFLICT ON CONSTRAINT uq_email_dispatch_receipts_tenant_event DO NOTHING` for the receipt claim.

### 16.4 Background services

Implement separate hosted services:

1. `EmailOutboxPublishWorker`
   - periodic polling
   - calls Application publish service
   - enabled independently

2. `RabbitMqEmailDispatchConsumer`
   - consumes standard and optionally priority queues
   - manually acks/rejects/nacks
   - creates scope and tenant context
   - calls Application consume service

3. `RabbitMqEmailDispatchDlqReplayWorker`
   - disabled by default
   - validates DB truth before replay
   - parks unsafe messages

### 16.5 Metrics and logs

Mirror CRMWorx low-cardinality dimensions:

- `stage`: `publish`, `consume`, `replay`
- `outcome`: `published`, `failed`, `confirm_timeout`, `acked`, `poison_rejected`, `nack_requeue`, `replayed`, `parked_already_sent`
- `source_type`: usually `email_outbox`
- `priority`: `NORMAL`, `HIGH`, `CRITICAL`, `unknown`

Use structured logs with:

- `eventId`
- `tenantId`
- `emailOutboxId`
- `sourceType`
- `priority`
- `correlationId`
- `causationId`

Avoid recipient email/body in routine logs.

---

## 17. Implementation Pitfalls To Avoid When Porting To Event

1. **Do not mark published after only calling RabbitMQ publish.** Wait for broker confirm and no return/unroutable signal.
2. **Do not embed email content in RabbitMQ messages.** Use pointer-only events.
3. **Do not use one retry counter for everything.** Publish retry and SMTP delivery retry are separate.
4. **Do not rely on RabbitMQ redelivery for business retry.** Persist `NextRetryAt` and ack deferred messages.
5. **Do not retry unknown SMTP outcomes automatically.** Unknown may already have been accepted by provider.
6. **Do not implement DLQ replay as blind requeue.** Validate DB row, tenant, event ID, and sent status first.
7. **Do not ship priority queue routing unless priority queue consumption is also implemented.** CRMWorx has a useful but incomplete priority consumption shape.
8. **Do not silently no-op missing endpoints/publishers.** CRMWorx generic relay has this weakness; Event should fail visibly.
9. **Do not store raw provider exception messages without sanitization.** Redact secrets and truncate.
10. **Do not inspect local role/claim state in UI for action availability.** This report complements Event's HAL rule: handlers/policies decide affordances, clients use links.
11. **Do not create options that do not drive runtime behavior.** CRMWorx exposes scheduler delay properties; Event should ensure the hosted service actually reads `IOptions` values.
12. **Do not assume unit DLQ replay tests prove live broker replay.** Add at least one container/e2e test for replaying or parking a real dead-lettered email-dispatch message.
13. **Do not enable priority routing without priority consumers and tests.** CRMWorx routes high-priority messages to a priority routing key but the verified listener binds only the standard queue.

---

## 18. Progressive Evidence Log

- Read CRMWorx application publish service, consume service, event contract, ports, receipt disposition, runtime control port.
- Read CRMWorx RabbitMQ publisher, standard listener, DLQ replay listener, scheduler, config, properties.
- Read CRMWorx Flyway migrations `V3_51` and `V3_52` for concrete tables, columns, constraints, indexes, and RLS policy shape.
- Read CRMWorx JPA repositories/adapters for conditional publish claim and receipt `ON CONFLICT` claim implementation.
- Read CRMWorx SMTP relay client for provider fallback, MIME composition, correlation header, tenant SMTP host guardrail, and metrics.
- Read test outlines for Application consume service, RabbitMQ publisher/listener/DLQ/config, SMTP relay.
- Collected focused background implementation inventory and test inventory agents, then merged their exact findings: additional generic integration outbox files, Testcontainers/E2E test evidence, missing test coverage, and scheduler/config consistency risks.
- Verified the report preserves the main CRMWorx distinction: email dispatch is the mature reliability path; generic integration relay is useful but weaker because it lacks confirm/return guarantees and can silently no-op for blank HTTP endpoints.

---

## 19. Sequence, Enrollment, Automation, RabbitMQ Deep Dive

This section expands the earlier email/outbox analysis with the CRMWorx sequence automation chain that is especially relevant for ISLAMU Event if Event will build multi-step attendee, organizer, notification, follow-up, reminder, or post-event workflows.

The key CRMWorx idea is not just "have sequences". The implementation splits the workflow into four durable stages:

1. **Sequence definition**: authored, versioned, published workflow steps.
2. **Sequence enrollment**: runtime cursor for one entity inside one sequence revision.
3. **Automation execution**: durable ledger for side effects triggered by a sequence step.
4. **Email dispatch**: queue-first RabbitMQ delivery pipeline for actual SMTP work.

That separation is what Event should copy conceptually. Each stage owns one state machine and one failure boundary.

### 19.1 Exact CRMWorx sequence files

| Area | File | What it owns |
|---|---|---|
| Sequence domain | `/home/amir/Oppworx/Github/crmworx-api/crmworx-domain/src/main/java/com/oppworx/crmworx/domain/entity/Sequence.java` | Tenant-scoped sequence definition: `id`, `tenantId`, `name`, `entityType`, `status`, version/publication fields. |
| Step domain | `/home/amir/Oppworx/Github/crmworx-api/crmworx-domain/src/main/java/com/oppworx/crmworx/domain/entity/SequenceStep.java` | Ordered step definition: `sequenceId`, `stepOrder`, `stepType`, `configJson`. |
| Enrollment domain | `/home/amir/Oppworx/Github/crmworx-api/crmworx-domain/src/main/java/com/oppworx/crmworx/domain/entity/SequenceEnrollment.java` | Runtime cursor: `currentStepNumber`, `status`, `lockToken`, `lockExpiresAt`, `lastProgressionKey`, `retryCount`, `nextRunAt`, `lastFailureReason`. |
| Automation execution domain | `/home/amir/Oppworx/Github/crmworx-api/crmworx-domain/src/main/java/com/oppworx/crmworx/domain/entity/AutomationExecution.java` | Durable side-effect ledger: `ruleId`, `ruleRevisionId`, aggregate identity, payload, `statusId`, attempts, correlation, retry/dead-letter fields. |
| Enrollment worker | `/home/amir/Oppworx/Github/crmworx-api/crmworx-application/src/main/java/com/oppworx/crmworx/application/features/sequenceenrollment/handlers/commands/ProcessDueSequenceEnrollmentsCommandHandler.java` | Batch processor for due enrollments; lease, progression, retry/backoff. |
| Manual advance | `/home/amir/Oppworx/Github/crmworx-api/crmworx-application/src/main/java/com/oppworx/crmworx/application/features/sequenceenrollment/handlers/commands/AdvanceSequenceEnrollmentCommandHandler.java` | Explicit one-enrollment progression with the same lease/idempotency principles. |
| Step dispatch port | `/home/amir/Oppworx/Github/crmworx-api/crmworx-application/src/main/java/com/oppworx/crmworx/application/contracts/infrastructure/sequence/SequenceStepActionDispatchPort.java` | Application abstraction for executing a current step. |
| Step dispatch result | `/home/amir/Oppworx/Github/crmworx-api/crmworx-application/src/main/java/com/oppworx/crmworx/application/features/sequenceenrollment/actions/SequenceStepActionDispatchResult.java` | Progression hints: continue, wait, or route to branch target. |
| Step dispatcher | `/home/amir/Oppworx/Github/crmworx-api/crmworx-infrastructure/src/main/java/com/oppworx/crmworx/infrastructure/sequence/SequenceStepActionDispatcherAdapter.java` | Infrastructure bridge from step config to email enqueue, automation trigger, task command, wait, branch. |
| Automation trigger | `/home/amir/Oppworx/Github/crmworx-api/crmworx-application/src/main/java/com/oppworx/crmworx/application/features/automationruntime/handlers/commands/TriggerAutomationEventCommandHandler.java` | Deduplicates trigger events and creates pending automation executions. |
| Automation processor | `/home/amir/Oppworx/Github/crmworx-api/crmworx-application/src/main/java/com/oppworx/crmworx/application/features/automationruntime/handlers/commands/ProcessPendingAutomationExecutionsCommandHandler.java` | Claims pending executions, dispatches actions, succeeds/retries/dead-letters. |
| Automation dispatcher | `/home/amir/Oppworx/Github/crmworx-api/crmworx-infrastructure/src/main/java/com/oppworx/crmworx/infrastructure/automation/OutboxAutomationExecutionDispatcher.java` | Turns automation actions into email outbox entries or integration outbox messages. |
| Automation scheduler | `/home/amir/Oppworx/Github/crmworx-api/crmworx-infrastructure/src/main/java/com/oppworx/crmworx/infrastructure/automation/AutomationExecutionScheduler.java` | `@Scheduled` processor wrapper requiring tenant/business-scope config. |

### 19.2 Database shape for sequence runtime

CRMWorx starts with sequence tables in `V2_17__create_sequences.sql`, then progressively hardens them with runtime fields and revision snapshots.

Important migrations:

- `/home/amir/Oppworx/Github/crmworx-api/crmworx-persistence/src/main/resources/db/migration/V2_17__create_sequences.sql`
  - Creates `sequences`.
  - Creates `sequence_steps`.
  - Creates `sequence_enrollments`.
- `/home/amir/Oppworx/Github/crmworx-api/crmworx-persistence/src/main/resources/db/migration/V3_30__add_sequence_enrollment_runtime_fields.sql`
  - Adds lease/retry/idempotency runtime columns to `sequence_enrollments`.
  - Adds runtime indexes for lock expiry, next run, and last progression key.
- `/home/amir/Oppworx/Github/crmworx-api/crmworx-persistence/src/main/resources/db/migration/V3_64__add_sequence_revisions.sql`
  - Adds immutable `sequence_revisions` and `sequence_step_revisions`.
  - Adds `sequence_enrollments.sequence_revision_id` so runtime enrollments pin to a published snapshot.
- `/home/amir/Oppworx/Github/crmworx-api/crmworx-persistence/src/main/resources/db/migration/V3_81__decompose_sequence_step_config_json.sql`
  - Decomposes `sequence_steps.config_json` into typed/denormalized columns such as email, trigger, wait, task, and branch fields.

The critical `SequenceEnrollment` fields are:

```text
id
tenant_id
sequence_id
sequence_revision_id
entity_type
entity_id
current_step_number
status
enrolled_at
completed_at
lock_token
lock_expires_at
last_progression_key
retry_count
next_run_at
last_failure_reason
created_at
updated_at
```

Event translation:

- Use `Guid` for aggregate IDs and sequence/enrollment IDs.
- Keep `SequenceEnrollment` as an aggregate/runtime entity, not a DTO projection.
- Pin enrollments to immutable published workflow revisions. Do not let an in-flight enrollment read mutable draft steps.
- Add separate fields for `CurrentStepOrder`, `NextRunAt`, `RetryCount`, `LastProgressionKey`, `LockToken`, and `LockExpiresAt`.
- Add indexes equivalent to:
  - `(TenantId, Status, NextRunAt)` for due work.
  - `(TenantId, LockExpiresAt)` for reclaiming expired leases.
  - `(TenantId, LastProgressionKey)` for debugging/idempotency lookup.

### 19.3 Sequence enrollment worker: lease plus progression key

The central batch worker is `ProcessDueSequenceEnrollmentsCommandHandler.handle(...)`.

Its verified implementation does the following:

1. Checks `processingConfig.isEnabled()` and returns a skip response if disabled.
2. Acquires a workload throttle via `throttlePort.tryAcquire(WORKLOAD, tenantId)`.
3. Computes `batchSize` and `maxRetries` from command values capped by config.
4. Loads due enrollments with `enrollmentRepository.findRunnableEnrollments(tenantId, now, batchSize)`.
5. Builds a deterministic progression key:

```text
wkr:{enrollmentId}:step:{currentStepNumber}:retry:{retryCount}
```

6. Skips the enrollment if `progressionKey.equals(enrollment.getLastProgressionKey())`.
7. Claims the enrollment lease with:

```java
enrollmentRepository.acquireLease(
    enrollment.getId(),
    enrollment.getTenantId(),
    progressionKey,
    now.plusSeconds(LEASE_SECONDS),
    now
)
```

8. Reloads the claimed enrollment from storage.
9. Calls `advanceEnrollment(claimedEnrollment, progressionKey, now)`.
10. On `BusinessRuleException`, records failure and handles it as non-retryable.
11. On other `RuntimeException`, records failure and handles it as retryable.
12. Releases workload throttle in `finally`.

The lease SQL is implemented in `JpaSequenceEnrollmentRepository.tryAcquireLease(...)`:

```java
update JpaSequenceEnrollment e
   set e.lockToken = :lockToken,
       e.lockExpiresAt = :lockExpiresAt
 where e.id = :id
   and e.tenantId = :tenantId
   and (e.lockExpiresAt is null or e.lockExpiresAt <= :now)
```

Event translation:

- Use a repository method like `TryAcquireSequenceEnrollmentLeaseAsync(enrollmentId, tenantId, lockToken, lockExpiresAt, now, cancellationToken)` returning `bool`.
- Do not throw on contention; contention is expected in background processors.
- Use an optimistic conditional `UPDATE` rather than loading and saving with an in-memory check.
- Store the progression key as the lease token or alongside it so duplicate worker attempts can be recognized.

### 19.4 Enrollment progression algorithm

`ProcessDueSequenceEnrollmentsCommandHandler.advanceEnrollment(...)` is the state machine core.

Verified flow:

1. `loadSequenceRevision(enrollment)` verifies the pinned `sequenceRevisionId` still exists.
2. `progressionService.validateAdvanceAllowed(enrollment)` blocks terminal enrollments.
3. Load step revisions with `stepRevisionReadPort.findBySequenceRevisionId(enrollment.getSequenceRevisionId())`.
4. Calculate `maxStepOrder`.
5. Find the current step by `stepOrder == enrollment.currentStepNumber`.
6. Dispatch the action:

```java
SequenceStepActionDispatchResult dispatchResult =
    dispatchCurrentStepAction(enrollment, currentStep, progressionKey);
```

7. Resolve the next step:
   - default: current step + 1
   - branch result: explicit route target
8. Resolve next run:
   - wait result: `now + duration`
   - immediate result: `now`
9. If `nextStepOrder > maxStepOrder`, mark enrollment `COMPLETED` and clear `nextRunAt`.
10. Otherwise, require forward progress, require target step exists, set `currentStepNumber`, keep `ACTIVE`, set `nextRunAt`, and call `schedulerPort.scheduleNextRun(...)`.
11. Finalize progression state, reset retry count, clear last failure, save.

Important guardrails:

- `requireForwardProgress(...)` prevents loops unless explicit branch support permits only valid forward routes.
- `requireStepExists(...)` catches invalid branch targets.
- Sequence revisions are read, not mutable draft steps.
- Successful progression resets retry/failure state.

Event translation:

- Create an Application handler such as `ProcessDueSequenceEnrollmentsHandler`.
- Keep sequence progression in Application, not Infrastructure, because it is workflow business logic.
- Put actual side-effect adapters behind ports, like CRMWorx does with `SequenceStepActionDispatchPort`.
- Use a `SequenceStepDispatchResult` record with the same three shapes: `ContinueImmediately`, `WaitUntil/WaitFor`, `RouteToStep`.

### 19.5 Progression failure behavior

`handleProgressionFailure(...)` is intentionally simple and durable:

```java
int retries = normalizeRetryCount(enrollment.getRetryCount()) + 1;
enrollment.setRetryCount(retries);
enrollment.setLastProgressionKey(progressionKey);
enrollment.setLastFailureReason(truncateFailureMessage(failureMessage));
enrollment.setLockToken(null);
enrollment.setLockExpiresAt(null);
enrollment.setCompletedAt(null);
```

Then:

- If non-retryable or retries exceed max retries:
  - clear `nextRunAt`
  - record hard failure telemetry
  - save enrollment with failure reason
- Else:
  - set `nextRunAt = now + min(2^retryCount, 3600 seconds)`
  - record retry telemetry
  - save enrollment

Notice that CRMWorx does **not** push failed progression directly to RabbitMQ. Progression retry remains database-owned. RabbitMQ is only used once the workflow emits durable email work.

Event translation:

- Use DB-backed retry for workflow progression.
- Do not model progression retry as queue requeue loops.
- Persist truncated, sanitized failure reasons for operations.
- Add a terminal/paused failure state or explicit `FailedAt` if Event needs operator repair workflows. CRMWorx currently leaves failed non-terminal enrollments with `nextRunAt = null`, so Event can improve by making the failed state more visible.

### 19.6 Step action dispatcher: exact side-effect mapping

`SequenceStepActionDispatcherAdapter.dispatch(...)` maps step types:

```java
if SEND_EMAIL -> dispatchSendEmail(...); return continueImmediately()
if TRIGGER_AUTOMATION_EVENT -> dispatchTriggerAutomationEvent(...); return continueImmediately()
if WAIT -> return waitFor(waitDuration(...))
if CREATE_TASK -> dispatchCreateTask(...); return continueImmediately()
if BRANCH -> return routeTo(resolveBranchTarget(...))
```

#### SEND_EMAIL step

`dispatchSendEmail(...)` reads:

- `to` or `recipientEmail`
- `subject`
- `body`
- `htmlBody`
- `replyTo`
- optional `businessScopeId`

Then calls:

```java
emailDeliveryPort.enqueue(new EmailMessage(
    context.tenantId(),
    businessScopeId,
    recipientEmail,
    replyToEmail,
    subject,
    textBody,
    htmlBody,
    context.progressionKey(),
    defaultIdempotencyKey(context, "email"),
    null
));
```

This is the key handoff: sequence step -> email intent -> email outbox -> RabbitMQ publisher -> RabbitMQ listener -> SMTP.

For Event, this means a sequence step should not call SMTP directly. It should create a notification/email dispatch intent and let the queue-first dispatch subsystem handle delivery.

#### TRIGGER_AUTOMATION_EVENT step

`dispatchTriggerAutomationEvent(...)` reads:

- required `businessScopeId`
- required `triggerEventType`
- optional `payload`
- optional `correlationKey`, defaulting to progression key
- optional `idempotencyKey`, defaulting to `sequence-step:trigger:{enrollmentId}:step:{stepOrder}`

It runs inside an internal tenant context and sends:

```java
pipeline.send(new TriggerAutomationEventCommand(
    tenantId,
    businessScopeId,
    AutomationTriggerType.fromValue(triggerEventType),
    AutomationAggregateType.fromValue(requiredAggregateType(context)),
    aggregateId,
    payloadJson,
    correlationKey,
    idempotencyKey,
    SYSTEM_ACTOR_ID
));
```

For Event, this maps naturally to MediatR:

```csharp
await mediator.Send(new TriggerAutomationEventCommand(
    tenantId,
    businessScopeId,
    triggerType,
    aggregateType,
    aggregateId,
    payloadJson,
    correlationKey,
    idempotencyKey,
    SystemActorId), cancellationToken);
```

The command should be handler-level authorized for internal/system execution and should still bind tenant context explicitly.

#### WAIT step

`WAIT` returns `SequenceStepActionDispatchResult.waitFor(waitDuration(config))`. CRMWorx supports config fields such as seconds/minutes/days and has weekend-skipping logic.

For Event, store wait state in `SequenceEnrollment.NextRunAt`. Do not schedule one timer per enrollment in memory.

#### CREATE_TASK step

`dispatchCreateTask(...)` sends a `CreateTaskCommand` through the pipeline with business scope, title, status, owner/team, related entity identity, and due date.

For Event, this is a useful pattern for later actions such as:

- create organizer follow-up task
- create moderation review task
- create speaker onboarding task
- create venue checklist item

#### BRANCH step

`evaluateBranch(...)` supports:

- `equals`
- `not_equals`
- `contains`
- `not_contains`
- `greater_than`
- `less_than`
- `exists`
- `not_exists`

It maps known condition fields from `SequenceStepExecutionContext` and routes to `trueStepOrder` or `falseStepOrder`.

Event should add branch operators cautiously. Keep the operator vocabulary small, deterministic, and covered by tests.

### 19.7 Automation trigger: receipt dedup before creating executions

`TriggerAutomationEventCommandHandler.handle(...)` implements the automation intake boundary.

Verified flow:

1. Build `AutomationEventContext` from command.
2. Choose dedup key:

```java
String dedupKey = command.idempotencyKey() == null || command.idempotencyKey().isBlank()
    ? command.correlationKey()
    : command.idempotencyKey().trim();
```

3. If dedup key exists, call `eventReceiptRepository.tryClaim(...)`.
4. If claim fails, return a deduplicated response without creating executions.
5. Load active rules via `ruleReadPort.findByFilters(tenantId, businessScopeId, triggerEventType, LifecycleStatus.ACTIVE)`.
6. For each rule, evaluate conditions.
7. Require `publishedRevisionId`; active rule without published revision is illegal.
8. Create `AutomationExecution` with:
   - `statusId = PENDING`
   - `attemptCount = 0`
   - `payloadJson = eventContext.payload().toString()`
   - `correlationKey = dedupKey`
   - `ruleRevisionId = publishedRevisionId`
9. Stop after first matching rule if `stopOtherRulesAfterMatch` is true.

The migration `/home/amir/Oppworx/Github/crmworx-api/crmworx-persistence/src/main/resources/db/migration/V3_48__add_automation_event_receipts.sql` creates `automation_event_receipts` with a uniqueness constraint for deduplication and RLS policy.

Event translation:

- Add a durable `AutomationEventReceipt` table for trigger dedup. Do not rely only on request idempotency middleware.
- Use a unique constraint around `(TenantId, BusinessScopeId, TriggerEventType, AggregateType, AggregateId, DedupKey)` or the Event-equivalent identity.
- Create execution rows only after receipt claim succeeds.
- Pin to a published automation rule revision, not mutable rule JSON.

### 19.8 Automation execution processor: claim, dispatch, retry, dead-letter

`ProcessPendingAutomationExecutionsCommandHandler.handle(...)` mirrors sequence processing but for side effects.

Verified flow:

1. Skip if processing disabled.
2. Acquire workload throttle.
3. Calculate batch size and max retries.
4. Find runnable pending executions by tenant/business scope/status/next run.
5. Claim each with `executionRepository.tryStartExecution(candidate.id, tenantId, now)`.
6. Reload claimed execution.
7. Record telemetry start.
8. Call `dispatcher.dispatch(claimedExecution)`.
9. On success:
   - `statusId = SUCCEEDED`
   - `completedAt = now`
   - clear `lastError`
   - clear `nextRunAt`
10. On failure:
   - if attempts >= max retries: `FAILED`, `completedAt`, `deadLetteredAt`, clear `nextRunAt`
   - else: `PENDING`, `nextRunAt = now + min(2^attemptCount, 3600 seconds)`

Event translation:

- Treat automation execution as its own durable state machine.
- Add explicit statuses: `Pending`, `Running`, `Succeeded`, `Failed`, `Cancelled`.
- Add `AttemptCount`, `NextRunAt`, `LastError`, `DeadLetteredAt`.
- Use conditional claim updates to avoid duplicate dispatch.
- Do not let a workflow enrollment directly send every external side effect. Enrollment should trigger automation; automation execution should dispatch effects.

### 19.9 Automation action dispatcher: email actions return to the email outbox

`OutboxAutomationExecutionDispatcher.dispatch(...)` is the bridge from automation execution to actual side effects.

Verified behavior:

1. Reject execution missing `ruleRevisionId`.
2. Load immutable rule revision by tenant/rule revision ID.
3. Parse `actionsJson` via `AutomationActionPayloadParser.parse(...)`.
4. For each action, build idempotency key:

```text
{executionId}:{actionType}:{index}
```

5. If action type is `SEND_EMAIL`, call:

```java
emailDeliveryPort.enqueue(buildEmailMessage(execution, action, idempotencyKey));
```

6. Otherwise, serialize an integration outbox envelope and call `outboxPublisher.enqueue(...)`.

The non-email integration envelope includes:

```json
{
  "automationExecutionId": "...",
  "ruleId": "...",
  "ruleRevisionId": "...",
  "tenantId": "...",
  "businessScopeId": "...",
  "actionType": "...",
  "sourceDomainEventId": "...",
  "correlationKey": "...",
  "action": { }
}
```

The email message builder:

- builds an `AutomationTemplateContext` from execution payload
- resolves templated `to` or `recipientEmail`
- uses default subject/body when not provided
- resolves `htmlBody` and `replyTo`
- creates `EmailMessage` with:
  - tenant ID
  - business scope ID
  - recipient
  - reply-to
  - subject
  - text/html body
  - correlation key
  - action idempotency key

Event translation:

- Model `AutomationExecutionDispatcherPort` in Application.
- Put email-specific dispatch behind `INotificationIntentWriter` or `IEmailDispatchRequestPort`.
- The dispatcher should not publish RabbitMQ messages directly. It should write the durable email outbox/notification intent.
- Non-email integrations can use a separate integration outbox, but should not be considered as reliable as the email path unless publisher confirms/returns are added.

### 19.10 Full runtime chain in CRMWorx

The complete chain is:

```text
SequenceEnrollment due
  -> ProcessDueSequenceEnrollmentsCommandHandler.handle
  -> tryAcquireLease(sequence_enrollments)
  -> load pinned sequence revision + step revisions
  -> SequenceStepActionDispatcherAdapter.dispatch
      -> SEND_EMAIL: EmailDeliveryPort.enqueue
      -> TRIGGER_AUTOMATION_EVENT: TriggerAutomationEventCommand
      -> WAIT: set enrollment.nextRunAt
      -> CREATE_TASK: CreateTaskCommand
      -> BRANCH: route currentStepNumber

TriggerAutomationEventCommandHandler.handle
  -> automation_event_receipts.tryClaim
  -> find active rules by tenant/businessScope/trigger
  -> evaluate conditions
  -> create AutomationExecution(PENDING)

ProcessPendingAutomationExecutionsCommandHandler.handle
  -> find runnable pending executions
  -> tryStartExecution
  -> OutboxAutomationExecutionDispatcher.dispatch
      -> SEND_EMAIL: EmailDeliveryPort.enqueue
      -> other action: IntegrationOutboxPublisher.enqueue

EmailDeliveryPort.enqueue
  -> email_outbox durable row
  -> EmailOutboxPublishScheduler
  -> EmailOutboxPublishService.publish
  -> RabbitMQEmailDispatchPublisher.publish with confirm/return checking
  -> RabbitMQEmailDispatchListener manual ACK/NACK/reject
  -> EmailDispatchConsumeService.consume
  -> SMTP delivery + receipts + delivery attempts
```

This is the implementation pattern Event should study closely: workflow progression, automation dispatch, and email delivery are not collapsed into one handler.

### 19.11 RabbitMQ's role in sequence automation

RabbitMQ is **not** used for every transition.

CRMWorx uses RabbitMQ for the final email dispatch transport, after durable database state exists:

- Sequence/enrollment progression is DB-polled and lease-protected.
- Automation execution processing is DB-polled and claim-protected.
- Email outbox publishing turns durable email intent into a pointer-only RabbitMQ message.
- RabbitMQ listener turns pointer event back into a DB-validated consume operation.

That is important. Event should not prematurely put sequence progression itself into RabbitMQ unless there is a strong reason. Database-backed due-work processing is easier to inspect, retry, pause, and repair.

Recommended Event rule:

> Use DB state machines for workflow decisions; use RabbitMQ only for transport of already-durable side-effect intents.

### 19.12 Sequence automation tests surfaced in CRMWorx

The focused test inventory identified the strongest sequence/automation coverage.

#### Application unit tests

| File | What it proves |
|---|---|
| `/home/amir/Oppworx/Github/crmworx-api/crmworx-application/src/test/java/com/oppworx/crmworx/application/features/sequence/SequenceCommandHandlerTest.java` | Sequence create/update/publish lifecycle, immutable revision snapshot behavior, non-draft guards. |
| `/home/amir/Oppworx/Github/crmworx-api/crmworx-application/src/test/java/com/oppworx/crmworx/application/features/sequence/SequenceQueryHandlerTest.java` | Sequence read/query normalization and tenant lookup behavior. |
| `/home/amir/Oppworx/Github/crmworx-api/crmworx-application/src/test/java/com/oppworx/crmworx/application/features/sequenceenrollment/SequenceEnrollmentCommandHandlerTest.java` | Enrollment create/update/delete, tenant and target validation. |
| `/home/amir/Oppworx/Github/crmworx-api/crmworx-application/src/test/java/com/oppworx/crmworx/application/features/sequenceenrollment/SequenceEnrollmentProgressionCommandHandlerTest.java` | Progression, idempotency, retry/lease, cancel, completion. |
| `/home/amir/Oppworx/Github/crmworx-api/crmworx-application/src/test/java/com/oppworx/crmworx/application/features/sequencestep/SequenceStepCommandHandlerTest.java` | Step create/update/delete ordering and draft guards. |
| `/home/amir/Oppworx/Github/crmworx-api/crmworx-application/src/test/java/com/oppworx/crmworx/application/features/sequencestep/SequenceStepActionPayloadParserTest.java` | Required config fields and valid payload shapes per step action type. |
| `/home/amir/Oppworx/Github/crmworx-api/crmworx-application/src/test/java/com/oppworx/crmworx/application/features/automationruntime/TriggerAutomationEventCommandHandlerTest.java` | Trigger dedup, rule matching, execution creation. |
| `/home/amir/Oppworx/Github/crmworx-api/crmworx-application/src/test/java/com/oppworx/crmworx/application/features/automationruntime/ProcessPendingAutomationExecutionsCommandHandlerTest.java` | Execution claim, dispatch, success, retry, hard failure. |
| `/home/amir/Oppworx/Github/crmworx-api/crmworx-application/src/test/java/com/oppworx/crmworx/application/features/automationruntime/AutomationConditionEvaluatorTest.java` | Condition operator behavior for automation rule matching. |

#### Infrastructure tests

| File | What it proves |
|---|---|
| `/home/amir/Oppworx/Github/crmworx-api/crmworx-infrastructure/src/test/java/com/oppworx/crmworx/infrastructure/sequence/SequenceStepActionDispatcherAdapterTest.java` | Step dispatch maps to email enqueue, automation trigger, wait result, task command, and branch target behavior. |
| `/home/amir/Oppworx/Github/crmworx-api/crmworx-infrastructure/src/test/java/com/oppworx/crmworx/infrastructure/automation/OutboxAutomationExecutionDispatcherTest.java` | Automation actions become email outbox intents or integration outbox envelopes with action idempotency keys. |
| `/home/amir/Oppworx/Github/crmworx-api/crmworx-infrastructure/src/test/java/com/oppworx/crmworx/infrastructure/automation/AutomationExecutionSchedulerTest.java` | Scheduled automation processing delegates correctly when configured. |
| `/home/amir/Oppworx/Github/crmworx-api/crmworx-infrastructure/src/test/java/com/oppworx/crmworx/infrastructure/messaging/RabbitMQEmailDispatchPublisherTest.java` | Publisher confirms, broker nack, unroutable returns, priority routing, secret redaction. |
| `/home/amir/Oppworx/Github/crmworx-api/crmworx-infrastructure/src/test/java/com/oppworx/crmworx/infrastructure/messaging/RabbitMQEmailDispatchListenerTest.java` | Manual ACK, reject-to-DLQ, poison reject, unexpected-failure requeue. |
| `/home/amir/Oppworx/Github/crmworx-api/crmworx-infrastructure/src/test/java/com/oppworx/crmworx/infrastructure/messaging/RabbitMQEmailDispatchDeadLetterReplayListenerTest.java` | Replay versus parking decisions. |
| `/home/amir/Oppworx/Github/crmworx-api/crmworx-infrastructure/src/test/java/com/oppworx/crmworx/infrastructure/messaging/RabbitMQConfigTest.java` | Exchange/queue/binding/manual-ack listener container configuration. |

#### E2E / integration tests

| File | What it proves |
|---|---|
| `/home/amir/Oppworx/Github/crmworx-api/crmworx-api/src/test/java/com/oppworx/crmworx/api/integration/SequenceLifecycleE2EIT.java` | Sequence lifecycle through API-level integration. |
| `/home/amir/Oppworx/Github/crmworx-api/crmworx-api/src/test/java/com/oppworx/crmworx/api/integration/SequenceAutomationEmailChainE2EIT.java` | Full chain: create sequence -> trigger automation step -> publish sequence -> create enrollment -> process enrollment -> process automation execution -> publish email outbox -> RabbitMQ/SMTP delivery -> receipt verification. |
| `/home/amir/Oppworx/Github/crmworx-api/crmworx-api/src/test/java/com/oppworx/crmworx/api/integration/TenantEmailDeliveryE2EIT.java` | Tenant-aware email delivery behavior. |
| `/home/amir/Oppworx/Github/crmworx-api/crmworx-api/src/test/java/com/oppworx/crmworx/api/integration/SequenceEnrollmentControllerIT.java` | API-level enrollment operations. |
| `/home/amir/Oppworx/Github/crmworx-api/crmworx-api/src/test/java/com/oppworx/crmworx/api/integration/AutomationExecutionControllerIT.java` | API-level automation execution reads. |

### 19.13 Suggested Event sequence automation blueprint

For Event, a Clean Architecture adaptation could look like this.

#### Domain entities

```text
Sequence
SequenceRevision
SequenceStepRevision
SequenceEnrollment
AutomationRule
AutomationRuleRevision
AutomationEventReceipt
AutomationExecution
NotificationDispatchOutbox / EmailDispatchOutbox
EmailDispatchReceipt
EmailDeliveryAttempt
```

#### Application commands/queries

```text
CreateSequenceCommand
PublishSequenceCommand
CreateSequenceEnrollmentCommand
ProcessDueSequenceEnrollmentsCommand
AdvanceSequenceEnrollmentCommand
TriggerAutomationEventCommand
ProcessPendingAutomationExecutionsCommand
```

#### Ports

```text
ISequenceEnrollmentRepository
ISequenceRevisionReadRepository
ISequenceStepActionDispatcher
IAutomationEventReceiptRepository
IAutomationExecutionRepository
IAutomationExecutionDispatcher
IEmailDispatchRequestPort
ISequenceProcessingTelemetry
IAutomationExecutionTelemetry
IWorkflowProcessingThrottle
```

Repositories should return entities, per Event's rule. Handler code should map to DTOs when needed.

#### Handler flow

```text
ProcessDueSequenceEnrollmentsHandler
  -> repository.ListRunnableAsync(tenantId, now, batchSize)
  -> repository.TryAcquireLeaseAsync(...)
  -> load pinned revision steps
  -> dispatcher.DispatchAsync(currentStep, context)
  -> update current step / next run / completion
  -> save enrollment

TriggerAutomationEventHandler
  -> receipts.TryClaimAsync(...)
  -> ruleRepository.ListActiveByTriggerAsync(...)
  -> conditionEvaluator.Matches(...)
  -> executionRepository.AddAsync(Pending execution)

ProcessPendingAutomationExecutionsHandler
  -> repository.ListRunnablePendingAsync(...)
  -> repository.TryStartAsync(...)
  -> dispatcher.DispatchAsync(execution)
  -> mark Succeeded or Pending retry or Failed/DeadLettered

AutomationExecutionDispatcher
  -> SEND_EMAIL: emailDispatchRequestPort.EnqueueAsync(...)
  -> other action: integrationOutbox.EnqueueAsync(...)
```

#### Event-specific step types to consider

CRMWorx has CRM-flavored actions. Event can use the same engine shape with event-platform actions:

| Event step type | Example |
|---|---|
| `SEND_EMAIL` | Send attendee reminder, organizer digest, speaker onboarding email. |
| `WAIT_UNTIL` / `WAIT_FOR` | Wait until 24 hours before event start. |
| `TRIGGER_AUTOMATION_EVENT` | Trigger follow-up automation after RSVP, check-in, cancellation, refund, policy change. |
| `CREATE_TASK` | Create organizer task for venue, speakers, accessibility, moderation. |
| `BRANCH` | Route based on registration status, payment state, attendance, language, membership, ticket type. |
| `SEND_NOTIFICATION` | In-app notification, not only SMTP. |
| `WEBHOOK` | Only if backed by a specialized outbox with retries and signatures. |

### 19.14 Event implementation cautions from CRMWorx

1. **Do not run workflow steps against mutable draft definitions.** Pin every enrollment to a published revision.
2. **Do not collapse enrollment and automation execution into one row.** Enrollment tracks progression; execution tracks side effects.
3. **Do not use RabbitMQ as the source of truth for wait states.** Store waits in `NextRunAt`.
4. **Do not dispatch SMTP inside sequence progression.** Write an email/notification outbox row.
5. **Do not use one idempotency key for the whole chain.** CRMWorx uses progression keys, trigger dedup keys, execution/action idempotency keys, publish event IDs, and dispatch receipts at different boundaries.
6. **Do not forget tenant re-binding for internal workers.** Worker-originated commands still need explicit tenant context.
7. **Do not make condition/branch operators too powerful early.** Keep a safe vocabulary and test every operator.
8. **Do not hide failed enrollments by only clearing `NextRunAt`.** CRMWorx makes this visible via failure reason, but Event should consider an explicit `Failed`/`PausedForRepair` state.
9. **Do not route high-priority queue messages unless workers consume the priority queue too.** The earlier CRMWorx email analysis found priority routing is present but standard listener binding is the verified consumption path.
10. **Do not treat generic integration outbox as equivalent to email dispatch.** The CRMWorx email path is stronger because it has confirm/return checks and consume receipts.

### 19.15 Sequence automation test blueprint for Event

Event should mirror CRMWorx coverage in layers:

#### Unit tests

- `ProcessDueSequenceEnrollmentsHandlerTests`
  - skips when disabled
  - respects throttle
  - claims lease before dispatch
  - skips duplicate `LastProgressionKey`
  - completes when next step is past max step
  - schedules `NextRunAt` for wait step
  - retries transient dispatch failure with exponential backoff
  - records hard failure after max retries
- `SequenceStepActionDispatcherTests`
  - SEND_EMAIL writes email dispatch request with progression correlation and deterministic idempotency key
  - TRIGGER_AUTOMATION_EVENT sends MediatR command with tenant context and dedup key
  - WAIT returns wait result only; no side effect
  - BRANCH rejects unsupported operators and missing target step
- `TriggerAutomationEventHandlerTests`
  - duplicate receipt does not create execution
  - unmatched rules create no execution
  - matching active rule creates pending execution pinned to revision
  - stop-after-first-match behavior
- `ProcessPendingAutomationExecutionsHandlerTests`
  - claim before dispatch
  - mark succeeded on dispatcher success
  - retry with backoff on dispatch failure
  - failed/dead-lettered after max retries
- `AutomationExecutionDispatcherTests`
  - SEND_EMAIL uses email dispatch outbox, not RabbitMQ directly
  - non-email actions use integration outbox envelope
  - action idempotency key includes execution/action/index

#### Integration tests

- Repository conditional lease update under concurrent workers.
- Automation event receipt unique constraint under concurrent trigger attempts.
- Enrollment query only returns active due unlocked/expired-lock rows for current tenant.
- Automation execution query only returns due pending rows for current tenant/business scope.

#### E2E tests

- Create sequence with trigger automation step.
- Publish sequence revision.
- Enroll an event/registration/organizer aggregate.
- Process enrollment.
- Verify automation execution row created.
- Process automation execution.
- Verify email dispatch outbox row created.
- Publish outbox to RabbitMQ with confirm.
- Consume RabbitMQ message.
- Verify delivery receipt/attempt row and Mailpit/Mailhog delivery.

This is the CRMWorx `SequenceAutomationEmailChainE2EIT` pattern translated into Event's .NET/Clean Architecture world.

### 19.16 Additional evidence added for sequence automation expansion

- Collected focused sequence automation implementation agent output and focused sequence test inventory output.
- Verified `ProcessDueSequenceEnrollmentsCommandHandler.handle`, `advanceEnrollment`, and `handleProgressionFailure` directly.
- Verified `JpaSequenceEnrollmentRepository.tryAcquireLease` conditional update directly.
- Verified `SequenceStepActionDispatcherAdapter.dispatch`, `dispatchSendEmail`, `dispatchTriggerAutomationEvent`, `dispatchCreateTask`, and `evaluateBranch` directly.
- Verified `TriggerAutomationEventCommandHandler.handle` and `createPendingExecutions` directly.
- Verified `ProcessPendingAutomationExecutionsCommandHandler.handle` and `handleDispatchFailure` directly.
- Verified `OutboxAutomationExecutionDispatcher.dispatch`, `buildActionPayload`, and `buildEmailMessage` directly.
- Verified migration inventory for sequence tables, enrollment runtime fields, automation executions, automation event receipts, sequence revisions, and decomposed sequence step config columns.

## 20. EAV, Custom Fields, Flexible Data Modeling, and What Event Should Copy Carefully

CRMWorx does **not** have one simple "classic EAV everywhere" implementation. It has a hybrid flexible data strategy:

1. A live/runtime work-item custom-field path: `work_item_properties` -> `work_item_property_options` -> `work_item_property_values`.
2. An older/legacy custom-field family: `custom_field_defs` and `custom_field_values`.
3. A broader schema/domain model for template-driven custom properties: `custom_property_*`, `entity_template_custom_property_*`, `entity_custom_property_*`, and denormalized projection tables.
4. Explicit columns for concepts that became core product behavior, for example `V3_68__add_customer_followup_fields_to_work_items.sql` adding `kind`, `case_number`, `contact_id`, and `organization_id` to `work_items`.

That split is the biggest modeling lesson for Event: **use EAV/custom fields only for tenant-defined optional metadata, and promote high-value business concepts into explicit columns/aggregates once they drive workflows, indexes, authorization, or reporting.**

### 20.1 Exact CRMWorx files for the live work-item custom-field path

| Layer | Files | What they do |
|---|---|---|
| Schema docs | `/home/amir/Oppworx/Github/crmworx-api/schemas/oppworx-crmworx.md` | Current documented schema for work-item properties and custom-property projection tables. |
| Initial DDL | `/home/amir/Oppworx/Github/crmworx-api/crmworx-persistence/src/main/resources/db/migration/V2_12__create_work_item_properties.sql` | Original richer dynamic property model with `property_type`, typed value columns, validation JSON, multi-value flag, and hierarchical options. |
| Runtime alignment | `V2_47__align_work_item_properties_with_runtime_model.sql`, `V2_48__align_work_item_property_option_tables_with_runtime_model.sql` | Aligns DB shape to the current JPA runtime model. |
| Explicit-column alternative | `V3_68__add_customer_followup_fields_to_work_items.sql` | Moves core follow-up concepts into first-class `work_items` columns and indexes. |
| Domain | `WorkItemProperty.java`, `WorkItemPropertyOption.java`, `WorkItemPropertyValue.java` | Domain-side definition/option/value entities. |
| Application commands | `CreateWorkItemPropertyCommand.java`, `CreateWorkItemPropertyValueCommand.java`, option commands | Tenant-scoped command records with Jakarta validation. |
| Application handlers | `CreateWorkItemPropertyCommandHandler.java`, `UpdateWorkItemPropertyCommandHandler.java`, `CreateWorkItemPropertyValueCommandHandler.java`, `CreateWorkItemPropertyOptionCommandHandler.java` | Thin PipelinR handlers that construct domain entities and save through repository ports. |
| Persistence entities | `JpaWorkItemProperty.java`, `JpaWorkItemPropertyOption.java`, `JpaWorkItemPropertyValue.java` | Runtime JPA table mapping. |
| Read projection | `JpaWorkItemPropertyValueRepository.findCustomFieldProjections(...)`, `WorkItemReadPortAdapter.loadProjectionLookups(...)` | Batched custom-field projection assembly for work-item list/detail responses. |
| API | `WorkItemPropertyController.java`, `WorkItemPropertyOptionController.java`, `WorkItemPropertyValueController.java`, `WorkItemControllerIT.java` | CRUD surfaces plus integration proof that list/detail expose different custom-field tiers. |

### 20.2 Current work-item custom-field schema shape

The current schema documents this runtime shape:

```text
work_item_properties
  id uuid PK
  tenant_id uuid NOT NULL
  name varchar(200) NOT NULL
  field_type_id int
  entity_type varchar(100) NOT NULL
  is_system boolean NOT NULL
  is_required boolean NOT NULL
  is_active boolean NOT NULL
  sort_order int NOT NULL
  config_json text
  created_at timestamptz NOT NULL
  updated_at timestamptz

work_item_property_options
  id uuid PK
  work_item_property_id uuid NOT NULL
  tenant_id uuid NOT NULL
  label varchar(255) NOT NULL
  value varchar(255) NOT NULL
  color varchar(50)
  sort_order int NOT NULL
  is_active boolean NOT NULL DEFAULT true
  parent_id uuid
  created_at timestamptz NOT NULL
  updated_at timestamptz
  index: (work_item_property_id, sort_order)

work_item_property_values
  id uuid PK
  work_item_property_id uuid NOT NULL
  work_item_id uuid NOT NULL
  tenant_id uuid NOT NULL
  value_text text
  value_option_id uuid
  created_at timestamptz NOT NULL
  updated_at timestamptz
  unique index: (work_item_id, work_item_property_id)
```

CRMWorx therefore gives each work item at most one value row per custom property. It supports either a free-text value (`value_text`) or a selected option (`value_option_id`). That is simpler than the original `V2_12` table, which had separate typed value columns (`value_number`, `value_boolean`, `value_date`, `value_user_id`) and flags like `is_multi`.

For Event, that tradeoff matters. A first implementation should likely start simple, but Event should not rely on conventions alone. If Event needs custom registration questions, attendee attributes, organizer metadata, speaker profile fields, or venue-specific fields, the value table should enforce one of these shapes:

```text
CustomFieldDefinition
CustomFieldOption
CustomFieldValue
CustomFieldProjection
```

But Event should add domain validation that CRMWorx only partially enforces:

- A select/multiselect field must reference valid options for the same tenant and definition.
- A text/textarea field must not set `ValueOptionId`.
- A required field must be present before a workflow advances if that workflow depends on it.
- A custom field used in filtering should have a normalized projection row.
- Option `value` should be unique per property if used in APIs, automations, or exports.

### 20.3 Live write flow: thin handlers and repository ports

`CreateWorkItemPropertyCommandHandler.handle(...)` is intentionally thin:

```java
@Transactional
public BaseCommandResponse<UUID> handle(CreateWorkItemPropertyCommand command) {
    var entity = new WorkItemProperty(
            uuidGenerator.generate(),
            command.tenantId(),
            command.name(),
            command.fieldTypeId(),
            command.entityType(),
            command.isSystem(),
            command.isRequired(),
            command.isActive(),
            command.sortOrder(),
            command.configJson(),
            null, null
    );
    var saved = repository.save(entity);
    return CommandResponseFactory.created(saved.getId(), "WorkItemProperty");
}
```

`CreateWorkItemPropertyValueCommandHandler.handle(...)` mirrors the same pattern:

```java
@Transactional
public BaseCommandResponse<UUID> handle(CreateWorkItemPropertyValueCommand command) {
    var entity = new WorkItemPropertyValue(
            uuidGenerator.generate(),
            command.workItemPropertyId(),
            command.workItemId(),
            command.tenantId(),
            command.valueText(),
            command.valueOptionId(),
            null, null
    );
    var saved = repository.save(entity);
    return CommandResponseFactory.created(saved.getId(), "WorkItemPropertyValue");
}
```

This follows Clean Architecture: controllers send commands, handlers create domain entities, persistence adapters map to JPA. The weakness is that value-type correctness is not visible in the handler. The handler accepts both `valueText` and `valueOptionId` as passed.

For Event, keep the good part but strengthen the boundary:

```text
CreateCustomFieldValueHandler
  -> manually instantiate validator
  -> load CustomFieldDefinition entity
  -> validate tenant, aggregate ownership, field type, required rules, option membership
  -> create/update CustomFieldValue entity
  -> update CustomFieldProjection row when searchable/filterable
```

Event repositories must return entities, not DTOs. So the write path should return `CustomFieldDefinition`, `CustomFieldOption`, and `CustomFieldValue` entities to handlers, then map to DTOs in handlers/API assemblers.

### 20.4 Read model: batched projection instead of N+1 EAV loading

CRMWorx's best EAV implementation idea is on the read side. `JpaWorkItemPropertyValueRepository.findCustomFieldProjections(...)` uses one native query for a batch of work items:

```sql
select
  pv.work_item_id as workItemId,
  p.id as workItemPropertyId,
  p.name as propertyKey,
  coalesce(p.display_name, p.name) as propertyLabel,
  p.field_type_id as fieldTypeId,
  p.sort_order as sortOrder,
  p.is_required as required,
  pv.value_text as valueText,
  pv.value_option_id as valueOptionId,
  o.label as optionLabel,
  o.value as optionValue,
  o.color as optionColor
from work_item_property_values pv
join work_item_properties p on p.id = pv.work_item_property_id
left join work_item_property_options o on o.id = pv.value_option_id
where pv.tenant_id = :tenantId
  and pv.work_item_id in (:workItemIds)
  and p.is_active = true
  and p.is_system = false
  and (p.entity_type is null or p.entity_type = 'work_item')
order by p.sort_order asc, p.name asc
```

`WorkItemReadPortAdapter.loadProjectionLookups(...)` then loads all related reference data and custom fields in batches, groups custom fields by work item ID, and maps them into response DTOs. Crucially, it has a detail/list tier:

```java
filter(projection -> includeDetailCustomFields || isSummarySafeFieldType(projection.getFieldTypeId()))
```

Then `toCustomFieldDto(...)` resolves display/value pairs:

```java
String displayValue = projection.getOptionLabel() != null ? projection.getOptionLabel() : projection.getValueText();
String value = projection.getOptionValue() != null ? projection.getOptionValue() : projection.getValueText();
```

`WorkItemControllerIT.getById_returnsFullCustomFieldsWhileListReturnsProjectedSubset()` proves the behavior:

- Detail response includes both `audience` and `admin_notes`.
- List response only includes `audience` because it is a summary-safe select field.

For Event, this is directly useful for attendee/registration/event custom attributes:

- Detail screens can return the complete custom-field set.
- List/search screens should return only whitelisted summary-safe fields.
- Filtering/search should use denormalized projection rows, not ad hoc joins from every list endpoint.
- API/HAL responses should decide whether a custom field is visible/editable through policy/link affordances, not local UI role checks.

### 20.5 Custom-property projection tables: schema-first idea, not fully wired runtime

The broader CRMWorx schema includes richer projection tables:

```text
custom_property_projections
  custom_property_definition_id
  custom_property_value_id unique
  entity_type
  entity_id
  tenant_id
  business_scope_id
  namespace
  key
  property_type
  exposure_level
  is_searchable
  is_filterable
  is_exportable
  is_analytics_relevant
  ordinal
  option_id
  text_value / number_value / boolean_value / date_time_value
  normalized_value
  indexes:
    (business_scope_id, entity_type, entity_id, namespace, key, ordinal)
    (business_scope_id, namespace, key, normalized_value)

entity_custom_property_projections
  same read-model idea for instantiated template properties
```

The schema explicitly says these projections are **not source of truth**. That is the correct pattern. The source of truth should be definition/value rows; projections exist for query performance, exports, analytics, and list filters.

The important caveat: the focused analysis found these richer `custom_property_*` and `entity_custom_property_*` families are schema/domain-heavy, with no clearly wired Application/Persistence runtime flow comparable to the work-item custom-field path. Event should treat this as a design idea, not a copy-paste implementation.

### 20.6 Template-driven metadata: useful for Event forms and recurring event setups

The domain model includes template concepts:

- `EntityTemplateCustomPropertyDefinition.java`
- `EntityTemplateCustomPropertyOption.java`
- `EntityCustomPropertyDefinition.java`
- `EntityCustomPropertyOption.java`
- `EntityCustomPropertyValue.java`
- `EntityCustomPropertyProjection.java`

The pattern is valuable even if CRMWorx has not fully wired every runtime path:

```text
Template definition
  -> template options
  -> instantiated entity definition
  -> instantiated entity options
  -> entity value
  -> denormalized projection
```

For Event, this maps naturally to:

- reusable event registration form templates;
- recurring event templates;
- tenant-specific attendee profile extensions;
- speaker/sponsor/vendor application forms;
- moderation/intake workflows;
- event-type-specific metadata without adding columns for every tenant request.

If Event implements this, the key fields to copy conceptually are source-template lineage and sync metadata:

```text
SourceTemplateDefinitionId
SourceTemplateOptionId
InstantiatedAt
LastSyncedFromTemplateAt
TemplateVersion
```

That gives operators a way to answer: "Did this field come from a template, was it customized, and can we safely sync it?"

### 20.7 EAV filtering: subquery filters and cache fragments

CRMWorx includes EAV-aware work-item filters in `WorkItemSubqueryFilter.java`. Example: `hasPropertyOption(UUID propertyId, UUID optionId)` builds a subquery equivalent to:

```sql
WHERE id IN (
  SELECT work_item_id
  FROM work_item_property_values
  WHERE work_item_property_id = ?
    AND value_option_id = ?
)
```

It also emits a cache fragment like:

```text
prop:{propertyId}:{optionId}
```

That is a practical pattern for Event's specification-driven query model. If Event adds custom registration/attendee/event fields, specifications should be immutable and cacheable:

```text
EventRegistrationSpecification
  .WithCustomOption(fieldId, optionId)
  .WithCustomNormalizedValue(fieldId, value)
  .WithRequiredFieldMissing(fieldId)
```

But Event should avoid making every EAV filter a runtime SQL join. For frequently queried fields, promote them into projection rows with `(TenantId, BusinessScopeId/EventId, FieldKey, NormalizedValue)` indexes.

### 20.8 Modeling decision rule for Event

Use this rule before adding an Event field:

| Need | Model as |
|---|---|
| Drives authorization, workflow state, tenant isolation, billing, HAL links, or frequent indexes | Explicit aggregate property/column |
| Tenant-defined optional attribute, shown mostly on detail screens | Custom field definition/value |
| Tenant-defined attribute used in search/filter/export/analytics | Definition/value plus projection row |
| Reusable form shape across event types or tenants | Template definition -> instantiated definition/value |
| High-volume list summary field | Either explicit column or summary-safe projection |
| Side-effect trigger condition | Custom field only if type-safe and projection-backed |

The CRMWorx `V3_68__add_customer_followup_fields_to_work_items.sql` migration is the clearest warning: when follow-up metadata became central, it moved to explicit columns with tenant-scoped indexes. Event should do the same for concepts like event date, registration state, payment state, attendance/check-in state, capacity, publish state, and organizer ownership.

### 20.9 EAV pitfalls found in CRMWorx that Event should avoid

1. **Multiple names for similar concepts.** CRMWorx has `custom_field`, `work_item_property`, and `custom_property` families. Event should pick one vocabulary early: for example `CustomFieldDefinition`, `CustomFieldOption`, `CustomFieldValue`, `CustomFieldProjection`.
2. **Magic numeric field types.** CRMWorx summary-safe behavior is based on numeric `field_type_id` values and a map in `WorkItemReadPortAdapter`. Event should use lookup rows plus a domain enum/value object in Application.
3. **Weak value-shape validation.** CRMWorx handlers accept `valueText` and `valueOptionId`. Event should validate mutual exclusivity, requiredness, option ownership, field type, and tenant ownership before saving.
4. **Schema ahead of runtime.** The richer `custom_property_*` schema is useful, but not fully wired like the work-item path. Event should not document toggles/tables as operational until handlers, repositories, tests, and runbooks exist.
5. **Missing obvious option uniqueness.** The searched work-item option schema/indexes show sorting indexes, but no clear uniqueness for option `value` per property. Event should add it if API values drive automation or imports.
6. **Read-model leakage risk.** Projection tables must remain read models. Do not edit projections directly; rebuild/update them from source-of-truth values transactionally.
7. **Tenant isolation must be layered.** CRMWorx passes tenant IDs through commands/repositories and uses tenant filters; Event should also keep EF query filters active and avoid disabling tenant filters for custom-field queries.

## 21. Testing and E2E Strategy: What Event Can Benefit From CRMWorx

CRMWorx's testing strategy is valuable because it separates speed, fidelity, and risk. It does not rely on one giant "integration test" category. It uses explicit lanes for unit, integration, E2E, critical E2E, contract, coverage, persistence migration, architecture, and performance.

### 21.1 Maven test lanes that Event can mirror conceptually

`crmworx-api/pom.xml` defines separate profiles:

| Profile | Evidence | Purpose |
|---|---|---|
| `integration-test` | includes `**/*IT.java`, excludes `**/*E2EIT.java`, `spring.profiles.active=test` | API integration lane without full E2E external-service scope. |
| `e2e-test` | includes `**/*E2EIT.java`, `spring.profiles.active=e2e` | Full E2E lane. |
| `e2e-critical-test` | includes selected critical E2E tests: JWT, Cerbos auth, opportunity lifecycle, tenant email delivery | Smaller high-signal release lane. |
| `contract-test` | includes `**/*PactVerificationTest.java` | Provider/consumer contract verification. |
| `openapi-export` | runs `OpenApiSpecGeneratorIT` with schema snapshot settings | Keeps OpenAPI export deterministic. |
| `coverage-gates` | JaCoCo class-level risk coverage checks | Makes risky areas fail build if coverage drops. |

For Event, the equivalent could be:

```text
Unit lane
Application unit lane
Persistence integration lane
API integration lane
Critical E2E lane
Full E2E lane
Architecture/context lane
Docs/OpenAPI lane
Performance smoke lane
```

Event already has architecture/context testing. The CRMWorx lesson is to keep a **critical E2E lane** small enough to run often but realistic enough to catch auth, tenant, async, and external-service regressions.

### 21.2 Shared test fixtures and container infrastructure

CRMWorx centralizes test infrastructure:

| File | Pattern |
|---|---|
| `BaseIntegrationTest.java` | Shared API integration base with tenant creation, business-scope creation, user setup, SQL helpers, and deterministic `tenantJwt(...)`. |
| `BaseE2EIntegrationTest.java` | E2E base with real Keycloak/Cerbos setup and readiness checks. |
| `SharedContainers.java` | Lazy singleton Testcontainers for PostgreSQL, Redis, Keycloak, Cerbos, RabbitMQ, and MailHog. |
| `DynamicPropertyRegistrySupport.java` | Registers lane-specific Spring properties so tests do not hardcode container ports. |
| `KeycloakTestClient.java` | Acquires real tokens for E2E auth tests. |
| `CerbosTestSupport.java` | Probes PDP readiness before E2E tests run. |

Directly verified examples:

```java
protected static RequestPostProcessor tenantJwt(UUID tenantId, UUID userId) {
    return jwt().jwt(j -> j
            .subject(userId.toString())
            .claim("tenant_id", tenantId.toString()));
}
```

`BaseE2EIntegrationTest.configureProperties(...)` starts Keycloak/Cerbos and delegates dynamic property registration:

```java
@DynamicPropertySource
static void configureProperties(DynamicPropertyRegistry registry) {
    SharedContainers.keycloak();
    SharedContainers.cerbos();
    DynamicPropertyRegistrySupport.registerE2eLaneProperties(registry);
}
```

`SharedContainers.rabbitMq()` and `mailhog()` are lazy singleton containers with exposed ports and startup timeouts.

For Event/Aspire, the .NET translation is:

- Testcontainers for PostgreSQL, Redis, RabbitMQ, Mailpit/MailHog, and any local policy/auth emulator when practical.
- A shared integration base that creates tenant, user, organization/business-scope fixtures.
- JWT helper methods for deterministic integration tests.
- A separate E2E base that uses real identity/policy services where the risk justifies it.
- Dynamic configuration wiring rather than hardcoded ports.

### 21.3 EAV/custom-field tests worth copying

The key custom-field test is `WorkItemControllerIT.getById_returnsFullCustomFieldsWhileListReturnsProjectedSubset()`.

It proves a product behavior, not just a repository query:

1. Seed tenant, user, business scope, work-item type, and status.
2. Create a work item.
3. Insert a select custom field `audience` and an option `Families`/`families`.
4. Insert a textarea custom field `admin_notes`.
5. Store both values.
6. GET detail endpoint and assert both custom fields are present.
7. GET list endpoint and assert only the summary-safe field is present.

This is exactly the test shape Event needs if it adds custom registration/attendee/event attributes:

```text
CustomFieldControllerIT / RegistrationControllerIT
  detail_returnsAllVisibleCustomFields
  list_returnsOnlySummarySafeCustomFields
  list_filtersByCustomOptionUsingProjection
  hiddenOrPolicyDeniedField_isNotReturned
  tenantCannotReadAnotherTenantCustomFieldDefinitionOrValue
```

Also useful: `WorkItemApiMapperTest` verifies API mapping copies custom field response values. Event should keep mapper/assembler tests around HAL responses so custom-field values do not accidentally bypass link/permission rules.

### 21.4 Workflow/RabbitMQ/SMTP E2E tests as an Event pattern

`SequenceAutomationEmailChainE2EIT.sequenceTrigger_toAutomationExecution_toRabbitMqToSmtp_deliversEmail()` is the strongest end-to-end async test pattern in CRMWorx. It verifies the full runtime chain:

```text
prepare tenant/admin scenario
upsert and enable platform email profile
create automation rule
create sequence
create trigger automation step
publish sequence
create contact
create enrollment
process due sequence enrollments
assert enrollment completed
process automation executions
assert succeeded automation execution exists
publish pending email outbox entries
wait for delivery completion
assert publishStatus=published
assert deliveryStatus=sent
assert receiptStatus=completed
assert attemptCount >= 1
assert MailHog contains recipient
```

This is the testing model Event should use for high-value workflows:

```text
EventAutomationEmailChainE2EIT
  create event template / automation rule
  publish event or registration workflow
  enroll attendee/organizer/speaker aggregate
  process sequence/automation handlers
  publish notification/email outbox
  consume RabbitMQ
  assert delivery receipt, attempt row, and Mailpit message
```

The test should assert database state at every boundary, not only final email arrival. That is what makes async failures diagnosable.

### 21.5 Tenant, auth, HAL, and architecture guardrails

The testing agent identified and direct outlines confirmed guardrail classes:

| Test | What Event can copy |
|---|---|
| `TenantPathResolutionIT.java` | Test canonical tenant path, JWT fallback, and membership fallback. |
| `TenantRlsEnforcementIT.java` | Prove database-level tenant isolation, not just API filtering. |
| `JwtValidationE2EIT.java` | Real issuer/audience/expiry validation against Keycloak. |
| `AuthorizationCerbosE2EIT.java` | Live policy allow/deny plus HATEOAS affordance behavior. |
| `HalSerializationGoldenTest.java` | Golden tests for HAL envelopes. |
| `LinkPolicyParityTest.java` | Guarded links must correspond to policy/action definitions. |
| `ApiArchitectureTest.java` | Layer dependency rules: API/Application/Domain/Persistence boundaries. |
| `ControllerConventionsTest.java` | Controllers cannot return domain entities, import domain entities, or build pipeline commands inline. |
| `AssemblerGuardrailTest.java` | HATEOAS assembler authorization boundaries. |

This aligns strongly with Event's existing rule that HAL links are the UI affordance source of truth. Event should add custom-field-specific HAL tests when metadata becomes editable:

```text
Custom field visible but not editable -> no edit link
Custom field definition locked by governance -> no delete link
Registration answer editable until deadline -> edit-answer link present
Policy outage -> protected links fail closed
```

### 21.6 Persistence and migration tests

CRMWorx has persistence-focused tests worth emulating:

- `PersistenceAdapterIntegrationTestBase.java`: shared PostgreSQL + Flyway JPA harness.
- `FlywayMigrationChainIT.flywayHistory_hasNoFailuresAndLatestVersionApplied()`.
- `FlywayMigrationChainIT.tenantScopedIndexes_existForCriticalTables()`.
- `FlywayMigrationChainIT.sequenceSteps_runtimeInsertKeepsLegacyStepNumberCompatible()`.
- `ProductionLikeMigrationRehearsalIT.migrateFromPreviousVersion_preservesTenantCustomerAndWorkflowData()`.

The migration rehearsal pattern is especially valuable for Event if EAV/custom-field tables are introduced. EAV schemas tend to evolve: field types, projections, required flags, option uniqueness, normalized values, and template lineage all change over time. Event should test migrations with seeded production-like records:

```text
tenant
event
registration
custom field definitions
custom options
custom values
projection rows
automation rule using a custom field
outbox row referencing an aggregate with custom data
```

Then migrate and assert:

- values are preserved;
- projections are rebuilt or still valid;
- unique constraints do not break existing tenants;
- tenant filters still isolate rows;
- automation conditions still evaluate.

### 21.7 RabbitMQ and SMTP testing layers

CRMWorx tests messaging at multiple levels:

| Level | Tests | What they prove |
|---|---|---|
| Unit/config | `RabbitMQConfigTest.java` | Exchange/queue/binding/manual-ack listener factory wiring. |
| Publisher behavior | `RabbitMQEmailDispatchPublisherTest.java` | Confirms, nacks, unroutable returns, priority routing, secret redaction. |
| Listener behavior | `RabbitMQEmailDispatchListenerTest.java` | ACK, reject-to-DLQ, poison reject, unexpected-failure requeue. |
| DLQ behavior | `RabbitMQEmailDispatchDeadLetterReplayListenerTest.java` | Replay versus parking decisions. |
| Real broker | `RabbitMQOutboxRelayClientContainerTest.java`, `RabbitMQDeadLetterContainerTest.java` | Container-backed RabbitMQ behavior. |
| Real SMTP | `SmtpEmailOutboxRelayClientContainerTest.java`, `TenantEmailDeliveryE2EIT.java` | MailHog/Mailpit delivery and receipt path. |

Event should mirror this for any queue-first notification/email workflow:

```text
Publisher unit tests
  confirm ack -> publish success
  broker nack -> retry scheduled
  unroutable return -> retry scheduled / config failure visible

Consumer unit tests
  valid event -> DB receipt completed + ACK
  tenant mismatch -> reject to DLQ
  duplicate receipt -> ACK no-op
  unexpected exception -> nack/requeue or DB retry depending on design

Container tests
  topology declares expected exchange/queues/DLQ
  unroutable message reaches return path
  DLQ replay parks already-sent/mismatched messages

E2E tests
  workflow -> outbox -> RabbitMQ -> consumer -> SMTP -> receipt/attempt row
```

### 21.8 Testing gaps and cautions found in CRMWorx

The CRMWorx testing approach is strong, but the analysis surfaced gaps Event can avoid:

1. Work-item custom fields have controller/spec/mapper coverage, but the agent did not find a dedicated `WorkItemReadPortAdapterIT` proving custom-field projection against PostgreSQL through the adapter.
2. The live work-item value handler does not visibly enforce field-type compatibility. Event should add handler/unit tests for type/option validation.
3. The richer `custom_property_*` schema/domain model lacks comparable runtime tests because it does not appear wired end-to-end like work-item properties.
4. Performance tests are baseline/harness-oriented rather than hard release gates. Event should at least gate severe regressions for hot list endpoints using custom-field filters.
5. E2E depth is strongest around auth, tenanting, automation, and email; not every CRUD area has a full-stack E2E test. Event should pick critical user journeys rather than trying to E2E everything.

### 21.9 Concrete Event test blueprint for EAV/data modeling

If Event adds custom fields or template-driven metadata, copy CRMWorx's layered strategy but strengthen validation:

#### Unit tests

- `CreateCustomFieldDefinitionHandlerTests`
  - validates key/name/type/tenant/business scope;
  - rejects duplicate key in same namespace/scope;
  - rejects system/governed field edits unless policy allows.
- `CreateCustomFieldValueHandlerTests`
  - text field rejects option ID;
  - select field requires option ID and rejects foreign-tenant option;
  - required field missing blocks workflow transition when configured;
  - normalized projection value is computed for searchable fields.
- `CustomFieldSpecificationTests`
  - immutable filter composition;
  - stable cache keys;
  - custom option/value filters preserve tenant predicate.

#### Persistence integration tests

- definition/value/projection round trip in PostgreSQL;
- tenant EF query filters remain active;
- unique constraints protect `(TenantId, Namespace, Key)` and option values;
- projections support list filtering without N+1 reads;
- migration rehearsal preserves custom values and projection rows.

#### API integration tests

- detail endpoint returns all visible custom fields;
- list endpoint returns only summary-safe fields;
- HAL edit/delete links respect policy/governance locks;
- cross-tenant definition/value access returns forbidden/not found according to Event's existing API policy;
- validation errors return RFC 7807 with stable field codes.

#### E2E tests

- create event/registration form template;
- instantiate event from template;
- submit registration with custom answers;
- run automation conditioned on a custom field;
- enqueue notification/email;
- RabbitMQ consume + SMTP delivery;
- assert DB receipt/attempt/projection state and Mailpit delivery.

This ties the EAV/data-modeling work directly to Event's automation focus instead of treating custom fields as passive metadata.

### 21.10 Additional evidence added for EAV/testing expansion

- Collected focused EAV/data-modeling agent output and focused testing/E2E agent output.
- Verified current schema sections for `work_item_properties`, `work_item_property_options`, `work_item_property_values`, `custom_property_projections`, and `entity_custom_property_projections` directly in `schemas/oppworx-crmworx.md`.
- Verified initial and follow-up migrations: `V2_12__create_work_item_properties.sql`, `V2_47__align_work_item_properties_with_runtime_model.sql`, `V2_48__align_work_item_property_option_tables_with_runtime_model.sql`, `V2_21__create_custom_fields.sql`, and `V3_68__add_customer_followup_fields_to_work_items.sql`.
- Verified `CreateWorkItemPropertyCommandHandler.handle`, `CreateWorkItemPropertyValueCommandHandler.handle`, `JpaWorkItemPropertyValueRepository.findCustomFieldProjections`, `WorkItemReadPortAdapter.loadProjectionLookups`, and `WorkItemReadPortAdapter.toCustomFieldDto` directly.
- Verified `WorkItemControllerIT.getById_returnsFullCustomFieldsWhileListReturnsProjectedSubset`, `BaseIntegrationTest.tenantJwt`, `BaseE2EIntegrationTest.configureProperties`, `SharedContainers.rabbitMq`, and `SequenceAutomationEmailChainE2EIT.sequenceTrigger_toAutomationExecution_toRabbitMqToSmtp_deliversEmail` directly.
- Verified CRMWorx API Maven lanes for integration, E2E, critical E2E, contract, OpenAPI export, and coverage gates directly in `crmworx-api/pom.xml`.
