---
name: plane-mcp
description: Use Plane MCP tools to manage ISLAMU Event project work items — create, update, search, transition states, assign cycles/labels, and maintain project tracking hygiene.
type: tool
enforcement: suggest
priority: high
---

<!-- ABOUTME: Teaches AI agents how to use the Plane MCP integration for the ISLAMU Event project. -->
<!-- ABOUTME: Covers tool inventory, project context (UUIDs), common workflows, batch patterns, and dedup hygiene. -->

## Purpose

This skill teaches AI agents to use the Plane (plane.so) MCP integration to manage the ISLAMU Event project's work items. It covers the full lifecycle: creating items, updating states, searching before creating (dedup), assigning cycles and labels, and bulk operations.

The ISLAMU Event project in Plane historically had <5% of actually-implemented features tracked. This skill ensures agents keep Plane synchronized with the codebase going forward.

**Always read `./resources/islamu-event-project.md` first** — it contains the canonical project ID, state UUIDs, cycle UUIDs, and label UUIDs needed for every MCP call.

## When to Load

Load this skill when the user says any of:

- "create a work item", "add to plane", "track in plane", "put this in the backlog"
- "update the ticket", "move to done", "change the state", "transition"
- "search plane", "find the work item", "does this exist in plane?"
- "assign to cycle", "add labels", "set priority"
- "EVENT-NNN" (referencing a specific work item by sequence ID)
- "project management", "tracker", "what's in the backlog"
- "synchronize plane", "sync the tracker", "plane is out of date"
- Any task involving the `plane_*` MCP tools

## When NOT to Load

- Code changes that don't need project tracking (use the relevant code skill instead)
- GitHub issue/PR management (use GitHub MCP tools, not Plane)
- General discussion about features without a tracking action
- Reading or writing documentation files (use docs-related skills)

## Must-Read Resource

**`./resources/islamu-event-project.md`** — Contains all canonical UUIDs for the ISLAMU Event project. Read this BEFORE any MCP call. Key values:

| Entity | Value |
|---|---|
| **Project ID** | `284d0bcc-083a-4af0-a909-6b1480dec7e2` |
| **Project Identifier** | `EVENT` |
| **Workspace slug** | `e0c78c4f-b1b1-418a-830b-e5d5cfb9264e` |

The resource file also contains all 17 state UUIDs, 2 cycle UUIDs, and 14 label UUIDs. Reference it instead of hardcoding.

## Top 5 Invariants

1. **ALWAYS search before creating.** Use `plane_list_work_items` or `plane_search_work_items` to check for duplicates. Plane already has 175+ items — creating duplicates is the #1 hygiene failure.

2. **Use the correct state UUID.** States are referenced by UUID, not name. The resource file maps names → UUIDs. Common states:
   - **Done**: `836060da-ba98-43a5-b6ab-f05cd9c2769b`
   - **In Progress**: `06895d7e-4494-4b6b-b16a-30d2ca259c65`
   - **Backlog** (default): `51fc7303-1224-4aae-ae89-3d3fcabe3905`
   - **Todo**: `3ad62172-eb5f-4e03-884f-0fa3392211ce`

3. **Always assign labels.** Items without labels are unfilterable. Use the label UUIDs from the resource file. Pick labels by domain:
   - API work → `API` label
   - Blazor/UI → `Web App` label
   - Database/EF Core → `Database` label
   - Auth/security → `Compliance` label
   - External services → `Integration` label

4. **Assign cycles after creation.** `plane_create_work_item` does NOT accept a cycle parameter. Create the item first, then use `plane_add_work_items_to_cycle` to assign it to v0.1.0 (shipped) or v1.0.0 (planned).

5. **Use HTML for descriptions.** The `description_html` parameter accepts HTML. Write descriptions as `<p>...</p>` blocks. Include file paths, class names, and architectural context so the work item is self-documenting.

## Top 5 Anti-Patterns

1. **Creating without searching.** Plane has 175+ items. Always run `plane_search_work_items(query="keyword")` or `plane_list_work_items(query="keyword")` first. If a similar item exists, update it instead of creating a duplicate.

2. **Wrong state for shipped features.** Features that are already implemented in the codebase should be `Done`, not `Backlog` or `Todo`. Check the codebase before choosing a state.

3. **Missing labels.** An item with zero labels is invisible in filtered views. Always assign at least one label matching the work's domain.

4. **Forgetting cycle assignment.** New items default to `cycle=none`. After creating, batch-assign to the appropriate cycle:
   - Shipped/released features → `v0.1.0` (`e8e48050-d259-4808-a13b-5de8f9b68d70`)
   - Planned/future features → `v1.0.0` (`638fb867-208e-4d50-a006-1bb6ee66cb75`)

5. **Using sequence IDs where UUIDs are required.** `plane_update_work_item` and `plane_add_work_items_to_cycle` require UUIDs, not `EVENT-NNN` sequence IDs. Use `plane_retrieve_work_item_by_identifier` to resolve `EVENT-NNN` → UUID first.

## Tool Quick Reference

### Work Item CRUD

| Action | Tool | Key Parameters |
|---|---|---|
| Create | `plane_create_work_item` | `project_id`, `name`, `state`, `labels[]`, `priority`, `description_html` |
| Get by UUID | `plane_retrieve_work_item` | `project_id`, `work_item_id` |
| Get by EVENT-NNN | `plane_retrieve_work_item_by_identifier` | `project_identifier="EVENT"`, `issue_identifier=<number>` |
| Update | `plane_update_work_item` | `project_id`, `work_item_id`, `state`/`labels`/`priority`/`name`/`description_html` |
| Delete | `plane_delete_work_item` | `project_id`, `work_item_id` |
| List/filter | `plane_list_work_items` | `project_id`, optional filters: `state_ids`, `assignee_ids`, `label_ids`, `cycle_ids`, `priorities`, `query` |
| Search workspace-wide | `plane_search_work_items` | `query` (searches all projects) |

### State Management

| Action | Tool |
|---|---|
| List states | `plane_list_states` |
| Create state | `plane_create_state` |
| Update state | `plane_update_state` |

### Cycle Management

| Action | Tool | Key Parameters |
|---|---|---|
| List cycles | `plane_list_cycles` | `project_id` |
| Add items to cycle | `plane_add_work_items_to_cycle` | `project_id`, `cycle_id`, `issue_ids[]` |
| List items in cycle | `plane_list_cycle_work_items` | `project_id`, `cycle_id` |
| Remove from cycle | `plane_remove_work_item_from_cycle` | `project_id`, `cycle_id`, `work_item_id` |
| Transfer between cycles | `plane_transfer_cycle_work_items` | `project_id`, `cycle_id`, `new_cycle_id` |

### Label Management

| Action | Tool |
|---|---|
| List labels | `plane_list_labels` |
| Create label | `plane_create_label` |

### Epic / Module / Milestone

| Action | Tool |
|---|---|
| Create epic | `plane_create_epic` |
| List epics | `plane_list_epics` |
| Create module | `plane_create_module` |
| Add items to module | `plane_add_work_items_to_module` |
| Create milestone | `plane_create_milestone` |
| Add items to milestone | `plane_add_work_items_to_milestone` |

### Comments / Links / Relations / Work Logs

| Action | Tool |
|---|---|
| Add comment | `plane_create_work_item_comment` |
| List comments | `plane_list_work_item_comments` |
| Add link | `plane_create_work_item_link` |
| Create relation (blocking, duplicate, etc.) | `plane_create_work_item_relation` |
| Log work time | `plane_create_work_log` |

### Project / Workspace

| Action | Tool |
|---|---|
| List projects | `plane_list_projects` |
| Get current user | `plane_get_me` |
| Get project members | `plane_get_project_members` |

## Core Workflows

### Workflow 1: Create a New Work Item (Safe Pattern)

```
Step 1: SEARCH for duplicates
  plane_search_work_items(query="keyword from title")

Step 2: If no duplicate, CREATE
  plane_create_work_item(
    project_id="284d0bcc-083a-4af0-a909-6b1480dec7e2",
    name="Descriptive title",
    state="836060da-ba98-43a5-b6ab-f05cd9c2769b",  // Done
    labels=["6aa64816-e481-4815-9dd8-e7f2e5efbc04"],  // API
    priority="high",
    description_html="<p>Detailed description with file paths and context.</p>"
  )

Step 3: ASSIGN TO CYCLE (separate call)
  plane_add_work_items_to_cycle(
    project_id="284d0bcc-...",
    cycle_id="e8e48050-...",  // v0.1.0
    issue_ids=["<UUID from step 2>"]
  )

Step 4: VERIFY
  plane_retrieve_work_item(project_id=..., work_item_id="<UUID>")
```

### Workflow 2: Update an Existing Item's State

```
Step 1: RESOLVE EVENT-NNN → UUID
  plane_retrieve_work_item_by_identifier(
    project_identifier="EVENT",
    issue_identifier=371
  )
  → Returns work_item UUID

Step 2: UPDATE STATE
  plane_update_work_item(
    project_id="284d0bcc-...",
    work_item_id="<UUID>",
    state="836060da-ba98-43a5-b6ab-f05cd9c2769b"  // Done
  )
```

### Workflow 3: Bulk-Create Multiple Items

For 5+ items, use **parallel tool calls** in a single response. Each `plane_create_work_item` is independent.

```
// Issue 5+ plane_create_work_item calls in ONE response
// Then collect all UUIDs from responses
// Then batch-assign cycles with plane_add_work_items_to_cycle(issue_ids=[...])
```

For 20+ items, **delegate to a subagent** via `task(category="unspecified-high", ...)` with the full item list and reference UUIDs. The subagent handles creation without consuming parent context.

### Workflow 4: Find What's Already Tracked

```
// List all items in the project
plane_list_work_items(project_id="284d0bcc-...", per_page=100)

// Filter by state
plane_list_work_items(project_id=..., state_ids=["836060da-..."])  // Done only

// Filter by label
plane_list_work_items(project_id=..., label_ids=["6aa64816-..."])  // API only

// Search by keyword
plane_search_work_items(query="authentication")
```

### Workflow 5: Assign Labels to Existing Items

```
Step 1: Get item UUID (retrieve by identifier or list)
Step 2: plane_update_work_item(
  project_id=...,
  work_item_id=...,
  labels=["6aa64816-...", "7c524715-..."]  // API + Compliance
)
```

## Priority Values

| Priority | When to Use |
|---|---|
| `urgent` | Security, auth, production-blocking |
| `high` | Core features, infrastructure, test coverage |
| `medium` | Polish, optimizations, documentation |
| `low` | Nice-to-have, research, inspiration |
| `none` | Default if unsure |

## Label Selection Guide

| Work Domain | Label(s) |
|---|---|
| API controllers, endpoints, middleware | `API` |
| Blazor pages, components, WASM client | `Web App` |
| EF Core, DbContext, migrations, repositories | `Database` |
| Cerbos, auth, secrets, security headers | `Compliance` |
| External services (S3, SMTP, RabbitMQ, federation) | `Integration` |
| Federation, ActivityPub, ATProto, PDS sync | `Federation`, `Decentralization` |
| CI/CD, scripts, background workers | `Automation` |
| Documentation, ADRs, guides | `Docs` |
| Research, spikes, investigations | `Research` |
| Prototypes, PoCs | `Prototype` |
| KMP (Kotlin Multiplatform) mobile/desktop | `KMP` |
| Marketing, SEO, public-facing | `Marketing` |
| Billing, subscriptions, payments | `Finance` |

## Batch Operations

### Parallel Creation (5-8 items per response)

Issue multiple `plane_create_work_item` calls in a single assistant response. The MCP server processes them in parallel. Collect UUIDs from each response for cycle assignment.

### Subagent Delegation (20+ items)

For large bulk operations, delegate to a `task(category="unspecified-high")` subagent:

```
task(
  category="unspecified-high",
  prompt="
    1. TASK: Create N Plane work items via plane_create_work_item
    2. REFERENCE: Project ID=284d0bcc-..., Done state=836060da-..., labels=[...]
    3. ITEMS: [full list with titles, descriptions, labels]
    4. MUST DO: Search before create, assign labels, return EVENT-NNN mapping
    5. MUST NOT: Create duplicates, skip labels
  "
)
```

The subagent handles the repetitive MCP calls and returns a summary mapping. This preserves parent agent context.

## Verification Hooks

After any bulk operation, verify:

```
// Count total items
plane_list_work_items(project_id=..., per_page=100)
→ Check total count matches expectation

// Check state distribution
plane_list_work_items(project_id=..., state_ids=["836060da-..."])  // Done count
plane_list_work_items(project_id=..., state_ids=["06895d7e-..."])  // In Progress count

// Check for items without cycles
plane_list_work_items(project_id=..., per_page=100)
→ Filter for cycle_ids == null in results

// Check for items without labels
→ Filter for labels == [] in results
```

## Common Pitfalls

### "I can't find the UUID for EVENT-371"

Use `plane_retrieve_work_item_by_identifier(project_identifier="EVENT", issue_identifier=371)`. The response includes the full `id` (UUID) and `sequence_id`.

### "The item was created but has no cycle"

`plane_create_work_item` does NOT accept a `cycle` parameter. After creation, use `plane_add_work_items_to_cycle(project_id=..., cycle_id=..., issue_ids=[...])` to assign.

### "I get an error when updating by EVENT-NNN"

`plane_update_work_item` requires `work_item_id` (UUID), not the sequence ID. Resolve first with `plane_retrieve_work_item_by_identifier`.

### "Labels aren't being assigned"

The `labels` parameter expects an array of label **UUIDs**, not names. Use the UUIDs from `./resources/islamu-event-project.md`. Example: `labels=["6aa64816-e481-4815-9dd8-e7f2e5efbc04"]` not `labels=["API"]`.

## Related Skills

- **`conventional-commit`** — When committing work that relates to a Plane item, reference the EVENT-NNN in the commit message.
- **`cqrs-mediatr-guidelines`** — When creating work items for handler/query work, use the correct CQRS terminology in the title.
- **`clean-architecture-rules`** — When creating work items for architecture changes, reference the layer boundaries.
- **`skill-authoring`** — When updating this skill or creating new skills.

## Change Log

| Date | Change |
|---|---|
| 2026-06-14 | Initial comprehensive skill created. Replaced 9-line skeleton. Covers full MCP tool inventory, 5 core workflows, batch patterns, label guide, and verification hooks. |
