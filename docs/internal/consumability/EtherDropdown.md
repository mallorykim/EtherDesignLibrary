# Consumability review — Dropdown

> Reviewed against
> [the review spec](../2026-09-01-component-consumability-review-spec.md). Findings are grounded in
> source (file:line); the official WinUI baseline is Windows App SDK 2.3.1
> (`Directory.Packages.props:7`).
>
> **Reviewed:** `EtherDropdown` (`: ComboBox`) —
> [EtherDropdown.cs](../../../src/Ether.DesignSystem.Controls/Controls/Inputs/EtherDropdown.cs),
> [EtherDropdown.xaml](../../../src/Ether.DesignSystem.Controls/Controls/Inputs/EtherDropdown.xaml).
> **Package:** `Ether.DesignSystem.Controls`. **Date:** 2026-09-01 (post-remediation, R3).

## Verdict at a glance
- **Base-control parity (B):** the core select-only `ComboBox` surface survives—data items,
  selection, placeholder, open/close, and `SelectionChanged` are inherited from the confirmed
  `ComboBox` base (`EtherDropdown.cs:12-18,64`), and the adapter accepts it as a `ComboBox`
  (`ControlInteractionAdapter.cs:42-54`). `Header`/`HeaderTemplate`/`Description` are documented
  exclusions with external-layout alternatives (`design library handoff/getting-started.md:247-268`).
  `PlaceholderText` is now a reconciled ✅, not stale (see below); `MaxDropDownHeight` is now
  resolved ✅ as well (see below) — `EtherDropdown.ApplyMaxVisibleHeight` composes it with
  `MaxVisibleItems` as an additional pixel ceiling on the popup (`EtherDropdown.cs`).
- **Design coverage (B):** `MaxVisibleItems` and `MenuGap` are public DPs
  (`EtherDropdown.cs:124-151`) with design defaults in the keyed style
  (`EtherDropdown.xaml:434-447`). Rich selection-box content and editable text are deliberately
  excluded and documented as text-only/select-only (`EtherDropdown.cs:36-40`).
- **Consumability (A):** declared DPs and the primary adapter path are covered
  (`RuntimeVerification.PropertyConsumption.cs:289-290,369`); the C1 data path now proves
  `SelectedItem` and `SelectedValue` two-way, VM-backed, in addition to `SelectedIndex`
  (`RuntimeVerification.R2.cs:98-116`), and C4 asserts the real ComboBox automation pattern —
  `ExpandCollapse` tracking `IsDropDownOpen` — instead of only peer/name
  (`RuntimeVerification.R2.cs:295-305`). Both are confirmed in the green runtime evidence
  (`twoWayBindings.Properties` includes `"EtherDropdown.SelectedItem"` and
  `"EtherDropdown.SelectedValue"`; `automationPatterns.Patterns` includes
  `"EtherDropdown.ExpandCollapse"`).

**Overall: consumer-ready.** `MaxDropDownHeight` is now resolved: `EtherDropdown.ApplyMaxVisibleHeight` was extended to compose it with `MaxVisibleItems` as an additional pixel ceiling on the popup's `ScrollViewer.MaxHeight`, and a dedicated fixture (`VerifyMaxDropDownHeightConstrainsPopup`, `RuntimeVerification.Dropdown.cs`, called from `VerifyDropdownAsync`) opens the popup with two distinct non-default heights and asserts each constrains it exactly. A plain XAML `{TemplateBinding MaxDropDownHeight}` was rejected as unsafe (see the resolution note below) in favor of this code-level composition. CLOSED, confirmed by a green `Verify-ConsumerFixtures.ps1` run — its marker (`artifacts/consumer-fixtures/runtime-result-9a2b552efe3b4c8bb6c9280635e54b77.json`, `outcome:"success"`) carries `dropdown.MaxDropDownHeightConstrainsPopup: true`, `MaxDropDownHeightSmallerCap: 80`, and `MaxDropDownHeightLargerCap: 400` — everything else Review A/B flagged is also closed. Review B now cleanly passes: `MaxDropDownHeight` is ✅.

---

## Review B — Completeness / Parity matrix

| Property / Event | Source | Expected (consumer) | Present? | Bindable/Observable? | Verdict |
| --- | --- | --- | --- | --- | --- |
| `ItemsSource` | base | bind the option list from a VM; count is data-driven | inherited; watched for collection swaps `EtherDropdown.cs:116-120` | ✔ DP | ✅ (fixture covers object source) |
| inline `ComboBoxItem`s | base | author options directly in XAML | ✔ Gallery authors nine `DropdownPage.xaml:44-59` | n/a | ✅ |
| `DisplayMemberPath` | base | select the model property displayed for each item | inherited; resolved for trigger text `EtherDropdown.cs:234-247` | ✔ DP | ✅ (fixture `RuntimeVerification.Dropdown.cs:147-165`) |
| `ItemTemplate` / `ItemTemplateSelector` | base | render rich items in the popup list | inherited; popup hosts `ItemsPresenter` `EtherDropdown.xaml:299-323` | ✔ DP | ✅ (unverified by fixture) |
| `SelectedIndex` | base | set/two-way current option by index | inherited; standard adapter fixture changes it `PropertyConsumption.cs:369` | ✔ DP | ✅ |
| `SelectedItem` | base | set/two-way the selected model/container | inherited; trigger reads it `EtherDropdown.cs:275-281`; VM-backed TwoWay round-trip now proven `RuntimeVerification.R2.cs:98-106` | ✔ DP | ✅ |
| `SelectedValue` / `SelectedValuePath` | base | bind a backend-safe model key | inherited; adapter emits both value and index `ControlInteractionAdapter.cs:47-54`; `SelectedValue` VM-backed TwoWay round-trip now proven `RuntimeVerification.R2.cs:108-116` | ✔ DP | ✅ (`SelectedValuePath` itself remains unverified by a fixture) |
| `SelectionChanged` | base | listen to selection → backend | inherited; Gallery handles it `DropdownPage.xaml:44-50` | event + `ObserveDropdown` `ControlInteractionAdapter.cs:42-54` | ✅ |
| `PlaceholderText` | base | prompt when no item is selected | inherited; callback + fallback `EtherDropdown.cs:112-114,269-281`; fixture now asserts the unselected-state render `RuntimeVerification.Dropdown.cs:63-70` | ✔ DP + callback | ✅ (reconciled; see note) |
| `IsDropDownOpen` / `DropDownOpened` / `DropDownClosed` | base | control and observe popup state | inherited; overrides call base `EtherDropdown.cs:284-304`; `IsDropDownOpen` now also asserted through UIA ExpandCollapse `RuntimeVerification.R2.cs:295-305` | ✔ DP + events | ✅ |
| `Header` / `HeaderTemplate` / `Description` | base | label and describe the field | inherited but template omits them `EtherDropdown.xaml:130-327` | base DPs ignored by template | 🚫 use external label/helper layout `getting-started.md:247-268` |
| `IsEditable` / `Text` / `TextSubmitted` | base | optionally accept typed values | inherited but explicitly unsupported; no `EditableText` part `EtherDropdown.cs:36-40` | DPs/events exist but template ignores them | 🚫 select-only alternative: `ItemsSource` + selection |
| selection-box templating | base | show rich selected content while closed | inherited API, intentionally replaced by text-only `TriggerText` `EtherDropdown.cs:26-40`; framework presenter collapsed `EtherDropdown.xaml:243-264` | base DPs exist but trigger ignores them | 🚫 use `DisplayMemberPath` / `ToString()` |
| `MaxDropDownHeight` | base | cap popup height | inherited; `ApplyMaxVisibleHeight` reads it and composes it with `MaxVisibleItems` as an additional pixel ceiling on the popup `ScrollViewer` (`EtherDropdown.cs`) — code-level consumption, not a `{TemplateBinding MaxDropDownHeight}` in the XAML (see the resolution note below for why) | ✔ DP; genuinely honored | ✅ **resolved and confirmed by the green run — `VerifyMaxDropDownHeightConstrainsPopup` asserts it, and `dropdown.MaxDropDownHeightConstrainsPopup:true` (with caps 80/400) confirms it held** |
| `MaxVisibleItems` | design | choose item count before scrolling | DP `EtherDropdown.cs:124-136`; style default 6 `EtherDropdown.xaml:445` | ✔ DP | ✅ |
| `MenuGap` | design | set trigger-to-popup spacing | DP `EtherDropdown.cs:139-151`; used to position popup `…Dropdown.cs:420-438` | ✔ DP | ✅ |
| `IsEnabled` | base | disable selection | inherited; `Disabled` VSM declared `EtherDropdown.cs:53-63` | ✔ DP | ✅ |

**Notes / gaps (B):**
- **The actual base is `ComboBox`.** The declaration is `public sealed class EtherDropdown : ComboBox`
  (`EtherDropdown.cs:64`), and the implementation intentionally preserves framework-owned
  `ContentPresenter`/drop-down state machinery (`EtherDropdown.cs:23-34`,
  `EtherDropdown.xaml:243-251`).
- **The compact select-only deviations are written down.** `IsEditable` and rich closed-state
  selection content are intentionally unsupported (`EtherDropdown.cs:36-40`), with
  `DisplayMemberPath`/`ToString()` as the trigger alternative (`EtherDropdown.cs:234-247`). Those
  rows are 🚫 rather than accidental gaps.
- **Header/Description is an intentional exclusion.** The custom template contains no presenter
  (`EtherDropdown.xaml:130-327`), while the consumer guide names external `TextBlock`/form layout as
  the alternative (`design library handoff/getting-started.md:247-268`).
- **The Placeholder contract is now reconciled, not stale.** Code watches `PlaceholderTextProperty`
  and displays it when no item is selected (`EtherDropdown.cs:112-114,269-281`); the registry entry
  was removed (it no longer lists `EtherDropdown.PlaceholderText` as unsupported,
  `scripts/UnsupportedProperties.psd1`), the consumer guide row was dropped
  (`design library handoff/getting-started.md`), and the fixture asserts the trigger text shows the
  placeholder while unselected and the selected item once one is chosen
  (`RuntimeVerification.Dropdown.cs:63-77`). `PlaceholderForeground` stays unbound and is now
  described accordingly (`scripts/UnsupportedProperties.psd1` PlaceholderForeground entry).
- **`MaxDropDownHeight` is now resolved.** `EtherDropdown.ApplyMaxVisibleHeight` (`EtherDropdown.cs`)
  now reads `MaxDropDownHeight` and composes it with `MaxVisibleItems` as an additional pixel
  ceiling: when `MaxVisibleItems <= 0` or the item count is at/under it, the popup `ScrollViewer`'s
  `MaxHeight` is set directly to `MaxDropDownHeight`; otherwise it is `Math.Min` of the
  `MaxVisibleItems`-derived height and `MaxDropDownHeight`. **Resolution path chosen: composed
  code-level consumption, not a XAML `{TemplateBinding MaxDropDownHeight}`.** A literal
  `TemplateBinding` on the `ScrollViewer`'s `MaxHeight` was considered and rejected: the same
  method already writes `ScrollViewer.MaxHeight` as a local value on every popup open/resize
  (`_menuScrollViewer.MaxHeight = maxHeight` in the pre-existing `MaxVisibleItems` logic, and
  unconditionally to `double.PositiveInfinity` when `MaxVisibleItems <= 0`) — a local value assignment
  always overrides and permanently clears a `TemplateBinding` expression, so a plain
  `TemplateBinding` would work exactly once and then silently go dead the next time the popup
  opened or resized. Composing the two constraints in the existing `ApplyMaxVisibleHeight` method
  (via the new private `SetMenuScrollViewerMaxHeight` helper) was the small, safe change that
  actually keeps working. `scripts/UnsupportedProperties.psd1`'s `AcknowledgedSilent` entry for
  `EtherDropdown.MaxDropDownHeight` was relabeled from `needs-review` to `behavioral` (not
  deleted): the generic closed-tree attached-visual-property probe that produces
  `Method == 'platform-dp-contract'` evidence screenshots the CLOSED trigger control and can never
  observe an effect that only manifests in the OPEN popup, so an accounting entry has to remain for
  `Verify-SilentPropertyCoverage.ps1`'s forward check — the same reason
  `EtherInput.AcceptsReturn`/`IsReadOnly` stay `behavioral` rather than disappearing once they
  became functional. A dedicated fixture (`VerifyMaxDropDownHeightConstrainsPopup`,
  `RuntimeVerification.Dropdown.cs`, called from `VerifyDropdownAsync`) is the actual proof: it
  opens an isolated probe's real popup (not just a `VisualStateManager` state) with
  `MaxVisibleItems = 0` and two distinct small `MaxDropDownHeight` values, and asserts the
  `ScrollViewer` is constrained to each value exactly. CLOSED, confirmed by a green
  `Verify-ConsumerFixtures.ps1` run: its marker
  (`artifacts/consumer-fixtures/runtime-result-9a2b552efe3b4c8bb6c9280635e54b77.json`,
  `outcome:"success"`) carries `dropdown.MaxDropDownHeightConstrainsPopup: true`,
  `MaxDropDownHeightSmallerCap: 80`, and `MaxDropDownHeightLargerCap: 400` — proving both distinct
  heights were opened and each constrained the popup to its expected cap. `EtherDropdown.md`'s
  matrix row above is ✅, and this is now a confirmed pass, not just an asserted one.

---

## Review A — Consumability report

```
Component: EtherDropdown
Base control: ComboBox   | Package: Ether.DesignSystem.Controls
Declared DPs: 2  (MaxVisibleItems, MenuGap)  | inherited public surface: full ComboBox/Selector

Rubric (declared DPs):
  R1 DependencyProperty ....... PASS  (public identifiers + CLR wrappers, EtherDropdown.cs:124-151)
  R2 Round-trips .............. PASS  (GetValue/SetValue wrappers, cs:132-151;
                                       fixture asserts both paths, PropertyConsumption.cs:62-97)
  R3 Observable ............... PASS  (fixture registers callbacks, PropertyConsumption.cs:74-91;
                                       type and samples included at cs:289-290)
  R4 TwoWay ................... PASS  (SelectedIndex exercised, PropertyConsumption.cs:369; VM-backed
                                       TwoWay SelectedItem and SelectedValue both proven,
                                       RuntimeVerification.R2.cs:98-116)
  R5 Documented ............... PASS  (identifier/wrapper docs now state the registered metadata
                                       default and the shipping style's effective default,
                                       EtherDropdown.cs:124-151)

Contracts:
  C1 ItemsSource / selection .. PASS  (ItemsSource + DisplayMemberPath + SelectedIndex fixture,
                                       RuntimeVerification.Dropdown.cs:147-165; SelectedItem and
                                       SelectedValue two-way round-trips now proven,
                                       RuntimeVerification.R2.cs:98-116; a consumer ItemTemplate
                                       still has no dedicated fixture)
  C2 Listenable + adapter ..... PASS  (SelectionChanged + ObserveDropdown,
                                       ControlInteractionAdapter.cs:42-54; exercised at
                                       PropertyConsumption.cs:369)
  C3 Command .................. N/A   (ComboBox selection is event/state based, not ICommand)
  C4 Automation peer/pattern .. PASS  (PatternInterface.ExpandCollapse/IExpandCollapseProvider
                                       asserted; Expand/Collapse tracks IsDropDownOpen in both
                                       directions, RuntimeVerification.R2.cs:295-305)

Harness: covered? YES. In PublicPropertyInventoryTypes (PublicPropertyInventory.cs:36),
         DeclaredPropertyOwnerTypes (PropertyConsumption.cs:270-281), the standard-interaction
         list (PropertyConsumption.cs:369), and RuntimeVerification.Dropdown.cs:18-21 +
         RuntimeVerification.R2.cs (TwoWay/ExpandCollapse). Runtime marker ETHER_CONSUMER_SMOKE
         outcome:"success" includes "EtherDropdown.SelectedItem"/"EtherDropdown.SelectedValue" in
         twoWayBindings.Properties and "EtherDropdown.ExpandCollapse" in
         automationPatterns.Patterns
         (artifacts/audit-runs/consumer-runtime-evidence-20260901-161450498/runtime-result.json).
         VerifyMaxDropDownHeightConstrainsPopup was added in a follow-up session to
         RuntimeVerification.Dropdown.cs, called from VerifyDropdownAsync (itself already part of
         VerifyAsync's call path); a later Verify-ConsumerFixtures.ps1 run confirmed it — its
         marker (artifacts/consumer-fixtures/runtime-result-9a2b552efe3b4c8bb6c9280635e54b77.json,
         outcome:"success") carries dropdown.MaxDropDownHeightConstrainsPopup:true,
         MaxDropDownHeightSmallerCap:80, MaxDropDownHeightLargerCap:400.
x64 build: green — confirmed by that same subsequent Verify-ConsumerFixtures.ps1 run, following
           the edit-only session that made one small product edit to
           EtherDropdown.ApplyMaxVisibleHeight (see the resolution note above) and the R2.11
           runtime run recorded in docs/internal/consumability/_RUN-ON-DESKTOP.md.
```

---

## Follow-ups (concrete, actionable)

1. ~~**[B, docs, harness] Reconcile the implemented Placeholder contract.**~~ DONE — removed from
   `scripts/UnsupportedProperties.psd1`, the consumer-guide row was dropped
   (`design library handoff/getting-started.md`), and `RuntimeVerification.Dropdown.cs:63-70` now asserts
   the unselected placeholder render.
2. ~~**[B, harness] Resolve `MaxDropDownHeight`.**~~ DONE. `ApplyMaxVisibleHeight`
   (`EtherDropdown.cs`) composes `MaxDropDownHeight` with `MaxVisibleItems` as an additional
   pixel ceiling on the popup `ScrollViewer` (a plain `{TemplateBinding MaxDropDownHeight}` was
   rejected — see the resolution note above for why it would silently stop working). A
   constrained-height popup probe was added to `RuntimeVerification.Dropdown.cs`
   (`VerifyMaxDropDownHeightConstrainsPopup`, called from `VerifyDropdownAsync`) comparing two
   non-default `MaxDropDownHeight` values. `scripts/UnsupportedProperties.psd1`'s
   `EtherDropdown.MaxDropDownHeight` entry was relabeled from `needs-review` to `behavioral` (kept,
   not deleted — the closed-tree visual-diff probe that produces its `platform-dp-contract`
   evidence can never observe an open-popup-only effect). CLOSED, confirmed by a green
   `Verify-ConsumerFixtures.ps1` run (`dropdown.MaxDropDownHeightConstrainsPopup: true`,
   `MaxDropDownHeightSmallerCap: 80`, `MaxDropDownHeightLargerCap: 400` in
   `artifacts/consumer-fixtures/runtime-result-9a2b552efe3b4c8bb6c9280635e54b77.json`).
3. ~~**[C1, R4, harness] Prove the complete data/selection path.**~~ DONE for `SelectedItem`/
   `SelectedValue` — `RuntimeVerification.R2.cs:98-116` proves both round-trip VM-backed in both
   directions. A consumer `ItemTemplate` and `SelectedValuePath` still have no dedicated fixture
   (minor residual, not a B failure).
4. ~~**[C4, harness] Assert the inherited ComboBox automation contract.**~~ DONE —
   `RuntimeVerification.R2.cs:295-305` requires `PatternInterface.ExpandCollapse`, casts to
   `IExpandCollapseProvider`, and asserts expand/collapse tracks `IsDropDownOpen` in both
   directions.
5. ~~**[R5] Document effective declared-property defaults.**~~ DONE — both identifier and wrapper
   XML docs in `EtherDropdown.cs:124-151` now distinguish the metadata default (`0`, `0d`) from the
   shipping style default (`6`, `Spacing4`).

Remaining: none. Item 2's fixture is wired into `VerifyDropdownAsync` (already part of
`VerifyAsync`'s call path) and has been exercised by an actual `Verify-ConsumerFixtures.ps1` run,
which reached `outcome:"success"` — closed and verified.
