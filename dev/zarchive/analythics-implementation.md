This plan is designed to guide a junior developer through the implementation of a multi-tenant, pluggable analytics system. It balances the need for "Enterprise Grade" features (RudderStack/PostHog) with the requirement for "Lightweight" self-hosting.

---

## 1. Context & Objectives

**Goal:** Create a unified analytics and feature-flagging system that allows each tenant to choose their own provider (PostHog, RudderStack, Plausible, or None) while maintaining strict data isolation.

**Architectural Principle:** **Thin Abstraction.** We will not abstract every feature of every tool. We will abstract the **intent** (e.g., "User Signed Up") and let the specific provider handle the **delivery**.

---

## 2. High-Level Architecture

1. **Domain Layer:** Defines the `IAnalyticsProvider` interface.
2. **Infrastructure Layer:** Contains specific implementations (PostHog, RudderStack, etc.).
3. **Application Layer:** A `UserTracker` service that resolves the correct provider based on the `TenantID`.
4. **Presentation (Blazor):** A JS-Interop wrapper to trigger tracking from the browser.

---

## 3. Implementation Plan

### Phase 1: Foundation & Data Contracts

Define how we store tenant settings and what the universal "Tracking" language looks like.

* **Task 1.1:** Update the `Tenant` entity/database table to include:
* `AnalyticsProviderType` (Enum: None, PostHog, RudderStack, Plausible)
* `AnalyticsWriteKey` (String)
* `AnalyticsEndpointUrl` (String - for self-hosted instances)


* **Task 1.2:** Create the `IAnalyticsProvider` interface in the Core/Domain project.

```csharp
public interface IAnalyticsProvider
{
    Task IdentifyAsync(string userId, IDictionary<string, object> traits);
    Task TrackAsync(string eventName, IDictionary<string, object> properties);
    Task<bool> IsFeatureEnabledAsync(string featureKey, string userId);
}

```

### Phase 2: Provider Implementation

Implement the specific logic for each supported tool.

* **Task 2.1:** **PostHog Implementation.** Use the `PostHog.NET` library.
* *Note:* Ensure the `Capture` call includes the `distinct_id`.


* **Task 2.2:** **RudderStack Implementation.** Use the `RudderStack.Analytics` library.
* *Note:* This implementation will work for both **Cloud** and **Self-Hosted** by simply changing the `dataPlaneUrl`.


* **Task 2.3:** **Null/No-Op Implementation.** A "silent" provider that does nothing for tenants who disable tracking.

### Phase 3: The Multi-Tenant Factory

This is the "Brain" that decides where data goes at runtime.

* **Task 3.1:** Create an `AnalyticsFactory` that takes the `CurrentTenant` context.
* **Task 3.2:** Register the Factory in `Program.cs` using Scoped lifetime.

```csharp
public class AnalyticsFactory {
    public IAnalyticsProvider GetProvider(TenantSettings settings) {
        return settings.Provider switch {
            AnalyticsType.PostHog => new PostHogProvider(settings.Key, settings.Url),
            AnalyticsType.RudderStack => new RudderStackProvider(settings.Key, settings.Url),
            _ => new NullProvider()
        };
    }
}

```

### Phase 4: Blazor (Frontend) Integration

Tracking button clicks and page views directly from the client.

* **Task 4.1:** Create a JavaScript file `analytics-bridge.js`. It will hold a simple function to initialize the provider's JS snippet (PostHog/RudderStack) based on the configuration sent from the server.
* **Task 4.2:** Build a Blazor `AnalyticsService` that calls these JS functions via `IJSRuntime`.

---

## 4. Detailed Task List for Junior Developer

| Task ID | Component | Description | Definition of Done |
| --- | --- | --- | --- |
| **AN-01** | Database | Add Analytics config columns to Tenant Table. | Migration applied to SQL. |
| **AN-02** | Core | Create `IAnalyticsProvider` and `AnalyticsType` Enum. | Interface exists in Domain. |
| **AN-03** | Infra | Implement `PostHogProvider.cs`. | Unit test proves data sends to PostHog. |
| **AN-04** | Infra | Implement `RudderStackProvider.cs`. | Works with both Cloud/Local URLs. |
| **AN-05** | API | Create `AnalyticsFactory` & register in DI. | Correct provider resolved per TenantID. |
| **AN-06** | Blazor | Create `AnalyticsBridge.razor` component. | Injects correct JS snippet into `<head>`. |
| **EM-01** | Mail | Add "Report Abuse" footer to all outgoing mail. | Footer visible in all test emails. |

---

## 5. Summary of Best Practices to Follow

1. **Feature Flag Graceful Degradation:** If a tenant switches from PostHog (which has flags) to Plausible (which doesn't), the code must default `IsFeatureEnabled` to `false` or `true` (safe default) rather than crashing.
2. **Sensitive Data:** Never track passwords or PII (Personally Identifiable Information) in the `properties` dictionary.
3. **ClickHouse Context:** Remind the team that if we use PostHog Self-Hosted, ClickHouse is managed *inside* the PostHog container—don't try to connect to it directly from the C# code unless performing "Enterprise" data warehousing.