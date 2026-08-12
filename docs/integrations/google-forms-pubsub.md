<!-- ABOUTME: Operator contract for Google Forms registration-provider Pub/Sub integration. -->
<!-- ABOUTME: Documents OAuth scope, Pub/Sub OIDC, renewal, sweep, mapping, and privacy boundaries. -->

# Google Forms Pub/Sub Integration

> **Audience:** Operators | Integrators
> **Status:** Implemented
> **Owner:** Platform/Ops
> **Last Verified:** 2026-08-11
> **Source Anchors:** `GoogleFormsRegistrationProviderDescriptor.cs`, `RegistrationProviderManagedPublishPreflightService.cs`, `RegistrationProviderSubscriptionLifecycleService.cs`, `RegistrationProviderConnection.cs`, `RegistrationProviderManagementHandlers.cs`, `GoogleFormsRegistrationProviderAdapterTests.cs`

Google Forms support is pinned to `GOOGLE_FORMS|GOOGLE_WORKSPACE|v1|ISLAMU_EVENT_GOOGLE_FORMS_PUBSUB_V1|2026-08-11`. It uses tenant-owned OAuth credentials, Google Forms API reads/provisioning, and OIDC-authenticated Pub/Sub push notifications that trigger response sweeps.

## Supported Contract

| Field | Required value |
|---|---|
| Provider code | `GOOGLE_FORMS` |
| Deployment | `GOOGLE_WORKSPACE` |
| API version | `v1` |
| Adapter policy | `ISLAMU_EVENT_GOOGLE_FORMS_PUBSUB_V1` |
| Evidence revision | `2026-08-11` |
| Correlation field | `system.registration_attempt_token` mapped to `entry.<digits>` |
| Pub/Sub callback effect | `registration.provider_response_sweep` |

Capabilities are server-derived from the descriptor and binding state: link/embed/manual import, schema read, managed form provisioning, response read, callback verification, subscription management, and reconciliation. Submission write/sink, auto-finalize, and Drive/file-upload handling are not advertised.

## Connection Fields

Create the tenant connection through the registration-provider management API or Studio HAL actions. The connection stores metadata only; OAuth credentials stay behind a tenant secret binding.

| Field | Required value |
|---|---|
| `ManagementApiBaseUrl` | `https://forms.googleapis.com/v1/` or another path on the pinned `https://forms.googleapis.com` origin. The adapter rejects any other management origin. |
| `PublicBaseUrl` | Must be `https://docs.google.com`. The adapter pins attendee launch URLs to that origin and rejects any other value before adding attempt correlation tokens. |
| `ProviderWorkspaceId` | Google Workspace/account reference chosen by the operator. |
| `ApiTokenSecretBindingId` | Tenant `registration_provider.api_token` binding containing either an access token or a JSON refresh-token envelope. |
| `WebhookSecretBindingId` | Leave unset/empty. Google Pub/Sub callbacks are authenticated with Google-signed OIDC tokens, not a shared webhook secret. |
| `GrantedOAuthScopes` | Exactly one of the two scope sets below. |
| `ProviderIdentity` | Non-empty provider account identity, bounded to 200 characters. |
| `PubSubConfigurationReference` | Non-empty Pub/Sub reference, bounded to 300 characters; shape below. |

`PubSubConfigurationReference` accepts JSON or semicolon text:

```json
{"topicName":"projects/{project}/topics/{topic}","audience":"https://{host}/api/integrations/registration/GOOGLE_FORMS/{bindingId}/callback","serviceAccountEmail":"google-pubsub-push@{project}.iam.gserviceaccount.com"}
```

Equivalent text form:

```text
topic=projects/{project}/topics/{topic};audience=https://{host}/api/integrations/registration/GOOGLE_FORMS/{bindingId}/callback;serviceAccountEmail=google-pubsub-push@{project}.iam.gserviceaccount.com
```

The JSON parser accepts `topicName` or `topic`, and `serviceAccountEmail` or `email`. The semicolon parser accepts `topic`, `audience`, and `serviceAccountEmail`. For text form, a bare value without `topic=` is treated as the topic only and leaves OIDC audience/email empty; that cannot pass callback verification.

## OAuth Scopes And Secret Binding

Minimal import/read scope set:

```text
openid email https://www.googleapis.com/auth/forms.body.readonly https://www.googleapis.com/auth/forms.responses.readonly
```

Managed provisioning adds exactly:

```text
https://www.googleapis.com/auth/forms.body
```

Do not request Google Drive scope. File upload fields are rejected before provisioning, and fetched responses containing `fileUploadAnswers` throw `PROVIDER_FILE_UPLOAD_UNSUPPORTED`.

The `registration_provider.api_token` secret may be a raw access token or JSON containing an `access_token`. If no access token is present, JSON must contain `refresh_token`, `client_id`, and `client_secret`; refresh success records `LastCredentialRefreshAt`. Successful Forms API calls record `LastAccessValidatedAt`.

## Pub/Sub Setup

1. Create a Google Pub/Sub topic such as `projects/{project}/topics/{topic}`.
2. Grant the Google Forms watch publisher permission to publish to that topic according to Google Workspace/Pub/Sub requirements.
3. Create a push subscription to the ISLAMU callback URL and enable OIDC authentication.
4. Set the push OIDC audience to the exact callback URL stored in `PubSubConfigurationReference.audience`.
5. Set `serviceAccountEmail` to the service account whose signed OIDC token will be attached to push requests.

The callback verifier accepts only Google OIDC tokens from `accounts.google.com` / `https://accounts.google.com`, validates the configured audience, requires `email_verified=true`, and requires the token email to match `serviceAccountEmail`. There is no shared callback secret for Google Pub/Sub; `registration_provider.webhook_secret` is not used by this adapter.

The Pub/Sub message is notify-only. It must identify the configured form, include a non-empty watch ID, and use event type `RESPONSES`; the callback queues a sweep effect instead of trusting response answers in the push body.

## Form Provisioning And Mapping

Managed publish preflight performs remote work when needed: it provisions a missing form, creates a missing Pub/Sub watch, persists the server-derived subscription state, then reads the remote schema. It does not renew existing watches; renewal belongs to the lifecycle worker. Managed provisioning calls Google Forms `forms.create`, `forms/{id}:batchUpdate`, then `forms/{id}:setPublishSettings` with `isPublished=true` and `isAcceptingResponses=true`. It reads the form back and fails closed unless both flags are true. Remote provisioning/subscription ambiguity is reported as a retryable `registration_provider_remote_acceptance_ambiguous` outcome.

Map exactly one active `system.registration_attempt_token` field to one `entry.<digits>` Google Forms question. Publish preflight rejects missing, malformed, or duplicate mappings. ISLAMU launches the form with `attemptId|attemptToken`; this value is a capability/correlation token only. It is not identity proof, is not a shared secret, and must not be used for account creation.

## Renewal, Recovery, And Health

Google Forms watches expire after about seven days. The lifecycle worker polls every 30 seconds, renews watches two days before expiry, and uses a two-minute processing lease with generation fencing.

Newly created subscription state is marked sweep-due immediately, so missed responses can be recovered before the first Pub/Sub push; the next normal sweep is scheduled six hours after a successful non-continuation sweep. The sweep asks Google Forms for responses with a timestamp filter from the previous checkpoint minus a ten-minute overlap, pages up to five pages of 100 responses, and stores an opaque `registration-provider-cursor:` continuation when more pages remain. Continuations run again immediately after the durable batch queue is persisted, so progress is never advanced before queued identifiers are safe. Renewal and sweep failures use independent persisted counters and exponential backoff capped at 60 minutes. Health surfaces stay bounded to validity, callback age, drift, reconciliation lag, queue depth, capability codes, generation, failure category, and timestamps; they do not expose answers, tokens, Pub/Sub payloads, continuation contents, or Google response bodies.

## Unsupported And Privacy Boundaries

- No Drive scope, Drive file metadata retention, or file-answer transfer is supported.
- No headless submission write/sink capability is claimed.
- No auto-finalize capability is claimed; downstream completion remains governed by the provider-submission effect pipeline and event/account policy. `AccountRequired` outcomes park rather than silently creating or linking accounts.
- Do not record raw OAuth tokens, OIDC tokens, Pub/Sub authorization headers, Forms answers, uploaded files, or Google response payloads in logs, metrics, docs, screenshots, or issue templates.

## Related

- [Integrations](../INTEGRATIONS.md)
- [Configuration](../CONFIGURATION.md#registration-provider-framework-configuration)
- [Secrets](../SECRETS.md)
- [Operations](../OPERATIONS.md#registration-provider-subscription-lifecycle)
