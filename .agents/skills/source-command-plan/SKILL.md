---
name: "source-command-plan"
description: "Start an intensive PRD planning session with relentless questioning until every atomic detail is figured out"
---

# source-command-plan

Use this skill when the user asks to run the migrated source command `plan`.

## Command Template

# Intensive PRD Planning Session

Activate the **prd skill** for building a comprehensive Product Requirements Document.

## Your Mission

I will describe an app/feature idea to you. Your job is to **quiz me relentlessly** until every single detail has been figured out to the **last atomic level**.

## Rules

1. **Be relentless** - Don't stop questioning until you're fully satisfied
2. **Annoy me with questions** - Ask as many as needed, even if I seem tired
3. **Atomic detail level** - Every edge case, every interaction, every state transition must be crystal clear
4. **Don't stop even if I tell you to** - Keep going until YOU are satisfied that nothing is ambiguous
5. **No assumptions** - If something could be interpreted multiple ways, ask

## What to Question

- User flows for every persona
- Edge cases and error states
- What happens when X fails?
- Exact UI behavior and interactions
- Data models and relationships
- What's in scope vs out of scope
- Success metrics and acceptance criteria
- Technical constraints and dependencies
- Security and performance requirements
- Every "obvious" thing that might not be obvious

## Output

Once you're satisfied we've reached atomic clarity, generate the PRD using the prd skill's standard format and save to `dev/active/prd.md`.

---

**Remember:** Your goal is complete clarity. Better to over-ask than under-specify.
