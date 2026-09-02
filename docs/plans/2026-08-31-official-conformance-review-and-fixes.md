# WinUI Official Conformance Review + Fix Log (2026-08-31)

## Background
The user reported that clicking ToggleSwitch from On to Off flickers `off→on→off`, suspecting the logic/implementation doesn't follow the official approach.
This expanded into: for every component except SegmentedControl, **review its code-behind and template against the official WinUI (microsoft-ui-xaml templates + contracts, WinUI-Gallery)**
to check fidelity to the official approach (these are all base controls, whose behavior should match WinUI exactly — only the skin should differ).

Reference baseline: WindowsAppSDK 2.3.1 / net8.0-windows10.0.19041.

## Core Insight (running through the whole document)
Our components fall into two categories:
- **Category A**: native controls wrapped with a keyed Style + ControlTemplate (ToggleSwitch/TextBox/ScrollBar…). The interaction logic lives in **closed-source native code**,
  which drives the template according to a fixed "template contract" (specific part names, VisualState groups/states/transitions). **Any deviation of the template from that contract can cause the closed-source logic to trip up at runtime.**
- **Category B**: genuinely custom controls (Button/Checkbox/Radio/Dropdown subclasses, or custom Slider/SteeringBar/ProgressBar/Masthead), which have their own code-behind.

**Lesson**: static comparison against the official template can catch "missing part / renamed / missing state" issues, but **determining whether a gap actually causes a bug requires runtime verification** —
the ToggleSwitch flicker is a case that static reasoning judged a "harmless no-op" but was actually a P0.

---

## ToggleSwitch Deep Dive (this round's main thread)
### Root Cause of the Flicker
The native ToggleSwitch enters the `Dragging` state on **press**, expecting the closed-source code to hold the knob/appearance in place via the official parts (`KnobTranslateTransform`/`SwitchKnobBounds`/`SwitchKnob`).
Our old template was **hand-written from scratch**: it renamed the part (`KnobTransform`), was missing `SwitchKnob`/`SwitchKnobBounds`, and used a Visibility swap instead of the official structure.
So on entering `Dragging`, the knob/track **falls back to the base value (= Off appearance)**:
- Off→On: falls back to Off, but it was already Off, so no visible change → no flicker.
- On→Off: falls back to Off ("snaps to Off") → on release, IsOn is still true so it slides back to On first → then flips and slides to Off → **a three-stage flicker**.
(No difference whether it's a Setter or a Storyboard — the value falls back either way on leaving the state.)

### Fix: rebuild with the official template as the skeleton, only reskin with Ether's look
Rebuilt `EtherSwitch.xaml`'s ControlTemplate to match the official ToggleSwitch default template: kept the official part names
(`SwitchKnob`/`SwitchKnobBounds`/`SwitchKnobOn`/`SwitchKnobOff`/`OuterBorder`/`OnTrackBacking`/`TrackGlow`/`KnobTranslateTransform`/`SwitchThumb`/`OffContentPresenter`/`OnContentPresenter`/`HeaderContentPresenter`)
and the `CommonStates`/`ToggleStates`/`ContentStates` structure; the Ether skin (gray/blue gradient track, white outer-stroke knob, glow, three-layer shadow, left-side label, focus ring, disabled state) is painted onto these parts.
`ToggleStates` ended up as a **self-driven** version: Off/On each explicitly set `KnobTranslateTransform.X` (0/12) plus all opacities (0.0833s);
`Dragging` is empty; the key is that **`OnToDraggingTransition` explicitly holds the entire On appearance (including X=12, OuterBorder=0)**,
so that On→press→Dragging no longer falls back to Off. Removed the fragile `RepositionThemeAnimation` + `{Binding TemplateSettings.*}`.

### The bug that actually caused the "whole-page crash" (introduced during the rebuild, now fixed)
`<ColumnDefinition Width="{StaticResource Spacing8}" />` — `Spacing8` is a `Double` resource, but `ColumnDefinition.Width` expects a `GridLength`,
a type mismatch → `XamlParseException` at instantiation (the crash offset being identical three times was this). Changed to the literal `Width="8"`.
**Methodological lesson: capture the actual exception with a global UnhandledException handler first — don't guess blindly.**

### Verification (self-verified, not by user clicks)
Drove the Gallery from a script using UI Automation: navigating to the Toggle page has no crash, `toggle-capable`=10 (all switches instantiated),
all 9 pages navigate without crashing, the UnhandledException log is empty; a CopyFromScreen screenshot confirmed the On/Off static appearance is correct with no artifacts.
**User has confirmed (2026-08-31)**: the On→Off flicker is gone, dragging tracks the pointer, and the appearance is correct. ToggleSwitch work is complete.

---

## Fixed This Round (Batch 1: clear, undisputed defects, all build-passing + runtime smoke-verified)
| Component | Issue | Fix | File |
|---|---|---|---|
| **EtherSwitch** | Flicker + dragging doesn't track the pointer + crash | Rebuilt with the official skeleton + fixed ColumnDefinition | EtherSwitch.xaml |
| **EtherDropdown** | P0 `PlaceholderText` not shown | TriggerText falls back to PlaceholderText when nothing is selected + listens for its changes | EtherDropdown.cs |
| **EtherCheckbox** | P0 `IsChecked=null` hangs / P1 `IsThreeState` throws | coerce null to false; IsThreeState coerces silently (no longer throws) | EtherCheckbox.cs |
| **EtherRadioButton** | Same as above | Same as above | EtherRadioButton.cs |
| **EtherIntelligenceButton** | P1 `ContentTemplate`/`ContentTemplateSelector` not bound | Added TemplateBinding to Cp | EtherIntelligenceButton.xaml |
| **EtherButton** | P1 FocusRing controlled by both the FocusStates and Disabled groups | Removed FocusRing.Visibility=Collapsed from the 3 Disabled states so FocusStates has sole control | EtherButton.xaml |
| **EtherSlider** | P1 automation name doesn't fall back to Title | Added GetNameCore → Title to the Peer | EtherSlider.Automation.cs |

Note: for Checkbox/Radio, if bound TwoWay as `bool?`, the null→false coercion writes false back (consistent with its "two-state" contract).

---

## Hardening Batch (2026-08-31, completed + verified)
The user asked to "leave no technical debt — harden everything that can be hardened." Done (build 0/0; UIA across all 11 pages with no crashes; SteeringBar out-of-range SetValue was tested and clamps to max with no infinite loop):
- **Slider + SteeringBar: added mouse-wheel stepping** (`PointerWheelChanged` → steps by `SmallChange`, reusing the existing clamp/snap logic, only responds when interactive).
- **Slider: fixed the `ValueChanged` oldValue for SnapToStops** (records the original oldValue while snapping and uses it when raising the nested event → one snap raises one event, with oldValue = the real previously-committed value).
- **SteeringBar: consolidated value clamping into a single source** (the `Value` CLR setter no longer normalizes; clamping is now done uniformly in the DP callback `OnRangePropertyChanged`, with `_normalizingValue` guarding against recursion) — eliminating the dual-path fragility, **without breaking the public `ValueChanged`/args API**.
- **EtherButton: `Variant`/`Size` parse failures are no longer silent** (if not yet Loaded, retries on Loaded; if it still fails, outputs a Debug diagnostic).
- **Dropdown: filled in the missing FocusStates sub-states** (FocusedPressed/PointerFocused/FocusedDropDown + metadata).
- **Slider/SteeringBar automation `SetValue` now throws `ElementNotEnabledException` when read-only**.
- **IntelligenceButton: removed the dead `FontSize` setter + fixed the icon name in the docs (.png→.svg)**.

**Still deferred (explicitly out of scope this round)**: rebasing SteeringBar to derive from `RangeBase` (a breaking API change with high regression risk — did a low-risk equivalent hardening instead); Radio's `AccessibilityView="Raw"` (needs Narrator testing).

## Awaiting Your Decision (Batch 2: design trade-offs / major architecture changes / visual design — not changed without approval)
- **Deliberately omitted official features** — ✅ **Decided (2026-08-31, user had no preference → default adopted): all confirmed as intentional simplifications / unsupported, not to be implemented, to be documented.** Item-by-item "unsupported" copy to add:
  - Input: no clear (X) button; `Header`/`Description` not supported (title/description are placed externally by the caller).
  - ProgressBar: determinate-only; `IsIndeterminate`/`ShowError`/`ShowPaused` not supported. (Partially documented in class comments already.)
  - Dropdown: compact, non-editable; `IsEditable` not supported; the trigger area only shows text (a rich `ItemTemplate` is not rendered in the trigger area). (Partially documented in class comments already.)
  - ScrollBar: intentionally an always-present 6px bar; no auto-hide/hover-expand/disabled-fade.
  - Switch: `Header` not supported; OffContent/OnContent are recommended to be equal width (unequal widths cause a width jump).
  - ✅ **Completed (2026-08-31)**: the above "unsupported" notes have been added to each component's comments — EtherInput.cs (no clear button/Header/Description), EtherProgressBar.cs (determinate-only, no indeterminate/error/paused), EtherDropdown.cs (IsEditable not supported; PlaceholderText already supported; trigger area is text-only), EtherScrollBar.xaml (always-present bar, no auto-hide/expand/disabled-fade), EtherSwitch.xaml (no Header; OffContent/OnContent recommended equal width). Comments only, build 0/0.
- **Accessibility**:
  - ✅ **Fixed (2026-08-31)**: Checkbox/Radio keyboard focus was invisible. Added an **inner-stroke focus ring** (hugging the 18×18 indicator, no white glow), a neutral high-contrast color (Light `AlphaBlack70` / Dark `AlphaWhite70`, HC `SystemColorHighlightColor`), shown only on keyboard focus. **Key finding**: driving the focus ring via the `FocusStates` VSM group initially **had no effect** — modern WinUI controls don't drive the `FocusStates` group; it only took effect after switching to toggling `FocusRing` from code-behind `OnGotFocus/OnLostFocus` (`FocusState==Keyboard`). Confirmed with real keyboard-Tab screenshots.
  - ✅ **Tested and ruled out as a false alarm (2026-08-31)**: I briefly suspected EtherSwitch/Button/Dropdown/IntelligenceButton's FocusStates were also dead — **testing disproved this**: on keyboard Tab, all 4 show their focus ring correctly (Switch's blue ring, Button's inner ring, Dropdown's blue ring, IntelligenceButton's purple ring). Reason: **only the CheckBox/RadioButton base classes fail to drive FocusStates in WinUI3** (they use the system focus visual instead), while Button/ToggleSwitch/ComboBox drive it normally. So the focus-ring issue is limited to Checkbox/Radio, which is fixed; nothing else needs changing.
  - Radio: `AccessibilityView="Raw"` is set on the content presenter (neither the official control nor Checkbox has it) → non-string content may be hidden from assistive technology, needs Narrator verification. **Untouched**.
  - Dropdown: FocusStates is missing FocusedPressed/PointerFocused/FocusedDropDown (its FocusStates group is live, so this item still stands, but is minor). **Untouched**.
- **Major architecture change**: SteeringBar manually reimplements RangeBase (dual-path Value coercion + a custom ValueChanged instead of a routed event) → ideally it would be changed to derive from RangeBase.
- **P2 style/naming**: Button is missing the 83ms BrushTransition; Slider's `ValueText` part actually renders the Title (should be named TitleText) + the SnapToStops oldValue issue; SteeringBar has a dead theme brush (the glass effect isn't rendered); IntelligenceButton's hardcoded Sparkles icon conflicts with the documentation example; and more (see the review draft for details).

Fully conformant: **EtherMasthead** (custom-control implementation is compliant, no P0/P1 issues).

---

## Review Scope
This review covered 11 components + 2 primitives (HandContentControl / EtherStringContentVisibilityConverter, neither with conformance issues).
The SegmentedControl family was excluded as previously agreed.
