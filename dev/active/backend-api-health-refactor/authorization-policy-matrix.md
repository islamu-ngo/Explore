<!-- ABOUTME: Authorization policy matrix for backend/API health refactor Phase 0. -->
<!-- ABOUTME: Maps API policies, handler metadata, Cerbos resources, HAL rels, and default roles. -->

# Authorization Policy Matrix

Last Updated: 2026-05-07 Europe/Brussels

## Purpose

This artifact replaces role-sounding policy names with capability/resource/action policies and keeps endpoint authorization, handler authorization, Cerbos policy, and HAL affordances aligned.

Required columns:

`Resource | Action | API Policy | Handler Attribute | Cerbos Resource | HAL Rel | Default Roles`

## Policy Naming Rules

- Use `Resource.Action` names: `Events.Publish`, `Templates.Manage`, `StorageObjects.ReadPresigned`.
- Do not use role-sounding names such as `template_admin` when the policy is actually a capability.
- Do not map privileged policy names to authentication-only behavior.
- Every privileged policy needs tests proving unauthenticated, authenticated-without-permission, and authorized cases.
- HAL rel availability must be based on the same resource/action decision as the API/handler path.

## Initial Matrix

| Resource | Action | API Policy | Handler Attribute | Cerbos Resource | HAL Rel | Default Roles |
|---|---|---|---|---|---|---|
| Event | Edit | `Events.Edit` | `[AuthorizeResource(ResourceKinds.Event, AuthorizationActions.Edit)]` or equivalent constant-backed attribute | `event` | `edit` | TenantAdmin, EventOwner, EventEditor |
| Event | Publish | `Events.Publish` | `[AuthorizeResource(ResourceKinds.Event, AuthorizationActions.Publish)]` | `event` | `publish` / `unpublish` | TenantAdmin, EventOwner, EventPublisher |
| Event | Delete | `Events.Delete` | `[AuthorizeResource(ResourceKinds.Event, AuthorizationActions.Delete)]` | `event` | `delete` | TenantAdmin, EventOwner |
| EventRegistration | Manage | `EventRegistrations.Manage` | `[AuthorizeResource(ResourceKinds.EventRegistration, AuthorizationActions.Manage)]` | `event_registration` | `approve`, `cancel`, `reject` | TenantAdmin, EventOwner, RegistrationManager |
| Template | Manage | `Templates.Manage` | `[AuthorizeResource(ResourceKinds.Template, AuthorizationActions.Manage)]` or new constants if absent | `template` | `manage-template` | TenantAdmin, TemplateManager |
| CustomProperty | Govern | `CustomProperties.Govern` | `[AuthorizeResource(ResourceKinds.CustomProperty, AuthorizationActions.Govern)]` or new constants if absent | `custom_property` | `govern` | TenantAdmin, GovernanceAdmin |
| PlatformNamespace | Edit | `PlatformNamespaces.Edit` | Host-admin secure request or platform namespace resource attribute | `platform_namespace` | `edit` | PlatformAdmin |
| Module | Manage | `Modules.Manage` | Module resource/action metadata | `module` | `enable`, `disable` | TenantAdmin, PlatformAdmin |
| StorageObject | ReadPresigned | `StorageObjects.ReadPresigned` | Storage object read/download metadata | `storage_object` | `download`, `presigned-download` | TenantAdmin, ResourceOwner, StorageManager |
| TenantSettings | Manage | `TenantSettings.Manage` | Tenant settings resource/action metadata | `tenant_settings` | `update-settings` | TenantAdmin |
| Bootstrap | Complete | `Bootstrap.Complete` or setup-secret policy to decide | Setup/bootstrap secure request | `bootstrap` | `complete-bootstrap` | SetupSecret, PlatformAdmin |
| AnalyticsRelay | Submit | `AnalyticsRelay.Submit` or dedicated anonymous ingestion policy to decide | Relay ingestion metadata if promoted to handler auth | `analytics_relay` | none unless exposed | Anonymous with strict limiter, or TenantAdmin for protected relay |
| Migration | Run | `Migrations.Run` | Host-admin/migration secure request | `migration` | none | PlatformAdmin, MigrationOperator |
| APIKey | InstanceAdminCrossTenant | `ApiKeys.InstanceAdminCrossTenant` | Machine-caller/host-admin secure request | `api_key` | none | InstanceAdminMachine |

## Known Replacement Targets

| Current Policy | Target Policy | Risk | Required Follow-Up |
|---|---|---|---|
| `template_admin` | `Templates.Manage` | Current name implies privilege but prior evidence found authentication-only behavior. | Replace policy registration and tests. |
| `event_editor` | `Events.Edit` / `Events.Publish` by use case | Current name is broad and role-like. | Split edit/publish/manage where endpoint inventory requires. |
| `property_governance_admin` | `CustomProperties.Govern` | Current name is role-like and placeholder-risky. | Map to governance handler attributes and Cerbos. |
| `platform_namespace_editor` | `PlatformNamespaces.Edit` | Platform scope requires host-admin semantics. | Require host-admin reason/audit. |

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
