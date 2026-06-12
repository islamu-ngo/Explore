---
name: prd
description: "Generate a Product Requirements Document (PRD) for a new feature. Use when planning a feature, starting a new project, or when asked to create a PRD. Triggers on: create a prd, write prd for, plan this feature, requirements for, spec out."
---

ABOUTME: PRD generation workflow (no implementation).
ABOUTME: Keep questions minimal and output structured PRD.

# PRD Generator

## Job Summary
1. Ask **3–5** clarifying questions (lettered options).
2. Produce a structured PRD.
3. Save to `dev/active/prd.md`.

**Important:** Do **not** implement. Only write the PRD.

## Clarifying Questions (Required)
- Ask only critical questions (Problem/Goal, Core Functionality, Scope, Success).
- Use lettered options (A/B/C/D) so users can answer quickly.

## PRD Structure (Required)
1. Introduction/Overview
2. Goals
3. User Stories (with acceptance criteria)
4. Functional Requirements (numbered)
5. Non‑Goals
6. Design Considerations (optional)
7. Technical Considerations (optional)
8. Success Metrics
9. Open Questions

**UI stories must include**: “Verify in browser using dev‑browser skill”.

## Archive Previous PRDs (Required)
Before writing a new PRD:
1. Read existing `dev/active/prd.md` if present.
2. If it’s a different feature, archive to `dev/archive/YYYY-MM-DD-feature-name/`.

## Output
- Markdown file: `dev/active/prd.md`
