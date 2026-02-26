ABOUTME: Context file for Blazor folder restructure — key decisions, current state, and resume instructions.
ABOUTME: This is a UI-only restructure — Blazor is a thin client consuming the API, not a backend.

# Blazor Folder Restructure — Context

**Last Updated: 2026-02-26**

---

## SESSION PROGRESS (2026-02-26)

### ✅ COMPLETED
- Full analysis of Explore.Blazor and Explore.Blazor.Client folder structures
- Researched Blazor InteractiveAuto best practices (Microsoft docs, community)
- Created restructure plan (UI-focused, not vertical slice)
- Created task checklist

### 🟡 IN PROGRESS
- Awaiting user approval of proposed structure before implementation

### ⚠️ BLOCKERS
- Need user approval of proposed structure
- Should coordinate: no parallel UI work during restructure

---

## Critical Context

**This is a UI-only project.** The Blazor Client is a thin frontend that calls `Explore.API`. There is no business logic in the Blazor layer — services are API proxies, validators mirror server-side rules, helpers format display data.

**The restructure only reorganizes the visual layer** (pages + their components) while keeping the service/infrastructure layer flat. This is NOT vertical slice architecture.

---

## Key Decisions

| # | Decision | Rationale |
|---|----------|-----------|
| 1 | **Co-locate page components WITH pages** (Pages/Events/Components/) | Eliminates the split between Pages/ and Components/ — one folder per domain area |
| 2 | **Keep Services/ as a flat top-level folder** | They're thin API proxies — easy to find by entity name |
| 3 | **Add Services/Lookup/ and Services/Http/ subfolders** | Group the 8 lookup wrappers and 4 HTTP handlers separately |
| 4 | **Create Shared/ for cross-domain components** | Loading, ErrorState, S3Image — used across many features |
| 5 | **Pluralize domain folders** | `Pages/Events/` not `Pages/Event/` — matches API convention |
| 6 | **Separate Dialogs/ from Components/** within page folders | MudBlazor dialogs are conceptually different from inline components |
| 7 | **Keep Layout/, Helpers/, Validators/, Models/, Constants/ unchanged** | Already well-organized — don't fix what isn't broken |
| 8 | **Delete empty folders** | Extensions/, Serialization/ are dead weight |
| 9 | **Move misplaced dialogs out of Pages/** | InviteMemberDialog, EditMemberRoleDialog are components, not pages |

---

## What Moves (Summary)

| From | To | File Count |
|------|----|-----------|
| `Components/Event/*` | `Pages/Events/Components/` + `Pages/Events/Dialogs/` | ~16 files |
| `Components/Admin/*` | `Pages/Admin/{Area}/Components/` + `Pages/Admin/Dialogs/` | ~25 files |
| `Components/Settings/*` | `Pages/User/Components/` | 5 files |
| `Components/` (root loose files) | `Shared/` or `Pages/Events/Components/` | ~8 files |
| `Pages/Organization/` dialogs | `Pages/Organizations/Dialogs/` | 2 files |
| `Pages/Event/` | `Pages/Events/` (rename to plural) | 7 pages |
| `Pages/Organization/` | `Pages/Organizations/` (rename to plural) | 9 files |
| Lookup services | `Services/Lookup/` | 8 files |
| HTTP handlers | `Services/Http/` | 4 files |

## What Does NOT Move

- **Layout/** — already clean
- **Services/** (domain proxy files at root) — stay flat
- **Helpers/** — already well-organized
- **Validators/** — already well-organized
- **Models/** — already well-organized
- **Constants/** — already well-organized
- **Routing/Guards/** — already well-organized
- **Clients/** — auto-generated
- **Configuration/** — single file
- **Providers/** — single file

---

## Key Files That Will Change

| File | Why |
|------|-----|
| `_Imports.razor` | New namespaces for Pages/Events, Pages/Organizations, Shared |
| `Routes.razor` | Component type references change when pages move to Pages/Events/ |
| `Program.cs` | Some service registrations may need namespace updates |
| Test project `using` statements | Must match new namespaces |

---

## Quick Resume

1. Read this file
2. Read `blazor-folder-restructure-tasks.md` for checklist
3. Read `blazor-folder-restructure-plan.md` for full proposed structure
4. Start with Phase 1 (create Shared/, move loose components)
5. Build + test after each phase
