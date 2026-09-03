# Lessons learned — release & verification gates

Things that cost real cycles during `scripts/Publish-Internal.ps1` rehearsals. **All of these are
gates that hosted CI does NOT run** (they need a real interactive desktop, or only exist in
`Publish-Internal`), so they surface for the first time at *release* time, one at a time, and each
full rehearsal is ~40 min. Read the pre-flight checklist first — it exists so the same problems do
not come back.

For the **runtime harness** gotchas (WinUI UIA patterns, TextBox native text pattern, shared-fixture
state leaks, external-consumer XAML paths), see
[`.claude/skills/consumability-review/references/lessons-learned.md`](.claude/skills/consumability-review/references/lessons-learned.md).
This file is the layer *above* that: the release gate chain.

---

## Pre-flight checklist — run BEFORE the first `Publish-Internal` rehearsal

The rehearsal aborts at the first failing gate and is expensive, so front-load the cheap checks.
After **any** reskin, token/font edit, new component, or new public DP:

1. **Static ripple scan** (seconds). A reskin/font/new-component staleness ~7 gates that do not
   show in the component diff — reconcile all of them up front. The full list and how to fix each is
   in [`.claude/skills/consumability-review/references/lessons-learned.md`](.claude/skills/consumability-review/references/lessons-learned.md)
   and in the reskin-ripple notes below.
2. **New component added?** → register it everywhere the manifests demand:
   - `x:Uid` + a `Strings/en-US/Resources.resw` entry on every new Gallery page (else
     `Verify-GalleryLocalization.ps1` fails).
   - the new page count in `scripts/Verify-GallerySmoke.ps1` (`$expectedRoundPageCount`).
   - any new `Verify-*.ps1` gate in `scripts/Gates.psd1` (else `Verify-GateManifest.ps1` fails).
   - the new `.xaml` in `Generic.xaml`'s merged set **and** `Verify-ResourceGraph.ps1`.
3. **Run the fast standalone gates** instead of waiting for the 40-min rehearsal to find their
   failures:
   - `pwsh scripts/Verify-GateManifest.ps1`
   - `pwsh scripts/Verify-GallerySmoke.ps1` (build-only is fine for the count)
   - `pwsh scripts/Verify-SilentPropertyCoverage.ps1 -EvidenceDir <newest artifacts/audit-runs/consumer-runtime-evidence-* dir>`
   - `pwsh scripts/Verify-ExternalConsumer.ps1` if the change touched markup-settable state.
4. **Only then** run the full `Publish-Internal.ps1` (no `-Push`) rehearsal.

---

## 1. ConsumerFixtures determinism flake — round-2 screenshot captured at 1x

- **Symptom:** `Publish-Internal` runs `Verify-ConsumerFixtures.ps1` **twice** and requires identical
  outcomes. Round 2 intermittently failed with a visual-baseline **size** mismatch, e.g.
  `progressBar/Light 861x29` vs the baseline's `2160x73`.
- **Root cause:** the harness captures with `RenderTargetBitmap.RenderAsync(control)`, which
  rasterizes at `XamlRoot.RasterizationScale`. A just-shown window can momentarily report
  `RasterizationScale == 1.0` before its PerMonitorV2 DPI context applies; if the first capture lands
  in that window it bakes 1x into that round only → the two rounds disagree. **Intermittent — one
  green run does not prove it fixed.**
- **Fix (`4e10f3d`):** `WaitForRasterizationScaleSettledAsync` in
  `tests/Ether.DesignSystem.ConsumerFixtures/RuntimeVerification.Infrastructure.cs` waits for the
  scale to stop changing before the first screenshot capture. Screenshot path only; the locked
  attached-visual-property audit capture is untouched.
- **Prevent:** never assume a freshly-shown WinUI window is at final DPI on the first frame — settle
  `RasterizationScale` before any size-sensitive capture. The `platform-dp-contract` classification
  is scale-invariant, so this only ever corrupts a screenshot **size**, never *which* property is
  silent.

## 2. SilentPropertyCoverage accounting goes stale on reskin / font / new component

- **Symptom:** `Verify-SilentPropertyCoverage.ps1` fails with a long list of properties that are
  `platform-dp-contract` in the newest evidence but unaccounted, plus mislabels and stale entries.
  **This gate runs only in `Publish-Internal`, not CI**, so it is invisible until the rehearsal.
- **Root cause:** `scripts/UnsupportedProperties.psd1` must account for **every** public inherited DP
  the newest runtime evidence marks `platform-dp-contract`. Three kinds of drift:
  - **Forward** — a newly-audited DP (new component like `EtherTabNavigation`/`EtherTabItem`, or a
    reused type pulled into the inventory like `EtherSegmentRadioButton` via `EtherPanelTabs`) is
    unaccounted.
  - **Template cross-check** — a reskin that adds/removes a `{TemplateBinding X}` flips the required
    bucket. The Masthead reskin dropped `BorderBrush`/`BorderThickness` bindings →
    `consumed-visually-stable` had to become `design-system-owned`.
  - **Reverse** — a font swap (Inter→Roboto) made a formerly-silent DP observable
    (`EtherInput.FontFamily`) → its stale entry had to be **removed**.
- **Fix (`02d3800`):** reconcile every entry. Buckets are **mechanically** verified: `design-system-owned`
  / `platform-noop` require the property **absent** from the mapped `TemplateFiles[Control]` `.xaml`
  (a whole-**file** grep — two controls sharing one template file, e.g. Tab Nav+Item, see each
  other's bindings); `consumed-visually-stable` requires it **present**. `behavioral` /
  `needs-review` are not template-checked (`needs-review` prints a non-fatal WARNING). Add a
  `TemplateFiles[Control]` mapping for any new templated control.
- **Prevent:** run the standalone gate against the newest evidence dir (seconds) after any of these
  changes — don't wait for the rehearsal. **Read the whole failure list**; don't under-count by
  grepping one control (2026-09-03 it looked like 28 Tab entries, it was really 47).

## 3. Reskin / new-component ripple across the CI gates

- **Symptom:** CI (or the rehearsal) fails on a gate that has nothing to do with the component you
  changed — `Verify-GalleryLocalization` (missing `x:Uid`), `Verify-GateManifest` (an unregistered
  new script), `Verify-GallerySmoke` (page count off by one), frozen token hashes, resource-graph
  merge set, visual baselines, per-component contract scripts.
- **Root cause / fix / full list:** these are enumerated with exact file locations in
  [`.claude/skills/consumability-review/references/lessons-learned.md`](.claude/skills/consumability-review/references/lessons-learned.md).
- **Prevent:** the pre-flight checklist above. The harness fails **serially**, so a static same-class
  scan up front collapses many 40-min cycles into one.

## 4. NuGet same-version cache trap (packaged MSIX build)

- **Symptom:** a packaged/MSIX build fails locally with `CS0246` for a control that clearly exists,
  while CI is green.
- **Root cause:** a stale, same-version package sits in the **global** NuGet cache; the MSIX gate
  lacks `--packages` isolation, so it restores the cached (old) package instead of current source.
  CI's clean cache does not have this problem.
- **Prevent:** trust CI over a dirty local cache. To reproduce/clear, remove the cached id from the
  global cache and restore again. (`Verify-ExternalConsumer.ps1` already clears + hash-verifies, so
  it never hits this.)

## 5. MSIX `mspdbcmf.exe could not be found`

- **Symptom:** CI MSIX produce warns/fails with `mspdbcmf.exe could not be found`.
- **Root cause:** gated by `@(PDBPayload)`, **not** `AppxSymbolPackageEnabled` (that knob is a trap
  and does not fix it). Cannot reproduce locally if the MSVC toolchain is installed.
- **Fix (`ff9870d`):** drop `.pdb` from `AppxPackagePayload`.

## 6. A green harness can still ship a real bug — run the external consumer

- **Symptom:** every in-repo gate is green, but a control is broken for the exact pattern a real
  consumer writes: **a property set as a XAML attribute, applied before the content child.**
- **Root cause / example:** `Verify-ConsumerFixtures.ps1` sets state in **code, after** content
  exists. `<EtherSegmentedControl SelectedValue="beta">` was silently cleared to null (callback fired
  with segments not yet realized) and the green harness never saw it — pre-existing for weeks. Fixed
  `4d51034`.
- **Prevent:** `scripts/Verify-ExternalConsumer.ps1` (real `dotnet add package` + real XAML markup)
  is the truest consumer sim and is **NOT in CI** — run it explicitly whenever the change touches
  markup-settable state. Green ≠ bug-free.

---

## Meta-lessons

- **The release gate chain is not in CI.** `Publish-Internal` runs `local-runtime` / `local-external`
  gates (see `scripts/Gates.psd1` tags) that hosted CI skips. A fully green PR can still fail the
  rehearsal. Budget for it; do not promise "one more run."
- **Fail serially → front-load statically.** Every gate aborts at the first failure. After you see
  the *class* of a problem, scan for all siblings before the next expensive run.
- **Iterate on the cheap gate, not the 40-min rehearsal.** Most release gates have a standalone
  `Verify-*.ps1` you can run in seconds against existing evidence. Use it.
- **Don't re-pack a version that was already pushed.** Preview packages are immutable once on the
  feed; bump the preview number instead.
