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
| `ShowTitle` | `bool` | `true`* | Show/hide the title. |
| `ShowValue` | `bool` | `true`* | Show/hide the value label. |
| `ValueFormat` | `string?` | `null` | Composite format string applied to `Value`, e.g. `"{0:0}%"`. With this unset (and no converter), the label is the built-in percent of `[Minimum, Maximum]`. |
| `ValueContentConverter` | `IValueConverter?` | `null` | Full control over the label text; takes precedence over `ValueFormat`. |

*Registered DP default is `false`; the shipping style sets both to `true`.

**Standard `RangeBase` members you'll use (inherited):**

| Member | Type | What it does |
|--------|------|--------------|
| `Value` | `double` | Current progress; the label tracks it live. |
| `Minimum` / `Maximum` | `double` | Range bounds. |
| `ValueChanged` | event | Fires when `Value` changes (code/binding-driven — the control is read-only); observable via the Interactions `ObserveRange` adapter. |

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

**Wire an action:** none — no user interaction. You *can* observe code/binding-driven progress changes via the inherited `ValueChanged` event (or the Interactions `ObserveRange` adapter).

**Determinate only.** `EtherProgressBar` has no `IsIndeterminate`, error, or paused states — for a
looping/indeterminate bar use the stock WinUI `ProgressBar`. A `ValueContentConverter` receives the
boxed `Value` as its value and the `EtherProgressBar` itself as its parameter, so it can read
`Minimum`/`Maximum`.

## Appearance & overrides

The design system provides this control's intended look. Standard appearance properties fall into
three groups, and which group a given property is in varies by property: some are **template-bound**,
so overriding them *does* take effect (but departs from the design language); some are **locked**, so
setting them has no effect; and a few inherited ones are **silent traps** that look settable but do
nothing. Rather than guess, use [getting-started §4](../getting-started.md#4-known-boundaries) — the gate-checked
boundary rules plus the trap-property list. Prefer the control's intended options over ad-hoc
appearance overrides to stay on-brand.
