# L3 EtherSteeringBar migration record

Date: 2026-08-26  
Status: preview implementation evidence, not stable-release readiness

`EtherSteeringBar` is the sixth L3 slice migrated onto the L2 `EtherProgressBar`
exemplar contract. It remains a custom `Control` (not `RangeBase`) with pointer
capture, keyboard stepping, optional stop snapping, and a live
`IRangeValueProvider`. Interaction behavior is preserved. `PreviewStatus`
remains Gallery-oriented and is still on the public surface; removing it would
be a PublicAPI breaking change.

**Default visual:** `IsTabStop=True`, `UseSystemFocusVisuals=False`, range 0–100
starting at 0, `SmallChange=1`, `LargeChange=10`, title and value labels shown.
Painted track and fill are 6 epx tall with CornerRadius 3 (pill ends). The
13×21 glass thumb is vertically centered on that track inside the 16 epx hit
strip. Labels sit 8 epx above the painted track (LabelRow margin 3 plus 5
inset). Knob fill is a Toolkit `AcrylicBrush` with BlurAmount 6 so the 21×13
glass actually shows the track. The hairline stroke is omitted. Thumb drop
shadow is `AttachedCardShadow`. `Padding` lives on the style and
template-binds onto `LayoutRoot` (`0,0,0,6` shadow bleed). Call sites that
already used the implicit style are unchanged.

## Implemented contract

- The constructor sets `DefaultStyleKey`; `DefaultEtherSteeringBarStyle` owns
  the default setters and template, and the implicit style is `BasedOn` that
  keyed style. The hand cursor stays in the constructor.
- The component declares `LayoutRoot` plus its thirteen interaction/label
  template parts and `LabelStates` (BothLabelsVisible / TitleOnly / ValueOnly /
  LabelsHidden). Label visibility is driven with `VisualStateManager`. Title
  content uses `TemplateBinding`; the value label still formats `Value` as a
  percent when `ValueContent` is unset. Track, fill, stop-marker, shadow-well,
  and thumb chrome are `AccessibilityView=Raw` so RangeValue stays on the
  control. The hit strip uses unthemed `EtherSteeringBarTransparentBrush`.
- Light, Dark, and HighContrast expose the same sixteen `EtherSteeringBar*`
  component resources. The template consumes those component keys only for
  color-bearing ThemeResources. Light and Dark bind track, title, value, and
  knob stroke `Color` to primitives (`Gray200` / `Gray600`, `Gray1000` /
  `Gray0`, `AlphaBlack70` / `AlphaWhite70`, `Gray0` / `Gray600`). Fill stays
  Figma Gradient-100 hex (`62128:25881` / `62134:28164`). Knob fill is in-app
  `AcrylicBrush` (Light near-white frost, Dark `Gray700` tint). Light knob
  stroke is `Gray0`; Dark stroke is `Gray600`. High Contrast uses Windows
  `SystemColor*` dynamic resources rather
  than changing frozen Foundation tokens. Decorative thumb shadows map to
  `SystemColorWindowColor`.
- Gallery specimens keep the continuous and snapped interactive bars plus the
  four label/preview combinations, each with an identifiable automation name.
- The private automation peer reports `Slider`, exposes a live
  `IRangeValueProvider` via `GetPattern(PatternInterface.RangeValue)`, accepts
  `SetValue` while interactive, rejects `SetValue` when `PreviewStatus` is
  forced or the control is disabled, falls back to a string title when the
  normal automation name is empty, and raises the RangeValue Value property
  event when the owner value changes.

## Direct-mutation exceptions

These stay code-driven because moving them onto VisualStateManager would
rewrite pointer capture, hover thumb sizing, stop-marker generation, and
Gallery `PreviewStatus` forcing:

- Fill width and thumb position follow `Value` during drag and keyboard input.
  At minimum the fill is still half the thumb width so the acrylic
  glass has a color seat; `Value` stays 0. Mid-range fill is unchanged.
- Hover enlarges the thumb (21×13 → 23×15) only while the pointer is over
  the knob, not the rest of the 16 epx hit strip. Drag on the bar is unchanged.
- Thumb drop shadow is `AttachedCardShadow` on `ThumbHost` (offset 0,2, blur 8).
- Disabled / `PreviewStatus=Disabled` swaps opacity on track, fill, shadows,
  and the disabled thumb shell.
- Stop markers are generated in code from `Stops` and consume
  `EtherSteeringBarStopMarker*` component brushes (no remaining hex fallbacks).

`PreviewStatus` is kept. It is a Gallery showcase affordance, not a product
interaction API, but it is already shipped in PublicAPI.Unshipped.

## Pinned-source parity

| Topic | WinUI `Slider` (Windows App SDK) | EtherSteeringBar | Notes |
| --- | --- | --- | --- |
| Base type | `Slider` / `RangeBase` | `Control` | Existing custom range + thumb; not converted to RangeBase in this slice. |
| Default style | `DefaultStyleKey` + generic implicit style | Same pattern via `DefaultEtherSteeringBarStyle` | Matches the L2 exemplar and WinUI templated-control guidance. |
| Automation | RangeValue, often `Slider` | Same: `GetPattern(RangeValue)`, `Slider` | Interactive `SetValue`; PreviewStatus/disabled lock the provider. |
| Thumb / track | Fluent chrome | Ether glass thumb + four-stop fill | Product visual, not a WinUI template clone. |
| Preview states | N/A | `PreviewStatus` | Gallery-oriented; retained with PublicAPI impact noted. |

A line-by-line template clone of WinUI `Slider` is deferred: Ether's glass
thumb, stop markers, and Gallery preview forcing are product design. Slider
itself remains a later L3 slice (`UserControl` conversion).

## Evidence and remaining exceptions

The unpackaged NuGet consumer fixture mounts default and interactive
`EtherSteeringBar` instances on a UI thread. Its `steeringBar` marker records
keyed default-style resolution (range 0–100, labels on,
`UseSystemFocusVisuals=False`, `IsTabStop`, and template), LayoutRoot /
InteractionSurface / FillBorder / ThumbHost / LabelRow / TitleText /
ValueLabel, four LabelStates
transitions, a 65% fill ratio, `GetPattern(PatternInterface.RangeValue)`,
accepted interactive `SetValue`, PreviewStatus locking the provider, Light/Dark
fill gradient stops, and Light/Dark template brush rebind. Stop-marker hosts
and shadow/thumb chrome remain `[TemplatePart]`s and are exercised by layout
updates rather than by opening a popup. `Verify-EtherSteeringBarContract.ps1`
additionally verifies metadata, keyed/implicit styles, theme-key symmetry,
High Contrast system resources, no literal template hex colors, gallery
automation names, runtime evidence fields, and CI wiring. PublicAPI already
listed the constructor, range APIs, `PreviewStatus`, and
`SteeringBarPreviewStatus`; this slice added no new public members.

WinUI convention AUDIT keeps these Steering Bar exceptions on purpose (no pixel
change): it is a custom `Control`, not a restyle of stock `Slider` /
`RangeBase`, so Minimum / Maximum / Value are owned DPs. Hover, press,
disabled chrome, fill width, thumb position, and stop markers stay
code-driven (no CommonStates). Track and thumb `CornerRadius` stay literal 3
because there is no radius token between `RadiusXs` (2) and `RadiusSm` (4).
The value label still formats a percent in code when `ValueContent` is unset.
`ShadowFar` / `ShadowNear` remain 0-size brush wells for `AttachedCardShadow`.
Highlight / sheen / top-highlight keys stay published even though overlay
layers were removed from the live thumb. `PreviewStatus` remains
Gallery-oriented.

This is structured runtime evidence, not a pixel screenshot baseline. The
High Contrast check is a static resource contract, not an on-device
verification across all Windows contrast themes. Formal Appium and
Accessibility Insights coverage remain L4 work. The repository's known
frozen global-token High Contrast deficit remains a release blocker; this
component does not alter those tokens or claim stable readiness.
