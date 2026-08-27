# L4-C slice — in-repo package-consumer quality gates

Date: 2026-08-26  
Status: in-repo quality substitutes, not preview publish

Unpackaged package consumers now extend the existing `ETHER_CONSUMER_SMOKE`
marker with UIA, 225% scale, localization, Light/Dark/HighContrast screenshots, and an
elapsed-time budget. Local `scripts/Verify-RuntimeGates.ps1` also runs unsigned
MSIX produce and arm64 pack/compile. These are in-repo substitutes. They are
not Accessibility Insights, not Appium / WinAppDriver, not hosted GUI CI, not
MSIX install, not arm64 runtime, and not nuget publish.

## Evidence

- `RuntimeVerification` writes marker fields `uia`, `textScale`,
  `localization`, `screenshots`, and `performance`.
- `scripts/Verify-ConsumerFixtures.ps1` asserts those fields on the unpackaged
  runtime-smoke path, plus a static `x:Uid="FixtureStatus"` check on both
  fixture windows.
- 225% is a `ScaleTransform` on `RootGrid`. It is not
  `UISettings.TextScaleFactor`.
- UIA is in-process `FrameworkElementAutomationPeer`. It is not out-of-process
  UIA. EtherDropdown bounding rect is explicit `LayoutFallback` (collapsed
  ContentPresenter); the other 12 named controls require UIA rects.
- Screenshots are generated under `artifacts/` (smoke `workRoot/screenshots`).
  They are not golden-image diffs.
- `scripts/Verify-MsixPackage.ps1` produces an unsigned `.msix` / `.msixbundle`.
  It does not call `Add-AppxPackage`.
- `scripts/Verify-Arm64Packages.ps1` packs Foundation + Controls for arm64 and
  compiles the unpackaged fixture. It does not launch the arm64 exe.
- `EtherColors` HighContrast now defines the same semantic slash keys as Light/Dark
  (234). Values are `{ThemeResource SystemColor*}`, not Gray/Blue primitives.
- Unpackaged HighContrast runtime is OS-selected: the fixture turns Windows
  High Contrast on via `SPI_SETHIGHCONTRAST`, waits for
  `AccessibilitySettings.HighContrast`, captures `consumer-highcontrast.png`,
  then restores in `finally`. Marker fields: `highContrast.osHighContrast`
  true, `dictionaryForced` false. Canvas color is the live SystemColor window
  (not injected `#FF00FF00`). Dictionary overlay is not the claim. The scheme
  is whatever this machine provides (`hc1` / `hc2` / `hcblack` / `hcwhite` on
  this host). This is not Contrast Aquatic, Desert, or Night Sky, and not
  Accessibility Insights.

## Still red

MSIX install/runtime (unsigned produce only; no `Add-AppxPackage`), arm64
runtime, Accessibility Insights, Appium, hosted CI WinUI smoke
(`build.yml` keeps `-SkipRuntimeSmoke`), OS contrast themes (Aquatic /
Desert / Night Sky) unless those `.theme` files exist and were applied, and
preview package publish.
