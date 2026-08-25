# Phase 3 Pilot: Dropdown Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Relocate Dropdown's template/style dictionary into `Controls/`, completing Phase 3.

**Architecture:** Unlike Phase 2's components, Dropdown already has a dedicated, substantial
`.cs` class (`Controls/EtherDropdown.cs`, 358 lines, a `ComboBox` subclass with hard-won
template-part access, content-hugging width via `MeasureOverride`, and a `SizeChanged`-based
popup-positioning fix — see its own doc comments for why). This task is **pure relocation of
`Resources/EtherDropdown.xaml` only** — do not touch `Controls/EtherDropdown.cs` at all, and do
not touch `OnApplyTemplate`, `MeasureOverride`, or the popup positioning logic even indirectly.
Byte-for-byte behavior parity is the bar, not a rewrite, per the parent plan's explicit
instruction for this phase.

The dictionary already follows the target pattern exactly: `<Style TargetType="controls:EtherDropdown">`
(no `x:Key`, line 314) is **implicit** — already correct, already proven safe since it targets
the custom `EtherDropdown` type, not a stock framework type. `EtherDropdownItem` (targets stock
`ComboBoxItem`) stays **keyed** — also already correct, referenced only via
`ItemContainerStyle="{StaticResource EtherDropdownItem}"` inside `EtherDropdown`'s own style,
never applied directly to any bare `ComboBoxItem` elsewhere. Neither keying decision changes in
this task.

**No consumer file changes needed:** `Views/Controls/DropdownPage.xaml` uses bare
`<controls:EtherDropdown>` everywhere (confirmed via grep — no `Style=` attribute anywhere in
that file), so it picks up the implicit style automatically regardless of which file the
dictionary physically lives in. `DropdownPage.xaml.cs` drives 3 STATES swatches via
`VisualStateManager.GoToState` (`DropdownHoverState` → `PointerOver`, `DropdownPressedState` →
`Pressed`, `DropdownOpenState` → `Opened`) — untouched, since neither the `EtherDropdown` type
nor its template's `VisualStateGroups` change.

**Tech Stack:** WinUI3 / .NET 8, unpackaged WinExe. No test framework — verification is
`dotnet build` + diff-equivalence check + grep exit gate. No GUI verification available.

---

## Task 1: Relocate Dropdown's template/style dictionary into `Controls/`

**Files:**
- Create: `Controls/EtherDropdown.xaml`
- Modify: `App.xaml` (merge source path, currently `Resources/EtherDropdown.xaml`)
- Delete: `Resources/EtherDropdown.xaml`

**No changes to:** `Controls/EtherDropdown.cs`, `Views/Controls/DropdownPage.xaml`,
`Views/Controls/DropdownPage.xaml.cs`.

- [ ] **Step 1: Create `Controls/EtherDropdown.xaml`**

Read the current file at `Resources/EtherDropdown.xaml` (~342 lines) and copy its FULL content
verbatim: the `x:Double x:Key="DropdownMinWidth"` resource, the
`ControlTemplate x:Key="EtherDropdownTemplate"` (with its `CommonStates`/`FocusStates`/
`DropDownStates` visual state groups, the `StateFill`/`OpenFill` dual-fill mechanism documented
in its own comment, every named element — `LayoutRoot`, `StateFill`, `OpenFill`, `Bg`,
`ContentPresenter`, `TriggerText`, `Arrow`, `ArrowRotation`, `ActiveStroke`, `FocusRing`,
`Popup`, `PopupBorder`, `ScrollViewer`), the `Style x:Key="EtherDropdownItem"` (targets stock
`ComboBoxItem`, stays keyed), and the final **implicit** `Style TargetType="controls:EtherDropdown"`
(no `x:Key` — stays implicit, do not add one). Nothing dropped, nothing re-keyed, nothing made
implicit or un-implicit relative to the current state.

The ONLY change: insert this block into the existing header comment, right before the closing
`-->`:

```
    LOCATION
      This dictionary lives beside its class (Controls/EtherDropdown.cs) and is merged
      explicitly from App.xaml. See the "Mechanism refinement" note in
      docs/superpowers/plans/2026-08-06-self-contained-component-authoring-plan.md.
```

Everything else must be copied exactly as-is.

- [ ] **Step 2: Repoint the `App.xaml` merge**

Find `<ResourceDictionary Source="Resources/EtherDropdown.xaml"/>` and change it to
`<ResourceDictionary Source="Controls/EtherDropdown.xaml"/>`. Keep it in the exact same
position in the merge list.

- [ ] **Step 3: Delete the old file**

`git rm Resources/EtherDropdown.xaml`

- [ ] **Step 4: Build**

`dotnet build EtherComponentSandbox.csproj -c Debug` — expect 0 errors.

- [ ] **Step 5: Diff-equivalence check**

Confirm the resources/template/styles in `Controls/EtherDropdown.xaml` are character-for-character
identical to the deleted file, other than the added LOCATION comment block. Specifically confirm
the final `Style TargetType="controls:EtherDropdown"` still has NO `x:Key` (implicit) and
`EtherDropdownItem` still has its `x:Key` (keyed) — this is the one detail most likely to get
flipped by mistake since every other file relocated so far in this session either added or kept
a key, never had one already-implicit style to preserve as-is. Do NOT claim GUI visual
verification — note the limitation in your report.

- [ ] **Step 6: Grep exit gate**

Repo-wide grep for `EtherDropdown` (excluding `obj/`/`bin/`). Every hit should be one of:
`Controls/EtherDropdown.cs`, `Controls/EtherDropdown.xaml`, `App.xaml`,
`Views/Controls/DropdownPage.xaml` (bare element usage, no `Style=`). Confirm zero diff on
`Controls/EtherDropdown.cs` — run `git diff` on it if you touched the working tree at all, it
must show nothing.

- [ ] **Step 7: Commit**

**IMPORTANT — commit hygiene:** Create ONE commit with only the 3 intended file changes
(`Controls/EtherDropdown.xaml` created, `App.xaml` modified, `Resources/EtherDropdown.xaml`
deleted). Do NOT touch `Controls/EtherDropdown.cs`. Do NOT amend any existing commit.

```bash
git add Controls/EtherDropdown.xaml App.xaml
git rm Resources/EtherDropdown.xaml
git commit -m "$(cat <<'EOF'
Relocate EtherDropdown template/style dictionary into Controls/

Phase 3 of the self-contained component authoring plan. Pure
relocation - Controls/EtherDropdown.cs (the hard-won ComboBox
subclass with its template-part access, content-hugging width, and
popup positioning fix) is completely untouched. The implicit style on
controls:EtherDropdown and the keyed EtherDropdownItem style both keep
their existing keying exactly as before. DropdownPage.xaml uses bare
<controls:EtherDropdown> everywhere, so no consumer changes needed.

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
EOF
)"
```

---

## Task 2: Verification sweep and retrospective

**Files:**
- Modify: `docs/superpowers/plans/2026-08-06-self-contained-component-authoring-plan.md`
  (Phase 3 status)

- [ ] **Step 1: Full sweep**

Confirm `dotnet build` succeeds cleanly. Grep the whole repo for `Resources/EtherDropdown`
(excluding `obj`/`bin`) — zero hits expected outside build cache. Explicitly confirm
`Controls/EtherDropdown.cs` has zero diff across this entire phase (`git diff` against the
commit before Task 1, or `git log -p -- Controls/EtherDropdown.cs` since the phase started —
should show nothing).

- [ ] **Step 2: Update the parent plan's Phase 3 section**

Add a "Status: done" note to Phase 3 in
`docs/superpowers/plans/2026-08-06-self-contained-component-authoring-plan.md`, recording the
commit and confirming `Controls/EtherDropdown.cs`'s internals were never touched — this is the
specific promise that phase made (treat this as pure relocation, not a rewrite) and worth
recording that it held.

- [ ] **Step 3: Commit the plan update**

```bash
git add docs/superpowers/plans/2026-08-06-self-contained-component-authoring-plan.md
git commit -m "$(cat <<'EOF'
Phase 3 retrospective: record status

Dropdown is relocated. Confirmed Controls/EtherDropdown.cs was never
touched across the phase - the hard-won template-part access,
content-hugging width, and popup positioning logic are all intact.

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
EOF
)"
```

- [ ] **Step 4: Report status to the user**

Summarize what was done, confirm zero consumer impact and zero `.cs` impact, state readiness
for Phase 4 (Segmented Control).
