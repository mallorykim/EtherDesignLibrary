# `codex/refactor` branch handoff

Date: 2026-08-27  
Branch: `codex/refactor`  
HEAD: `48cac9f` plus HighContrast OS-selected runtime docs in this commit  
Audience: any agent continuing, reviewing, or verifying this branch **without chat history**.

This file is the operational context. It supersedes the 2026-08-26 pause notes that still talked about L2-in-progress and linked `src/Views/**`. Slice-level architecture notes under `docs/architecture/2026-08-26-*.md` remain valid for control-specific exceptions; if a path in those notes still says `src/Controls` or `src/Views`, the files now live under `src/Ether.DesignSystem.Controls/` and `samples/Ether.DesignSystem.Gallery/`.

The **original execution plan** is reproduced in [Appendix A](#appendix-a--original-execution-plan). Section 3 of that plan is a **pre-branch audit**, not a description of today's tree.

---

## 0. Read this first

1. This branch turns `EtherComponentSandbox` into a WinUI 3 design-library skeleton: Foundation + Controls packages, Gallery sample, package-consumer fixtures, and migrated templated controls.
2. It is **preview**, not production. Do not claim Fluent parity, stable API, or nuget.org readiness.
3. **Do not `nuget push`.** Local pack is `scripts/Pack-PreviewPackages.ps1` → `artifacts/packages/` (gitignored). Publish steps stay in `docs/releases/0.1.0-preview.1.md`.
4. **Do not** add Gallery GUI or consumer runtime smoke to `.github/workflows/build.yml`. Hosted CI is static-only (`-SkipRuntimeSmoke`). Local runtime owner: `scripts/Verify-RuntimeGates.ps1`.
5. **Do not** edit frozen token **content** except the already-landed `EtherColors.xaml` HighContrast slash-key mapping. Other token files are byte-frozen.
6. **Do not** invent UnitTests/UITests, Appium, Accessibility Insights, MSIX install, or arm64 runtime just to make the plan diagram look complete.
7. `pwsh` is not on PATH here. Use `powershell -File`. Before rebuilds, stop leftover `Ether.DesignSystem.ConsumerFixtures.Unpackaged` / Gallery / sandbox processes if they lock outputs.
8. Named fixture `x:Name` / `AutomationProperties.Name` values are contract. Do not rename them.

### Hard constraints still in force

| Constraint | Today |
| --- | --- |
| Token freeze | Paths below; hashes must match. Only authorized `EtherColors` HC mapping differs from plan-start hash `79440485…`. |
| Dependency | `Foundation ← Controls ← Gallery`. Consumer fixtures use **PackageReference** from a local nupkg feed, never library ProjectReference. |
| TFM | `net8.0-windows10.0.19041.0`, min OS `10.0.17763.0`. No framework upgrade on this branch. |
| Public API | Preview. Baselines: `PublicAPI.Shipped.txt` / `PublicAPI.Unshipped.txt`. `Unshipped` is the current surface, not a removal ledger. |
| Namespace | Still `EtherSandbox` / `Ether.DesignSystem.Controls` (preview public API). Do not rename as a drive-by. |
| Fonts / Assets | Stay at **repo root**. Frozen typography uses `ms-appx:///Fonts/...` and `ms-appx:///Assets/...`. |

Frozen token hashes (after the byte-for-byte Git move):

| File | `git hash-object` |
| --- | --- |
| `src/Ether.DesignSystem.Foundation/Resources/Tokens/EtherPrimitives.xaml` | `d6ff0e5301b3672dbb492484c8d0ea584aca12be` |
| `src/Ether.DesignSystem.Foundation/Resources/Tokens/EtherColors.xaml` | `d7c218e5a631e86552ed2b089cbc03bbd571a090` |
| `src/Ether.DesignSystem.Foundation/Resources/Tokens/EtherSpacing.xaml` | `5d631cd2ebde306d389a1fa8999000359441bcd2` |
| `src/Ether.DesignSystem.Foundation/Resources/Tokens/EtherTypography.xaml` | `b24444567ee95467408e004fe7fab622eba79759` |
| `src/Ether.DesignSystem.Foundation/Resources/Tokens/EtherIconGeometries.xaml` | `134c1667934376ba4943cf350b110a02607c81f4` |

```powershell
git diff -- src/Ether.DesignSystem.Foundation/Resources/Tokens
git ls-files 'src/Ether.DesignSystem.Foundation/Resources/Tokens/*' | ForEach-Object { git hash-object $_ }
```

External authoring spec used at branch start (`2026-08-24-winui3-fluent-design-library-authoring-spec.md`) may no longer exist on disk. **Do not invent a replacement spec.** The repository plan in Appendix A is the execution contract.

---

## 1. Where things live (today)

```text
Ether.DesignSystem.slnx
├─ src/Ether.DesignSystem.Foundation/     tokens, Themes/Foundation.xaml, buildTransitive fonts/assets
├─ src/Ether.DesignSystem.Controls/       packable controls, EtherCard, Themes/Generic.xaml + DesignSystem.xaml, PublicAPI
samples/Ether.DesignSystem.Gallery/       WinExe Gallery: App, Views, GalleryControls, Strings/en-US, Visuals
tests/Ether.DesignSystem.ConsumerFixtures/
│  Unpackaged/  + Packaged/               PackageReference-only hosts (not in the .slnx)
scripts/Verify-*.ps1
docs/architecture/2026-08-26-*.md
EtherComponentSandbox.csproj              legacy second host (not in Ether.DesignSystem.slnx)
Fonts/  Assets/                           repo-root host-root URIs — do not move
```

**Resource graph (do not bypass):**

`App / consumer` → `ms-appx:///Ether.DesignSystem.Controls/Themes/DesignSystem.xaml` → Controls `Generic.xaml` → Foundation `Foundation.xaml` → five token dictionaries.

Gallery also merges Gallery-only `ms-appx:///Resources/Visuals/EtherPageTitleGradient.xaml`.

**Legacy sandbox** ProjectReferences Foundation + Controls and compiles Gallery sources. It no longer compiles a second copy of controls or the flattened `src/Themes/Generic.xaml` graph (that file was deleted).

**Not created:** `tests/Ether.DesignSystem.UnitTests`, `tests/Ether.DesignSystem.UITests`.

**HighContrast runtime (docs in this commit):** unpackaged consumer proof is OS-selected (`SPI_SETHIGHCONTRAST`), not dictionary overlay. Named OS themes remain red; see §4.

---

## 2. Milestone status vs the original plan

Plan phases are Appendix A §5. Status is **this branch at `2d56d6f`**.

| Plan | Intent | Status |
| --- | --- | --- |
| **L0-A** | Directory.Build.*, `.slnx`, Foundation / Controls / Gallery split, Controls `Generic.xaml` + `DesignSystem.xaml` | **Done** |
| **L0-B** | Pack, PublicAPI, local nupkg feed, packaged + unpackaged consumer fixtures | **Done** |
| **L0-C** | CI, resource-key/graph audits, Gallery smoke, `SEMVER.md` | **Done** for static CI. Hosted CI does **not** launch WinUI. |
| **L1** | Foundation public merge graph, fonts/assets via `buildTransitive`, Light/Dark identity | **Done**. Unpackaged `StorageFile` host-root URIs remain 0/3 (documented). |
| **L2** | `EtherProgressBar` as templated exemplar | **Done** (preview exceptions documented). |
| **L3** | Ten control/style migrations | **Done** (`b3d3bf1`). Order in git is not identical to the numbered list (SteeringBar landed with the big L2/L3 commit; Slider/Masthead/Switch/ScrollBar closed L3). |
| **L4.1–2** | Gallery `ControlExample` (example/output/source/copy, responsive) | **Done** for **control** pages. Foundations primitives + Home stay on plain `ComponentPage` on purpose. |
| **L4.3 quality gates** | HC baselines, Appium, UIA snapshots, Insights, 225%, RTL, localization | **Split.** In-repo substitutes are green. External engines are **not** installed and stay red. See §4. |
| **L4.4–5** | Perf budgets, arm64 **runtime**, x64/arm64 package fixtures, **publish** | Partial: unsigned MSIX **produce**, arm64 **pack/compile**, in-repo PackageReference proof. **Publish held.** |
| **§4.1 layout** | Sources physically in Foundation / Controls / Gallery | **Done** (`2d56d6f`). Fonts/Assets stay at repo root. |
| **§4.1 tests** | UnitTests + UITests projects | **Not done** (intentionally not faked). |

Independent L0–L4 review (at the time, before relocation and some convention polish): `docs/architecture/2026-08-26-foundation-refactor-full-review-handoff.md` recorded **PASS with findings**. Prefer **this** file for current paths and remaining work.

---

## 3. What this branch actually shipped

Refactor commits after color-migration HEAD `09d93cd`:

| Commit | What it established |
| --- | --- |
| `efe5a21` | L0–L2 skeleton + L3 through SteeringBar (Foundation/Controls/Gallery, NuGet, CI, ProgressBar exemplar, Button→SteeringBar) |
| `b3d3bf1` | L3 complete: Slider + Masthead conversions; Switch keyed + ScrollBar implicit |
| `4bba0a9` | L4 first pass: `ControlExample` + Button/ProgressBar pilots |
| `e842f32` | ControlExample on remaining control pages |
| `4abe029` … `1d376e7` | Independent-review handoff and recorded PASS |
| `40df5ec` | Review follow-ups; package-consumer smoke host usable |
| `59835bc` | Package-consumer RTL gate; publish hold |
| `3cffdd3` | In-process UIA, 225% scale, fixture `.resw`, screenshots, timing |
| `23254c1` | arm64 pack + fixture compile (not runtime) |
| `066a2af` | Unsigned packaged-fixture MSIX produce (not install) |
| `43b7a64` | Fail empty UIA rects except EtherDropdown `LayoutFallback` |
| `fdbffb8` | Record in-repo L4-C substitutes without claiming Insights/Appium/publish |
| `2d56d6f` | Physical `git mv` into project trees; sandbox becomes a consumer host; remaining WinUI convention / Gallery loc / RTL work that was still uncommitted |

### L0 / L1 (engineering foundation)

- `Directory.Build.props`, `Directory.Packages.props`, `Ether.DesignSystem.slnx`
- Packages `Ether.DesignSystem.Foundation` `0.1.0-preview.1` and `Ether.DesignSystem.Controls` `0.1.0-preview.1` (lockstep)
- SourceLink, snupkg, XML docs, package XBF packing
- PublicAPI analyzers (`RS0016`/`RS0017` as errors)
- Consumer fixtures: Unpackaged (runtime) + Packaged (build + unsigned MSIX produce)
- Foundation `buildTransitive` copies `Fonts/` and `Assets/` to the **host** output root (not `ReferencePath` hacks)
- CI: `.github/workflows/build.yml` — Debug/Release x64 build, resource graph/keys, every `Verify-Ether*Contract.ps1`, `Verify-WinUiConventions.ps1`, `Verify-GalleryControlExample.ps1`, `Verify-ConsumerFixtures.ps1 -SkipRuntimeSmoke`

### L2 exemplar — EtherProgressBar

Templated `RangeBase`: `DefaultStyleKey`, `DefaultEtherProgressBarStyle` + implicit `BasedOn`, `[TemplatePart]` / `[TemplateVisualState]`, component ThemeDictionaries (Light/Dark/HC `SystemColor*`), private read-only `IRangeValueProvider`. Consumer proof uses `GetPattern` + in-process `AutomationRangeValueChanged` with `RangeValuePatternIdentifiers.ValueProperty`.  
Doc: `docs/architecture/2026-08-26-l2-ether-progressbar-exemplar.md`.

### L3 — all ten planned control/style slices

Each has a contract script, Gallery automation names, and unpackaged consumer runtime marker.

| Control / style | Base / contract | Documented exceptions (not silent bugs) |
| --- | --- | --- |
| EtherButton | `Button`; RightIconStates | Preview removal of `RightIconVisibility`. Content slot is `ContentPresenter` `Cp`. |
| EtherCheckbox / EtherRadioButton | native bases | Empty-subclass era ended; evidence tightness documented. |
| EtherInput | `TextBox` | Placeholder Light/Dark proof; Disabled via GoToState + `IsEnabled=false`. |
| EtherDropdown | `ComboBox` | Closed `Popup.Child` proof; DropDownStates still `GoToState` for Opened/Closed chrome. UIA bounding rect is explicit `LayoutFallback`. |
| EtherSegmentedControl | custom | Caster/shadow from collapsed template `CasterBrushSource` / `ShadowBrushSource`. |
| EtherIntelligenceButton | custom | Instant `0:0:0` transitions stay without easing. |
| EtherSteeringBar | `RangeBase` + peer | `PreviewStatus` kept. **`SteeringBarValueChangedEventArgs : EventArgs` is still preview public API** (spec preferred not to derive from `EventArgs`; left unchanged to avoid an unforced public break). |
| EtherSlider | **UserControl → `RangeBase`** | 63 bars + Knob are **template-declared**; code updates fill, `Canvas.Left`, knob hover/press. Not `RenderBars`. |
| EtherMasthead | **UserControl → `Control`**, packable | Close posts `WM_CLOSE` (`Window.Close` path), not `AppWindow.Destroy()`. |
| EtherSwitch | **keyed** stock `ToggleSwitch` style | Must **not** become implicit. |
| EtherScrollBar | **implicit** stock `ScrollBar` | Vertical + horizontal templates. |

EtherCard is Foundation-adjacent **Border styles** in Controls (`Resources/Foundations/EtherCard.xaml`) with Light/Dark/HC `ThemeDictionaries`. It is **not** a templated `Control`.

### L4 Gallery

- `ControlExample`: Example / Output / Source / Copy; 720px wide vs stacked; Copy confirmation 1500ms
- Options stay `ComponentPage.InteractiveControls`
- Control pages wrapping it: Button, ProgressBar, Checkbox, RadioButton, Input, Dropdown, SegmentedControl, IntelligenceButton, SteeringBar, Slider, ToggleSwitch, ScrollBar, Masthead, Card
- Foundations (Colors, Typography, Spacing, Radius, Icons) + Home: plain `ComponentPage`
- Smoke catalog **20/20** Light and Dark
- Gallery chrome `.resw` + `x:Uid` (`samples/Ether.DesignSystem.Gallery/Strings/en-US/Resources.resw`). Do **not** add a duplicate `<PRIResource>` (NETSDK1022).
- Gallery RTL: OS direction via `CultureInfo.CurrentUICulture.TextInfo.IsRightToLeft`. Smoke forces `RootGrid.FlowDirection=RightToLeft` and asserts NavView + ContentFrame inherit. **Do not** use `ResourceContext.GetForViewIndependentUse()` for this — it native-crashed unpackaged (`0xC000027B`).

### WinUI convention polish (landed with `2d56d6f`)

- `HighContrastAdjustment=None` on default control styles (CI: `Verify-WinUiConventions.ps1`)
- `CubicEase` on non-instant `VisualTransition`s
- DP field-before-wrapper on EtherButton Variant/Size, EtherProgressBar, EtherMasthead
- `OnApplyTemplate` calls `base` first where it was missing
- Exhaustive `default` throw on `CatalogNode` switches

### L4-C in-repo substitutes (green, **not** publish)

On the unpackaged **package consumer** unless noted:

- RTL `RightToLeft` inheritance + automation names (13 controls)
- In-process UIA snapshot (12 real rects; EtherDropdown `LayoutFallback`)
- 225% `ScaleTransform` (not OS text scaling)
- Fixture `.resw` / `x:Uid`
- Light/Dark `RenderTargetBitmap` under `artifacts/` (not HC, not golden-image CI)
- Elapsed harness `< 20000ms` (not scroll/animation budgets)
- Unsigned MSIX **produce** (`Verify-MsixPackage.ps1`; no `Add-AppxPackage`)
- arm64 **pack + fixture compile** (`Verify-Arm64Packages.ps1`; exe not launched)
- Local aggregator: `scripts/Verify-RuntimeGates.ps1`

---

## 4. Remaining / deliberately unfinished

Treat these as **known open work**, not as “the last agent forgot.” Do not fake them.

### Red external / release gates (do not install or claim)

- Accessibility Insights
- Appium / out-of-process UIA / WinAppDriver
- Hosted-CI WinUI launch (`build.yml` stays `-SkipRuntimeSmoke`)
- MSIX **install** and **runtime**
- arm64 **runtime** smoke
- On-device High Contrast themes (Aquatic / Desert / Night Sky)
- **nuget.org / GitHub Packages / Azure Artifacts publish**
- Out-of-repo consumer against a real feed (in-repo fixtures only prove `ether-local`)

### Plan items never built

- `tests/Ether.DesignSystem.UnitTests`
- `tests/Ether.DesignSystem.UITests`
- ControlExample on Foundations primitive pages (intentionally skipped)
- Charts, NavRail, other product controls not in the L3 list of ten
- Framework / Windows App SDK upgrades

### Honest limitations (documented, still true)

- Unpackaged `StorageFile.GetFileFromApplicationUriAsync` for host-root `ms-appx:///Fonts|Assets` → **0/3** in the consumer marker
- Packaged fixture: comma-named font files vs MSIX tooling (packaged is **build-only** for those fonts)
- `SteeringBarValueChangedEventArgs : EventArgs` left as preview API
- EtherCard is styled `Border`, not a templated control
- SteeringBar stop markers still use `new Border` in places
- Component ThemeDictionary hex is allowed at component scope; global tokens stay frozen
- Copy-to-clipboard is Gallery-only; no consumer fixture for Copy click
- `SourceXaml` snippets are representative, not always byte-identical to the live tree
- Slider / Segmented host fake `TemplateVisualState` where the platform tree cannot prove the state otherwise
- Public namespace still `EtherSandbox*`
- Gallery page-body copy is in `Strings/en-US/Resources.resw` (en-US only; `SourceXaml` stays English)
- HighContrast consumer runtime is OS-selected (`SPI_SETHIGHCONTRAST`); dictionary overlay / injected SystemColor* is not the claim. Contrast Aquatic / Desert / Night Sky are not proven unless those `.theme` files exist and were applied

### Do not “improve” without a new mandate

- Token **values** (except a new, explicit HC/product decision)
- Flattening Fonts into the Foundation project (breaks frozen URIs)
- Making EtherSwitch implicit or EtherScrollBar keyed
- Adding Gallery GUI to hosted CI
- Broad warning suppressions
- Rewriting SteeringBar `EventArgs` inheritance “to match the spec” without accepting a preview public-API change

---

## 5. How to verify (re-run; do not trust this paragraph alone)

Windows x64. PowerShell 5.1. From repo root; use `;` not `&&`.

```powershell
dotnet build Ether.DesignSystem.slnx -c Debug -p:Platform=x64
dotnet build Ether.DesignSystem.slnx -c Release -p:Platform=x64
dotnet build EtherComponentSandbox.csproj -c Debug -p:Platform=x64
dotnet build EtherComponentSandbox.csproj -c Release -p:Platform=x64

powershell -File .\scripts\Verify-ResourceGraph.ps1
powershell -File .\scripts\Verify-ResourceKeys.ps1
powershell -File .\scripts\Verify-ResourceKeys.ps1 -RequireHighContrastParity
powershell -File .\scripts\Verify-WinUiConventions.ps1
powershell -File .\scripts\Verify-GalleryControlExample.ps1
Get-ChildItem .\scripts\Verify-Ether*Contract.ps1 | ForEach-Object { powershell -File $_.FullName }

# Local desktop only (launches WinUI):
powershell -File .\scripts\Verify-GallerySmoke.ps1
powershell -File .\scripts\Verify-ConsumerFixtures.ps1
# or the aggregator:
powershell -File .\scripts\Verify-RuntimeGates.ps1 -SkipSolutionBuild

git diff -- src/Ether.DesignSystem.Foundation/Resources/Tokens
git ls-files 'src/Ether.DesignSystem.Foundation/Resources/Tokens/*' | ForEach-Object { git hash-object $_ }
```

Last full matrix before this handoff (2026-08-27, after `2d56d6f` content was still the working tree that became that commit): solution and sandbox Debug+Release x64 **0/0**; Light/Dark/HC keys **234/234/234**; Gallery smoke Light 20/20, Dark 20/20, RTL inherited; unpackaged consumer runtime passed for 13 controls + RTL + UIA + 225% + loc; StorageFile still 0/3.

Expect hosted CI to skip the GUI lines.

---

## 6. Related files (do not treat `.superpowers/` as source of truth)

| File | Role |
| --- | --- |
| This file (`HANDOFF.md`) | Branch context for the next agent |
| `docs/architecture/2026-08-26-repository-foundation-refactor-plan.md` | Same text as Appendix A (canonical path in git) |
| `docs/architecture/foundation-resource-contract.md` | Merge graph, fonts, elevation, HC parity gate |
| `docs/architecture/2026-08-26-l2-ether-progressbar-exemplar.md` | L2 exceptions |
| `docs/architecture/2026-08-26-l3-*.md` | Per-control migration notes |
| `docs/architecture/2026-08-26-l4-gallery-control-example.md` | ControlExample + still-red list |
| `docs/architecture/2026-08-26-l4c-quality-gates-plan.md` | How in-repo L4-C substitutes were implemented |
| `docs/architecture/2026-08-26-l4c-in-repo-quality-gates.md` | What those substitutes do and do not claim |
| `docs/releases/0.1.0-preview.1.md` | Pack vs publish hold |
| `SEMVER.md` | Preview/stable and resource-key compatibility |
| `docs/architecture/2026-08-26-foundation-refactor-full-review-handoff.md` | Independent review at L4 deepen; **paths in §4/§5 are stale** (pre-`git mv`) |

Official references used for control-authoring decisions:

- https://learn.microsoft.com/en-us/windows/apps/winui/winui3/xaml-templated-controls-csharp-winui-3
- https://learn.microsoft.com/en-us/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.controls.control.defaultstylekey
- https://learn.microsoft.com/en-us/windows/apps/design/accessibility/high-contrast-themes
- https://learn.microsoft.com/en-us/windows/apps/develop/platform/xaml/xaml-theme-resources

---

## Appendix A — Original execution plan

The following is the governing plan for this branch (`docs/architecture/2026-08-26-repository-foundation-refactor-plan.md` as maintained in git).

**How to read it now:**

- §3 “Current-state audit” describes the **sandbox before this branch**, not HEAD.
- Token **paths** in the maintained copy were updated after the `2d56d6f` Git move. Plan-start on-disk path was `src/Resources/Tokens/`. Content hashes for primitives/spacing/typography/icons never changed. `EtherColors` plan-start hash was `794404850d4eb20a2fc3ec43c3480be8f318ac64`; the authorized HighContrast slash-key mapping is `d7c218e5a631e86552ed2b089cbc03bbd571a090`.
- §4.1 originally allowed Link-first; physical move was allowed only after consumer fixtures passed. That move is done.
- §5 L4 quality/publish bullets are **not** all green; see §4 of this handoff.
- §2.3 named `gpt-5.6-terra` for coding. Later slices used whatever models the harness had (`cursor-grok-4.6-high-fast` appears in older notes). Follow the user’s current model instructions, not that sentence.

---

# Ether WinUI 3 design-library foundation refactor plan

Date: 2026-08-26  
Status: approved for execution  
Branch: `codex/refactor`

## 1. Objective

Refactor the current fast-moving `EtherComponentSandbox` into a repository that can grow into
the production-grade WinUI 3 design library defined by
`2026-08-24-winui3-fluent-design-library-authoring-spec.md`.

This iteration establishes the repository skeleton and engineering boundaries. It does not claim
that the existing controls already meet Fluent parity, accessibility, localization, packaging,
performance, or API-compatibility release gates.

The governing principles are:

1. Code is the source of truth.
2. Reusable controls use templated-control patterns rather than `UserControl`.
3. Foundation, controls, Gallery, and tests have explicit ownership boundaries.
4. Theme, accessibility, localization, public API, and package consumption are designed in from
   the start, then completed incrementally.
5. Every implementation slice remains buildable and reviewable.

## 2. Hard constraints

### 2.1 Frozen token content

The existing Primitive Token and Semantic Token files must not be edited during this refactor.
Their keys, values, theme mappings, and file contents are frozen.

Baseline Git blob hashes at plan start:

| File | Blob hash |
|---|---|
| `src/Resources/Tokens/EtherPrimitives.xaml` | `d6ff0e5301b3672dbb492484c8d0ea584aca12be` |
| `src/Resources/Tokens/EtherColors.xaml` | `794404850d4eb20a2fc3ec43c3480be8f318ac64` |
| `src/Resources/Tokens/EtherSpacing.xaml` | `5d631cd2ebde306d389a1fa8999000359441bcd2` |
| `src/Resources/Tokens/EtherTypography.xaml` | `b24444567ee95467408e004fe7fab622eba79759` |
| `src/Resources/Tokens/EtherIconGeometries.xaml` | `134c1667934376ba4943cf350b110a02607c81f4` |

Authorized later exception: `EtherColors.xaml` HighContrast slash-key mapping now hashes
`d7c218e5a631e86552ed2b089cbc03bbd571a090`. Other token files remain frozen at the
hashes above.

Every coding slice must finish with both:

```powershell
git diff -- src/Ether.DesignSystem.Foundation/Resources/Tokens
git ls-files 'src/Ether.DesignSystem.Foundation/Resources/Tokens/*' | ForEach-Object { git hash-object $_ }
```

Token gaps found by the audit are recorded but not corrected in this refactor.

### 2.2 Preserve behavior before improving behavior

The initial project split must not redesign controls or change visual values. Behavior, visual,
accessibility, and Fluent-parity improvements happen after the package and Gallery boundaries are
proven.

### 2.3 Coding ownership

The primary agent owns architecture, task definition, review, and acceptance. Coding is delegated
to `gpt-5.6-terra` subagents at high reasoning effort. A subagent may not broaden a slice when it
encounters an unexpected platform or XAML-compilation constraint.

## 3. Current-state audit

### 3.1 What is already useful

- The repository builds successfully for x64 Debug with zero warnings and zero errors.
- Primitive, semantic, spacing, typography, radius, icon, and component resources already exist.
- `EtherColors.xaml` uses `StaticResource` links inside its Light and Dark theme dictionaries,
  matching the specification's theme-dictionary correctness guard.
- Light and Dark currently contain the same 234 unique resource keys.
- A HighContrast dictionary exists.
- Several controls inherit appropriate native WinUI bases (`Button`, `CheckBox`, `RadioButton`,
  `TextBox`, `ComboBox`, `RangeBase`) rather than reimplementing all native behavior.
- `EtherDropdown` has an established template-part and popup/layout implementation worth
  preserving during the structural split.
- `EtherSteeringBar` already includes template parts, keyboard handling, pointer capture, and a
  complete `IRangeValueProvider` peer skeleton.
- The current app already functions as an early Gallery with foundation and component pages and a
  Light/Dark theme switch.

### 3.2 Architecture gaps

- One `WinExe` project currently owns the application, tokens, controls, assets, Gallery pages,
  and `Generic.xaml`.
- There are no Foundation or Controls class-library assemblies.
- The current `Generic.xaml` is an app-level master merge dictionary, not a Controls-assembly
  default-style dictionary with a separate consumer entry point.
- No actual control sets `DefaultStyleKey`; the only occurrence is a comment explaining its
  absence.
- Resource paths are flattened through project `Link` metadata and app-root `ms-appx:///` paths.
  This has not been proven from a `.nupkg` consumer.
- There is no `Directory.Packages.props`, active `Directory.Build.props`, package metadata,
  `buildTransitive` support, or package validation.

### 3.3 Control-authoring gaps

- Reusable `EtherSlider` and `EtherMasthead` are public `UserControl` implementations.
- No controls declare `[TemplateVisualState]` metadata.
- Only `EtherSteeringBar` creates a custom AutomationPeer.
- Existing templates do not follow the required keyed `Default[Control]Style` plus implicit
  `BasedOn` structure.
- There are 153 hard-coded XAML color occurrences outside the token dictionaries.
- Many templates consume primitives or global semantic resources directly instead of exposing
  component-level lightweight styling keys.
- Sixteen `VisualTransition` declarations exist; most specify durations without an explicit
  easing function.
- `EtherSlider` draws and mutates its visual tree from code and does not support High Contrast,
  keyboard semantics, or automation as a slider.
- `EtherSteeringBar` mutates visual properties directly instead of driving appearance through
  VisualStateManager, and its public `PreviewStatus` API is Gallery-oriented.
- Some public API patterns conflict with the specification: DP wrapper logic, constructor DP
  assignment, and an event-data class deriving from `System.EventArgs`.

### 3.4 Theme and token observations

- Light and Dark key sets match.
- HighContrast contains 89 of the 234 Light/Dark keys; 145 keys are absent, primarily the newer
  slash-named semantic and component roles.
- Only Checkbox, RadioButton, and Input currently define component-local Light/Dark/HighContrast
  dictionaries.
- Legacy PascalCase and newer slash-style semantic names coexist.
- Because token content is frozen, these facts are audit records only. Later controls may add
  component-level aliases without changing existing Primitive or Semantic Tokens.

### 3.5 Accessibility and localization gaps

- There are no `.resw` files and no `x:Uid` usage.
- Only a small number of `AutomationProperties.Name` declarations exist.
- `HighContrastAdjustment` is not configured.
- There is no Narrator, UIA-tree, text-scaling, contrast-theme, RTL, or pseudolocalization gate.

### 3.6 Testing and governance gaps

- There are no unit-test, UI-test, or NuGet consumer-fixture projects.
- There is no CI configuration.
- There are no `PublicAPI.Shipped.txt` or `PublicAPI.Unshipped.txt` files.
- The editor configuration only establishes line endings and encoding; analyzer and XAML-format
  policy are not enforced.
- There is no SemVer policy, package compatibility baseline, parity matrix, or performance budget.

## 4. Architecture decisions for this repository

### 4.1 Project layout

The target repository layout is:

```text
Ether/
├─ Directory.Build.props
├─ Directory.Packages.props
├─ Ether.DesignSystem.slnx
├─ src/
│  ├─ Ether.DesignSystem.Foundation/
│  └─ Ether.DesignSystem.Controls/
├─ samples/
│  └─ Ether.DesignSystem.Gallery/
├─ tests/
│  ├─ Ether.DesignSystem.UnitTests/
│  ├─ Ether.DesignSystem.UITests/
│  └─ Ether.DesignSystem.ConsumerFixtures/
└─ docs/
```

Frozen token source files now live at `src/Ether.DesignSystem.Foundation/Resources/Tokens/` after
a byte-for-byte Git move (hashes unchanged). Packable controls live under
`src/Ether.DesignSystem.Controls/`, and Gallery sources live under
`samples/Ether.DesignSystem.Gallery/`. Fonts and Assets remain at the repository root.

### 4.2 Dependency direction

```text
Foundation <- Controls <- Gallery
Foundation <- Consumer fixtures
Controls   <- Consumer fixtures
```

Foundation contains no custom control classes and no `Generic.xaml`. Controls owns
`Themes/Generic.xaml` and `Themes/DesignSystem.xaml`. Gallery helpers, forced preview states, and
sample-only UI remain in Gallery.

### 4.3 WinUI reuse policy

Reuse native WinUI control behavior whenever an appropriate base exists. Deriving from `Button`,
`ComboBox`, `TextBox`, `RangeBase`, and similar bases is preferred over reimplementing input and
automation behavior. The sealed stock `ToggleSwitch` and the stock `ScrollBar` created internally
by `ScrollViewer` remain explicitly styled native controls unless a future product requirement
justifies a full custom implementation.

### 4.4 Framework support

Keep the current `net8.0-windows10.0.19041.0` target and minimum OS `10.0.17763.0` during the
structural refactor. This is an explicit initial policy of “.NET 8 consumers only,” satisfying the
specification's requirement to choose a TFM policy. Framework and Windows App SDK upgrades are a
separate, evidence-backed change after package consumption works.

### 4.5 Package identity

Use two coordinated packages:

- `Ether.DesignSystem.Foundation`
- `Ether.DesignSystem.Controls`

Controls depends on the same release line of Foundation. Both packages are released in lockstep
until compatibility evidence supports independent versioning. Gallery and test projects are not
packable.

### 4.6 API maturity

Existing reusable controls are preview API until the first preview package establishes a PublicAPI
baseline. Gallery-only helpers and preview-state controls must not become package API accidentally.

## 5. Execution plan

### Phase L0-A — Repository and build skeleton

Deliverables:

1. Activate root `Directory.Build.props` and `Directory.Packages.props`.
2. Create the Foundation, Controls, and Gallery projects and add them to
   `Ether.DesignSystem.slnx`.
3. Keep package versions pinned and centrally declared.
4. Split compile/resource ownership without changing token content or visual behavior.
5. Establish `Themes/Generic.xaml` only in Controls and a separate
   `Themes/DesignSystem.xaml` consumer merge entry.
6. Keep the existing Gallery executable working through project references.

Exit criteria:

- Full solution builds for x64 Debug and Release.
- Frozen token files have zero diff and baseline blob hashes.
- Foundation has no control classes and no `Generic.xaml`.
- Controls has no dependency on Gallery.
- Existing Gallery page catalog still compiles and launches.

### Phase L0-B — Pack and real consumer skeleton

Deliverables:

1. Add package metadata, deterministic build, SourceLink, symbols, documentation files, and
   `IsPackable` only to the two libraries.
2. Add `PublicAPI.Unshipped.txt` scaffolding and API analyzer configuration.
3. Pack both real `.nupkg` files into a local feed.
4. Create packaged and unpackaged consumer fixtures that restore packages from the local feed,
   never through `ProjectReference`.
5. Verify XBF/PRI/resource dictionaries/fonts/icons and default style lookup.
6. Add a `buildTransitive` workaround only if the fixture proves it necessary; document the
   evidence and remove it if unnecessary.

Exit criteria:

- `dotnet pack` succeeds for both packages.
- Consumer fixtures restore and build from the produced packages.
- A minimal control and at least one Foundation resource resolve at runtime.
- Package contents are asserted after extraction.
- Token files remain unchanged.

### Phase L0-C — Minimum governance and smoke gates

Deliverables:

1. Add build CI for x64 Debug/Release and package-consumer smoke tests.
2. Enforce nullable warnings, .NET analyzers, deterministic output, and Release warnings-as-errors
   without mixing broad warning cleanup into the project split.
3. Add a resource-key audit that reports Light/Dark/HighContrast key-set differences without
   rewriting frozen tokens.
4. Add a Gallery navigation smoke-test mechanism so missing XAML resources fail before manual
   discovery.
5. Add `SEMVER.md` with preview/stable and resource-key compatibility rules.

Exit criteria:

- CI can reproduce build, pack, and package-consumer smoke results.
- Public API changes are visible in review.
- HighContrast gaps are reported, not silently ignored or automatically mutated.

### Phase L1 — Foundation integration, still token-frozen

Deliverables:

1. Make the existing frozen token dictionaries and assets the Foundation assembly's source of
   truth.
2. Prove Light/Dark switching and resource identity from the Gallery and `.nupkg` fixtures.
3. Define elevation and asset-loading scaffolding without changing current token values.
4. Document the existing HighContrast key deficit as an explicit release blocker.

Exit criteria:

- Gallery resolves Foundation resources only through the Foundation/Controls public merge graph.
- Light and Dark visual output remains unchanged.
- Fonts and icons load from both ProjectReference and package consumers.
- Frozen token hashes remain unchanged.

### Phase L2 — One compliant exemplar control

Use one bounded control as the canonical implementation. `EtherProgressBar` is the preferred first
candidate because it already derives from `RangeBase`, has limited interaction, and is smaller than
Dropdown or SteeringBar.

Deliverables:

1. Set `DefaultStyleKey`; introduce `[TemplatePart]` and `[TemplateVisualState]` contracts.
2. Implement keyed `DefaultEtherProgressBarStyle` plus implicit `BasedOn` style in
   `Themes/Generic.xaml`.
3. Move default DP values into Style setters where required.
4. Add component-level lightweight styling keys in all three themes without changing Primitive or
   Semantic Tokens.
5. Remove hard-coded template colors from this control.
6. Add XML documentation, PublicAPI entries, UI-thread unit tests, Gallery example, theme snapshots,
   accessibility checks, reduced-motion handling where applicable, and package-consumer coverage.

Exit criteria:

- The single-control Definition of Done in the governing specification is satisfied or every
  remaining exception is documented as a release blocker.
- The exemplar becomes the template for later controls.

### Phase L3 — Incremental control migration

Migrate one control per reviewable slice. Recommended order:

1. Button
2. Checkbox and RadioButton
3. Input
4. Dropdown
5. SegmentedControl
6. IntelligenceButton
7. SteeringBar
8. Slider conversion from `UserControl`
9. Masthead conversion from `UserControl`
10. Stock ToggleSwitch and ScrollBar style contracts

Each slice must add its contract metadata, keyed default style, component aliases, automation and
input verification, Gallery story, PublicAPI delta, and package-consumer smoke coverage. Controls
with an upstream WinUI equivalent also receive a pinned-source parity row.

### Phase L4 — Gallery, parity, and release gates

Deliverables:

1. Convert the current sample app to the WinUI-Gallery `ControlExample` pattern.
2. Add per-control examples, options, output, source snippets, copy support, responsive layout, and
   design guidance.
3. Add Light/Dark/HighContrast visual baselines, Appium interaction tests, UIA snapshots,
   Accessibility Insights gates, 225% text scaling, RTL, and localization.
4. Add performance budgets, material fallbacks, multi-input matrix, x64/arm64 package fixtures,
   and API/package compatibility baselines.
5. Publish the first preview package only after all declared preview gates are green.

## 6. Deferred work and non-goals for L0

- Do not alter Primitive or Semantic Token content.
- Do not complete the missing HighContrast semantic keys.
- Do not mass-remove hard-coded colors from every control.
- Do not upgrade .NET, Windows App SDK, or minimum OS in the same changes as the project split.
- Do not rewrite all controls as bare `Control` subclasses when a native WinUI base already
  provides the right semantics.
- Do not claim Fluent parity, stable API, or production readiness after only L0.

## 7. Review protocol for delegated coding

For every coding slice, the primary agent will:

1. Give the subagent an explicit file/scope boundary and acceptance tests.
2. Require the subagent to stop on unexpected XAML compiler, package-resource, or platform
   behavior instead of inventing a workaround.
3. Inspect the full diff, with special attention to token files and unrelated user work.
4. Build and test independently rather than relying on the subagent's report.
5. Verify token hashes.
6. Accept, request a bounded correction, or revert only the subagent's own slice.
