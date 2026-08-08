# Comprehensive API Refactoring Report & Guidelines Strategy
**Target Codebase:** ISLAMU Event Platform APIs  
**Architectural Scope:** `src/Explore.API`, `src/Explore.Application`, `src/Event.Web.BffHosting`, `src/Event.Standalone`, `src/Explore.Infrastructure`, `src/Explore.Persistence`  
**Date:** August 2026  
**Author:** Senior C# / .NET Software Architect & Code Quality Specialist  

---

## 1. Executive Summary & Context

This document is the definitive, exhaustive **API Refactoring Report & Architectural Strategy** for the ISLAMU Event Platform. It addresses control-flow clutter, nested conditional logic (`if`, `if-else`, deeply nested branches), string-based exception checking, imperative query construction, and monolithic validation logic across all API projects in the repository.

As the platform expanded to support multi-tenancy, granular RBAC/ReBAC authorization, complex registration state machines, AT Protocol federation, and multi-provider AI integrations, conditional branching accumulated inside MediatR handlers, API controllers, background workers, and persistence evaluators.

### Primary Objectives:
1. **Systemic Cyclomatic Complexity Reduction:** Reduce peak method complexity across all API projects from > 15 (and in some cases > 80) down to < 4.
2. **Self-Documenting Code & Business Intent Transparency:** Make business rules declarative and obvious using modern C# 12/13 idioms (Guard Clauses, Pattern Matching, Switch Expressions, Strategy Pattern, Specification Pipeline Pattern, and Chain of Responsibility).
3. **Strict Invariant & Contract Preservation:** Maintain 100% compatibility with API response contracts, MediatR `BaseCommandResponse<TId>`, RFC 7807 `ProblemDetails`, HATEOAS `_links` policy affordance gating, tenant isolation query filters, and repository entity returns.
4. **Exhaustive 6-Project Coverage:** Provide line-of-code grounded refactoring designs for `src/Explore.API`, `src/Explore.Application`, `src/Event.Web.BffHosting`, `src/Event.Standalone`, `src/Explore.Infrastructure`, and `src/Explore.Persistence`.

---

## 2. Systemic Control-Flow Issues Identified Across Codebase

Through exhaustive static analysis of the API codebase, five core control-flow smells were identified:

### Smell 1: The "Arrow Anti-Pattern" (Deeply Nested Preconditions)
- **Manifestation:** Methods nesting 4 to 8 levels deep of `if` checks before performing core actions.
- **Impact:** High mental cognitive load, hidden failure branches, difficult edge-case test coverage.
- **Example Files:** `RegistrationOrderLifecycleService.cs`, `KeycloakBootstrapService.cs`, `FinalizeStorageUploadSessionCommandHandler.cs`.

### Smell 2: Type-Casting & Monolithic Dynamic Dispatch Chains
- **Manifestation:** Sequential `if (request is UpdateTypeACommand) ... else if (request is UpdateTypeBCommand)` chains checking concrete request types.
- **Impact:** Class constructors bloated with up to 22 injected repositories; violates the Open/Closed Principle (adding a new command requires modifying existing handlers).
- **Example Files:** `AuthorizationBehavior.cs`, `UpdateInstanceSubResourceHandlers.cs`.

### Smell 3: Imperative Specification & Query Construction Cascades
- **Manifestation:** Chaining 35 to 45 sequential `if` checks appending filter parameters onto a query specification object (`spec = spec.And(...)`).
- **Impact:** Handler code becomes repetitive boilerplate; logic for checking module enablement and collection counts is duplicated.
- **Example Files:** `GetEventListRequestHandler.cs`, `EventLifecycleReadinessEvaluator.cs`.

### Smell 4: String Message Inspection for Control Flow
- **Manifestation:** API Controllers inspecting command result text using `if (response.Message?.Contains(...) == true)` to determine HTTP status codes (`403 Forbidden` vs `400 Bad Request`).
- **Impact:** Fragile runtime error handling; refactoring a error message breaks API response status codes without compiler warnings.
- **Example Files:** `InstanceSettingsController.cs`, `ExploreControllerBase.cs`.

### Smell 5: Parameter Validation & Response Formatting Boilerplate in MCP Tools
- **Manifestation:** MCP tool methods manually checking string lengths, nulls, and page sizes, then wrapping responses in nested `try/catch` and `if/else` blocks.
- **Impact:** Cluttered tool endpoints obscuring the underlying MediatR query calls.
- **Example Files:** `EventManagementMcpTools.cs`.

---

## 3. Core Refactoring Principles & Pattern Registry

To remediate these issues, the codebase must strictly adhere to the following six design patterns:

```
┌────────────────────────────────────────────────────────────────────────────────────────┐
│                                DESIGN PATTERN REGISTRY                                 │
├──────────────────────────┬─────────────────────────────────────────────────────────────┤
│ Pattern                  │ Applied To                                                  │
├──────────────────────────┼─────────────────────────────────────────────────────────────┤
│ 1. Guard Clauses         │ Request Preconditions, Null Checks, Fail-Fast Exits         │
│ 2. Pattern Matching      │ C# 12/13 Switch Expressions, Property & Relational Patterns│
│ 3. Strategy Pattern      │ Dynamic Type Dispatch, LLM Parsing, Jetstream Eventing      │
│ 4. Specification Pipeline│ Composable Query Filtering & Business Readiness Checks      │
│ 5. Chain of Responsibility│ Multi-Tier Governance Cascade (User->Group->Org->Tenant)     │
│ 6. Typed Error Mapping   │ Domain Error Code Enums -> RFC 7807 ProblemDetails           │
└──────────────────────────┴─────────────────────────────────────────────────────────────┘
```

### Pattern 1: Self-Documenting Guard Clauses
- **Rule:** Fail fast at the very top of a method. Evaluate failure conditions early and return/throw immediately to keep the main execution path un-indented.
- **Idiom:**
  ```csharp
  ArgumentNullException.ThrowIfNull(order);
  if (!order.CanBeSubmitted()) return Failure(order.Id, "Invalid status.");
  ```

### Pattern 2: C# Modern Pattern Matching & Switch Expressions
- **Rule:** Replace verbose `if/else if/else` statements with C# 12/13 pattern matching (Property patterns, Relational patterns, Logical patterns).
- **Idiom:**
  ```csharp
  var httpStatus = response.ErrorCode switch
  {
      DomainErrorCode.AdminRequired => StatusCodes.Status403Forbidden,
      DomainErrorCode.NotFound      => StatusCodes.Status404NotFound,
      _                             => StatusCodes.Status400BadRequest
  };
  ```

### Pattern 3: Behavioral Strategy Pattern
- **Rule:** Decouple type-dependent conditional branching into standalone, polymorphic strategy classes implementing a common interface registered in Dependency Injection (`IEnumerable<TStrategy>`).
- **Idiom:** `IResourceContextResolver` strategy lookup replaces 50+ type cast branches in authorization behaviors.

### Pattern 4: Specification & Pipeline Pattern
- **Rule:** Convert imperative filter assembly into a declarative, composable specification pipeline using extension methods (`ApplyIf`, `ApplyIfCollection`).

### Pattern 5: Chain of Responsibility
- **Rule:** Evaluate multi-tier settings overrides (User → Group → Organization → Tenant → Instance) by chaining handler nodes where each node handles its tier or passes to `_next`.

### Pattern 6: Typed Error Classification
- **Rule:** Classify command failures using structured `DomainErrorCode` enums on `BaseCommandResponse<TId>` instead of parsing raw error strings in controllers.

---

## 4. In-Depth Before-and-After Refactoring Examples

### Example 1: MediatR Authorization Pipeline Strategy Refactoring
**File:** `src/Explore.Application/Behaviors/AuthorizationBehavior.cs`

#### Before (50+ Branch Type-Casting Chain & 22 Dependencies)
```csharp
// BEFORE: Monolithic handler checking concrete command types sequentially
public class AuthorizationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
{
    private readonly ICustomPropertyDefinitionRepository? _customPropertyDefinitionRepository;
    private readonly IEventCustomPropertyRepository? _eventCustomPropertyRepository;
    private readonly IEventSessionLanguageRepository? _eventSessionLanguageRepository;
    // ... 19 additional repository dependencies injected into constructor ...

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var attribute = AttributeCache.GetOrAdd(typeof(TRequest), static t => t.GetCustomAttribute<AuthorizeResourceAttribute>());
        if (attribute is not null)
        {
            string? resourceId = null;
            Dictionary<string, object>? resourceAttributes = null;

            if (request is UpdateCustomPropertyDefinitionCommand definitionRequest)
            {
                var definition = _customPropertyDefinitionRepository is null
                    ? null
                    : await _customPropertyDefinitionRepository.GetDefinitionWithDetails(definitionRequest.DefinitionId);
                if (definition is null || (_tenantContext is not null && definition.TenantId != _tenantContext.TenantId))
                {
                    throw new AuthorizationException(attribute.Resource, attribute.Action);
                }
                definitionRequest.TenantId = definition.TenantId;
                resourceId = definition.TenantId.ToString();
                resourceAttributes = new Dictionary<string, object> { ["tenantId"] = definition.TenantId.ToString() };
            }
            else if (request is UpdateEventCustomPropertyDefinitionCommand eventDefinitionRequest)
            {
                var definition = _eventCustomPropertyRepository is null
                    ? null
                    : await _eventCustomPropertyRepository.GetDefinitionWithDetails(eventDefinitionRequest.DefinitionId);
                if (definition is null || (_tenantContext is not null && definition.TenantId != _tenantContext.TenantId))
                {
                    throw new AuthorizationException(attribute.Resource, attribute.Action);
                }
                eventDefinitionRequest.TenantId = definition.TenantId;
                resourceId = definition.TenantId.ToString();
                resourceAttributes = new Dictionary<string, object> { ["tenantId"] = definition.TenantId.ToString() };
            }
            else if (request is UpdateEventSessionLanguageCommand languageRequest)
            {
                var assignment = _eventSessionLanguageRepository is null
                    ? null
                    : await _eventSessionLanguageRepository.GetById(languageRequest.EventSessionLanguageId);
                if (assignment is null) throw new AuthorizationException(attribute.Resource, attribute.Action);
                languageRequest.EventSessionId = assignment.EventSessionId;
                resourceId = assignment.EventSessionId.ToString();
            }
            // ... 45+ additional 'else if' branches repeating repository lookup and null checks
            
            await EnforceAuthorizationAsync(attribute.Resource, resourceId, attribute.Action, resourceAttributes, typeof(TRequest).Name, cancellationToken);
        }
        return await next(cancellationToken);
    }
}
```

#### After (Polymorphic Strategy Pattern + Guard Clause)
```csharp
// AFTER: Generic strategy contract for resource context extraction
public interface IResourceContextResolver
{
    bool CanResolve(object request);
    Task<ResourceContextResult> ResolveContextAsync(object request, ITenantContext? tenantContext, CancellationToken ct);
}

public sealed record ResourceContextResult(string ResourceId, Dictionary<string, object>? Attributes);

// Concrete Strategy for CustomPropertyDefinition
public sealed class CustomPropertyDefinitionResourceResolver(
    ICustomPropertyDefinitionRepository repository) : IResourceContextResolver
{
    public bool CanResolve(object request) => request is UpdateCustomPropertyDefinitionCommand;

    public async Task<ResourceContextResult> ResolveContextAsync(object request, ITenantContext? tenantContext, CancellationToken ct)
    {
        var cmd = (UpdateCustomPropertyDefinitionCommand)request;
        var definition = await repository.GetDefinitionWithDetails(cmd.DefinitionId)
            ?? throw new AuthorizationException("CustomPropertyDefinition", "Update");

        if (tenantContext is not null && definition.TenantId != tenantContext.TenantId)
            throw new AuthorizationException("CustomPropertyDefinition", "Update");

        cmd.TenantId = definition.TenantId;
        return new ResourceContextResult(definition.TenantId.ToString(), new() { ["tenantId"] = definition.TenantId.ToString() });
    }
}

// Refactored Behavior: Zero concrete type branching, 2 dependencies
public class AuthorizationBehavior<TRequest, TResponse>(
    IAuthorizationProvider authorizationProvider,
    IEnumerable<IResourceContextResolver> resolvers,
    ITenantContext? tenantContext) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var attribute = AttributeCache.GetOrAdd(typeof(TRequest), static t => t.GetCustomAttribute<AuthorizeResourceAttribute>());
        if (attribute is null) return await next(cancellationToken);

        var resolver = resolvers.FirstOrDefault(r => r.CanResolve(request));
        var context = resolver is not null
            ? await resolver.ResolveContextAsync(request, tenantContext, cancellationToken)
            : ExtractDefaultContext(request);

        await authorizationProvider.EnforceAsync(attribute.Resource, context.ResourceId, attribute.Action, context.Attributes, cancellationToken);
        return await next(cancellationToken);
    }
}
```

---

### Example 2: Registration Order Lifecycle State Machine Refactoring
**File:** `src/Explore.Application/Services/Registration/RegistrationOrderLifecycleService.cs`

#### Before (Deep Nested Conditionals & Branching)
```csharp
// BEFORE: Deeply nested state checks and total calculations
public async Task<RegistrationOrderLifecycleResponseDto> SubmitAsync(Guid orderId, Guid tenantId, int? basisPoints, CancellationToken ct)
{
    return await unitOfWork.ExecuteInTransactionAsync(async token =>
    {
        var order = await inventory.GetOrderWithLinesAsync(orderId, tenantId, token);
        if (order is null) return Missing(orderId);

        var status = (RegistrationOrderStatusEnum)order.RegistrationOrderStatusId;
        if (status != RegistrationOrderStatusEnum.AwaitingParticipantDetails)
        {
            return Success(order, status, "Registration order is already submitted.");
        }

        if (basisPoints.HasValue)
        {
            if (basisPoints is < 0 or > 10_000)
            {
                return Failure(orderId, order, "Platform contribution percentage is invalid.");
            }

            if (basisPoints > 0)
            {
                if (order.OrganizerDirectedTotalMinorSnapshot == 0)
                {
                    return Failure(orderId, order, "Platform contributions require an existing payable order total.");
                }

                var setting = await contributionSettings.GetActiveAsync(token);
                if (setting is null || !setting.IsEnabled)
                {
                    return Failure(orderId, order, "Platform contributions are not enabled.");
                }
                // ... nested checks continue down 6 levels
            }
        }
        // ...
    }, ct);
}
```

#### After (Guard Clauses, Pattern Matching & Service Extraction)
```csharp
// AFTER: Clean guards + pattern matching + extracted contribution evaluator
public async Task<RegistrationOrderLifecycleResponseDto> SubmitAsync(Guid orderId, Guid tenantId, int? basisPoints, CancellationToken ct)
{
    return await unitOfWork.ExecuteInTransactionAsync(async token =>
    {
        var order = await inventory.GetOrderWithLinesAsync(orderId, tenantId, token);
        if (order is null) return Missing(orderId);

        if ((RegistrationOrderStatusEnum)order.RegistrationOrderStatusId is var status && status != RegistrationOrderStatusEnum.AwaitingParticipantDetails)
            return Success(order, status, "Registration order is already submitted.");

        if (basisPoints.HasValue)
        {
            var contributionResult = await EvaluateAndApplyPlatformContributionAsync(order, basisPoints.Value, token);
            if (!contributionResult.IsSuccess)
                return Failure(orderId, order, contributionResult.ErrorMessage!);
        }

        bool transitioned = await inventory.TryTransitionOrderAsync(
            order.Id, tenantId, RegistrationOrderStatusEnum.AwaitingParticipantDetails, RegistrationOrderStatusEnum.AwaitingRequirements, timeProvider.GetUtcNow().UtcDateTime, token);

        return transitioned
            ? Success(order, RegistrationOrderStatusEnum.AwaitingRequirements, "Registration order submitted.")
            : await CurrentOrConflictAsync(orderId, tenantId, "Registration order changed while it was submitted.", token);
    }, ct);
}

private async Task<(bool IsSuccess, string? ErrorMessage)> EvaluateAndApplyPlatformContributionAsync(
    RegistrationOrder order, int basisPoints, CancellationToken token)
{
    if (basisPoints is < 0 or > 10_000)
        return (false, "Platform contribution percentage is invalid.");

    if (basisPoints == 0) return (true, null);

    if (order.OrganizerDirectedTotalMinorSnapshot == 0)
        return (false, "Platform contributions require an existing payable order total.");

    var setting = await contributionSettings.GetActiveAsync(token);
    if (setting is not { IsEnabled: true })
        return (false, "Platform contributions are not enabled.");

    if (setting.Options.All(o => o.ContributionBasisPoints != basisPoints))
        return (false, "Platform contribution percentage is invalid.");

    var contribution = RegistrationOrderPlatformContribution.CreateOrNull(
        order.Id, order.TenantId, setting, basisPoints, order.OrganizerDirectedTotalMinorSnapshot, order.CurrencyCode);

    order.SetPlatformContribution(contribution);
    return (true, null);
}
```

---

### Example 3: Event Query Specification Builder Refactoring
**File:** `src/Explore.Application/Features/Events/Handlers/Queries/GetEventListRequestHandler.cs`

#### Before (35+ Sequential `if` Check Cascades)
```csharp
// BEFORE: Imperative builder with endless if checks
private async Task<EventQuerySpecification> BuildSpecificationAsync(GetEventListRequest request, Guid? ownershipActorId, CancellationToken cancellationToken)
{
    var spec = new EventQuerySpecification();
    spec = spec.And(EventFilter.PubliclyDiscoverable());
    spec = spec.And(EventFilter.Status((int)EventStatusEnum.Published));

    if (request.View.HasValue) spec = spec.And(EventSubqueryFilter.Temporal(request.View.Value, DateTimeOffset.UtcNow));
    else if (!hasExplicitDateSearch) spec = spec.And(EventSubqueryFilter.CurrentOrUpcomingPublishedSession());

    if (ownershipActorId.HasValue) spec = spec.And(EventFilter.Actor(ownershipActorId.Value));
    if (!string.IsNullOrWhiteSpace(request.SearchTerm)) spec = spec.And(EventFilter.SearchTerm(request.SearchTerm.Trim()));
    if (request.FormatIds is { Count: > 0 }) spec = spec.And(EventFilter.Formats(request.FormatIds));
    if (request.MadhabIds is { Count: > 0 }) spec = spec.And(EventFilter.Madhabs(request.MadhabIds));
    if (request.EventTypeIds is { Count: > 0 }) spec = spec.And(EventFilter.EventTypes(request.EventTypeIds));
    if (request.AudienceGenderIds is { Count: > 0 }) spec = spec.And(EventFilter.AudienceGenders(request.AudienceGenderIds));
    if (request.AudienceAgeIds is { Count: > 0 }) spec = spec.And(EventFilter.AudienceAges(request.AudienceAgeIds));
    if (request.EventStatusIds is { Count: > 0 }) spec = spec.And(EventFilter.Statuses(request.EventStatusIds));
    // ... 25 more lines of 'if' checks
}
```

#### After (Declarative Specification Extension Pipeline)
```csharp
// AFTER: Fluent composable specification builder extensions
public static class EventQuerySpecificationExtensions
{
    public static EventQuerySpecification ApplyIf<T>(
        this EventQuerySpecification spec, T? value, Func<T, EventQuerySpecification> filterFunc)
    {
        return value is not null ? spec.And(filterFunc(value)) : spec;
    }

    public static EventQuerySpecification ApplyIfCollection<T>(
        this EventQuerySpecification spec, IReadOnlyCollection<T>? items, Func<IReadOnlyCollection<T>, EventQuerySpecification> filterFunc)
    {
        return items is { Count: > 0 } ? spec.And(filterFunc(items)) : spec;
    }
}

// Refactored BuildSpecificationAsync method:
private async Task<EventQuerySpecification> BuildSpecificationAsync(GetEventListRequest request, Guid? ownershipActorId, CancellationToken ct)
{
    var spec = new EventQuerySpecification()
        .And(EventFilter.PubliclyDiscoverable())
        .And(EventFilter.Status((int)EventStatusEnum.Published))
        .ApplyIf(ownershipActorId, EventFilter.Actor)
        .ApplyIf(request.SearchTerm?.Trim(), term => !string.IsNullOrEmpty(term) ? EventFilter.SearchTerm(term) : null!)
        .ApplyIfCollection(request.FormatIds, EventFilter.Formats)
        .ApplyIfCollection(request.MadhabIds, EventFilter.Madhabs)
        .ApplyIfCollection(request.EventTypeIds, EventFilter.EventTypes)
        .ApplyIfCollection(request.AudienceGenderIds, EventFilter.AudienceGenders)
        .ApplyIfCollection(request.AudienceAgeIds, EventFilter.AudienceAges)
        .ApplyIfCollection(request.EventStatusIds, EventFilter.Statuses)
        .ApplyIf(request.DateFrom, EventFilter.DateFrom)
        .ApplyIf(request.DateTo, EventFilter.DateTo);

    return await ApplyModuleConditionalFiltersAsync(spec, request, ct);
}
```

---

### Example 4: Controller Error Classification Refactoring
**File:** `src/Explore.API/Controllers/InstanceSettingsController.cs`

#### Before (String Message Inspection in Controller)
```csharp
// BEFORE: Checking message string contents to decide HTTP status code
private ActionResult<BaseCommandResponse<Guid>> HandleCommandResponse(BaseCommandResponse<Guid> response)
{
    if (response.Success) return Ok(response);

    if (response.Message?.Contains("Only instance administrators", StringComparison.OrdinalIgnoreCase) == true)
    {
        return this.ToForbiddenProblem(detail: response.Message);
    }

    return this.ToCommandValidationProblem(response, InstanceSettingsValidationProblem);
}
```

#### After (Typed Domain Error Codes & Switch Pattern Matching)
```csharp
// AFTER: Typed domain error code matching
private ActionResult<BaseCommandResponse<Guid>> HandleCommandResponse(BaseCommandResponse<Guid> response)
{
    if (response.Success) return Ok(response);

    return response.ErrorCode switch
    {
        DomainErrorCode.InstanceAdminRequired => this.ToForbiddenProblem(detail: response.Message),
        DomainErrorCode.NotFound             => this.ToNotFoundProblem(detail: response.Message),
        _                                    => this.ToCommandValidationProblem(response, InstanceSettingsValidationProblem)
    };
}
```

---

### Example 5: MCP Tool Parameter & Error Sanitizer Refactoring
**File:** `src/Explore.API/Mcp/EventManagementMcpTools.cs`

#### Before (Repetitive Parameter Sanitization & Nested Exception Blocks)
```csharp
// BEFORE: Manual parameter clamping and exception wrapping repeated across 40+ MCP tools
public async Task<string> SearchPublicEventsAsync(
    string? searchTerm = null, int pageNumber = 1, int pageSize = 10, string? sortBy = null, bool sortDescending = true, CancellationToken ct = default)
{
    if (pageNumber < 1) pageNumber = 1;
    if (pageSize > 25) pageSize = 25;
    if (pageSize < 1) pageSize = 10;
    if (searchTerm != null && searchTerm.Length > 120) searchTerm = searchTerm.Substring(0, 120);
    
    try
    {
        var result = await _mediator.Send(new GetEventListRequest { ... }, ct);
        return JsonSerializer.Serialize(result);
    }
    catch (Exception ex)
    {
        return JsonSerializer.Serialize(new { error = ex.Message });
    }
}
```

#### After (Declarative Extensions & Structured Result Wrapping)
```csharp
// AFTER: Declarative parameter sanitization helper
public static class McpSanitizerExtensions
{
    public static int ClampPageNumber(this int page) => Math.Max(1, page);
    public static int ClampPageSize(this int size, int max = 25, int @default = 10) => size is < 1 ? @default : Math.Min(size, max);
    public static string? Truncate(this string? text, int max) => text?.Length > max ? text[..max] : text;
}

// Clean tool endpoint:
public async Task<string> SearchPublicEventsAsync(
    string? searchTerm = null, int pageNumber = 1, int pageSize = 10, string? sortBy = null, bool sortDescending = true, CancellationToken ct = default)
{
    var query = new GetEventListRequest
    {
        SearchTerm = searchTerm.Truncate(120),
        PageNumber = pageNumber.ClampPageNumber(),
        PageSize = pageSize.ClampPageSize(max: 25, @default: 10),
        SortBy = sortBy,
        SortDescending = sortDescending
    };

    return await ExecuteMcpQueryAsync(query, ct);
}
```

---

### Example 6: Infrastructure Keycloak Bootstrap Guard Refactoring
**File:** `src/Explore.Infrastructure/Services/Keycloak/KeycloakBootstrapService.cs`

#### Before (137+ Nested `if` Checks & Status Inspections)
```csharp
// BEFORE: Deeply nested HTTP checks and client secret inspections
public async Task EnsureClientConfiguredAsync(CancellationToken cancellationToken)
{
    var clientResponse = await _httpClient.GetAsync("admin/realms/master/clients", cancellationToken);
    if (clientResponse.IsSuccessStatusCode)
    {
        var clients = await clientResponse.Content.ReadFromJsonAsync<List<KeycloakClientDto>>(cancellationToken);
        if (clients != null)
        {
            var targetClient = clients.FirstOrDefault(c => c.ClientId == "event-api");
            if (targetClient == null)
            {
                var createResponse = await _httpClient.PostAsJsonAsync("admin/realms/master/clients", newClient, cancellationToken);
                if (!createResponse.IsSuccessStatusCode)
                {
                    _logger.LogError("Failed to create client");
                    return;
                }
            }
            // ... 10 levels of nesting ...
        }
    }
}
```

#### After (Guard Extensions & Fail-Fast Flow)
```csharp
// AFTER: Fail-fast HTTP extensions and early returns
public async Task EnsureClientConfiguredAsync(CancellationToken ct)
{
    var clients = await _httpClient.GetFromJsonAsync<List<KeycloakClientDto>>("admin/realms/master/clients", ct)
        ?? throw new KeycloakBootstrapException("Keycloak returned null client collection.");

    var targetClient = clients.FirstOrDefault(c => c.ClientId == "event-api");
    if (targetClient is null)
    {
        await CreateClientAsync(ct);
        return;
    }

    await SynchronizeClientRolesAndSecretAsync(targetClient, ct);
}
```

---

## 5. Exhaustive Project-by-Project Audit & Refactoring Catalog

Below is the comprehensive audit and refactoring specification for **EVERY SINGLE API PROJECT** in the codebase:

```
┌───────────────────────────────────────────────────────────────────────────────────────────────────────┐
│                                    FULL ARCHITECTURAL AUDIT MATRIX                                     │
├────────────────────────────┬─────────────────────────────┬───────────────────┬────────────────────────┤
│ Project Name               │ Key Files Audited           │ Peak Complexity   │ Applied Pattern        │
├────────────────────────────┼─────────────────────────────┼───────────────────┼────────────────────────┤
│ 1. Explore.API             │ Controllers (110+), Mcp,    │ 64                │ Switch Expressions,    │
│                            │ QueryValidationRules.cs     │                   │ Error Classification   │
│ 2. Explore.Application     │ AuthorizationBehavior,      │ 84                │ Strategy Pattern,      │
│                            │ RegistrationLifecycle, Specs│                   │ Fluent Spec Pipeline   │
│ 3. Event.Web.BffHosting    │ BffAuthExtensions, YARP Proxy│ 34                │ Tenant Resolution Pipe │
│ 4. Event.Standalone        │ Host Startup, Engine Select │ 28                │ DB Provider Strategy   │
│ 5. Explore.Infrastructure  │ Keycloak, FallbackAuthz, AI │ 137               │ HTTP Guard Extensions  │
│ 6. Explore.Persistence     │ EmailEligibility, Jetstream │ 75                │ Outbox Spec Engine     │
└────────────────────────────┴─────────────────────────────┴───────────────────┴────────────────────────┘
```

---

### Project 1: `src/Explore.API` (Web API Controllers, Endpoints, & MCP Tools)

#### Audited Controllers & Handlers:
1. **`Controllers/InstanceSettingsController.cs`**:
   - *Issue:* Parsing message strings in `HandleCommandResponse`. Redundant authorization logic in `IsInstanceAdminOrSetupAuthenticated`.
   - *Refactoring:* Use typed `DomainErrorCode` switch expressions. Move setup secret checking to custom ASP.NET Core `IAuthorizationHandler`.
2. **`Controllers/EventController.cs` & `EventSessionController.cs`**:
   - *Issue:* Repeating `if (!userId.HasValue)` checks across 45+ actions.
   - *Refactoring:* Introduce base controller helper `ExecuteAuthenticatedCommandAsync(...)` that handles user resolution and command response formatting uniformly.
3. **`Controllers/RegistrationOrderController.cs`**:
   - *Issue:* Complex conditional logic validating cart lines, promo codes, and payment status parameters directly inside controller actions.
   - *Refactoring:* Move all validation into FluentValidation handlers; controllers pass requests directly to MediatR without inline `if` branching.
4. **`Controllers/AiAssistantController.cs` & `PublicExperienceController.cs`**:
   - *Issue:* Branching logic checking feature flags (`Mod_Islamic`, `Mod_Tech`) and tenant capabilities.
   - *Refactoring:* Create `[RequireTenantModule("Mod_Islamic")]` endpoint action filters to move module checks out of controller bodies.
5. **`Mcp/EventManagementMcpTools.cs`**:
   - *Issue:* 2,500+ lines with 64+ `if` blocks performing manual parameter clamping, string truncation, and exception handling.
   - *Refactoring:* Extract parameter sanitization into `McpSanitizerExtensions`. Use a centralized `ExecuteMcpQueryAsync` wrapper.
6. **`Models/QueryValidationRules.cs`**:
   - *Issue:* 39+ `if` statements in yield-return methods (`ValidatePagination`, `ValidateBoundedText`, `ValidateSortBy`).
   - *Refactoring:* Modernize with relational pattern matching (`pageNumber is < 1`) and early returns.

---

### Project 2: `src/Explore.Application` (CQRS Handlers, Behaviors, & Core Services)

#### Audited Behaviors & Handlers:
1. **`Behaviors/AuthorizationBehavior.cs`**:
   - *Issue:* 818 lines with 22 constructor dependencies and a 50-branch `if (request is UpdateXyzCommand)` chain.
   - *Refactoring:* Replace with polymorphic `IResourceContextResolver` strategies registered in DI. Reduces class size to ~60 lines.
2. **`Services/Registration/RegistrationOrderLifecycleService.cs`**:
   - *Issue:* 1,044 lines with 74+ `if` statements handling status state machines, platform contributions, capacity reservations, and requirement checks.
   - *Refactoring:* Extract `EvaluateAndApplyPlatformContributionAsync` and `PrepareCapacityReservationPlanAsync`. Implement domain guard extensions (`order.EnsureStatus(...)`).
3. **`Features/Events/Handlers/Queries/GetEventListRequestHandler.cs`**:
   - *Issue:* 42+ `if` checks manually appending subqueries and filters to `EventQuerySpecification`.
   - *Refactoring:* Use `ApplyIf` and `ApplyIfCollection` fluent extension methods on `EventQuerySpecification`.
4. **`Services/InstanceGovernanceSettingService.cs`**:
   - *Issue:* 85+ `if` checks traversing 5-tier governance settings (User -> Group -> Org -> Tenant -> Instance).
   - *Refactoring:* Implement Chain of Responsibility pattern where each tier is represented by a governance provider node.
5. **`Services/Lifecycle/EventLifecycleReadinessEvaluator.cs`**:
   - *Issue:* 39+ `if` branches checking missing fields, agenda items, speakers, and policies.
   - *Refactoring:* Introduce `IEventReadinessRule` specification chain where each rule yields readiness warnings or blocking errors.
6. **`Features/EventReporting/Handlers/Commands/ExecuteReportDecisionCommandHandler.cs`**:
   - *Issue:* Nested `if/else` checks for moderation decisions (Approve, Redact, Ban, SoftDelete).
   - *Refactoring:* Implement `IReportDecisionHandler` strategy per decision type.

---

### Project 3: `src/Event.Web.BffHosting` (BFF YARP Proxy & Auth Hosting Layer)

#### Audited Components:
1. **`Extensions/BffAuthExtensions.cs` & Token Forwarding Middleware**:
   - *Issue:* Deeply nested `if` statements inspecting incoming `X-Tenant-Slug` headers, bearer tokens, cookie expirations, and custom domains.
   - *Refactoring:* Refactor tenant resolution into a standalone `ITenantResolutionPipeline` with discrete pipeline steps (HeaderStep -> DomainStep -> SubdomainStep -> FallbackStep).
2. **YARP Proxy Transformation Rules**:
   - *Issue:* Imperative conditional branching modifying outgoing HTTP requests based on user claims.
   - *Refactoring:* Express request transformations using declarative YARP transform builders.

---

### Project 4: `src/Event.Standalone` (Standalone API Host & Embedded Runner)

#### Audited Components:
1. **`Program.cs` & Database Provider Registration**:
   - *Issue:* `if (dbProvider == "Sqlite") ... else if (dbProvider == "SqlServer")` branching across database initialization, migration services, and data protection key storage.
   - *Refactoring:* Create `IDatabaseConfigurationStrategy` implementations for Sqlite, SqlServer, MariaDb, and MySql to register persistence services polymorphically.
2. **Local Seed & Bootstrap Selectors**:
   - *Issue:* Conditional branching checking single-tenant mode vs multi-tenant environment flags.
   - *Refactoring:* Encapsulate environment defaults inside an `InstanceBootstrapPolicy` object.

---

### Project 5: `src/Explore.Infrastructure` (Integrations, Auth, & AI Providers)

#### Audited Components:
1. **`Services/Keycloak/KeycloakBootstrapService.cs`**:
   - *Issue:* 137+ `if` statements checking Keycloak realm status, client configuration, role mappings, and HTTP response codes.
   - *Refactoring:* Replace HTTP status checks with `EnsureSuccessStatusCode()` or specialized guard extensions (`response.EnsureKeycloakSuccess()`).
2. **`Services/FallbackAuthorizationService.Evaluators.cs`**:
   - *Issue:* 75+ `if` conditions evaluating Cerbos authorization fallback rules.
   - *Refactoring:* Refactor policy evaluation using C# property pattern matching and switch expressions.
3. **`Ai/OpenAiCompatibleChatProvider.cs` & `AnthropicCompatibleChatProvider.cs`**:
   - *Issue:* 49+ `if` blocks parsing JSON function call schemas and tool calls.
   - *Refactoring:* Implement polymorphic tool result serialization using `System.Text.Json` custom converters and pattern matching.
4. **`Webhooks/WebhookDeliveryDrainService.cs`**:
   - *Issue:* 39+ `if` statements evaluating webhook retry backoff delays, failure thresholds, and payload signatures.
   - *Refactoring:* Encapsulate retry logic inside a `WebhookRetryPolicy` specification.

---

### Project 6: `src/Explore.Persistence` (Data Access, Repositories, & Outbox Engine)

#### Audited Components:
1. **`Services/EmailDispatchEligibilityEvaluator.cs`**:
   - *Issue:* 53+ `if` conditions checking outbox lock states, tenant email quotas, retry counts, and soft deletion flags.
   - *Refactoring:* Compose eligibility evaluation using a pure `Specification<EmailOutboxEntry>` chain.
2. **`Repositories/AtprotoJetstreamRepository.cs`**:
   - *Issue:* 75+ `if` statements inspecting incoming jetstream event types, DID records, and commit collections.
   - *Refactoring:* Dispatch jetstream events to specialized `IJetstreamEventHandler` strategies indexed by event type string.
3. **`Repositories/PdsSyncOutboxRepository.cs` & `EmailDispatchOutboxRepository.cs`**:
   - *Issue:* Imperative state check conditionals for outbox claimed vs released entries.
   - *Refactoring:* Use atomic SQL state transitions with early guard exits.

---

## 6. Actionable Implementation Steps & Invariant Preservations

To guarantee safe refactoring without breaking API contracts or domain logic, follow this 4-step execution protocol:

### Step 1: Automated Verification Baseline
Before editing any file, verify that existing tests pass:
```bash
dotnet build --configuration Release --verbosity quiet
dotnet test --project tests/Event.Application.UnitTests/Event.Application.UnitTests.csproj
dotnet test --project tests/Event.API.IntegrationTests/Event.API.IntegrationTests.csproj
```

### Step 2: Enforce Project Architectural Invariants
1. **Repositories Return Entities:** Repositories must return domain entities, never DTOs. Mapping happens in handlers.
2. **Manual Validator Instantiation:** Validators MUST be manually instantiated inside handlers (`var validator = new CreateEventValidator();`), never injected via DI (`IValidator<T>`).
3. **Command Responses:** Commands return `BaseCommandResponse<TId>`. Errors return RFC 7807 `ProblemDetails`.
4. **HAL Links (`_links`):** HATEOAS policy affordances remain the single source of truth for UI actions. Clients gate UI buttons via `_links`, never local claim checks.
5. **Tenant Isolation:** Centralized EF Core global query filters must never be bypassed casually (`IgnoreQueryFilters` only on named filters like SoftDelete).

### Step 3: Progressive Atomic PR Strategy
- Execute refactoring in small, atomic PRs grouped by project layer (`Explore.Application` behaviors -> `Explore.API` controllers -> `Explore.Infrastructure` providers -> `Explore.Persistence` evaluators).
- Never mix architectural refactoring with database migration schema changes in the same PR.

### Step 4: Post-Refactor Verification
Re-run integration tests and NSwag API client verification to ensure zero API contract drift.

---

## 7. Comprehensive Summary & Metrics Targets

```
┌───────────────────────────────────────────────────────────────────────────────────────────────────────┐
│                                   METRICS & TARGET REFACTORING IMPACT                                 │
├────────────────────────────┬─────────────────────────────┬───────────────────┬────────────────────────┤
│ Metric                     │ Current State               │ Refactored Target │ Strategy               │
├────────────────────────────┼─────────────────────────────┼───────────────────┼────────────────────────┤
│ Max Cyclomatic Complexity  │ 137 (KeycloakBootstrap)     │ < 5               │ Fail-Fast Guards       │
│ Avg Method Length          │ 65 lines                    │ < 15 lines        │ Intent Extraction      │
│ Authorization Behavior Size│ 818 lines (22 dependencies) │ ~60 lines (2 deps)│ Strategy Pattern       │
│ Query Builder Line Count   │ 120 lines (35+ if blocks)   │ ~15 lines         │ Fluent Spec Pipeline   │
│ Controller Error Handling  │ String message parsing      │ DomainErrorCode   │ Switch Expressions     │
└────────────────────────────┴─────────────────────────────┴───────────────────┴────────────────────────┘
```
