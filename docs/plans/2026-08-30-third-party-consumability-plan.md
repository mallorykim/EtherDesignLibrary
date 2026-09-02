# Third-Party (Internal Team) Consumability Verification and Release Plan

Date: 2026-08-30
Branch: `codex/refine-components`
Scope: `Ether.DesignSystem.Foundation` / `Ether.DesignSystem.Controls` / `Ether.DesignSystem.Interactions` (`0.1.0-preview.1`, TFM `net8.0-windows10.0.19041.0`)
Audience: maintainers of this library. This document is a plan only and contains no code changes.

> **Execution progress update (2026-08-30, subsequent commits)**: After this document was written, A1 (two-tier consumption verification, see
> `scripts/Verify-ExternalConsumer.ps1`), A2 (removing `InternalsVisibleTo` privilege), A3 (26
> golden-baseline control PNGs), and part of A4 (Gallery markup coverage cross-check) have landed (commits
> `e4260f6`, `0832287`, `d649d75`, `61b7bcc`); B1 (honest property-verification wording), described later in this
> section, B4 (narrowing the platform declaration to x64), and B5 (wiring `Verify-MsixPackage.ps1` into hosted CI) were completed in
> this round (this revision, made after commit `af909c93`). **The numbers appearing in the body of this document —
> `1501 / 482 / 37 / 946`, "13 audited types", "about 1019 callable-only" — are all
> verification results against the state of the code as of when the 2026-08-30 plan was written; `EtherSegmentedTrack` was
> subsequently removed (see `af909c93`), and the current authoritative numbers are `1388 / visual 445 / semantic 69 / platform 874`, "12
> audited types", "943 callable-only" (`1388 − 445`); the body text is not corrected line by line, in order to preserve the
> verification record as it stood when the plan was written. The completion status of the remaining sub-items — B2 (Interactions PublicApiAnalyzers), B3 (making the release gate a formal process), B6 (consumer
> onboarding documentation, already covered by `docs/consumers/getting-started.md`) — was not individually re-verified
> in this update and still reflects the body text as originally written.**
>
> **Execution progress update (2026-08-30, third-round re-check)**: this round independently verified, item by item, B2/B3/B6 — marked
> "not re-verified" in the previous update — as well as A5, whose status the body text did not give, all strictly against
> reading the source code/scripts/CI configuration, not taking the self-reported status at hand-off at face value:
> - **A5 (RootNamespace decision)**: completed, decision = rename to align with the package name. `Ether.DesignSystem.Controls.csproj`'s `RootNamespace` is now `Ether.DesignSystem.Controls` (commit `e4260f6`, "align namespaces with package ids and prove external consumption"); `docs/consumers/getting-started.md` explicitly tells consumers not to use the old `EtherSandbox.Controls`.
> - **B2 (wire Interactions into PublicApiAnalyzers)**: confirmed complete on the main line — `PublicAPI.Shipped.txt`/`PublicAPI.Unshipped.txt`, the `Microsoft.CodeAnalysis.PublicApiAnalyzers` reference, and `WarningsAsErrors=RS0016;RS0017` are all in place. However, the "while you're at it, also add the missing `RepositoryUrl`/`RepositoryType`/`EnablePackageValidation`" mentioned in this item's "how to do it" had previously been missed (found by cross-checking property by property against the Controls/Foundation csproj files) — this round has added these three properties to `Ether.DesignSystem.Interactions.csproj`.
> - **B3 (make the release gate a formal process)**: confirmed as indeed not yet done — there was previously no release-entry script at all under `scripts/`. This round adds `scripts/Publish-Internal.ps1`: it chains all the static gates + `Verify-ConsumerFixtures.ps1` (two rounds) + `Verify-ExternalConsumer.ps1` + `Verify-MsixPackage.ps1`, validates that the evidence timestamp is newer than this run and that the version number has not already been used by the local feed / known release manifest, defaults to verify + pack only without pushing, and only actually runs `dotnet nuget push` with `-Push` (printing a confirmation before pushing); the GitHub Packages owner/feed are made required parameters, erroring out and explaining what's needed if missing.
> - **B4 (narrow the platform declaration to x64)**: the original judgment of "complete" holds, but one place where the declaration and reality were still misaligned was found — `Directory.Build.props` had earlier been narrowed to `Platforms=x64`, but the solution-platform mappings in `Ether.DesignSystem.slnx` were not cleaned up at the same time, leaving 11 leftover `x86` + 11 leftover `arm64` mappings (two unbuildable options would show up in the VS platform selector). This round has cleaned up `Ether.DesignSystem.slnx` to keep only `x64`, and verified with `dotnet build -p:Platform=x64` (Debug and Release) that the build still succeeds after the slnx change.
> - **B5 (wire dual-form MSIX verification into CI)**: verified as complete — `.github/workflows/build.yml` already has a "Verify unsigned MSIX package produce" step, running `Verify-MsixPackage.ps1 -SkipSolutionBuild`, consistent with the plan's approach of "put the build/structure-assertion part into hosted CI, keep the GUI install/run part in local RuntimeGates."
> - **B6 (consumer onboarding documentation)**: after checking the plan's checklist item by item, `docs/consumers/getting-started.md` covers GitHub Packages onboarding, the minimal csproj, xmlns, troubleshooting, known limitations, and versioning discipline, but had two gaps, both now filled in this round: (a) the troubleshooting table was missing the "restore resolves to a same-named public package" row (after `<clear/>` there are still two sources coexisting — `github-ether` and `nuget.org` — with a silent-wrong-resolution risk when one of them errors) — a new row has been added with a package source mapping recommendation; (b) "one markup example per control matching the Gallery page" previously covered only 3 controls (Button/ProgressBar/Input), but `samples/Ether.DesignSystem.Gallery/Views` actually has 14 control pages — a new "5. Per-Control Markup Reference" subsection has been added covering all 14 controls, with markup drawn from the already-verified `*Proof` elements in `tests/Ether.DesignSystem.ConsumerFixtures/Unpackaged/MainWindow.xaml` (rather than newly written), consistent with the verification approach of A1/A4. As for the checklist's automated spot-check script for "every code block in the README must have an equivalent in the fixture/Gallery," the plan's original text itself says this is "may" (optional) rather than mandatory, so no script was added this round — it still relies on manual review.
> - **B1 status confirmation (outside this round's verification scope, but the source was read in passing to check the accuracy of the intro note)**: `TryInvokeOnUnattachedInstance` in `RuntimeVerification.FullPropertyCode.cs:85-86` still "reads the current value → writes it back unchanged" (not yet upgraded to writing a perturbed value + read-back verification); the wording in `docs/releases/0.1.0-preview.1.md:58` and `src/Ether.DesignSystem.Controls/README.md` already uses honest phrasing ("445 of 1,388 ... the remaining 943 are verified only as callable"). That is, B1's "wording-first" part is indeed complete, while the "upgrade the setter to a round-trip" part is indeed not done — consistent with the previous update's wording of "B1 (honest property-verification wording) complete" (which did not claim the round-trip was complete), so this is not a status that needs correcting.
> - **C5 (hygiene item, optional) verification**: (a) the repo-root `EtherComponentSandbox.csproj`/`EtherComponentSandbox.slnx` were verified this round to be dead code (`git ls-files` shows them tracked, but no script/CI/other csproj in the repo references them outside of this plan and other historical documents; the last meaningful change was commit `9b4d492`, and they have gone untouched for the 43 commits since) and have been deleted (`git rm`); `.vs/` is already on line 9 of `.gitignore`. (b) `Directory.Build.props.example` is still a deprecated file that self-describes as "no longer required" — **not deleted** this round, since it is outside this task's explicit authorization scope, left to the maintainer's discretion. (c) the "proprietary/internal-use-only" declaration file still has not been created.

## 0. Goals and Confirmed Premises

### 0.1 Goal

Prove that the three NuGet packages can be correctly consumed by a **consumer project with no relationship whatsoever to this repository**, satisfying three hard requirements:

1. The consumption method matches Microsoft's official approach exactly (aligned with the templated-control / NuGet consumption path of the WinUI 3 Gallery and Microsoft Learn, with no private shortcuts);
2. After a third party references the packages, all UI remains fixed (backed by an executable regression gate, not a verbal promise);
3. Every public property / API works correctly through the official means (XAML markup + C#).

Operational definition of "no cheating": **the consumer project used for verification must not have anything a real consumer couldn't get** — no `InternalsVisibleTo` privilege, no inheriting this repo's MSBuild configuration, no dependence on implicit version injection from within the repo; verification assertions must not be loosened to the point of meaninglessness just to pass.

### 0.2 Confirmed Premises (decided by the user; the plan is finalized on this basis)

1. **Internal company use only, distributed via GitHub Packages (a private repository), not published publicly.** "Third party" = a sibling team inside the company: they don't have this repo's build configuration, can't reach internal members, write markup in their own XAML, and expect the UI to stay unchanged after upgrading the package. GitHub Packages' NuGet registry **requires the consumer to authenticate with a token to restore (even within the same organization)** — the sibling team needs a PAT (`read:packages`) and a `nuget.config` before onboarding; this is the first hurdle of onboarding and must be included in the verification and documentation scope. Everything related to public releases (nuget.org validation, license expressions, NuGet package signing) does not apply and has been removed from the plan; the "proprietary/internal-use-only" declaration is downgraded to an optional hygiene item to guard against accidental public exposure (§4.C5).
2. **Consumer machines are x64 only.** Yet `Directory.Build.props` declares `Platforms=x86;x64;arm64` and `RuntimeIdentifiers=win-x86;win-x64;win-arm64`, while CI only runs `-p:Platform=x64` — ARM64 has never been built in CI. This amounts to promising a support scope that has never been verified and must be tightened (§4.B4, narrowing the declaration is recommended).
3. **Both packaged (MSIX) and unpackaged consumption forms must continue to be verified.** Reason: **`.pri` resource loading behaves differently under packaged versus unpackaged**, and a library working fine under one form while resources go missing under the other is one of the most classic real-world failure modes for WinUI 3 libraries (backed by the official issues in §2.2). The repo already has two fixtures (`Packaged/` includes `Package.appxmanifest`, and `Verify-MsixPackage.ps1` builds a real MSIX), but that script is not wired into CI (addressed in §4.B5). **MSIX signing is out of scope for this library** — signing is the sibling team's concern when they publish their own app; this library is always just a NuGet package, and the plan does not schedule any signing work for it.

---

## 1. Current-State Verification

All of the following facts were verified by directly reading the source code on this branch (2026-08-30).

### 1.1 Existing Real Evidence (assets worth keeping)

`scripts/Verify-ConsumerFixtures.ps1` (1,116 lines) already accomplishes:

- Really `dotnet pack`s the three nupkgs → writes them into a local feed (`artifacts/consumer-fixtures/local-feed`);
- Two separate consumer projects (`tests/Ether.DesignSystem.ConsumerFixtures/{Unpackaged,Packaged}`) restore via `NuGet.Config` (`<clear/>` + local feed + nuget.org); the script asserts the csproj has **no** `ProjectReference` (`Assert-NoProjectReference`), and asserts Foundation is only reachable as a transitive dependency (`Assert-FoundationFlowsTransitively`);
- Package-structure assertions: `lib/net8.0-windows10.0.19041/*.dll` + `.pri`, `Themes/*.xbf`, Foundation's `contentFiles` fonts/SVGs and `buildTransitive/*.targets`, Controls has **no** `buildTransitive` leakage, the nuspec declares the Foundation dependency;
- Launches a real WinUI host (an Unpackaged exe), verifying the getters/setters of 1,501 public writable properties, per-item change+layout+`RenderTargetBitmap` evidence for 482 visual properties, SetValue/GetValue/callbacks/JSON envelopes for 37 Ether-owned DPs, 12 interaction adapters, resource keys, fonts, SVGs, RTL, UIA, 2.25x text scaling, localization, OS high-contrast mode, and a 13-control × Light/Dark screenshot matrix;
- Frozen hashes of the token source files (`EtherPrimitives/EtherColors/EtherSpacing/EtherTypography/EtherIconGeometries.xaml`).

This skeleton is correct: **building a real package, restoring it independently, and running it in a real host** is exactly the official consumption path. The problem is that the gaps below make it "look like a third party, but not actually be one."

### 1.2 Gap Inventory (verified item by item, ordered by priority under the new premises)

| Priority | Original # | Gap | Verification Result |
|---|---|------|---------|
| 1 | Gap 2 | The fixture inherits the repo's build configuration | **True — the wall the sibling team is most likely to hit on day one of onboarding, the highest-value item to fix**. The fixture sits inside the repo tree and automatically inherits the root `Directory.Build.props` (TFM, `Platforms`, `UseWinUI`, etc. all come from here — the fixture csproj doesn't even write a `TargetFramework`) and `Directory.Packages.props` (CPM, `Microsoft.WindowsAppSDK` 2.3.1 fed in by the central version; the fixture's `<PackageReference Include="Microsoft.WindowsAppSDK" />` has no version number, and the Ether package references use CPM-semantics `VersionOverride`). **Consequence: the dependencies our nupkg itself declares have never been checked in a clean environment**; the sibling team doesn't have these files, so at restore time what gets resolved is the dependency the package declares — if that declaration is missing or wrong, they fail to restore on day one, and the gate would never catch it. |
| 2 | Gap 1 | The fixture has `InternalsVisibleTo` privilege | **True**. `src/Ether.DesignSystem.Controls/Ether.DesignSystem.Controls.csproj:33-34` grants internals visibility to the two fixtures; `tests/.../RuntimeVerification.Masthead.cs:140,171,186` actually calls the `internal` method `EtherMasthead.RefreshCaptionIconColorForCurrentState` (`src/.../EtherMasthead.xaml.cs:471`). Acceptance can go all-green even when the public surface is insufficient, while the sibling team simply can't call it. |
| 3 | Gap 3 | Zero guarantee that "UI stays fixed" | **True**. `Assert-ScreenshotMarker` only asserts: file exists, >128 bytes, dimensions >0, light≠dark hash. There is no golden baseline at all — changing every control's appearance would still pass. UI drift after a package upgrade is the sibling team's most direct pain point. Known obstacle: screenshots have cross-run rendering noise (`dropdown-light.png` was observed to alternate between 2 hashes), so baselines can't use byte comparison. The repo already has a ready-made tolerance algorithm: `ChannelToleranceLevels = 1` + `SignificantPixelFraction = 0.0002` in `RuntimeVerification.AttachedVisualProperties.cs:200,207`. |
| 4 | Gap 5 | Insufficient XAML markup consumption coverage | **True in broad strokes, but the original wording needs correction** (see 1.3). What the sibling team actually writes is `<ether:EtherButton Variant="Tertiary"/>`. |
| 5 | Gap 4 | "1501 properties usable" is overstated | **True**. `RuntimeVerification.FullPropertyCode.cs:86-87`: on a newly created detached instance, after `GetValue` it **writes the same current value back unchanged** (`property.SetValue(detached, value)`). This only proves the setter doesn't throw, not that the value takes effect. Honest phrasing: 482 have observable-effect evidence, the remaining ~1019 only prove callability. |
| 6 | Gap 6 | Interactions has no API-stability protection | **True**. `src/Ether.DesignSystem.Interactions/Ether.DesignSystem.Interactions.csproj` has no `Microsoft.CodeAnalysis.PublicApiAnalyzers` and no `RS0016;RS0017` WarningsAsErrors, so the public surface can silently drift. **Additional finding**: it's also missing `RepositoryUrl`, `RepositoryType`, and `EnablePackageValidation` (which Controls and Foundation have). |
| 7 | Gap 8 | Incomplete CI coverage | **True, needs elaboration** (see 1.3). Contains two sub-items: no enforcement mechanism for the runtime gate (8a), the platform declaration being out of sync with CI + `Verify-MsixPackage.ps1` not wired into CI (8b). |
| Low | Gap 7 | Incomplete package metadata | **Factually true** (no `PackageLicenseExpression`/`PackageLicenseFile`/`PackageIcon`, no LICENSE at the repo root), **but under internal-only use this is not a blocker**: there's no external-licensing legal issue, and nuget.org validation doesn't apply. Downgraded to an optional hygiene item (§4.C5): add a "proprietary/internal-use-only" declaration, to guard against future accidental public exposure. |

### 1.3 Two Corrections / Elaborations to the Handed-Off Facts

1. **Gap 5's original wording is inaccurate**. The claim "the only property set in markup is `Style=`" doesn't hold: `tests/.../Unpackaged/MainWindow.xaml` actually sets multiple Ether DPs and platform DPs in markup — `EtherProgressBar`'s `Title`/`ValueContent`/`Value`, the same three on `EtherSteeringBar`, `EtherMasthead`'s `EnableWindowCommands="False"`, `EtherInput.PlaceholderText`, `EtherDropdown.SelectedIndex`, `ToggleSwitch.IsOn`, and so on. **The real shape of the gap** is: the markup coverage only reaches string / object / double / bool / int properties that happen to be set along the way, while **enum-typed DPs (`Variant="Tertiary"`, `Size="Small"`), collection-typed DPs (`Stops`, `Labels`), and the vast majority of Ether DPs have never gone through the XAML markup path**. Setting a DP from XAML goes through the XamlMetadataProvider's type resolution/value conversion — a completely different code path from the C# setter — and enums and collections are exactly the types most prone to breaking. The gap is real; its scope is narrower than stated.
2. **Gap 8 needs elaboration**. `Verify-Arm64Packages.ps1`, `Verify-MsixPackage.ps1`, and `Verify-GallerySmoke.ps1` are not entirely uncalled — they're already chained together by `scripts/Verify-RuntimeGates.ps1` (which positions itself as the "local/self-hosted desktop owner" and notes that hosted CI must not call it). The real gap is: (a) **there is no mechanism guaranteeing this local gate is actually run before release** — it's not in CI, not enforced at any release-process checkpoint, and relies entirely on self-discipline; (b) the CI build matrix only covers x64 — the other two platforms `Platforms` declares aren't even compiled in CI (ARM64 has never been built in CI), and `Verify-MsixPackage.ps1` isn't wired into CI.

### 1.4 New Findings During Planning (outside the original list)

- **N1 (important, irreversible after release)**: `Ether.DesignSystem.Controls`'s `RootNamespace` is `Ether.DesignSystem.Controls` (csproj:4), so consumer XAML must write `xmlns:ether="using:Ether.DesignSystem.Controls"` — inconsistent with the package name `Ether.DesignSystem.Controls`, and exposing the internal codename "Sandbox". Once the sibling team has onboarded the package, renaming the namespace becomes a breaking change — **before release is the only zero-cost window for a rename** (decision in §4.A5).
- **N2**: The repo root still has leftover `EtherComponentSandbox.csproj` / `EtherComponentSandbox.slnx` / `app.manifest` / root-level `Assets/`, `Fonts/`, and other files from the sandbox era. They don't go into the package, but they blur "what the product actually is" (§4.C5).
- **N3**: `Directory.Build.props.example` already self-declares as deprecated and can be deleted (§4.C5).

---

## 2. Official Baseline: How Microsoft Does It

Every item's approach in the plan is checked against the following official sources, to avoid inventing a non-standard path. Internal distribution changes nothing about the consumption mechanism — GitHub Packages is just another source in `nuget.config` (one extra step of PAT authentication); the restore/build/run path is exactly the same as for a public package.

### 2.1 Templated-Control Authoring Conventions (Microsoft Learn)

The official conventions established by [Build XAML templated controls (WinUI 3)](https://learn.microsoft.com/en-us/windows/apps/winui/winui3/xaml-templated-controls-winui-3):

- The constructor sets `DefaultStyleKey = typeof(X)`;
- The default style must live in `Themes/Generic.xaml` — **the folder name and file name are a hard framework requirement**;
- DP declaration pattern: `public static readonly DependencyProperty XProperty = DependencyProperty.Register(nameof(X), typeof(T), typeof(Owner), new PropertyMetadata(default, OnXChanged))` + a CLR wrapper going through `GetValue`/`SetValue`;
- Template interaction fetches the template part via `OnApplyTemplate`; the official consumption example is precisely **setting properties in XAML markup** (`<local:BgLabelControl Background="Red" Label="Hello, World!"/>`).

The repo's existing `scripts/Verify-WinUiConventions.ps1` already checks these kinds of conventions in CI; the direction is correct — keep it and continue tightening it.

### 2.2 Consuming a WinUI 3 Library via NuGet (Microsoft Learn + microsoft-ui-xaml issues)

- The [C# component NuGet consumption walkthrough](https://learn.microsoft.com/en-us/windows/apps/develop/platform/csharp-winrt/create-winrt-component-winui-cswinrt) makes clear the official path by which a C# WinUI component is consumed by another App SDK application, and acknowledges known limitations of the NuGet-reference form;
- [microsoft-ui-xaml #7830](https://github.com/microsoft/microsoft-ui-xaml/issues/7830) and [#10970](https://github.com/microsoft/microsoft-ui-xaml/issues/10970) document known official pitfalls of packaging a WinUI 3 control library as NuGet (`Generic.xbf` failing to be located, `ms-appx:///{Assembly}/Themes/Generic.xaml` failing to resolve, etc.), and **`.pri` resource resolution behaving differently under packaged versus unpackaged** is a recurring theme among them. This establishes two things: end-to-end consumption verification must be kept and strengthened (a successful build doesn't imply "it runs"); both the packaged and unpackaged forms must be verified (the basis for §0.2 premise 3).
- This library's current package layout (`lib/{tfm}/{AssemblyName}/Themes/*.xbf` + `.pri` at the same level as the dll) matches the Windows App SDK itself and the CommunityToolkit.WinUI family of packages, an approach already validated by the ecosystem — **this is not cheating**; Foundation using `contentFiles` + `buildTransitive` targets to land fonts/SVGs in the host's output is also an official NuGet asset-propagation mechanism.

### 2.3 WinUI 3 Gallery's Sample Organization (microsoft/WinUI-Gallery)

The pattern of the [WinUI Gallery repository](https://github.com/microsoft/WinUI-Gallery): one sample page per control, with the page hosting **the live control plus the XAML markup and code-behind that generate it** via `ControlExample`, and the control catalog driven by `ControlInfoData.json` metadata. Implications for this library:

- `samples/Ether.DesignSystem.Gallery` is already organized one page per control (`Views/Controls/*Page.xaml`), gated in CI by `Verify-GalleryControlExample.ps1`/`Verify-GalleryLocalization.ps1`, and the direction matches the Gallery;
- The markup the Gallery shows is "the officially recommended way to write it as a consumer." Therefore **every markup pattern that appears in a Gallery page must work identically for a pure-NuGet consumer** — this is the source material for the §4.A4 XAML matrix.

### 2.4 MSBuild Configuration Boundaries (Microsoft Learn)

[Customize your build (the Directory.Build.props lookup rule)](https://learn.microsoft.com/en-us/visualstudio/msbuild/customize-by-directory): MSBuild searches upward from the project directory and stops at the **first** `Directory.Build.props` it finds; if none is found, there is none. Physically placing the consumer project outside the repo tree guarantees zero inheritance by construction — this is the official mechanism underlying §4.A1's first tier, not a hack.

---

## 3. Plan Overview

Three priority tiers. Each item states: what it must prove / how to do it / success criteria / why it conforms to official convention.

```
A. Release Blockers (must not push to the internal feed until all done)
  A1  Two-tier real consumption verification: out-of-repo consumer (CI tier) + GitHub Packages dress-rehearsal tier   [Gap 2, top priority]
  A2  Remove InternalsVisibleTo, de-privilege the fixture                        [Gap 1]
  A3  UI golden-baseline gate (tolerance comparison)                             [Gap 3]
  A4  XAML markup consumption matrix (official markup for every public DP)       [Gap 5]
  A5  RootNamespace decision (Ether.DesignSystem.Controls → ?)                   [N1, needs maintainer decision]

B. Required Before Release (complete before tagging)
  B1  Honest property verification + upgrade setter to round-trip                [Gap 4]
  B2  Wire Interactions into PublicApiAnalyzers                                  [Gap 6]
  B3  Make the release gate a formal process: RuntimeGates + GitHub Packages
      dress rehearsal become release conditions                                 [Gap 8a]
  B4  Tighten the platform declaration: align Platforms with CI reality
      (x64 only)                                                                [Gap 8b]
  B5  Wire dual-form MSIX verification into CI (Verify-MsixPackage.ps1)          [Gap 8b]
  B6  Consumer onboarding documentation (GitHub Packages authentication +
      usage consistent with the Gallery)

C. Post-Release / Optional
  C1  OS version matrix (real 17763-floor testing)
  C2  API reference documentation site
  C3  Appium/UIA real-input interaction automation
  C4  WindowsAppSDK version-compatibility matrix
  C5  Hygiene items: internal-proprietary declaration, Interactions metadata alignment, repo leftover cleanup
  C6  Self-hosted Windows runner (automate the local runtime gate)
```

---

## 4. Item-by-Item Plan

### A1 — Two-Tier Real Consumption Verification (Release blocker, top priority, the single highest-value item in the whole plan)

- **What it must prove**: (Tier 1) the dependencies **our nupkg itself declares** are sufficient for a project with zero relationship to the repo to restore/build/run — without the repo's CPM secretly feeding in versions, without `Directory.Build.props` secretly feeding in the TFM; (Tier 2) the sibling team can pull the package from GitHub Packages **just by following the documentation** and get it running, including clearing the authentication hurdle.
- **How to do it**:

  **Tier 1 (in CI, runs every time, no network/secret dependency)**:
  1. **Move the consumer project out of the repo tree**: during verification, `Verify-ConsumerFixtures.ps1` **copies** the fixture project files and the shared `RuntimeVerification*.cs` into a workspace outside the repo tree (e.g. `$env:TEMP\ether-consumer-<guid>\`), and restores/builds/runs there. Being outside the repo tree means, per the MSBuild lookup rule (§2.4), non-inheritance of `Directory.Build.props`/`Directory.Packages.props` is **guaranteed by construction**; the workspace also gets an empty sentinel `Directory.Build.props`/`Directory.Packages.props` (`<Project/>` + `ManagePackageVersionsCentrally=false`) as a belt-and-braces measure (to guard against the extreme case of a same-named file existing further up the drive root). The source files remain version-controlled in the repo — copying is just the execution form — this is isomorphic to "a third party gets the source template and builds their own project," and does not constitute a privilege.
  2. **Write the fixture csproj to look like a real consumer**: modeled on the official Windows App SDK templates (Blank App, Packaged / Unpackaged) — explicit `TargetFramework=net8.0-windows10.0.19041.0`, `TargetPlatformMinVersion`, `Platforms`, `UseWinUI` (except where the template lets the SDK set these implicitly), **every `PackageReference` carries an explicit `Version`** (`Microsoft.WindowsAppSDK` pinned to a fixed version; the Ether package references switch the CPM-semantics `VersionOverride` to a plain `Version`).
  3. **Restore source**: the workspace carries its own `NuGet.Config` — `<clear/>` + a local folder feed (the pack output) + nuget.org (upstream dependencies).
  4. **Anti-inheritance assertion**: the script runs `dotnet msbuild -getProperty:ManagePackageVersionsCentrally,TargetFramework,Platforms` against the workspace project, asserting CPM=false and that the TFM comes from the project itself; and asserts every `PackageReference` in the csproj has an explicit `Version`.
  5. **One-off mutation sensitivity check** (recorded in this item's acceptance record, not in CI): temporarily mark the Controls csproj's WindowsAppSDK reference `PrivateAssets="all"` (making the nuspec omit that dependency), rerun the gate, and confirm Tier-1 restore/build **fails**; then restore it and confirm it passes. The conclusion is written into the script's comments — proving the gate genuinely checks the nuspec dependency declaration.

  **Tier 2 (release dress rehearsal, manual or periodic, one of B3's release conditions)**:
  6. Actually push the three packages to the GitHub Packages private feed (a pre-release version number or a dedicated rehearsal feed);
  7. On a clean machine (or at minimum a clean user environment / containerized SDK environment), operate strictly according to the onboarding documentation in §4.B6: configure a PAT (`read:packages`) + `nuget.config` → `dotnet restore` → build → run the fixture assertions, all green;
  8. Archive the rehearsal results (restore logs, marker JSON, machine environment) into `artifacts/release-evidence/{version}/`.
- **Success criteria**: Tier-1 mutation test goes red→green; in the workspace's `project.assets.json`, the WindowsAppSDK version source = the project's explicit declaration ∩ the package's dependency range, not CPM; all existing runtime assertions still pass under the new form; Tier 2 has been walked through once in a clean environment using only the documentation, with evidence kept.
- **Why this is official**: what a consumer gets is exactly the VS template + `dotnet add package` + feed authentication; Tier 1 aligns the fixture precisely to that form, and Tier 2 aligns the feed and authentication too. `Directory.Build.props`'s stop-at-nearest behavior is documented MSBuild behavior (§2.4).
- **Effort**: Tier 1, 2–3 days (including getting both fixtures working and the mutation verification); Tier 2, 1 day (depends on the GitHub Packages feed being ready).
- **Dependencies**: none. Do this first — it changes the fixture's form, and A3/A4 should both land on the new form.

### A2 — Remove `InternalsVisibleTo`, De-Privilege the Fixture (Release blocker)

- **What it must prove**: the verification consumer has exactly the same visibility as the sibling team — only the public API is visible. As long as `InternalsVisibleTo` exists, any "third party can use it" conclusion carries an asterisk: acceptance can go all-green even when the public surface is insufficient.
- **How to do it**:
  1. Delete the two `InternalsVisibleTo` lines at `src/Ether.DesignSystem.Controls/Ether.DesignSystem.Controls.csproj:33-34`;
  2. Handle the sole internal consumption point, `RuntimeVerification.Masthead.cs` (which calls `RefreshCaptionIconColorForCurrentState` to assert the caption icon follows the theme). Choose one of the following, (a) recommended:
     (a) Rewrite it as a **publicly observable path**: switch `RootGrid.RequestedTheme`, wait for the compositor to settle (reusing AttachedVisualProperties' stable-sampling method), and do a `RenderTargetBitmap` pixel assertion on the caption-button area (the icon color hitting the expected value under Light/Dark respectively) — this tests behavior a consumer can actually see;
     (b) if the pixel assertion is genuinely unstable in this scenario, move this test into the repo's internal unit/integration test project (which can legitimately hold internals), and downgrade the fixture to a public-behavior assertion. **Not allowed**: "keeping IVT with the intent to remove it later."
  3. Add a static assertion to `Verify-ConsumerFixtures.ps1`: grep `src/**/*.csproj` for `InternalsVisibleTo`, and fail if any target points to `*ConsumerFixtures*` (guards against regression).
- **Success criteria**: zero `InternalsVisibleTo` under `src` pointing at the fixture; the fixture compiles fully with no references to internal members; the Masthead theme assertion is still present and still goes red when the `ActualThemeChanged` logic is broken (a temporary mutation check of sensitivity).
- **Why this is official**: Microsoft's own library tests (WinUI, Toolkit) distinguish "white-box unit tests (IVT allowed)" from "end-to-end sample/package verification (pure public API)"; consumer evidence must belong to the latter category.
- **Effort**: 0.5–1.5 days.
- **Dependencies**: recommended after A1 Tier 1 (so it lands on the new fixture form).

### A3 — UI Golden-Baseline Gate (Release blocker)

- **What it must prove**: hard requirement 2 — the UI stays fixed after the sibling team upgrades the package. Currently the only constraint on appearance is "light≠dark," which is effectively no constraint at all.
- **How to do it**:
  1. **Baseline assets**: commit the current (design-approved) per-control `RenderTargetBitmap` PNGs for 13 controls × {Light, Dark} into `tests/Ether.DesignSystem.ConsumerFixtures/Baselines/`, along with a `baseline-manifest.json` (each image's pixel dimensions, the generating machine's OS build, display scale, generation date, source commit). Full-page screenshots have many noise sources (window chrome, etc.), so **the baseline uses only per-control captures**; full-page screenshots keep their existing weak assertion.
  2. **Comparison algorithm**: don't use byte/hash comparison (cross-run rendering noise has been observed: `dropdown-light.png` alternates between 2 hashes). Reuse the algorithm already validated by the 482-property audit in this repo: a per-pixel, per-channel (4 channels) difference greater than `ChannelToleranceLevels(=1)` counts as a changed pixel; the comparison fails once the number of changed pixels exceeds `max(MinimumSignificantPixelCount, total pixel count × SignificantPixelFraction(=0.0002))`. **Dimensions must match pixel-for-pixel** (a dimension change is not subject to tolerance). The comparison is implemented in the fixture's managed code (same process, same pixel format as the screenshot); the script only checks the conclusion, avoiding a second PNG decoder's discrepancies.
  3. **Failure output**: dump a three-piece set (baseline / actual / diff heatmap PNG) to `artifacts/` for human judgment.
  4. **Baseline update process**: only the explicit `-UpdateVisualBaselines` switch allows overwriting; baseline PNGs are version-controlled by git, and updates show up as a binary diff in the PR + before/after comparison images attached to the PR description. On the default path, baselines are read-only.
  5. **Environment pinned**: the baseline gate runs in the same class of environment used to generate the baseline (local/self-hosted desktop, following the same-machine policy as RuntimeGates); the manifest records the environment and it's validated for a match before comparing (the scale factor must be 100%; a mismatch fails immediately with a message, rather than producing incomparable images).
  6. **Belt and braces (source-side freeze)**: following the existing token hash freeze, add `Themes/Generic.xaml`, `Themes/DesignSystem.xaml`, and each control's style XAML to a "changes require explicit confirmation" hash checklist (a changed hash must come with either an updated baseline or an explanation of why there's no visual change). Source freezing catches "the source changed"; the pixel baseline catches "the rendered result changed" — the two complement each other.
- **Success criteria**: mutation verification — temporarily change a control's fill-color brush, and the gate must go red; after restoring, 3 consecutive full runs must be green (the tolerance absorbs known noise without swallowing real changes).
- **Why this is official**: visual-regression baselines plus tolerance comparison are common practice across WinUI/Win2D/Toolkit ecosystem test infrastructure; capture uses the public API `RenderTargetBitmap` and happens inside the consumer's own host process, introducing no private rendering path.
- **Effort**: 2–3 days.
- **Dependencies**: A1 Tier 1 and A2 must come first (baseline generation should happen on the de-privileged fixture).

### A4 — XAML Markup Consumption Matrix (Release blocker)

- **What it must prove**: that hard requirement 3 holds under **official consumption syntax**: every public Ether DP can be set via an XAML literal markup and take effect (enum, bool, double, string, collection), because that is exactly the form the Gallery shows and the sibling team actually writes (`<ether:EtherButton Variant="Tertiary"/>`).
- **How to do it**:
  1. Add a "MarkupMatrix" section to the XAML of both fixtures (which must be instantiated within the visual tree), placing at least one instance for **all 37 Ether DPs** with a markup literal set to a non-default value, focusing on: enums (`Variant="Tertiary"`, `Size="Small"`), bools (`ShowLabels="False"`, etc.), double/int, string, and **collection-type DPs using property-element syntax** (`<controls:EtherSlider.Stops>…`); then sample a number of inherited platform DPs (ones with type converters, like `CornerRadius`, `Padding`).
  2. Runtime assertions: for every instance in the matrix, read back the DP value and check it == the value declared in markup, and that the control's `defaultStyleResolved`, layout, and render succeed (reusing existing infrastructure). Results are folded into the runtime marker JSON, with a count assertion added on the script side (matrix entry count == 37 + the sample count, to prevent silent shrinkage).
  3. Source-material cross-check: every markup pattern that appears in `samples/Ether.DesignSystem.Gallery/Views/Controls/*Page.xaml` must have an equivalent entry in the matrix (the script compares the Gallery page's attribute set against the matrix's coverage set, folded into `Verify-GalleryControlExample.ps1` or a new assertion).
- **Success criteria**: the matrix count assertion passes; mutation verification — temporarily break some enum DP's registration (change the type / remove an enum member), and XAML loading must fail or the assertion must go red.
- **Why this is official**: Learn's templated-control tutorial and every Gallery example treat XAML markup consumption as the primary path (§2.1/§2.3); testing only the C# setter precisely bypasses the official main path.
- **Effort**: 2–3 days.
- **Dependencies**: A1 Tier 1 (the new fixture form); the A5 decision (write it after the xmlns is finalized, to avoid rework).

### A5 — RootNamespace Decision (Release blocker (the decision itself); the change depends on the decision)

- **Problem**: consumer XAML must write `xmlns:ether="using:Ether.DesignSystem.Controls"`, which doesn't match the package name and exposes the internal codename "Sandbox." Changing it after the sibling team has onboarded the package = a breaking change, requiring coordination with every consumer at that point — **before release is the only zero-cost window for a rename**.
- **How to do it**: the maintainer decides whether to rename or not (a product-naming decision, which this plan does not make on their behalf). If renaming: change the `RootNamespace`/namespace → refresh across the whole repo (Gallery, fixtures, script assertions, a full column swap in `PublicAPI.Shipped/Unshipped.txt`, 13 L3 contract scripts). If keeping it: explicitly spell out the xmlns syntax in the README and package description, so consumers don't have to guess.
- **Effort**: keeping it, 0.1 days; renaming, 1–2 days (mechanical but wide-reaching).

### B1 — Honest Property Verification + Setter Round-Trip (Required before release)

- **What it must prove**: that internal claims match the strength of the evidence; upgrading "callable only" to "the write actually takes effect."
- **How to do it**:
  1. **Wording first** (zero risk): uniformly change the `Verify-ConsumerFixtures.ps1` success output, the package README, and the release notes to: "482 visual properties have per-item observable-effect evidence (pixel/layout/visibility/contract); an additional 1,019 public writable properties have had their getter/setter verified as callable without throwing." Remove every claim at the level of "1,501 properties work normally."
  2. **Setter upgrade**: upgrade `TryInvokeOnUnattachedInstance` in `RuntimeVerification.FullPropertyCode.cs` from "write the original value back" to **write a non-current value and read it back**: generate a perturbed value by type (negate a bool, +1 on a numeric value (respecting min/max/NaN semantics), append a suffix to a string, advance an enum to its next member, `new` one up for a constructible reference type), then after `SetValue`, assert `GetValue` reads back == what was written (coercing properties get their own whitelist recording the expected value after coercion). Properties that can't be written to / read back go into an explicit classification list (platform read-only mirrors, lifecycle-bound properties, etc.); the list's count feeds into the marker assertion, and silent skipping is disallowed.
- **Success criteria**: the identity round-trip count + classification count == 1,501 holds; 0 differences across two consecutive runs (maintaining the existing determinism standard); zero overstatement remaining in the wording.
- **Why this is official**: the semantics of the DP contract (§2.1) is precisely that get returns the value set (or the coerced value); a round-trip is a direct test of the official contract.
- **Effort**: 1.5–2 days (the long tail is the coercion whitelist).

### B2 — Wire Interactions into PublicApiAnalyzers (Required before release)

- **How to do it**: add `Microsoft.CodeAnalysis.PublicApiAnalyzers` (version via CPM), `PublicAPI.Shipped.txt`/`PublicAPI.Unshipped.txt` (auto-generate the baseline with the analyzer on the first pass), and `WarningsAsErrors=RS0016;RS0017` to `Ether.DesignSystem.Interactions.csproj`, fully aligned with Controls/Foundation; while at it, add its missing `RepositoryUrl`/`RepositoryType`/`EnablePackageValidation` (internal consumption also benefits from SourceLink traceability and package validation, at the cost of a single line each).
- **Success criteria**: under a Release build, deliberately adding a public method without updating the txt files → build fails; passes after fixing it.
- **Why this is official**: consistent with the practice of the other two packages in this repo and with the .NET team's own API-baseline practice.
- **Effort**: 0.5 days.

### B3 — Make the Release Gate a Formal Process (Required before release)

- **What it must prove**: that the full runtime acceptance (`Verify-RuntimeGates.ps1` = full ConsumerFixtures + GallerySmoke + MSIX + platform pack) and the A1 Tier-2 dress rehearsal **always** run before every release to GitHub Packages, rather than relying on self-discipline. It's a fact that a hosted runner can't spin up a real WinUI GUI (the workflow's comment stands), so the approach is process enforcement rather than forcing this onto hosted CI.
- **How to do it** (in increasing order of cost — do 1 first, 2 is recommended, 3 is the end state, see C6):
  1. **Enforced via a release script**: add `scripts/Publish-Internal.ps1` as the single release entry point: first run `Verify-RuntimeGates.ps1` in full, write the marker JSON + baseline-comparison conclusion + run environment into `artifacts/release-evidence/{version}/`, and only run `dotnet nuget push` to GitHub Packages (`https://nuget.pkg.github.com/{org}/index.json`, with the PAT via an environment variable/credential manager, never written to disk) once validation passes. Immediately after pushing, run the A1 Tier-2 dress rehearsal and fold its evidence into the same directory. Document the rule: any push not going through this script is considered an invalid release.
  2. **Leave a trace**: releases go through a tag-triggered workflow, with one job that validates and uploads the evidence directory as a build artifact (version number, marker outcome==success, timestamp within 48h before the tag), serving as the release admission check.
- **Success criteria**: Publish-Internal refuses to push when the evidence is missing or marker outcome ≠ success; if the rehearsal fails, the release is voided and rolled back (GitHub Packages supports deleting a version).
- **Effort**: 1 day (not including the initial feed/PAT setup).

### B4 — Tighten the Platform Declaration: x64 Only (Required before release)

- **Problem**: `Directory.Build.props` declares `Platforms=x86;x64;arm64` and `RuntimeIdentifiers=win-x86;win-x64;win-arm64`, but CI only builds x64 — ARM64 has never been built in CI — promising a support scope that has never been verified. Consumers have confirmed they are x64 only.
- **How to do it** (the former is recommended):
  - **Option 1 (recommended): narrow the declaration to match reality**. `Platforms=x64`, `RuntimeIdentifiers=win-x64`; disable `Verify-Arm64Packages.ps1` (remove it from the `Verify-RuntimeGates.ps1` chain; the script can stay under `scripts/` with a header note "not enabled — restore only if an ARM64 consumer ever appears"); the README explicitly states "currently x64 only." If ARM64 demand genuinely appears in future, restoring the declaration must come together with restoring verification — declaration and evidence must move in lockstep.
  - Option 2: keep the declaration, but explicitly label x86/arm64 as "unverified, unsupported" in the README and package description, and add at least compile-level coverage for these two platforms in CI. Downside: continues paying maintenance cost for platforms nobody uses, and "declared but unsupported" is still a trap for consumers.
- **Success criteria**: every member of the declared `Platforms` has CI-level build evidence (automatically satisfied under Option 1); the documentation, csproj, and CI are all consistent.
- **Effort**: 0.5 days.

### B5 — Wire Dual-Form MSIX Verification into CI (Required before release)

- **What it must prove**: that the library loads correctly under both the packaged (MSIX) and unpackaged host forms — **`.pri` resource resolution takes a different path under each form** (§0.2 premise 3, §2.2), and one form working while resources go missing under the other is a classic real WinUI 3 library failure.
- **How to do it**:
  1. Wire `Verify-MsixPackage.ps1` (which builds the Packaged fixture, producing a real MSIX and asserting on it) **into the hosted CI**'s `package-consumers` job — building the MSIX and asserting on its package structure need no GUI, so a hosted runner can run them; if the script contains GUI steps like install/launch, split off a `-ProduceOnly` switch and keep the GUI part in the local `Verify-RuntimeGates.ps1`;
  2. Make the Packaged fixture's **runtime** verification (install the MSIX → launch → run the marker assertions) part of the local RuntimeGates (currently Packaged only builds and doesn't run — upgrade it to actually run; A1 Tier 1's workspace form applies to it equally);
  3. Clarify the boundary: this library does not do MSIX signing — signing belongs to the sibling team's own app-release process; local verification just needs a test certificate or the `Add-AppxPackage -Register` sideload path, explained in the README.
- **Success criteria**: CI produces and asserts on the MSIX structure every time; in the local gate, the packaged fixture is actually installed and run with the marker fully green; mutation verification — temporarily break `.pri` packaging (e.g. remove `.pri` from under lib), and at least one of packaged or unpackaged must go red.
- **Why this is official**: the packaged/unpackaged duality is exactly the two deployment forms officially supported by the Windows App SDK; verifying both is direct coverage of the official support matrix.
- **Effort**: 1–1.5 days (actually running Packaged is the main cost).

### B6 — Consumer Onboarding Documentation (Required before release)

- **How to do it**: rewrite the three packages' READMEs (already `PackageReadmeFile`) plus the repo's `docs/consuming.md` from the sibling team's perspective; must include:
  1. **GitHub Packages onboarding**: a `nuget.config` example (adding `https://nuget.pkg.github.com/{org}/index.json` under `<packageSources>` + `<packageSourceCredentials>` referencing the PAT via an environment variable, with an explicit warning not to commit the PAT in plain text); the PAT's permission scope (`read:packages`, needing SSO authorization if the org has SSO enabled); a **common-failure-symptom and troubleshooting table**: 401 (PAT missing/expired/SSO not authorized), 404 (wrong org segment in the feed URL, misspelled package name), `Unable to load the service index` (proxy/network), restore resolving to a same-named public package (source-mapping recommendation);
  2. install commands, a minimal csproj (mirroring A1 Tier 1's consumer form, with an explicit WindowsAppSDK version), the `xmlns` declaration (per the A5 decision), and the exact syntax for merging `DesignSystem.xaml` in App.xaml;
  3. one markup example per control consistent with the Gallery page; an explanation of the packaged/unpackaged differences (including a one-line explainer of the `.pri` behavior difference); "currently x64 only" (B4);
  4. every code block in the README must have an equivalent in the fixture/Gallery (to guard against documentation rot; may be spot-checked by a script).
- **Success criteria**: the A1 Tier-2 dress rehearsal is walked through in a clean environment **using only this documentation** (the documentation is itself the test case); a colleague unfamiliar with this repo builds a project from scratch following the documentation and gets the same button shown on the Gallery's home page running, doing one real-human verification and recording it.
- **Effort**: 1 day.

### C — Post-Release / Optional

| # | Item | Description |
|---|------|------|
| C1 | OS matrix | `TargetPlatformMinVersion=10.0.17763.0` has never been verified on a real 17763 machine; run one round against consumers' actual OS baseline. |
| C2 | API reference site | Generated (via DocFX etc.) from the XML docs produced by `GenerateDocumentationFile`, published to an internal site. |
| C3 | Real-input interaction automation | Current interaction verification is at the in-process adapter level; add Appium/WinAppDriver-level real keyboard/mouse/touch paths (the release notes already acknowledge "Insights/Appium not yet done"). |
| C4 | WASDK compatibility matrix | Restore the consumer project against a stable WindowsAppSDK higher than 2.3.1, to verify the package's declared version range is compatible with consumers upgrading WASDK. |
| C5 | Hygiene items (optional) | (a) add a "proprietary/internal-use-only" declaration file at the repo root and note it in the csproj — to guard against future accidental public exposure, not required; (b) clean up leftover `EtherComponentSandbox.*`, root-level `Assets/`/`Fonts/`, and the already-deprecated `Directory.Build.props.example`. |
| C6 | Self-hosted Windows runner | Move `Verify-RuntimeGates.ps1` (including running Packaged for real, baseline comparison) onto a self-hosted interactive runner, completely removing the manual step from B3. |

---

## 5. Dependency Order and Effort Summary

```
A1 Tier 1 (2–3d) ──┬─→ A2 (0.5–1.5d) ─→ A3 (2–3d) ─→ B1 (1.5–2d) ─→ B3 (1d, incl. A1 Tier-2 rehearsal 1d)
                    └─(A5 decision, maintainer)→ A4 (2–3d) ─→ B6 (1d)
B2 (0.5d, anytime)   B4 (0.5d, anytime)   B5 (1–1.5d, after A1 Tier 1)
```

- Critical path: A1 Tier 1 → A2 → A3 → B1 → B3 (including the rehearsal), about 8–10.5 person-days;
- All of A+B: about **13–17 person-days** (roughly 3 weeks for one person), not counting the wait for the A5 decision or the C-tier items;
- Earliest possible release point: A-tier fully green + B-tier fully green + `Publish-Internal.ps1` has completed one real GitHub Packages push with evidence + the Tier-2 rehearsal fully green.

---

## 6. References

- Microsoft Learn — [Build XAML templated controls (WinUI 3)](https://learn.microsoft.com/en-us/windows/apps/winui/winui3/xaml-templated-controls-winui-3) (DefaultStyleKey, the hard Themes/Generic.xaml naming requirement, the DP declaration pattern, OnApplyTemplate, the markup-consumption example)
- Microsoft Learn — [Create a C# component with WinUI controls…](https://learn.microsoft.com/en-us/windows/apps/develop/platform/csharp-winrt/create-winrt-component-winui-cswinrt) (the official component NuGet consumption path and known limitations)
- microsoft/WinUI-Gallery — [repository](https://github.com/microsoft/WinUI-Gallery) (one sample page per control, ControlExample, ControlInfoData.json metadata-driven, pages showing markup + code-behind)
- microsoft/microsoft-ui-xaml — [#7830](https://github.com/microsoft/microsoft-ui-xaml/issues/7830), [#10970](https://github.com/microsoft/microsoft-ui-xaml/issues/10970) (known issues with packaging a WinUI 3 control library as NuGet: `Generic.xbf`/`.pri` resolving differently under packaged versus unpackaged, backing up the need for end-to-end dual-form consumption verification)
- Microsoft Learn — [Customize your build (the Directory.Build.props lookup rule)](https://learn.microsoft.com/en-us/visualstudio/msbuild/customize-by-directory) (stop-at-nearest, the official mechanism behind A1 Tier 1's "outside the repo tree, zero inheritance guaranteed by construction")

---

## 7. Release Versioning Discipline

**Any content change must bump the version number — never reuse a version number that has already been packed.** NuGet caches by package ID plus version number; reusing a version number causes consumers as well as verification scripts to silently receive the old package. This happened once locally: after a namespace rename, the package was rebuilt but the version number wasn't changed, the global cache hit the old package, and external-consumer verification failed to compile across the board — while `restore` still reported success.
