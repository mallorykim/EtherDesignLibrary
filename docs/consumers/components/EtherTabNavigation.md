# EtherTabNavigation

A horizontal tab strip. A thin `ListView` subclass, so selection binding uses the inherited
`Selector` properties. Tabs are `EtherTabItem`s (inline, or generated from `ItemsSource`).

- **Type:** `Ether.DesignSystem.Controls.EtherTabNavigation` (real control)
- **Item type:** `Ether.DesignSystem.Controls.EtherTabItem` (a `ListViewItem`)
- **Base:** `Microsoft.UI.Xaml.Controls.ListView`
- **Gallery:** `Views/Navigation/TabNavigationPage.xaml`

## Use it

```xml
<ether:EtherTabNavigation SelectedIndex="0" AutomationProperties.Name="Package tab navigation">
    <ether:EtherTabItem Content="Overview" Icon="Home"/>
    <ether:EtherTabItem Content="Details" Icon="Document"/>
    <ether:EtherTabItem Content="History" Icon="Clock"/>
</ether:EtherTabNavigation>
```

## Consumer API

**`EtherTabItem` (Ether-specific):**

| Member | Type | What it does |
|--------|------|--------------|
| `Icon` | `string?` | Per-tab icon key. **Inline `EtherTabItem` only** (see the limitation below). |
| `Content` | `object` | Tab label. |

**`EtherTabNavigation` — standard `ListView`/`Selector` members you'll use:**

| Member | Type | What it does |
|--------|------|--------------|
| `ItemsSource` | `object` | Bound tab collection. |
| `SelectedIndex` / `SelectedItem` | — | Selection (TwoWay-capable). |
| `SelectionChanged` | event | Fires on selection change (no `Command`). |

## Bind & wire

```xml
<ether:EtherTabNavigation ItemsSource="{x:Bind ViewModel.Sections}"
                          SelectedIndex="{x:Bind ViewModel.SelectedSectionIndex, Mode=TwoWay}"/>
```

Proven at `RuntimeVerification.R2.cs:168-186` (`SelectedIndex`/`SelectedItem` TwoWay) and
`R2.cs:205-211` (`VerifyDataPaths`).

No `Command`; bind the native `SelectionChanged`:

```xml
<ether:EtherTabNavigation ItemsSource="{x:Bind ViewModel.Sections}"
                          SelectionChanged="{x:Bind ViewModel.OnSectionSelectionChanged}"/>
```

> **Known limitation:** `Icon` is a per-container property on inline `EtherTabItem`. The
> `ItemsSource`-generated path has no `PrepareContainerForItemOverride`, so **data-bound tabs cannot
> show a per-item icon** — use inline `EtherTabItem` if icons are required.

## Design-system-owned appearance

The design system owns the look of the tab strip and each `EtherTabItem`. Most standard appearance
properties are **design-system-owned** and have no visible effect when set; a few *are* honored by
the template (some fonts, content alignment, and the item padding) — treat
[getting-started §4](../getting-started.md#4-known-boundaries) as the authoritative per-property list
(inert vs. consumed), not this summary.
