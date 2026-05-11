# Plan: Implement Admin Impersonation (Break-Glass Access)

**Last Updated**: 2026-01-30

---

## 1. Executive Summary

This document outlines the plan to implement an "Admin Impersonation" feature, also known as a "Break-Glass" access model. This feature is designed to fill a critical gap in the existing two-tier administration architecture. While the current system correctly isolates tenants, it lacks a formal, audited mechanism for an Instance Administrator to access tenant-specific data in emergencies (e.g., legal requests, critical support issues).

This plan will introduce a secure, temporary, and heavily audited workflow that allows an Instance Administrator to assume the role of a Tenant Administrator for a specific tenant, ensuring all actions are logged and justified. This respects the principle of least privilege by default while providing a necessary tool for platform governance.

## 2. Current State Analysis

- The platform has a well-defined two-tier administration model: **Instance Administrator** (platform owner) and **Tenant Administrator** (customer).
- The `MULTI_TENANCY.md` documentation explicitly states that Instance Admins **cannot** access tenant-specific business data by default.
- Data is effectively isolated using global query filters based on `TenantId`.
- There is currently no sanctioned process for an Instance Admin to manage or view a specific tenant's data, which is a risk for handling urgent legal, security, or support escalations.
- Authorization is handled within MediatR handlers and HATEOAS link policies, which is the correct foundation for this feature.
- The domain model lacks a specific entity for auditing high-privilege administrative actions like impersonation.

## 3. Proposed Future State

- An Instance Administrator will have access to a new feature in their admin dashboard to "Impersonate" a specific tenant.
- Activating impersonation will require the admin to provide a mandatory justification (e.g., support ticket number, legal case ID).
- Once activated, the Instance Admin's session will be temporarily augmented with a special claim indicating they are acting on behalf of the selected tenant.
- All actions taken during the impersonation session will be recorded in a new, dedicated `ImpersonationAuditLog` table.
- The UI will display a persistent, highly visible banner indicating that an impersonation session is active.
- HATEOAS link policies will be updated to recognize the impersonation claim, dynamically showing management links (e.g., `delete`, `edit`) for tenant resources.
- The system will provide a clear "Stop Impersonation" action to end the session and revert to normal privileges.

## 4. Implementation Phases

The implementation is broken down into phases corresponding to the Clean Architecture layers, ensuring a bottom-up and orderly development process.

---

### **Phase 1: Domain Layer**

The foundation for auditing.

- **Task 1.1: Create `ImpersonationAuditLog` Entity**
  - **File**: `Explore.Domain/Entities/ImpersonationAuditLog.cs`
  - **Acceptance Criteria**:
    - [ ] Entity created with properties: `Id` (Guid), `TenantId`, `InstanceAdminUserId`, `ImpersonatedTenantAdminUserId` (nullable), `Justification` (string), `SessionStartTime` (DateTime), `SessionEndTime` (DateTime, nullable).
    - [ ] Entity inherits from a base auditable entity if one exists, or includes `CreatedAt`, `CreatedBy`.
    - [ ] Follows project conventions for entities.
  - **Effort**: S

---

### **Phase 2: Application Layer**

The core logic for the feature.

- **Task 2.1: Create `StartImpersonation` Command**
  - **File**: `Explore.Application/Features/Admin/Commands/StartImpersonationCommand.cs`
  - **Acceptance Criteria**:
    - [ ] Create `StartImpersonationCommand` with properties: `TenantId`, `Justification`.
    - [ ] Create `StartImpersonationCommandHandler` that:
      - [ ] Validates that the current user is an Instance Administrator.
      - [ ] Validates that the `TenantId` is valid.
      - [ ] Creates a new `ImpersonationAuditLog` entry with the justification and start time.
      - [ ] Returns a `BaseCommandResponse<Guid>` containing the ID of the new audit log entry.
  - **Effort**: M

- **Task 2.2: Create `StopImpersonation` Command**
  - **File**: `Explore.Application/Features/Admin/Commands/StopImpersonationCommand.cs`
  - **Acceptance Criteria**:
    - [ ] Create `StopImpersonationCommand` with property: `AuditLogId`.
    - [ ] Create `StopImpersonationCommandHandler` that:
      - [ ] Validates the current user is an Instance Administrator.
      - [ ] Finds the `ImpersonationAuditLog` entry by `AuditLogId`.
      - [ ] Sets the `SessionEndTime` to the current time.
      - [ ] Saves the changes to the database.
  - **Effort**: S

- **Task 2.3: Define `IImpersonationService` Interface**
  - **File**: `Explore.Application/Contracts/Infrastructure/IImpersonationService.cs`
  - **Acceptance Criteria**:
    - [ ] Interface defines methods for managing the impersonation state in the user's session (e.g., in the BFF's cookie).
    - [ ] Methods like `StartImpersonationInSession(tenantId, auditLogId)` and `ClearImpersonationFromSession()`.
  - **Effort**: S

---

### **Phase 3: Infrastructure Layer**

Implementing the persistence and session management.

- **Task 3.1: Add `ImpersonationAuditLog` to DbContext and Create Migration**
  - **File**: `Explore.Persistence/ExploreDbContext.cs`
  - **Acceptance Criteria**:
    - [ ] Add `DbSet<ImpersonationAuditLog>` to `ExploreDbContext`.
    - [ ] Configure the entity using `IEntityTypeConfiguration<ImpersonationAuditLog>`.
    - [ ] Generate a new EF Core migration: `dotnet ef migrations add AddImpersonationAuditLog`.
    - [ ] Verify the migration script correctly creates the table.
  - **Effort**: M

- **Task 3.2: Implement `ImpersonationService`**
  - **File**: `Explore.Blazor/Services/ImpersonationService.cs` (Lives in the BFF project as it manages the session cookie).
  - **Acceptance Criteria**:
    - [ ] Implement `IImpersonationService`.
    - [ ] `StartImpersonationInSession` should add `impersonated_tenant_id` and `impersonation_audit_id` claims to the user's authentication cookie properties.
    - [ ] `ClearImpersonationFromSession` should remove these claims.
    - [ ] This service will require working with ASP.NET Core Identity's `SignInManager` or `IAuthenticationService` to refresh the user's session cookie with the new claims.
  - **Effort**: L

---

### **Phase 4: API & Presentation (Blazor BFF)**

Exposing the functionality and making the system aware of the new state.

- **Task 4.1: Create Admin Impersonation API Endpoints**
  - **File**: `Explore.API/Controllers/AdminController.cs` (or a new `ImpersonationController.cs`)
  - **Acceptance Criteria**:
    - [ ] Create `POST /api/admin/impersonation/start` endpoint that maps to the `StartImpersonationCommand`.
    - [ ] Create `POST /api/admin/impersonation/stop` endpoint that maps to the `StopImpersonationCommand`.
    - [ ] Both endpoints must be protected and require the Instance Administrator role.
  - **Effort**: M
  - **Note**: These endpoints will likely be called from the Blazor BFF, not directly from the client.

- **Task 4.2: Update HATEOAS Link Policies**
  - **Files**: `Explore.API/Hateoas/Policies/*.cs`
  - **Acceptance Criteria**:
    - [ ] Modify the `.When()` conditions for administrative links (e.g., `Delete`, `Edit`) on tenant-owned resources (like Events, Organizations).
    - [ ] The logic should be updated to: `user.IsInRole("TenantAdmin") || (user.IsInRole("InstanceAdmin") && user.HasClaim("impersonated_tenant_id", resource.TenantId))`.
    - [ ] This ensures links appear for both the tenant's own admin and the impersonating instance admin.
  - **Effort**: M

- **Task 4.3: Create Impersonation Endpoints in Blazor BFF**
  - **File**: `Explore.Blazor/Controllers/ImpersonationController.cs`
  - **Acceptance Criteria**:
    - [ ] Create BFF endpoints (`/impersonation/start`, `/impersonation/stop`) that the Blazor client will call.
    - [ ] These endpoints will, in turn, call the `IImpersonationService` (Task 3.2) to update the session cookie and call the backend API (Task 4.1) via YARP to log the audit event.
  - **Effort**: L

---

### **Phase 5: Blazor UI Layer**

The user-facing components for the Instance Admin.

- **Task 5.1: Create Impersonation Start Component/Modal**
  - **File**: `Explore.Blazor.Client/Components/Admin/ImpersonateTenantModal.razor`
  - **Acceptance Criteria**:
    - [ ] Create a UI component (likely a modal dialog) for Instance Admins.
    - [ ] It should allow selecting a tenant (e.g., from a dropdown or search).
    - [ ] It must have a mandatory `textarea` for the justification.
    - [ ] The "Start" button should call the BFF endpoint (`/impersonation/start`).
  - **Effort**: M

- **Task 5.2: Create Global Impersonation Banner**
  - **File**: `Explore.Blazor.Client/Layout/MainLayout.razor` (or similar)
  - **Acceptance Criteria**:
    - [ ] Add a component to the main layout that is always visible during an impersonation session.
    - [ ] The banner should be prominent (e.g., bright yellow or red background).
    - [ ] It must display text like "WARNING: You are impersonating Tenant [Tenant Name]. All actions are being audited."
    - [ ] It should include a "Stop Impersonating" button that calls the BFF endpoint (`/impersonation/stop`).
    - [ ] The visibility of the banner should be driven by the presence of the `impersonated_tenant_id` claim on the `ClaimsPrincipal`.
  - **Effort**: M

---

### **Phase 6: Testing & Documentation**

- **Task 6.1: Write Unit & Integration Tests**
  - **Acceptance Criteria**:
    - [ ] Unit test the new CQRS handlers.
    - [ ] Write integration tests for the API endpoints, ensuring they are correctly protected.
    - [ ] Write an integration test that verifies HATEOAS links are correctly added/hidden based on impersonation status.
  - **Effort**: L

- **Task 6.2: Update Documentation**
  - **Files**: `docs/MULTI_TENANCY.md`, `docs/SECURITY-MODEL.md`
  - **Acceptance Criteria**:
    - [ ] Update `MULTI_TENANCY.md` to document the "Break-Glass" capability as a formal exception to the data isolation rule.
    - [ ] Update `SECURITY.md` to mention the impersonation claim and audit logging mechanism.
  - **Effort**: S

## 5. Risk Assessment and Mitigation

- **Risk**: Overly permissive logic could grant unintended access.
  - **Mitigation**: The impersonation claim must be checked alongside the `TenantId` of the resource being accessed. Unit and integration tests are critical.
- **Risk**: Session management is complex; claims might not be added/removed correctly.
  - **Mitigation**: The `ImpersonationService` must be carefully tested. Manual testing of the end-to-end login -> impersonate -> stop impersonating -> logout flow is required.
- **Risk**: Performance impact of adding claims to the auth cookie.
  - **Mitigation**: The size of the added claims is small. The impact is expected to be negligible, but should be monitored if session cookie size becomes an issue.

## 6. Success Metrics

- Instance Administrators can successfully impersonate a tenant after providing justification.
- All impersonation sessions and the reasons for them are recorded in the `ImpersonationAuditLog`.
- While impersonating, the admin can access and manage the tenant's data as if they were a Tenant Admin.
- The UI clearly indicates when an impersonation session is active.
- When the session is stopped, privileges immediately revert to normal.
