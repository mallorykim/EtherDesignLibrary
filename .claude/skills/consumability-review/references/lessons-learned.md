# Lessons learned & gotchas (read before touching the runtime harness)

These cost us real serial-failure cycles. Each cycle is a ~8–15 min build+run and the harness aborts
at the first failing assertion, so an avoidable mistake here is expensive.

## WinUI UIA patterns are NOT uniform — pick the one the base peer actually implements
Testing every control for `Toggle` is wrong and will fail at runtime:
- **Invoke** (`IInvokeProvider`): Button, IntelligenceButton.
- **Toggle** (`IToggleProvider`): CheckBox, ToggleSwitch.
- **SelectionItem** (`ISelectionItemProvider`, `IsSelected` tracks `IsChecked`): **RadioButton** and
  each segment RadioButton — **NOT Toggle** (RadioButton's peer is single-select semantics).
- **ExpandCollapse** tracking `IsDropDownOpen`: ComboBox/Dropdown.
- **Selection** (`ISelectionProvider`): ListView/TabNavigation. A `ContentControl` host (e.g.
  SegmentedControl) exposes **no** host Selection pattern — verify selection via its items' SelectionItem.
- **RangeValue**: Slider/SteeringBar/ProgressBar/ScrollBar.

## WinUI TextBox's text pattern is NATIVE, not managed
`TextBoxAutomationPeer.GetPattern(PatternInterface.Value)` **and** `PatternInterface.Text` both return
null in-process — the pattern is surfaced to out-of-process UIA clients (Narrator) natively, not via
the managed provider. So a bare `TextBox`/`EtherInput` C4 proof asserts `GetAutomationControlType()==
Edit` + `IsKeyboardFocusable()` + `IsEnabled()`, **not** a managed `IValueProvider`/`ITextProvider`.
(We wasted two cycles here — first assuming Value, then Text.)

## The harness needs a REAL interactive desktop
`IsKeyboardFocusable`, compositor screenshots, and OS High Contrast fail or return false in a
headless/detached window-station (e.g. a sandboxed `codex exec` child). A `IsKeyboardFocusable=false`
there is an **environment artifact, not a product bug**. Run the PowerShell script directly in the
logged-in session; confirm interactivity with `SessionId=1`, `WinSta0\Default`, unlocked InputDesktop.

## Shared-fixture state leaks fail on ANY desktop
The harness reuses control instances across stages. An earlier visual-state proof set
`EtherInput.IsTabStop=false` (and left TabNav with nothing selected) and never restored it, so a later
automation assert threw everywhere — this looked like a headless issue but wasn't. **Fix = restore the
control's genuine default right before the assert** (a TextBox defaults to `IsTabStop=true`; mirror the
existing intelligenceButton restore). This is setup-completion, not weakening. A `Selector.GetSelection()`
returns **null** (not empty) when nothing is selected → select something before asserting `.Length`.

## Distinguish test-setup gaps from product bugs (and act differently)
- **Test-setup gap** (assertion presupposes state the fixture never set; leaked/unrestored state;
  unrealized container a peer needs): complete the setup so the assertion tests the real contract.
  Safe to fix + log. The assertion stays unchanged.
- **Product bug** or **golden-image truth** or **acceptance-criteria change**: STOP and surface it for
  a decision. Don't force-fix, don't fit the gate.

## Process realities
- **One failure per run.** The harness aborts at the first failure, so issues come out serially — a
  clean run can still hide the next problem behind the one you just fixed. Budget for a few cycles;
  don't promise "one more run."
- **Front-load same-class fixes.** Once you've seen the *class* of a bug (e.g. shared-state leaks),
  scan the remaining stages statically for more of the same before the next run — collapses cycles.
- **Supervise citations.** An audit/agent can cite plausible-but-wrong `file:line`. Spot-check with
  grep/Read. All our delegate citations turned out real — because we checked, not because we assumed.
- **Your own plan can be wrong.** Our remediation plan had three incorrect WinUI UIA expectations; the
  honesty rules (assertion preserved, failure surfaced) are what exposed them instead of a false green.

## Orchestration that doesn't stall
Delegate **edits** to an edit-only subagent (no build/run). Run the long verification yourself as a
**background Bash** command — it notifies you once on completion. A subagent that launches a background
run and then repeatedly "waits for the notification" burns tokens without converging; if one does, stop
it and take over the run/review yourself. Model choice (Codex vs Sonnet vs the session model) is
secondary — the execution *context* (interactive desktop) and this split are what matter.
