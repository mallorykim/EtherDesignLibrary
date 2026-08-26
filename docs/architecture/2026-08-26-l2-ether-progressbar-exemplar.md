# L2 EtherProgressBar exemplar record

Date: 2026-08-26  
Status: preview implementation evidence, not stable-release readiness

`EtherProgressBar` is the L2 bounded exemplar. It remains a determinate, non-interactive
`RangeBase` control: it does not add an indeterminate mode or animation. Value changes split
the existing layout columns directly, so reduced-motion support is N/A for this control.

## Implemented contract

- The constructor sets `DefaultStyleKey`; `DefaultEtherProgressBarStyle` owns the default
  range and label setters, and the implicit style is `BasedOn` that keyed style.
- The component declares its five template parts and uses `LabelStates` for all four supported
  title/value combinations. It no longer sets template label visibility directly.
- Light, Dark, and HighContrast expose the same four `EtherProgressBar*` component resources.
  The template consumes those component keys only for its color-bearing values. Light and Dark
  retain the original four-stop gradient; High Contrast uses Windows `SystemColor*` dynamic
  resources rather than changing frozen Foundation tokens.
- The private automation peer reports `ProgressBar`, exposes a read-only `IRangeValueProvider`,
  returns `double.NaN` for small/large change, rejects `SetValue`, falls back to a string title
  only when the normal automation name is empty, and raises the RangeValue Value property event
  when the owner value changes.
- Gallery specimens retain the simulator and all four label combinations, each with an
  identifiable automation name.

## Evidence and remaining exceptions

The unpackaged NuGet consumer fixture mounts the real control on a UI thread. Its marker records
keyed style lookup/default setters, template parts, four observable label transitions, a 65%
fill ratio, read-only automation values, rejected `SetValue`, Light/Dark gradient stops, and
Light/Dark template brush values. `Verify-EtherProgressBarContract.ps1` additionally verifies
metadata, keyed/implicit styles, theme-key symmetry, High Contrast system resources, no literal
template hex colors, and the required runtime evidence fields.

This is structured runtime evidence, not a pixel screenshot baseline. The High Contrast check is
a static resource contract, not an on-device verification across all Windows contrast themes.
Formal Appium and Accessibility Insights coverage remain L4 work. The repository's known frozen
global-token High Contrast deficit remains a release blocker; this component does not alter those
tokens or claim stable readiness.
