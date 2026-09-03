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

None. `MaxWidth="240"` and the `body/s-regular` text style are baked into the style; pair the
`TextBlock`'s `Text` with `TextWrapping="Wrap"` (as above) and longer copy wraps on its own. There
is no Ether-specific property to bind and no interaction to wire.

If you need actual hover-triggered tooltip behavior, use WinUI's `ToolTipService` and place this
styled `Border` (or its content) as the tooltip's content.

## Design-system-owned

The tooltip chrome (inverse surface, radius, padding, max width, text style) is fixed by the style —
there is nothing to override.
