---
created: 2026-06-14
updated: 2026-06-14
publish: false
type: research
status: active
tags:
  - project/islamu-event
  - topic/software-architecture
  - topic/ddd
project: event
org: islamu
---

# Event Draft Lifecycle Architecture Consultation

## Executive Recommendation

Keep the current single-aggregate approach. Do not introduce `EventDraft` tables or an `EventDraft` entity.

After reviewing the live repository at `/home/amir/ISLAMU/Github/Event`, the important correction is this: the Event codebase already implements the preferred direction for event drafts. `EventStatusEnum` already includes `Draft`, `Published`, `Cancelled`, `Completed`, and `Archived`; lookup seeding already creates `DRAFT`; `CreateEventDraftRequestDto` already maps into the canonical `CreateEventRequest`; and public event queries already exclude draft events.

The remaining architecture problem is therefore not event draft storage. The remaining problem is hardening the lifecycle around the existing model:

- Keep normal `Event` rows with `EventStatus = Draft` for event drafts.
- Keep `Event.Title`, tenant, owning Actor, status, visibility, and format as structural requirements.
- Keep draft-flexible event fields nullable, especially `Description`, optional lookups, media, URLs, price, and schedule rollups.
- Extend publish readiness from the current minimal check into a policy-aware readiness service.
- Add an explicit lifecycle/status model for `EventSession`, because session drafts and speaker submissions are not yet modeled.
- Extend the existing event-team/role model for speaker or program-committee workflows instead of treating `EventSessionSpeaker` as an authorization model.
- Reuse the existing policy infrastructure for configurable validation rather than adding an unrelated policy subsystem.

This keeps one durable identity for the event, avoids clone/merge complexity, aligns with the live code, and focuses new work where the codebase actually has gaps: session lifecycle, contributor workflow, and policy-aware publication checks.

## Source Basis

This version is based on the live ISLAMU Event repository at `/home/amir/ISLAMU/Github/Event`, not on the vault-only project notes.

Primary codebase sources reviewed:

- `README.md`
- `docs/ARCHITECTURE.md`
- `docs/DOMAIN.md`
- `Explore.Domain/Event.cs`
- `Explore.Domain/EventSession.cs`
- `Explore.Domain/EventStatus.cs`
- `Explore.Domain/Enums/EventStatusEnum.cs`
- `Explore.Domain/EventRoleAssignment.cs`
- `Explore.Domain/EventSessionSpeaker.cs`
- `Explore.Domain/Policies/EventPolicy.cs`
- `Explore.Domain/Policies/InstancePolicySet.cs`
- `Explore.Domain/Policies/TenantPolicySet.cs`
- `Explore.Domain/Policies/OrganizationPolicySet.cs`
- `Explore.Domain/Policies/PolicySlot.cs`
- `Explore.Persistence/Configurations/Entities/EventConfiguration.cs`
- `Explore.Persistence/Configurations/Entities/EventSessionConfiguration.cs`
- `Explore.Persistence/Configurations/Entities/EventRoleAssignmentConfiguration.cs`
- `Explore.Persistence/Configurations/Entities/EventSessionSpeakerConfiguration.cs`
- `Explore.Persistence/Seed/LookupTableSeeder.cs`
- `Explore.Persistence/Services/PolicyResolver.cs`
- `Explore.Application/DTOs/Event/CreateEventRequest.cs`
- `Explore.Application/DTOs/Event/CreateEventDraftRequestDto.cs`
- `Explore.Application/DTOs/Event/UpdateEventDraftRequestDto.cs`
- `Explore.Application/DTOs/Event/PublishEventRequestDto.cs`
- `Explore.Application/DTOs/Event/Validators/CreateEventRequestValidator.cs`
- `Explore.Application/DTOs/Event/Validators/UpdateEventDraftRequestDtoValidator.cs`
- `Explore.Application/Features/Events/Handlers/Commands/CreateEventCommandHandler.cs`
- `Explore.Application/Features/Events/Handlers/Commands/UpdateEventDraftCommandHandler.cs`
- `Explore.Application/Features/Events/Handlers/Commands/PublishEventCommandHandler.cs`
- `Explore.Application/Services/EventPublishReadinessEvaluator.cs`
- `Explore.Application/Features/Events/Handlers/Queries/GetEventListRequestHandler.cs`
- `Explore.Application/Features/Events/Handlers/Queries/GetEventDetailsRequestHandler.cs`
- `Explore.Application/Features/Events/Handlers/Queries/GetEventCalendarExportRequestHandler.cs`
- `Explore.Application/DTOs/EventSession/CreateEventSessionDto.cs`
- `Explore.Application/DTOs/EventSession/UpdateEventSessionDto.cs`
- `Explore.Application/DTOs/EventSession/Validators/CreateEventSessionDtoValidator.cs`
- `Explore.Application/DTOs/EventSession/Validators/UpdateEventSessionDtoValidator.cs`
- `Explore.Application/Features/EventSessions/Handlers/Commands/CreateEventSessionCommandHandler.cs`
- `Explore.Application/Features/EventSessions/Handlers/Commands/UpdateEventSessionCommandHandler.cs`
- `Explore.Application/Features/EventRoleAssignments/Handlers/Commands/AssignEventRoleCommandHandler.cs`
- `Explore.Application/Features/EventRoleAssignments/Handlers/Commands/AssignEventRoleByEmailCommandHandler.cs`
- `Explore.Application/Authorization/EventRoleAuthorityCeilingService.cs`
- `Explore.Application/Services/EventActorResolver.cs`
- `Explore.Application/Features/Events/Handlers/Queries/GetEventCreationContextRequestHandler.cs`
- `Explore.Application/DTOs/Instance/EventPolicyDto.cs`
- `Explore.Application/DTOs/Onboarding/TenantPolicySettingsDto.cs`
- `Explore.Blazor.Client/Pages/Events/CreateEvent.razor.cs`
- `Explore.Blazor.Client/Pages/Events/MyEvents.razor.cs`
- `Explore.Blazor.Client/Services/EventCreationEligibilityService.cs`
- `Event.Application.UnitTests/Features/Events/Validators/CreateEventRequestValidatorTests.cs`
- `Event.Application.UnitTests/Features/Events/Commands/CreateEventCommandHandlerTests.cs`
- `Event.Application.UnitTests/Features/EventSessions/Commands/CreateEventSessionCommandHandlerTests.cs`

The conclusion below is code-grounded: the live repository already has event-level draft support and should be evolved, not replaced.

## Live Codebase Findings

The current codebase is closer to the recommended architecture than the earlier vault-note analysis suggested.

| Area | Current code finding | Architecture implication |
|---|---|---|
| Event draft status | `EventStatusEnum` already has `Draft = 1`; `LookupTableSeeder` seeds `DRAFT`, `PUBLISHED`, `CANCELLED`, `COMPLETED`, and `ARCHIVED`. | Do not add a new draft entity. Keep using the existing status lookup. |
| Event storage | `Event.Title`, `ActorId`, `TenantId`, `EventStatusId`, `VisibilityTypeId`, and `EventFormatId` are required. Many business fields are nullable. | The event shell already separates structural integrity from publish completeness. |
| Event creation | `CreateEventRequestValidator` allows title-only/minimal requests. Tests explicitly cover minimal draft/import-shaped creation. | The canonical create path is already draft-friendly. |
| Draft creation | `CreateEventDraftRequestDto` maps to `CreateEventRequest` with `EventStatusId = 1` and no sessions/days/rooms/agenda graph. | The code already treats a draft as a normal event with draft status. |
| Draft update | `UpdateEventDraftRequestDto` is scalar-only and preserves server-owned status, actor, tenant, and session-derived projection fields. | Good boundary, but the handler should verify the target event is still draft if this endpoint is draft-only. |
| Public reads | Event list/discovery excludes draft/archived by default; draft details are not generally public; calendar export uses published events. | Public filtering already exists and should be preserved in future changes. |
| Publish | `PublishEventCommandHandler` uses `EventPublishReadinessEvaluator`, then sets status to `Published` and enqueues outbox messages. | Readiness exists, but is too thin for policy-driven publication quality. |
| Session model | `EventSession` has no status and requires `StartTime` and `EndTime`. Create/update session validators require schedule data. | Session drafting and speaker submissions are the main lifecycle gap. |
| Roles | `EventRoleAssignment` supports event-scoped operational roles, but seeded roles are owner/manager/registration/check-in only. | FOSDEM-like speaker workflows need contributor/reviewer roles or a constrained invitation model. |
| Policies | `EventPolicy`, `PolicySlot`, and `PolicyResolver` already support instance/tenant/organization policy resolution and locks. | Configurable validation should extend this infrastructure instead of inventing a separate rules layer. |

## Why A Single Aggregate Is The Better Fit

The live repository already uses a single event model:

- `Event` is the core aggregate/entity for the event or program container.
- `EventStatusId` is a required lookup FK on `Event`.
- `Draft` is already a seeded event status.
- Draft creation already maps into the normal event creation path.
- Public reads already filter draft events out of public surfaces.
- Publish already transitions the real event row to `Published` and emits outbox messages.

Separate `EventDraft` tables would work against the current implementation. They would duplicate the same domain concepts across events, sessions, speakers, agenda items, rooms, days, languages, categories, tags, role assignments, publish readiness, outbox behavior, permissions, cache invalidation, and indexes. They would also require temporary IDs, clone/merge logic, mapping between draft IDs and published IDs, and repeated validation paths.

A draft is not a different kind of event. It is an event at an earlier lifecycle state.

## Recommended Lifecycle Model

Keep `Event.EventStatusId` as a non-null foreign key to the `EventStatus` lookup. Build on the existing seeded statuses:

| Master code | Meaning |
|---|---|
| `DRAFT` | Internal editable event, not public, not exported to public calendar, and not federated as published content. |
| `PUBLISHED` | Publicly visible according to visibility and authorization rules. |
| `CANCELLED` | Previously valid event cancelled by organizer/moderator. |
| `COMPLETED` | Event has occurred and is retained as a completed public or historical record. |
| `ARCHIVED` | No longer active in organizer workflows. |

Add more event statuses only when the product needs them. For example, `REVIEW_PENDING` or `REJECTED` may be useful if the approval workflow becomes explicit in the event lifecycle. If review is mostly a moderation process, it can also live in a separate approval field or workflow table rather than bloating `EventStatus`.

Avoid overloading external event-status semantics with internal workflow semantics. If ActivityPub, Schema.org, or Mobilizon-like statuses need values such as tentative, confirmed, or cancelled, map those separately from the internal authoring workflow when necessary.

Use `EventStatus = Draft` to mean the event is not discoverable and not federated as a public event. Use `VisibilityTypeId` to describe access scope once the event is publishable. A draft event should not appear in public indexes, calendar exports, public notification fanout, or federation/outbox flows intended for published content.

## EventSession Status

Add status to `EventSession`. This is the largest model gap in the current codebase.

Today, `EventSession` has required `StartTime` and `EndTime`, and both `CreateEventSessionDtoValidator` and `UpdateEventSessionDtoValidator` require concrete schedule values. That is correct for scheduled program items, but it blocks incomplete speaker-created session drafts unless the UI invents fake dates or placeholders.

A FOSDEM-like event has two lifecycles:

- The event lifecycle: the conference or program exists under an Actor, usually an organization or group.
- The session lifecycle: speakers submit talks/workshops/panels that may be drafted, submitted, reviewed, approved, rejected, scheduled, published, or cancelled independently.

Recommended session statuses:

| Master code | Meaning |
|---|---|
| `draft` | Speaker or organizer is still editing the session. |
| `submitted` | Speaker submitted it for organizer review. |
| `under_review` | Organizer/moderator is evaluating it. |
| `approved` | Accepted internally but not necessarily public. |
| `published` | Publicly visible as part of the event program. |
| `rejected` | Declined by organizer/moderator. |
| `cancelled` | Accepted/published session cancelled. |
| `archived` | No longer active. |

This can be implemented as either `EventSessionStatusId` with an `event_session_status` lookup or a shared controlled content-status lookup. Prefer `event_session_status` if session workflow will diverge from event workflow.

If real session drafts may omit date/time, also change the session schedule model deliberately:

- Make `StartTime` and `EndTime` nullable for draft/proposal sessions, or introduce an explicit proposal command shape that can persist schedule-later sessions.
- Keep the `end > start` invariant only when both values are present.
- Keep room-overlap constraints only for active scheduled sessions with room and time data.
- Do not use fake times to satisfy the database. Fake schedule data will leak into search, conflict checks, reminders, exports, and review screens.

## What Should Stay Required

Keep these fields non-null at the database level because they define identity, authorization, tenancy, or aggregate structure. This matches the live event model well.

- `id`
- `tenant_id`
- `actor_id` or equivalent owning Actor reference
- `event_status_id`
- `visibility_type_id`
- `event_format_id`
- `title` for `Event`, as the baseline human-readable identifier across native, imported, archived, and draft records
- `event_session.event_id`
- parent IDs for junction/sub-entities such as session speakers, languages, and agenda items
- `created_at`
- `created_by_actor_id` or `created_by_user_id`, if present
- `updated_at` where the existing convention requires it

This preserves the safety boundary: drafts are incomplete, but they are never ownerless, tenantless, unnamed, or outside the authorization model.

## What Should Become Nullable For Drafts

The live `Event` model already follows this principle for most event-level fields. `Description`, `Content`, `EventTypeId`, audience fields, featured image, URLs, price, schedule rollups, template/source fields, and many other business fields are nullable.

Do not reverse that. In particular, keep `Description` nullable. It is a quality field for many public contexts, but imported events, archived records, minimal drafts, and external platform data may not have it.

Fields that are required only for publication should be nullable in EF Core and in the database. They should be validated when transitioning out of draft/review states.

Likely event-level publish-required fields include:

- `event_type_id`
- `audience_gender_id`
- `audience_age_id`
- `featured_image` or `featured_image_id`, if publication requires one
- `begins_on` / `ends_on` or equivalent event-level schedule fields
- `timezone`
- `location_id` or online address, depending on event format
- `description`, only if a specific instance or tenant policy requires it for publication

Likely session-level publish-required fields include:

- `title`
- `description`, if program display requires it
- `start_time` / `end_time` or `begins_on` / `ends_on`
- `location_id` or online room details, depending on session format
- `registration_mode_id`, if sessions can be individually registered
- at least one speaker, if the session type requires a speaker

Likely agenda-item publish-required fields include:

- `title`
- `start_time`
- `end_time`

Do not make everything nullable. Make only draft-flexible fields nullable. Keep structural constraints required.

## Domain And Command Design

The codebase already has the right shape for event creation and drafting:

- `CreateEventCommand` is the canonical event graph creation command.
- `CreateEventRequestValidator` already permits minimal title-only event creation and optional null lookups.
- `CreateEventDraftRequestDto` maps into `CreateEventRequest` with draft status and empty graph lists.
- `UpdateEventDraftRequestDto` provides a constrained scalar update shape for drafts.
- `PublishEventCommandHandler` already uses `EventPublishReadinessEvaluator` before setting status to published.

Keep this direction. Do not add `EventDraft`. Instead, refine the lifecycle commands and readiness checks.

Recommended command shape from the current baseline:

- `CreateEventCommand`: canonical creation path that can accept draft/import-shaped events when policy allows it.
- `CreateEventDraftRequestDto`: keep as the progressive authoring shell and continue mapping it to canonical event creation.
- `ImportEventCommand`: add or keep as a tolerant import path when the source is an external platform, bot, archive, or backfill process. Require source identity, tenant, owner Actor, title, and provenance, while accepting missing description, incomplete categorization, missing media, or partially mapped lookup data.
- `UpdateEventDraftCommand`: keep scalar and server-owned-field-safe, but add a lifecycle guard so it cannot accidentally edit an already published event through a draft endpoint.
- `SubmitEventForReviewCommand`: checks readiness for review and moves status from `draft` to `review_pending`.
- `PublishEventCommand`: keep the current readiness gate, but evolve it into policy-aware readiness before changing status and emitting outbox messages.
- `CancelEventCommand`: enforces cancellation rules and downstream effects.
- `ArchiveEventCommand`: removes the event from active organizer workflows without pretending it was deleted.
- `CreateSessionDraftCommand`: creates a session under an event with minimal required ownership and status.
- `SubmitSessionForReviewCommand`: moves speaker-created sessions into organizer review.
- `ApproveSessionCommand`: accepts the session for the program.
- `PublishSessionCommand`: exposes the session publicly when the parent event/program rules allow it.

Use FluentValidation for command shape, supplied FK existence, simple ranges, and authorization-adjacent checks. Use the domain entity for lifecycle transitions and aggregate invariants.

This means the EF model does not need to encode every business rule as `IsRequired()`. `IsRequired()` should be reserved for fields that are always required across all paths. Command handlers then express the differences between native creation, external import, draft editing, archive transitions, moderation review, and publication.

For UI quality, extend the current readiness evaluator rather than relying only on exceptions:

```csharp
var readiness = eventPublishReadinessEvaluator.Evaluate(@event, effectivePolicy);
```

That method should return missing fields and rule violations grouped by event, sessions, speakers, locations, agenda items, and publication/federation constraints. Throw exceptions only when executing forbidden transitions, such as publishing a rejected event or approving a session for an event where the user has no review permission.

The current `EventPublishReadinessEvaluator` is intentionally small: it blocks cancelled/archived events, requires a title, and requires `FirstSessionStartUtc`. That is a useful safety net because the publish outbox payload uses `FirstSessionStartUtc!.Value`. It is not yet a complete product-quality or tenant-policy readiness model.

## Configurable Validation Policies

Because most event fields are nullable in storage and the application layer owns completeness rules, the next design question is whether administrators can configure those rules. The answer should be yes, but with boundaries.

The repository already has policy infrastructure:

- `EventPolicy` currently controls event submission/UI toggles such as user, organization, and group submitted events.
- `PolicySlot<T>` supports local values plus child override locks.
- `InstancePolicySet`, `TenantPolicySet`, and `OrganizationPolicySet` contain event policy slots.
- `PolicyResolver` resolves instance -> tenant -> organization policy decisions and respects locks.
- `TenantPolicySettingsDto` and related services already expose settings such as `RequireEventApproval`.
- `EventActorResolver` already uses policy/settings to decide whether personal event submission is allowed.

The missing part is not policy infrastructure. The missing part is an event validation or publish policy model that expresses required fields and readiness profiles.

Support configurable validation policies for fields that are business-required, quality-required, or workflow-required. Do not make structural invariants configurable.

Non-configurable hard requirements should include:

- `id`
- `tenant_id`
- owning Actor or organizer reference
- `Event.Title`
- status
- aggregate parent links
- authorization and tenant isolation
- basic consistency rules such as end time after start time when both are supplied

Configurable policy requirements may include:

- `description`
- featured image
- event type
- event format
- audience age/gender
- category or tags
- location or online address
- timezone
- registration settings
- speaker list
- session description
- agenda completeness
- import provenance fields beyond the minimum source identifier

This creates three layers of validation:

| Layer | Owner | Example |
|---|---|---|
| Database invariants | EF Core/database | Tenant, owner Actor, status, parent IDs, title. |
| Domain invariants | Domain entity/application service | A cancelled event cannot be published without being reopened; end time must be after start time. |
| Configurable policy | Instance/tenant settings resolved by application layer | Native-created events require description on one instance, but not on another. |

## Instance And Tenant Policy Scope

Use the existing policy resolution model: instance-level policy as the default, tenant-level overrides only where the instance permits, and organization-level overrides only where tenant policy permits.

This matters for self-hosting:

- ISLAMU's hosted instance may require rich fields for native event creation, such as description, category, format, audience, and image.
- A self-hoster may allow native events without description because their community prefers lightweight posting.
- Another self-hoster may disable bot/import creation unless imported events include title, description, date, location, source URL, and organizer.
- A tenant inside an instance may want stricter requirements than the instance default for its own events.
- An instance administrator may forbid tenants from loosening certain quality or moderation requirements.

Recommended resolution order:

1. Load hard-coded system invariants.
2. Load instance policy defaults.
3. Apply tenant overrides where the instance permits overrides.
4. Apply command/source-specific policy.
5. Apply lifecycle transition policy, such as publish or submit-for-review readiness.

Do not scatter this logic across command handlers. Add a central application service such as `IEventValidationPolicyProvider` or `IEventPublishPolicyProvider` that composes the existing `IPolicyResolver`, tenant settings, command/source profile, and lifecycle transition.

## Command-Specific Policy Profiles

Policies should be scoped by command or ingestion source, not just by field name.

Useful policy profiles include:

| Policy profile | Typical strictness |
|---|---|
| `native_create` | Strict on the hosted ISLAMU instance; configurable for self-hosters. |
| `draft_create` | Minimal: title, tenant, owner Actor, status. |
| `import_create` | Tolerant but requires source/provenance and may require review before publication. |
| `bot_create` | Configurable and optionally disabled. Often should default to review-required. |
| `archive_create` | Tolerant, because historical events may be incomplete. |
| `submit_for_review` | Stricter than draft, weaker than final publication if moderation fills gaps. |
| `publish` | Strict enough for public display, search, notifications, federation, and platform quality. |

This allows different deployments to make legitimate choices without changing the data model.

Example policies:

| Scenario | Policy |
|---|---|
| ISLAMU hosted native event creation | Require title, description, type, format, audience, date/time, timezone, and location or online address. |
| ISLAMU hosted import | Require title, source URL/source ID, owner Actor, tenant, and enough date/location data to avoid misleading users; route incomplete imports to review. |
| Strict self-hoster bot ingestion | Disable bot ingestion or reject imported events missing description, date, and location. |
| Lightweight self-hoster native creation | Allow native creation with title, date, and organizer only; recommend description but do not block. |
| Archive/backfill import | Allow missing description, image, audience, and category, while marking the event archived or historical. |

## Policy Storage Shape

Start with a controlled policy model rather than arbitrary executable rules.

Reasonable options:

- Extend `EventPolicy` if required-field policies fit naturally into the existing instance/tenant/organization policy sets.
- Store simple instance defaults in configuration for self-hosting.
- Store simple tenant overrides in the existing settings/policy model if that remains the source of truth.
- Use a dedicated audited policy table only if validation profiles become complex, UI-managed, or versioned.

If a dedicated table becomes necessary, it could look like:

| Field | Purpose |
|---|---|
| `id` | Policy identity. |
| `tenant_id` | Null for instance default, set for tenant override. |
| `profile` | `native_create`, `import_create`, `bot_create`, `draft_create`, `publish`, etc. |
| `is_enabled` | Allows disabling bot/import/native paths per deployment or tenant. |
| `required_fields` | Controlled list of field keys, not arbitrary property names. |
| `requires_review` | Forces imported/bot-created content into moderation before publication. |
| `allow_tenant_override` | Instance-level control over whether tenants may change the profile. |
| `created_at` / `updated_at` | Audit trail. |

Use controlled field keys such as `description`, `featured_image`, `event_type`, `event_format`, `audience`, `location_or_online_address`, `timezone`, and `registration_settings`. Avoid raw reflection over entity property names because policies are product rules, not direct database mappings.

If the policy is stored as JSON, still validate it against a known schema and expose only supported field keys in the admin UI.

## Bot And Import Controls

Bots and external importers should call explicit commands such as `ImportEventCommand` or `CreateEventFromBotCommand`. They should not bypass application validation by writing entities directly.

This aligns with the existing AI assistant draft tooling: AI proposed create-draft actions are mapped into `CreateEventDraftRequestDto` and then routed through canonical application flow. Keep that pattern. Treat AI/bot proposals as untrusted input that becomes an application command, not as direct entity mutation.

Policy should decide:

- whether the ingestion source is enabled
- which fields are required for that source
- whether incomplete events are accepted as draft/review-only
- whether imported events can ever auto-publish
- whether the source is trusted
- whether source provenance is mandatory
- whether imported updates can overwrite organizer-edited fields

For self-hosters with strict data quality requirements, bot creation can be disabled entirely or configured to reject incomplete events. For self-hosters that prioritize broad discovery, bot creation can be allowed with tolerant requirements and review before publication.

The important boundary is this: tolerant import should not mean public publication. An instance can accept incomplete imported records into draft/review/archive states without allowing them into public discovery.

## Policy-Aware Readiness

Update the existing readiness checks so they accept an effective policy:

```csharp
var policy = await eventPublishPolicyProvider.GetPolicyAsync(context);
var readiness = eventPublishReadinessEvaluator.Evaluate(@event, policy);
```

The readiness result should explain whether a field is missing because of a hard invariant, a domain rule, an instance policy, a tenant policy, or a command-specific policy. That distinction matters for admin UX and debugging.

For example, the UI can say:

- "Description is required by this tenant's native event creation policy."
- "Source URL is required because this event was imported."
- "This bot source is disabled by the instance administrator."
- "Location or online address is required before publication."

## Admin UX And Governance

Policy configuration should be visible, auditable, and understandable.

Recommended admin UX:

- Show separate policy profiles for native creation, draft creation, import, bot ingestion, archive/backfill, review, and publication.
- Mark non-configurable hard requirements as locked.
- Let instance administrators lock specific requirements so tenants cannot loosen them.
- Show whether a policy accepts incomplete records as draft/review-only or allows publication.
- Keep an audit log when policies change.
- Warn administrators when loosening policies affects public quality, moderation workload, federation output, or search reliability.

Avoid turning this into a fully generic rules engine too early. A rules engine increases power, but it also increases support burden, testing cost, and security risk. Start with named profiles and controlled field keys. Add more expressiveness only when real deployments need it.

## EF Core Configuration Guidance

The event-level EF Core configuration already mostly matches the lifecycle model. The priority is to preserve that shape and extend it carefully.

- Keep draft-flexible event properties nullable in the entity and database.
- Keep ownership and tenant foreign keys required.
- Keep `EventStatusId` required.
- Add `EventSessionStatusId` as required.
- Seed session status lookup rows with stable `MasterCode` values.
- Add check constraints only for always-valid invariants, such as `end > start` when both schedule values are present, non-negative price, and non-negative capacity.
- Use filtered unique indexes for slugs if drafts may not have slugs, for example unique only where `slug is not null` or only where status is public/published.
- Ensure public read queries filter to published status and permitted visibility.
- Ensure internal organizer/speaker queries include drafts only when the current actor has permission.
- Ensure outbox/federation handlers ignore draft and internal-review updates.

For sessions, decide explicitly whether draft sessions can omit schedule data. If yes, EF and database constraints must change together:

- `EventSession.StartTime` and `EventSession.EndTime` become nullable.
- `EventSession.Reschedule(...)` becomes the method for scheduled states, not for every draft mutation.
- The end-after-start check becomes conditional on both values being present.
- Room overlap exclusion applies only to active, scheduled, room-bound sessions.

If the team does not want nullable session times, then the product should not promise schedule-later session drafts. It should instead require at least provisional times at session draft creation.

## Collaboration And Speaker Self-Service

The live codebase has event-scoped role assignments, but they are operational roles today, not a full speaker contribution workflow.

Current event roles include:

- `EventOwner`
- `EventManager`
- `RegistrationManager`
- `CheckInStaff`

`EventRoleAssignment` is a strong base: it is tenant-scoped, event-scoped, auditable, status-aware, and protected by authority-ceiling checks. However, `EventSessionSpeaker` is only a junction between an `Actor` and an `EventSession`. It does not grant edit rights, track invitation state, own a submission, or represent review workflow.

For speaker self-service, extend the event collaboration model rather than overloading `EventSessionSpeaker`:

| Field | Purpose |
|---|---|
| `event_id` | The event/program scope. |
| `actor_id` or `user_id` | The invited person, organization, or speaker identity. |
| `role` | Add roles such as `speaker`, `session_reviewer`, `program_committee`, or equivalent controlled event permissions. |
| `status` | `invited`, `accepted`, `revoked`, `expired`. |
| `invite_token` | Optional constrained invitation link. |
| `created_at` / `accepted_at` | Audit trail. |

The current `AssignEventRoleByEmailCommandHandler` only assigns roles to existing users by email. A FOSDEM-like workflow also needs invitation/onboarding behavior for speakers who do not yet have accounts. The speaker link should not grant broad event administration. It should create or accept a constrained event membership that lets the speaker create and edit their own draft sessions, submit them for review, and see review feedback.

The event aggregate or application service should enforce that:

- Organizers can manage the event and review sessions.
- Session reviewers can approve/reject sessions but not necessarily edit core event settings.
- Speakers can edit their own draft/submitted sessions within allowed states.
- Public participants cannot access draft sessions unless invited.

## FOSDEM-Like Program Workflow

For a conference-style event, one `EventStatus` may not be enough to describe the whole workflow. The event can be unpublished while its call for sessions is open, or the event can be published while some sessions remain draft or under review.

Consider adding a separate program/submission phase concept:

| Concept | Example values |
|---|---|
| Event publication status | `draft`, `review_pending`, `published`, `cancelled`, `archived` |
| Program submission phase | `closed`, `open`, `reviewing`, `finalized` |
| Session status | `draft`, `submitted`, `under_review`, `approved`, `published`, `rejected`, `cancelled` |

This avoids forcing `EventStatus` to carry unrelated meanings such as "call for talks is open". If the platform grows into full conference management, a `CallForSessions` concept may be cleaner than adding too many meanings to `EventStatus`.

This aligns with the repository architecture documentation, which distinguishes `Event` as the event/program container, `EventSessionGroup` as tracks/devrooms/stages/program sections, `EventSession` as scheduled content items, and `EventAgendaItem` as logistics such as breaks/meals/prayer/transitions.

## When A Revision Or Shadow Draft Is Justified

Do not create `EventDraft` now.

A separate pending revision model is justified only if the product needs simultaneous editing of an already published event while the public version remains unchanged until approval. In that case, prefer a revision/change-set pattern linked to the real event, not a parallel draft table hierarchy.

Possible future pattern:

- `EventRevision` or `EventChangeSet`
- linked to `event_id`
- stores proposed changes as JSON or a snapshot
- validates on apply/publish
- keeps public `Event` unchanged until approval

That is a later editorial workflow feature. It should not be used to solve initial incomplete draft creation.

## Migration Plan

1. Do not add `EventDraft`.
2. Do not add a new `Draft` event status; it already exists.
3. Preserve the existing nullable event shell and keep `Event.Title`, tenant, Actor, status, visibility, and format required.
4. Add a draft lifecycle guard to `UpdateEventDraftCommandHandler` if the endpoint is intended to mutate only draft events.
5. Extend `EventPublishReadinessEvaluator` into a policy-aware readiness service.
6. Extend `EventPolicy` or adjacent policy settings with controlled required-field profiles for native creation, import, bot ingestion, review, and publication.
7. Add `EventSessionStatusId` and seed session statuses.
8. Decide whether session drafts can omit schedule data. If yes, migrate session schedule fields and constraints intentionally.
9. Add session lifecycle commands: create draft, submit, approve/reject, publish, cancel, archive.
10. Extend event roles/permissions or add invitation membership for speaker and program-reviewer workflows.
11. Update public session queries, search indexes, calendar export, federation/outbox behavior, notification handlers, and HAL links to exclude non-public session states.
12. Remove UI magic status IDs/strings, especially hard-coded draft/published handling in `MyEvents.razor.cs`, in favor of enum-backed constants, lookup metadata, or server-provided actions.

## Minimum Test Coverage

Some relevant event tests already exist. Add coverage for the remaining gaps before or alongside the migration:

- Existing coverage: minimal event draft/import-shaped creation succeeds with title, tenant, owner Actor, and draft status.
- Updating an incomplete event draft succeeds when supplied values are valid.
- Updating through the draft endpoint fails if the event is already published, archived, cancelled, or otherwise not draft.
- Publishing an incomplete event fails with a readiness report listing missing fields.
- Publishing a complete event succeeds, sets status to `Published`, and enqueues the expected outbox messages.
- Creating a session draft under an event succeeds without title/time when the actor has speaker or organizer rights.
- Submitting a session for review fails if publish/review-required fields are missing.
- Approving/publishing a complete session succeeds.
- Draft events and draft sessions are absent from public search, public event pages, federation activities, feeds, and notifications.
- Tenant isolation prevents actors from accessing drafts in another tenant.
- Speaker-scoped access prevents one speaker from editing another speaker's session unless explicitly permitted.
- Instance policy can make native event creation strict while import creation remains tolerant.
- Tenant policy can tighten allowed fields when instance policy permits overrides.
- Instance policy can prevent tenants from loosening locked requirements.
- Bot/import creation can be disabled by policy.
- Bot/import creation can accept incomplete records into review or archive state without publishing them.
- Readiness reports identify whether a missing field comes from a hard invariant, domain rule, instance policy, tenant policy, or command profile.
- Publish still enqueues the correct outbox messages only after readiness succeeds.
- Calendar exports and public event/session queries exclude drafts and internal-review content.
- UI/HAL actions expose valid transitions without hard-coded status IDs in the client.

## Tradeoffs And Mitigations

The main cost of this approach is nullable fields in the domain model. The current event model already accepts that tradeoff, and it is the correct tradeoff for drafts, imports, archive/backfill, and self-hosted policy variation.

Adding configurable policies introduces a second cost: validation becomes deployment-dependent. That is powerful for self-hosting, but it can make behavior harder to reason about if policies are scattered or overly dynamic.

Adding session statuses and possibly nullable session times introduces a third cost: scheduling code, overlap checks, calendar export, and room/day assignment must distinguish unscheduled drafts from scheduled public sessions.

Mitigations:

- Keep nullable fields explicit and intentional.
- Do not expose draft entities through public DTOs.
- Use separate internal draft/edit DTOs and public published DTOs.
- Centralize readiness validation on the aggregate or domain service.
- Centralize effective policy resolution in one provider/service.
- Use named policy profiles and controlled field keys instead of arbitrary executable rules.
- Audit policy changes.
- Keep hard invariants non-configurable.
- Keep public queries strict: published status plus visibility plus tenant rules.
- Keep federation/outbox behavior status-aware.
- Keep session scheduling constraints state-aware if session drafts can be unscheduled.
- Use server-provided HAL actions or lookup metadata so clients do not hard-code status IDs.

The alternative, separate draft tables, moves complexity out of nullability and into duplication, ID mapping, merge logic, and consistency bugs. For this platform, that is the worse tradeoff.

## Final Position

Model drafts as lifecycle states of the real `Event` and `EventSession` records.

For `Event`, the live repository already does the most important part: drafts are normal events with `EventStatus = Draft`. Keep that and strengthen it.

For `EventSession`, add the missing lifecycle model so speaker-created talks can be drafted, submitted, reviewed, approved, and published without fake schedule data.

For configurable validation, extend the existing policy infrastructure so instances and tenants can choose which nullable business fields are required for native creation, imports, bot ingestion, review, and publication.

This preserves aggregate identity, matches the current Clean Architecture/CQRS implementation, keeps public/federation boundaries status-aware, gives self-hosters configurable quality gates, and supports a realistic FOSDEM-like organizer/speaker workflow without introducing `EventDraft` as a parallel domain model.
