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

None. The `EtherTooltip` style sets the `Border` chrome (inverse surface, radius, padding,
`BorderThickness="2"`, `BackgroundSizing="OuterBorderEdge"`, `MaxWidth="240"`) **and pins
`HorizontalAlignment="Left"` / `VerticalAlignment="Top"`** — so the surface hugs its content and does
**not** stretch to fill its parent by default (override those two on your `Border` if you need it to). It does **not** style the child: the `body/s-regular` text style,
`EtherTooltipForegroundBrush`, and `TextWrapping="Wrap"` shown above are **required markup you apply
to the child `TextBlock`**, not baked into the style. There is no Ether-specific property to bind and
no interaction to wire.

If you need actual hover-triggered tooltip behavior, use WinUI's `ToolTipService` and place this
styled `Border` (or its content) as the tooltip's content.

## Appearance & overrides

The `Border` chrome (inverse surface, radius, padding, `BorderThickness`, `BackgroundSizing`,
`MaxWidth`) and its `Left`/`Top` alignment come from the style; the child
`TextBlock`'s typography is yours to set (use `body/s-regular` + `EtherTooltipForegroundBrush` to
match the design). You *can* override the `Border` properties, but that departs from the design.
