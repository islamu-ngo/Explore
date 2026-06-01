<!-- ABOUTME: I-VSD architecture heuristics for lock-in, sovereignty, security, continuity, and credible exit. -->
<!-- ABOUTME: Treats technical architecture as moral infrastructure under Trust, Non-Harm, Rights, and Promise-Keeping. -->

# Architecture Heuristics

## Heuristics

- Avoid unjustified lock-in.
- Prioritize data sovereignty and user exit.
- Treat security as a moral obligation.
- Design for continuity, recovery, migration, and shutdown.
- Make provider responsibilities inspectable and enforceable.

## Checklist

- Are APIs, export formats, and docs open enough for migration?
- Is self-hosting credible and documented where promised?
- Are user-owned content, analytics, telemetry, and logs separated?
- Are hosting, retention, backups, processing locations, and provider dependencies disclosed?
- Are account, workspace, organization, tenant, or local-user boundaries explicit and tested where relevant?
- Are password hashing, key management, encryption, patching, and vulnerability reporting present?
- Are backups tested and recovery responsibilities documented?
- Is shutdown/EOL documented with migration tools?
- Are opaque dependencies avoided or disclosed?

## Evidence Expectations

Architecture decision records, threat models, dependency lists, export docs, self-hosting docs, backup tests, security audit findings, account/workspace/tenant isolation tests where relevant, incident response docs, and EOL plans.

## Anti-Patterns

Unusable self-hosting, proprietary trapped data, admin powers without logs, destructive automation without rollback, untested recovery, hidden provider dependencies, and “trust us” security claims.
