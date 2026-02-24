ABOUTME: Common Clean Architecture violations.
ABOUTME: Use with dependency-rules.md and fix-patterns.md.

# Common Violations

## Top Violations
- **Domain references EF/Core or Application** → remove and move to Persistence.
- **Application uses DbContext** → introduce repository interface in Application.
- **Application uses ASP.NET Core types** → return `BaseCommandResponse<T>` and map in controller.
- **Domain uses DataAnnotations** (except `[ForeignKey]`) → move to FluentValidation.
- **Infrastructure references API/Blazor** → pass data in, don’t call controllers.
- **Circular project references** → break via interfaces in Application.

## Fix References
- [dependency-rules.md](dependency-rules.md)
- [fix-patterns.md](fix-patterns.md)
