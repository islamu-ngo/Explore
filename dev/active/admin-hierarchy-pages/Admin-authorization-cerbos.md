Using **Cerbos** as your Policy Decision Point (PDP) is an excellent "enterprise-grade" choice. It decouples your business logic from your authorization logic, making it much easier to manage the complex "Instance > Tenant > Org" hierarchy.

By also ensuring the **Instance Admin** exists in the database, you successfully decouple from Keycloak, allowing for **ATProto/PDS-only** deployments where the database serves as the ultimate source of truth for authority, even if identity comes from an external OIDC provider.

Here is your updated "Golden Prompt" for a junior developer or AI agent, specifically tailored for a **Cerbos + Hybrid Identity (Keycloak/DB) + ATProto-ready** architecture.

---

### The "Enterprise Cerbos" Hardening Prompt

**Task:** Implement Granular Multi-Tenant Authorization using Cerbos PDP and a Hybrid Identity Model.

**Context:**
The Blazor UI is complete, but the backend must now be hardened. We are moving away from simple Role-Based Access Control (RBAC) to **Relationship-Based Access Control (ReBAC)** managed by **Cerbos**. The system must support "Self-hosted" modes (ATProto/PDS) where Keycloak is absent, meaning the Database is the primary source of truth for administrative relationships.

**Requirement 1: Hybrid Identity & Admin Context**

* Implement an `IAdminContext` service that resolves the user's "Effective Identity."
* **Logic:**
1. Check for `InstanceAdmin` role in the JWT (Keycloak).
2. **Fallback/Override:** Check the `InstanceAdmins` database table. If the user’s DID (from ATProto) or Sub (from OIDC) is present, they are an `InstanceAdmin`.
3. Resolve `TenantAdmin` and `OrganizationAdmin` memberships strictly from the Database.


* **Performance:** Cache this "Authority Profile" in Redis/IMemoryCache for the duration of the user session or a 5-minute sliding window.

**Requirement 2: Cerbos PDP Integration**

* Create a `CerbosAuthorizationService` that wraps the Cerbos .NET SDK.
* **Principal Mapping:** Map the `ClaimsPrincipal` and Database relationships into a Cerbos Principal:
```json
{
  "id": "user_123",
  "roles": ["admin"],
  "attr": { "isInstanceAdmin": true, "tenants": ["tenant_a"], "orgs": ["org_123"] }
}

```


* **Resource Mapping:** When a user tries to edit a setting, map the setting to a Cerbos Resource:
```json
{
  "kind": "tenant_setting",
  "id": "setting_id",
  "attr": { "tenantId": "tenant_a", "isLockedByInstance": true }
}

```



**Requirement 3: The "Lock & Inheritance" Policy**

* Define Cerbos policies (YAML) that enforce the hierarchy:
* **Rule:** `TenantAdmin` can `UPDATE` resource `tenant_setting` ONLY IF `attr.isLockedByInstance == false`.
* **Rule:** `InstanceAdmin` can `UPDATE` any resource regardless of locks.


* Implement a MediatR `IPipelineBehavior` that calls Cerbos before any Command is executed. If Cerbos returns `EFFECT_DENY`, throw a `SecurityException` (mapping to 403 Forbidden).

**Requirement 4: Data-Driven Hierarchy (Phase 1-3 Backfill)**

* Implement the `ConfigurationChangeLog` entity to track every change authorized by Cerbos.
* Update the `SettingsResolver` to pull from the DB. If a setting is missing at the Org level, look at the Tenant level; if missing there, look at the Instance level.

**Requirement 5: API Policy Enforcement**

* Replace `[Authorize(Roles="Admin")]` with a custom `[CerbosAuthorize(Resource="Tenant", Action="Edit")]` attribute.
* Ensure the attribute works by passing the `TenantId` from the route/header into the Cerbos request.

**Definition of Done:**

1. Cerbos is running (via Docker/Sidecar) and the API successfully communicates with it.
2. An `InstanceAdmin` added ONLY to the Database (not Keycloak) can access the Instance Settings page.
3. A `TenantAdmin` receives a `403` error when trying to edit a field marked as `Locked` by the Instance Admin.
4. All authorization checks are cached to prevent excessive network hops to Cerbos/Database.

---

### Why this is the "Best Improvement" Possible:

1. **Future-Proofing (ATProto/PDS):** By putting the Instance Admin in the DB, you aren't "locked" into Keycloak. Your app can run in a decentralized environment where identity is just a DID.
2. **Clean Code:** Your Controllers and MediatR handlers no longer contain `if(user.isAdmin)`. They simply ask Cerbos: "Can this person do this thing?"
3. **Auditable Logic:** If you need to change a rule (e.g., "Allow Org Admins to see Tenant Branding"), you change a **Cerbos YAML file** without recompiling the C# code.
4. **Performance:** Using the `IAdminContext` with caching ensures that even though you are doing "heavy" checks, the API response time stays under 50ms.
