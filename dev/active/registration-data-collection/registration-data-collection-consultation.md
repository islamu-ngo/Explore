# Registration Data Collection and Forms Provider Architecture Consultation

## Executive assessment

Your product direction is correct, but the registration questionnaire must **not** be implemented by reusing the existing Event or EventSession custom-property tables.

Event custom properties describe the event. Registration answers describe a participant’s application or attendance relationship with the event. Those are different subjects, ownership scopes, privacy boundaries, retention rules, and lifecycle models.

The repository documentation already establishes the relevant architectural rule: workflow-critical registration status, idempotency, deduplication, and delivery state must remain explicit domain state rather than being hidden inside generic EAV/custom-property rows.

The correct architecture is therefore:

> **A dedicated Registration Data Collection bounded context that reuses the custom-property system’s typed field primitives and governance patterns, but not its entities or database tables.**

ISLAMU Event should remain authoritative for:

* registration intent and participant identity;
* required registration requirements;
* normalized accepted answers;
* validation results;
* approval, capacity, and waitlist decisions;
* final EventSession registrations;
* consent evidence and retention;
* audit history.

Formbricks, Google Forms, Microsoft Forms, and the built-in form renderer should be treated as **collection channels**, not as alternative owners of the registration aggregate.

The strongest provider strategy is:

1. **Built-in Native provider** as the reference implementation and default.
2. **Formbricks** as the first deep external provider, supporting managed, embedded, redirect, and headless modes.
3. **Google Forms** as an embedded/redirect provider with API-based schema and response synchronization.
4. **Microsoft Forms** as an embedded/redirect provider with Power Automate or Logic Apps acting as the supported completion bridge.
5. Multiple providers per event through **requirements and channels**, not through a single composite provider class.

---

# 1. The first conceptual boundary: Event data versus registration data

The existing custom-property architecture extends `Event` and `EventSession` with governed, typed, long-tail attributes. It explicitly preserves Event as the parent program and EventSession as its child scheduled unit.

Examples of Event attributes:

* dress code;
* wheelchair accessibility;
* translation availability;
* childcare offered;
* recommended preparation;
* organizer-defined classifications.

Examples of registration answers:

* the attendee’s dietary restrictions;
* emergency contact;
* volunteer preference;
* accessibility accommodation request;
* consent to photography;
* selected workshop;
* accompanying child information.

The first group is **about the event**. The second group is **about a registration application, participant, or selected session**.

That means the analogous layering should be:

### Registration Layer 1 — first-class core state

This includes:

* participant or authenticated user identity;
* Event and EventSession selections;
* event/day/session registration scope;
* ticket or participant quantity;
* invitation code;
* capacity reservation;
* approval state;
* waitlist state;
* payment state when payments are added;
* cancellation state;
* registration lifecycle;
* legal consent records.

These concepts must never be generic questions because application logic depends on them.

### Registration Layer 2 — typed registration profiles

This is for standardized registration extensions that become broadly reusable, such as:

* volunteer registration;
* vendor applications;
* speaker applications;
* childcare registration;
* group or family booking;
* accommodation workflows.

As with Event aspects, a frequently standardized field should eventually be promoted out of generic form answers into typed schema.

### Registration Layer 3 — organizer-defined registration questions

This is where the flexible form system belongs:

* “Which mosque do you normally attend?”
* “Do you require a vegetarian meal?”
* “What topics are you most interested in?”
* “Tell us about your previous experience.”
* “Choose your preferred volunteer role.”

This separation gives you the flexibility of dynamic forms without turning registration processing into a generic rules engine.

---

# 2. Provider research: the providers are not functionally equivalent

A uniform abstraction is possible, but pretending that every provider has the same capabilities would be a serious design mistake.

| Provider            | Presentation                                            | Schema and response integration                                            | Completion evidence                                                           | Recommended role                     |
| ------------------- | ------------------------------------------------------- | -------------------------------------------------------------------------- | ----------------------------------------------------------------------------- | ------------------------------------ |
| **Native ISLAMU**   | Native Blazor UI                                        | Full canonical schema and typed submission pipeline                        | Same application transaction                                                  | Default and reference implementation |
| **Formbricks**      | Link, iframe/embed, SDK, or ISLAMU-rendered headless UI | Management and Client APIs; headless survey schema and response submission | Signed `responseFinished` webhook, API fetch, reconciliation                  | Deep external provider               |
| **Google Forms**    | Link or official iframe embed                           | Forms API can create, update, read forms and retrieve responses            | Cloud Pub/Sub notification followed by response fetch                         | Managed/imported external channel    |
| **Microsoft Forms** | Link or official iframe embed                           | Supported automation is through Microsoft Forms connector                  | Power Automate/Logic Apps trigger, then “Get response details,” then callback | Delegated connector channel          |

## Formbricks

Formbricks explicitly supports headless surveys: ISLAMU can fetch the survey schema, render its own UI, submit responses, and consume either webhooks or the Management API. Formbricks documents `responseCreated`, `responseUpdated`, and `responseFinished` events. ([Formbricks][1])

It also implements Standard Webhooks using:

* `webhook-id`;
* `webhook-timestamp`;
* `webhook-signature`;
* `whsec_` signing secrets;
* HMAC-SHA256 over the raw body;
* replay-tolerance checks.

That is highly compatible with the security model ISLAMU already uses for incoming and outgoing webhook processing. The cryptographic core can be shared, with a provider-specific header profile. ([Formbricks][2])

Formbricks currently documents API v1 and labels API v2 as beta. The first supported ISLAMU profile should therefore pin API v1 and a tested Formbricks deployment version rather than automatically following the newest API. ([Formbricks][3])

## Google Forms

The Google Forms API supports creating and modifying forms, reading form content, retrieving responses, managing publication, and receiving push notifications. ([Google for Developers][4])

Google response notifications are delivered through Cloud Pub/Sub. A notification carries the form ID, watch ID, and event type, but not the full response, so ISLAMU must make a separate API request to retrieve new responses. Watches last one week and must be renewed. ([Google for Developers][5])

The published API exposes response retrieval through `GET forms/{formId}/responses`; it does not expose a corresponding API operation that lets a custom ISLAMU headless UI submit a Google Forms response. Google Forms can therefore support link and iframe presentation, but not true headless submission through its documented REST API. ([Google for Developers][4])

A currently important operational detail is that Google Forms created by API after **June 30, 2026** start unpublished and must explicitly be published before they can accept responses. ([Google for Developers][6])

## Microsoft Forms

Microsoft’s documented Forms connector exposes:

* “When a new response is submitted”;
* “Get response details”;
* a response ID from the trigger that is supplied to the details action.

The connector works with organizational accounts. ([Microsoft Learn][7])

As of July 20, 2026, Microsoft’s supported public integration surface does not provide a Microsoft Graph Forms-response endpoint equivalent to Google’s Forms API. Microsoft’s documented path is the Forms connector through Power Automate or Logic Apps. A Microsoft moderator likewise states that Graph does not expose a supported Forms-response endpoint. ([Microsoft Learn][7])

Microsoft Forms officially supports both sharing links and embedding forms in a web page. ([Microsoft Support][8])

Undocumented internal Forms endpoints occasionally circulated in community answers should not become an enterprise production dependency. They have no supported versioning or compatibility contract.

---

# 3. Do not model this as one provider enum

A design such as this would be too weak:

```text
RegistrationFormProvider =
  Local | Formbricks | Google | Microsoft
```

It mixes several separate decisions:

* Who owns the canonical schema?
* Who renders the form?
* Where is the response initially stored?
* How is completion reported?
* Can ISLAMU fetch the response?
* Can ISLAMU create or update the external form?
* Is the form an alternative channel or an additional mandatory requirement?
* Should the response also be mirrored into another system?

These must be separate dimensions.

## Recommended dimensions

### Schema authority

```text
IslamuCanonical
ExternalImported
ExternalOpaque
```

* `IslamuCanonical`: ISLAMU owns the form version and may provision an equivalent external form.
* `ExternalImported`: an external form is imported and converted into a frozen ISLAMU schema version.
* `ExternalOpaque`: ISLAMU only knows that an external form exists. This cannot support reliable automatic validation.

### Presentation mode

```text
Native
Headless
Embed
Redirect
```

### Collection mode

```text
CollectNatively
CollectExternallyAndImport
CollectNativelyAndMirror
CompletionOnly
```

### Completion mode

```text
InlineTransaction
SignedWebhook
PushNotificationThenFetch
DelegatedConnectorCallback
PollingReconciliation
ManualReconciliation
```

### Trust level

```text
FirstParty
SignedProvider
AuthenticatedProviderFetch
DelegatedAutomation
UserReturnOnly
ManualImport
```

An event can require a minimum trust level before a submission is allowed to finalize automatically.

---

# 4. The key model for supporting multiple providers together

The best abstraction is not “one form with several provider IDs.” It is:

```text
Event
└── RegistrationWorkflow
    ├── RegistrationRequirement A
    │   ├── Native channel
    │   ├── Formbricks channel
    │   └── Microsoft Forms channel
    ├── RegistrationRequirement B
    │   └── Formbricks waiver channel
    └── RegistrationRequirement C
        └── Optional Google Forms survey
```

## Registration workflow

A workflow represents the complete registration process for an event or registration purpose.

Examples:

* normal attendee registration;
* volunteer application;
* vendor application;
* speaker application;
* press accreditation.

## Registration requirement

A requirement represents one logical set of data that must be completed.

Examples:

* attendee information;
* session selection;
* parental consent;
* volunteer questionnaire;
* liability waiver.

## Registration channel

A channel is one provider-specific method for satisfying a requirement.

For example:

```text
Requirement: Attendee Details
Completion rule: ANY

Channels:
- Built-in ISLAMU form
- Existing Microsoft Form
- Existing Google Form
```

Another requirement could be:

```text
Requirement: Waiver
Completion rule: REQUIRED

Channels:
- Formbricks survey only
```

At workflow level:

* mandatory requirements use `ALL`;
* alternative channels inside one requirement use `ANY`;
* optional requirements do not block finalization.

This allows providers to operate simultaneously without creating a fragile `CompositeFormProvider`.

---

# 5. Canonical domain and persistence model

The current schema already separates `EventRegistrationIntent` from the final per-session `EventRegistration`. That is the right base: the intent is the application-level aggregate, while registrations represent actual session participation.

The following tables should be added around that structure.

## Authoring and workflow tables

| Entity                        | Responsibility                                         |
| ----------------------------- | ------------------------------------------------------ |
| `RegistrationWorkflow`        | Overall registration process for an Event and purpose  |
| `RegistrationRequirement`     | A mandatory, alternative, or optional data requirement |
| `RegistrationForm`            | Stable logical form identity                           |
| `RegistrationFormVersion`     | Immutable published snapshot of a form                 |
| `RegistrationFormSection`     | Presentation grouping and ordering                     |
| `RegistrationFormField`       | Typed question definition in one version               |
| `RegistrationFormFieldOption` | Stable options for choice fields                       |
| `RegistrationFormRule`        | Bounded conditional display/requiredness logic         |
| `RegistrationFormTemplate`    | Reusable tenant or organization form blueprint         |

Important `RegistrationFormVersion` fields should include:

* `Version`;
* `Status`;
* `SchemaHash`;
* `PublishedAt`;
* `RetiredAt`;
* `SourceTemplateId`;
* `SourceTemplateVersion`;
* `ConcurrencyStamp`.

Published versions must be immutable. An edit creates a new draft version. Existing attempts remain pinned to the version they started with.

This follows the same supportability principle already used by custom-property templates: later template changes must not silently rewrite existing runtime definitions or historical submissions.

## Provider configuration tables

| Entity                               | Responsibility                                                  |
| ------------------------------------ | --------------------------------------------------------------- |
| `RegistrationProviderConnection`     | Reusable provider account, endpoint, OAuth or secret references |
| `RegistrationProviderBinding`        | Connects a form version to one external form or survey          |
| `RegistrationProviderCapability`     | Verified capabilities for the exact provider profile            |
| `RegistrationProviderFieldMapping`   | Maps canonical fields to external question IDs                  |
| `RegistrationProviderOptionMapping`  | Maps canonical options to external option IDs or values         |
| `RegistrationProviderSchemaRevision` | Records imported schema fingerprint and synchronization state   |
| `RegistrationChannel`                | Makes a binding available for one workflow requirement          |

A provider connection should belong to an appropriate scope:

* instance;
* tenant;
* organization;
* possibly group.

Credentials must be referenced through existing secret bindings, never stored on the form or event.

A binding should record:

* provider kind;
* connection ID;
* external form/survey ID;
* external revision or schema fingerprint;
* mapping revision;
* presentation mode;
* collection mode;
* completion mode;
* trust level;
* capability profile version;
* publication state;
* synchronization state;
* health state;
* last successful validation timestamp.

## Runtime collection tables

| Entity                               | Responsibility                                                |
| ------------------------------------ | ------------------------------------------------------------- |
| `RegistrationAttempt`                | One launch of one channel by a registrant                     |
| `RegistrationSubmission`             | One logical response from Native or an external provider      |
| `RegistrationSubmissionRevision`     | Immutable revision when external responses can be edited      |
| `RegistrationAnswer`                 | One normalized atomic typed answer                            |
| `RegistrationAnswerFile`             | File metadata, quarantine, scan, and storage reference        |
| `RegistrationSubmissionIssue`        | Validation, mapping, drift, and reconciliation issues         |
| `RegistrationRequirementFulfillment` | Records that an intent fulfilled a requirement                |
| `RegistrationFinalizationEffect`     | Idempotent durable command/effect that finalizes registration |
| `RegistrationAmendment`              | Controlled changes after final registration                   |

## RegistrationAttempt

An attempt must pin all relevant runtime versions:

* `EventRegistrationIntentId`;
* `RegistrationRequirementId`;
* `RegistrationChannelId`;
* `RegistrationFormVersionId`;
* `RegistrationProviderBindingId`;
* `ProviderMappingRevision`;
* `AttemptTokenHash`;
* `Status`;
* `CreatedAt`;
* `LaunchedAt`;
* `ExpiresAt`;
* `SupersededAt`.

Never resolve “the current active form” after a response arrives. Resolve the exact form, provider binding, and mapping revision captured by the attempt.

## RegistrationSubmission

Recommended fields include:

* `AttemptId`;
* `ProviderResponseId`;
* `ProviderResponseRevision`;
* `ReceivedAt`;
* `ProviderSubmittedAt`;
* `PayloadHash`;
* `ProviderSchemaFingerprint`;
* `VerificationStatus`;
* `NormalizationStatus`;
* `ValidationStatus`;
* `TrustLevel`;
* `RawPayloadRetentionUntil`;
* `FinalizedAt`;
* `SupersedesSubmissionId`.

Recommended uniqueness:

```text
ProviderBindingId + ProviderResponseId + ProviderResponseRevision
```

The same provider callback may arrive repeatedly. That uniqueness constraint turns repeated delivery into an acknowledged no-op.

---

# 6. Strongly typed and normalized answers

Dynamic forms inevitably require metadata-driven storage, but that does not mean answers must become untyped JSON blobs.

## Recommended answer shape

Use one row per atomic answer value:

```text
RegistrationAnswer
- Id
- TenantId
- SubmissionId
- FormFieldId
- AnswerSubjectTypeId
- AnswerSubjectId
- Ordinal
- TextValue
- IntegerValue
- DecimalValue
- BooleanValue
- DateValue
- TimeValue
- InstantValue
- OptionId
- SensitiveValueId
- CreatedAt
```

Use a database constraint equivalent to:

```sql
CHECK (
    num_nonnulls(
        text_value,
        integer_value,
        decimal_value,
        boolean_value,
        date_value,
        time_value,
        instant_value,
        option_id,
        sensitive_value_id
    ) = 1
)
```

Then add type-specific checks proving the populated column agrees with the field’s declared type.

For multivalue fields, store multiple answer rows using `Ordinal`.

Examples:

* multiple-choice answer: multiple `OptionId` rows;
* ranking answer: multiple `OptionId` rows ordered by `Ordinal`;
* matrix question: decompose into child field identities;
* repeated participant information: attach answers to different participant subjects.

Do not store the canonical answer set as one JSON document. A short-retention copy of the raw provider payload may be retained for diagnostics and reconciliation, but it is not the source of truth.

## Recommended portable field types

The provider-neutral profile should begin with:

* `ShortText`;
* `LongText`;
* `Integer`;
* `Decimal`;
* `Boolean`;
* `Date`;
* `Time`;
* `Instant`;
* `Email`;
* `Phone`;
* `Url`;
* `CountryCode`;
* `LanguageTag`;
* `SingleChoice`;
* `MultipleChoice`;
* `Rating`;
* `Consent`;
* `File`.

Composite fields should be decomposed:

* a name becomes given name, family name, and optional display name;
* an address becomes structured address components;
* a matrix becomes stable row/column child fields;
* a ranking becomes ordered option rows.

Provider-only question types may be supported as `OpaqueExternal`, but:

* they cannot satisfy a required canonical field;
* they cannot participate in automatic registration decisions;
* they are not available for provider-neutral analytics.

## Stable semantic identity

Each field needs both:

* an immutable form-version field ID;
* a stable machine identity such as `Namespace + Key`.

Examples:

```text
platform.registration/email
platform.registration/phone
platform.registration/session_selection
tenant.community/dietary_requirements
tenant.volunteers/preferred_role
pack.childcare/child_age
```

External provider question IDs must only appear in mapping tables. They must never become ISLAMU’s canonical field identity.

---

# 7. Validation, normalization, and sanitization

“Sanitization” should not mean silently deleting user input until it appears valid.

Use this pipeline:

```text
Provider value
  -> decode provider-specific shape
  -> normalize encoding and representation
  -> parse as canonical type
  -> apply field constraints
  -> apply cross-field registration rules
  -> persist typed value
  -> encode safely at output
```

## Normalization examples

* Unicode: normalize consistently, such as NFC.
* Text: normalize line endings and bounded surrounding whitespace.
* Email: store original display value plus a normalized comparison value.
* Phone: normalize to E.164 when country context is known.
* URL: parse structurally and permit configured schemes, normally HTTPS only.
* Country: ISO alpha-2 code.
* Language: BCP 47 language tag.
* Decimal: use database decimal, not floating point.
* Date: store as `date` where no time is intended.
* Instant: store an absolute UTC instant when an actual moment is intended.
* Option: map to an internal option ID rather than retaining provider labels.

## Sanitization rules

* Do not allow HTML in normal text fields.
* Do not store provider-rendered HTML.
* Escape text according to the output context.
* Use a dedicated rich-text type only if there is a strong business requirement and an audited allowlist sanitizer.
* Reject malformed URLs rather than attempting to repair them.
* Reject invalid types rather than coercing ambiguous input.
* Use file MIME detection, size limits, malware scanning, and quarantine before exposing uploads.

The existing custom-property validation vocabulary—requiredness, length, range, patterns, URL schemes, option membership, and multivalue shape—can be extracted into shared value objects and validators. The registration form system should reuse those primitives while adding registration-specific privacy, consent, branching, and lifecycle rules.

---

# 8. JSON Schema should be an interchange contract, not the database source of truth

JSON Schema 2020-12 is the current published JSON Schema version and separates core schema behavior from validation vocabulary. ([json-schema.org][9])

For ISLAMU:

* relational form/version/field/option rows remain authoritative;
* a JSON Schema 2020-12 document is generated for each published form version;
* the generated schema is immutable and content-hashed;
* provider adapters and external SDK consumers can use it;
* API documentation can expose it;
* validation errors can use stable JSON pointers or field keys.

Use separate artifacts for:

1. **Data schema** — types and validation.
2. **UI schema** — sections, help text, ordering, renderer hints.
3. **Logic schema** — bounded visibility and requiredness conditions.
4. **Provider mapping schema** — external question and option identities.

Do not turn JSON Schema itself into a general workflow engine.

## Bounded condition language

Support a deliberately small expression language:

* `equals`;
* `notEquals`;
* `in`;
* `contains`;
* `exists`;
* numeric comparison;
* date comparison;
* `all`;
* `any`;
* `not`.

Conditions may inspect earlier answers in the same form version.

Do not support:

* arbitrary JavaScript;
* arbitrary C# expressions;
* SQL;
* HTTP calls;
* authorization decisions;
* capacity decisions;
* payment decisions;
* registration-state mutation.

---

# 9. Provider abstraction

Do not create one enormous `IRegistrationFormProvider` whose implementations throw `NotSupportedException` for half their methods.

Use capability-specific interfaces:

```text
IRegistrationProviderDescriptor
IRegistrationPresentationProvider
IRegistrationSchemaReader
IRegistrationFormProvisioner
IRegistrationSubmissionWriter
IRegistrationSubmissionReader
IRegistrationCallbackVerifier
IRegistrationSubscriptionManager
IRegistrationReconciliationProvider
IRegistrationSubmissionSink
```

## Why `IRegistrationSubmissionSink` is separate

Formbricks, Google Sheets, Excel, a CRM, or a webhook consumer may be used as a downstream destination even when the form was completed natively.

For example:

```text
Built-in ISLAMU form
  -> canonical normalized submission
  -> finalized registration
  -> mirror approved fields to Formbricks
  -> export approved fields to Microsoft Excel
```

This is not the same as using Formbricks or Microsoft Forms to collect the response.

Separating collection providers from output sinks prevents an explosion of combinations such as:

```text
LocalWithFormbricksAndExcelProvider
GoogleWithFormbricksMirrorProvider
MicrosoftWithLocalFallbackProvider
```

## Capability vocabulary

Recommended capabilities include:

```text
SCHEMA_READ
FORM_CREATE
FORM_UPDATE
FORM_PUBLISH
NATIVE_RENDER
HEADLESS_RENDER
EMBED
REDIRECT
RESPONSE_WRITE
RESPONSE_READ
SIGNED_CALLBACK
PUSH_NOTIFICATION
POLLING_RECONCILIATION
PREFILL
OPAQUE_CORRELATION
FILE_UPLOAD
CONDITIONAL_LOGIC
MULTILINGUAL
RESPONSE_EDIT
RESPONSE_DELETE
SINGLE_USE_LINK
```

Runtime behavior should resolve:

```text
proven provider capability
∩ configured connection capability
∩ tenant governance
∩ form mapping compatibility
∩ authorization
```

The existing webhook architecture already follows a strong versioned capability-authority pattern rather than inferring features from provider names. Registration providers should follow the same design.

A capability profile should be bound to an exact tuple such as:

```text
ProviderCode
DeploymentKind
ProviderVersion or API Version
AdapterPolicyVersion
ConformanceEvidenceRevision
```

Unknown or untested tuples should fail closed for automatic registration finalization. They may still be available as redirect-only or manual-reconciliation channels.

---

# 10. Submission and finalization lifecycle

Do not use `ApprovalStatus` to represent provider processing.

There are at least three independent state machines.

## Registration intent state

```text
AwaitingRequirements
Processing
AwaitingApproval
Waitlisted
Confirmed
Rejected
Cancelled
Expired
NeedsReconciliation
```

## Registration attempt state

```text
Created
Launched
Submitted
Expired
Superseded
Cancelled
```

## Submission state

```text
Received
ProviderVerified
Normalized
Valid
Invalid
RequirementFulfilled
Finalized
Rejected
NeedsReconciliation
```

## Canonical flow

```text
Create registration intent
  -> evaluate required workflow requirements
  -> create attempt for selected channel
  -> launch native/headless/embed/redirect experience
  -> receive provider completion evidence
  -> verify provider evidence
  -> deduplicate
  -> normalize mapped answers
  -> validate against pinned form version
  -> fulfill requirement
  -> determine whether all mandatory requirements are fulfilled
  -> apply approval and capacity policy
  -> atomically materialize EventRegistration rows
  -> write outbox events
  -> notify attendee and organizer after commit
```

External form completion means:

> “A provider says a response was completed.”

It must not automatically mean:

> “The attendee now owns a confirmed place.”

ISLAMU must still enforce:

* identity correlation;
* required-field completeness;
* canonical type validation;
* mapping validity;
* schema-drift checks;
* registration dates;
* event/session availability;
* capacity;
* duplicate-registration rules;
* invitation or eligibility policy;
* manual approval;
* waitlist policy.

---

# 11. Incoming callback architecture

The repository already defines the correct callback pattern:

```text
Provider callback
  -> read raw body
  -> verify signature or API key
  -> write idempotent incoming message
  -> execute Application command or durable effect
```

It also requires duplicate acknowledgement and prohibits direct sensitive aggregate mutation from callback handlers.

Registration integrations should extend that existing intake system rather than introduce a separate unsafe callback mechanism.

Recommended endpoints include:

```text
POST /api/integrations/registration/formbricks/{bindingId}/callback
POST /api/integrations/registration/microsoft/{bindingId}/callback
POST /api/integrations/registration/google/pubsub
```

The controller should:

1. read and retain exact bounded bytes;
2. resolve the provider binding without exposing tenant existence;
3. verify the provider-specific proof;
4. enforce body-size and timestamp limits;
5. insert or acknowledge the incoming message;
6. create one unique registration-processing effect;
7. return promptly.

A worker then:

1. claims the effect using fencing or optimistic concurrency;
2. revalidates the retained evidence;
3. fetches the provider response where supported;
4. normalizes and validates;
5. fulfills the requirement;
6. finalizes the registration in an application transaction;
7. completes the effect only after the command succeeds.

The callback controller must never create `EventRegistration` rows directly.

---

# 12. Identity and attempt correlation

A provider response must be correlated with a registration intent before it can result in automatic registration.

## Recommended mechanism

Before showing or redirecting to an external form:

1. require an authenticated ISLAMU session for the first version;
2. create `EventRegistrationIntent`;
3. create `RegistrationAttempt`;
4. generate a cryptographically random attempt token;
5. store only its hash;
6. place the token in provider-supported metadata or a prefilled field;
7. make the token single-use at finalization;
8. set an expiry.

The token is correlation evidence. It is not itself proof that the current respondent owns the authenticated user account unless the surrounding flow provides that guarantee.

## Guest registration

The current registration model is user-centric. Before accepting unauthenticated public-form responses as registrations, add a first-class participant model such as:

```text
RegistrationParticipant
RegistrationParticipantPii
RegistrationParticipantVerification
```

Do not represent an anonymous attendee’s identity only as custom answers.

A safe initial scope is:

> Automatic external-form registration requires an authenticated ISLAMU user.

Guest registration can then be introduced deliberately with email verification, participant deduplication, and PII deletion semantics.

---

# 13. Capacity and external forms

An attendee may remain on an external form for several minutes. Capacity may change during that period.

Support explicit capacity policies:

### No hold

* no capacity is reserved when the form starts;
* capacity is evaluated at finalization;
* if full, the intent becomes waitlisted or rejected.

### Timed hold

* a short capacity hold is created when the attempt starts;
* the hold has an absolute expiry;
* finalization consumes it;
* expiration releases it automatically.

### Approval-only application

* no seat is promised at submission;
* completion produces `AwaitingApproval`;
* capacity is allocated only after approval.

Do not reserve a place indefinitely just because a user opened an embedded Google, Microsoft, or Formbricks form.

The final capacity decision and EventRegistration inserts should occur in one transaction with appropriate row or counter locking.

---

# 14. Detailed Formbricks design

Formbricks should be the first external provider to receive enterprise-grade support.

## Mode A — bring-your-own Formbricks survey

The organizer connects an existing Formbricks deployment and survey.

ISLAMU:

* imports or reads the survey schema;
* maps questions and options to canonical fields;
* records the schema fingerprint;
* registers a signed webhook;
* presents the survey through link or embed;
* receives `responseFinished`;
* imports and validates the response.

This gives existing Formbricks users a low-friction path.

## Mode B — ISLAMU-managed Formbricks provisioning

ISLAMU owns the canonical registration form version and creates or updates the Formbricks survey from it.

Publishing should require a compatibility preflight:

* all required canonical fields are supported;
* all canonical options have mappings;
* no unsupported mandatory condition exists;
* webhook registration succeeds;
* the survey is active;
* schema fingerprint matches the generated mapping.

## Mode C — headless Formbricks

ISLAMU renders its own Blazor form while Formbricks manages the survey definition and/or response copy.

Formbricks explicitly documents fetching the survey schema, rendering a custom frontend, and posting responses through its APIs. It also recommends server-side proxying when server validation, PII handling, or response enrichment is required. Registration data fits that category. ([Formbricks][1])

For automatic registration, submit through the ISLAMU backend rather than directly from the browser:

```text
Blazor
  -> ISLAMU registration submission API
  -> canonical validation and persistence
  -> optional Formbricks response write
```

This avoids making a browser-accessible Formbricks response endpoint the registration trust boundary.

## Mode D — mirror-only sink

The form is completed natively in ISLAMU. After validation, selected permitted fields are mirrored asynchronously to Formbricks for survey analytics.

This should use `IRegistrationSubmissionSink`, not the collection-provider interface.

## Formbricks self-hosting topology

For a managed Formbricks deployment:

* keep Formbricks’ database separate from the ISLAMU application database;
* use a workspace isolation strategy per ISLAMU tenant;
* keep admin and public survey domains separate where appropriate;
* store API keys and webhook secrets through ISLAMU secret bindings;
* pin the image and adapter compatibility profile;
* expose Formbricks only in optional local/full infrastructure profiles.

Formbricks documents a public/private domain split where public surveys and client APIs use a public domain while management remains on a private domain. ([Formbricks][10])

When Formbricks file-upload questions are enabled, its self-hosted deployment requires compatible object storage; Formbricks documents S3-compatible storage and browser upload requirements. ([Formbricks][11])

---

# 15. Detailed Google Forms design

Google Forms should support:

* connect Google account or Workspace;
* select an existing form;
* import its schema;
* map fields;
* optionally create a new form from an ISLAMU form version;
* publish it explicitly;
* present it by redirect or iframe;
* create a `RESPONSES` watch;
* renew the watch before its one-week expiry;
* fetch responses after Pub/Sub notifications;
* reconcile missed responses periodically.

## Google connection model

The provider connection should retain:

* OAuth credential secret reference;
* granted scopes;
* Google user or service identity;
* form ownership/access validation;
* Cloud project and Pub/Sub configuration reference;
* last token refresh;
* last successful response fetch;
* watch ID and expiry.

## Completion processing

```text
Google Pub/Sub notification
  -> authenticate intake
  -> deduplicate Pub/Sub message
  -> resolve form and binding
  -> list responses after stored checkpoint
  -> deduplicate response IDs
  -> map and normalize answers
  -> finalize or reconcile
```

Because notifications do not include the response data, the provider adapter must fetch it separately. ([Google for Developers][5])

## Correlation

Google officially supports pre-filled answers and iframe embedding. It does not document a hidden server-bound attempt metadata channel in the same way a deeply integrated survey platform can. ([Google Help][12])

Therefore:

* a prefilled attempt token may be used for correlation;
* the token should be signed or random, single-use, and expiring;
* it should not be treated as independent identity proof;
* stronger automatic registration should also require authenticated respondent identity, email matching under controlled form settings, or a user return-confirmation step;
* otherwise, route the submission to `NeedsReconciliation`.

## Limitations

Google Forms should not advertise:

* headless response submission;
* signed direct response webhooks;
* indefinite push subscriptions;
* guaranteed hidden metadata;
* automatic registration without correlation.

File-upload responses require additional Google Drive permissions, controlled import, file-size checks, malware scanning, and a decision about whether ISLAMU copies the file into its own object storage.

---

# 16. Detailed Microsoft Forms design

Microsoft Forms should initially support:

* official link presentation;
* official iframe embedding;
* a Power Automate or Logic Apps installation template;
* a signed or API-key-protected callback to ISLAMU;
* manual field mapping;
* test-event verification;
* connection health and reconciliation status.

## Recommended flow template

```text
Microsoft Forms:
When a new response is submitted
  -> Get response details
  -> Construct canonical callback envelope
  -> Add form ID, response ID, attempt token, timestamp
  -> Add ISLAMU API key or signature
  -> POST to ISLAMU callback endpoint
```

ISLAMU should publish a versioned Power Automate solution or documented flow template rather than asking every organizer to invent the integration independently.

The callback envelope should include:

* provider code;
* binding ID;
* form ID;
* response ID;
* attempt token;
* response timestamp;
* mapped response values;
* connector contract version;
* idempotency key.

## Trust boundary

This callback is not a Microsoft Forms-native signed webhook. It is an organizer-controlled automation flow.

Classify it as:

```text
TrustLevel = DelegatedAutomation
```

A tenant or event policy can decide whether that trust level permits automatic finalization or requires organizer review.

## Supported scope

Because the Microsoft Forms connector only works with organizational accounts, deep automatic integration should initially be limited to Microsoft 365 work or school accounts. ([Microsoft Learn][7])

Personal Microsoft Forms may still be offered as:

* redirect;
* iframe;
* manual confirmation;
* imported CSV/Excel reconciliation.

## Excel is a sink, not the transaction authority

An organizer may already depend on the Excel workbook generated from Microsoft Forms. Preserve that workflow, but do not wait for Excel synchronization to determine whether the registration completed.

The real-time path should be:

```text
Forms connector -> response details -> ISLAMU callback
```

Excel remains:

* an organizer reporting surface;
* an external data sink;
* a backup reconciliation source.

---

# 17. Provider switching and schema drift

## Switching providers

A provider switch must affect **future attempts**, not rewrite active attempts.

Correct behavior:

1. Existing attempts remain pinned to their original channel and mapping.
2. A new channel becomes primary for new attempts.
3. Old bindings remain available for callbacks and reconciliation.
4. An attendee may explicitly restart using a fallback channel.
5. Restart creates a new attempt and marks the former attempt `Superseded`.
6. A late callback from a superseded attempt is retained but cannot create a duplicate registration.

This mirrors the safe principle already used by the webhook system: provider switches affect new work while historical provider evidence remains where it originated.

## Schema drift

For every provider binding, retain:

* the last imported schema;
* a normalized schema fingerprint;
* provider revision when available;
* field and option mapping revision;
* last checked timestamp;
* compatibility result.

Classify drift:

```text
NoDrift
AdditiveOptionalChange
LabelOnlyChange
MappingRequired
RequiredFieldRemoved
TypeChanged
OptionSetChanged
UnsupportedChange
```

Recommended behavior:

* additive optional field: continue, warn;
* label-only change: continue;
* required field removed: fail closed;
* type change: fail closed;
* option-set change: require mapping review;
* unmapped provider-only field: retain externally or ignore according to policy;
* unknown schema revision: `NeedsReconciliation`.

Never silently rewrite mappings after submissions already exist.

---

# 18. Privacy and sensitive attendee information

Registration answers will frequently be more sensitive than public Event metadata.

Every field should include explicit governance:

* `DataClassification`;
* `PurposeCode`;
* `RetentionPolicyId`;
* `OrganizerVisibility`;
* `IsExportable`;
* `IsAnalyticsRelevant`;
* `IsOperationallyFilterable`;
* `RequiresExplicitConsent`;
* `IsReusableAcrossEvents`;
* `IsProviderTransferAllowed`.

Recommended classifications:

```text
Operational
Personal
SensitivePersonal
ConsentEvidence
Restricted
```

Examples:

* meal preference: personal or sensitive depending on context;
* disability accommodation: sensitive;
* emergency contact: restricted;
* marketing opt-in: consent evidence;
* volunteer role: operational.

## Sensitive value storage

For sensitive values, consider a split model:

```text
RegistrationAnswer
  -> general metadata and type

RegistrationSensitiveAnswerValue
  -> encrypted canonical value
  -> encryption key version
  -> optional tightly governed blind index
```

Do not make sensitive values generally searchable.

## Consent is not a Boolean answer

A consent field should create immutable evidence containing:

* consent purpose;
* exact text snapshot;
* text version;
* UI version;
* language;
* granted timestamp;
* withdrawal timestamp;
* participant;
* registration intent;
* provider/submission source.

A generic `BooleanValue = true` is not enough for long-term supportability.

## Third-party disclosure

When an external provider is selected, the attendee should see a clear notice before launch stating that the specified provider will process the answers.

The binding should record:

* external processor/provider name;
* data categories transferred;
* external retention statement;
* organizer-provided privacy link;
* whether ISLAMU imports a normalized copy;
* whether files leave ISLAMU-controlled storage.

---

# 19. Embedding and unified UX

An iframe can make the page feel unified, but it does not make the integration trustworthy or technically first-party.

## Rules

* Only allow provider domains configured on an approved provider connection.
* Do not allow arbitrary organizer-provided iframe HTML.
* Parse the provider form ID or URL and generate the iframe server-side.
* Add the domain to a controlled CSP `frame-src` allowlist.
* Provide “Open form in a new tab” as a fallback.
* Give the iframe an accessible title.
* Preserve keyboard focus behavior.
* Do not infer completion from iframe navigation or disappearance.
* Do not attempt to inspect a cross-origin iframe.
* Use `postMessage` only where a provider explicitly documents a message contract and validate the exact origin.

After the user submits externally, the ISLAMU page should display:

```text
We received your form submission.
Your registration is being processed.
```

The page can poll the registration-intent status or receive a server-side status update. It should not show “Registration confirmed” until the ISLAMU finalization transaction succeeds.

Formbricks headless mode is the best route for a truly unified visual experience because ISLAMU controls the renderer. Google and Microsoft should remain official iframe or redirect experiences.

---

# 20. Outgoing events and webhooks

After canonical processing, ISLAMU can publish events such as:

```text
registration.intent.created
registration.attempt.launched
registration.submission.received
registration.submission.validated
registration.submission.needs_reconciliation
registration.awaiting_approval
registration.confirmed
registration.waitlisted
registration.rejected
registration.cancelled
```

These should use the existing canonical webhook ledger and provider delivery architecture.

By default, webhook payloads should be thin:

```json
{
  "intentId": "...",
  "submissionId": "...",
  "eventId": "...",
  "status": "confirmed"
}
```

Do not include arbitrary attendee answers in general registration lifecycle webhooks.

A separately authorized endpoint can provide approved answer data to an integration that has:

* the required scope;
* field-level export permission;
* tenant authorization;
* an active retention window;
* audited access.

The Standard Webhooks specification recommends stable event types and formal payload schemas, and distinguishes thin from full payloads. The current ISLAMU webhook envelope is already directionally compatible, so no wholesale envelope rewrite is necessary. ([GitHub][13])

---

# 21. Operational requirements

Each provider should expose health and support signals.

## Formbricks

* connection validity;
* supported version tuple;
* survey existence;
* webhook registration status;
* schema drift;
* callback age;
* response-fetch reconciliation lag.

## Google

* OAuth token health;
* Pub/Sub watch state;
* watch expiry;
* watch renewal failure;
* last notification;
* last fetched response timestamp;
* reconciliation checkpoint;
* quota or rate-limit failures.

## Microsoft

* callback flow test result;
* last successful callback;
* connector contract version;
* stale binding warning;
* field-mapping completeness;
* expected-response-versus-callback reconciliation.

## Bounded metrics

Use only bounded labels such as:

```text
provider
operation
outcome
trust_level
completion_mode
failure_category
```

Do not label metrics with:

* tenant ID;
* Event ID;
* form ID;
* response ID;
* attendee ID;
* question key;
* email;
* answer value.

---

# 22. Recommended implementation phases

## Phase 0 — architecture and contracts

Create an ADR locking:

* Registration Data Collection bounded context;
* ISLAMU canonical authority;
* workflow/requirement/channel model;
* immutable form versions;
* provider capability authority;
* submission trust levels;
* answer privacy model;
* external completion versus registration finalization distinction.

Also add the new contribution-contract intent because the current intent table is unlikely to contain a registration-provider architecture category.

## Phase 1 — Native provider

Implement:

* workflow and requirement entities;
* immutable form versions;
* fields, options, and bounded conditions;
* Native Blazor renderer;
* typed normalized answer persistence;
* validation;
* requirement fulfillment;
* approval, capacity, and waitlist finalization;
* consent evidence;
* exports;
* tests.

This provider becomes the reference behavior against which every external adapter is tested.

## Phase 2 — provider framework

Implement:

* provider connections;
* bindings;
* capability profiles;
* field and option mappings;
* attempts;
* incoming provider effects;
* schema fingerprinting;
* reconciliation;
* provider health;
* provider-specific launch descriptors.

## Phase 3 — Formbricks

In this order:

1. BYO survey link/iframe.
2. Signed `responseFinished` callback.
3. Management API response retrieval.
4. Schema import and mapping.
5. Managed survey provisioning.
6. Headless ISLAMU renderer.
7. Native-to-Formbricks mirror sink.
8. Files and multilingual conformance.

## Phase 4 — Microsoft Forms

Implement:

* link/embed channel;
* versioned Power Automate solution;
* callback endpoint;
* API-key or signed callback;
* test setup wizard;
* manual schema mapping;
* organization-account restriction;
* reconciliation and manual import.

## Phase 5 — Google Forms

Implement:

* Google OAuth provider connection;
* form selection/import;
* form creation and explicit publication;
* field mapping;
* Pub/Sub watch creation and renewal;
* response checkpoint fetch;
* retry/backoff;
* schema drift;
* Drive file handling;
* reconciliation.

## Phase 6 — advanced orchestration

Add:

* multiple requirements per workflow;
* alternative and fallback channels;
* guest participants;
* group/family registration;
* provider migration;
* post-finalization amendments;
* answer projections and governed analytics;
* registration-form templates and packs.

---

# 23. Essential test matrix

The feature should not be considered production-ready without tests covering:

### Canonical form behavior

* immutable published versions;
* required and optional fields;
* all canonical types;
* multivalue ordering;
* option retirement;
* conditional requiredness;
* schema hash stability;
* template provenance.

### Provider conformance

* exact provider/version capability tuple;
* unsupported capability fails closed;
* field mapping completeness;
* option mapping;
* schema drift;
* provider deletion;
* token expiry;
* credential rotation.

### Callback reliability

* valid signature;
* invalid signature;
* stale timestamp;
* duplicate callback;
* out-of-order callback;
* callback before user return;
* user return before callback;
* provider accepted response but callback lost;
* fetch succeeds after callback retry;
* response edited after completion.

### Registration correctness

* two concurrent attempts by one user;
* fallback provider after failure;
* late superseded-provider callback;
* capacity race;
* waitlist race;
* approval required;
* duplicate finalization effect;
* transaction rollback;
* outbox creation after commit.

### Privacy and security

* no answers in logs;
* no answers in metric labels;
* no raw payload in ProblemDetails;
* cross-tenant binding access returns generic not-found;
* sensitive-answer encryption;
* retention cleanup;
* export authorization;
* file quarantine;
* CSP domain allowlist;
* malicious provider URL and SSRF attempts.

### UX and accessibility

* native keyboard completion;
* iframe accessible title;
* new-tab fallback;
* processing status announcements;
* callback-delayed completion;
* mobile layout;
* RTL;
* provider-unavailable fallback.

---

# 24. Architectural anti-patterns to reject

Do not implement any of the following:

1. **A single `ExternalRegistrationUrl` as the complete integration model.**
2. **Attendee answers inside Event custom-property values.**
3. **All response data stored only as JSONB.**
4. **Provider question IDs used as canonical field IDs.**
5. **Webhook controller directly inserting registrations.**
6. **External success page treated as proof of completion.**
7. **Iframe navigation treated as proof of completion.**
8. **Provider completion treated as capacity confirmation.**
9. **`ApprovalStatus` overloaded with synchronization states.**
10. **One provider interface with mostly unsupported methods.**
11. **Provider capabilities inferred from a provider-name string.**
12. **In-flight attempts silently switched to another provider.**
13. **External schema changes silently applied to published form versions.**
14. **Undocumented Microsoft internal APIs used in production.**
15. **Raw answers placed in ordinary outgoing registration webhooks.**
16. **Consent represented only by a Boolean field.**
17. **Anonymous external response attached directly to a User without verified correlation.**
18. **Provider-specific fields allowed to become mandatory across alternative channels without a canonical equivalent.**

---

# Final CTO recommendation

Build the feature around this invariant:

> **ISLAMU Event owns the registration workflow and normalized registration record. A form provider supplies a versioned collection channel and evidence of completion.**

The database structure should be:

```text
Event
  -> RegistrationWorkflow
     -> RegistrationRequirement
        -> RegistrationFormVersion
        -> RegistrationChannel
           -> RegistrationProviderBinding

EventRegistrationIntent
  -> RegistrationAttempt
     -> RegistrationSubmission
        -> RegistrationAnswer
        -> RegistrationAnswerFile
        -> RegistrationSubmissionIssue

EventRegistrationIntent
  -> RegistrationRequirementFulfillment
  -> EventRegistration
```

The built-in provider should define the complete contract. Formbricks should receive the deepest integration because its API, headless support, signed webhooks, and self-hostability make true feature parity realistic. Google Forms should use API import/provisioning, Pub/Sub notifications, response fetch, and iframe/redirect presentation. Microsoft Forms should use an officially documented Forms connector and a versioned Power Automate or Logic Apps callback solution.

That design preserves ISLAMU’s core differentiator: data remains typed, normalized, queryable, governed, auditable, and useful for filtering and analytics, regardless of which form interface the organizer chooses.

# Consultation Report No. 2

## Event Participation Modes, Community-Reported Listings, Guest Registration, Ticket Types, and Group Bookings

## 1. Purpose and relationship to Consultation Report No. 1

This report extends the first registration-data and forms-provider consultation. It does not repeat the provider abstraction, canonical form schema, answer normalization, callback verification, or Formbricks/Google Forms/Microsoft Forms recommendations already covered there.

This second report defines the broader **event participation model** around those forms:

* events that are only informational listings;
* walk-in events with no advance registration;
* events managed entirely on another platform;
* community-reported events submitted by somebody who is not the organizer;
* authenticated and unauthenticated registration;
* optional and non-synchronized external forms;
* multiple ticket types and quantities;
* family, household, group, and company bookings;
* the domain state required before a future payment step;
* the authorization and privacy boundary between a listing contributor and the real organizer.

The core conclusion is:

> **Listing an event, managing participation, collecting data, selling tickets, and receiving attendee information are separate authorities. Possessing one authority must never automatically grant the others.**

---

# 2. Executive architectural conclusion

The current Event model already contains the beginnings of this product direction through:

* `IsRegistrationRequired`;
* `IsUserReported`;
* `EventUrl`;
* `ExternalRegistrationUrl`;
* event-level price fields.

Those fields demonstrate that the platform already recognizes internal registration, community-reported events, and external participation. However, a collection of booleans and URLs cannot express the complete scenario matrix now required.

The new design must separate at least five independent dimensions:

1. **Who submitted and manages the listing?**
2. **Which system manages attendance or registration?**
3. **Is advance registration available, optional, or required?**
4. **Must the participant have an ISLAMU account?**
5. **What data, if any, is synchronized from an external form?**

Ticketing then adds another independent dimension:

6. **What admission products, quantities, participants, and capacity rules apply?**

These dimensions must be first-class typed domain state. They are policy-critical and cannot be represented as Layer 3 custom properties. The existing custom-property governance expressly requires workflow-critical registration state and authorization-sensitive concepts to remain explicit domain state rather than generic EAV rows.

---

# 3. Separate the four authorities

Every event should be evaluated against four separate authorities.

## 3.1 Listing authority

Listing authority answers:

> Who is allowed to contribute or maintain the public event information shown on ISLAMU Event?

Examples:

* verified organizer;
* organization administrator;
* tenant curator;
* community contributor;
* imported system;
* federated source.

Listing authority permits actions such as:

* supplying the title, schedule, location, images, and source URL;
* correcting public event information;
* submitting the listing for moderation.

It does **not** automatically permit:

* configuring registration;
* viewing attendees;
* collecting payments;
* exporting email addresses.

## 3.2 Participation-management authority

Participation-management authority answers:

> Which system is authoritative for reservations, capacity, approvals, waitlists, ticket quantities, and confirmed attendance?

The authoritative system may be:

* ISLAMU Event;
* an external event-management platform;
* no system at all because it is a walk-in or informational event.

There must be exactly one authoritative capacity and confirmation system for an event or a defined admission inventory.

## 3.3 Data-collection authority

Data-collection authority answers:

> Which system collects registration or questionnaire answers, and which copy is authoritative?

The collection channel may be:

* the native ISLAMU form;
* Formbricks;
* Google Forms;
* Microsoft Forms;
* another future provider.

A provider may collect data without ISLAMU receiving the answers. It may also send only completion evidence, selected fields, or a complete normalized response.

## 3.4 Commercial authority

Commercial authority answers:

> Who is permitted to define paid admission products and eventually collect payments?

Only a verified organizer or explicitly authorized managing actor may receive this authority.

A community contributor who reported an event must never receive commercial authority merely because they created the listing.

---

# 4. Replace boolean combinations with a typed participation policy

`IsRegistrationRequired` is too ambiguous for the required model. It does not distinguish:

* no registration offered;
* no registration needed;
* optional registration;
* external registration;
* ISLAMU registration;
* anonymous registration;
* authenticated registration;
* required external form;
* optional questionnaire.

Introduce a first-class `EventParticipationConfiguration`.

## 4.1 Participation handling mode

Recommended values:

| Code               | Meaning                                                                                |
| ------------------ | -------------------------------------------------------------------------------------- |
| `INFORMATION_ONLY` | ISLAMU provides the event listing, but there is no participation CTA.                  |
| `WALK_IN`          | No advance registration is expected; attendance is handled at the venue or informally. |
| `EXTERNAL_MANAGED` | Registration or event management occurs entirely in an external system.                |
| `ISLAMU_MANAGED`   | ISLAMU owns registration, capacity, ticket selection, and confirmation.                |

These values describe who manages participation. They do not describe whether entry is open, approval-based, or invite-only.

## 4.2 Advance-registration obligation

Add a separate value:

| Code             | Meaning                                                            |
| ---------------- | ------------------------------------------------------------------ |
| `NOT_APPLICABLE` | No advance registration workflow exists.                           |
| `OPTIONAL`       | A person may register, but registration is not required to attend. |
| `REQUIRED`       | Advance registration is expected for attendance.                   |

Examples:

* `WALK_IN + NOT_APPLICABLE`: arrive at the location.
* `EXTERNAL_MANAGED + REQUIRED`: register on the organizer’s external platform.
* `ISLAMU_MANAGED + OPTIONAL`: reserve a place through ISLAMU, but walk-ins are accepted.
* `ISLAMU_MANAGED + REQUIRED`: complete ISLAMU registration before attending.

## 4.3 Admission-decision mode

Keep admission policy separate:

* open/automatic;
* approval required;
* invite only;
* closed;
* waitlist only.

Authentication and admission approval must not be conflated. An event can allow guest registration while still requiring organizer approval.

## 4.4 Identity-access mode

Recommended values:

| Code                       | Meaning                                                                                                         |
| -------------------------- | --------------------------------------------------------------------------------------------------------------- |
| `ACCOUNT_REQUIRED`         | The buyer or lead registrant must authenticate with ISLAMU.                                                     |
| `GUEST_ALLOWED`            | A user may sign in or continue without an account.                                                              |
| `CAPABILITY_TOKEN_ALLOWED` | No recoverable account or verified email is required; management is possible only with an opaque booking token. |

The organizer may combine these with guest recovery requirements:

* verified email required;
* unverified email accepted;
* email optional;
* capability-link-only;
* no recovery after the confirmation page.

---

# 5. Valid public participation scenarios

The following scenarios must all be supported as normal configurations rather than edge cases.

| Scenario                                                 | Provenance            | Participation mode                       | Account requirement        | ISLAMU registration record                     |
| -------------------------------------------------------- | --------------------- | ---------------------------------------- | -------------------------- | ---------------------------------------------- |
| Public information listing only                          | Organizer or reported | `INFORMATION_ONLY`                       | None                       | No                                             |
| Walk-in event                                            | Organizer             | `WALK_IN`                                | None                       | No                                             |
| Event managed on another platform                        | Organizer             | `EXTERNAL_MANAGED`                       | Controlled externally      | No                                             |
| Community-reported event linking to official source      | Community reported    | `EXTERNAL_MANAGED` or `INFORMATION_ONLY` | Controlled externally      | No                                             |
| Native registration for members                          | Organizer             | `ISLAMU_MANAGED`                         | `ACCOUNT_REQUIRED`         | Yes                                            |
| Native public registration                               | Organizer             | `ISLAMU_MANAGED`                         | `GUEST_ALLOWED`            | Yes                                            |
| Registration asking only for a name                      | Organizer             | `ISLAMU_MANAGED`                         | `CAPABILITY_TOKEN_ALLOWED` | Yes                                            |
| Walk-in event with optional questionnaire                | Organizer             | `WALK_IN`                                | None                       | No registration; optional form only            |
| Native ticket registration with optional external survey | Organizer             | `ISLAMU_MANAGED`                         | Configurable               | Yes; survey does not block                     |
| External form with no ISLAMU synchronization             | Organizer             | `EXTERNAL_MANAGED` or optional action    | None                       | No, unless separate ISLAMU registration exists |

---

# 6. Event public actions must be a collection, including an empty collection

An event should not be forced to have either a registration button or an external redirect.

Introduce `EventPublicAction` as a typed, ordered collection.

Recommended action kinds:

| Action kind              | Intended use                                                        |
| ------------------------ | ------------------------------------------------------------------- |
| `ORIGINAL_SOURCE`        | View the source from which a community-reported event was obtained. |
| `EXTERNAL_EVENT_PAGE`    | View the organizer’s event-management page.                         |
| `EXTERNAL_REGISTRATION`  | Register through an external platform.                              |
| `OPTIONAL_QUESTIONNAIRE` | Open a questionnaire that does not determine attendance.            |
| `LIVESTREAM`             | Open an event livestream where applicable.                          |
| `ORGANIZER_CONTACT`      | Contact the organizer through an approved channel.                  |

The native ISLAMU registration action should normally be generated by the API from the participation configuration rather than stored as an arbitrary URL.

## 6.1 Zero actions is valid

An event may have no participation-related actions.

The UI may still offer platform actions such as:

* save event;
* share;
* add to calendar;
* report incorrect information.

But it must display no registration, redirect, or questionnaire button.

## 6.2 One primary participation action

At most one action should be presented as the primary participation CTA.

Examples:

* “Register on ISLAMU”
* “Register on organizer website”
* “View event on organizer website”

Optional questionnaires and source links should normally be secondary actions.

## 6.3 External action labels must be semantically accurate

Do not label every external link as “Register.”

Use:

* **View original event page**
* **Register on organizer website**
* **Open optional questionnaire**
* **View livestream**

An unsynchronized form must not produce language such as:

* “You are registered”
* “Your place is confirmed”
* “ISLAMU received your registration”

unless ISLAMU has independently received and validated sufficient completion evidence.

---

# 7. Community-reported events require a separate provenance and authority model

A user who contributes an event listing is not necessarily the event organizer.

The current `IsUserReported` field is useful as a starting point but is too coarse. Replace or supplement it with an explicit provenance model.

## 7.1 Recommended provenance values

```text
ORGANIZER_CREATED
COMMUNITY_REPORTED
TENANT_CURATED
IMPORTED
FEDERATED
```

Provenance must remain historical. If a community-reported event is later claimed by the organizer, the system should still be able to say:

> Originally submitted by a community contributor; organizer now verified.

## 7.2 Do not overload `ActorId`

The existing Event has a required `ActorId`. That field must not silently mean all of the following:

* person who submitted the listing;
* person or organization organizing the event;
* actor authorized to edit the listing;
* actor entitled to attendee information;
* actor that receives payments.

Recommended explicit identities:

| Field                 | Meaning                                                             |
| --------------------- | ------------------------------------------------------------------- |
| `SubmittedByUserId`   | User who contributed the event to ISLAMU.                           |
| `PublishedByActorId`  | Actor under whose authority the listing is published inside ISLAMU. |
| `OrganizerActorId`    | Verified organizer actor, nullable for unclaimed external events.   |
| `SourcePublisherName` | Human-readable external source or organizer name.                   |
| `SourceUrl`           | Original authoritative or reported URL.                             |

For an organizer-created event, `PublishedByActorId` and `OrganizerActorId` may be the same.

For a community-reported event:

* `SubmittedByUserId` is the contributor;
* `OrganizerActorId` is initially null;
* the contributor must not be treated as the organizer;
* `PublishedByActorId` may be a tenant curation actor or another governed publishing authority.

## 7.3 Community-reported-event UI

A community-reported event must have a persistent UI disclosure.

Recommended card badge:

> **Community reported**

Recommended detail-page panel:

> This event was shared by a community member and has not yet been claimed by the organizer. Verify the latest information on the original event page.

The panel should include:

* external source domain;
* date the source was last checked;
* “Suggest a correction” action;
* “Claim this event” action;
* “Report broken or unsafe link” action.

The badge must be derived from provenance state. The contributor must not be able to hide it.

## 7.4 Allowed powers for a community contributor

A community contributor may be allowed to:

* submit event information;
* update the submission while it is awaiting moderation;
* supply the original source URL;
* suggest later corrections;
* view the moderation status of their submission;
* withdraw their contribution before publication.

The contributor must not be allowed to:

* configure native registration;
* create registration forms;
* connect Formbricks, Google Forms, or Microsoft Forms;
* create ticket types;
* configure capacity or waitlists;
* collect or receive payments;
* view registrants;
* export attendee information;
* receive attendee email addresses;
* create organizer contact-consent prompts;
* configure registration webhooks.

## 7.5 External links on reported events

A reported event may still link to the original organizer’s platform.

However, before organizer verification:

* use fixed, non-deceptive labels;
* show the destination domain;
* mark the link as external;
* route it through an approved stored action, not a raw open-redirect query parameter;
* restrict schemes to HTTPS except explicitly governed local deployments;
* permit users to report unsafe or outdated destinations.

For an unverified community submission, the safest default label is:

> **View original event page**

After a moderator verifies that the URL is the official organizer registration page, the UI may use:

> **Register on organizer website**

---

# 8. Organizer claim and management transfer

Introduce `EventOrganizerClaim`.

Recommended fields:

```text
Id
TenantId
EventId
ClaimantActorId
ClaimStatus
EvidenceType
EvidenceReference
SubmittedAt
ReviewedAt
ReviewedByUserId
DecisionReasonCode
ConcurrencyStamp
```

Recommended states:

```text
Pending
EvidenceRequired
Approved
Rejected
Withdrawn
Expired
```

## 8.1 Claim approval effects

An approved claim may:

* set `OrganizerActorId`;
* grant event-management authorization;
* permit configuration of registration, forms, tickets, and future payments;
* display an organizer-verification UI marker;
* preserve the original community provenance.

## 8.2 No retroactive attendee-data grant

Claim approval must never retroactively grant access to data previously collected for another recipient or purpose.

In the normal design, unclaimed reported events cannot collect native registration information in the first place. This invariant prevents the problem entirely.

If any historical optional interaction data exists, the organizer must not receive it unless:

* the original consent named that organizer actor;
* the purpose permits the new access;
* retention has not expired;
* authorization still succeeds.

---

# 9. External event management is a first-class mode, not a fallback

Many organizers will use ISLAMU only for discovery and lead generation while maintaining:

* registration;
* ticketing;
* attendee communication;
* check-in;
* payments;
* reporting

on another platform.

This is a legitimate primary product mode.

## 9.1 External-managed event behavior

When `ParticipationHandlingMode = EXTERNAL_MANAGED`:

* ISLAMU publishes and enriches the listing;
* the primary CTA redirects to the organizer’s external system;
* ISLAMU does not create a canonical registration;
* ISLAMU does not reserve capacity;
* ISLAMU does not claim that registration succeeded;
* ISLAMU does not expose an attendee dashboard;
* ISLAMU does not infer attendance from an outbound click.

## 9.2 Lead and click attribution

ISLAMU may record aggregate outbound-action engagement:

```text
EventId
ActionId
OccurredAt
ReferrerSurface
AnonymousSessionClass
Outcome
```

This can provide:

* number of external registration clicks;
* event-page-to-external-page conversion;
* campaign attribution.

It must not automatically provide the organizer with:

* user account ID;
* email;
* profile;
* precise browsing history.

A click is not a registration and should not be named one in metrics or UI.

An optional referral token may be passed to the external platform, but it must:

* contain no direct personal identifier;
* be purpose-limited;
* expire;
* be generated only when the organizer has configured compatible attribution.

---

# 10. External form synchronization must be independently configurable

The first report defined external form providers and registration channels. This report adds a required per-binding synchronization policy.

## 10.1 Recommended answer synchronization modes

| Mode              | What ISLAMU receives                                             | Permitted effect                                        |
| ----------------- | ---------------------------------------------------------------- | ------------------------------------------------------- |
| `NONE`            | No completion or answer data                                     | External-only or optional action                        |
| `COMPLETION_ONLY` | Response identifier, verified completion, timestamps, no answers | May fulfill a requirement if correlation is trustworthy |
| `SELECTED_FIELDS` | Only explicitly mapped and approved fields                       | Normalize selected answers                              |
| `FULL_CANONICAL`  | All supported mapped answers                                     | Full canonical submission                               |
| `MIRROR_ONLY`     | ISLAMU sends approved data outward                               | External system is a downstream sink                    |

This setting belongs on the **registration channel or provider binding for the event**, not globally on the provider connection.

One organizer may use:

* Formbricks with full synchronization for one event;
* Formbricks completion-only for another;
* a Google Form with no synchronization for a walk-in event;
* a Microsoft Form as a fully external registration process.

## 10.2 No synchronization

When `AnswerSyncMode = NONE`:

* ISLAMU does not store provider answers;
* ISLAMU does not store a normalized registration submission;
* no webhook is required for registration processing;
* the form may remain completely external;
* the form may be embedded or opened externally;
* ISLAMU must not show a completion status it cannot verify.

This mode is suitable for:

* optional surveys;
* external organizer registration;
* forms whose data must remain entirely inside the organizer’s existing system.

## 10.3 Completion-only synchronization

Completion-only synchronization is an important middle ground.

The provider sends or exposes:

* provider response ID;
* registration-attempt token;
* completed timestamp;
* completion state;
* schema or binding revision;
* payload hash where relevant.

ISLAMU deliberately does not import the attendee’s answers.

This supports:

* privacy-minimized registration;
* organizers who want answers to remain inside Formbricks or Microsoft 365;
* ISLAMU capacity and ticketing with external form completion.

Completion-only can fulfill a mandatory requirement only when:

* the provider completion is verified;
* the response correlates to a pinned registration attempt;
* the provider binding and schema revision match;
* the attempt has not expired or been superseded.

## 10.4 Optional forms

Add explicit fields to `RegistrationRequirement`:

```text
Criticality
CanSkip
CompletionEffect
AnswerSyncMode
AppliesToSubjectType
```

Recommended criticality values:

```text
Required
Optional
Informational
PostRegistration
```

Recommended completion effects:

```text
BlocksRegistration
EnrichesRegistration
NoRegistrationEffect
```

An optional form must:

* display “Optional” clearly;
* provide a visible “Skip and continue” action;
* never block ticket confirmation;
* record `SkippedByRegistrant` when useful so the UI does not repeatedly prompt;
* avoid presenting skipped status as an error.

## 10.5 Standalone optional forms for walk-in events

A `WALK_IN` event may offer a standalone form asking only:

* name;
* dietary estimate;
* volunteer interest;
* optional contact information.

The form is not a registration unless the organizer explicitly enables an ISLAMU-managed registration workflow.

The UI should call it:

* optional questionnaire;
* attendance-interest form;
* volunteer-interest form;

not a confirmed reservation.

---

# 11. Guest registration requires changing the current registration aggregate

The current registration intent and registration rows require `UserId`, and the current session registration uniqueness is centered on one user per session. That structure cannot represent:

* unauthenticated registrations;
* one parent purchasing tickets for several children;
* one company purchasing tickets for many employees;
* multiple tickets of the same type;
* unnamed ticket holders;
* deferred participant assignment.

Do not solve this by making only `UserId` nullable. The aggregate itself is account-centric.

The correct distinction is:

```text
Buyer / Lead Booker
        ↓
Registration Order
        ↓
Order Lines / Ticket Quantities
        ↓
Participants
        ↓
Admission Assignments
```

## 11.1 Buyer is not necessarily an attendee

The buyer or lead booker may be:

* an authenticated user;
* an unauthenticated parent;
* an organization representative;
* a company administrator;
* a mosque volunteer;
* a household member.

The buyer may or may not attend.

## 11.2 Participant is not necessarily a user

A participant may be:

* linked to an ISLAMU user;
* a guest with a name and email;
* a child represented by a guardian;
* an unnamed ticket holder;
* an employee assigned later;
* a participant whose details are deliberately not collected.

## 11.3 Recommended guest registration behavior

When guest registration is allowed:

1. Create an opaque guest registration session.
2. Generate a high-entropy management token.
3. Store only its cryptographic hash.
4. Scope the token to one order.
5. Permit only limited operations:

   * view;
   * continue;
   * amend before cutoff;
   * cancel where permitted.
6. Expire or rotate it after order completion according to policy.
7. Never authorize guest access using a guessable order ID.

If email is provided, ISLAMU may send a management link.

If only a name is required:

* display a booking reference;
* permit capability-token management;
* warn that the booking cannot be recovered if the token is lost.

---

# 12. Anonymous writes require a new governed endpoint class

The repository currently classifies public endpoints as anonymous reads with no tenant mutation, while writes are normally authenticated. Guest registration cannot be implemented cleanly under that binary rule.

Introduce an explicit endpoint classification such as:

```text
PublicTransactional
```

or:

```text
AnonymousMutation
```

Recommended characteristics:

| Concern          | Requirement                                                |
| ---------------- | ---------------------------------------------------------- |
| Authentication   | `[AllowAnonymous]`                                         |
| Scope            | Only narrowly defined public transactions                  |
| Rate limiting    | Dedicated registration policy                              |
| CSRF             | Antiforgery for browser-originated same-site requests      |
| Idempotency      | Required for create/finalize operations                    |
| Authorization    | Event policy plus guest capability token                   |
| Data exposure    | Minimal, order-scoped                                      |
| Logging          | No submitted PII or form answers                           |
| Abuse control    | IP/session throttling, quotas, optional challenge provider |
| Tenant isolation | Resolved before mutation and enforced on every lookup      |

Do not weaken the meaning of the existing `Public` classification. Add a new class so public mutations remain visible and testable in architecture rules and OpenAPI.

---

# 13. Ticket types are first-class admission products

Ticket types must not be represented as custom form options.

Examples include:

* Adult;
* Child;
* Student;
* Senior;
* Family;
* VIP;
* Sponsor;
* Volunteer;
* Company package;
* Custom organizer-defined type.

Each is a first-class admission product with its own:

* title;
* description;
* price;
* availability;
* capacity;
* purchase limits;
* participant-data requirements;
* admission entitlements.

## 13.1 Recommended `EventTicketType`

```text
Id
TenantId
EventId
TicketCatalogVersionId
Code
DisplayName
Description
PriceAmount
CurrencyCode
CapacityPoolId
MinimumQuantityPerOrder
MaximumQuantityPerOrder
MaximumQuantityPerAccount
MaximumQuantityPerVerifiedContact
ParticipantDataCollectionMode
SalesStartAt
SalesEndAt
IsPublished
IsActive
SortOrder
ConcurrencyStamp
```

Potential typed eligibility fields may include:

```text
MinimumAge
MaximumAge
RequiresGuardian
RequiresApproval
```

Do not rely on a ticket being named “Child” to infer policy.

## 13.2 Ticket catalog versioning

Ticket configuration should follow the same immutable-publication approach as form versions:

```text
Draft
Published
Retired
```

A published catalog revision is immutable.

Changing:

* price;
* quantity limits;
* entitlement;
* participant-detail requirements;
* capacity-pool binding

creates a new revision.

Orders already started remain pinned to the revision they used.

## 13.3 One currency per order

For the first enterprise implementation:

* permit multiple ticket prices;
* require one currency across the active ticket catalog for an event/order;
* snapshot the currency and unit price on every order line.

Supporting multiple currencies inside one order would unnecessarily couple this phase to payment and settlement complexity.

## 13.4 Default general-admission ticket

Even a simple free event should internally use a default ticket type such as:

```text
GENERAL_ADMISSION
Price = 0
Capacity = unlimited or configured
```

The UI may hide ticket selection when there is only one fixed-quantity ticket type.

This avoids maintaining two separate registration engines:

* simple registration;
* ticket registration.

---

# 14. Shared capacity pools

Ticket type capacity and venue capacity are not always the same.

Example:

* 150 Adult tickets;
* 100 Child tickets;
* venue capacity of 200 people.

Adult and Child ticket types may need to share one capacity pool of 200.

Introduce `EventCapacityPool`:

```text
Id
TenantId
EventId
Name
MaximumQuantity
HoldDurationSeconds
OversellPolicy
IsActive
ConcurrencyStamp
```

Multiple ticket types may reference the same pool.

This supports:

* shared room capacity;
* separate VIP allocation;
* sponsor allocation;
* volunteer allocation;
* session-specific limits;
* family packages consuming multiple admission units.

---

# 15. Ticket entitlements and EventSession compatibility

The platform correctly preserves Event as the parent program and EventSession as its scheduled child. Ticketing should follow that aggregate direction rather than making sessions independent peer events.

A ticket type may grant admission to:

* the whole event;
* one event day;
* one session;
* a predefined session bundle;
* a choice of sessions.

Introduce `TicketTypeEntitlement`:

```text
TicketTypeId
EntitlementScopeType
EventDayId
EventSessionId
IncludedQuantity
SelectionRule
```

Recommended selection rules:

```text
AllIncluded
FixedSelection
ChooseOne
ChooseUpToN
```

Examples:

* Weekend Pass: all days and sessions.
* Friday Pass: one EventDay.
* Workshop Ticket: one EventSession.
* Conference Ticket: choose three workshops.
* Family Pass: multiple admission units across the whole event.

---

# 16. Registration order aggregate

Replace or substantially evolve the current user-centric `EventRegistrationIntent` into a booking/order aggregate.

## 16.1 `RegistrationOrder`

```text
Id
TenantId
EventId
AccountUserId
PurchaserActorId
BookingPartyType
Status
ParticipationConfigurationVersion
TicketCatalogVersionId
RegistrationWorkflowVersionId
GuestAccessTokenHash
CreatedAt
ExpiresAt
SubmittedAt
ConfirmedAt
CancelledAt
ConcurrencyStamp
```

Recommended `BookingPartyType` values:

```text
Individual
Household
Organization
Company
CommunityGroup
```

`AccountUserId` is nullable.

## 16.2 Purchaser PII

Use a separate PII structure:

```text
RegistrationOrderPii
- RegistrationOrderId
- ContactName
- Email
- NormalizedEmail
- Phone
- OrganizationName
```

Only collect the fields required by the event policy.

An account should not be automatically created from a guest order.

## 16.3 `RegistrationOrderLine`

```text
Id
RegistrationOrderId
TicketTypeId
Quantity
UnitPriceAmountSnapshot
CurrencyCodeSnapshot
LineSubtotalSnapshot
TicketTypeNameSnapshot
TicketCatalogVersionId
```

The snapshot protects in-flight orders from later ticket edits.

## 16.4 `RegistrationParticipant`

```text
Id
RegistrationOrderId
LinkedUserId
ParticipantPiiId
ParticipantType
GuardianParticipantId
Status
CreatedAt
```

A participant can be:

```text
Adult
Child
Dependent
Employee
Guest
Unnamed
```

The type is operational, not a replacement for all organizer-defined questions.

## 16.5 Ticket assignments

Introduce:

```text
RegistrationTicketAssignment
- Id
- RegistrationOrderLineId
- ParticipantId
- Ordinal
- AssignmentStatus
```

A ticket may initially remain unassigned when permitted.

## 16.6 Future admission ticket

After a free order is confirmed, or after a future payment succeeds, ISLAMU may materialize:

```text
AdmissionTicket
```

That future entity can contain:

* admission code;
* QR token;
* participant assignment;
* entitlement;
* check-in state.

Payment-provider architecture remains outside this report.

---

# 17. Participant-data collection modes

A family or company booking does not always require the same data for every ticket.

Add a participant-data policy at ticket-type or registration-requirement level.

Recommended modes:

| Mode                  | Behavior                                                                   |
| --------------------- | -------------------------------------------------------------------------- |
| `NONE`                | No participant identity is collected.                                      |
| `LEAD_BOOKER_ONLY`    | Only purchaser information is required.                                    |
| `PER_TICKET_OPTIONAL` | Participant details may be assigned to each ticket.                        |
| `PER_TICKET_REQUIRED` | Every ticket must be assigned before confirmation.                         |
| `DEFERRED_ASSIGNMENT` | Order can proceed; participant assignment is required by a later deadline. |

Examples:

### Family booking

```text
2 × Adult
3 × Child
```

Possible configuration:

* lead booker name and email required;
* adult names optional;
* child names required;
* guardian relationship required;
* dietary answers collected per child;
* one household address collected once at order level.

### Company booking

```text
25 × Employee Admission
```

Possible configuration:

* company contact required;
* employee names deferred;
* CSV or bulk assignment later;
* maximum 50 tickets per organization order;
* participant email optional;
* one billing or administrative contact collected at order level.

---

# 18. Registration answers must support multiple subjects

The first report recommended `AnswerSubjectTypeId` and `AnswerSubjectId`. This becomes essential for group bookings.

An answer may belong to:

```text
RegistrationOrder
Purchaser
Participant
TicketAssignment
SessionSelection
```

Examples:

| Question             | Subject                         |
| -------------------- | ------------------------------- |
| Company name         | RegistrationOrder or Purchaser  |
| Contact email        | Purchaser                       |
| Dietary restriction  | Participant                     |
| Child age            | Participant                     |
| Preferred workshop   | TicketAssignment or Participant |
| Accept booking terms | Purchaser                       |
| Photo consent        | Individual Participant          |

Do not copy one answer across every participant unless the form explicitly states it applies to the whole booking.

Registration requirements should support applicability such as:

```text
AllOrders
SpecificTicketType
EveryParticipant
LeadBookerOnly
ChildParticipants
SpecificSessionSelection
```

Use typed applicability rules, not arbitrary executable scripts.

---

# 19. Ticket quantity limits

Organizers need distinct limits.

## 19.1 Per-order limit

Example:

> A single booking may contain no more than six Child tickets.

Field:

```text
MaximumQuantityPerOrder
```

## 19.2 Per-account limit

Example:

> One authenticated user may acquire no more than four VIP tickets.

Field:

```text
MaximumQuantityPerAccount
```

## 19.3 Per-verified-contact limit

For guest registration:

```text
MaximumQuantityPerVerifiedContact
```

This requires a verified email or equivalent contact identity.

## 19.4 Per-booking-party limit

For company or organization bookings:

```text
MaximumQuantityPerBookingParty
```

The booking party should be linked to an existing Actor where possible.

## 19.5 Anonymous enforcement limitation

A hard “per user” limit cannot be guaranteed for a completely anonymous registrant who provides no verifiable identity.

In that mode, ISLAMU can apply only best-effort controls such as:

* per order;
* capability session;
* short-retention network signals;
* browser session;
* rate limiting.

The organizer UI must explain:

> Hard per-person limits require an account or a verified contact method.

The platform should never imply stronger enforcement than it can provide.

---

# 20. Capacity holds before payment

Payment is out of scope, but the pre-payment model must still prevent overselling.

Introduce `RegistrationInventoryHold`.

```text
Id
RegistrationOrderId
CapacityPoolId
TicketTypeId
Quantity
Status
CreatedAt
ExpiresAt
ConsumedAt
ReleasedAt
ConcurrencyStamp
```

Recommended states:

```text
Active
Consumed
Released
Expired
Cancelled
```

## 20.1 Hold policy

Recommended configurable policies:

| Policy                    | Behavior                                                           |
| ------------------------- | ------------------------------------------------------------------ |
| `NO_HOLD_UNTIL_READY`     | Capacity is checked only when the user completes required details. |
| `TIMED_HOLD_ON_SELECTION` | Capacity is temporarily reserved after ticket selection.           |
| `APPROVAL_NO_HOLD`        | Application is submitted without reserving capacity.               |
| `WAITLIST_WHEN_FULL`      | A full order becomes a waitlist request.                           |

The UI must display a visible hold expiry when applicable.

## 20.2 Atomic reservation

Creating a hold must atomically:

1. validate the active ticket-catalog version;
2. enforce ticket quantity limits;
3. check ticket and shared-pool capacity;
4. create the order lines;
5. reserve the required quantity;
6. write the order and hold state.

Repository governance already requires multi-step writes to execute inside a unit-of-work transaction, with external side effects after commit.

No provider HTTP request, email, webhook publication, or payment call should occur inside the inventory transaction.

---

# 21. Pre-payment registration lifecycle

Recommended `RegistrationOrderStatus` values:

```text
Draft
AwaitingIdentity
AwaitingParticipantDetails
AwaitingRequirements
ReadyForCheckout
AwaitingPayment
AwaitingApproval
Waitlisted
Confirmed
Expired
Cancelled
NeedsReconciliation
```

## 21.1 Free registration

```text
Draft
 -> AwaitingRequirements
 -> ReadyForCheckout
 -> Confirmed
```

There is no payment step.

## 21.2 Paid registration

```text
Draft
 -> AwaitingRequirements
 -> ReadyForCheckout
 -> AwaitingPayment
```

The future payment system owns the next transition.

This report does not define:

* payment providers;
* authorization/capture;
* refunds;
* chargebacks;
* taxes;
* payouts;
* settlement.

It defines only the stable order, price, participant, and capacity state needed before that boundary.

## 21.3 Approval-required registration

```text
Draft
 -> AwaitingRequirements
 -> AwaitingApproval
 -> Confirmed or Rejected
```

The system should define whether approval happens:

* before payment;
* after payment authorization;
* after full payment.

That later decision belongs to the payment consultation.

---

# 22. Contact sharing and attendee email authorization

The current contact-share model is linked to a `User`, a recipient actor, and optionally a registration intent. That is directionally correct for naming the recipient, but guest and group registration require a more general consent subject.

## 22.1 Verified recipient requirement

An email-sharing consent prompt may be displayed only when:

* `OrganizerActorId` is present;
* the organizer is verified or explicitly authorized;
* the consent purpose is configured;
* the event is not an unclaimed community-reported listing.

The recipient must be the verified organizer actor, not:

* the user who submitted the listing;
* an arbitrary external email;
* a tenant administrator merely moderating the event;
* an instance administrator.

## 22.2 Guest consent subjects

Replace the assumption that every consent subject is a User.

Recommended subject types:

```text
User
RegistrationPurchaser
RegistrationParticipant
GuestContact
```

A consent record should refer to the subject through a typed subject reference.

## 22.3 Purchaser cannot consent for every adult

For a group booking:

* the purchaser may consent to sharing their own contact information;
* the purchaser must not automatically grant marketing or contact-sharing consent for unrelated adult participants;
* each adult participant requires their own consent where such consent is required;
* a guardian may provide operational information for a child according to applicable policy;
* marketing contact collection for children should be disabled by default.

## 22.4 No retroactive consent

The following must never imply consent:

* purchasing a ticket;
* completing registration;
* being assigned to a ticket;
* the organizer later claiming a reported event;
* using an external form;
* clicking an external registration link.

---

# 23. Authorization matrix

| Action                            |                 Community contributor |    Verified organizer |                Tenant curator |       Instance administrator |
| --------------------------------- | ------------------------------------: | --------------------: | ----------------------------: | ---------------------------: |
| Submit event listing              |                                   Yes |                   Yes |                           Yes |                     Governed |
| Edit pending community submission |                        Own submission |                   N/A |                           Yes | No automatic business access |
| Correct published reported event  |                          Suggest only |  After verified claim |                      Moderate | No automatic business access |
| Configure external source link    |                Source only, moderated |                   Yes |                      Moderate |                     Governed |
| Enable native registration        |                                    No |                   Yes |     Only with event authority |       No automatic authority |
| Configure external form provider  |                                    No |                   Yes | Only with delegated authority |          Infrastructure only |
| Create ticket types               |                                    No |                   Yes | Only with delegated authority |       No automatic authority |
| Configure future payments         |                                    No |                   Yes | Only with delegated authority |          Infrastructure only |
| View attendees                    |                                    No | Yes, permission-gated |  Only if explicitly delegated |          No automatic access |
| Export consented emails           |                                    No |    Yes, purpose-gated |  Only if explicitly delegated |          No automatic access |
| Claim event                       | May request if actual organizer actor |                   Yes |                  Review claim |  No automatic business claim |

Authorization should use resource attributes such as:

```text
provenance_type
organizer_actor_id
submitted_by_user_id
participation_mode
organizer_verification_status
management_authority
```

Do not duplicate these rules as client-side role checks.

The repository already treats server-authored HAL links as the authoritative UI affordance contract; the Blazor client should display actions only when the API emits the corresponding link.

---

# 24. Recommended HAL relations

Public event resources may expose:

```text
self
view-original-source
external-event-page
external-registration
start-registration
start-guest-registration
sign-in-to-register
optional-questionnaire
claim-event
suggest-correction
report-external-link
```

Organizer-authorized resources may additionally expose:

```text
configure-participation
manage-public-actions
manage-registration-workflow
manage-ticket-types
manage-capacity-pools
view-registration-orders
view-participants
export-consented-contacts
configure-future-payment
```

A community contributor should receive only contributor-safe relations.

Do not add DTO booleans such as:

```text
CanViewAttendees
CanCreateTickets
CanConfigurePayments
```

The server-authored links should remain the action authority.

---

# 25. External-link security

All external event, registration, and form URLs are untrusted input.

Required controls:

* HTTPS by default;
* normalized URI parsing;
* blocked dangerous schemes;
* no raw organizer-provided iframe markup;
* stored action ID for redirect endpoints;
* no generic `?url=` open-redirect endpoint;
* external-domain disclosure;
* `noopener`;
* `noreferrer` where appropriate;
* bounded link-checking process;
* no backend fetch to private or metadata networks;
* moderation and user-reporting path for malicious links;
* action disablement without deleting provenance.

Recommended action health states:

```text
PendingReview
Active
Broken
Unsafe
Disabled
Expired
```

For community-reported events, links should be reviewed more strictly than verified-organizer links.

---

# 26. Provider callbacks remain separate from public actions

An external link or embedded form is a public presentation action.

A provider callback is an authenticated integration message.

Do not merge them.

Where completion synchronization is enabled, the existing incoming-integration pattern remains correct:

```text
Provider callback
 -> raw body verification
 -> idempotent incoming message
 -> durable effect
 -> Application command
```

Provider callbacks must not directly confirm registration or allocate tickets inside the controller.

When synchronization is disabled, no registration callback is required.

---

# 27. Normative functional requirements

## 27.1 Event listing and provenance

| ID           | Requirement                                                                                             |
| ------------ | ------------------------------------------------------------------------------------------------------- |
| `FR-PROV-01` | Every event shall have an explicit provenance type.                                                     |
| `FR-PROV-02` | The system shall distinguish the listing contributor from the organizer.                                |
| `FR-PROV-03` | Community-reported events shall display a non-removable public provenance indicator.                    |
| `FR-PROV-04` | A community contributor shall not receive registration, ticketing, payment, or attendee-data authority. |
| `FR-PROV-05` | A reported event may include a moderated original-source or official external-event URL.                |
| `FR-PROV-06` | The actual organizer shall be able to claim a reported event through an auditable claim process.        |
| `FR-PROV-07` | Organizer claim approval shall not retroactively grant access to previously collected data.             |
| `FR-PROV-08` | An event shall be publishable with no participation action.                                             |

## 27.2 Participation actions

| ID          | Requirement                                                                                                        |
| ----------- | ------------------------------------------------------------------------------------------------------------------ |
| `FR-ACT-01` | An organizer shall be able to choose information-only, walk-in, external-managed, or ISLAMU-managed participation. |
| `FR-ACT-02` | An event shall support zero public participation actions.                                                          |
| `FR-ACT-03` | An event shall support an external event-management redirect without creating an ISLAMU registration.              |
| `FR-ACT-04` | External action labels shall distinguish event pages, registration pages, and optional questionnaires.             |
| `FR-ACT-05` | ISLAMU shall not infer successful registration from an outbound click or iframe navigation.                        |
| `FR-ACT-06` | The public API shall expose participation actions through server-authored HAL links.                               |
| `FR-ACT-07` | External action engagement may be counted without disclosing attendee identity.                                    |

## 27.3 Authentication and guest access

| ID            | Requirement                                                                                  |
| ------------- | -------------------------------------------------------------------------------------------- |
| `FR-GUEST-01` | An organizer shall be able to require an authenticated ISLAMU account.                       |
| `FR-GUEST-02` | An organizer shall be able to permit guest registration.                                     |
| `FR-GUEST-03` | A guest flow shall support a name-only registration where configured.                        |
| `FR-GUEST-04` | A guest registration shall receive an opaque scoped management token.                        |
| `FR-GUEST-05` | The system shall not automatically create an account from guest registration data.           |
| `FR-GUEST-06` | Public registration mutations shall use a dedicated governed endpoint classification.        |
| `FR-GUEST-07` | Authentication requirement shall remain separate from approval, invite, and waitlist policy. |

## 27.4 External forms and synchronization

| ID           | Requirement                                                                                                            |
| ------------ | ---------------------------------------------------------------------------------------------------------------------- |
| `FR-SYNC-01` | Form-answer synchronization shall be configurable per event channel or binding.                                        |
| `FR-SYNC-02` | Supported synchronization modes shall include none, completion-only, selected fields, full canonical, and mirror-only. |
| `FR-SYNC-03` | An optional form shall be skippable and shall not block registration.                                                  |
| `FR-SYNC-04` | A no-sync external form shall not automatically fulfill an ISLAMU registration requirement.                            |
| `FR-SYNC-05` | Completion-only synchronization shall not persist answer values.                                                       |
| `FR-SYNC-06` | A walk-in event may expose an optional standalone form without creating a registration.                                |
| `FR-SYNC-07` | The UI shall identify forms that are optional and externally processed.                                                |

## 27.5 Tickets and group booking

| ID             | Requirement                                                                                                              |
| -------------- | ------------------------------------------------------------------------------------------------------------------------ |
| `FR-TICKET-01` | A verified organizer shall be able to create multiple custom ticket types.                                               |
| `FR-TICKET-02` | Each ticket type shall support a custom price, currency, capacity, ordering, and availability window.                    |
| `FR-TICKET-03` | A buyer shall be able to select multiple quantities across multiple ticket types in one order.                           |
| `FR-TICKET-04` | A booking shall distinguish the purchaser from individual participants.                                                  |
| `FR-TICKET-05` | A booking shall support participants who do not have ISLAMU accounts.                                                    |
| `FR-TICKET-06` | A ticket type shall define whether participant data is not collected, optional, required, lead-booker-only, or deferred. |
| `FR-TICKET-07` | Ticket types shall support per-order, per-account, per-verified-contact, and per-booking-party limits.                   |
| `FR-TICKET-08` | Multiple ticket types shall be able to share one capacity pool.                                                          |
| `FR-TICKET-09` | Ticket types shall be able to grant event-, day-, session-, or bundle-level entitlements.                                |
| `FR-TICKET-10` | A family or company booking shall be representable as one order with many ticket lines and participants.                 |
| `FR-TICKET-11` | Paid order lines shall be fully modeled before the future payment handoff.                                               |
| `FR-TICKET-12` | A free order shall be confirmable without a payment workflow.                                                            |
| `FR-TICKET-13` | Ticket prices and rules shall be snapshotted for in-flight orders.                                                       |
| `FR-TICKET-14` | Capacity holds shall expire and release inventory automatically.                                                         |

## 27.6 Privacy and data authority

| ID           | Requirement                                                                                      |
| ------------ | ------------------------------------------------------------------------------------------------ |
| `FR-PRIV-01` | A community contributor shall never receive attendee email addresses.                            |
| `FR-PRIV-02` | Contact-sharing consent shall name a verified organizer actor as recipient.                      |
| `FR-PRIV-03` | Guest contacts and participants shall be valid consent subjects without requiring a User record. |
| `FR-PRIV-04` | A purchaser’s consent shall not automatically apply to every adult participant.                  |
| `FR-PRIV-05` | ISLAMU shall not expose attendee-management surfaces for external-managed events.                |
| `FR-PRIV-06` | External provider disclosures shall identify where attendee data is processed.                   |

---

# 28. Non-functional requirements

| ID       | Requirement                                                                                                                                                 |
| -------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `NFR-01` | Provenance, participation authority, authentication mode, ticket limits, capacity, and consent recipient shall be typed first-class state.                  |
| `NFR-02` | Authorization shall fail closed when organizer authority is absent or ambiguous.                                                                            |
| `NFR-03` | Guest create, update, and finalize operations shall be idempotent.                                                                                          |
| `NFR-04` | Ticket allocation and capacity holds shall be concurrency-safe across multiple API replicas.                                                                |
| `NFR-05` | Guest and participant PII shall be isolated from ordinary order state and governed by retention.                                                            |
| `NFR-06` | Price, ticket-catalog, form, consent-text, and participation-policy versions shall be snapshotted.                                                          |
| `NFR-07` | External links shall be protected against unsafe schemes, open redirects, and private-network access.                                                       |
| `NFR-08` | Public registration endpoints shall have dedicated rate limits and anti-abuse controls.                                                                     |
| `NFR-09` | Logs, traces, metrics, and ProblemDetails shall not contain attendee answers, emails, guest tokens, or provider payloads.                                   |
| `NFR-10` | Operational metrics shall use bounded dimensions such as mode, outcome, ticket status, and provider.                                                        |
| `NFR-11` | All ticket quantity controls, optional-form skip controls, provenance badges, and status changes shall satisfy the repository’s accessibility requirements. |
| `NFR-12` | Provider outages shall not remove access to external no-sync links or information-only event pages.                                                         |
| `NFR-13` | Organizer claims, price changes, capacity changes, attendee exports, and consent access shall be audited.                                                   |
| `NFR-14` | Public event read models shall not expose management URLs, provider secrets, guest tokens, or internal attendee counts unless explicitly intended.          |

---

# 29. Recommended persistence model

```text
Event
├── EventProvenance
├── EventParticipationConfiguration
├── EventPublicAction[]
├── EventOrganizerClaim[]
├── EventTicketCatalogVersion[]
│   ├── EventTicketType[]
│   │   └── TicketTypeEntitlement[]
│   └── EventCapacityPool[]
└── RegistrationWorkflow
    └── RegistrationRequirement[]
        └── RegistrationChannel[]

RegistrationOrder
├── RegistrationOrderPii
├── RegistrationOrderLine[]
├── RegistrationParticipant[]
├── RegistrationTicketAssignment[]
├── RegistrationInventoryHold[]
├── RegistrationRequirementFulfillment[]
└── RegistrationSubmission[]
    └── RegistrationAnswer[]
```

Future:

```text
RegistrationOrder
├── PaymentAttempt[]
└── AdmissionTicket[]
```

The future payment entities are intentionally outside this report.

---

# 30. Migration from the existing model

Breaking changes are preferable to preserving misleading fields.

| Existing model                    | Recommended destination                         |
| --------------------------------- | ----------------------------------------------- |
| `Event.IsUserReported`            | `EventProvenance.ProvenanceType`                |
| `Event.IsRegistrationRequired`    | `EventParticipationConfiguration`               |
| `Event.EventUrl`                  | Typed `EventPublicAction`                       |
| `Event.ExternalRegistrationUrl`   | Typed `EventPublicAction` or provider binding   |
| `Event.Price`                     | Derived display from active ticket types        |
| `EventSession.Price`              | Session ticket type or entitlement pricing      |
| `EventRegistrationIntent.UserId`  | Nullable `RegistrationOrder.AccountUserId`      |
| `EventRegistration.UserId`        | Optional `RegistrationParticipant.LinkedUserId` |
| Unique session-user registration  | Ticket/admission assignment uniqueness          |
| `EventContactShareConsent.UserId` | Typed consent subject reference                 |

## 30.1 Existing-data migration defaults

Suggested migration behavior:

1. `IsUserReported = true`

   * provenance becomes `COMMUNITY_REPORTED`;
   * no organizer actor is inferred from the contributor;
   * native registration/ticketing disabled pending claim.

2. `ExternalRegistrationUrl` present

   * create `EXTERNAL_REGISTRATION` action;
   * participation mode becomes `EXTERNAL_MANAGED` unless another explicit authority exists.

3. `IsRegistrationRequired = true` with no external registration URL

   * participation mode becomes `ISLAMU_MANAGED`;
   * access mode defaults to `ACCOUNT_REQUIRED` to preserve current behavior.

4. Existing non-null Event price

   * create one General Admission ticket type;
   * snapshot the original currency and price.

5. Existing single-user registrations

   * create one order;
   * create one General Admission line;
   * create one participant linked to the historical User;
   * preserve session entitlements.

---

# 31. Required test matrix

## 31.1 Participation modes

* information-only event emits no participation CTA;
* walk-in event displays no advance-registration message;
* external-managed event emits only external action;
* ISLAMU-managed event emits native registration action;
* closed event emits no start action;
* optional registration clearly indicates attendance may not require booking.

## 31.2 Community-reported events

* contributor cannot create a form;
* contributor cannot create a ticket type;
* contributor cannot view registrations;
* contributor cannot configure future payment;
* reported badge cannot be removed;
* external source link uses fixed safe labeling;
* claim approval grants organizer-management links;
* claim approval does not grant historical unrelated data;
* rejected claim preserves provenance and existing management authority.

## 31.3 Guest registration

* account-required event rejects anonymous start;
* guest-allowed event accepts anonymous start;
* capability token can access only its order;
* guessed order ID provides no access;
* expired token fails safely;
* guest order can later be linked to an authenticated account through an explicit verification flow;
* name-only order can complete when policy permits;
* no account is silently created.

## 31.4 External forms

* no-sync optional form can be skipped;
* no-sync form does not fulfill a required ISLAMU requirement;
* completion-only form stores no answer values;
* selected-field mode stores only approved mapped fields;
* provider completion for an expired attempt cannot finalize an order;
* provider response for a superseded attempt cannot create duplicate registration;
* external-managed event never creates an ISLAMU registration from a click.

## 31.5 Tickets and group bookings

* one order can contain Adult and Child quantities;
* per-order ticket limit is enforced;
* per-account limit is enforced atomically;
* verified-contact limit is enforced for guests;
* anonymous no-contact mode does not claim hard per-person enforcement;
* shared capacity pool prevents combined overselling;
* two concurrent orders cannot both consume the final place;
* expired hold returns capacity;
* ticket-price revision does not alter existing order lines;
* participant details can be required for Child but optional for Adult;
* company order supports deferred participant assignment;
* session entitlements are correctly materialized from ticket type;
* free order confirms without payment;
* paid order stops at `AwaitingPayment`.

## 31.6 Consent and attendee access

* no contact-sharing prompt on an unclaimed reported event;
* contributor receives no attendee-data HAL link;
* verified organizer receives only authorized attendee-data links;
* purchaser consent applies only to purchaser contact;
* adult participant consent is independent;
* withdrawn consent is excluded from exports;
* organizer claim does not reactivate or reinterpret old consent;
* export is audited and tenant-scoped.

---

# 32. Implementation sequence

## Phase 0 — decision records and governance

Create ADRs covering:

* event provenance and organizer authority;
* participation-handling modes;
* guest registration and public transactional endpoints;
* buyer/order/participant separation;
* ticket catalog and capacity pools;
* external synchronization modes;
* attendee-data authority.

Add a new Contribution Contract intent because guest registration and public transactional writes do not fit the current read-public/write-authenticated classification.

## Phase 1 — provenance and public actions

Implement:

* `EventProvenance`;
* organizer versus contributor identity;
* community-reported UI badge;
* external source/action model;
* information-only and walk-in modes;
* claim workflow;
* authorization restrictions.

This phase delivers discovery-only use cases without waiting for ticketing.

## Phase 2 — participation configuration

Implement:

* typed participation mode;
* advance-registration obligation;
* identity-access mode;
* HAL action generation;
* external-managed event behavior;
* no-action event behavior.

## Phase 3 — guest transaction security

Implement:

* `PublicTransactional` endpoint class;
* guest registration principal;
* hashed capability tokens;
* guest order recovery;
* rate limiting;
* antiforgery and idempotency;
* PII separation.

## Phase 4 — ticket catalog and orders

Implement:

* ticket-catalog versions;
* ticket types;
* shared capacity pools;
* order lines;
* price snapshots;
* ticket quantity limits;
* inventory holds;
* free-order confirmation;
* paid-order `AwaitingPayment` boundary.

## Phase 5 — family and company participants

Implement:

* buyer versus participant;
* multiple participant assignments;
* participant-data modes;
* deferred assignment;
* requirement applicability by ticket type and participant;
* group-booking amendment flows.

## Phase 6 — synchronization-policy extension

Extend Report No. 1 provider architecture with:

* `NONE`;
* `COMPLETION_ONLY`;
* `SELECTED_FIELDS`;
* `FULL_CANONICAL`;
* `MIRROR_ONLY`;
* optional and skippable requirements;
* walk-in standalone questionnaires.

## Phase 7 — consent and attendee-data surfaces

Implement:

* verified organizer recipient rule;
* guest consent subjects;
* per-participant consent;
* audited exports;
* retention;
* HAL-protected attendee management.

## Phase 8 — future payment consultation

Only after the order, ticket, participant, price, capacity, and approval models are stable should the payment architecture define:

* provider abstraction;
* checkout;
* authorization/capture;
* refunds;
* fees;
* tax;
* payouts;
* reconciliation.

---

# 33. Architectural anti-patterns to reject

Do not implement any of the following:

1. Adding more booleans to `Event` for every registration combination.
2. Treating `ActorId` simultaneously as reporter, publisher, organizer, and payment recipient.
3. Granting organizer rights to whoever created a reported listing.
4. Allowing a reported-event contributor to connect a form provider.
5. Allowing a reported-event contributor to view attendee email addresses.
6. Treating an external-link click as a registration.
7. Treating iframe completion or return navigation as registration proof.
8. Calling an optional questionnaire “registration.”
9. Requiring every attendee to have an ISLAMU account.
10. Making only `EventRegistrationIntent.UserId` nullable while retaining the one-user aggregate.
11. Storing one quantity field on a user registration instead of modeling order lines and participants.
12. Modeling ticket types as custom-form choices.
13. Using Event or EventSession `Price` as the authoritative price after ticket types exist.
14. Enforcing family or company quantity limits only in the UI.
15. Claiming hard per-user limits for anonymous, unverified registrants.
16. Running provider HTTP calls inside the capacity-reservation transaction.
17. Letting multiple unsynchronized systems independently own the same capacity pool.
18. Allowing a no-sync form to block ISLAMU registration.
19. Automatically sharing purchaser consent with all adult participants.
20. Granting historical attendee data to an organizer who later claims an event.
21. Creating generic open-redirect endpoints for organizer URLs.
22. Hiding community-reported provenance after publication.
23. Using Layer 3 custom properties for provenance, registration authority, ticket limits, or payment status.
24. Adding client-side `CanViewAttendees` or role checks instead of server-authored HAL links.
25. Building payment-provider integration before the order and inventory aggregate is stable.

---

# 34. Final CTO recommendation

The combined architecture from Report No. 1 and this report should be governed by the following invariant:

> **ISLAMU Event owns the meaning of its own event listing, registration order, ticket inventory, participant assignments, and consent records only when the event is configured as ISLAMU-managed. An external system may instead remain fully authoritative, and a community contributor may supply a listing without receiving organizer authority.**

The most important required redesign is the move from:

```text
Event
 -> IsRegistrationRequired
 -> ExternalRegistrationUrl
 -> EventRegistration(UserId)
```

to:

```text
Event
├── Provenance and organizer authority
├── Participation configuration
├── Zero or more public actions
├── Optional external form channels
└── Versioned ticket catalog

RegistrationOrder
├── Buyer or guest capability
├── Multiple ticket-type quantities
├── Multiple participants
├── Requirement fulfillments
├── Capacity holds
└── Future payment handoff
```

This model supports all required scenarios without misrepresenting authority:

* an organizer using ISLAMU only to generate external leads;
* a community member contributing a useful event listing;
* an event with no registration or redirect action;
* a walk-in event with an optional form;
* a public guest registration asking only for a name;
* an authenticated-only application process;
* a Formbricks, Google Forms, or Microsoft Forms flow with no answer synchronization;
* completion-only integration without storing answers;
* a father selecting tickets for an entire family;
* a company reserving tickets for many employees;
* child and adult ticket types with different prices and requirements;
* a future payment step that can be added without redesigning registration again.

The current user-centric registration model should not be incrementally patched for these requirements. It should be deliberately replaced by a **provenance-aware, authority-aware, buyer–order–participant–ticket architecture** before payment integration begins.


[1]: https://formbricks.com/docs/surveys/best-practices/headless-surveys "https://formbricks.com/docs/surveys/best-practices/headless-surveys"
[2]: https://formbricks.com/docs/platform/features/integrations/webhooks "https://formbricks.com/docs/platform/features/integrations/webhooks"
[3]: https://formbricks.com/docs/api-reference/rest-api "https://formbricks.com/docs/api-reference/rest-api"
[4]: https://developers.google.com/workspace/forms/api/guides "https://developers.google.com/workspace/forms/api/guides"
[5]: https://developers.google.com/workspace/forms/api/guides/push-notifications "https://developers.google.com/workspace/forms/api/guides/push-notifications"
[6]: https://developers.google.com/workspace/forms/api/guides/api-changes-to-google-forms "https://developers.google.com/workspace/forms/api/guides/api-changes-to-google-forms"
[7]: https://learn.microsoft.com/en-us/connectors/microsoftforms/ "https://learn.microsoft.com/en-us/connectors/microsoftforms/"
[8]: https://support.microsoft.com/en-us/forms/send-a-form-and-collect-responses "https://support.microsoft.com/en-us/forms/send-a-form-and-collect-responses"
[9]: https://json-schema.org/specification "https://json-schema.org/specification"
[10]: https://formbricks.com/docs/self-hosting/configuration/domain-configuration "https://formbricks.com/docs/self-hosting/configuration/domain-configuration"
[11]: https://formbricks.com/docs/self-hosting/configuration/file-uploads "https://formbricks.com/docs/self-hosting/configuration/file-uploads"
[12]: https://support.google.com/docs/answer/2839588?hl=en&utm_source=chatgpt.com "Publish & share your form with responders"
[13]: https://github.com/standard-webhooks/standard-webhooks/blob/main/spec/standard-webhooks.md "https://github.com/standard-webhooks/standard-webhooks/blob/main/spec/standard-webhooks.md"
