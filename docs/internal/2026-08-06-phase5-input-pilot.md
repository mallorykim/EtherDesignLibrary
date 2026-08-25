# Phase 5 Pilot: Input Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Relocate Input's style dictionary into `Controls/`, completing Phase 5.

**Architecture — simplification from the parent plan's original Phase 5 description:** The
parent plan worried Input carries "the largest forwarding surface of any control in scope:
`Text`, `PlaceholderText`, `SelectionStart`/`Length`, `MaxLength`, `InputScope`, `TextChanged`,
`Header`, focus routing, and its automation peer." That concern assumed a new subclass would be
needed (the same assumption Phase 2 made and then found unnecessary for Checkbox/Radio
Button/Intelligence Button). Inspection confirms Input has no dedicated class today either —
`Resources/EtherInput.xaml` is purely a keyed `Style x:Key="EtherInput" TargetType="TextBox"`
on the **stock** `TextBox` type. Since nothing wraps or subclasses `TextBox`, there is nothing
to forward — every consumer already gets 100% of `TextBox`'s native API (`Text`,
`PlaceholderText`, `SelectionStart`, `MaxLength`, `InputScope`, `TextChanged`, focus routing,
automation peer, all of it) for free, because it *is* a `TextBox`, not a wrapper around one.
This phase is therefore the same low-risk pure relocation as Checkbox/Radio Button/Intelligence
Button in Phase 2, not the higher-risk task the parent plan anticipated.

The style stays **keyed** (not implicit) since `TextBox` is a stock framework type — an
implicit style would silently restyle every bare `<TextBox>` anywhere else in the app.

**No consumer file changes needed:** `Views/Controls/InputPage.xaml` is the only consumer,
referencing the style by key (`Style="{StaticResource EtherInput}"` at all 6 `<TextBox>`
elements) — unaffected by which file the dictionary lives in. `InputPage.xaml.cs` drives 2
STATES swatches via `VisualStateManager.GoToState` (`HoverInput` → `PointerOver`, `ActiveInput`
→ `Focused`) — untouched, since the `TextBox` type and its template's `VisualStateGroups` don't
change.

**Tech Stack:** WinUI3 / .NET 8, unpackaged WinExe. No test framework — verification is
`dotnet build` + diff-equivalence check + grep exit gate. No GUI verification available.

---

## Task 1: Relocate Input's style dictionary into `Controls/`

**Files:**
- Create: `Controls/EtherInput.xaml`
- Modify: `App.xaml` (merge source path, currently `Resources/EtherInput.xaml`)
- Delete: `Resources/EtherInput.xaml`

**No changes to:** `Views/Controls/InputPage.xaml`, `Views/Controls/InputPage.xaml.cs`. No
dedicated `.cs` class exists for this component and none is introduced.

- [ ] **Step 1: Create `Controls/EtherInput.xaml`**

Read the current file at `Resources/EtherInput.xaml` (~154 lines) and copy its FULL content
verbatim: the 6 `SolidColorBrush` resources (`InputFillDefault`, `InputFillHover`,
`InputBorderDefault`, `InputBorderFocus`, `InputText`, `InputPlaceholder`, `InputSelection` —
7 total, recount from the actual file), the `ControlTemplate x:Key="EtherInputTemplate"` (with
its `CommonStates` visual state group — `Normal`/`PointerOver`/`Focused`/`Disabled` — and every
named element: `LayoutRoot`, `BorderElement`, `PlaceholderTextContentPresenter`,
`ContentElement`), and `Style x:Key="EtherInput"` — nothing dropped, nothing made implicit, the
`x:Key="EtherInput"` must be preserved exactly. Note this file has no top decorative
"USAGE"/measurement-table header block like some other relocated files — its header is a spec
table plus a state-mapping explanation; preserve that exact structure, don't restructure it to
match a different file's convention.

The ONLY change: insert this block into the existing header comment, right before the closing
`-->`:

```
    LOCATION
      No dedicated class today — this file styles the stock TextBox type directly and is
      merged explicitly from App.xaml. See the "Mechanism refinement" note in
      docs/superpowers/plans/2026-08-06-self-contained-component-authoring-plan.md.
```

Everything else must be copied exactly as-is.

- [ ] **Step 2: Repoint the `App.xaml` merge**

Find `<ResourceDictionary Source="Resources/EtherInput.xaml"/>` and change it to
`<ResourceDictionary Source="Controls/EtherInput.xaml"/>`. Keep it in the exact same position in
the merge list.

- [ ] **Step 3: Delete the old file**

`git rm Resources/EtherInput.xaml`

- [ ] **Step 4: Build**

`dotnet build EtherComponentSandbox.csproj -c Debug` — expect 0 errors.

- [ ] **Step 5: Diff-equivalence check**

Confirm the resources/`ControlTemplate`/`Style` content in `Controls/EtherInput.xaml` is
character-for-character identical to the deleted file, other than the added LOCATION comment
block. Do NOT claim GUI visual verification — note that limitation in your report instead.

- [ ] **Step 6: Grep exit gate**

Repo-wide grep for `EtherInput` (excluding `obj/`/`bin/`). Every hit should be one of:
`Controls/EtherInput.xaml`, `App.xaml`, `Views/Controls/InputPage.xaml`. Note: a plain-text
grep for `EtherInput` may also match unrelated substrings in other files (e.g. any comment
mentioning "input" generally) — check any such hits carefully, they are very likely false
positives, not real references. If you find a reference to the old
`Resources/EtherInput.xaml` path anywhere outside build output, STOP and report it.

- [ ] **Step 7: Commit**

**IMPORTANT — commit hygiene:** Create ONE commit with only the 3 intended file changes. Do NOT
amend any existing commit. If `git status` shows anything unexpected, STOP and report it.

```bash
git add Controls/EtherInput.xaml App.xaml
git rm Resources/EtherInput.xaml
git commit -m "$(cat <<'EOF'
Relocate EtherInput style into Controls/

Phase 5 of the self-contained component authoring plan. No dedicated
class needed - stays a keyed style on the stock TextBox type, same
pattern as Checkbox/Radio Button/Intelligence Button in Phase 2. Since
nothing wraps TextBox, there is no forwarding surface to worry about
(the parent plan's original concern about this phase assumed a
subclass would be needed - it isn't). InputPage.xaml is the only
consumer, referencing the style by key already, so no consumer changes
needed.

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
EOF
)"
```

---

## Task 2: Verification sweep and retrospective

**Files:**
- Modify: `docs/superpowers/plans/2026-08-06-self-contained-component-authoring-plan.md`
  (Phase 5 status + simplification note)

- [ ] **Step 1: Full sweep**

Confirm `dotnet build` succeeds cleanly. Grep the whole repo for `Resources/EtherInput`
(excluding `obj`/`bin`) — zero hits expected outside build cache.

- [ ] **Step 2: Update the parent plan's Phase 5 section**

Add a "Status: done, simplified" note to Phase 5 in
`docs/superpowers/plans/2026-08-06-self-contained-component-authoring-plan.md`, recording that
no subclass was needed (same finding pattern as Phase 2) and the commit reference.

- [ ] **Step 3: Commit the plan update**

```bash
git add docs/superpowers/plans/2026-08-06-self-contained-component-authoring-plan.md
git commit -m "$(cat <<'EOF'
Phase 5 retrospective: record status, correct forwarding-surface assumption

Input is relocated. Like Phase 2, no subclass was needed - Input is a
keyed style on the stock TextBox type, so the parent plan's "largest
forwarding surface" concern didn't apply: nothing wraps TextBox, so
there's nothing to forward.

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
EOF
)"
```

- [ ] **Step 4: Report status to the user**

Summarize what was done, confirm zero consumer impact, state readiness for Phase 6 (Slider —
this one needs an actual bug audit, not just relocation, per the parent plan's own findings
about hardcoded colors and a theme-toggle defect).
