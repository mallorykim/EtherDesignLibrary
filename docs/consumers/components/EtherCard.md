# EtherCard

A content surface. **Style-only** — a set of keyed `Style`s applied to `Border`, in three variants
(`Normal` / `Intelligence` / `Callout`). Not a type with its own dependency properties; a card is a
static surface, not a control.

- **Kind:** keyed `Style`s on `Border` (`EtherCardNormal`, `EtherCardIntelligence`,
  `EtherCardCallout`, plus matching `…Body` inner styles)
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

Pick the variant by choosing the style key: `EtherCardNormal`, `EtherCardIntelligence`, or
`EtherCardCallout` (each with its matching `…Body` inner style).

## Design-system-owned

The card's chrome (surface color, radius, elevation, insets) is fixed by the style. There is nothing
to override — to change the look, choose a different variant or don't use the style.
