# Phase 2 Pilot: Checkbox, Radio Button, Intelligence Button Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Relocate Checkbox, Radio Button, and Intelligence Button's templates into `Controls/`,
completing the "Updated"-tagged component list minus Scroll Bar (permanently out of scope) and
Label/Filter Chip (not Updated-tagged, deferred).

**Architecture — simplification from the parent plan's original Phase 2 description:** The
parent plan (`docs/superpowers/plans/2026-08-06-self-contained-component-authoring-plan.md`)
originally called for introducing new subclasses (`EtherCheckbox : CheckBox`,
`EtherRadioButton : RadioButton`) for this batch. That made sense under the original
`DefaultStyleKey`/`Generic.xaml` mechanism, but Phase 0 proved a simpler mechanism works fine:
explicit merge + keyed style, **no subclass at all**, exactly like Toggle Switch. Inspection for
this phase confirms all three components fit that same pattern with zero new behavior needed:

- **Checkbox** — style is keyed (`EtherCheckbox`) targeting the stock `CheckBox` type. Consumed
  by `CheckboxPage.xaml`, but also `Views/ComponentPage.xaml:78` (shared chrome!) and
  `Views/Controls/ButtonPage.xaml:18`. No dedicated class exists or is needed.
- **Radio Button** — style is keyed (`EtherRadioButton`) targeting the stock `RadioButton` type.
  Only consumed by `RadioButtonPage.xaml` today. No dedicated class exists or is needed —
  `RadioButtonPage.xaml` already uses plain `<RadioButton GroupName="Interactive" .../>`, so
  parent/`GroupName`-based grouping is untouched by a pure relocation (nothing about the type
  changes).
- **Intelligence Button** — style is keyed (`EtherIntelligenceButton`) targeting the stock
  `Button` type (not even a subclass — the demo page applies it to `controls:HandButton`, the
  already-existing shared cursor helper in `Controls/HandButton.cs`, which stays put and is
  **not** touched by this phase).

So Phase 2 is now three independent, low-risk relocations — the exact same mechanic proven
twice already in Phase 0 (Progress Bar, Toggle Switch) and once in Phase 1 (Button's 6 keys):
move the `ResourceDictionary` content into `Controls/`, repoint the single `App.xaml` merge
line, delete the old file. All three styles **stay keyed** (none becomes implicit) since all
three target stock framework types — an implicit style on `CheckBox`/`RadioButton`/`Button`
would silently restyle every bare instance of that type anywhere else in the app, the exact
mistake the parent plan's `EtherScrollBar` finding warned against.

**Bonus fix, Intelligence Button only (re-verified via pure codepoint inspection, no character
printing):** the mojibake is real. It was misdiagnosed twice during planning before being
pinned down correctly — worth recording both false turns since they reveal a real tooling trap:
printing non-ASCII characters through this session's Bash tool can silently mangle or drop them
(confirmed independently on `Controls/EtherButton.xaml`'s already-correct `·`, which printed as
a replacement character through the same pipeline despite being valid). The only reliable check
turned out to be comparing `ord()`/hex codepoint values directly, with no character ever
printed to a terminal. Under that method:

- **4 locations** (originally reported as line 2, 35, 36, 38) contain the two-codepoint sequence
  U+00C2 + U+00B7 ("Â" immediately followed by "·") where a single U+00B7 ("·") belongs — the
  classic mojibake signature of UTF-8-encoded "·" (bytes `C2 B7`) misread as Windows-1252 and
  re-encoded.
- **2 locations** (line 28, 37) contain U+00C3 + U+2014 ("Ã" immediately followed by an em dash)
  where a single U+00D7 ("×") belongs — the same mojibake pattern for "×" (UTF-8 bytes `C3 97`;
  byte `97` is em dash in Windows-1252 specifically, confirming the misinterpretation codepage).
- **Line 3** (the decorative row under the title) is genuinely corrupted beyond recovery —
  repeating `U+00E2, U+2022, U+0090` where U+0090 is a C1 control byte, not valid intentional
  text. Replaced with a plain ASCII `=` row matching `Controls/EtherButton.xaml`'s header
  convention (same visual width as the title line) — a safe normalization, not a guess at
  restoring unrecoverable original content.

Fixed programmatically (Python file I/O, reading and writing bytes directly — no character ever
passed through a terminal print) rather than by hand-editing, specifically to avoid the same
transcription risk that caused the two earlier misdiagnoses. Verified after the fact: zero
remaining instances of either mojibake pattern, and the entire `ControlTemplate`/`Style` body
(152 lines) byte-identical to the original.

**Tech Stack:** WinUI3 / .NET 8, unpackaged WinExe. No test framework — verification is
`dotnet build` + diff-equivalence check + grep exit gate, per established convention. No GUI
verification available in this environment.

---

## Task 1: Relocate Checkbox's style into `Controls/`

**Files:**
- Create: `Controls/EtherCheckbox.xaml`
- Modify: `App.xaml` (merge source path, currently `Resources/EtherCheckbox.xaml`)
- Delete: `Resources/EtherCheckbox.xaml`

**No changes to any consumer file** — not `Views/Controls/CheckboxPage.xaml`, not
`Views/ComponentPage.xaml`, not `Views/Controls/ButtonPage.xaml`. The style key
(`EtherCheckbox`) is preserved verbatim.

- [ ] **Step 1: Create `Controls/EtherCheckbox.xaml`**

Read the current file at `Resources/EtherCheckbox.xaml` (~204 lines) and copy its FULL content
verbatim: all `SolidColorBrush` resources (`CheckboxFillDefault` through
`CheckboxDisabledGlyph`), the `ControlTemplate x:Key="EtherCheckboxTemplate"` with its
`CommonStates`/`CheckStates` visual state groups and every named element, and the
`Style x:Key="EtherCheckbox"` — nothing dropped, nothing made implicit (it targets the stock
`CheckBox` type). The only change: insert this block into the existing header comment,
immediately after the existing description/STATES/colour-note content and before the closing
`-->`:

```
    LOCATION
      This dictionary lives beside its class... wait, Checkbox has no dedicated class today
      (it styles the stock CheckBox type directly) — this file is merged explicitly from
      App.xaml. See the "Mechanism refinement" note in
      docs/superpowers/plans/2026-08-06-self-contained-component-authoring-plan.md.
```

(Adjust the wording slightly if it reads awkwardly given there's no `.cs` to reference — the
point is to note the explicit-merge mechanism, matching the convention from
`Controls/EtherSwitch.xaml`'s own header, which has the same "no dedicated class" situation.)

- [ ] **Step 2: Repoint the `App.xaml` merge**

Change `<ResourceDictionary Source="Resources/EtherCheckbox.xaml"/>` to
`<ResourceDictionary Source="Controls/EtherCheckbox.xaml"/>`, same position in the merge list.

- [ ] **Step 3: Delete the old file**

`git rm Resources/EtherCheckbox.xaml`

- [ ] **Step 4: Build**

`dotnet build EtherComponentSandbox.csproj -c Debug` — expect 0 errors.

- [ ] **Step 5: Diff-equivalence check**

Confirm `Controls/EtherCheckbox.xaml`'s resources/template/style are character-for-character
identical to the deleted file, aside from the LOCATION addition. No GUI verification available
— note the limitation.

- [ ] **Step 6: Grep exit gate**

Grep the repo (excluding `obj`/`bin`) for `EtherCheckbox` — every hit should be
`Controls/EtherCheckbox.xaml`, `App.xaml`, or one of the three known consumers
(`CheckboxPage.xaml`, `ComponentPage.xaml`, `ButtonPage.xaml`). No reference to the old
`Resources/` path should remain.

- [ ] **Step 7: Commit**

```bash
git add Controls/EtherCheckbox.xaml App.xaml
git rm Resources/EtherCheckbox.xaml
git commit -m "$(cat <<'EOF'
Relocate EtherCheckbox style into Controls/

Phase 2 of the self-contained component authoring plan. No dedicated
class needed - stays a keyed style on the stock CheckBox type, same
pattern as Toggle Switch in Phase 0. Consumers (CheckboxPage.xaml,
the shared ComponentPage.xaml chrome, ButtonPage.xaml) are untouched.

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
EOF
)"
```

---

## Task 2: Relocate Radio Button's style into `Controls/`

**Files:**
- Create: `Controls/EtherRadioButton.xaml`
- Modify: `App.xaml` (merge source path, currently `Resources/EtherRadioButton.xaml`)
- Delete: `Resources/EtherRadioButton.xaml`

**No changes to any consumer file** — `Views/Controls/RadioButtonPage.xaml` is untouched; its
`GroupName="Interactive"` grouping and `IsChecked` bindings are unaffected since the
`RadioButton` type itself never changes.

- [ ] **Step 1: Create `Controls/EtherRadioButton.xaml`**

Copy the full content of `Resources/EtherRadioButton.xaml` (~203 lines) verbatim — all
`SolidColorBrush` resources, the `ControlTemplate x:Key="EtherRadioButtonTemplate"` with its
visual state groups, and `Style x:Key="EtherRadioButton"` (stays keyed, targets stock
`RadioButton`). Add the same LOCATION-style header note as Task 1 (adapted: "no dedicated class
— styles the stock RadioButton type directly").

- [ ] **Step 2: Repoint the `App.xaml` merge**

`Source="Resources/EtherRadioButton.xaml"` → `Source="Controls/EtherRadioButton.xaml"`, same
position.

- [ ] **Step 3: Delete the old file**

`git rm Resources/EtherRadioButton.xaml`

- [ ] **Step 4: Build**

`dotnet build EtherComponentSandbox.csproj -c Debug` — expect 0 errors.

- [ ] **Step 5: Diff-equivalence check**

Character-for-character identical to the deleted file aside from the header addition.

- [ ] **Step 6: Grep exit gate**

Grep for `EtherRadioButton` — every hit should be `Controls/EtherRadioButton.xaml`, `App.xaml`,
or `RadioButtonPage.xaml`.

- [ ] **Step 7: Commit**

```bash
git add Controls/EtherRadioButton.xaml App.xaml
git rm Resources/EtherRadioButton.xaml
git commit -m "$(cat <<'EOF'
Relocate EtherRadioButton style into Controls/

Phase 2 continued. No dedicated class needed - stays a keyed style on
the stock RadioButton type. GroupName-based grouping in
RadioButtonPage.xaml is untouched since the type itself never changes.

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
EOF
)"
```

---

## Task 3: Relocate Intelligence Button's style into `Controls/`, fix garbled header text

**Files:**
- Create: `Controls/EtherIntelligenceButton.xaml`
- Modify: `App.xaml` (merge source path, currently `Resources/EtherIntelligenceButton.xaml`)
- Delete: `Resources/EtherIntelligenceButton.xaml`

**No changes to any consumer file** — `Views/Controls/IntelligenceButtonPage.xaml` (which
applies the style to `controls:HandButton`, not touched) and `Resources/EtherIntelligenceButton.xaml`'s
own USAGE-comment example are unaffected in terms of the style key. `Controls/HandButton.cs`
itself is not touched by this task.

- [ ] **Step 1: Create `Controls/EtherIntelligenceButton.xaml`**

Copy the full content of `Resources/EtherIntelligenceButton.xaml` (~197 lines), with these
corrections to the header comment (everything else — all resources, the `ControlTemplate`, the
`Style x:Key="EtherIntelligenceButton"` — copied verbatim, unchanged):

1. Fix the ONE genuine defect — line 3, the decorative row under the title (currently a string
   of what displays as `â•â•â•â•...`, but is actually invalid/corrupted bytes, confirmed by
   reading the file in binary and decoding as UTF-8: it contains real garbage, not a clean
   reversible mojibake). Replace that entire line with a plain ASCII `=` row matching
   `Controls/EtherButton.xaml`'s header convention — count characters so the row is the same
   visual width as the title line above it (line 2). Do not try to guess or "restore" what the
   original decorative character was meant to be; a plain `=` row is a safe normalization of
   confirmed-corrupted content, matching established repo convention.
2. **Leave everything else in the header exactly as-is.** A first-pass read made lines like
   "Ether Design System Â· Intelligence Action Button" and "24Ã—24" look corrupted, but a
   byte-level check (open the file in binary mode and decode as UTF-8) proved these are
   **already valid, correct characters** — `·` (middle dot, U+00B7) and `×` (multiplication
   sign, U+00D7) — the earlier garbled appearance was a rendering artifact of one tool's
   display, not a defect in the file's actual bytes. Do not "fix" anything on these lines; if
   you independently re-verify by reading the raw bytes and find something that genuinely looks
   corrupted beyond line 3, stop and report it with your byte-level evidence rather than editing
   based on how the text merely displays in a tool.
3. Add the LOCATION note (same pattern as Tasks 1–2: no dedicated class, explicit merge from
   `App.xaml`, points at the parent plan's "Mechanism refinement" section).

- [ ] **Step 2: Repoint the `App.xaml` merge**

`Source="Resources/EtherIntelligenceButton.xaml"` → `Source="Controls/EtherIntelligenceButton.xaml"`,
same position.

- [ ] **Step 3: Delete the old file**

`git rm Resources/EtherIntelligenceButton.xaml`

- [ ] **Step 4: Build**

`dotnet build EtherComponentSandbox.csproj -c Debug` — expect 0 errors.

- [ ] **Step 5: Diff-equivalence check**

Confirm the `ControlTemplate` and `Style` bodies (everything except the header comment) are
character-for-character identical to the deleted file. For the header, confirm: (a) line 3 was
replaced with a plain `=` row, (b) every other line is byte-identical to the original — verify
by codepoint (e.g. read both files in binary and decode as UTF-8, don't rely on visual
inspection through a tool that may mis-render), and (c) the LOCATION block was added correctly.

- [ ] **Step 6: Grep exit gate**

Grep for `EtherIntelligenceButton` — every hit should be
`Controls/EtherIntelligenceButton.xaml`, `App.xaml`, or `IntelligenceButtonPage.xaml`.

- [ ] **Step 7: Commit**

```bash
git add Controls/EtherIntelligenceButton.xaml App.xaml
git rm Resources/EtherIntelligenceButton.xaml
git commit -m "$(cat <<'EOF'
Relocate EtherIntelligenceButton style into Controls/, fix garbled header text

Phase 2 continued. No dedicated class needed - stays a keyed style on
the stock Button type, applied via the existing shared HandButton
helper (unchanged). Also fixes line 3 of the header comment, a
genuinely corrupted decorative separator row (confirmed via
byte-level inspection, not just visual rendering) - replaced with a
plain ASCII = row matching EtherButton.xaml's header convention.
Everything else in the header was already valid UTF-8 despite
initially appearing garbled in one tool's display; left untouched.

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
EOF
)"
```

---

## Task 4: Verification sweep and retrospective

**Files:**
- Modify: `docs/superpowers/plans/2026-08-06-self-contained-component-authoring-plan.md`
  (Phase 2 status + simplification note)

- [ ] **Step 1: Full sweep**

Confirm `dotnet build` succeeds cleanly. Grep the whole repo for `Resources/EtherCheckbox`,
`Resources/EtherRadioButton`, `Resources/EtherIntelligenceButton` (excluding `obj`/`bin`) —
zero hits expected outside build cache.

- [ ] **Step 2: Update the parent plan's Phase 2 section**

Add a "Status: done" note to Phase 2 in
`docs/superpowers/plans/2026-08-06-self-contained-component-authoring-plan.md`, recording: the
simplification (no new subclasses needed — all three stayed keyed styles on stock types,
mirroring Toggle Switch), the commits, and the Intelligence Button mojibake fix as an
incidental correction. Also note that Label/Filter Chip (grouped with this batch in the
original Phase 2 description) was deliberately deferred — it isn't "Updated"-tagged and the
user's instruction scoped this round to Updated-tagged components only.

- [ ] **Step 3: Commit the plan update**

```bash
git add docs/superpowers/plans/2026-08-06-self-contained-component-authoring-plan.md
git commit -m "$(cat <<'EOF'
Phase 2 retrospective: record status, simplification, Label/Filter Chip deferral

Checkbox, Radio Button, and Intelligence Button are relocated. None
needed a new subclass - all three work as pure keyed-style relocations
identical to Toggle Switch's Phase 0 pattern, since no new behavior
was required. Label/Filter Chip deliberately deferred (not
Updated-tagged, out of this round's scope per user instruction).

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
EOF
)"
```

- [ ] **Step 4: Report status to the user**

Summarize what was done, confirm zero consumer impact across all three components, note the
Intelligence Button text fix, and state readiness for Phase 3 (Dropdown).
