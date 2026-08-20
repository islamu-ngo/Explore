---
name: grill-me
description: "Load when the user asks to be grilled, interviewed relentlessly, or stress-tested on a plan or design; not for ordinary plan creation, review, or implementation."
type: workflow
enforcement: suggest
priority: medium
---
<!-- ABOUTME: Decision-tree interview workflow for stress-testing a plan or design. -->
<!-- ABOUTME: Resolves codebase facts directly and asks the user one decision question at a time. -->

## Rules

- Ask exactly one question per response and wait for the answer.
- Give a recommended answer with a concise rationale before each question.
- Resolve upstream decisions before asking about choices that depend on them.
- When repository evidence can answer a question, inspect the codebase and treat the finding as resolved instead of asking the user.
- Continue until every relevant branch is resolved and both sides share the same understanding.

## Workflow

1. Find the nearest unresolved decision that gates the remaining design.
2. Resolve it from repository evidence when possible; otherwise recommend an answer and ask one question.
3. Use the answer to select the next dependent branch, revisiting earlier decisions when it exposes a conflict.
4. When no relevant branches remain, summarize the agreed decisions, assumptions, and open risks.
