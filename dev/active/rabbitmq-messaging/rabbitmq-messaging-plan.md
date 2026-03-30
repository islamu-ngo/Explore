# Message Queue Provider Abstraction — Implementation Plan

Last Updated: 2026-03-29

## Executive Summary

Build a **provider-agnostic message queue abstraction** — the same pattern used for analytics (`IAnalyticsProvider` → `RuntimeAnalyticsProvider`), secrets (`ISecretProvider` → factory), and feature flags. Self-hosters choose their message queue: **RabbitMQ (default, included in docker-compose)**, Kafka, NATS, Azure Service Bus, Redis Streams, etc. The abstraction is powered by **MQContract** — a .NET library that already provides a unified API over 14+ message queue backends.

MQContract is used to its fullest: publish, subscribe, consumer registration, middleware (OpenTelemetry, compression), resilience policies, and the query/response pattern. The existing `LoggingOutboxMessageDispatcher` is replaced by a real dispatcher that publishes through the resolved message queue provider. Consumer registration enables the API to also *receive* messages (e.g., cross-service events, webhook relay).

**This is a breaking change.** We're in development — no backwards compatibility concerns.

---

## Current State Analysis

### Existing Provider Abstraction Patterns (to mirror)

| Concern | Application Interface | Config Resolver | Runtime Provider | Governance Keys | Enum |
|---------|----------------------|-----------------|------------------|-----------------|------|
| Analytics | `IAnalyticsProvider` | `IAnalyticsConfigResolver` | `RuntimeAnalyticsProvider` | `analytics.*` | `AnalyticsProviderEnum` |
| Secrets | `ISecretProvider` | `SecretProviderFactory` | Factory-resolved | `SecretProvider:*` | `SecretProviderType` |
| Feature Flags | `IFeatureFlagService` | (wraps OpenFeature) | `OpenFeatureFlagService` | — | — |
| Authorization | `IAuthorizationProvider` | (settings-driven) | `RuntimeAuthorizationProvider` | `authorization.*` | — |
| Translation | `ITranslationManagementProvider` | `ITranslationConfigResolver` | `RuntimeTranslationProvider` | `localization.*` | — |
| **Message Queue** | **TODO** | **TODO** | **TODO** | **TODO** | **TODO** |

### Current Outbox Infrastructure
- `OutboxMessage` entity → DB transaction → `OutboxProcessor` (5s poll) → `IOutboxMessageDispatcher` → `LoggingOutboxMessageDispatcher` (no-op)
- Retry with exponential backoff, dead-letter after max retries
- Optimistic locking for multi-worker safety

### Key Files to Modify/Replace

| File | Action |
|------|--------|
| `Explore.Application/Contracts/Infrastructure/IOutboxMessageDispatcher.cs` | Keep — dispatcher uses resolved provider |
| `Explore.Infrastructure/Outbox/LoggingOutboxMessageDispatcher.cs` | Delete — replaced by real dispatcher |
| `Explore.Infrastructure/InfrastructureServicesRegistration.cs` | Add messaging DI |
| `Explore.Domain/Constants/GovernanceSettingKeys.cs` | Add `Messaging.*` keys |
| `Explore.Domain/Enums/` | Add `MessageQueueProviderEnum` |
| `Explore.API/BackgroundServices/OutboxProcessor.cs` | No change — already calls `IOutboxMessageDispatcher` |
| `Explore.API/Program.cs` | Register health checks, Aspire RabbitMQ |

---

## Proposed Architecture

### Layer Map (Clean Architecture)

```
Explore.Domain
  └─ MessageQueueProviderEnum (None=0, RabbitMQ=1, Kafka=2, NATS=3, AzureServiceBus=4, Redis=5)
  └─ GovernanceSettingKeys.Messaging.* (provider, enabled, endpoint, etc.)

Explore.Application
  └─ Contracts/Infrastructure/IMessageBrokerProvider.cs   ← provider-agnostic publish/subscribe
  └─ Contracts/Infrastructure/IMessageBrokerConfigResolver.cs
  └─ Models/MessagingConfiguration.cs
  └─ IntegrationEvents/  ← typed integration event contracts

Explore.Infrastructure/Messaging/
  └─ Config/
  │   └─ MessageBrokerConfigResolver.cs     ← resolves from governance settings
  │   └─ MessageBrokerSettings.cs           ← static fallback config
  ├─ Providers/
  │   ├─ RuntimeMessageBrokerProvider.cs    ← delegates to resolved provider (like RuntimeAnalyticsProvider)
  │   ├─ RabbitMqMessageBrokerProvider.cs   ← MQContract + RabbitMQ connector
  │   ├─ NullMessageBrokerProvider.cs       ← no-op (like NullAnalyticsProvider)
  │   └─ (future: KafkaMessageBrokerProvider, NatsMessageBrokerProvider, etc.)
  ├─ Consumers/
  │   └─ (MQContract consumer registrations)
  ├─ Middleware/
  │   └─ (MQContract middleware: OpenTelemetry, compression)
  ├─ MqContractOutboxDispatcher.cs          ← IOutboxMessageDispatcher → publishes via IMessageBrokerProvider
  ├─ MessagingHealthCheck.cs
  └─ MessagingMetrics.cs
```

### Runtime Flow

```
                    ┌─────────────────────────────────────────────┐
                    │  GovernanceSettingKeys.Messaging.*           │
                    │  messaging.provider = "rabbitmq"            │
                    │  messaging.enabled = true                   │
                    │  messaging.endpoint = "amqp://..."          │
                    └──────────────┬──────────────────────────────┘
                                   │
                    ┌──────────────▼──────────────────────────────┐
                    │  IMessageBrokerConfigResolver.ResolveAsync() │
                    │  (hierarchical: system → tenant override)    │
                    └──────────────┬──────────────────────────────┘
                                   │
                    ┌──────────────▼──────────────────────────────┐
                    │  RuntimeMessageBrokerProvider                │
                    │  switch(config.Provider)                     │
                    │    RabbitMQ → RabbitMqMessageBrokerProvider  │
                    │    None     → NullMessageBrokerProvider      │
                    │    Kafka    → (future)                       │
                    └──────────────┬──────────────────────────────┘
                                   │
              ┌────────────────────┼────────────────────┐
              │                    │                     │
    ┌─────────▼─────────┐  ┌──────▼──────┐  ┌──────────▼──────────┐
    │ OutboxProcessor    │  │ Consumers   │  │ Query/Response      │
    │ → DispatchAsync()  │  │ (subscribe) │  │ (request/reply)     │
    │ → PublishAsync()   │  │             │  │                     │
    └───────────────────┘  └─────────────┘  └─────────────────────┘
```

### Self-Hoster Experience

```jsonc
// appsettings.json — self-hoster picks their MQ provider
{
  "Messaging": {
    // Static fallback when governance DB not available (startup bootstrap)
    "Provider": "rabbitmq",     // or "kafka", "nats", "none"
    "RabbitMQ": {
      "HostName": "rabbitmq",
      "Port": 5672,
      "UserName": "guest",
      "Password": "guest",
      "VirtualHost": "/"
    }
  }
}
```

```
// Governance settings (DB) — instance admin can lock or allow tenant override
messaging.provider = "rabbitmq"
messaging.enabled = true
messaging.endpoint = "amqp://rabbitmq:5672"
messaging.exchange_prefix = "islamu.event"
```

---

## MQContract — Full Usage

### What We Use

| Feature | Usage |
|---------|-------|
| **ContractConnection** | Core publish/subscribe facade |
| **RabbitMQ Connector** | Default provider (`MQContract.RabbitMQ.Connection`) |
| **InMemory Connector** | Testing + `NullMessageBrokerProvider` when `messaging.provider=none` |
| **`[Message]` attribute** | Channel routing and message type identification |
| **Consumer registration** | `RegisterPubSubConsumerAsync<T>` for incoming integration events |
| **Middleware: OpenTelemetry** | Distributed tracing across message boundaries |
| **Middleware: Compression** | GZip for large payloads |
| **Resilience policies** | Retry + circuit breaker on publish (complementary to outbox retry) |
| **Message versioning** | Version attribute on integration event contracts |
| **PingAsync** | Health check connectivity verification |
| **Bulk publish** | `BulkPublishAsync` for batch outbox processing |

### NuGet Packages

| Package | Project | Purpose |
|---------|---------|---------|
| `MQContract` | `Explore.Infrastructure` | Core contract connection |
| `MQContract.Abstractions` | `Explore.Application` | `[Message]` attribute for integration events |
| `MQContract.RabbitMQ` | `Explore.Infrastructure` | RabbitMQ connector |
| `MQContract.InMemory` | `Explore.Infrastructure` + test projects | Testing + NullProvider |
| `Aspire.Hosting.RabbitMQ` | `Explore.AppHost` | Local dev container |

---

## Implementation Phases

### Phase 1: Domain & Application Contracts (Effort: M)

**Goal:** Define the provider abstraction in Domain + Application layers.

#### Task 1.1: Add `MessageQueueProviderEnum`
- **File**: `Explore.Domain/Enums/MessageQueueProviderEnum.cs`
- **Acceptance**:
  - [ ] `None=0, RabbitMQ=1, Kafka=2, NATS=3, AzureServiceBus=4, Redis=5`
  - [ ] ABOUTME header, file-scoped namespace
- **Effort**: S
- **Skill**: `clean-architecture-rules`

#### Task 1.2: Add Governance Setting Keys
- **File**: `Explore.Domain/Constants/GovernanceSettingKeys.cs`
- **Action**: Add `Messaging` nested class
- **Keys**:
  - `messaging.provider` — provider name (string → enum)
  - `messaging.enabled` — master enable switch
  - `messaging.endpoint` — broker endpoint URL / connection string
  - `messaging.exchange_prefix` — prefix for exchange/topic naming
  - `messaging.username` — broker auth (non-secret, for governance display)
  - `messaging.virtual_host` — RabbitMQ vhost / Kafka cluster ID
- **Acceptance**:
  - [ ] Follows existing nested class pattern
  - [ ] All keys prefixed with `messaging.`
- **Effort**: S
- **Skill**: `clean-architecture-rules`

#### Task 1.3: Add `TenantDelegation.LockMessaging` Key
- **File**: `Explore.Domain/Constants/GovernanceSettingKeys.cs`
- **Action**: Add `LockMessaging` to `TenantDelegation` class
- **Acceptance**:
  - [ ] `governance.lock_tenant_messaging` follows existing lock pattern
- **Effort**: S

#### Task 1.4: Define `IMessageBrokerProvider` Interface
- **File**: `Explore.Application/Contracts/Infrastructure/IMessageBrokerProvider.cs`
- **Action**: Provider-agnostic message broker contract
- **Methods**:
  - `PublishAsync<TMessage>(TMessage message, CancellationToken ct)` → `Task<MessagePublishResult>`
  - `BulkPublishAsync<TMessage>(IEnumerable<TMessage> messages, CancellationToken ct)` → `Task<BulkPublishResult>`
  - `SubscribeAsync<TMessage>(Func<TMessage, MessageHeader, ValueTask> handler, Action<Exception> errorHandler, CancellationToken ct)` → `Task<IAsyncDisposable>`
  - `PingAsync(CancellationToken ct)` → `Task<bool>`
  - `CloseAsync()` → `Task`
  - Property: `MessageQueueProviderEnum ProviderType`
  - Property: `bool IsConnected`
- **Acceptance**:
  - [ ] Fire-and-forget safe pattern (like `IAnalyticsProvider` — failures logged, not thrown to callers)
  - [ ] `MessagePublishResult` record with `Success`, `MessageId`, `Error`
  - [ ] ABOUTME header
- **Effort**: M
- **Skill**: `clean-architecture-rules`

#### Task 1.5: Define `IMessageBrokerConfigResolver` Interface
- **File**: `Explore.Application/Contracts/Infrastructure/IMessageBrokerConfigResolver.cs`
- **Action**: Mirrors `IAnalyticsConfigResolver`
- **Methods**:
  - `ResolveAsync(CancellationToken ct)` → `Task<MessagingConfiguration>`
  - `InvalidateCache(Guid? tenantId)`
- **Acceptance**:
  - [ ] Follows `IAnalyticsConfigResolver` pattern exactly
- **Effort**: S

#### Task 1.6: Define `MessagingConfiguration` Model
- **File**: `Explore.Application/Models/MessagingConfiguration.cs`
- **Properties**: `Provider` (enum), `IsEnabled` (bool), `Endpoint`, `ExchangePrefix`, `Username`, `VirtualHost`
- **Acceptance**:
  - [ ] Mirrors `AnalyticsConfiguration` shape
- **Effort**: S

#### Task 1.7: Add `MQContract.Abstractions` to Application Layer
- **File**: `Explore.Application/Explore.Application.csproj`
- **Action**: Add `MQContract.Abstractions` for `[Message]` attribute
- **Acceptance**:
  - [ ] Only Abstractions package (no implementation leakage)
  - [ ] Build passes
- **Effort**: S

#### Task 1.8: Define Integration Event Contracts
- **File**: `Explore.Application/IntegrationEvents/` (new directory)
- **Action**: Typed integration event records with `[Message]` attributes
- **Events** (initial set):
  - `EventPublishedIntegrationEvent` — `[Message(channel: "events.published")]`
  - `EventCreatedIntegrationEvent` — `[Message(channel: "events.created")]`
  - `RegistrationConfirmedIntegrationEvent` — `[Message(channel: "registrations.confirmed")]`
  - `OrganizationCreatedIntegrationEvent` — `[Message(channel: "organizations.created")]`
- **Acceptance**:
  - [ ] Each event has `[Message]` attribute with channel and version
  - [ ] Records contain all needed correlation fields
  - [ ] ABOUTME headers
- **Effort**: M

---

### Phase 2: Infrastructure — Providers & Config Resolver (Effort: L)

**Goal:** Implement RabbitMQ provider, null provider, runtime provider, and config resolver.

#### Task 2.1: Add NuGet Packages
- **Files**:
  - `Explore.Infrastructure/Explore.Infrastructure.csproj` → `MQContract`, `MQContract.RabbitMQ`, `MQContract.InMemory`
  - `Explore.AppHost/Explore.AppHost.csproj` → `Aspire.Hosting.RabbitMQ`
- **Effort**: S

#### Task 2.2: Implement `NullMessageBrokerProvider`
- **File**: `Explore.Infrastructure/Messaging/Providers/NullMessageBrokerProvider.cs`
- **Action**: No-op provider (like `NullAnalyticsProvider`)
- **Behavior**: All methods log debug and return success. `IsConnected = true`. `ProviderType = None`.
- **Acceptance**:
  - [ ] Zero side effects
  - [ ] Uses `MQContract.InMemory` connection (so MQContract middleware still works for testing)
- **Effort**: S

#### Task 2.3: Implement `RabbitMqMessageBrokerProvider`
- **File**: `Explore.Infrastructure/Messaging/Providers/RabbitMqMessageBrokerProvider.cs`
- **Action**: Full MQContract usage with RabbitMQ connector
- **Responsibilities**:
  - Creates `MQContract.RabbitMQ.Connection` from resolved config
  - Wraps in `ContractConnection.Instance()` with OpenTelemetry middleware
  - Registers MQContract resilience policy (retry + circuit breaker)
  - Registers compression middleware for payloads > threshold
  - Exposes `PublishAsync`, `BulkPublishAsync`, `SubscribeAsync`
  - Implements `IAsyncDisposable` for graceful shutdown
  - Uses `PingAsync()` for health check
- **MQContract features used**:
  - `ContractConnection.Instance(serviceConnection, logger: logger)`
  - `RegisterMiddlewareAsync<OpenTelemetryMiddleware>()`
  - `RegisterMiddlewareAsync<CompressionMiddleware>()`
  - `RegisterResiliencePolicy(retryPolicy: (3, attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt))), circuitBreakPolicy: (5, TimeSpan.FromSeconds(30)))`
  - `PublishAsync<TMessage>()` / `BulkPublishAsync<TMessage>()`
  - `SubscribeAsync<TMessage>()`
- **Acceptance**:
  - [ ] Full MQContract integration (middleware, resilience, publish, subscribe)
  - [ ] Connection created lazily, disposed on shutdown
  - [ ] Logs connection lifecycle events
  - [ ] Topic exchanges with configurable prefix
- **Effort**: L
- **Skill**: `clean-architecture-rules`

#### Task 2.4: Implement `RuntimeMessageBrokerProvider`
- **File**: `Explore.Infrastructure/Messaging/Providers/RuntimeMessageBrokerProvider.cs`
- **Action**: Runtime-switching wrapper (mirrors `RuntimeAnalyticsProvider`)
- **Behavior**:
  - Injects all concrete providers + `IMessageBrokerConfigResolver`
  - `ResolveProviderAsync()` → cache resolved provider for 5 min
  - Delegates all operations to resolved provider
  - Falls back to `NullMessageBrokerProvider` on resolution error
- **Acceptance**:
  - [ ] Follows `RuntimeAnalyticsProvider` pattern exactly
  - [ ] Cached resolution with 5-min TTL
  - [ ] Fallback on error
- **Effort**: M
- **Skill**: `clean-architecture-rules`

#### Task 2.5: Implement `MessageBrokerConfigResolver`
- **File**: `Explore.Infrastructure/Messaging/Config/MessageBrokerConfigResolver.cs`
- **Action**: Resolves from governance settings (mirrors `AnalyticsConfigResolver`)
- **Behavior**:
  - Uses `IHierarchicalSettingsResolver` for cascade: system → tenant override
  - Reads `messaging.*` governance keys
  - Parses provider enum from string
  - Short-lived cache (5 min) per tenant
  - Falls back to static `MessageBrokerSettings` (appsettings) when governance DB unavailable
- **Acceptance**:
  - [ ] Follows `AnalyticsConfigResolver` pattern
  - [ ] Tenant-scoped caching
  - [ ] Static config fallback for bootstrap
- **Effort**: M

#### Task 2.6: Create `MessageBrokerSettings` Static Config
- **File**: `Explore.Infrastructure/Messaging/Config/MessageBrokerSettings.cs`
- **Action**: Static appsettings POCO for bootstrap/fallback
- **Properties**: Provider, Enabled, RabbitMQ (nested: HostName, Port, UserName, Password, VirtualHost, UseSsl), ExchangePrefix
- **Effort**: S

#### Task 2.7: Implement `MqContractOutboxDispatcher`
- **File**: `Explore.Infrastructure/Messaging/MqContractOutboxDispatcher.cs`
- **Action**: Replaces `LoggingOutboxMessageDispatcher`
- **Behavior**:
  - Receives `IMessageBrokerProvider` (runtime-resolved)
  - Maps `OutboxMessage.EventType` → typed integration event
  - Calls `provider.PublishAsync<TIntegrationEvent>(event)`
  - On failure → throws (OutboxProcessor handles retry)
  - Uses `BulkPublishAsync` when OutboxProcessor processes batches
- **Acceptance**:
  - [ ] Implements `IOutboxMessageDispatcher`
  - [ ] Uses runtime provider (supports broker switching)
  - [ ] Maps all outbox fields to typed integration events
  - [ ] Throws on publish failure
- **Effort**: M
- **Skill**: `outbox-pattern`

#### Task 2.8: Delete `LoggingOutboxMessageDispatcher`
- **File**: `Explore.Infrastructure/Outbox/LoggingOutboxMessageDispatcher.cs`
- **Action**: Delete. Replaced by `MqContractOutboxDispatcher`. `NullMessageBrokerProvider` handles the "no broker" case.
- **Effort**: S

---

### Phase 3: DI Registration & Configuration (Effort: M)

**Goal:** Wire everything in DI and configuration files.

#### Task 3.1: Register Messaging Services
- **File**: `Explore.Infrastructure/InfrastructureServicesRegistration.cs`
- **Action**: Replace `LoggingOutboxMessageDispatcher` registration with full messaging stack
- **Registrations**:
  ```csharp
  // Message broker providers (all registered, runtime selects)
  services.AddSingleton<NullMessageBrokerProvider>();
  services.AddSingleton<RabbitMqMessageBrokerProvider>();
  services.AddScoped<IMessageBrokerConfigResolver, MessageBrokerConfigResolver>();
  services.AddScoped<RuntimeMessageBrokerProvider>();
  services.AddScoped<IMessageBrokerProvider>(sp => sp.GetRequiredService<RuntimeMessageBrokerProvider>());

  // Outbox dispatcher (now real)
  services.AddScoped<IOutboxMessageDispatcher, MqContractOutboxDispatcher>();
  ```
- **Acceptance**:
  - [ ] All providers registered (like analytics providers)
  - [ ] `LoggingOutboxMessageDispatcher` registration removed
  - [ ] Settings bound from config
- **Effort**: M

#### Task 3.2: Add appsettings Configuration
- **Files**: `Explore.API/appsettings.json`, `appsettings.Development.json`
- **Config**:
  ```json
  {
    "Messaging": {
      "Provider": "rabbitmq",
      "Enabled": true,
      "ExchangePrefix": "islamu.event",
      "RabbitMQ": {
        "HostName": "localhost",
        "Port": 5672,
        "UserName": "guest",
        "Password": "guest",
        "VirtualHost": "/",
        "UseSsl": false
      }
    }
  }
  ```
- **Effort**: S

#### Task 3.3: Add Governance Settings Seed Data
- **File**: Database seeder (wherever `SystemSetting` seeds live)
- **Action**: Seed `messaging.*` governance keys with defaults
- **Effort**: S

---

### Phase 4: Health, Observability & Metrics (Effort: M)

#### Task 4.1: Implement `MessagingHealthCheck`
- **File**: `Explore.Infrastructure/Messaging/MessagingHealthCheck.cs`
- **Action**: `IHealthCheck` using `IMessageBrokerProvider.PingAsync()`
- **Behavior**: Healthy/Degraded/Unhealthy based on `PingAsync` + consecutive failure tracking (like `SecretProviderHealthCheck`)
- **Acceptance**:
  - [ ] Follows `SecretProviderHealthCheck` pattern
  - [ ] Returns provider type in health data
  - [ ] Registered in `/health`, excluded from `/alive`
- **Effort**: S

#### Task 4.2: Implement `MessagingMetrics`
- **File**: `Explore.Infrastructure/Messaging/MessagingMetrics.cs`
- **Meter**: `Explore.Messaging`
- **Instruments**:
  - `messaging.published` (Counter) — tagged: provider, channel, tenant_id
  - `messaging.publish_failed` (Counter) — tagged: provider, channel, error_type
  - `messaging.publish_duration_seconds` (Histogram)
  - `messaging.consumed` (Counter) — tagged: provider, channel, consumer
  - `messaging.consume_failed` (Counter)
- **Acceptance**:
  - [ ] Follows `BusinessMetrics` and `SecretRefreshMetrics` patterns
- **Effort**: S

#### Task 4.3: Register Health Check in API
- **File**: `Explore.API/Program.cs`
- **Acceptance**:
  - [ ] Conditionally registered when messaging enabled
  - [ ] Tagged `messaging`
- **Effort**: S

---

### Phase 5: Docker Compose & Aspire (Effort: M)

**Goal:** RabbitMQ is the default in docker-compose. Also available in Aspire.

#### Task 5.1: Add RabbitMQ to Docker Compose
- **File**: `docker-compose.yml` (or `docker-compose.override.yml`)
- **Config**:
  ```yaml
  rabbitmq:
    image: rabbitmq:4-management-alpine
    ports:
      - "5672:5672"
      - "15672:15672"
    environment:
      RABBITMQ_DEFAULT_USER: guest
      RABBITMQ_DEFAULT_PASS: guest
    volumes:
      - rabbitmq_data:/var/lib/rabbitmq
    healthcheck:
      test: rabbitmq-diagnostics -q ping
      interval: 10s
      timeout: 5s
      retries: 5
  ```
- **Acceptance**:
  - [ ] RabbitMQ included as default infrastructure
  - [ ] Management UI on :15672
  - [ ] API service depends_on rabbitmq
  - [ ] Health check configured
- **Effort**: S

#### Task 5.2: Add RabbitMQ to Aspire AppHost
- **File**: `Explore.AppHost/AppHost.cs`
- **Action**: `AddRabbitMQ("messaging")` + wire to API
- **Acceptance**:
  - [ ] Starts with Aspire
  - [ ] Connection string injected into API
  - [ ] Management UI reference
- **Effort**: M

#### Task 5.3: Update API Bootstrap for Aspire Connection
- **File**: `Explore.API/Program.cs`
- **Action**: Read Aspire-injected RabbitMQ connection → merge into `Messaging:RabbitMQ` config
- **Effort**: S

---

### Phase 6: Testing (Effort: L)

#### Task 6.1: Unit Tests — `RabbitMqMessageBrokerProvider`
- **File**: `Event.Application.UnitTests/Infrastructure/Messaging/RabbitMqMessageBrokerProviderTests.cs`
- **Tests**:
  - Publishes message with correct channel
  - BulkPublish sends all messages
  - Returns error result on failure (does not throw)
  - Resilience policy retries transient failures
  - Connection lifecycle (lazy create, dispose)
- **Uses**: `MQContract.InMemory` as transport
- **Effort**: M

#### Task 6.2: Unit Tests — `RuntimeMessageBrokerProvider`
- **File**: `Event.Application.UnitTests/Infrastructure/Messaging/RuntimeMessageBrokerProviderTests.cs`
- **Tests**:
  - Delegates to correct provider based on resolved config
  - Falls back to NullProvider on resolution error
  - Caches resolved provider
  - Handles disabled messaging
- **Effort**: M

#### Task 6.3: Unit Tests — `MqContractOutboxDispatcher`
- **File**: `Event.Application.UnitTests/Infrastructure/Messaging/MqContractOutboxDispatcherTests.cs`
- **Tests**:
  - Maps OutboxMessage to typed integration event
  - Publishes via IMessageBrokerProvider
  - Throws on publish failure (triggers outbox retry)
  - Handles unknown event types
- **Effort**: M

#### Task 6.4: Unit Tests — `MessageBrokerConfigResolver`
- **File**: `Event.Application.UnitTests/Infrastructure/Messaging/MessageBrokerConfigResolverTests.cs`
- **Tests**:
  - Resolves from governance settings
  - Caches per tenant
  - Falls back to static config
  - Parses provider enum correctly
- **Effort**: S

#### Task 6.5: Unit Tests — Integration Event Contracts
- **File**: `Event.Application.UnitTests/Infrastructure/Messaging/IntegrationEventContractTests.cs`
- **Tests**:
  - All events have `[Message]` attribute
  - Channel names follow convention
  - Serialization round-trips correctly
- **Effort**: S

#### Task 6.6: Integration Tests — End-to-End Outbox → Broker
- **File**: `Event.API.IntegrationTests/Features/MessagingIntegrationTests.cs`
- **Tests**:
  - OutboxProcessor dispatches via real provider (InMemory transport)
  - Message arrives with correct channel and payload
  - Retry on transient failure works
  - Provider switch at runtime works
  - NullProvider gracefully no-ops
- **Uses**: `MQContract.InMemory` (no real RabbitMQ in CI)
- **Effort**: L

#### Task 6.7: Architecture Tests — Messaging Convention Enforcement
- **File**: `Event.Architecture.Tests/MessagingConventionTests.cs`
- **Tests**:
  - All integration events have `[Message]` attribute
  - All `messaging.*` governance keys use correct prefix
  - `IMessageBrokerProvider` is only referenced in Application + Infrastructure layers
- **Effort**: S

---

## Governance Settings Design

### Keys

| Key | Type | Default | Description |
|-----|------|---------|-------------|
| `messaging.provider` | string | `"rabbitmq"` | Active provider: `none`, `rabbitmq`, `kafka`, `nats`, `azure_service_bus`, `redis` |
| `messaging.enabled` | bool | `true` | Master enable switch |
| `messaging.endpoint` | string | `""` | Broker connection endpoint |
| `messaging.exchange_prefix` | string | `"islamu.event"` | Exchange/topic naming prefix |
| `messaging.username` | string | `""` | Broker auth username (non-secret) |
| `messaging.virtual_host` | string | `"/"` | RabbitMQ vhost / logical partition |
| `governance.lock_tenant_messaging` | bool | `false` | Lock tenant messaging override |

### Sensitive Config (via SecretProvider, not governance)

Broker passwords/tokens go through the existing `SecretProvider` abstraction — not stored as governance settings.

---

## Risk Assessment

| Risk | Impact | Likelihood | Mitigation |
|------|--------|------------|------------|
| MQContract edge cases under load | Medium | Medium | User commits to maintaining if needed. Library is MIT licensed. RabbitMQ connector is thin (~300 LOC). |
| Provider switch loses in-flight messages | Low | Low | Outbox guarantees delivery — messages retry until new provider is ready |
| Governance DB unavailable at startup | Medium | Low | Static `MessageBrokerSettings` from appsettings serves as bootstrap fallback |
| Consumer registration across provider switch | Medium | Medium | Consumers re-register when provider changes. Short reconnection window acceptable. |

---

## Success Metrics

1. **Self-hoster can pick their MQ**: Set `messaging.provider` in governance or appsettings → system uses that broker.
2. **RabbitMQ works out of the box**: docker-compose includes RabbitMQ. Zero config needed for default setup.
3. **Outbox delivers for real**: `OutboxProcessor` messages reach actual broker, not a log warning.
4. **Full MQContract**: Middleware (telemetry, compression), resilience, consumers all active.
5. **Observable**: Health checks, Prometheus metrics, OpenTelemetry traces across message boundaries.

---

## Future Providers (Not in Scope, But Designed For)

| Provider | Package | Effort When Needed |
|----------|---------|-------------------|
| Kafka | `MQContract.Kafka` | S — implement `KafkaMessageBrokerProvider`, add enum value |
| NATS | `MQContract.NATS` | S — same pattern |
| Azure Service Bus | `MQContract.AzureServiceBus` | S — same pattern |
| Redis Streams | `MQContract.Redis` | S — same pattern |
| Amazon SNS+SQS | `MQContract.AmazonSNQS` | S — same pattern |

Each new provider is a single class implementing `IMessageBrokerProvider` + registering in DI. MQContract handles all transport-specific concerns.

---

## Potential Risks & Unknowns

The highest-risk area is **consumer lifecycle during runtime provider switching**. When a self-hoster changes `messaging.provider` from RabbitMQ to Kafka via governance settings, active subscriptions on the old provider need to be torn down and re-established on the new provider. The `RuntimeMessageBrokerProvider` cache invalidation path needs to handle this gracefully — likely by tracking active subscriptions and re-registering them on provider change. This is the most complex piece architecturally.

A secondary concern is **MQContract's `ContractConnection` singleton lifecycle vs scoped DI**. The `RabbitMqMessageBrokerProvider` holds a long-lived connection, but the `RuntimeMessageBrokerProvider` is scoped. The concrete providers should be singletons (one connection per process), while the runtime wrapper is scoped (resolves per-request). This matches how `RuntimeAnalyticsProvider` works — scoped wrapper, concrete providers injected as singletons via HttpClient factory.
