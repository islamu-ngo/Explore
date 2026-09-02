---
description: Choose Cerbos or local RBAC and enforce resource actions through HAL links.
---

# Authorization

Authorization is separate from authentication and is a runtime-explicit choice between Cerbos and local database-backed RBAC.

## Decision flow

1. Endpoint authorization establishes the broad access boundary.
2. MediatR resource authorization evaluates the actual tenant, resource, action, and current state.
3. The handler performs the domain transition only after authorization succeeds.
4. The response exposes currently allowed follow-up actions in HAL `_links`.

Clients must not infer edit, delete, refund, check-in, or administration actions from local roles, claims, provider type, or cached state. Broad checks may guard a whole page or route, but resource mutation affordances come from the server response.

## Cerbos

Cerbos runtime decisions use the gRPC PDP. Policy synchronization and administration are separate operational paths. When Cerbos is selected, PDP outage, missing policy, or tenant BYO-PDP failure denies access. ISLAMU Event does not silently fall back to local RBAC.

Verify `/_cerbos/health`, policy availability, the application authorization readiness check, and a real allow/deny resource decision. Switching to local RBAC is an explicit operator change, not an outage shortcut.

## Local RBAC

Local RBAC uses the application's persisted role and resource model. It remains subject to tenant resolution, endpoint policy, current resource state, concurrency, and domain invariants. Selecting it does not authorize clients to manufacture actions absent from HAL.

## Failure handling

Authorization failures use bounded ProblemDetails and do not disclose policy internals. Missing links and denied operations should be investigated through caller, tenant, target state, provider intent, and policy health. Never “fix” a denial by adding only a frontend role check.
