---
name: blazor-ui-conventions
description: "Load for Blazor/MudBlazor component or page changes involving render modes, dialogs, parameters/events, HAL-gated actions, generated API clients, theming, or component tests; add accessibility or CSS-isolation skills only when those concerns are touched."
type: pattern
enforcement: suggest
priority: high
---
<!-- ABOUTME: Blazor UI conventions for Razor components, MudBlazor v9 usage, render modes, HAL-driven affordances, and shared design-system wrappers. -->
<!-- ABOUTME: Keeps Explore.Blazor and Explore.Blazor.Client aligned with InteractiveAuto, wrapper components, immutable state flows, and BFF-safe UI behavior. -->

## Must-Read Docs
- [../../../docs/BLAZOR.md](../../../docs/BLAZOR.md)
- [../../../docs/ARCHITECTURE.md](../../../docs/ARCHITECTURE.md)
- [../../../docs/DESIGN_SYSTEM.md](../../../docs/DESIGN_SYSTEM.md)
- [../../../docs/ACCESSIBILITY.md](../../../docs/ACCESSIBILITY.md)

## Top 5 Invariants
1. The default render mode is `InteractiveAuto`, so `HttpContext` must not be used in InteractiveAuto or WASM execution paths.
2. MudBlazor v9 APIs are required, including `ShowAsync<T>()`, `ShowMessageBoxAsync`, `CloseAsync`, `<CustomContent>` for `MudFileUpload`, unified `Palette`, and `IConverter<TInput,TOutput>`.
3. HAL `_links` is the single source of truth for action affordances, so edit or delete buttons are gated by link presence rather than role or claim inspection.
4. Child-to-parent communication uses `[Parameter]` and `EventCallback`, and immutable `Range<T>` or `DateRange` values are replaced rather than mutated.
5. Theming flows through the three-tier token system and shared wrappers in `Explore.Blazor.Client/Components/Common/` instead of ad hoc component styling.

## Top 5 Anti-Patterns
1. Using `HttpContext` in InteractiveAuto or WASM code paths causes runtime failures when the component executes outside the server request pipeline.
2. Gating UI action buttons with roles or claims instead of HAL `_links` drifts from the API authorization contract.
3. Using removed MudBlazor v8 APIs such as `Show<T>()`, `ShowMessageBox`, or `<ActivatorContent>` in file uploads causes v9 migration breakage.
4. Calling the API directly from Blazor components instead of going through the BFF bypasses the security and tenancy boundary.
5. Using heavy shadows or `Elevation > 2` for ordinary content cards fights the project visual system and makes wrappers inconsistent.

## Minimal Examples
```csharp
@if (EventDto.HasHalLink("edit"))
{
    <AppButton Variant="Variant.Filled" OnClick="OpenEditDialogAsync">
        Edit
    </AppButton>
}

@code {
    [Parameter] public EventListItemDto EventDto { get; set; } = default!;

    private Task OpenEditDialogAsync() => Task.CompletedTask;
}
```

```csharp
public sealed class EventActions(IDialogService dialogService)
{
    public async Task OpenAsync(Guid eventId)
    {
        var parameters = new DialogParameters<EditEventDialog>
        {
            { x => x.EventId, eventId }
        };

        IDialogReference dialog = await dialogService.ShowAsync<EditEventDialog>(
            "Edit event",
            parameters);

        await dialog.Result;
    }
}
```

## Verification Hooks
- `dotnet test --project tests/Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/BlazorClientArchitectureTests/*" --minimum-expected-tests 1 --no-progress --maximum-parallel-tests 1`
- `dotnet test --project tests/Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --verbosity quiet`
- `dotnet test --project tests/Explore.Blazor.IntegrationTests/Explore.Blazor.IntegrationTests.csproj --configuration Release --verbosity quiet`

## Related Skills
- [../blazor-bff-patterns/SKILL.md](../blazor-bff-patterns/SKILL.md)
- [../blazor-css-isolation/SKILL.md](../blazor-css-isolation/SKILL.md)
- [../design-system/SKILL.md](../design-system/SKILL.md)
- [../auth-patterns/SKILL.md](../auth-patterns/SKILL.md)
