# EtherSteeringBar

An interactive value bar (think playback/scrubber) with optional stops, a title, and a value label.
Like `EtherProgressBar`, the value label **always** reflects the live `Value` — no static string can
desync from it; you choose whether to show it and how to format it.

- **Type:** `Ether.DesignSystem.Controls.EtherSteeringBar` (real control)
- **Base:** `Microsoft.UI.Xaml.Controls.Control` (it declares its own `Value`/`Minimum`/`Maximum`)
- **Gallery:** `Views/Controls/SteeringBarPage.xaml`

## Use it

```xml
<!-- Leave ValueFormat unset: the value label shows the built-in percent of
     [Minimum, Maximum] and stays in sync as Value changes. -->
<ether:EtherSteeringBar HorizontalAlignment="Stretch"
                        Title="Package playback"
                        Value="65"
                        AutomationProperties.Name="Package steering bar"/>
```

## Consumer API

**Ether-specific members:**

| Member | Type | What it does |
|--------|------|--------------|
| `Value` | `double` | Current value (TwoWay-capable). |
| `Minimum` / `Maximum` | `double` | Range bounds. |
| `SmallChange` / `LargeChange` | `double` | Keyboard/step increments. |
| `StepFrequency` | `double` | Step granularity. |
| `Stops` | `DoubleCollection?` | Discrete stop positions. |
| `SnapToStops` | `bool` | Snap the value to the nearest stop. |
| `ShowStops` | `bool` | Render stop ticks. |
| `ShowTitle` / `ShowValue` | `bool` | Show/hide the title / value label. |
| `Title` | `object?` | Optional label. |
| `ValueFormat` | `string?` | Composite format string for the label, e.g. `"{0:0} dB"`. |
| `ValueContentConverter` | `IValueConverter?` | Full control over the label; takes precedence over `ValueFormat`. Value = `Value`, parameter = the control (read `Minimum`/`Maximum`). |
| `ValueChanged` | event `SteeringBarValueChangedEventArgs` | Fires on **every drag tick** (`OldValue`/`NewValue`). |
| `MoveToNextStop()` / `MoveToPreviousStop()` | method → `bool` | Programmatically step between stops. |

## Bind & wire

`Value` is TwoWay. For custom units without a converter, set `ValueFormat` once (it needs no
`ValueChanged` handler to stay in sync):

```xml
<ether:EtherSteeringBar Value="{x:Bind ViewModel.Playback, Mode=TwoWay}"
                        Title="Playback" ValueFormat="{}{0:0} dB"
                        HorizontalAlignment="Stretch"/>
```

For full label control, set a `ValueContentConverter` (mirrors `Slider.ThumbToolTipValueConverter`):

```xml
<Page.Resources><local:DecibelConverter x:Key="DecibelConverter"/></Page.Resources>
...
<ether:EtherSteeringBar Value="{x:Bind ViewModel.Playback, Mode=TwoWay}"
                        ValueContentConverter="{StaticResource DecibelConverter}"
                        HorizontalAlignment="Stretch"/>
```

**Wire an action:** `ValueChanged` fires on **every drag tick**, not once per commit — it is a poor
fit for an `ICommand` (which is why there isn't one). Debounce in the handler if you only want the
settled value:

```xml
<ether:EtherSteeringBar Value="{x:Bind ViewModel.Playback, Mode=TwoWay}"
                        ValueChanged="{x:Bind ViewModel.OnPlaybackChanged}"/>
```

Proven at `RuntimeVerification.R2.cs:138-146` (`VerifyTwoWayBindings`). Stepping precedence: a
non-positive `StepFrequency` disables uniform snapping, and a populated `Stops` with `SnapToStops=true`
takes precedence over `StepFrequency`. `ValueChanged` fires for value changes from user input, code,
or automation, and carries old/new values.

## Appearance & overrides

The design system provides this control's intended look. Standard appearance properties fall into
three groups, and which group a given property is in varies by property: some are **template-bound**,
so overriding them *does* take effect (but departs from the design language); some are **locked**, so
setting them has no effect; and a few inherited ones are **silent traps** that look settable but do
nothing. Rather than guess, use [getting-started §4](../getting-started.md#4-known-boundaries) — the gate-checked
boundary rules plus the trap-property list. Prefer the control's intended options over ad-hoc
appearance overrides to stay on-brand.
