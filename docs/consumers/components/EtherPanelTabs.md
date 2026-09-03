# EtherPanelTabs

Panel-style tabs. **Not a new type** — it is the same `EtherSegmentedControl` /`EtherSegmentPanel`/
`EtherSegmentRadioButton` skeleton with a different skin: a translucent track and a
near-black/near-white selected pill instead of the brand-blue one. Same selection contract as
[EtherSegmentedControl](EtherSegmentedControl.md).

- **Kind:** keyed `Style`s (`EtherPanelTabs` on the host, `EtherPanelTabSegment` on each segment)
- **Gallery:** `Views/Navigation/PanelTabsPage.xaml`

## Use it

```xml
<ether:EtherSegmentedControl Style="{StaticResource EtherPanelTabs}"
                             AutomationProperties.Name="Package panel tabs">
    <ether:EtherSegmentPanel>
        <ether:EtherSegmentRadioButton Style="{StaticResource EtherPanelTabSegment}"
                     GroupName="package-panel-tabs" Content="Overview" IsChecked="True"/>
        <ether:EtherSegmentRadioButton Style="{StaticResource EtherPanelTabSegment}"
                     GroupName="package-panel-tabs" Content="Details"/>
    </ether:EtherSegmentPanel>
</ether:EtherSegmentedControl>
```

## Consumer API

Identical to [EtherSegmentedControl](EtherSegmentedControl.md): `ItemsSource` / `ItemTemplate` /
`DisplayMemberPath` / `SelectedIndex` / `SelectedItem` / `SelectedValue` (all TwoWay-capable),
`SelectionChanged`, and the user-initiated-only `SelectionCommand` / `SelectionCommandParameter`
pair. See that page for the data-driven and command markup, which applies here unchanged.

The **only** difference is the two style keys: `EtherPanelTabs` on the host and
`EtherPanelTabSegment` on each segment (instead of `EtherSegmentedControl`'s defaults /
`EtherSegment`).

## Design-system-owned

The panel-tab skin (track, pill) is fixed by the two styles. Appearance is design-system-owned
exactly as on `EtherSegmentedControl` — most overrides have no effect, a few *are* honored; see
[getting-started §4](../getting-started.md#4-known-boundaries) for the authoritative per-property list.
