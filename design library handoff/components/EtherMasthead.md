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
| `EnableWindowCommands` | `bool` | Default `true`. Whether the caption buttons **invoke host window commands** (and whether `ActionInvoked` fires). It does **not** hide or disable the buttons — when `false` they are still shown, but clicking them does nothing. |
| `ShowSettings` | `bool` | Show the settings affordance (default `true`). |
| `ShowSearch` / `ShowMenuIcon` / `ShowChevron` | `bool` | Show each affordance (default `false`). |
| `ActionInvoked` | event `MastheadActionInvokedEventArgs` | Reports which caption action fired (`Action`: `MastheadAction.Minimize`/`MaximizeRestore`/`Close`). |

## Bind & wire

The `Show*`/`EnableWindowCommands` flags are `OneWay`:

```xml
<ether:EtherMasthead ShowSettings="{x:Bind ViewModel.CanConfigure, Mode=OneWay}"
                     ShowSearch="True"
                     AutomationProperties.Name="App masthead"/>
```

`ActionInvoked` fires **before** the host window command executes and cannot be cancelled — but it
is raised **only when `EnableWindowCommands` is `true`** (that flag gates the caption buttons *and*
this event):

```xml
<ether:EtherMasthead ActionInvoked="{x:Bind ViewModel.OnMastheadAction}"/>
```

> The menu / settings / search / chevron slots are **decorative only** — they have no click hook,
> and `ObserveMasthead` does **not** make them actionable (it only reports the caption-button
> `ActionInvoked`). To act on those affordances, place your own interactive control (e.g. a `Button`)
> beside the masthead.

## Appearance & overrides

The design system provides this control's intended look. Standard appearance properties fall into
three groups, and which group a given property is in varies by property: some are **template-bound**,
so overriding them *does* take effect (but departs from the design language); some are **locked**, so
setting them has no effect; and a few inherited ones are **silent traps** that look settable but do
nothing. Rather than guess, use [getting-started §4](../getting-started.md#4-known-boundaries) — the gate-checked
boundary rules plus the trap-property list. Prefer the control's intended options over ad-hoc
appearance overrides to stay on-brand.
