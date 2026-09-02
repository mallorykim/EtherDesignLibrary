# Runbook — the consumability verification, on an interactive desktop

> **Status: GREEN.** Waves R1 (product API), R2 (harness expansion), and the runtime run described
> below are all done. `scripts/Verify-ConsumerFixtures.ps1` has been run to completion on a real,
> interactive Windows desktop and reached
> `{"marker":"ETHER_CONSUMER_SMOKE","outcome":"success", ...}` — see
> `artifacts/audit-runs/consumer-runtime-evidence-20260901-161450498/runtime-result.json`. This
> document now records **what that run proved** and **how to reproduce it**, rather than describing
> outstanding work.

## Prerequisite
Run in an **interactive, logged-in Windows desktop session** (not SSH / service / headless). The user
must be able to see a window appear. WinAppSDK/WinUI 3 workload installed (the same environment that
builds the Gallery).

## One command (from the repo root `C:\Ether lib`)
```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Verify-ConsumerFixtures.ps1
```
This builds the solution (x64 Debug), launches the **unpackaged** fixture, drives UIA, captures
screenshots, and runs the High Contrast pass. Useful switches:
- `-SkipSolutionBuild` — if you already built and only want to re-run the runtime smoke.
- `-UpdateVisualBaselines` — **only** when an intentional visual change means the tracked baseline
  PNGs should be updated (otherwise leave off so screenshot diffs stay visible in code review).

## What went GREEN interactively (all R2 work, confirmed in the 2026-09-01 run)
- UIA patterns: Invoke (Button, IntelligenceButton), Toggle (Checkbox, Switch), **SelectionItem**
  (RadioButton + each segment), ExpandCollapse (Dropdown), Selection (TabNavigation), and **EtherInput
  control-type=Edit + keyboard-focusable + enabled**.
- 13 component result fields, `propertyConsumption`, `publicPropertyInventory` (15 types, now incl.
  TabNavigation/TabItem/SegmentRadioButton), `twoWayBindings` (12 properties), `styleResources`
  (Card/Switch/ScrollBar), `commands` (4 controls), `dataPaths` (TabNav + SegmentedControl via
  ObserveSelection/ObserveMasthead), screenshots (Light+Dark, 13 controls), and `highContrast` (OS
  High Contrast, 13 controls).
- Final marker: `{"marker":"ETHER_CONSUMER_SMOKE","outcome":"success", ...}`, recorded at
  `artifacts/audit-runs/consumer-runtime-evidence-20260901-161450498/runtime-result.json` (also
  written to `artifacts/consumer-fixtures/runtime-result*.json` on each run).

## Correction to a prior claim in this file — the `IsKeyboardFocusable` finding was NOT a headless-only limitation

An earlier version of this runbook said the `IsKeyboardFocusable` assertion "only fails headless /
is expected to pass on a real desktop." **That was wrong, and the mistake would have hidden a real
bug if left uncorrected.** The actual cause: `RuntimeVerification.Input.cs:36-37` sets
`input.IsHitTestVisible = false; input.IsTabStop = false;` on the shared `EtherInput` fixture
instance as part of an earlier visual-state proof, and never restores either flag afterward. Because
`WinUI`'s `TextBox` defaults to `IsTabStop = true`, that left the shared instance genuinely
non-focusable for every check that ran after it — **on any desktop, headless or not.** It reached
"passes headless" framing only because the headless run never got far enough to hit the automation
assertion in the first place; it was never actually verified to self-correct on a real desktop.

The fix (`RuntimeVerification.R2.cs:278-290`) restores both flags — mirroring the existing
`intelligenceButton` restore pattern already used elsewhere in the fixture — immediately before the
`EtherInput` automation-peer assertion, so that check tests `EtherInput`'s real, shipped default
(keyboard-focusable), not leaked fixture state from an unrelated earlier proof. With that fix in
place, the assertion now passes on the interactive run described below, and would have failed the
same way on a second headless attempt that got far enough to reach it — confirming it was a shared
mutable state bug, not an environment limitation.

## Packaged (MSIX) runtime
The unpackaged run above covers the substance and is what reached `outcome:"success"`. The
**packaged** fixture additionally requires MSIX install/launch; run it if you want packaged parity
(the script handles the packaged path where the machine allows sideloading). This was **not**
separately re-run as part of this remediation wave — it remains an environment-dependent item (the
user's machine must allow sideloading), not a code gap. Record its marker the same way if run.

## If a GENUINE failure appears (not the corrected finding above)
That is a real finding. Capture the marker `message` + stack, fix the smallest cause (do **not**
weaken assertions), rebuild, rerun.

## Current proven state (for the record)
- x64 Debug/Release build: Controls, Interactions, ConsumerFixtures (Packaged + Unpackaged) — 0 errors.
- Static: `scripts/Verify-UnsupportedProperties.ps1` passes (registry ⇄ docs consistent) as of the
  R2 registry reconciliation (Checkbox/RadioButton D4 notes, Dropdown `PlaceholderText` removal,
  Switch `Header`/`HeaderTemplate` entries retired — see `docs/internal/consumability/_SUMMARY.md`).
- **Interactive runtime (2026-09-01): full green.** The run got through every UIA pattern check
  (Invoke/Toggle/SelectionItem/ExpandCollapse/Selection, plus `EtherInput`'s honest Edit/focusable
  contract), all TwoWay bindings, all Command proofs, all data-path proofs, the style-resource
  fixtures (Card/Switch/ScrollBar including High Contrast), and the standard 13-control
  Light/Dark/High-Contrast screenshot pass, ending in
  `{"marker":"ETHER_CONSUMER_SMOKE","outcome":"success", ...}`. This is the run every `✅` verdict
  in the 15 `docs/internal/consumability/*.md` reports and `_SUMMARY.md` cites.
- **Update (same date, later):** a follow-up edit-only session closed all four named residuals above
  (`EtherDropdown.MaxDropDownHeight`, `EtherScrollBar` RangeValue/`Minimum`/`Maximum` round-trip,
  `EtherRadioButton` `GroupName` mutual exclusion, `EtherMasthead` per-caption Invoke) with new fixture
  assertions, and a subsequent `Verify-ConsumerFixtures.ps1` run confirmed all four hold at runtime
  (`artifacts/consumer-fixtures/runtime-result-9a2b552efe3b4c8bb6c9280635e54b77.json`). See
  `_SUMMARY.md` → "CLOSED — formerly named residuals" for the authoritative, evidence-cited list.
  The only residual left is packaged MSIX parity, which was not independently re-run this wave (see
  above).
