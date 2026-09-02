# Consumer Experience Optimization Plan — Making It Easier for Third-Party Developers to Adopt Ether

> **Date:** 2026-09-01 **Branch:** `codex/refine-components` **Nature:** Planning document (this document contains no code changes, builds, or runs).
>
> **Prerequisite (already complete, not redone in this document):** All 15 components have passed both the consumability and completeness reviews;
> `ConsumerFixtures` runtime verification is green (`{"marker":"ETHER_CONSUMER_SMOKE","outcome":"success"}`), and everything has been committed.
> Proven capabilities: every public property is a `DependencyProperty`; TwoWay binding for 12 state properties
> (`tests/Ether.DesignSystem.ConsumerFixtures/RuntimeVerification.R2.cs:57-194`); `Command` on the button family
> (`R2.cs:374-431`); the `ItemsSource` data path for Dropdown / SegmentedControl / TabNavigation
> (`R2.cs:196-236`). For the complete closure status, see
> [`consumability/_SUMMARY.md`](consumability/_SUMMARY.md) and
> [`2026-09-01-consumability-remediation-plan.md`](2026-09-01-consumability-remediation-plan.md).
>
> **The question this document answers:** now that the components "work," how do we make them **faster and easier** for a third-party developer unfamiliar with this repo to adopt?
>
> **Effort scale:** S ≤ half a day; M = half a day to 2 days; L > 2 days.
> **Execution approach:** follows the remediation plan's convention — coding done by Codex, reviewed in waves by another agent;
> every "acceptance criterion" must have evidence within the repo (fixture / script / gate); "should be fine" is not accepted.

---

## 0. One-page overview (recommended order)

| # | Item | One-liner | Value to consumers | Effort | Needs user decision |
| --- | --- | --- | --- | --- | --- |
| **P0** | Push the branch + open a PR | Push the 73 commits that have never gone through hosted CI, and let CI run once | Indirect (protects the work, surfaces CI issues) | **S** | No |
| **P1** | Consumer docs: per-control "copy-pasteable + bind data + wire actions" examples + MVVM chapter | The existing docs only teach "what it looks like," not "how to bind, how to wire it up" | **Highest** | **M** | Small (doc language, whether to recommend Toolkit.Mvvm) |
| **P2** | Gallery data-binding demonstrations | Turn the already-proven `ItemsSource`/TwoWay/`Command` capabilities into visible, copyable living documentation | High | **M** | Small (whether Gallery adopts an MVVM package) |
| **P3** | Add an optional `Command` to SegmentedControl (other controls evaluated and not added) | The data-driven segmented control currently has no "wire an action" hook at all — only event glue code is possible | Medium | **M** | **Yes** (API naming, whether it fires only on user-initiated action) |
| **P4** | Card discoverability: Option A (fill out the docs, folded into P1) / Option B (turn it into a real type, deferred) | The six keys behind `<Border Style=…>` are very hard for IntelliSense to discover | Medium-low | A **S** / B **L** | **Yes** (design decision) |
| **P5** | Closing item: packaged MSIX runtime verification | Currently **no script at all** installs/launches an MSIX; the release-blockers spec has already judged this a non-blocker | Low (internal, x64 unpackaged prioritized) | **M** (machine-dependent) | **Yes** (whether to make it mandatory before release) |

**Recommended order: P0 → P1 → P2 → P3 (if approved) → P4-A (alongside P1's completion; P4-B on hold) → P5 (if approved, requires a Developer Mode machine).**

Rationale: P0 costs almost nothing and precedes everything else (CI feedback will affect all downstream work); P1 has the highest value/cost ratio and changes no product API;
P2 gives P1's examples a "living" counterpart; P3 is the only API addition worth doing, after which P1/P2 just need one more section added;
P4-B and P5 are both design/environment items requiring a decision, and should not block the first four.

---

## P0 — Push the branch and open a PR (S)

**One-liner:** the current work exists only on this machine — back it up first and let CI run once.

**Technical detail:** on `origin`, only `main` and `codex/refactor` exist, and both sit at `8029f13`; the current branch
`codex/refine-components` is **73 commits** ahead of `main` and has **no upstream** (`git branch -vv` shows no tracking info).
This means the entire remediation effort has never gone through the hosted CI in `.github/workflows/build.yml`
(triggered on `push` / `pull_request`, `build.yml:9-11`) — i.e., none of the 26 `ci` gates in
`scripts/Gates.psd1:48-74` have verified these 73 commits in the hosted environment.

**Steps:**
1. `git push -u origin codex/refine-components`.
2. `gh pr create --base main --draft`, with the PR description linking three documents:
   `consumability/_SUMMARY.md`, `2026-09-01-consumability-remediation-plan.md`,
   `2026-09-01-component-consumability-review-spec.md`; note the runtime evidence file path
   (`artifacts/consumer-fixtures/runtime-result-9a2b552efe3b4c8bb6c9280635e54b77.json`).
3. Wait for the `build` and `package-consumers` jobs to complete; pay particular attention to the final step, `Verify-GateManifest.ps1`
   (`build.yml:134-140`) — it asserts that the workflow steps match `Gates.psd1`, and is the gate most likely to turn red because of a locally added script.
4. If CI is red, fix per the gate output; once green, move the PR from draft to ready (merging is the user's decision).

**Acceptance criteria:** the branch exists on `origin`; the PR exists; both CI jobs are green.
**Not doing:** no `nuget push` (`HANDOFF.md:3` — `Publish-Internal.ps1` is the sole authorized entry point, and release status remains on hold,
`docs/releases/0.1.0-preview.1.md:3`).

---

## P1 — Consumer docs: per-control "copy-paste + bind data + wire actions" (M, highest value)

**One-liner:** the current onboarding docs tell developers what a control "looks like," but not "how to bind data to it, how to make a click trigger your own code."

### 1.1 Current-state assessment (`docs/consumers/getting-started.md`)

Parts already in good shape: GitHub Packages onboarding (§1), the minimal project and `App.xaml` merge (§2, `:75-227`),
the troubleshooting table (§3, `:231-239`), the unsupported-properties registry (§4.1, `:261-273`), version discipline (§6).

**Gaps (verified item by item):**

| # | Gap | Evidence |
| --- | --- | --- |
| G1 | §5 "Per-control marker reference" (`:305-446`) **has appearance properties only** — the entire section has not a single `x:Bind`, `Command=`, or `ItemsSource` | Grepping the whole document for `TabNavigation\|ItemsSource\|x:Bind\|Command=\|ControlInteractionAdapter` hits only `:15` and `:107` (both are package-reference lines) |
| G2 | **Tab Navigation has no §5 entry** — the only one missing among the 15 components | §5 has only 14 sections: Button…ScrollBar; `EtherTabNavigation` appears zero times in the document |
| G3 | SegmentedControl's D1 data-driven contract (`ItemsSource`/`ItemTemplate`/`DisplayMemberPath`/`SelectedIndex`/`SelectedItem`, `src/Ether.DesignSystem.Controls/Controls/Inputs/EtherSegmentedControl.cs:37-126`) **is not written into the docs**; §5 still only teaches the inline `EtherSegmentRadioButton` pattern (`:363-379`) | Same as above |
| G4 | `Ether.DesignSystem.Interactions` (one of the three packages) **has no usage examples at all**; its README is prose only, with no code (`src/Ether.DesignSystem.Interactions/README.md:1-12`) | `ControlInteractionAdapter` already has 10 Observe\* methods (`src/Ether.DesignSystem.Interactions/ControlInteractionAdapter.cs:21-182`), yet no one knows how to call them |
| G5 | Masthead's `ActionInvoked` event (`Controls/Navigation/EtherMasthead.xaml.cs:119-120`), Slider/SteeringBar's `StepFrequency`/`Stops`/`SnapToStops`, Dropdown's `MaxVisibleItems`/`MenuGap` (`EtherDropdown.cs:126-154`), Button's `Variant`/`Size`/`LeftIcon`/`RightIcon` (`EtherButton.cs:87-145`, appearing only incidentally in the §2 example) — these "knobs," whether added by remediation or already existing, have no examples in §5 | Cross-referenced against each control's DP list |
| G6 | **There is no README.md at the repo root**; the only thing pointing people to `docs/consumers/getting-started.md` is a single line in the package README (`src/Ether.DesignSystem.Controls/README.md:5`) | `Glob README.md` finds no hit at the root |
| G7 | The documentation's own anti-rot rule (from the previous plan, B6.4: "every code block in the README must have an equivalent findable in a fixture/Gallery," `docs/plans/2026-08-30-third-party-consumability-plan.md:283`) only holds for the existing 14 sections; the newly added binding/command examples currently have **no XAML-form counterpart** in the fixtures (the TwoWay/Command proofs are constructed in C# code, `R2.cs:49-55, 388-398`) | — |

### 1.2 Concrete changes

**A. Convert §5 to a fixed three-part structure per control** ("appearance → bind data → wire actions"), each part citing existing proof:

| Control | Bind data (proven properties) | Wire action | Evidence |
| --- | --- | --- | --- |
| EtherButton / EtherIntelligenceButton | `Content`, `IsEnabled` | `Command="{x:Bind ViewModel.SaveCommand}"` + `CommandParameter` | `R2.cs:388-398` |
| EtherCheckbox | `IsChecked` TwoWay (two-state, null→false) | `Command` (fires on Toggle) | `R2.cs:68-76, 400-412` |
| EtherRadioButton | `IsChecked` TwoWay + `GroupName` | `Command` (fires on SelectionItem) | `R2.cs:78-86, 419-431` |
| EtherInput | `Text` TwoWay | `TextChanged="{x:Bind ViewModel.OnQueryChanged}"` | `R2.cs:88-96` |
| EtherDropdown | `ItemsSource` + `SelectedItem`/`SelectedValue` (+`SelectedValuePath`) TwoWay | `SelectionChanged` → x:Bind method | `R2.cs:98-116`; the `SelectedValuePath` recommendation in `Interactions/README.md:9` |
| EtherSegmentedControl | `ItemsSource` + `DisplayMemberPath`/`ItemTemplate` + `SelectedIndex`/`SelectedValue` TwoWay; inline pattern retained | `SelectionChanged` (`SegmentedSelectionChangedEventArgs` carries Old/New); add `Command` if P3 is approved | `R2.cs:118-136, 213-224` |
| EtherTabNavigation (**new entry**) | `ItemsSource` + `SelectedIndex`/`SelectedItem` TwoWay | `SelectionChanged` (the standard `Selector` event) | `R2.cs:168-186, 205-211` |
| EtherSlider / EtherSteeringBar | `Value` TwoWay, `StepFrequency`, `Stops`+`SnapToStops` | `ValueChanged`; **note that this fires on every drag tick** | `R2.cs:138-146`; `EtherSteeringBar.xaml.cs:381-384, 476-487` |
| EtherSwitch (Style) | `IsOn` TwoWay | `Toggled` | `R2.cs:148-156` |
| EtherProgressBar | `Value`/`Title`/`ValueContent`/`ShowTitle`/`ShowValue` OneWay | None (read-only display) | `EtherProgressBar.cs:60-93` |
| EtherMasthead | `ShowSettings`/`ShowSearch`/`ShowMenuIcon`/`ShowChevron`/`EnableWindowCommands` | `ActionInvoked` (the `MastheadAction` enum); icon slots are decorative (D2 🚫) | `EtherMasthead.xaml.cs:120-214, 543/561/585` |
| EtherCard / EtherScrollBar (Style) | No Ether properties | None | See P4-A |

**B. Add a new §7, "Binding to a ViewModel and MVVM Patterns"** (placed after §5 and before §6, or as a separate file
`docs/consumers/mvvm-patterns.md` linked from getting-started):

1. **`x:Bind` TwoWay to an `INotifyPropertyChanged` VM** — give a VM shaped exactly like the fixture's
   (the `TwoWayViewModel` pattern from `R2.cs:17-45`) + one page of XAML; emphasize that `x:Bind` defaults to `OneTime`, and editable properties must specify
   `Mode=TwoWay` (this is WinUI's most common pitfall, `deepwiki/WinUI3-XAML-AI-Coding-Guide.md:14`).
2. **Button-family `Command`/`CommandParameter`** — already proven for four controls, cite directly.
3. **Event → VM method: `{x:Bind ViewModel.OnSelectionChanged}`** — WinUI's built-in event binding,
   **requires no extra NuGet package**, and the signature can either match the event or take no parameters. This is the **preferred answer** for "controls without a Command" (Dropdown, TabNavigation,
   Slider, SteeringBar, Switch, Input), lighter-weight than introducing Behaviors.
4. **`Microsoft.Xaml.Interactivity` (`EventTriggerBehavior` + `InvokeCommandAction`) as an alternative** —
   give one example, but clearly note: this package is **not** in this repo's `Directory.Packages.props:6-13`, and is a consumer-opted-in dependency;
   this repo will not produce fixture proof for it (unless the user decides to add it to the fixtures, see 1.4).
5. **Three `ItemsSource` examples** (Dropdown / SegmentedControl / TabNavigation) + two honest limitation notes:
   - TabNavigation's `Icon` is a **container (`EtherTabItem`) property** (`EtherTabNavigation.cs:28-41`),
     so data-driven-generated tabs **cannot get per-item icons** (there's no `PrepareContainerForItemOverride`, `EtherTabNavigation.cs:9-18`); use inline `EtherTabItem` if icons are needed.
   - The Dropdown trigger only displays text (`DisplayMemberPath`/`ToString`); a rich `ItemTemplate` only takes effect in the list
     (`EtherDropdown.cs:42-43`).
6. **One end-to-end Interactions adapter example**: `ObserveButton` / `ObserveSelection` / `ObserveMasthead` →
   subscribe to `InteractionProduced` → write to the app's outbox (`ControlInteractionAdapter.cs:17-18, 57-69, 115-127`).
   Positioned as a "backend event envelope," **not** MVVM glue, to avoid readers conflating the two layers.
7. **Common mistakes side-by-side**: the `DataContext` difference between `{Binding}` vs `{x:Bind}`; `x:Bind` inside a `DataTemplate` must specify
   `x:DataType`; `IsChecked` is `bool?`, so the VM should use `bool?` or a converter.

**C. Root-level `README.md` (S, can go in the same batch as P0):** three sections — what this is, how to adopt it (linking to getting-started),
and a repo map (`src/`, `samples/`, `tests/`, `docs/internal/consumability/`, `scripts/Gates.psd1`).

**D. Anti-rot (optional, M; if skipped, rely on a manual rule instead):** land the §5/§7 XAML examples in a dedicated
`DocsSnippets` region within the fixtures (`tests/Ether.DesignSystem.ConsumerFixtures/Unpackaged/MainWindow.xaml` already has a pattern of 13
`*Proof`-marked sections, `:44-293`), and add a new `scripts/Verify-ConsumerDocSnippets.ps1` asserting "documentation code blocks ⊆ fixture markers."
Note: any new `Verify-*.ps1` must be registered in both `scripts/Gates.psd1` and `build.yml`, or
`Verify-GateManifest.ps1` will go red (`Gates.psd1:101-114`).

### 1.3 Effort and sequencing
- A + B + C: **M** (roughly 1–1.5 days, pure documentation + one local run of `Verify-UnsupportedProperties.ps1`, since the §4.1 table is script-generated/validated, `:249-254`).
- D: an additional **M**; recommend executing A/B under the manual rule first, and leaving D until after P2 (the Gallery examples themselves serve as a second living counterpart).

### 1.4 Decisions needing the user
1. **Documentation language**: getting-started is in Chinese, while `docs/internal/2026-09-01-*` is in English. Recommendation: keep the consumer docs in Chinese (the readers are the internal team), with terminology left in English as-is.
2. **Whether to "designate" `CommunityToolkit.Mvvm`** as the VM pattern in the examples (`[ObservableProperty]`/`[RelayCommand]`).
   This affects only the consumer side, not the library itself; this repo's deepwiki guide already recommends it (`deepwiki/WinUI3-XAML-AI-Coding-Guide.md:708-714, 742`).
   Recommendation: use hand-written `INotifyPropertyChanged` in the primary examples (consistent with the fixtures, zero dependency), with the Toolkit-equivalent pattern noted as a sidebar.
3. **Whether to add `Microsoft.Xaml.Interactivity` to the fixtures** to provide runtime proof for the Behaviors example. Recommendation: no — the documentation should clearly state "consumer-opted-in, not verified by this repo."

---

## P2 — Gallery data-binding demonstrations (M)

**One-liner:** every Gallery example today uses hand-written, hardcoded items and code-behind event handlers — there's no way to see what "binding a collection, binding a ViewModel" looks like.

**Technical detail (current state):**
- `samples/Ether.DesignSystem.Gallery/Views/Controls/SegmentedControlPage.xaml:19-94`: three groups of inline
  `EtherSegmentRadioButton`, with output driven by a code-behind `SelectionChanged` handler (`.xaml.cs:31-35`);
  `SpecimenXaml` also only shows the inline form (`.xaml.cs:9-20`).
- `Views/Navigation/TabNavigationPage.xaml:12-33`: three groups of inline `EtherTabItem`; `.xaml.cs:21-25` is likewise code-behind.
- `Views/Controls/DropdownPage.xaml:44-59`: nine inline `ComboBoxItem`.
- `Views/Controls/ButtonPage.xaml:49-138`: seven `Click=` handlers, **not a single `Command=`**.
- The Gallery project has no MVVM package at all (`Ether.DesignSystem.Gallery.csproj:18-24` only references WASDK), and no `ViewModels/` directory.

**Concrete changes (add one "DATA-DRIVEN" block per page, placed after the existing blocks, without touching them):**

1. **SegmentedControlPage**: add a fourth block,
   `ItemsSource="{x:Bind Periods}" DisplayMemberPath="Label" SelectedItem="{x:Bind SelectedPeriod, Mode=TwoWay}"`;
   the page class gains `ObservableCollection<PeriodItem> Periods` + `SelectedPeriod` (implementing `INotifyPropertyChanged`);
   OutputText is updated by `SelectedPeriod`'s setter (proving TwoWay genuinely writes back), no longer relying only on the event.
   Update `SpecimenXaml` correspondingly to show both the "inline" and "data-driven" sections.
2. **TabNavigationPage**: add `ItemsSource="{x:Bind Sections}"` + `DisplayMemberPath` (or `ItemTemplate`) +
   `SelectedIndex="{x:Bind SelectedSectionIndex, Mode=TwoWay}"`; add a one-line note next to the block title stating that "data-driven tabs don't support `Icon`" (consistent with P1-B-5).
3. **DropdownPage**: add a block with `ItemsSource` + `SelectedValuePath="Id"` + `SelectedValue` TwoWay (matching the pattern recommended in Interactions README:9).
4. **ButtonPage** (optional, S): change one of the buttons to `Command="{x:Bind SaveCommand}"`, with the page class providing a minimal `ICommand`
   (can directly reuse the fixture's `RecordingCommand` shape, `R2.cs:364-372`), making "the button family natively supports Command" visible.
5. Put view models under `samples/Ether.DesignSystem.Gallery/ViewModels/` (or as a `partial` within the page), **hand-written INPC**, no new package introduced.

**Gate impact (already checked, none block):**
- `scripts/Verify-GalleryControlExample.ps1:109-127` only asserts that the page is still on `ComponentPage`/`ControlExample`,
  that `SourceXaml` binds `SpecimenXaml`, and that the code contains the control name — the new blocks don't trigger it.
- `scripts/Verify-GalleryLocalization.ps1:89-102` only requires `ComponentPage`'s `x:Uid` Title/Description against a fixed key set (`:60-84`);
  `x:Uid` on new `TextBlock`s within the page is a **convention**, not a gated requirement (the architecture doc acknowledges "page body copy is still mostly English,"
  `docs/architecture/2026-08-26-l4-gallery-control-example.md:90`). Recommendation: still add `x:Uid` per convention and register it in
  `Strings/en-US/Resources.resw` (the existing `GalleryOutput.Selected` is at `:1558`).
- The catalog `ComponentCatalog.cs:61-83` needs no change (no new pages added).

**Verification:** the two static CI gates + a local run of `scripts/Verify-GallerySmoke.ps1` (`Gates.psd1:78`, local-runtime).
**Effort:** M (about 1 day, including one local Gallery smoke run).
**Decision:** whether Gallery adopts `CommunityToolkit.Mvvm`. Recommendation: don't (the Gallery is a `ProjectReference` host,
and shouldn't imply the library has this dependency; hand-written INPC is consistent with the fixtures).

---

## P3 — Exposing more interactions as `ICommand`: per-control evaluation (M, needs a decision)

**One-liner:** reduce how much "call the ViewModel from an event handler" glue code developers have to write — but only add this where it's a genuine convenience and doesn't deviate from the native WinUI contract.

**Constraint:** `AGENTS.md:8-13` requires components to have "behavior consistent with the corresponding official WinUI control, differing only in appearance"; any deviation must state its rationale
(`AGENTS.md:37-40`). In native WinUI, only the `ButtonBase` family has `Command`; `Selector` (ComboBox/ListView),
`RangeBase` (Slider), and `ToggleSwitch` do not. So adding `Command` to a **thin subclass** is a deviation, whereas adding it to an **Ether-owned type**
(a hand-templated `ContentControl`/`Control`) is a reasonable addition — consistent with the precedents of
D1 (SegmentedControl gaining `ItemsSource`) and D2 (Masthead gaining `ActionInvoked`).

**Evaluation table:**

| Control | Base class | Existing notification surface | Does the native counterpart have Command? | Conclusion |
| --- | --- | --- | --- | --- |
| **EtherSegmentedControl** | `ContentControl` (Ether-owned, `EtherSegmentedControl.cs:28`) | `SelectionChanged` event (`:129`) + 5 selection DPs | No direct counterpart (`RadioButtons` doesn't have one either) | **Recommend adding** (see below) |
| EtherSteeringBar | `Control` (Ether-owned, `EtherSteeringBar.xaml.cs:78`) | `ValueChanged` (`:131`), **fires on every drag tick** (`:381-384 → :476-487`); releasing (`:861-869`) has no separate "commit" notification | `Slider` doesn't have one | **Don't add**: a Command would execute once per tick — wrong semantics; if there's a need, first add a `ValueCommitted` **event** (new semantics, a separate item) |
| EtherSlider | `RangeBase` (`EtherSlider.xaml.cs:45`) | Native `ValueChanged` (`:313`), likewise per-tick; `:604-614` release has no commit event | None | **Don't add**, same reasoning |
| EtherTabNavigation | `ListView` (`EtherTabNavigation.cs:9`) | Native `SelectionChanged` | None (`NavigationView` also only has an `ItemInvoked` event) | **Don't add**: would deviate from the ListView contract; use `{x:Bind}` event binding or `ObserveSelection` (`ControlInteractionAdapter.cs:57-69`) |
| EtherDropdown | `ComboBox` (`EtherDropdown.cs:67`) | Native `SelectionChanged` | None | **Don't add** |
| EtherMasthead | `Control` | `ActionInvoked` (`:120`), fires before the host window's command (`:543/561/585`), not cancelable | No counterpart | **Don't add**: the three actions are already window commands, and a VM rarely needs to take them over; "intercepting close" would be a separate requirement (a cancelable parameter) |
| EtherSwitch | Keyed Style only (`EtherSwitch.xaml.cs:6-13`, deliberately typeless) | `Toggled` | `ToggleSwitch` doesn't have one | **Cannot add** (no type to hang a DP on; `IsOn` TwoWay is already sufficient) |
| EtherInput | `TextBox` (`EtherInput.cs:28`) | `TextChanged` | None | **Don't add** |
| Button / IntelligenceButton / Checkbox / RadioButton | `ButtonBase` family | Native `Command` | Yes | **Already done** (`R2.cs:374-431`) |

**Why SegmentedControl is worth adding to:** an inline segment is `EtherSegmentRadioButton : RadioButton`
(`EtherSegmentRadioButton.cs:26`), and each segment **already** has a native `Command`; but the segments **generated from `ItemsSource`**
are created internally by `CreateGeneratedSegment` (`:251-263`), and consumers have no hook at all to set a `Command` on them — the data-driven path,
which is D1's newly added primary path, is the one place where only event glue code is possible.

**Design draft (for decision):**
- Add two new DPs, `SelectionCommand : ICommand?` + `SelectionCommandParameter : object?` (registered the same way as `:37-77`).
  When `CommandParameter` is null, `SelectedValue` is used as the parameter.
- **Fires only on user-initiated action**: carry a `userInitiated` flag through the `OnSegmentChecked` (`:373-377`) → `ApplySelection` (`:382-401`) path;
  programmatically setting `SelectedIndex`/rebuilding via `ItemsSource` (`:214-249`) does **not** fire it — consistent with
  `ButtonBase.Command`'s semantics of only executing on a genuine user click. Execution happens right after the `SelectionChanged` event (`:195-197`).
- Proof: extend `VerifyCommands` (`R2.cs:374-386`) with one more case, using `RecordingCommand` (`:364-372`) + the segment's
  `SelectionItem.Select()` (precedent at `:419-431`) to assert "executes exactly once with the correct parameter," then assert that programmatically setting `SelectedIndex` does **not** execute it.
- Registration: `src/Ether.DesignSystem.Controls/PublicAPI.Unshipped.txt` (pattern at `:203-204`);
  `scripts/Verify-EtherSegmentedControlContract.ps1` needs updating in sync if it asserts the public surface; the C3 row of `consumability/EtherSegmentedControl.md`;
  one added line each in getting-started §5 and the P2 Gallery block.
- The documentation should state that this is an **addition** relative to the official skeleton, along with the rationale (`AGENTS.md:39-40`).

**Effort:** M (about 1 day: DPs + trigger logic + fixture + PublicAPI + docs + one run of `Verify-ConsumerFixtures.ps1`).
**Needs a decision:**
1. Whether to do it.
2. Naming: `Command`/`CommandParameter` (same names as `ButtonBase`, most familiar in IntelliSense) or `SelectionCommand` (clearer semantics, doesn't conflict with future other actions). The latter is recommended.
3. Whether it fires only on user-initiated action (recommended: yes).

---

## P4 — Card discoverability (A: S, alongside P1; B: L, needs a design decision)

**One-liner:** developers typing `<ether:` in IntelliSense can't find Card, because it isn't a control type — it's six style keys wrapped around a `Border`.

**Current state:** `src/Ether.DesignSystem.Controls/Resources/Foundations/EtherCard.xaml:95-149` defines six
keyed Styles with `TargetType="Border"` (Normal/Intelligence/Callout, each with a shell+body pair); Callout is a **composite structure**
that can only be used by pasting the example (`:151-185`), and even the Gallery assembles it by hand, with even the header arrow being a Gallery-supplied SVG
(`Views/Surfaces/CardPage.xaml:56-87`, SVG at `:68-77`). The review conclusion recorded "cannot be instantiated" as 🚫 and gave an alternative
(`consumability/EtherCard.md:33, 44-50`). getting-started only demonstrates the Normal variant (`:420-429`).

### Option A (recommended to do now, S, folded into P1)
- getting-started's §5 Card entry gets fully filled out with complete, paste-ready XAML for **all three** combinations (Intelligence, Callout including the header row),
  with the header arrow switched to the icon library's `IconArrowRight`
  (`src/Ether.DesignSystem.Foundation/Resources/Tokens/EtherIconGeometries.xaml:91`), so consumers don't need to bring their own SVG.
- At the start of the entry, add one sentence explaining "why it's a Style and not a type," and list a table of the six keys' purposes.
- The Gallery `CardPage`'s `SpecimenXaml` is synchronized to show all three sections.

### Option B (deferred; L, design decision)
Turn Card into `EtherCard : ContentControl`, with `Variant` (`Normal|Intelligence|Callout`) + two or three DPs for `Header`/`HeaderTemplate`
(used by Callout); the default style goes in `Controls/Surfaces/EtherCard.xaml` and is merged in `Themes/Generic.xaml` (currently `:24`);
the six keyed Styles are **retained** (they're already shipped API — removing them would be a breaking change).

| Benefit | Cost / risk |
| --- | --- |
| `<ether:EtherCard Variant="Callout" Header="Intelligence Insight">…</ether:EtherCard>` works in one line, discoverable via IntelliSense | **No official WinUI skeleton to copy**: the WinUI Gallery's card is a `Border`/`Grid`; the Toolkit's `SettingsCard` derives from `ButtonBase` (clickable), which doesn't match the "decorative surface" semantics → the template must be hand-written, with the rationale documented per `AGENTS.md:37-40` |
| The Callout composite structure gets pulled into the library, with the arrow/header styling maintained in one place | Adds a 16th type: `PublicPropertyInventoryTypes`, `DeclaredPropertyOwnerTypes`, PublicAPI, a new `Verify-EtherCardContract.ps1` (+`Gates.psd1`+`build.yml`), the six-key fixture in `StyleResources.cs:13-30` must be kept alongside a new type-based fixture, `EtherCard.md` rewritten, and the `EtherCardNormal` snippet asserted by `Verify-GalleryControlExample.ps1:29` must remain compatible |
| If a "clickable card" or similar variant is designed in the future, the type is the only place to carry it | No second consumer has raised this need yet; whether hardcoded sizes like `MinWidth=392` stay in the style or become a DP needs a design call |

**Recommendation:** do A now; hold B until a real consumer need arises, or a design introduces a variant that a Style can't express. This is a design decision for the user to make.

---

## P5 — Closing item: packaged MSIX runtime verification (M, machine-dependent, needs a decision)

**One-liner:** the one remaining "leftover item" actually can't be closed just by "running it on a different machine" — currently **no script at all installs and launches an MSIX**, and that path needs to be built first.

**Verified facts (correcting the statement in `_RUN-ON-DESKTOP.md:59-64`):**
- `scripts/Verify-ConsumerFixtures.ps1:862-874` **builds** both the Packaged and Unpackaged fixtures; `:884-889` only asserts, for Packaged,
  that the output file exists; the runtime smoke at `:891-930` **only launches the Unpackaged exe** (`:895`).
- `scripts/Verify-MsixPackage.ps1:28-48` only produces an **unsigned** MSIX, and explicitly prints "not installed."
- The fixtures README states "installation and launch are deliberately out of ... scope," and the Packaged fixture
  **opts out of the font's transitive copy** (`tests/Ether.DesignSystem.ConsumerFixtures/README.md:10-14`) — so even if it were run, font path behavior would differ from Unpackaged.
- The good news: Packaged's `App.xaml.cs:21` responds to `ETHER_CONSUMER_SMOKE=1` just like Unpackaged, and the `RuntimeVerification` code is shared;
  but the result-marker path is passed via an environment variable (`RuntimeVerification.cs:571`, `Infrastructure.cs:279-282`), and an MSIX app activated via the shell **does not inherit** the launching shell's environment variables.
- The release-blockers spec has already judged "install MSIX on a clean machine → run it" to be an **optional follow-up, not a blocker for this round**
  (`docs/plans/2026-08-31-release-blockers-spec.md:96, 643`; the R-08 correction is recorded at `:472-486`), and the package READMEs and release notes have already been honestly annotated
  (`src/Ether.DesignSystem.Controls/README.md:11`; `docs/releases/0.1.0-preview.1.md:79`).

**If it's decided to do this, the steps are:**
1. **Fix the copy first (S, can be done immediately)**: change `_RUN-ON-DESKTOP.md:59-64` to say "no scripted path exists yet, needs to be built per the steps below," to avoid misleading the next person.
2. **Installation method (decision)**: (a) `Add-AppxPackage -Register <bin>\AppxManifest.xml` loose deployment (requires Developer Mode, unsigned);
   (b) test-certificate signing + `Add-AppxPackage <msix>`; (c) `Add-AppxPackage -AllowUnsigned` (Win11 22H2+ with Developer Mode). Recommend (a).
3. **Launch with environment variables in the package context**: `Invoke-CommandInDesktopPackage -PackageFamilyName … -AppId … -Command …`,
   or change the result path to be passed via launch arguments / `ApplicationData.LocalFolder` (requires modifying the read logic near `RuntimeVerification.cs:571`, shared by both hosts).
4. Add a new `scripts/Verify-PackagedRuntime.ps1` (or add a `-Packaged` switch to `Verify-ConsumerFixtures.ps1`), registered as
   a `local-runtime` entry in `Gates.psd1` (`:77-95`), passing `Verify-GateManifest.ps1`.
5. Run it on a Developer Mode machine, with the marker written to `artifacts/audit-runs/…`, and update the leftover section at `_SUMMARY.md:195-202`, package README:11, and release notes:79.

**Effort:** M (about 1 day for the script + marker-passing rework; the run itself is machine-dependent).
**Decision:** whether to make this mandatory before this preview release. Given the existing spec and the "internal-only / x64 / unpackaged prioritized" distribution decision, recommend **not** making it a blocker, sequenced after P1–P3.

---

## 6. Items already confirmed as "complete or not worth doing" (no longer planned)

- **TwoWay binding, button-family Command, three `ItemsSource` paths** — all already proven by fixtures (`_SUMMARY.md:78-84, 98-103, 109-114`); only documentation and demonstrations (P1/P2) remain, no product changes needed.
- **Interactions adapter** — already covers the observable surface of all 15 components (`_SUMMARY.md:115-122`); all that's missing is usage examples (P1-B-6).
- **Adding `Command` to Slider / SteeringBar / TabNavigation / Dropdown / Masthead / Switch / Input** — evaluated and not added (P3 table), with reasons respectively being "per-tick firing has wrong semantics," "deviates from the native Selector/ToggleSwitch contract," and "no type to hang it on."
- **Turning Card into a type (P4-B)** — at this stage the benefit doesn't outweigh the L-level cost and the architectural deviation of "no official skeleton"; on hold.
- **Running GUI/MSIX installation on hosted CI** — explicitly forbidden by `HANDOFF.md:4` and `build.yml:3-7`; P5 can only be done in a local local-runtime gate.

## 7. Decision checklist (for the user to decide)

| # | Decision | Related item | Recommendation |
| --- | --- | --- | --- |
| D-1 | Keep consumer docs in Chinese? | P1 | Yes |
| D-2 | Should examples recommend `CommunityToolkit.Mvvm`? | P1 | Hand-written INPC in the main line, Toolkit pattern as a sidebar |
| D-3 | Add `Microsoft.Xaml.Interactivity` to the fixtures for proof? | P1 | No, document it as "consumer-opted-in" |
| D-4 | Should Gallery adopt an MVVM package? | P2 | No |
| D-5 | Add `SelectionCommand` to SegmentedControl? Naming? Fire only on user-initiated action? | P3 | Add it; `SelectionCommand`; yes |
| D-6 | Card: A (fill out the docs) or B (real type)? | P4 | A first, B on hold |
| D-7 | Is packaged MSIX runtime a release blocker? | P5 | No (consistent with the R-08 decision), sequenced after P1–P3 |

## 8. Recommended execution waves

- **Wave 0 (half a day):** P0 push + PR; root README (P1-C); `_RUN-ON-DESKTOP.md:59-64` copy fix (P5-1).
- **Wave 1 (1–1.5 days):** P1-A/B documentation rewrite + P4-A three Card examples; run `Verify-UnsupportedProperties.ps1` to confirm the §4.1 table isn't broken.
- **Wave 2 (1 day):** P2 Gallery data-driven blocks ×3 (+ optional ButtonPage Command); local `Verify-GallerySmoke.ps1`.
- **Wave 3 (1 day, if D-5 is approved):** P3 SegmentedControl `SelectionCommand` + fixture + PublicAPI; local `Verify-ConsumerFixtures.ps1`; backfill one section each into P1/P2.
- **Wave 4 (per D-7, 1 day + a machine):** P5 packaged runtime script and an actual run.

Each wave's completion criteria match the remediation plan: change + evidence + green gates; anything not run is explicitly stated as "not run," never reported green.
