# EtherRadioButton

A single-select radio button. Reskins WinUI's `RadioButton`. Same two-state coercion as
`EtherCheckbox` (`null` → `false`); mutual exclusion is by `GroupName`. Its UIA actuation is
`SelectionItem.Select()`, not `Toggle()`.

- **Type:** `Ether.DesignSystem.Controls.EtherRadioButton` (real control)
- **Base:** `Microsoft.UI.Xaml.Controls.RadioButton`
- **Gallery:** `Views/Controls/RadioButtonPage.xaml`

## Use it

```xml
<ether:EtherRadioButton GroupName="package-radio"
                        Content="Package radio"
                        AutomationProperties.Name="Package radio button" />
```

## Consumer API

`EtherRadioButton` adds no properties of its own — use the standard `RadioButton` API:

| Member | Type | What it does |
|--------|------|--------------|
| `IsChecked` | `bool?` | Selected state (TwoWay). Two-state: `null` coerces to `false`. |
| `GroupName` | `string` | Radios sharing a `GroupName` are mutually exclusive. |
| `Content` | `object` | Label. |
| `Command` / `CommandParameter` | `ICommand` / `object` | Fires once per selection. |

## Bind & wire

`IsChecked` TwoWay + `GroupName` for exclusion:

```xml
<ether:EtherRadioButton GroupName="delivery-speed" Content="Standard"
                        IsChecked="{x:Bind ViewModel.IsStandardDelivery, Mode=TwoWay}"/>
<ether:EtherRadioButton GroupName="delivery-speed" Content="Express"
                        IsChecked="{x:Bind ViewModel.IsExpressDelivery, Mode=TwoWay}"/>
```

Proven at `RuntimeVerification.R2.cs:78-86` (TwoWay) and group-name mutual exclusion
(`RadioButtonVerification.GroupNameMutualExclusionVerified`).

`Command`/`CommandParameter` fire once per selection:

```xml
<ether:EtherRadioButton GroupName="delivery-speed" Content="Express"
                        Command="{x:Bind ViewModel.SelectDeliverySpeedCommand}"
                        CommandParameter="Express"/>
```

Proven at `R2.cs:384, 419-431` (`VerifySelectionItemCommand`).

## Design-system-owned (no effect if you set them)

Appearance properties — `Background`, `BorderBrush`, `CornerRadius`, `Foreground`, `Font*`,
`Padding`, `*ContentAlignment` — are inert by design. Full list:
[getting-started §4](../getting-started.md#4-known-boundaries).
