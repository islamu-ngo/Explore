<!-- ABOUTME: Authorization policy matrix for backend/API health refactor Phase 0. -->
<!-- ABOUTME: Maps API policies, handler metadata, Cerbos resources, HAL rels, and default roles. -->

# Authorization Policy Matrix

Last Updated: 2026-06-14 Europe/Brussels

## Purpose

This artifact replaces role-sounding policy names with capability/resource/action policies and keeps endpoint authorization, handler authorization, Cerbos policy, and HAL affordances aligned.

Required columns:

`Resource | Action | API Policy | Handler Attribute | Cerbos Resource | HAL Rel | Default Roles`

## Policy Naming Rules

- Use `Resource.Action` names: `Events.Publish`, `Templates.Manage`, `StorageObjects.ReadPresigned`.
- Do not use role-sounding names such as `template_admin` when the policy is actually a capability.
- Do not map privileged policy names to authentication-only behavior.
- Every privileged policy needs tests proving unauthenticated, authenticated-without-permission, and authorized cases.
- HAL rel availability must be based on the same resource/action decision as the API/handler path; Blazor must not recreate this decision from roles, claims, or local organization membership caches.

## Initial Matrix

| Resource | Action | API Policy | Handler Attribute | Cerbos Resource | HAL Rel | Default Roles |
|---|---|---|---|---|---|---|
| Event | Edit | `Events.Edit` | `[AuthorizeResource(ResourceKinds.Event, AuthorizationActions.Edit)]` or equivalent constant-backed attribute | `event` | `edit` | TenantAdmin, EventOwner, EventEditor |
| Event | Publish | `Events.Publish` | `[AuthorizeResource(ResourceKinds.Event, AuthorizationActions.Publish)]` | `event` | `publish` / `unpublish` | TenantAdmin, EventOwner, EventPublisher |
| Event | Delete | `Events.Delete` | `[AuthorizeResource(ResourceKinds.Event, AuthorizationActions.Delete)]` | `event` | `delete` | TenantAdmin, EventOwner |
| EventRegistration | ReadOwn | `EventRegistrations.ReadOwn` | `[AuthorizeResource(ResourceKinds.EventRegistration, AuthorizationActions.ReadOwn)]` or secure-query equivalent | `event_registration` | `self`, `registrations` | RegisteredUser |
| EventRegistration | ReadForEvent | `EventRegistrations.ReadForEvent` | `[AuthorizeResource(ResourceKinds.EventRegistration, AuthorizationActions.ReadForEvent)]` | `event_registration` | `registrations` | TenantAdmin, EventOwner, RegistrationManager |
| EventRegistration | Manage | `EventRegistrations.Manage` | `[AuthorizeResource(ResourceKinds.EventRegistration, AuthorizationActions.Manage)]` | `event_registration` | `approve`, `cancel`, `reject` | TenantAdmin, EventOwner, RegistrationManager |
| Template sync | Diff / Apply / ViewHistory | coarse `[Authorize]`; no auth-only `template_admin` policy | `[AuthorizeResource(ResourceKinds.CustomPropertyTemplate, AuthorizationActions.CustomPropertyTemplates.SyncDiff/SyncApply/View)]` + `ISecureRequest` on sync requests | `islamuevent_custom_property_template` | `sync-diff`, `sync-apply`, `sync-history` | TenantAdmin for sync diff/apply; authenticated view semantics follow existing custom-property-template policy |
| CustomProperty | Govern | `CustomProperties.Govern` | `[AuthorizeResource(ResourceKinds.CustomProperty, AuthorizationActions.Govern)]` or new constants if absent | `custom_property` | `govern` | TenantAdmin, GovernanceAdmin |
| PlatformNamespace | Edit | `PlatformNamespaces.Edit` | Host-admin secure request or platform namespace resource attribute | `platform_namespace` | `edit` | PlatformAdmin |
| Module | Manage | `Modules.Manage` | Module resource/action metadata | `module` | `enable`, `disable` | TenantAdmin, PlatformAdmin |
| StorageObject | ReadPresigned | `StorageObjects.ReadPresigned` | Storage object read/download metadata | `storage_object` | `download`, `presigned-download` | TenantAdmin, ResourceOwner, StorageManager |
| TenantSettings | Manage | `TenantSettings.Manage` | Tenant settings resource/action metadata | `tenant_settings` | `update-settings` | TenantAdmin |
| TenantUserRoleGrant | Read | `TenantUserRoleGrants.Read` | `[AuthorizeResource(ResourceKinds.TenantUserRoleGrant, AuthorizationActions.Read)]` or secure-query equivalent | `tenant_user_role_grant` | `role-grants` | TenantAdmin, PlatformAdmin |
| TenantUserRoleGrant | Manage | `TenantUserRoleGrants.Manage` | `[AuthorizeResource(ResourceKinds.TenantUserRoleGrant, AuthorizationActions.Manage)]` | `tenant_user_role_grant` | `grant-role`, `revoke-role` | TenantAdmin, PlatformAdmin |
| OrganizationMember | Read | `OrganizationMembers.Read` | `[AuthorizeResource(ResourceKinds.OrganizationMember, AuthorizationActions.Read)]` or secure-query equivalent | `organization_member` | `members` | OrganizationAdmin, OrganizationMember, TenantAdmin |
| OrganizationMember | Manage | `OrganizationMembers.Manage` | `[AuthorizeResource(ResourceKinds.OrganizationMember, AuthorizationActions.Manage)]` | `organization_member` | `invite`, `remove-member`, `change-role` | OrganizationAdmin, TenantAdmin |
| Footer tenant writes | Update | tenant-governed footer write convention | `[AuthorizeResource(ResourceKinds.Tenant, AuthorizationActions.Update)]` + `ISecureRequest` on tenant footer link-group/link/settings commands | `islamuevent_tenant` | `create`, `edit`, `delete`, `reorder`, `update-settings` once HAL links are wired | TenantAdmin; future FooterManager requires new resource/action parity |
| Bootstrap | Complete | `Bootstrap.Complete` or setup-secret policy to decide | Setup/bootstrap secure request | `bootstrap` | `complete-bootstrap` | SetupSecret, PlatformAdmin |
| AnalyticsRelay | Submit | `AnalyticsRelay.Submit` or dedicated anonymous ingestion policy to decide | Relay ingestion metadata if promoted to handler auth | `analytics_relay` | none unless exposed | Anonymous with strict limiter, or TenantAdmin for protected relay |
| Migration | Run | `Migrations.Run` | Host-admin/migration secure request | `migration` | none | PlatformAdmin, MigrationOperator |
| APIKey | InstanceAdminCrossTenant | `ApiKeys.InstanceAdminCrossTenant` | Machine-caller/host-admin secure request | `api_key` | none | InstanceAdminMachine |

## Known Replacement Targets

| Current Policy | Target Policy | Risk | Required Follow-Up |
|---|---|---|---|
| `template_admin` | Custom-property-template sync metadata (`SyncDiff` / `SyncApply` / `View`) | Removed on 2026-06-14. It was authentication-only and is no longer registered or used by sync controllers. | Keep Application metadata, HAL link permissions, and architecture/unit tests aligned; introduce a distinct `Templates.Manage` family only with full parity work. |
| `event_editor` | `Events.Edit` / `Events.Publish` by use case | Removed on 2026-06-14 because no active C# policy usage remained; the old name was broad and role-like. | Introduce event edit/publish/manage capability metadata only where endpoint inventory identifies active gaps. |
| `property_governance_admin` | `CustomProperties.Govern` | Removed on 2026-06-14 because no active C# policy usage remained; projection/governance requests already use explicit resource metadata. | Add or refine governance capability metadata only where endpoint inventory identifies active gaps, with Cerbos/fallback/HAL parity. |
| `platform_namespace_editor` | `PlatformNamespaces.Edit` | Removed on 2026-06-14 because no active C# policy usage remained; platform scope still requires host-admin semantics when implemented. | Introduce platform namespace edit metadata only with host-admin reason/audit, Cerbos/fallback/HAL parity, and tests. |

## Fallback Provider Alignment Notes

- `FallbackAuthorizationService` must be checked against this matrix because prior evidence found instance-admin bypass semantics and broad authenticated view/create allowances.
- Machine caller scope mapping must remain a ceiling, not an authorization shortcut: an API key scope may permit evaluation, but resource/action policy still decides the final result.
- Authorization parity tests should compare endpoint policy, handler metadata, fallback provider decision, Cerbos resource/action naming, and HAL rel availability for each matrix row.

## Grouped Policy Families To Populate After Full Inventory Import

The generated inventory contains many `Authenticated` CRUD operations whose controller-level metadata does not yet prove resource-action authorization. Phase 0B must expand these families into concrete matrix rows where the handler/resource model requires more than plain authentication.

| Resource Family | Candidate Actions | Expected Policy Family | Notes |
|---|---|---|---|
| Actor / ActorKeyStore / DID / IndexedDid | Create, Update, Delete, ManageKeyMaterial | `Actors.*`, `ActorKeys.*`, `Dids.*` | Key/custody operations likely need owner/admin semantics rather than generic auth. |
| Organization / OrganizationMember / OrganizationReview | Create, Edit, Delete, Invite, Accept, Decline, ManageMembers, ApproveStatus | `Organizations.*`, `OrganizationMembers.*` | Membership and approval routes require ownership/admin distinction. |
| Group / GroupMember | Create, Edit, Delete, ManageMembers | `Groups.*`, `GroupMembers.*` | Public member lists must be separately classified from member management. |
| EventSession / Agenda / Day / Language / Grouping | Create, Edit, Delete, Assign, Unassign | `EventSessions.*`, `EventSchedule.*` | These should usually inherit event ownership/editor semantics. |
| CustomPropertyDefinition / EventCustomProperty / SessionCustomProperty | Create, Edit, Delete, SetValue, Govern | `CustomProperties.*` | Governance/admin projection operations need stronger policy than value writes. |
| Tenant/Footer/Instance Settings | Read, Manage, Lock, Unlock, TestConnection | `TenantSettings.*`, `InstanceSettings.*` | Provider/test operations are auth-sensitive and audit-required. |
| Notification / ContactShareConsent / ExternalApiKey | ReadOwn, ManageOwn, Export, Revoke, UsageReport | `Notifications.*`, `ContactShares.*`, `ApiKeys.*` | User-owned reads must not become tenant-wide reads. |
| Localization / Theme / Module Admin | CatalogRead, Create, Update, Delete, Enable, Disable, Test | `Localization.*`, `Themes.*`, `Modules.*` | Admin operations need capability policies and audit. |

## 2026-06-13 Audit Must-Fill Rows

Before implementation, validate the initial matrix rows above against concrete `ResourceKinds`/`AuthorizationActions` constants. 2026-06-13 footer validation found no existing `ResourceKinds.Footer` or generic `AuthorizationActions.Manage`, so the current implementation uses the existing tenant-governed write convention instead of inventing a partial new policy family. `template_admin` has now been removed and mapped to existing `CustomPropertyTemplate` sync/view metadata. 2026-06-14 cleanup also removed the unused auth-only `event_editor`, `property_governance_admin`, and `platform_namespace_editor` registrations because no active `[Authorize(Policy = ...)]` usage remained; future concrete event, governance, or platform-namespace capability families must add resource kind/action metadata, Cerbos policy/schema, fallback, machine scopes, HAL metadata, and tests atomically. Remaining P0 rows are `EventRegistration.ReadOwn`, `EventRegistration.ReadForEvent`, `TenantUserRoleGrant.Read`, and `OrganizationMember.Read`; a future distinct `Footer.Manage` or `Templates.Manage` family must follow the same parity rule.
