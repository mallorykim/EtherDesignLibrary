# L3 EtherIntelligenceButton migration record

Date: 2026-08-26  
Status: preview implementation evidence, not stable-release readiness

`EtherIntelligenceButton` is an L3 control migrated onto the L2 `EtherProgressBar`
/ L3 `EtherButton` exemplar contract. It remains a native `Button` subclass with
a hand cursor and the intelligence gradient border / glow visual. It does not
add variants, trailing-icon APIs, an indeterminate mode, or animation APIs.

**Default visual:** Padding `8,8,12,8`, font size 14, centered content,
`UseSystemFocusVisuals=False`, existing dual-stroke glow template. Call sites
that already used the implicit style are unchanged (this control has no extra
named styles).

## Implemented contract

- The constructor sets `DefaultStyleKey`; `DefaultEtherIntelligenceButtonStyle`
  owns the default setters and template, and the implicit style is `BasedOn`
  that keyed style. The hand cursor stays in the constructor.
- The component declares the named template parts the template actually uses
  (`Bg`, dual intelligence strokes, glow layers, `Cp`, `FocusRing`) plus
  CommonStates and FocusStates. Native `Button` still drives those states.
- Light, Dark, and HighContrast expose the same 13 `EtherIntelligenceButton*`
  component resources. The template consumes those component keys only for
  color-bearing values. Light and Dark retain the previous token aliases
  (`BackgroundCanvas`, `TextSecondary`, `BorderIntelligenceBlue`,
  `BorderIntelligence`, `BorderIntelligenceHover`, `ai/accent-purple`) and the
  previous glow hex literals (now in the component theme dictionaries). High
  Contrast uses Windows `SystemColor*` dynamic resources rather than changing
  frozen Foundation tokens. Decorative glows map to `SystemColorWindowColor`
  so they do not compete with the High Contrast face/text pair; pressed core
  glow uses `SystemColorHighlightColor`.
- Gallery specimens keep the functional and state matrix and add identifiable
  automation names on the interactive, long-label, default, hover, pressed,
  and disabled specimens.

## Pinned-source parity

| Topic | WinUI `Button` (Windows App SDK) | EtherIntelligenceButton | Notes |
| --- | --- | --- | --- |
| Base type | `Button` | `Button` | Native interaction, click, command, and CommonStates/FocusStates are reused. |
| Default style | `DefaultStyleKey` + generic implicit style | Same pattern via `DefaultEtherIntelligenceButtonStyle` | Matches the L2 exemplar and WinUI templated-control guidance. |
| Content | `ContentPresenter` | Sparkles icon + `ContentPresenter` | Custom layout (built-in icon, dual gradient stroke, glow halo) is an Ether visual, not a WinUI template clone. |
| Variants | N/A for stock Button | None | Not invented; this control is a single intelligence treatment. |
| Indeterminate | N/A | N/A | Not invented. |

A line-by-line template clone of WinUI `Button` is deferred: Ether's dual
inside gradient stroke and layered glow are product design, not missing
Fluent chrome.

## Evidence and remaining exceptions

The unpackaged NuGet consumer fixture mounts default and interactive
`EtherIntelligenceButton` instances on a UI thread. Its `intelligenceButton`
marker records keyed default-style resolution (font size 14, padding `8,8,12,8`,
`UseSystemFocusVisuals=False`, and template), the nine named template parts,
Normal / PointerOver / Pressed / Disabled CommonStates via `GoToState`
(radial vs linear hover stroke, pressed core-glow color, 40% disabled opacity
with collapsed glow layers), automation name, and Light/Dark label foreground
rebind (`TextSecondary` / `Gray1000` vs `Gray0`). `Verify-EtherIntelligenceButtonContract.ps1`
additionally verifies metadata, keyed/implicit styles, theme-key symmetry,
High Contrast system resources, no literal template hex colors, gallery
automation names, runtime evidence fields, and CI wiring. PublicAPI already
listed the constructor; Wave 1 added no new public members.

This is structured runtime evidence, not a pixel screenshot baseline. The
High Contrast check is a static resource contract, not an on-device
verification across all Windows contrast themes. Formal Appium and
Accessibility Insights coverage remain L4 work. The repository's known
frozen global-token High Contrast deficit remains a release blocker; this
component does not alter those tokens or claim stable readiness.
