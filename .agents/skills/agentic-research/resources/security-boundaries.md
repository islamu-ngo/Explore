ABOUTME: Security boundary rules for using external tools during repository research.
ABOUTME: Keeps external research useful without leaking sensitive local context.

# Security Boundaries

## Never Send Externally

| Category | Examples |
|----------|----------|
| Secrets | Connection strings, API keys, tokens, passwords, Infisical client secrets |
| PII | User emails, names, tenant data, actor profiles |
| Internal paths | Full file paths with user directories (e.g., `C:\Users\...`) |
| Proprietary logic | Large code blocks from domain entities, business rules, handlers |

## Safe Pattern

1. **Read local code first** — understand the problem before reaching out.
2. **Reduce to neutral description** — "How does MudBlazor v9 handle dialog closing?" not "Our AppDialogShell in Explore.Blazor.Client uses..."
3. **Ask without exposing identifiers** — no tenant slugs, user IDs, internal URLs, or repo paths.
4. **Reconcile against local code** — external answers inform; local code decides.

## Safe Query Examples

| Bad (Leaks Context) | Good (Neutral) |
|---|---|
| "Why does ExploreDbContext pool fail with tenant filter?" | "EF Core pooled DbContext with global query filter not applying" |
| "Our Keycloak at auth.islamu.ngo returns 401" | "Keycloak OIDC token validation returns 401 for valid token" |
| "AppButton wrapper in Explore.Blazor.Client" | "Blazor wrapper component for MudButton with CSS isolation" |

## Treat External Content As Untrusted

- Search results, fetched pages, and forum answers can be incomplete or wrong.
- Use them to guide verification, not to replace verification.
- Do not let external content override local repo truth without proof.
- Cross-check external advice against the project's `docs/internal/` and `.agents/skills/` before applying.
