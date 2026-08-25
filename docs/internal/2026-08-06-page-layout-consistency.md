# Sandbox Page Layout Consistency Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make every sandbox leaf page under `Views/**` use the design system's existing named typography styles (`Resources/EtherTypography.xaml`) and spacing/radius tokens (`Resources/EtherSpacing.xaml`) instead of hand-typed inline values, so the same visual role (e.g. a caption above a swatch grid) renders identically everywhere. Also restores ~35 strings where an encoding issue mangled `—`/`×`/`–`/`−` into a literal `=`, adds the missing Disabled toggle to `LabelFilterChipPage`, and points two duplicated inline gradients at the existing `EtherBarThickGradient` resource.

**Architecture:** Pure XAML edits, file by file. No new resources, styles, or components — every substitution points at a token/style that already exists in `Resources/EtherTypography.xaml`, `Resources/EtherSpacing.xaml`, or `Resources/EtherDataGraphics.xaml`, all three already globally merged in `App.xaml`. `Views/DataDisplay/*` and `Views/Navigation/*` internal layout numbers are explicitly untouched (they match the documented Figma source in `EtherDataGraphics.xaml` verbatim) — those folders only get the text-garbling fix where it applies.

**Tech Stack:** WinUI3 / XAML, .NET 8, `EtherComponentSandbox.csproj`. No test project exists; verification is `dotnet build` (compiles + XAML-binds) plus a visual pass you do yourself at the end, since this session has no way to drive a live WinUI3 window.

---

## Two established substitution rules (used throughout)

**Rule A — group-header caption.** Every occurrence of this exact inline triplet:
```xml
FontFamily="{StaticResource InterFont}"
FontSize="11" FontWeight="SemiBold"
Foreground="{ThemeResource TextTertiary}"
```
becomes:
```xml
Style="{StaticResource EtherMicroSemiBold}"
Foreground="{ThemeResource TextTertiary}"
```

**Rule B — per-specimen label.** Every occurrence of this exact inline pair:
```xml
FontFamily="{StaticResource InterFont}"
FontSize="11" Foreground="{ThemeResource TextTertiary}"
```
(no `FontWeight`) becomes:
```xml
Style="{StaticResource EtherMicroRegular}"
Foreground="{ThemeResource TextTertiary}"
```
Any trailing attribute on the same element (e.g. `HorizontalAlignment="Center"`) stays untouched, just re-indented if needed.

**Not touched by either rule:** `ComponentPage.xaml`'s own INTERACTIVE/STATES header style (10px + `CharacterSpacing="80"`) and its reuse for `ButtonPage`'s nested LARGE/SMALL sub-headers — that combination has no matching named style, is already visually consistent everywhere it appears, and forcing it to Rule A would collapse two distinct nesting depths into one style. Leave every `FontSize="10" FontWeight="SemiBold" CharacterSpacing="80"` block exactly as-is.

---

### Task 1: `Views/Controls/ButtonPage.xaml`

**Files:**
- Modify: `Views/Controls/ButtonPage.xaml`

- [ ] **Step 1: Apply Rule A to the 6 group-header captions**

  Occurrences (identical 3-line suffix, apply `replace_all`): `Text="PRIMARY"` (×2, lines 28 and 123), `Text="SECONDARY"` (×2, lines 57 and 241), `Text="TERTIARY"` (×2, lines 86 and 359). Find:
  ```xml
                             FontFamily="{StaticResource InterFont}"
                             FontSize="11" FontWeight="SemiBold"
                             Foreground="{ThemeResource TextTertiary}"/>
  ```
  Replace with:
  ```xml
                             Style="{StaticResource EtherMicroSemiBold}"
                             Foreground="{ThemeResource TextTertiary}"/>
  ```
  This exact 4-space-less-indented block (23 spaces before `FontFamily`) appears once per header line — verify all 6 land correctly by re-reading the file after the edit.

- [ ] **Step 2: Apply Rule B to the InteractiveContent "Large"/"Small" labels (4 occurrences, 23-space indent)**

  Find (appears for `Text="Large"` ×3 and `Text="Small"` ×3 inside `InteractiveContent`):
  ```xml
                                       FontFamily="{StaticResource InterFont}"
                                       FontSize="11" Foreground="{ThemeResource TextTertiary}"
                                       HorizontalAlignment="Center"/>
  ```
  Replace with:
  ```xml
                                       Style="{StaticResource EtherMicroRegular}"
                                       Foreground="{ThemeResource TextTertiary}"
                                       HorizontalAlignment="Center"/>
  ```
  Use `replace_all` — this pattern is character-identical for all 6 (Large ×3, Small ×3).

- [ ] **Step 3: Apply Rule B to the StatesContent "Default"/"Hover"/"Pressed"/"Disabled" labels (24 occurrences, 27-space indent, one nesting level deeper)**

  Find (appears 24 times — 3 variants × 2 sizes × 4 states):
  ```xml
                                           FontFamily="{StaticResource InterFont}"
                                           FontSize="11" Foreground="{ThemeResource TextTertiary}"
                                           HorizontalAlignment="Center"/>
  ```
  Replace with:
  ```xml
                                           Style="{StaticResource EtherMicroRegular}"
                                           Foreground="{ThemeResource TextTertiary}"
                                           HorizontalAlignment="Center"/>
  ```
  Use `replace_all`.

- [ ] **Step 4: Leave every `FontSize="10" FontWeight="SemiBold" CharacterSpacing="80"` LARGE/SMALL sub-header (in StatesContent) untouched — do not apply Rule A there.**

- [ ] **Step 5: Build**

  Run: `cd /c/Agroa/ether-sandbox && dotnet build EtherComponentSandbox.csproj -c Debug`
  Expected: `Build succeeded. 0 Warning(s) 0 Error(s)`

- [ ] **Step 6: Commit**

```bash
git add Views/Controls/ButtonPage.xaml
git commit -m "Use EtherMicro caption styles in ButtonPage instead of inline properties"
```

---

### Task 2: `Views/Controls/CheckboxPage.xaml`

**Files:**
- Modify: `Views/Controls/CheckboxPage.xaml`

- [ ] **Step 1: Apply Rule A to the 2 group-header captions**

  `Text="UNCHECKED"` (line 21) and `Text="CHECKED"` (line 74), each followed by:
  ```xml
                               FontFamily="{StaticResource InterFont}"
                               FontSize="11" FontWeight="SemiBold"
                               Foreground="{ThemeResource TextTertiary}"/>
  ```
  → Rule A replacement (see top of document). `replace_all`.

- [ ] **Step 2: Apply Rule B to the 7 per-specimen labels** (`Default` ×2, `Hover` ×2, `Pressed` ×1, `Disabled` ×2), each followed by:
  ```xml
                                       FontFamily="{StaticResource InterFont}"
                                       FontSize="11" Foreground="{ThemeResource TextTertiary}"
                                       HorizontalAlignment="Center"/>
  ```
  → Rule B replacement. `replace_all`.

- [ ] **Step 3: Unify the outer `StatesContent` spacing token**

  Find: `<StackPanel Spacing="20">` (line 17, the root of `StatesContent`)
  Replace: `<StackPanel Spacing="{StaticResource SpaceLg}">`

  (`SpaceLg` = 24, matching `ButtonPage`'s equivalent outer spacing for the same "list of variant-group blocks" role — was previously 20, an off-scale value.)

- [ ] **Step 4: Build** — same command as Task 1, Step 5.

- [ ] **Step 5: Commit**

```bash
git add Views/Controls/CheckboxPage.xaml
git commit -m "Use EtherMicro caption styles and SpaceLg token in CheckboxPage"
```

---

### Task 3: `Views/Controls/RadioButtonPage.xaml`

**Files:**
- Modify: `Views/Controls/RadioButtonPage.xaml`

- [ ] **Step 1: Apply Rule A** to `Text="UNCHECKED"` (line 27) and `Text="CHECKED"` (line 84). `replace_all`.

- [ ] **Step 2: Apply Rule B** to the 7 per-specimen labels (`Default` ×2, `Hover` ×2, `Pressed` ×1, `Disabled` ×2). `replace_all`.

- [ ] **Step 3: Unify spacing token** — `<StackPanel Spacing="20">` (line 22, root of `StatesContent`) → `<StackPanel Spacing="{StaticResource SpaceLg}">`.

- [ ] **Step 4: Build** — same command.

- [ ] **Step 5: Commit**

```bash
git add Views/Controls/RadioButtonPage.xaml
git commit -m "Use EtherMicro caption styles and SpaceLg token in RadioButtonPage"
```

---

### Task 4: `Views/Controls/ToggleSwitchPage.xaml`

**Files:**
- Modify: `Views/Controls/ToggleSwitchPage.xaml`

- [ ] **Step 1: Apply Rule A** to `Text="OFF"` (line 23) and `Text="ON"` (line 84). `replace_all`.

- [ ] **Step 2: Apply Rule B** to the 8 per-specimen labels (`Default` ×2, `Hover` ×2, `Pressed` ×2, `Disabled` ×2). `replace_all`.

- [ ] **Step 3: Unify spacing token** — `<StackPanel Spacing="20">` (line 19, root of `StatesContent`) → `<StackPanel Spacing="{StaticResource SpaceLg}">`.

- [ ] **Step 4: Build** — same command.

- [ ] **Step 5: Commit**

```bash
git add Views/Controls/ToggleSwitchPage.xaml
git commit -m "Use EtherMicro caption styles and SpaceLg token in ToggleSwitchPage"
```

---

### Task 5: `Views/Controls/DropdownPage.xaml`

**Files:**
- Modify: `Views/Controls/DropdownPage.xaml`

- [ ] **Step 1: Apply Rule B to the 8 STATES specimen labels** (`Default`, `Hover`, `Pressed`, `Open (active)`, `Open / item hover`, `Open / item pressed`, `Open / scrollbar`, `Disabled` — each followed by):
  ```xml
                               FontFamily="{StaticResource InterFont}"
                               FontSize="11" Foreground="{ThemeResource TextTertiary}"
                               HorizontalAlignment="Center"/>
  ```
  → Rule B replacement (21-space indent — one nesting level shallower than Button/Checkbox since Dropdown's STATES row isn't double-nested). `replace_all`.

- [ ] **Step 2: Do NOT touch `Page.Resources`** (`DropdownMenuPreviewContainer`, `DropdownMenuPreviewItem`, `DropdownMenuPreviewText`) — that's a distinct role (simulated live menu-item text matching the real `EtherDropdownItem` control), not a specimen caption. Leave `CharacterSpacing="20"` and `FontWeight="Medium"` as-is.

- [ ] **Step 3: Build** — same command.

- [ ] **Step 4: Commit**

```bash
git add Views/Controls/DropdownPage.xaml
git commit -m "Use EtherMicroRegular style for DropdownPage state labels"
```

---

### Task 6: `Views/Controls/InputPage.xaml`

**Files:**
- Modify: `Views/Controls/InputPage.xaml`

- [ ] **Step 1: Apply Rule B to the 5 STATES labels** (`Default`, `Hover`, `Active`, `Filled`, `Disabled`). Note: this file's version has no `HorizontalAlignment="Center"` trailing line — find:
  ```xml
                               FontFamily="{StaticResource InterFont}"
                               FontSize="11" Foreground="{ThemeResource TextTertiary}"/>
  ```
  Replace:
  ```xml
                               Style="{StaticResource EtherMicroRegular}"
                               Foreground="{ThemeResource TextTertiary}"/>
  ```
  `replace_all`.

- [ ] **Step 2: Build** — same command.

- [ ] **Step 3: Commit**

```bash
git add Views/Controls/InputPage.xaml
git commit -m "Use EtherMicroRegular style for InputPage state labels"
```

---

### Task 7: `Views/Controls/IntelligenceButtonPage.xaml`

**Files:**
- Modify: `Views/Controls/IntelligenceButtonPage.xaml`

- [ ] **Step 1: Apply Rule B to the 6 labels** (`Functional`, `With longer label` in InteractiveContent; `Default`, `Hover`, `Pressed`, `Disabled` in StatesContent), each followed by:
  ```xml
                               FontFamily="{StaticResource InterFont}"
                               FontSize="11" Foreground="{ThemeResource TextTertiary}"
                               HorizontalAlignment="Center"/>
  ```
  → Rule B replacement. `replace_all`.

- [ ] **Step 2: Leave the `Content="=  Optimise my battery"` string on the `HandButton` untouched.** That leading `=` is a mis-rendered icon glyph (not a text-encoding issue) — out of scope for this pass, flagged separately (see end of this plan).

- [ ] **Step 3: Build** — same command.

- [ ] **Step 4: Commit**

```bash
git add Views/Controls/IntelligenceButtonPage.xaml
git commit -m "Use EtherMicroRegular style for IntelligenceButtonPage labels"
```

---

### Task 8: `Views/Controls/SliderPage.xaml`

**Files:**
- Modify: `Views/Controls/SliderPage.xaml`

- [ ] **Step 1: Apply Rule B to the 3 labels** (`50% (interactive = drag me)`, `25%`, `75%`), each followed by:
  ```xml
                           FontFamily="{StaticResource InterFont}"
                           FontSize="11" Foreground="{ThemeResource TextTertiary}"/>
  ```
  → Rule B replacement (no trailing `HorizontalAlignment`). `replace_all`.

- [ ] **Step 2: Fix garbled text** — the encoding issue turned an intended em dash into `=`. This is also the only in-scope file where the label text itself changes.

  Find: `Text="50% (interactive = drag me)"`
  Replace: `Text="50% (interactive — drag me)"`

- [ ] **Step 3: Build** — same command.

- [ ] **Step 4: Commit**

```bash
git add Views/Controls/SliderPage.xaml
git commit -m "Use EtherMicroRegular style in SliderPage; fix garbled em dash"
```

---

### Task 9: `Views/Controls/SegmentedControlPage.xaml`

**Files:**
- Modify: `Views/Controls/SegmentedControlPage.xaml`

- [ ] **Step 1: Apply Rule A to the 3 InteractiveContent group headers** (`2 SEGMENTS`, `3 SEGMENTS`, `4 SEGMENTS`), each followed by:
  ```xml
                               FontFamily="{StaticResource InterFont}"
                               FontSize="11" FontWeight="SemiBold"
                               Foreground="{ThemeResource TextTertiary}"/>
  ```
  → Rule A replacement. `replace_all`.

- [ ] **Step 2: Fix the wrong font family on the 4 StatesContent labels** (`Default`, `Selected`, `Hover`, `Pressed`) — these are the only captions in the whole app using `InstrumentSans` where every other caption uses Inter. Find (appears 4×, `replace_all`):
  ```xml
                               FontFamily="{StaticResource InstrumentSans}"
                               FontSize="11" FontWeight="SemiBold"
                               Foreground="{ThemeResource TextTertiary}"
                               HorizontalAlignment="Center"/>
  ```
  Replace:
  ```xml
                               Style="{StaticResource EtherMicroSemiBold}"
                               Foreground="{ThemeResource TextTertiary}"
                               HorizontalAlignment="Center"/>
  ```

- [ ] **Step 3: Build** — same command.

- [ ] **Step 4: Commit**

```bash
git add Views/Controls/SegmentedControlPage.xaml
git commit -m "Fix SegmentedControlPage state labels using wrong font family; adopt EtherMicro styles"
```

---

### Task 10: `Views/Controls/LabelFilterChipPage.xaml`

**Files:**
- Modify: `Views/Controls/LabelFilterChipPage.xaml`

- [ ] **Step 1: Add the missing Disabled toggle**, matching the pattern every other interactive Controls page uses. Find:
  ```xml
      <views:ComponentPage
          Title="Label / Filter Chip"
          Description="ToggleButton used as a filter chip or badge. Click to toggle. Shows Checked = Unchecked transition.">
  ```
  Replace:
  ```xml
      <views:ComponentPage
          Title="Label / Filter Chip"
          HasDisabledToggle="True"
          Description="ToggleButton used as a filter chip or badge. Click to toggle. Shows Checked — Unchecked transition.">
  ```
  (This also fixes the garbled `=` in the same edit — no code-behind wiring needed: `ComponentPage`'s existing `InteractiveHost.IsEnabled` cascade already disables whatever is in `InteractiveContent`, confirmed by reading `Views/ComponentPage.xaml.cs`.)

- [ ] **Step 2: Apply Rule A** to `Text="FILTER CHIP GROUP (click any chip)"` (line 13):
  ```xml
                             FontFamily="{StaticResource InterFont}"
                             FontSize="11" FontWeight="SemiBold"
                             Foreground="{ThemeResource TextTertiary}"/>
  ```
  → Rule A replacement.

- [ ] **Step 3: Apply Rule B to the 3 StatesContent labels** (`Unchecked`, `Checked`, `Disabled`), each followed by (no trailing `HorizontalAlignment`):
  ```xml
                               FontFamily="{StaticResource InterFont}"
                               FontSize="11" Foreground="{ThemeResource TextTertiary}"/>
  ```
  → Rule B replacement. `replace_all`.

- [ ] **Step 4: Build** — same command.

- [ ] **Step 5: Commit**

```bash
git add Views/Controls/LabelFilterChipPage.xaml
git commit -m "Add Disabled toggle to LabelFilterChipPage; adopt EtherMicro styles; fix garbled dash"
```

---

### Task 11: `Views/Foundations/ColorsPage.xaml`

**Files:**
- Modify: `Views/Foundations/ColorsPage.xaml`

- [ ] **Step 1: Replace the 6 section headers with `EtherH4`.** Each of `Text="Background"`, `Text="Text"`, `Text="Action = Primary"`, `Text="Status"`, `Text="AI Accent"`, `Text="Border"` is followed by:
  ```xml
                               FontSize="16" FontWeight="SemiBold"
                               Foreground="{ThemeResource TextPrimary}"/>
  ```
  and preceded on the same line by `FontFamily="{StaticResource InstrumentSans}"`. This exact combination (`InstrumentSans` + 16 + SemiBold) is `EtherH4` verbatim. For each of the 6, change:
  ```xml
                    <TextBlock Text="Background" FontFamily="{StaticResource InstrumentSans}"
                               FontSize="16" FontWeight="SemiBold"
                               Foreground="{ThemeResource TextPrimary}"/>
  ```
  to:
  ```xml
                    <TextBlock Text="Background"
                               Style="{StaticResource EtherH4}"
                               Foreground="{ThemeResource TextPrimary}"/>
  ```
  Repeat for `Text="Text"`, `Text="Action — Primary"` (garbled-text fix applied here too, see Step 2), `Text="Status"`, `Text="AI Accent"`, `Text="Border"` — same attribute shape each time, only the `Text=` value differs.

- [ ] **Step 2: Fix garbled text** (2 instances):

  Find: `Description="Semantic ThemeResource tokens = toggle Light/Dark in the pane footer to verify mode-adaptive swaps.">`
  Replace: `Description="Semantic ThemeResource tokens — toggle Light/Dark in the pane footer to verify mode-adaptive swaps.">`

  Find: `Text="Action = Primary"`
  Replace: `Text="Action — Primary"`
  (Apply as part of Step 1's edit for that specific header, not a separate pass.)

- [ ] **Step 3: Point the Intelligence gradient swatch at the shared resource** instead of retyping the stops. Find:
  ```xml
                            <Border Width="80" Height="48" CornerRadius="6">
                                <Border.Background>
                                    <LinearGradientBrush StartPoint="0,0.5" EndPoint="1,0.5">
                                        <GradientStop Color="#8FFFC3" Offset="0.00"/>
                                        <GradientStop Color="#0EB2FF" Offset="0.25"/>
                                        <GradientStop Color="#0015FF" Offset="0.75"/>
                                        <GradientStop Color="#141414" Offset="1.00"/>
                                    </LinearGradientBrush>
                                </Border.Background>
                            </Border>
  ```
  Replace:
  ```xml
                            <Border Width="80" Height="48" CornerRadius="6"
                                    Background="{StaticResource EtherBarThickGradient}"/>
  ```
  (Note: this changes the gradient's stop offsets slightly — from this page's ad hoc 0/.25/.75/1.00 to the canonical 0/.09/.92/1.05 used by the real chart component — so the swatch now shows the actual token instead of an approximation. Small, intentional visual correction, not a no-op.)

- [ ] **Step 4: Do not touch the `SwatchLabel` / `SwatchGroup` `Page.Resources` styles** — already centralized, already a good pattern, out of scope.

- [ ] **Step 5: Build** — same command.

- [ ] **Step 6: Commit**

```bash
git add Views/Foundations/ColorsPage.xaml
git commit -m "Use EtherH4 for ColorsPage section headers; fix garbled dash; share intelligence gradient resource"
```

---

### Task 12: `Views/Foundations/TypographyPage.xaml`

**Files:**
- Modify: `Views/Foundations/TypographyPage.xaml`

- [ ] **Step 1: Apply Rule A to the 12 row-label headers** (`Display XL`, `H1`, `H2`, `H3`, `H4`, `H5`, `H6` in InteractiveContent; `Body XL Regular`, `Body M Regular`, `Body M SemiBold`, `Body S Regular`, `Micro SemiBold` in StatesContent). Each is followed by:
  ```xml
                                   FontSize="11" FontWeight="SemiBold"
                                   Foreground="{ThemeResource TextTertiary}"/>
  ```
  preceded by `FontFamily="{StaticResource InterFont}"` on the `Text=` line. Change each from:
  ```xml
                          <TextBlock Text="Display XL" FontFamily="{StaticResource InterFont}"
                                     FontSize="11" FontWeight="SemiBold"
                                     Foreground="{ThemeResource TextTertiary}"/>
  ```
  to:
  ```xml
                          <TextBlock Text="Display XL"
                                     Style="{StaticResource EtherMicroSemiBold}"
                                     Foreground="{ThemeResource TextTertiary}"/>
  ```
  Repeat for all 12 (`H1` through `H6`, `Body XL Regular`, `Body M Regular`, `Body M SemiBold`, `Body S Regular`, `Micro SemiBold`).

- [ ] **Step 2: Do NOT touch the two self-referential top labels** — `Text="Instrument Sans"` (line 13, deliberately rendered `FontFamily="{StaticResource InstrumentSans}"` to demonstrate the font by its own name) and `Text="Inter"` (line 151, `FontFamily="{StaticResource InterFont}"` for the same reason). Applying Rule A here would force `FontFamily=InterFont` onto the "Instrument Sans" label, breaking the intentional self-demo. Leave both exactly as they are.

- [ ] **Step 3: Do NOT touch the 10px detail-caption line below each row label** (e.g. `Text="96 px — Bold — −40 ls"` at `FontSize="10"`) beyond the text fix in Step 5 — that's a distinct, deliberately smaller tier with no matching named style, and it's already consistent everywhere it's used in this file.

- [ ] **Step 4: Unify the row-gap margin.** InteractiveContent's rows use `Margin="0,0,0,20"`, StatesContent's use `Margin="0,0,0,16"` for the identical "specimen row" pattern. Standardize both to the token `SpaceMd` (16 — the value StatesContent already uses).

  In InteractiveContent, find (appears 6 times — the label `Text="Instrument Sans"` line, and the `Grid Margin="0,0,0,20"` opening tag for each of the first 6 rows; the 7th/last row, H6, already has no trailing margin, leave it):
  ```xml
                           Margin="0,0,0,20"/>
  ```
  This exact string appears once, for the top `Text="Instrument Sans"` label. Replace with:
  ```xml
                           Margin="0,0,0,{StaticResource SpaceMd}"/>
  ```
  Note: `Margin` is a `Thickness`, and `{StaticResource SpaceMd}` is `x:Double` (16) — a bare double resource cannot be interpolated inside a comma-separated `Margin` string in XAML. Use the literal numeral instead, since `PaddingMd`/`SpaceMd` tokens don't have a "bottom-only" Thickness variant to reference directly:
  ```xml
                           Margin="0,0,0,16"/>
  ```
  So this step is: change every `Margin="0,0,0,20"` in `InteractiveContent` (7 occurrences: the top label plus 6 of the 7 `Grid` rows — `H6`'s `Grid` has no `Margin` attribute at all, leave it) to `Margin="0,0,0,16"`. `replace_all` is safe since the value is identical for all matches within this file.

- [ ] **Step 5: Fix garbled text (15 instances).** Apply each of the following exact replacements:

  | Find | Replace |
  |---|---|
  | `Text="96 px = Bold = =40 ls"` | `Text="96 px — Bold — −40 ls"` |
  | `Text="32 px = SemiBold"` | `Text="32 px — SemiBold"` |
  | `Text="24 px = SemiBold"` | `Text="24 px — SemiBold"` |
  | `Text="20 px = SemiBold"` | `Text="20 px — SemiBold"` |
  | `Text="18 px = SemiBold"` | `Text="18 px — SemiBold"` |
  | `Text="16 px = SemiBold"` | `Text="16 px — SemiBold"` |
  | `Text="14 px = SemiBold"` (appears twice — one in InteractiveContent's H6 row, one in StatesContent's "Body M SemiBold" row) | `Text="14 px — SemiBold"` for both — use `replace_all` |
  | `Text="18 px = Regular"` | `Text="18 px — Regular"` |
  | `Text="14 px = Regular"` | `Text="14 px — Regular"` |
  | `Text="12 px = Regular"` | `Text="12 px — Regular"` |
  | `Text="11 px = SemiBold"` | `Text="11 px — SemiBold"` |
  | `Text="94% Battery Health = Good Condition"` | `Text="94% Battery Health — Good Condition"` |
  | `Text="Last updated 12 minutes ago = Automatic updates enabled"` | `Text="Last updated 12 minutes ago — Automatic updates enabled"` |
  | `Text="NORMAL  =  HOVER + PRESS TO TEST  =  DISABLED"` | `Text="NORMAL  —  HOVER + PRESS TO TEST  —  DISABLED"` |

  Also fix the page `Description` (Step 5 continued): find `Description="Full Ether type scale. Instrument Sans for display and headings = Inter for body and UI text.">`, replace `Description="Full Ether type scale. Instrument Sans for display and headings — Inter for body and UI text.">`.

- [ ] **Step 6: Build** — same command.

- [ ] **Step 7: Commit**

```bash
git add Views/Foundations/TypographyPage.xaml
git commit -m "Use EtherMicroSemiBold row labels and SpaceMd margin in TypographyPage; fix garbled dashes"
```

---

### Task 13: `Views/Surfaces/CardPage.xaml`

**Files:**
- Modify: `Views/Surfaces/CardPage.xaml`

- [ ] **Step 1: Apply Rule A to the 2 group-header captions** (`Text="NORMAL CARD = minimal / stat block"` and `Text="CALLOUT WRAPPER = inline status callout"`), which also fixes their garbled text. Find:
  ```xml
                <TextBlock Text="NORMAL CARD = minimal / stat block"
                           FontFamily="{StaticResource InterFont}"
                           FontSize="11" FontWeight="SemiBold"
                           Foreground="{ThemeResource TextTertiary}"/>
  ```
  Replace:
  ```xml
                <TextBlock Text="NORMAL CARD — minimal / stat block"
                           Style="{StaticResource EtherMicroSemiBold}"
                           Foreground="{ThemeResource TextTertiary}"/>
  ```
  Find:
  ```xml
                <TextBlock Text="CALLOUT WRAPPER = inline status callout"
                           FontFamily="{StaticResource InterFont}"
                           FontSize="11" FontWeight="SemiBold"
                           Foreground="{ThemeResource TextTertiary}"/>
  ```
  Replace:
  ```xml
                <TextBlock Text="CALLOUT WRAPPER — inline status callout"
                           Style="{StaticResource EtherMicroSemiBold}"
                           Foreground="{ThemeResource TextTertiary}"/>
  ```

- [ ] **Step 2: Standardize the two callout secondary-text lines to 12px** (matching `EtherBodySRegular`, and matching the `FontSize="12"` already used by this same page's "Good condition" / "Of 1000 estimated" captions for the identical "secondary descriptive line" role). Find (appears twice, `replace_all`):
  ```xml
                                   FontFamily="{StaticResource InterFont}"
                                   FontSize="13"
                                   Foreground="{ThemeResource TextSecondary}"
                                   TextWrapping="Wrap"/>
  ```
  Replace:
  ```xml
                                   Style="{StaticResource EtherBodySRegular}"
                                   Foreground="{ThemeResource TextSecondary}"
                                   TextWrapping="Wrap"/>
  ```

- [ ] **Step 3: Replace the callout `Padding="16"` with the matching token** (both callout `Border` elements). Find (`replace_all`, 2 occurrences):
  ```xml
                        CornerRadius="{StaticResource RadiusSurface}"
                        Padding="16"
                        MaxWidth="480">
  ```
  Replace:
  ```xml
                        CornerRadius="{StaticResource RadiusSurface}"
                        Padding="{StaticResource PaddingMd}"
                        MaxWidth="480">
  ```
  (Per the earlier decision: keep the callouts' own structure — do not force `EtherCardNormal` onto them, since that style's `MinHeight="220"` and visible border don't fit a 2-line status banner. This step only swaps the literal `16` for the identical-value `PaddingMd` token.)

- [ ] **Step 4: Replace the status-dot `CornerRadius="4"` with the matching token** (both status dots — `Width="8" Height="8"`). Find (`replace_all`, 2 occurrences):
  ```xml
                            <Border Width="8" Height="8" CornerRadius="4"
  ```
  Replace:
  ```xml
                            <Border Width="8" Height="8" CornerRadius="{StaticResource RadiusControlSm}"
  ```

- [ ] **Step 5: Build** — same command.

- [ ] **Step 6: Commit**

```bash
git add Views/Surfaces/CardPage.xaml
git commit -m "Use EtherMicro/EtherBodySRegular styles and spacing tokens in CardPage; fix garbled dashes"
```

---

### Task 14: `Views/Surfaces/AIRecommendationPage.xaml` and `Views/Surfaces/ExpressChargePage.xaml`

**Files:**
- Modify: `Views/Surfaces/AIRecommendationPage.xaml`
- Modify: `Views/Surfaces/ExpressChargePage.xaml`

- [ ] **Step 1: In `AIRecommendationPage.xaml`, replace the title's inline properties with `EtherH4`.** Find:
  ```xml
                          <TextBlock Text="AI Recommendation"
                                     FontFamily="{StaticResource InstrumentSans}"
                                     FontSize="16" FontWeight="SemiBold"
                                     Foreground="{ThemeResource TextPrimary}"
                                     VerticalAlignment="Center"/>
  ```
  Replace:
  ```xml
                          <TextBlock Text="AI Recommendation"
                                     Style="{StaticResource EtherH4}"
                                     Foreground="{ThemeResource TextPrimary}"
                                     VerticalAlignment="Center"/>
  ```

- [ ] **Step 2: Leave `Text="="` (the AI sparkle glyph placeholder) untouched** — same out-of-scope icon-glyph issue as `IntelligenceButtonPage`, flagged separately at the end of this plan.

- [ ] **Step 3: In `ExpressChargePage.xaml`, replace the title's inline properties with `EtherH4`.** Find:
  ```xml
                      <TextBlock Text="Express Charge"
                                 FontFamily="{StaticResource InstrumentSans}"
                                 FontSize="16" FontWeight="SemiBold"
                                 Foreground="{ThemeResource TextPrimary}"/>
  ```
  Replace:
  ```xml
                      <TextBlock Text="Express Charge"
                                 Style="{StaticResource EtherH4}"
                                 Foreground="{ThemeResource TextPrimary}"/>
  ```

- [ ] **Step 4: Build** — same command.

- [ ] **Step 5: Commit**

```bash
git add Views/Surfaces/AIRecommendationPage.xaml Views/Surfaces/ExpressChargePage.xaml
git commit -m "Use EtherH4 for card titles in AIRecommendationPage and ExpressChargePage"
```

---

### Task 15: `Views/DataDisplay/BarChartSinglePage.xaml`

**Files:**
- Modify: `Views/DataDisplay/BarChartSinglePage.xaml`

No layout/token changes in this file beyond the gradient and text fixes below — its internal padding/sizing matches the documented `EtherDataGraphics.xaml` spec and stays as-is.

- [ ] **Step 1: Point the gradient fill at the shared resource.** Find:
  ```xml
                    <Border CornerRadius="{StaticResource RadiusControlSm}">
                        <Border.Background>
                            <LinearGradientBrush StartPoint="0,0.5" EndPoint="1,0.5">
                                <GradientStop Color="#8FFFC3" Offset="0.00"/>
                                <GradientStop Color="#0EB2FF" Offset="0.09"/>
                                <GradientStop Color="#0015FF" Offset="0.92"/>
                                <GradientStop Color="#141414" Offset="1.05"/>
                            </LinearGradientBrush>
                        </Border.Background>
                    </Border>
  ```
  Replace:
  ```xml
                    <Border CornerRadius="{StaticResource RadiusControlSm}"
                            Background="{StaticResource EtherBarThickGradient}"/>
  ```
  (Stops are character-identical to `EtherBarThickGradient` — zero visual change, pure dedup.)

- [ ] **Step 2: Fix garbled text** (2 instances):

  Find: `Text="Thick = Gradient progress fill: #8FFFC3 = #0EB2FF = #0015FF = #141414."`
  Replace: `Text="Thick — Gradient progress fill: #8FFFC3 — #0EB2FF — #0015FF — #141414."`

  Find: `Text="Thin = Slim 4 px track with label row. Fill uses BackgroundBrand."`
  Replace: `Text="Thin — Slim 4 px track with label row. Fill uses BackgroundBrand."`

- [ ] **Step 3: Build** — same command.

- [ ] **Step 4: Commit**

```bash
git add Views/DataDisplay/BarChartSinglePage.xaml
git commit -m "Share EtherBarThickGradient resource in BarChartSinglePage; fix garbled dashes"
```

---

### Task 16: Text-only fixes in `DataBlocksPage.xaml`, `LineChartPage.xaml`, `NavRailPage.xaml`

**Files:**
- Modify: `Views/DataDisplay/DataBlocksPage.xaml`
- Modify: `Views/DataDisplay/LineChartPage.xaml`
- Modify: `Views/Navigation/NavRailPage.xaml`

These three files' internal layout is explicitly out of scope (matches the `EtherDataGraphics.xaml`/Figma source verbatim) — only their `Description` text is touched.

- [ ] **Step 1: `DataBlocksPage.xaml`** — the `=` here is a garbled multiplication sign describing the 2×2 grid dimension, not a clause separator:

  Find: `Description="2=2 stat grid. BackgroundSurfaceRaised blocks with optional StatusWarningBg highlight.">`
  Replace: `Description="2×2 stat grid. BackgroundSurfaceRaised blocks with optional StatusWarningBg highlight.">`

- [ ] **Step 2: `LineChartPage.xaml`** — two distinct fixes in the same string: `(Jan=Jun)` is a month range (en dash), `overlay = replace` is a clause separator (em dash):

  Find: `Description="Y-axis labels, X-axis labels (Jan=Jun), BorderSubtle grid lines, and colour-coded legend. Chart lines are a PNG overlay = replace Image.Source with actual chart render.">`
  Replace: `Description="Y-axis labels, X-axis labels (Jan–Jun), BorderSubtle grid lines, and colour-coded legend. Chart lines are a PNG overlay — replace Image.Source with actual chart render.">`

- [ ] **Step 3: `NavRailPage.xaml`** — three `=` in one string: two are clause separators (em dash), the last (`85=70`) is a width×height dimension pair (multiplication sign):

  Find: `Description="Active pill: #1A1A1A = radius 13 = 85=70. Inactive items: TextNavNarrowInactive.">`
  Replace: `Description="Active pill: #1A1A1A — radius 13 — 85×70. Inactive items: TextNavNarrowInactive.">`

- [ ] **Step 4: Build** — same command.

- [ ] **Step 5: Commit**

```bash
git add Views/DataDisplay/DataBlocksPage.xaml Views/DataDisplay/LineChartPage.xaml Views/Navigation/NavRailPage.xaml
git commit -m "Fix garbled dash/multiplication-sign/en-dash characters in description text"
```

---

### Task 17: `Views/AIDesignLanguagePage.xaml`

**Files:**
- Modify: `Views/AIDesignLanguagePage.xaml`

- [ ] **Step 1: Apply Rule A to the 2 group-header captions**, fixing their garbled text in the same edit. Find:
  ```xml
                <TextBlock Text="DELL AI ASSISTANT PANEL = EtherAIAssistant (Empty state)"
                           FontFamily="{StaticResource InterFont}"
                           FontSize="11" FontWeight="SemiBold"
                           Foreground="{ThemeResource TextTertiary}"/>
  ```
  Replace:
  ```xml
                <TextBlock Text="DELL AI ASSISTANT PANEL — EtherAIAssistant (Empty state)"
                           Style="{StaticResource EtherMicroSemiBold}"
                           Foreground="{ThemeResource TextTertiary}"/>
  ```
  Find:
  ```xml
                <TextBlock Text="Device Title Gradient = EtherDeviceTitle"
                           FontFamily="{StaticResource InterFont}"
                           FontSize="11" FontWeight="SemiBold"
                           Foreground="{ThemeResource TextTertiary}"/>
  ```
  Replace:
  ```xml
                <TextBlock Text="Device Title Gradient — EtherDeviceTitle"
                           Style="{StaticResource EtherMicroSemiBold}"
                           Foreground="{ThemeResource TextTertiary}"/>
  ```

- [ ] **Step 2: Apply Rule B to the one 11px-Regular label** (`Text="342 px wide panel..."`). Find:
  ```xml
                <TextBlock Text="342 px wide panel with sparkle icon, prompt input, and toolbar. BackgroundSurfaceRaised host."
                           FontFamily="{StaticResource InterFont}"
                           FontSize="11" Foreground="{ThemeResource TextTertiary}"/>
  ```
  Replace:
  ```xml
                <TextBlock Text="342 px wide panel with sparkle icon, prompt input, and toolbar. BackgroundSurfaceRaised host."
                           Style="{StaticResource EtherMicroRegular}"
                           Foreground="{ThemeResource TextTertiary}"/>
  ```

- [ ] **Step 3: Fix the page `Description` and the 12px detail line** (leave the 12px `FontSize` itself alone — it already matches `EtherBodySRegular`'s size and this line isn't part of the caption pattern; only the text changes). Both `=` here are minus signs on numbers, not separators:

  Find: `Description="How AI reads across the system: the Dell AI Assistant panel and the device title gradient. Both are mode-invariant = look the same in Light and Dark.">`
  Replace: `Description="How AI reads across the system: the Dell AI Assistant panel and the device title gradient. Both are mode-invariant — look the same in Light and Dark.">`

  Find: `Text="96 px Instrument Sans Bold with a navy-to-mid-blue linear gradient. Letter spacing =3.84 px (=40 units). Mode-invariant."`
  Replace: `Text="96 px Instrument Sans Bold with a navy-to-mid-blue linear gradient. Letter spacing −3.84 px (−40 units). Mode-invariant."`

- [ ] **Step 4: Build** — same command.

- [ ] **Step 5: Commit**

```bash
git add Views/AIDesignLanguagePage.xaml
git commit -m "Use EtherMicro caption styles in AIDesignLanguagePage; fix garbled dashes and minus signs"
```

---

## Explicitly out of scope (flag, don't fix here)

Two occurrences of a bare `Text="="` / `Content="=  ..."` are not text-encoding garbling — they're a mis-rendered icon glyph (likely an AI "sparkle" character that the source font doesn't cover), a different root cause requiring a look at how icons are rendered elsewhere in the app (`FontIcon`/`Symbol` vs. raw Unicode glyph) before choosing a fix:
- `Views/Surfaces/AIRecommendationPage.xaml` line 16: `<TextBlock Text="=" FontSize="16" Foreground="{ThemeResource AiAccentBlue}"/>`
- `Views/Controls/IntelligenceButtonPage.xaml` line 29: `Content="=  Optimise my battery"`

After this plan's tasks are committed, flag this to the user as a follow-up rather than guessing at a replacement glyph.

## Self-Review

**Spec coverage:** All 8 numbered items from the design spec are covered — caption typography (Tasks 1–13, 17), secondary body text (Task 13 Step 2), generic padding/spacing tokens (Tasks 2–4 Step 3, Task 13 Steps 3–4), circular swatch dots (Task 13 Step 4), duplicated gradient (Tasks 11 Step 3, 15 Step 1), garbled text (Tasks 8, 10–13, 15–17), `LabelFilterChipPage` Disabled toggle (Task 10 Step 1), `CardPage` callout reuse (Task 13 — resolved as token-only per the correction agreed with the user, not literal `EtherCardNormal` reuse). `Views/Navigation/{DeviceMenuBarPage,MastheadPage,RightPanelPage,WideNavPage}.xaml` and `Views/DataDisplay/{BarChartTieredPage,ProgressBarPage}.xaml` had no garbled text and no in-scope layout issues found during research — no task needed for them.

**Placeholder scan:** No "TBD"/"add appropriate"/"similar to Task N" language — every step gives literal find/replace text or an exact rationale for why a given block is intentionally left alone.

**Type consistency:** N/A (XAML markup, not typed code) — style/token key names (`EtherMicroSemiBold`, `EtherMicroRegular`, `EtherH4`, `SpaceLg`, `SpaceMd`, `PaddingMd`, `RadiusControlSm`, `EtherBarThickGradient`) were verified to exist with those exact keys in `Resources/EtherTypography.xaml`, `Resources/EtherSpacing.xaml`, and `Resources/EtherDataGraphics.xaml` before being used in any task.

---

## Final verification (after all 17 tasks)

- [ ] Run a full clean build: `cd /c/Agroa/ether-sandbox && dotnet build EtherComponentSandbox.csproj -c Debug`
- [ ] Launch the app (you'll need to do this outside this session — no WinUI3-drivable browser/CLI surface is available here) and click through every page in the left nav, in both Light and Dark theme, to confirm captions/spacing read consistently and nothing collapsed or overflowed.
