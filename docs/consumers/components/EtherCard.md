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
interaction surface. Put your content (title, body, icon) in the `Border`'s children exactly as you
would with a plain `Border`, and bind that content normally.

Pick the variant by choosing the outer style key — `EtherCardNormal`, `EtherCardIntelligence`, or
`EtherCardCalloutShell` — with its matching `…Body` inner style (see the Gallery
`Views/Surfaces/CardPage.xaml`).

## Design-system-owned

The card's chrome (surface color, radius, elevation, insets) is fixed by the style. There is nothing
to override — to change the look, choose a different variant or don't use the style.
