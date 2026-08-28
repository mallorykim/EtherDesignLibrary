# L3 EtherSlider migration record

Date: 2026-08-26  
Status: preview implementation evidence, not stable-release readiness

`EtherSlider` is the seventh L3 slice. It is converted from a `UserControl` to a
templated `RangeBase` control so Minimum, Maximum, Value, and change steps use
the WinUI range contract. The bar-chart visual (even-count 2px ticks, 4px knob,
hover/press knob sizing) stretches with the parent width. Tick count stays even
and follows Figma density (2 epx bar + 3 epx gap; leftover width goes into
gaps). Max 512 ticks.

**Default visual:** `IsTabStop=True`, `UseSystemFocusVisuals=False`, range 0–100
starting at 50, `SmallChange=1`, `LargeChange=10`. Call sites that already used
the implicit style with an explicit `Value` are unchanged.

## Implemented contract

- The constructor sets `DefaultStyleKey`; `DefaultEtherSliderStyle` owns the
  default setters and template, and the implicit style is `BasedOn` that keyed
  style. The hand cursor stays in the constructor. The constructor also creates
  an empty `Labels` collection so XAML content (`<x:String>…</x:String>`) can
  append. Empty Labels generate even Minimum–Maximum ticks (0–100 → 0, 20, 40,
  60, 80, 100).
- The component declares `ValueText`, `BarCanvas`, `Knob`, `LabelRow`, and four
  collapsed brush-source parts (`HighlightBrushSource`, `InactiveBrushSource`,
  `KnobBrushSource`, `KnobPressedBrushSource`). There are no `[TemplateVisualState]`
  attributes because no VisualStateManager groups are driven; bar fill,
  `Canvas.Left`, knob hover/press/keyboard-focus sizing, and title/label visibility stay
  code-driven. Tick bars are generated to an even count (`MinBarCount` 8,
  `MaxBarCount` 512) that fills the stretched track at 2+3 epx density; the template declares
  only `Knob`. Title and tick labels are
  optional (`ShowTitle` / `ShowLabels`). The template declares 11 label slots
  (`MaxLabelCount`) in a single-cell row; unused slots collapse. Labels are UI
  only. The payload is always inherited `RangeBase.Value` (`double`).
- Pointer, keyboard, and UI Automation input round to the nearest integer
  (`Math.Round`) when `SnapToStops` is false, so a default 0–100 slider emits
  `0, 1, …, 100`. Programmatic `Value` assignment is not rounded unless snap is
  on. `SnapToStops` uses explicit `Stops` or even positions inferred from
  `Labels` (5 labels → 0, 25, 50, 75, 100). Enabling snap, changing Stops or
  Labels while snap is on, or assigning `Value` while snap is on coerces onto
  the nearest stop.
- Light, Dark, and HighContrast expose the same six `EtherSlider*` component
  resources. The template consumes those component keys only for color-bearing
  ThemeResources. Light and Dark bind `Color` to primitive tokens (Figma Light
  Tick slider `62181:5284`, Dark `62181:5341`: highlight `AlphaBrandGlow65`,
  knob `Blue600`/`Blue500`, unselected `Gray300`/`Gray600`, title
  `Gray1000`/`Gray0`, labels `AlphaBlack70`/`AlphaWhite70`). High Contrast uses
  Windows `SystemColor*` dynamic resources rather than changing frozen Foundation
  tokens. The bar canvas uses unthemed `EtherSliderTransparentBrush`. Title is
  Instrument Sans SemiBold 14 at 18 epx, 4 epx above the track. Tick labels are
  Inter Medium 11, 4 epx below the track.
- `GetThemeColor` string lookups are removed. Code-generated bars and the knob
  copy `Background` from collapsed template `Border` parts whose fills are
  `{ThemeResource EtherSlider*}` (`HighlightBrushSource`, `InactiveBrushSource`,
  `KnobBrushSource`, `KnobPressedBrushSource`). Generated rectangles bind `Fill`
  to those sources so Light/Dark rebind follows the element's `ActualTheme`.
- `FormatValue(double)` remains a public instance method.
- Gallery INTERACTIVE covers continuous 0–100 and named labels with snap.
  STATES covers 25/75, title/labels/track chrome, named labels with and without
  snap, explicit `Stops`, and a custom 0–10 range. Each specimen has an
  identifiable automation name. Bind `Value` TwoWay for the backend payload.
- The private automation peer reports `Slider`, exposes a live
  `IRangeValueProvider` via `GetPattern(PatternInterface.RangeValue)`, accepts
  `SetValue` while enabled (including no-op values that round to the current
  `Value`), rejects `SetValue` when disabled, and raises the
  RangeValue Value property event when the owner value changes.

## Direct-mutation exceptions

These stay code-driven because pointer capture and knob hover/press sizing
should not be rewritten onto VisualStateManager:

- `UpdateBarLayout()` creates an even tick count for the current track width
  at Figma density (2 epx bar + 3 epx gap). Fill is `Round(ratio × barCount)`.
- The knob is the `Knob` template part. Hover, press, and keyboard focus expand it from
  4×45 / radius 2 to 6×45 / radius 4. Press swaps the pressed component brush.
  `UseSystemFocusVisuals` stays False so custom High Contrast brushes are not
  overdrawn; Tab focus is the hover-sized knob, not a system halo.
- Title visibility follows `ShowTitle` and a non-empty `Title`. Tick labels
  follow `ShowLabels` and `Labels` (capped at 11) or even auto-generated range
  values (0–100 → 0, 20, 40, 60, 80, 100). `Stops` / `SnapToStops` match
  `EtherSteeringBar` for explicit `DoubleCollection` values, and also infer even
  positions from Labels when Stops is empty. Enabling snap coerces `Value`.
- Pointer capture on `BarCanvas` maps X to a 0–N slot, then to the current
  Minimum–Maximum span with integer rounding.

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
| Thumb / track | Fluent thumb and track | Even-count bar chart + 4px knob | Product visual; ticks are generated to fill the parent width. |
| Keyboard | Arrow / page / home / end | Same stepping via RangeBase change properties; keyboard focus uses the hover knob size | System focus visuals stay off (Ether HC convention). |
| Continuous value | Live `double` | Interactive input rounds to integers; snap uses stop values | Labels/title are not the payload. |

A line-by-line template clone of WinUI `Slider` is deferred: Ether's bar-chart
ticks and knob sizing are product design.

## Evidence and remaining exceptions

The unpackaged NuGet consumer fixture mounts default and interactive
`EtherSlider` instances on a UI thread. Its `slider` marker records keyed
default-style resolution (range 0–100 starting at 50, `SmallChange=1`,
`LargeChange=10`, `UseSystemFocusVisuals=False`, `IsTabStop`, and template),
ValueText / BarCanvas parts, a 65% highlighted-bar ratio across the even tick
count, `FormatValue`,
`GetPattern(PatternInterface.RangeValue)`, accepted interactive `SetValue`,
no-op `SetValue` after integer rounding, disabled locking the provider,
named `Labels`, `SnapToStops` coercing 47 → 50, `MoveToNextStop` /
`MoveToPreviousStop`, explicit `Stops`, and Light/Dark template brush rebind.
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
