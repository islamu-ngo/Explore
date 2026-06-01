<!-- ABOUTME: Core I-VSD principles and the six domains where provider responsibility is reviewed. -->
<!-- ABOUTME: Provides traceability tables from Islamic principles to software decisions, violations, and evidence. -->

# Principles And Domains

## Principle Table

| Principle | Software meaning | Provider decisions | Typical violations | Evidence |
| --- | --- | --- | --- | --- |
| Trust / Amanah | Stewardship of data, infrastructure, access, community, promises, and admin power. | Hosting, backups, support, privileged access, uptime, governance. | Hidden admin access, weak recovery, broken portability promise. | Architecture docs, access logs, backup tests, support policy. |
| Truthfulness / Sidq | Accurate claims and clear limitations. | Marketing, pricing copy, privacy statements, AI labels, roadmap. | AI washing, false privacy/security claims, roadmap as current. | Claim-evidence register, product copy, release notes. |
| Justice / Adl | Fair treatment, burden distribution, appeals, access, and enforcement. | Moderation, ranking, pricing, support, accessibility. | Arbitrary enforcement, inaccessible flows, biased ranking. | Policy docs, appeal logs, accessibility reports, moderation metrics. |
| Non-Harm / La Darar | Avoid foreseeable privacy, security, financial, spiritual, social, and operational harms. | Data collection, AI outputs, security controls, incident response. | Intrusive tracking, destructive automation, negligent patching. | Threat models, incident records, audit findings. |
| Rights of People | Respect consent, property, privacy, reputation, dignity, export, deletion, and contestability. | Data rights, account controls, content ownership, dispute flows. | No export/delete, reputational harm without appeal, confusing consent. | Data flows, terms, UI flows, appeals, deletion logs. |
| Avoiding Riba | Avoid interest/usury exposure where applicable. | Credit, late fees, subscriptions, investments, sponsorships. | Interest-bearing financing or unclear late-fee model. | Contracts, payment terms, scholar/legal notes. |
| Avoiding Gharar | Reduce excessive uncertainty. | Pricing, renewals, terms, capabilities, dependencies. | Hidden fees, vague limits, unclear renewal or data use. | Pricing page, terms, onboarding copy, dependency list. |
| Avoiding Deception | Avoid misleading defaults, hidden motives, fake trust, and manipulative comparisons. | UX, marketing, comparison tables, endorsements. | Fake reviews, preselected paid options, checkmark flattening. | UI screenshots, marketing copy, review provenance. |
| Promise-Keeping | Keep privacy, support, portability, licensing, uptime, pricing, and lifecycle commitments. | EOL, migrations, grandfathering, SLAs, open-source governance. | Broken lifetime promise, license bait-and-switch. | Changelog, terms history, migration tools, governance docs. |
| Excellence / Ihsan | Build beyond minimum legal compliance. | Accessibility, reliability, testing, maintainability, docs, security. | Bare-minimum compliance with fragile operations. | Test suites, accessibility audits, docs, SLOs. |
| Modesty / Haya | Avoid unnecessary exposure and attention exploitation. | Visibility defaults, media display, recommendations, notifications. | Public-by-default sensitive data, immodest incentive loops. | Default settings, recommendation rules, content controls. |
| Avoiding Spying / Tajassus | Avoid unjustified collection, inference, tracking, profiling, and monitoring. | Analytics, telemetry, location, device signals, admin monitoring. | Cross-context tracking, unnecessary telemetry, profiling for ads. | Data inventory, consent copy, retention schedule, access logs. |

## Domain Table

| Domain | Responsibility question |
| --- | --- |
| Strategic | Does strategy pressure the provider toward deception, surveillance, riba, lock-in, abandonment, or enshittification? |
| Design | Do flows and defaults protect user interest rather than exploiting satisfaction, inattention, fear, guilt, or confusion? |
| Technical | Are responsibilities inspectable, enforceable, reversible, secure, portable, maintainable, and safely isolated for the product model? |
| Operational | Are promises supported by support, incident response, moderation, backup, EOL, and migration practices? |
| Governance | Are harmful decisions constrained, auditable, appealable, and escalated to the right authority? |
| Evaluation | Is there evidence from audits, incidents, complaints, appeals, stakeholder feedback, and longitudinal review? |

## Principle-To-Domain Examples

- Trust in technical architecture: account/workspace/tenant or local-user isolation, backup recovery, privileged-access logs, and credible self-hosting docs where relevant.
- Truthfulness in marketing: bounded AI, privacy, open-source, availability, and comparison claims.
- Justice in moderation: clear rules, proportional enforcement, appeals, and consistency metrics.
- Non-Harm in privacy/security: least data, threat modeling, vulnerability reporting, and incident response.
- Promise-Keeping in EOL/pricing: migration paths, notice periods, grandfathering, and transparent term changes.
- Modesty in content defaults: restrictive public visibility and non-exploitative recommendations.
- Avoiding Spying in telemetry: no unnecessary tracking, profiling, or intrusive device signals.
