# ARCHITECTURAL EXECUTION PLAN: MODULAR EXTENSIBILITY & GOVERNANCE

## 1. STRATEGIC GOAL

Transform the `Explore` API from a fixed-schema event platform into a **composition-based container system**. This allows the platform to support any event type (Islamic, Tech, Medical, etc.) via "Aspects" and "Cascading Policies" without further database migrations for new domains.

---

## BLOCK 1: THE CASCADING SETTINGS ENGINE

**Goal:** Implement a three-tier configuration hierarchy (System → Tenant → Event) with locking capabilities.

### 1.1 Data Model (Explore.Domain)

* **Create `SystemSetting**`: `Key (string)`, `Value (string)`, `IsLocked (bool)`, `AllowedValues (string JSON)`.
* **Create `TenantSetting**`: `TenantId (Guid)`, `Key (string)`, `Value (string)`.

### 1.2 Resolution Logic (Explore.Application)

* **Implement `ISettingsResolver**`:
* `GetSettingAsync<T>(string key, Guid tenantId)`
* **Logic:** 1. Check `SystemSetting.IsLocked`. If `true`, return `SystemSetting.Value`.
2. Check `TenantSetting` for an override.
3. Fall back to `SystemSetting.Value` (Default).



---

## BLOCK 2: RELATIONAL ASPECT ARCHITECTURE (ENTITY REFACTOR)

**Goal:** Strip domain-specific fields from the core `Event` entity and move them into optional 1:1 "Aspect" tables.

### 2.1 Refactor Core Entity (Explore.Domain/Event.cs)

* **Remove:** `MadhabId` and other Islamic-specific fields.
* **Add Navigation Properties:**
```csharp
public virtual EventIslamicAspect? IslamicAspect { get; set; }
public virtual EventTechAspect? TechAspect { get; set; }
// Container for dynamic metadata (JSON for small/rare fields)
public string? MetadataJson { get; set; } 

```



### 2.2 Create Aspect Tables (Explore.Persistence)

* **`EventIslamicAspect`**: Primary Key `EventId` (FK to `Events.Id`). Contains `MadhabId`, `PrayerOffset`, `GenderMode`.
* **`EventTechAspect`**: Primary Key `EventId` (FK to `Events.Id`). Contains `GithubRepoUrl`, `TechStack`.

### 2.3 MediatR & DTO Updates

* Update `EventDto` to include an `AvailableAspects` list (e.g., `["Islamic", "Tech"]`).
* Modify `GetEventDetailsRequestHandler` to use `.Include(e => e.IslamicAspect)` and `.Include(e => e.TechAspect)`.

---

## BLOCK 3: REQUEST-SCOPED STRATEGY RESOLVER

**Goal:** Enable modular business logic (like scheduling based on prayer times) that adapts at runtime.

### 3.1 Implementation (Explore.Infrastructure)

* **Define `IEventStrategy**`: Base interface for modular logic.
* **Implement `IslamicSchedulingStrategy**`: Calculates event timings based on a prayer time API/library.
* **Create `StrategyResolver**`:
* Injected into MediatR Handlers.
* Uses `TenantContext` to identify which module strategy to provide.



---

## BLOCK 4: MODULE GOVERNANCE & ONBOARDING

**Goal:** Control module visibility per tenant so a "Tech Hub" never sees "Islamic" fields.

### 4.1 Governance Tables

* **`ModuleDefinition`**: `Key` (e.g., "Mod_Islamic"), `Name`, `WizardSchemaUrl`.
* **`TenantCapability`**: `TenantId`, `ModuleKey`, `IsEnabled`.

### 4.2 API Discovery Endpoint

* **`GET /api/modules/available`**: Returns only modules enabled for the current `TenantId`.
* This drives the **Dynamic Step Sequencer** in the UI.

---

## BLOCK 5: VIRTUAL TENANT MASKING (DEPLOYMENT MODES)

**Goal:** Support Single-Tenant "masking" while keeping the code natively Multi-Tenant.

### 5.1 Middleware Refactor (Explore.API)

* Modify `TenantContext` middleware:
* **If `DeploymentMode == SingleTenant**`: Hardcode `TenantId` to `SeedIds.DefaultTenantId`.
* **If `DeploymentMode == MultiTenant**`: Resolve `TenantId` via subdomain or header.


* **Security:** Block `SuperAdmin` controllers if `SingleTenant` mode is active to simplify the UI experience.

---

## EXECUTION GUIDELINES FOR THE AI AGENT

1. **Persistence:** Use `EntityTypeConfiguration` classes for the new Aspect tables to keep `ExploreDbContext` clean.
2. **Mapping:** Use `MappingProfile.cs` to handle the conversion between `Event` + `Aspects` → `EventDto`.
3. **HATEOAS:** Update `EventLinkPolicy` to generate aspect-specific detail links (e.g., `/_links/islamic-details`).
4. **Validation:** Create `EventAspectValidator` that dynamically validates the payload based on the `ModuleKey` provided in the request.

---

This refactored plan integrates your vision for **UUID v7 performance**, **AT-Proto PDS hosting**, and a **high-performance filtering system** for event aspects. It moves away from "brittle" normalization while avoiding the "slow" pitfalls of pure JSON.

# REFACTORED EXECUTION PLAN: THE DECENTRALIZED EVENT HUB (v3.0)

## 1. IDENTITY & PRIMARY KEYS (UUID v7 + DID)

**Goal:** Use UUID v7 for database efficiency (monotonicity) and DIDs for protocol interoperability.

### 1.1 Data Model (Explore.Domain)

* **Core Entity:** `Actor` (User/Organization) and `Event`.
* **Primary Key:** `Guid Id` (Generated as **UUID v7** in the Application layer or DB default).
* **Identity Property:** `string Did` (Unique Index).
* *Logic:* The `Id` handles local FK relations and B-Tree performance; the `Did` handles the AT-Proto/ActivityPub identity.


* **Handle:** `string Handle` (e.g., `organizer.islamu.io`).

---

## 2. DYNAMIC ASPECT ARCHITECTURE (PERFORMANCE OPTIMIZED)

**Goal:** Allow organizers to attach "Aspects" (Islamic, Tech, etc.) while ensuring seekers can filter them as fast as native columns.

### 2.1 The "Hybrid Aspect" Pattern

Instead of choosing between JSON (slow) or 1:1 Tables (inflexible), use **Shadow-Table Indexing**:

1. **`Event` Table:** Holds the UUID v7, Title, Start/End Date, and a `Metadata` JSONB column.
2. **`AspectRegistry` Table:** A 1:1 relational table for "High-Frequency" aspects (like `IslamicAspect` or `TechAspect`).
3. **MediatR Strategy:**
* When an organizer saves an event with the "Islamic" aspect, the system writes to the JSONB *and* synchronously updates the `IslamicAspect` relational table.
* **Seeker Filtering:** All "Lookup" queries (e.g., "Filter by Madhab") run against the **Relational Table** (fast B-Tree joins).
* **Display:** The detailed view reads from the **JSONB** (flexible, no joins needed for secondary metadata).



---

## 3. PDS HOSTING & SYNCHRONIZATION (THE "LIVING REPO")

**Goal:** Islamu hosts the PDS but allows external handle login, keeping local DB and PDS in sync.

### 3.1 The "Dual-Write" Sync Logic (`Explore.Infrastructure`)

Implement an **Outbox Pattern** to manage the Personal Data Server (PDS) state:

1. **Local Save:** User creates an event  Saved to `Explore.Database`.
2. **Outbox Entry:** A record is created in `PdsSyncOutbox`.
3. **Background Worker:**
* Picks up the Outbox entry.
* Pushes the record to the local **MST (Merkle Search Tree)** if the user is hosted by us.
* If the user uses an external PDS (via Handle), it calls the external `com.atproto.repo.applyWrites` endpoint.


4. **Conflict Resolution:** The `PdsSyncService` treats the PDS as the "Source of Truth" for decentralized identity, but the Local DB as the "Performance Cache" for the UI.

---

## 4. IMPLEMENTATION BLOCKS FOR THE AI AGENT

### BLOCK A: Persistence & ID Generation

* Update `ExploreDbContext` to use `Guid` as PK.
* Configure **PostgreSQL UUID v7** generation:
```csharp
builder.Entity<Event>()
       .Property(e => e.Id)
       .HasDefaultValueSql("uuid_generate_v7()"); // Or Application-side generation

```


* Add `HasIndex(e => e.Did).IsUnique()`.

### BLOCK B: The Aspect Filtering Request

* Create a `GetEventsQuery` that accepts a `Dictionary<string, string> Filters`.
* Implement a **Query Specification** that maps these dynamic filters to the corresponding Relational Aspect tables (e.g., `filter: "madhab:hanafi"`  `query.Where(e => e.IslamicAspect.Madhab == "Hanafi")`).

### BLOCK C: The PDS Adapter Service

* Implement `IPdsService` with two methods:
* `HostRecordAsync(DID, Record)`: Updates the local MST for Islamu-hosted users.
* `ProxyRecordAsync(RemotePDS, Record)`: Signs and pushes data to an external PDS.



---

## SUMMARY OF THE IMPROVED APPROACH

* **Performance:** UUID v7 ensures your database stays fast as you scale to millions of events.
* **Normalization:** You get the best of both worlds—data is normalized in "Aspect Tables" for filtering, but denormalized in "JSONB/PDS" for extensibility.
* **User Choice:** Users aren't forced to understand PDS/AT-Proto. If they sign up with you, you act as their PDS. If they are "Pro" users with their own PDS, your software becomes a "Relay" for their data.

**How to give this to the AI Agent:**

> "Refactor the Explore API to use UUID v7 for all Primary Keys. Implement a hybrid aspect system where core metadata is in JSONB but filterable properties are mirrored in 1:1 relational tables. Add an Actor system supporting both internal DIDs (hosted PDS) and external DIDs (remote PDS) with a background sync worker."