<!-- ABOUTME: I-VSD technical decision framework for stack selection, portability, privacy, auth, isolation, standards, and operations. -->
<!-- ABOUTME: Separates architecture traceability from unproven claims about reliability, trust, security, or accessibility. -->

# Technical Decision Framework

## Selection Criteria

- Open source where it supports inspectability and stewardship.
- Self-hostable when promised or needed for provider/user sovereignty.
- Data portability and documented export formats.
- Client or instance freedom where relevant.
- No unjustified vendor lock-in.
- Privacy-respecting architecture and minimal telemetry.
- Inspectable authentication, authorization, account/workspace/tenant or local-user isolation, and administrative powers.
- Community governance for shared infrastructure.
- Standards orientation and interoperability.
- Federation readiness without premature claims.
- Accessibility-oriented architecture.
- Sustainable documentation, tests, maintenance, and operations.

## Claim Boundaries

Allowed: “This stack supports traceability to Trust, portability, and inspectability heuristics.”

Not allowed without evidence: “This stack proves reliability, accessibility, security, user trust, or long-term ethical outcomes.”

## Evidence Expectations

Architecture records, dependency choices, auth/authz design, account/workspace/tenant or local-user isolation model where relevant, export docs, tests, runbooks, accessibility strategy, threat model, ops plan, and known tradeoffs.
