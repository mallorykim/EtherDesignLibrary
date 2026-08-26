# Foundation resource contract

`Ether.DesignSystem.Foundation` exposes one public XAML merge entry: `Themes/Foundation.xaml`.
It merges the five frozen token dictionaries in their established dependency order: primitives,
colors, spacing, typography, then icon geometries. Consumers do not merge these implementation
files directly.

`Ether.DesignSystem.Controls/Themes/Generic.xaml` merges Foundation once, before control
templates. Applications merge only `Ether.DesignSystem.Controls/Themes/DesignSystem.xaml` for
design-system resources (alongside the WinUI `XamlControlsResources` required by unpackaged
hosts). This preserves the framework resource ordering while keeping the Foundation-to-Controls
graph private behind the Controls entry point.

## Host-root assets

The frozen typography dictionaries deliberately use `ms-appx:///Fonts/...` URIs. Foundation
packages therefore include the existing `Fonts/` and `Assets/` folders and a narrowly scoped
`buildTransitive` target adds them to the consuming host's Content/PRI graph before resource
processing and copies them to the host output root. This is an asset and resource contract, not
an assembly-resolution workaround: it must not add `ReferencePath` entries or otherwise alter
library resolution. Packaged MSIX fixture builds retain the documented limitation
that its tooling rejects the existing comma-named font filenames, so it is build-only in L1.

The unpackaged WinUI fixture records every official `ms-appx:///` URI and attempts
`StorageFile.GetFileFromApplicationUriAsync` for each. On the currently supported unpackaged
host this API rejects host-root content with `ArgumentException` even after `%2C` escaping the
frozen comma filenames. The marker preserves that diagnosis rather than claiming success. It then
requires the corresponding non-empty host output files, the actual XAML `FontFamily` resources,
and a visible `SvgImageSource` loaded through the same `ms-appx` SVG URI. A packaged runtime
fixture remains deferred because the frozen comma-named fonts are not MSIX-tooling compatible.

## Elevation and accessibility boundary

L1 publishes no elevation token and does not invent visual values. Components that later need
elevation use WinUI's native `ThemeShadow` plus `Translation` at component scope, with fallback
behavior determined by the component's accessibility contract. A shadow must never be the only
High Contrast boundary signal; contrast-safe outline, fill, or other non-shadow separation is
required.

## Release gate

Frozen token files remain byte-for-byte unchanged in L1. `Verify-ResourceKeys.ps1` always fails
Light/Dark mismatches and duplicate keys. It reports the known High Contrast deficit during
preview; every stable release must run it with `-RequireHighContrastParity`, which fails until the
145 missing keys are addressed in an authorized token change.
