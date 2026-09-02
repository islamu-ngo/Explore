---
description: Govern long-tail event and session data with explicit exposure and use grants.
---

# Custom Properties

Custom properties support legitimate long-tail fields on events and sessions. They are not a user-defined entity system, rules engine, or replacement for typed policy concepts.

## Definition controls

Each definition governs:

* value type and allowed input;
* exposure ceiling;
* search, filter, export, moderation, and analytics grants;
* system-owned status;
* template relationship;
* retirement and purge behavior.

`ExposureLevel` is a hard ceiling. Purpose flags grant narrower use inside that ceiling. Clients, queries, exports, and analytics must not broaden either rule locally.

## Authority and projection

Raw custom-property values remain authoritative. Projection rows are rebuildable read optimizations. Core fields and typed module aspects remain policy truth when a concept belongs there.

Template updates do not silently rewrite existing events or sessions. This protects historical meaning and prevents an administrator from retroactively changing participant-facing records.

## Removal

Normal deletion retires a definition and preserves history. Hard purge is an explicit, audited, reason-bearing operation and is blocked while dependencies remain.

Current calendar export includes core fields only. Do not promise that custom properties automatically appear in calendars, feeds, moderation tools, or every export.

## Operator checklist

Before enabling a property, document its purpose, type, exposure ceiling, permitted uses, retention, migration behavior, and owner. Test public, authenticated, organizer, export, and analytics views independently. If the value becomes widely shared or policy-bearing, migrate it to a core or typed aspect instead of adding more flags.
