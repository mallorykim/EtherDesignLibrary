# EtherIntelligenceButton

An AI-affordance button — like `EtherButton`, but with a built-in sparkles icon by default. A
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

## Design-system-owned appearance

The design system owns this control's look. Most standard appearance properties — background,
borders, corner radius, colors, most typography, padding, content alignment — are
**design-system-owned**: setting them typically has no visible effect, by design, so every consuming
app stays consistent. A few *are* honored by the template, and which ones varies by control — so
treat [getting-started §4](../getting-started.md#4-known-boundaries) as the authoritative
per-property list (inert vs. consumed), not this summary.
