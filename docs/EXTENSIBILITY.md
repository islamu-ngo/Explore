# Extensibility Architecture

> **Metadata-Driven Modular Design**
>
> This document describes the aspect-based extensibility model that enables deep customization
> without database schema changes or application restarts.

**Last Updated**: January 2026

---

## Overview

The platform uses a **Metadata-Driven Aspect Architecture** to handle diverse requirements (cultural, religious, technical) dynamically. This allows:
- Adding new "aspects" to entities without schema changes
- Enabling/disabling features per tenant
- Cultural customization (e.g., Islamic prayer-based scheduling)
- Technical configuration (e.g., render modes, API versioning)

---

## Core Concept: Aspects vs Inheritance

### The Problem with Inheritance

Traditional OOP inheritance creates combinatorial explosion:
- `IslamicEvent`, `TechEvent` - 2 types
- `IslamicTechEvent` - need a third?
- Every combination requires a new class

### The Aspect Solution

Instead of "Is this an Islamic Event?", ask "Does this Event have Islamic details?"

**Key Insight**: An event is a generic container that can "wear" different hats (aspects) simultaneously.

| Approach | Flexibility | Schema Changes | Runtime Cost |
|----------|-------------|----------------|--------------|
| Inheritance | Low | Every combination | None |
| Aspects | High | Never | Minimal |

---

## Data Architecture: The Relational Aspect Pattern

### Core Entity Structure

Core entities contain only universal properties:
- Identity fields (`Id`, `TenantId`)
- Common properties (`Title`, `CreatedAt`)
- Audit fields (`CreatedBy`, `UpdatedAt`)

### Extension Tables (Aspects)

Aspects are optional 1:1 relationships using **shared primary key**:

| Table | Purpose | Links To |
|-------|---------|----------|
| `Events` | Core event data | - |
| `EventIslamicDetails` | Islamic-specific attributes | `Events.Id` |
| `EventTechDetails` | Tech-specific attributes | `Events.Id` |

**Key Design**: The aspect table's primary key IS also its foreign key to the core entity.

### Composition Example

An event that is **both** Islamic and Tech simply has rows in all relevant tables sharing the same ID:

| Event #100 | Has Row In |
|------------|------------|
| Core data | `Events` |
| Islamic data | `EventIslamicDetails` |
| Tech data | `EventTechDetails` |

---

## Module Governance Pattern

### Three-Tier Module Availability

Modules (sets of aspects and logic) cascade through three levels:

| Tier | Scope | Example |
|------|-------|---------|
| **Instance** | What's physically possible | "Server has Islamic + Tech modules" |
| **Tenant** | What's active for this org | "Our mosque only uses Islamic" |
| **Entity** | What's selected for this item | "This event is Islamic-only" |

### Module Visibility Rules

A module appears in UI only if:
1. ✅ Instance Admin has enabled it globally
2. ✅ Tenant Admin has activated it for their community
3. ✅ User has permission to use it

**Result**: Tenant A (Mosque) never sees "Hackathon" field; Tenant B (Tech Hub) never sees "Madhab" dropdown.

---

## Logic Architecture: Request-Scoped Strategies

### Strategy Selection at Runtime

Business logic adapts per-request without application restart:

1. **HTTP Request** → User attempts action
2. **Tenant Context** → Middleware identifies tenant
3. **Module Check** → System determines active modules
4. **Strategy Selection** → Resolver returns appropriate implementation

### Strategy Pattern Examples

| Module | Strategy Interface | Implementation |
|--------|-------------------|----------------|
| Islamic | `ISchedulingStrategy` | Calculates time based on prayer schedule |
| Tech | `ISchedulingStrategy` | Standard datetime scheduling |
| Default | `ISchedulingStrategy` | Fallback implementation |

### Policy Engine

Cross-cutting policies evaluate before operations:
- **Submission Policy**: Can this user create this entity type?
- **Visibility Policy**: Can this user view this entity?
- **Approval Policy**: Does this require moderation?

---

## Dynamic Taxonomies

### User-Defined Metadata

For flexibility beyond compiled modules, use a metadata schema:

| Concept | Purpose |
|---------|---------|
| **Taxonomy Definition** | Defines a field (name, type, allowed values) |
| **Taxonomy Value** | Stores data linked to an entity |

### When to Use

| Scenario | Use Module? | Use Taxonomy? |
|----------|-------------|---------------|
| Complex business logic | ✅ | ❌ |
| Simple categorization | ❌ | ✅ |
| Requires validation | ✅ | ❌ |
| User-defined labels | ❌ | ✅ |

---

## API Architecture: Polymorphic Responses

### Discriminated DTOs

API responses include a base DTO plus a list of active aspects:

| Response Field | Purpose |
|----------------|---------|
| `id`, `title`, etc. | Core entity data |
| `aspects[]` | List of active aspect types |
| `islamicDetails` | Islamic aspect data (if present) |
| `techDetails` | Tech aspect data (if present) |

### Client-Side Mapping

Frontend dynamically renders components based on aspect presence:

| Aspect Type | Renders Component |
|-------------|-------------------|
| `Islamic` | Prayer time selector, Madhab dropdown |
| `Tech` | GitHub link, hackathon track |
| (none) | Generic form only |

---

## UI Architecture: Dynamic Forms

### Blueprint-Driven Forms

The API sends form definitions, not hardcoded UIs:
1. Client requests entity creation form
2. Server returns schema based on tenant's active modules
3. Client renders form dynamically
4. Submission includes only relevant aspect data

### Step Sequencer Pattern

Complex wizards use dynamic step loading:
1. User selects intent ("What type of event?")
2. System fetches module's wizard steps
3. UI loads appropriate components
4. Data saved with module discriminator

---

## Module Development Guidelines

### Adding a New Module

1. **Define Aspect Table** - Schema for module-specific data
2. **Create Strategy Implementations** - Business logic variations
3. **Register in Module Catalog** - Make discoverable to system
4. **Build UI Components** - Forms, displays, wizards
5. **Document Configuration** - Tenant-facing settings

### Module Independence

Modules should be:
- **Self-contained** - No hard dependencies on other modules
- **Gracefully degrading** - Works if dependencies missing
- **Tenant-configurable** - Respects tenant settings
- **Performance-conscious** - Lazy-loaded when possible

---

## Related Documentation

- **[MULTI_TENANCY.md](MULTI_TENANCY.md)** - Tenant isolation model
- **[DOMAIN.md](DOMAIN.md)** - Core entity definitions
- **[RENDER_POLICIES.md](RENDER_POLICIES.md)** - UI flexibility patterns

## Implementation Reference

For code patterns and implementation details:
- **`dotnet-efcore-guidelines`** skill - Optional 1:1 relationships
- **`cqrs-mediatr-guidelines`** skill - Handler patterns for aspects
- **`blazor-ui-conventions`** skill - Dynamic component rendering
