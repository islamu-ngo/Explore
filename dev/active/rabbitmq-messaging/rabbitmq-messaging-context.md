# Message Queue Provider Abstraction — Context

Last Updated: 2026-03-29

## SESSION PROGRESS (2026-03-29)

### ✅ COMPLETED
- MQContract library deep analysis (cloned to /tmp/MQContract, all core files read)
- Research: MQContract vs MassTransit vs NServiceBus, RabbitMQ best practices
- Current outbox architecture analysis
- Existing provider abstraction patterns studied (analytics, secrets, feature flags, authorization, translation)
- Governance settings pattern analyzed (`GovernanceSettingKeys`, `AnalyticsConfigResolver`, `RuntimeAnalyticsProvider`)
- Plan v2 created — full provider abstraction with MQContract at full capacity
- Task checklist v2 created

### 🟡 IN PROGRESS
- Nothing — plan review stage

### ⚠️ BLOCKERS
- None. Plan ready for implementation.

---

## Key Decisions

### D1: Full Provider Abstraction (like Analytics)
**Why:** Self-hosters must be able to choose their message queue (RabbitMQ, Kafka, NATS, etc.) — same pattern as analytics (PostHog/Plausible/Rybbit/RudderStack/None), secrets (Infisical/Vault/None), and authorization (Cerbos/Local). Runtime switching via governance settings with tenant override support.

### D2: Use MQContract to Its Fullest
**Why:** MQContract already provides the transport abstraction with 14+ connectors. We use its full feature set: publish, subscribe, consumer registration, middleware (OpenTelemetry, compression), resilience policies, bulk publish, query/response, and message versioning. No artificial restrictions.

### D3: RabbitMQ is the Default, Included in Docker Compose
**Why:** Self-hosters need a zero-config messaging setup. RabbitMQ is the most widely deployed open-source MQ. Included in docker-compose alongside PostgreSQL and other infrastructure.

### D4: Breaking Changes Are Fine
**Why:** Development stage. `LoggingOutboxMessageDispatcher` is deleted outright — replaced by `MqContractOutboxDispatcher` + `NullMessageBrokerProvider` for the no-broker case.

### D5: Governance Settings Control Provider Selection
**Why:** Mirrors analytics: instance admin sets `messaging.provider=rabbitmq` at system level, can lock it or let tenants override. The `IHierarchicalSettingsResolver` cascade handles system → tenant settings.

### D6: Integration Events in Application Layer
**Why:** Typed integration event contracts (`[Message]` decorated records) belong in Application layer because they define the public contract other services consume. `MQContract.Abstractions` package (attributes only) is the only MQContract dependency in Application layer.

---

## Provider Abstraction Pattern Reference

### Analytics (model to follow)

```
Domain:     AnalyticsProviderEnum (None, Posthog, Plausible, Rybbit, RudderStack)
            GovernanceSettingKeys.Analytics.* (provider, enabled, endpoint, api_key, ...)

Application: IAnalyticsProvider (Track, Identify, PageView, GroupIdentify)
             IAnalyticsConfigResolver → AnalyticsConfiguration

Infrastructure: RuntimeAnalyticsProvider (scoped, delegates to resolved provider)
                PostHogAnalyticsProvider, PlausibleAnalyticsProvider, etc. (via HttpClient)
                NullAnalyticsProvider (no-op)
                AnalyticsConfigResolver (IHierarchicalSettingsResolver + cache)

DI: All concrete providers registered, RuntimeAnalyticsProvider wraps + resolves
```

### Messaging (what we build)

```
Domain:     MessageQueueProviderEnum (None, RabbitMQ, Kafka, NATS, AzureServiceBus, Redis)
            GovernanceSettingKeys.Messaging.* (provider, enabled, endpoint, exchange_prefix, ...)

Application: IMessageBrokerProvider (Publish, BulkPublish, Subscribe, Ping, Close)
             IMessageBrokerConfigResolver → MessagingConfiguration
             IntegrationEvents/ ([Message]-decorated records)

Infrastructure: RuntimeMessageBrokerProvider (scoped, delegates to resolved provider)
                RabbitMqMessageBrokerProvider (singleton, MQContract + RabbitMQ connector)
                NullMessageBrokerProvider (singleton, MQContract InMemory)
                MessageBrokerConfigResolver (IHierarchicalSettingsResolver + cache)
                MqContractOutboxDispatcher → IOutboxMessageDispatcher (uses IMessageBrokerProvider)
                MessagingHealthCheck, MessagingMetrics

DI: All concrete providers registered, RuntimeMessageBrokerProvider wraps + resolves
```

---

## MQContract Library — Key APIs Used

### Connection Setup
```csharp
// RabbitMQ
var transport = new MQContract.RabbitMQ.Connection(new ConnectionFactory { ... });
var conn = ContractConnection.Instance(transport, logger: logger);

// Middleware registration
await conn.RegisterMiddlewareAsync<OpenTelemetryMiddleware>();
await conn.RegisterMiddlewareAsync<CompressionMiddleware>();

// Resilience
conn.RegisterResiliencePolicy(
    retryPolicy: (3, attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt))),
    circuitBreakPolicy: (5, TimeSpan.FromSeconds(30)));
```

### Publish
```csharp
var result = await conn.PublishAsync(new EventPublishedIntegrationEvent { ... });
if (result.IsError) throw new MessagePublishException(result.Error);
```

### Subscribe (Consumer Registration)
```csharp
var sub = await conn.SubscribeAsync<EventPublishedIntegrationEvent>(
    handler: async (msg, header) => { /* process */ },
    errorHandler: ex => logger.LogError(ex, "Consumer error"));
// sub implements IAsyncDisposable
```

### Message Contract
```csharp
[Message(channel: "events.published", typeName: "EventPublished", typeVersion: "1.0.0")]
public record EventPublishedIntegrationEvent
{
    public Guid EventId { get; init; }
    public Guid TenantId { get; init; }
    public string Title { get; init; }
    public DateTimeOffset OccurredAt { get; init; }
}
```

### Health Check
```csharp
var pingResult = await conn.PingAsync(); // bool
```

---

## Key Files

### Domain Layer (new)
| File | Purpose |
|------|---------|
| `Explore.Domain/Enums/MessageQueueProviderEnum.cs` | Provider enum |
| `Explore.Domain/Constants/GovernanceSettingKeys.cs` | Add `Messaging` class + `LockMessaging` |

### Application Layer (new)
| File | Purpose |
|------|---------|
| `Explore.Application/Contracts/Infrastructure/IMessageBrokerProvider.cs` | Provider contract |
| `Explore.Application/Contracts/Infrastructure/IMessageBrokerConfigResolver.cs` | Config resolver contract |
| `Explore.Application/Models/MessagingConfiguration.cs` | Resolved config model |
| `Explore.Application/IntegrationEvents/*.cs` | Typed event contracts |

### Infrastructure Layer (new)
| File | Purpose |
|------|---------|
| `Explore.Infrastructure/Messaging/Providers/RuntimeMessageBrokerProvider.cs` | Runtime wrapper |
| `Explore.Infrastructure/Messaging/Providers/RabbitMqMessageBrokerProvider.cs` | RabbitMQ via MQContract |
| `Explore.Infrastructure/Messaging/Providers/NullMessageBrokerProvider.cs` | No-op provider |
| `Explore.Infrastructure/Messaging/Config/MessageBrokerConfigResolver.cs` | Settings resolver |
| `Explore.Infrastructure/Messaging/Config/MessageBrokerSettings.cs` | Static config POCO |
| `Explore.Infrastructure/Messaging/MqContractOutboxDispatcher.cs` | Real outbox dispatcher |
| `Explore.Infrastructure/Messaging/MessagingHealthCheck.cs` | Health check |
| `Explore.Infrastructure/Messaging/MessagingMetrics.cs` | Prometheus metrics |

### Files to Delete
| File | Reason |
|------|--------|
| `Explore.Infrastructure/Outbox/LoggingOutboxMessageDispatcher.cs` | Replaced by `MqContractOutboxDispatcher` + `NullMessageBrokerProvider` |

### Files to Modify
| File | Change |
|------|--------|
| `Explore.Infrastructure/InfrastructureServicesRegistration.cs` | Replace outbox dispatcher, add messaging DI |
| `Explore.Domain/Constants/GovernanceSettingKeys.cs` | Add `Messaging` + `LockMessaging` |
| `Explore.API/Program.cs` | Health check, Aspire RabbitMQ |
| `Explore.API/appsettings.json` | Add `Messaging` section |
| `Explore.AppHost/AppHost.cs` | Add RabbitMQ resource |
| `docker-compose.yml` | Add RabbitMQ service |

### Test Files (new)
| File | Purpose |
|------|---------|
| `Event.Application.UnitTests/Infrastructure/Messaging/RabbitMqMessageBrokerProviderTests.cs` | Provider tests |
| `Event.Application.UnitTests/Infrastructure/Messaging/RuntimeMessageBrokerProviderTests.cs` | Runtime switching |
| `Event.Application.UnitTests/Infrastructure/Messaging/MqContractOutboxDispatcherTests.cs` | Dispatcher tests |
| `Event.Application.UnitTests/Infrastructure/Messaging/MessageBrokerConfigResolverTests.cs` | Config resolver |
| `Event.Application.UnitTests/Infrastructure/Messaging/IntegrationEventContractTests.cs` | Contract validation |
| `Event.API.IntegrationTests/Features/MessagingIntegrationTests.cs` | E2E outbox→broker |
| `Event.Architecture.Tests/MessagingConventionTests.cs` | Architecture rules |

---

## Dependencies

### NuGet Packages
| Package | Version | Project |
|---------|---------|---------|
| `MQContract.Abstractions` | 3.9.2 | `Explore.Application` |
| `MQContract` | 3.9.2 | `Explore.Infrastructure` |
| `MQContract.RabbitMQ` | 3.9.2 | `Explore.Infrastructure` |
| `MQContract.InMemory` | 3.9.2 | `Explore.Infrastructure` + test projects |
| `Aspire.Hosting.RabbitMQ` | latest | `Explore.AppHost` |

---

## Quick Resume

To continue implementation:
1. Read this file and `rabbitmq-messaging-tasks.md`
2. Start with Phase 1 (Domain + Application contracts)
3. Follow TDD: write failing test → implement → verify
4. Build after each phase: `dotnet build --configuration Release --verbosity quiet`
5. Test each project individually per CLAUDE.md instructions
