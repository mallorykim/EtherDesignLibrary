# EtherCheckbox

A two-state checkbox. Reskins WinUI's `CheckBox` (a `ToggleButton`). Deliberately **two-state only**
— it is not three-state; setting `IsThreeState=true` is rejected and a `null` `IsChecked` coerces to
`false`.

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
| `Command` / `CommandParameter` | `ICommand` / `object` | Fires on every check/uncheck. |
| `Checked` / `Unchecked` | events | Code-behind alternatives to `Command`. |

## Bind & wire

Bind a plain `bool` VM property TwoWay (only read the transient `null` if you deliberately want it):

```xml
<ether:EtherCheckbox Content="Send me updates"
                     IsChecked="{x:Bind ViewModel.SubscribeToUpdates, Mode=TwoWay}"/>
```

Proven at `RuntimeVerification.R2.cs:68-76` (`VerifyTwoWayBindings`).

As a `ToggleButton`, `Command`/`CommandParameter` fire on every toggle. Most apps need only one of
`IsChecked` TwoWay **or** `Command` — add `Command` only when something besides the bound state
(telemetry, a save) must run on toggle:

```xml
<ether:EtherCheckbox Content="Send me updates"
                     Command="{x:Bind ViewModel.ToggleSubscriptionCommand}"
                     CommandParameter="{x:Bind ViewModel.SubscriptionId}"/>
```

Proven at `R2.cs:379, 400-412` (`VerifyToggleCommand`).

## Design-system-owned appearance

The design system owns this control's look. Most standard appearance properties — background,
borders, corner radius, colors, most typography, padding, content alignment — are
**design-system-owned**: setting them typically has no visible effect. `IsThreeState` is also inert
(this control is two-state). A few appearance properties *are* honored by the template — treat
[getting-started §4](../getting-started.md#4-known-boundaries) as the authoritative per-property list
(inert vs. consumed), not this summary.
