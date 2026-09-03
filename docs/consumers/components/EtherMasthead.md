# EtherMasthead

An application title bar with window commands (minimize / maximize-restore / close) and optional
affordances (menu, search, settings, chevron).

- **Type:** `Ether.DesignSystem.Controls.EtherMasthead` (real control)
- **Base:** `Microsoft.UI.Xaml.Controls.Control`
- **Gallery:** `Views/Navigation/MastheadPage.xaml`

## Use it

```xml
<!-- EnableWindowCommands="False" here only to demo it embedded in a plain panel;
     real title-bar usage should keep the default (True). -->
<ether:EtherMasthead EnableWindowCommands="False"
                     AutomationProperties.Name="Package masthead"/>
```

## Consumer API

**Ether-specific members:**

| Member | Type | What it does |
|--------|------|--------------|
| `EnableWindowCommands` | `bool` | Enable the minimize/maximize/close caption buttons. |
| `ShowChevron` | `bool` | Show the chevron affordance. |
| `ShowMenuIcon` | `bool` | Show the menu icon. |
| `ShowSearch` | `bool` | Show the search affordance. |
| `ShowSettings` | `bool` | Show the settings affordance. |
| `ActionInvoked` | event `MastheadActionInvokedEventArgs` | Reports which caption action fired (`Action`: `MastheadAction.Minimize`/`MaximizeRestore`/`Close`). |

## Bind & wire

The `Show*`/`EnableWindowCommands` flags are `OneWay`:

```xml
<ether:EtherMasthead ShowSettings="{x:Bind ViewModel.CanConfigure, Mode=OneWay}"
                     ShowSearch="True"
                     AutomationProperties.Name="App masthead"/>
```

`ActionInvoked` fires **before** the host window command executes and cannot be cancelled:

```xml
<ether:EtherMasthead ActionInvoked="{x:Bind ViewModel.OnMastheadAction}"/>
```

> The menu / settings / search icon slots are **decorative only** (no built-in click hook). Wire a
> real action via the Interactions adapter (`ObserveMasthead`, see
> [getting-started §7](../getting-started.md#7-interactions-adapter-optional-backend-telemetry))
> or your own overlay.

## Design-system-owned appearance

The design system owns this control's look. Most standard appearance properties — background,
borders, corner radius, colors, most typography, padding, content alignment — are
**design-system-owned**: setting them typically has no visible effect, by design, so every consuming
app stays consistent. A few *are* honored by the template, and which ones varies by control — so
treat [getting-started §4](../getting-started.md#4-known-boundaries) as the authoritative
per-property list (inert vs. consumed), not this summary.
