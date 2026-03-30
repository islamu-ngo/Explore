# Message Queue Provider Abstraction — Task Checklist

Last Updated: 2026-03-29

## Phase 1: Domain & Application Contracts ⏳ NOT STARTED

- [ ] **1.1** Create `MessageQueueProviderEnum` in `Explore.Domain/Enums/` — **S**
  - None=0, RabbitMQ=1, Kafka=2, NATS=3, AzureServiceBus=4, Redis=5
- [ ] **1.2** Add `GovernanceSettingKeys.Messaging` class — **S**
  - Keys: provider, enabled, endpoint, exchange_prefix, username, virtual_host
- [ ] **1.3** Add `TenantDelegation.LockMessaging` key — **S**
- [ ] **1.4** Define `IMessageBrokerProvider` interface — **M**
  - PublishAsync, BulkPublishAsync, SubscribeAsync, PingAsync, CloseAsync, ProviderType, IsConnected
  - MessagePublishResult, BulkPublishResult records
- [ ] **1.5** Define `IMessageBrokerConfigResolver` interface — **S**
  - ResolveAsync → MessagingConfiguration, InvalidateCache
- [ ] **1.6** Create `MessagingConfiguration` model — **S**
  - Provider (enum), IsEnabled, Endpoint, ExchangePrefix, Username, VirtualHost
- [ ] **1.7** Add `MQContract.Abstractions` NuGet to `Explore.Application.csproj` — **S**
- [ ] **1.8** Define integration event contracts in `Explore.Application/IntegrationEvents/` — **M**
  - EventPublished, EventCreated, RegistrationConfirmed, OrganizationCreated
  - Each with [Message] attribute, channel, version

## Phase 2: Infrastructure — Providers & Config (Effort: L) ⏳ NOT STARTED

- [ ] **2.1** Add NuGet packages to Infrastructure + AppHost — **S**
  - MQContract, MQContract.RabbitMQ, MQContract.InMemory, Aspire.Hosting.RabbitMQ
- [ ] **2.2** Implement `NullMessageBrokerProvider` — **S**
  - No-op, uses InMemory connector, ProviderType=None
- [ ] **2.3** Implement `RabbitMqMessageBrokerProvider` — **L**
  - Full MQContract: middleware (OpenTelemetry, compression), resilience, publish, subscribe
  - Lazy connection, IAsyncDisposable, PingAsync health
- [ ] **2.4** Implement `RuntimeMessageBrokerProvider` — **M**
  - Mirrors RuntimeAnalyticsProvider: cached resolution, fallback to Null, scoped wrapper
- [ ] **2.5** Implement `MessageBrokerConfigResolver` — **M**
  - IHierarchicalSettingsResolver, messaging.* keys, tenant cache, static fallback
- [ ] **2.6** Create `MessageBrokerSettings` static config POCO — **S**
  - Provider, Enabled, RabbitMQ (nested), ExchangePrefix
- [ ] **2.7** Implement `MqContractOutboxDispatcher` — **M**
  - Replaces LoggingOutboxMessageDispatcher, maps OutboxMessage → typed event → PublishAsync
- [ ] **2.8** Delete `LoggingOutboxMessageDispatcher.cs` — **S**

## Phase 3: DI Registration & Configuration ⏳ NOT STARTED

- [ ] **3.1** Register messaging services in `InfrastructureServicesRegistration.cs` — **M**
  - All providers + RuntimeWrapper + ConfigResolver + Dispatcher
- [ ] **3.2** Add `Messaging` section to appsettings — **S**
  - Provider=rabbitmq, Enabled=true, RabbitMQ defaults
- [ ] **3.3** Add messaging governance settings seed data — **S**

## Phase 4: Health, Observability & Metrics ⏳ NOT STARTED

- [ ] **4.1** Implement `MessagingHealthCheck` — **S**
  - PingAsync-based, consecutive failure tracking, tagged `messaging`
- [ ] **4.2** Implement `MessagingMetrics` — **S**
  - Meter: Explore.Messaging. Counters: published, publish_failed, consumed, consume_failed. Histogram: duration.
- [ ] **4.3** Register health check in `Program.cs` — **S**

## Phase 5: Docker Compose & Aspire ⏳ NOT STARTED

- [ ] **5.1** Add RabbitMQ service to `docker-compose.yml` — **S**
  - rabbitmq:4-management-alpine, ports 5672+15672, healthcheck
- [ ] **5.2** Add RabbitMQ resource to `Explore.AppHost/AppHost.cs` — **M**
- [ ] **5.3** Update API bootstrap for Aspire connection string — **S**

## Phase 6: Testing ⏳ NOT STARTED

- [ ] **6.1** Unit tests: `RabbitMqMessageBrokerProviderTests` — **M**
  - Publish, BulkPublish, failure handling, resilience, lifecycle (InMemory transport)
- [ ] **6.2** Unit tests: `RuntimeMessageBrokerProviderTests` — **M**
  - Provider delegation, fallback, caching, disabled state
- [ ] **6.3** Unit tests: `MqContractOutboxDispatcherTests` — **M**
  - OutboxMessage → typed event mapping, publish delegation, failure throws
- [ ] **6.4** Unit tests: `MessageBrokerConfigResolverTests` — **S**
  - Governance resolution, caching, static fallback, enum parsing
- [ ] **6.5** Unit tests: `IntegrationEventContractTests` — **S**
  - [Message] attribute presence, channel convention, serialization round-trip
- [ ] **6.6** Integration tests: `MessagingIntegrationTests` — **L**
  - E2E outbox → dispatcher → InMemory publish, retry, provider switch
- [ ] **6.7** Architecture tests: `MessagingConventionTests` — **S**
  - [Message] on all events, key prefix, layer boundary enforcement

---

## Summary

| Phase | Tasks | Status |
|-------|-------|--------|
| 1. Domain & Application Contracts | 8 | ⏳ |
| 2. Infrastructure Providers | 8 | ⏳ |
| 3. DI & Configuration | 3 | ⏳ |
| 4. Health & Observability | 3 | ⏳ |
| 5. Docker & Aspire | 3 | ⏳ |
| 6. Testing | 7 | ⏳ |
| **Total** | **32** | |

## Task Dependencies

```
Phase 1 (contracts) ──────────→ Phase 2 (providers)
  1.1 (enum)     → 2.3, 2.4
  1.2 (keys)     → 2.5, 3.3
  1.4 (interface) → 2.2, 2.3, 2.4, 2.7
  1.5 (resolver)  → 2.5
  1.8 (events)    → 2.7

Phase 2 (providers) ──────────→ Phase 3 (DI)
  2.2-2.7         → 3.1

Phase 3 (DI) ────────────────→ Phase 4 (health)
  3.1             → 4.1, 4.2, 4.3

Phase 2 (providers) ──────────→ Phase 5 (docker/aspire)
  2.3             → 5.1, 5.2, 5.3

Phase 1-4 ────────────────────→ Phase 6 (testing)
  All phases      → 6.1-6.7
```

Note: TDD means tests are written alongside each phase, not deferred to Phase 6.
Phase 6 captures the test inventory — actual test writing happens during each phase.
