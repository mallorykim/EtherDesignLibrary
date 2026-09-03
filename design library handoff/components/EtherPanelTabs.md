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

Same contract as [EtherSegmentedControl](EtherSegmentedControl.md) (including that `ItemTemplate`
applies to non-string model items only): `ItemsSource` / `ItemTemplate` /
`DisplayMemberPath` / `SelectedIndex` / `SelectedItem` / `SelectedValue` (all TwoWay-capable),
`SelectionChanged`, and the user-initiated-only `SelectionCommand` / `SelectionCommandParameter`
pair. See that page for the selection/command semantics, which apply here unchanged. `EtherSegmentPanel.Spacing` (a `double`, default `4`) sets the inter-segment gap. The visual
difference is the two style keys: `EtherPanelTabs` on the host and `EtherPanelTabSegment` on each
**inline** segment.

> **Skinning data-driven segments.** `ItemsSource`-generated segments resolve the `EtherSegment`
> **resource key** from the control/ancestor/application scope (not a fixed style object) and have no
> per-item style property. By default that key is the blue segmented-control style, so a data-driven
> Panel Tabs renders with that skin. Two options: author **inline** `EtherSegmentRadioButton`s with
> `Style="{StaticResource EtherPanelTabSegment}"` (simplest), **or** define a scoped `EtherSegment`
> resource (based on `EtherPanelTabSegment`) in the control's/page's `Resources` so generated segments
> pick up the panel-tab skin.

## Appearance & overrides

The panel-tab skin (track, pill) is fixed by the two styles. Because this *is* `EtherSegmentedControl`,
appearance behaves exactly as there — some properties are template-bound (overriding works but
departs from the design language), some are locked, and a few are silent traps; see
[getting-started §4](../getting-started.md#4-known-boundaries) for the boundary rules plus the trap-property list.
