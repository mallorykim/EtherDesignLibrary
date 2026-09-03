# Ether Design System Foundation (Preview)

`Ether.DesignSystem.Foundation` is a preview WinUI 3 resource library containing Ether design tokens, fonts, and icon assets.

Its supported XAML entry point is `Themes/Foundation.xaml`; Controls consumes it and applications
consume Controls' `Themes/DesignSystem.xaml`. The repository's Foundation resource contract
documents merge ordering, host-root `Fonts`/`Assets` behavior, elevation boundaries, and the
MSIX font-name limitation.

This package is a preview, distributed internally only through a private GitHub Packages feed — not nuget.org. Preview notes: `docs/releases/0.1.0-preview.1.md`. Consumption guide: `design library handoff/getting-started.md`.

**Verified, with in-repo consumer-fixture runtime evidence** (NuGet `PackageReference` only, no `ProjectReference`, but the fixture shares this repository's build configuration): x64 unpackaged and packaged (MSIX build/produce, not install) hosts consuming Foundation as a transitive dependency of Controls; resource, font, and icon asset resolution in both host forms; Light/Dark theming and OS-selected High Contrast, including a dedicated High Contrast foreground/background pairing gate (`Verify-HighContrastPairing.ps1`).

**Verified, with a truly external check** (`scripts/Verify-ExternalConsumer.ps1`, an out-of-repo consumer project with none of this repository's `Directory.Build.props`/`Directory.Packages.props` inherited): x64 unpackaged restore, build, and basic runtime rendering through the public NuGet package surface only.

**Not yet verified:** MSIX installation and launch from an installed package (only unsigned MSIX build/produce is verified — no `Add-AppxPackage`); any architecture other than x64 (the only architecture this package currently declares or builds for); stable-release readiness (preview API can still change).

Foundation and Controls are released by this repository in lockstep.
