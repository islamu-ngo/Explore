> Previous migration guide: [v7.0.0 Migration Guide](https://github.com/MudBlazor/MudBlazor/discussions/12658), [v8.0.0 Migration Guide](https://github.com/MudBlazor/MudBlazor/discussions/12659).
> 
> MudBlazor version 9.0.0 brings significant *_breaking changes_. This migration guide will help you upgrade from v8 to v9.
> 
> **Note:** Please limit discussion strictly to migration or reporting errors in this guide. For general feedback about version 9.0.0, use the appropriate discussion channels.
> 
> Warning
> 
> Many obsolete APIs marked for removal in v8 have been removed in v9. The compiler will **NOT** always catch these changes at compile time if you're using dynamic invocation or reflection. Ensure thorough testing after migration.
> 
> ## Converters: Complete Rework
> The converter system has been completely redesigned for better performance, type safety, and extensibility. This is one of the most significant breaking changes in v9.
> 
> **Removed:**
> 
> * `Converter<T, U>` class
> * `Converter<T>` class
> * `DefaultConverter` (old implementation)
> * `BoolConverter` (old implementation)
> * `DateConverter`
> * `NumericConverter.AreEqual` method
> * `Converters` static class
> 
> **Replaced with:**
> 
> * `IConverter<TInput, TOutput>` interface
> * `ICultureAwareConverter<TInput, TOutput>` interface
> * `IReversibleConverter<TInput, TOutput>` interface
> * `DefaultConverter<T>` (new implementation)
> * `BoolConverter<T>` (new implementation)
> * `RangeConverter<T>`
> * `DeferredConverter<TInput, TOutput>`
> * `EmptyConverter<TInput, TOutput>`
> * `ConversionResult<T>` for error handling
> * `ConverterExtensions` for fluent API
> * `Conversions` static class for common conversions
> 
> **Breaking Changes:**
> 
> 1. **Custom converters must implement interfaces:**
> 
> **Before (v8):**
> 
> public class MyConverter : Converter<MyType>
> {
>     public MyConverter()
>     {
>         SetFunc = value => value?.ToString() ?? string.Empty;
>         GetFunc = str => MyType.Parse(str);
>     }
> }
> **After (v9):**
> 
> public class MyConverter : IReversibleConverter<MyType, string>
> {
>     public string Convert(MyType input)
>     {
>         // ...
>     }
> 
>     public MyType ConvertBack(string input)
>     {
>         // ...
>     }
> }
> 2. **Inline converters**
> 
> **Before (v8):**
> 
> private Converter<ConverterElement?> _elementConverter = new Converter<ConverterElement?>
> {
> 	SetFunc = value => value?.ToString(),
> 	GetFunc = text => new ConverterElement { Name = text }
> };
> **After (v9):**
> 
> private IConverter<ConverterElement?, string?> _elementConverter = Conversions
> 	.From((ConverterElement? value) => value?.ToString(),
> 		text => new ConverterElement { Name = text });
> 3. **Component Converter property changes:**
> 
> Components like `MudTextField<T>`, `MudNumericField<T>`, etc. still have a `Converter` property, but it now expects the new converter types. The framework provides automatic conversion for most built-in types.
> 
> 4. **GetDefaultConverter() method added (PR [#12365](https://github.com/MudBlazor/MudBlazor/pull/12365)):**
> 
> All components inheriting from `MudFormComponent` must now implement a `GetDefaultConverter()` method instead of setting the converter in the constructor. This provides compile-time safety and allows wrapper components to use `null` for the `Converter` parameter.
> 
> ❌ **Before (v8):**
> 
> public class MyInput : MudFormComponent<MyType, string>
> {
>     public MyInput()
>     {
>         Converter = new DefaultConverter<MyType>
>         {
>             Culture = GetCulture,
>             Format = GetFormat
>         };
>     }
> }
> ✅ **After (v9):**
> 
> public class MyInput : MudFormComponent<MyType, string>
> {
>     // Constructor no longer sets Converter
> 
>     protected override IConverter<MyType?, string?> GetDefaultConverter()
>     {
>         return new DefaultConverter<MyType>
>         {
>             Culture = GetCulture,
>             Format = GetFormat
>         };
>     }
> }
> **Key changes:**
> 
> * **Converter parameter is now nullable**: You can set `Converter` to `null`, and the component will use `GetDefaultConverter()` as fallback
> * **Compile-time safety**: Forgetting to implement `GetDefaultConverter()` now causes a compile error instead of a runtime exception
> * **Better for wrappers**: Wrapper components no longer need to explicitly pass a converter if they want default behavior
> 
> **Important:** The `Converter` parameter is checked first. If it's `null`, `GetDefaultConverter()` is called once and cached. Use `GetConverter()` method (not the `Converter` property) when you need to access the active converter in component logic.
> 
> **Example accessing the converter:**
> 
> // ❌ Don't access Converter directly if it might be null
> var converter = Converter; // May be null!
> 
> // ✅ Use GetConverter() which handles the fallback
> var converter = GetConverter(); // Always returns non-null
> **Migration tip:** If you created custom converters, you'll need to rewrite them to implement the new interfaces. See PR [#12177](https://github.com/MudBlazor/MudBlazor/pull/12177) for detailed examples. If you inherit from `MudFormComponent`, implement `GetDefaultConverter()` (PR [#12365](https://github.com/MudBlazor/MudBlazor/pull/12365)).
> 
> ## Remove All Obsolete/Deprecated Code
> All code marked with `[Obsolete]` or `[Deprecated]` in v8 has been removed in v9.
> 
> **Removed from DialogService:**
> 
> * `Show(Type)` - use `ShowAsync(Type)`
> * `Show<T>()` - use `ShowAsync<T>()`
> * `ShowMessageBox()` - use `ShowMessageBoxAsync()`
> * `ShowForm<T>()` - use `ShowFormAsync<T>()`
> * `Close()` - use `CloseAsync()`
> 
> **Removed from MudDataGrid:**
> 
> * `ExpandAllGroups()` - use `ExpandAllGroupsAsync`
> * `CollapseAllGroups()` - use `CollapseAllGroupsAsync`
> 
> **Removed from MudSelect:**
> 
> * `Clear` - use `ClearAsync`
> 
> **Removed from MudTabs:**
> 
> * `ActivatePanel` - use `ActivatePanelAsync`
> 
> **Removed from MudMenu:**
> 
> * `Stylename`
> 
> **Removed from ElementReferenceExtensions:**
> 
> * `MudDetachBlurEventWithJS` - use `Use mudElementRef.removeOnBlurEvent via js invoke instead`
> 
> More details: [#12142](https://github.com/MudBlazor/MudBlazor/pull/12142)
> 
> ## MudGlobal: Theming Properties Removed
> All theming-related properties have been removed from `MudGlobal`. These experimental properties created maintenance burden and blurred the boundary between behavioral and visual concerns. Use CSS variables, theme configuration, or explicit component parameters instead.
> 
> **Removed from MudGlobal:**
> 
> * `MudGlobal.Rounded` (static property)
> * `MudGlobal.ButtonDefaults.Color` (default: `Color.Default`)
> * `MudGlobal.ButtonDefaults.Variant` (default: `Variant.Text`)
> * `MudGlobal.InputDefaults.ShrinkLabel` (default: `false`)
> * `MudGlobal.InputDefaults.Variant` (default: `Variant.Text`)
> * `MudGlobal.InputDefaults.Margin` (default: `Margin.None`)
> * `MudGlobal.LinkDefaults.Color` (default: `Color.Primary`)
> * `MudGlobal.LinkDefaults.Typo` (default: `Typo.body1`)
> * `MudGlobal.LinkDefaults.Underline` (default: `Underline.Hover`)
> * `MudGlobal.GridDefaults.Spacing` (default: `6`)
> * `MudGlobal.StackDefaults.Spacing` (default: `3`)
> * `MudGlobal.PopoverDefaults.Elevation` (default: `8`)
> 
> **Retained non-theming properties in MudGlobal:**
> 
> * `DialogDefaults.DefaultFocus`
> * `MenuDefaults.HoverDelay`
> * `PopoverDefaults.ModalOverlay`
> * `TooltipDefaults.Delay/Duration`
> * `TransitionDefaults.Delay/Duration`
> * `UnhandledExceptionHandler`
> 
> **Components affected:** All affected components now use hard-coded defaults matching the previous `MudGlobal` default values:
> 
> * `MudButton`, `MudIconButton`, `MudToggleIconButton` - now default to `Color.Default` and `Variant.Text`
> * `MudBaseInput` and all derived inputs (`MudTextField`, `MudNumericField`, etc.) - now default to `Variant.Text`, `Margin.None`, and `ShrinkLabel = false`
> * `MudLink` - now defaults to `Color.Primary`, `Typo.body1`, and `Underline.Hover`
> * `MudGrid` - now defaults to `Spacing = 6`
> * `MudStack` - now defaults to `Spacing = 3`
> * `MudPopover` - now defaults to `Elevation = 8`
> * `MudPicker` and all derived pickers - now default to `Elevation = 8` for the popover
> * Components with `Square`/`Rounded` parameters (`MudAlert`, `MudAvatar`, `MudAvatarGroup`, `MudCard`, `MudDataGrid`, `MudExpansionPanels`, `MudNavMenu`, `MudPaper`, `MudPicker`, `MudPopover`, `MudProgressCircular`, `MudProgressLinear`, `MudSimpleTable`, `MudTable`, `MudTabs`) - no longer respect `MudGlobal.Rounded`
> 
> **Migration:** Users relying on global theming should migrate to:
> 
> 1. **Explicit component parameters** - Set properties directly on each component
> 2. **Theme tokens** - Use theme configuration for colors, typography, and shape
> 3. **Wrapper components** - Create app-specific wrapper components for shared styling
> 4. **CSS** - Apply custom styles via CSS classes or variables
> 
> **Example migration:**
> 
> **Before (v8):**
> 
> // Program.cs or Startup.cs
> MudGlobal.ButtonDefaults.Variant = Variant.Filled;
> MudGlobal.InputDefaults.Variant = Variant.Outlined;
> **After (v9) - Option 1: Explicit parameters:**
> 
> <MudButton Variant="Variant.Filled">Click Me</MudButton>
> <MudTextField Variant="Variant.Outlined" />
> **After (v9) - Option 2: Wrapper component:**
> 
> @* AppButton.razor *@
> <MudButton Variant="Variant.Filled" Class="@Class" @attributes="AdditionalAttributes">
>     @ChildContent
> </MudButton>
> 
> @code {
>     [Parameter] public string? Class { get; set; }
>     [Parameter] public RenderFragment? ChildContent { get; set; }
>     [Parameter(CaptureUnmatchedValues = true)] 
>     public IDictionary<string, object>? AdditionalAttributes { get; set; }
> }
> More details: [#12141](https://github.com/MudBlazor/MudBlazor/pull/12141)
> 
> ## DialogService
> ### ShowMessageBox Renamed
> Replace `ShowMessageBox` with `ShowMessageBoxAsync`:
> 
> More details: [#12292](https://github.com/MudBlazor/MudBlazor/pull/12292)
> 
> ### Dialog.DefaultFocus Moved
> `MudGlobal.DialogDefaults.DefaultFocus` has been moved to `MudDialogProvider`.
> 
> **Before (v8):**
> 
> MudGlobal.DialogDefaults.DefaultFocus = DefaultFocus.FirstChild;
> **After (v9):**
> 
> <MudDialogProvider DefaultFocus="DefaultFocus.FirstChild" />
> Or set it via `DialogOptions`:
> 
> var options = new DialogOptions { DefaultFocus = DefaultFocus.FirstChild };
> More details: [#12297](https://github.com/MudBlazor/MudBlazor/pull/12297)
> 
> ## MudTheme: Palette Type Changes
> `PaletteLight` and `PaletteDark` are now of type `Palette` instead of their specific types.
> 
> **Before (v8):**
> 
> PaletteLight PaletteLight { get; set; }
> PaletteDark PaletteDark { get; set; }
> **After (v9):**
> 
> Palette PaletteLight { get; set; }
> Palette PaletteDark { get; set; }
> This should have minimal impact as both derive from `Palette`.
> 
> More details: [#12148](https://github.com/MudBlazor/MudBlazor/pull/12148)
> 
> ### Transition Defaults Moved
> Popover transition defaults moved from `MudGlobal` to `PopoverOptions`.
> 
> **Before (v8):**
> 
> MudGlobal.PopoverDefaults.TransitionDuration = 300;
> **After (v9):**
> 
> builder.Services.AddMudServices(config =>
> {
>     config.PopoverOptions.TransitionDuration = 300;
> });
> More details: [#12300](https://github.com/MudBlazor/MudBlazor/pull/12300)
> 
> ## MudMenu: MenuContext Replaces IActivatable
> `MudMenu.ActivatorContent` now receives a `MenuContext` parameter instead of using `IActivatable` via cascading value. The `MenuContext` provides explicit async methods (`OpenAsync`, `CloseAsync`, `ToggleAsync`, `CloseAllAsync`) for controlling menus.
> 
> **Breaking Changes:**
> 
> 1. **`ActivatorContent` signature changed from `RenderFragment?` to `RenderFragment<MenuContext>?`**
> 2. **Menu is no longer opened implicitly** - You must explicitly call context methods in event handlers
> 3. **`IActivatable.Activate` method removed** from `MudMenu`
> 4. **Root div event handlers only fire for default activators** (Button, Icon, Label)
> 
> **MenuContext API:**
> 
> public sealed class MenuContext
> {
>     public Task OpenAsync(EventArgs? args = null);
>     public Task CloseAsync();
>     public Task ToggleAsync(EventArgs? args = null);
>     public Task CloseAllAsync();
> }
> **Migration Examples:**
> 
> ❌ **Before (v8) - Implicit activation:**
> 
> <MudMenu>
>     <ActivatorContent>
>         <MudButton Variant="Variant.Filled">Open Menu</MudButton>
>     </ActivatorContent>
>     <ChildContent>
>         <MudMenuItem>Item 1</MudMenuItem>
>     </ChildContent>
> </MudMenu>
> ✅ **After (v9) - Explicit context usage:**
> 
> <MudMenu>
>     <ActivatorContent>
>         <MudButton Variant="Variant.Filled" OnClick="@context.ToggleAsync">Open Menu</MudButton>
>     </ActivatorContent>
>     <ChildContent>
>         <MudMenuItem>Item 1</MudMenuItem>
>     </ChildContent>
> </MudMenu>
> **Left Click:**
> 
> <MudMenu ActivationEvent="MouseEvent.LeftClick">
>     <ActivatorContent>
>         <MudChip OnClick="@(() => context.ToggleAsync())">Click Me</MudChip>
>     </ActivatorContent>
> </MudMenu>
> **Right Click:**
> 
> <MudMenu ActivationEvent="MouseEvent.RightClick">
>     <ActivatorContent>
>         <div @oncontextmenu="@context.ToggleAsync" @oncontextmenu:preventDefault="true">
>             <MudChip>Right Click Me</MudChip>
>         </div>
>     </ActivatorContent>
> </MudMenu>
> **Mouse Over:**
> 
> <MudMenu ActivationEvent="MouseEvent.MouseOver">
>     <ActivatorContent>
>         <div @onpointerenter="@context.OpenAsync" @onpointerleave="@context.CloseAsync">
>             <MudChip>Hover Over Me</MudChip>
>         </div>
>     </ActivatorContent>
> </MudMenu>
> **Positioned at Cursor:**
> 
> <MudMenu PositionAtCursor="true">
>     <ActivatorContent>
>         @* Pass event args to OpenAsync/ToggleAsync for cursor positioning *@
>         <div @onclick="@context.ToggleAsync" style="cursor: pointer">
>             <MudCard>
>                 <MudCardContent>Click anywhere on this card</MudCardContent>
>             </MudCard>
>         </div>
>     </ActivatorContent>
> </MudMenu>
> **Non-interactive Activators:**
> 
> For non-interactive elements like `MudAvatar`, wrap them in a div with event handlers:
> 
> <MudMenu>
>     <ActivatorContent>
>         <div @onclick="@context.ToggleAsync" style="cursor: pointer">
>             <MudAvatar>
>                 <MudImage Src="avatar.jpg" />
>             </MudAvatar>
>         </div>
>     </ActivatorContent>
> </MudMenu>
> More details: [#12145](https://github.com/MudBlazor/MudBlazor/pull/12145)
> 
> ## MudFormComponent & MudBaseInput: API Changes
> The API had inconsistent naming between `MudFormComponent` and `MudBaseInput` that has been fixed for consistency.
> 
> ### MudFormComponent: Error and ErrorId Two-Way Bindable
> `Error` and `ErrorId` are now two-way bindable parameters.
> 
> **New capability (v9):**
> 
> <MudTextField @bind-Error="myError" @bind-ErrorId="myErrorId" />
> More details: [#12138](https://github.com/MudBlazor/MudBlazor/pull/12138), [#12140](https://github.com/MudBlazor/MudBlazor/pull/12140)
> 
> ### Method Naming Changes
> Several methods have been renamed for consistency:
> 
> * `Reset()` → `ResetAsync()` (was already marked async)
> * `Validate()` → `ValidateAsync()` (if async)
> * `ReadValue()` → `ReadValue` (property-style, no parentheses)
> 
> More details: [#12147](https://github.com/MudBlazor/MudBlazor/pull/12147), [#12310](https://github.com/MudBlazor/MudBlazor/pull/12310)
> 
> ### WriteValueAsync and SetValueAsync Renamed
> **Breaking Changes (PR [#12312](https://github.com/MudBlazor/MudBlazor/pull/12312)):**
> 
> 1. **`MudFormComponent.WriteValueAsync` renamed to `SetValueAsync` then to `SetValueCoreAsync`:**
> 
> To match the `ReadValue` property pattern (Read/Set), `WriteValueAsync` was renamed, then later renamed again with `Core` suffix.
> 
> ❌ **Before (v8):**
> 
> protected internal virtual T? ReadValue { get; }
> protected virtual Task WriteValueAsync(T? value)
> ✅ **After (v9 - Final):**
> 
> protected internal virtual T? ReadValue { get; }
> protected virtual Task SetValueCoreAsync(T? value)  // was WriteValueAsync → SetValueAsync → SetValueCoreAsync
> 2. **`MudBaseInput.SetValueAsync(T?, bool, bool)` renamed to `SetValueAndUpdateTextAsync`:**
> 
> To mirror the existing `SetTextAndUpdateValueAsync` method and avoid conflict with the base `SetValueAsync` method.
> 
> 3. **`SetTextAsync` renamed to `SetTextCoreAsync`:**
> 
> For consistency with the `Core` suffix pattern.
> 
> ❌ **Before (v8):**
> 
> // Text API
> protected internal string? ReadText { get; }
> protected Task SetTextAsync(string? text);
> protected Task SetTextAndUpdateValueAsync(string? text, bool updateValue = true);
> 
> // Value API (inconsistent)
> protected internal T? ReadValue { get; }
> protected Task SetValueAsync(T? value, bool updateText = true, bool force = false);
> ✅ **After (v9 - Final):**
> 
> // Text API
> protected internal string? ReadText { get; }
> protected Task SetTextCoreAsync(string? text);  // was SetTextAsync
> protected Task SetTextAndUpdateValueAsync(string? text, bool updateValue = true);
> 
> // Value API
> protected internal T? ReadValue { get; }
> protected virtual Task SetValueCoreAsync(T? value);  // was WriteValueAsync → SetValueAsync
> protected Task SetValueAndUpdateTextAsync(T? value, bool updateText = true, bool force = false);  // was SetValueAsync(T?, bool, bool)
> **Migration:**
> 
> **If you override `WriteValueAsync` in a custom component:**
> 
> ❌ **Before (v8):**
> 
> protected override Task WriteValueAsync(MyType? value)
> {
>     _value = value;
>     return Task.CompletedTask;
> }
> ✅ **After (v9):**
> 
> protected override Task SetValueCoreAsync(MyType? value)
> {
>     _value = value;
>     return Task.CompletedTask;
> }
> **If you call `SetValueAsync(value, updateText, force)` in a custom input:**
> 
> ❌ **Before (v8):**
> 
> await SetValueAsync(newValue, updateText: true, force: false);
> ✅ **After (v9):**
> 
> await SetValueAndUpdateTextAsync(newValue, updateText: true, force: false);
> **If you were calling `SetTextAsync` internally:**
> 
> ❌ **Before (v8):**
> 
> await SetTextAsync(newText);
> ✅ **After (v9):**
> 
> await SetTextCoreAsync(newText);
> All are protected APIs, so external impact should be minimal. Only affects custom components that inherit from `MudFormComponent` or `MudBaseInput` and override these methods.
> 
> More details: [#12312](https://github.com/MudBlazor/MudBlazor/pull/12312), [#12489](https://github.com/MudBlazor/MudBlazor/pull/12489)
> 
> ## MudSelect
> ### SelectedValues Changed to IReadOnlyCollection
> **Before (v8):**
> 
> ICollection<T> SelectedValues { get; set; }
> **After (v9):**
> 
> IReadOnlyCollection<T> SelectedValues { get; set; }
> More details: [#12619](https://github.com/MudBlazor/MudBlazor/pull/12619)
> 
> ## EventListener / EventManager Removed
> The `EventListener`, `EventListenerFactory`, and related event management infrastructure have been completely removed.
> 
> **Removed:**
> 
> * `IEventListener` / `EventListener`
> * `IEventListenerFactory` / `EventListenerFactory`
> * `IEventManager`
> * `WebEventJsonContext`
> 
> More details: [#12532](https://github.com/MudBlazor/MudBlazor/pull/12532)
> 
> ## Range and DateRange: Setters Removed
> `Range<T>.Start`, `Range<T>.End`, `DateRange.Start`, and `DateRange.End` properties no longer have setters. These classes should now be treated as immutable to ensure proper `GetHashCode()` behavior and thread safety.
> 
> **Breaking Changes:**
> 
> 1. **`Range<T>` properties are now read-only (init-only)**
> 2. **`DateRange` properties are now read-only (init-only)**
> 3. **Must create new instances instead of mutating existing ones**
> 
> **Complete API Changes:**
> 
> ❌ **Before (v8):**
> 
> public class Range<T>
> {
>     public T? Start { get; set; }  // Had setter
>     public T? End { get; set; }    // Had setter
> }
> 
> public class DateRange : Range<DateTime?>
> {
>     // Inherited mutable properties
> }
> ✅ **After (v9):**
> 
> public class Range<T> : IEquatable<Range<T>?>
> {
>     public T? Start { get; }  // No setter
>     public T? End { get; }    // No setter
>     
>     public Range() : this(default, default) { }
>     public Range(T? start, T? end) { Start = start; End = end; }
> }
> 
> public class DateRange : Range<DateTime?>, IEquatable<DateRange?>
> {
>     public DateRange() : this(null, null) { }
>     public DateRange(DateTime? start, DateTime? end) : base(start, end) { }
> }
> **Migration:**
> 
> ❌ **Before (v8) - Mutating existing instances:**
> 
> // Direct mutation
> var range = new DateRange();
> range.Start = DateTime.Today;
> range.End = DateTime.Today.AddDays(7);
> 
> // Or modifying .Start/.End properties
> dateRange.Start = newStartDate;
> dateRange.End = newEndDate;
> ✅ **After (v9) - Creating new instances:**
> 
> // Option 1: Constructor
> var range = new DateRange(DateTime.Today, DateTime.Today.AddDays(7));
> 
> // Option 2: Replace entire instance
> dateRange = new DateRange(newStartDate, newEndDate);
> 
> // For custom Range<T>
> var customRange = new Range<int>(1, 100);
> More details: [#12319](https://github.com/MudBlazor/MudBlazor/pull/12319)
> 
> ### New Analyzers
> Three new Roslyn analyzers enforce ParameterState best practices:
> 
> * **MUD0010**: Warning when reading a ParameterState property directly outside constructors
> * **MUD0011**: Error when writing to a ParameterState property
> * **MUD0012**: Warning when accessing ParameterState properties from outside the component (with code fix to use `GetState()`)
> 
> **Example:**
> 
> public class MyComponent{
>     private readonly ParameterState<int> _valueState;
> 
>     // ... Init of _valueState in ctor
> 
>     [Parameter, ParameterState]
>     public int Value { get; set; }
> }
> 
> 
> // MUD0010: Don't read ParameterState properties directly
> var value = Value; // ❌ Warning
> var value = _valueState.Value; // ✅ OK
> 
> // MUD0011: Error when writing to a ParameterState property
> Value = 10; // ❌ Warning
> _valueState.SetValueAsync(10); // ✅ OK
> 
> public MyComponent()
> {
>     var value = this.Value; // ✅ OK in constructor
> }
> 
> public override Task SetParametersAsync(ParameterView parameters)
> {
>     Value = 10; // ✅ OK in SetParametersAsync
> }
> 
> // MUD0012: External access
> myComponent.Value; // ❌ Warning
> 
> // Use GetState() instead
> myComponent.GetState(x => x.Value); // ✅ OK, c
> More details: [#12197](https://github.com/MudBlazor/MudBlazor/pull/12197), [#12203](https://github.com/MudBlazor/MudBlazor/pull/12203), [#12205](https://github.com/MudBlazor/MudBlazor/pull/12205)
> 
> ## MudStepper
> ### IStepContext: New Public API for Step State Management
> Previously, `MudStepper` exposed `MudStep` components directly through `RenderFragment` parameters and public properties. This had two major problems:
> 
> 1. **Writable parameters**: Consumers could write directly to step parameters, causing unexpected side effects
> 2. **Parameter state synchronization**: Properties like `Completed`, `Skipped`, `Disabled`, and `HasError` use `ParameterState`, requiring consumers to use `@bind-*` or call `.GetState()` to see changes
> 
> `IStepContext` eliminates these issues by providing a read-only interface for step state and controlled mutation methods.
> 
> **Breaking Changes:**
> 
> 1. **MudStepper.Steps changed from `IReadOnlyList<MudStep>` to `IReadOnlyList<IStepContext>`:**
> 
> ❌ **Before (v8):**
> 
> public IReadOnlyList<MudStep> Steps { get; }
> 
> // Direct property access could miss ParameterState updates
> var isCompleted = stepper.Steps[0].Completed; // May not reflect latest state
> ✅ **After (v9):**
> 
> public IReadOnlyList<IStepContext> Steps { get; }
> 
> // IStepContext properties are always current
> var isCompleted = stepper.Steps[0].Completed; // Always up-to-date
> 2. **MudStepper.ActiveStep changed from `MudStep?` to `IStepContext?`:**
> 
> ❌ **Before (v8):**
> 
> public MudStep? ActiveStep { get; }
> ✅ **After (v9):**
> 
> public IStepContext? ActiveStep { get; }
> 3. **Template parameters changed to use `IStepContext`:**
> 
> All stepper templates now receive `IStepContext` instead of `MudStep`:
> 
> * `RenderFragment<MudStep>? TitleTemplate` → `RenderFragment<IStepContext>? TitleTemplate`
> * `RenderFragment<MudStep>? LabelTemplate` → `RenderFragment<IStepContext>? LabelTemplate`
> * `RenderFragment<MudStep>? ConnectorTemplate` → `RenderFragment<IStepContext>? ConnectorTemplate`
> 
> **IStepContext Contract:**
> 
> public interface IStepContext
> {
>     // Read-only state properties
>     string? Title { get; }
>     bool Completed { get; }
>     bool Disabled { get; }
>     bool HasError { get; }
>     bool Skipped { get; }
>     bool Skippable { get; }
>     bool IsActive { get; }
> 
>     // Controlled mutation methods
>     Task SetHasErrorAsync(bool value, bool refreshParent = true);
>     Task SetCompletedAsync(bool value, bool refreshParent = true);
>     Task SetDisabledAsync(bool value, bool refreshParent = true);
>     Task SetSkippedAsync(bool value, bool refreshParent = true);
> }
> **Migration:**
> 
> Most code accessing step state **will just work** because `IStepContext` exposes the same properties.
> 
> **If you were using `.GetState()` to read step properties** (no longer needed):
> 
> ❌ **Before (v8):**
> 
> <MudChip Color="@GetColor(step.GetState(s => s.Completed))">
>     @step.Title
> </MudChip>
> ✅ **After (v9):**
> 
> @* IStepContext properties are always current *@
> <MudChip Color="@GetColor(step.Completed)">
>     @step.Title
> </MudChip>
> **Benefits:**
> 
> * ✅ No more manual `.GetState()` calls needed
> * ✅ Step state is always synchronized
> * ✅ Prevents accidental writes to step parameters
> 
> More details: [#12212](https://github.com/MudBlazor/MudBlazor/pull/12212)
> 
> ## MudChart: Complete Type Unification + INumber Support
> MudChart underwent a massive refactor to unify inconsistencies, add `INumber<T>` support (no longer restricted to `double`), introduce two new chart types (Radar, Rose), and enable combination charts. This is one of the largest breaking changes in v9.
> 
> **Key Changes:**
> 
> 1. **`INumber<T>` Support** - Charts now support any numeric type (`int`, `decimal`, `float`, etc.) instead of only `double`
> 2. **Unified Data Model** - All charts use `ChartSeries` and `ChartData<T>` instead of raw arrays or custom structs
> 3. **Type-Specific Options** - `AxisChartOptions` and generic `ChartOptions` replaced with specific types (`BarChartOptions`, `LineChartOptions`, etc.)
> 4. **Base Class Reorganization** - Clearer separation between axis charts (`MudAxisChartBase`) and radial charts (`MudRadialChartBase`)
> 5. **Consistent Parameter Naming** - `XAxisLabels`, `InputLabels`, etc. unified to `ChartLabels`
> 6. **New Chart Types** - Radar and Rose charts added
> 7. **Combination Charts** - Multiple chart types can be overlaid (e.g., Bar + Line)
> 
> **Breaking Changes - Removed Classes:**
> 
> Removed	Replacement
> `AxisChartOptions`	Type-specific options: `BarChartOptions`, `LineChartOptions`, `TimeSeriesChartOptions`, etc.
> `ChartOptions`	Type-specific options: `PieChartOptions`, `DonutChartOptions`, `RadarChartOptions`, etc.
> `MudTimeSeriesChart`	`<MudChart ChartType="ChartType.Line">` or `<MudChart ChartType="ChartType.TimeSeries">`
> `MudCategoryChartBase`	`MudAxisChartBase` or `MudRadialChartBase`
> `MudCategoryAxisChartBase`	`MudAxisChartBase`
> `TimeSeriesChartSeries`	`ChartSeries` + `ChartData<T>`
> `NodeChartOptions`	`SankeyChartOptions`
> **Breaking Changes - Parameter Renames:**
> 
> Old Parameter	New Parameter	Applies To
> `XAxisLabels`	`ChartLabels`	All chart types
> `XAxisChartOptions`	Type-specific options (`BarChartOptions`, etc.)	All chart types
> `InputData`	`ChartSeries`	Pie, Donut charts
> `InputLabels`	`ChartLabels`	Pie, Donut charts
> `CircleDonutRatio`	`DonutRingRatio`	`DonutChartOptions`
> `StackedBarWidthRatio`	`BarWidthRatio`	`StackedBarChartOptions`
> `DataMarkerTooltipTitleFormat`	`TooltipTitleFormat`	`ChartSeries`
> `ShowDataMarkers`	`ShowDataMarkers`	Moved to `LineChartOptions` and `TimeSeriesChartOptions`
> `LineDisplayType`	`LineDisplayType`	Moved to `SeriesDisplayOverrides`
> `FillOpacity`	`FillOpacity`	Moved to `SeriesDisplayOverrides` and `DefaultRadialChartOptions`
> `TimeSeriesChartSeries.TimeValue`	`TimeValue`	`TimeSeries` in `ChartData`
> `Nodes` & `Edges`	`ChartSeries`	Sankey chart (nodes auto-generated)
> **Available Chart-Specific Options:**
> 
> * `BarChartOptions`
> * `LineChartOptions`
> * `HeatMapChartOptions`
> * `DonutChartOptions`
> * `PieChartOptions`
> * `StackedBarChartOptions`
> * `RadarChartOptions`
> * `RoseChartOptions`
> * `TimeSeriesChartOptions`
> * `SankeyChartOptions`
> 
> **Migration Examples:**
> 
> ### Data Model Migration
> ❌ **Before (v8) - Raw double arrays:**
> 
> <MudChart ChartType="ChartType.Pie" 
>           InputData="@data" 
>           InputLabels="@labels" />
> 
> @code {
>     private double[] data = { 25, 50, 25 };
>     private string[] labels = { "A", "B", "C" };
> }
> ✅ **After (v9) - ChartSeries with INumber support:**
> 
> <MudChart ChartType="ChartType.Pie" 
>           ChartSeries="@series" 
>           ChartLabels="@labels" />
> 
> @code {
>     private List<ChartSeries<double>> series = new()
>     {
>         new ChartSeries<double>
>         {
>             Data = new double[] { 25, 50, 25 }.AsChartDataSet()
>         }
>     };
>     private string[] labels = { "A", "B", "C" };
> }
> **Tip:** Use `AsChartDataSet()` extension method to convert `T[]` → `List<ChartData<T>>` for easy migration:
> 
> double[] values = { 1.5, 2.3, 3.7 };
> var chartData = values.AsChartDataSet(); // List<ChartData<double>>
> ### INumber Support - Use Any Numeric Type
> ✅ **After (v9) - Use int, decimal, etc.:**
> 
> <MudChart ChartType="ChartType.Bar" ChartSeries="@series" />
> 
> @code {
>     private List<ChartSeries<int>> series = new()
>     {
>         new ChartSeries<int>
>         {
>             Name = "Sales",
>             Data = new int[] { 100, 200, 150, 300 }.AsChartDataSet()  // int instead of double
>         }
>     };
> }
> // Decimal for financial data
> decimal[] revenue = { 1000.50m, 2500.75m, 1800.25m };
> var series = new ChartSeries 
> { 
>     Data = revenue.AsChartDataSet() 
> };
> ### Chart Options Migration
> ❌ **Before (v8) - Generic AxisChartOptions:**
> 
> <MudChart ChartType="ChartType.Bar" 
>           XAxisChartOptions="@chartOptions" />
> 
> @code {
>     private AxisChartOptions chartOptions = new()
>     {
>         YAxisTicks = 10,
>         YAxisFormat = "N0"
>     };
> }
> ✅ **After (v9) - Type-specific options:**
> 
> <MudChart ChartType="ChartType.Bar" 
>           ChartOptions="@barOptions" />
> 
> @code {
>     private BarChartOptions barOptions = new()
>     {
>         YAxisTicks = 10,
>         YAxisFormat = "N0",
>         BarWidthRatio = 0.8  // Bar-specific option
>     };
> }
> ### TimeSeriesChart Migration
> ❌ **Before (v8) - Dedicated component:**
> 
> <MudTimeSeriesChart ChartType="ChartType.Line" 
>                     ChartSeries="@timeSeriesData" />
> 
> @code {
>     private List<TimeSeriesChartSeries> timeSeriesData = new()
>     {
>         new TimeSeriesChartSeries
>         {
>             Name = "Temperature",
>             TimeValue = new DateTime(2024, 1, 1),
>             Value = 20.5
>         }
>     };
> }
> ✅ **After (v9) - Use MudChart with ChartData:**
> 
> <MudChart ChartType="ChartType.TimeSeries" 
>           ChartSeries="@series"
>           ChartOptions="@timeOptions" />
> 
> @code {
>     private List<ChartSeries<double>> series = new()
>     {
>         new ChartSeries<double>
>         {
>             Name = "Temperature",
>             Data = new List<ChartData<double>>
>             {
>                 new() { TimeValue = new DateTime(2024, 1, 1), Value = 20.5 },
>                 new() { TimeValue = new DateTime(2024, 1, 2), Value = 22.3 }
>             }
>         }
>     };
>     
>     private TimeSeriesChartOptions timeOptions = new()
>     {
>         ShowDataMarkers = true
>     };
> }
> ### Combination/Mixed Charts (New Feature)
> ✅ **After (v9) - Overlay multiple chart types:**
> 
> <MudChart ChartType="ChartType.Bar" ChartSeries="@mixedSeries" />
> 
> @code {
>     private List<ChartSeries<double>> mixedSeries = new()
>     {
>         new ChartSeries<double>
>         {
>             Name = "Revenue",
>             ChartType = ChartType.Bar,  // Bars
>             Data = new double[] { 100, 150, 120 }.AsChartDataSet()
>         },
>         new ChartSeries<double>
>         {
>             Name = "Target",
>             ChartType = ChartType.Line,  // Line overlay
>             Data = new double[] { 110, 140, 130 }.AsChartDataSet()
>         }
>     };
> }
> ### New Chart Types
> **Radar Chart:**
> 
> <MudChart ChartType="ChartType.Radar" 
>           ChartSeries="@radarSeries"
>           ChartLabels="@radarLabels" />
> **Rose Chart:**
> 
> <MudChart ChartType="ChartType.Rose" 
>           ChartSeries="@roseSeries"
>           ChartLabels="@roseLabels" />
> ### Sankey Chart Migration
> ❌ **Before (v8) - Explicit nodes:**
> 
> <MudChart ChartType="ChartType.Sankey"
>           Nodes="@nodes"
>           Edges="@edges" />
> ✅ **After (v9) - Nodes auto-generated from series:**
> 
> <MudChart ChartType="ChartType.Sankey"
>           ChartSeries="@sankeyData" />
> 
> @code {
>     private List<ChartSeries<int>> sankeyData = new()
>     {
>         new ChartSeries<int>
>         {
>             // Edges define the flow; nodes are generated automatically
>             Data = new List<ChartData<int>>
>             {
>                 new() { Source = "A", Target = "B", Value = 10 },
>                 new() { Source = "B", Target = "C", Value = 5 }
>             }
>         }
>     };
> }
> **New Features:**
> 
> ✅ Show values within Pie Chart segments ✅ Bar and Stacked Bar chart justification options ✅ Combination charts (Bar + Line overlay) ✅ Interchangeable chart types ✅ Tooltip customization (`TooltipTemplate`, `TooltipPositionFunc`) ✅ Radar and Rose chart types ✅ Series visibility toggling for all chart types ✅ Stacked Bar charts support negative values ✅ Dynamic chart scaling on visibility changes ✅ Dynamic font scaling for HeatMap cell values ✅ HeatMap tooltips match other chart styles
> 
> **Migration Checklist:**
> 
> * [ ]  Replace `double[]` arrays with `ChartData<T>` using `AsChartDataSet()`[ ]  Replace `AxisChartOptions`/`ChartOptions` with type-specific options[ ]  Rename `XAxisLabels` → `ChartLabels`[ ]  Rename `InputData` → `ChartSeries` (Pie/Donut)[ ]  Replace `MudTimeSeriesChart` with `<MudChart ChartType="ChartType.TimeSeries">`[ ]  Update `TimeSeriesChartSeries` to `ChartSeries` + `ChartData<T>`[ ]  Move `ShowDataMarkers`, `LineDisplayType`, `FillOpacity` to chart options[ ]  Update Sankey charts to use `ChartSeries` (nodes auto-generated)[ ]  Consider using `INumber<T>` types (`int`, `decimal`) instead of `double` where appropriate
> 
> More details: [#11458](https://github.com/MudBlazor/MudBlazor/pull/11458)
> 
> ## MudChat - Component Removal
> **Status: Breaking Change - Entire Component Family Removed**
> 
> The entire MudChat component family has been **completely removed** from MudBlazor v9 and moved to the [MudX](https://github.com/MudXtra/MudX/) extension library. This is part of MudBlazor's strategy to focus on core components and manage resources effectively.
> 
> ### Removed Components
> All chat-related components have been removed:
> 
> // ❌ REMOVED - No longer available in MudBlazor v9
> <MudChat />
> <MudChatBubble />
> <MudChatHeader />
> <MudChatFooter />
> ### Removed Types
> All chat-related enums and types have been removed:
> 
> // ❌ REMOVED
> ChatBubblePosition
> ChatArrowPosition
> ### Migration Path
> To continue using chat components, install the **MudX.MudBlazor.Extension** library:
> 
> **MudX Repository:** https://github.com/MudXtra/MudX/
> 
> More details: [#12151](https://github.com/MudBlazor/MudBlazor/pull/12151)
> 
> ## MudTreeView - ITreeItemData Interface and Children Collection Changes
> **Status: Breaking Change - API Type Changes**
> 
> MudTreeView now uses a new `ITreeItemData<T>` interface and changes the `Children` property from a mutable collection to read-only.
> 
> ### API Changes
> **MudTreeView Properties:**
> 
> // ❌ Old (v8)
> public IReadOnlyCollection<TreeItemData<T>>? Items { get; set; }
> public RenderFragment<TreeItemData<T>>? ItemTemplate { get; set; }
> public Func<TreeItemData<T>, Task<bool>>? FilterFunc { get; set; }
> 
> // ✅ New (v9)
> public IReadOnlyCollection<ITreeItemData<T>>? Items { get; set; }
> public RenderFragment<ITreeItemData<T>>? ItemTemplate { get; set; }
> public Func<ITreeItemData<T>, Task<bool>>? FilterFunc { get; set; }
> **MudTreeViewItem Properties:**
> 
> // ❌ Old (v8)
> public IReadOnlyCollection<TreeItemData<T?>>? Items { get; set; }
> public EventCallback<IReadOnlyCollection<TreeItemData<T?>>?> ItemsChanged { get; set; }
> 
> // ✅ New (v9)
> public IReadOnlyCollection<ITreeItemData<T?>>? Items { get; set; }
> public EventCallback<IReadOnlyCollection<ITreeItemData<T?>>?> ItemsChanged { get; set; }
> **3. Update Children Manipulation (Now Read-Only):**
> 
> // ❌ Old (v8) - Direct mutation
> var item = new TreeItemData<string> { Text = "Parent" };
> item.Children = new List<TreeItemData<string>>();
> item.Children.Add(new TreeItemData<string> { Text = "Child" });
> 
> // ✅ New (v9) - Create new list, assign read-only collection
> var item = new TreeItemData<string> { Text = "Parent" };
> var children = new List<TreeItemData<string>>
> {
>     new TreeItemData<string> { Text = "Child" }
> };
> item.Children = children;
> More details: [#12090](https://github.com/MudBlazor/MudBlazor/pull/12090)
> 
> ## MudDataGrid: ServerData CancellationToken Support
> **Status: Breaking Change - Method Signature Changed**
> 
> The `ServerData` function signature now requires a `CancellationToken` parameter. This enables automatic cancellation of pending data loads when users rapidly navigate pages or change filters, preventing unnecessary API calls and race conditions.
> 
> ### Breaking Changes
> **ServerData Signature Changed:**
> 
> // ❌ Old (v8)
> public Func<GridState<T>, Task<GridData<T>>> ServerData { get; set; }
> 
> // ✅ New (v9)
> public Func<GridState<T>, CancellationToken, Task<GridData<T>>> ServerData { get; set; }
> ## CssBuilder and StyleBuilder
> `CssBuilder` and `StyleBuilder` are now declared as `readonly struct` for better performance by avoiding hidden defensive copies.
> 
> Warning
> 
> **Breaking Change:** `default(CssBuilder)` and `default(StyleBuilder)` will now throw `NullReferenceException` at runtime and are **no longer supported**.
> 
> **Migration required if you were using:**
> 
> ❌ **Don't use:**
> 
> var cssBuilder = default(CssBuilder);
> cssBuilder.AddClass("my-class"); // NullReferenceException!
> 
> var styleBuilder = default(StyleBuilder);
> styleBuilder.AddStyle("color", "red"); // NullReferenceException!
> ✅ **Use instead:**
> 
> // Option 1: Use constructor
> var cssBuilder = new CssBuilder();
> cssBuilder.AddClass("my-class");
> 
> var styleBuilder = new StyleBuilder();
> styleBuilder.AddStyle("color", "red");
> 
> // Option 2: Use static factory methods
> var cssBuilder = CssBuilder.Default()
>     .AddClass("my-class");
> 
> var styleBuilder = StyleBuilder.Default()
>     .AddStyle("color", "red");
> 
> // Option 3: Use builder pattern (recommended)
> var classes = new CssBuilder("base-class")
>     .AddClass("additional-class", when: someCondition)
>     .Build();
> 
> var styles = new StyleBuilder("display", "flex")
>     .AddStyle("color", color.ToString())
>     .Build();
> More details: [#12598](https://github.com/MudBlazor/MudBlazor/pull/12598)
> 
> ## MudFileUpload - IActivator Removal and Default Behavior
> **Status: Breaking Change - Activation Pattern Changed**
> 
> MudFileUpload no longer implements `IActivator`. The `ActivationContent` parameter has been replaced with `CustomContent`, and the activation behavior has changed.
> 
> ### Breaking Changes
> **1. ActivationContent Renamed to CustomContent:**
> 
> // ❌ Old (v8)
> <MudFileUpload>
>     <ActivationContent>
>         <MudButton>Select File</MudButton>
>     </ActivationContent>
> </MudFileUpload>
> 
> // ✅ New (v9)
> <MudFileUpload>
>     <CustomContent Context="fileUpload">
>         <MudButton OnClick="@fileUpload.OpenFilePickerAsync">Select File</MudButton>
>     </CustomContent>
> </MudFileUpload>
> **2. Activate Method Removed:**
> 
> // ❌ Old (v8)
> MudFileUpload fileUpload;
> 
> await fileUpload.Activate(args);
> 
> // ✅ New (v9)
> MudFileUpload fileUpload;
> 
> await fileUpload.OpenFilePickerAsync();
> **3. CustomContent No Longer Auto-Opens Picker:**
> 
> Caution
> 
> In v8, `ActivationContent` automatically opened the file picker when clicked. In v9, `CustomContent` requires you to **manually call** `OpenFilePickerAsync()` via an `OnClick` handler.
> 
> @* ❌ Old (v8) - Automatic activation *@
> <MudFileUpload T="IBrowserFile">
>     <ActivationContent>
>         <MudButton Color="Color.Primary">
>             Select File
>         </MudButton>
>     </ActivationContent>
> </MudFileUpload>
> 
> @* ✅ New (v9) - Manual activation required *@
> <MudFileUpload T="IBrowserFile">
>     <CustomContent Context="fileUpload">
>         <MudButton Color="Color.Primary" 
>                    OnClick="@fileUpload.OpenFilePickerAsync">
>             Select File
>         </MudButton>
>     </CustomContent>
> </MudFileUpload>
> ### New Features and Improvements
> **1. Default File List Rendering:**
> 
> If `SelectedTemplate` is not provided, MudFileUpload now renders a **default file list** automatically:
> 
> @* Simple usage - Shows default file list *@
> <MudFileUpload T="IBrowserFile" 
>                @bind-Files="_files" 
>                MaximumFileCount="10" />
> 
> @* Hide default file list *@
> <MudFileUpload T="IBrowserFile" 
>                @bind-Files="_files">
>     <SelectedTemplate></SelectedTemplate>
> </MudFileUpload>
> **2. Built-in Drag and Drop:**
> 
> MudFileUpload now has native drag-and-drop support:
> 
> <MudFileUpload T="IBrowserFile" 
>                @bind-Files="_files"
>                DragAndDrop="true"
>                Dragging="@_isDragging">
>     <CustomContent Context="fileUpload">
>         <MudText>Drag files here or click to browse</MudText>
>     </CustomContent>
> </MudFileUpload>
> 
> @code {
>     private IReadOnlyList<IBrowserFile> _files = new List<IBrowserFile>();
>     private bool _isDragging;
> }
> **3. New Public Methods:**
> 
> // Get list of filenames
> IReadOnlyList<string> filenames = fileUpload.GetFilenames();
> 
> // Remove specific file by name
> await fileUpload.RemoveFile("document.pdf");
> 
> // Open file picker programmatically
> await fileUpload.OpenFilePickerAsync();
> ### Migration Examples
> **Example 1: Simple File Upload with Custom Button**
> 
> @* ❌ Old (v8) *@
> <MudFileUpload T="IBrowserFile" @bind-Files="_files">
>     <ActivationContent>
>         <MudButton Variant="Variant.Filled" Color="Color.Primary">
>             <MudIcon Icon="@Icons.Material.Filled.Upload" Class="mr-2" />
>             Upload File
>         </MudButton>
>     </ActivationContent>
>     <SelectedTemplate>
>         @if (_files != null)
>         {
>             <MudText>Selected: @_files.Name</MudText>
>         }
>     </SelectedTemplate>
> </MudFileUpload>
> 
> @* ✅ New (v9) *@
> <MudFileUpload T="IBrowserFile" @bind-Files="_files">
>     <CustomContent Context="fileUpload">
>         <MudButton Variant="Variant.Filled" 
>                    Color="Color.Primary"
>                    OnClick="@fileUpload.OpenFilePickerAsync">
>             <MudIcon Icon="@Icons.Material.Filled.Upload" Class="mr-2" />
>             Upload File
>         </MudButton>
>     </CustomContent>
>     <SelectedTemplate>
>         @if (_files != null)
>         {
>             <MudText>Selected: @_files.Name</MudText>
>         }
>     </SelectedTemplate>
> </MudFileUpload>
> **Example 2: Drag and Drop with Custom Zone**
> 
> @* New in v9 - Built-in drag and drop *@
> <MudFileUpload T="IReadOnlyList<IBrowserFile>" 
>                @bind-Files="_files"
>                DragAndDrop="true"
>                Dragging="@_isDragging"
>                MaximumFileCount="5">
>     <CustomContent Context="fileUpload">
>         <MudPaper Outlined="true" 
>                   Class="@(_isDragging ? "mud-primary-text" : "")"
>                   Style="min-height: 200px; display: flex; align-items: center; justify-content: center;">
>             <MudStack AlignItems="AlignItems.Center">
>                 <MudIcon Icon="@Icons.Material.Filled.CloudUpload" Size="Size.Large" />
>                 <MudText Typo="Typo.h6">
>                     Drag files here or 
>                     <MudLink OnClick="@fileUpload.OpenFilePickerAsync">click to browse</MudLink>
>                 </MudText>
>             </MudStack>
>         </MudPaper>
>     </CustomContent>
> </MudFileUpload>
> 
> @code {
>     private IReadOnlyList<IBrowserFile> _files = new List<IBrowserFile>();
>     private bool _isDragging;
> }
> **Example 3: Using Default File List**
> 
> @* New in v9 - Default rendering without SelectedTemplate *@
> <MudFileUpload T="IReadOnlyList<IBrowserFile>" 
>                @bind-Files="_files"
>                MaximumFileCount="10" />
> 
> @* The above automatically renders:
>    - A button to select files
>    - A list of selected files
>    - Remove buttons for each file
> *@
> **Example 4: Programmatic File Removal**
> 
> <MudFileUpload @ref="_fileUpload" 
>                T="IReadOnlyList<IBrowserFile>" 
>                @bind-Files="_files" />
> 
> <MudButton OnClick="RemoveFirstFile">Remove First File</MudButton>
> 
> @code {
>     private MudFileUpload<IReadOnlyList<IBrowserFile>> _fileUpload;
>     private IReadOnlyList<IBrowserFile> _files = new List<IBrowserFile>();
> 
>     private async Task RemoveFirstFile()
>     {
>         var filenames = _fileUpload.GetFilenames();
>         if (filenames.Any())
>         {
>             await _fileUpload.RemoveFile(filenames.First());
>         }
>     }
> }
> ### Summary of Changes
> v8	v9
> `<ActivationContent>`	`<CustomContent Context="fileUpload">`
> Auto-opens picker on click	Manual `OnClick="@fileUpload.OpenFilePickerAsync"` required
> `Activate(args)` method	`OpenFilePickerAsync()` method
> No default file list	Default file list rendered if `SelectedTemplate` is empty
> No built-in drag & drop	`DragAndDrop="true"` parameter
> No drag state tracking	`Dragging` parameter
> No file management API	`GetFilenames()`, `RemoveFile()` methods
> More details: [#10487](https://github.com/MudBlazor/MudBlazor/pull/10487)
> 
> ## Modal Default Changed ([#12101](https://github.com/MudBlazor/MudBlazor/pull/12101))
> The default value of the `Modal` parameter for popover-based components has changed from `true` to `false`.
> 
> **Impact:** - Popovers no longer block background interaction by default. - Applications relying on modal overlay behavior must now opt in explicitly.
> 
> If you want the previous behavior, explicitly set:
> 
> <MudMenu Modal="true">
> ## Popover Flipping Behavior ([#12298](https://github.com/MudBlazor/MudBlazor/pull/12298))
> ## Popover Configuration Consolidated ([#12286](https://github.com/MudBlazor/MudBlazor/pull/12286))
> ## Popover Overflow Behavior ([#12411](https://github.com/MudBlazor/MudBlazor/pull/12411))
> Popover flipping behavior is now controlled exclusively through `PopoverOptions` with a default of **FlipAlways**.
> 
> **Impact:** - Popovers now flip more aggressively to remain within the viewport. - Visual placement behavior may differ from previous versions. **Impact:** - `MudGlobal.PopoverDefaults` has been removed. - **Impact:** - Flipping behavior must be configured through `PopoverOptions` **Impact:** - Component-level flipping configuration is no longer supported.
> 
> Configure flipping via Program.cs or where you add your MudServices:
> 
>             services.AddMudServices(config =>
>             {
>                 config.SnackbarConfiguration.PositionClass = Defaults.Classes.Position.BottomLeft;
>                 config.PopoverOptions.OverflowBehavior = OverflowBehavior.FlipNever;
>             });
> ## Other Changes
> * **PopoverOptions.Mode**: `PopoverOptions.Mode` / `PopoverMode` was removed. [#12596](https://github.com/MudBlazor/MudBlazor/pull/12596)
> * **MudSelect**: `SelectOption(object?)` method parameter type changed from `object?` to `T?`. [#12623](https://github.com/MudBlazor/MudBlazor/pull/12623)
> * **MudBaseInput**: `TextUpdateSuppression` parameter removed from all derived components. [#12306](https://github.com/MudBlazor/MudBlazor/pull/12306)
> * **MudBaseInput**: `ForceUpdate()` method removed from all derived input components. [#12542](https://github.com/MudBlazor/MudBlazor/pull/12542)
> * **MudSelect**: `WaitForRender()` method removed. [#12541](https://github.com/MudBlazor/MudBlazor/pull/12541)
> * **MudSelect**: `Open` parameter now supports two-way binding with `@bind-Open`. [#12589](https://github.com/MudBlazor/MudBlazor/pull/12589)
> * **MudCollapse**: Content now rendered inside a `<span>` element for better structure. [#12590](https://github.com/MudBlazor/MudBlazor/pull/12590)
> * **MudColorPicker**: Fixed support for `null` color values and improved throttling. [#12567](https://github.com/MudBlazor/MudBlazor/pull/12567)
> * **MudDialogContainer**: `OnMouseUp` event handler renamed to `OnMouseUpAsync` and is now `private`. [#12514](https://github.com/MudBlazor/MudBlazor/pull/12514)
> * **MudThemeProvider**: `ObserveSystemThemeChange` parameter renamed to `ObserveSystemDarkModeChange` for consistency. Removed obsolete methods: `GetSystemPreference()`, `WatchSystemPreference()`, and `SystemPreferenceChanged()` (use `GetSystemDarkModeAsync()`, `WatchSystemDarkModeAsync()`, and `SystemDarkModeChangedAsync()` instead). [#12022](https://github.com/MudBlazor/MudBlazor/pull/12022)
> * **MudLink**: `Typo` parameter now defaults to `Typo.inherit` (was `Typo.body1`) to automatically match surrounding text typography. [#12094](https://github.com/MudBlazor/MudBlazor/pull/12094)
> * **MudSnackbar**: Snackbars with action buttons now require interaction by default (won't auto-dismiss) following Material Design 3 guidelines. Explicitly set `RequireInteraction` to override this behavior. [#12108](https://github.com/MudBlazor/MudBlazor/pull/12108)
> * **MudTabs**: `TabPanelClass` renamed to `TabButtonsClass` (applies to all tab buttons). `PanelClass` renamed to `TabPanelsClass` (applies to panel wrapper). **MudTabPanel**: `Class` now only applies to the button element; added new `PanelClass` property to style the panel element. [#12156](https://github.com/MudBlazor/MudBlazor/pull/12156)
> * **Masking API**: `MaskChar` converted to immutable `readonly struct` (use constructor). `RegexMask.Delimiters` → `DelimiterCharacters`. `IMask.Mask` and `IMask.Text` now non-nullable. `BaseMask` protected fields now private (use protected properties/methods). [#12314](https://github.com/MudBlazor/MudBlazor/pull/12314)
> * **IScrollListener**: Changed from `IDisposable` to `IAsyncDisposable` - use `await DisposeAsync()` instead of `Dispose()`. Added `ReportRateMs` property (default: 10ms) to control scroll reporting rate. Added `GetCurrentScrollDataAsync()` method to get current scroll position without user input. Added `ScrollEventArgs.ClientHeight` and `ScrollEventArgs.ClientWidth` properties. `IScrollListenerFactory.Create()` now has overload with `reportRateMs` parameter. [#12183](https://github.com/MudBlazor/MudBlazor/pull/12183)

