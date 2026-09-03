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
| `SmallChange` | `double` | Arrow-key / wheel increment (distinct from Ether's `StepFrequency`). |
| `LargeChange` | `double` | PageUp/PageDown increment. |
| `ValueChanged` | event | Fires on every value change. |

## Bind & wire

`Value` is TwoWay, plus `StepFrequency`/`Stops`/`SnapToStops` for stepped ranges — the same
`RangeBase` contract as `EtherSteeringBar`:

```xml
<ether:EtherSlider Value="{x:Bind ViewModel.Volume, Mode=TwoWay}"
                   HorizontalAlignment="Stretch"/>
```

**Label / stop notes:** at most 11 custom `Labels` render; empty `Labels` auto-generate; labels are
UI-only; an empty `Stops` can be inferred from the labels; `SnapToStops` wins over `StepFrequency`; a
non-positive `StepFrequency` disables uniform snapping; and `FormatValue` returns a current-culture,
rounded whole number.

**Wire an action:** `ValueChanged` fires on **every tick** (same caveat as `EtherSteeringBar`) —
debounce if you only care about the settled value:

```xml
<ether:EtherSlider Value="{x:Bind ViewModel.Volume, Mode=TwoWay}"
                   ValueChanged="{x:Bind ViewModel.OnVolumeChanged}"/>
```

## Appearance & overrides

The design system provides this control's intended look. Standard appearance properties fall into
three groups, and which group a given property is in varies by property: some are **template-bound**,
so overriding them *does* take effect (but departs from the design language); some are **locked**, so
setting them has no effect; and a few inherited ones are **silent traps** that look settable but do
nothing. Rather than guess, use [getting-started §4](../getting-started.md#4-known-boundaries) — the gate-checked
boundary rules plus the trap-property list. Prefer the control's intended options over ad-hoc
appearance overrides to stay on-brand.
