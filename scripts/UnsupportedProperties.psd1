@{
    # Single source of truth for public, inherited WinUI base-class properties that Ether's
    # custom control templates do NOT consume. Consumers can set these: the build succeeds,
    # nothing throws, and nothing happens on screen (or, for EtherDropdown.Text/IsEditable,
    # nothing happens functionally either).
    #
    # scripts/Verify-UnsupportedProperties.ps1 asserts every entry below is genuinely
    # zero-consumption in the named template file(s), and that docs/consumers/getting-started.md
    # documents the exact same property list. Both the gate and the docs read this file; there
    # is no second copy of the list anywhere else. If someone later implements one of these
    # properties, remove its entry here (the gate will otherwise start failing the moment the
    # implementation lands, because the template file will then contain the binding this file
    # asserts is absent).
    #
    # CheckKind:
    #   'TemplateBindingAbsent' - the property is a decoration-only DP (Header, HeaderTemplate,
    #     Description, PlaceholderText, PlaceholderForeground). WinUI's own default template
    #     renders these only via an explicit `{TemplateBinding <Property>}` somewhere in the
    #     ControlTemplate; there is no other mechanism. The gate asserts that exact token does
    #     not appear in TemplateFile.
    #   'TemplatePartAbsent' - the property only has an effect through a specific, framework-
    #     documented named template part (ComboBox's editable mode needs a part named
    #     "EditableText" - see [TemplatePart] on ComboBox). The gate asserts TemplateFile does
    #     not declare a part with that name.
    #
    # Empirical basis (see PR description / audit report for the full probe transcript): a
    # RegisterPropertyChangedCallback probe was attached to every property below on live
    # EtherDropdown / EtherInput / ToggleSwitch(EtherSwitch) instances and exercised through
    # construction, template application, selection change, dropdown open/close, focus,
    # IsEditable toggle + subsequent selection change, and IsOn toggle. Zero callbacks fired
    # for any of them except IsEditable itself firing once, synchronously, for the test's own
    # explicit write - confirming none of these DPs are written by WinUI internals during normal
    # interaction, so intercepting them carries no risk of collateral framework breakage.
    Entries = @(
        @{
            Control      = 'EtherDropdown'
            Property     = 'Header'
            BaseType     = 'ComboBox'
            TemplateFile = 'src/Ether.DesignSystem.Controls/Controls/Inputs/EtherDropdown.xaml'
            CheckKind    = 'TemplateBindingAbsent'
            Alternative  = 'Wrap the EtherDropdown in a StackPanel/Grid with an external TextBlock as the label, or use a form container that lays out its own label above the control.'
        }
        @{
            Control      = 'EtherDropdown'
            Property     = 'HeaderTemplate'
            BaseType     = 'ComboBox'
            TemplateFile = 'src/Ether.DesignSystem.Controls/Controls/Inputs/EtherDropdown.xaml'
            CheckKind    = 'TemplateBindingAbsent'
            Alternative  = 'Same as Header: build the label markup outside the control instead of via HeaderTemplate.'
        }
        @{
            Control      = 'EtherDropdown'
            Property     = 'Description'
            BaseType     = 'ComboBox'
            TemplateFile = 'src/Ether.DesignSystem.Controls/Controls/Inputs/EtherDropdown.xaml'
            CheckKind    = 'TemplateBindingAbsent'
            Alternative  = 'Place a second TextBlock below the control for helper/description text.'
        }
        @{
            Control      = 'EtherDropdown'
            Property     = 'PlaceholderText'
            BaseType     = 'ComboBox'
            TemplateFile = 'src/Ether.DesignSystem.Controls/Controls/Inputs/EtherDropdown.xaml'
            CheckKind    = 'TemplateBindingAbsent'
            Alternative  = 'EtherDropdown always shows a selected item via TriggerText; there is no unselected placeholder state. Add a real placeholder ComboBoxItem (e.g. Content="Select...") and treat it as the default selection instead.'
        }
        @{
            Control      = 'EtherDropdown'
            Property     = 'PlaceholderForeground'
            BaseType     = 'ComboBox'
            TemplateFile = 'src/Ether.DesignSystem.Controls/Controls/Inputs/EtherDropdown.xaml'
            CheckKind    = 'TemplateBindingAbsent'
            Alternative  = 'Follows PlaceholderText: not applicable while there is no placeholder visual.'
        }
        @{
            Control      = 'EtherDropdown'
            Property     = 'Text'
            BaseType     = 'ComboBox'
            TemplateFile = 'src/Ether.DesignSystem.Controls/Controls/Inputs/EtherDropdown.xaml'
            CheckKind    = 'TemplatePartAbsent'
            TemplatePartName = 'EditableText'
            Alternative  = 'EtherDropdown is a selection-only control (Figma spec has no free-text combo variant). It does not offer an editable mode. If consumers need free-text entry, use EtherInput, or a plain WinUI editable ComboBox styled separately.'
        }
        @{
            Control      = 'EtherDropdown'
            Property     = 'IsEditable'
            BaseType     = 'ComboBox'
            TemplateFile = 'src/Ether.DesignSystem.Controls/Controls/Inputs/EtherDropdown.xaml'
            CheckKind    = 'TemplatePartAbsent'
            TemplatePartName = 'EditableText'
            Alternative  = 'Same as Text: selection-only control by design, no editable mode. Setting IsEditable=true does not enable typing; it silently changes nothing because the template has no "EditableText" part.'
        }
        @{
            Control      = 'EtherInput'
            Property     = 'Header'
            BaseType     = 'TextBox'
            TemplateFile = 'src/Ether.DesignSystem.Controls/Controls/Inputs/EtherInput.xaml'
            CheckKind    = 'TemplateBindingAbsent'
            Alternative  = 'This is an existing, deliberate design decision (see the EtherInput.cs remarks: "does not add ... header/description slots"), not an oversight. Wrap EtherInput in your own label/description layout.'
        }
        @{
            Control      = 'EtherInput'
            Property     = 'HeaderTemplate'
            BaseType     = 'TextBox'
            TemplateFile = 'src/Ether.DesignSystem.Controls/Controls/Inputs/EtherInput.xaml'
            CheckKind    = 'TemplateBindingAbsent'
            Alternative  = 'Same as Header.'
        }
        @{
            Control      = 'EtherInput'
            Property     = 'Description'
            BaseType     = 'TextBox'
            TemplateFile = 'src/Ether.DesignSystem.Controls/Controls/Inputs/EtherInput.xaml'
            CheckKind    = 'TemplateBindingAbsent'
            Alternative  = 'Same as Header: place a second TextBlock below the control.'
        }
        @{
            Control      = 'EtherSwitch'
            Property     = 'Header'
            BaseType     = 'ToggleSwitch'
            TemplateFile = 'src/Ether.DesignSystem.Controls/Controls/Inputs/EtherSwitch.xaml'
            CheckKind    = 'TemplateBindingAbsent'
            Alternative  = 'EtherSwitch is a keyed Style="{StaticResource EtherSwitch}" applied to a stock ToggleSwitch, not a subclass; there is no code-behind to intercept the write in. Wrap the ToggleSwitch in your own label layout instead of using Header.'
        }
        @{
            Control      = 'EtherSwitch'
            Property     = 'HeaderTemplate'
            BaseType     = 'ToggleSwitch'
            TemplateFile = 'src/Ether.DesignSystem.Controls/Controls/Inputs/EtherSwitch.xaml'
            CheckKind    = 'TemplateBindingAbsent'
            Alternative  = 'Same as Header.'
        }
        # NOTE: ToggleSwitch does NOT have a Description property in Microsoft.WindowsAppSDK
        # 2.3.1 (confirmed via reflection: Microsoft.UI.Xaml.Controls.ToggleSwitch exposes only
        # Header and HeaderTemplate from this family - no Description). It was not added here.
    )
}
