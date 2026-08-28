# L3 EtherCheckbox and EtherRadioButton migration record

Date: 2026-08-26  
Status: preview implementation evidence, not stable-release readiness

`EtherCheckbox` and `EtherRadioButton` are the second L3 slice migrated onto the L2
`EtherProgressBar` / L3 `EtherButton` exemplar contract. They remain native `CheckBox`
and `RadioButton` subclasses. Neither control adds an indeterminate visual, animation
APIs, or a FocusStates group (the current spec has no focus ring).

**Default visual:** two-state (`IsThreeState=False`), `UseSystemFocusVisuals=False`,
zero min size/padding, left/center alignment, existing templates. Call sites that set
an explicit named `Style` are unchanged (these controls have no extra named styles).

## Implemented contract

- Constructors set `DefaultStyleKey`; `DefaultEtherCheckboxStyle` /
  `DefaultEtherRadioButtonStyle` own the default setters and templates, and each
  implicit style is `BasedOn` that keyed style.
- Each component declares the template parts the templates actually use
  (`LayoutRoot`, fill/stroke faces, `Label`, plus `Glyph` or `Dot`)
  and CommonStates / CheckStates. Native toggle visuals still drive those states.
  Both name `UncheckedStroke`, `CheckedFill`, and `CheckedStroke` (VSM targets)
  and bind `ContentTemplate` / `ContentTemplateSelector` / `ContentTransitions`
  on `Label`, matching WinUI CheckBox / RadioButton ContentPresenter.
  RadioButton also sets `AutomationProperties.AccessibilityView=Raw` on `Label`
  so the native peer does not announce the content twice.
- Light, Dark, and HighContrast expose the same `EtherCheckbox*` /
  `EtherRadioButton*` component resources. Templates consume those component keys
  only for color-bearing ThemeResources. Light/Dark bind `Color` to primitive
  tokens (Checkbox Figma Light set `62085:7907`, Dark spec tables `62093:60` /
  `62093:163`; RadioButton Light set `62102:8755`, Dark specimens
  `62102:8800`–`8805`). High Contrast uses Windows `SystemColor*` dynamic
  resources rather than changing frozen Foundation tokens. Disabled is Default
  colours at 40% opacity. RadioButton checked-disabled paints the inner disc
  into the fill as one gradient so 40% group opacity cannot turn the centre
  blue. Unused remaining `*DisabledBrush` keys stay for High Contrast symmetry.
- Structural overlay fills (`LayoutRoot` and stroke-ring backgrounds) use unthemed
  `EtherCheckboxTransparentBrush` / `EtherRadioButtonTransparentBrush` so the ring
  still reveals the face beneath it without putting a non-SystemColor brush in High
  Contrast theme dictionaries.
- Gallery specimens keep the existing state matrices and add identifiable
  automation names on the interactive and default checked/unchecked specimens.

## Pinned-source parity

| Topic | WinUI `CheckBox` / `RadioButton` | Ether | Notes |
| --- | --- | --- | --- |
| Base type | `CheckBox` / `RadioButton` | Same | Native toggle, GroupName exclusion, CommonStates/CheckStates are reused. |
| Default style | `DefaultStyleKey` + generic implicit style | Same pattern via keyed `DefaultEther*` styles | Matches the L2 exemplar and WinUI templated-control guidance. |
| Indeterminate | CheckBox can be three-state | `IsThreeState=False`; Indeterminate unstyled | Preserves today's Ether spec. |
| Focus ring | System focus visuals | `UseSystemFocusVisuals=False`; no FocusStates | Spec has no focus treatment; a gap-ring previously read as a halo. |

A line-by-line template clone of WinUI CheckBox/RadioButton is deferred: Ether's
separate fill/stroke faces, 18px box, and Inter SemiBold 12 label are product
design, not missing Fluent chrome.

## Evidence and remaining exceptions

The unpackaged NuGet consumer fixture mounts default and interactive
`EtherCheckbox` / `EtherRadioButton` instances on a UI thread. Its `checkbox` and
`radioButton` markers record keyed default-style resolution (`IsThreeState=False`
plus template), template parts, both CheckStates, automation names, and Light/Dark
label-foreground rebind (`AlphaBlack70` vs `AlphaWhite70`). Unchecked-fill brushes also
differ by theme in the dictionaries, but CommonStates VisualState setters apply a
local `Background` that does not re-evaluate `ThemeResource` on theme change; the
fixture therefore samples the live label pair, the same way Button samples Secondary
instead of Primary. `Verify-EtherCheckboxContract.ps1` and
`Verify-EtherRadioButtonContract.ps1` additionally verify metadata, keyed/implicit
styles, theme-key symmetry, High Contrast system resources, no literal template hex
colors, and the required runtime evidence fields.

This is structured runtime evidence, not a pixel screenshot baseline. The High
Contrast check is a static resource contract, not an on-device verification across
all Windows contrast themes. Formal Appium and Accessibility Insights coverage
remain L4 work. The repository's known frozen global-token High Contrast deficit
remains a release blocker; these components do not alter those tokens or claim
stable readiness.
