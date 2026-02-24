ABOUTME: Minimal UI patterns for forms, dialogs, and tables in this project.
ABOUTME: Keeps only non-inferable rules for consistency and safety.

# Common Patterns (Lean)

## Forms
- Use MudBlazor components + validation.
- Show inline errors and disable submit while saving.

## Dialogs
- Use MudDialog for confirmations and form entry.
- Always return a clear result (ok/cancel + optional payload).

## Tables & Lists
- Prefer `MudTable` for CRUD lists.
- Provide empty/loading states and deterministic sort.

## Errors
- Catch and surface user-friendly messages; log exceptions.

## Related
- [mudblazor-usage.md](mudblazor-usage.md)
