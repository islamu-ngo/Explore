This is a comprehensive architectural plan for **ISLAMU Event**, designed to be project-agnostic, enterprise-grade, and highly modular.
This execution strategy plan outlines the architecture for a highly flexible, open-source, multi-tenant event management platform. It allows for deep cultural/religious customization (e.g., Islamic prayer-based scheduling and gender segregation) without database schema changes or application restarts.

---

## Important to Note:
This does not take into account this repo's convention. Meaning it was only given a little bit of context regarding those technical decisions, so the naming conventions are wrong and other such types of things. Take into account to re-read all the documentation necessary and use all the related skills and agents for this task.

---

# ARCHITECTURE PLAN: ISLAMU MODULAR PLATFORM

## 1. Executive Summary

The goal is to build a self-hostable, multi-tenant event management system that supports extreme flexibility without code modification or application restarts. The system distinguishes between **Instance Administration** (Infrastructure/SaaS Provider) and **Tenant Administration** (Community/Organization), allowing for a hierarchy of control. It utilizes a **Metadata-Driven Aspect Architecture** to handle diverse cultural logic (e.g., Islamic prayer times) and technical configurations (e.g., Render Modes) dynamically.

---

## 2. Hierarchy of Authority & Configuration

The system enforces a strict cascading configuration model.

### 2.1 The Two-Tier Admin Model

* **Tier 1: Instance Administrator (Super Admin)**
* **Scope:** Controls the physical server, global resource limits, enabled modules (globally), and "Enforcement Policies."
* **Capabilities:** Can lock specific settings (e.g., "All Free Tier tenants *must* use WebAssembly") or delegate them.


* **Tier 2: Tenant Administrator (Customer)**
* **Scope:** Controls their specific community instance (data, branding, local logic).
* **Capabilities:** Configures settings *within the bounds* allowed by the Instance Admin.



### 2.2 The Cascading Settings Engine

Settings are resolved at runtime using a **"Fall-Through" Strategy**:

1. **Check Tenant Override:** Does the tenant have a specific setting?
2. **Check Instance Enforcement:** Does the Instance Admin forbid overriding this?
3. **Fall to Default:** Use the system default.

**Database Schema for Settings:**

* `SystemSettings`: (`Key`, `Value`, `IsLocked`, `AllowedValues`)
* `TenantSettings`: (`TenantId`, `Key`, `Value`)

> **Scenario:** The Instance Admin sets `RenderMode` to "Auto" but marks `IsLocked = false`. The Tenant Admin sees this dropdown in their dashboard and can change it to "Server." If `IsLocked = true`, the dropdown is disabled/hidden.

---

## 3. Data Architecture: The Relational "Aspect" Pattern

To support modularity without "JSON dumps," we use a **Table-per-Type (TPT)** approach with strict multi-tenant isolation.

### 3.1 Core Entity Structure

* **`Core.Events`**: The immutable backbone.
* Columns: `Id`, `TenantId`, `Title`, `BaseStartTime`, `CreatedByUserId`.


* **`Core.EventRules`** (Security Policy):
* Defines *who* can post: `AllowUserSubmissions` (bool), `RequireModeration` (bool), `AdminOnlyMode` (bool).



### 3.2 Modular Extension Tables ("Aspects")

Modules add data tables that link to the Core Event ID. They are only queried if the Tenant has the module enabled.

* **`Mod_Islamic.EventAttributes`**: `EventId`, `MadhabId`, `PrayerTimeOffset`, `GenderSegregationMode`.
* **`Mod_Tech.EventAttributes`**: `EventId`, `RepoUrl`, `HackathonTrack`.

### 3.3 Dynamic Taxonomies (User-Defined Data)

For flexibility beyond code modules (e.g., "Local Committee"), we use a metadata schema:

* `TaxonomyDefinitions`: Defines the "Field" (Name, Type, Options).
* `TaxonomyValues`: Stores the "Data" linked to an event.

---

## 4. Logic Architecture: Strategy & Dependency Injection

Business logic (like scheduling) adapts per request without restarting the application.

### 4.1 The "Request-Scoped" Resolver

We do not inject a single `IScheduler`. We inject a **Resolver**.

**Workflow:**

1. **HTTP Request:** User attempts to create an event.
2. **Tenant Context:** Middleware identifies `TenantId`.
3. **Module Check:** System sees `Module = Islamic`.
4. **Strategy Selection:** The Resolver returns `IslamicSchedulingStrategy` (which calculates start time based on Maghrib prayer).

### 4.2 The "Event Posting" Logic Engine

To handle your requirement about *who* can post (Admins vs. Users):

* **Policy Engine:** An `IEventSubmissionPolicy` service runs before saving.
* **Logic:**
* *If `AdminOnlyMode` is True:* Reject all non-admin requests.
* *If `AllowUserSubmissions` is True:* Accept but set `Status = PendingApproval` (if moderation is on) or `Status = FlaggedUserSubmitted`.



---

## 5. UI Architecture: Policy-Based Render Modes

Interactivity is managed via a **Policy Object** injected into the layout, separating "Technical implementation" from "Page Content."

### 5.1 The Classification System

Pages in the code are tagged with a logical type, not a technical mode.

* `PageType.Content` (Blogs, Listings)
* `PageType.Operational` (Dashboards, Editors)

### 5.2 The Resolution Matrix

The **Instance Admin** defines the policy map in their dashboard:

| Policy Name | Operational Pages | Content Pages | Use Case |
| --- | --- | --- | --- |
| **"Performance Saver"** | `WebAssembly` | `Static` | Free Tier / Low Server Cost |
| **"Premium Fast"** | `Server` | `Static` | Paid Tier / High Responsiveness |
| **"Modern Edge"** | `Auto` | `Auto` | High-End Devices |

### 5.3 Implementation (Blazor)

A Wrapper Component reads this configuration:

```razor
@inject IRenderPolicyService PolicyService
@* The component asks: "I am an Operational Page. How should I render?" *@
<DynamicComponent Type="@PageType" @rendermode="@PolicyService.GetMode(PageType.Operational)" />

```

---

## 6. API Architecture: Polymorphic Clients

To ensure the Front-End (Blazor) matches the Back-End (API) flexibly:

* **OpenAPI Discriminators:** The API returns a base `EventDto` containing a list of `Aspects`.
* **Strongly Typed Client:** NSwag generates specific classes (`IslamicAspectDto`, `TechAspectDto`).
* **UI Mapping:** A `ComponentMapper` dictionary tells Blazor: *"When you see `IslamicAspectDto`, render the `IslamicPrayerSlider.razor` component."*

---

## 7. Implementation Roadmap

### Phase 1: The Core Foundation

* **Infrastructure:** Set up the Multi-tenant Middleware (`TenantId` resolution).
* **Admin Dashboard (Instance):** Build the UI to create Tenants and set Global Policies (Locking/Enforcement).
* **Data Layer:** Create `Core.Events` and the "Aspect" base classes.

### Phase 2: The Logic Engine

* **Submission Rules:** Implement the `EventSubmissionPolicy` (Admin vs. User posting logic).
* **Module System:** Build the `ISchedulingStrategy` and the `Mod_Islamic` library.
* **DI Resolver:** Implement the request-scoped service selector.

### Phase 3: The Flexible UI

* **Render Policies:** Implement the `IRenderPolicyService` and the Admin UI to configure it.
* **Dynamic Forms:** Build the "Blueprint" engine where the API sends form definitions to Blazor.
* **Refinement:** Add the "Taxonomy" system for user-defined lookup tables.

# Module Governance Pattern

This is the **"Module Governance Pattern"** implementation. It solves the hierarchy problem by treating capabilities as "Assets" that are distributed downwards from Instance to Tenant to Event.

Here is the Enterprise-Grade implementation plan for **ISLAMU**.

---

### 1. The Core Concept: "Cascading Module Availability"

You must implement a **Three-Tier Policy Engine**. You don't just "enable" features; you define **Allow-Lists** at each level.

* **Tier 1 (Instance):** The Physical Server Owner. They define what is *physically possible* on this installation.
* **Tier 2 (Tenant):** The Community Admin. They define what is *active* for their organization (subset of Tier 1).
* **Tier 3 (Event):** The User/Wizard. They select a *specific use-case* for a single event (subset of Tier 2).

---

### 2. Database Schema Implementation

You need three specific tables to handle this hierarchy and the flexibility you want.

#### A. `ModuleDefinitions` (Static System Data)

The "Catalog" of what your software *can* do.
| Id | Key | Name | Description | StepSchemaEndpoint |
| :--- | :--- | :--- | :--- | :--- |
| 1 | `Mod_Islamic` | Islamic Event | Prayer times, gender segregation | `/api/modules/islamic/wizard-steps` |
| 2 | `Mod_Tech` | Tech Event | Hackathon tracks, GitHub links | `/api/modules/tech/wizard-steps` |

#### B. `InstanceConfig` (The "Super Admin" Rules)

| Key | Value |
| --- | --- |
| `AllowedModules` | `["Mod_Islamic", "Mod_Tech"]` (JSON) |
| `ForceSingleModule` | `null` (If set to "Mod_Islamic", ALL tenants are forced to this) |

#### C. `TenantCapabilities` (The "Community" Rules)

| TenantId | ModuleKey | IsEnabled |
| --- | --- | --- |
| `Tenant_A` | `Mod_Islamic` | `true` |
| `Tenant_A` | `Mod_Tech` | `false` |
| `Tenant_B` | `Mod_Tech` | `true` |

---

### 3. The Onboarding Flows

#### Flow A: Instance Admin Onboarding (First Run)

When the software is installed and the first user (Super Admin) logs in, show a **"Platform Purpose Wizard"**.

1. **Question:** "How will this instance be used?"
* *Option A:* **"Dedicated Community"** (e.g., Only Islamic).
* *Action:* Sets `InstanceConfig.AllowedModules = ["Mod_Islamic"]`.
* *Result:* The "Tech" module is physically disabled for everyone.


* *Option B:* **"SaaS / Hosting Provider"** (Mixed use).
* *Action:* Sets `InstanceConfig.AllowedModules = ["Mod_Islamic", "Mod_Tech", ...]`.
* *Result:* All modules are available for allocation.





#### Flow B: Tenant Onboarding (Created by Instance Admin)

When the Instance Admin creates a new Tenant:

1. **UI:** Shows a checklist of "Available Modules" (filtered by `InstanceConfig`).
2. **Action:** Admin checks "Islamic" and unchecks "Tech" for *this specific tenant*.
3. **Result:** When the Tenant Admin logs in, they *only* see Islamic options. They don't even know the Tech module exists.

---

### 4. The "Event Creation" Wizard Implementation

This is the "Secret Sauce" for the User Experience. Do not build one giant form. Build a **Dynamic Step Sequencer**.

#### Step 1: The "Intent" Selector

* **API Call:** `GET /api/events/available-types`
* *Backend Logic:* Look at `TenantCapabilities` for the current tenant. Return only the enabled modules.


* **UI:** User sees: "What kind of event are you organizing?"
* [ 🕌 Islamic Event ]
* [ 💻 Tech Event ]



#### Step 2: The "Schema Fetch"

* **Action:** User clicks "Islamic Event".
* **API Call:** `GET /api/modules/islamic/schema`
* **Response:**
```json
{
  "wizardSteps": [
    { "step": "Basics", "component": "StandardInfo" },
    { "step": "ReligiousContext", "component": "IslamicSettings" } 
  ]
}

```


* **UI:** The Frontend dynamically loads the `IslamicSettings` component (from your NSwag/Blazor map).

#### Step 3: The Data Save

* The payload saved to the database includes a `Discriminator` or `ModuleKey` so the backend knows which Extension Tables to write to.

---

### 5. Single-Tenant vs. Multi-Tenant Mode

To handle this without maintaining two codebases, use a **"Virtual Tenant" Strategy**.

**In `appsettings.json`:**

```json
"DeploymentMode": "SingleTenant" // or "MultiTenant"

```

**The Logic:**

1. **Multi-Tenant Mode:**
* Tenant ID is resolved from the **Subdomain** (e.g., `mosque.islamu.com`).
* Instance Admin Dashboard is at `admin.islamu.com`.


2. **Single-Tenant Mode:**
* Tenant ID is hardcoded to `DefaultTenant` in the middleware.
* The "Instance Admin" and "Tenant Admin" permissions are **Merged**.
* The "Create Tenant" UI is hidden.
* The "Instance Settings" page simply updates the `DefaultTenant`'s capabilities directly.



**Benefit:** You write the code *once* (for multi-tenant). Single-tenant is just multi-tenant restricted to one ID.

### Summary of Authority Hierarchy

1. **Instance Admin:** "I have installed the Tech and Islamic modules on this server."
2. **Tenant Admin:** "My community is a Mosque, so I only enable the Islamic module."
3. **Event Organizer:** "I am creating a Friday Prayer event." (The Tech options are invisible).

This structure guarantees that **Tenant 1 (Mosque)** never sees a "Hackathon" field, and **Tenant 2 (Tech Hub)** never sees a "Madhab" dropdown, while you (the developer) maintain a single, clean codebase.


# Runtime Deployment Switching

You are asking for a "Masking Strategy." The application is *always* technically multi-tenant internally (to avoid maintaining two codebases), but you want a "Single Tenant Mask" that the admin can toggle ON/OFF at runtime.

Here is the implementation plan for the **Dynamic Instance Governance System**.

### 1. The "Masking" Architecture (Middleware)

Do not use compilation flags (`#if DEBUG`) or environment variables for this. Use a **Database-Driven Middleware** that intercepts requests *before* the Multi-Tenant Resolver kicks in.

#### The "Instance Mode" Middleware Logic

Create a middleware that runs immediately after `Authentication`:

```csharp
public class DeploymentModeMiddleware {
    private readonly RequestDelegate _next;
    private readonly IInstanceConfigService _config; // Caches DB settings for speed

    public async Task InvokeAsync(HttpContext context) {
        var mode = await _config.GetDeploymentModeAsync(); // "Single" or "Multi"

        if (mode == DeploymentMode.SingleTenant) {
            // 1. FORCE the tenant context to the Default Tenant (ID: 1)
            context.Items["TenantId"] = "Default";
            
            // 2. BLOCK access to "Super Admin" routes if they are not relevant
            if (context.Request.Path.StartsWithSegments("/super-admin/tenants")) {
                context.Response.StatusCode = 404; // Pretend it doesn't exist
                return;
            }
        } 
        else {
            // Multi-Tenant Mode: Let the next middleware (TenantResolver) do its job
            // It will parse subdomain/header to find the tenant.
        }

        await _next(context);
    }
}

```

**Why this works without restart:**
The `_config.GetDeploymentModeAsync()` checks a cached value from your database. When you toggle the switch in the Admin Dashboard, you update the DB and invalidate the cache. The *very next request* runs through this middleware, sees "Multi," and suddenly the mask drops.

---

### 2. The Admin Dashboard "Switch" Implementation

You need a dedicated **"Instance Governance"** page in the Super Admin dashboard.

#### The Toggle: "Deployment Mode"

* **Single Tenant Mode (Default):**
* *Backend Effect:* Middleware forces `TenantId = 1`.
* *UI Effect:* Hides the "Tenants" list. The Admin Dashboard looks like a standard app admin panel.


* **Multi-Tenant Mode:**
* *Backend Effect:* Middleware stops forcing ID. Tenant Resolver starts looking at Subdomains (`mosque.app.com`, `tech.app.com`).
* *UI Effect:* The "Tenants" menu item appears.



#### The Toggle: "Onboarding Policy" (Your specific question)

You asked how to control *who* can create tenants (Self-Service vs. Sales Call). This is a second setting in the same table.

**Database Table:** `InstanceSettings`
| Key | Value | Description |
| :--- | :--- | :--- |
| `DeploymentMode` | `Single` | (Single/Multi) |
| `TenantRegistrationPolicy` | `InviteOnly` | (Open/InviteOnly/Closed) |

**The Logic for `TenantRegistrationPolicy`:**

1. **`Open` (Self-Service):** The Login page shows a "Create your Community" button. Anyone can fill a form and spawn a new Tenant.
2. **`InviteOnly` (Sales Call):** The button is hidden. A user must contact sales. The Super Admin manually clicks "Create Tenant" in the dashboard and sends an invite link to the customer.
3. **`Closed`:** No new tenants allowed (Maintenance mode).

---

### 3. The "First Run" Experience (Zero Config)

To make the "First Run" seamless as you requested:

1. **Installation:** The app installs with a seeded SQL Migration.
* `InstanceSettings`: `DeploymentMode = 'Single'`, `TenantRegistrationPolicy = 'Closed'`.
* `Tenants`: Insert 1 row: `Id='Default'`, `Name='My Community'`, `Modules='All'`.


2. **First Launch:**
* User visits `localhost:5000`.
* Middleware sees `Mode=Single`.
* It silently forces `Tenant='Default'`.
* User registers "Admin". They are created as the Admin of the Default Tenant.
* **Result:** It feels exactly like a standard, single-tenant app. They don't even know the multi-tenant engine is purring underneath.



---

### 4. Transitioning to Multi-Tenant (The "Growth" Moment)

One day, the admin decides to host other communities.

1. **Action:** Admin goes to `Settings > Instance`.
2. **Change:** Toggles "Mode" to **Multi-Tenant**.
3. **Save:** Application clears cache.
4. **Instant Result:**
* The "Tenants" sidebar item appears.
* The Admin can now click "Create Tenant" to onboard a second community.
* The middleware stops forcing "Default" and starts routing `community2.islamu.com` to the new tenant.



### Summary Checklist for Implementation

1. **Database:** Create `InstanceSettings` table (Key/Value).
2. **Middleware:** Implement `DeploymentModeMiddleware` before `TenantResolutionMiddleware`.
3. **Caching:** Use `IMemoryCache` for the settings (1-minute expiration or manual invalidation) so you don't hit the DB on every HTTP request.
4. **UI:** Wrap the "Tenants" menu link in a generic `@if (InstanceConfig.IsMultiTenant)` block.
5. **Registration Page:** Wrap the "Start a Community" button in `@if (InstanceConfig.RegistrationPolicy == 'Open')`.

# Optional One-to-One Aspect

This is the exact scenario where standard inheritance (Class `IslamicTechEvent : Event`) breaks down because you would need a new class for every possible combination (Islamic+Tech, Islamic+Medical, Tech+Medical, etc.).

Your intuition about "linking" tables is close, but you don't need a "middleman" mapping table (which is used for Many-to-Many). Instead, you should use the **"Optional One-to-One" (Aspect) Pattern**.

### The Conceptual Shift

Instead of thinking "Is this an Islamic Event?", think "Does this Event *have* Islamic details?"
An event is just a generic container. It can "wear" different hats (Aspects) simultaneously.

### 1. The Database Schema (The Correct "Aspect" Approach)

You do not need an intermediate table. You simply make the Primary Key of the "Aspect" table also serve as the Foreign Key to the core Event.

* **`Events` Table (Core)**
* `Id` (PK)
* `Title`
* `StartDateTime`
* `TenantId`


* **`EventIslamicDetails` Table (Aspect 1)**
* `EventId` (PK & FK to `Events.Id`) <--- *This is the link*
* `MadhabType`
* `PrayerOffsetMinutes`


* **`EventTechDetails` Table (Aspect 2)**
* `EventId` (PK & FK to `Events.Id`)
* `GithubRepoUrl`
* `HackathonTrack`



If `Event #100` is **both** Islamic and Tech, it will simply have a row in *all three tables* sharing the ID `100`.

### 2. Entity Framework Core Implementation

You model this in C# using **Composition**, not Inheritance.

```csharp
// The Core Entity
public class Event
{
    public int Id { get; set; }
    public string Title { get; set; }

    // Navigation Properties (Note: They are nullable/optional)
    public IslamicDetail? IslamicDetail { get; set; }
    public TechDetail? TechDetail { get; set; }
}

public class IslamicDetail
{
    [Key, ForeignKey("Event")]
    public int EventId { get; set; }
    
    public string Madhab { get; set; }
    
    // Navigation back to core
    public Event Event { get; set; }
}

public class TechDetail
{
    [Key, ForeignKey("Event")]
    public int EventId { get; set; }
    
    public string GithubRepo { get; set; }
    
    public Event Event { get; set; }
}

```

### 3. Fetching the Data (The "Super Query")

You asked: *"How can I fetch all information? Do I need to look up where the info is?"*
No. You write **one single query** that attempts to fetch everything. EF Core handles the "mapping" automatically using `LEFT JOINs`.

If an event is *only* Tech, `IslamicDetail` will simply be `null` in the result. You don't need to check "which tables to query" beforehand.

```csharp
var events = await context.Events
    // Eagerly load the "Aspects"
    .Include(e => e.IslamicDetail)
    .Include(e => e.TechDetail)
    .Where(e => e.TenantId == currentTenantId)
    .ToListAsync();

foreach (var evt in events)
{
    // C# automatically knows what the event "is" based on which properties are not null
    if (evt.IslamicDetail != null && evt.TechDetail != null) 
    {
        Console.WriteLine($"Hybrid Event! Madhab: {evt.IslamicDetail.Madhab}, Repo: {evt.TechDetail.GithubRepo}");
    }
    else if (evt.IslamicDetail != null)
    {
        Console.WriteLine("Islamic Only");
    }
}

```

### 4. Why this is better than your "Intermediary Table" idea

Your idea of an intermediary table (EventID, IslamicID, TechID) creates a **Multi-Hop** problem.

* **Your way:** `Event` -> `LinkTable` -> `IslamicTable` (Requires 2 joins, slower).
* **Aspect way:** `Event` -> `IslamicTable` (Requires 1 join, faster).

### 5. Advanced: The "Poly-Aspect" Strategy (For extreme modularity)

If you have 50 different modules and don't want 50 `.Include()` statements, you can flip the logic.

Instead of the API returning one big object, the API returns the **Core Event** and a list of **"Available Aspects"**.

* **API Response for Event #100:**
```json
{
  "id": 100,
  "title": "Islamic Code Ethics",
  "aspects": ["Islamic", "Tech"] // computed column
}

```


* **UI Logic:**
1. UI receives event.
2. UI sees "Islamic" in aspects -> Calls `GET /api/v1/events/100/islamic-details`
3. UI sees "Tech" in aspects -> Calls `GET /api/v1/events/100/tech-details`



**Recommendation:** Stick to **Option 3 (Eager Loading)** for now. SQL databases can easily handle 5-10 `LEFT JOINs` without sweating. It is much simpler to develop.

advanced Entity Framework Core relationships, configure and optimize the one-to-one mapping technique essential for aspect-based design.
