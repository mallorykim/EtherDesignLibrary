# L2 EtherProgressBar exemplar record

Date: 2026-08-26  
Status: preview implementation evidence, not stable-release readiness

`EtherProgressBar` is the L2 bounded exemplar. It remains a determinate, non-interactive
`RangeBase` control: it does not add an indeterminate mode or animation. Value changes split
the existing layout columns directly, so reduced-motion support is N/A for this control.

## Implemented contract

- The constructor sets `DefaultStyleKey`; `DefaultEtherProgressBarStyle` owns the default
  range and label setters (`IsTabStop=False`, `UseSystemFocusVisuals=False`, `Padding`
  0,4,0,12 template-bound onto `LayoutRoot`), and the implicit style is `BasedOn` that
  keyed style. Track/fill chrome is `AccessibilityView=Raw`; RangeValue stays on the
  control. Corner radius uses primitive `RadiusXs`.
- The component declares its five template parts and uses `LabelStates` for all four supported
  title/value combinations. It no longer sets template label visibility directly.
- Light, Dark, and HighContrast expose the same four `EtherProgressBar*` component resources.
  The template consumes those component keys only for its color-bearing values. Light and Dark
  bind fill, track, and label `Color` to primitives (`Blue700` / `Blue400`,
  `Gray75` / `Gray700`, `Gray1000` / `Gray0`, `AlphaBlack70`). Figma Light
  `62110:22813` and Dark `62120:1374` use a solid `background/Progress-fill`,
  not the Steering Bar navy-to-cyan gradient. High Contrast uses Windows
  `SystemColor*` dynamic resources rather than changing frozen Foundation tokens.
- The private automation peer reports `ProgressBar`, exposes a read-only `IRangeValueProvider`,
  returns `double.NaN` for small/large change, rejects `SetValue`, falls back to a string title
  only when the normal automation name is empty, and raises the RangeValue Value property event
  when the owner value changes.
- Gallery specimens retain the simulator and all four label combinations, each with an
  identifiable automation name.

## Evidence and remaining exceptions

The unpackaged NuGet consumer fixture mounts the real control on a UI thread. Its marker records
keyed style lookup/default setters, template parts, four observable label transitions, a 65%
fill ratio, read-only automation values, rejected `SetValue`, in-process
`AutomationRangeValueChanged` subscription (`valuePropertyChangedSubscribed`),
Light/Dark solid fills (`Blue700` / `Blue400`), and
Light/Dark template brush values. `Verify-EtherProgressBarContract.ps1` additionally verifies
metadata, keyed/implicit styles, theme-key symmetry, primitive fill/track/label bindings, High
Contrast system resources, no literal template hex colors, and the required runtime evidence fields.

This is structured runtime evidence, not a pixel screenshot baseline. The High Contrast check is
a static resource contract, not an on-device verification across all Windows contrast themes.
Formal Appium and Accessibility Insights coverage remain L4 work. The repository's known frozen
global-token High Contrast deficit remains a release blocker; this component does not alter those
tokens or claim stable readiness.

WinUI convention AUDIT keeps these Progress Bar exceptions on purpose (no pixel change): it is a
custom `RangeBase`, not a restyle of stock `ProgressBar`, so there is no indeterminate template
or DeterminateStates group. Figma has no hover/pressed/disabled chrome, so CommonStates are
absent. Label type is product Instrument Sans SemiBold 14 rather than `TemplateBinding`
FontFamily. Dark value stays `Gray0` with the title (Figma paints both the same); Light value
stays `AlphaBlack70`. Steering Bar keeps the navy-to-cyan gradient.
