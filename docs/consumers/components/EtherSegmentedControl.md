# EtherSegmentedControl

A horizontal single-select control — a row of segments where at most one is selected (none until a
segment is checked or a selection is set; `SelectedIndex` starts at `-1`). Supports two
authoring styles: **inline** segments (`EtherSegmentRadioButton`s you write by hand) or a
**data-driven** contract (`ItemsSource` + a `Selected*` property), and the two can be mixed across
instances.

- **Type:** `Ether.DesignSystem.Controls.EtherSegmentedControl` (real control)
- **Base:** `Microsoft.UI.Xaml.Controls.ContentControl` (so `ItemsSource`/`Selected*` are Ether-added, not inherited `Selector` members)
- **Segment item:** `Ether.DesignSystem.Controls.EtherSegmentRadioButton` (apply the `EtherSegment`
  style, inside an `EtherSegmentPanel`)
- **Gallery:** `Views/Controls/SegmentedControlPage.xaml`

## Use it

Inline segments — full hover / pressed / checked feedback:

```xml
<ether:EtherSegmentedControl AutomationProperties.Name="Package segmented control">
    <ether:EtherSegmentPanel>
        <ether:EtherSegmentRadioButton Style="{StaticResource EtherSegment}"
                     GroupName="package-segment" Content="A" IsChecked="True"/>
        <ether:EtherSegmentRadioButton Style="{StaticResource EtherSegment}"
                     GroupName="package-segment" Content="B"/>
    </ether:EtherSegmentPanel>
</ether:EtherSegmentedControl>
```

A plain `RadioButton` with the `EtherSegment` style still shows the checked pill, but has no
hover/pressed feedback — prefer `EtherSegmentRadioButton`.

Data-driven — bind a collection and a selection:

```xml
<ether:EtherSegmentedControl AutomationProperties.Name="Time range"
                             ItemsSource="{x:Bind ViewModel.Periods}"
                             DisplayMemberPath="Label"
                             SelectedItem="{x:Bind ViewModel.SelectedPeriod, Mode=TwoWay}"/>
```

## Consumer API

**Ether-specific members:**

| Member | Type | What it does |
|--------|------|--------------|
| `ItemsSource` | `object?` | Collection that generates one `EtherSegmentRadioButton` per item. |
| `DisplayMemberPath` | `string?` | Property path used to render each generated segment's content. |
| `ItemTemplate` | `DataTemplate?` | Template for each generated segment (alternative to `DisplayMemberPath`). |
| `SelectedIndex` | `int` | Selected position; `TwoWay`-capable. |
| `SelectedItem` | `object?` | Selected data item; `TwoWay`-capable. |
| `SelectedValue` | `object?` | Selected value; `TwoWay`-capable. For **inline** segments it is the segment's `Tag` (falling back to `Content`); for **generated** segments it is the source item. There is no `SelectedValuePath`. |
| `SelectionChanged` | event `SegmentedSelectionChangedEventArgs` | Fires on **every** selection change (user or programmatic); carries `OldValue`/`NewValue`. |
| `SelectionCommand` | `ICommand?` | Fires **only on user-initiated** selection (never on a programmatic `Selected*` assignment). Guarded by `CanExecute`. |
| `SelectionCommandParameter` | `object?` | Passed to `SelectionCommand` instead of `SelectedValue` when set. |

`SegmentedSelectionChangedEventArgs`: `OldValue` (`object?`), `NewValue` (`object?`).

**Segment item — `EtherSegmentRadioButton`** (inherits `RadioButton`): use `Content`, `IsChecked`,
`GroupName`, and the native `Command`/`CommandParameter`. Wrap the segments in an
`EtherSegmentPanel` (its `Spacing` property, a `double`, default `4`, sets the inter-segment gap).

`EtherSegmentRadioButton` is also designed to be **subclassed**: it exposes the protected part-name
constants `HoverLayerPartName`/`PressedLayerPartName`/`CheckedLayerPartName`, the virtual
`IsPointerOverEffective`/`IsPressedEffective`, and `UpdateSegmentVisual()` for a custom segment
visual.

> **Accessibility:** the host is a `ContentControl`, not a `Selector`, so it exposes **no** host-level
> UIA Selection provider. Automation clients read selection from each `EtherSegmentRadioButton`'s
> `SelectionItem` pattern, not from the control.

## Bind & wire

`ItemsSource` + `DisplayMemberPath`/`ItemTemplate` + `SelectedIndex`/`SelectedItem`/`SelectedValue`
are all `TwoWay`-capable. Proven at `RuntimeVerification.R2.cs:118-136` (TwoWay
`SelectedValue`/`SelectedIndex`) and `R2.cs:213-224` (`VerifyDataPaths`: one segment generated per
item, `DisplayMemberPath` shapes content, selection stays synchronized).

`SelectionChanged` fires on every change and carries old/new:

```xml
<ether:EtherSegmentedControl ItemsSource="{x:Bind ViewModel.Periods}"
                             DisplayMemberPath="Label"
                             SelectionChanged="{x:Bind ViewModel.OnPeriodSelectionChanged}"/>
```

For an MVVM command that fires **only on the user's pick** (not on a programmatic selection), use
`SelectionCommand`/`SelectionCommandParameter` — same semantics as `ButtonBase.Command`:

```xml
<ether:EtherSegmentedControl ItemsSource="{x:Bind ViewModel.Periods}"
                             DisplayMemberPath="Label"
                             SelectionCommand="{x:Bind ViewModel.SelectPeriodCommand}"/>
```

Proven at `RuntimeVerification.R2.cs` (`VerifySegmentedControlSelectionCommand`): exactly one
execution with the expected parameter on a user pick, zero on a programmatic `SelectedIndex` change,
zero when `CanExecute` is `false`.

> **Setting the initial selection in XAML works.** `<ether:EtherSegmentedControl SelectedValue="beta">`
> is applied correctly even though the attribute is evaluated before the segments realize — the
> control defers and re-applies the value once its items exist (fixed in `preview.7`). You do not
> need to set the initial selection in code-behind.

## Appearance & overrides

The design system provides the segmented-control look (the selected pill). Standard appearance
properties vary — some are **template-bound** (overriding works but departs from the design
language), some are **locked**, and a few are **silent traps** — so use
[getting-started §4](../getting-started.md#4-known-boundaries) as the boundary rules plus the trap-property list rather than assuming.

## Related

[EtherPanelTabs](EtherPanelTabs.md) is the same control and contract with a different skin (a
translucent track and a near-black/near-white selected pill).
