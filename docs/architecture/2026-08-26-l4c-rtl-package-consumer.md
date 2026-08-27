# L4-C slice — package-consumer RTL + automation names

Date: 2026-08-26  
Status: in-repo quality gate, not preview publish

Unpackaged package consumers now run a second layout pass after the existing
Light/Dark control markers: `RootGrid.FlowDirection = RightToLeft`. Each of the
13 named package specimens must inherit `RightToLeft` and keep the same
`AutomationProperties.Name` the LTR pass already required.

This is not Gallery RTL, not OS text-scale, and not a pixel-mirroring audit
of Slider bars or Dropdown popups. Fixture `.resw` / `x:Uid` and 225%
`ScaleTransform` are separate in-repo substitutes (see
`docs/architecture/2026-08-26-l4c-in-repo-quality-gates.md`).

## Evidence

- `RuntimeVerification.VerifyRtlAsync` writes `rtl.rootFlowDirection` plus
  `rtl.controls[].{id,flowDirection,automationName}`.
- `Verify-ConsumerFixtures.ps1` rejects fixture XAML that drops those
  automation names, and rejects a runtime marker that is missing RTL
  inheritance or renamed peers.

## Still red

Insights, Appium, hosted GUI CI, MSIX install/runtime, arm64 runtime, and
preview package publish. Unsigned MSIX produce and arm64 pack/compile are
in-repo green; they are not install or arm64 runtime. High Contrast semantic
key parity is green (`SystemColor*`); on-device contrast themes are not.
