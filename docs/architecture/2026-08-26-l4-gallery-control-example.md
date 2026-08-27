# L4 Gallery ControlExample

Date: 2026-08-26  
Status: preview Gallery chrome, not stable-release readiness

This pass evolves the Gallery toward the WinUI Gallery `ControlExample` pattern
without claiming preview-package publish readiness. Frozen Primitive/Semantic
tokens were not changed.

First pass (`4bba0a9`) piloted Button + Progress Bar. The deepen pass rolls
`ControlExample` across remaining **control** Gallery pages. Foundations
primitive pages stay on plain `ComponentPage`.

## What shipped

- Gallery-only `ControlExample` (`src/Views/ControlExample.xaml`) with:
  - **Example** slot (content property) for the live specimen
  - **Output** slot (`OutputText`) for live bound values
  - **Source** panel for the specimen XAML snippet
  - **Copy** button (`Clipboard.SetContent`) with a short "Copied" confirmation
  - **Responsive layout:** `WideLayout` places SOURCE beside the specimen when
    this control is at least 720px wide; `NarrowLayout` stacks SOURCE below
- **Options** remain `ComponentPage.InteractiveControls`, right of the
  INTERACTIVE header (including the existing Disabled checkbox). That matches
  WinUI Gallery's options column without breaking pages that already use the
  header slot.
- `ComponentPage` disables only `ControlExample.IsExampleEnabled` when the
  page uses `ControlExample`, so Disabled does not gray out SOURCE/Copy.
- `scripts/Verify-GalleryControlExample.ps1` static contract, wired in CI.
- Gallery smoke page catalog is unchanged (still 20 pages, Light + Dark).

## Migrated pages

These wrap `ComponentPage.InteractiveContent` in `ControlExample` with a
`SpecimenXaml` snippet. `StatesContent` stays outside the example. Live
`OutputText` is used where the page has a meaningful interaction:

Folders follow Gallery information architecture, not a single `Controls/` dump
(`ProgressBarPage` in DataDisplay, `ScrollBarPage` in Foundations as a control
story, `MastheadPage` in Navigation, `CardPage` in Surfaces).

| Page | Location | Live output |
| --- | --- | --- |
| `ButtonPage` | Controls | Last click |
| `ProgressBarPage` | DataDisplay | Progress value |
| `CheckboxPage` | Controls | Checked |
| `RadioButtonPage` | Controls | Selected option |
| `InputPage` | Controls | Typed text |
| `DropdownPage` | Controls | Selected item |
| `SegmentedControlPage` | Controls | Selected segment |
| `IntelligenceButtonPage` | Controls | Last click |
| `SteeringBarPage` | Controls | Value |
| `SliderPage` | Controls | Value |
| `ToggleSwitchPage` | Controls | On / Off |
| `ScrollBarPage` | Foundations (control story) | None (scroll host) |
| `MastheadPage` | Navigation | None (caption preview) |
| `CardPage` | Surfaces | None (type previews) |

Left on plain `ComponentPage` (Foundations primitives + Home):

- `ColorsPage`, `TypographyPage`, `SpacingPage`, `RadiusPage`, `IconsPage`
- `HomePage`

## Responsive layout

The breakpoint is **control width 720px**, not window width. Gallery's
NavigationView pane would make `AdaptiveTrigger MinWindowWidth` fire too early.
At typical desktop content widths the source sits to the right of the specimen.
Below 720px, or when `SourceXaml` is empty, SOURCE stacks under the specimen.
There is no documented hard minimum window width beyond "the stacked layout is
the fallback."

## What this is not

This is not a pixel-faithful clone of WinUI Gallery `ControlExample` (no C#
source expander, no rich-text highlighter, no per-example Options column inside
the control). Options stay on the page header by design for this pass.

## L4-C in-repo substitutes vs still-red gates

In-repo package-consumer quality is green. Insights, Appium, hosted GUI CI,
MSIX install, arm64 runtime, and preview publish are not.

**In-repo (package consumer):**

| Gate | Status |
| --- | --- |
| RTL inheritance + automation names | Green. See `docs/architecture/2026-08-26-l4c-rtl-package-consumer.md`. |
| In-process UIA snapshot (13 named controls) | Green substitute. Not Appium / out-of-process UIA. EtherDropdown bounding rect is explicit `LayoutFallback` (collapsed ContentPresenter); the other 12 require UIA rects. |
| 225% `ScaleTransform` + `.resw` / `x:Uid` | Green substitute. Not OS text-scale or Gallery localization. |
| Light/Dark `RenderTargetBitmap` captures | Green generate-under-artifacts. Not High Contrast, not golden-image CI. |
| Elapsed verification budget `< 20000ms` | Green harness. Not scroll/animation budgets. |
| Unsigned MSIX produce | Green produce. Install/runtime stays red. |
| arm64 pack + fixture compile | Green compile. arm64 runtime stays red. |

**Still red:**

| Gate | Why |
| --- | --- |
| Appium / out-of-process UIA | No Appium host. |
| Accessibility Insights automation | Insights engine not in CI. |
| Hosted CI WinUI smoke | Still `Verify-RuntimeGates.ps1` local-only; `build.yml` keeps `-SkipRuntimeSmoke`. |
| MSIX install/runtime | Unsigned produce only. |
| arm64 runtime smoke | Pack/build only. |
| High Contrast token parity | 145 keys missing. Frozen. |
| Preview package publish | User hold. `docs/releases/0.1.0-preview.1.md`. |

`RangeValuePatternIdentifiers.ValueProperty` in-process UIA subscription for
ProgressBar remains undocumented-as-blocked (no public WinUI API); that is
unchanged from L2.

## Evidence

`Verify-GalleryControlExample.ps1` checks the control surface, every migrated
control page wrapping `ControlExample` with `SpecimenXaml`, Foundations
primitive pages remaining on plain `ComponentPage`, the architecture note, and
CI wiring for the static contract. Runtime proof for pages is local
`scripts/Verify-RuntimeGates.ps1` (`Verify-GallerySmoke.ps1` Light/Dark 20/20,
unpackaged consumer markers including RightToLeft inheritance, unsigned MSIX
produce, and arm64 pack/compile), not hosted `build.yml`. Screenshots are
generated under artifacts. This is not Appium, Insights, MSIX install, arm64
runtime, or golden-image CI.
