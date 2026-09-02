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

## Open — L3 contract gates (6 failing, scoped locally 2026-09-01)
Two flavors: **(a) stale gate expectation** (likely a list/key-set the gate hardcodes that drifted) vs.
**(b) real template/style fix** (the control genuinely violates the asserted contract). Triage each
before fixing; (b) items may carry a design decision.

| Gate | Error | Likely flavor |
| --- | --- | --- |
| `Verify-EtherButtonContract.ps1:208` | "EtherButton Disabled state must collapse FocusRing." | (b) template — Disabled VSM needs `Target="FocusRing.Visibility" Value="Collapsed"` |
| `Verify-EtherCheckboxContract.ps1` | "component resource keys differ from the expected lightweight key set." | (a) likely stale key-set |
| `Verify-EtherRadioButtonContract.ps1` | "component resource keys differ from the expected lightweight key set." | (a) likely stale key-set |
| `Verify-EtherIntelligenceButtonContract.ps1` | "DefaultEtherIntelligenceButtonStyle is missing its \<setter\>." | (a/b) triage the named setter |
| `Verify-EtherSteeringBarContract.ps1` | "\<Description\> is missing required pattern '\<Pattern\>'." | (a/b) triage the pattern |
| `Verify-EtherSwitchContract.ps1` | "EtherSwitch template is missing named part LayoutRoot." | (b) template part name |

Passing L3 gates (for reference): Dropdown, Input, Masthead, ProgressBar, ScrollBar, SegmentedControl,
Slider.

## Status-unknown — downstream `package-consumers` steps (not yet reached)
CI stops at the first failure, so these have **not run** since the L3 gates above fail first. Re-check
after the L3 gates are green:
- Verify silently-ineffective public properties documented (`Verify-SilentPropertyCoverage.ps1`)
- Verify backend-consumable interaction contracts
- Verify WinUI 3 control conventions (`Verify-WinUiConventions.ps1`)
- Verify High Contrast foreground/background pairing (`Verify-HighContrastPairing.ps1`)
- Verify Gallery ControlExample + localization contracts (note: the DX P2 Gallery demos add new blocks —
  re-run these after the DX work merges)
- Verify property-evidence wording (R-06)
- Verify package consumers / unsigned MSIX produce
- Verify the gate manifest matches the workflow (anti-drift, R-02)

## How to work this backlog
Same discipline as the consumability skill: for each gate, read what it asserts, decide flavor (a)
stale-expectation vs (b) real fix, fix the smallest correct thing, verify the single script locally
(`powershell -File scripts/Verify-Ether<X>Contract.ps1`), then push and let CI reveal the next. For (b)
template fixes, reference the official WinUI skeleton per `AGENTS.md` and flag any design decision.
