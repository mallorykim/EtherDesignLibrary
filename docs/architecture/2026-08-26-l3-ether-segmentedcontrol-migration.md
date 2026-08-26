# L3 EtherSegmentedControl migration record

Date: 2026-08-26  
Status: preview implementation evidence, not stable-release readiness

`EtherSegmentedControl` is an L3 slice migrated onto the L2 `EtherProgressBar` /
L3 Button / Checkbox / Input / Dropdown exemplar contract. It remains a
`ContentControl` host for `RadioButton` / `HandRadioButton` segments. Composition
drop-shadow layers in `EtherSegmentedControl.cs` are unchanged. Wave 2 adds the
consumer-fixture runtime marker, CI wiring, and PublicAPI confirmation.

**Default visual:** `Padding4`, `BackgroundSegmentTrack`, left/center content
alignment, existing track template (`ShadowHost` + `TrackSurface` with 8/30
shadow inset). Call sites that set `Style="{StaticResource EtherSegmentedTrack}"`
are unchanged. `EtherSegment` remains the keyed `RadioButton` item style.

## Implemented contract

- The constructor sets `DefaultStyleKey`; `DefaultEtherSegmentedControlStyle`
  owns the default setters and track template, and the implicit style is
  `BasedOn` that keyed style. `EtherSegmentedTrack` remains a keyed alias of
  the default style. `EtherSegment` stays the public segment style key.
- The host declares the template parts the track template actually uses
  (`ShadowHost`, `TrackSurface`). The host has no VisualState groups;
  CommonStates / FocusStates live on the `EtherSegment` `RadioButton` template.
  `HandRadioButton` still drives hover/pressed/checked layer opacity in code.
- Light, Dark, and HighContrast expose the same seven `EtherSegmentedControl*`
  component resources. Track and segment templates consume those component keys
  only for color-bearing ThemeResources. Light and Dark retain the previous
  token aliases (`BackgroundSegmentTrack`, `BackgroundSegmentHover`,
  `BackgroundSegmentPressed`, `background/brand`, `TextSecondary`,
  `text/on-brand`, `border/focus`). High Contrast uses Windows `SystemColor*`
  dynamic resources rather than changing frozen Foundation tokens. The empty
  segment overlay uses unthemed `EtherSegmentedControlTransparentBrush`.
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

A line-by-line clone of Community Toolkit `Segmented` is deferred: Ether's
composition shadows, opacity-layer hover, and `HandRadioButton` workaround are
product design, not missing Fluent chrome.

## Evidence and remaining exceptions

The unpackaged NuGet consumer fixture mounts default and interactive
`EtherSegmentedControl` instances on a UI thread. Its `segmentedControl` marker
records keyed default-style resolution (`Padding4`, left/center content
alignment, and template), `ShadowHost` / `TrackSurface` in the visual tree,
at least two `EtherSegment` radio buttons, Unchecked/Checked segment
CommonStates via `IsChecked` plus `GoToState` (current-state name; Dark
`TextSecondary` and `text/on-brand` both alias `Gray0`, so foreground color
is not a Dark-safe proof), automation
name, and Light/Dark track-background rebind (`BackgroundSegmentTrack`
`Gray25` vs `Gray0`). Host VisualState groups remain absent on purpose;
`HandRadioButton` is a Gallery-only type and is excluded from the Controls
package, so the consumer fixture uses documented `RadioButton` + `EtherSegment`
call sites. `Verify-EtherSegmentedControlContract.ps1` additionally verifies
metadata, keyed/implicit/alias styles, `EtherSegment`, theme-key symmetry,
High Contrast system resources, no literal template hex colors, composition
shadow lookups, runtime evidence fields, and CI wiring.

This is structured runtime evidence, not a pixel screenshot baseline. The High
Contrast check is a static resource contract, not an on-device verification
across all Windows contrast themes. Formal Appium and Accessibility Insights
coverage remain L4 work. The repository's known frozen global-token High
Contrast deficit remains a release blocker; this component does not alter those
tokens or claim stable readiness.

Composition caster fills in `ApplyCasterColor` remain hardcoded
`BackgroundSurface` ARGB values (`#FF121215` dark / `#FFFFFFFF` light). That is
existing composition behavior, not a template color, and is preserved so the
translucent dark track does not show a mismatched caster patch.
