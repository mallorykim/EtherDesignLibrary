# Foundation refactor — independent review report

Date: 2026-08-26  
Branch: `codex/refactor`  
Reviewed range: `09d93cd..50f080f` (134 files, +11718/−1235)  
Brief: `docs/architecture/2026-08-26-foundation-refactor-full-review-handoff.md`  
Reviewer: independent agent session (no reliance on prior agent reports under `.superpowers/`)

## Verdict

**PASS.** The L0–L4 foundation refactor matches the handoff's claims. All four hard
constraints hold, the full verification matrix is green (the only red gate is the
declared High Contrast parity blocker, failing with exactly the documented count),
and no undisclosed regression or overclaimed deliverable was found. Four
non-blocking findings are recorded below.

## Method

1. Direct verification of frozen tokens, commit history, and PublicAPI baselines.
2. A build/run agent executed the complete verification matrix from handoff §5.
3. A static-audit agent checked project boundaries, L2/L3 contract spot-checks,
   L4 ControlExample coverage, documentation honesty, and token-copy drift risk.

## Hard-constraint verification

| Constraint | Result |
| --- | --- |
| Frozen tokens | ✅ All five `git hash-object` values match the handoff baseline; `git diff -- src/Resources/Tokens` empty; **no commit in the review range touches the directory**; single source of truth (only gitignored `artifacts/` pack outputs duplicate the files) |
| Dependency direction | ✅ Foundation has zero ProjectReferences; Controls references Foundation only; Gallery uses ProjectReference (allowed for the sample) and links `src/Views/**`; consumer fixtures use `PackageReference` `0.1.0-preview.1` from the `ether-local` feed via `NuGet.Config` — no ProjectReference |
| Preview maturity | ✅ `PublicAPI.Shipped.txt` empty in both libraries; Unshipped = 152 (Controls) + 5 (Foundation) entries |
| HC parity blocker | ✅ `Verify-ResourceKeys.ps1 -RequireHighContrastParity` fails with **exactly Missing: 145**, HighContrast-only: 0 |

## Verification matrix results

| Check | Result |
| --- | --- |
| `dotnet build` Debug x64 | ✅ 0 warnings / 0 errors |
| `dotnet build` Release x64 | ✅ 0 warnings / 0 errors |
| 13 × `Verify-Ether*Contract.ps1` | ✅ All pass |
| `Verify-GalleryControlExample.ps1` | ✅ Pass |
| `Verify-ResourceGraph.ps1` | ✅ Pass (consumer → DesignSystem → Generic → Foundation → 5 token dictionaries) |
| `Verify-ResourceKeys.ps1` | ✅ Light 234; Dark 234; HighContrast 89; no Light/Dark-only keys |
| `Verify-ResourceKeys.ps1 -RequireHighContrastParity` | ❌ Expected fail, Missing: 145 (declared blocker, exact match) |
| `Verify-ConsumerFixtures.ps1 -SkipSolutionBuild` | ✅ All 13 runtime markers present (progressBar…scrollBar); StorageFile 0/3 is the documented unpackaged host-root limitation, not a new defect |
| `Verify-GallerySmoke.ps1` | ✅ Light 20/20; Dark 20/20 |

## Structural spot-checks (all confirmed)

- **EtherProgressBar (L2 exemplar):** `DefaultStyleKey` in constructor
  (`EtherProgressBar.cs:49`); keyed `DefaultEtherProgressBarStyle` + implicit
  `BasedOn` style (`EtherProgressBar.xaml:42,129`); control template uses only
  theme/static resource brushes (hex lives only in `ThemeDictionaries`); private
  sealed automation peer implements read-only `IRangeValueProvider`
  (`EtherProgressBar.cs:164`) with `SetValue` throwing.
- **EtherSlider:** `sealed class EtherSlider : RangeBase`
  (`EtherSlider.xaml.cs:34`); code-driven `RenderBars()` (63 bars) retained as
  the documented exception.
- **EtherMasthead:** `sealed class EtherMasthead : Control`
  (`EtherMasthead.xaml.cs:40`); `AppWindow` resolved via
  `XamlRoot?.ContentIslandEnvironment` and Close calls `Destroy()`.
- **EtherSwitch / EtherScrollBar:** keyed `x:Key="EtherSwitch"` ToggleSwitch
  style (`EtherSwitch.xaml:286`) with no implicit ToggleSwitch style anywhere in
  `src/`; implicit ScrollBar style (`EtherScrollBar.xaml:88`) covering vertical
  and horizontal roots, the only ScrollBar style in live code.
- **EtherButton:** `RightIconVisibility` fully removed from live code; only doc
  mentions remain, and `Verify-EtherButtonContract.ps1:63` actively rejects its
  reintroduction. `RightIcon` remains and visibility is VSM-driven
  (`RightIconStates`).
- **ControlExample (L4):** Gallery-only (zero references in either library
  csproj); wraps exactly the 14 claimed control pages; the five Foundations
  primitive pages (Colors, Typography, Spacing, Radius, Icons) remain plain
  `ComponentPage`.
- **Documentation honesty:** `HANDOFF.md` and the L4 doc list every L4-C item
  (pixel baselines, Appium/UIA, Accessibility Insights, 225%/RTL/localization,
  arm64 fixture, MSIX, performance budgets, preview publish) as deferred/red.
  No overclaim found.

## Findings (non-blocking)

1. **CI gate gap (most actionable).** `.github/workflows/build.yml` never invokes
   `Verify-GallerySmoke.ps1`, and runs `Verify-ConsumerFixtures.ps1` with
   `-SkipRuntimeSmoke`. The "Gallery smoke 20/20" and L3 runtime-marker pass
   criteria are therefore only enforceable by manual/local runs, not by the
   pipeline. No document claims otherwise, so this is a gap, not a
   misrepresentation — but a self-hosted/windowed CI lane (or a scheduled manual
   checklist) should own these before the first preview publish.
2. **Handoff wording imprecision.** Handoff §6.8 implies a PublicAPI "removal
   record" for the `RightIconVisibility` preview break; `PublicAPI.Unshipped.txt`
   contains no such ledger entry (the analyzer format does not track removals
   that way). The removal is backed only by docs plus the contract-script guard.
3. **Page folder placement.** `ScrollBarPage.xaml` lives under
   `src/Views/Foundations/` and `ProgressBarPage.xaml` under
   `src/Views/DataDisplay/`, inconsistent with the layout the architecture map
   implies for control pages. Organizational only.
4. **Untracked scratch.** `.superpowers/` sits untracked in the worktree. The
   handoff already disclaims it as non-authoritative; add it to `.gitignore` to
   prevent accidental commits.

## Conclusion

The refactor can be accepted at the state the handoff declares. The real release
blockers remain the declared L4-C list (HC 145-key gap, pixel/UIA/accessibility
automation, arm64, MSIX, first preview publish); this review found no new ones.
