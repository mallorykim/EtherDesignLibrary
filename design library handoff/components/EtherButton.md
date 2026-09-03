# EtherButton

A push button. Reskins WinUI's `Microsoft.UI.Xaml.Controls.Button` (a `ButtonBase`), so click,
command, focus, and accessibility all behave exactly like the platform button — only the look
changes. Three visual variants (primary / secondary / tertiary), two sizes (large / small), and
optional leading/trailing icon slots.

- **Type:** `Ether.DesignSystem.Controls.EtherButton` (real control)
- **Base:** `Microsoft.UI.Xaml.Controls.Button`
- **Gallery:** `Views/Controls/ButtonPage.xaml`

## Use it

```xml
<ether:EtherButton Content="Package button"
                   AutomationProperties.Name="Package button" />

<ether:EtherButton Style="{StaticResource EtherButtonSecondary}"
                   Content="Secondary package button"
                   AutomationProperties.Name="Secondary package button" />
```

A bare button (no `Style`, no `Variant`/`Size`) renders as **primary, large** — that look comes from
the default style. To change it, use **either** a keyed `Style` **or** the `Variant`/`Size` pair, not
both on the same element (whichever is assigned last wins). The `Variant`/`Size` route needs **both**
set — setting only one does nothing (they resolve a style only as a pair):

```xml
<!-- style keys -->
<ether:EtherButton Style="{StaticResource EtherButtonTertiary}" Content="Tertiary" />
<ether:EtherButton Style="{StaticResource EtherButtonSecondarySmall}" Content="Small secondary" />

<!-- or BOTH properties together -->
<ether:EtherButton Variant="Tertiary" Size="Small" Content="Same result" />
```

Icon slots take any `IconElement` (e.g. `FontIcon`, `PathIcon`, `SymbolIcon`):

```xml
<ether:EtherButton Content="Add">
    <ether:EtherButton.LeftIcon>
        <SymbolIcon Symbol="Add" />
    </ether:EtherButton.LeftIcon>
</ether:EtherButton>
```

## Consumer API

**Ether-specific members:**

| Member | Type | Default | What it does |
|--------|------|---------|--------------|
| `Variant` | `EtherButtonVariant?` | `null` (Primary via default style) | Visual emphasis: `Primary`, `Secondary`, `Tertiary`. Set together with `Size` to resolve a style; equivalent to the `EtherButton{Primary,Secondary,Tertiary}` style keys. |
| `Size` | `EtherButtonSize?` | `null` (Large via default style) | `Large` or `Small`. Only takes effect when `Variant` is also set. Equivalent to the `…Small` style keys. |
| `LeftIcon` | `IconElement?` | `null` | Leading icon, rendered before the content. |
| `RightIcon` | `IconElement?` | `null` | Trailing icon, rendered after the content. |

Style keys (apply via `Style="{StaticResource …}"`): `EtherButtonPrimary`, `EtherButtonSecondary`,
`EtherButtonTertiary`, and their `…Small` counterparts.

**Standard WinUI members you'll use (inherited, work normally):**

| Member | Type | What it does |
|--------|------|--------------|
| `Content` | `object` | Button label (usually a string). |
| `IsEnabled` | `bool` | Enables/disables interaction + input/automation state. |
| `Command` / `CommandParameter` | `ICommand` / `object` | MVVM action, fired on click. |
| `Click` | event | Code-behind click handler, if you prefer events. |

## Bind & wire

`Content`/`IsEnabled` are plain `ContentControl`/`Control` properties — a one-way `x:Bind` is
enough (there is no user-editable state to push back):

```xml
<ether:EtherButton Content="{x:Bind ViewModel.SaveLabel, Mode=OneWay}"
                   IsEnabled="{x:Bind ViewModel.CanSave, Mode=OneWay}"
                   Style="{StaticResource EtherButtonPrimary}"/>
```

Because it's a `ButtonBase`, the native `Command`/`CommandParameter` pair works with no event glue:

```xml
<ether:EtherButton Content="Save"
                   Command="{x:Bind ViewModel.SaveCommand}"
                   CommandParameter="{x:Bind ViewModel.CurrentItem, Mode=OneWay}"
                   Style="{StaticResource EtherButtonPrimary}"/>
```

Proven at `tests/Ether.DesignSystem.ConsumerFixtures/RuntimeVerification.R2.cs`
(`VerifyButtonCommand`): `Command`/`CommandParameter` fire exactly once per click, with the
expected parameter.

## Appearance & overrides

`EtherButton`'s look comes from `Variant`/`Size`, and **which overrides take effect depends on the
variant**. The **Primary** template template-binds `Background`, `BorderBrush`, `CornerRadius`,
`Padding`, `Foreground`, `FontSize`, `FontWeight`, and alignment — overriding those works (but
departs from the look). The **Secondary**/**Tertiary** templates hard-code most of those (background,
radius, foreground, fonts) to their variant tokens, so only a few (`Padding`, alignment, `FontSize`;
`BorderThickness` on Secondary) respond to an override. `CharacterSpacing`/`FontStretch` and
low-level `BackgroundSizing`/`CompositeMode` are always inert. Prefer `Variant`/`Size` over ad-hoc
overrides. See
[getting-started §4](../getting-started.md#4-known-boundaries) for the boundary rules plus the trap-property list.
