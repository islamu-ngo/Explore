# Context: Landing and Organization Pages Enhancements

## Key Files
- **Backend API**:
  - `Explore.API/Controllers/EventController.cs`: Main entry point for event listing.
  - `Explore.Application/Features/Events/Requests/Queries/GetEventListRequest.cs`: Request DTO.
  - `Explore.Application/Features/Events/Handlers/Queries/GetEventListRequestHandler.cs`: Request handler.
  - `Explore.Application/Specifications/Events/EventFilter.cs`: Core filter logic.
  - `Explore.Application/Specifications/Events/EventSubqueryFilter.cs`: Subquery filter logic (junction tables).
  - `Explore.Persistence/Repositories/EventRepository.cs`: Implementation of specification application.

- **Frontend Blazor**:
  - `Explore.Blazor.Client/Pages/Landing/LandingPageForUsers.razor`: User-specific landing page.
  - `Explore.Blazor.Client/Pages/Landing/LandingPageForNonUsers.razor`: Public landing page.
  - `Explore.Blazor.Client/Pages/Organizations/OrganizationProfile.razor`: Organization profile page.

## Decisions
- **Past Event Definition**: An event is considered "Past" if its LAST session has already started (`Max(StartTime) <= Now`).
- **Default Behavior**: API will hide past events by default to improve discoverability of relevant content.
- **Organization Profile**: Will override the default to show all events, categorized into Upcoming and Past.

## Essential Interfaces
```csharp
public enum EventSubqueryFilterType {
    // ...
    FutureOnly
}

public class GetEventListRequest {
    public bool IncludePastEvents { get; set; } = false;
}
```

Last Updated: 2026-03-10
