# Consumability review — Tab Navigation

> Reviewed against
> [the review spec](../2026-09-01-component-consumability-review-spec.md). Findings are grounded in
> source (file:line); the official WinUI baseline is Windows App SDK 2.3.1
> (`Directory.Packages.props:7`).
>
> **Reviewed:** `EtherTabNavigation` (`: ListView`) + `EtherTabItem` (`: ListViewItem`) —
> [EtherTabNavigation.cs](../../../src/Ether.DesignSystem.Controls/Controls/Navigation/EtherTabNavigation.cs),
> [EtherTabNavigation.xaml](../../../src/Ether.DesignSystem.Controls/Controls/Navigation/EtherTabNavigation.xaml).
> **Package:** `Ether.DesignSystem.Controls`. **Date:** 2026-09-01 (post-remediation, R3). This was
> the spec's original worked sample; it is now a fully audited report like the other 14, with its
> two harness gaps closed.

## Verdict at a glance
- **Base-control parity (B):** strong. Faithful ListView reskin — official skeleton kept (item-container
  override `EtherTabNavigation.cs:14-17`, stock VSM groups retained `EtherTabNavigation.xaml:63-67`),
  so `ItemsSource` / `SelectedItem` / `SelectedIndex` / `SelectionChanged` / `ItemTemplate` /
  `SelectionMode` all come through inherited — and are now fixture-proven, not just asserted by
  inheritance (see below).
- **Design coverage (B):** `EtherTabItem.Icon` is a **string keyed to the internal icon library**
  (D3), confirmed intentional: the class remarks document it as "on-brand icon-library names only;
  arbitrary `IconElement` is out of contract by design," so a consumer cannot pass an arbitrary
  image/glyph. No code change — this is a locked, documented 🚫, not an open question anymore.
- **Consumability (A):** both prior gaps are closed. `EtherTabNavigation`/`EtherTabItem` are now in
  the `ConsumerFixtures` harness (`RuntimeVerification.PublicPropertyInventory.cs:47-48`;
  `RuntimeVerification.PropertyConsumption.cs:280,327`), and `ControlInteractionAdapter` gained
  `ObserveSelection(Selector, …)` for `SelectionChanged` → backend
  (`ControlInteractionAdapter.cs:56-69`), exercised end to end
  (`RuntimeVerification.PropertyConsumption.cs:382-387`). TwoWay `SelectedIndex`/`SelectedItem` are
  proven VM-backed (`RuntimeVerification.R2.cs:168-186`), the C1 `ItemsSource`+selection data path is
  proven (`RuntimeVerification.R2.cs:205-211`), and the ListView `Selection` UIA pattern is asserted
  single-select (`RuntimeVerification.R2.cs:307-337`).

**Overall: consumer-ready.** The D3 `Icon` shape decision is resolved (🚫, documented, no widening); all three of Review A's follow-ups are closed by the harness (`ETHER_CONSUMER_SMOKE` `outcome:"success"`).

---

## Review B — Completeness / Parity matrix

| Property / Event | Source | Expected (consumer) | Present? | Bindable/Observable? | Verdict |
| --- | --- | --- | --- | --- | --- |
| `ItemsSource` | base | bind the tab list from a VM; count is data-driven | inherited `ListView.ItemsSource`; data-path fixture-proven `RuntimeVerification.R2.cs:205-211` | ✔ DP | ✅ |
| inline `EtherTabItem`s | design | author tabs directly in XAML | ✔ (Gallery uses 2/3/5) `TabNavigationPage.xaml:15-31` | n/a | ✅ |
| `SelectedIndex` | base | set/two-way current tab | inherited; VM-backed TwoWay round-trip now proven `RuntimeVerification.R2.cs:168-176` | ✔ DP | ✅ |
| `SelectedItem` | base | two-way current tab (data or container) | inherited; VM-backed TwoWay round-trip now proven `RuntimeVerification.R2.cs:178-186` | ✔ DP | ✅ (see note) |
| `SelectedValue`/`SelectedValuePath` | base | select by model key | inherited | ✔ DP | ✅ (unverified by a dedicated fixture) |
| `SelectionChanged` | base | **listen to tab switch → backend** | inherited; used `TabNavigationPage.xaml:15`; now adapted end to end | event + `ObserveSelection` `ControlInteractionAdapter.cs:56-69`; exercised at `PropertyConsumption.cs:382-387` | ✅ **closed this wave** |
| `SelectionMode` | base | single-select (default) | set `=Single` `EtherTabNavigation.xaml:88` | ✔ | ✅ |
| `ItemTemplate`/`ItemTemplateSelector` | base | custom tab content | inherited | ✔ DP | ✅ (unverified by fixture) |
| `EtherTabItem.Content` | base | tab label / arbitrary content | inherited; string path via converter `…xaml:78-79` | ✔ DP | ✅ |
| `EtherTabItem.Icon` | design | per-tab leading icon, on/off | `Icon` (string DP) `EtherTabNavigation.cs:28-41`; resolved against `EtherIconGeometries` `…cs:70-98` | ✔ DP + callback | 🚫 **string-only by design (D3)** — resolved against the internal icon library; an unknown name silently collapses the slot |
| `EtherTabItem.IsSelected` | base | per-item selected state | inherited | ✔ DP | ✅ |
| `IsEnabled` (per item) | base | disable a tab | inherited; VSM `Disabled` `…xaml:60` | ✔ | ✅ |

**Notes / gaps (B):**
- **Icon is confirmed string-only by design (D3), not an open question.** `Icon="Home"` resolves
  against `EtherIconGeometries` (`EtherTabNavigation.cs:70-98`); an unknown name silently collapses
  the slot. The remediation plan locked this as intentional — "on-brand icon-library names only;
  arbitrary `IconElement` is out of contract by design" — and no code change was made. The row is
  marked 🚫 with that reason; a consumer who needs an arbitrary glyph places their own `IconElement`
  beside the tab item rather than through `Icon`.
- `SelectedItem` returns the `EtherTabItem` container for inline tabs, or the bound model under
  `ItemsSource`; this is confirmed by the data-path fixture, which asserts `SelectedItem` resolves
  to the actual string member of the `ItemsSource` array after a `SelectedIndex` change
  (`RuntimeVerification.R2.cs:205-211`).

---

## Review A — Consumability report

```
Component: EtherTabNavigation (+ EtherTabItem)
Base control: ListView / ListViewItem   | Package: Ether.DesignSystem.Controls
Declared DPs: 1  (EtherTabItem.Icon)  | inherited public surface: full ListView/Selector

Rubric (declared DP: EtherTabItem.Icon):
  R1 DependencyProperty ....... PASS  (IconProperty + CLR wrapper, EtherTabNavigation.cs:28-41)
  R2 Round-trips .............. PASS  (standard GetValue/SetValue wrapper; fixture asserts both
                                       paths, PropertyConsumption.cs:62-97)
  R3 Observable ............... PASS  (OnIconChanged callback, cs:29,52-53; fixture registers the
                                       callback with the "Home" sample, PropertyConsumption.cs:327)
  R4 TwoWay ................... N/A   (set-once config, not user-edited state)
  R5 Documented ............... PASS  (clear XML-doc, cs:31-36)

Contracts:
  C1 ItemsSource / selection .. PASS  (ItemsSource drives Items.Count, and SelectedIndex changes
                                       resolve SelectedItem to the correct member,
                                       RuntimeVerification.R2.cs:205-211)
  C2 Listenable + adapter ..... PASS  (SelectionChanged is listenable, and
                                       ControlInteractionAdapter now has ObserveSelection(Selector,
                                       …) accepting ListView/EtherTabNavigation and any Selector,
                                       ControlInteractionAdapter.cs:56-69; exercised end to end at
                                       PropertyConsumption.cs:382-387)
  C3 Command .................. N/A   (tab strips interact via selection, not ICommand)
  C4 Automation peer/pattern .. PASS  (ListView Selection pattern asserted single-select —
                                       CanSelectMultiple=false, exactly one selected item,
                                       RuntimeVerification.R2.cs:307-337)

Harness: covered? YES. EtherTabNavigation and EtherTabItem are now in
         PublicPropertyInventoryTypes (RuntimeVerification.PublicPropertyInventory.cs:47-48) and
         EtherTabItem is in DeclaredPropertyOwnerTypes
         (RuntimeVerification.PropertyConsumption.cs:280) with an Icon="Home" sample (…cs:327).
         TwoWay, data-path, adapter, and Selection-automation fixtures all exist in
         RuntimeVerification.R2.cs and RuntimeVerification.PropertyConsumption.cs. Runtime marker
         ETHER_CONSUMER_SMOKE outcome:"success" includes "EtherTabNavigation.SelectedIndex"/
         "EtherTabNavigation.SelectedItem" in twoWayBindings.Properties,
         dataPaths.TabSelectionSynchronized:true, and "EtherTabNavigation.Selection" in
         automationPatterns.Patterns, and 15 types (including EtherTabNavigation/EtherTabItem) in
         publicPropertyInventory
         (artifacts/audit-runs/consumer-runtime-evidence-20260901-161450498/runtime-result.json).
x64 build: green (Controls + Interactions + ConsumerFixtures, per the R2.11 runtime run recorded
           in docs/internal/consumability/_RUN-ON-DESKTOP.md).
```

---

## Follow-ups (concrete, actionable)

1. ~~**[C2, backend] Add a selection observer to `ControlInteractionAdapter`.**~~ DONE — new
   `ObserveSelection(Selector control, …)` (covers `ListView`/`EtherTabNavigation` and any
   `Selector`) emits `{ SelectedIndex, SelectedValue }` as an `InteractionEvent`, mirroring
   `ObserveDropdown` (`ControlInteractionAdapter.cs:56-69`). Added to
   `VerifyStandardInteractionAdapters` (`PropertyConsumption.cs:382-387`).
2. ~~**[harness] Put Tab Navigation into the inventory.**~~ DONE — `EtherTabNavigation`, `EtherTabItem`
   are in `PublicPropertyInventoryTypes` (`…PublicPropertyInventory.cs:47-48`) and `EtherTabItem` is
   in `DeclaredPropertyOwnerTypes` (`…PropertyConsumption.cs:280`) with an `Icon` sample (`"Home"`,
   `…cs:327`), so R1–R3 + the backend envelope are proven, not assumed.
3. ~~**[C1/C4] Add a Tab Navigation fixture.**~~ DONE — `RuntimeVerification.R2.cs` binds an
   `ItemsSource`, asserts `SelectedIndex`/`SelectedItem` two-way round-trip (`R2.cs:168-186`), a
   data-driven item-count/selection-sync path (`R2.cs:205-211`), fires `SelectionChanged` through
   the new adapter into a backend envelope (`PropertyConsumption.cs:382-387`), and checks the
   Selection automation pattern (`R2.cs:307-337`). A separate dedicated
   `RuntimeVerification.TabNavigation.cs` file was not created — the proofs live in `R2.cs` and
   `PropertyConsumption.cs` instead, which is an equally valid harness location.
4. ~~**[B decision] Confirm the `Icon` contract** (string-only vs. `IconElement`).~~ DONE (D3) —
   locked as string-only, documented 🚫 in the matrix above; no widening.

No remaining follow-ups. `SelectedValue`/`SelectedValuePath` and a consumer `ItemTemplate` remain
without a dedicated fixture (minor residual, not a rubric failure — the same status these carry for
`EtherDropdown`).
