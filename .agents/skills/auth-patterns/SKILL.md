---
name: auth-patterns
description: Apply project authentication and authorization rules for BFF token handling, JWT validation, claim extraction, and handler-level access checks.
type: pattern
enforcement: suggest
priority: critical
---
<!-- ABOUTME: Authentication and authorization rules for BFF cookies, JWT validation, Keycloak claims, endpoint protection, and HAL-based UI affordances. -->
<!-- ABOUTME: Keeps tokens out of the browser, preserves claim extraction order, and aligns API, handler, and client authorization behavior. -->

## Purpose
Use this skill for browser-to-BFF-to-API authentication flow, endpoint protection defaults, claim extraction, and resource authorization behavior. It keeps security logic consistent across controllers, handlers, and HAL-driven clients.

## When to Load
- Keywords: auth, JWT, OIDC, Keycloak, authorize, claim, audience, authorized party, cookie forwarding.
- File patterns: `*Controller.cs`, `*Program.cs`, `**/Authorization/**/*.cs`, `Explore.API/**/*.cs`, `Explore.Blazor/**/*.cs`.
- Intent IDs: `add-write-endpoint`, `add-get-endpoint`, `add-hal-link`, `blazor-component-affordance`.

## When NOT to Load
- Not for pure Blazor rendering, theming, or dialog issues; use [../blazor-ui-conventions/SKILL.md](../blazor-ui-conventions/SKILL.md).
- Not for non-auth CQRS structure work where access rules are unchanged; use [../cqrs-mediatr-guidelines/SKILL.md](../cqrs-mediatr-guidelines/SKILL.md).

## Must-Read Docs
- [../../../docs/SECURITY-MODEL.md](../../../docs/SECURITY-MODEL.md)
- [../../../docs/API.md](../../../docs/API.md)
- [../../../docs/AUTHORIZATION.md](../../../docs/AUTHORIZATION.md)
- [../../../docs/AUTHORIZATION_PATTERNS.md](../../../docs/AUTHORIZATION_PATTERNS.md)
- [../../../docs/QUICK_REFERENCE.md](../../../docs/QUICK_REFERENCE.md)
- [resources/local-authorization-provider.md](resources/local-authorization-provider.md)

## Top 5 Invariants
1. The browser never sees tokens because the BFF stores them in HttpOnly cookies and forwards a `Bearer` token to the API.
2. User ID extraction follows `sub` then `nameidentifier` then `sid`, and a missing user identifier yields `401 Unauthorized`.
3. JWT validation checks both `aud` and `azp` with a five-minute clock skew, and authorized audiences include `islamu-event-api` and `islamu-event-blazor`.
4. Endpoint defaults are `GET` with `[AllowAnonymous]`, writes with `[Authorize]`, while resource/action checks route through `AuthorizationBehavior` to `RuntimeAuthorizationProvider` (which delegates to Cerbos gRPC PDP or `FallbackAuthorizationService`).
5. HATEOAS authorization follows the Candidate, Normalize, Batch, and Materialize pipeline and fails closed, leveraging `AuthorityProfile` pre-resolution in local mode and making `_links` the only client-side source of truth for action gating.

## Top 5 Anti-Patterns
1. Storing tokens in `localStorage` or `sessionStorage` bypasses the BFF boundary and weakens browser-side security.
2. Gating UI actions with role or claim inspection instead of HAL `_links` drifts from the server authorization contract.
3. Logging raw JWTs leaks secrets into traces, logs, and support artifacts.
4. Disabling the `Tenant` query filter during runtime request handling creates cross-tenant authorization and data-isolation bugs.
5. Validating only `aud` or only `azp` allows unauthorized clients to present otherwise valid tokens.

## Minimal Examples
```csharp
public static class ClaimsPrincipalExtensions
{
    public static string GetRequiredUserId(this ClaimsPrincipal user)
    {
        string? userId = user.FindFirstValue("sub")
            ?? user.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? user.FindFirstValue("sid");

        return string.IsNullOrWhiteSpace(userId)
            ? throw new UnauthorizedAccessException("Missing user id claim.")
            : userId;
    }
}
```

```csharp
public sealed record UpdateEventCommand(Guid EventId, string Title)
    : IAuthorizedRequest<BaseCommandResponse<Guid>>;

public sealed class UpdateEventHandler(IEventRepository repository)
{
    public async Task<BaseCommandResponse<Guid>> Handle(
        UpdateEventCommand request,
        CancellationToken cancellationToken)
    {
        Event entity = await repository.GetRequiredAsync(request.EventId, cancellationToken);
        entity.Rename(request.Title);
        await repository.UpdateAsync(entity, cancellationToken);
        return new BaseCommandResponse<Guid>(entity.Id, true, "Updated");
    }
}
```

## Verification Hooks
- `dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet --filter FullyQualifiedName~Event.Architecture.Tests.AuthorizationParityTests`
- `dotnet test --project Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet`
- `dotnet build --configuration Release --verbosity quiet`

## Related Skills
- [../clean-architecture-rules/SKILL.md](../clean-architecture-rules/SKILL.md)
- [../cqrs-mediatr-guidelines/SKILL.md](../cqrs-mediatr-guidelines/SKILL.md)
- [../blazor-bff-patterns/SKILL.md](../blazor-bff-patterns/SKILL.md)
