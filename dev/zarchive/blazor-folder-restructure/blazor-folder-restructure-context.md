ABOUTME: Context file for Blazor folder restructure — key decisions, current state, and resume instructions.
ABOUTME: This is a UI-only restructure — Blazor is a thin client consuming the API, not a backend.

# Blazor Folder Restructure — Context

**Last Updated: 2026-02-27**

---

## SESSION PROGRESS (2026-02-27)

### ✅ COMPLETED
- Full folder/namespace migration completed for `Explore.Blazor.Client`:
  - `Pages/Event` -> `Pages/Events`, `Pages/Organization` -> `Pages/Organizations`
  - `Components/*` moved into feature `Pages/*/Components` and `Pages/*/Dialogs`
  - Shared primitives moved to `Shared/`
  - Services reorganized into `Services/Lookup` and `Services/Http`
- Added feature-scoped `_Imports.razor` files:
  - `Explore.Blazor.Client/Pages/Events/_Imports.razor`
  - `Explore.Blazor.Client/Pages/Organizations/_Imports.razor`
  - `Explore.Blazor.Client/Pages/Admin/_Imports.razor`
  - `Explore.Blazor.Client/Pages/User/_Imports.razor`
  - `Explore.Blazor.Client/Shared/_Imports.razor`
- Added dialog static helper pattern in code-behind (`.razor.cs`) for moved dialogs and updated call sites.
- Cleaned residual empty migration folders:
  - `Explore.Blazor.Client/Components/` tree
  - `Explore.Blazor.Client/Pages/Events/Event`
  - `Explore.Blazor.Client/Pages/Organizations/Organization`
  - `Explore.Blazor.Client/Pages/Admin/TenantSettings`
- Fixed post-migration test compile break caused by stale test imports:
  - `Explore.Blazor.Client.Tests/_Imports.razor` now uses `Explore.Blazor.Client.Shared`
  - `Explore.Blazor.Client.Tests/GlobalUsings.cs` now includes lookup namespaces
- Verification completed:
  - `dotnet build --configuration Release --verbosity quiet` (pass, warnings only)
  - `dotnet test --project Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --verbosity quiet` (pass, warnings only)
  - orphan `.razor.css` check (no orphans)

### 🟡 IN PROGRESS
- Documentation handoff sync across `dev/active/*` and journal updates for context reset continuity.

### ⚠️ BLOCKERS
- No technical blocker on restructure itself.
- Remaining non-blocking noise: pre-existing analyzer/nullability warnings across solution.

---

## This Session: Key Decisions

1. Keep dialog invocation logic in `.razor.cs` partials via static `ShowAsync(...)`; avoid inline `.razor` helper logic.
2. Treat stale test imports as migration fallout and fix immediately to restore runnable test loop.
3. Prefer continuity updates in every active task doc rather than only the active folder, so reset recovery is deterministic.

---

## Files Modified This Session (high-signal)

- `Explore.Blazor.Client.Tests/_Imports.razor` - replaced stale `Explore.Blazor.Client.Components` using with `Explore.Blazor.Client.Shared`.
- `Explore.Blazor.Client.Tests/GlobalUsings.cs` - added lookup namespace imports to match moved service and contract files.
- `dev/active/blazor-folder-restructure/blazor-folder-restructure-context.md` - refreshed execution state, blockers, and resume protocol.
- `dev/active/blazor-folder-restructure/blazor-folder-restructure-tasks.md` - added execution-status section and current remaining work.
- `dev/_journal/journal.md` - captured migration pitfalls, verification approach, and deferred work.
- `dev/_journal/MAJOR_DECISIONS.md` - recorded session-level decisions for folder restructure completion and handoff policy.

---

## Unfinished Work / Follow-up

1. Synchronize docs that still describe this track as "awaiting approval" or "not started".
2. If needed by release criteria, run full mandatory suite from `CLAUDE.md` (all test projects, not only Blazor tests).
3. Optional quality pass: reduce pre-existing MudBlazor/analyzer/nullability warnings in separate scope.

---

## Handoff Notes

- **Current working focus:** documentation continuity updates (not active code implementation).
- **Exact files actively edited in this handoff step:**
  - `dev/active/blazor-folder-restructure/blazor-folder-restructure-context.md`
  - `dev/active/blazor-folder-restructure/blazor-folder-restructure-tasks.md`
  - `dev/_journal/journal.md`
  - `dev/_journal/MAJOR_DECISIONS.md`
- **Uncommitted changes needing attention:** large existing working tree remains from restructure and docs refresh; verify scope before commit.

---

## Restart Commands (copy/paste)

```bash
dotnet build --configuration Release --verbosity quiet
dotnet test --project Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --verbosity quiet
```

If full mandatory validation is required by policy, run all project-level tests listed in `CLAUDE.md`.

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
| 10 | **Feature-level `_Imports.razor`** | Each domain folder auto-imports its Components/ + Dialogs/ — keeps root lean, localizes scope |
| 11 | **Shared/ is strict generic-only boundary** | No domain logic; all `[Parameter]`s documented with `<summary>`; "could it work in another project?" test |
| 12 | **All dialogs expose static `ShowAsync`** | Encapsulates DialogParameters/DialogOptions inside the dialog; pages call one typed method |

---

## Blazor-Specific Gotchas (Non-Negotiable Verification)

These are the silent failure points. Run checks after EVERY batch of file moves:

| # | Gotcha | Symptom | Prevention |
|---|--------|---------|------------|
| 1 | **`_Imports.razor` cascade gap** | "Component 'X' was not found" — cryptic, looks like component doesn't exist | Create feature-level `_Imports.razor` BEFORE moving files |
| 2 | **`@page` directives vs Routes.razor** | Routes break if Routes.razor has hardcoded typeof() namespace refs | Grep Routes.razor after page moves |
| 3 | **Dialog type refs in .cs code-behind** | Runtime failure — .razor.cs files don't inherit from `_Imports.razor` | Grep all `ShowAsync<Dialog>` / `Show<Dialog>` call sites BEFORE each dialog move |
| 4 | **CSS isolation file pairing** | Styles silently disappear — zero errors | Always move .razor + .razor.cs + .razor.css as a unit; run orphan check |
| 5 | **Stale `_Imports.razor` files** | Old usings resolve to deleted folders or conflict | Delete old `_Imports.razor` when moving out of a folder |

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
| `_Imports.razor` (root) | Slim down — remove old Component/Page namespaces, rely on cascading feature imports |
| `Pages/Events/_Imports.razor` (NEW) | Cascading: auto-imports Events/Components/ + Events/Dialogs/ |
| `Pages/Organizations/_Imports.razor` (NEW) | Cascading: auto-imports Organizations/Dialogs/ |
| `Pages/Admin/_Imports.razor` (NEW) | Cascading: auto-imports all Admin sub-area Components/ + Dialogs/ |
| `Pages/User/_Imports.razor` (NEW) | Cascading: auto-imports User/Components/ |
| `Routes.razor` | Component type references change when pages move to Pages/Events/ |
| `Program.cs` | Some service registrations may need namespace updates |
| Every dialog .razor/.razor.cs | Add static `ShowAsync` method |
| Every `.cs` file calling dialogs | Update `using` + migrate to `ShowAsync` call pattern |
| Test project `using` statements | Must match new namespaces |

---

## Quick Resume

1. Read this file and `blazor-folder-restructure-tasks.md` first.
2. Run:
   - `dotnet build --configuration Release --verbosity quiet`
   - `dotnet test --project Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --verbosity quiet`
3. If green, continue with documentation alignment and final scope validation before commit/PR.
