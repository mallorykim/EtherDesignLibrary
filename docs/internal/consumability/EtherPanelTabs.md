# Consumability review — Panel Tabs

> Reviewed against
> [the review spec](../2026-09-01-component-consumability-review-spec.md). Findings are grounded in
> source (file:line); the official WinUI baseline is Windows App SDK 2.3.1
> (`Directory.Packages.props:7`).
>
> **Reviewed:** the keyed `EtherPanelTabs` + `EtherPanelTabSegment` `Style` resources and their two
> `ControlTemplate`s + themed brushes —
> [EtherPanelTabs.xaml](../../../src/Ether.DesignSystem.Controls/Controls/Inputs/EtherPanelTabs.xaml).
> Reuses `EtherSegmentedControl` (`: ContentControl`,
> [EtherSegmentedControl.cs:34](../../../src/Ether.DesignSystem.Controls/Controls/Inputs/EtherSegmentedControl.cs))
> + `EtherSegmentRadioButton` (`: RadioButton`,
> [EtherSegmentRadioButton.cs:26](../../../src/Ether.DesignSystem.Controls/Controls/Inputs/EtherSegmentRadioButton.cs)).
> **Package:** `Ether.DesignSystem.Controls`. **Date:** 2026-09-03 (new component, commit `4e4596d`).

## Verdict at a glance
- **Base-control parity (B):** Panel Tabs is a **style-only re-skin**, not a new control. It ships two
  keyed styles — `EtherPanelTabs` (`TargetType="controls:EtherSegmentedControl"`,
  `EtherPanelTabs.xaml:250-261`) applied to the segmented-control host, and `EtherPanelTabSegment`
  (`TargetType="RadioButton"`, `EtherPanelTabs.xaml:236-248`) applied to each segment — plus two
  `ControlTemplate`s (`EtherPanelTabsTrackTemplate` `:96-125`, `EtherPanelTabSegmentTemplate`
  `:129-234`) and a themed brush set (`:54-86`). The class, behaviour, selection model, radio
  grouping, and template part names are **reused unchanged** from `EtherSegmentedControl` /
  `EtherSegmentRadioButton` — "only the skin differs" (`EtherPanelTabs.xaml:5-10,37-40`). It is
  consumed as `<controls:EtherSegmentedControl Style="{StaticResource EtherPanelTabs}">` with
  `<controls:EtherSegmentRadioButton Style="{StaticResource EtherPanelTabSegment}"/>` children
  (`EtherPanelTabs.xaml:12-21`; `PanelTabsPage.xaml:19-42`).
- **Design coverage (B):** the SidePanelTab treatment (translucent track, near-black/near-white
  selected pill, larger segment, native `ThemeShadow` float) is fully supplied by the two styles +
  templates + brushes (`EtherPanelTabs.xaml:96-261`); Light/Dark/HighContrast are all present
  (`:54-86`). The `ThemeShadow` cannot be per-theme coloured (documented limitation,
  `EtherPanelTabs.xaml:28-31,91-95`) — an accepted skin trade-off, not a consumability gap.
- **Consumability (A):** because Panel Tabs is a re-skin of `EtherSegmentedControl`, the entire
  consumable API of that control (`ItemsSource`/`ItemTemplate`/`DisplayMemberPath`/`SelectedIndex`/
  `SelectedItem`/`SelectedValue`/`SelectionChanged`/`SelectionCommand` + `EtherSegmentPanel.Spacing`
  + each segment's `IsChecked`/`GroupName`/`SelectionItem` pattern) remains available and is already
  proven for the underlying control ([EtherSegmentedControl.md](EtherSegmentedControl.md)). The
  style-resource consumability bar itself — the two keys resolve, apply to the right target types,
  and the themed brushes re-resolve across Light/Dark/HighContrast — is now **fixture-proven**:
  `RuntimeVerification.StyleResources.cs` resolves `EtherPanelTabs`/`EtherPanelTabSegment`, asserts
  their `EtherSegmentedControl`/`RadioButton` target types, and re-resolves the themed track brush
  across themes (green run: `styleResources.panelTabsStyleResolved=true`).

**Overall: consumer-ready by construction, harness-verified.** Review B passes (style-only re-skin,
no ❌/⚠️). Review A's former open residual is **CLOSED**: `RuntimeVerification.StyleResources.cs` now
resolves `EtherPanelTabs`/`EtherPanelTabSegment`, asserts they apply to `EtherSegmentedControl` /
`RadioButton`, and proves the themed brushes re-resolve across themes
(`styleResources.panelTabsStyleResolved=true`, `PanelTabsStyleKeys:["EtherPanelTabs","EtherPanelTabSegment"]`).
The second finding is also **CLOSED**: `EtherPanelTabs.xaml` + `EtherTooltip.xaml` were added to
`scripts/Verify-ResourceGraph.ps1`'s expected Generic.xaml merge set, so that gate is green again. The
reused host's own API is fully covered; Panel Tabs adds no new public surface to cover. The only
remaining item is an optional `Verify-EtherPanelTabsContract.ps1` parity script (see Follow-ups).

---

## Review B — Completeness / Parity matrix

| Property / Event | Source | Expected (consumer) | Present? | Bindable/Observable? | Verdict |
| --- | --- | --- | --- | --- | --- |
| consumable control type | base | determine whether `EtherPanelTabs` is a type | ✘; it is two keyed styles applied to the existing `EtherSegmentedControl`/segment `EtherPanelTabs.xaml:236-261` | n/a | 🚫 apply the styles to `EtherSegmentedControl` + `EtherSegmentRadioButton`, not a new type `EtherPanelTabs.xaml:12-21` |
| panel-tab track skin | design | apply the translucent floating side-panel track | `EtherPanelTabs` style + `EtherPanelTabsTrackTemplate` (`Padding=4`, `RadiusSm`, `ThemeShadow` via `Translation Z=28`) `EtherPanelTabs.xaml:96-125,250-261` | keyed style setters | ✅ |
| panel-tab segment skin | design | apply the larger pill segment + hover/pressed/checked layers | `EtherPanelTabSegment` style + `EtherPanelTabSegmentTemplate` (`Padding=12,10`, `RadiusSm`, Hover/Pressed/Checked layers) `EtherPanelTabs.xaml:129-248` | keyed style setters | ✅ |
| selection / count / index / value | base (reused) | select by index/item/value; data-drive count | inherited from `EtherSegmentedControl` (host style does not shadow it) `EtherSegmentedControl.cs:37-126`; proven in [EtherSegmentedControl.md](EtherSegmentedControl.md) | ✔ DPs + `SelectionChanged` | ✅ |
| segment content / checked / grouping | base (reused) | label + radio mutual exclusion | inherited by `EtherSegmentRadioButton : RadioButton`; template part names (`HoverLayer`/`PressedLayer`/`CheckedLayer`/`Bg`/`Cp`/`FocusRing`/`TextContent`) match so the class drives it unchanged `EtherPanelTabs.xaml:37-40,180-232`; `EtherSegmentRadioButton.cs:28-40` | ✔ DPs + events | ✅ |
| hover / pressed feedback fidelity | base (reused) | full hover/pressed layer feedback | requires `EtherSegmentRadioButton` (drives layers from live pointer state), not a plain `RadioButton` `EtherSegmentRadioButton.cs:15-25`; Gallery uses `EtherSegmentRadioButton` `PanelTabsPage.xaml:22-40` | code-behind driven | ✅ (plain `RadioButton` shows only the checked pill — inherited from the segment style contract) |
| Light / Dark / High Contrast | design | resolve appropriate brushes in all themes | three theme dictionaries define all nine `EtherPanelTabs*` keys `EtherPanelTabs.xaml:54-86` (HighContrast uses `SystemColor*`) | theme resources | ✅ present; **re-resolution fixture-proven** (`StyleResources.cs`, `panelTabsStyleResolved=true`) |
| High Contrast fg/bg pairing | design | readable text on Highlight-filled states | HighContrast defines paired `EtherPanelTabsSegmentForeground*CheckedBrush = SystemColorHighlightTextColor` `EtherPanelTabs.xaml:80-82`; satisfies `Verify-HighContrastPairing.ps1` Rule B | static analysis | ✅ |
| resource availability | design | resolve the keys after merging Controls | merged from Controls `Generic.xaml:22` | resource lookup | ✅ present; **fixture-proven** (`StyleResources.cs`, green run); `Verify-ResourceGraph.ps1` merge set reconciled |
| native drop shadow colour per theme | design | tint the float shadow per theme | ✘ `ThemeShadow` is uncolourable `EtherPanelTabs.xaml:28-31,91-95` | n/a | 🚫 accepted skin trade-off (Composition shadow deferred); documented in source |

**Notes / gaps (B):**
- **The answer to the type-or-Style question is Style only.** There is no `x:Class` in
  `EtherPanelTabs.xaml:48-51`; the public artifacts are two keyed styles
  (`EtherPanelTabs.xaml:236-261`), two templates, and the themed brush set. Consumers instantiate the
  existing `EtherSegmentedControl` + `EtherSegmentRadioButton` and swap the style keys.
- **Skeleton reuse is verified structurally.** The segment template's part names match the ones
  `EtherSegmentRadioButton` looks up (`HoverLayer`/`PressedLayer`/`CheckedLayer`, plus
  `Bg`/`Cp`/`FocusRing`/`TextContent`), so the class drives the panel-tab skin unchanged
  (`EtherPanelTabs.xaml:37-40`; `EtherSegmentRadioButton.cs:28-40`) — a re-skin, not a fork.
- **`EtherPanelTabSegment` targets `RadioButton`, not `EtherSegmentRadioButton`** (mirrors the
  base `EtherSegment` style at `EtherSegmentedControl.xaml:259`). It therefore applies to both, but
  full hover/pressed feedback needs the `EtherSegmentRadioButton` subclass (the checked pill alone
  renders on a plain `RadioButton`) — the same documented contract the base segment carries
  (`EtherSegmentRadioButton.cs:8-13`).

---

## Review A — Consumability report

```
Component: EtherPanelTabs + EtherPanelTabSegment keyed Styles (no Ether control type)
Base "control": EtherSegmentedControl (ContentControl) reused as host; EtherSegmentRadioButton
                (RadioButton) reused as segment — style-only re-skin, no new behaviour/class.
Styled surfaces: EtherSegmentedControl (host) + RadioButton (segment)
Package: Ether.DesignSystem.Controls
Declared DPs: 0 new  | consumable API inherited whole from EtherSegmentedControl/EtherSegmentRadioButton

Rubric (styled reused surfaces):
  R1 DependencyProperty ....... N/A for the skin (adds no DP); the reused host's DPs pass R1 —
                                       EtherSegmentedControl.cs:37-126 (proven in EtherSegmentedControl.md)
  R2 Round-trips .............. Inherited from EtherSegmentedControl (proven, PropertyConsumption.cs:62-97);
                                       the skin adds no value contract
  R3 Observable ............... Inherited (SelectionChanged + segment SelectionItem, proven for the host)
  R4 TwoWay ................... Inherited (SelectedValue/SelectedIndex TwoWay proven, R2.cs:118-136)
  R5 Documented ............... N/A for the skin (no Ether-owned DP/identifier pair); usage +
                                       measurements documented at EtherPanelTabs.xaml:1-47
Contracts:
  C1 ItemsSource / selection .. Inherited from EtherSegmentedControl (data path proven, R2.cs:213-224)
  C2 Listenable + adapter ..... Inherited (ObserveSegmentedControl, ControlInteractionAdapter.cs:98-112)
  C3 Command .................. Inherited (SelectionCommand, EtherSegmentedControl.cs:85-97)
  C4 Automation peer/pattern .. Inherited (each segment's SelectionItem pattern, R2.cs:343-359)

Harness: covered? YES for the skin (CLOSED) — and the reused host's API was already covered.
         RuntimeVerification.StyleResources.cs now adds a Panel Tabs key set alongside Card/Switch/ScrollBar:
         it resolves EtherPanelTabs/EtherPanelTabSegment, asserts they apply to
         EtherSegmentedControl/RadioButton, and re-resolves the themed track brush across
         Light/Dark/HighContrast. The result surfaces as styleResources.panelTabsStyleResolved=true
         (PanelTabsStyleKeys:["EtherPanelTabs","EtherPanelTabSegment"]), gated in
         scripts/Verify-ConsumerFixtures.ps1, and confirmed by a green run (outcome:"success",
         artifacts/consumer-fixtures/runtime-result-101ef2918b2143728697bbf6cd4c90c5.json, 2026-09-03).
         Additionally, Verify-ResourceGraph.ps1's hardcoded Generic.xaml merge set now includes
         EtherPanelTabs.xaml and EtherTooltip.xaml (Verify-ResourceGraph.ps1:86-99), so that gate is green.
x64 build: the dictionary compiles and merges (Generic.xaml:22); the style-resource fixture references the
           panel-tab skin keys and the green run attests they resolve across themes.
```

---

## Follow-ups (concrete, actionable)

1. **[harness, style-resource] Add `EtherPanelTabs` + `EtherPanelTabSegment` to the style-resource
   fixture — DONE (CLOSED).** `RuntimeVerification.StyleResources.cs` now resolves both keys, asserts
   `EtherPanelTabs` targets `EtherSegmentedControl` and `EtherPanelTabSegment` targets `RadioButton`
   (Template setter), and re-resolves the themed track brush across Light/Dark with a HighContrast
   `SolidColorBrush` check in `VerifyStyleResourcesInHighContrast`. The `StyleResourceVerification`
   result surfaces `PanelTabsStyleResolved` + `PanelTabsStyleKeys`, wired into the `styleResources`
   marker block of `scripts/Verify-ConsumerFixtures.ps1`; the green run emits
   `styleResources.panelTabsStyleResolved=true`
   (`artifacts/consumer-fixtures/runtime-result-101ef2918b2143728697bbf6cd4c90c5.json`, 2026-09-03).
   This is Panel Tabs' inventory entry, analogous to Card's.
2. **[harness, stale contract] Update `scripts/Verify-ResourceGraph.ps1:86-99` — DONE (CLOSED).**
   `EtherPanelTabs.xaml` and `EtherTooltip.xaml` were added to the expected Generic.xaml merge set in
   source order (`Generic.xaml:22,26`), so `Assert-SetEquals` passes and the gate is green on the
   current tree.
3. **[harness, optional contract script] Consider a `Verify-EtherPanelTabsContract.ps1`** parity gate
   (asserting the two theme dictionaries define the same `EtherPanelTabs*` keys and that
   `EtherPanelTabs.xaml` is merged from `Generic.xaml`). Optional only — the StyleResources fixture and
   the reconciled ResourceGraph gate already cover the skin.
4. **[none — reused host]** No new product API is required; the underlying `EtherSegmentedControl`
   API is already consumer-ready and harness-covered (see [EtherSegmentedControl.md](EtherSegmentedControl.md)).
   Panel Tabs adds only a skin.
