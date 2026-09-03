---
description: Govern long-tail event and session data with explicit exposure and use grants.
---

# Custom Properties Governance

Custom properties allow organizers and tenant administrators to capture legitimate long-tail fields on events, sessions, and attendee registrations. They provide flexible data collection without polluting the core relational schema or weakening domain invariants (see [Modular Event Aspects](modular-event-aspects.md)).

---

## Governance & Exposure Ceilings

Each property definition enforces strict access and privacy controls:

* **Value Type**: Text, Number, Boolean, Single-Select, Multi-Select, or File Attachment.
* **Exposure Ceilings**:
  * `Public`: Visible on public event listings and marketing pages.
  * `Private`: Visible strictly to authenticated organizers and event staff.
  * `System`: Restricted to platform background processes and administrative tooling.
* **Purpose Grants**: Explicit permission flags determine whether an answer may be used in search indexes, CSV attendee exports, or moderation reviews.

> [!IMPORTANT]
> The configured `ExposureLevel` is a strict ceiling. Client UI code and analytics jobs can never widen access beyond what the server authorizes.

---

## Lifecycle: Retirement vs. Hard Purge

1. **Normal Deletion (Retirement)**: Soft-deletes the field definition. Existing event registrations preserve their historical answers for auditability and financial reporting, but no new events can select the retired question.
2. **Hard Purge**: An audited, administrator-confirmed operation that permanently deletes field definitions and scrubs all collected answers from database storage (see [Privacy Erasure & GDPR Compliance](../security-and-identity/privacy-erasure.md)).
3. **Template Immutability**: Editing a registration template never retroactively alters published events or past tickets.

---

## Related Guides & Next Steps

* **[Modular Event Aspects](modular-event-aspects.md)** — When to use typed relational aspects vs. custom properties.
* **[Ticketing & Check-In](ticketing-and-check-in.md)** — How custom registration questions integrate into ticket checkout.
* **[Google & Microsoft Forms](../integrations-and-ai/google-and-microsoft-forms.md)** — Map external survey responses into native custom properties.
* **[Privacy Erasure & GDPR](../security-and-identity/privacy-erasure.md)** — Learn how custom property responses are purged upon user account deletion.
