# Phase 4 Pilot: Segmented Control Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Relocate Segmented Control's style dictionary into `Controls/`, completing Phase 4.

**Architecture:** `Controls/EtherSegmentedTrack.cs` (a `ContentControl` subclass that renders
Composition-layer drop shadows behind the track surface — `ShadowHost`/`TrackSurface` template
parts, `InitializeShadows`/`UpdateShadowSize`) already exists and is **not touched** by this
task — pure XAML relocation, same as Dropdown in Phase 3. `Resources/EtherSegmentedControl.xaml`
defines two keyed styles: `EtherSegmentedTrack` (targets stock `ContentControl` — must stay
keyed, an implicit style on `ContentControl` would be catastrophic, that's an extremely common
base type) and `EtherSegment` (targets stock `RadioButton` — must also stay keyed, for the same
reason established in Phase 2's Radio Button task). Neither becomes implicit; neither loses its
key. This file was checked byte-level for encoding corruption during planning and found
**completely clean** — no mojibake, unlike Intelligence Button in Phase 2 — its `═` decorative
separators are legitimate, correctly-encoded box-drawing characters (U+2550), confirmed via
codepoint inspection, not visual read. No text fix needed this phase.

**No consumer file changes needed:** `Views/Controls/SegmentedControlPage.xaml` is the only real
consumer, referencing both styles by key (`Style="{StaticResource EtherSegmentedTrack}"` /
`Style="{StaticResource EtherSegment}"`) — unaffected by which file the dictionary lives in.
`SegmentedControlPage.xaml.cs` drives 2 STATES swatches via `VisualStateManager.GoToState`
(`SegmentHoverState` → `PointerOver`, `SegmentPressedState` → `Pressed`) — untouched, since
neither the `RadioButton` type nor the template's `VisualStateGroups` change.
`Controls/EtherRightPanel.xaml` has a comment referencing `EtherSegment` (confirmed in an
earlier phase's consumer-inventory correction — not a live `StaticResource` use), so it needs no
changes either, but re-confirm this hasn't changed since that check.

**Tech Stack:** WinUI3 / .NET 8, unpackaged WinExe. No test framework — verification is
`dotnet build` + diff-equivalence check + grep exit gate. No GUI verification available.

---

## Task 1: Relocate Segmented Control's style dictionary into `Controls/`

**Files:**
- Create: `Controls/EtherSegmentedControl.xaml`
- Modify: `App.xaml` (merge source path, currently `Resources/EtherSegmentedControl.xaml`)
- Delete: `Resources/EtherSegmentedControl.xaml`

**No changes to:** `Controls/EtherSegmentedTrack.cs`, `Views/Controls/SegmentedControlPage.xaml`,
`Views/Controls/SegmentedControlPage.xaml.cs`.

- [ ] **Step 1: Create `Controls/EtherSegmentedControl.xaml`**

Read the current file at `Resources/EtherSegmentedControl.xaml` (~174 lines) and copy its FULL
content verbatim: the `ControlTemplate x:Key="EtherSegmentedTrackTemplate"` (with the
`ShadowHost`/`TrackSurface` named elements the `.cs` class looks up via `GetTemplateChild`), the
`Style x:Key="EtherSegmentedTrack"` (stays keyed), the `ControlTemplate x:Key="EtherSegmentTemplate"`
(with its `CommonStates`/`FocusStates` visual state groups — note the `Checked`/
`CheckedPointerOver`/`CheckedPressed`/`Indeterminate` states specific to this template, and every
named element: `Bg`, `Cp`, `FocusRing`), the `Style x:Key="EtherSegment"` (stays keyed), and the
"READY-TO-USE EXAMPLE STRUCTURE" comment block at the end (lines 156–171, including its own
`═══` box-drawing separators — these are legitimate characters, confirmed clean via codepoint
check during planning; copy them verbatim, do not alter them). Nothing dropped, nothing re-keyed.

The ONLY change: insert this block into the existing header comment, right before the closing
`-->`:

```
    LOCATION
      This dictionary lives beside its class (Controls/EtherSegmentedTrack.cs) and is merged
      explicitly from App.xaml. See the "Mechanism refinement" note in
      docs/superpowers/plans/2026-08-06-self-contained-component-authoring-plan.md.
```

Everything else must be copied exactly as-is. **Do not "fix" the `═` separator characters** —
they were verified clean during planning (single codepoint U+2550 repeated, no mojibake) and
copying them via your normal file-read/file-write path is safe; only the earlier
`EtherIntelligenceButton.xaml` task involved genuinely corrupted text, this file does not.

- [ ] **Step 2: Repoint the `App.xaml` merge**

Find `<ResourceDictionary Source="Resources/EtherSegmentedControl.xaml"/>` and change it to
`<ResourceDictionary Source="Controls/EtherSegmentedControl.xaml"/>`. Keep it in the exact same
position in the merge list.

- [ ] **Step 3: Delete the old file**

`git rm Resources/EtherSegmentedControl.xaml`

- [ ] **Step 4: Build**

`dotnet build EtherComponentSandbox.csproj -c Debug` — expect 0 errors.

- [ ] **Step 5: Diff-equivalence check**

Confirm the templates/styles in `Controls/EtherSegmentedControl.xaml` are character-for-character
identical to the deleted file, other than the added LOCATION comment block. Confirm both
`x:Key="EtherSegmentedTrack"` and `x:Key="EtherSegment"` are present and unchanged (both stay
keyed — this file has no implicit style to worry about, unlike Dropdown). Do NOT claim GUI
visual verification — note the limitation in your report instead.

- [ ] **Step 6: Grep exit gate**

Repo-wide grep for `EtherSegmentedTrack` and `EtherSegment\b` (excluding `obj/`/`bin/`). Every
hit should be one of: `Controls/EtherSegmentedTrack.cs`, `Controls/EtherSegmentedControl.xaml`,
`App.xaml`, `Views/Controls/SegmentedControlPage.xaml`, and the known comment-only reference in
`Controls/EtherRightPanel.xaml` (confirm it's still just a comment, not a live `StaticResource`
use — if it has become live, STOP and report it rather than silently proceeding). If you find a
reference to the old `Resources/EtherSegmentedControl.xaml` path anywhere outside build output,
STOP and report it. Also run `git diff -- Controls/EtherSegmentedTrack.cs` and confirm it shows
nothing.

- [ ] **Step 7: Commit**

**IMPORTANT — commit hygiene:** Create ONE commit with only the 3 intended file changes. Do NOT
touch `Controls/EtherSegmentedTrack.cs`. Do NOT amend any existing commit.

```bash
git add Controls/EtherSegmentedControl.xaml App.xaml
git rm Resources/EtherSegmentedControl.xaml
git commit -m "$(cat <<'EOF'
Relocate EtherSegmentedControl style dictionary into Controls/

Phase 4 of the self-contained component authoring plan. Pure
relocation - Controls/EtherSegmentedTrack.cs (the ContentControl
subclass rendering Composition-layer drop shadows) is completely
untouched. Both EtherSegmentedTrack (targets stock ContentControl) and
EtherSegment (targets stock RadioButton) stay keyed, as they must -
neither targets a custom type. SegmentedControlPage.xaml references
both by key already, so no consumer changes needed. Verified the
file's decorative box-drawing separators are legitimate, correctly
encoded characters, not mojibake.

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
EOF
)"
```

---

## Task 2: Verification sweep and retrospective

**Files:**
- Modify: `docs/superpowers/plans/2026-08-06-self-contained-component-authoring-plan.md`
  (Phase 4 status)

- [ ] **Step 1: Full sweep**

Confirm `dotnet build` succeeds cleanly. Grep the whole repo for `Resources/EtherSegmentedControl`
(excluding `obj`/`bin`) — zero hits expected outside build cache. Explicitly confirm
`Controls/EtherSegmentedTrack.cs` has zero diff across this entire phase.

- [ ] **Step 2: Update the parent plan's Phase 4 section**

Add a "Status: done" note to Phase 4 in
`docs/superpowers/plans/2026-08-06-self-contained-component-authoring-plan.md`, recording the
commit and confirming `Controls/EtherSegmentedTrack.cs`'s Composition-shadow logic was never
touched.

- [ ] **Step 3: Commit the plan update**

```bash
git add docs/superpowers/plans/2026-08-06-self-contained-component-authoring-plan.md
git commit -m "$(cat <<'EOF'
Phase 4 retrospective: record status

Segmented Control is relocated. Confirmed
Controls/EtherSegmentedTrack.cs (Composition-layer shadow rendering)
was never touched across the phase.

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
EOF
)"
```

- [ ] **Step 4: Report status to the user**

Summarize what was done, confirm zero consumer impact and zero `.cs` impact, state readiness
for Phase 5 (Input).
