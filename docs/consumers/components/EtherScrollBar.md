# EtherScrollBar

A styled scrollbar. **Implicit style** — once `DesignSystem.xaml` is merged, it applies
automatically to the native `ScrollBar`/`ScrollViewer`; you don't add a `Style=`. It is an
always-visible 6 px overlay (persistent-only).

- **Kind:** implicit `Style` on `Microsoft.UI.Xaml.Controls.Primitives.ScrollBar`
- **Gallery:** `Views/Foundations/ScrollBarPage.xaml`

## Use it

```xml
<ScrollViewer Width="240" Height="200"
              VerticalScrollBarVisibility="Visible"
              HorizontalScrollBarVisibility="Visible">
    <Border Width="400" Height="400"/>
</ScrollViewer>
```

## Consumer API

None. The style applies implicitly and adds no Ether-specific properties or events. To observe
scroll position, bind the native `ScrollViewer` APIs (`ViewChanged`, `VerticalOffset`, …) directly —
nothing here is Ether-owned.

## Persistent-only — important limitation

> This is an **always-visible** scrollbar. The template does **not** define the
> `ScrollingIndicatorStates`/`NoIndicator` groups, so it does **not** auto-hide when idle, expand on
> hover, or fade when disabled the way a native `ScrollBar` does — it is always rendered as a 6 px
> overlay. If your app needs auto-hide / consciousness-of-idle behavior, **do not rely on this
> implicit style** — set an explicit `Style=` (the native default, or your own). The template also omits **stepper
> arrows**; if you need line-up/line-down arrow buttons, use a different explicit style.

## Design-system-owned

The scrollbar's look and its persistent-only behavior are fixed by the style. Override it only by
applying a different `Style` explicitly (see the limitation above).
