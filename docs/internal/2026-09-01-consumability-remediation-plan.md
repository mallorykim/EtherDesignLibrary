# Consumability remediation plan — drive every component to green

> **Goal:** every one of the 15 components passes **both** Review A (consumability) and Review B
> (completeness) per the gates in
> [the review spec](2026-09-01-component-consumability-review-spec.md). "Green" = (1) the change is
> made in code/contract, (2) a `ConsumerFixtures` assertion **proves** it, and (3) the harness builds
> and runs green on **x64**. A row may stay red-free as a documented **🚫** (intentional exclusion with
> a written reason + the consumer's alternative) — that counts as green per the spec gate.
>
> **Inputs:** the 15 per-component reports + rollup in [`consumability/`](consumability/_SUMMARY.md).
> Every fix below cites the report/source that motivates it; re-verify against live source before editing.
>
> **Execution:** supervised, in waves (R1→R3). Coding is done by Codex; another agent reviews between
> waves. **Do not fake green** — if a fixture cannot execute in this environment, say so and flag the
> runtime portion for the user's machine; never mark a row ✅ without its proof.

## Locked decisions (do not re-litigate)
- **D1 — SegmentedControl:** make it functionally complete against the official single-select
  items-control contract (WinUI `RadioButtons`/`Selector`): **add `ItemsSource`, `ItemTemplate`/
  `DisplayMemberPath`, `SelectedIndex`, `SelectedItem`** **additively** on top of the existing inline
  `EtherSegmentPanel`/`EtherSegmentRadioButton` + `SelectedValue`/`SelectionChanged` architecture
  (`EtherSegmentedControl.cs:23-173`). Keep the inline path fully working (like `ListView` supports
  both inline items and `ItemsSource`). Do **not** re-derive the class from `Selector`/`RadioButtons`
  — extend the current `ContentControl`.
- **D2 — Masthead:** add a public `ActionInvoked` event (+ `MastheadAction` enum + public args) for
  the window caption actions (minimize / maximize / restore / close), raised from the caption handlers
  **before** the host command; add `ObserveMasthead` to the adapter. The menu/search/settings/chevron
  icon slots **stay decorative** — mark that row 🚫 with the reason "decorative affordances; place an
  interactive control beside the masthead for actions."
- **D3 — Tab Navigation `Icon`:** string-only, resolved against the internal Ether icon library, is
  **intentional**. Mark the row 🚫 ("on-brand icon-library names only; arbitrary `IconElement` is out
  of contract by design") — no code change.
- **D4 — Checkbox / Radio three-state:** two-state is **intended** and deliberately implemented
  (`EtherCheckbox.cs:75-105` coerces null→false and `IsThreeState`→false, with a documented rationale;
  `EtherRadioButton` mirrors it). This is a **stale-fixture/registry** problem, not a code bug — fix
  the tests/registry/docs to assert the coercion, **do not** change the control behavior.

---

## Wave R1 — Product API (public-surface changes) + backend adapter
*Highest risk; changes the shipped API. Build `Ether.DesignSystem.Controls` + `Ether.DesignSystem.Interactions` after. Reviewer checks the public surface before R2.*

- [ ] **R1.1 — Slider `StepFrequency`** *(closes `EtherSlider.md` B ❌)*. Add a public
  `StepFrequencyProperty` DP (`double`, default `1.0`, XML-doc'd) to `EtherSlider`
  (`Inputs/EtherSlider.xaml.cs`). In `NormalizeInputValue` (`…xaml.cs:778-789`) replace the
  unconditional `Math.Round(clamped)` with step-relative snapping:
  `Minimum + Math.Round((clamped - Minimum) / step) * step` where `step = StepFrequency > 0 ?
  StepFrequency : (no rounding)`; keep `Stops`/`SnapToStops` taking precedence when snapping is on.
  Match WinUI `Slider.StepFrequency` semantics.
- [ ] **R1.2 — Steering Bar `StepFrequency`** *(closes `EtherSteeringBar.md` B ❌)*. Same DP + same
  step-relative snapping in `EtherSteeringBar`'s value-normalization path
  (`Inputs/EtherSteeringBar.xaml.cs`; locate its clamp/round method). Keep `SmallChange`/`LargeChange`/
  `Stops`/`SnapToStops` behavior intact.
- [ ] **R1.3 — SegmentedControl items-control contract (D1)** *(closes `EtherSegmentedControl.md` B ❌)*.
  In `EtherSegmentedControl.cs`: add `ItemsSource` (`object`/`IEnumerable`) + `ItemTemplate`
  (`DataTemplate`) + `DisplayMemberPath` + `SelectedIndex` (`int`) + `SelectedItem` (`object`) DPs.
  When `ItemsSource` is set, generate one `EtherSegmentRadioButton` per item into the
  `EtherSegmentPanel` (Content = item via `ItemTemplate`/`DisplayMemberPath`; `Tag` = item so
  `SelectedValue` returns the item), and keep `SelectedIndex`/`SelectedItem`/`SelectedValue`/
  `SelectionChanged` mutually consistent through the existing `_synchronizingSelection` guard
  (`…cs:114-169`). Inline segments keep working when `ItemsSource` is null.
- [ ] **R1.4 — Masthead action event (D2)** *(closes `EtherMasthead.md` B ❌ + underpins C2)*. Add a
  public `enum MastheadAction { Minimize, MaximizeRestore, Close }`, `MastheadActionInvokedEventArgs`
  (public, carrying the action), and `event EventHandler<MastheadActionInvokedEventArgs> ActionInvoked`
  on `EtherMasthead` (`Navigation/EtherMasthead.xaml.cs`). Raise it from each caption handler
  (`…xaml.cs:531-594`) **before** invoking the host-window command, gated by the existing
  `EnableWindowCommands`. Document the decorative icon-slot 🚫 in the class remarks.
- [ ] **R1.5 — Input `HorizontalTextAlignment`** *(closes `EtherInput.md` stale/needs-review)*. In the
  `EtherInput` template bind the inner text presenter's alignment to `HorizontalTextAlignment`/
  `TextAlignment` so it actually takes effect; if the template genuinely cannot honor it, instead mark
  it 🚫 with the reason. Then remove it from `scripts/UnsupportedProperties.psd1:315` (or move to
  supported) accordingly.
- [ ] **R1.6 — Adapter: `ObserveSelection` + `ObserveMasthead`** *(closes `EtherTabNavigation.md` C2
  and `EtherMasthead.md` C2)*. In `src/Ether.DesignSystem.Interactions/ControlInteractionAdapter.cs`
  add `ObserveSelection(Selector control, …)` (emits `{ SelectedIndex, SelectedValue }` on
  `SelectionChanged`; accepts `ListView`/`EtherTabNavigation` and any `Selector`) and
  `ObserveMasthead(EtherMasthead control, …)` (emits the `MastheadAction` on `ActionInvoked`), mirroring
  the existing typed observers (`…cs:41-118`). Validate subscription args eagerly like the others.
- [ ] **R1.7 — Build check.** `dotnet build` the Controls + Interactions projects on **x64**, 0 errors.
  Report the public API added (new DPs/events/enums) for reviewer sign-off.

## Wave R2 — Stale-contract reconciliation + harness expansion + run green
*Mostly test/doc/registry; extend `ConsumerFixtures` to PROVE every rubric/contract, then build + run.*

**Stale contracts (make code ⇄ tests ⇄ registry ⇄ docs consistent):**
- [ ] **R2.1 — Checkbox/Radio two-state (D4).** Update `RuntimeVerification.Checkbox.cs:80-104` and
  `RuntimeVerification.RadioButton.cs:80-104` to assert the coercion (set `IsChecked=null` ⇒ ends
  `false`; set `IsThreeState=true` ⇒ coerced `false`) instead of expecting an exception. Reconcile
  `scripts/UnsupportedProperties.psd1` + `design library handoff/getting-started.md` to describe two-state.
- [ ] **R2.2 — Switch fixture parts.** Update `RuntimeVerification.ToggleSwitch.cs:40,53-63` to the
  template part names actually in `EtherSwitch.xaml` (verify against `…xaml:104-223`).
- [ ] **R2.3 — Dropdown PlaceholderText.** Remove it from `scripts/UnsupportedProperties.psd1:180-187`,
  regenerate the `design library handoff/getting-started.md` row, add an unselected-`PlaceholderText` assertion
  to `RuntimeVerification.Dropdown.cs`.

**Inventory + declared-DP coverage:**
- [ ] **R2.4 — Inventory completeness.** Add `EtherTabNavigation`, `EtherTabItem`,
  `EtherSegmentRadioButton` to `PublicPropertyInventoryTypes`
  (`RuntimeVerification.PublicPropertyInventory.cs:32-46`). Add `EtherTabItem` and the new
  SegmentedControl/Slider/SteeringBar DP owners to `DeclaredPropertyOwnerTypes`
  (`RuntimeVerification.PropertyConsumption.cs:247-257`) with runtime samples in `CreatePropertySample`.
- [ ] **R2.5 — Style-resource fixtures** (the three style-only "controls"). Add fixtures that resolve
  the keyed/implicit styles and assert their target type + key template parts + Light/Dark/HC brush
  re-resolution: **Card** (six `Border` styles, `EtherCard.xaml:95-149`), **Switch** (keyed
  `EtherSwitch` over `ToggleSwitch`, `EtherSwitch.xaml:212`), **ScrollBar** (implicit `ScrollBar` style,
  `EtherScrollBar.xaml:106`).

**Prove the rubric/contracts (turn A "unverified/FAIL" → PASS):**
- [ ] **R2.6 — TwoWay (R4).** VM-backed two-way round-trip fixtures for: `EtherCheckbox.IsChecked`,
  `EtherRadioButton.IsChecked`, `EtherInput.Text`, `EtherDropdown.SelectedItem`/`SelectedValue`,
  `EtherSegmentedControl.SelectedValue`/`SelectedIndex`, `EtherSteeringBar.Value`, `EtherSwitch.IsOn`,
  `EtherScrollBar.Value`, `EtherTabNavigation.SelectedIndex`/`SelectedItem`.
- [ ] **R2.7 — Automation patterns (C4).** Assert the real UIA pattern (not just peer/name). **Use
  the pattern each WinUI base peer actually implements — do not assume Toggle for everything:**
  - **Invoke** — `EtherButton`, `EtherIntelligenceButton` (ButtonAutomationPeer → `IInvokeProvider`).
  - **Toggle** — `EtherCheckbox`, `EtherSwitch` (CheckBox/ToggleSwitch peers → `IToggleProvider`).
  - **SelectionItem** — `EtherRadioButton` and each `EtherSegmentRadioButton`
    (RadioButtonAutomationPeer → `ISelectionItemProvider`, **NOT** Toggle — this was the R2 runtime
    failure). Assert `IsSelected` reflects `IsChecked`.
  - **`EtherInput` — control-type + focusability, NOT a managed text/value provider.** WinUI's
    `TextBox` surfaces its editable-text UIA pattern **natively**: `TextBoxAutomationPeer.GetPattern`
    returns neither `IValueProvider` (`PatternInterface.Value`) **nor** a managed `ITextProvider`
    (`PatternInterface.Text`) in-process — both return null (empirically confirmed twice in the R2
    reruns; the Text pattern is provided to out-of-process UIA clients like Narrator natively). This is
    **not** an EtherInput gap (it is a bare `TextBox` with no custom peer). The honest in-process C4
    proof: assert the peer's `GetAutomationControlType() == AutomationControlType.Edit`,
    `IsKeyboardFocusable()`, and `IsEnabled()`; add a code comment that the editable Text pattern is
    native/out-of-process. Do NOT add a product automation peer, and do NOT keep the Value/Text
    `GetPattern` assertions.
  - **ExpandCollapse** tracking `IsDropDownOpen` — `EtherDropdown`.
  - **Selection** — `EtherTabNavigation` (ListView peer → `ISelectionProvider`).
  - **`EtherSegmentedControl`** is a `ContentControl` host with **no host-level Selection pattern** —
    verify the group's single-selection semantics through its segments' `SelectionItem` pattern
    instead of asserting a Selection provider on the host.
  - (RangeValue already covered for Slider/SteeringBar/ProgressBar.)
- [ ] **R2.8 — Command (C3).** Exercise `Command`/`CommandParameter` for `EtherButton`,
  `EtherIntelligenceButton`, `EtherCheckbox`, `EtherRadioButton`.
- [ ] **R2.9 — New data/adapter paths.** C1 data-path fixture for `EtherTabNavigation`
  (ItemsSource + selection) and `EtherSegmentedControl` (ItemsSource + selection); adapter fixtures
  driving the new `ObserveSelection` (TabNav) and `ObserveMasthead` (Masthead) into backend envelopes;
  add both to `VerifyStandardInteractionAdapters` (`…PropertyConsumption.cs:310-353`).
- [ ] **R2.10 — R5 docs.** Complete default-value XML-doc on the declared DPs flagged in the reports:
  Button, Dropdown, Slider, SegmentedControl, SteeringBar, Masthead, ProgressBar.
- [ ] **R2.11 — Build + run harness on x64.** Build Controls + Interactions + ConsumerFixtures
  (Packaged + Unpackaged) on x64 (`dotnet workload restore` first if MSB4276 appears). Run the
  runtime verification. Iterate until green. If any fixture cannot execute headless in this
  environment, list exactly which and why, and flag the runtime portion for the user's machine — **do
  not report green for what did not run.**

## Wave R3 — Flip reports to green + rollup
- [ ] **R3.1** Update each of the 15 `consumability/*.md` verdicts to reflect the fixes (✅ where
  proven; 🚫 for D1–D3 documented exclusions), keeping every claim cited to the now-updated source.
- [ ] **R3.2** Rewrite `consumability/_SUMMARY.md` rollup to the post-fix state; the "cross-cutting
  gaps" section becomes a "closed / residual" ledger. Any row not green must state exactly why and
  what remains (e.g. a runtime fixture that needs the user's machine).

## Acceptance (definition of done)
1. `dotnet build` clean on **x64** for Controls, Interactions, ConsumerFixtures.
2. The `ConsumerFixtures` runtime verification passes for every component (or the un-runnable portion
   is explicitly flagged, per R2.11).
3. Every `consumability/*.md` gate is satisfied — no ❌/⚠️ except documented 🚫 (D1–D3).
4. `_SUMMARY.md` shows the all-green rollup with residuals (if any) named.
