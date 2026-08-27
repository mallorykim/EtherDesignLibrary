# L3 EtherSlider migration record

Date: 2026-08-26  
Status: preview implementation evidence, not stable-release readiness

`EtherSlider` is the seventh L3 slice. It is converted from a `UserControl` to a
templated `RangeBase` control so Minimum, Maximum, Value, and change steps use
the WinUI range contract. The bar-chart visual (63×2px ticks, 4px knob,
hover/press knob sizing) and click/drag interaction are preserved.

**Default visual:** `IsTabStop=True`, `UseSystemFocusVisuals=False`, range 0–100
starting at 50, `SmallChange=1`, `LargeChange=10`. Call sites that already used
the implicit style with an explicit `Value` are unchanged.

## Implemented contract

- The constructor sets `DefaultStyleKey`; `DefaultEtherSliderStyle` owns the
  default setters and template, and the implicit style is `BasedOn` that keyed
  style. The hand cursor stays in the constructor.
- The component declares `ValueText`, `BarCanvas`, `Knob`, and four collapsed brush-source
  parts (`HighlightBrushSource`, `InactiveBrushSource`, `KnobBrushSource`,
  `KnobPressedBrushSource`). There are no `[TemplateVisualState]` attributes
  because no VisualStateManager groups are driven; bar fill, `Canvas.Left`, and
  knob hover/press sizing stay code-driven against template-declared rectangles.
- Light, Dark, and HighContrast expose the same five `EtherSlider*` component
  resources. The template consumes those component keys only for color-bearing
  ThemeResources. Light and Dark alias the previous product tokens
  (`ActionPrimaryBg`, `ActionPrimaryBgPressed`, `BackgroundTrack`,
  `text/primary`). High Contrast uses Windows `SystemColor*` dynamic resources
  rather than changing frozen Foundation tokens.
- `GetThemeColor` string lookups are removed. Code-generated bars and the knob
  copy `Background` from collapsed template `Border` parts whose fills are
  `{ThemeResource EtherSlider*}` (`HighlightBrushSource`, `InactiveBrushSource`,
  `KnobBrushSource`, `KnobPressedBrushSource`). Generated rectangles bind `Fill`
  to those sources so Light/Dark rebind follows the element's `ActualTheme`.
- `FormatValue(double)` remains a public instance method.
- Gallery specimens keep the interactive slider plus the 25% and 75% states,
  each with an identifiable automation name.
- The private automation peer reports `Slider`, exposes a live
  `IRangeValueProvider` via `GetPattern(PatternInterface.RangeValue)`, accepts
  `SetValue` while enabled, rejects `SetValue` when disabled, and raises the
  RangeValue Value property event when the owner value changes.

## Direct-mutation exceptions

These stay code-driven because pointer capture and knob hover/press sizing
should not be rewritten onto VisualStateManager:

- `UpdateBarLayout()` assigns highlight/inactive fills and `Canvas.Left` on the
  63 template-declared bar rectangles whenever Value, range, theme, or enabled
  state changes.
- The knob is the `Knob` template part. Hover/press expand it from
  4×45 / radius 2 to 6×45 / radius 4 and swap the pressed component brush.
- The value label text and left margin follow the knob.
- Pointer capture on `BarCanvas` maps X to a 0–63 slot, then to the current
  Minimum–Maximum span with integer rounding, matching the previous drag feel
  on the default 0–100 range.

Keyboard stepping (arrows, PageUp/PageDown, Home/End) is added with the
RangeBase conversion so the control can be a tab stop with a live RangeValue
peer. Disabled interaction is gated on `IsEnabled`; there is no Gallery
`PreviewStatus` surface.

## Pinned-source parity

| Topic | WinUI `Slider` (Windows App SDK) | EtherSlider | Notes |
| --- | --- | --- | --- |
| Base type | `Slider` / `RangeBase` | `RangeBase` | Bar-chart chrome is not a WinUI Slider template clone. |
| Default style | `DefaultStyleKey` + generic implicit style | Same pattern via `DefaultEtherSliderStyle` | Matches the L2 exemplar and WinUI templated-control guidance. |
| Automation | RangeValue, `Slider` | Same: `GetPattern(RangeValue)`, `Slider` | Interactive `SetValue`; disabled locks the provider. |
| Thumb / track | Fluent thumb and track | 63-bar chart + 4px knob | Product visual; bars and knob are template-declared. |
| Keyboard | Arrow / page / home / end | Same stepping via RangeBase change properties | Added with this conversion. |

A line-by-line template clone of WinUI `Slider` is deferred: Ether's bar-chart
ticks and knob sizing are product design.

## Evidence and remaining exceptions

The unpackaged NuGet consumer fixture mounts default and interactive
`EtherSlider` instances on a UI thread. Its `slider` marker records keyed
default-style resolution (range 0–100 starting at 50, `SmallChange=1`,
`LargeChange=10`, `UseSystemFocusVisuals=False`, `IsTabStop`, and template),
ValueText / BarCanvas parts, a 65% highlighted-bar ratio, `FormatValue`,
`GetPattern(PatternInterface.RangeValue)`, accepted interactive `SetValue`,
disabled locking the provider, and Light/Dark template brush rebind.
`Verify-EtherSliderContract.ps1` additionally verifies metadata, keyed/implicit
styles, theme-key symmetry, High Contrast system resources, no literal template
hex colors, gallery automation names, runtime evidence fields, and CI wiring.

PublicAPI drops UserControl generated members (`InitializeComponent`, `Connect`,
`GetBindingConnector`) and the local `Value` / `ValueProperty` now inherited
from `RangeBase`. `FormatValue` is retained.

This is structured runtime evidence, not a pixel screenshot baseline. The
High Contrast check is a static resource contract, not an on-device
verification across all Windows contrast themes. Formal Appium and
Accessibility Insights coverage remain L4 work. The repository's known
frozen global-token High Contrast deficit remains a release blocker; this
component does not alter those tokens or claim stable readiness.
