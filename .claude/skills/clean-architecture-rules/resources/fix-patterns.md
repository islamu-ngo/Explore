ABOUTME: Fix patterns for common Clean Architecture violations.
ABOUTME: Use this as a short checklist during refactors.

# Fix Patterns

## Core Fixes
- Move business rules from controllers → Application/Domain.
- Replace direct DbContext access in Application → repository interface.
- Move external service usage → Infrastructure via Application interface.
- Keep DTOs in Application; API/Blazor consume them.

## Quick Decision Guide
1. Business rule? → **Domain**
2. Use case/workflow? → **Application**
3. Database access? → **Persistence** (via interface)
4. External service? → **Infrastructure** (via interface)
5. HTTP/UI specific? → **API/Blazor**

**Related**: `dependency-rules.md`, `layer-responsibilities.md`.
