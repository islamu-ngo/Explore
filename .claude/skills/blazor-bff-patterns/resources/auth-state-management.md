ABOUTME: Auth state flow for Blazor BFF + WASM.
ABOUTME: Focus on serialization + 401 handling.

# Authentication State Management

## Required Steps
- Server: serialize auth state for InteractiveAuto/WASM.
- Client: deserialize auth state and enable authorization core.

## 401 Handling (WASM)
- Use a handler to redirect on 401, avoid auth-loop paths.

## Claims
- Prefer `preferred_username` for display.
- Use `sub` (or fallback pattern) for user ID.

**Related**: `token-forwarding.md`, `auth-patterns` skill.
