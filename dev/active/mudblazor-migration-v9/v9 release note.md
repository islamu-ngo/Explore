Breaking Changes
MudSwitch: Decouple label font size from Size property by @danielchalmers in #11132
Deprecate some public Style properties by @danielchalmers in #11407
MudDataGrid: Add CancellationToken support for ServerData by @w3ori in #11841
ThemeProvider: Rename ObserveSystemThemeChange to ObserveSystemDarkModeChange and remove obsolete methods by @Copilot in #12022
MudChart: Chart Type Unification + 2 New Charts by @Anu6is in #11458
MudGlobal: Remove deprecated theming properties by @Copilot in #12141
Popovers: Make overlay popovers non-modal by default by @Copilot in #12101
v9: Remove all code marked obsolete/deprecated by @Copilot in #12142
MudSnackbar: Make snackbar require interaction when action present by @danielchalmers in #12108
MudFormComponent: Rename methods to async, use await by @ScarletKuro in #12147
MudLink: Inherit typography by default by @danielchalmers in #12094
MudColorPicker: Use ParameterState by @ScarletKuro in #10357
MudTreeView: Add ITreeItemData and make Children IReadOnlyCollection by @danielchalmers in #12090
Converters: Rework conversion system by @ScarletKuro in #12177
ResizeObserver: Resizes observer and bounding client rect cleanup by @91378246 in #12173
Popovers: Remove and Replace DropdownSettings by @versile2 in #12186
MudTabs: Rename class properties; MudTabPanel: add PanelClass property by @filipvalentin in #12156
MudStepper: Add IStepContext by @ScarletKuro in #12212
MudSelect: Use ParameterState for MultiSelection by @ScarletKuro in #12241
MudSelect: use ParameterState by @Copilot in #12244
MudBaseInput: Use ParameterState for Text by @Copilot in #12259
MudBaseInput: ParameterState for Value by @ScarletKuro in #12267
MudBooleanInput: Use ParameterState for Value by @ScarletKuro in #12273
DialogService: Rename ShowMessageBox to ShowMessageBoxAsync by @91378246 in #12292
MudPopover: Remove MudGlobal.PopoverDefaults and move properties to PopoverOptions by @Copilot in #12286
MudDialog: Move DefaultFocus from MudGlobal.DialogDefaults to MudDialogProvider by @Copilot in #12297
MudPopover: Remove OverflowBehavior property from components, use PopoverOptions directly by @Copilot in #12298
MudPopover: Move transition defaults from MudGlobal to PopoverOptions by @danielchalmers in #12300
MudInput: Use bind:get/bind:set by @ScarletKuro in #12272
MudBaseInput: Remove TextUpdateSuppression by @ScarletKuro in #12306
Refactor: Improve MudDebouncedInput, ThrottleDispatcher, DebounceDispatcher by @Copilot in #12296
MudBaseInput & MudFormComponent: Fix API naming inconsistency by @Copilot in #12312
MudMenu: Replace IActivatable with MenuContext by @Copilot in #12145
Masking: Improve abstraction, add more tests by @Copilot in #12314
Range: Remove setters by @ScarletKuro in #12319
MudChat: Remove components in v9, redirect to MudX by @Copilot in #12151
ParameterState: Add ResolveEffectiveParameter, ParameterStateCollection for shared handlers by @ScarletKuro in #12347
MudFormComponent: Add GetDefaultConverter, cleanup by @ScarletKuro in #12365
MudSplitPanel: Add get and set divider position functions by @91378246 in #12370
MudInput: Replace AutoGrow with Sizing by @danielchalmers in #12417
ScrollListener: Add report rate and GetCurrentScrollDataAsync by @91378246 in #12183
MudTextField: Add async postfixes by @91378246 in #12484
Components: SetTextAsync->SetTextCoreAsync, SetValueAsync->SetValueCoreAsync by @ScarletKuro in #12489
MudDialogContainer: Rename OnMouseUp to OnMouseUpAsync, make private by @ScarletKuro in #12514
Remove EventListener / EventManager by @ScarletKuro in #12532
MudThemeProvider: Fix script and refactor by @meenzen in #12534
MudBaseInput: Remove ForceUpdate by @ScarletKuro in #12542
MudSelect: Add two-way Open parameter by @ScarletKuro in #12589
PopoverOptions: Remove PopoverMode by @ScarletKuro in #12596
CssBuilder/StyleBuilder: declare as readonly struct by @ScarletKuro in #12598
Components: Migrate time-dependent logic to TimeProvider abstraction by @Copilot in #12592
MudSelect/MudSelectItem: Improve communication between them by @Copilot in #12582
MudSelect: Change SelectedValues to IReadOnlyCollection by @Copilot in #12619
MudSelect: generic over object SelectOption by @ScarletKuro in #12623
MudDataGrid: Add ability to continue editing by @ntark in #12430
MudColorPicker: Fix support for null color values and throttling by @Dnawrkshp in #12567
MudFileUpload: Remove IActivator, Add defaults by @versile2 in #10487
New Features
MudFabMenu: Add new component by @91378246 in #12097
MudDatePicker: Add keyboard navigation by @spingee in #12028
MudFormComponent: Make Error two-way bindable by @ScarletKuro in #12138
MudFormComponent: Make ErrorId two-way bindable by @ScarletKuro in #12140
Palette: Make PaletteLight and PaletteDark of type Palette by @danielchalmers in #12148
ThemeProvider: Add bind-CurrentPalette parameter by @Copilot in #12149
MudHotkey: Add new component for handling hotkeys by @91378246 in #12079
MudCheckBox: Improve Accessibility, Add mud-sr-only by @versile2 in #12123
MudSplitPanel: Add new component by @91378246 in #12116
MudTable: Use theme typography for font styling by @Copilot in #12152
MudFormComponent: change ReadValue() -> ReadValue by @ScarletKuro in #12310
Identifier: Optimize, make it public API by @Copilot in #12339
MudBaseInput: Make GetInputType protected by @ScarletKuro in #12351
SequenceComparer: Optimize by using SequenceEqual by @ScarletKuro in #12356
KeyInterceptorService: Add option to ignore repeat events when holding down keys by @JMolenkamp in #12376
KeyInterceptorService: Allow omitting TargetClass to attach event handlers to element itself by @JMolenkamp in #12377
KeyInterceptorService: Do not observe dom when targeting the element itself by @JMolenkamp in #12380
MudDataGrid: Allow disabling selection per row via criteria by @Anu6is in #11554
MudLink: Add StartIcon and EndIcon properties by @danielchalmers in #12407
MudRipple: Provide immediate visual feedback by @danielchalmers in #12409
MudPicker & MudRangeInput: add customizable ClearIcon parameter by @pwasilewski in #12425
Add TimeProvider support to DebounceDispatcher and ThrottleDispatcher by @Copilot in #12435
MudSelect: Add SelectionOnEnter, Improve Pager keyboard UX by @nccadman19 in #12405
MudCard: Add ContentPadding property by @91378246 in #12446
MudProgressLinear: Add ShowBackground by @91378246 in #12443
MudTextField: Add insert functions by @91378246 in #12483
Popovers: Change default OverflowBehavior from FlipOnOpen to FlipAlways by @Copilot in #12411
MudVirtualize: Add MaxItemCount by @ScarletKuro in #12536
MudTable: Add row disabled by @91378246 in #12441
KeyInterceptorService: Add KeyCommand concept by @ScarletKuro in #12512
MudProgressCircular, MudProgressLinear: Add aria-busy attribute by @Copilot in #12586
MudForm: Add OnEnterPressed by @91378246 in #12570
MudCheckBox, MudRadio, MudSwitch: Add or improve aria-label support by @danielchalmers in #12591
MudDialogProvider: Add CloseOnNavigation to DialogOptions to optionally close dialogs on navigation by @aaronleev in #12437
MudDataGrid: Format pagination numbers with group separators and improve InfoFormat logic by @angusdumaresq in #12605
MudExitPrompt: Add component by @91378246 in #12287
MudTable: Display numeric values in pager info with thousand separators by @angusdumaresq in #12674