# Phase 7 Pilot: Scroll Bar Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Relocate Scroll Bar's style dictionary into `Controls/`, completing the full
Updated-tagged component sweep.

**Architecture — why this phase exists after Scroll Bar was originally excluded:** the parent
plan's original Phase 6 section said Scroll Bar should be permanently excluded from this
migration, on the reasoning that `Resources/EtherScrollBar.xaml:73` is an implicit
`<Style TargetType="ScrollBar">` applied app-wide, so "making it component-local is
meaningless." The user correctly pushed back: that conflated two separate questions —
**where the file lives** (the actual subject of this whole plan: does this component have one
obvious owning location an engineer can find and hand off) versus **whether the style stays
implicit** (a behavior/keying question, already handled correctly elsewhere in this plan without
needing to exclude a file from relocation — `Controls/EtherDropdown.xaml`'s implicit style moved
in Phase 3 and stayed implicit; nothing about relocating a file requires changing its keying).
Scroll Bar's thumb/track visuals are a genuine custom-authored design-system component (WinUI's
stock scrollbar looks nothing like this — see the file's own header comment), not an OS default
being incidentally overridden. It deserves the same "one file, own folder" treatment as every
other component in this plan.

**This phase relocates the file and changes nothing about its behavior.** The style stays
**implicit** (`TargetType="ScrollBar"`, no `x:Key`) exactly as it is today — this is correct,
intentional, load-bearing behavior (every `ScrollViewer` in the app, including ones added
later, should automatically get the custom scrollbar with zero per-page setup), not something
this phase should "fix" into a keyed style. The only change is which folder the file lives in
and one App.xaml merge line.

**No consumer file changes needed:** grep confirms exactly 2 files mention `EtherScrollBar`
outside `App.xaml` and the dictionary itself — `Controls/EtherDropdown.xaml:232` ("Scroll bar
styling now lives in the app-wide EtherScrollBar.xaml") and
`Views/Foundations/ScrollBarPage.xaml:8` ("The global ScrollBar style in
Resources/EtherScrollBar.xaml applies implicitly...") — both are **descriptive comments**, not
live `StaticResource`/`Style=` references. Since the style is implicit, there is no keyed
reference anywhere in the codebase to begin with — every `ScrollViewer` picks it up
automatically regardless of which file it's merged from. Both comments should be updated to
reference the new path as part of this task (they're documentation, not code, but stale path
references in comments are exactly the kind of thing this whole plan exists to clean up).

**Tech Stack:** WinUI3 / .NET 8, unpackaged WinExe. No test framework — verification is
`dotnet build` + diff-equivalence check + grep exit gate. No GUI verification available.

---

## Task 1: Relocate Scroll Bar's style dictionary into `Controls/`

**Files:**
- Create: `Controls/EtherScrollBar.xaml`
- Modify: `App.xaml` (merge source path, currently `Resources/EtherScrollBar.xaml`)
- Modify: `Controls/EtherDropdown.xaml` (comment only, line 232 — update path reference)
- Modify: `Views/Foundations/ScrollBarPage.xaml` (comment only, line 8 — update path reference)
- Delete: `Resources/EtherScrollBar.xaml`

**No changes to any other file.** No `.cs` class exists for this component and none is
introduced.

- [ ] **Step 1: Create `Controls/EtherScrollBar.xaml`**

Read the current file at `Resources/EtherScrollBar.xaml` (~173 lines) and copy its FULL content
verbatim: the `SolidColorBrush x:Key="EtherScrollThumbHover"` resource, the
`ControlTemplate x:Key="EtherScrollBarRepeatButtonTemplate"`, the
`ControlTemplate x:Key="EtherScrollBarThumbTemplate"` (with its `CommonStates` visual state
group — `Normal`/`PointerOver`/`Pressed`/`Disabled` — and named element `ThumbFill`), and the
final **implicit** `Style TargetType="ScrollBar"` (no `x:Key` — this MUST stay implicit, do not
add a key) with its full vertical/horizontal `ControlTemplate` (named elements `VerticalRoot`,
`VerticalSmallDecrease`, `VerticalLargeDecrease`, `VerticalThumb`, `VerticalLargeIncrease`,
`VerticalSmallIncrease`, and the mirrored `Horizontal*` set). Nothing dropped, nothing re-keyed.

The ONLY change: insert this block into the existing header comment, right before the closing
`-->`:

```
    LOCATION
      No dedicated class today — this file styles the stock ScrollBar type directly (as an
      implicit, app-wide style — see above) and is merged explicitly from App.xaml. See the
      "Mechanism refinement" note in
      docs/superpowers/plans/2026-08-06-self-contained-component-authoring-plan.md.
```

Everything else must be copied exactly as-is.

- [ ] **Step 2: Repoint the `App.xaml` merge**

Find `<ResourceDictionary Source="Resources/EtherScrollBar.xaml"/>` and change it to
`<ResourceDictionary Source="Controls/EtherScrollBar.xaml"/>`. Keep it in the exact same
position in the merge list.

- [ ] **Step 3: Update the two comment references**

In `Controls/EtherDropdown.xaml`, find the comment at (approximately) line 232:
```xml
<!-- Scroll bar styling now lives in the app-wide EtherScrollBar.xaml. -->
```
Change to:
```xml
<!-- Scroll bar styling now lives in the app-wide Controls/EtherScrollBar.xaml. -->
```

In `Views/Foundations/ScrollBarPage.xaml`, find the comment at (approximately) line 8:
```xml
The global ScrollBar style in Resources/EtherScrollBar.xaml applies implicitly
```
Change to:
```xml
The global ScrollBar style in Controls/EtherScrollBar.xaml applies implicitly
```
Change only the path text — do not alter surrounding words, punctuation, or the rest of either
comment.

- [ ] **Step 4: Delete the old file**

`git rm Resources/EtherScrollBar.xaml`

- [ ] **Step 5: Build**

`dotnet build EtherComponentSandbox.csproj -c Debug` — expect 0 errors.

- [ ] **Step 6: Diff-equivalence check**

Confirm the resources/templates/style in `Controls/EtherScrollBar.xaml` are
character-for-character identical to the deleted file, other than the added LOCATION comment
block. Specifically confirm the final `Style TargetType="ScrollBar"` still has NO `x:Key`
(stayed implicit) — this is the detail most likely to get "fixed" by mistake, since every other
component this session that targets a stock framework type ended up keyed, and an implementer
unfamiliar with this specific file's intentional design could reasonably but wrongly assume this
one should follow that same pattern. It should NOT — Scroll Bar is the one exception, and it's
deliberate. Do NOT claim GUI visual verification — note the limitation in your report instead.

- [ ] **Step 7: Grep exit gate**

Repo-wide grep for `EtherScrollBar` (excluding `obj/`/`bin/`). Every hit should be one of:
`Controls/EtherScrollBar.xaml`, `App.xaml`, `Controls/EtherDropdown.xaml` (comment, now updated),
`Views/Foundations/ScrollBarPage.xaml` (comment, now updated). If you find a reference to the
old `Resources/EtherScrollBar.xaml` path anywhere outside build output, STOP and report it.

- [ ] **Step 8: Commit**

**IMPORTANT — commit hygiene:** Create ONE commit with only the 5 intended file changes. Do NOT
amend any existing commit.

```bash
git add Controls/EtherScrollBar.xaml App.xaml Controls/EtherDropdown.xaml Views/Foundations/ScrollBarPage.xaml
git rm Resources/EtherScrollBar.xaml
git commit -m "$(cat <<'EOF'
Relocate EtherScrollBar style into Controls/

Phase 7 - added after the user correctly pointed out that the
original plan's Scroll Bar exclusion conflated "where the file lives"
with "whether the style stays implicit". The style stays implicit and
app-wide-applying exactly as before (that part was correct and
intentional); only the file's location changes. No dedicated class
needed. Updated two stale path references in comments
(EtherDropdown.xaml, ScrollBarPage.xaml) found during relocation.

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
EOF
)"
```

---

## Task 2: Verification sweep and retrospective

**Files:**
- Modify: `docs/superpowers/plans/2026-08-06-self-contained-component-authoring-plan.md`
  (Phase 7 status, closing summary table update)

- [ ] **Step 1: Full sweep**

Confirm `dotnet build` succeeds cleanly. Grep the whole repo for `Resources/EtherScrollBar`
(excluding `obj`/`bin`) — zero hits expected outside build cache. Navigate (via grep, since no
GUI) every page reachable from `ComponentCatalog.Nodes` to confirm none of them reference the
old path — since the style is implicit and applies to every `ScrollViewer`, this is really a
check that no page's demo content assumed the old explicit path anywhere (none should, but
confirm).

- [ ] **Step 2: Update the parent plan's closing summary**

In `docs/superpowers/plans/2026-08-06-self-contained-component-authoring-plan.md`'s closing
summary table, add a row for Scroll Bar (Phase 7, commit reference, "implicit style stays
implicit — file relocation only"). Remove Scroll Bar from any remaining "permanently out of
scope" language if any survived the earlier correction commit.

- [ ] **Step 3: Commit the plan update**

```bash
git add docs/superpowers/plans/2026-08-06-self-contained-component-authoring-plan.md
git commit -m "$(cat <<'EOF'
Phase 7 retrospective: record status, update closing summary

Scroll Bar is relocated. The full Updated-tagged component list is
now complete - all 11 components (Progress Bar, Toggle Switch, Button,
Checkbox, Radio Button, Intelligence Button, Dropdown, Segmented
Control, Input, Slider, Scroll Bar) live in Controls/.

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
EOF
)"
```

- [ ] **Step 4: Report status to the user**

Summarize what was done, confirm zero behavior change (style still implicit, still app-wide),
confirm the two comment updates, and give the final closing state: every Updated-tagged
component is now migrated with no exceptions.
