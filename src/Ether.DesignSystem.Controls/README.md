# Ether Design System Controls (Preview)

`Ether.DesignSystem.Controls` is a preview WinUI 3 control library and the consumer resource entry point for Ether design-system resources.

This package is a preview, distributed internally only through a private GitHub Packages feed — not nuget.org. Preview notes: `docs/releases/0.1.0-preview.1.md`. Consumption guide: `docs/consumers/getting-started.md`.

**Verified, with in-repo consumer-fixture runtime evidence** (NuGet `PackageReference` only, no `ProjectReference`, but the fixture shares this repository's build configuration): x64 unpackaged and packaged (MSIX build/produce, not install) hosts; UI Automation (automation names, control types, bounding rects), RTL layout inheritance, 2.25x text scale without clipping, localization resource-loader wiring, and OS-selected High Contrast — including a dedicated High Contrast foreground/background pairing gate (`Verify-HighContrastPairing.ps1`); Light/Dark theming; 445 of 1,388 public writable properties have per-property observable evidence (pixel/layout/visibility/contract) from being mutated, laid out, and rendered on an attached control — the remaining 1,388 − 445 = 943 are verified only as callable (getter/setter invoked without throwing on a detached instance, which does not prove a consumer-supplied value visibly takes effect); golden-baseline screenshots for all 13 controls in Light and Dark (26 images).

**Verified, with a truly external check** (`scripts/Verify-ExternalConsumer.ps1`, an out-of-repo consumer project with none of this repository's `Directory.Build.props`/`Directory.Packages.props` inherited): x64 unpackaged restore, build, and basic runtime rendering through the public NuGet package surface only.

**Not yet verified:** MSIX installation and launch from an installed package (only unsigned MSIX build/produce is verified — no `Add-AppxPackage`); any architecture other than x64 (the only architecture this package currently declares or builds for); Fluent parity; stable-release readiness (preview API can still change).

Foundation and Controls are released by this repository in lockstep.
