# EtherDropdown

A selection dropdown. Reskins WinUI's `ComboBox`. **Selection-only** by design — there is no
free-text/editable mode. Selection binding uses the inherited `Selector`/`ComboBox` properties;
`EtherDropdown` adds only popup-sizing knobs.

- **Type:** `Ether.DesignSystem.Controls.EtherDropdown` (real control)
- **Base:** `Microsoft.UI.Xaml.Controls.ComboBox`
- **Gallery:** `Views/Controls/DropdownPage.xaml`

## Use it

```xml
<ether:EtherDropdown SelectedIndex="0"
                     HorizontalAlignment="Stretch"
                     AutomationProperties.Name="Package dropdown">
    <ComboBoxItem Content="10 Minutes"/>
    <ComboBoxItem Content="30 Minutes"/>
    <ComboBoxItem Content="1 Hour"/>
</ether:EtherDropdown>
```

## Consumer API

**Ether-specific members:**

| Member | Type | What it does |
|--------|------|--------------|
| `MaxVisibleItems` | `int` | Cap on rows shown before the popup scrolls. |
| `MenuGap` | `double` | Gap between the trigger and the open popup. |

**Standard WinUI members you'll use (inherited):**

| Member | Type | What it does |
|--------|------|--------------|
| `ItemsSource` | `object` | Bound item collection. |
| `SelectedItem` / `SelectedIndex` / `SelectedValue` | — | Selection (TwoWay-capable). |
| `SelectedValuePath` / `DisplayMemberPath` | `string` | Value/display projections. |
| `PlaceholderText` | `string` | Shown on the trigger while nothing is selected. |
| `MaxDropDownHeight` | `double` | Inherited, and it works — composed with `MaxVisibleItems` as an additional pixel ceiling on the popup. |
| `SelectionChanged` | event | Fires on selection change (no `Command`). |

## Bind & wire

```xml
<ether:EtherDropdown ItemsSource="{x:Bind ViewModel.DurationOptions}"
                     DisplayMemberPath="Label"
                     SelectedValuePath="Id"
                     SelectedValue="{x:Bind ViewModel.SelectedDurationId, Mode=TwoWay}"
                     HorizontalAlignment="Stretch"/>
```

Proven at `RuntimeVerification.R2.cs:98-116` (`VerifyTwoWayBindings`). Set `SelectedValuePath` (or a
stable ID on the item model) before treating `SelectedValue` as a business identifier.

No `Command`; bind the native `SelectionChanged`:

```xml
<ether:EtherDropdown ItemsSource="{x:Bind ViewModel.DurationOptions}"
                     DisplayMemberPath="Label"
                     SelectionChanged="{x:Bind ViewModel.OnDurationChanged}"/>
```

> The closed trigger only ever shows plain text (`DisplayMemberPath`/`ToString()`); a rich
> `ItemTemplate` renders inside the **open** menu only, not on the trigger.

## Not honored — selection-only by design

Setting these has no effect (the control has no editable text part / header slots):

| Property | Instead |
|----------|---------|
| `IsEditable`, `Text` | Selection-only — no typing. For free text, use [EtherInput](EtherInput.md) or a plain styled `ComboBox`. |
| `Header`, `HeaderTemplate` | Build the label markup outside the control (a `TextBlock` above it). |
| `Description` | Place a second `TextBlock` below the control. |
| `PlaceholderForeground` | `PlaceholderText` itself works; its color is not template-bound — use an external placeholder treatment if the color must be controlled. |

## Appearance & overrides

The design system provides this control's intended look. Standard appearance properties fall into
three groups, and which group a given property is in varies by property: some are **template-bound**,
so overriding them *does* take effect (but departs from the design language); some are **locked**, so
setting them has no effect; and a few inherited ones are **silent traps** that look settable but do
nothing. Rather than guess, use [getting-started §4](../getting-started.md#4-known-boundaries) — the
authoritative, gate-checked per-property breakdown. Prefer the control's intended options over ad-hoc
appearance overrides to stay on-brand.
