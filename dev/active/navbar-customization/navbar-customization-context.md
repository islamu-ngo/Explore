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
