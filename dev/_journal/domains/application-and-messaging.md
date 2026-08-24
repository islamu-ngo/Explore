<!-- ABOUTME: Domain journal for Application layer, MediatR/CQRS, Outbox messaging, and settings cascade. -->
<!-- ABOUTME: Captures durable findings on command handlers, event dispatching, and asynchronous workflows. -->

# Application & Messaging Knowledge Ledger

> **Scope**: `Explore.Application`, CQRS/MediatR pipeline, Transactional Outbox, RabbitMQ/MQContract, and EAV.

---

## 1. Architectural Decisions

- **Transactional Outbox Mandate**: Any domain event, webhook, or external notification resulting from an aggregate state change must be written to the local outbox in the same database transaction.
- **Provider Abstraction for Messaging**: `IMessagingProvider` defines the application contract, wrapped by `RuntimeMessagingProvider` (scoped with cache) and `RabbitMqMessagingProvider` (singleton with lazy init).
- **5-Tier Governance Settings Cascade**: Settings resolve through User $\rightarrow$ Group $\rightarrow$ Organization $\rightarrow$ Tenant $\rightarrow$ Instance. Instance-level locks prevent higher-tier overrides unless running in single-tenant mode.

---

## 2. Technical Insights & Patterns

- **MQContract OpenTelemetry Source Alignment**: `RabbitMqMessagingProvider` calls `contractConnection.EnableOpenTelemetry(activitySource: "MQContract")`, which requires `Explore.ServiceDefaults/Extensions.cs` to declare `.AddSource("MQContract")`. Mismatched source names cause missing traces.
- **Lazy Singleton Connection Pattern**: Connection instances (`ContractConnection`) are expensive and managed as singletons behind `SemaphoreSlim(1,1)` thread synchronization, properly disposed on application shutdown.
- **Quartz.NET Options Collision**: Quartz ships its own `Quartz.QuartzSchedulerOptions`. The application configuration class must be named `QuartzSchedulerSettings` to avoid namespace collisions.
- **Quartz `JobDataMap` Key Safety**: `JobDataMap.GetString(key)` throws `KeyNotFoundException` on missing keys. Always probe using `TryGetValue` and treat absent payloads as logged no-ops to avoid infinite scheduler retry loops.
- **Quartz 6-Field Cron Expressions**: Quartz rejects cron expressions that set both day-of-month and day-of-week to `*`. One must be `?` (e.g. `*/10 * * * * ?`).

---

## 3. Failed Approaches & Lessons

- **Scope Creep in Delegated Handler Cleanup**: Never rename or mutate repository interfaces (e.g. `IGenericRepository`) or CQRS request signatures during handler cleanup tasks. Always enforce strict boundaries on contract modifications.
- **Generic BackgroundService Timer Loops**: Hand-rolled timer loops in `BackgroundService` are banned for periodic sweeps. All periodic work belongs in Quartz.NET registered via `AddSweepJob<TJob>`.
