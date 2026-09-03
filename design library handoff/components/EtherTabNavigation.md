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
| `Icon` | `string?` | Per-tab icon **key from the Ether icon library** (e.g. `Home`). An unknown name silently collapses the slot; arbitrary `IconElement`s are not supported. Inline `EtherTabItem` only (see the limitation below). |
| `Content` | `object` | Tab label. |

**`EtherTabNavigation` — standard `ListView`/`Selector` members you'll use:**

| Member | Type | What it does |
|--------|------|--------------|
| `ItemsSource` | `object` | Bound tab collection. |
| `SelectedIndex` / `SelectedItem` | — | Selection (TwoWay-capable). `SelectedItem` is the `EtherTabItem` container for inline tabs, but the bound **model** for `ItemsSource` tabs. |
| `SelectedValue` / `SelectedValuePath` | — | Stable model-key selection (TwoWay). |
| `SelectionMode` | `ListViewSelectionMode` | Ships as `Single`. |
| `ItemTemplate` / `ItemTemplateSelector` | — | Template **non-string** data-bound tabs; a string `ItemsSource` renders the raw string, bypassing the template. A model `ItemTemplate` can include an icon. |
| `SelectionChanged` | event | Fires on selection change (no `Command`). |

Per-item `EtherTabItem` also exposes the inherited `IsSelected` and `IsEnabled`.

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

> **Icons on data-bound tabs:** data binding cannot populate the dedicated `EtherTabItem.Icon` slot
> (generated containers get no per-item `Icon`). To show an icon on a data-bound tab, render it inside
> a (non-string model) `ItemTemplate`; the built-in `Icon` slot is inline-`EtherTabItem` only.

## Appearance & overrides

The design system provides the tab-strip look. Standard appearance properties on the strip and each
`EtherTabItem` vary — some are **template-bound** (overriding works but departs from the design
language), some are **locked**, and a few are **silent traps** — so use
[getting-started §4](../getting-started.md#4-known-boundaries) as the boundary rules plus the trap-property list.
