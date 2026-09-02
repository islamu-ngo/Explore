<!-- ABOUTME: Architectural decision record for event participation, provenance, and organizer authority. -->
<!-- ABOUTME: Defines typed participation policy, public transactional writes, consent subjects, and HAL authority. -->

# ADR-017: Event Participation Authority Model

| | |
|---|---|
| **Status** | Accepted |
| **Date** | 2026-07-26 |
| **Deciders** | ISLAMU Event Platform — Architecture, Security, Registration workstreams |
| **Supersedes** | Boolean registration and community-reporting authority semantics on `Event` |
| **Superseded by** | — |

## Context

The existing event model conflates listing provenance, publishing authority, organizer authority, participation management, data access, and commercial authority. `IsRegistrationRequired`, `IsUserReported`, `EventUrl`, and `ExternalRegistrationUrl` cannot accurately express information-only, walk-in, externally managed, or platform-managed participation. Treating the listing submitter as the organizer would also expose provider configuration and attendee data to community contributors.

Guest checkout requires narrowly scoped anonymous writes. The existing `Public` endpoint class promises no tenant mutation, while `Authenticated` excludes legitimate guest flows. Weakening either class would remove a useful security boundary.

## Decision

### Provenance and authority

1. Event provenance is required typed state: `ORGANIZER_CREATED`, `COMMUNITY_REPORTED`, `TENANT_CURATED`, `IMPORTED`, or `FEDERATED`.
2. `SubmittedByUserId`, nullable `OrganizerActorId`, `SourcePublisherName`, and `SourceUrl` record separate facts. Provenance is historical and remains visible after publication or organizer claim.
3. `Event.ActorId` means publishing authority. Phase 1 will rename it to `PublishedByActorId` if its reference breadth permits a clean development-mode rename; otherwise that narrowed meaning is documented and enforced.
4. Listing, participation-management, data-collection, and commercial authority are evaluated separately from typed state and authorization policy. A community contributor receives correction and claim affordances only, never organizer, provider, ticketing, or attendee-data authority.
5. An approved organizer claim can set future organizer authority. It cannot erase provenance, imply attendee consent, or grant historical attendee data.

### Participation configuration and public actions

1. Every event owns an explicit `EventParticipationConfiguration`. No implicit business default is persisted or inferred.
2. Participation handling is one of `INFORMATION_ONLY`, `WALK_IN`, `EXTERNAL_MANAGED`, or `PLATFORM_MANAGED`.
3. Advance-registration obligation is independently `NOT_APPLICABLE`, `OPTIONAL`, or `REQUIRED`. Identity access is independently `ACCOUNT_REQUIRED`, `GUEST_ALLOWED`, or `CAPABILITY_TOKEN_ALLOWED`. Admission decisions continue to use `RegistrationMode`.
4. `IsRegistrationRequired` is deleted. Event and session price fields are deleted after the versioned ticket catalog becomes authoritative.
5. Public participation actions are typed, ordered resources. Zero actions is valid, and at most one is primary. Labels describe the real destination; an external action never claims that registration occurs on ISLAMU.
6. The API authors every participant and organizer affordance through HAL. The Blazor client checks link presence and never reconstructs authority from roles, claims, provenance, or local state.

### Public transactional writes

1. `EndpointClass.PublicTransactional` is the only anonymous tenant-mutation class. It is limited to guest order start, continuation, finalization, and scoped capability management.
2. Every such endpoint has a dedicated rate-limit policy, explicit browser antiforgery treatment, required idempotency on create/finalize, scoped hashed capability authorization, generic not-found behavior, minimal order exposure, and PII-free telemetry.
3. Architecture tests enforce the classification contract. `Public` remains read-only, and ordinary writes remain authenticated.

### Consent and attendee data

1. Consent is immutable evidence containing purpose, exact text and UI-version snapshots, language, grant/withdrawal timestamps, and provider/submission provenance.
2. Consent subjects are typed as User, Registration Purchaser, Registration Participant, or Guest Contact. Purchasing never implies consent for another adult participant.
3. Contact-sharing requires a present, verified organizer recipient. Unclaimed community-reported events cannot request or reveal attendee contact data.
4. A later organizer claim grants no retroactive consent or historical attendee-data access.

## Rejected alternatives

The following consultation anti-patterns are forbidden:

1. Adding booleans to `Event` for every participation combination.
2. Treating `ActorId` as reporter, publisher, organizer, and payment recipient simultaneously.
3. Granting organizer rights to the creator of a reported listing.
4. Allowing a reported-event contributor to connect a form provider.
5. Allowing a reported-event contributor to view attendee email addresses.
6. Treating an external-link click as a registration.
7. Treating iframe completion or return navigation as registration proof.
8. Calling an optional questionnaire “registration.”
9. Requiring every attendee to have an ISLAMU account.
18. Allowing a no-sync form to block ISLAMU registration.
19. Automatically sharing purchaser consent with all adult participants.
20. Granting historical attendee data to an organizer who later claims an event.
21. Creating generic open-redirect endpoints for organizer URLs.
22. Hiding community-reported provenance after publication.
23. Using Layer 3 custom properties for provenance, registration authority, ticket limits, or payment status.
24. Adding client-side capability booleans or role checks instead of server-authored HAL links.

## Consequences

- Event creation and update contracts must supply explicit participation state.
- Cerbos resources and HAL policies gain provenance, organizer, participation, and capability-aware attributes while remaining fail closed.
- Guest registration obtains a governed security class instead of weakening public or authenticated endpoint semantics.
- Community-reported events remain useful and correctable without transferring organizer or attendee-data authority.
- Consent and contact-sharing flows become subject-aware and auditable.
- The public and Studio UIs render only API-authored actions and preserve community provenance.

## Related

- `dev/active/registration-data-collection/registration-data-collection-consultation.md` Report 2 §§3–12, 22, 25–26, 33
- `dev/active/registration-data-collection/registration-data-collection-plan.md` D8–D10, D12
- `docs/AUTHORIZATION.md`
- `docs/SECURITY-MODEL.md`
- ADR-016: Registration Data Collection Context And Provider Channels
