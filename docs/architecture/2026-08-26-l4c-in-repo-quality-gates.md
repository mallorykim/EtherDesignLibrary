# L4-C slice — in-repo package-consumer quality gates

Date: 2026-08-26  
Status: in-repo quality substitutes, not preview publish

Unpackaged package consumers now extend the existing `ETHER_CONSUMER_SMOKE`
marker with UIA, 225% scale, localization, Light/Dark screenshots, and an
elapsed-time budget. These are in-repo substitutes. They are not Accessibility
Insights, not Appium / WinAppDriver, not hosted GUI CI, and not nuget publish.

## Evidence

- `RuntimeVerification` writes marker fields `uia`, `textScale`,
  `localization`, `screenshots`, and `performance`.
- `scripts/Verify-ConsumerFixtures.ps1` asserts those fields on the unpackaged
  runtime-smoke path, plus a static `x:Uid="FixtureStatus"` check on both
  fixture windows.
- 225% is a `ScaleTransform` on `RootGrid`. It is not
  `UISettings.TextScaleFactor`.
- UIA is in-process `FrameworkElementAutomationPeer`. It is not out-of-process
  UIA.
- Screenshots are generated under `artifacts/` (smoke `workRoot/screenshots`).
  They are not golden-image diffs.

## Still red

MSIX install/runtime, arm64 runtime, Accessibility Insights, Appium, hosted CI
WinUI smoke, and preview package publish. Later tasks may green unsigned MSIX
produce and arm64 pack/build only.
