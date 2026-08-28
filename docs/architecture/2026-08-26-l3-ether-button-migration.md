# L3 EtherButton migration record

Date: 2026-08-26  
Status: preview implementation evidence, not stable-release readiness

`EtherButton` is the first L3 control migrated onto the L2 `EtherProgressBar` exemplar
contract. It remains a native `Button` subclass with a hand cursor and independently optional
leading (`LeftIcon`) and trailing (`RightIcon`) `IconElement` slots. It does not add an
indeterminate mode or animation APIs.

**Default visual:** Figma "Default" mapped to the existing public Primary large API
(`EtherButtonVariant.Primary` + `EtherButtonSize.Large`), matching `EtherButtonPrimary`
(minimum width `108EPX`, height `46EPX`, padding `20EPX` horizontal / `12EPX` vertical,
font size `14EPX`, 4EPX radius; Small keeps minimum width `94EPX` at `32EPX` height with
`16EPX`/`8EPX` padding). Existing call sites that set an
explicit named `Style` are unchanged. Horizontal padding (`20`/`16EPX`) is a deliberate
deviation from Figma's uniform `12`/`8EPX` (2026-08-27): Figma's Default button is fixed at
108EPX wide so short labels get extra visual inset there, while content-sized WinUI buttons
were limited to exactly 12EPX and read too tight.

## Implemented contract

- The constructor sets `DefaultStyleKey`; `DefaultEtherButtonStyle` owns the default Primary
  medium setters and template, and the implicit style is `BasedOn` that keyed style.
- The six named styles (`EtherButtonPrimary` / `PrimarySmall` / `Secondary` /
  `SecondarySmall` / `Tertiary` / `TertiarySmall`) remain public keys. `Variant`/`Size`
  selection still resolves those same keys.
- The component declares `Bg`, `Stroke` (Primary/Secondary overlay), `Cp`, `FocusRing`,
  `LeftIcon`, and `RightIcon` template parts plus CommonStates, FocusStates, and independent
  icon-state groups. Each icon slot is driven with `VisualStateManager`; the previous
  `RightIconVisibility` dependency property is removed (preview/unshipped API).
  `Cp` template-binds `Content` and `ContentTemplate`. `Variant`/`Size`
  resolve named styles from the element's resource scope (local → parents → application).
  `PublicAPI.Unshipped.txt` is the current surface, not a removal ledger; the contract script
  rejects the identifier if it returns.
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
| Content | `ContentPresenter` | Label + optional `LeftIcon` / `RightIcon` slots | Custom layout (optical nudge and independent icon slots) is an Ether visual, not a WinUI template clone. |
| Icons | Not a first-class `Button` API (`AppBarButton.Icon` is the closest relative) | `LeftIcon` + `RightIcon` | Both slots collapse independently, preserving pure-text buttons and allowing any `IconElement`. |
| Indeterminate | N/A | N/A | Not invented. |

A line-by-line template clone of WinUI `Button` is deferred: Ether's three visual variants and
optical label nudge are product design, not missing Fluent chrome.

## Evidence and remaining exceptions

The unpackaged NuGet consumer fixture mounts default, named-style, and Secondary `EtherButton`
instances on a UI thread. Its `button` marker records keyed default-style resolution (Primary
Default dimensions/typography/template), both optional icon parts and their independent state
transitions/collapse proof, automation name, and Light/Dark Secondary background/border rebind
(Primary fills are the same Blue600/Gray0 pair in both themes, so Secondary border is the
differing pair).
`Verify-EtherButtonContract.ps1` additionally verifies metadata, keyed/implicit/named styles,
theme-key symmetry, High Contrast system resources, no literal template hex colors, and the
required runtime evidence fields.

This is structured runtime evidence, not a pixel screenshot baseline. The High Contrast check
is a static resource contract, not an on-device verification across all Windows contrast
themes. Formal Appium and Accessibility Insights coverage remain L4 work. The repository's
known frozen global-token High Contrast deficit remains a release blocker; this component does
not alter those tokens or claim stable readiness.

All three variants retain the Figma 46EPX Default / 32EPX Small hit targets. The existing public
`Large` enum name is therefore a compatibility name for Figma's "Default" size; no API rename
is needed. Figma documents 20EPX Default and 16EPX Small icon instances, while the optional slots
intentionally preserve each caller-supplied `IconElement` size and glyph.

## Figma variable mapping (source: file ursRC201v8IiVeafliI45F, node 62075:5624)

Variable names as published in Figma, captured 2026-08-27 for future token linking.

| Figma variable | Figma value | WinUI component resource | Token chain | Status |
| --- | --- | --- | --- | --- |
| `Buttons/Primary/Background/Default` | `#0D62FF` | `EtherButtonPrimaryBackgroundBrush` | `action/primary/bg` → `Blue600` | match |
| `Buttons/Primary/Background/Hover` | `#0055FF` | `EtherButtonPrimaryBackgroundHoverBrush` | `Blue700` | match |
| `Buttons/Primary/Background/Pressed` | `#0054E5` | `EtherButtonPrimaryBackgroundPressedBrush` | `Blue800` | match |
| `Buttons/Primary/Background/Disabled` | `#0D62FF` | `EtherButtonPrimaryBackgroundDisabledBrush` | `Blue600`, rendered at 40% opacity | match |
| `Buttons/Primary/border/Default` | `#FFFFFF` @ 25% | `EtherButtonPrimaryBorderBrush` | `#40FFFFFF` | match |
| `text/on-brand` | `#FFFFFF` | `EtherButtonPrimaryForegroundBrush` | `action/primary/fg` → `Gray0` | match |
| `Buttons/Secondary/Background/Default` | transparent | `EtherButtonSecondaryBackgroundBrush` | `Transparent` | match |
| `Buttons/Secondary/Background/Hover` | `#F5F7F8` | `EtherButtonSecondaryBackgroundHoverBrush` | `ActionSecondaryBgHover` → `Gray25` | match |
| `Buttons/Secondary/Background/Pressed` | `#EFF3F6` | `EtherButtonSecondaryBackgroundPressedBrush` | `ActionSecondaryBgPressed` → `Gray50` | match |
| `Buttons/Secondary/Background/Disabled` | transparent | — | 40% opacity fade | match |
| `Borders/border-light` | `#011C4B` @ 20% | `EtherButtonSecondaryBorderBrush` | `#33011C4B` (= `AlphaNavy20`) | match |
| `text/primary` | `#000000` | `EtherButtonSecondaryForegroundBrush` | `action/secondary/fg` → `Gray1000` | match |
| `Buttons/Tertiary/Text/Default` | `#0D62FF` | `EtherButtonTertiaryForegroundBrush` | `Blue600` | match (fixed 2026-08-27; was `Blue800`) |
| `Buttons/Tertiary/Text/Hover` | `#0055FF` | `EtherButtonTertiaryForegroundHoverBrush` | `Blue700` | match |
| `Buttons/Tertiary/Text/Pressed` | `#0054E5` | `EtherButtonTertiaryForegroundPressedBrush` | `Blue800` | match |
| `Buttons/Tertiary/Text/Disabled` | `#0D62FF` | — | 40% opacity fade | match |

Rendering note: Figma strokes composite over the component's own fill, so the Primary 25%-white
border is visible on the blue background. WinUI `Border.BorderBrush` composites against the page
behind the control instead, which made the ring invisible on light pages. The Primary and
Secondary templates therefore draw the stroke as a separate `Stroke` overlay `Border` above `Bg`.
