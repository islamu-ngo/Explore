---
description: Use typed Islamic and technology event data without weakening the core model.
---

# Modular Event Aspects

ISLAMU Event separates universally shared event/session fields from optional typed sector aspects. This preserves a stable core model while allowing explicit module-specific behavior.

## Model layers

1. **Core fields:** title, schedule, venue, lifecycle, organizer, and other shared concepts.
2. **Typed aspects:** relational models for sector-specific event and session data.
3. **Governed custom properties:** controlled long-tail fields that do not belong in the core or a typed module.

Current typed areas include Islamic-event and technology-event data, including session-level Islamic details. They are first-class relational contracts rather than opaque JSON property bags.

## Module gating

Filters and behavior are active only when the corresponding module, such as `Mod_Islamic` or `Mod_Tech`, is enabled. A disabled module's filters are ignored instead of being partially applied.

Technology-event data exists in the model, but its event-creation strategy is not active. Adopters must verify the deployed capability rather than infer it from schema presence.

## Design boundary

Use a typed aspect when the concept has shared meaning, validation, query semantics, policy, or lifecycle behavior. Use a governed custom property only for legitimate tenant/event-specific long-tail data. Neither mechanism may replace authorization, payment truth, admission, moderation, ranking, or lifecycle state.

## Acceptance

Test creation and reads with the module enabled and disabled, verify module-specific filters do not affect disabled deployments, and confirm outward contracts omit or constrain unavailable behavior. Document the exact modules enabled for each tenant.
