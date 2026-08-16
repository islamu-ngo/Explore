<!-- ABOUTME: Atomic consultation framework for CTO-level ISLAMU Event product and architecture decisions. -->
<!-- ABOUTME: Provides decision lenses, option scoring, and repo-specific patterns for strategic advice. -->

# CTO Consultation Framework

## Consultation Contract

Start every consulting answer by identifying what kind of decision is being made:

1. Product direction: audience, positioning, roadmap, packaging, differentiation.
2. Architecture direction: layer ownership, contracts, data model, operational lifecycle.
3. Infrastructure posture: required dependency, optional profile, managed-provider path, or future tier.
4. UX/workflow design: who acts, what they see, what authorization source gates the action.
5. Compliance and convention: privacy, retention, audit, accessibility, localization, security, self-hosting documentation.

Then state whether the answer is based only on repository sources or also on current external research. Use external research for current market, competitor, vendor, legal, regulatory, pricing, or library-status claims, and cite dates/sources in the final recommendation.

## Product Thesis To Preserve

ISLAMU Event is not only a public event listing site. It is an open-source event discovery and management platform that must work for:

- the ISLAMU-hosted public instance;
- single-tenant self-hosters such as one mosque, nonprofit, conference team, campus, or local community;
- multi-tenant operators hosting many organizations;
- enterprise and managed-provider deployments that require governance, audit, policy, and integration discipline;
- white-label deployments that need tenant and organization customization without forking the codebase.

Consulting advice should therefore avoid forcing every deployment into one SaaS posture. Prefer progressive capability: small deployments should stay simple, while advanced operators can enable stronger policy, analytics, storage, authorization, messaging, and observability choices.

## Repository Decision Lenses

Use these lenses before recommending a feature shape.

| Lens | Ask |
|---|---|
| Authority | Is the source of truth Instance, Tenant, Organization, Group, User, API, BFF, Client, PostgreSQL, external provider, or derived projection? |
| Tier | Is this Tier 1 Humble, Tier 2 Community, or Tier 3 Ummah-Scale behavior? |
| Mode | Does it differ between SingleTenant and MultiTenant? |
| Layer | Is the data Layer 1 core, Layer 2 typed sector schema, or Layer 3 governed extension? |
| Affordance | Is the UI action exposed by HAL `_links`, a BFF status endpoint, or a non-authoritative visual hint? |
| Operator burden | Does it add a required service, secret, migration, backup path, health check, retention policy, or recovery runbook? |
| Tenant isolation | Does it preserve EF tenant filters, bounded bypass reasons, and future RLS compatibility? |
| Durability | Is this durable business state, rebuildable projection, compliance evidence, or ephemeral cache? |
| Interoperability | Does it affect OpenAPI, generated client, federation lexicons, webhooks, import/export, or external API keys? |
| Governance | Can Instance admins lock/delegate it, and can Tenants/Organizations customize it safely? |

## Recommendation Format

A complete consultation should include:

1. Decision: one sentence naming the actual choice.
2. Current facts: repo-grounded facts from docs/code, not generic architecture theory.
3. Options: usually 2-4 realistic options, including "defer" when valid.
4. Recommendation: a clear default, not an endless list.
5. Rationale: tradeoffs across product, architecture, security, operations, and UX.
6. Implementation path: phased slices that can be tested and documented.
7. Risks and reversibility: what can fail, what is hard to undo, and what must be measured.
8. Verification: tests, architecture checks, health checks, docs, or external evidence needed.

## Option Scoring

Score options qualitatively as Low, Medium, or High across:

- user value;
- implementation complexity;
- operational complexity;
- tenant/security risk;
- self-hosting friendliness;
- enterprise readiness;
- reversibility;
- market differentiation.

Avoid recommending a high-complexity/high-operator-burden path unless it unlocks clear enterprise value or removes a real platform risk.

## Infrastructure Optionality Pattern

When deciding whether infrastructure should be required, optional, or hidden:

1. Required only if the core product cannot work safely without it.
2. Optional profile if it improves scale, policy, delivery guarantees, or enterprise controls but small deployments can run without it.
3. Internal implementation detail if users should not manage it directly.
4. Product admin surface if operators need stable business state, replay, audit, configuration, or remediation.
5. Infrastructure dashboard only for instance operators and never as the product source of truth.

Example: Quartz.NET is the default scheduler trigger for Basic Dispatch Mode, but its status endpoint should remain disabled by default, instance-admin-only, and secondary to EmailDispatchOutbox state, health checks, metrics, and HAL-gated product admin APIs.

## Organizer Analytics Pattern

For an event-organizer analytics dashboard, separate:

- operational analytics: registrations, capacity, check-in, waitlist, contact-share consent, email delivery status;
- discovery analytics: views, referrers, search/filter impressions, conversion rate;
- tenant governance: provider, consent mode, relay/proxy/direct transport, retention, tenant lock/delegation;
- privacy/compliance: pseudonymous defaults, consent-driven identified analytics, no raw PII in metrics tags;
- UX authorization: dashboard availability from API/HAL or server-confirmed status, not local role checks.

A prudent path is usually:

1. Phase 1: server business metrics and event/registration aggregate read models from existing tables.
2. Phase 2: organizer dashboard with HAL-gated access and low-cardinality metrics.
3. Phase 3: optional analytics provider integration using existing `analytics.*` governance settings.
4. Phase 4: benchmarking, retention, export, and enterprise reporting only after usage proves demand.

## Market And Competition Analysis

For market positioning, compare against categories rather than only named products:

- discovery-first event platforms;
- ticketing and registration platforms;
- community/association management tools;
- conference/session management tools;
- enterprise event management suites;
- open-source/self-hosted event tools;
- calendaring/federated event discovery ecosystems.

Evaluate competitors by:

- self-hosting and licensing posture;
- white-label and tenant governance;
- API and integration quality;
- organizer operations depth;
- discovery/SEO strength;
- compliance and audit features;
- extensibility model;
- total operational burden;
- accessibility/localization maturity.

Because the market changes, verify current competitor capabilities, pricing, licensing, and acquisition/vendor status before making external claims.

## Legal And Convention Framing

Do not present legal advice. Present engineering controls and professional conventions:

- data minimization, consent, retention, export, withdrawal, audit evidence;
- administrator role separation and least privilege;
- security headers, CSRF protection, token handling, secret ownership, and redaction;
- accessibility baseline such as WCAG AA-aligned implementation and testing;
- localization and RTL support as product quality, not afterthought;
- operator-visible backup, restore, upgrade, and rollback paths;
- AGPL/open-source implications as prompts for counsel review when commercial hosting, plugins, or private modifications are discussed.

When legal/regulatory conclusions depend on jurisdiction or current law, recommend consulting qualified counsel and provide the technical decision record counsel needs to review.

## Implementation Advice Boundaries

Consultation may propose implementation slices, but it should not silently bypass the Contribution Contract. If the user asks to proceed with implementation:

1. classify the implementation intent in `.claude/contract/intents.yaml`;
2. load the relevant docs, rules, and implementation skills;
3. keep changes within scoped paths;
4. update docs where the intent requires it;
5. run the minimum tests and architecture checks.

Strategic advice is useful only if it can become a coherent sequence of governed repository changes.
