# Context: Navbar Customization

## Key Files
- `Explore.Domain/Tenant.cs` - Core entity.
- `Explore.Blazor.Client/Layout/NavMenu.razor` - The component to modify.
- `Explore.API/Controllers/TenantController.cs` - API endpoint location.
- `Explore.Persistence/ExploreDbContext.cs` - DB Context.

## Architecture Decisions
- **Entity Name**: `TenantNavigationLink` to allow future expansion (e.g., specific icons, permission roles).
- **Storage**: Relational table (not JSON) to allow easy querying/ordering.
- **UI Lib**: MudBlazor `MudNavLink` for rendering.
- **Admin UI**: MudBlazor `MudTable` or `MudList` with Up/Down reordering controls.

## Dependencies
- **Authentication**: Admin features require `TenantAdmin` role.
- **Multi-tenancy**: Must strictly use `ITenantContext` to resolve `TenantId`.

## References
- [MudBlazor NavMenu Docs](https://mudblazor.com/components/navmenu)
- [MudBlazor Icons](https://mudblazor.com/features/icons)
## Context Reset Session Update (2026-02-15 21:25 Europe/Brussels)

- Current implementation state: No new implementation changes in this session for this track.
- Key decisions made this session: Priority shifted to analytics implementation completion and verification.
- Files modified and why: None in this track during this session.
- Blockers/issues discovered: None newly discovered for this track.
- Next immediate steps: Continue from highest-priority unchecked items in `navbar-customization-tasks.md`.
