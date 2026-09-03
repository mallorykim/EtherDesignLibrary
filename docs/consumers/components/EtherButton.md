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

The default (no `Style`) is the **primary, large** button. Pick a variant/size either with a keyed
`Style` or with the `Variant`/`Size` properties — they are equivalent:

```xml
<!-- style keys -->
<ether:EtherButton Style="{StaticResource EtherButtonTertiary}" Content="Tertiary" />
<ether:EtherButton Style="{StaticResource EtherButtonSecondarySmall}" Content="Small secondary" />

<!-- or properties -->
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
| `Variant` | `EtherButtonVariant?` | `Primary` | Visual emphasis: `Primary`, `Secondary`, `Tertiary`. Equivalent to the `EtherButton{Primary,Secondary,Tertiary}` style keys. |
| `Size` | `EtherButtonSize?` | `Large` | `Large` or `Small`. Equivalent to the `…Small` style keys. |
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

## Design-system-owned appearance

The design system owns the button's look. Most standard appearance properties — background, borders,
corner radius, colors, most typography, padding, content alignment — are **design-system-owned**:
setting them typically has no visible effect, by design. Choose the emphasis and size with
`Variant`/`Size` instead. A few appearance properties *are* honored by the template — treat
[getting-started §4](../getting-started.md#4-known-boundaries) as the authoritative per-property list
(inert vs. consumed), not this summary.
