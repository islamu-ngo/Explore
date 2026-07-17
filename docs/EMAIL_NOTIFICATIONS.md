ABOUTME: Documents implemented SMTP email delivery and its boundary from in-app notifications.
ABOUTME: Prevents unsupported claims about notification fanout, queueing, unsubscribe, or delivery tracking.

# Email And Notifications

> **Audience:** Operators | Admins | Contributors
> **Status:** Mixed
> **Owner:** Platform/Ops
> **Last Verified:** 2026-07-04
> **Source Anchors:** `Explore.Infrastructure/Mail/`, `Explore.Infrastructure/EmailDispatchDrainService.cs`, `Explore.Infrastructure.Tests/Infrastructure/EmailDispatchDrainMailpitTests.cs`, `Explore.Infrastructure.Tests/Infrastructure/SmtpEmailServiceMailpitTests.cs`, `Explore.Persistence/Seed/DatabaseSeeder.cs`, `Explore.AppHost/AppHost.cs`, `docker-compose.yml`, `Explore.Domain/Settings/Definitions/EmailSettingDefinitions.cs`, `Explore.API/HealthChecks/SmtpHealthCheck.cs`, `Explore.Application/Services/EventPublishedNotificationFanoutService.cs`, `Explore.Application/Services/EventModerationNotificationFanoutService.cs`, `docs/CONFIGURATION.md`, `docs/SECRETS.md`

Email delivery is implemented as direct SMTP sending. In-app notifications are a separate authenticated inbox feature; actor-subscription fanout and moderation attendee fanout create durable in-app notification rows and are not SMTP/email fanout pipelines.

## Approved Lifecycle Expansion (Planned, Not Implemented)

The approved workstream in `dev/active/email-responsibility-architecture/` retains MailKit, Mailpit, and PostgreSQL `EmailDispatchOutbox`. It adds explicit `NotificationIntent` and per-channel `NotificationDelivery` relationships, atomic recipient materialization, immutable event/session fanout occurrences, reporter consent withdrawal, heavy-moderation availability mail, and safe reminders. Existing behavior below remains the runtime truth until each task ships.

Channel policy is fixed: registration and critical event changes use required in-app plus optional verified/preference-gated email; reporter receipt/outcome and follow-up use separate consent purposes; heavy moderation uses required in-app plus operational email when a current verified address exists; light moderation email stays deferred; reminders remain optional; managed tenant-administrator invitations retain their authorization-bound destination. Dispatch may narrow the snapshotted policy but never broaden it.

The provider-handoff transition is the suppression fence. Consent, preference, cancellation, and supersession can stop work before it; after it, SMTP/persistence uncertainty is `Unknown`, not an automatic retry or a claim that an in-flight message was recalled. Sent/skipped content redacts after 180 days, unresolved replay material waits for operator resolution, and redacted work is never replayable.

## What Is Implemented

| Area | Implemented Behavior |
|---|---|
| SMTP sending | `SmtpEmailService` sends one message through MailKit `SmtpClient` using resolved tenant-aware SMTP configuration. |
| SMTP testing | `SmtpEmailService.TestConnectionAsync` and instance settings endpoints can test SMTP connectivity. |
| Resilience | `EmailResiliencePipelines` retries transient SMTP failures with exponential backoff and jitter. |
| Admin settings | Instance admins can read, update, and test SMTP settings in the instance admin settings surface. |
| Health check | `SmtpHealthCheck` participates in readiness as the `smtp` health check. |
| In-app notifications | Notification controller/client paths handle inbox actions such as read, archive, snooze, and delete separately from SMTP. Actor-subscription fanout and moderation attendee fanout create in-app rows only. |
| Notification preference matrix | Current-user, organization, and group preference matrices gate non-required in-app fanout and direct email dispatch before provider handoff. |

User-facing notification preferences are implemented for the direct dispatch and in-app fanout paths described here. They do not create general notification-to-email fanout: actor-subscription and moderation fanout still write durable in-app notifications only, and direct SMTP delivery still comes from explicit `EmailDispatchOutbox` workflows.

## SMTP Settings

The email settings model is defined by `EmailSettingDefinitions` and grouped by `EmailSettingGroup`.

| Setting | Purpose |
|---|---|
| `email.smtp_host` | SMTP host. Required with from-address for resolved config. |
| `email.smtp_port` | SMTP port. |
| `email.smtp_security` | SMTP security mode mapped by the SMTP service. |
| `email.from_address` | Default sender address. Required with host for resolved config. |
| `email.from_name` | Default sender display name. |
| `email.smtp_timeout_seconds` | SMTP connection/send timeout. |
| `email.smtp_skip_cert_validation` | Certificate validation bypass for controlled environments only. |
| `email.smtp_username` | Sensitive SMTP username. |
| `email.smtp_password` | Sensitive SMTP password. |

`SmtpConfigResolver` reads the cascading settings model and caches resolved configuration per tenant for five minutes. It returns no SMTP configuration when required values such as host or from-address are missing.

Local Aspire and Compose runs start Mailpit for email capture. Aspire exposes SMTP on `localhost:1025` and the UI at `http://localhost:8025`; Compose containers use SMTP host `mailpit` and port `1025`. Development database seeding uses `MAIL_SMTP_*`, then `SMTP_*` aliases, then these Mailpit defaults when no SMTP host has been configured.

## Secret Handling

`email.smtp_username` and `email.smtp_password` are sensitive settings. Treat them as secrets in reviews, logs, exports, support bundles, and screenshots.

- Use [SECRETS.md](SECRETS.md) for provider setup and redaction expectations.
- Use [CONFIGURATION.md](CONFIGURATION.md) for runtime configuration and persisted settings boundaries.
- Do not document plaintext SMTP credentials in sample files or issue reports.
- If SMTP settings are restored after a backup, validate the connection before reopening workflows that depend on email.

The docs should not claim more about email-secret encryption than the source proves. The implementation marks the credentials sensitive and routes them through the settings/secrets model.

## Sending Boundary

`SmtpEmailService` supports the message shape represented by the infrastructure email models:

- `To`, `Cc`, and `Bcc` recipients.
- `ReplyTo`.
- Custom headers.
- HTML and plain-text body content.
- Attachments and inline images.

The service creates a new SMTP client per send. It does not document a queue, background dispatcher, delivery tracking, bounce processor, or unsubscribe workflow in the SMTP send path.

## Basic Dispatch Test Evidence

Basic Dispatch uses `EmailDispatchDrainService` as the scheduler-neutral boundary. TickerQ, hosted-service fallback, and future transports must delegate to this drain instead of sending SMTP directly from handlers or controllers.

| Behavior | Evidence |
|---|---|
| Direct SMTP provider handoff | `SmtpEmailServiceMailpitTests` sends through MailKit to Mailpit and verifies recipient, sender, subject, text body, HTML body, connection success, and result-field redaction for sentinel body/secret values. |
| SMTP settings resolution | `SmtpConfigResolverTests` verifies tenant `SettingContext` propagation, per-tenant cache separation, missing required settings, defaults, and cache invalidation. |
| Pending outbox drain | `EmailDispatchDrainMailpitTests` starts with a pending `EmailDispatchOutbox`, runs `ProcessBatchAsync`, sends through real SMTP to Mailpit, and records `Sent` outbox state plus succeeded attempt and completed receipt state. |
| Duplicate claim protection | `EmailDispatchDrainMailpitTests` races two `ProcessSingleAsync` consumers for one outbox row and verifies exactly one Mailpit message, one attempt, and one completed receipt. |
| Failure outcomes | `EmailDispatchDrainServiceTests` covers retry-scheduled SMTP failures, exhausted dead-letter outcomes, timeout-like unknown outcomes, legacy recipient preference skips, matrix preference skips, tenant pause before SMTP handoff, and stale-processing recovery. |
| Tenant pause/resume and operator actions | `EmailDispatchTenantControlRepositoryTests`, `EmailDispatchAdminControllerTests`, and `EmailDispatchAdminHateoasTests` cover PostgreSQL pause/resume state, API problem mapping, write-route policies, and HAL replay/park affordance rules. |
| Scheduler triggers | `EmailDispatchTickerQJobsTests` and `EmailDispatchProcessorTests` prove TickerQ and hosted-service fallback paths call the same scheduler-neutral drain service instead of owning SMTP, RabbitMQ, or payload logic. |
| Readiness states | `EmailDispatchHealthCheckTests` covers Basic Dispatch enabled, intentionally disabled, `Mode=Disabled`, TickerQ scheduler disabled, and HostedService states. `EmailDispatchRabbitMqHealthCheckTests` covers RabbitMQ disabled, healthy-enabled, and unhealthy transport states independently from Basic Dispatch. |
| RabbitMQ runtime fixture | `RabbitMqContainerFixtureTests` starts a Testcontainers RabbitMQ management image, verifies an AMQP connection string, and reads bounded management overview diagnostics for later live transport tests. |
| RabbitMQ live topology and publish outcomes | `RabbitMqEmailDispatchTransportLiveTests` enables the real transport against the fixture, declares dispatch/DLX/parking topology, verifies durable direct exchanges and queues through the management API, verifies dispatch queue DLX arguments, confirms readiness is healthy, confirms routable pointer publishes, returns `mandatory_return` for an unbound routing key, and reads the live broker payload to prove only pointer fields are serialized. |
| RabbitMQ live consumer | `RabbitMqEmailDispatchConsumerMailpitTests` starts the manual-ack consumer, publishes a valid pointer, drains the durable outbox through real SMTP to Mailpit, persists `Sent` attempt/receipt state, and waits for RabbitMQ ready/unacknowledged counters to reach zero after the durable outcome. The same class proves malformed JSON and valid pointers without a durable outbox reject to the DLQ without sending Mailpit email. |
| RabbitMQ DLQ replay and parking | `RabbitMqEmailDispatchDeadLetterReplayLiveTests` starts the replay worker, resets a dead-lettered durable row before republishing to the dispatch queue, ACKs the original DLQ delivery, and parks missing-outbox payloads to the parking queue. |
| Browser registration email | `RegistrationFlowTests` clears Mailpit, registers through the Aspire-backed API/BFF/browser stack, waits for the durable outbox row to become `Sent`, verifies attempt/receipt rows, finds the Mailpit message for the registrant, and checks semantic body text plus event title. |

Focused commands:

These commands are focused development lanes, not release evidence by themselves. Lifecycle-email release evidence records each named test class and its exact non-zero count, runs the full affected projects, and runs the explicit `Email` Mailpit lane; a broad OR filter plus `--minimum-expected-tests 1` is not accepted as proof of a new behavior.

```bash
dotnet test --project Explore.Infrastructure.Tests/Explore.Infrastructure.Tests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/*/*[Category=Email]" --minimum-expected-tests 1
dotnet test --project Explore.Infrastructure.Tests/Explore.Infrastructure.Tests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/*/*[Category=RabbitMQ]" --minimum-expected-tests 1
dotnet test --project Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/EmailDispatchTenantControlRepositoryTests/*" --minimum-expected-tests 1
dotnet test --project Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/EmailDispatchAdminControllerTests/*|/*/*/EmailDispatchAdminHateoasTests/*" --minimum-expected-tests 1
dotnet test --project Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/EmailDispatchTickerQJobsTests/*|/*/*/EmailDispatchProcessorTests/*" --minimum-expected-tests 1
dotnet test --project Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/EmailDispatchHealthCheckTests/*" --minimum-expected-tests 1
dotnet test --project Explore.Infrastructure.Tests/Explore.Infrastructure.Tests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/EmailDispatchRabbitMqHealthCheckTests/*" --minimum-expected-tests 1
dotnet test --project Explore.Infrastructure.Tests/Explore.Infrastructure.Tests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/RabbitMqContainerFixtureTests/*" --minimum-expected-tests 1
dotnet test --project Explore.Infrastructure.Tests/Explore.Infrastructure.Tests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/RabbitMqEmailDispatchTransportLiveTests/*" --minimum-expected-tests 1
dotnet test --project Explore.Infrastructure.Tests/Explore.Infrastructure.Tests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/RabbitMqEmailDispatchConsumerMailpitTests/*" --minimum-expected-tests 1
dotnet test --project Explore.Infrastructure.Tests/Explore.Infrastructure.Tests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/RabbitMqEmailDispatchDeadLetterReplayLiveTests/*" --minimum-expected-tests 1
```

## Admin Workflow

Instance administrators manage SMTP from the instance settings surface:

1. Open the instance admin settings page.
2. Review SMTP host, port, security, sender, timeout, and credential fields.
3. Save the settings.
4. Run the SMTP connection test.
5. Check readiness health if the connection test or mail send fails.

The admin UI source is `Explore.Blazor.Client/Pages/Admin/Instance/Components/InstanceSmtpSection.razor`, and the API source is `Explore.API/Controllers/InstanceSettingsController.cs`.

## In-App Notifications Boundary

In-app notifications are implemented through notification controller/client/repository paths, not through SMTP delivery.

| Capability | Boundary |
|---|---|
| Read/archive/snooze/delete notification inbox items | In-app notification feature. |
| SMTP send | `IEmailService` / `SmtpEmailService`. |
| User, organization, and group notification preferences | Implemented as matrix category/channel choices resolved before non-required in-app fanout rows and before direct SMTP provider handoff. |
| Actor-subscription fanout | Implemented only as durable in-app `Notification` row creation through the outbox fanout path, gated by the in-app preference channel before row creation. |
| Event moderation attendee fanout | Implemented only as durable in-app `Notification` row creation through the outbox fanout path. Heavy moderation uses generic, linkless in-app copy and still does not send email. Trust-safety requiredness is resolved through the preference matrix. |
| Notification-to-email fanout | Not implemented in the inspected notification path; no SMTP call is made by actor-subscription or moderation fanout. |

`EmailDispatchDrainService` preserves tenant pause, processing claims, receipt claims, legacy unsubscribe checks, retry/dead-letter handling, and operator park/replay behavior. When the matrix disables a non-required email category, the drain marks the durable row and receipt as `Skipped` with failure category `recipient_notification_preference_disabled` before SMTP handoff.

Keep the future notifications doc focused on in-app notification behavior when that doc is created, and link back here only for SMTP delivery boundaries.

## Troubleshooting

| Symptom | First Checks |
|---|---|
| SMTP health is degraded or unhealthy | Verify host, port, security mode, credentials, and network reachability. |
| Connection test fails | Run the instance SMTP test and check API logs from `SmtpEmailService.TestConnectionAsync`. |
| Settings update appears ignored | Wait for the resolver cache window or re-run the settings/test path that invalidates SMTP configuration for the current tenant. |
| Sends time out | Check `email.smtp_timeout_seconds`, firewall rules, and provider throttling. |
| TLS/certificate failures | Verify security mode and only use `email.smtp_skip_cert_validation` for controlled non-production scenarios. |
| Local development mail does not arrive | Open Mailpit at `http://localhost:8025` and confirm `email.smtp_host`/`email.smtp_port` resolve to `localhost:1025` for Aspire or `mailpit:1025` for Compose. |
| RabbitMQ dispatch health is unhealthy | Confirm `EmailDispatchRabbitMq:Enabled` is intentional, then check broker connectivity, topology names, parking queue settings, and bounded transport logs. Basic Dispatch can stay healthy without RabbitMQ when broker dispatch is disabled. |
| RabbitMQ DLQ grows | Inspect the HAL-gated EmailDispatch admin status before replay. Malformed or missing durable pointers should park or remain in DLQ evidence; replayable rows must reset durable state before republish. |

Local development seeding can provide Mailpit values when SMTP host is empty or still set to the retired `mailpit.openislamu.org` seed. Treat those as development conveniences, not production defaults.

## Related Documentation

- [CONFIGURATION.md](CONFIGURATION.md) - configuration and settings boundaries.
- [SECRETS.md](SECRETS.md) - secret-provider and sensitive value handling.
- [ADMIN_GUIDE.md](ADMIN_GUIDE.md) - instance admin SMTP workflow.
- [OPERATIONS.md](OPERATIONS.md) - health/readiness context.
- [TROUBLESHOOTING.md](TROUBLESHOOTING.md) - symptom-first operator triage.
