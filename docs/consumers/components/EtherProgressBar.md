# EtherProgressBar

A read-only progress indicator with an optional title and value label. A `RangeBase`-derived control
with the standard `Value`/`Minimum`/`Maximum` contract. The value label **always** reflects the live
`Value` — by design there is no way to set a static string that could drift out of sync (the design
system bakes the logic in; you only choose whether to show it and, optionally, how to format it).

- **Type:** `Ether.DesignSystem.Controls.EtherProgressBar` (real control)
- **Base:** `Microsoft.UI.Xaml.Controls.Primitives.RangeBase`
- **Gallery:** `Views/DataDisplay/ProgressBarPage.xaml`

## Use it

```xml
<!-- Leave ValueFormat unset for the built-in synced percent of [Minimum, Maximum]. -->
<ether:EtherProgressBar HorizontalAlignment="Stretch"
                        Title="Package download"
                        Value="65"
                        AutomationProperties.Name="Package download progress" />
```

## Consumer API

**Ether-specific members:**

| Member | Type | Default | What it does |
|--------|------|---------|--------------|
| `Title` | `object` | `null` | Optional label above the bar. |
| `ShowTitle` | `bool` | — | Show/hide the title. |
| `ShowValue` | `bool` | — | Show/hide the value label. |
| `ValueFormat` | `string?` | `null` | Composite format string applied to `Value`, e.g. `"{0:0}%"`. With this unset (and no converter), the label is the built-in percent of `[Minimum, Maximum]`. |
| `ValueContentConverter` | `IValueConverter?` | `null` | Full control over the label text; takes precedence over `ValueFormat`. |

**Standard `RangeBase` members you'll use (inherited):**

| Member | Type | What it does |
|--------|------|--------------|
| `Value` | `double` | Current progress; the label tracks it live. |
| `Minimum` / `Maximum` | `double` | Range bounds. |

## Bind & wire

`Value`/`Title`/`ShowTitle`/`ShowValue` are `OneWay`-friendly display properties. `ValueFormat` is a
composite format string — set it once, don't bind it per value:

```xml
<ether:EtherProgressBar Value="{x:Bind ViewModel.DownloadPercent, Mode=OneWay}"
                        Title="{x:Bind ViewModel.DownloadLabel, Mode=OneWay}"
                        ValueFormat="{}{0:0}%"
                        ShowTitle="True" ShowValue="True"
                        HorizontalAlignment="Stretch"/>
```

**Wire an action:** none. This is a status indicator with no interaction surface.

## Design-system-owned (no effect if you set them)

Appearance properties — `Background`, `BorderBrush`, `CornerRadius`, `Foreground`, `Font*`,
`Padding`, `*ContentAlignment` — are inert by design. Full list:
[getting-started §4](../getting-started.md#4-known-boundaries).
