---
description: >-
  Durable in-app messaging, SMTP delivery, push hints, and optional subscriber
  synchronization.
---

# Communications & Notifications

ISLAMU Event keeps durable notification state separate from realtime hints, SMTP delivery, and external subscriber synchronization. Each channel has its own authority, readiness, and recovery path.

## In this section

* [In-App Notifications](in-app-notifications.md) — durable inbox actions, outbox fanout, SSE, and Web Push.
* [Email SMTP](email-smtp.md) — MailKit dispatch, secret bindings, readiness, retry, and safe recovery.
* [Listmonk](listmonk.md) — optional external registration-subscriber synchronization.

Monitor each channel independently. Never infer final delivery from an SSE/push hint, create email from every inbox row, or treat Listmonk as the authoritative inbox.
