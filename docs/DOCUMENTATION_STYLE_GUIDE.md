ABOUTME: Defines the writing and formatting conventions for repository documentation.
ABOUTME: Optimized for concise, implementation-accurate docs that are easy for juniors to use.

# Documentation Style Guide

## Writing Principles

- Write for action: what to do, where to look, what is enforced.
- Prefer factual language over promotional wording.
- Prefer short sections over long narratives.
- Prioritize non-inferable facts (exact keys, fallback order, defaults, constraints).
- If a statement can drift, link it to a concrete source file.

## Voice and Tone

- Use direct, active voice.
- Address reader as "you" only when giving instructions.
- Avoid vague wording like "usually", "often", "might" unless uncertainty is real and explicit.

## Structure

Recommended page shape:

1. Purpose (one sentence)
2. Core rules or behavior
3. Practical usage notes
4. Related docs

Keep headings simple: `#`, `##`, `###`.

## Formatting Rules

- Use inline code for identifiers, settings, endpoints, and paths.
- Use tables for comparisons and key lists.
- Keep code blocks short and only when needed.
- Avoid large diagram-like ASCII blocks.
- Prefer text flows and tables over decorative visuals.

## Content Rules

- Separate implemented behavior from roadmap ideas.
- Mark assumptions explicitly when unavoidable.
- Do not duplicate large sections across multiple docs.
- Update docs in the same change when behavior changes.

## Terminology Baseline

Use consistent core terms:

- Instance: deployment owner scope.
- Tenant: isolated community scope.
- Organization: managed entity within tenant.
- BFF: `Explore.Blazor` server host.
- Client: `Explore.Blazor.Client` WASM UI.
- API: `Explore.API`.

## Doc Review Checklist

- Is every key technical claim traceable to code?
- Is the page concise and task-relevant?
- Are examples minimal and non-repetitive?
- Are related docs linked?
- Did we remove stale or duplicate sections?
