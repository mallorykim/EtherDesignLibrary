# EtherCard

A content surface. **Style-only** — a set of keyed `Style`s applied to `Border`, in three variants
(`Normal` / `Intelligence` / `Callout`). Not a type with its own dependency properties; a card is a
static surface, not a control.

- **Kind:** keyed `Style`s on `Border`. Outer/body pairs: `EtherCardNormal` + `EtherCardNormalBody`,
  `EtherCardIntelligence` + `EtherCardIntelligenceBody`, and `EtherCardCalloutShell` +
  `EtherCardCalloutBody` (the callout is a composite shell + body).
- **Gallery:** `Views/Surfaces/CardPage.xaml`

## Use it

```xml
<Border Style="{StaticResource EtherCardNormal}">
    <Border Style="{StaticResource EtherCardNormalBody}">
        <TextBlock Text="Card" Style="{StaticResource headers/h3}"
                   Foreground="{ThemeResource text/primary}"/>
    </Border>
</Border>
```

## Consumer API

None. `EtherCard` is a set of keyed `Style`s on `Border`, with no Ether-specific properties and no
interaction surface. Put your content in the `Border`'s single `Child` (wrap multiple elements in a panel), exactly as you
would with a plain `Border`, and bind that content normally.

Pick the variant by choosing the outer style key — `EtherCardNormal`, `EtherCardIntelligence`, or
`EtherCardCalloutShell` — with its matching `…Body` inner style (see the Gallery
`Views/Surfaces/CardPage.xaml`). The `Callout` variant is a composite (an outer shell plus a separate
header region and `EtherCardCalloutBody`), so copy its Gallery markup rather than the simple
Normal shell+body shown above.

## Appearance & overrides

Every card value comes from ordinary `Style` setters, so you *can* locally override a property on the
`Border` and it takes effect — but that departs from the design treatment, so prefer choosing a
variant. Each outer style also imposes a **minimum size** that affects layout: `EtherCardNormal`
392x172, `EtherCardIntelligence` 392x130, `EtherCardCalloutShell` 394x206 (DIPs).
