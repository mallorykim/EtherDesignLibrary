# L3 EtherMasthead migration record

Date: 2026-08-26  
Status: preview implementation evidence, not stable-release readiness

`EtherMasthead` is the eighth L3 slice. It is converted from a `UserControl` to a
templated `Control` so default visuals live on `DefaultEtherMastheadStyle`, optional
icon visibility is a VisualStateManager contract, and window chrome remains on the
control. The control is **packable**: it is compiled into
`Ether.DesignSystem.Controls`, merged from `Themes/Generic.xaml`, and resolves
its host `AppWindow` from `XamlRoot.ContentIslandEnvironment.AppWindowId` instead
of sandbox `App.MainWindow`.

**Default visual:** `UseSystemFocusVisuals=False`, transparent background, stretch
alignment, settings icon shown, search/menu/chevron collapsed, caption buttons
always present (44×44 hit targets). Call sites that already set `ShowSettings`,
`PreviewIsMaximized`, or `EnableWindowCommands` are unchanged.

## Implemented contract

- The constructor sets `DefaultStyleKey`; `DefaultEtherMastheadStyle` owns the
  default setters and template, and the implicit style is `BasedOn` that keyed
  style. Optional-icon defaults remain on dependency-property metadata
  (`ShowSettings=true`, others false).
- Template parts: `MenuIconSlot`, `SearchIconSlot`, `SettingsButton`,
  `ChevronSlot`, `MinimizeButton`, `MaximizeRestoreButton`, `CloseButton`,
  `MaximizeIcon`, `RestoreIcon`.
- Visual states: `SettingsIconStates`, `SearchIconStates`, `MenuIconStates`,
  `ChevronStates`, and `WindowStates` (`WindowRestored` / `WindowMaximized`).
- Light, Dark, and HighContrast expose the same four `EtherMasthead*` component
  resources. Templates consume those component keys only for color-bearing
  ThemeResources. Light and Dark alias the previous product tokens
  (`ActionSecondaryBgHover`, `ActionSecondaryBgPressed`, `border/focus`,
  `text/primary`). High Contrast uses Windows `SystemColor*` dynamic resources.
- Window chrome is preserved. Minimize/maximize/restore call
  `OverlappedPresenter` on the host `AppWindow`. Close calls `AppWindow.Destroy()`
  rather than `App.MainWindow.Close()` so the control does not take a sandbox
  `App` dependency. Gallery specimens continue to set `EnableWindowCommands=False`
  and may force `PreviewIsMaximized`.
- Gallery `MastheadPage` keeps the default and maximized specimens and adds
  identifiable automation names.

## Packaging decision

Masthead is **packable**, not sandbox-only. The previous Controls csproj exclude
existed because `App.MainWindow` could not compile into the library. Host window
resolution through `XamlRoot` removes that blocker. Gallery no longer compiles a
private copy of the XAML/code-behind.

## Direct-mutation exceptions

Caption button `Click` handlers stay code-driven. Moving AppWindow presenter
calls onto VisualStateManager would not model window commands. Maximize/restore
**glyph** visibility is a `WindowStates` visual state; the automation name on
`MaximizeRestoreButton` still updates in code.

## Pinned-source parity

| Topic | WinUI title-bar guidance | EtherMasthead | Notes |
| --- | --- | --- | --- |
| Base type | Templated `Control` | `Control` | Not a UserControl after this slice. |
| Default style | `DefaultStyleKey` + generic implicit style | Same via `DefaultEtherMastheadStyle` | Matches L2/L3 exemplar. |
| Window chrome | AppWindow caption buttons | Same commands, custom 44px glyphs | Product visual, not OS caption buttons. |
| Host coupling | Window from XamlRoot / AppWindowId | `ContentIslandEnvironment.AppWindowId` | Packable; no `App.MainWindow`. |

A clone of the OS caption-button template is deferred: Ether's 44px DDS2-aligned
chrome is product design. Hosts still call `ExtendsContentIntoTitleBar` and
`SetTitleBar` themselves.

## Evidence and remaining exceptions

The unpackaged NuGet consumer fixture mounts default and interactive
`EtherMasthead` instances on a UI thread. Its `masthead` marker records keyed
default-style resolution (`ShowSettings` true, search/menu/chevron false,
`UseSystemFocusVisuals=False`, template), caption and optional-icon parts, one
optional-icon state (`SearchCollapsed` → `SearchVisible`), and Light/Dark icon
foreground rebind. `Verify-EtherMastheadContract.ps1` additionally verifies
metadata, keyed/implicit styles, theme-key symmetry, High Contrast system
resources, no literal template hex colors, gallery automation names, packable
AppWindow wiring, Controls inclusion, and CI wiring.

PublicAPI adds the templated `EtherMasthead` surface (`ShowSettings`,
`ShowSearch`, `ShowMenuIcon`, `ShowChevron`, `PreviewIsMaximized`,
`EnableWindowCommands`). UserControl generated members
(`InitializeComponent`, `Connect`, `GetBindingConnector`) are not part of the
Controls public API because Masthead was previously excluded from that project.
`OnApplyTemplate` is protected on a sealed type, so it is not a PublicAPI
surface.

This is structured runtime evidence, not a pixel screenshot baseline. The
High Contrast check is a static resource contract, not an on-device
verification across all Windows contrast themes. Formal Appium and
Accessibility Insights coverage remain L4 work. The repository's known
frozen global-token High Contrast deficit remains a release blocker; this
component does not alter those tokens or claim stable readiness.
