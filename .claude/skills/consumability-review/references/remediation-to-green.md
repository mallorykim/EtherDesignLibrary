# Remediation → green

Drive every red/amber row to green in waves. Lock genuine **product decisions** first (they change
what gets built): is a missing knob a real gap to add, or an intentional exclusion to document as 🚫
with the consumer's alternative? Examples we hit: SegmentedControl got a real `ItemsSource` (official
`RadioButtons`/`Selector` single-select contract); TabNav `Icon` stayed string-only-by-design (🚫);
Masthead icon slots stayed decorative (🚫) while caption actions got a real `ActionInvoked` event.

## The waves
- **R1 — product (`src/`).** Public API/behavior changes the consumer gets: new DPs, events, adapter
  observers. Update **both** `PublicAPI.Unshipped.txt` files (the Roslyn public-API analyzer FAILS the
  build if a new public member isn't registered). Build Controls + Interactions on x64, 0 errors,
  before moving on.
- **R2 — harness (`tests/ConsumerFixtures` + `scripts/`).** Add fixtures that *prove* each rubric/
  contract (inventory completeness incl. every shipping type + style-only controls via a style-resource
  fixture; TwoWay round-trips; UIA patterns; Command; new data/adapter paths; R5 docs). Reconcile stale
  contracts (code ⇄ tests ⇄ `UnsupportedProperties.psd1` ⇄ `getting-started.md`). **Every new assertion
  must be wired into a stage `RuntimeVerification.VerifyAsync` actually calls** — an un-called fixture
  method never runs. Then build + run to green.
- **R3 — docs.** Flip the per-component reports + `_SUMMARY.md` to the post-fix state (✅ / documented
  🚫), re-citing to current source. Doc-only.

## Running the verification
Run on a **real interactive desktop** (see `lessons-learned.md` — headless fails focus/screenshots/
High Contrast falsely):
```
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Verify-ConsumerFixtures.ps1
```
Green = the marker `artifacts/consumer-fixtures/runtime-result*.json` reaches
`{"marker":"ETHER_CONSUMER_SMOKE","outcome":"success"}` (a failed run writes all fields null — a
populated new field is proof that assertion ran and passed). Switches: `-SkipSolutionBuild` (reuse a
built tree), `-UpdateVisualBaselines` (regenerate PNGs — reviewed only, see below). The **packaged
MSIX** runtime needs a sideload-allowing machine; the **unpackaged** run is the substantiated green.

Orchestration: delegate edits to an **edit-only** subagent; run the verification yourself as a
**background Bash** command. Don't let a subagent own the long run (it stalls waiting). See SKILL.md.

## Baseline regeneration — reviewed, never blind
`-UpdateVisualBaselines` overwrites **all** ~26 PNGs and silently accepts any diff (and it skips the
comparisons on that run). So:
1. First make the capture **deterministic** — pin the canonical state before capture (e.g.
   `segmentedControl.SelectedIndex = 0`), since the screenshot otherwise captures whatever leftover
   state earlier stages left on a shared control.
2. Regenerate, then `git diff --stat` the baselines and **inspect every changed PNG**. Keep only
   intended changes; most 0–7-byte diffs are within-tolerance byte churn → revert them (the old
   baseline still passes comparison). A real state change (a moved selection pill, a stray focus ring)
   → inspect and decide; an *unexpected* control changing → investigate as a possible regression.
3. Re-run WITHOUT the switch to confirm `outcome:"success"` against the curated baselines.

## Acceptance constants drift
The verify script's marker-validation block hardcodes inventory splits (writable / visual / semantic /
platform, observable / contract-only). Growing the audited-type set makes them stale — and they can be
internally inconsistent (visual+semantic+platform must sum to the inventory total). Reconcile them to
the **deterministic run output** (verify the gate then passes against the real numbers), and update any
docs that cite them. This is legitimate bookkeeping — but confirm every delta is explained by an
approved change; don't fit a gate to an unexplained moving target.

## Definition of done
1. `dotnet build` clean on x64 for Controls, Interactions, ConsumerFixtures.
2. The runtime verification passes for every component (or the un-runnable slice — packaged MSIX,
   anything needing an interactive desktop you lack — is explicitly flagged, not faked).
3. Every report gate satisfied — no ❌/⚠️ except documented 🚫.
4. `_SUMMARY.md` shows the rollup with residuals named.
