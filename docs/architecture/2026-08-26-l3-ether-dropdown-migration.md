# L3 EtherDropdown migration record

Date: 2026-08-26  
Status: preview implementation evidence, not stable-release readiness

`EtherDropdown` is the fourth L3 slice migrated onto the L2 `EtherProgressBar` /
L3 Button / Checkbox / Input exemplar contract. It remains a native `ComboBox`
subclass. Existing ComboBox workarounds are preserved: the framework-owned
`ContentPresenter` stays hidden, selection is shown through `TriggerText`,
the items panel is a `StackPanel` rather than `CarouselPanel`, open versus
hover fills are split so menu pointer events cannot tint the trigger, and
content-hugging width continues to come from `MeasureOverride`.

**Default visual:** Inter Medium 11, `Padding8`, MinHeight 32, MinWidth 130,
`MaxVisibleItems` 6, `MenuGap` 4, `UseSystemFocusVisuals=False`, keyed
`EtherDropdownItem` containers. Call sites that already used the implicit
style are unchanged. `EtherDropdown` remains a keyed alias of
`DefaultEtherDropdownStyle` for older `StaticResource` call sites.

## Implemented contract

- The constructor sets `DefaultStyleKey`; `DefaultEtherDropdownStyle` owns the
  default setters and template, and the implicit style is `BasedOn` that keyed
  style. `MaxVisibleItems` and `MenuGap` metadata defaults are unset (0) so
  the keyed style supplies the product values.
- The component declares the five existing template parts (`TriggerText`,
  `Arrow`, `Popup`, `PopupBorder`, `ScrollViewer`) plus CommonStates
  (Normal / PointerOver / Pressed / Disabled), FocusStates (Focused /
  Unfocused), and DropDownStates (Opened / Closed). Native `ComboBox` still
  drives those states.
- Light, Dark, and HighContrast expose the same ten `EtherDropdown*`
  component resources. The control and item templates consume those component
  keys only for color-bearing ThemeResources. Light and Dark retain the
  previous token aliases (`BackgroundDropdown*`, `TextSecondary`,
  `BorderDropdownActive`, `border/focus`). High Contrast uses Windows
  `SystemColor*` dynamic resources rather than changing frozen Foundation
  tokens.
- Gallery specimens keep the existing state matrix and add identifiable
  automation names on the interactive, default, hover, pressed, open, and
  disabled specimens.

## Pinned-source parity

| Topic | WinUI `ComboBox` (Windows App SDK) | EtherDropdown | Notes |
| --- | --- | --- | --- |
| Base type | `ComboBox` | `ComboBox` | Native selection, popup, CommonStates, FocusStates, and DropDownStates are reused. |
| Default style | `DefaultStyleKey` + generic implicit style | Same pattern via `DefaultEtherDropdownStyle` | Matches the L2 exemplar and WinUI templated-control guidance. |
| ContentPresenter | Framework-owned part; ComboBox clears Content while open | Kept, collapsed; `TriggerText` shows the selection | Existing ComboBox workaround; renaming the part breaks DropDownStates. |
| Items panel | `CarouselPanel` | `StackPanel` | Existing workaround so the host ScrollViewer reports a real extent. |
| Focus | System focus visuals and/or FocusStates | `UseSystemFocusVisuals=False`; FocusStates `Focused` 2 px ring | Preserves today's Ether spec. |

A line-by-line template clone of WinUI `ComboBox` is deferred: Ether's compact
32 px trigger, content-hugging width, gradient open stroke, and 6-item scroll
cap are product design, not missing Fluent chrome.

## Evidence and remaining exceptions

The unpackaged NuGet consumer fixture mounts default and interactive
`EtherDropdown` instances on a UI thread. Its `dropdown` marker records keyed
default-style resolution (`MaxVisibleItems` 6 plus `UseSystemFocusVisuals=False`,
item container style, and template), TriggerText / Arrow / Popup in the closed
visual tree, TriggerText selection proof with collapsed ContentPresenter,
Opened/Closed DropDownStates via `GoToState` (no live popup open, to avoid
flaky timing), automation name, and Light/Dark trigger foreground rebind
(`TextSecondary` / `Gray1000` vs `Gray0`). `PopupBorder` and `ScrollViewer`
remain `[TemplatePart]`s. ComboBox does not parent those parts into the closed
visual tree; the fixture walks `Popup.Child` while `popup.IsOpen` is false so
the closed menu parts are still proven. `Verify-EtherDropdownContract.ps1`
additionally verifies metadata, keyed/implicit/alias styles, `EtherDropdownItem`,
theme-key symmetry, High Contrast system resources, no literal template hex
colors, and the required runtime evidence fields.

This is structured runtime evidence, not a pixel screenshot baseline. The
High Contrast check is a static resource contract, not an on-device
verification across all Windows contrast themes. Formal Appium and
Accessibility Insights coverage remain L4 work. The repository's known
frozen global-token High Contrast deficit remains a release blocker; this
component does not alter those tokens or claim stable readiness.
