# Phase 8: Thin Subclasses for Checkbox, Radio Button, Toggle Switch, Intelligence Button, Input

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Give 5 components their own dedicated `.cs` class, so every component in the catalog
(except Scroll Bar, which cannot follow this pattern — see below) is a uniform two-file
`Controls/EtherX.cs` + `Controls/EtherX.xaml` pair, consumed as `<controls:EtherX/>` with no
`Style=` attribute needed anywhere.

**Why this reverses an earlier decision:** Phase 2/5 of the parent plan deliberately did NOT
introduce subclasses for these components, reasoning that "no new behavior is needed, so no
subclass is needed" — a valid YAGNI call at the time. The user has since clarified the actual
goal differently: **uniform handoff experience** matters more than **minimal code**. A component
that's "just a keyed style on a stock type" requires an extra sentence of explanation every time
("write `<CheckBox Style="{StaticResource EtherCheckbox}"/>`") that a real `<controls:EtherX/>`
tag doesn't. This phase trades a small amount of boilerplate (5 near-empty subclass files) for
that uniformity.

**Scroll Bar is excluded from this phase, permanently, for a hard architectural reason (not
scope-cutting):** `ScrollViewer`'s own control template internally instantiates a literal stock
`ScrollBar` element — there is no way for a page author to substitute a custom subclass there
without retemplating `ScrollViewer` itself (a much larger, different, unrequested change). Scroll
Bar's whole value is that it applies automatically to every scrollbar in the app, including ones
inside other components' internal templates (e.g. Dropdown's popup menu) — that only works
because it's an implicit style on the literal `ScrollBar` type. Giving it a dedicated subclass
would not break anything by itself, but it also wouldn't achieve the goal (uniform uncomplicated
usage), since the app-wide auto-apply behavior would still need to stay on the stock type in
parallel. Scroll Bar remains a one-file component; this is inherent to what it does, not
inconsistent effort.

**Toggle Switch is ALSO excluded, discovered mid-execution — a second, different hard
architectural reason.** `Microsoft.UI.Xaml.Controls.ToggleSwitch` is a `sealed` class in
WinUI3/WindowsAppSDK — confirmed by attempting `public sealed class EtherSwitch : ToggleSwitch`
and getting `error CS0509: 'EtherSwitch': cannot derive from sealed type 'ToggleSwitch'`, a
compiler-level fact, not a design choice anyone made. Unlike `CheckBox`, `RadioButton`, `Button`,
and `TextBox` (all successfully subclassed elsewhere in this codebase), `ToggleSwitch` cannot be
subclassed by anyone, for any reason — this is a restriction Microsoft placed on the type itself.
The two alternatives considered and rejected:
- **Reimplement as a `Control` that doesn't derive from `ToggleSwitch`:** would require rebuilding
  on/off state, click-to-toggle logic, keyboard support, and the automation peer from scratch —
  a fundamentally different, much larger undertaking than "thin subclass, zero behavior change,"
  and completely out of proportion to what this phase is trying to achieve.
- **Wrap in a `UserControl`:** already explicitly rejected by this project's own architecture
  decisions (see the parent plan's "Mechanism decision" section) — wrapping would move `x:Name`
  from the actual `ToggleSwitch` instance to the outer wrapper, breaking every
  `VisualStateManager.GoToState` call in `ToggleSwitchPage.xaml.cs`, which targets the inner
  control's own template states directly.

Toggle Switch stays exactly as Phase 0 left it: a keyed style on the stock `ToggleSwitch` type,
consumed as `<ToggleSwitch Style="{StaticResource EtherSwitch}"/>`. This is a second permanent,
architecturally-required exception alongside Scroll Bar — not an inconsistency, and not
something a future engineer should try to "fix" without first checking whether `ToggleSwitch`
is still sealed in whatever WindowsAppSDK version is current at the time.

**The mechanism, identical for all 5 components:**
1. Create `Controls/Ether<X>.cs`: a minimal `sealed class EtherX : <StockType>` with no members
   unless the component specifically needs one (only Intelligence Button needs a constructor,
   for the hand cursor — see Task 4).
2. In the existing `Controls/Ether<X>.xaml`, retarget every `TargetType="<StockType>"` to
   `TargetType="controls:EtherX"` (both the `ControlTemplate` and the `Style`), and **drop the
   `Style`'s `x:Key`** so it becomes implicit. This is now safe and correct — implicit styles are
   only dangerous when they target a *stock* type (silently restyling unrelated bare elements
   elsewhere); targeting a brand-new custom type that nothing else in the app could accidentally
   instantiate has no such risk, and matches the existing convention already used by
   `EtherButton`/`EtherDropdown`/`EtherProgressBar`/`EtherSegmentedTrack`.
3. Update the file's `LOCATION` header comment from "No dedicated class today — this file
   styles the stock X type directly" to the standard "This dictionary lives beside its class
   (Controls/EtherX.cs)" wording used by every other 2-file component.
4. Update every consumer: change `<StockType Style="{StaticResource EtherX}" .../>` to
   `<controls:EtherX .../>` — same element, same other attributes, just drop the `Style=` line
   and rename the tag (both opening and closing) to the new type. `x:Name`, `Content`,
   `IsChecked`/`IsOn`/`GroupName`/`Text`/`PlaceholderText`, `IsHitTestVisible`, `IsTabStop`,
   `IsEnabled`, and any event handlers stay exactly as they are — none of that changes meaning
   when the tag becomes a subclass, since the subclass inherits everything from its base type.

**Verification per task:** after editing every consumer file, grep-count the OLD stock-type tag
with `Style="{StaticResource EtherX}"` — it must be **zero** — and grep-count the NEW
`controls:EtherX` tag — it must match the number of specimens the file had before (listed per
task below). `GoToState`-driven pages are especially important to re-verify: confirm every
`x:Name` referenced in that page's `.xaml.cs` still exists on an element of the new tag, since
`VisualStateManager.GoToState` calls are unaffected by the type rename (they operate on the
`x:Name`-resolved object reference, not the XAML tag name) but this must be confirmed, not
assumed.

**Tech Stack:** WinUI3 / .NET 8. No test framework — verification is `dotnet build` + grep-based
occurrence counting + `GoToState` `x:Name` cross-check. No GUI verification available.

---

## Task 1: Checkbox

**Files:**
- Create: `Controls/EtherCheckbox.cs`
- Modify: `Controls/EtherCheckbox.xaml` (retarget + LOCATION comment)
- Modify: `Views/Controls/CheckboxPage.xaml` (8 specimens; needs `xmlns:controls` added — it
  doesn't have one today)
- Modify: `Views/ComponentPage.xaml` (1 specimen, the `DisabledCheck` used by every component
  page's optional Disabled toggle — `xmlns:controls` already present)
- Modify: `Views/Controls/ButtonPage.xaml` (1 specimen, the `IconToggle` — `xmlns:controls`
  already present)

- [ ] **Step 1: Create `Controls/EtherCheckbox.cs`**

```csharp
using Microsoft.UI.Xaml.Controls;

namespace Ether.DesignSystem.Controls;

/// <summary>The Ether checkbox — a templated CheckBox. No behavior changes beyond the visual
/// template; see Controls/EtherCheckbox.xaml. IsThreeState is set to False by the style.</summary>
public sealed class EtherCheckbox : CheckBox
{
}
```

- [ ] **Step 2: Retarget `Controls/EtherCheckbox.xaml`**

Read the file first. Change `<ControlTemplate x:Key="EtherCheckboxTemplate" TargetType="CheckBox">`
to `<ControlTemplate x:Key="EtherCheckboxTemplate" TargetType="controls:EtherCheckbox">`. Change
`<Style x:Key="EtherCheckbox" TargetType="CheckBox">` to
`<Style TargetType="controls:EtherCheckbox">` (drop the `x:Key`). `xmlns:controls` is already
declared in this file (used for `HandContentControl`) — no namespace addition needed. Update the
`LOCATION` block from "No dedicated class today — this file styles the stock CheckBox type
directly and is merged explicitly from App.xaml" to "This dictionary lives beside its class
(Controls/EtherCheckbox.cs) and is merged explicitly from App.xaml" — keep the rest of that
block (the "Mechanism refinement" pointer) unchanged. Nothing else in the file changes — every
brush, `VisualState`, and named element stays exactly as-is.

- [ ] **Step 3: Update `Views/Controls/CheckboxPage.xaml`**

Add `xmlns:controls="using:Ether.DesignSystem.Controls"` to the `<Page>` root element (alongside the
existing `xmlns:views`). Then, for all 8 `<CheckBox ... Style="{StaticResource EtherCheckbox}" .../>`
elements in this file (1 in `InteractiveContent`, 4 in the UNCHECKED states block — including
the ones named `UncheckedHover` and `UncheckedPressed` — 3 in the CHECKED states block —
including `CheckedHover`): remove the `Style="{StaticResource EtherCheckbox}"` line from each,
and rename both the opening and closing tag from `CheckBox`/`</CheckBox>`(self-closing `/>` in
most cases) to `controls:EtherCheckbox`. Every other attribute (`x:Name`, `Content`,
`IsChecked`, `IsEnabled`, `IsHitTestVisible`, `IsTabStop`) stays unchanged on each element.

- [ ] **Step 4: Update `Views/ComponentPage.xaml`**

Find the `DisabledCheck` element:
```xml
<CheckBox x:Name="DisabledCheck"
          Content="Disabled"
          Style="{StaticResource EtherCheckbox}"
          Visibility="{x:Bind BoolToVisibility(HasDisabledToggle), Mode=OneTime}"
          Checked="OnDisabledToggled"
          Unchecked="OnDisabledToggled"/>
```
Remove the `Style=` line and change the tag to `controls:EtherCheckbox`. `xmlns:controls` is
already declared in this file's root `<UserControl>` element.

- [ ] **Step 5: Update `Views/Controls/ButtonPage.xaml`**

Find the `IconToggle` element:
```xml
<CheckBox x:Name="IconToggle"
          Content="Trailing icon"
          Style="{StaticResource EtherCheckbox}"
          IsChecked="True"
          Checked="IconToggle_Changed"
          Unchecked="IconToggle_Changed"/>
```
Remove the `Style=` line and change the tag to `controls:EtherCheckbox`. `xmlns:controls` is
already declared in this file's root `<Page>` element.

- [ ] **Step 6: Build**

`dotnet build EtherComponentSandbox.csproj -c Debug` — expect 0 errors.

- [ ] **Step 7: Verify**

Grep the whole repo for `Style="{StaticResource EtherCheckbox}"` — must be **zero** hits. Grep
for `controls:EtherCheckbox` — must hit exactly 10 element tags total across the 3 consumer
files (8 in CheckboxPage.xaml, 1 in ComponentPage.xaml, 1 in ButtonPage.xaml), each appearing
twice if not self-closing (open+close) or once if self-closing — count element occurrences, not
raw line matches. Confirm `CheckboxPage.xaml.cs`'s `GoToState` calls (`UncheckedHover`,
`UncheckedPressed`, `CheckedHover`) still resolve to real named elements in the updated file.

- [ ] **Step 8: Commit**

```bash
git add Controls/EtherCheckbox.cs Controls/EtherCheckbox.xaml Views/Controls/CheckboxPage.xaml Views/ComponentPage.xaml Views/Controls/ButtonPage.xaml
git commit -m "$(cat <<'EOF'
Add EtherCheckbox subclass, retarget style, update all consumers

Phase 8: reverses the earlier "no subclass needed" simplification per
explicit user request - uniform two-file component handoff matters
more than minimal code. EtherCheckbox is now a real controls:EtherCheckbox
element, consumed with no Style= needed anywhere. Style stays implicit
(safe now that it targets a custom type, not the stock CheckBox).
Updated all 3 consumers: CheckboxPage.xaml, ComponentPage.xaml's
shared Disabled-toggle checkbox, and ButtonPage.xaml's icon toggle.

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
EOF
)"
```

---

## Task 2: Radio Button

**Files:**
- Create: `Controls/EtherRadioButton.cs`
- Modify: `Controls/EtherRadioButton.xaml` (retarget + LOCATION comment)
- Modify: `Views/Controls/RadioButtonPage.xaml` (10 specimens: 3 in InteractiveContent + 4
  UNCHECKED + 3 CHECKED; needs `xmlns:controls` added)

- [ ] **Step 1: Create `Controls/EtherRadioButton.cs`**

```csharp
using Microsoft.UI.Xaml.Controls;

namespace Ether.DesignSystem.Controls;

/// <summary>The Ether radio button — a templated RadioButton. No behavior changes beyond the
/// visual template; see Controls/EtherRadioButton.xaml. GroupName-based mutual exclusion works
/// identically to the stock RadioButton, since nothing about that mechanism is overridden.</summary>
public sealed class EtherRadioButton : RadioButton
{
}
```

- [ ] **Step 2: Retarget `Controls/EtherRadioButton.xaml`**

Same mechanism as Task 1: `<ControlTemplate x:Key="EtherRadioButtonTemplate" TargetType="RadioButton">`
→ `TargetType="controls:EtherRadioButton"`; `<Style x:Key="EtherRadioButton" TargetType="RadioButton">`
→ `<Style TargetType="controls:EtherRadioButton">` (drop `x:Key`). `xmlns:controls` already
present (used for `HandContentControl`). Update the `LOCATION` block to reference
`Controls/EtherRadioButton.cs` instead of "No dedicated class today." Nothing else changes —
every brush and `VisualState` stays exactly as-is, including the literal `CornerRadius="9"`
values (not a token — this is intentional, per the file's own comment about rendering a full
circle).

- [ ] **Step 3: Update `Views/Controls/RadioButtonPage.xaml`**

Add `xmlns:controls="using:Ether.DesignSystem.Controls"` to the `<Page>` root. For all 10
`<RadioButton ... Style="{StaticResource EtherRadioButton}" .../>` elements — 3 in
`InteractiveContent` sharing `GroupName="Interactive"` (Option A/B/C); 4 in UNCHECKED states
each with its own unique `GroupName` (`u-default`, `u-hover`, `u-pressed`, `u-disabled`,
including the ones named `UncheckedHover`/`UncheckedPressed`); 3 in CHECKED states each with its
own unique `GroupName` (`c-default`, `c-hover`, `c-disabled`, including the one named
`CheckedHover`) — remove the `Style=` line from each, rename tag to `controls:EtherRadioButton`.
**Preserve every `GroupName` value exactly** — these are deliberately unique per specimen so the
display-only swatches never interfere with each other's checked state; this is unrelated to and
unaffected by the type rename, but transcribe carefully since there are many similar-looking
blocks.

- [ ] **Step 4: Build**

`dotnet build EtherComponentSandbox.csproj -c Debug` — expect 0 errors.

- [ ] **Step 5: Verify**

Grep for `Style="{StaticResource EtherRadioButton}"` — zero hits. Grep for
`controls:EtherRadioButton` — must hit exactly 10 element tags. Confirm every `GroupName` value from the original file is still present,
unchanged, attached to the correct specimen (cross-check against the Default/Hover/Pressed/
Disabled labels each one sits under). Confirm `RadioButtonPage.xaml.cs`'s `GoToState` calls
(`UncheckedHover`, `UncheckedPressed`, `CheckedHover`) still resolve.

- [ ] **Step 6: Commit**

```bash
git add Controls/EtherRadioButton.cs Controls/EtherRadioButton.xaml Views/Controls/RadioButtonPage.xaml
git commit -m "$(cat <<'EOF'
Add EtherRadioButton subclass, retarget style, update consumer

Phase 8 continued. Style stays implicit (safe now that it targets a
custom type). GroupName-based grouping is unaffected by the type
rename - every specimen's unique GroupName preserved exactly.

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
EOF
)"
```

---

## Task 3: Toggle Switch — SKIPPED, architecturally impossible

**Status: N/A, permanently.** `ToggleSwitch` is a `sealed` class in WinUI3 — confirmed by an
actual compile attempt (`error CS0509: 'EtherSwitch': cannot derive from sealed type
'ToggleSwitch'`) during execution of this task, not assumed in advance. See the "Toggle Switch
is ALSO excluded" section near the top of this document for the full reasoning and the two
rejected alternatives. `Controls/EtherSwitch.xaml` and `Views/Controls/ToggleSwitchPage.xaml`
are unchanged from Phase 0 — Toggle Switch remains `<ToggleSwitch Style="{StaticResource
EtherSwitch}"/>`, a keyed style on the stock type, exactly as it was before this phase started.

**Original task text below, kept for the record — do not execute it.** The `.cs` file it
specifies does not compile and never will while `ToggleSwitch` stays sealed.

**Files (not modified — task skipped):**
- ~~Create: `Controls/EtherSwitch.cs`~~
- ~~Modify: `Controls/EtherSwitch.xaml` (retarget + LOCATION comment)~~
- ~~Modify: `Views/Controls/ToggleSwitchPage.xaml` (9 specimens; needs `xmlns:controls` added)~~

- [ ] **Step 1: Create `Controls/EtherSwitch.cs`**

```csharp
using Microsoft.UI.Xaml.Controls;

namespace Ether.DesignSystem.Controls;

/// <summary>The Ether toggle switch — a templated ToggleSwitch. No behavior changes beyond the
/// visual template; see Controls/EtherSwitch.xaml.</summary>
public sealed class EtherSwitch : ToggleSwitch
{
}
```

- [ ] **Step 2: Retarget `Controls/EtherSwitch.xaml`**

`<ControlTemplate x:Key="EtherSwitchTemplate" TargetType="ToggleSwitch">` →
`TargetType="controls:EtherSwitch"`; `<Style x:Key="EtherSwitch" TargetType="ToggleSwitch">` →
`<Style TargetType="controls:EtherSwitch">` (drop `x:Key`). `xmlns:controls` already present
(used for `HandContentControl`). **This is a deliberate behavior change from Phase 0's decision**
to keep this style keyed — that decision was correct at the time (the style targeted the stock
`ToggleSwitch` type, so implicit would have been dangerous), but now that it targets the custom
`controls:EtherSwitch` type, implicit is safe, matching every other 2-file component's
convention. Update the file's header comment (it currently doesn't have a `LOCATION` block at
all, just inline notes from the Phase 0 relocation) — add a `LOCATION` block matching the other
files' convention, referencing `Controls/EtherSwitch.cs`.

- [ ] **Step 3: Update `Views/Controls/ToggleSwitchPage.xaml`**

Add `xmlns:controls="using:Ether.DesignSystem.Controls"` to the `<Page>` root. For all 9
`<ToggleSwitch ... Style="{StaticResource EtherSwitch}" .../>` elements (1 in
`InteractiveContent`, 4 in OFF states — including `OffHoverState`/`OffPressedState` — 4 in ON
states — including `OnHoverState`/`OnPressedState`): remove the `Style=` line from each, rename
tag to `controls:EtherSwitch`. Every other attribute (`x:Name`, `IsOn`, `OffContent`,
`OnContent`, `IsEnabled`, `IsHitTestVisible`, `IsTabStop`) stays unchanged.

- [ ] **Step 4: Build**

`dotnet build EtherComponentSandbox.csproj -c Debug` — expect 0 errors.

- [ ] **Step 5: Verify**

Grep for `Style="{StaticResource EtherSwitch}"` — zero hits. Grep for `controls:EtherSwitch` and
count against the actual number of `<ToggleSwitch>` elements the original file had. Confirm
`ToggleSwitchPage.xaml.cs`'s `GoToState` calls (`OffHoverState`, `OffPressedState`,
`OnHoverState`, `OnPressedState`) still resolve.

- [ ] **Step 6: Commit**

```bash
git add Controls/EtherSwitch.cs Controls/EtherSwitch.xaml Views/Controls/ToggleSwitchPage.xaml
git commit -m "$(cat <<'EOF'
Add EtherSwitch subclass, retarget style, update consumer

Phase 8 continued. Reverses Phase 0's deliberate keyed-not-implicit
choice for this file - that was correct when the style targeted the
stock ToggleSwitch type, but now that it targets the custom
controls:EtherSwitch type, implicit is safe and consistent with every
other 2-file component.

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
EOF
)"
```

---

## Task 4: Intelligence Button

**Files:**
- Create: `Controls/EtherIntelligenceButton.cs`
- Modify: `Controls/EtherIntelligenceButton.xaml` (retarget + LOCATION comment + add
  `xmlns:controls`, not currently present in this file)
- Modify: `Views/Controls/IntelligenceButtonPage.xaml` (6 specimens: 2 use
  `controls:HandButton` today, 4 use plain `Button` — both become `controls:EtherIntelligenceButton`)

**Note:** `Controls/HandButton.cs` (the shared cursor helper) is NOT touched or deprecated by
this task — it remains available for any other component that needs a bare hand-cursor button
without a specific visual template. `EtherIntelligenceButton` gets its own hand cursor directly
in its constructor (matching `EtherButton`'s existing convention — see
`Controls/EtherButton.cs`), rather than inheriting from `HandButton`, so it's self-contained
like every other 2-file component in this codebase.

- [ ] **Step 1: Create `Controls/EtherIntelligenceButton.cs`**

```csharp
using Microsoft.UI.Input;
using Microsoft.UI.Xaml.Controls;

namespace Ether.DesignSystem.Controls;

/// <summary>The Ether intelligence button — a templated Button with a hand cursor, matching
/// EtherButton's convention (see Controls/EtherButton.cs). No behavior changes beyond the
/// visual template and cursor; see Controls/EtherIntelligenceButton.xaml.</summary>
public sealed class EtherIntelligenceButton : Button
{
    public EtherIntelligenceButton()
    {
        ProtectedCursor = InputSystemCursor.Create(InputSystemCursorShape.Hand);
    }
}
```

- [ ] **Step 2: Retarget `Controls/EtherIntelligenceButton.xaml`**

Add `xmlns:controls="using:Ether.DesignSystem.Controls"` to the root `<ResourceDictionary>` element
(this file currently only declares `xmlns:ui` and `xmlns:media` for CommunityToolkit — those
stay, just add the new namespace alongside them). Change
`<ControlTemplate x:Key="EtherIntelligenceButtonTemplate" TargetType="Button">` to
`TargetType="controls:EtherIntelligenceButton"`. Change
`<Style x:Key="EtherIntelligenceButton" TargetType="Button">` to
`<Style TargetType="controls:EtherIntelligenceButton">` (drop `x:Key`). Update the `USAGE`
section of the header comment: the existing examples show `<Button Style="{StaticResource
EtherIntelligenceButton}" .../>` — update to `<controls:EtherIntelligenceButton .../>` (both the
simple and with-icon-stack examples). Add a `LOCATION` note referencing
`Controls/EtherIntelligenceButton.cs`, matching the other files' convention. Nothing else in the
file changes — every glow layer, gradient, and `VisualState` stays exactly as-is.

- [ ] **Step 3: Update `Views/Controls/IntelligenceButtonPage.xaml`**

`xmlns:controls` is already present in this file. For the 2 `<controls:HandButton ...
Style="{StaticResource EtherIntelligenceButton}" .../>` elements in `InteractiveContent`
("What's this mean?" and the "= Optimise my battery" one — leave that garbled `=` character
exactly as it is, it's a separate, already-flagged, deliberately-untouched icon-glyph issue, not
part of this task): remove the `Style=` line and change the tag from `controls:HandButton` to
`controls:EtherIntelligenceButton`. For the 4 plain `<Button ... Style="{StaticResource
EtherIntelligenceButton}" .../>` elements in `StatesContent` (Default, `IntelligenceHoverState`,
`IntelligencePressedState`, `IntelligenceDisabledState`): remove the `Style=` line and change the
tag from `Button` to `controls:EtherIntelligenceButton`. Every other attribute (`x:Name`,
`Content`, `HorizontalContentAlignment`, `IsEnabled`, `IsHitTestVisible`, `IsTabStop`) stays
unchanged on each.

- [ ] **Step 4: Build**

`dotnet build EtherComponentSandbox.csproj -c Debug` — expect 0 errors.

- [ ] **Step 5: Verify**

Grep for `Style="{StaticResource EtherIntelligenceButton}"` — zero hits. Grep for
`controls:EtherIntelligenceButton` — must hit exactly 6 element tags. Confirm
`Controls/HandButton.cs` shows zero diff (untouched). Confirm
`IntelligenceButtonPage.xaml.cs`'s `GoToState` calls (`IntelligenceHoverState`,
`IntelligencePressedState`) still resolve, and its `ApplyIcons`/`SetRightIcon`-equivalent logic
— actually this page has no icon-toggle logic (that's Button's page, not this one) — just
confirm the file's existing code-behind (if any beyond GoToState) still compiles against the
renamed elements.

- [ ] **Step 6: Commit**

```bash
git add Controls/EtherIntelligenceButton.cs Controls/EtherIntelligenceButton.xaml Views/Controls/IntelligenceButtonPage.xaml
git commit -m "$(cat <<'EOF'
Add EtherIntelligenceButton subclass, retarget style, update consumer

Phase 8 continued. Gets its own hand cursor in the constructor,
matching EtherButton's existing convention, rather than inheriting
HandButton - stays self-contained like every other 2-file component.
HandButton itself is untouched, still available as a shared helper.
Style stays implicit (safe now that it targets a custom type, not the
stock Button type, which is used everywhere).

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
EOF
)"
```

---

## Task 5: Input

**Files:**
- Create: `Controls/EtherInput.cs`
- Modify: `Controls/EtherInput.xaml` (retarget + LOCATION comment + add `xmlns:controls`, not
  currently present)
- Modify: `Views/Controls/InputPage.xaml` (6 specimens; needs `xmlns:controls` added)

- [ ] **Step 1: Create `Controls/EtherInput.cs`**

```csharp
using Microsoft.UI.Xaml.Controls;

namespace Ether.DesignSystem.Controls;

/// <summary>The Ether input field — a templated TextBox. No behavior changes beyond the
/// visual template; see Controls/EtherInput.xaml.</summary>
public sealed class EtherInput : TextBox
{
}
```

- [ ] **Step 2: Retarget `Controls/EtherInput.xaml`**

Add `xmlns:controls="using:Ether.DesignSystem.Controls"` to the root `<ResourceDictionary>` element
(this file currently has no `xmlns:controls` at all). Change
`<ControlTemplate x:Key="EtherInputTemplate" TargetType="TextBox">` to
`TargetType="controls:EtherInput"`. Change `<Style x:Key="EtherInput" TargetType="TextBox">` to
`<Style TargetType="controls:EtherInput">` (drop `x:Key`). Update the `LOCATION` block from "No
dedicated class today" to reference `Controls/EtherInput.cs`. Nothing else changes — every
brush, `VisualState`, and named element (`LayoutRoot`, `BorderElement`,
`PlaceholderTextContentPresenter`, `ContentElement`) stays exactly as-is, including the
framework-required `Control.IsTemplateFocusTarget="True"` attached property and the exact
`x:Name="ContentElement"` naming that `TextBox`'s own template contract depends on.

- [ ] **Step 3: Update `Views/Controls/InputPage.xaml`**

Add `xmlns:controls="using:Ether.DesignSystem.Controls"` to the `<Page>` root. For all 6
`<TextBox ... Style="{StaticResource EtherInput}" .../>` elements (1 in `InteractiveContent`,
5 in `StatesContent`: Default, `HoverInput`, `ActiveInput`, Filled, Disabled): remove the
`Style=` line from each, rename tag to `controls:EtherInput`. Every other attribute
(`x:Name`, `PlaceholderText`, `Text`, `Width`, `IsEnabled`, `IsHitTestVisible`, `IsTabStop`)
stays unchanged.

- [ ] **Step 4: Build**

`dotnet build EtherComponentSandbox.csproj -c Debug` — expect 0 errors.

- [ ] **Step 5: Verify**

Grep for `Style="{StaticResource EtherInput}"` — zero hits. Grep for `controls:EtherInput` —
must hit exactly 6 element tags. Confirm `InputPage.xaml.cs`'s `GoToState` calls (`HoverInput`,
`ActiveInput`) still resolve.

- [ ] **Step 6: Commit**

```bash
git add Controls/EtherInput.cs Controls/EtherInput.xaml Views/Controls/InputPage.xaml
git commit -m "$(cat <<'EOF'
Add EtherInput subclass, retarget style, update consumer

Phase 8 continued (final component subclass). Style stays implicit
(safe now that it targets a custom type, not the stock TextBox type).
Framework-required template parts (ContentElement,
Control.IsTemplateFocusTarget) preserved exactly.

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
EOF
)"
```

---

## Task 6: Verification sweep and retrospective

**Files:**
- Modify: `docs/superpowers/plans/2026-08-06-self-contained-component-authoring-plan.md`
  (Phase 8 status)

- [ ] **Step 1: Full build and sweep**

`dotnet build EtherComponentSandbox.csproj -c Debug` — expect 0 errors. Grep the whole repo
(excluding `obj`/`bin`) for `Style="{StaticResource EtherCheckbox}"`,
`Style="{StaticResource EtherRadioButton}"`, `Style="{StaticResource EtherSwitch}"`,
`Style="{StaticResource EtherIntelligenceButton}"`, `Style="{StaticResource EtherInput}"` — all
five must be **zero** hits anywhere in the repo. This is the real completion signal for this
phase: no file anywhere should still reference these 5 components via the old
`Style="{StaticResource ...}"` pattern.

- [ ] **Step 2: Full-catalog sanity pass**

Since there's no GUI, grep every file listed in `ComponentCatalog.cs`'s `Nodes` array for any
remaining bare stock-type usage of these 5 controls without the new `controls:` prefix, to make
sure nothing outside the 5 known consumer files was missed (e.g. a page that happens to also use
a plain `<CheckBox>` for some unrelated purpose is fine and expected — only check for accidental
survivors of the OLD `Style="{StaticResource EtherX}"` pattern specifically, already covered by
Step 1's grep, so this step is really just re-confirming Step 1's result holds project-wide, not
just in the 5 files this plan named).

- [ ] **Step 3: Update the parent plan**

Add a new "Phase 8" section (after the closing summary added at the end of the original 7-phase
sweep) to `docs/superpowers/plans/2026-08-06-self-contained-component-authoring-plan.md`,
recording: what changed (5 new subclasses, consumer updates, why — user wanted uniform 2-file
handoff over minimal code), the Scroll Bar exception and why it's permanent, and the 5 commits.
Update the component table to show all components now uniformly 2-file except Scroll Bar
(1-file, architecturally required) and the UserControl composites (which were always 2-file).

- [ ] **Step 4: Commit the plan update**

```bash
git add docs/superpowers/plans/2026-08-06-self-contained-component-authoring-plan.md
git commit -m "$(cat <<'EOF'
Phase 8 retrospective: record status, uniform 2-file component sweep

Checkbox, Radio Button, Toggle Switch, Intelligence Button, and Input
all now have dedicated subclasses and are consumed as
<controls:EtherX/> with no Style= needed anywhere. Scroll Bar remains
the sole permanent exception - ScrollViewer's own template
instantiates a literal stock ScrollBar internally, so a subclass could
never be substituted there without retemplating ScrollViewer itself.

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
EOF
)"
```

- [ ] **Step 5: Report status to the user**

Summarize: every component except Scroll Bar (architecturally exempt) is now a uniform 2-file
`Controls/EtherX.cs` + `.xaml` pair, consumable as `<controls:EtherX/>` with zero extra
explanation needed. Confirm the full grep sweep found zero remaining old-style references.
