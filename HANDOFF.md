# Repository foundation refactor handoff

Date: 2026-08-26  
Branch: `codex/refactor`  
Pause point: L2 accepted in working tree (foundation + ProgressBar exemplar + verifier fixes; not yet committed as of this update)

## User mandate

- Follow the supplied WinUI 3 Fluent design-library authoring specification; repository-specific
  execution details are captured in
  `docs/architecture/2026-08-26-repository-foundation-refactor-plan.md`.
- The repository is in rapid iteration. Establish a durable skeleton and engineering foundation
  first, then improve breadth and polish incrementally.
- The primary agent owns audit, planning, and review. Coding work must be delegated to a
  `gpt-5.6-terra` subagent with high reasoning effort.
- Do not change existing Primitive or Semantic Tokens. For this refactor, treat every file under
  `src/Resources/Tokens/` as frozen unless the user explicitly changes that instruction.
- When implementation details are uncertain, research the official sources referenced by the
  governing specification.

The original specification path used at the start of the work was
`C:\DDPM-NEXUS-PILOT\docs\reference\superpowers\specs\2026-08-24-winui3-fluent-design-library-authoring-spec.md`.
It was read before work began but was no longer present at that path during the latest session.
Do not substitute new product requirements for it; use the repository plan as the recorded
specification-aligned execution contract until the source file is restored.

## Frozen token proof

Expected Git blob hashes:

| File | Hash |
| --- | --- |
| `src/Resources/Tokens/EtherPrimitives.xaml` | `d6ff0e5301b3672dbb492484c8d0ea584aca12be` |
| `src/Resources/Tokens/EtherColors.xaml` | `794404850d4eb20a2fc3ec43c3480be8f318ac64` |
| `src/Resources/Tokens/EtherSpacing.xaml` | `5d631cd2ebde306d389a1fa8999000359441bcd2` |
| `src/Resources/Tokens/EtherTypography.xaml` | `b24444567ee95467408e004fe7fab622eba79759` |
| `src/Resources/Tokens/EtherIconGeometries.xaml` | `134c1667934376ba4943cf350b110a02607c81f4` |

These hashes matched immediately before this pause commit, and `git diff --
src/Resources/Tokens` was empty.

## Completed and previously verified

### L0 — repository and package foundation

- Added central build/package configuration and `Ether.DesignSystem.slnx`.
- Split Foundation, Controls, and Gallery projects with dependency direction
  `Foundation <- Controls <- Gallery`; preserved the legacy sandbox project.
- Added preview NuGet packages, SourceLink/symbols/XML docs, PublicAPI baselines, package XBF
  content, and Controls-only packaged/unpackaged consumer fixtures.
- Added CI, resource-key and resource-graph audits, Gallery smoke testing, semantic-versioning
  policy, and bounded analyzer cleanup without broad suppressions.
- Verified Debug and Release x64 builds for the new solution and legacy project, package layout,
  unpackaged consumer runtime, and Light/Dark Gallery smoke before L2 began.

### L1 — Foundation resource contract

- Added `Themes/Foundation.xaml` and enforced the public merge graph:
  consumer -> Controls `DesignSystem.xaml` -> Controls `Generic.xaml` -> Foundation
  `Foundation.xaml` -> the five frozen token dictionaries.
- Added transitive font/SVG delivery without `ReferencePath` manipulation.
- Recorded the unpackaged `StorageFile` host-root `ms-appx` limitation honestly while proving the
  same assets through output files, FontFamily, and a visible `SvgImageSource`.
- Kept the known global High Contrast deficit as a stable-release blocker: Light and Dark each
  expose 234 keys, High Contrast exposes 89, and 145 keys remain missing from High Contrast.

See `docs/architecture/foundation-resource-contract.md` for the full contract.

## L2 EtherProgressBar — accepted

L2 is accepted as a preview exemplar (not stable-release readiness). Evidence reports live under
`.superpowers/sdd/` (`task-l2-verify-report.md`, `task-l2-fix-consumer-fixtures-report.md`,
`task-l2-review-report.md`, `task-l2-fix-rangevalue-evidence-report.md`,
`task-l2-rangevalue-rereview-report.md`, `task-l2-primary-recheck-report.md`).

Delivered:

- `DefaultStyleKey`, `DefaultEtherProgressBarStyle`, and implicit `BasedOn` style.
- Style-owned range/label defaults rather than constructor DP assignments.
- Five `[TemplatePart]` declarations and four `LabelStates` via
  `[TemplateVisualState]` plus `VisualStateManager`.
- Component-scoped Light/Dark/HighContrast resources; the template no longer contains literal
  colors. High Contrast uses Windows `SystemColor*` resources. Frozen tokens were not changed.
- A private, read-only ProgressBar `IRangeValueProvider` automation peer with Value-change events,
  `double.NaN` small/large changes, and name fallback to string `Title`.
- Gallery automation names for the simulator and four label variants.
- Package-consumer runtime assertions for default style, template parts, all label states, 65%
  fill layout, automation via `GetPattern(PatternInterface.RangeValue)`, rejected `SetValue`,
  live pattern value observation for `valueChangeExercised`, and Light/Dark template brushes/gradient.
- Static contract verifier wired into CI.
- Consumer-fixture verifier extracts `.nupkg` via `ZipFile` (`Expand-Package`) for Windows
  PowerShell 5.1 compatibility.

Detailed scope and acknowledged exceptions are in
`docs/architecture/2026-08-26-l2-ether-progressbar-exemplar.md`.

## Post-L2 verification matrix (independently re-run)

1. `dotnet build Ether.DesignSystem.slnx -c Release -p:Platform=x64` — passed (0/0).
2. `dotnet build EtherComponentSandbox.csproj -c Debug -p:Platform=x64` — passed.
3. `dotnet build EtherComponentSandbox.csproj -c Release -p:Platform=x64` — passed.
4. `./scripts/Verify-ResourceGraph.ps1` — passed.
5. `./scripts/Verify-ResourceKeys.ps1` — passed (Light/Dark 234; HC 89; missing 145).
6. `./scripts/Verify-ResourceKeys.ps1 -RequireHighContrastParity` — expected fail only for the
   documented 145 frozen-token gaps.
7. `./scripts/Verify-ConsumerFixtures.ps1 -SkipSolutionBuild` — passed after `Expand-Package` fix;
   unpackaged GUI marker and all `progressBar` fields validated; primary recheck also passed.
8. `./scripts/Verify-GallerySmoke.ps1` — passed (Light 20/20; Dark 20/20).
9. `git diff --check`, frozen-token hashes, residual process check — passed.
10. `./scripts/Verify-EtherProgressBarContract.ps1` — passed (including after RangeValue evidence
    strengthening).

Review: APPROVE after Important fixes (PS 5.1 nupkg extract; `GetPattern` RangeValue evidence).
Remaining UIA `RangeValuePatternIdentifiers.ValueProperty` subscription stays L4 (no public
in-process WinUI API). Coding/review subagents for this close-out used `cursor-grok-4.6-high-fast`
(`gpt-5.6-terra` was unavailable in the harness).

## L3-1 EtherButton — accepted

Accepted with documented preview exceptions (see
`docs/architecture/2026-08-26-l3-ether-button-migration.md` and
`.superpowers/sdd/task-l3-button-*.md`). Primary recheck passed with both `button` and
`progressBar` consumer markers.

## L3-2 EtherCheckbox + EtherRadioButton — accepted

Accepted with documented evidence-tightness concerns (same class as L3-1). Primary recheck
passed with `checkbox`, `radioButton`, `button`, and `progressBar` markers.
See `.superpowers/sdd/task-l3-checkbox-radio-*.md`.

## L3-3 EtherInput — accepted

Accepted with documented evidence exceptions (placeholder Light/Dark proof; Disabled via
GoToState + IsEnabled=false). Primary recheck passed with `input` plus prior consumer markers.
See `.superpowers/sdd/task-l3-input-*.md`.

## L3-4 EtherDropdown — accepted

Accepted with documented ComboBox closed-tree / GoToState evidence exceptions.
See `.superpowers/sdd/task-l3-dropdown-report.md`.

## L3-5 EtherSegmentedControl + EtherIntelligenceButton — accepted

Plan A: Wave 1 parallel (control-only) + Wave 2 integration with **merged full-matrix recheck**
(no separate primary subagent). See `.superpowers/sdd/task-l3-wave2-integration-report.md`
and Wave 1 reports.

## L3 migration protocol (optimized)

- Parallel Wave 1: Controls Debug + local contract only.
- Wave 2 / serial slices: one integrator/implementer runs full matrix once.
- Review subagents use `composer-2.5-fast`; implement uses `cursor-grok-4.6-high-fast`.
See `.superpowers/sdd/plan-a-parallel-protocol.md`.

## L3-7 EtherSlider — accepted

UserControl → templated `RangeBase`. Code-driven 63-bar rendering documented.
Review: Approved with concerns. Report: `.superpowers/sdd/task-l3-slider-report.md`.

## L3-8 EtherMasthead — accepted

UserControl → templated `Control`, now packable in Controls. Window chrome via
`XamlRoot`/`AppWindow` (Close uses `Destroy()`). Review: Approved with concerns.
Report: `.superpowers/sdd/task-l3-masthead-report.md`.

## L3-9 EtherSwitch + EtherScrollBar — accepted

Stock-type style contracts (keyed ToggleSwitch / implicit ScrollBar). Review: Approved with
concerns. Report: `.superpowers/sdd/task-l3-switch-scrollbar-report.md`.

## L3 migration — complete

Committed at `b3d3bf1`. All plan item 10 controls migrated.

## L4 — accepted (preview first pass) + ControlExample deepen complete

Gallery `ControlExample` chrome and preview-gate documentation. This is **not**
full L4 completion or preview-package publish. See
`docs/architecture/2026-08-26-l4-gallery-control-example.md`.

Model policy: **`cursor-grok-4.6-high-fast`** for implement and review.

Shipped:

- Gallery-only `ControlExample` with Example, Output, Source, Copy, and
  720px-wide responsive stacking. Options remain
  `ComponentPage.InteractiveControls` (right of INTERACTIVE).
- Control pages wrapping `ControlExample`: Button, Progress Bar, Checkbox,
  Radio Button, Input, Dropdown, Segmented Control, Intelligence Button,
  Steering Bar, Slider, Toggle Switch, Scroll Bar, Masthead, Card.
- Foundations primitives (Colors, Typography, Spacing, Radius, Icons) and
  Home stay on plain `ComponentPage`. Smoke catalog remains 20/20.
- `scripts/Verify-GalleryControlExample.ps1` wired in CI.

In-repo L4-C substitutes (package consumer, not publish): RTL inheritance;
in-process UIA snapshot (EtherDropdown `LayoutFallback`; other 12 UIA rects);
225% `ScaleTransform` + fixture `.resw` / `x:Uid`; Light/Dark captures under
artifacts; elapsed harness `< 20000ms`; unsigned MSIX **produce**; arm64
**pack/compile**. Local owner: `scripts/Verify-RuntimeGates.ps1`.

Still red: Accessibility Insights; Appium / out-of-process UIA; hosted-CI
WinUI smoke (`build.yml` keeps `-SkipRuntimeSmoke`); MSIX **install/runtime**;
arm64 **runtime**; High Contrast 145-key parity; preview package **publish**
(held).

Local pack is ready (`scripts/Pack-PreviewPackages.ps1` → `artifacts/packages/`).
Do not push until the first public preview's control set is accepted. Remaining
push steps live in `docs/releases/0.1.0-preview.1.md`. In-repo consumer fixtures
already prove PackageReference consumption from a local nupkg feed.

Keep Primitive/Semantic Tokens frozen.

## Whole-work independent review handoff

For an agent reviewing **the entire L0–L4 effort** without chat context, start at:

**`docs/architecture/2026-08-26-foundation-refactor-full-review-handoff.md`**

It includes the commit map (`efe5a21` … `e842f32` + this handoff commit), phase
deliverables, frozen-token hashes, the full verification matrix, known concerns,
and an explicit L4-C deferral list. Prefer that document over `.superpowers/` scratch.

## Known non-completions and release blockers

- The L2 runtime evidence is structured, not a pixel screenshot baseline.
- High Contrast validation for EtherProgressBar is currently a static resource-contract check,
  not an on-device pass across all Windows contrast themes.
- In-repo substitutes exist for RTL inheritance, in-process UIA (dropdown
  `LayoutFallback`), 225% `ScaleTransform`, fixture `.resw` / `x:Uid`, artifact
  screenshots, elapsed harness, unsigned MSIX produce, and arm64 pack/compile.
  Accessibility Insights, Appium / out-of-process UIA, hosted GUI CI, MSIX
  install/runtime, arm64 runtime, and preview **publish** remain red.
- Unpackaged package-consumer RTL (`RightToLeft` inheritance + automation names) is in
  `Verify-ConsumerFixtures.ps1`; it is not Gallery RTL or Insights.
- Hosted CI does not launch WinUI; Gallery smoke, L3 runtime markers, MSIX
  produce, and arm64 pack are `scripts/Verify-RuntimeGates.ps1` on a desktop
  session, not `build.yml`.
- The frozen global token High Contrast parity gate intentionally remains red.
- Do not broaden L2 into bulk control conversion. After L2 passes review, continue with the
  migration-template phase defined by the repository plan.

## Official references used for the latest decisions

- WinUI templated controls: https://learn.microsoft.com/en-us/windows/apps/winui/winui3/xaml-templated-controls-csharp-winui-3
- `DefaultStyleKey`: https://learn.microsoft.com/en-us/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.controls.control.defaultstylekey
- `VisualStateManager.GoToState`: https://learn.microsoft.com/en-us/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.visualstatemanager.gotostate
- Contrast themes: https://learn.microsoft.com/en-us/windows/apps/design/accessibility/high-contrast-themes
- XAML theme resources: https://learn.microsoft.com/en-us/windows/apps/develop/platform/xaml/xaml-theme-resources
- ProgressBar UI Automation contract: https://learn.microsoft.com/en-us/windows/win32/winauto/uiauto-supportprogressbarcontroltype
- WinUI `IRangeValueProvider`: https://learn.microsoft.com/en-us/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.automation.provider.irangevalueprovider

