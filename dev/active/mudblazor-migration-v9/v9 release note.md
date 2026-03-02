What's Changed
Breaking Changes
MudSwitch: Decouple label font size from Size property by @danielchalmers in MudSwitch: Decouple label font size from Size property #11132
Deprecate some public Style properties by @danielchalmers in Deprecate some public Style properties #11407
MudDataGrid: Add CancellationToken support for ServerData by @w3ori in MudDataGrid: Add CancellationToken support for ServerData #11841
ThemeProvider: Rename ObserveSystemThemeChange to ObserveSystemDarkModeChange and remove obsolete methods by @Copilot in ThemeProvider: Rename ObserveSystemThemeChange to ObserveSystemDarkModeChange and remove obsolete methods #12022
MudChart: Chart Type Unification + 2 New Charts by @Anu6is in MudChart: Chart Type Unification + 2 New Charts #11458
MudGlobal: Remove deprecated theming properties by @Copilot in MudGlobal: Remove deprecated theming properties #12141
Popovers: Make overlay popovers non-modal by default by @Copilot in Popovers: Make overlay popovers non-modal by default #12101
v9: Remove all code marked obsolete/deprecated by @Copilot in v9: Remove all code marked obsolete/deprecated #12142
MudSnackbar: Make snackbar require interaction when action present by @danielchalmers in MudSnackbar: Make snackbar require interaction when action present #12108
MudFormComponent: Rename methods to async, use await by @ScarletKuro in MudFormComponent: Rename methods to async, use await #12147
MudLink: Inherit typography by default by @danielchalmers in MudLink: Inherit typography by default #12094
MudColorPicker: Use ParameterState by @ScarletKuro in MudColorPicker: Use ParameterState #10357
MudTreeView: Add ITreeItemData and make Children IReadOnlyCollection by @danielchalmers in MudTreeView: Add ITreeItemData and make Children IReadOnlyCollection #12090
Converters: Rework conversion system by @ScarletKuro in Converters: Rework conversion system #12177
ResizeObserver: Resizes observer and bounding client rect cleanup by @91378246 in ResizeObserver: Resizes observer and bounding client rect cleanup #12173
Popovers: Remove and Replace DropdownSettings by @versile2 in Popovers: Remove and Replace DropdownSettings #12186
MudTabs: Rename class properties; MudTabPanel: add PanelClass property by @filipvalentin in MudTabs: Rename class properties; MudTabPanel: add PanelClass property #12156
MudStepper: Add IStepContext by @ScarletKuro in MudStepper: Add IStepContext #12212
MudSelect: Use ParameterState for MultiSelection by @ScarletKuro in MudSelect: Use ParameterState for MultiSelection #12241
MudSelect: use ParameterState by @Copilot in MudSelect: use ParameterState #12244
MudBaseInput: Use ParameterState for Text by @Copilot in MudBaseInput: Use ParameterState for Text #12259
MudBaseInput: ParameterState for Value by @ScarletKuro in MudBaseInput: ParameterState for Value #12267
MudBooleanInput: Use ParameterState for Value by @ScarletKuro in MudBooleanInput: Use ParameterState for Value #12273
DialogService: Rename ShowMessageBox to ShowMessageBoxAsync by @91378246 in DialogService: Rename ShowMessageBox to ShowMessageBoxAsync #12292
MudPopover: Remove MudGlobal.PopoverDefaults and move properties to PopoverOptions by @Copilot in MudPopover: Remove MudGlobal.PopoverDefaults and move properties to PopoverOptions #12286
MudDialog: Move DefaultFocus from MudGlobal.DialogDefaults to MudDialogProvider by @Copilot in MudDialog: Move DefaultFocus from MudGlobal.DialogDefaults to MudDialogProvider #12297
MudPopover: Remove OverflowBehavior property from components, use PopoverOptions directly by @Copilot in MudPopover: Remove OverflowBehavior property from components, use PopoverOptions directly #12298
MudPopover: Move transition defaults from MudGlobal to PopoverOptions by @danielchalmers in MudPopover: Move transition defaults from MudGlobal to PopoverOptions #12300
MudInput: Use bind:get/bind:set by @ScarletKuro in MudInput: Use bind:get/bind:set #12272
MudBaseInput: Remove TextUpdateSuppression by @ScarletKuro in MudBaseInput: Remove TextUpdateSuppression #12306
Refactor: Improve MudDebouncedInput, ThrottleDispatcher, DebounceDispatcher by @Copilot in Refactor: Improve MudDebouncedInput, ThrottleDispatcher, DebounceDispatcher #12296
MudBaseInput & MudFormComponent: Fix API naming inconsistency by @Copilot in MudBaseInput & MudFormComponent: Fix API naming inconsistency #12312
MudMenu: Replace IActivatable with MenuContext by @Copilot in MudMenu: Replace IActivatable with MenuContext #12145
Masking: Improve abstraction, add more tests by @Copilot in Masking: Improve abstraction, add more tests #12314
Range: Remove setters by @ScarletKuro in Range<T>: Remove setters #12319
MudChat: Remove components in v9, redirect to MudX by @Copilot in MudChat: Remove components in v9, redirect to MudX #12151
ParameterState: Add ResolveEffectiveParameter, ParameterStateCollection for shared handlers by @ScarletKuro in ParameterState: Add ResolveEffectiveParameter, ParameterStateCollection for shared handlers #12347
MudFormComponent: Add GetDefaultConverter, cleanup by @ScarletKuro in MudFormComponent: Add GetDefaultConverter, cleanup #12365
MudSplitPanel: Add get and set divider position functions by @91378246 in MudSplitPanel: Add get and set divider position functions #12370
MudInput: Replace AutoGrow with Sizing by @danielchalmers in MudInput: Replace AutoGrow with Sizing #12417
ScrollListener: Add report rate and GetCurrentScrollDataAsync by @91378246 in ScrollListener: Add report rate and GetCurrentScrollDataAsync #12183
MudTextField: Add async postfixes by @91378246 in MudTextField: Add async postfixes #12484
Components: SetTextAsync->SetTextCoreAsync, SetValueAsync->SetValueCoreAsync by @ScarletKuro in Components: SetTextAsync->SetTextCoreAsync, SetValueAsync->SetValueCoreAsync #12489
MudDialogContainer: Rename OnMouseUp to OnMouseUpAsync, make private by @ScarletKuro in MudDialogContainer: Rename OnMouseUp to OnMouseUpAsync, make private #12514
Remove EventListener / EventManager by @ScarletKuro in Remove EventListener / EventManager #12532
MudThemeProvider: Fix script and refactor by @meenzen in MudThemeProvider: Fix script and refactor #12534
MudBaseInput: Remove ForceUpdate by @ScarletKuro in MudBaseInput: Remove ForceUpdate #12542
MudSelect: Add two-way Open parameter by @ScarletKuro in MudSelect: Add two-way Open parameter #12589
PopoverOptions: Remove PopoverMode by @ScarletKuro in PopoverOptions: Remove PopoverMode #12596
CssBuilder/StyleBuilder: declare as readonly struct by @ScarletKuro in CssBuilder/StyleBuilder: declare as readonly struct #12598
Components: Migrate time-dependent logic to TimeProvider abstraction by @Copilot in Components: Migrate time-dependent logic to TimeProvider abstraction #12592
MudSelect/MudSelectItem: Improve communication between them by @Copilot in MudSelect/MudSelectItem: Improve communication between them #12582
MudSelect: Change SelectedValues to IReadOnlyCollection by @Copilot in MudSelect: Change SelectedValues to IReadOnlyCollection #12619
MudSelect: generic over object SelectOption by @ScarletKuro in MudSelect: generic over object SelectOption #12623
MudDataGrid: Add ability to continue editing by @ntark in MudDataGrid: Add ability to continue editing #12430
MudColorPicker: Fix support for null color values and throttling by @Dnawrkshp in MudColorPicker: Fix support for null color values and throttling #12567
MudFileUpload: Remove IActivator, Add defaults by @versile2 in MudFileUpload: Remove IActivator, Add defaults #10487
New Features
MudFabMenu: Add new component by @91378246 in MudFabMenu: Add new component #12097
MudDatePicker: Add keyboard navigation by @spingee in MudDatePicker: Add keyboard navigation #12028
MudFormComponent: Make Error two-way bindable by @ScarletKuro in MudFormComponent: Make Error two-way bindable #12138
MudFormComponent: Make ErrorId two-way bindable by @ScarletKuro in MudFormComponent: Make ErrorId two-way bindable #12140
Palette: Make PaletteLight and PaletteDark of type Palette by @danielchalmers in Palette: Make PaletteLight and PaletteDark of type Palette #12148
ThemeProvider: Add bind-CurrentPalette parameter by @Copilot in ThemeProvider: Add bind-CurrentPalette parameter #12149
MudHotkey: Add new component for handling hotkeys by @91378246 in MudHotkey: Add new component for handling hotkeys #12079
MudCheckBox: Improve Accessibility, Add mud-sr-only by @versile2 in MudCheckBox: Improve Accessibility, Add mud-sr-only #12123
MudSplitPanel: Add new component by @91378246 in MudSplitPanel: Add new component #12116
MudTable: Use theme typography for font styling by @Copilot in MudTable: Use theme typography for font styling #12152
MudFormComponent: change ReadValue() -> ReadValue by @ScarletKuro in MudFormComponent: change ReadValue() -> ReadValue #12310
Identifier: Optimize, make it public API by @Copilot in Identifier: Optimize, make it public API #12339
MudBaseInput: Make GetInputType protected by @ScarletKuro in MudBaseInput: Make GetInputType protected #12351
SequenceComparer: Optimize by using SequenceEqual by @ScarletKuro in SequenceComparer: Optimize by using SequenceEqual #12356
KeyInterceptorService: Add option to ignore repeat events when holding down keys by @JMolenkamp in KeyInterceptorService: Add option to ignore repeat events when holding down keys #12376
KeyInterceptorService: Allow omitting TargetClass to attach event handlers to element itself by @JMolenkamp in KeyInterceptorService: Allow omitting TargetClass to attach event handlers to element itself #12377
KeyInterceptorService: Do not observe dom when targeting the element itself by @JMolenkamp in KeyInterceptorService: Do not observe dom when targeting the element itself #12380
MudDataGrid: Allow disabling selection per row via criteria by @Anu6is in MudDataGrid: Allow disabling selection per row via criteria #11554
MudLink: Add StartIcon and EndIcon properties by @danielchalmers in MudLink: Add StartIcon and EndIcon properties #12407
MudRipple: Provide immediate visual feedback by @danielchalmers in MudRipple: Provide immediate visual feedback #12409
MudPicker & MudRangeInput: add customizable ClearIcon parameter by @pwasilewski in MudPicker & MudRangeInput: add customizable ClearIcon parameter #12425
Add TimeProvider support to DebounceDispatcher and ThrottleDispatcher by @Copilot in Add TimeProvider support to DebounceDispatcher and ThrottleDispatcher #12435
MudSelect: Add SelectionOnEnter, Improve Pager keyboard UX by @nccadman19 in MudSelect: Add SelectionOnEnter, Improve Pager keyboard UX #12405
MudCard: Add ContentPadding property by @91378246 in MudCard: Add ContentPadding property #12446
MudProgressLinear: Add ShowBackground by @91378246 in MudProgressLinear: Add ShowBackground #12443
MudTextField: Add insert functions by @91378246 in MudTextField: Add insert functions #12483
Popovers: Change default OverflowBehavior from FlipOnOpen to FlipAlways by @Copilot in Popovers: Change default OverflowBehavior from FlipOnOpen to FlipAlways #12411
MudVirtualize: Add MaxItemCount by @ScarletKuro in MudVirtualize: Add MaxItemCount #12536
MudTable: Add row disabled by @91378246 in MudTable: Add row disabled #12441
KeyInterceptorService: Add KeyCommand concept by @ScarletKuro in KeyInterceptorService: Add KeyCommand concept #12512
MudProgressCircular, MudProgressLinear: Add aria-busy attribute by @Copilot in MudProgressCircular, MudProgressLinear: Add aria-busy attribute #12586
MudForm: Add OnEnterPressed by @91378246 in MudForm: Add OnEnterPressed #12570
MudCheckBox, MudRadio, MudSwitch: Add or improve aria-label support by @danielchalmers in MudCheckBox, MudRadio, MudSwitch: Add or improve aria-label support #12591
MudDialogProvider: Add CloseOnNavigation to DialogOptions to optionally close dialogs on navigation by @aaronleev in MudDialogProvider: Add CloseOnNavigation to DialogOptions to optionally close dialogs on navigation #12437
MudDataGrid: Format pagination numbers with group separators and improve InfoFormat logic by @angusdumaresq in MudDataGrid: Format pagination numbers with group separators and improve InfoFormat logic #12605
MudExitPrompt: Add component by @91378246 in MudExitPrompt: Add component #12287
MudTable: Display numeric values in pager info with thousand separators by @angusdumaresq in MudTable: Display numeric values in pager info with thousand separators #12674
Bug Fixes
MudAutocomplete: Fix OpenChanged being called twice after clearing selection by @Yomodo in MudAutocomplete: Fix OpenChanged being called twice after clearing selection #12076
MudColor: Add MudColorComparer by @ScarletKuro in MudColor: Add MudColorComparer #12143
ParameterState: Fix Value edge case, fix nullability by @ScarletKuro in ParameterState: Fix Value edge case, fix nullability #12179
MudDateRangePicker: Fix StartMonth being ignored when DateRange is set by @ChristosMaragkos in MudDateRangePicker: Fix StartMonth being ignored when DateRange is set #12191
Analyzer: Fix MUD0012 analyzer false positive for Expression<Func<>> by @Copilot in Analyzer: Fix MUD0012 analyzer false positive for Expression<Func<>> #12216
ParameterState: Force "initialization" after SetValueAsync by @ScarletKuro in ParameterState: Force "initialization" after SetValueAsync #12242
MudDrawer: Fix CSS animation flicker on re-render by using transition instead of animation by @daveHylde in MudDrawer: Fix CSS animation flicker on re-render by using transition instead of animation #12279
MudFocusTrap: guard against disposal race in OnAfterRenderAsync by @jpacc260 in MudFocusTrap: guard against disposal race in OnAfterRenderAsync #12252
DebounceDispatcher: Fix race condition causing flaky test by @Copilot in DebounceDispatcher: Fix race condition causing flaky test #12334
MudDatePicker: Fix FixYear by @ScarletKuro in MudDatePicker: Fix FixYear #12372
MudPicker: Adds new style for disabled MudPicker static variant (Disabled property on static variant of MudDateRangePicker does not work #11761) by @dbarisakkurt in MudPicker: Adds new style for disabled MudPicker static variant (#11761) #12352
MudDateRangePicker: Implement ResetValueAsync for MudForm reset by @pwasilewski in MudDateRangePicker: Implement ResetValueAsync for MudForm reset #12390
MudBaseDatePicker: Ensure GetMonthStart always returns the first day of the month by @pwasilewski in MudBaseDatePicker: Ensure GetMonthStart always returns the first day of the month #12386
MudDatePicker: Prevent ArgumentOutOfRangeException at DateTime boundaries by @Copilot in MudDatePicker: Prevent ArgumentOutOfRangeException at DateTime boundaries #12378
DebounceDispatcher: Fix ObjectDisposedException from premature CTS disposal by @Copilot in DebounceDispatcher: Fix ObjectDisposedException from premature CTS disposal #12393
MudDatePicker: Fix keyboard navigation with fixed year/month by @spingee in MudDatePicker: Fix keyboard navigation with fixed year/month #12180
MudDialog: Prevent default on close button to not trigger validations by @spingee in MudDialog: Prevent default on close button to not trigger validations #12332
MudDataGrid: Fix stale selection/hierarchy cleanup by @Xsodia in MudDataGrid: Fix stale selection/hierarchy cleanup #12354
MudCollapse: Hide content after collapse animation by @Anu6is in MudCollapse: Hide content after collapse animation #12455
MudDataGrid: Fix SelectedItems event callback not firing due to shared reference by @Copilot in MudDataGrid: Fix SelectedItems event callback not firing due to shared reference #12511
MudMenu: Fix race condition, Improve test case reliability by @danielchalmers in MudMenu: Fix race condition, Improve test case reliability #12510
MudSelect: Replace async void with Task by @ScarletKuro in MudSelect: Replace async void with Task #12539
MudTheme: Don't cache supplied Theme parameter by @ScarletKuro in MudTheme: Don't cache supplied Theme parameter #12559
MudSwitch, MudCheckBox, MudRadio: Render content inside span by @danielchalmers in MudSwitch, MudCheckBox, MudRadio: Render content inside span #12590
MudSplitPanel: Fix splitter reset on double click even if drag occurs by @91378246 in MudSplitPanel: Fix splitter reset on double click even if drag occurs #12594
MudTable: Add accessible name to loading progress bar by @Copilot in MudTable: Add accessible name to loading progress bar #12618
ThrottleDispatcher: Fix not throttling fast-completing actions by @ScarletKuro in ThrottleDispatcher: Fix not throttling fast-completing actions #12636
MudTable: Cursor pointer behaves like v8 again by @RobbertK92 in MudTable: Cursor pointer behaves like v8 again #12680
MudChart: Fix Radial Chart Issues by @Anu6is in MudChart: Fix Radial Chart Issues #12685
New Contributors
@spingee made their first contribution in MudDatePicker: Add keyboard navigation #12028
@ChristosMaragkos made their first contribution in MudDateRangePicker: Fix StartMonth being ignored when DateRange is set #12191
@filipvalentin made their first contribution in MudTabs: Rename class properties; MudTabPanel: add PanelClass property #12156
@daveHylde made their first contribution in MudDrawer: Fix CSS animation flicker on re-render by using transition instead of animation #12279
@jpacc260 made their first contribution in MudFocusTrap: guard against disposal race in OnAfterRenderAsync #12252
@mcbodge made their first contribution in Docs: Add Mud MCP extension #12340
@JMolenkamp made their first contribution in KeyInterceptorService: Add option to ignore repeat events when holding down keys #12376
@dbarisakkurt made their first contribution in MudPicker: Adds new style for disabled MudPicker static variant (#11761) #12352
@fuguiKz made their first contribution in Docs: Clarify MudOverlay 'overlay view' #12355
@Xsodia made their first contribution in MudDataGrid: Fix stale selection/hierarchy cleanup #12354
@corvinsz made their first contribution in Docs: Fix typo in section header for Parameter State #12459
@wrakocy made their first contribution in Docs: Add notice to Masking page regarding use with Blazor Server #12454
@aaronleev made their first contribution in MudDialogProvider: Add CloseOnNavigation to DialogOptions to optionally close dialogs on navigation #12437
@angusdumaresq made their first contribution in MudDataGrid: Format pagination numbers with group separators and improve InfoFormat logic #12605
@ntark made their first contribution in MudDataGrid: Add ability to continue editing #12430
@Dnawrkshp made their first contribution in MudColorPicker: Fix support for null color values and throttling #12567
@RobbertK92 made their first contribution in MudTable: Cursor pointer behaves like v8 again #12680