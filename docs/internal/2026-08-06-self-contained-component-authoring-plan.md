# Self-contained component authoring plan

Date: 2026-08-06
Status: revised after independent review (see "Revision log" at end)

## Goal

Move the reusable Ether components toward a single, obvious ownership model:

- each reusable component lives in `Controls/`
- each reusable component has a `.xaml` + `.xaml.cs` pair
- engineers use the component from that pair instead of having to discover a split across `Controls/*.cs`, `Resources/Ether*.xaml`, and sample pages under `Views/Controls/*`

This plan is about authoring structure and consumption model. It is not a visual redesign.

## Why this change

Today, several reusable components are split across multiple places:

- behavior/type in `Controls/*.cs`
- look/template in `Resources/Ether*.xaml`
- examples in `Views/Controls/*Page.xaml`
- shared registration in `App.xaml`

That structure works for a design-system sandbox, but it creates friction for product engineers:

1. **No single handoff unit.** When someone asks for "the Button component", the answer is currently multiple files, not one obvious component pair.
2. **Split ownership.** A visual tweak may live in `Resources/EtherButton.xaml`, while API/behavior lives in `Controls/EtherButton.cs`.
3. **Harder onboarding.** New engineers can easily mistake `Views/Controls/ButtonPage.xaml` for the component itself.
4. **Harder extraction into a product app.** Copying one component requires tracing style/template/resource dependencies by hand.
5. **Inconsistent mental model.** Some components already exist as `.xaml + .xaml.cs` pairs (`EtherAIAssistant`, `EtherSlider`, navigation controls), while others are style dictionaries or pure C# controls.

The desired end state is simpler: the component entry point an engineer edits is the same component entry point an engineer consumes.

## Current state summary

Current reusable-control patterns in the repo:

### Pattern A — split control class + resource dictionary

Examples:

- `Controls/EtherButton.cs` + `Resources/EtherButton.xaml`
- `Controls/EtherDropdown.cs` + `Resources/EtherDropdown.xaml`
- resource-only styles such as `Resources/EtherCheckbox.xaml`, `Resources/EtherRadioButton.xaml`, `Resources/EtherInput.xaml`, `Resources/EtherIntelligenceButton.xaml`

### Pattern B — self-contained `.xaml + .xaml.cs`

Examples:

- `Controls/EtherAIAssistant.xaml` + `.xaml.cs`
- `Controls/EtherSlider.xaml` + `.xaml.cs`
- `Controls/EtherDeviceMenu.xaml` + `.xaml.cs`
- `Controls/EtherMasthead.xaml` + `.xaml.cs`
- `Controls/EtherNavRail.xaml` + `.xaml.cs`
- `Controls/EtherRightPanel.xaml` + `.xaml.cs`
- `Controls/EtherWideNav.xaml` + `.xaml.cs`
- `Controls/UpdatedBadge.xaml` + `.xaml.cs`

### Pattern C — gallery/demo pages

Examples:

- `Views/Controls/ButtonPage.xaml` + `.xaml.cs`
- `Views/Controls/CheckboxPage.xaml` + `.xaml.cs`
- `Views/Controls/InputPage.xaml` + `.xaml.cs`

These are sample/showcase pages, not the reusable component source of truth.

## Decision

Adopt a **component-local authoring model** for reusable sandbox components:

- one reusable component = one `Controls/<Component>.xaml` + `Controls/<Component>.xaml.cs` pair
- sample/demo pages remain in `Views/Controls/*`
- shared design tokens remain centralized in `Resources/` (`EtherColors`, `EtherSpacing`, `EtherTypography`, etc.)
- component-specific templates/styles move out of top-level `Resources/Ether*.xaml` files and into the component's own XAML where practical

This means the component becomes the primary unit of:

- ownership
- editing
- review
- reuse
- handoff to other engineers

### Template resolution mechanism (decided up front — do not defer to Phase 1)

This is the one decision the rest of the plan depends on, so it is pinned here instead of
being "tuned during implementation."

**Two candidate shapes exist, and they are not interchangeable:**

- **UserControl** (`Controls/<X>.xaml` has `x:Class`, composes native controls inside it). This
  is what "self-contained `.xaml` + `.xaml.cs` pair" suggests on first read, and it is the
  right shape for genuine composites (`EtherAIAssistant`, `EtherRightPanel`, `EtherMasthead`,
  nav surfaces — all already built this way, all out of scope for this plan).
- **Templated control**: the class stays a subclass of the native control (`EtherButton :
  Button`, `EtherDropdown : ComboBox`, etc.), sets `DefaultStyleKey`, and its
  `ControlTemplate`/`Style` lives in a co-located `ResourceDictionary` under `Controls/`
  (merged once via `Themes/Generic.xaml`, the standard WinUI convention — nothing in this repo
  uses this today; `Controls/EtherProgressBar.cs` even has a comment noting it does *not* set
  `DefaultStyleKey`).

**Decision: the ten in-scope controls use the templated-control shape, not UserControl.**

Reason: every one of them is either already a subclass of a native control, or is currently a
keyed style on one (see the corrected Pattern A/B split below). Concretely, in this codebase
today:

- 8 of the 10 demo pages drive their STATES gallery with
  `VisualStateManager.GoToState(specimen, "PointerOver" | "Pressed" | ...)` directly on the
  specimen instance (`ButtonPage.xaml.cs`, `CheckboxPage.xaml.cs`, `RadioButtonPage.xaml.cs`,
  `ToggleSwitchPage.xaml.cs`, `SegmentedControlPage.xaml.cs`, `IntelligenceButtonPage.xaml.cs`,
  `InputPage.xaml.cs`, `DropdownPage.xaml.cs`). That only works because the specimen's template
  owns real `CommonStates`/`CheckStates`/`FocusStates` groups. Wrapping any of these in a
  `UserControl` makes `GoToState` silently return `false` — no build error, no exception, the
  STATES card just renders all-Normal.
- `EtherDropdown` is a 358-line `ComboBox` subclass whose `OnApplyTemplate`/`MeasureOverride`
  depend on `GetTemplateChild`, and whose popup positioning was hard-won (see the repo's own
  `SizeChanged`-not-`LayoutUpdated` fix history). Rewrapping it as a `UserControl` re-litigates
  bugs that are already solved.
- `RadioButton` grouping is parent-based (or `GroupName`-based). Wrapping each radio in its own
  `UserControl` gives every radio button a private visual parent, silently breaking
  parent-based grouping and forcing `GroupName` to become mandatory and forwarded as a DP —
  plus each wrapper becomes an extra, unwanted tab stop unless `IsTabStop="False"` is set
  explicitly on it.

Templated-control shape avoids all three problems for free: `Click`/`Command`, focus routing,
automation peers, `GoToState`, and native grouping all keep working because the underlying
type never changes — only where its template lives changes.

**Mechanism refinement (added when Phase 0 was planned in detail):** `DefaultStyleKey` +
`Themes/Generic.xaml` auto-discovery is a real WinUI3 pattern, but this app's own `App.xaml`
carries a header comment noting that this specific *unpackaged* app does not get framework
control resources "for free" and needs explicit merges elsewhere (`XamlControlsResources`) —
a different mechanism, but a signal not to gate a real change on unverified auto-discovery
behavior. Phase 0 therefore uses the guaranteed-safe variant: the co-located dictionary is
still merged **explicitly** from `App.xaml`, just repointed from `Resources/<Component>.xaml`
to `Controls/<Component>.xaml` — the same proven mechanism already working for `EtherDropdown`
today. This keeps the folder-ownership benefit (component owns its own template) without
depending on unverified framework behavior. `DefaultStyleKey`/`Generic.xaml` auto-discovery can
be revisited as an optional follow-up once this pattern is proven, not as a Phase 0 dependency.

**Consequence for "one component = one file pair":** for controls that don't have their own
class today (Checkbox, Radio Button, Toggle Switch, Label/Filter Chip, Intelligence Button —
currently just keyed styles on stock controls), migration means introducing a thin subclass
(`EtherCheckbox : CheckBox`, etc.) with `DefaultStyleKey`, not a `UserControl`. This is real,
scoped work (Phase 2 below is sized for it) but it is mechanical and low-risk compared to
reimplementing checked-state semantics from scratch.

### Corrected current-state split

The original Pattern A grouping conflated two different situations. Splitting them matters
because they carry different migration cost:

- **A1 — has its own control class + dictionary today:** Button, Dropdown, Progress Bar,
  Segmented Control (track). These already have the behavior; migration is template
  relocation + `DefaultStyleKey` wiring.
- **A2 — keyed style on a stock control, no class of its own:** Checkbox, Radio Button, Toggle
  Switch, Label/Filter Chip, Intelligence Button. Migration means creating the subclass first,
  then relocating the template.

## Scope

### In scope

1. **Reusable controls in the Controls catalog**
   - Button
   - Checkbox
   - Dropdown
   - Input
   - Intelligence Button
   - Label / Filter Chip
   - Radio Button
   - Segmented Control
   - Slider
   - Toggle Switch

2. **Related reusable primitives that support those controls**
   - helper classes and dependency properties currently used only to make the controls work
   - component-local visual states, internal layout, and template parts

3. **Consumer updates**
   - update sandbox pages under `Views/Controls/*`
   - update every other confirmed consumer, not just pages — see "Consumer inventory" below.
     `grep`-derived, not assumed: the shared `Views/ComponentPage.xaml` chrome, other
     `Controls/*` composites, and Surfaces pages all reference these styles too.

4. **Resource registration cleanup**
   - stop globally merging component-specific style dictionaries once the component no longer depends on them as an external entry point
   - keep true shared foundation dictionaries in `App.xaml`
   - **exit gate, per component, before deleting its `App.xaml` merge line:** grep the whole
     repo for every `StaticResource`/`ThemeResource` key the old dictionary defined. Zero hits
     outside the component's own new dictionary = safe to delete. This is what catches the
     "shared chrome" class of breakage below — it will not show up from opening only the
     migrated component's own demo page.

### Consumer inventory (grep-verified 2026-08-06, re-verify before each phase)

The original scope statement ("update sandbox pages... and any other pages") undercounts
consumers. Confirmed non-`Views/Controls/*` consumers of styles in scope for this migration:

| Style key(s) | Consumer | Why it matters |
|---|---|---|
| `EtherCheckbox` | `Views/ComponentPage.xaml:78` | Shared chrome embedded by **every** demo page in the app — a broken checkbox style here doesn't just break Checkbox's own page. |
| `EtherCheckbox` | `Views/Controls/ButtonPage.xaml:18` | Cross-component reference. |
| `EtherButtonTertiary` | `Controls/EtherAIAssistant.xaml:126` | Other components (not pages) consume Button's styles — live `StaticResource` reference. |
| `EtherButtonPrimarySmall` / `EtherButtonTertiarySmall` | `Views/Surfaces/AIRecommendationPage.xaml`, `CardPage.xaml`, `ExpressChargePage.xaml` | Three Surfaces pages, outside the Controls demo pages entirely. |

Re-run the grep per component before its phase starts — this table is a snapshot, not a
guarantee against drift.

**Correction (found during Phase 1 implementation, 2026-08-06):** this table originally also
listed `Controls/EtherRightPanel.xaml` as a live consumer of both `EtherButton*` and
`EtherSegment*` styles. On inspection, both references in that file are **comments**, not live
`StaticResource` usages — `EtherRightPanel.xaml:75` (`<!-- Apply EtherSegment style when
resources are merged -->`) and `:108` (`<!-- Apply Style="{StaticResource EtherButtonPrimary}"
when resources are merged -->`) are TODO-style notes, not active bindings. `EtherRightPanel` is
not an actual consumer of either component today — removed from the table above. This was an
error in the original architecture-review pass (the independent reviews that shaped this plan
were not independently re-verified line-by-line against every individual claim before being
incorporated); Phase 1's own verification caught it. Re-check `EtherRightPanel.xaml` fresh when
Segmented Control's phase starts, since a comment today could become a live reference later.

### Out of scope

1. Redesigning component visuals, states, spacing, tokens, or typography.
2. Reworking navigation/surface composites that are already `.xaml + .xaml.cs` unless they are touched only for consistency.
3. Turning the sandbox into a standalone component NuGet/library in this pass.
4. Eliminating all shared resources; token dictionaries remain shared by design.
5. Migrating every non-control resource file under `Resources/`.

## Architecture target

### Folder intent after migration

- `Controls/` = reusable component source of truth
- `Views/Controls/` = showcase/demo pages only
- `Resources/` = shared design tokens and truly cross-component foundations only

### Expected usage style after migration

Current button usage:

```xml
<controls:EtherButton Content="Primary Action"
                      Style="{StaticResource EtherButtonPrimary}" />
```

Target button usage:

```xml
<controls:EtherButton Content="Primary Action"
                      Variant="Primary"
                      Size="Large" />
```

The exact API can be tuned during implementation, but the key change is that the engineer consumes a self-contained control, not a control-plus-style-dictionary contract.

### Shared resources that remain global

These should stay centralized and merged in `App.xaml`:

- `Resources/EtherColors.xaml`
- `Resources/EtherSpacing.xaml`
- `Resources/EtherTypography.xaml`
- other true foundation dictionaries (for example data-graphics or cross-app brushes) where multiple components intentionally share the same token set

### Component-local resources

These should move into the component XAML when they are only meaningful for that component:

- component-specific control templates
- component-specific brushes that are not part of the shared design token set
- local visual states
- component-private layout resources

## Rationale behind the split

This plan deliberately does **not** duplicate the entire design system into every component.

Why:

- full duplication would make handoff easier in the short term but would fragment the token source of truth
- shared tokens should still be shared, or visual drift becomes inevitable
- the real pain point is not shared foundations; it is split component ownership between class and template files living in different places

So the intended compromise is:

- **component API + visual structure** become local to the component
- **foundational tokens** remain shared

That gives engineers one obvious component to work with without giving up centralized design tokens.

## Migration strategy

Do this incrementally, not as one giant rewrite. Every phase below uses the templated-control
shape decided above, not `UserControl`, and every phase ends with the grep exit gate plus a
full navigation sweep (see Verification).

### Phase 0 — pilot on Progress Bar, prove the mechanism

`Controls/EtherProgressBar.cs` + `Resources/EtherProgressBar.xaml` is the cheapest place to
prove the co-located-dictionary pattern works in this repo: one style key, one
consumer (`Views/DataDisplay/ProgressBarPage.xaml`), no `GoToState` pinning to worry about.

- add `Controls/EtherProgressBar.xaml` (`ResourceDictionary`, no `x:Class`) containing the
  relocated template, style made implicit (no `x:Key`, safe since `EtherProgressBar` is a
  custom sealed type)
- repoint the existing `App.xaml` merge line from `Resources/EtherProgressBar.xaml` to
  `Controls/EtherProgressBar.xaml` — explicit merge, not `DefaultStyleKey`/`Generic.xaml` (see
  "Mechanism refinement" above)
- update `ProgressBarPage.xaml` call sites to drop the now-redundant `Style=` attribute, since
  the style resolves implicitly once merged

**Status: done.** Implemented in commits `33d3efa` (relocation) and `3fda978` (a stale
doc-comment in `EtherProgressBar.cs` caught by code-quality review, fixed as a follow-up —
lesson for later phases: when a component's `.cs` file has a doc comment pointing at its old
`Resources/` path, that comment needs updating in the same task, not just the XAML).

Then repeat the identical mechanic on **Toggle Switch** (also one style key, one consumer) as
a second, independent confirmation before touching anything with wider reach — except the style
stays KEYED (`x:Key="EtherSwitch"`), not implicit, since `ToggleSwitch` is a stock framework
type and an implicit style would silently restyle every bare `<ToggleSwitch>` in the app; only
the file location and `App.xaml` merge target change, consumer call sites are untouched.

**Status: done.** Implemented in commit `fa8f3d1`. No mechanism surprises — confirmed the
`VisualStateManager.GoToState` calls in `ToggleSwitchPage.xaml.cs` were provably unaffected
(zero diff to that file), matching the prediction in the mechanism decision section above.

**Why not Button first (reversing the original plan):** Button has the widest blast radius in
the repo — 6 style keys, 3 templates, and per the consumer inventory above, referenced from
`EtherAIAssistant` and 3 Surfaces pages, not just its own demo page (the original inventory also
listed `EtherRightPanel` here — corrected below, it turned out to be comment-only). Learning the
mechanism there means debugging the mechanism and the widest consumer set at the same time.
Prove the mechanism cheaply first.

### Phase 1 — Button

Now apply the proven mechanism to Button:

- add `Controls/EtherButton.xaml` (co-located `ResourceDictionary`, templated-control shape —
  `EtherButton` stays a `Button` subclass, do **not** convert it to `UserControl`)
- move the 3 templates from `Resources/EtherButton.xaml` into the co-located dictionary,
  repoint the existing `App.xaml` merge line (the proven Phase 0 mechanism — explicit merge,
  not `DefaultStyleKey`/`Generic.xaml`)
- all 6 style keys stay keyed (a single implicit style can't represent 6 variant/size
  combinations) — this means **no consumer file needs any edit** for the relocation itself,
  since every `Style="{StaticResource EtherButtonX}"` reference keeps resolving unchanged; the
  "update consumers in the same PR" concern below turned out to apply only to the *optional*
  Variant/Size API, not the relocation
- keep public behavior parity: `Content` (text-only today — all 3 templates render it via
  `TextBlock Text="{TemplateBinding Content}"`, confirmed as the current contract, not a gap),
  `IsEnabled`, `Click`/`Command`, `RightIcon`, cursor behavior
- pin down the Variant/Size API shape (forced in this phase per the original plan's reasoning) —
  implemented as **opt-in** DPs that assign the whole `Style` object rather than individual
  properties, so a DP write can never fight an existing local-value override
  (`HorizontalContentAlignment`, `IsHitTestVisible`, `IsTabStop`) at any call site, and no
  existing consumer needs to change to adopt it

**Status: done.** Implemented in commits `e1f8934` (relocation, zero consumer impact), `ca57a92`
(opt-in `Variant`/`Size` API), `ff02926` (doc-comment fix + confirmed the XAML attribute-parsing
path works via a build-time smoke test, not just C# compilation). See
`docs/superpowers/plans/2026-08-06-phase1-button-pilot.md` for the granular task plan. Both
independent reviews' core worry about Button (wide consumer set) turned out to be mitigated
almost entirely by keeping the styles keyed rather than needing any consumer migration.

### Phase 2 — controls with no class of their own today (Pattern A2)

Checkbox, Radio Button, Toggle Switch (already done in Phase 0), Label / Filter Chip,
Intelligence Button.

**Further simplification, discovered during Phase 2 implementation (supersedes the paragraph
below):** no new subclass turned out to be necessary for Checkbox, Radio Button, or Intelligence
Button. The original reasoning below (from architecture review, before Phase 0's mechanism was
proven) assumed a subclass was needed to give each component "a `.cs` file of its own." But
Phase 0's Toggle Switch already proved a component can be a single `.xaml` file — a keyed style
on a stock control, explicitly merged, no class at all — and that pattern turned out to apply
here too, since none of these three needed new behavior. Checkbox and Radio Button remained
keyed styles on the stock `CheckBox`/`RadioButton` types; Intelligence Button remained a keyed
style on the stock `Button` type, applied via the pre-existing shared `HandButton` helper. This
is *lower* risk than the subclass approach below, not higher — nothing about `GoToState`,
check/toggle semantics, or `GroupName` grouping needed "preserving" via a new subclass, because
the types themselves were never touched.

Original reasoning (superseded, kept for context): ~~introduce `Ether<X> : <NativeControl>`
with a co-located dictionary merged explicitly from `App.xaml`, not a `UserControl` wrapper —
this is what preserves `GoToState`-driven states, native check/toggle semantics, and (for Radio
Button specifically) parent/`GroupName`-based grouping without extra work; forward `IsChecked`,
`Content`, `IsEnabled`, `GroupName` (Radio Button) as needed.~~

Intelligence Button and Label/Filter Chip both build on the shared `HandButton` /
`HandContentControl` cursor helpers in `Controls/` — these stay as shared helper classes, they
are not migrated into any single component's folder.

**Status: Checkbox, Radio Button, and Intelligence Button done.** Implemented in commits
`eebac18` (Checkbox), `fc86a0b` (Radio Button), `8387cd2` (Intelligence Button — also fixed
genuine mojibake in its header comment, found and fixed via pure codepoint inspection after two
earlier misdiagnoses caused by a terminal-printing artifact; see that plan's own retrospective
for the full story). **Label / Filter Chip deliberately deferred** — it isn't "Updated"-tagged
in `ComponentCatalog.cs`, and the user scoped this round of work to Updated-tagged components
only. It remains grouped in this Phase 2 description for whenever it's picked up, since the
same "no subclass needed" simplification will almost certainly apply to it too.

### Phase 3 — Dropdown (own phase, not grouped)

Dropdown is closer to "already compliant" than "needs interaction redesign": it's a real
`ComboBox` subclass with an already-implicit style (`Resources/EtherDropdown.xaml:314` has no
`x:Key`), and its popup positioning / template-part logic is hard-won. Treat this phase as pure
relocation:

- move the template into `Controls/EtherDropdown.xaml`, repoint the existing `App.xaml` merge
  line (the proven Phase 0 mechanism) — keep the style implicit, as it already is today
- do **not** touch `OnApplyTemplate`, `MeasureOverride`, or the `SizeChanged`-based popup
  positioning logic — byte-for-byte behavior parity is the bar here, not a rewrite
- verify keyboard/focus/selection behavior explicitly, since this control carries the most
  interaction surface of the A1 group

**Status: done.** Implemented in commit `1e8eed2`. `Controls/EtherDropdown.cs` — the
`MeasureOverride` width-hugging, the 5 `[TemplatePart]`-declared template parts, and the
`SizeChanged`-based popup positioning fix — confirmed untouched (zero diff across the whole
phase). Both reviewers independently re-verified the `StateFill`/`OpenFill` dual-fill invariant
structurally (which group writes which property), not just by confirming the explanatory
comment survived the copy.

### Phase 4 — Segmented Control

Explicitly a two-file component: `Controls/EtherSegmentedTrack.xaml`/`.cs` (container) plus its
co-located `EtherSegment` item style — "one component = one file pair" here means one *pair*
covering container + item template, not a forced single file.

**Status: done, with a small simplification from the description above.** Implemented in commit
`02645d6`. Both the container style (`EtherSegmentedTrack`, targets stock `ContentControl`) and
the item style (`EtherSegment`, targets stock `RadioButton`) ended up in one file,
`Controls/EtherSegmentedControl.xaml`, rather than two separate `.xaml` files — the original
`Resources/EtherSegmentedControl.xaml` already combined both, and there was no reason to split
them apart during relocation. `Controls/EtherSegmentedTrack.cs` (the `.cs` half, rendering
Composition-layer drop shadows) is untouched, confirmed zero diff throughout. The "one component
= one file *pair*" framing above still holds in spirit — one `.cs` + one `.xaml`, just with both
styles inside that single `.xaml`, not two.

### Phase 5 — Input

Kept as its own phase (not grouped with Dropdown as originally planned) because it carries the
largest forwarding surface of any control in scope: `Text`, `PlaceholderText`,
`SelectionStart`/`Length`, `MaxLength`, `InputScope`, `TextChanged`, `Header`, focus routing,
and its automation peer. Grouping it with Dropdown was underestimating both.

**Status: done, simplified.** Implemented in commit `c59e8b1`. The "largest forwarding surface"
concern turned out not to apply: Input has no dedicated class today, just like
Checkbox/Radio Button/Intelligence Button in Phase 2 — it's a keyed style on the stock
`TextBox` type. The "no subclass = no forwarding needed" reasoning holds regardless of how large
`TextBox`'s native API is, for a structural reason, not a coincidental one: forwarding is only a
concern when something sits *between* the consumer and the framework type. With no wrapper or
subclass, consumers already call `TextBox` members directly — there's nothing to proxy no matter
how many members `TextBox` has. The real risk in a `TextBox` retemplate isn't API forwarding,
it's silent part-name/attached-property breakage (`ContentElement`, `Control.IsTemplateFocusTarget`)
— both confirmed intact by this phase's review.

### Phase 6 — Slider audit (not "confirm and move on")

`EtherSlider` already has the `.xaml` + `.xaml.cs` file shape, but inspection found it doesn't
actually satisfy the plan's own token rationale yet:

- `Controls/EtherSlider.xaml.cs` hardcodes brush colors via `Color.FromArgb(...)` for the knob
  states, while its own header comment claims those colors come from the `ActionPrimaryBg`
  token — the doc comment and the code already disagree
- its highlight-brush lookup reads `Application.Current.Resources` directly rather than
  following the app's actual theme-switching mechanism (`RootGrid.RequestedTheme`), so it may
  not repaint correctly on theme toggle

Fix these as part of this phase, not "confirm it already satisfies the model" — otherwise the
migration propagates a real defect into the pattern the other nine controls are copying.

**Status: done, with a second-pass correction worth recording.** Implemented in commits
`0838755` and `3b9bacc`. The first commit fixed the hardcoded pressed-knob color mismatch
correctly, but its theme-repaint fix had a real bug: it read
`Application.Current.Resources.ThemeDictionaries` directly, which does not recurse into the
`ThemeDictionaries` of dictionaries merged in via `MergedDictionaries` — so the lookup always
silently fell through to a hardcoded fallback, defeating the fix. This was invisible for
`ActionPrimaryBg`/`ActionPrimaryBgPressed` (their fallbacks happen to equal the real Light/Dark
token values) but would have left `BackgroundTrack` permanently wrong in Dark theme (the one
color that actually differs between themes) — exactly the bug this phase was meant to fix,
reintroduced in a harder-to-notice form. Caught by code-quality review, not spec-compliance
review — a reminder that byte-for-byte spec matching doesn't catch a logic bug in the spec's
own proposed code. Fixed in `3b9bacc` by iterating `MergedDictionaries` and checking each
dictionary's own `ThemeDictionaries`, then independently re-verified by a second reviewer
tracing the actual object graph, not just re-checking the diff.

**Correction (2026-08-06, after the six-phase sweep completed):** this section originally said
Scroll Bar should be permanently excluded from the migration, since
`Resources/EtherScrollBar.xaml:73` is an implicit `<Style TargetType="ScrollBar">` applied
app-wide. That conflated two separate questions: *where the file lives* (the actual subject of
this whole plan) and *whether the style stays implicit* (a behavior question, already settled
correctly elsewhere — e.g. `EtherDropdown`'s implicit style moved to `Controls/` in Phase 3
without becoming keyed). The user pointed this out directly: Scroll Bar's thumb/track visuals
are a genuine custom-authored design-system component (not the OS default look), so it deserves
the same "own folder, own file" ownership as every other component in this plan — the fact that
it currently *auto-applies app-wide* is a legitimate, intentional keying decision to keep
exactly as-is, not a reason to exclude the file from relocation entirely. See Phase 7 below.

### Phase 7 — cleanup and deprecation

After consumer migration is complete:

- remove or shrink obsolete component-specific dictionaries from `Resources/`
- remove old style keys that are no longer public entry points
- update `App.xaml` merged dictionaries to keep only shared foundations
- update sample pages and any internal guidance/comments to reflect the new consumption pattern

## Detailed work items

### 1. Define the public API for each migrated control

Before coding each component, decide:

- what dependency properties the control exposes
- which native events must be forwarded
- whether size/variant are enums, strings, or style-like keys
- whether content is text-only or full `object`/templated content

### 2. Keep the external consumer story simple

Each component should answer these questions immediately:

- what XAML tag do I use?
- what properties matter most?
- where do I edit the visuals?
- which file pair is the source of truth?

### 3. Preserve platform behavior where reasonable

For primitive controls, the migration must not accidentally strip away useful native behavior.

Examples:

- Button should still behave like a button
- Checkbox and Radio Button should preserve accessible checked-state semantics
- Input and Dropdown should preserve focus, keyboard, and binding expectations

### 4. Avoid hidden global dependencies where possible

If a component still depends on a global resource, that dependency should be one of:

- shared design tokens
- shared converters/utilities with a clear foundation role

Avoid leaving behind component-private templates or brushes in global dictionaries.

## Risks and mitigations

### Risk 1 — losing native control flexibility

**Mitigation:** resolved by the mechanism decision above — templated-control shape, not
`UserControl`, for all ten in-scope controls. Native DPs, events, focus/automation behavior,
and (for Radio Button) grouping are inherited for free rather than needing reimplementation.
Still verify bindings, commands, and accessibility behavior explicitly per component.

### Risk 2 — API churn across sample pages and product pages

**Mitigation:** migrate one component at a time; update **all** confirmed call sites (per the
Consumer inventory table, not just `Views/Controls/*`) in the same change; add temporary
compatibility shims only if absolutely necessary.

### Risk 3 — Token/resource breakage during extraction

**Mitigation:** keep shared foundations in `Resources/`; only move component-private visuals
into local component XAML; run the grep exit gate before deleting any `App.xaml` merge line.

### Risk 4 — Large refactor becomes hard to review

**Mitigation:** phased PRs/commits matching the Migration strategy phases above (0 through 7).
One component (or the Phase 2 subclass-introduction batch) per PR.

### Risk 5 — missed consumer causes a silent runtime crash

New risk identified during review: a missed consumer doesn't fail the build — it fails at
**runtime page navigation**, with no signal until someone opens that specific page.

**Mitigation:** the grep exit gate (Scope §4) plus the full-catalog navigation sweep
(Verification below) are both mandatory phase-exit steps, not optional polish.

### Risk 6 — `App.xaml` de-registration is the irreversible step

Deleting a dictionary's merge line from `App.xaml` is what makes a phase hard to revert.

**Mitigation:** never delete a component's `App.xaml` merge line until the grep exit gate for
that specific component is clean. Keep the old dictionary file merged (even if now partially
redundant) until then — cheap insurance against an incomplete consumer sweep.

## Verification

For each migrated component:

1. **Before migrating:** capture a screenshot baseline of the affected demo page(s) — default /
   hover / pressed / disabled / focused / checked states as documented in that component's
   existing dictionary header comment — in **both** Light and Dark theme (the toggle already
   exists via `RootGrid.RequestedTheme` in `MainWindow.xaml.cs`).
2. `dotnet build EtherComponentSandbox.csproj -c Debug` — necessary but **not sufficient**:
   missing `StaticResource` keys fail at runtime page navigation (`XamlParseException`), not at
   compile time.
3. Open the corresponding demo page(s) under `Views/Controls/*` and compare against the
   baseline screenshots, both themes.
4. **Navigate to every page in `ComponentCatalog.Nodes`**, not just the migrated one — this is
   the step that catches a missed consumer (Risk 5). A page that isn't the one you touched can
   still be the one that crashes.
5. Verify keyboard focus and pointer interactions explicitly.
6. Verify any forwarded DP/event surface used by current callers.
7. Run the grep exit gate from Scope §4 before deleting the old `App.xaml` merge line.

## Definition of done

A component is considered migrated when all of the following are true:

- its primary source of truth is a `Controls/<Component>` file set: for a templated control
  this is `Controls/<Component>.cs` (or `.xaml.cs` where a class file already exists) + a
  co-located `Controls/<Component>.xaml` **`ResourceDictionary`** merged explicitly from
  `App.xaml` (the mechanism proven in Phase 0 — see "Mechanism refinement" above; `Generic.xaml`
  auto-discovery remains an optional future experiment, not a requirement) — a literal
  `x:Class`-bearing `.xaml.cs` pair is required only for the handful of components that are
  genuine composites, not for the ten controls in this plan
- an engineer can discover its usage from that component directly
- existing consumers — per the Consumer inventory, including non-page consumers — no longer
  depend on the old public style-dictionary entry point
- component-private visuals are no longer split across distant files
- shared design tokens remain centralized and unchanged
- the sandbox builds, every page in `ComponentCatalog.Nodes` still navigates without error, and
  the component's own demo page still renders all documented states correctly in both themes

## Recommended first implementation slice

Start with **Progress Bar** (Phase 0), immediately followed by **Toggle Switch** — see Phase 0
above for the reasoning. Button moves to Phase 1, after the mechanism is proven, not before.

**Status: Phase 0 complete** (commits `33d3efa`, `3fda978`, `fa8f3d1` — see
`docs/superpowers/plans/2026-08-06-phase0-progress-bar-toggle-switch-pilot.md` for the granular
task plan). Both pilot components were reviewed via independent spec-compliance and code-quality
review, no mechanism surprises — the explicit-merge, co-located-dictionary pattern worked
exactly as designed, `GoToState`-driven states were provably unaffected in both cases, and
resource resolution order held. Phase 1 (Button) is ready to start.

This reverses the original "Button first" recommendation. Button remains the component that
forces the real architectural decisions (content model, variant model, size model) — it's just
no longer the place to also be debugging the underlying mechanism, since it also has the
largest confirmed consumer set of any control in scope.

If Progress Bar and Toggle Switch feel good in day-to-day engineering use, apply the same model
to the remaining controls in the phases above.

## Revision log

**2026-08-06 — revised after two independent architectural reviews** (run separately, each
blind to the other, each instructed to inspect the actual codebase rather than review the
prose in isolation). Both independently converged on the same critical finding — the plan
never decided whether "self-contained" means `UserControl` composition or a templated control
with a co-located dictionary, and that choice determines whether `GoToState`-driven states,
`EtherDropdown`'s template-part logic, and `RadioButton` grouping survive the migration — plus
the same consumer-inventory gap (`ComponentPage.xaml`, `EtherAIAssistant`, `EtherRightPanel`,
and three Surfaces pages depend on styles this migration touches but the original scope never
named them). All load-bearing claims from both reviews were independently re-verified against
the repository (grep + direct file reads) before being incorporated — see the Consumer
inventory and Template resolution mechanism sections above, which cite exact files and line
numbers rather than restating the reviews' prose. Changes made: added the mechanism decision;
corrected the Pattern A split (A1 has-a-class vs A2 needs-a-class); reordered phases by actual
complexity (Progress Bar pilot, Dropdown alone, Input alone, Slider gets a real audit); removed
Scroll Bar from scope; added the consumer inventory and grep/navigation-sweep exit gates; changed
the Definition of Done to accept the templated-control shape.

## Closing summary — Updated-tagged component sweep (2026-08-06, updated after Phase 7)

All six original phases, plus a seventh added after the user correctly pushed back on the
Scroll Bar exclusion (see the correction above), are done. Every "Updated"-tagged component now
owns its own template/style in `Controls/`:

| Component | Phase | Commit(s) | Notes |
|---|---|---|---|
| Progress Bar | 0 | `33d3efa`, `3fda978` | Implicit style; caught a stale doc-comment path |
| Toggle Switch | 0, 8 | `fa8f3d1` | **Permanent exception**: `ToggleSwitch` is `sealed` in WinUI3 (confirmed by compiler error `CS0509` attempting a subclass in Phase 8) — stays a keyed style on the stock type, the only component besides Scroll Bar that cannot become a `<controls:EtherX/>` |
| Button | 1 | `e1f8934`, `ca57a92`, `ff02926` | Zero consumer impact; added opt-in Variant/Size API |
| Checkbox | 2, 8 | `eebac18`, `8bdfcb1` | Phase 2: relocated as keyed style. Phase 8: given a real `EtherCheckbox : CheckBox` subclass, consumed as `<controls:EtherCheckbox/>` |
| Radio Button | 2, 8 | `fc86a0b`, `4afb249` | Phase 8 subclass; `GroupName`-based grouping confirmed unaffected by the type rename |
| Intelligence Button | 2, 8 | `8387cd2`, `3c549bb` | Phase 2: fixed genuine mojibake. Phase 8: subclass gets its own hand cursor (matching `EtherButton`'s convention), `HandButton` itself untouched |
| Dropdown | 3 | `1e8eed2` | Pure relocation; `.cs` untouched, confirmed by both reviewers |
| Segmented Control | 4 | `02645d6` | Pure relocation; `.cs` untouched |
| Input | 5, 8 | `c59e8b1`, `396a054` | Phase 8 subclass; framework-required `ContentElement`/`IsTemplateFocusTarget` template parts confirmed intact |
| Slider | 6 | `0838755`, `3b9bacc` | Real bug fix, not relocation — see Phase 6 above |
| Scroll Bar | 7 | `5b9b4f6` | Implicit style stays implicit (the one deliberate exception); added after user correction |

**Deliberately not done:** Label / Filter Chip (not Updated-tagged, out of this round's scope
per user instruction).

## Phase 8 — thin subclasses for uniform two-file components (2026-08-06)

**Status: done, with one architecturally-forced exception.** After the original seven-phase
sweep, the user asked for a further pass: give every remaining "keyed style on a stock type"
component (Checkbox, Radio Button, Toggle Switch, Intelligence Button, Input) its own dedicated
`.cs` subclass, so it's consumed as `<controls:EtherX/>` with zero extra explanation needed —
reversing the earlier Phase 2/5 "no subclass needed" simplification, because uniform handoff
turned out to matter more than avoiding a thin subclass. Full plan and per-task detail in
`docs/superpowers/plans/2026-08-06-phase8-thin-subclasses.md`.

4 of the 5 succeeded (Checkbox `8bdfcb1`, Radio Button `4afb249`, Intelligence Button `3c549bb`,
Input `396a054`, plus a whitespace-only indentation cleanup `6b06bbe`) — each gets a minimal
`sealed class EtherX : <StockType>` with no members (Intelligence Button's the one exception,
needing a constructor to set its own hand cursor, matching `EtherButton`'s existing convention),
its style retargeted from a keyed style on the stock type to an implicit style on the new custom
type (safe now, since nothing else could accidentally instantiate the new type), and every
consumer converted from `<StockType Style="{StaticResource EtherX}"/>` to `<controls:EtherX/>`.

**Toggle Switch could not follow this pattern — discovered mid-execution, not assumed in
advance.** `Microsoft.UI.Xaml.Controls.ToggleSwitch` is a `sealed` class in WinUI3; an actual
attempt to write `EtherSwitch : ToggleSwitch` failed to compile (`CS0509: cannot derive from
sealed type 'ToggleSwitch'`). This is a compiler-enforced fact, not a design choice — Microsoft
restricts this specific type from being subclassed at all, by anyone. The two alternatives
(reimplement the toggle state machine from a bare `Control`, or wrap in a `UserControl`) were
both considered and rejected: the former is a fundamentally larger undertaking than "thin
subclass, zero behavior change" and wildly disproportionate to this phase's goal; the latter was
already ruled out by this project's own earlier architecture decision (a `UserControl` wrapper
would move `x:Name` off the actual `ToggleSwitch` instance, breaking every
`VisualStateManager.GoToState` call in `ToggleSwitchPage.xaml.cs`). Toggle Switch remains exactly
as Phase 0 left it — a keyed style on the stock type — and now joins Scroll Bar as the second
of two permanent, architecturally-forced exceptions to the uniform 2-file pattern.

**Final state:** 9 of 11 Updated-tagged components are now genuine `Controls/EtherX.cs` +
`.xaml` pairs consumed as `<controls:EtherX/>`. Scroll Bar and Toggle Switch remain one-file
components, each for a distinct, hard, non-negotiable architectural reason — not inconsistent
effort.

**What held up across all eight phases, worth remembering for future work in this repo:**
- The "keyed style relocation, zero consumer changes" pattern proved correct far more often
  than the original architecture review assumed — every component in Phase 2/5 needed no new
  subclass *to relocate*, once the mechanism moved away from `DefaultStyleKey`/`Generic.xaml` to
  explicit `App.xaml` merge. Whether a subclass is *worth adding anyway*, for a uniform handoff
  experience, turned out to be a separate question with a different answer (Phase 8).
- "Where a file lives," "whether its style is implicit vs. keyed," and "whether it has a
  dedicated subclass" are three independent decisions — conflating the first two was the
  original mistake that excluded Scroll Bar; assuming "no new behavior needed" meant "no
  subclass wanted" was the assumption Phase 8 revisited. None of these three questions implies
  an answer to either of the others.
- Two components turned out to have hard, compiler/framework-level reasons they can't follow
  the uniform pattern at all (Scroll Bar: `ScrollViewer` internally instantiates a literal stock
  `ScrollBar`, no way to substitute a subclass; Toggle Switch: the type itself is `sealed`).
  Both were discovered by actually trying, not by upfront analysis — worth remembering that
  "this should generalize" claims need testing against the one component that might be the
  exception, not just applying the pattern by rote to everything left.
- Independent spec-compliance + code-quality review on every single task caught real issues at
  least three times across the full project: a mis-amended commit (Progress Bar), a genuine
  logic bug in a fix that looked correct on a shallow read (Slider's `ThemeDictionaries`
  lookup), and an architecturally-impossible task spec (Toggle Switch subclassing) that no
  amount of careful transcription could have made work. Neither the first two would have been
  caught by trusting an implementer's self-report, and the third is exactly why "if you hit an
  unexpected compile error, stop and report rather than improvise" is worth stating explicitly
  in every implementer prompt, not just assumed.
- Printing non-ASCII text through this session's Bash tool is unreliable and caused two
  self-corrections during the Intelligence Button mojibake fix — pure codepoint (`ord()`/hex)
  inspection, never printing the character itself, was the only reliable verification method
  found.
