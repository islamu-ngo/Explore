---
description: Configure durable MailKit SMTP dispatch, secret bindings, readiness, retry, and recovery.
---

# Email SMTP Delivery

Transactional email in ISLAMU Event is processed through a durable `EmailDispatchOutbox` worker powered by MailKit. SMTP is reserved for explicit transactional intents (such as ticket delivery, account verification, and payment receipts), rather than duplicating every minor [In-App Notification](in-app-notifications.md).

---

## 1. SMTP Configuration

Operators supply non-secret and secret parameters via [Environment Variables](../configuration-and-operations/environment-variables.md#6-email-smtp--outbox):

* **Server Settings**: `EMAIL_SMTP_HOST`, `EMAIL_SMTP_PORT`, `EMAIL_SMTP_SECURITY` (`None`, `StartTls`, or `SslOnConnect`).
* **Sender Identity**: `EMAIL_FROM_ADDRESS`, `EMAIL_FROM_NAME`.
* **Credentials**: `SMTP_USERNAME`, `SMTP_PASSWORD` (managed securely via [Secrets Management](../configuration-and-operations/secrets.md)).
* **Testing Capture**: Local development and Docker Compose evaluation default to [Mailpit](../getting-started/5-minute-quickstart.md#accessing-endpoints) on port `1025` (webmail on `:8025`).

---

## 2. Delivery Behavior & Retry Guarantees

* **Tenant Routing**: Outbox workers evaluate tenant-specific branding and sender headers before dispatch.
* **Bounded Retries**: Transient failures (e.g. SMTP connection timeout, rate-limiting) trigger exponential backoff retries with jitter.
* **Readiness Probes**: The email worker reports its status independently to `/health/email`, allowing operators to distinguish general API health from third-party mail relay outages.

---

## 3. Production Verification Checklist

1. Configure SMTP host and port in `.env`.
2. Securely bind `SMTP_USERNAME` and `SMTP_PASSWORD` via Environment or Infisical.
3. Confirm health status via `/health`.
4. Register a test user or issue a free ticket (see [Ticketing & Check-In](../events-and-ticketing/ticketing-and-check-in.md)) to trigger a confirmation message.
5. Verify TLS handshake, sender headers, and inbox receipt.

---

## Related Guides & Next Steps

* **[Environment Variables Reference](../configuration-and-operations/environment-variables.md#6-email-smtp--outbox)** — Complete list of all email configuration keys.
* **[In-App Notifications](in-app-notifications.md)** — Understand the relationship between in-app and email channels.
* **[Listmonk Integration](listmonk.md)** — Synchronize community newsletters to self-hosted Listmonk.
* **[Ticketing & Check-In](../events-and-ticketing/ticketing-and-check-in.md)** — Manage email-based lost ticket capability links.
