# EtherIntelligenceButton

An AI-affordance button — like `EtherButton`, but with a built-in sparkles icon by default (and **no** `Variant`/`Size` — it has a single design; use `EtherButton` if you need variants/sizes). A
`Button`, so click/command/focus/accessibility are the platform's.

- **Type:** `Ether.DesignSystem.Controls.EtherIntelligenceButton` (real control)
- **Base:** `Microsoft.UI.Xaml.Controls.Button`
- **Gallery:** `Views/Controls/IntelligenceButtonPage.xaml`

## Use it

```xml
<ether:EtherIntelligenceButton Content="Package intelligence"
                               AutomationProperties.Name="Package intelligence button"/>
```

## Consumer API

**Ether-specific members:**

| Member | Type | Default | What it does |
|--------|------|---------|--------------|
| `LeftIcon` | `IconElement?` | sparkles glyph | Leading icon. Set to `{x:Null}` to remove it. |
| `RightIcon` | `IconElement?` | `null` | Trailing icon. |

The slots collapse automatically when empty, so layout stays tight either way.

**Standard WinUI members you'll use (inherited):** `Content`, `IsEnabled`, `Command` /
`CommandParameter`, `Click` — identical to [EtherButton](EtherButton.md).

## Use the icon slots

```xml
<!-- Keep the sparkles, add a trailing chevron -->
<ether:EtherIntelligenceButton Content="Ask Ether">
    <ether:EtherIntelligenceButton.RightIcon>
        <FontIcon Glyph="&#xE76C;"/>
    </ether:EtherIntelligenceButton.RightIcon>
</ether:EtherIntelligenceButton>

<!-- No leading icon at all -->
<ether:EtherIntelligenceButton Content="Plain" LeftIcon="{x:Null}"/>
```

## Bind & wire

Same as `EtherButton` — `Content`/`IsEnabled` `OneWay`, and `Command`/`CommandParameter`:

```xml
<ether:EtherIntelligenceButton Content="Ask Ether"
                               Command="{x:Bind ViewModel.AskCommand}"/>
```

Proven at `RuntimeVerification.R2.cs:378, 388-398` (`VerifyButtonCommand`).

## Appearance & overrides

The design system provides this control's intended look. Standard appearance properties fall into
three groups, and which group a given property is in varies by property: some are **template-bound**,
so overriding them *does* take effect (but departs from the design language); some are **locked**, so
setting them has no effect; and a few inherited ones are **silent traps** that look settable but do
nothing. Rather than guess, use [getting-started §4](../getting-started.md#4-known-boundaries) — the gate-checked
boundary rules plus the trap-property list. Prefer the control's intended options over ad-hoc
appearance overrides to stay on-brand.
