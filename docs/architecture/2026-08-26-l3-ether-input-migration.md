# L3 EtherInput migration record

Date: 2026-08-26  
Status: preview implementation evidence, not stable-release readiness

`EtherInput` is the third L3 slice migrated onto the L2 `EtherProgressBar` /
L3 `EtherButton` / `EtherCheckbox` exemplar contract. It remains a native
`TextBox` subclass. It does not add a clear button, header/description slots,
indeterminate visuals, animation APIs, or a FocusStates group (focus is the
brand-blue CommonStates border).

**Default visual:** Inter Regular 14, padding `16,12` (Figma 16
  horizontal, 12 vertical; 48 px field keeps a ~22 px line slot), MinHeight
  and MaxHeight 48.
  `ContentElement` uses `Margin="{TemplateBinding BorderThickness}"` like
  `DefaultTextBoxStyle`. Placeholder is centered in that same padded slot so
  it shares the typed text midline (`TextBlock` otherwise top-aligns).
`UseSystemFocusVisuals=False`, existing single-border template. Call sites
that already used the implicit style are unchanged (this control has no extra
named styles).

## Implemented contract

- The constructor sets `DefaultStyleKey`; `DefaultEtherInputStyle` owns the
  default setters and template, and the implicit style is `BasedOn` that keyed
  style.
- The component declares the template parts the template actually uses
  (`LayoutRoot`, `BorderElement`, `PlaceholderTextContentPresenter`,
  `ContentElement`) and CommonStates (Normal / PointerOver / Focused /
  Disabled). Native `TextBox` still drives those states. Corner radius uses
  `RadiusMd`; `BorderElement` template-binds `CornerRadius` like
  `DefaultTextBoxStyle`.
- Light, Dark, and HighContrast expose the same seven `EtherInput*` component
  resources. The template consumes those component keys only for color-bearing
  ThemeResources. Light and Dark bind `Color` to primitive tokens (Figma Light
  set `62102:10891`, Dark specimens `62110:12110`–`12114`). Default/Hover/Filled
  stroke is `AlphaWhite0` / `AlphaBlack0`; Active is `Blue500` / `Blue400`.
  High Contrast uses Windows `SystemColor*` dynamic resources rather than
  changing frozen Foundation tokens.
- Gallery specimens keep the existing state matrix and add identifiable
  automation names on the interactive, default, hover, active, filled, and
  disabled specimens.

## Pinned-source parity

| Topic | WinUI `TextBox` (Windows App SDK) | EtherInput | Notes |
| --- | --- | --- | --- |
| Base type | `TextBox` | `TextBox` | Native editing, placeholder, selection, and CommonStates are reused. |
| Default style | `DefaultStyleKey` + generic implicit style | Same pattern via `DefaultEtherInputStyle` | Matches the L2 exemplar and WinUI templated-control guidance. |
| Template parts | `ContentElement`, placeholder presenter, optional delete button / header | `ContentElement`, `PlaceholderTextContentPresenter`, `BorderElement`, `LayoutRoot` | Ether keeps the current single-border field; delete button and header/description are not in today's spec. |
| Corner radius | `CornerRadius` on the control; `BorderElement` template-binds it | Same, default `RadiusMd` (8px) | Primitive token, not the `radius/control` alias. |
| Focus | System focus visuals and/or FocusStates | `UseSystemFocusVisuals=False`; CommonStates `Focused` brand-blue border | Preserves today's Ether spec. |
| Clear button | Optional `Button` part | Not present | Not invented. |

A line-by-line template clone of WinUI `TextBox` is deferred: Ether's 8px
radius, Inter Regular 14, and 40%/60% fill hover are product design, not
missing Fluent chrome.

## Evidence and remaining exceptions

The unpackaged NuGet consumer fixture mounts default and interactive
`EtherInput` instances on a UI thread. Its `input` marker records keyed
default-style resolution (`FontSize` 14 plus `UseSystemFocusVisuals=False`
and template), template parts, all four CommonStates (hover fill, focus
border, disabled 50% opacity), automation name, and Light/Dark placeholder
foreground rebind (`AlphaBlack70` vs `AlphaWhite70`). Fill and border
brushes also differ by theme in the dictionaries, but CommonStates
VisualState setters apply a local `Background`/`BorderBrush` that does not
re-evaluate `ThemeResource` on theme change; the fixture therefore samples
the live placeholder pair, the same way Checkbox samples label instead of
unchecked fill. Disabled opacity is asserted after `IsEnabled=false` plus
`VisualStateManager.GoToState("Disabled")`: `IsEnabled=false` alone did not
apply the template's `LayoutRoot.Opacity=0.5` setter after a prior forced
Focused state. Gallery disabled specimens still set `IsEnabled=False` in
XAML before load. The consumer verifier binds the JSON `input` property to
`$etherInput` because PowerShell's automatic `$input` variable would hide
the marker. `Verify-EtherInputContract.ps1` additionally verifies
metadata, keyed/implicit styles, theme-key symmetry, High Contrast system
resources, no literal template hex colors, and the required runtime evidence
fields.

This is structured runtime evidence, not a pixel screenshot baseline. The
High Contrast check is a static resource contract, not an on-device
verification across all Windows contrast themes. Formal Appium and
Accessibility Insights coverage remain L4 work. The repository's known
frozen global-token High Contrast deficit remains a release blocker; this
component does not alter those tokens or claim stable readiness.
