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
| `Command` / `CommandParameter` | `ICommand` / `object` | Fires on user **activation** (including activating an already-selected radio), **not** on a programmatic/bound `IsChecked` change. |

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

`Command`/`CommandParameter` fire on user activation (a radio can be activated again while already
selected), not on a programmatic `IsChecked` change:

```xml
<ether:EtherRadioButton GroupName="delivery-speed" Content="Express"
                        Command="{x:Bind ViewModel.SelectDeliverySpeedCommand}"
                        CommandParameter="Express"/>
```

Proven at `R2.cs:384, 419-431` (`VerifySelectionItemCommand`).

## Appearance & overrides

The design system provides this control's intended look. Standard appearance properties fall into
three groups, and which group a given property is in varies by property: some are **template-bound**,
so overriding them *does* take effect (but departs from the design language); some are **locked**, so
setting them has no effect; and a few inherited ones are **silent traps** that look settable but do
nothing. Rather than guess, use [getting-started §4](../getting-started.md#4-known-boundaries) — the gate-checked
boundary rules plus the trap-property list. Prefer the control's intended options over ad-hoc
appearance overrides to stay on-brand.
