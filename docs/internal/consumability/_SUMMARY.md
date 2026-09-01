# Consumability review — Final summary

> **Scope:** all 15 reviewed Ether components/resources. **Date:** 2026-09-01 (post-remediation, R3).
> Verdicts use the gates in
> [the review spec](../2026-09-01-component-consumability-review-spec.md); detailed evidence and
> actionable work are in the linked component reports. This rollup reflects the state **after**
> Waves R1 (product API), R2 (harness expansion), and the R2.11 runtime run reached
> `{"marker":"ETHER_CONSUMER_SMOKE","outcome":"success", ...}` in
> `artifacts/audit-runs/consumer-runtime-evidence-20260901-161450498/runtime-result.json` — see
> [`_RUN-ON-DESKTOP.md`](_RUN-ON-DESKTOP.md) for how that run was produced. The remediation plan is
> [`../2026-09-01-consumability-remediation-plan.md`](../2026-09-01-consumability-remediation-plan.md).
>
> **Post-R2.11 update (same date):** a follow-up, edit-only session closed the four named residuals
> below (Dropdown `MaxDropDownHeight`, ScrollBar RangeValue/Minimum-Maximum/docs, RadioButton
> `GroupName` mutual exclusion, Masthead per-caption Invoke) by adding fixture assertions — and, for
> Dropdown, one small product edit — all wired into stages `RuntimeVerification.VerifyAsync` already
> calls. That session did not build or run the harness, so the R2.11 evidence file above did not yet
> reflect these additions.
>
> **Confirmation run (same date, later):** `scripts/Verify-ConsumerFixtures.ps1` was run and reached
> `{"marker":"ETHER_CONSUMER_SMOKE","outcome":"success", ...}`, producing
> `artifacts/consumer-fixtures/runtime-result-9a2b552efe3b4c8bb6c9280635e54b77.json`. That marker
> contains the fields proving all four new assertions actually executed and held:
> `radioButton.GroupNameMutualExclusionVerified: true`;
> `masthead.MinimizeButtonInvokeExposed: true` + `masthead.CloseButtonInvokeExposed: true`;
> `dropdown.MaxDropDownHeightConstrainsPopup: true` with `MaxDropDownHeightSmallerCap: 80` and
> `MaxDropDownHeightLargerCap: 400`; and `scrollBar.RangeValuePatternExposed: true` +
> `scrollBar.RangeValueTracksValue: true` + `scrollBar.MinimumRoundTrip: 5` +
> `scrollBar.MaximumRoundTrip: 120`. All four rows in the "Rollup" table below are now **CLOSED and
> confirmed by a green run**, not merely asserted. The only residual left anywhere in this rollup is
> the packaged MSIX interactive runtime, which is an environment constraint (see
> [`_RUN-ON-DESKTOP.md`](_RUN-ON-DESKTOP.md)), not a code or fixture gap.

## Rollup

| Component | Base type | B verdict | A verdict | harness-covered? | residual follow-ups |
| --- | --- | --- | --- | --- | --- |
| [EtherButton](EtherButton.md) | `Button` (`EtherButton.cs:38`) | ✅ PASS (`EtherButton.md:13-26`) | ✅ PASS (`EtherButton.md:59-99`) | YES (`EtherButton.md:90-96`) | 0 |
| [EtherCard](EtherCard.md) | `Border` (six keyed Styles; no Ether type) (`EtherCard.xaml:95-149`) | ✅ PASS (`EtherCard.md:12-24`) | ✅ PASS (`EtherCard.md:59-95`) | YES — style-resource fixture (`EtherCard.md:80-90`) | 0 |
| [EtherCheckbox](EtherCheckbox.md) | `CheckBox` (`EtherCheckbox.cs:30`) | 🚫 D4 documented (`EtherCheckbox.md:13-31`) | ✅ PASS (`EtherCheckbox.md:65-107`) | YES (`EtherCheckbox.md:95-103`) | 0 |
| [EtherDropdown](EtherDropdown.md) | `ComboBox` (`EtherDropdown.cs:64`) | ✅ PASS (`EtherDropdown.md:13-35`, `MaxDropDownHeight` resolved and confirmed) | ✅ PASS (`EtherDropdown.md:88-131`) | YES — confirmed by the green run (`dropdown.MaxDropDownHeightConstrainsPopup:true`, `runtime-result-9a2b552efe3b4c8bb6c9280635e54b77.json`) | 0 — CLOSED, confirmed by the green run |
| [EtherInput](EtherInput.md) | `TextBox` (`EtherInput.cs:28`) | ✅ PASS (`EtherInput.md:13-30`) | ✅ PASS (`EtherInput.md:67-110`) | YES (`EtherInput.md:99-107`) | 0 |
| [EtherIntelligenceButton](EtherIntelligenceButton.md) | `Button` (`EtherIntelligenceButton.cs:33`) | ✅ PASS (`EtherIntelligenceButton.md:13-27`) | ✅ PASS (`EtherIntelligenceButton.md:58-95`) | YES (`EtherIntelligenceButton.md:83-90`) | 0 |
| [EtherMasthead](EtherMasthead.md) | `Control` (`EtherMasthead.xaml.cs:57`) | 🚫 D2 documented (`EtherMasthead.md:14-37`) | ✅ PASS (`EtherMasthead.md:77-125`, per-caption `Invoke` confirmed for Minimize/Close) | YES — confirmed by the green run (`masthead.MinimizeButtonInvokeExposed:true`, `masthead.CloseButtonInvokeExposed:true`, `runtime-result-9a2b552efe3b4c8bb6c9280635e54b77.json`) | 0 — CLOSED, confirmed by the green run |
| [EtherProgressBar](EtherProgressBar.md) | `RangeBase` (`EtherProgressBar.cs:34`) | ✅ PASS (`EtherProgressBar.md:13-25`) | ✅ PASS (`EtherProgressBar.md:59-97`) | YES (`EtherProgressBar.md:92-96`) | 0 |
| [EtherRadioButton](EtherRadioButton.md) | `RadioButton` (`EtherRadioButton.cs:30`) | 🚫 D4 documented (`EtherRadioButton.md:13-29`) | ✅ PASS (`EtherRadioButton.md:64-108`, `GroupName` mutual exclusion confirmed) | YES — confirmed by the green run (`radioButton.GroupNameMutualExclusionVerified:true`, `runtime-result-9a2b552efe3b4c8bb6c9280635e54b77.json`) | 0 — CLOSED, confirmed by the green run |
| [EtherScrollBar](EtherScrollBar.md) | `ScrollBar` (implicit Style; no Ether type) (`EtherScrollBar.xaml:106-205`) | ✅ PASS (`EtherScrollBar.md:13-29`) | ✅ PASS (`EtherScrollBar.md:65-110`, RangeValue automation + `Minimum`/`Maximum` round-trip confirmed) | YES — confirmed by the green run (`scrollBar.RangeValuePatternExposed:true`, `scrollBar.RangeValueTracksValue:true`, `scrollBar.MinimumRoundTrip:5`, `scrollBar.MaximumRoundTrip:120`, `runtime-result-9a2b552efe3b4c8bb6c9280635e54b77.json`) | 0 — CLOSED, confirmed by the green run |
| [EtherSegmentedControl](EtherSegmentedControl.md) | `ContentControl` + `Panel`/`RadioButton` (`EtherSegmentedControl.cs:28`; `EtherSegmentPanel.cs:15`; `EtherSegmentRadioButton.cs:8`) | ✅ PASS — D1 additive contract shipped (`EtherSegmentedControl.md:17-41`) | ✅ PASS (`EtherSegmentedControl.md:83-137`) | YES (`EtherSegmentedControl.md:126-136`) | 0 |
| [EtherSlider](EtherSlider.md) | `RangeBase` (`EtherSlider.xaml.cs:45`) | ✅ PASS — `StepFrequency` closed (`EtherSlider.md:15-31`) | ✅ PASS (`EtherSlider.md:69-112`) | YES (`EtherSlider.md:104-112`) | 0 |
| [EtherSteeringBar](EtherSteeringBar.md) | `Control` (`EtherSteeringBar.xaml.cs:78`) | ✅ PASS — `StepFrequency` closed (`EtherSteeringBar.md:13-27`) | ✅ PASS (`EtherSteeringBar.md:68-111`) | YES (`EtherSteeringBar.md:103-109`) | 0 |
| [EtherSwitch](EtherSwitch.md) | `ToggleSwitch` (keyed Style; no Ether type) (`EtherSwitch.xaml:212-223`) | ✅ PASS (`EtherSwitch.md:13-34`) | ✅ PASS (`EtherSwitch.md:72-117`) | YES (`EtherSwitch.md:106-116`) | 0 |
| [EtherTabNavigation](EtherTabNavigation.md) | `ListView` + `ListViewItem` (`EtherTabNavigation.cs:9,21`) | 🚫 D3 documented (`EtherTabNavigation.md:15-35`) | ✅ PASS (`EtherTabNavigation.md:70-116`) | YES (`EtherTabNavigation.md:105-115`) | 0 |

**All fifteen components now have zero *named* residual follow-ups** — a follow-up session added
fixture assertions (and, for Dropdown, one small product edit) closing the four residuals the prior
wave left open on Dropdown, Masthead, RadioButton, and ScrollBar, and a subsequent
`Verify-ConsumerFixtures.ps1` run confirmed all four hold at runtime (see the "Confirmation run" note
above). Four components (Checkbox, RadioButton, Masthead, TabNavigation) still carry documented 🚫
decisions (D2/D3/D4) that count as green per the spec's pass/fail gate (§3.4: "no ❌ ... except rows
explicitly marked 🚫 with a written justification").

**15/15 components pass both reviews.** The sole remaining residual anywhere in this rollup is the
packaged MSIX interactive runtime — an environment constraint (sideload-capable machine required),
not a code or fixture gap; see [`_RUN-ON-DESKTOP.md`](_RUN-ON-DESKTOP.md).

## Closed / residual

Each item below is a gap the pre-remediation `_SUMMARY.md` flagged as cross-cutting. This section
records how — or whether — this remediation wave closed it.

- **Inventory type-only and incomplete for the shipped surface — CLOSED.**
  `PublicPropertyInventoryTypes` grew from 12 to 15 Ether types, adding `EtherSegmentRadioButton`,
  `EtherTabNavigation`, and `EtherTabItem`
  (`tests/Ether.DesignSystem.ConsumerFixtures/RuntimeVerification.PublicPropertyInventory.cs:32-48`).
  The keyed Switch Style, implicit ScrollBar Style, and six Card Styles still correctly have **no**
  Ether type to add — that gap is closed differently, by a dedicated style-resource fixture
  (`RuntimeVerification.StyleResources.cs`) that resolves and exercises all three instead.
- **TwoWay consumer binding was not proven for editable state — CLOSED for 12 properties.**
  `RuntimeVerification.R2.cs`'s `VerifyTwoWayBindings` VM-binds and round-trips in both directions:
  `EtherCheckbox.IsChecked`, `EtherRadioButton.IsChecked`, `EtherInput.Text`,
  `EtherDropdown.SelectedItem`, `EtherDropdown.SelectedValue`, `EtherSegmentedControl.SelectedValue`,
  `EtherSegmentedControl.SelectedIndex`, `EtherSteeringBar.Value`, `EtherSwitch.IsOn`,
  `EtherScrollBar.Value`, `EtherTabNavigation.SelectedIndex`, `EtherTabNavigation.SelectedItem`
  (`R2.cs:57-194`). Confirmed in the green run: all 12 appear in `twoWayBindings.Properties`.
- **Most interactive controls lacked pattern-level automation proof — CLOSED for 11 of 11.**
  `RuntimeVerification.R2.cs`'s `VerifyAutomationPatterns` asserts: Invoke (Button,
  IntelligenceButton), Toggle (Checkbox, Switch), SelectionItem (RadioButton + each
  `EtherSegmentRadioButton`, correctly **not** Toggle), the honest Edit/focusable/enabled contract
  for Input (TextBox's editable-text pattern is native/out-of-process, not a product gap), ExpandCollapse
  (Dropdown), and Selection (TabNavigation) (`R2.cs:249-361`). SegmentedControl's host is confirmed
  to correctly expose **no** Selection provider (`R2.cs:339-341`). **`ScrollBar`'s RangeValue
  automation pattern is now also asserted and confirmed** by `VerifyScrollBarRangeValueAutomation`
  (`RuntimeVerification.ScrollBar.cs`, called from `VerifyScrollBarAsync`) — the green run's
  `scrollBar.RangeValuePatternExposed: true` / `RangeValueTracksValue: true` fields prove it held.
  Slider, SteeringBar, and ProgressBar keep their pre-existing asserted
  RangeValue contracts unchanged (`RuntimeVerification.Slider.cs:104-135`;
  `RuntimeVerification.SteeringBar.cs:81-149`; `RuntimeVerification.ProgressBar.cs:65-138`).
- **Inherited command behavior was unproved on the button/toggle family — CLOSED.**
  `RuntimeVerification.R2.cs`'s `VerifyCommands` assigns `Command`/`CommandParameter` and actuates
  through each control's real UIA pattern — Invoke for Button/IntelligenceButton, Toggle for
  Checkbox, SelectionItem.Select() for RadioButton (not Toggle) — asserting exactly one execution
  with the expected parameter (`R2.cs:374-431`). Confirmed in the green run:
  `commands.Controls` lists all four.
- **Strict R5 default documentation was repeatedly incomplete — CLOSED for all seven named
  components.** Button, Dropdown, Slider, SegmentedControl, SteeringBar, Masthead, and ProgressBar
  now state each registered metadata default (and, where the shipping style overrides it, that
  effective default too) on both the `…Property` identifier and the CLR wrapper — see each
  component's Review A `R5` line.
- **Collection/data-driven contracts needed a common proof standard — CLOSED.** SegmentedControl
  gained the additive D1 items-control contract (`ItemsSource`/`ItemTemplate`/
  `DisplayMemberPath`/`SelectedIndex`/`SelectedItem`,
  `src/Ether.DesignSystem.Controls/Controls/Inputs/EtherSegmentedControl.cs:37-126`), Dropdown's
  `SelectedItem`/`SelectedValue` TwoWay round-trips are proven, and Tab Navigation's
  `ItemsSource`+selection data path is proven (`RuntimeVerification.R2.cs:196-236`).
- **Two interactive controls lacked an adapter path — CLOSED.** Tab Navigation:
  `ControlInteractionAdapter.ObserveSelection(Selector, …)` was added
  (`src/Ether.DesignSystem.Interactions/ControlInteractionAdapter.cs:56-69`). Masthead: a public
  `ActionInvoked` event (+ `MastheadAction` enum + public args,
  `src/Ether.DesignSystem.Controls/Controls/Navigation/MastheadAction.cs`) and
  `ControlInteractionAdapter.ObserveMasthead(EtherMasthead, …)` were added
  (`ControlInteractionAdapter.cs:115-126`). Both are exercised end to end in
  `VerifyStandardInteractionAdapters` (`RuntimeVerification.PropertyConsumption.cs:382-405`).
- **Several live consumer contracts were stale or unresolved — CLOSED, all four.**
  Checkbox/RadioButton (D4): the two-state coercion (`IsChecked=null ⇒ false`,
  `IsThreeState=true ⇒ false`) is now asserted directly by the fixtures instead of expecting a
  removed `NotSupportedException` (`RuntimeVerification.Checkbox.cs:67,85-94`;
  `RuntimeVerification.RadioButton.cs:67,85-94`), and the registry/consumer-guide description was
  updated to match. Switch: the fixture's named template parts were reconciled with the current
  template — `OuterBorder`/`OnTrackBacking`/`KnobFrame`/`SwitchAreaGrid`
  (`RuntimeVerification.ToggleSwitch.cs:57-90`). Dropdown: `PlaceholderText` was removed from
  `scripts/UnsupportedProperties.psd1`'s unsupported registry, dropped from the generated table in
  `docs/consumers/getting-started.md`, and the fixture now asserts the unselected-state render
  (`RuntimeVerification.Dropdown.cs:63-70`). Input: `HorizontalTextAlignment` is now
  template-bound (the placeholder's `TextAlignment` binds to it,
  `src/Ether.DesignSystem.Controls/Controls/Inputs/EtherInput.xaml:151`) and was removed from
  `needs-review`.
- **The B failures clustered around missing consumer-facing knobs — CLOSED, all four.** Slider and
  SteeringBar both gained a uniform `StepFrequency` DP with step-relative snapping
  (`EtherSlider.xaml.cs:159-164,227-231,801-813`; `EtherSteeringBar.xaml.cs:163-166,250-254,468-474`).
  SegmentedControl gained the D1 data-driven selector surface (above). Masthead gained the D2
  consumer action contract (above). Tab Navigation's string-only `Icon` shape (D3) was confirmed
  intentional and documented 🚫 — no code change, matching the locked decision.

### CLOSED — formerly named residuals

All four of the previous wave's named residuals were addressed in a follow-up edit-only session and
have since been **confirmed by a green `Verify-ConsumerFixtures.ps1` run**
(`{"marker":"ETHER_CONSUMER_SMOKE","outcome":"success", ...}`,
`artifacts/consumer-fixtures/runtime-result-9a2b552efe3b4c8bb6c9280635e54b77.json`):

- **`EtherDropdown.MaxDropDownHeight`** — CLOSED, confirmed. `EtherDropdown.ApplyMaxVisibleHeight`
  (`EtherDropdown.cs`) composes it with `MaxVisibleItems` as an additional pixel ceiling on the
  popup `ScrollViewer`; a plain `{TemplateBinding MaxDropDownHeight}` was rejected as unsafe (the
  same method already writes `ScrollViewer.MaxHeight` as a local value on every open/resize, which
  would silently clobber a binding — see `EtherDropdown.md`'s resolution note). The dedicated
  fixture, `VerifyMaxDropDownHeightConstrainsPopup` (`RuntimeVerification.Dropdown.cs`, called from
  `VerifyDropdownAsync`), opens the popup with two distinct non-default heights and asserts each
  constrains it exactly. `scripts/UnsupportedProperties.psd1`'s entry was relabeled from
  `needs-review` to `behavioral` (kept, not deleted, since its closed-tree visual-diff probe can
  never observe an open-popup-only effect). **Confirmed by the green run:** the marker's `dropdown`
  record carries `MaxDropDownHeightConstrainsPopup: true`, `MaxDropDownHeightSmallerCap: 80`, and
  `MaxDropDownHeightLargerCap: 400` — proving both non-default heights were actually opened and
  each one constrained the popup to the expected cap.
- **`EtherScrollBar`** — CLOSED, confirmed. All three items addressed: (1)
  `VerifyScrollBarRangeValueAutomation` (`RuntimeVerification.ScrollBar.cs`) requires
  `PatternInterface.RangeValue` to resolve to `IRangeValueProvider` and asserts `Value` tracks the
  control; (2) `VerifyScrollBarMinimumMaximumRoundTrip` asserts `Minimum`/`Maximum` round-trip
  through `GetValue` and the CLR wrapper; (3) a persistent-only-mode callout was added next to the
  usage sample in `docs/consumers/getting-started.md`. All wired into `VerifyScrollBarAsync`.
  **Confirmed by the green run:** the marker's `scrollBar` record carries
  `RangeValuePatternExposed: true`, `RangeValueTracksValue: true`, `MinimumRoundTrip: 5`, and
  `MaximumRoundTrip: 120`.
- **`EtherRadioButton` `GroupName` mutual exclusion** — CLOSED, confirmed.
  `VerifyRadioButtonGroupNameMutualExclusionAsync` (`RuntimeVerification.RadioButton.cs`, called
  from `VerifyRadioButtonAsync`) hosts two same-`GroupName` `EtherRadioButton` instances under a
  shared scratch-Canvas parent, checks one, then checks the other, and asserts the first
  auto-unchecks. **Confirmed by the green run:** the marker's `radioButton` record carries
  `GroupNameMutualExclusionVerified: true`.
- **`EtherMasthead` per-caption-button automation** — CLOSED, confirmed.
  `VerifyMastheadCaptionButtonInvokePatterns` (`RuntimeVerification.Masthead.cs`, called from
  `VerifyMastheadAsync`) locates `MinimizeButton` and `CloseButton` and individually asserts each
  resolves `PatternInterface.Invoke` to `IInvokeProvider`, deliberately without invoking either
  (invoking would minimize/close the fixture's real host window mid-run). Combined with the
  pre-existing `MaximizeRestoreButton` proof (which does click it, via the C2 adapter test), all
  three caption buttons now have individually asserted Invoke coverage. **Confirmed by the green
  run:** the marker's `masthead` record carries `MinimizeButtonInvokeExposed: true` and
  `CloseButtonInvokeExposed: true`.

Against the spec's pass/fail gates: Dropdown, Masthead, RadioButton, and ScrollBar all now have
assertions in place for what were previously their only open items — Dropdown additionally required
(and got) one small product edit, the other three were harness-only — **and every one of those
assertions has now been exercised by an actual `Verify-ConsumerFixtures.ps1` run and held**, per the
marker fields cited above. These four rows are reconfirmed green, not merely asserted.

### Residual — the only one remaining

- **Packaged MSIX runtime on a user machine.** Per [`_RUN-ON-DESKTOP.md`](_RUN-ON-DESKTOP.md), the
  **unpackaged** fixture run is the substantiated green
  (`{"marker":"ETHER_CONSUMER_SMOKE","outcome":"success", ...}`, confirmed again by
  `runtime-result-9a2b552efe3b4c8bb6c9280635e54b77.json`); the packaged fixture additionally requires
  MSIX install/launch on a machine that allows sideloading. This is an environment constraint, not a
  code or fixture gap, and remains the sole residual across all 15 components.
