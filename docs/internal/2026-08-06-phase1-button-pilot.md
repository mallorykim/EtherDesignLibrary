# Phase 1 Pilot: Button Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Apply the co-located-dictionary pattern proven in Phase 0
(`docs/superpowers/plans/2026-08-06-phase0-progress-bar-toggle-switch-pilot.md`) to Button —
the component with the widest consumer set in the app — and pin down the Variant/Size API
shape the parent plan calls for, without repeating the risk both independent architecture
reviews flagged (a DP-driven size setter fighting existing local-value overrides at call
sites).

**Architecture:** Same mechanism as Phase 0: move `Resources/EtherButton.xaml`'s content into
a co-located `Controls/EtherButton.xaml`, repoint the single `App.xaml` merge line, delete the
old file. Unlike Progress Bar, Button's 6 styles **stay keyed** (`EtherButtonPrimary`,
`EtherButtonPrimarySmall`, `EtherButtonSecondary`, `EtherButtonSecondarySmall`,
`EtherButtonTertiary`, `EtherButtonTertiarySmall`) — a single implicit style can't represent 6
variant/size combinations, so this is the same "keyed, not implicit" choice already made for
Toggle Switch in Phase 0, just with 6 keys instead of 1. **Because the keys don't change, no
consumer file needs any edit for the relocation itself** — every existing
`Style="{StaticResource EtherButtonX}"` call site keeps resolving exactly as before, the same
way `ToggleSwitchPage.xaml`'s call sites needed zero changes in Phase 0. This eliminates most of
the risk the two independent architecture reviews (run separately on the parent plan) flagged
about Button's wide consumer set — that risk was about consumers breaking if keys moved or
disappeared, and this task does neither.

The Variant/Size API (Task 2) is added as **pure opt-in convenience**, not a replacement for
the keyed styles: when both `Variant` and `Size` are explicitly set on an `EtherButton`, the
control assigns its whole `Style` property to the matching named style resource — the same
thing a caller does by hand today with `Style="{StaticResource EtherButtonPrimarySmall}"`, just
computed instead of hand-written. It does **not** set `MinHeight`/`Padding`/`FontSize` as
separate local values, which is the design both independent reviews warned would permanently
outrank a style setter and collide with existing local overrides
(`HorizontalContentAlignment`, `IsHitTestVisible`, `IsTabStop` — all present in
`ButtonPage.xaml`'s STATES swatches). Because it's opt-in and unset by default, **zero existing
consumers need to change** to adopt this task.

**Tech Stack:** WinUI3 / .NET 8, unpackaged WinExe, `EtherComponentSandbox.csproj`. No test
framework — verification is `dotnet build` + code-level diff verification + full-catalog
navigation sweep, per the parent plan's Verification section. No GUI screenshot capability is
available in this environment (documented limitation from Phase 0) — visual confirmation is
deferred to the user running the app.

---

## Task 1: Relocate Button's styles and templates into `Controls/`

**Files:**
- Create: `Controls/EtherButton.xaml`
- Modify: `App.xaml` (merge source path, currently `Resources/EtherButton.xaml`)
- Delete: `Resources/EtherButton.xaml`

**No changes to any consumer file in this task** — not `Views/Controls/ButtonPage.xaml`, not
`Controls/EtherAIAssistant.xaml`, not `Views/Surfaces/ExpressChargePage.xaml`, not
`Views/Surfaces/CardPage.xaml`, not `Views/Surfaces/AIRecommendationPage.xaml`. All 6 style keys
are preserved verbatim, so every existing `Style="{StaticResource EtherButtonX}"` reference
keeps resolving unchanged. (One clarification for whoever does the consumer sweep: earlier
review passes on the parent plan listed `Controls/EtherRightPanel.xaml` as an `EtherButton*`
consumer. On inspection, `EtherRightPanel.xaml:108` only contains a **comment** —
`<!-- Apply Style="{StaticResource EtherButtonPrimary}" when resources are merged -->` — not a
live `StaticResource` reference. It is not an actual consumer today; don't spend time updating
it in this task, and note the correction if you touch the parent plan's consumer table.)

- [ ] **Step 1: Create `Controls/EtherButton.xaml`**

Move the entire content of `Resources/EtherButton.xaml` verbatim — all 3 `ControlTemplate`s
(`EtherButtonPrimaryTemplate`, `EtherButtonSecondaryTemplate`, `EtherButtonTertiaryTemplate`),
the `MultiplyConverter` resource, and all 6 `Style` elements exactly as they are today,
including every `x:Key`. Do not drop any key — unlike Progress Bar, none of these become
implicit. The only change is a one-line addition to the header comment block noting the new
location; every measurement, color token, `VisualState`, and template structure must be
byte-for-byte identical to the current file. Read the current file at
`Resources/EtherButton.xaml` and copy its full content (all ~349 lines) into the new location,
changing only:

1. Add this line inside the existing header comment block, near the top (after the `USAGE`
   section, before `VISUAL STATES` — keep the rest of the comment exactly as-is):

```
    LOCATION
      This dictionary lives beside its class (Controls/EtherButton.cs) and is merged
      explicitly from App.xaml — see the "Mechanism refinement" note in
      docs/superpowers/plans/2026-08-06-self-contained-component-authoring-plan.md.
```

2. Nothing else changes. Every `ControlTemplate`, every `Style`, every `x:Key`, every measurement
   and color token stays exactly as in the current `Resources/EtherButton.xaml`.

- [ ] **Step 2: Repoint the `App.xaml` merge**

In `App.xaml`, change:
```xml
                <ResourceDictionary Source="Resources/EtherButton.xaml"/>
```
to:
```xml
                <ResourceDictionary Source="Controls/EtherButton.xaml"/>
```
Keep it in the exact same position in the merge list — do not reorder any other lines.

- [ ] **Step 3: Delete the old file**

Delete `Resources/EtherButton.xaml` (e.g. `git rm Resources/EtherButton.xaml`).

- [ ] **Step 4: Build**

Run: `dotnet build EtherComponentSandbox.csproj -c Debug`
Expected: build succeeds, 0 errors.

- [ ] **Step 5: Diff-equivalence check**

Confirm the `ControlTemplate`/`Style` content in `Controls/EtherButton.xaml` is
character-for-character identical to what was in the deleted `Resources/EtherButton.xaml`,
other than the one added `LOCATION` comment line. Do not claim GUI visual verification — this
environment cannot render the WinUI3 window; note that limitation in your report.

- [ ] **Step 6: Grep exit gate**

Run a repo-wide grep for `EtherButton` (excluding `obj/`/`bin/` build output) and confirm every
hit is one of: `Controls/EtherButton.cs`, `Controls/EtherButton.xaml`, `App.xaml`, or a
consumer file using `Style="{StaticResource EtherButtonX}"` (`ButtonPage.xaml`,
`EtherAIAssistant.xaml`, `ExpressChargePage.xaml`, `CardPage.xaml`,
`AIRecommendationPage.xaml`). If you find a reference to the old `Resources/EtherButton.xaml`
path anywhere outside build output, stop and report it.

- [ ] **Step 7: Commit**

```bash
git add Controls/EtherButton.xaml App.xaml
git rm Resources/EtherButton.xaml
git commit -m "$(cat <<'EOF'
Relocate EtherButton templates and styles into Controls/

Phase 1 of the self-contained component authoring plan. All 6 style
keys stay keyed (unlike Progress Bar's implicit style) since a single
implicit style cannot represent 6 variant/size combinations - no
consumer file needs any change, every existing
Style="{StaticResource EtherButtonX}" reference keeps resolving as
before.

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
EOF
)"
```

---

## Task 2: Add opt-in Variant/Size convenience API to `EtherButton`

**Files:**
- Modify: `Controls/EtherButton.cs`
- Modify: `Controls/EtherButton.xaml` (header comment only — document the new usage form)

This satisfies the parent plan's "pin down the Variant/Size API shape" requirement for Phase 1,
using the collision-safe design described in this plan's Architecture section: the DPs assign
the whole `Style` object, never individual properties, and do nothing unless both are
explicitly set. **No consumer file changes in this task either** — this is additive.

- [ ] **Step 1: Add `Variant` and `Size` enums and DPs to `Controls/EtherButton.cs`**

Add two public enums and two dependency properties. When both are set (non-null), the control
looks up and assigns the corresponding named style from `Application.Current.Resources`. Use
this exact implementation, inserted into the `EtherButton` class after the existing
`RightIconVisibility` property block (before the closing brace of the class):

```csharp
    /// <summary>Visual weight of the button. Paired with <see cref="Size"/> to select one of
    /// the 6 named styles (e.g. <c>EtherButtonPrimarySmall</c>) automatically — an alternative
    /// to setting <c>Style</c> directly. Unset (null) by default: existing call sites that set
    /// <c>Style</c> explicitly are completely unaffected by this property's existence.</summary>
    public EtherButtonVariant? Variant
    {
        get => (EtherButtonVariant?)GetValue(VariantProperty);
        set => SetValue(VariantProperty, value);
    }
    public static readonly DependencyProperty VariantProperty = DependencyProperty.Register(
        nameof(Variant), typeof(EtherButtonVariant?), typeof(EtherButton),
        new PropertyMetadata(null, OnVariantOrSizeChanged));

    /// <summary>Size of the button. See <see cref="Variant"/> — both must be set for this to
    /// take effect.</summary>
    public EtherButtonSize? Size
    {
        get => (EtherButtonSize?)GetValue(SizeProperty);
        set => SetValue(SizeProperty, value);
    }
    public static readonly DependencyProperty SizeProperty = DependencyProperty.Register(
        nameof(Size), typeof(EtherButtonSize?), typeof(EtherButton),
        new PropertyMetadata(null, OnVariantOrSizeChanged));

    /// <summary>
    /// Resolves Variant + Size to one of the 6 named styles and assigns the whole Style
    /// property — the same thing a caller does by hand with
    /// Style="{StaticResource EtherButtonPrimarySmall}". Deliberately does NOT set MinHeight,
    /// Padding, or FontSize as separate local values: those come from the resolved Style's own
    /// setters, so they can never fight a local-value override a caller sets directly (e.g.
    /// HorizontalContentAlignment, IsHitTestVisible) the way a DP-driven per-property setter
    /// would. No-ops until both Variant and Size are set, so existing Style= call sites are
    /// entirely unaffected by this property's existence.
    /// </summary>
    private static void OnVariantOrSizeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var button = (EtherButton)d;
        if (button.Variant is not EtherButtonVariant variant || button.Size is not EtherButtonSize size)
            return;

        var key = (variant, size) switch
        {
            (EtherButtonVariant.Primary, EtherButtonSize.Large) => "EtherButtonPrimary",
            (EtherButtonVariant.Primary, EtherButtonSize.Small) => "EtherButtonPrimarySmall",
            (EtherButtonVariant.Secondary, EtherButtonSize.Large) => "EtherButtonSecondary",
            (EtherButtonVariant.Secondary, EtherButtonSize.Small) => "EtherButtonSecondarySmall",
            (EtherButtonVariant.Tertiary, EtherButtonSize.Large) => "EtherButtonTertiary",
            (EtherButtonVariant.Tertiary, EtherButtonSize.Small) => "EtherButtonTertiarySmall",
            _ => throw new ArgumentOutOfRangeException(nameof(variant)),
        };

        if (Application.Current.Resources.TryGetValue(key, out var style) && style is Style s)
            button.Style = s;
    }
}

/// <summary>Visual weight for <see cref="EtherButton.Variant"/>.</summary>
public enum EtherButtonVariant { Primary, Secondary, Tertiary }

/// <summary>Size for <see cref="EtherButton.Size"/>.</summary>
public enum EtherButtonSize { Large, Small }
```

Note the closing `}` in the snippet above closes the `EtherButton` class — the two enums are
declared as siblings of the class, inside the same `EtherSandbox.Controls` namespace block, not
nested inside it. Add `using Microsoft.UI.Xaml.Controls;` if `Style` (the type, from
`Microsoft.UI.Xaml`) or `Application` isn't already resolvable — check the existing `using`
list at the top of the file first, since `Microsoft.UI.Xaml` is already imported there.

- [ ] **Step 2: Document the new usage form in `Controls/EtherButton.xaml`'s header comment**

In the `USAGE` section of the header comment (added/relocated in Task 1), add one line after
the existing 6 usage examples:

```
      Convenience form  <controls:EtherButton Variant="Primary" Size="Small" Content="Save"/>
                        (equivalent to Style="{StaticResource EtherButtonPrimarySmall}" — set
                        BOTH Variant and Size, or neither; existing Style= call sites are
                        unaffected either way)
```

- [ ] **Step 3: Build**

Run: `dotnet build EtherComponentSandbox.csproj -c Debug`
Expected: build succeeds, 0 errors. This also validates the `switch` expression is exhaustive
(the compiler will warn on a non-exhaustive pattern match over the two enums).

- [ ] **Step 4: Confirm zero impact on existing call sites**

Run `dotnet build` and confirm no warnings changed for `Views/Controls/ButtonPage.xaml` or any
other consumer — none of them set `Variant`/`Size`, so `OnVariantOrSizeChanged` never fires for
them and their behavior is provably identical to before this task. State this explicitly in
your report rather than assuming it.

- [ ] **Step 5: Commit**

```bash
git add Controls/EtherButton.cs Controls/EtherButton.xaml
git commit -m "$(cat <<'EOF'
Add opt-in Variant/Size convenience API to EtherButton

Pins down the Variant/Size API shape the parent plan calls for in
Phase 1, using a collision-safe design: the DPs assign the whole
Style object (the same thing a caller does by hand today), never
individual properties like MinHeight/Padding/FontSize, so they can
never fight a local-value override at a call site. No-ops until both
Variant and Size are set - existing Style= call sites are completely
unaffected.

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
EOF
)"
```

---

## Task 3: Full verification sweep, consumer-inventory correction, and retrospective

**Files:**
- Modify: `docs/superpowers/plans/2026-08-06-self-contained-component-authoring-plan.md`
  (Consumer inventory correction + Phase 1 status + Revision log)

- [ ] **Step 1: Full-catalog navigation sweep**

Confirm the app builds cleanly (`dotnet build EtherComponentSandbox.csproj -c Debug`, 0
errors/warnings). Since GUI navigation can't be performed in this environment, instead do a
static sweep: grep every file listed in `ComponentCatalog.cs`'s `Nodes` array and confirm none
of them reference `Resources/EtherButton.xaml` or any Button style key that no longer exists.
Cross-reference against the grep exit gate already run in Task 1 Step 6.

- [ ] **Step 2: Correct the parent plan's Consumer inventory table**

In `docs/superpowers/plans/2026-08-06-self-contained-component-authoring-plan.md`, find the
"Consumer inventory" table (in the `## Scope` section). The row listing `EtherButton*` |
`Controls/EtherAIAssistant.xaml, Controls/EtherRightPanel.xaml` is half-wrong:
`EtherAIAssistant.xaml` is a real, live consumer (confirmed: `Style="{StaticResource
EtherButtonTertiary}"` at line 126), but `EtherRightPanel.xaml` only contains a comment
referencing `EtherButtonPrimary`, not a live `StaticResource` reference — it is not an actual
consumer today. Update that row to reflect this, and add a short note that the original
consumer inventory (written during architecture review, before this phase's implementation)
wasn't independently verified line-by-line against every claim — this task's verification
caught the discrepancy. Don't remove `EtherRightPanel.xaml` from any *future*-phase list without
checking it fresh, since it may become a real consumer later, or may have live references to
other components (e.g. Segmented Control) not checked in this task.

- [ ] **Step 3: Update Phase 1 status in the parent plan**

Add a "Status: complete" note to the Phase 1 section, listing the commits from Task 1 and Task
2, mirroring the Phase 0 status notes already present for Progress Bar and Toggle Switch.

- [ ] **Step 4: Commit the plan updates**

```bash
git add docs/superpowers/plans/2026-08-06-self-contained-component-authoring-plan.md
git commit -m "$(cat <<'EOF'
Phase 1 retrospective: correct consumer inventory, record status

EtherRightPanel.xaml was listed as a live EtherButton consumer in the
original architecture review, but only contains a comment referencing
the style, not an actual StaticResource use - corrected. Phase 1
(Button relocation + Variant/Size API) is complete.

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
EOF
)"
```

- [ ] **Step 5: Report status to the user**

Summarize: Button relocated (zero consumer impact), Variant/Size API added (opt-in, zero
consumer impact), verification results, any surprises, and readiness for Phase 2 (Checkbox,
Radio Button, Label/Filter Chip, Intelligence Button — the "no class of their own today"
batch).
