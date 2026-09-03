# Component usage guides

One page per control: what it is, copy-paste markup, the **Ether-specific API** it adds, the
standard WinUI members you'll actually use, how to bind and wire actions, and what the design
system deliberately owns (so you don't fight it).

New here? Set up the package first — [getting-started.md](../getting-started.md) covers the feed
(§1), the minimal app + the resource dictionary you must merge (§2), and cross-cutting topics:
troubleshooting (§3), the full inherited-but-not-consumed boundary list (§4), MVVM binding
patterns (§6), the optional Interactions telemetry adapter (§7), and versioning (§8). Every page
below assumes you have declared `xmlns:ether="using:Ether.DesignSystem.Controls"` and merged
`Themes/DesignSystem.xaml`.

## How to read a page

- **Use it** — the exact markup shape the out-of-repo consumer test exercises; copy it.
- **Consumer API** — two tables: the members Ether *adds*, and the standard WinUI members that work
  normally. If a member isn't listed, assume the design system owns it (see the last section).
- **Bind & wire** — the proven MVVM/`x:Bind`/`Command` paths, with the fixture that proves each.
- **Design-system-owned** — appearance properties that are inert by design. Setting them is not an
  error; it just has no effect, which keeps every consuming app visually consistent.

## Controls

### Actions
- [EtherButton](EtherButton.md) — primary/secondary/tertiary button with optional icon slots.
- [EtherIntelligenceButton](EtherIntelligenceButton.md) — AI-affordance button with a default sparkles icon.

### Inputs & selection
- [EtherInput](EtherInput.md) — single-line text field (`TextBox` reskin).
- [EtherDropdown](EtherDropdown.md) — selection dropdown (`ComboBox` reskin).
- [EtherCheckbox](EtherCheckbox.md) — two-state checkbox.
- [EtherRadioButton](EtherRadioButton.md) — single-select radio.
- [EtherSwitch](EtherSwitch.md) — on/off toggle (`ToggleSwitch` style).
- [EtherSlider](EtherSlider.md) — value slider with optional stops, labels, and title.
- [EtherSegmentedControl](EtherSegmentedControl.md) — segmented single-select with a data-driven contract.
- [EtherPanelTabs](EtherPanelTabs.md) — panel-style tabs (a reskin of the segmented control).

### Data display
- [EtherProgressBar](EtherProgressBar.md) — progress with optional title/value label.
- [EtherSteeringBar](EtherSteeringBar.md) — interactive value bar with stops.
- [EtherScrollBar](EtherScrollBar.md) — styled scrollbar.

### Navigation & surfaces
- [EtherMasthead](EtherMasthead.md) — app title bar with window commands + affordances.
- [EtherTabNavigation](EtherTabNavigation.md) — top tab strip (`ListView` reskin) with `EtherTabItem`s.
- [EtherCard](EtherCard.md) — content surface (keyed `Border` style).
- [EtherTooltip](EtherTooltip.md) — static tooltip surface (keyed `Border` style).

## The two shapes of "control" here

Some entries are **real Ether types** (a C# class you place as `<ether:EtherButton .../>`); others
are **style-only resources** — a keyed `Style` you apply to a stock WinUI control
(`<ToggleSwitch Style="{StaticResource EtherSwitch}"/>`), with no new type. Each page says which it
is at the top. Style-only ones (Card, Tooltip, PanelTabs, Switch, ScrollBar) expose the underlying
WinUI control's own API — the design system only changes their skin.
