# DX optimization spec — consumer docs + Gallery data-binding + SegmentedControl command

> Executable spec for the approved subset of the
> [optimization plan](2026-09-01-consumer-experience-optimization-plan.md): **P1** consumer docs
> (English), **P2** Gallery data-binding demos, **P3** an optional `SelectionCommand` on
> EtherSegmentedControl. Branch `codex/refine-components` (pushed to origin, no PR yet).
>
> **Locked decisions:** consumer docs in **English**; **no new package dependency** added to the
> product or the Gallery (docs *reference* CommunityToolkit.Mvvm / Microsoft.Xaml.Interactivity as
> consumer-side options, the library depends on neither); SegmentedControl command is
> **`SelectionCommand`**, fired on **user-initiated** selection only.
>
> **Execution:** Sonnet 5 codes each part. Edits are delegated; the verification run is executed from
> the main session as a background Bash command (see the consumability skill's orchestration note —
> don't let a subagent own the long run). Honesty rules from that skill apply: never weaken an
> assertion, never report green for what didn't run.

## P1 — Consumer documentation (English). No product code.

Goal: a third-party developer can, for **every** component, copy-paste a working example that shows
not just how it *looks* but how to **bind data** and **wire actions**. Grounded gaps (per the plan):
`docs/consumers/getting-started.md` has no `x:Bind`/`Command=`/`ItemsSource`/adapter usage; Tab
Navigation is the only component with no §5 entry; the D1 SegmentedControl data-driven contract is
undocumented; the Interactions package appears only in a package-reference line; the repo has no root
README.

- [ ] **P1.1 — Per-component usage, three-part structure.** In `docs/consumers/getting-started.md`,
  give each of the 15 components a copy-paste block in the shape **appearance → bind data → wire an
  action**. Show the real API: a `{x:Bind …, Mode=TwoWay}` on the state property, `ItemsSource` +
  `SelectedIndex/SelectedItem` for the collection controls (Dropdown, SegmentedControl,
  TabNavigation), `Command`/`CommandParameter` for the button family, and the relevant event for the
  rest. **Add the missing Tab Navigation entry.**
- [ ] **P1.2 — "Binding to a view-model" / MVVM patterns section.** One new section covering: DP TwoWay
  binding; the `ItemsSource` data-driven path (incl. the D1 SegmentedControl contract —
  `ItemsSource`/`ItemTemplate`/`DisplayMemberPath`/`SelectedIndex`/`SelectedItem`, inline segments
  still supported); and **event-to-command** for the controls that expose events not commands (show
  the `Microsoft.Xaml.Interactivity` `InvokeCommandAction` pattern, and mention `CommunityToolkit.Mvvm`
  `RelayCommand` — as consumer options, explicitly noting the Ether packages don't depend on them).
- [ ] **P1.3 — Interactions adapter usage.** A short subsection showing
  `ControlInteractionAdapter.Observe…(control, eventType, context, componentId)` →
  `InteractionProduced` → enqueue into an `IInteractionSink`, so a consumer can forward interactions
  to a backend. Frame it as the optional backend-telemetry path, distinct from ordinary MVVM binding.
- [ ] **P1.4 — Root `README.md`.** Create `README.md` at the repo root: what Ether is, the three
  packages (Foundation/Controls/Interactions) and what each is for, install (`dotnet add package`),
  a 10-line "hello button" quick-start, and links to `docs/consumers/getting-started.md` + the
  component reports. Keep it tight.

Acceptance (P1): docs render; every component has a data-binding + action example; Tab Navigation
entry present; `scripts/Verify-*` doc-consistency gates still pass (e.g.
`Verify-UnsupportedProperties.ps1`, and any getting-started table gate — check before/after).

## P2 — Gallery data-binding demos. Gallery XAML/C#; no new package dependency.

Goal: make the data-driven capability *visible* and self-documenting. Existing pages are inline items
+ code-behind events (`ButtonPage` has 7 `Click=` and no `Command=`). Fable verified new blocks don't
trip `Verify-GalleryControlExample.ps1` / `Verify-GalleryLocalization.ps1` — re-check after editing.

- [ ] **P2.1 — Add an `ItemsSource` + TwoWay demo block** to `SegmentedControlPage.xaml`,
  `Navigation/TabNavigationPage.xaml`, and `Controls/DropdownPage.xaml`: a small in-page view-model
  (a plain `ObservableCollection<T>` + a selected-index/value property on the page or a lightweight
  local class — **no `CommunityToolkit.Mvvm` dependency**), bound via `x:Bind`, with a live readout of
  the selection. Label it clearly (e.g. "Data-bound (ItemsSource)") next to the existing inline demo.
- [ ] **P2.2 — Optional `Command` example on `ButtonPage.xaml`.** One button wired via
  `Command`/`CommandParameter` (a simple page-level `ICommand`) alongside the existing `Click=`
  examples, to demonstrate the MVVM path. Keep it minimal.
- [ ] **P2.3** Localization: follow the page's existing `x:Uid` convention for any new visible strings
  so `Verify-GalleryLocalization.ps1` stays green (or add the resource entries it expects).

Acceptance (P2): Gallery builds x64 and launches; the new demo blocks render and the selection readout
updates on interaction; gallery gate scripts pass.

## P3 — EtherSegmentedControl optional `SelectionCommand`. Product code + fixture.

Rationale (per the plan's per-control analysis): Slider/SteeringBar `ValueChanged` fires per drag tick
(wrong for a command); TabNav/Dropdown are thin ListView/ComboBox subclasses (adding a command
deviates from the AGENTS.md native-contract rule); Switch has no type. **SegmentedControl's
`ItemsSource`-generated path is the one interactive surface with no "wire an action" hook** — so it
alone gets an optional command.

- [ ] **P3.1 — Add DPs** to `src/Ether.DesignSystem.Controls/Controls/Inputs/EtherSegmentedControl.cs`:
  `public ICommand? SelectionCommand` (+ `SelectionCommandProperty`) and
  `public object? SelectionCommandParameter` (+ `…Property`), both XML-doc'd with defaults.
- [ ] **P3.2 — Fire on user-initiated selection only.** When the selection changes because the **user**
  picked a segment, invoke `SelectionCommand` (guarded by `CanExecute`) with `SelectionCommandParameter`
  if set, else the `SelectedValue`. It must **not** fire on programmatic `SelectedValue`/`SelectedIndex`
  assignment — mirror `ICommand` "user action" semantics (a Button's `Command` fires on Click, not on a
  programmatic invoke of state). The user-initiated path runs through `OnSegmentChecked`; the
  synchronizing/programmatic path (`_synchronizingSelection`) must be excluded. Keep `SelectionChanged`
  unchanged (it still fires on both, as today).
- [ ] **P3.3 — Register public API:** add the two properties + two DP fields to
  `src/Ether.DesignSystem.Controls/PublicAPI.Unshipped.txt` (the Roslyn analyzer fails the build
  otherwise).
- [ ] **P3.4 — Prove it.** Add a fixture (in the SegmentedControl runtime verification, wired into a
  stage `RuntimeVerification.VerifyAsync` actually calls): assign a counting `ICommand`, simulate a
  **user** segment selection, assert exactly one execution with the expected parameter; then a
  **programmatic** `SelectedIndex` change asserts **zero** additional executions. Extend the relevant
  verification record with the new fields so the green marker attests they ran.

Acceptance (P3): Controls + Interactions + ConsumerFixtures build x64 (0 errors, PublicAPI registered);
`scripts/Verify-ConsumerFixtures.ps1` reaches `{"marker":"ETHER_CONSUMER_SMOKE","outcome":"success"}`
with the new SelectionCommand fields populated in the marker.

## Overall acceptance & wrap
1. x64 build clean (Controls/Interactions/ConsumerFixtures/Gallery).
2. `scripts/Verify-ConsumerFixtures.ps1` green (P3 proven); gallery + doc gate scripts green.
3. Gallery launches and the data-binding demos work.
4. Docs (getting-started + root README) are complete and English.
5. Update the SegmentedControl consumability report if the new command changes its A/B verdicts;
   otherwise no report changes. Commit in logical groups when the user asks.
