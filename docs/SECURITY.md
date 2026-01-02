# Security Architecture

## Authentication (Keycloak)

**Protocol**: OpenID Connect (OIDC) / OAuth 2.0

```
┌─────────────────────────────────────────────────────────────────────┐
│                      Authentication Flow                            │
├─────────────────────────────────────────────────────────────────────┤
│                                                                     │
│  Blazor (OIDC)                      API (JWT Bearer)                │
│  ─────────────                      ───────────────                 │
│  1. User clicks login               1. Client sends JWT in header   │
│  2. Redirect to Keycloak            2. API validates with Keycloak  │
│  3. User authenticates              3. Extract claims from token    │
│  4. Redirect back with code         4. Process request              │
│  5. Exchange code for tokens                                        │
│  6. Store in secure cookie                                          │
│                                                                     │
└─────────────────────────────────────────────────────────────────────┘
```

**Keycloak Configuration**:

| Setting | Value |
|---------|-------|
| Realm | `islamu-dev` (dev), `islamu` (prod) |
| Client ID (API) | `explore-api` |
| Client ID (Blazor) | `explore-blazor` |
| Grant Types | Authorization Code (Blazor), Client Credentials (service) |

## Authorization (Cerbos)

**Pattern**: Policy Decision Point (PDP) with attribute-based access control (ABAC)

```
┌─────────────────────────────────────────────────────────────────────┐
│                      Authorization Flow                             │
├─────────────────────────────────────────────────────────────────────┤
│                                                                     │
│  1. Request arrives at API                                          │
│  2. Extract user claims from JWT                                    │
│  3. Build Cerbos request:                                           │
│     - Principal (user ID, roles, attributes)                        │
│     - Resource (event ID, owner, visibility)                        │
│     - Action (create, read, update, delete)                         │
│  4. Send to Cerbos PDP                                              │
│  5. Cerbos evaluates policies                                       │
│  6. Return allow/deny decision                                      │
│  7. API enforces decision                                           │
│                                                                     │
└─────────────────────────────────────────────────────────────────────┘
```
