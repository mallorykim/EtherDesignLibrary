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

