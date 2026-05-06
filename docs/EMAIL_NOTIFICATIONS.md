ABOUTME: Documents implemented SMTP email delivery and its boundary from in-app notifications.
ABOUTME: Prevents unsupported claims about notification fanout, queueing, unsubscribe, or delivery tracking.

# Email And Notifications

> **Audience:** Operators | Admins | Contributors
> **Status:** Mixed
> **Owner:** Platform/Ops
> **Last Verified:** 2026-05-06
> **Source Anchors:** `Explore.Infrastructure/Mail/`, `Explore.Domain/Settings/Definitions/EmailSettingDefinitions.cs`, `Explore.API/HealthChecks/SmtpHealthCheck.cs`, `docs/CONFIGURATION.md`, `docs/SECRETS.md`

Email delivery is implemented as direct SMTP sending. In-app notifications are a separate authenticated inbox feature and are not currently documented as an email fanout pipeline.

## What Is Implemented

| Area | Implemented Behavior |
|---|---|
| SMTP sending | `SmtpEmailService` sends one message through MailKit `SmtpClient` using resolved tenant-aware SMTP configuration. |
| SMTP testing | `SmtpEmailService.TestConnectionAsync` and instance settings endpoints can test SMTP connectivity. |
| Resilience | `EmailResiliencePipelines` retries transient SMTP failures with exponential backoff and jitter. |
| Admin settings | Instance admins can read, update, and test SMTP settings in the instance admin settings surface. |
| Health check | `SmtpHealthCheck` participates in readiness as the `smtp` health check. |
| In-app notifications | Notification controller/client paths handle inbox actions such as read, archive, snooze, and delete separately from SMTP. |

User-facing email notification preferences are not implemented as an active delivery feature. No inspected product workflow currently calls `IEmailService.SendAsync`; do not claim notification-to-email fanout until source code wires notifications or another workflow to `IEmailService`.

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
| User email preferences | Not implemented as active email delivery behavior in the inspected source. |
| Notification-to-email fanout | Not implemented in the inspected notification path. |

Keep the future notifications doc focused on in-app notification behavior when that doc is created, and link back here only for SMTP delivery boundaries.

## Troubleshooting

| Symptom | First Checks |
|---|---|
| SMTP health is degraded or unhealthy | Verify host, port, security mode, credentials, and network reachability. |
| Connection test fails | Run the instance SMTP test and check API logs from `SmtpEmailService.TestConnectionAsync`. |
| Settings update appears ignored | Wait for the resolver cache window or re-run the settings/test path that invalidates SMTP configuration for the current tenant. |
| Sends time out | Check `email.smtp_timeout_seconds`, firewall rules, and provider throttling. |
| TLS/certificate failures | Verify security mode and only use `email.smtp_skip_cert_validation` for controlled non-production scenarios. |
| Local development mail does not arrive | Confirm the seeded local SMTP defaults or your local mail sink configuration. |

Local development seeding can provide default SMTP-like values when SMTP host is empty. Treat those as development conveniences, not production defaults.

## Related Documentation

- [CONFIGURATION.md](CONFIGURATION.md) - configuration and settings boundaries.
- [SECRETS.md](SECRETS.md) - secret-provider and sensitive value handling.
- [ADMIN_GUIDE.md](ADMIN_GUIDE.md) - instance admin SMTP workflow.
- [OPERATIONS.md](OPERATIONS.md) - health/readiness context.
- [TROUBLESHOOTING.md](TROUBLESHOOTING.md) - symptom-first operator triage.
