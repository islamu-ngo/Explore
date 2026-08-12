<!-- ABOUTME: Review checklist for independent Structure, Sequence, and Organization in externally informed work. -->
<!-- ABOUTME: Applies AFC as a conservative engineering heuristic while preserving legal-evidence boundaries. -->

# SSO And Provenance Review

## Abstraction

Describe the external behavior and proposed ISLAMU design at these levels:

1. user or operational goal;
2. workflow and state transitions;
3. subsystem and layer boundaries;
4. domain entities and data relationships;
5. operations, ordering, errors, UI composition, and tests.

## Filtration

For each similarity, record whether it is dictated by:

- functionality or security;
- a public standard, protocol, or required wire contract;
- platform/framework constraints;
- efficiency or interoperability;
- public-domain or commonplace domain practice;
- no external constraint.

The classification is review evidence, not a legal conclusion. Uncertain or discretionary similarities remain in scope for comparison.

## Comparison

Review remaining similarities in naming, grouping, module boundaries, state machines, schema relationships, operation sequence, UI hierarchy, error taxonomy, test arrangement, and prose. A different language, ORM, or framework alone does not establish independence.

Pass only when discretionary expression is independently designed and traceable to ISLAMU requirements. Redesign unexplained similarities or obtain legal review.

## Minimum Evidence

- Sanitized handoff path and source register.
- Implementer attestation that restricted source was not accessed or supplied.
- Repository-native design anchors and at least one meaningful independent design decision.
- List of constrained similarities and why each was unavoidable.
- Reviewer, date, decision (`pass`, `redesign`, or `legal review`), tests, PR, and journal link.

