---
description: >-
  Configure durable MailKit SMTP dispatch, secret bindings, readiness, retry,
  and recovery.
---

# Email SMTP

Email is sent from durable `EmailDispatchOutbox` work using MailKit. SMTP is a delivery channel for explicit intents, not a mirror of every in-app notification.

## Configuration

Operators provide non-secret settings such as:

* SMTP host and port;
* TLS mode;
* sender identity;
* tenant-level routing where delegated;
* timeout and supported delivery settings.

Credentials are bound from Environment or Infisical according to deployment policy. They must not appear in governance manifests, committed files, logs, screenshots, health payloads, or support bundles.

Local development uses Mailpit so test mail can be inspected without contacting external recipients.

## Delivery behavior

The worker resolves tenant-aware settings, attempts delivery, and applies bounded retry/backoff for transient failures. Email readiness is reported separately from general application health so an operator can distinguish API availability from mail-provider availability.

Required heavy-moderation workflows track explicit channel state. Other fanout remains in-app only unless its intent includes email.

## Acceptance

1. Configure non-secret settings through the supported settings surface.
2. Bind credentials through the selected secret authority.
3. Confirm SMTP/email readiness.
4. Send a controlled test message.
5. Verify sender identity, TLS, delivery, and transient retry behavior.
6. Remove test recipient data from operational evidence.

## Recovery

Restore the server or credential binding, confirm readiness, inspect bounded failed-work metadata, and replay through the supported worker/retry path. Verify final provider and durable dispatch state. Do not copy message bodies or recipient addresses into tickets to prove the fix.
