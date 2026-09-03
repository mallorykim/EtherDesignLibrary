# EtherSegmentedControl

A horizontal single-select control — a row of segments where exactly one is selected. Supports two
authoring styles: **inline** segments (`EtherSegmentRadioButton`s you write by hand) or a
**data-driven** contract (`ItemsSource` + a `Selected*` property), and the two can be mixed across
instances.

- **Type:** `Ether.DesignSystem.Controls.EtherSegmentedControl` (real control)
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
| `SelectedValue` | `object?` | Selected value; `TwoWay`-capable. |
| `SelectionChanged` | event `SegmentedSelectionChangedEventArgs` | Fires on **every** selection change (user or programmatic); carries `OldValue`/`NewValue`. |
| `SelectionCommand` | `ICommand?` | Fires **only on user-initiated** selection (never on a programmatic `Selected*` assignment). Guarded by `CanExecute`. |
| `SelectionCommandParameter` | `object?` | Passed to `SelectionCommand` instead of `SelectedValue` when set. |

`SegmentedSelectionChangedEventArgs`: `OldValue` (`object?`), `NewValue` (`object?`).

**Segment item — `EtherSegmentRadioButton`** (inherits `RadioButton`): use `Content`, `IsChecked`,
`GroupName`, and the native `Command`/`CommandParameter`. Wrap the segments in an
`EtherSegmentPanel` (its `Spacing` property, a `double`, sets the inter-segment gap).

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

## Design-system-owned (no effect if you set them)

Appearance properties — `Background`, `BorderBrush`, `BorderThickness`, `CornerRadius`,
`Foreground`, `Font*`, `Padding`, `CharacterSpacing`, `*ContentAlignment` — are inert on
`EtherSegmentedControl` and on `EtherSegmentRadioButton` by design. The selected-pill look is fixed
by the design system. Full list and rationale: [getting-started §4](../getting-started.md#4-known-boundaries).

## Related

[EtherPanelTabs](EtherPanelTabs.md) is the same control and contract with a different skin (a
translucent track and a near-black/near-white selected pill).
