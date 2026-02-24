ABOUTME: Fallback order for extracting user ID from claims.
ABOUTME: Keep extraction consistent across API and handlers.

# User ID Extraction Pattern

## Rule (Required)
Use this fallback order when extracting a user ID from `ClaimsPrincipal`:

1. `sub`
2. `http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier`
3. `sid`

## Minimal Example
```csharp
var userId = user.FindFirst("sub")?.Value
    ?? user.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value
    ?? user.FindFirst("sid")?.Value;
```

## Notes
- If missing, treat as unauthenticated (return 401 or equivalent).
- Keep parsing centralized; don’t duplicate logic in multiple controllers.

**Related**: `auth-patterns` skill.
