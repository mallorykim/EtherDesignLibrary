# Phase 6 Audit: Slider Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Fix the real defects in `Controls/EtherSlider.xaml.cs` that both independent
architecture reviews flagged, completing Phase 6 — the final phase of the Updated-tagged
component list.

**Architecture — this phase is different from every prior one.** `Controls/EtherSlider.xaml` +
`.xaml.cs` already live in the target location (`Controls/`, a genuine `UserControl` with
`x:Class`, no `App.xaml` merge entry at all — confirmed via grep, none exists). **There is no
relocation work in this phase.** The parent plan's own Phase 6 text says to audit and fix
`EtherSlider`, "not confirm it already satisfies the model" — this plan does exactly that.

**Confirmed defects (verified against the actual token values in `Resources/EtherColors.xaml`,
not assumed):**

1. **Pressed-knob color is a genuine mismatch, not just an inconsistency.**
   `Controls/EtherSlider.xaml.cs:51` hardcodes `Color.FromArgb(0xFF, 0x04, 0x68, 0xDA)` (`#0468DA`)
   for the pressed-knob brush. The actual design-system token `ActionPrimaryBgPressed`
   (`Resources/EtherColors.xaml:92,210`) is `#FF0054E5` — a different blue. This is a real,
   visible defect: the slider's pressed-knob color has never matched the design system's actual
   pressed-state color.

2. **Normal-knob/highlight color is hardcoded but happens to currently match the token.**
   `Color.FromArgb(0xFF, 0x0D, 0x62, 0xFF)` (`#0D62FF`, used at lines 50 and 72) exactly equals
   `ActionPrimaryBg` (`#FF0D62FF`, identical in both Light and Dark theme dictionaries). No
   visible bug today, but it's fragile (won't follow a future token change) and doesn't respect
   High Contrast mode (where `ActionPrimaryBg` resolves to `SystemColorHighlightColor`, a
   genuinely different value) — since it's hardcoded rather than read from the token.

3. **The theme-repaint mechanism is architecturally unsound, independent of whether it currently
   produces a visible bug.** `GetThemeBrush()` (`Controls/EtherSlider.xaml.cs:240-245`) calls
   `Application.Current.Resources.TryGetValue(resourceKey, ...)` directly on the flat merged
   dictionary. `Resources/EtherColors.xaml`'s brushes live inside
   `<ResourceDictionary.ThemeDictionaries>` (keyed `"Light"`/`"Dark"`/`"HighContrast"`,
   `EtherColors.xaml:24,143,275`), and this app's theme toggle sets `RootGrid.RequestedTheme`
   locally (`MainWindow.xaml.cs:174`), not `Application.Current.RequestedTheme` globally. A
   direct `Application.Current.Resources.TryGetValue` call is not guaranteed to reflect that
   local override correctly, and — separately, this is the bigger issue — **there is no
   `ActualThemeChanged` subscription anywhere in `EtherSlider`**, so even a theoretically-correct
   lookup would only ever run once (at construction/first render) and never again when the user
   toggles the theme at runtime. `BackgroundTrack` genuinely differs between Light
   (`#FFBECCD7`) and Dark (`#FF242930`) — this is the one color in this file where the bug is
   concretely visible: the slider's unselected-bar color will not update when the user toggles
   themes after the slider has already rendered once.

4. **The header doc comment is stale and states a wrong value.** `Controls/EtherSlider.xaml:17`
   claims `Knob fill: ActionPrimaryBg (#FF0077FF)` — the real token value is `#FF0D62FF`, not
   `#FF0077FF`. Also `Controls/EtherSlider.xaml:13` claims `Highlighted bar fill #990077FF
   (ActionPrimaryBg at 60%)` — implying 60% opacity — but the actual code renders highlighted
   bars at full opacity (`0xFF` alpha, `Controls/EtherSlider.xaml.cs:72`), no 60% reduction
   anywhere. **Do not change the rendered opacity to match the comment** — that would be a
   visual redesign decision outside this refactor's charter (the parent plan is explicit: no
   visual redesign). Instead, correct the comment to describe what the code actually and
   correctly renders.

**What this phase does NOT do:** no visual redesign, no opacity change to match the stale
60%-claim in the old comment, no new features. The pressed-color and theme-repaint fixes are
explicitly in-scope per the parent plan's own Phase 6 text ("fix these as part of this phase")
and both independent architecture reviews that shaped this plan — they are defect fixes to
already-broken/fragile behavior, not new design decisions.

**Tech Stack:** WinUI3 / .NET 8. No test framework — verification is `dotnet build` + code
inspection confirming the fix is architecturally correct (this environment cannot visually
confirm the pressed-color or theme-toggle fix renders correctly — that's deferred to the user
running the app, same limitation as every other phase this session).

---

## Task 1: Fix `EtherSlider`'s theme-color resolution and the pressed-color mismatch

**Files:**
- Modify: `Controls/EtherSlider.xaml.cs`
- Modify: `Controls/EtherSlider.xaml` (header comment only)

- [ ] **Step 1: Replace `GetThemeBrush` with a theme-correct `GetThemeColor` helper**

Read the current file. Replace the existing `GetThemeBrush` method (lines 240-245):

```csharp
    private SolidColorBrush? GetThemeBrush(string resourceKey)
    {
        if (Application.Current.Resources.TryGetValue(resourceKey, out var res) && res is SolidColorBrush b)
            return b;
        return null;
    }
```

with:

```csharp
    /// <summary>
    /// Resolves a token's Color for the element's CURRENT effective theme, indexing
    /// Application.Current.Resources.ThemeDictionaries directly by ActualTheme rather than
    /// doing a flat Application.Current.Resources lookup (which is not guaranteed to reflect
    /// a local RequestedTheme override like RootGrid.RequestedTheme in MainWindow.xaml.cs).
    /// ActualTheme is always resolved to Light or Dark (never ElementTheme.Default), so only
    /// those two branches are needed. High Contrast is not handled here — this app's runtime
    /// theme toggle only switches Light/Dark, and ActualTheme cannot report High Contrast the
    /// way the declarative {ThemeResource} markup extension can; out of scope for this fix.
    /// </summary>
    private Color GetThemeColor(string resourceKey, Color fallback)
    {
        var themeKey = ActualTheme == ElementTheme.Dark ? "Dark" : "Light";
        if (Application.Current.Resources.ThemeDictionaries.TryGetValue(themeKey, out var dictObj) &&
            dictObj is ResourceDictionary themeDict &&
            themeDict.TryGetValue(resourceKey, out var res) &&
            res is SolidColorBrush b)
        {
            return b.Color;
        }
        return fallback;
    }
```

- [ ] **Step 2: Make the knob brushes mutable fields, resolved via the token**

Replace:

```csharp
    private readonly SolidColorBrush _knobNormalBrush;
    private readonly SolidColorBrush _knobPressedBrush;
```

with:

```csharp
    private readonly SolidColorBrush _knobNormalBrush = new(Colors.Transparent);
    private readonly SolidColorBrush _knobPressedBrush = new(Colors.Transparent);
```

(Still `readonly` — only the *object* stays fixed, its `.Color` property gets mutated in place
by `RefreshThemeBrushes` below, which is what makes it "live" across theme changes: any
`Rectangle.Fill` already pointing at this brush instance repaints automatically when its
`.Color` changes, no re-render needed for the knob specifically.)

- [ ] **Step 3: Replace the constructor's hardcoded color assignment with a theme-aware refresh, and subscribe to theme changes**

Replace:

```csharp
    public EtherSlider()
    {
        this.InitializeComponent();
        _knobNormalBrush = new SolidColorBrush(Color.FromArgb(0xFF, 0x0D, 0x62, 0xFF));
        _knobPressedBrush = new SolidColorBrush(Color.FromArgb(0xFF, 0x04, 0x68, 0xDA));
        this.ProtectedCursor = InputSystemCursor.Create(InputSystemCursorShape.Hand);
        this.Loaded += (_, _) => RenderBars();
    }
```

with:

```csharp
    public EtherSlider()
    {
        this.InitializeComponent();
        RefreshThemeBrushes();
        this.ProtectedCursor = InputSystemCursor.Create(InputSystemCursorShape.Hand);
        // RenderBars() recreates its own local highlight/inactive brushes from the current
        // theme every call, and RefreshThemeBrushes() mutates the long-lived knob brushes in
        // place - together these keep every color on this control in sync when the user
        // toggles Light/Dark at runtime (RootGrid.RequestedTheme in MainWindow.xaml.cs).
        this.ActualThemeChanged += (_, _) =>
        {
            RefreshThemeBrushes();
            RenderBars();
        };
        this.Loaded += (_, _) => RenderBars();
    }

    /// <summary>Fixed defect: the pressed-knob color was previously hardcoded to #0468DA, which
    /// does not match the design system's actual ActionPrimaryBgPressed token (#0054E5). Both
    /// brushes now read their color from the token, with the pre-existing hardcoded values kept
    /// only as a fallback for the (should-never-happen) case the token is missing.</summary>
    private void RefreshThemeBrushes()
    {
        _knobNormalBrush.Color = GetThemeColor("ActionPrimaryBg", Color.FromArgb(0xFF, 0x0D, 0x62, 0xFF));
        _knobPressedBrush.Color = GetThemeColor("ActionPrimaryBgPressed", Color.FromArgb(0xFF, 0x00, 0x54, 0xE5));
    }
```

- [ ] **Step 4: Update `RenderBars()` to resolve its local brushes the same way**

Replace:

```csharp
        // Resolved theme colors
        var highlightBrush = new SolidColorBrush(Color.FromArgb(0xFF, 0x0D, 0x62, 0xFF));
        var inactiveBrush = GetThemeBrush("BackgroundTrack")
            ?? new SolidColorBrush(Color.FromArgb(0xFF, 0xBE, 0xCC, 0xD7));
```

with:

```csharp
        // Resolved theme colors — re-resolved on every call (Value change or theme change),
        // so these two always reflect the current ActualTheme.
        var highlightBrush = new SolidColorBrush(GetThemeColor("ActionPrimaryBg", Color.FromArgb(0xFF, 0x0D, 0x62, 0xFF)));
        var inactiveBrush = new SolidColorBrush(GetThemeColor("BackgroundTrack", Color.FromArgb(0xFF, 0xBE, 0xCC, 0xD7)));
```

- [ ] **Step 5: Correct the stale header comment in `Controls/EtherSlider.xaml`**

The current header (lines 1-20) contains two inaccuracies. Change:

```
      Highlighted  bar fill #990077FF (ActionPrimaryBg at 60%)
      Unselected   bar fill BackgroundTrack

    TOKENS:
      Knob fill:        ActionPrimaryBg (#FF0077FF)
      Highlight fill:   #990077FF
      Unselected fill:  BackgroundTrack
```

to:

```
      Highlighted  bar fill ActionPrimaryBg, full opacity (same color as the knob)
      Unselected   bar fill BackgroundTrack

    TOKENS (resolved live from the current theme via GetThemeColor, not hardcoded):
      Knob fill:        ActionPrimaryBg (#FF0D62FF in Light/Dark; both currently identical)
      Knob fill, pressed: ActionPrimaryBgPressed (#FF0054E5)
      Highlight fill:   ActionPrimaryBg, same as knob fill, full opacity
      Unselected fill:  BackgroundTrack (differs by theme: #FFBECCD7 Light / #FF242930 Dark)
```

This corrects the wrong hex value the old comment stated (`#FF0077FF` was never the real value
— `#FF0D62FF` always was) and removes the false 60%-opacity claim, without changing any actual
rendered color.

- [ ] **Step 6: Build**

`dotnet build EtherComponentSandbox.csproj -c Debug` — expect 0 errors. Also confirm no unused
`using` warnings appear from removing the old `GetThemeBrush` method (check whether
`Microsoft.UI.Xaml.Media` or other usings become unused — they won't, `SolidColorBrush` and
`Color` are still used throughout).

- [ ] **Step 7: Self-verify the fix is architecturally sound (no GUI available)**

Since this environment cannot render the app, verify via reasoning + code inspection rather than
a screenshot:
- Confirm `ActionPrimaryBgPressed` really is `#FF0054E5` in `Resources/EtherColors.xaml` (grep
  it) and that the new `RefreshThemeBrushes()` reads exactly that key.
- Confirm `ActualThemeChanged` is a real event on `UserControl`/`FrameworkElement` (it is —
  standard WinUI3 API) and that subscribing to it in the constructor is safe (no risk of
  double-subscription, since this runs once per instance).
- Confirm `Application.Current.Resources.ThemeDictionaries` is the correct API surface for
  manually indexing a themed resource by key (it is — `ResourceDictionary.ThemeDictionaries` is
  a `IDictionary<object, object>` of theme-name → nested `ResourceDictionary`, and this is the
  standard low-level way to do what `{ThemeResource}` does declaratively).
- Confirm `RenderBars()` is still called at exactly the same times as before (`Value` change,
  `Loaded`) plus the new `ActualThemeChanged` trigger — no change to the existing call sites.

- [ ] **Step 8: Commit**

```bash
git add Controls/EtherSlider.xaml.cs Controls/EtherSlider.xaml
git commit -m "$(cat <<'EOF'
Fix EtherSlider's hardcoded pressed-color mismatch and theme-repaint bug

Phase 6 (final phase) of the self-contained component authoring plan.
Two real defects, both flagged by independent architecture review and
confirmed against the actual token values in EtherColors.xaml:

1. The pressed-knob color was hardcoded to #0468DA, which never
   matched the real ActionPrimaryBgPressed token (#0054E5).
2. Colors were resolved once via a flat Application.Current.Resources
   lookup with no ActualThemeChanged subscription, so toggling the
   app's Light/Dark theme at runtime never repainted the slider -
   most visibly wrong for BackgroundTrack, which genuinely differs
   between themes (#FFBECCD7 Light / #FF242930 Dark).

Fixed by resolving colors through Application.Current.Resources
.ThemeDictionaries indexed by ActualTheme (the correct low-level
equivalent of what {ThemeResource} does declaratively), and
subscribing to ActualThemeChanged to re-resolve and re-render when the
theme changes. Also corrected a stale header comment that stated the
wrong hex value for ActionPrimaryBg and a 60%-opacity claim that
never matched the code's actual full-opacity rendering - comment
fixed to match existing (unchanged) visual behavior, not the other
way around, since changing rendered opacity would be a visual
redesign outside this refactor's scope.

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
EOF
)"
```

---

## Task 2: Retrospective — final phase of the Updated-tagged component sweep

**Files:**
- Modify: `docs/superpowers/plans/2026-08-06-self-contained-component-authoring-plan.md`
  (Phase 6 status)

- [ ] **Step 1: Final build check**

`dotnet build EtherComponentSandbox.csproj -c Debug` — expect 0 errors.

- [ ] **Step 2: Update the parent plan's Phase 6 section and add a closing summary**

Add a "Status: done" note to Phase 6, and since this is the last of the six Updated-tagged
components (Progress Bar, Toggle Switch, Button, Checkbox, Radio Button, Intelligence Button,
Dropdown, Segmented Control, Input, Slider — Scroll Bar excluded by design, Label/Filter Chip
deferred per user instruction), add a short closing note listing what's done and what's
deliberately not: all 10 Updated-tagged components except Scroll Bar are migrated; Label/Filter
Chip remains open for a future round if the user wants it.

- [ ] **Step 3: Commit the plan update**

```bash
git add docs/superpowers/plans/2026-08-06-self-contained-component-authoring-plan.md
git commit -m "$(cat <<'EOF'
Phase 6 retrospective: record status, close out Updated-tagged sweep

Slider's pressed-color and theme-repaint bugs are fixed. This
completes the Updated-tagged component migration: Progress Bar,
Toggle Switch, Button, Checkbox, Radio Button, Intelligence Button,
Dropdown, Segmented Control, Input, and Slider are all done. Scroll
Bar stays permanently out of scope (app-wide implicit framework
style). Label/Filter Chip remains open for a future round.

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
EOF
)"
```

- [ ] **Step 4: Report status to the user**

Summarize the fix, confirm it's architecturally verified (build + reasoning, no GUI available),
and give a full closing summary of everything done across all 6 phases this session.
