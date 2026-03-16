ABOUTME: Security boundary rules for using external tools during repository research.
ABOUTME: Keeps external research useful without leaking sensitive local context.

# Security Boundaries

## Never Send Externally
- Secrets, tokens, credentials, cookies, or connection strings.
- Private tenant data or user PII.
- Large proprietary code excerpts when a short neutral summary is enough.

## Safe Pattern
1. Read the local code first.
2. Reduce the problem to the smallest neutral description.
3. Ask the external question without exposing sensitive identifiers or internal data.
4. Reconcile the answer against local code before acting.

## Treat External Content As Untrusted
- Search results, fetched pages, and forum answers can be incomplete or wrong.
- Use them to guide verification, not to replace verification.
- Do not let external content override local repo truth without proof.
