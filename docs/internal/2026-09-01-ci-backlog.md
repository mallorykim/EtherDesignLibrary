# CI backlog — pre-existing gate failures on `codex/refine-components`

> **Context.** The branch was 73 commits ahead of `origin/main` and had **never run through CI**
> until 2026-09-01. When first pushed, CI (`.github/workflows/build.yml`) surfaced a series of
> pre-existing contract/anti-drift gate failures. These are **unrelated to the consumability review +
> remediation and the DX-optimization work** (both of which are independently verified green by
> `scripts/Verify-ConsumerFixtures.ps1` → `ETHER_CONSUMER_SMOKE outcome:"success"`). Per decision,
> these are recorded here as backlog rather than fixed in that work.
>
> The `build` job (Debug/Release x64 compile) **passes**. All failures are in the `package-consumers`
> job's static audit steps, which stop at the first failure — so items below the first-unreached line
> are **status-unknown** until the ones above them are fixed.

## Fixed already (2 commits, pushed)
- **HighContrast token parity** — `background/tabs/{default,selected,hover,pressed}` were in Light/Dark
  but missing from the HighContrast theme dictionary in `EtherColors.xaml`. Added + updated the frozen
  token-hash in `Verify-ResourceKeys.ps1` / `Verify-ConsumerFixtures.ps1`. (commit `da3b0c5`)
- **Resource-graph gate** — `Verify-ResourceGraph.ps1`'s expected Controls `Generic.xaml` merge list
  omitted `EtherTabNavigation.xaml`. Registered it. (commit `f2090ad`)

## RESOLVED — L3 contract gates (all 6 green, 2026-09-02)
Each triaged **(a) stale gate expectation** vs. **(b) real template/style fix**, fixed the smallest
correct thing, and re-verified the single script locally (exit 0). All 6 now pass.

| Gate | Flavor | Fix |
| --- | --- | --- |
| `Verify-EtherButtonContract.ps1` | (b) template | Added `<Setter Target="FocusRing.Visibility" Value="Collapsed"/>` to all 3 Disabled VisualStates in `EtherButton.xaml` — the Disabled state genuinely left the focus ring visible. |
| `Verify-EtherCheckboxContract.ps1` | (a) stale gate | Added `EtherCheckboxFocusBrush` to `$expectedComponentKeys` — the template already ships the key; the frozen list omitted it. |
| `Verify-EtherRadioButtonContract.ps1` | (a) stale gate | Added `EtherRadioButtonFocusBrush` to `$expectedComponentKeys` — same drift as Checkbox. |
| `Verify-EtherIntelligenceButtonContract.ps1` | (a/b) | Added `<Setter Property="FontSize" Value="{StaticResource Size14}"/>` to `DefaultEtherIntelligenceButtonStyle` — matches the template's hardcoded content-presenter FontSize, so **no visual change**; satisfies the gate's "style declares FontSize" contract. |
| `Verify-EtherSteeringBarContract.ps1` | (a) gate wording | Added the value-normalization rationale comment + refactored `if (e.Property == ValueProperty)` into the named-candidate form the gate matches. **No behavior change** (reentrancy guard + `NormalizeValue` already present). |
| `Verify-EtherSwitchContract.ps1` | (a) stale gate (whole tail) | The template was **rebuilt on the official WinUI ToggleSwitch skeleton** (commit `4cdfe3e`) adopting stock part names, but the gate (last touched `af909c9`, **before** the rebuild) still asserted a non-stock variant's phantom names. The `ConsumerFixtures` fixture already uses the stock names — the gate was the lone drifter. See the reconciliation below. |

**EtherSwitch gate reconciliation (flavor (a), the big one).** Phantom gate name → real skeleton part:
`OffLabel`→`OffContentPresenter`, `OnLabel`→`OnContentPresenter`, `SwitchArea`→`SwitchAreaGrid`,
`KnobTransform.X` Setter→`KnobTranslateTransform.X` Storyboard `To="12"`, `TrackOn.Opacity`→`TrackVisuals.Opacity`,
`OnLabel.Opacity`→`LabelsHost.Opacity`, `Knob.BorderBrush`→`KnobFrame.BorderBrush`,
`KnobFill.Background`→`SwitchKnobOff.Fill`, `KnobFillOn.Background`→`SwitchKnobOn.Background`. Every
check's **intent is preserved** (knob geometry 22×14 / 18×10, margin 3, On-travel 12, Disabled fades to
0.4, Disabled knob brushes, no BitmapCache) — only the part names now match the skeleton the template +
fixture actually use. Rewriting the template to the gate's names was rejected: it would violate AGENTS.md
("keep the official skeleton") and break the documented off→on→off flicker fix (On-travel is a stock
ToggleStates Storyboard, not a Setter). Two additive template touches also satisfy legit gate intents with
no better reconciliation target: named the root `x:Name="LayoutRoot"` and cited Figma node `62138:28454`
in the header (both harmless — an x:Name on a previously-unnamed root and a doc comment; no visual change).

Passing L3 gates (for reference): Dropdown, Input, Masthead, ProgressBar, ScrollBar, SegmentedControl,
Slider.

## Downstream `package-consumers` steps — status after L3 went green (CI run 33586917387)
With the L3 gates green in CI, the job advanced past them and exposed the **next latent failure**. The
`package-consumers` job runs on a **fresh runner with NO `dotnet restore`/`build` step** (it is a
static-audit + fixture-pack job), so any step that shells out to dotnet must self-restore.

- **Verify silently-ineffective public properties** (`Verify-UnsupportedProperties.ps1`) — ✅ green in CI.
- **Verify backend-consumable interaction contracts** (`Verify-InteractionContracts.ps1`) — ❌ was the
  next CI failure: `NETSDK1004` (missing `project.assets.json` for `Interactions.ContractTests`). Its
  `dotnet run … --no-restore` (line 45) assumed a prior restore that this job never does. **Fixed
  2026-09-02**: dropped `--no-restore` so the smoke test self-restores. (Passed locally only because a
  dev machine already has the assets file — a local-vs-CI gap; CI is authoritative here.)
- **WinUiConventions / HighContrastPairing / GalleryControlExample / GalleryLocalization /
  PropertyEvidenceWording / GateManifest** — all static file-read audits; green locally, no restore gap.
  (The DX P2 Gallery ControlExample + localization gates pass — the new data-binding demo blocks conform.)
- **Verify package consumers** (`Verify-ConsumerFixtures.ps1 -SkipSolutionBuild -SkipRuntimeSmoke`) —
  self-restores each fixture against the freshly-packed local feed (line 870) and guards staleness with
  `Assert-RestoredControlsPackageMatchesLocalFeed` (872); expected green in CI.
- **Verify unsigned MSIX produce** (`Verify-MsixPackage.ps1 -SkipSolutionBuild`) — **uncertain**. Fails
  **locally** with `CS0246: EtherTabNavigation could not be found` in the Packaged fixture — the classic
  NuGet *same-version-different-content* cache trap: the dev global cache holds a stale `0.1.0-preview.1`
  Controls package from before `EtherTabNavigation` existed, and the MSIX build reuses it. A fresh CI
  runner has an empty global cache and pulls the freshly-packed package from the local feed, so CI may
  pass where local fails. **Pending CI confirmation** — this is the documented "sole residual
  = packaged MSIX" blocker; do not chase the unreproducible-in-CI local cache artifact.
- **Verify the gate manifest matches the workflow** (`Verify-GateManifest.ps1`, R-02) — static; green locally.

## ConsumerFixtures acceptance constants — RESOLVED (correcting an earlier mis-diagnosis)
- The `Verify-ConsumerFixtures.ps1` throw "unpackaged runtime smoke fixture did not verify Foundation
  resources, assets, and theme re-resolution" has a **misleadingly generic message**: its actual
  condition (`:1215-1251`) gates on **acceptance-count constants** (`expectedConsumedProperties`,
  inventory/writable/classification counts), **not** on `assets[].StorageFileResolved`. The real
  blocker after the DX work was the P3 `EtherSegmentedControl.SelectionCommand` /
  `SelectionCommandParameter` DPs (2 new public writable DPs) drifting the counts while the constants
  weren't updated. Reconciled 2026-09-02 (expectedConsumedProperties +2, inventory 1764→1766, writable
  1395→1397, platform 1122→1124) → `Consumer fixtures passed`. An earlier commit (`d8e07a1`) wrongly
  blamed a "flaky asset StorageFile check"; that was a mis-read — `assets[].StorageFileResolved:false`
  is **expected and tolerated** (the fixture's `AssertApplicationFileAsync` documents that unpackaged
  hosts can't resolve ms-appx via `StorageFile`, and proves the asset via on-disk + XAML load instead;
  `svgImageLoaded:true`). Lesson: read the *condition*, not the throw *message*; and when you add a DP
  to an audited control, reconcile these constants.

## How to work this backlog
Same discipline as the consumability skill: for each gate, read what it asserts, decide flavor (a)
stale-expectation vs (b) real fix, fix the smallest correct thing, verify the single script locally
(`powershell -File scripts/Verify-Ether<X>Contract.ps1`), then push and let CI reveal the next. For (b)
template fixes, reference the official WinUI skeleton per `AGENTS.md` and flag any design decision.
