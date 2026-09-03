# EtherTooltip

A static tooltip surface — inverse-surface chrome (dark on light, light on dark). **Style-only**: a
keyed `Style` on `Border`. This is a **visual surface only**, not the WinUI `ToolTip`/
`ToolTipService` — it has no hover/dismiss/placement behavior of its own.

- **Kind:** keyed `Style` (`EtherTooltip`) on `Border`
- **Gallery:** `Views/Surfaces/TooltipPage.xaml`

## Use it

```xml
<Border Style="{StaticResource EtherTooltip}">
    <TextBlock Text="Tooltip"
               Style="{StaticResource body/s-regular}"
               Foreground="{ThemeResource EtherTooltipForegroundBrush}"
               TextWrapping="Wrap"/>
</Border>
```

## Consumer API

None. The `EtherTooltip` style sets only the `Border` chrome (inverse surface, radius, padding, and
`MaxWidth="240"`). It does **not** style the child: the `body/s-regular` text style,
`EtherTooltipForegroundBrush`, and `TextWrapping="Wrap"` shown above are **required markup you apply
to the child `TextBlock`**, not baked into the style. There is no Ether-specific property to bind and
no interaction to wire.

If you need actual hover-triggered tooltip behavior, use WinUI's `ToolTipService` and place this
styled `Border` (or its content) as the tooltip's content.

## Appearance & overrides

The `Border` chrome (inverse surface, radius, padding, `MaxWidth`) comes from the style; the child
`TextBlock`'s typography is yours to set (use `body/s-regular` + `EtherTooltipForegroundBrush` to
match the design). You *can* override the `Border` properties, but that departs from the design.
