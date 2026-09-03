# EtherCheckbox

A two-state checkbox. Reskins WinUI's `CheckBox` (a `ToggleButton`). Deliberately **two-state only**
— it is not three-state; `IsThreeState=true` is silently coerced back to `false` (no exception), and a
`null` `IsChecked` coerces to `false`.

- **Type:** `Ether.DesignSystem.Controls.EtherCheckbox` (real control)
- **Base:** `Microsoft.UI.Xaml.Controls.CheckBox`
- **Gallery:** `Views/Controls/CheckboxPage.xaml`

## Use it

```xml
<ether:EtherCheckbox Content="Package checkbox"
                     AutomationProperties.Name="Package checkbox" />
```

## Consumer API

`EtherCheckbox` adds no properties of its own — use the standard `CheckBox`/`ToggleButton` API:

| Member | Type | What it does |
|--------|------|--------------|
| `IsChecked` | `bool?` | Checked state (TwoWay). Two-state: `null` coerces to `false`. |
| `Content` | `object` | Label. |
| `Command` / `CommandParameter` | `ICommand` / `object` | Fires on user **activation** (a click/toggle), **not** on a programmatic/bound `IsChecked` change. Use `Checked`/`Unchecked` to observe every state change. |
| `Checked` / `Unchecked` | events | Code-behind alternatives to `Command`. |

## Bind & wire

Bind a plain `bool` VM property TwoWay (only read the transient `null` if you deliberately want it):

```xml
<ether:EtherCheckbox Content="Send me updates"
                     IsChecked="{x:Bind ViewModel.SubscribeToUpdates, Mode=TwoWay}"/>
```

Proven at `RuntimeVerification.R2.cs:68-76` (`VerifyTwoWayBindings`).

As a `ToggleButton`, `Command`/`CommandParameter` fire on user activation (not on a programmatic
`IsChecked` change). Most apps need only one of
`IsChecked` TwoWay **or** `Command` — add `Command` only when something besides the bound state
(telemetry, a save) must run on toggle:

```xml
<ether:EtherCheckbox Content="Send me updates"
                     Command="{x:Bind ViewModel.ToggleSubscriptionCommand}"
                     CommandParameter="{x:Bind ViewModel.SubscriptionId}"/>
```

Proven at `R2.cs:379, 400-412` (`VerifyToggleCommand`).

## Appearance & overrides

The design system provides this control's look. Standard appearance properties vary by property:
some are **template-bound** (overriding works but departs from the design language), some are
**locked** (no effect), and a few inherited ones are **silent traps**. `IsThreeState` is locked —
this control is two-state (`IsChecked=null` coerces to `false`). See
[getting-started §4](../getting-started.md#4-known-boundaries) for the boundary rules plus the trap-property list.
