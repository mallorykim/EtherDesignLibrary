# Foundation refactor — full review handoff

Date: 2026-08-26  
Branch: `codex/refactor`  
Purpose: Give an **independent reviewing agent** a self-contained map of the entire
repository foundation refactor (L0–L4) so it can audit correctness, scope, frozen
tokens, packaging, and evidence **without relying on chat history**.

**Review tip:** Prefer reading this file + the architecture docs + running the
verification matrix below over trusting prior agent reports under `.superpowers/`.

---

## 1. Mission and constraints

### Mission

Refactor the fast-moving `EtherComponentSandbox` into a production-oriented WinUI 3
design-library skeleton per:

- `docs/architecture/2026-08-26-repository-foundation-refactor-plan.md`
- Original authoring intent (WinUI 3 Fluent design-library authoring spec; source path
  may no longer exist — treat the repository plan as the execution contract)

### Hard constraints (must still hold)

1. **`src/Resources/Tokens/**` is frozen** — no content edits. Baseline blob hashes:

| File | Expected `git hash-object` |
| --- | --- |
| `src/Resources/Tokens/EtherPrimitives.xaml` | `d6ff0e5301b3672dbb492484c8d0ea584aca12be` |
| `src/Resources/Tokens/EtherColors.xaml` | `794404850d4eb20a2fc3ec43c3480be8f318ac64` |
| `src/Resources/Tokens/EtherSpacing.xaml` | `5d631cd2ebde306d389a1fa8999000359441bcd2` |
| `src/Resources/Tokens/EtherTypography.xaml` | `b24444567ee95467408e004fe7fab622eba79759` |
| `src/Resources/Tokens/EtherIconGeometries.xaml` | `134c1667934376ba4943cf350b110a02607c81f4` |

2. Dependency direction: **Foundation ← Controls ← Gallery**. Consumers use NuGet packages,
   not ProjectReference to libraries in fixtures.
3. Preview API maturity — not stable release readiness.
4. Global High Contrast key parity for frozen tokens remains an intentional release blocker
   (145 keys missing from HC vs Light/Dark 234).

---

## 2. Commit map (review range)

Work since color-migration HEAD `09d93cd` is captured mainly in these phase commits:

| Commit | Phase | Summary |
| --- | --- | --- |
| `efe5a21` | L0–L2 + L3 early | Foundation/Controls/Gallery split, NuGet, CI, ProgressBar exemplar, Button→SteeringBar |
| `b3d3bf1` | L3 complete | Slider + Masthead conversions; Switch keyed + ScrollBar implicit stock styles |
| `4bba0a9` | L4 first pass | Gallery `ControlExample` + Button/ProgressBar pilots + static verifier |
| `e842f32` | L4 deepen | ControlExample rollout to remaining 12 control pages |

**Suggested review range:** `09d93cd..HEAD` (or `efe5a21^..HEAD` if focusing only on
foundation refactor commits).

Local scratch (not committed): `.superpowers/sdd/*` task reports — optional evidence,
**not** source of truth.

---

## 3. What each phase delivered

### L0 — Repository / package foundation

- `Directory.Build.props`, `Directory.Packages.props`, `Ether.DesignSystem.slnx`
- Projects: `Ether.DesignSystem.Foundation`, `Ether.DesignSystem.Controls`,
  `samples/Ether.DesignSystem.Gallery` (links existing `src/Views/**`)
- Preview NuGet packaging, SourceLink/symbols/XML docs, PublicAPI baselines
- Packaged + unpackaged consumer fixtures under `tests/Ether.DesignSystem.ConsumerFixtures/`
- CI: `.github/workflows/build.yml`
- Scripts: `Verify-ResourceGraph.ps1`, `Verify-ResourceKeys.ps1`, `Verify-ConsumerFixtures.ps1`,
  `Verify-GallerySmoke.ps1`
- `SEMVER.md`, `docs/architecture/foundation-resource-contract.md`

### L1 — Foundation resource contract

- Public merge graph: consumer → Controls `DesignSystem.xaml` → Controls `Generic.xaml` →
  Foundation `Foundation.xaml` → five frozen token dictionaries
- Transitive fonts/SVG via Foundation `buildTransitive` targets
- Documented unpackaged `StorageFile` / host-root `ms-appx` limitation

### L2 — EtherProgressBar exemplar

- `DefaultStyleKey`, keyed `DefaultEtherProgressBarStyle` + implicit `BasedOn`
- Template parts + LabelStates via VSM; component Light/Dark/HC keys (no template hex)
- Private read-only `IRangeValueProvider` peer; consumer evidence uses `GetPattern`
- `scripts/Verify-EtherProgressBarContract.ps1`
- Doc: `docs/architecture/2026-08-26-l2-ether-progressbar-exemplar.md`

### L3 — Control migration (complete)

Each control: templated contract (or stock style dictionary), component theme keys,
Gallery automation names, consumer runtime marker, contract script, CI wire, architecture note.

| Control / style | Notes |
| --- | --- |
| EtherButton | DefaultStyleKey; RightIconStates; preview `RightIconVisibility` removed |
| EtherCheckbox / EtherRadioButton | Empty subclasses → full contract |
| EtherInput | TextBox-based; Light/Dark proof has documented placeholders |
| EtherDropdown | ComboBox workarounds preserved; closed-tree Popup parts evidence limited |
| EtherSegmentedControl / EtherIntelligenceButton | Plan A parallel Wave1 + Wave2 integration |
| EtherSteeringBar | Complex peer + PreviewStatus kept; VSM for labels |
| EtherSlider | **UserControl → RangeBase**; 63-bar render remains code-driven |
| EtherMasthead | **UserControl → Control**, packable; AppWindow via XamlRoot; Close=`Destroy()` |
| EtherSwitch | **Keyed** stock ToggleSwitch style (not implicit) |
| EtherScrollBar | **Implicit** stock ScrollBar style (vertical + horizontal) |

Contract scripts live under `scripts/Verify-Ether*Contract.ps1` (13 control/style scripts
including ProgressBar). Runtime markers in
`tests/.../RuntimeVerification.cs` cover progressBar, button, checkbox, radioButton,
input, dropdown, segmentedControl, intelligenceButton, steeringBar, slider, masthead,
toggleSwitch, scrollBar.

### L4 — Gallery ControlExample (first pass + deepen)

**Shipped:**

- Gallery-only `src/Views/ControlExample.xaml` — Example / Output / Source / Copy,
  720px responsive stack
- Options remain `ComponentPage.InteractiveControls`
- **14 control pages** wrap ControlExample: Button, ProgressBar, Checkbox, RadioButton,
  Input, Dropdown, SegmentedControl, IntelligenceButton, SteeringBar, Slider,
  ToggleSwitch, ScrollBar, Masthead, Card
- Foundations primitives (Colors, Typography, Spacing, Radius, Icons) stay plain
  `ComponentPage`
- `scripts/Verify-GalleryControlExample.ps1` + CI
- Doc: `docs/architecture/2026-08-26-l4-gallery-control-example.md`
- Gallery smoke catalog still **20 pages** Light + Dark

**Explicitly NOT shipped (L4-C, still red):**

- Pixel screenshot baselines
- Appium / UIA snapshot suite
- Accessibility Insights automation
- 225% text scaling / RTL / localization gates
- arm64 package consumer fixture
- MSIX install/runtime proof
- Performance budgets / material fallbacks
- Multi-input matrix beyond current structured markers
- **First preview NuGet publish** (blocked until declared preview gates are green)

---

## 4. Architecture map (where to look)

```text
Ether.DesignSystem.slnx
├─ src/Ether.DesignSystem.Foundation/     # tokens + Foundation.xaml + buildTransitive
├─ src/Ether.DesignSystem.Controls/       # Generic.xaml / DesignSystem.xaml + PublicAPI
├─ src/Controls/**                        # control sources (linked into Controls)
├─ src/Views/**                           # Gallery pages (linked into Gallery)
├─ samples/Ether.DesignSystem.Gallery/
├─ tests/Ether.DesignSystem.ConsumerFixtures/
├─ scripts/Verify-*.ps1
└─ docs/architecture/2026-08-26-*.md
```

Resource merge (conceptual):

`App / consumer` → `DesignSystem.xaml` → `Generic.xaml` → `Foundation.xaml` → frozen tokens

---

## 5. Independent verification matrix (reviewer should re-run)

From repo root on Windows x64 (PowerShell; use `;` not `&&`):

```powershell
# Builds
dotnet build Ether.DesignSystem.slnx -c Debug -p:Platform=x64
dotnet build Ether.DesignSystem.slnx -c Release -p:Platform=x64

# Contracts (all Verify-Ether*Contract.ps1 + Gallery ControlExample)
Get-ChildItem .\scripts\Verify-Ether*Contract.ps1 | ForEach-Object { & $_.FullName }
.\scripts\Verify-GalleryControlExample.ps1

# Resources
.\scripts\Verify-ResourceGraph.ps1
.\scripts\Verify-ResourceKeys.ps1
# Expected fail ONLY for 145 frozen HC gaps:
.\scripts\Verify-ResourceKeys.ps1 -RequireHighContrastParity

# Runtime
.\scripts\Verify-ConsumerFixtures.ps1 -SkipSolutionBuild
.\scripts\Verify-GallerySmoke.ps1

# Frozen tokens
git diff -- src/Resources/Tokens
git hash-object src/Resources/Tokens/EtherPrimitives.xaml
git hash-object src/Resources/Tokens/EtherColors.xaml
git hash-object src/Resources/Tokens/EtherSpacing.xaml
git hash-object src/Resources/Tokens/EtherTypography.xaml
git hash-object src/Resources/Tokens/EtherIconGeometries.xaml
```

Expect: builds 0/0; contracts green; HC parity gate red with **exactly** Missing:145;
consumer markers present for all L3 controls; Gallery smoke Light 20/20 and Dark 20/20.

---

## 6. Known concerns for reviewers (not silent bugs)

Classify carefully — many are **documented exceptions**, not accidental regressions:

1. **Structured evidence ≠ pixel / UIA-out-of-process proof**
2. **High Contrast** for components is mostly static `SystemColor*` contracts
3. **EtherSlider** bars/knob still generated in code (`RenderBars`)
4. **EtherDropdown** closed Popup visual tree / GoToState evidence limits
5. **EtherMasthead** Close uses `AppWindow.Destroy()` instead of sandbox `Window.Close`
6. **EtherSwitch** must remain **keyed**; **ScrollBar** remains **implicit**
7. **ControlExample** Copy is clipboard-only (no runtime fixture for Copy click);
   SourceXaml snippets are representative, not always byte-identical to live trees
8. **Preview PublicAPI** — unshipped baselines; preview breaks (e.g. Button
   `RightIconVisibility` removal) are intentional within preview policy
9. Consumer fixture unpackaged StorageFile host-root limitation still recorded

---

## 7. Suggested review checklist for an independent agent

1. Confirm freeze: token hashes + empty `git diff -- src/Resources/Tokens`
2. Confirm project boundaries: Foundation has no controls; Controls has no Gallery refs;
   fixtures restore packages from local feed
3. Spot-check ProgressBar (L2 exemplar) and one complex control (Dropdown or SteeringBar)
4. Spot-check Slider + Masthead UserControl conversions
5. Spot-check Switch keyed vs ScrollBar implicit
6. Spot-check ControlExample on Button + one rolled-out page (e.g. Checkbox)
7. Re-run verification matrix; treat unexpected HC failures / fixture failures as bugs
8. Confirm L4-C items are **not** falsely claimed complete in docs/HANDOFF
9. Do not require pixel/Appium/publish for “L4 deepen complete” — those remain future work

---

## 8. Out of scope / next work after this handoff

- Opening L4-C gates and preview package publish
- Expanding ControlExample to Foundations primitive pages (intentionally skipped)
- Token HC parity fill-in (frozen; separate product decision)
- Framework / Windows App SDK upgrades

---

## 9. Related short handoffs

- Operational pause notes: `HANDOFF.md` (may lag; prefer this file for whole-work review)
- Plan: `docs/architecture/2026-08-26-repository-foundation-refactor-plan.md`
- Per-slice notes: `docs/architecture/2026-08-26-l2-*.md`, `*-l3-*.md`, `*-l4-*.md`
