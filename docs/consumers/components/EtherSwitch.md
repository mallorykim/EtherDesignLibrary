# EtherSwitch

An on/off toggle. **Style-only** — `EtherSwitch` is a keyed `Style` applied to the native
`ToggleSwitch`, not a new type. You keep the full `ToggleSwitch` API; the design system only changes
the skin.

- **Kind:** keyed `Style` (`EtherSwitch`) on `Microsoft.UI.Xaml.Controls.ToggleSwitch`
- **Gallery:** `Views/Controls/ToggleSwitchPage.xaml`

## Use it

```xml
<ToggleSwitch Style="{StaticResource EtherSwitch}"
              IsOn="True"
              OffContent="Off"
              OnContent="On"
              AutomationProperties.Name="Package toggle switch"/>
```

## Consumer API

Standard `ToggleSwitch` members (there is no Ether-specific API):

| Member | Type | What it does |
|--------|------|--------------|
| `IsOn` | `bool` | On/off state (TwoWay). |
| `OnContent` / `OffContent` | `object` | Labels for each state. |
| `Toggled` | event | Fires on toggle. |

## Bind & wire

```xml
<ToggleSwitch Style="{StaticResource EtherSwitch}"
              IsOn="{x:Bind ViewModel.NotificationsEnabled, Mode=TwoWay}"
              OffContent="Off" OnContent="On"/>
```

Proven at `RuntimeVerification.R2.cs:148-156` (`VerifyTwoWayBindings`).

`ToggleSwitch` has no `Command`; bind the native `Toggled` event:

```xml
<ToggleSwitch Style="{StaticResource EtherSwitch}"
              IsOn="{x:Bind ViewModel.NotificationsEnabled, Mode=TwoWay}"
              Toggled="{x:Bind ViewModel.OnNotificationsToggled}"/>
```

## Design-system-owned

The `EtherSwitch` style fixes the toggle's look. Restyle it only by not using this style. Appearance
overrides applied alongside it are governed by the same boundaries as the real controls
([getting-started §4](../getting-started.md#4-known-boundaries)).
