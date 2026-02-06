# .NET 10, C# 14, EF Core 10 & bUnit 2.x Research Report

> **Research Date**: February 2026
> **Purpose**: Identify features relevant to the ISLAMU Explore Blazor Clean Architecture application
> **Status**: Research-only (no implementation)

---

## Table of Contents

1. [.NET 10 / Blazor Features](#1-net-10--blazor-features)
2. [C# 14 Language Features](#2-c-14-language-features)
3. [EF Core 10 Features](#3-ef-core-10-features)
4. [bUnit 2.x for .NET 10](#4-bunit-2x-for-net-10)
5. [Security Best Practices](#5-blazor-net-10-security-best-practices)
6. [Actionable Recommendations](#6-actionable-recommendations)

---

## 1. .NET 10 / Blazor Features

### 1.1 `[PersistentState]` Declarative Attribute (HIGH IMPACT)

Blazor .NET 10 introduces a declarative `[PersistentState]` attribute that drastically simplifies state persistence during prerendering. Previously, persisting state required ~20 lines of boilerplate code (`PersistentComponentState`, `RegisterOnPersisting`, `IDisposable`). Now:

```csharp
// BEFORE (.NET 9) - verbose
@implements IDisposable
@inject PersistentComponentState ApplicationState

@code {
    public List<Movie>? MoviesList { get; set; }
    private PersistingComponentStateSubscription? persistingSubscription;

    protected override async Task OnInitializedAsync()
    {
        if (!ApplicationState.TryTakeFromJson<List<Movie>>(nameof(MoviesList), out var movies))
            MoviesList = await MovieService.GetMoviesAsync();
        else
            MoviesList = movies;

        persistingSubscription = ApplicationState.RegisterOnPersisting(() =>
        {
            ApplicationState.PersistAsJson(nameof(MoviesList), MoviesList);
            return Task.CompletedTask;
        });
    }

    public void Dispose() => persistingSubscription?.Dispose();
}

// AFTER (.NET 10) - declarative
@code {
    [PersistentState]
    public List<Movie>? MoviesList { get; set; }

    protected override async Task OnInitializedAsync()
    {
        MoviesList ??= await MovieService.GetMoviesAsync();
    }
}
```

**Additional options:**
- `[PersistentState(AllowUpdates = true)]` - allows state updates during enhanced navigation (for read-only cached data)
- `[PersistentState(RestoreBehavior = RestoreBehavior.SkipInitialValue)]` - skip restore during prerender
- `[PersistentState(RestoreBehavior = RestoreBehavior.SkipLastSnapshot)]` - skip restore on reconnect (ensures fresh data)
- Services can be registered as persistent: `RegisterPersistentService<TService>()` on the Razor components builder

**Relevance to Explore**: Any component that fetches data on init and needs prerendering support can benefit. Reduces boilerplate in all interactive pages.

### 1.2 NotFoundPage Parameter on Router (HIGH IMPACT)

The `Router` component now has a `NotFoundPage` parameter. Combined with `NavigationManager.NotFound()`:

```csharp
<Router AppAssembly="@typeof(Program).Assembly" NotFoundPage="typeof(Pages.NotFound)">
    <Found Context="routeData">
        <RouteView RouteData="@routeData" />
        <FocusOnNavigate RouteData="@routeData" Selector="h1" />
    </Found>
</Router>
```

- Works with `UseStatusCodePagesWithReExecute` middleware
- Supports static SSR and global interactive rendering
- The `<NotFound>` render fragment is deprecated in .NET 10

**Relevance to Explore**: Proper 404 handling across all render modes. Should adopt `NotFoundPage` pattern.

### 1.3 Reconnection UI Component (MEDIUM IMPACT)

The Blazor Web App template now includes a `ReconnectModal` component with collocated CSS/JS for improved reconnection UX. Key improvements:
- Doesn't insert styles programmatically (CSP-compliant for `style-src`)
- New `components-reconnect-state-changed` event
- New `retrying` reconnection state
- Falls back to default UI if no custom component is defined

**Relevance to Explore**: Adopt the template's `ReconnectModal` for CSP compliance.

### 1.4 Improved Form Validation (HIGH IMPACT)

Source-generator-based validation replaces reflection-based validation:

```csharp
// Program.cs
builder.Services.AddValidation();

// Model class (NOT in .razor files - must be in .cs files)
[ValidatableType]
public class Order
{
    public Customer Customer { get; set; } = new();
    public List<OrderItem> OrderItems { get; set; } = [];
}
```

Key improvements:
- **Nested object validation** - validates complex objects and collections
- **Source-generator based** - AOT compatible, better performance
- **`[SkipValidation]`** attribute to exclude properties/types
- **`[ValidatableType]`** required on root model type
- Model types must be in `.cs` files (not `.razor` files) due to source generator limitations

**Relevance to Explore**: Can validate nested DTOs (e.g., Event with nested Address, Schedule). Works with existing `DataAnnotationsValidator`.

### 1.5 QuickGrid Improvements (MEDIUM IMPACT)

- **`RowClass` parameter**: Conditionally apply CSS classes to rows based on item data
- **`HideColumnOptionsAsync()`**: Programmatically close column options UI

```csharp
<QuickGrid ... RowClass="GetRowCssClass">
@code {
    private string GetRowCssClass(MyGridItem item) =>
        item.IsArchived ? "row-archived" : null;
}
```

### 1.6 Circuit State Persistence (MEDIUM IMPACT)

Blazor can now persist circuit state when WebSocket connection is lost:
- Browser tab throttling
- Mobile app switching
- Network interruptions
- Proactive resource management (pausing inactive circuits)

Users resume sessions without losing unsaved work. No full-page refresh needed.

### 1.7 JS Interop Improvements (MEDIUM IMPACT)

New JS interop capabilities:
- **`InvokeConstructorAsync`** - Create JS objects with `new` and get `IJSObjectReference`
- **`GetValueAsync<T>`** - Read JS property values
- **`SetValueAsync<T>`** - Write JS property values
- Sync versions available for in-process scenarios

### 1.8 Other Notable Blazor Changes

| Feature | Impact | Notes |
|---------|--------|-------|
| `NavigateTo` no longer scrolls to top for same-page nav | Low | Good for query string changes |
| `NavLinkMatch.All` ignores query/fragment | Low | Prevents active class loss on query changes |
| HttpClient response streaming enabled by default | Medium | Breaking: `ReadAsStreamAsync` returns `BrowserHttpReadStream` not `MemoryStream` |
| Blazor script served as static web asset | Low | Automatic compression + fingerprinting |
| Client-side fingerprinting for WASM | Medium | Opt-in with `OverrideHtmlAssetPlaceholders` |
| `ResourcePreloader` component | Low | Replaces `<link>` headers for WASM preloading |
| `BlazorDisableThrowNavigationException` | Medium | Consistent SSR/interactive navigation behavior |
| Metrics and tracing for Blazor | High | Observability: component lifecycle, navigation, events, circuit |
| `OwningComponentBase` implements `IAsyncDisposable` | Low | Better async resource cleanup |
| `InputHidden` component | Low | Hidden form fields |
| Passkey/WebAuthn support | Medium | Phishing-resistant auth via FIDO2 |
| Serialization extensibility for persistent state | Low | Custom `PersistentComponentStateSerializer<T>` |

### 1.9 Minimal APIs / ASP.NET Core

- **Server-Sent Events (SSE)** support via `TypedResults.ServerSentEvents`
- **Validation support** in Minimal APIs with `builder.Services.AddValidation()`
- **`LeftJoin` / `RightJoin`** LINQ operators (first-class support)
- **Empty string → null** for nullable value types in `[FromForm]`

---

## 2. C# 14 Language Features

### 2.1 Extension Members (HIGH IMPACT)

C# 14 introduces full extension types — not just extension methods. You can now declare extension properties, static extension members, and user-defined operators:

```csharp
public static class Enumerable
{
    // Instance extension members
    extension<TSource>(IEnumerable<TSource> source)
    {
        // Extension PROPERTY (new!)
        public bool IsEmpty => !source.Any();
        
        // Extension method (existing, new syntax)
        public IEnumerable<TSource> Where(Func<TSource, bool> predicate) { ... }
    }

    // Static extension members
    extension<TSource>(IEnumerable<TSource>)
    {
        public static IEnumerable<TSource> Identity => Enumerable.Empty<TSource>();
        
        // Extension OPERATOR (new!)
        public static IEnumerable<TSource> operator +(
            IEnumerable<TSource> left, IEnumerable<TSource> right) => left.Concat(right);
    }
}
```

**Relevance to Explore**: 
- Extension properties on domain entities (e.g., `event.IsUpcoming`, `event.FormattedDate`)
- Cleaner domain logic without polluting entity classes
- Extension properties on `ClaimsPrincipal` for user helpers

### 2.2 `field` Keyword (HIGH IMPACT)

Auto-property backing field access without declaring the field:

```csharp
// BEFORE
private string _msg;
public string Message
{
    get => _msg;
    set => _msg = value ?? throw new ArgumentNullException(nameof(value));
}

// AFTER (C# 14)
public string Message
{
    get;
    set => field = value ?? throw new ArgumentNullException(nameof(value));
}
```

**Relevance to Explore**:
- Domain entity property validation without backing fields
- Cleaner ViewModel property setters with `StateHasChanged` triggers
- Reduces boilerplate in DTOs with validation logic

### 2.3 Null-Conditional Assignment (HIGH IMPACT)

```csharp
// BEFORE
if (customer is not null)
{
    customer.Order = GetCurrentOrder();
}

// AFTER (C# 14)
customer?.Order = GetCurrentOrder();

// Also works with compound assignment
customer?.Score += 10;
```

The right side is evaluated only when the left side isn't null.

**Relevance to Explore**: Simplifies null-checking throughout handlers, services, and components.

### 2.4 Implicit Span Conversions (MEDIUM IMPACT)

First-class `Span<T>` and `ReadOnlySpan<T>` support:
- Implicit conversions between `T[]`, `Span<T>`, and `ReadOnlySpan<T>`
- Better generic type inference
- Extension method receivers

**Relevance to Explore**: Performance improvements in string processing, data transformations. Useful in hot paths.

### 2.5 Simple Lambda Parameters with Modifiers (MEDIUM IMPACT)

```csharp
// BEFORE - had to specify types with modifiers
TryParse<int> parse = (string text, out int result) => Int32.TryParse(text, out result);

// AFTER - types inferred even with modifiers
TryParse<int> parse = (text, out result) => Int32.TryParse(text, out result);
```

### 2.6 `nameof` with Unbound Generic Types (LOW IMPACT)

```csharp
nameof(List<>)  // Returns "List" - previously required nameof(List<int>)
```

### 2.7 Partial Constructors and Events (MEDIUM IMPACT)

Constructors and events can now be partial members. Useful for source generators.

### 2.8 User-Defined Compound Assignment (LOW IMPACT)

Custom types can now define compound assignment operators (`+=`, `-=`, etc.).

### Summary of C# 14 Features by Impact

| Feature | Impact | Primary Use Case |
|---------|--------|-----------------|
| Extension members | High | Domain extensions, utility properties |
| `field` keyword | High | Property validation, reduced boilerplate |
| Null-conditional assignment | High | Null-safe property assignment everywhere |
| Implicit Span conversions | Medium | Performance in hot paths |
| Lambda parameter modifiers | Medium | Cleaner delegate usage |
| Partial constructors/events | Medium | Source generator scenarios |
| `nameof` unbound generics | Low | Logging, error messages |
| Compound assignment operators | Low | Custom numeric types |

---

## 3. EF Core 10 Features

### 3.1 Named Query Filters (HIGH IMPACT - Already in CLAUDE.md!)

EF Core 10 introduces named query filters, which our project already references in CLAUDE.md Rule #11:

```csharp
modelBuilder.Entity<Blog>()
    .HasQueryFilter("SoftDelete", b => !b.IsDeleted)
    .HasQueryFilter("TenantFilter", b => b.TenantId == tenantId);

// Selectively disable specific filters
var allBlogs = await context.Blogs
    .IgnoreQueryFilters(["SoftDelete"])
    .ToListAsync();
```

**Relevance to Explore**: This is already specified as a project convention. Enables:
- Soft delete filter: `.HasQueryFilter("SoftDelete", e => !e.IsDeleted)`
- Tenant filter if multi-tenancy is added
- Admin views that selectively bypass soft-delete

### 3.2 Complex Types Improvements (HIGH IMPACT)

Major improvements to complex types:
- **Optional complex types** (`Address?`) now supported
- **JSON mapping** for complex types: `.ComplexProperty(c => c.Address, c => c.ToJson())`
- **Struct support** for complex types
- **`ExecuteUpdateAsync` support** for JSON complex types
- Complex types have **value semantics** (unlike owned entities)

```csharp
// Complex type in JSON column
modelBuilder.Entity<Customer>(b =>
{
    b.ComplexProperty(c => c.ShippingAddress, c => c.ToJson());
});

// Bulk update JSON properties
await context.Blogs.ExecuteUpdateAsync(s =>
    s.SetProperty(b => b.Details.Views, b => b.Details.Views + 1));
```

**Relevance to Explore**: Event metadata, location data, or schedule details could be modeled as complex types in JSON columns.

### 3.3 LeftJoin / RightJoin LINQ Operators (HIGH IMPACT)

First-class LINQ support for LEFT/RIGHT JOIN:

```csharp
// BEFORE (.NET 9) - complex GroupJoin + SelectMany + DefaultIfEmpty
var query = context.Students
    .GroupJoin(context.Departments, s => s.DepartmentID, d => d.ID, (s, depts) => new { s, depts })
    .SelectMany(x => x.depts.DefaultIfEmpty(), (x, d) => new { ... });

// AFTER (.NET 10) - clean and readable
var query = context.Students
    .LeftJoin(
        context.Departments,
        student => student.DepartmentID,
        department => department.ID,
        (student, department) => new 
        { 
            student.FirstName,
            Department = department.Name ?? "[NONE]"
        });
```

**Relevance to Explore**: Simplifies queries joining Events with optional Members, Categories, etc.

### 3.4 Improved Parameterized Collections (HIGH IMPACT)

New default: each collection value becomes its own SQL parameter with "padding":

```sql
-- .NET 10 default (new)
SELECT * FROM Blogs WHERE Id IN (@ids1, @ids2, @ids3)

-- .NET 9 default (JSON)
SELECT * FROM Blogs WHERE Id IN (SELECT value FROM OPENJSON(@ids))

-- Override per-query
var blogs = await context.Blogs.Where(b => EF.Constant(ids).Contains(b.Id)).ToListAsync();
```

Better query plan caching and cardinality information for the database planner.

### 3.5 ExecuteUpdateAsync with Regular Lambda (HIGH IMPACT)

No more expression tree manipulation for conditional updates:

```csharp
// BEFORE - manual Expression tree manipulation (complex and error-prone)
Expression<Func<SetPropertyCalls<Blog>, SetPropertyCalls<Blog>>> setters = ...;

// AFTER (.NET 10) - regular lambda with conditionals
await context.Blogs.ExecuteUpdateAsync(s =>
{
    s.SetProperty(b => b.Views, 8);
    if (nameChanged)
    {
        s.SetProperty(b => b.Name, "foo");
    }
});
```

### 3.6 Security Improvements

- **Redacted inlined constants** in logging by default (prevents PII leaks)
- **SQL injection analyzer** warns on string concatenation in `FromSqlRaw`

### 3.7 Vector Search (SQL Server 2025 / Azure SQL)

Full support for `vector` data type and `VECTOR_DISTANCE()` for AI/embedding workloads:

```csharp
[Column(TypeName = "vector(1536)")]
public SqlVector<float> Embedding { get; set; }
```

### 3.8 JSON Data Type (SQL Server 2025 / Azure SQL)

Native `json` column type instead of `nvarchar(max)`:
- Primitive collections and complex types auto-map to `json`
- `JSON_VALUE` with `RETURNING` clause for typed extraction
- Existing `nvarchar` JSON columns auto-migrate to `json` type

### 3.9 Other EF Core 10 Improvements

| Feature | Notes |
|---------|-------|
| Custom default constraint names | `HasDefaultValueSql("GETDATE()", "DF_Post_CreatedDate")` |
| Named default constraints | `UseNamedDefaultConstraints()` for auto-naming |
| Split query ordering fix | Consistent ordering across split queries |
| `DateOnly.ToDateTime()` translation | New SQL translation |
| `COALESCE` → `ISNULL` on SQL Server | Optimization |
| Simplified parameter names | `@city` instead of `@__city_0` |
| Cosmos DB full-text search | `FullTextContains`, `FullTextScore` |
| Cosmos hybrid search | `Rrf()` combining vector + full-text |

---

## 4. bUnit 2.x for .NET 10

### 4.1 Version Status

**Latest**: bUnit 2.5.3 (released January 8, 2026)

Major version 2.0 was released November 21, 2025 targeting .NET 10:
- **Dropped** support for all versions prior to .NET 8
- **Added** `net10.0` target framework
- API cleanup and simplifications
- Improved renderer logic for edge cases
- Improved JSInterop developer experience

### 4.2 Key bUnit 2.x Features

| Version | Feature |
|---------|---------|
| 2.5.3 | `FindByTestId` for querying elements by test ID |
| 2.5.3 | `Render(RenderFragment)` preferred via `OverloadResolutionAttribute` |
| 2.3.4 | Generic `Find<TComponent, TElement>` and `FindAll<TComponent, TElement>` |
| 2.3.4 | Generic `WaitForElement<TComponent, TElement>` and `WaitForElements<TComponent, TElement>` |
| 2.2.2 | `FindByAllByLabel` in `bunit.web.query` |
| 2.1.1 | `AuthenticationState` registered in services container (not RenderTree) |
| 2.0.66 | Form submission from buttons outside form via HTML5 `form` attribute |
| 1.37.7 | Support for `RendererInfo` and `AssignedRenderMode` (.NET 9+) |
| 1.38.5 | xUnit v3 support in bunit.template |

### 4.3 Testing Patterns for .NET 10

**Testing Render Modes**: bUnit 1.37.7+ supports `RendererInfo` and `AssignedRenderMode`, essential for testing InteractiveAuto components.

**Testing Authentication**: bUnit 2.1.1 moved `AuthenticationState` to the services container, matching .NET 10's `AddAuthenticationStateSerialization` pattern.

**Test Runner Compatibility**: 
- xUnit v3 supported (template since 1.38.5)
- TUnit not explicitly listed but bUnit is test-runner agnostic (based on `TestContext` not test framework)

### 4.4 Relevance to Explore

The project uses bUnit with TUnit (per `Explore.Blazor.Client.Tests`). Key upgrade considerations:
1. Upgrade to bUnit 2.5.x for .NET 10 support
2. Use `FindByTestId` for more resilient component queries
3. Generic `Find<TComponent, TElement>` for type-safe element queries
4. `AuthenticationState` in services aligns with our auth testing patterns

---

## 5. Blazor .NET 10 Security Best Practices

### 5.1 CSRF/XSRF Protection

**Automatic in Blazor**: Antiforgery services are added when `AddRazorComponents` is called. The middleware is added via `UseAntiforgery()`.

Key points:
- `AntiforgeryToken` component auto-added to `EditForm` instances
- `AntiforgeryStateProvider` provides tokens for manual AJAX calls
- Tokens stored in component state (available to interactive components)
- Only required for `application/x-www-form-urlencoded`, `multipart/form-data`, or `text/plain` enctypes
- API endpoints (JSON) don't need antiforgery if using Bearer tokens

**Configuration options**:
```csharp
builder.Services.AddAntiforgery(options =>
{
    options.FormFieldName = "AntiforgeryFieldname";
    options.HeaderName = "X-CSRF-TOKEN-HEADERNAME";
    options.SuppressXFrameOptionsHeader = false;
});

// Secure cookie in non-Development
if (!builder.Environment.IsDevelopment())
{
    builder.Services.AddAntiforgery(o =>
        o.Cookie.SecurePolicy = CookieSecurePolicy.Always);
}
```

### 5.2 Authentication State (.NET 10)

**Server → Client flow**:
```csharp
// Server Program.cs
builder.Services.AddRazorComponents()
    .AddInteractiveWebAssemblyComponents()
    .AddAuthenticationStateSerialization(
        options => options.SerializeAllClaims = true);

// Client Program.cs  
builder.Services.AddAuthorizationCore();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddAuthenticationStateDeserialization();
```

**Revalidation**: `IdentityRevalidatingAuthenticationStateProvider` revalidates security stamp every 30 minutes. Adjustable:
```csharp
protected override TimeSpan RevalidationInterval => TimeSpan.FromMinutes(20);
```

### 5.3 Passkey/WebAuthn Support (NEW in .NET 10)

ASP.NET Core Identity now supports passkey authentication:
- Based on WebAuthn and FIDO2 standards
- Phishing-resistant, device-based authentication
- Blazor Web App template includes passkey management UI
- Biometrics and security keys supported

### 5.4 Reconnection UI and CSP Compliance

The new `ReconnectModal` component:
- Doesn't inject styles programmatically
- Complies with `style-src` CSP directives
- Framework's default reconnection UI could cause CSP violations (before .NET 10)

### 5.5 Content Security Policy Recommendations

```
Content-Security-Policy:
  default-src 'self';
  script-src 'self' 'wasm-unsafe-eval';
  style-src 'self';
  connect-src 'self' wss:;
  img-src 'self' data:;
  font-src 'self';
  frame-ancestors 'none';
```

- Use `ReconnectModal` component to avoid `style-src 'unsafe-inline'`
- Blazor script fingerprinting prevents cache poisoning
- `X-Frame-Options: SAMEORIGIN` set by default (antiforgery middleware)

### 5.6 Additional Security Patterns

- **Never trust client-side auth checks** - all authorization must be server-enforced
- **Use Secret Manager** for local development credentials
- **Managed Identities** for Azure services (no credentials in code)
- **Temporary Redirection URL validity** configurable (default 5 minutes)
- **EF Core 10** redacts inlined constants from SQL logging by default
- **SQL injection analyzer** warns on string concatenation in raw SQL methods

---

## 6. Actionable Recommendations

### Priority 1: High-Impact, Low-Effort Adoptions

| # | Item | Effort | Impact |
|---|------|--------|--------|
| 1 | **Adopt `[PersistentState]`** on all components that persist prerender state | Low | High - eliminates boilerplate |
| 2 | **Use Named Query Filters** (already in CLAUDE.md rules) | Low | High - flexible soft-delete |
| 3 | **Use `field` keyword** in property setters with validation | Low | Medium - cleaner code |
| 4 | **Use null-conditional assignment** (`?.=`) throughout handlers | Low | Medium - cleaner null-checks |
| 5 | **Upgrade bUnit to 2.5.x** | Low | High - .NET 10 support |
| 6 | **Adopt `NotFoundPage` on Router** | Low | Medium - proper 404 handling |

### Priority 2: Medium-Effort Improvements

| # | Item | Effort | Impact |
|---|------|--------|--------|
| 7 | **Adopt `ReconnectModal`** component from template | Medium | Medium - CSP compliance |
| 8 | **Use `AddValidation()` + `[ValidatableType]`** for nested form validation | Medium | High - better validation |
| 9 | **Use `LeftJoin` LINQ operator** to simplify complex queries | Medium | Medium - readability |
| 10 | **Adopt extension members** for domain entity helpers | Medium | Medium - cleaner architecture |
| 11 | **Use `ExecuteUpdateAsync` regular lambda** for conditional bulk updates | Medium | Medium - simpler code |
| 12 | **Enable Blazor metrics/tracing** for observability | Medium | High - production insights |

### Priority 3: Architecture Considerations (Discuss First)

| # | Item | Effort | Impact |
|---|------|--------|--------|
| 13 | **Complex types for JSON columns** (replace owned entities) | High | High - better modeling |
| 14 | **Passkey/WebAuthn** adoption for passwordless auth | High | High - security improvement |
| 15 | **Circuit state persistence** configuration | Medium | Medium - mobile UX |
| 16 | **Source-generator validation** migration from reflection | High | Medium - AOT readiness |

### Breaking Changes to Watch

1. **HttpClient response streaming** enabled by default in WASM - `ReadAsStreamAsync` returns `BrowserHttpReadStream` not `MemoryStream`
2. **`<NotFound>` render fragment** deprecated - migrate to `NotFoundPage` parameter
3. **EF Core JSON columns** auto-migrate from `nvarchar(max)` to `json` type (SQL Server 2025)
4. **`NavigationException`** behavior change during static SSR (opt-in via MSBuild property)
5. **bUnit 2.x** dropped .NET versions prior to .NET 8

### Questions for Next Steps

1. Which Priority 1 items should we start implementing first?
2. Should we adopt complex types (JSON mapping) for any current owned entities?
3. Is passkey/WebAuthn support a requirement for the project's auth roadmap?
4. Should we target SQL Server 2025 features (native `json`, `vector`) or maintain backward compatibility?
