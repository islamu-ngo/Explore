# Blazor Pagination Support - Context

**Last Updated**: 2026-01-15

## SESSION PROGRESS (2026-01-15)

### ? COMPLETED
- Created plan, context, and tasks files for Blazor pagination support.
- Identified key Blazor client services and pages affected by pagination changes.
- Noted NSwag client needs regeneration to include paginated response schemas.

### ?? IN PROGRESS
- None.

### ?? BLOCKERS
- None.

## Key Files

- `Explore.Blazor.Client/Pages/Event/EventList.razor` - public event listing with local filtering and pagination.
- `Explore.Blazor.Client/Pages/Event/MyEvents.razor` - my events list with local pagination.
- `Explore.Blazor.Client/Services/EventService.cs` - event list methods return ICollection.
- `Explore.Blazor.Client/Services/LandingPageService.cs` - uses EventAllAsync to build landing data.
- `Explore.Blazor.Client/Clients/EventApiClient.g.cs` - generated client; needs pagination schema updates.
- `Explore.API/Controllers/EventController.cs` - reference for pagination query parameters.

## Decisions

- Use API pagination metadata for UI totals and page controls.
- Keep lookup tables on full fetch via pageSize=100 or loop through pages.

## Quick Resume

1. Regenerate or adjust API client for paginated endpoints.
2. Update EventService and LandingPageService signatures to use paginated responses.
3. Refactor EventList and MyEvents to server-driven pagination.
4. Update remaining list pages and admin screens.
