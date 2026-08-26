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
starting at 0, title and value labels shown, existing rounded track / gradient
fill / glass thumb template. Call sites that already used the implicit style
are unchanged.

## Implemented contract

- The constructor sets `DefaultStyleKey`; `DefaultEtherSteeringBarStyle` owns
  the default setters and template, and the implicit style is `BasedOn` that
  keyed style. The hand cursor stays in the constructor.
- The component declares its thirteen existing template parts plus
  `LabelStates` (BothLabelsVisible / TitleOnly / ValueOnly / LabelsHidden).
  Label visibility is driven with `VisualStateManager`. Title content uses
  `TemplateBinding`; the value label still formats `Value` as a percent when
  `ValueContent` is unset.
- Light, Dark, and HighContrast expose the same sixteen `EtherSteeringBar*`
  component resources. The template consumes those component keys only for
  color-bearing ThemeResources. Light and Dark retain the previous fill
  gradient, glass-thumb treatment, and token aliases (`background/track`,
  `text/primary`, `TextSecondary`). High Contrast uses Windows `SystemColor*`
  dynamic resources rather than changing frozen Foundation tokens. Decorative
  thumb shadows map to `SystemColorWindowColor`.
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
- Hover enlarges the thumb (21×13 → 23×15) by mutating `ThumbHost` size/margin.
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
`UseSystemFocusVisuals=False`, `IsTabStop`, and template), InteractionSurface /
FillBorder / ThumbHost / LabelRow / TitleText / ValueLabel, four LabelStates
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

This is structured runtime evidence, not a pixel screenshot baseline. The
High Contrast check is a static resource contract, not an on-device
verification across all Windows contrast themes. Formal Appium and
Accessibility Insights coverage remain L4 work. The repository's known
frozen global-token High Contrast deficit remains a release blocker; this
component does not alter those tokens or claim stable readiness.
