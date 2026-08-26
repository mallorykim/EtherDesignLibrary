# L3 EtherButton migration record

Date: 2026-08-26  
Status: preview implementation evidence, not stable-release readiness

`EtherButton` is the first L3 control migrated onto the L2 `EtherProgressBar` exemplar
contract. It remains a native `Button` subclass with a hand cursor and an optional trailing
`RightIcon`. It does not add an indeterminate mode or animation APIs.

**Default visual:** Primary medium (`EtherButtonVariant.Primary` + `EtherButtonSize.Large`),
matching `EtherButtonPrimary` (MinHeight 40, padding `16,10,16,10`, font size 14). Call sites
that set an explicit named `Style` are unchanged.

## Implemented contract

- The constructor sets `DefaultStyleKey`; `DefaultEtherButtonStyle` owns the default Primary
  medium setters and template, and the implicit style is `BasedOn` that keyed style.
- The six named styles (`EtherButtonPrimary` / `PrimarySmall` / `Secondary` /
  `SecondarySmall` / `Tertiary` / `TertiarySmall`) remain public keys. `Variant`/`Size`
  selection still resolves those same keys.
- The component declares the `RightIcon` template part plus CommonStates, FocusStates, and
  RightIconStates. Trailing-icon visibility is driven with `VisualStateManager`; the previous
  `RightIconVisibility` dependency property is removed (preview/unshipped API).
- Light, Dark, and HighContrast expose the same 18 `EtherButton*` component resources. The
  three control templates consume those component keys only for color-bearing values. Light and
  Dark retain the previous token mappings (including the Primary `#40FFFFFF` hairline, now in
  the component theme dictionary). High Contrast uses Windows `SystemColor*` dynamic resources
  rather than changing frozen Foundation tokens.
- Gallery specimens keep the six named-style stories and add a default-style specimen. Key
  interactive specimens have identifiable automation names.

## Pinned-source parity

| Topic | WinUI `Button` (Windows App SDK) | EtherButton | Notes |
| --- | --- | --- | --- |
| Base type | `Button` | `Button` | Native interaction, click, command, and CommonStates/FocusStates are reused. |
| Default style | `DefaultStyleKey` + generic implicit style | Same pattern via `DefaultEtherButtonStyle` | Matches the L2 exemplar and WinUI templated-control guidance. |
| Content | `ContentPresenter` | Label `TextBlock` + optional `RightIcon` slot | Custom layout (optical nudge, trailing icon) is an Ether visual, not a WinUI template clone. |
| Right icon | Not a first-class `Button` API (`AppBarButton.Icon` is the closest relative) | `RightIcon` + RightIconStates | Kept; collapsing the empty slot is required to preserve current visuals. |
| Indeterminate | N/A | N/A | Not invented. |

A line-by-line template clone of WinUI `Button` is deferred: Ether's three visual variants and
optical label nudge are product design, not missing Fluent chrome.

## Evidence and remaining exceptions

The unpackaged NuGet consumer fixture mounts default, named-style, and Secondary `EtherButton`
instances on a UI thread. Its `button` marker records keyed default-style resolution (Primary
medium MinHeight/FontSize/template), the `RightIcon` part, both RightIconStates, collapse
proof, automation name, and Light/Dark Secondary background/border rebind (Primary fills are
the same Blue600/Gray0 pair in both themes, so Secondary border is the differing pair).
`Verify-EtherButtonContract.ps1` additionally verifies metadata, keyed/implicit/named styles,
theme-key symmetry, High Contrast system resources, no literal template hex colors, and the
required runtime evidence fields.

This is structured runtime evidence, not a pixel screenshot baseline. The High Contrast check
is a static resource contract, not an on-device verification across all Windows contrast
themes. Formal Appium and Accessibility Insights coverage remain L4 work. The repository's
known frozen global-token High Contrast deficit remains a release blocker; this component does
not alter those tokens or claim stable readiness.

Tertiary styles override `MinHeight` to `0` so BasedOn `DefaultEtherButtonStyle` does not
inherit the Primary 40px minimum; that preserves the previous auto-height tertiary layout.
