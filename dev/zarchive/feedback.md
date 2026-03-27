This is a strong plan. It reads like real architecture work, not feature brainstorming.

My overall assessment: **the direction is correct, the seam decisions are mostly right, and the implementation has already cleared the hardest conceptual hurdle**. The biggest remaining risk is no longer “can this work?” It is **whether the next slices preserve the same clarity around authority boundaries, operational semantics, and long-term principal design**.

## Overall verdict

I would rate this as **enterprise-grade and implementation-worthy**, with one caveat:

You are no longer blocked on the auth/tenant seam. The plan is past that. The remaining work is mostly about **hardening product semantics and operations semantics** so the feature does not become technically correct but operationally ambiguous.

In other words:

* **Architecture**: solid
* **Security posture**: strong
* **Pipeline design**: correct
* **Operational completeness**: not finished yet
* **Authorization model evolution**: needs one more pass before broader rollout

## What is especially strong

### 1. You solved the hardest problem at the right boundary

The best part of this plan is that it does **not** try to force API-key tenant authority into the pre-auth tenant middleware. The split-phase model is the right solution for this codebase.

That was the real architectural trap. A weaker design would have done one of these:

* trusted `X-Tenant-Slug` too early for machine callers
* duplicated tenant logic in controllers
* bypassed the auth subsystem
* created separate pipelines for JWT and API keys

You avoided all of those.

### 2. The caller matrix is mature

The distinction between:

* BFF user callers
* direct JWT callers
* tenant-bound API-key callers
* instance-admin operational callers

is exactly the kind of separation mature platforms need. Too many systems collapse these into “authenticated caller,” then spend years clawing back boundaries.

Your plan correctly treats these as **different trust models**, not just different credentials.

### 3. The credential/principal split is the right long-term choice

This is one of the strongest architectural decisions in the document.

Separating:

* persisted credential material (`ExternalApiKey`)
* runtime principal/claims shape

gives you a clean path toward:

* service accounts
* robot users
* stronger machine auth later
* Cerbos evaluation
* delegated org automation

without rewriting storage or middleware.

That is exactly the kind of separation that keeps enterprise systems from ossifying.

### 4. The admin-boundary discipline is very good

Keeping instance-admin access **metadata-only** unless a separate emergency path is explicitly approved is the right call.

That aligns with your existing admin hierarchy and prevents the very common anti-pattern where platform admins become implicit tenant superusers “for convenience.”

### 5. You are building observability early enough

Adding counters, mutation timestamps, auth outcomes, mismatch metrics, and per-key throttling before chasing UI polish is the right priority order.

A lot of API-key implementations ship management CRUD first and only later realize they cannot explain:

* why a key stopped working
* whether it was throttled
* whether it expired
* whether it mismatched tenant context
* whether it was ever used

You are already avoiding that failure mode.

---

## The main things I would tighten before calling the next phases “fully ready”

## 1. The scope model needs to become a first-class contract now

This is the biggest remaining architectural gap.

You already have the storage model, auth path, management slice, and limiter isolation. But the plan still treats the v1 scope catalog as a later task. I would pull that forward.

Why this matters:

If the scope vocabulary is vague, the system will drift into one of two bad states:

* scopes become cosmetic labels while real access is still determined elsewhere
* scopes become overly granular and impossible to reason about

You need a clear v1 rule such as:

* scopes are **capability ceilings**
* scopes never grant authority on their own
* final access is always `intersection(scope ceiling, owner authority, resource policy)`

That sentence should become part of the formal design.

I would strongly recommend defining v1 scopes around **coarse bounded capability families**, not action explosion. For example:

* `events.read`
* `events.write`
* `registrations.read`
* `registrations.write`
* `organizations.read`
* `organizations.manage`
* `private.read`
* `analytics.read`
* `admin.metadata.read` only for explicitly approved non-tenant business metadata

Do not let v1 become 40 tiny scopes unless Cerbos policy authoring is already prepared to absorb that complexity.

## 2. Organization-owned keys need one more semantic clarification

You have the right instinct to support both user keys and organization keys. But I would make one thing explicit:

**An organization key should not be treated as a “user without a user.”**

It should be represented as a machine principal with its own principal type and ownership semantics. Even if it is backed by `OwnerType = Organization`, the runtime authorization model should be very clear that it is not an impersonated person.

That affects:

* audit wording
* activity attribution
* future UI
* revocation when membership changes
* resource provenance

I would formalize three concepts even if only two are implemented today:

* human user principal
* organization automation principal
* future tenant/platform machine principal

That will prevent subtle policy confusion later.

## 3. Rotation needs an explicit product decision before more surface area is added

Your plan correctly calls this out, but I would elevate it even more.

You should decide now whether v1 rotation is:

* **revoke + recreate only**, or
* **true rotate with overlap window**

My recommendation for enterprise-grade adoption is:

* support **explicit overlap rotation**
* old and new secret both valid for a bounded window
* window duration configurable but capped
* every rotation emits distinct observability events
* post-window automatic revocation is deterministic

Why: real integrations are rarely rotated atomically. Without overlap, users script around your platform in unsafe ways.

If you do not want to build overlap yet, then say clearly that v1 does **not** support zero-downtime rotation. Do not leave it ambiguous.

## 4. Usage rollups should be treated as a separate subsystem, not an afterthought

Your current `LastUsedAt` / `LastUsedIp` touch path is good as a tactical measure. It is not sufficient as an operational reporting model.

I would explicitly separate:

* **hot path metadata**: `LastUsedAt`, maybe sampled `LastUsedIp`
* **event/counter telemetry**: auth outcome, throttle, revoke, mismatch, success
* **aggregated usage reporting**: per key / per tenant / per time bucket
* **forensic/audit retention**: optional, bounded, maybe not in v1

The important thing is not just adding a table. It is choosing the reporting model deliberately.

My recommendation:

* do **not** store per-request usage rows in the relational hot path
* prefer metric counters and scheduled aggregation
* only add relational rollup tables if you know the reporting questions you want answered

For v1, I would target:

* daily per-key usage counts
* daily per-tenant usage counts
* auth failure counts by reason
* throttle counts
* last success / last failure timestamps

That gives real operational value without inventing a massive analytics subsystem.

## 5. Cluster semantics should move from “docs later” to “design invariant now”

You already note that in-process limiting is not cluster-wide. Good. But I would make the platform behavior explicit immediately:

* per-key limiter is **node-local**
* per-tenant quota, if introduced later, is **not guaranteed cross-node** without a shared store
* operational docs must state that multi-node self-hosting trades strict quota precision for simplicity unless a distributed limiter is added

This matters because enterprise readers often interpret “per-key throttling” as globally consistent.

I would even encode this in naming and docs:

* “local abuse protection”
* not “global quota enforcement”

That wording matters.

## 6. API-key authentication should become more explicit about failure taxonomy

You already record several outcomes. Good. I would make the outcome vocabulary stable and public to internal operators.

Suggested fixed outcome taxonomy:

* missing
* malformed
* unknown_key_id
* secret_mismatch
* revoked
* expired
* owner_inactive
* tenant_inactive
* tenant_mismatch
* success

That gives operators and dashboards a stable language. It also helps prevent log drift across later handlers and middleware.

## 7. Wrong-tenant semantics deserve one more intentional check

Using `404` for mismatch is defensible because it hides tenancy details. I agree with the posture.

But I would document one rule very explicitly:

**Use the same response behavior for unresolved tenant and mismatched tenant whenever practical, unless operational debugging endpoints are intentionally privileged.**

Otherwise callers can use subtle differences to infer valid tenant bindings.

If the current plan already does this in effect, I would state it more strongly.

---

## What I would change in the phase ordering

I would slightly reorder the next work.

### Before more API surface or Blazor UX, finish these four items:

1. **Freeze the v1 scope catalog**
2. **Decide rotation semantics**
3. **Define usage-rollup strategy**
4. **Document cluster throttling semantics**

Once those are fixed, the rest of management endpoints, OpenAPI notes, admin reporting, and UI become much safer to build because the contract under them is stable.

Right now you have a lot of functional slices implemented, but the product-operational contract is still partly in motion.

---

## Feedback by phase

## Phase 0

This phase is already successful. I would call it closed.

Not just “in progress.” Closed.

You have:

* explicit auth dispatch
* split-phase tenant handling
* trusted forwarded-header hardening
* seam tests
* persisted-key auth integration
* mismatch behavior

That is enough to move this from exploratory to foundational.

## Phase 1

Mostly good, but incomplete until scopes are formalized.

The aggregate design is correct. The missing piece is not storage, it is the **capability ceiling model**.

## Phase 2

Very good trajectory.

The biggest remaining improvement here is to formalize a richer principal abstraction around machine identities so the application layer can stay expressive as organization automation evolves.

I would also ensure owner invalidation rules are centralized and not gradually duplicated across handlers and auth logic.

## Phase 3

Good so far.

The main architectural warning: do not let “usage rollup” become an ad hoc persistence patch. Decide whether it is metrics-backed, table-backed, or hybrid.

## Phase 4

This is the strongest part of the design.

My only recommendation is to keep any future changes from collapsing the clean seam you established. This part should now be treated as a protected architectural boundary.

## Phase 5

Good start, but this phase is not done until your docs and terminology are precise enough that operators understand what is guaranteed and what is best-effort.

## Phase 6

Reasonable, but metadata-only instance-admin reporting needs a written contract before you expose anything. Define exactly what platform operators can see.

For example:

* counts
* trends
* throttle rates
* last-seen times
* key status counts
* issuance/revocation volume

But not:

* raw secrets
* full policy details that reveal tenant internals
* resource payload usage details

## Phase 7

Correctly deferred.

This was the right call. UI before auth semantics would have been backwards.

## Phase 8

Also correct.

One note: Cerbos integration should treat machine principals as first-class subjects, not as special cases bolted onto user policy.

---

## Specific enterprise-grade recommendations

### Recommendation 1: Add a formal “authorization equation”

Put this in the plan explicitly:

**Effective access = authenticated principal type + owner authority + key scopes + tenant context + resource policy.**

That gives future contributors a clear model. It also prevents people from treating scopes as standalone authorization.

### Recommendation 2: Reserve room for sender-constrained auth later

Do not build it now, but document that the credential/principal design allows future evolution toward:

* IP restrictions
* mTLS
* signed requests
* DPoP-like sender constraints

This is useful for enterprise readers and helps justify the current architecture.

### Recommendation 3: Add a “key lifecycle state machine” doc

Not a long one. Just explicit allowed transitions:

* Active -> Revoked
* Active -> Expired
* Active -> RotatingOld / RotatingNew if overlap is supported
* Expired != Revoked
* Revoked is terminal

That will help handlers, UI, docs, and tests stay aligned.

### Recommendation 4: Add a “policy ownership immutability” rule

You already keep owner and tenant binding immutable on update. Good.

I would state this as a formal invariant:

* update may change name, scopes, expiry, quotas, allowlist
* update may never change owner type, owner id, or tenant binding
* those require replacement, not mutation

That is the right enterprise posture.

### Recommendation 5: Make audit language human-readable and stable

Operators should be able to answer:

* who created this key
* what type of key it is
* what tenant/org it belongs to
* when it was last successfully used
* why it most recently failed
* when it was rotated or revoked
* whether it was throttled

That should guide both logs and reporting.

---

## Things I would not change

I would **not** change the following:

* policy-scheme dispatch approach
* `X-API-Key` dedicated header
* split pre-auth / post-auth tenant handling
* key-bound tenant authority for API keys
* metadata-only instance-admin visibility
* tenant-filter bypass limited to auth lookup
* controllers staying thin
* repository/entity patterns matching current architecture
* not reusing `UserAuthenticationToken` as the core aggregate

Those are all the right calls.

---

## Final assessment

As a senior architect review, my conclusion is:

**This plan is well above average and already demonstrates strong architectural judgment.**
It correctly protects the most important seams:

* trust boundary
* tenant authority
* principal modeling
* admin isolation
* operational safety

The feature is no longer waiting on “core architecture.” It is waiting on **contract hardening** in four areas:

1. scope vocabulary
2. rotation semantics
3. usage-rollup model
4. clustered throttling semantics

Get those four locked down, and the rest of the roadmap becomes straightforward, defensible, and much easier to document and ship.

If you want, I can turn this into a **redline review of the plan itself**, with exact edits by section and wording changes for enterprise-readiness.
