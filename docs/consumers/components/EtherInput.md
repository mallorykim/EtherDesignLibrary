# EtherInput

A single-line text field. Reskins WinUI's `TextBox`, so `Text`, typing behavior, and accessibility
are the platform's. By deliberate design it has **no built-in header/description slots** — lay those
out yourself.

- **Type:** `Ether.DesignSystem.Controls.EtherInput` (real control)
- **Base:** `Microsoft.UI.Xaml.Controls.TextBox`
- **Gallery:** `Views/Controls/InputPage.xaml`

## Use it

```xml
<ether:EtherInput PlaceholderText="Package input"
                  HorizontalAlignment="Stretch"
                  AutomationProperties.Name="Package input" />
```

## Consumer API

`EtherInput` adds no properties of its own — use the standard `TextBox` API:

| Member | Type | What it does |
|--------|------|--------------|
| `Text` | `string` | The text value (TwoWay). |
| `PlaceholderText` | `string` | Placeholder shown when empty. |
| `IsReadOnly` | `bool` | Block edits (behavioral, works). |
| `MaxLength`, `InputScope`, `TextWrapping` | — | Standard `TextBox` behavior. |
| `TextChanged` | event | Fires on text change. |

## Bind & wire

`Text` is TwoWay:

```xml
<ether:EtherInput PlaceholderText="Search"
                  Text="{x:Bind ViewModel.SearchQuery, Mode=TwoWay}"
                  HorizontalAlignment="Stretch"/>
```

Proven at `RuntimeVerification.R2.cs:88-96` (`VerifyTwoWayBindings`).

No `Command`; bind the native `TextChanged` event straight to a VM method (the handler may take
`(object, TextChangedEventArgs)` or no parameters):

```xml
<ether:EtherInput PlaceholderText="Search"
                  Text="{x:Bind ViewModel.SearchQuery, Mode=TwoWay}"
                  TextChanged="{x:Bind ViewModel.OnQueryChanged}"/>
```

## Not honored — set these up yourself

`EtherInput` deliberately drops the `TextBox` label/description family. Setting them has no effect:

| Property | Instead |
|----------|---------|
| `Header` | Wrap `EtherInput` in your own label layout (an external `TextBlock` above it). |
| `HeaderTemplate` | Same as `Header`. |
| `Description` | Place a second `TextBlock` below the control. |

This is a design decision (see the `EtherInput.cs` remarks), not an oversight.

## Design-system-owned appearance

The design system owns this control's look. Most standard appearance properties — background,
borders, corner radius, colors, most typography, padding — are **design-system-owned**: setting them
typically has no visible effect, by design. A few *are* honored by the template (for example text
alignment and the placeholder text) — treat
[getting-started §4](../getting-started.md#4-known-boundaries) as the authoritative per-property list
(inert vs. consumed), not this summary.
