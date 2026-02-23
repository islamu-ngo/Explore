# Context: Implement Admin Impersonation

**Last Updated**: 2026-01-30

---

## 1. Key Architectural Decisions

- **"Break-Glass" Model**: The feature will not be a standing permission but an explicit, temporary, and audited action. This aligns with the Principle of Least Privilege while providing necessary administrative capability.
- **Session-Based Claim**: Impersonation status will be managed by adding a temporary `impersonated_tenant_id` claim to the user's BFF session cookie. This is non-persistent and confined to the session.
- **Mandatory Justification & Auditing**: All impersonation sessions *must* be initiated with a justification, which is then stored in a dedicated, immutable `ImpersonationAuditLog` table. This is the core of the feature's security and accountability.
- **BFF-Mediated**: The logic for modifying the user's session (adding/removing claims) will reside in the Blazor BFF project (`Explore.Blazor`), as it is the owner of the authentication cookie. The UI client (`Explore.Blazor.Client`) remains unaware of the security implementation details.
- **HATEOAS-Driven Permissions**: The visibility of management actions in the UI will be controlled by updating HATEOAS link policies in the API (`Explore.API`) to recognize the new `impersonated_tenant_id` claim.

## 2. Core Dependencies & Related Concepts

- **Multi-Tenancy Architecture**: This feature is a formal exception to the strict data isolation rules defined in `docs/MULTI_TENANCY.md`. It relies on the existing `TenantId` infrastructure.
- **Clean Architecture & CQRS**: The implementation will follow existing patterns, introducing new CQRS commands and queries within the Application layer and using MediatR for orchestration.
- **BFF Security Model**: The plan depends heavily on the Backend-for-Frontend pattern outlined in `docs/SECURITY.md`, where the `Explore.Blazor` project manages the secure session cookie.
- **HATEOAS Framework**: The dynamic nature of the UI relies on the existing HATEOAS infrastructure (`Explore.API/Hateoas` and `Explore.Application/Hateoas`), which will be extended to support the impersonation context.

## 3. Key Files & Locations for Implementation

### Domain & Data
- `Explore.Domain/Entities/`: Where the new `ImpersonationAuditLog.cs` entity will be created.
- `Explore.Persistence/ExploreDbContext.cs`: Will be updated to include the new `DbSet`.
- `Explore.Persistence/Migrations/`: A new EF Core migration will be generated here.

### Application Logic (CQRS)
- `Explore.Application/Features/Admin/Commands/`: Location for the new `StartImpersonationCommand.cs` and `StopImpersonationCommand.cs`.
- `Explore.Application/Contracts/Infrastructure/`: Location for the `IImpersonationService.cs` interface.

### Infrastructure & Presentation
- `Explore.Blazor/Services/`: The concrete `ImpersonationService.cs` implementation will live here, managing the BFF session.
- `Explore.Blazor/Controllers/`: A controller will be added here to expose impersonation functions to the Blazor Client.
- `Explore.API/Controllers/`: The API controller that receives requests from the BFF to orchestrate the CQRS commands.
- `Explore.API/Hateoas/Policies/`: These files will be modified to include the impersonation check in their `.When()` conditions.

### User Interface (Blazor)
- `Explore.Blazor.Client/Components/Admin/`: A new component for initiating the impersonation session will be created here.
- `Explore.Blazor.Client/Layout/MainLayout.razor`: Will be modified to include the persistent "Impersonation Active" banner.
## Context Reset Session Update (2026-02-15 21:25 Europe/Brussels)

- Current implementation state: No new implementation changes in this session for this track.
- Key decisions made this session: Priority shifted to analytics implementation completion and verification.
- Files modified and why: None in this track during this session.
- Blockers/issues discovered: None newly discovered for this track.
- Next immediate steps: Continue from highest-priority unchecked items in `implement-admin-impersonation-tasks.md`.

## Context Reset Session Update (2026-02-23 18:12 Europe/Brussels)

- Current implementation state: No new implementation changes in this session for this track.
- Key decisions made this session: Priority focused on admin consolidation handoff in navbar customization track.
- Files modified and why: None in this track during this session.
- Blockers/issues discovered: None newly discovered for this track.
- Next immediate steps: Continue from highest-priority unchecked items in this task file.
