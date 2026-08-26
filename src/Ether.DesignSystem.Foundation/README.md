# Ether Design System Foundation (Preview)

`Ether.DesignSystem.Foundation` is a preview WinUI 3 resource library containing Ether design tokens, fonts, and icon assets.

Its supported XAML entry point is `Themes/Foundation.xaml`; Controls consumes it and applications
consume Controls' `Themes/DesignSystem.xaml`. The repository's Foundation resource contract
documents merge ordering, host-root `Fonts`/`Assets` behavior, elevation boundaries, and the
MSIX font-name limitation.

This package is not production-ready. x64 package consumers are verified for unpackaged runtime and packaged build. MSIX installation/runtime, other architectures, accessibility, localization, full High Contrast coverage, and stable-release readiness remain incomplete. Foundation and Controls are released by this repository in lockstep.
