# Technical gotchas (WinUI authoring + build)

Accumulated, hard-won working notes on **non-obvious technical traps in this codebase** — the kind
that cost real debugging cycles and are easy to re-hit. Curated for maintainers and coding agents.

This file covers **WinUI control-authoring** and **build/verification tooling**. Two adjacent layers
are documented elsewhere — read them too:

- **Release-gate gotchas** (reskin ripples across ~7 gates, the ConsumerFixtures DPI-determinism
  flake, `SilentPropertyCoverage` accounting, the NuGet same-version cache trap, the MSIX
  `mspdbcmf` warning, why the external-consumer check catches XAML bugs CI misses) → **[../../LESSONS.md](../../LESSONS.md)**.
- **ConsumerFixtures runtime-harness gotchas** (UIA patterns per base type, native `TextBox` text
  pattern, shared-fixture state leaks, visual-baseline flake, baseline regen) →
  **[`.claude/skills/consumability-review/references/lessons-learned.md`](../../.claude/skills/consumability-review/references/lessons-learned.md)**.

---

## WinUI control authoring

### Reskin, don't rewrite — copy the official VSM group/state names exactly
The repo rule ([AGENTS.md](../../AGENTS.md)): start from the closest official WinUI control and swap
only the skin, preserving the official **template part names**, **VisualState group/state names**, and
automation peers. Inventing state names silently breaks transitions (see the two items below).

### CheckBox / RadioButton do NOT drive the `FocusStates` VSM group
A custom keyboard focus ring wired as a `<VisualStateGroup x:Name="FocusStates">` **does not fire**
for `EtherCheckbox` / `EtherRadioButton` — their WinUI base classes use the system focus visual, not a
`FocusStates` group. Drive the ring from code-behind instead: grab the `FocusRing` part in
`OnApplyTemplate`, and toggle its visibility in `OnGotFocus`/`OnLostFocus` when
`FocusState == FocusState.Keyboard` (keyboard-only reveal). **`EtherButton`/`EtherIntelligenceButton`
(Button), `EtherSwitch` (ToggleSwitch), and `EtherDropdown` (ComboBox) DO drive `FocusStates`** — do
not "fix" those. Focus-ring color: use a neutral high-contrast brush (Light `AlphaBlack70` / Dark
`AlphaWhite70`) so it reads on both the unchecked and the blue checked state.

### Selector item VSM state names differ by base (`ListViewItem` vs `ListBoxItem`)
When you write a custom item `ControlTemplate` for a single-select selector, the **combined**
`CommonStates` names depend on the base:
- **`ListViewItem`** (`X : ListView`, e.g. `EtherTabNavigation`): `Normal, Selected, PointerOver,
  PointerOverSelected, PointerOverPressed, Pressed, PressedSelected` — **`Selected` is LAST**;
  `Disabled` is a **separate `DisabledStates` group**; there is no `SelectedUnfocused`.
- **`ListBoxItem`** (`X : ListBox`): `Normal, PointerOver, Pressed, Disabled, Selected,
  SelectedUnfocused, SelectedPointerOver, SelectedPressed` — **`Selected` FIRST**.

Symptom of using the wrong set: click-while-hovering doesn't show the selected pill (it only appears
after the pointer leaves, when plain `Selected` fires), because the control called
`GoToState("PointerOverSelected")` and the template only had `SelectedPointerOver`. Robustness: layer
opacity backgrounds toggled by VSM revert cleanly (a `Background={ThemeResource}` **Setter** sticks);
put the opaque selected layer on top so "selected + hover/pressed" masks them without extra states.

### A shared `Geometry` renders blank after ListView recycle / page re-nav — deep-clone per instance
WinUI (`WindowsAppSDK 2.3.1`) `Geometry` derives from `DependencyObject` and (unlike WPF Freezables)
**cannot be frozen/cloned by the platform for safe reuse**. Handing the *same* `Geometry` instance to
a second `PathIcon` after the first's visual tree is torn down (ListView container recycling, or a
`Frame` re-creating a page with the default `NavigationCacheMode=Disabled`) leaves the Content slot
present but the glyph **blank**. The shared instance hides in an icon-library `Style`
(`EtherIconGeometries.xaml`): after XAML load the `Data` `Setter.Value` is **already a coerced
`Geometry` object, not the `"F1 M…"` string**, so applying that Style (or copying its `Setter.Value`)
still shares the one object. **Fix:** deep-clone the whole geometry object graph per instance (see
`Controls/Navigation/GeometryCloner.cs`) and rebuild the icon each `OnApplyTemplate`. Symptom: icon
shows on first visit, blank after navigating away and back. A plain `ContentControl` using the same
icon Style may not reproduce it (doesn't hit the recycle timing) — that is not proof it's safe;
verify by re-navigating 2+ times at runtime. (microsoft-ui-xaml #827.)

### A keyed `Style` on a custom `Control` still inherits the default style's `MinWidth`/`MinHeight`/`Padding`
An **explicit keyed `Style`** (no `BasedOn`) on an Ether custom `Control` (e.g. `EtherButton`) still
gets the control's **default (implicit) style setters** for any property the keyed style doesn't set.
`DefaultEtherButtonStyle` sets `MinWidth=108 MinHeight=46 Padding=20,12`; a keyed caption style that
set only `Width=46 Height=40` still rendered **108×46** (the `Width`/`Height` were floored by the
leaked `MinWidth`/`MinHeight`). **Fix:** pin `MinWidth`/`MinHeight` (and `Padding`) explicitly in the
keyed style, not just `Width`/`Height`. Symptom: a reskinned small button looks too wide/tall with big
margins, neighbors don't touch despite `Spacing=0`. (Confirmed on `EtherMastheadCaptionButtonStyle`.)

### "Design-system-owned" does NOT mean "setting it has no effect"
Most Ether control templates **do** `{TemplateBinding}` their appearance properties (verified: 
`EtherButton.xaml` binds `Background`/`BorderBrush`/`CornerRadius`/`Padding`/`Foreground`/`FontSize`/
`FontWeight`/both `*ContentAlignment`s), so a consumer override **takes effect** — it just departs
from the intended look. The accurate three-way split for any appearance property:
- **template-bound** → overriding works (has a `{TemplateBinding}`; `observable` or
  `consumed-visually-stable` in `scripts/UnsupportedProperties.psd1`);
- **locked / no effect** → `design-system-owned` (fixed token, no `{TemplateBinding}`) or `platform-noop`;
- **silent trap** → the closed `Entries` list (e.g. `EtherDropdown`/`EtherInput` `Header`/`Description`).

To classify one: grep the control's `.xaml` for `{TemplateBinding <Prop>}` and read its psd1 bucket.
The gate-checked authoritative per-property source is `scripts/UnsupportedProperties.psd1`; the
consumer-facing §4 in `design library handoff/getting-started.md` explains the concept + the traps.
Never write a blanket "appearance has no effect" claim.

### `EtherDropdown` short-menu popup clip — fixed structurally; do NOT retry a runtime height fix
The stock `ComboBox` sizes its popup to `itemCount × itemHeight` and is blind to the template's
`StackPanel.Spacing` and `PopupBorder` padding, so a menu with `≤ MaxVisibleItems` items clipped its
last row. **Resolved structurally** (commit `d971970`): fold the inter-item gap into each item's
`Margin` (the stock counts item margin when sizing, but not panel `Spacing`) and zero the popup
border's vertical padding. **Do not** try to fix it by setting `_popupBorder.MinHeight` at runtime from
the popup's `SizeChanged` — growing the chrome re-fires `SizeChanged` and trips WinUI's layout-cycle
detector (`COMException 0x88000FA8` on `set_IsDropDownOpen(true)`, an intermittent crash-on-open).
Three code strategies (unconditional, change-guarded, monotonic) all cycle; a one-shot write is
overwritten by the base. It must be structural.

---

## Build / verification tooling

### Build & run the Gallery (x64, unpackaged) + the `MSB4276` restore trap
```
dotnet build samples\Ether.DesignSystem.Gallery\Ether.DesignSystem.Gallery.csproj -p:Platform=x64 -p:Configuration=Debug -restore
```
A **fresh** restore (after deleting `obj/bin`) can fail with `MSB4276`
(`WorkloadAutoImportPropsLocator` Sdk dir not found). Fix: run `dotnet workload restore
Ether.DesignSystem.slnx` first, then build. Incremental builds (restore skipped) don't hit it. Kill
any running Gallery instance before rebuilding — it locks `Controls.dll`.

### Self-verify runtime behavior without user clicks (UIA + screenshot + exception log)
- **Drive the app** headlessly: `Add-Type -AssemblyName UIAutomationClient, UIAutomationTypes`, get
  the window via `AutomationElement::FromHandle($proc.MainWindowHandle)`, find nav items by
  `NameProperty`, `Invoke`/`Select`; detect crashes via `$proc.HasExited` (a page whose controls fail
  to realize shows 0 elements for a pattern condition).
- **Capture the real XAML exception** (don't guess): temporarily add
  `this.UnhandledException += …File.AppendAllText(…e.Message…)` in the Gallery `App` ctor, navigate to
  the failing page, read the log, then remove it.
- **Screenshot true live pixels** with `System.Drawing` + `Graphics.CopyFromScreen` over the window's
  screen rect. **`PrintWindow(…PW_RENDERFULLCONTENT)` forces a fresh re-render that MASKS
  stale-composition/shadow bugs** — do not trust it to verify a rendering bug. **DPI:** the GDI thread
  must call `SetThreadDpiAwarenessContext(PER_MONITOR_AWARE_V2)` before `GetWindowRect`, or on a
  high-DPI monitor the rect is logical px while the capture is physical px (you grab only the
  top-left quarter, or an all-white image). Static shots verify appearance, not sub-second transition
  flicker — that still needs a human eyeball.
