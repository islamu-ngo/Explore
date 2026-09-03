---
description: Use typed Islamic and technology event data without weakening the core model.
---

# Modular Event Aspects

ISLAMU Event separates universally shared event/session fields from optional typed sector aspects. This preserves a clean, lean core domain model while allowing rich, first-class relational extensions.

---

## Three-Tier Event Model Layers

1. **Core Domain Fields**: Universally shared concepts: title, description, schedule, venue location, organizer ownership, capacity, and publication lifecycle state.
2. **Typed Sector Aspects**: Relational models for sector-specific event and session data (e.g. Islamic event details, prayer accommodations, speaker credentials, technology workshop requirements).
3. **[Governed Custom Properties](custom-properties.md)**: Controlled long-tail fields and attendee registration questionnaires that do not belong in the core relational schema.

> [!NOTE]
> Typed aspects are structured database entities with strongly typed foreign keys and indices, not unstructured, opaque JSON property bags.

---

## Feature Module Gating

Sector capabilities are governed by tenant feature flags (e.g. `Mod_Islamic` or `Mod_Tech`):
* When a module is enabled, its sector-specific fields, filters, and UI editors become active.
* If a module is disabled for a tenant, its filters and validation rules are cleanly bypassed rather than partially applied.
* Public API responses omit disabled module attributes, keeping responses compact.

---

## Architectural Design Boundary

* Use a **Typed Aspect** when the concept possesses universal community semantics, dedicated validation, query indices, or lifecycle hooks (e.g. prayer times, halal catering, tech tracks).
* Use a **[Governed Custom Property](custom-properties.md)** for organizer-specific, one-off questions (e.g. "T-shirt size", "Dietary allergies", "Emergency contact").
* Neither mechanism may ever be used to bypass [Authorization](../security-and-identity/authorization.md), [Payment Truth](paid-events-and-payouts.md), or [Admission Issuance](ticketing-and-check-in.md).

---

## Related Guides & Next Steps

* **[Custom Properties Governance](custom-properties.md)** — Design custom attendee registration forms.
* **[Ticketing & Check-In](ticketing-and-check-in.md)** — Manage capacity, admission tickets, and QR validation.
* **[Paid Events & Payouts](paid-events-and-payouts.md)** — Connect Stripe accounts and manage paid tickets.
* **[Administration Guide](../administration-and-branding/admin-guide.md)** — Enable and disable sector modules in the admin console.
