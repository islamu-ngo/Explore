---
description: Integrate Google Workspace and Microsoft 365 Forms through their bounded supported contracts.
---

# Google & Microsoft Forms Integration

Organizers can map external survey tools (Google Forms and Microsoft Forms) directly into event attendee records. The platform ingests external form responses and projects them into native [Custom Properties](../events-and-ticketing/custom-properties.md) without compromising attendee identity truth.

---

## 1. Google Forms (Google Workspace)

The Google Forms integration is designed for Google Workspace organizations:
* **Authentication**: Uses tenant-owned Google OAuth 2.0 and Google Forms REST API.
* **Notification Flow**: Employs OIDC-authenticated Google Cloud Pub/Sub topics. Pub/Sub messages act as notify-only triggers: the platform queues an authenticated fetch back to the Google Forms API to read the authoritative responses.
* **Limitations**: File-upload questions and live submission writebacks are not supported.

---

## 2. Microsoft Forms (Microsoft 365)

Microsoft Forms integration targets Microsoft 365 organizational tenants using an organizer-owned Power Automate connector flow (`POWER_AUTOMATE_V1`):
* **Activation**: Requires a dedicated binding callback key and field mapping configuration.
* **Delivery**: Power Automate pushes response envelopes to the platform's incoming callback route.
* **Payload Verification**: Payloads are idempotently ingested and matched against registered attendee tickets (see [Ticketing & Check-In](../events-and-ticketing/ticketing-and-check-in.md)).

---

## 3. Data Reconciliation & Privacy Boundaries

External form correlation connects questionnaire answers to existing event registrations:
* It never manufactures new user accounts or bypasses [Keycloak Authentication](../security-and-identity/authentication.md).
* Answers are subject to the same privacy ceilings and GDPR purge rules as native questions (see [Privacy Erasure & GDPR Compliance](../security-and-identity/privacy-erasure.md)).

---

## Related Guides & Next Steps

* **[Custom Properties Governance](../events-and-ticketing/custom-properties.md)** — Learn how long-tail attendee data is governed and purged.
* **[Ticketing & Check-In](../events-and-ticketing/ticketing-and-check-in.md)** — Map questionnaire responses to admission tickets.
* **[Webhooks & Callbacks](webhooks.md)** — Understand incoming webhook signatures and idempotency.
* **[Privacy Erasure & GDPR](../security-and-identity/privacy-erasure.md)** — Scrubbing attendee responses upon account deletion.
