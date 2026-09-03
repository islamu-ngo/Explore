---
description: Durable in-app messaging, SMTP delivery, push hints, and optional subscriber synchronization.
---

# Communications & Notifications

ISLAMU Event decouples authoritative in-app notification state from transient transport hints, SMTP email delivery, and external newsletter synchronization. Each communication channel maintains its own independent readiness probes, retry mechanisms, and recovery runbooks.

---

## In this Section

* **[In-App Notifications](in-app-notifications.md)** — Authoritative user notification inbox, SSE badge refresh hints, and Web Push notifications.
* **[Email SMTP](email-smtp.md)** — MailKit transactional dispatch outbox, SMTP configuration, transient retry loops, and Mailpit testing.
* **[Listmonk Integration](listmonk.md)** — Automated attendee newsletter synchronization into self-hosted Listmonk mailing lists.

---

## Channel Authority Boundaries

* The **In-App Inbox** is the primary source of notification truth; Server-Sent Events (SSE) and Web Push are merely lightweight hints directing the client to refresh its inbox.
* **SMTP Email** is reserved for explicit transactional intents (e.g. ticket delivery, password reset, payment receipts) and is not mirrored for every minor in-app event.
* **Listmonk** receives opt-in newsletter subscriber syncs; it never acts as a platform notification store.

---

## Related Guides & Next Steps

* **[Ticketing & Check-In](../events-and-ticketing/ticketing-and-check-in.md)** — Ticket delivery and lost-ticket capability emails.
* **[Environment Variables Reference](../configuration-and-operations/environment-variables.md#6-email-smtp--outbox)** — SMTP host, port, credentials, and TLS dials.
* **[Secrets Management](../configuration-and-operations/secrets.md)** — Securely store SMTP passwords and Listmonk API tokens.
* **[Webhooks & Callbacks](../integrations-and-ai/webhooks.md)** — Distribute operational events to external webhook subscribers.
