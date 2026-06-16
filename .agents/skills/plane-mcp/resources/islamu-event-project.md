ISLAMU Event Project (`EVENT`)

> **Status:** DRAFT v1 — pending deduplication refinement against existing Plane work items.
> **Generated:** 2026-06-14
> **Source basis:** `docs/PROJECT.md`, `docs/CODEBASE_STRUCTURE.md`, `docs/index.md`, `README.md`, `dev/_journal/journal.md`, `AGENTS.md`, `.claude/contract/intents.yaml`.
> **Scope:** Every implemented capability surfaced by documentation analysis (no deep source-code reading, per user instruction).
> **Author:** Sisyphus (OhMyOpenCode), under user authorization.

---

## 1. Purpose & Methodology

The Plane project `EVENT` (`284d0bcc-083a-4af0-a909-6b1480dec7e2`) currently tracks **less than 5%** of what is actually implemented in the `islamu-ngo/Event` repository. This report closes that gap by enumerating **every capability** that should exist as a Plane work item, with proposed state, labels, priority, and cycle assignment.

**Method:**
1. Catalog every implemented capability surfaced in `docs/` (70 markdown files), `dev/_journal/journal.md`, `README.md`, and `AGENTS.md`.
2. Map each capability to a proposed Plane work item.
3. Assign state by implementation status:
   - **Done** (`836060da-ba98-43a5-b6ab-f05cd9c2769b`) — shipped, verified, in production code paths.
   - **Released** (`0a2eacca-8523-4857-8c82-658dea34b669`) — shipped AND running in a tagged release (reserved for items in `v0.1.0` tag scope).
   - **In Progress** (`06895d7e-4494-4b6b-b16a-30d2ca259c65`) — partially implemented, actively worked.
   - **Backlog** (`51fc7303-1224-4aae-ae89-3d3fcabe3905`) — roadmap / not yet implemented / scaffolding only.
4. Assign cycle: `v0.1.0` for shipped/in-progress; `v1.0.0` for roadmap.

**Pending refinement:** Once the existing Plane work-item list is parsed (delegated extraction in progress), this report will be updated to:
- Mark items already in Plane as `[EXISTING]` with their actual `EVENT-NNN` identifier.
- Recommend state transitions for items present but mislabeled.
- List items in Plane that are NOT in this report for human review.

---

## 2. Reference Data (Use These IDs When Creating Items)

### 2.1 State UUIDs

| State | UUID | Group |
|---|---|---|
| Backlog (DEFAULT) | `51fc7303-1224-4aae-ae89-3d3fcabe3905` | backlog |
| Candidate | `5b913d8f-6344-4597-84be-6a2733d1c9a3` | backlog |
| Todo | `3ad62172-eb5f-4e03-884f-0fa3392211ce` | unstarted |
| Scheduled | `9f559470-5fca-4636-bfae-303ae6b0af35` | unstarted |
| Ready | `49c11eb9-4a06-4776-8260-1431cec9a21c` | unstarted |
| Waiting Dependency | `c72e6e47-e578-4199-ad21-046597da06b2` | unstarted |
| **In Progress** | `06895d7e-4494-4b6b-b16a-30d2ca259c65` | started |
| Review Needed | `b2b6bda9-36e2-44b4-a9ea-d3664352c1b6` | started |
| Changes Requested | `d4931873-45d7-496b-bd5a-6405be7f6ac2` | started |
| Testing | `c207dd60-4ff0-498b-bb41-88dc1c755137` | started |
| Ready for Release | `a4b87a78-305b-4296-a05d-8f817241334e` | started |
| Released | `0a2eacca-8523-4857-8c82-658dea34b669` | completed |
| Monitoring | `520fc0e9-d920-40e5-9cf0-3d6d437d27b4` | completed |
| **Done** | `836060da-ba98-43a5-b6ab-f05cd9c2769b` | completed |
| Cancelled | `bbbe851e-ca9d-47c4-9015-ff436232ba45` | cancelled |
| Won't Do | `63e87952-e120-4563-84b8-6138f6c5304d` | cancelled |
| Replaced | `242303d8-fb84-4338-a1d7-a2de6bdb8e93` | cancelled |

### 2.2 Cycle UUIDs

| Cycle | UUID | Status | Use For |
|---|---|---|---|
| `v0.1.0` | `e8e48050-d259-4808-a13b-5de8f9b68d70` | in-progress (38 issues, 1 done) | Shipped + in-progress capabilities |
| `v1.0.0` | `638fb867-208e-4d50-a006-1bb6ee66cb75` | backlog (18 issues, 0 done) | Roadmap / post-MVP |

### 2.3 Label UUIDs

| Label | UUID |
|---|---|
| Database | `246a0805-4d13-4240-a1db-9c30700303cb` |
| Docs | `de71e194-4605-4de4-a1c5-305eaac19e19` |
| Automation | `6a1d5a15-612e-4657-b6ac-b5e3835901c4` |
| Prototype | `f84e2e50-1325-48d9-992a-7c1da80e5ac7` |
| Research | `2bcaf737-8adb-4aa1-9970-87e7a24ca40f` |
| Marketing | `cafa9b50-3a80-4b6c-bd3e-54c1c8960708` |
| Web App | `113b3ec2-f1ee-442b-a8c2-8fd54222904e` |
| API | `6aa64816-e481-4815-9dd8-e7f2e5efbc04` |
| KMP (mobile & desktop) | `1dbbfddd-801d-499f-b82d-076962df5fdd` |
| Integration | `462822b0-1615-445d-8b82-8e51cec71511` |
| Decentralization | `e8010bda-52ff-49c0-a6c7-4dd2f2a87785` |
| Federation | `980e1183-60e4-43f5-a3ff-2e4b8385ecca` |
| Compliance | `7c524715-6d58-4203-a776-c9634a5a377c` |
| Finance | `6d72f6e7-a520-485c-add6-f91bdaff45ce` |

> **Project ID:** `284d0bcc-083a-4af0-a909-6b1480dec7e2`
> **Workspace slug:** `e0c78c4f-b1b1-418a-830b-e5d5cfb9264e`
