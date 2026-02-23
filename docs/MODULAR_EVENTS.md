# Modular Event Architecture

> **Cultural and Domain-Specific Event Customization**
>
> This document describes how the platform supports deep customization for different
> cultural, religious, and domain-specific event types through the aspect pattern.

**Last Updated**: February 2026

---

## Overview

Events are the core aggregate of the platform. Rather than using inheritance to handle different event types (Islamic, Tech, Educational, etc.), the platform uses a **composition-based aspect pattern** that allows:

- Any event to have multiple "aspects" simultaneously
- Cultural customization without schema changes
- Domain-specific fields added per tenant
- Graceful degradation when aspects are unavailable

---

## The Aspect Pattern for Events

### Core Event vs Aspects

| Component | Contains | Example |
|-----------|----------|---------|
| **Core Event** | Universal properties | Title, date, organization, status |
| **Islamic Aspect** | Religious customization | Madhab, prayer time offset, gender segregation |
| **Tech Aspect** | Technical events | Skill level, tech stack tags, hackathon flags |
| **Educational Aspect** | Learning events | Certification, prerequisites |

### Why Not Inheritance?

| Scenario | Inheritance Approach | Aspect Approach |
|----------|---------------------|-----------------|
| Islamic Conference | `IslamicEvent` class | Event + Islamic aspect |
| Tech Hackathon | `TechEvent` class | Event + Tech aspect |
| Islamic Tech Conference | `IslamicTechEvent` class? | Event + Islamic aspect + Tech aspect |
| 5 event types | 31 possible classes | 5 aspect tables |

**Aspect pattern scales linearly; inheritance scales exponentially.**

---

## Islamic Event Customization

### Islamic-Specific Attributes

| Attribute | Purpose | Values |
|-----------|---------|--------|
| **Madhab** | Jurisprudence school | Hanafi, Maliki, Shafi'i, Hanbali, Mixed |
| **Reference Prayer** | Prayer used for scheduling | Fajr, Sunrise, Dhuhr, Asr, Maghrib, Isha |
| **Prayer Offset** | Schedule relative to prayer | Minutes before/after reference prayer |
| **Gender Segregation** | Attendance policy | Men-only, Women-only, Segregated, Mixed, Family |
| **Quran Recitation** | Includes recitation | Boolean flag |
| **Primary Language** | Islamic content language | Language lookup ID |

### Prayer-Based Scheduling

Instead of absolute times, events can be scheduled relative to prayer times:

| Scheduling Mode | Description |
|-----------------|-------------|
| **Absolute** | Standard datetime (default) |
| **After Prayer** | X minutes after specified prayer |
| **Before Prayer** | X minutes before specified prayer |
| **Between Prayers** | Between two specified prayers |

**Benefit**: Same event "30 minutes after Maghrib" automatically adjusts for different locations and dates.

### Gender Segregation Modes

| Mode | Description | Facility Requirements |
|------|-------------|----------------------|
| **Men Only** | Restricted to men | Single section |
| **Women Only** | Restricted to women | Single section |
| **Segregated** | Both attend, separate areas | Dual sections, barriers |
| **Family** | Families together, singles separate | Family + singles sections |
| **Mixed** | No segregation | Open seating |

---

## Tech Event Customization

### Tech-Specific Attributes

| Attribute | Purpose | Example |
|-----------|---------|---------|
| **Repository URL** | Code repository link | GitHub, GitLab URL |
| **Hackathon Track** | Competition category | AI, Web, Mobile |
| **Skill Level** | Required expertise | AllLevels, Beginner, Intermediate, Advanced |
| **Stack Tags** | Technologies used | .NET, React, Python |
| **Requires Laptop** | Device requirement | Boolean flag |
| **Coding Competition** | Competitive format | Boolean flag |

---

## Event Type Resolution

### Determining Active Aspects

When loading an event:

1. **Core data** always loaded
2. **Aspect presence** determined by existence of aspect record
3. **UI components** rendered based on present aspects
4. **Validation** applies aspect-specific rules

### API Response Structure

Events include metadata about active aspects:

| Field | Purpose |
|-------|---------|
| `id`, `title`, etc. | Core event data |
| `aspects[]` | List of active aspect identifiers |
| `islamicDetails` | Islamic aspect data (if present) |
| `techDetails` | Tech aspect data (if present) |

---

## Module-Based Aspect Availability

### Tenant Configuration

Tenants enable aspects through module configuration:

| Tenant Type | Enabled Modules | Available Aspects |
|-------------|-----------------|-------------------|
| Mosque | Core, Islamic | Basic + Islamic |
| Tech Community | Core, Tech | Basic + Tech |
| University | Core, Islamic, Educational | Basic + Islamic + Educational |

### Aspect Visibility

Aspects appear in UI only when:
1. ✅ Module enabled at instance level
2. ✅ Module activated for tenant
3. ✅ User has permission to use it

**Result**: A tech community never sees "Madhab" dropdown; a mosque never sees "GitHub Repo" field.

---

## Creating Events with Aspects

### Event Creation Flow

1. **Select Intent** → "What type of event?" (filters by tenant's modules)
2. **Core Details** → Title, description, organization
3. **Aspect Details** → Dynamic forms based on selected aspects
4. **Review & Publish** → Validation across all aspects

### Dynamic Form Generation

The API returns form schema based on active aspects:

| Request | Response |
|---------|----------|
| `GET /api/events/schema` | Core fields only |
| `GET /api/events/schema?aspects=islamic` | Core + Islamic fields |
| `GET /api/events/schema?aspects=islamic,tech` | Core + Islamic + Tech fields |

---

## Validation Across Aspects

### Aspect-Specific Validation

Each aspect can contribute validation rules:

| Aspect | Validation Rule | Example |
|--------|-----------------|---------|
| Islamic | Prayer time must be valid | Can't schedule during Jumah prayer |
| Tech | Repository URL must be accessible | GitHub URL returns 200 |
| Educational | Prerequisites must exist | Referenced courses must exist |

### Cross-Aspect Validation

Some validations span multiple aspects:

| Rule | Aspects Involved |
|------|------------------|
| "Women-only tech workshop" | Islamic (gender) + Tech (track) |
| "Prayer break during hackathon" | Islamic (prayer) + Tech (hackathon) |

---

## Querying Events by Aspect

### Filtered Queries

Events can be queried by aspect presence:

| Query | Returns |
|-------|---------|
| All events | Events regardless of aspects |
| Islamic events | Events with Islamic aspect (when module enabled) |
| Tech events | Events with Tech aspect (when module enabled) |
| Islamic tech events | Events with both aspects (when both modules enabled) |

### Aspect-Specific Filters

Within an aspect, specific filters apply:

| Filter | Aspect | Example |
|--------|--------|---------|
| By Madhab | Islamic | "Hanafi events only" |
| By Gender | Islamic | "Women-only events" |
| By Prayer | Islamic | "Maghrib-relative events" |
| By Recitation | Islamic | "Includes Quran recitation" |
| By Skill Level | Tech | "Beginner-friendly" |
| By Stack | Tech | ".NET events" |
| By Competition | Tech | "Hackathon events" |

### Module-Conditional Filters

Aspect filters in the event list API are **silently ignored** when the corresponding module is disabled for the tenant. This keeps the endpoint stable across tenant configurations while preventing invalid combinations.

---

## Extending with New Aspects

### Adding a New Aspect

To add a new event aspect (e.g., "Medical"):

1. **Define Aspect Table** → `EventMedicalDetails` with medical-specific fields
2. **Create Module** → Package aspect with logic and UI
3. **Register** → Add to module catalog
4. **Enable** → Instance/tenant can activate

### Aspect Independence

Aspects should be:
- **Self-contained** → No dependencies on other aspects
- **Gracefully optional** → Events work without any aspects
- **Backward compatible** → New aspects don't break existing events

---

## Related Documentation

- **[EXTENSIBILITY.md](EXTENSIBILITY.md)** - General aspect architecture
- **[MULTI_TENANCY.md](MULTI_TENANCY.md)** - Tenant module configuration
- **[DOMAIN.md](DOMAIN.md)** - Entity relationships

## Implementation Reference

For code patterns:
- **`dotnet-efcore-guidelines`** skill - Optional 1:1 relationships
- **`cqrs-mediatr-guidelines`** skill - Handler patterns for aspects
