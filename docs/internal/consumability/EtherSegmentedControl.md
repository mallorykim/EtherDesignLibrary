# Consumability review — Segmented Control

> Reviewed against
> [the review spec](../2026-09-01-component-consumability-review-spec.md). Findings are grounded in
> source (file:line); the official WinUI baseline is Windows App SDK 2.3.1
> (`Directory.Packages.props:7`).
>
> **Reviewed:** `EtherSegmentedControl` (`: ContentControl`) + `EtherSegmentPanel` (`: Panel`) +
> `EtherSegmentRadioButton` (`: RadioButton`) + `SegmentedSelectionChangedEventArgs` —
> [EtherSegmentedControl.cs](../../../src/Ether.DesignSystem.Controls/Controls/Inputs/EtherSegmentedControl.cs),
> [EtherSegmentedControl.xaml](../../../src/Ether.DesignSystem.Controls/Controls/Inputs/EtherSegmentedControl.xaml),
> [EtherSegmentPanel.cs](../../../src/Ether.DesignSystem.Controls/Controls/Inputs/EtherSegmentPanel.cs),
> [EtherSegmentRadioButton.cs](../../../src/Ether.DesignSystem.Controls/Controls/Inputs/EtherSegmentRadioButton.cs),
> [SegmentedSelectionChangedEventArgs.cs](../../../src/Ether.DesignSystem.Controls/Controls/Inputs/SegmentedSelectionChangedEventArgs.cs).
> **Package:** `Ether.DesignSystem.Controls`. **Date:** 2026-09-01 (post-remediation, R3).

## Verdict at a glance
- **Base-control parity (B):** the host stays `ContentControl` per locked decision D1 (not
  re-derived from `Selector`/`RadioButtons`), but it now **additively** implements the official
  single-select items-control contract on top of the existing inline `EtherSegmentPanel`/
  `EtherSegmentRadioButton` architecture: `ItemsSource`, `ItemTemplate`, `DisplayMemberPath`,
  `SelectedIndex`, and `SelectedItem` are new public DPs (`EtherSegmentedControl.cs:37-77`) that
  generate one `EtherSegmentRadioButton` per item and keep all four selection surfaces
  (`SelectedIndex`/`SelectedItem`/`SelectedValue`/`SelectionChanged`) synchronized through the
  existing `_synchronizingSelection` guard (`…cs:189-249,382-401`). Inline segments keep working
  unchanged when `ItemsSource` is null (`…cs:157-180`).
- **Design coverage (B):** inline two/three/four-option designs still work in the Gallery
  (`SegmentedControlPage.xaml:24-92`), the custom public event args expose both old and new
  consumable values (`SegmentedSelectionChangedEventArgs.cs:3-17`), and the data-driven path is now
  proven end to end: binding `ItemsSource`+`DisplayMemberPath` generates the right segment count and
  content, and selection changes keep `SelectedIndex`/`SelectedItem`/`SelectedValue` in sync
  (`RuntimeVerification.R2.cs:213-224`). This closes the prior B ❌.
- **Consumability (A):** `SelectedValue`, `SelectedIndex`, the new C1 properties, and panel
  `Spacing` are all DP-fixture covered (`RuntimeVerification.PropertyConsumption.cs:270-281,
  296-303`), selection reaches `ObserveSegmentedControl` (`ControlInteractionAdapter.cs:98-112`),
  TwoWay `SelectedValue`/`SelectedIndex` are both proven VM-backed
  (`RuntimeVerification.R2.cs:118-136`), `EtherSegmentRadioButton` is now in the public inventory
  (`RuntimeVerification.PublicPropertyInventory.cs:43`), and automation is proven at the correct
  level: the host is confirmed to expose **no** host-level `Selection` provider (by design — it is
  a `ContentControl`, not a `Selector`), while each segment's `SelectionItem` pattern is asserted
  with `IsSelected` tracking `IsChecked` (`RuntimeVerification.R2.cs:339-359`).

**Overall: consumer-ready.** Review B's C1 data-driven surface gap is closed (D1 additive contract shipped); all four of Review A's follow-ups are closed by the harness (`ETHER_CONSUMER_SMOKE` `outcome:"success"`).

---

## Review B — Completeness / Parity matrix

| Property / Event | Source | Expected (consumer) | Present? | Bindable/Observable? | Verdict |
| --- | --- | --- | --- | --- | --- |
| inline segments | design | author 2/3/4 options directly in XAML | ✔ `EtherSegmentPanel` + `EtherSegmentRadioButton`; Gallery examples `SegmentedControlPage.xaml:24-92`; kept fully working alongside `ItemsSource` `EtherSegmentedControl.cs:157-180` | n/a | ✅ |
| `ItemsSource` | design | bind an option collection from a VM; count is data-driven | ✔ new DP; generates one `EtherSegmentRadioButton` per item `EtherSegmentedControl.cs:37-42,80-84,214-249`; data path fixture-proven `RuntimeVerification.R2.cs:213-224` | ✔ DP + callback | ✅ **closed this wave (D1)** |
| `ItemTemplate` / `DisplayMemberPath` | design | render consumer models as segments | ✔ both new DPs `EtherSegmentedControl.cs:44-56,86-101`; consumed by `CreateGeneratedSegment`/`ResolveDisplayMember` `…cs:251-283` | ✔ DPs | ✅ **closed this wave (D1)** |
| `SelectedValue` | design | two-way bind a stable backend value | object DP; uses `Tag`, falling back to `Content` `EtherSegmentedControl.cs:72-77,122-126`; VM-backed TwoWay round-trip now proven `RuntimeVerification.R2.cs:118-126` | ✔ DP + callback | ✅ |
| `SelectedItem` / `SelectedIndex` | design | select by model or index | ✔ both new DPs, kept mutually consistent with `SelectedValue` via `_synchronizingSelection` `EtherSegmentedControl.cs:58-70,103-115,189-249,382-401`; `SelectedIndex` VM-backed TwoWay round-trip now proven `RuntimeVerification.R2.cs:128-136` | ✔ DPs | ✅ **closed this wave (D1)** |
| `SelectionChanged` | design | listen to old/new selection → backend | public `EventHandler<SegmentedSelectionChangedEventArgs>` `EtherSegmentedControl.cs:129` | event + adapter `ControlInteractionAdapter.cs:98-112` | ✅ |
| event args value | design | receive public, backend-consumable old/new values | public sealed args with `OldValue`/`NewValue` `SegmentedSelectionChangedEventArgs.cs:3-17` | immutable public values | ✅ |
| equal-width layout / `Spacing` | design | stretch options evenly and set the gap | `EtherSegmentPanel.Spacing` DP; layout divides width `EtherSegmentPanel.cs:18-31,72-90` | ✔ DP + callback | ✅ |
| segment content / checked state / grouping | base | provide label/template and radio mutual exclusion | inherited by `EtherSegmentRadioButton : RadioButton` `EtherSegmentRadioButton.cs:8-26`; template passes content `EtherSegmentedControl.xaml:221-245` | ✔ DPs + events | ✅ |
| `IsEnabled` | base | disable the host/options | inherited on host/items; segment Disabled state exists `EtherSegmentedControl.xaml:151-170` | ✔ DP; host propagation unverified | ✅ (unverified by fixture) |
| host-level Selection automation | design | determine whether the host itself is a UIA selection container | ✗ by design — `ContentControl`, not `Selector`; explicitly confirmed absent `RuntimeVerification.R2.cs:339-341` | n/a | 🚫 group semantics are verified through each segment's SelectionItem pattern instead (D1) |
| locked host appearance | base | override track chrome/font/foreground | registry records fixed or platform-noop properties `scripts/UnsupportedProperties.psd1:393-403` | writable DPs; selected bindings only | 🚫 use the fixed track/segment styles `getting-started.md:368-384` |

**Notes / gaps (B):**
- **D1 is implemented additively, exactly as locked.** `EtherSegmentedControl` is still
  `public class EtherSegmentedControl : ContentControl` (`EtherSegmentedControl.cs:28`); the new
  `ItemsSource`/`ItemTemplate`/`DisplayMemberPath`/`SelectedIndex`/`SelectedItem` DPs sit alongside
  the pre-existing inline-content/`SelectedValue` surface, not in place of it. When `ItemsSource` is
  null the control falls back to the original inline-content scan (`OnItemsSourceChanged`,
  `…cs:157-180`); when set, it generates segments into a fresh `EtherSegmentPanel`
  (`RebuildGeneratedContent`, `…cs:214-249`) — the class remarks document this explicitly
  (`…cs:9-26`).
- **The custom event contract itself is consumable.** The event type is public
  (`EtherSegmentedControl.cs:129`), its args are public and immutable
  (`SegmentedSelectionChangedEventArgs.cs:3-17`), and the adapter serializes both values
  (`ControlInteractionAdapter.cs:98-112`).
- **The registry is consistent with the host template.** It records font size/family and content
  alignment as consumed, while other chrome stays fixed (`scripts/UnsupportedProperties.psd1:393-405`);
  the template contains those content/font bindings (`EtherSegmentedControl.xaml:221-245`).

---

## Review A — Consumability report

```
Component: EtherSegmentedControl (+ EtherSegmentPanel, EtherSegmentRadioButton, event args)
Base control: ContentControl / Panel / RadioButton   | Package: Ether.DesignSystem.Controls
Declared DPs: 7  (ItemsSource, ItemTemplate, DisplayMemberPath, SelectedIndex, SelectedItem,
                  SelectedValue, EtherSegmentPanel.Spacing)
                 | inherited public surface: ContentControl + RadioButton item surface

Rubric (declared DPs):
  R1 DependencyProperty ....... PASS  (public identifiers + wrappers,
                                       EtherSegmentedControl.cs:37-126;
                                       EtherSegmentPanel.cs:18-31)
  R2 Round-trips .............. PASS  (fixture's generic SetValue/wrapper checks,
                                       PropertyConsumption.cs:62-97; samples at cs:296-303)
  R3 Observable ............... PASS  (callbacks + fixture registration,
                                       EtherSegmentedControl.cs:38-77,157-212;
                                       EtherSegmentPanel.cs:19-36)
  R4 TwoWay ................... PASS  (VM-backed SelectedValue and SelectedIndex bindings both
                                       round-trip in both directions, RuntimeVerification.R2.cs:118-136)
  R5 Documented ............... PASS  (identifier/wrapper docs for all seven declared properties
                                       now state their registered default, including SelectedValue's
                                       null default and Spacing's 4-DIP default,
                                       EtherSegmentedControl.cs:37-126; EtherSegmentPanel.cs:18-31)

Contracts:
  C1 ItemsSource / selection .. PASS  (ItemsSource + ItemTemplate/DisplayMemberPath + SelectedIndex
                                       + SelectedItem + SelectedValue all present and mutually
                                       synchronized, EtherSegmentedControl.cs:37-126,189-249,
                                       382-401; data path fixture-proven —item count, generated
                                       content, and selection sync— RuntimeVerification.R2.cs:213-224)
  C2 Listenable + adapter ..... PASS  (public args/event + ObserveSegmentedControl,
                                       ControlInteractionAdapter.cs:98-112; exercised at
                                       PropertyConsumption.cs:371)
  C3 Command .................. N/A   (host selection is state/event based; item command behavior
                                       is inherited RadioButton behavior)
  C4 Automation peer/pattern .. PASS  (host correctly has NO Selection provider —confirmed absent,
                                       not merely unchecked, RuntimeVerification.R2.cs:339-341—
                                       and each generated/inline segment's SelectionItem pattern is
                                       asserted with IsSelected tracking IsChecked,
                                       RuntimeVerification.R2.cs:343-359)

Harness: covered? YES. EtherSegmentedControl/EtherSegmentPanel/EtherSegmentRadioButton are all in
         PublicPropertyInventoryTypes (PublicPropertyInventory.cs:41-43) and
         DeclaredPropertyOwnerTypes (PropertyConsumption.cs:270-281); adapter, TwoWay, data-path,
         and automation fixtures all exist and are confirmed green
         (PropertyConsumption.cs:371; RuntimeVerification.R2.cs:118-136,213-224,339-359). Runtime
         marker ETHER_CONSUMER_SMOKE outcome:"success" includes "EtherSegmentedControl.SelectedValue"
         and "EtherSegmentedControl.SelectedIndex" in twoWayBindings.Properties,
         dataPaths.SegmentSelectionSynchronized:true, and
         "EtherSegmentedControl.Segment[0].SelectionItem"/"[1]" in automationPatterns.Patterns
         (artifacts/audit-runs/consumer-runtime-evidence-20260901-161450498/runtime-result.json).
x64 build: green (Controls + Interactions + ConsumerFixtures, per the R2.11 runtime run recorded
           in docs/internal/consumability/_RUN-ON-DESKTOP.md).
```

---

## Follow-ups (concrete, actionable)

1. ~~**[B, C1, R4] Add a selector-backed data contract.**~~ DONE, additively per locked decision D1
   — `EtherSegmentedControl.cs:37-126,157-249` adds `ItemsSource`/`ItemTemplate`/
   `DisplayMemberPath`/`SelectedIndex`/`SelectedItem` on top of the existing inline architecture
   (not a re-derivation from `Selector`/`RadioButtons`); VM-backed two-way assertions exist in
   `RuntimeVerification.R2.cs:118-136`.
2. ~~**[C4, harness] Prove selection automation.**~~ DONE — `RuntimeVerification.R2.cs:339-359`
   confirms the host correctly exposes no Selection provider (by design) and requires each
   generated/inline segment's SelectionItem pattern, including selected-state changes through
   automation.
3. ~~**[harness] Inventory the public segment item type.**~~ DONE — `EtherSegmentRadioButton` is now
   in `PublicPropertyInventoryTypes` (`RuntimeVerification.PublicPropertyInventory.cs:43`).
4. ~~**[R5] Complete declared-DP default docs.**~~ DONE — identifier and wrapper XML docs for
   `SelectedValue` (default null) and `Spacing` (default `4d`, in device-independent pixels, "epx"
   corrected) are complete (`EtherSegmentedControl.cs:122-126`; `EtherSegmentPanel.cs:18-31`), and
   the five new C1 properties carry the same treatment.

No remaining follow-ups.
