ABOUTME: Source selection guidance for agentic research in this repository.
ABOUTME: Explains when to stay local, when to use official docs, and when external research is justified.

# Source Selection

## Evidence Order
1. Local code, tests, config, and project files.
2. Local docs and `.claude` files.
3. Official framework or library docs.
4. External research tools.

## Stay Local When
- You need controller, handler, DI, middleware, route, or Blazor component behavior.
- The question is about repo conventions, file layout, test expectations, or existing patterns.
- The code or docs already answer the question with a targeted read or search.

## Escalate To Official Docs When
- Package, framework, runtime, or SDK behavior is unclear.
- You need authoritative migration or breaking-change guidance.
- Security-sensitive defaults or middleware behavior must be confirmed.

## Escalate To External Research When
- You need standards, RFCs, advisories, ecosystem comparisons, or broader implementation landscape.
- Official docs are insufficient or do not cover the comparison question.

## Stop Conditions
- The repo already answers the question.
- Official docs confirm the behavior you need.
- Additional sources are repeating the same conclusion without adding value.
