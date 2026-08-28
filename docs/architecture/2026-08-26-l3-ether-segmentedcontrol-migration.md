# L3 EtherSegmentedControl migration record

Date: 2026-08-26  
Status: preview implementation evidence, not stable-release readiness

`EtherSegmentedControl` is an L3 slice migrated onto the L2 `EtherProgressBar` /
L3 Button / Checkbox / Input / Dropdown exemplar contract. It remains a
`ContentControl` host for `RadioButton` / `HandRadioButton` segments. Composition
drop-shadow layers in `EtherSegmentedControl.cs` stay on `ShadowHost`. Wave 2 adds the
consumer-fixture runtime marker, CI wiring, and PublicAPI confirmation.

**Default visual:** padding `4`, Light `AlphaWhite40` / Dark `AlphaBlack70` track,
stretch host and content alignment, `IsTabStop=False`, `UseSystemFocusVisuals=False`.
`ShadowHost` is a `Border` with `-24,0` horizontal bleed; `TrackSurface` keeps the
8/30 vertical inset. Segments are Instrument Sans SemiBold 12, padding `16,12`,
min-height 33. Call sites that set `Style="{StaticResource EtherSegmentedTrack}"`
are unchanged. `EtherSegment` remains the keyed `RadioButton` item style.
`EtherSegmentPanel` equal-width row is the supported items host.
`EtherSegmentedTrack` is a compatibility subclass; `DefaultStyleKey` stays
`typeof(EtherSegmentedControl)`.

## Implemented contract

- The constructor sets `DefaultStyleKey`; `DefaultEtherSegmentedControlStyle`
  owns the default setters and track template, and the implicit style is
  `BasedOn` that keyed style. `EtherSegmentedTrack` remains a keyed alias of
  the default style. `EtherSegment` stays the public segment style key.
- The host declares the template parts the track template actually uses
  (`ShadowHost`, `TrackSurface`, `CasterBrushSource`, `ShadowBrushSource`).
  The host has no VisualState groups; CommonStates / FocusStates live on the
  `EtherSegment` `RadioButton` template. `HandRadioButton` still drives
  hover/pressed/checked layer opacity in code (Gallery-only type).
- Track and segment `ContentPresenter`s bind Content / ContentTemplate /
  ContentTemplateSelector / ContentTransitions, TemplateBind content
  alignment, and set `AutomationProperties.AccessibilityView=Raw` so the
  native radio peer does not announce the presenter twice.
- Light, Dark, and HighContrast expose the same nine `EtherSegmentedControl*`
  component resources. Track and segment templates consume those component keys
  only for color-bearing ThemeResources. Light and Dark bind `Color` to
  primitive tokens (Figma selected `62110:23700`, Light track `62110:24735`,
  Dark `62110:24850`). High Contrast uses Windows `SystemColor*` dynamic
  resources rather than changing frozen Foundation tokens. The empty
  segment overlay uses unthemed `EtherSegmentedControlTransparentBrush`.
  Track radius is `RadiusMd`; segment pills use `RadiusSm`. Caster and shadow
  RGB come from collapsed template `Border` parts (`EtherSegmentedControlCasterBrush`
  / `EtherSegmentedControlShadowBrush`) so High Contrast rebind follows
  `ActualTheme`.
- Gallery specimens keep the 2/3/4-segment interactive stories and the
  default/selected/hover/pressed state matrix. The two-segment host uses the
  implicit default style; the others keep the `EtherSegmentedTrack` alias.
  Key hosts and segments have identifiable automation names.

## Pinned-source parity

| Topic | WinUI / Windows App SDK | EtherSegmentedControl | Notes |
| --- | --- | --- | --- |
| Base type | No first-party `Segmented` in WASDK; Community Toolkit `Segmented` exists | `ContentControl` + `RadioButton` items | Preserves today's composition host; does not invent a Segmented subclass. |
| Default style | `DefaultStyleKey` + generic implicit style | Same pattern via `DefaultEtherSegmentedControlStyle` | Matches the L2 exemplar and WinUI templated-control guidance. |
| Selection | Toolkit `Segmented` owns selected index | Native `RadioButton` `GroupName` | Not invented. |
| Shadows | Typically XAML `ThemeShadow` / `DropShadowPanel` | Composition `DropShadow` on `ShadowHost` | Existing Figma-calibrated layers are preserved in code. |
| Host tab stop | Chrome hosts are not extra tab stops | `IsTabStop=False` | Tab order goes to the segment `RadioButton`s. |

A line-by-line clone of Community Toolkit `Segmented` is deferred: Ether's
composition shadows, opacity-layer hover, and `HandRadioButton` workaround are
product design, not missing Fluent chrome.

## Evidence and remaining exceptions

The unpackaged NuGet consumer fixture mounts default and interactive
`EtherSegmentedControl` instances on a UI thread. Its `segmentedControl` marker
records keyed default-style resolution (`Padding` 4, stretch alignment,
`IsTabStop=False`, `UseSystemFocusVisuals=False`, and template), `ShadowHost` /
`TrackSurface` in the visual tree, at least two `EtherSegment` radio buttons,
Unchecked/Checked segment CommonStates via `IsChecked` plus `GoToState`
(current-state name; Dark unselected text is `AlphaWhite70` and checked text is
`Gray0`), automation name, and Light/Dark track-background rebind
(`AlphaWhite40` vs `AlphaBlack70`). Host VisualState groups remain absent on
purpose; `HandRadioButton` is a Gallery-only type and is excluded from the
Controls package, so the consumer fixture uses documented `RadioButton` +
`EtherSegment` call sites. Named VSM parts on `EtherSegment` cannot take
`[TemplatePart]` because the item is a keyed `RadioButton` style, not a custom
class. `Verify-EtherSegmentedControlContract.ps1` additionally verifies
metadata, keyed/implicit/alias styles, `EtherSegment`, theme-key symmetry,
High Contrast system resources, no literal template hex colors, composition
shadow lookups, runtime evidence fields, and CI wiring.

This is structured runtime evidence, not a pixel screenshot baseline. The High
Contrast check is a static resource contract, not an on-device verification
across all Windows contrast themes. Formal Appium and Accessibility Insights
coverage remain L4 work. The repository's known frozen global-token High
Contrast deficit remains a release blocker; this component does not alter those
tokens or claim stable readiness.

Composition drop-shadow clipping on multi-segment tracks (far blur cut at a
segment boundary) remains an open visual defect. AUDIT does not change shadow
metrics or clip geometry.
