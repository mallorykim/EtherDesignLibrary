# Consumability review — Tooltip

> Reviewed against
> [the review spec](../2026-09-01-component-consumability-review-spec.md). Findings are grounded in
> source (file:line); the official WinUI baseline is Windows App SDK 2.3.1
> (`Directory.Packages.props:7`).
>
> **Reviewed:** the keyed `EtherTooltip` `Style` for stock `Border` + its three themed component
> brushes —
> [EtherTooltip.xaml](../../../src/Ether.DesignSystem.Controls/Resources/Foundations/EtherTooltip.xaml).
> **Package:** `Ether.DesignSystem.Controls`. **Date:** 2026-09-03 (new component, commit `ed1b13b`).

## Verdict at a glance
- **Base-control parity (B):** `EtherTooltip` is **not** the WinUI `ToolTip` / `ToolTipService`
  control. It is a **style-only resource** — a single keyed `Style` targeting stock `Border`
  (`EtherTooltip.xaml:67-79`) plus three themed brushes
  (`EtherTooltipBackgroundBrush`/`EtherTooltipForegroundBrush`/`EtherTooltipStrokeBrush`,
  `EtherTooltip.xaml:48-64`). The source states this explicitly: "A pure visual style — no behavior…
  It carries no hover/dismiss/placement logic" (`EtherTooltip.xaml:6-10`). It is consumed as
  `<Border Style="{StaticResource EtherTooltip}">…</Border>` (`EtherTooltip.xaml:12-18`;
  `TooltipPage.xaml:20-25`).
- **Design coverage (B):** the inverse-surface tooltip skin (inverse fill, 2px stroke, 8px radius,
  `16,8` padding, `MaxWidth 240` for wrap) is supplied by the single style
  (`EtherTooltip.xaml:67-79`); Light/Dark/HighContrast values are all present
  (`EtherTooltip.xaml:48-64`). There is no placement/pointer/auto-dismiss surface **by design** — a
  consumer that needs real tooltip behavior uses WinUI `ToolTipService.ToolTip` and can put this
  styled Border inside it.
- **Consumability (A):** the consumability bar for a style-only resource is narrower than for a
  control: the keys must (1) resolve after merging Controls, (2) apply to the correct `TargetType`
  (`Border`), and (3) re-resolve their themed brushes across Light/Dark/HighContrast. All three are
  now **fixture-proven** — the dictionary is merged from Controls `Generic.xaml:26`, and
  `RuntimeVerification.StyleResources.cs` resolves `EtherTooltip`, asserts the `Border` `TargetType`,
  and re-resolves the themed Background/Stroke brushes across Light/Dark plus a High-Contrast
  `SolidColorBrush` check. The green run emits `styleResources.tooltipStyleResolved=true`
  (`TooltipStyleKeys:["EtherTooltip"]`) and it is gated in `scripts/Verify-ConsumerFixtures.ps1`
  (evidence marker `artifacts/consumer-fixtures/runtime-result-101ef2918b2143728697bbf6cd4c90c5.json`,
  run 2026-09-03).

**Overall: consumer-ready by construction, harness-verified.** Review B passes (style-only surface,
no ❌/⚠️). Review A's former open residual is now **CLOSED**: a ConsumerFixtures assertion
(`RuntimeVerification.StyleResources.cs`) resolves the `EtherTooltip` style key, asserts its `Border`
`TargetType`, and proves the themed brushes re-resolve across themes — surfaced as
`styleResources.tooltipStyleResolved=true` in the green run. The only remaining item is an optional
parity contract script (see Follow-ups).

---

## Review B — Completeness / Parity matrix

| Property / Event | Source | Expected (consumer) | Present? | Bindable/Observable? | Verdict |
| --- | --- | --- | --- | --- | --- |
| consumable control type | base | determine whether `EtherTooltip` can be instantiated | ✘; resource file declares a style + brushes only `EtherTooltip.xaml:44-79` | n/a | 🚫 apply the `Style` to a stock `Border`, not an `EtherTooltip` type `EtherTooltip.xaml:12-18` |
| tooltip surface skin | design | paint the inverse tooltip chrome (fill/stroke/radius/padding/max-width) | `EtherTooltip` `Style` sets `Background`/`BorderBrush`/`BorderThickness=2`/`CornerRadius=RadiusMd`/`BackgroundSizing=OuterBorderEdge`/`Padding=16,8`/`MaxWidth=240` `EtherTooltip.xaml:67-79` | keyed style setters | ✅ |
| tooltip text | base | host the label text; wrap at max width | supplied by the styled Border's `Child` `TextBlock` (usage pairs `body/s-regular` + `TextWrapping=Wrap`) `EtherTooltip.xaml:12-18`; `TooltipPage.xaml:21-24,31-35` | stock Border/TextBlock properties | ✅ |
| foreground brush | design | color the text to match the inverse surface | `EtherTooltipForegroundBrush` (consumer sets it on the child `TextBlock`) `EtherTooltip.xaml:50,56,61` | themed resource | ✅ |
| Light / Dark / High Contrast | design | resolve appropriate brushes in all themes | three theme dictionaries define all three keys `EtherTooltip.xaml:48-64` (HighContrast uses `SystemColor*`) | theme resources | ✅ present; **re-resolution fixture-proven** (`StyleResources.cs`, `tooltipStyleResolved=true`) |
| resource availability | design | resolve the style/brush keys after merging Controls | merged from Controls `Generic.xaml:26` | resource lookup | ✅ present; **resolution fixture-proven** (`StyleResources.cs`, green run) |
| placement / hover / dismiss / auto-open | base (WinUI ToolTip) | show/hide relative to a target with timeout | ✘ none — "no hover/dismiss/placement logic" `EtherTooltip.xaml:6-10` | n/a | 🚫 use WinUI `ToolTipService.ToolTip`; place this styled `Border` as its content if the DS skin is wanted |

**Notes / gaps (B):**
- **The answer to the type-or-Style question is Style only.** There is no `x:Class` or control
  declaration in `EtherTooltip.xaml:44-46`; the only public artifacts are one keyed `Style`
  (`TargetType="Border"`, `EtherTooltip.xaml:67`) and three `SolidColorBrush` keys per theme
  (`EtherTooltip.xaml:48-64`).
- **The "no behavior" exclusion is intentional and documented in-source.** The header states the
  resource "is just XAML, meant to stand in for a tooltip's appearance" (`EtherTooltip.xaml:6-11`).
  This is not a missing tooltip property; it is a deliberate scope boundary. A consumer needing live
  tooltip semantics has the platform `ToolTipService` alternative.
- **The executable style-resource proof now exists** (like Card): `RuntimeVerification.StyleResources.cs`
  resolves and exercises the `EtherTooltip` key (`tooltipStyleResolved=true`). The type-only public
  inventory (`RuntimeVerification.PublicPropertyInventory.cs:32-49`) correctly lists control types only
  — expected, since Tooltip has no Ether-owned type. The only outstanding item is the optional parity
  contract script; see Follow-ups.

---

## Review A — Consumability report

```
Component: EtherTooltip keyed Style + 3 themed brushes (no Ether control type)
Base "control": WinUI ToolTip / ToolTipService (NOT reused; static visual only)
Styled surface: Border   | Package: Ether.DesignSystem.Controls
Declared DPs: 0  | inherited public surface: stock Border

Rubric (styled stock surface):
  R1 DependencyProperty ....... N/A   (no Ether-owned type/DP; the key targets Border,
                                       EtherTooltip.xaml:67)
  R2 Round-trips .............. N/A   (no Ether wrapper/value contract; the style sets stock
                                       Border properties only, EtherTooltip.xaml:67-79)
  R3 Observable ............... N/A   (no Ether-owned state/event; a decorative Border style)
  R4 TwoWay ................... N/A   (no user-editable value property)
  R5 Documented ............... N/A   (no Ether-owned DP/identifier pair; usage and measurements
                                       are documented at EtherTooltip.xaml:1-42)

Contracts:
  C1 ItemsSource / selection .. N/A   (not a collection control)
  C2 Listenable + adapter ..... N/A   (a static styled Border has no control event contract)
  C3 Command .................. N/A   (tooltips are not actions)
  C4 Automation peer/pattern .. N/A   (no interaction role; the styled Border is decorative chrome —
                                       a consumer that needs tooltip UIA semantics uses ToolTipService)

Harness: covered? YES (CLOSED). EtherTooltip is now resolved by RuntimeVerification.StyleResources.cs,
         which adds a Tooltip key set alongside Card/Switch/ScrollBar: it resolves EtherTooltip, asserts
         the Border TargetType, and re-resolves the themed Background/Stroke brushes across Light/Dark
         plus a High-Contrast SolidColorBrush check. The result is surfaced as
         styleResources.tooltipStyleResolved=true (TooltipStyleKeys:["EtherTooltip"]) and gated in
         scripts/Verify-ConsumerFixtures.ps1. A green run confirms it: outcome:"success", evidence
         artifacts/consumer-fixtures/runtime-result-101ef2918b2143728697bbf6cd4c90c5.json (2026-09-03).
         (Verify-ResourceKeys.ps1 still audits only the Foundation EtherColors.xaml token dictionary,
         not this component dictionary — by design; no registration required. A dedicated
         Verify-EtherTooltipContract.ps1 remains an optional follow-up.)
x64 build: the dictionary compiles and merges (Generic.xaml:26); the style-resource fixture now
           references the keys and the green run attests they resolve across themes.
```

---

## Follow-ups (concrete, actionable)

1. **[harness, style-resource] Add `EtherTooltip` to the style-resource fixture — DONE (CLOSED).**
   `tests/Ether.DesignSystem.ConsumerFixtures/RuntimeVerification.StyleResources.cs` now resolves
   `EtherTooltip`, asserts it is a `Style` with `TargetType == typeof(Border)`, resolves its
   `Background`/`BorderBrush` to a `Brush`, and re-resolves the themed brushes
   (`EtherTooltipBackgroundBrush`, `EtherTooltipForegroundBrush`, `EtherTooltipStrokeBrush`) across
   Light/Dark with a High-Contrast `SolidColorBrush` check in `VerifyStyleResourcesInHighContrast`.
   The `StyleResourceVerification` result surfaces `TooltipStyleResolved` + `TooltipStyleKeys`, wired
   into the `styleResources` marker block of `scripts/Verify-ConsumerFixtures.ps1`, and the green run
   emits `styleResources.tooltipStyleResolved=true`
   (`artifacts/consumer-fixtures/runtime-result-101ef2918b2143728697bbf6cd4c90c5.json`, 2026-09-03).
   This is Tooltip's inventory entry, the same way Card's is `StyleResources.cs`.
2. **[harness, optional contract script] Consider a `Verify-EtherTooltipContract.ps1`** modeled on
   the Card/component contract gates: assert the three theme dictionaries define exactly the same
   three component-scoped `EtherTooltip*` keys, that HighContrast uses `SystemColor*`, and that
   `EtherTooltip.xaml` is merged from `Generic.xaml`. Note: **no `Verify-ResourceKeys.ps1`
   registration is required** — that script audits only `EtherColors.xaml` (Foundation tokens),
   not component brush dictionaries.
