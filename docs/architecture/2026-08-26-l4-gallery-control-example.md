# L4 Gallery ControlExample (first pass)

Date: 2026-08-26  
Status: preview Gallery chrome, not stable-release readiness

This pass evolves the Gallery toward the WinUI Gallery `ControlExample` pattern
without converting every page and without claiming preview-package publish
readiness. Frozen Primitive/Semantic tokens were not changed.

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
- Pilot pages: `ButtonPage` and `ProgressBarPage` wrap their interactive
  specimens in `ControlExample` with real snippets and live output (last click
  / progress value).
- Other Gallery pages stay on the existing `ComponentPage` Interactive + States
  API. `CheckboxPage` is the contract canary for that backward compatibility.
- `ComponentPage` disables only `ControlExample.IsExampleEnabled` when the
  page uses `ControlExample`, so Disabled does not gray out SOURCE/Copy.
- `scripts/Verify-GalleryControlExample.ps1` static contract, wired in CI.
- Gallery smoke page catalog is unchanged (still 20 pages, Light + Dark).

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

## L4-C deferred (explicit)

These remain red preview gates and were **not** implemented:

| Gate | Why deferred |
| --- | --- |
| Pixel screenshot baselines | Needs a capture harness and Light/Dark/HighContrast image store. |
| Appium / UIA snapshot suite | No Appium host or snapshot corpus in-repo yet. |
| Accessibility Insights automation | Requires the Insights engine in CI. |
| 225% text scaling / RTL / localization | No scaling/flow/resource-language fixtures. |
| arm64 consumer fixture | Current fixtures and CI are x64. |
| MSIX install/runtime proof | Gallery and fixtures stay unpackaged; hosted GUI launch is skipped in CI. |
| Performance budgets | No startup/scroll/animation budget harness. |
| Preview package publish | Gate stays red until the rows above exist. |

`RangeValuePatternIdentifiers.ValueProperty` in-process UIA subscription for
ProgressBar remains undocumented-as-blocked (no public WinUI API); that is
unchanged from L2.

## Evidence

`Verify-GalleryControlExample.ps1` checks the control surface, Button/Progress
Bar pilots, CheckboxPage backward compatibility, the architecture note, and CI
wiring. Runtime proof for pages is still `Verify-GallerySmoke.ps1` (20/20 Light
and Dark). This is not a screenshot or Appium baseline.
