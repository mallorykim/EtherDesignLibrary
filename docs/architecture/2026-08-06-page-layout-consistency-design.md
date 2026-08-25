# Sandbox page layout consistency — design

Date: 2026-08-06
Status: approved

## Problem

Every leaf page under `Views/**` already renders through the shared `ComponentPage`
chrome (title/description → INTERACTIVE card → STATES card), so the page-level shell
is uniform. The visual "messiness" the user is reacting to lives one level down: each
page hand-rolls the typography, spacing, and radius values for its own content instead
of referencing the design tokens the project already defines in
`Resources/EtherTypography.xaml` (named `TextBlock` styles, e.g. `EtherMicroSemiBold`
= 11px SemiBold Inter) and `Resources/EtherSpacing.xaml` (4pt spacing/radius scale,
e.g. `PaddingMd` = 16, `RadiusControlSm` = 4). Because pages retype these values by
hand, the same conceptual role (e.g. a "PRIMARY / SECONDARY" sub-group caption above a
swatch grid) lands at different pixel sizes on different pages.

Before proposing fixes, `Resources/EtherDataGraphics.xaml` was checked: it documents
the exact Figma source node IDs for the chart components (tiered bar, single bar,
data blocks, line chart), and several values that looked like drift at first glance
(tiered-bar legend `CornerRadius="3"`, line-chart legend `CornerRadius="1"`,
`DataBlocksPage`'s `Padding="16,13,16,13"`) are verbatim copies of that canonical
spec — different shapes for different chart types, not inconsistency. Those are
excluded from this pass; changing them would break fidelity to the design source
rather than improve consistency.

## Scope

In scope — mechanical application of existing tokens/styles, no new abstractions,
no change to what any page displays or how any component behaves, except the two
structural fixes explicitly approved below:

1. **Caption typography.** Standardize every in-card sub-group caption (e.g.
   "PRIMARY", "UNCHECKED", "LARGE", "NORMAL CARD") to
   `Style="{StaticResource EtherMicroSemiBold}"` + `Foreground="{ThemeResource TextTertiary}"`
   (11px SemiBold Inter, no letter-spacing). This reserves the existing 10px +
   `CharacterSpacing="80"` style purely for `ComponentPage`'s own INTERACTIVE/STATES
   section headers, producing a clean 2-tier caption hierarchy instead of 4+
   competing sizes. Includes fixing `SegmentedControlPage.xaml`'s STATES sub-labels,
   which currently use `InstrumentSans` instead of `InterFont`.
2. **Secondary body text.** Standardize 12px-vs-13px interchangeable use for the same
   "secondary descriptive line" role (e.g. `CardPage`'s callout description bodies)
   to `EtherBodySRegular` (12px), matching the sizing already used elsewhere on the
   same page for the same role.
3. **Generic container padding.** Where a page's padding is an ad hoc number and not
   a copied chart-spec snippet from `EtherDataGraphics.xaml`, replace it with the
   matching `PaddingXs`/`Sm`/`Md`/`Lg` token (same visual value, now traceable to the
   scale).
4. **Small circular swatch/status dots.** Replace hardcoded `CornerRadius` values that
   already happen to equal an existing radius token (e.g. `CardPage`'s `Width=8
   Height=8 CornerRadius="4"`) with `{StaticResource RadiusControlSm}`. No visual
   change — same pixel value, now a token reference.
5. **Duplicated intelligence gradient.** `BarChartSinglePage.xaml` and
   `ColorsPage.xaml` both inline the same 4-stop gradient that
   `EtherDataGraphics.xaml` already exposes as `EtherBarThickGradient`. Point both at
   the shared resource instead of repeating the stops.
6. **Garbled description text.** ~10 files contain a literal `=` where an encoding
   issue mangled an intended ×/→/− character (e.g. `"2=2 stat grid"` →
   `"2×2 stat grid"`, `"96 px = Bold = −40 ls"` → `"96 px · Bold · −40 ls"`,
   `"Active pill: #1A1A1A = radius 13 = 85=70"` → corrected per original intent).
   Restore the intended characters.
7. **`LabelFilterChipPage` missing Disabled toggle** (approved). Add
   `HasDisabledToggle="True"` and wire an `IsEnabled` binding on the live specimen,
   matching the pattern already used in `CheckboxPage.xaml` /
   `RadioButtonPage.xaml`.
8. **`CardPage` callouts reimplement card chrome by hand** (approved). Swap the two
   hand-rolled `Border Background=... Padding="16" CornerRadius=...` callouts to
   reuse the `EtherCardNormal` style, matching how `AIRecommendationPage.xaml` /
   `ExpressChargePage.xaml` already wrap their specimens.

Explicitly out of scope: `Views/DataDisplay/*` and `Views/Navigation/*` internal
layout numbers, and anything inside the `EtherDataGraphics.xaml` copy-paste
snippets — these already match the documented Figma source 1:1.

## Non-goals

- No new shared XAML components, styles, or resource keys beyond what already exists
  (the one exception — none needed; `EtherMicroSemiBold`, the `Padding*`/`Radius*`
  tokens, and `EtherBarThickGradient` all already exist).
- No change to page content, component choice, or information architecture beyond
  items 7 and 8 above (both already approved).
- No change to the top-level nav order or category colors (handled in prior turns).

## File list (leaf pages touched)

Controls: `ButtonPage`, `CheckboxPage`, `DropdownPage`, `InputPage`,
`IntelligenceButtonPage`, `LabelFilterChipPage`, `RadioButtonPage`,
`SegmentedControlPage`, `SliderPage`, `ToggleSwitchPage`.

Foundations: `ColorsPage`, `TypographyPage` (also fixes its own internal 20px vs 16px
row-gap margin inconsistency between InteractiveContent and StatesContent — both
become the same token).

Surfaces: `AIRecommendationPage` (spot-check only, likely already clean),
`CardPage`, `ExpressChargePage` (spot-check only).

Data display: `BarChartSinglePage` (gradient reference only — layout otherwise
untouched).

Top-level: `AIDesignLanguagePage` (spot-check only).

Each file gets: caption style pass, padding/radius token pass where applicable,
garbled-character text fix where applicable. `LabelFilterChipPage` and `CardPage`
additionally get their structural fix (items 7/8).

## Verification

`dotnet build EtherComponentSandbox.csproj -c Debug` after each batch of files (the
build has been running in ~12s throughout this session). This is a Windows desktop
WinUI3 app with no browser or CLI-drivable UI surface available to this session, so
final visual confirmation of the running app is left to the user — the response will
call out exactly which pages changed for a quick pass.
