# Message Queue Provider Abstraction — Task Checklist

Last Updated: 2026-04-25

## Phase 1: Domain & Application Contracts ✅ COMPLETE (8/8)

- [x] **1.1** Create `MessagingProviderEnum` in `Explore.Domain/Enums/` — None=0, RabbitMq=1, InMemory=2 ✅
- [x] **1.2** Create `IMessagingProvider` interface — PublishAsync, BulkPublishAsync, SubscribeAsync ✅
- [x] **1.3** Create `IMessagingConfigResolver` interface — ResolveAsync, InvalidateCache ✅
- [x] **1.4** Create `MessagingConfiguration` POCO — 14 properties with MQContract defaults ✅
- [x] **1.5** Add `GovernanceSettingKeys.Messaging` + `TenantDelegation.LockMessaging` ✅
- [x] **1.6** Create `MessagingSettingGroup` (ISettingGroup with batch loading) ✅
- [x] **1.7** Create `IntegrationEventBase` abstract record ✅
- [x] **1.8** Create `EventPublishedIntegrationEvent` with [Message] attribute ✅

## Phase 2: Infrastructure Providers ✅ COMPLETE (8/8)

- [x] **2.1** Implement `RabbitMqMessagingProvider` — Full MQContract (OTEL, compression, Polly resilience, lazy init) ✅
- [x] **2.2** Implement `NullMessagingProvider` — No-op with debug logging ✅
- [x] **2.3** Implement `RuntimeMessagingProvider` — Runtime selection, exception fallback ✅
- [x] **2.4** Implement `MessagingConfigResolver` — IHierarchicalSettingsResolver, 5-min cache, per-tenant ✅
- [x] **2.5** Implement `MqContractOutboxMessageDispatcher` — EventType → typed event → channel routing ✅
- [x] **2.6** Delete `LoggingOutboxMessageDispatcher` ✅
- [x] **2.7** OpenTelemetry middleware — Integrated in RabbitMqMessagingProvider ✅
- [x] **2.8** Polly resilience policies — Integrated in RabbitMqMessagingProvider ✅

## Phase 3: DI Registration & Configuration ✅ COMPLETE (3/3)

- [x] **3.1** Register messaging services in `InfrastructureServicesRegistration.cs` ✅
- [x] **3.2** Replace IOutboxMessageDispatcher → MqContractOutboxMessageDispatcher ✅
- [x] **3.3** Add MQContract OpenTelemetry source to `Explore.ServiceDefaults/Extensions.cs` ✅

## Phase 4: Health, Observability & Metrics ⏳ NOT STARTED (0/3)

- [ ] **4.1** Create `MessagingHealthCheck` — IHealthCheck using IMessagingProvider
  - File: `Explore.Infrastructure/Messaging/MessagingHealthCheck.cs`
  - Pattern: `SecretProviderHealthCheck` (consecutive failure tracking, tagged "messaging")
  - Returns provider type in health data
- [ ] **4.2** Create `MessagingMetrics` — Prometheus meter + counters
  - File: `Explore.Infrastructure/Messaging/MessagingMetrics.cs`
  - Meter: "Explore.Messaging"
  - Counters: published, publish_failed, consumed, consume_failed
  - Histogram: publish_duration_seconds
  - Pattern: `BusinessMetrics`, `SecretRefreshMetrics`
- [ ] **4.3** Register health check in `Explore.API/Program.cs`
  - Conditionally registered when messaging enabled
  - Tagged "messaging", excluded from "/alive"

## Phase 5: Docker Compose & Aspire ⏳ NOT STARTED (0/3)

- [ ] **5.1** Add RabbitMQ to `docker-compose.yml`
  - `rabbitmq:4-management-alpine`, ports 5672+15672, healthcheck
  - API depends_on rabbitmq
- [ ] **5.2** Add RabbitMQ resource to `Explore.AppHost/AppHost.cs`
  - `builder.AddRabbitMQ("messaging")` + wire to API
- [ ] **5.3** Update `.env.example` with RabbitMQ settings

## Phase 6: Testing ⏳ NOT STARTED (0/7)

- [ ] **6.1** Unit tests: `RabbitMqMessagingProvider` — publish, bulk, failure, resilience, lifecycle
- [ ] **6.2** Unit tests: `RuntimeMessagingProvider` — delegation, fallback, caching, disabled
- [ ] **6.3** Unit tests: `MessagingConfigResolver` — governance resolution, caching, enum parsing
- [ ] **6.4** Unit tests: `MqContractOutboxMessageDispatcher` — EventType mapping, publish delegation, failure throws
- [ ] **6.5** Integration tests: RabbitMQ publish/subscribe (InMemory transport)
- [ ] **6.6** Integration tests: Outbox → dispatcher → broker E2E
- [ ] **6.7** Architecture tests: [Message] on events, layer boundaries, key prefix convention

---

## Summary

| Phase | Tasks | Status |
|-------|-------|--------|
| 1. Domain & Application Contracts | 8 | ✅ |
| 2. Infrastructure Providers | 8 | ✅ |
| 3. DI & Configuration | 3 | ✅ |
| 4. Health & Observability | 3 | ⏳ |
| 5. Docker & Aspire | 3 | ⏳ |
| 6. Testing | 7 | ⏳ |
| **Total** | **32** | **19/32 done** |

## ⚠️ Pre-Work Verification Required

Before starting Phase 4, **MUST** verify build:
```bash
dotnet build --configuration Release --verbosity quiet
```
Known risks:
1. MQContract NuGet packages may not be in `.csproj` files
2. Journal entry from 2026-04-24 mentions orphan MQContract build break in EventPublishedIntegrationEvent.cs
3. No tests have been run yet

## Session Handoff — 2026-05-03 Europe/Brussels

- [x] No task-state changes were made for this workstream during the sidebar dock refactor handoff session.
- [ ] Reconfirm this workstream's current state from its existing context/plan before resuming implementation.
