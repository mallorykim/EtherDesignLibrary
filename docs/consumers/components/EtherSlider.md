# EtherSlider

A value slider with optional stops, a title, and end labels. A `RangeBase`-derived control with the
standard `Value`/`Minimum`/`Maximum` contract.

- **Type:** `Ether.DesignSystem.Controls.EtherSlider` (real control)
- **Base:** `Microsoft.UI.Xaml.Controls.Primitives.RangeBase`
- **Gallery:** `Views/Controls/SliderPage.xaml`

## Use it

```xml
<ether:EtherSlider HorizontalAlignment="Stretch"
                   Value="65"
                   AutomationProperties.Name="Package slider"/>
```

## Consumer API

**Ether-specific members:**

| Member | Type | What it does |
|--------|------|--------------|
| `Title` | `string?` | Optional label above the track. |
| `ShowTitle` | `bool` | Show/hide the title. |
| `ShowLabels` | `bool` | Show/hide end labels. |
| `Labels` | `EtherSliderLabelCollection?` | Custom label set. |
| `Stops` | `DoubleCollection?` | Discrete stop positions. |
| `SnapToStops` | `bool` | Snap value to the nearest stop. |
| `StepFrequency` | `double` | Step granularity. |
| `FormatValue(double)` | method → `string` | The control's value-to-text formatter (e.g. for tooltips). |
| `MoveToNextStop()` / `MoveToPreviousStop()` | method → `bool` | Programmatically step between stops. |

**Standard WinUI members you'll use (inherited):**

| Member | Type | What it does |
|--------|------|--------------|
| `Value` | `double` | Current value (TwoWay-capable). |
| `Minimum` / `Maximum` | `double` | Range bounds. |
| `ValueChanged` | event | Fires on every value change. |

## Bind & wire

`Value` is TwoWay, plus `StepFrequency`/`Stops`/`SnapToStops` for stepped ranges — the same
`RangeBase` contract as `EtherSteeringBar`:

```xml
<ether:EtherSlider Value="{x:Bind ViewModel.Volume, Mode=TwoWay}"
                   HorizontalAlignment="Stretch"/>
```

**Wire an action:** `ValueChanged` fires on **every tick** (same caveat as `EtherSteeringBar`) —
debounce if you only care about the settled value:

```xml
<ether:EtherSlider Value="{x:Bind ViewModel.Volume, Mode=TwoWay}"
                   ValueChanged="{x:Bind ViewModel.OnVolumeChanged}"/>
```

## Design-system-owned appearance

The design system owns this control's look. Most standard appearance properties — background,
borders, corner radius, colors, most typography, padding, content alignment — are
**design-system-owned**: setting them typically has no visible effect, by design, so every consuming
app stays consistent. A few *are* honored by the template, and which ones varies by control — so
treat [getting-started §4](../getting-started.md#4-known-boundaries) as the authoritative
per-property list (inert vs. consumed), not this summary.
