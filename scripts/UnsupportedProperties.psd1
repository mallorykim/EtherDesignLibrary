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
    # ============================================================================================
    # R-07: THIS FILE HAS TWO SECTIONS - a CLOSED list and an OPEN one.
    # (see docs/plans/2026-08-31-release-blockers-spec.md, section R-07, for the full history -
    # do not edit that spec file; this comment is the operative account of what the code does.)
    #
    #   Entries (below) - CLOSED allowlist, exactly 12 "trap" properties: consumer-facing
    #     functional/decoration properties (Header, HeaderTemplate, Description, PlaceholderText,
    #     PlaceholderForeground, Text, IsEditable) on EtherDropdown/EtherInput/EtherSwitch that a
    #     consumer would REASONABLY expect to work but that silently do nothing.
    #     scripts/Verify-UnsupportedProperties.ps1 statically re-checks only THESE 12 - it cannot
    #     discover a 13th silently-ineffective property, by design (it is a regression guard for
    #     known traps, not a scanner). Do not add new entries here without also adding an
    #     Alternative and updating docs/consumers/getting-started.md's machine-checked table -
    #     this list must stay hand-curated and small; that is what keeps every entry in it a
    #     genuine "a reasonable consumer would expect this to work" trap.
    #
    #   AcknowledgedSilent (below Entries) - OPEN accounting of every OTHER public, inherited
    #     property that the newest artifacts/audit-runs/consumer-runtime-evidence-*/
    #     runtime-result.json classifies as Method == 'platform-dp-contract': the DP getter/
    #     setter round-trips without throwing on an attached, rendered control, but the control's
    #     RenderTargetBitmap is pixel-for-pixel identical before and after - i.e. the property is
    #     publicly writable and inherited from a WinUI base class, but Ether's fixed control
    #     template does not consume it anywhere. As of the newest evidence run
    #     (consumer-runtime-evidence-20260831-033043851), that is 156 properties; 10 of those 156
    #     are already covered by the 12 Entries above (the other 2 Entries, EtherSwitch.Header/
    #     HeaderTemplate, do not appear in this evidence at all because EtherSwitch is a keyed
    #     Style applied to a stock ToggleSwitch, not a distinct type the runtime harness attaches
    #     evidence to by name) - leaving 146 properties accounted for here.
    #
    #     R-07 decision #2 (see the spec file) is that NONE of these 156 properties get wired up
    #     in this pass - this is a design system, and appearance is intentionally not overridable
    #     by consumers, so the engineering deliverable is DETECTION + ACCOUNTING, not
    #     implementation. Every AcknowledgedSilent entry carries a Bucket:
    #
    #       'design-system-owned' - an appearance property that the design system deliberately
    #         does not let consumers override, to keep the visual language consistent across
    #         every app that consumes it ("no visible effect" is a FEATURE here, not a bug).
    #         Classification rule used to generate this list: the property name is EXACTLY one
    #         of, or ENDS WITH one of (same appearance family, e.g. PlaceholderForeground ends
    #         with Foreground): Background, BackgroundSizing, BorderBrush, BorderThickness,
    #         CornerRadius, Padding, Foreground, FontSize, FontFamily, FontWeight, FontStyle,
    #         FontStretch, CharacterSpacing, HorizontalContentAlignment, VerticalContentAlignment.
    #         This is a deliberately narrow, mechanical rule (see
    #         scripts/Verify-SilentPropertyCoverage.ps1's own header for the same list) so the
    #         classification a human can audit does not depend on subjective judgment calls.
    #
    #       'platform-noop' - everything else: low-level rendering-internal DPs (Clip,
    #         CompositeMode) that essentially no consumer would ever set, PLUS a small group of
    #         behavioral/content DPs (EtherInput.AcceptsReturn/CharacterCasing/
    #         HorizontalTextAlignment/IsReadOnly/PlaceholderText, EtherDropdown.
    #         DisplayMemberPath/MaxDropDownHeight, EtherProgressBar.IsEnabled) that are NOT
    #         appearance properties but that Ether's fixed template genuinely does not wire to
    #         any visual or interactive effect either. Those behavioral entries carry an explicit
    #         Reason field and are flagged for a FUTURE human review of whether they should be
    #         promoted to a genuine trap Entry with an Alternative - that promotion is out of
    #         scope for this change (R-07 is detection + accounting, zero new TemplateBindings,
    #         zero new trap Entries), so they are parked in platform-noop for now rather than
    #         silently left unaccounted-for.
    #
    #   scripts/Verify-SilentPropertyCoverage.ps1 (a 'local-runtime' gate, since it needs the
    #   evidence file a GUI runtime pass produces) enforces two directions against the newest
    #   evidence file:
    #     - forward: every platform-dp-contract property must be in Entries OR AcknowledgedSilent
    #       (a brand-new, unaccounted-for silently-ineffective property FAILS the gate, naming
    #       the property and asking a human to classify it - nothing is ever auto-added here)
    #     - reverse: every AcknowledgedSilent entry must STILL be platform-dp-contract in that
    #       evidence (so a property that becomes genuinely wired/observable cannot leave a stale,
    #       lying entry behind)
    #   This is the OPEN half of what was previously a closed, 12-entry-only detection mechanism.
    # ============================================================================================
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

    # OPEN accounting (R-07) - see the file-header comment above for the full rule. Generated
    # programmatically from artifacts/audit-runs/consumer-runtime-evidence-20260831-033043851/
    # runtime-result.json's attachedVisualProperties.Evidence: every Method == 'platform-dp-
    # contract' property that is not already one of the 12 Entries above. 146 entries: 119
    # design-system-owned + 27 platform-noop. Do NOT hand-add entries here for a newly-discovered
    # platform-dp-contract property - regenerate from the newest evidence file and re-apply the
    # classification rule (or hand-classify a genuinely new/ambiguous one), so this list always
    # traces back to a real evidence run instead of accumulating hand-typed guesses.
    AcknowledgedSilent = @(
        # EtherButton
        @{ Property = 'EtherButton.BackgroundSizing'; Bucket = 'design-system-owned' }
        @{ Property = 'EtherButton.CharacterSpacing'; Bucket = 'design-system-owned' }
        @{ Property = 'EtherButton.CompositeMode'; Bucket = 'platform-noop' }
        @{ Property = 'EtherButton.FontStretch'; Bucket = 'design-system-owned' }
        @{ Property = 'EtherButton.HorizontalContentAlignment'; Bucket = 'design-system-owned' }
        @{ Property = 'EtherButton.VerticalContentAlignment'; Bucket = 'design-system-owned' }

        # EtherCheckbox
        @{ Property = 'EtherCheckbox.Background'; Bucket = 'design-system-owned' }
        @{ Property = 'EtherCheckbox.BackgroundSizing'; Bucket = 'design-system-owned' }
        @{ Property = 'EtherCheckbox.BorderBrush'; Bucket = 'design-system-owned' }
        @{ Property = 'EtherCheckbox.BorderThickness'; Bucket = 'design-system-owned' }
        @{ Property = 'EtherCheckbox.CharacterSpacing'; Bucket = 'design-system-owned' }
        @{ Property = 'EtherCheckbox.Clip'; Bucket = 'platform-noop' }
        @{ Property = 'EtherCheckbox.CompositeMode'; Bucket = 'platform-noop' }
        @{ Property = 'EtherCheckbox.CornerRadius'; Bucket = 'design-system-owned' }
        @{ Property = 'EtherCheckbox.FontSize'; Bucket = 'design-system-owned' }
        @{ Property = 'EtherCheckbox.FontStretch'; Bucket = 'design-system-owned' }
        @{ Property = 'EtherCheckbox.HorizontalContentAlignment'; Bucket = 'design-system-owned' }
        @{ Property = 'EtherCheckbox.Padding'; Bucket = 'design-system-owned' }
        @{ Property = 'EtherCheckbox.VerticalContentAlignment'; Bucket = 'design-system-owned' }

        # EtherDropdown
        @{ Property = 'EtherDropdown.BackgroundSizing'; Bucket = 'design-system-owned' }
        @{ Property = 'EtherDropdown.BorderBrush'; Bucket = 'design-system-owned' }
        @{ Property = 'EtherDropdown.BorderThickness'; Bucket = 'design-system-owned' }
        @{ Property = 'EtherDropdown.CharacterSpacing'; Bucket = 'design-system-owned' }
        @{ Property = 'EtherDropdown.CompositeMode'; Bucket = 'platform-noop' }
        @{ Property = 'EtherDropdown.DisplayMemberPath'; Bucket = 'platform-noop'; Reason = 'Content-shaping DP for editable/data-bound ComboBox scenarios; EtherDropdown is selection-only (see the IsEditable trap entry) and its template never reads this DP.' }
        @{ Property = 'EtherDropdown.FontStretch'; Bucket = 'design-system-owned' }
        @{ Property = 'EtherDropdown.HorizontalContentAlignment'; Bucket = 'design-system-owned' }
        @{ Property = 'EtherDropdown.MaxDropDownHeight'; Bucket = 'platform-noop'; Reason = 'Popup sizing is fully owned by the EtherDropdown template (fixed Popup/PopupBorder/ScrollViewer parts); the inherited cap is never consulted.' }
        @{ Property = 'EtherDropdown.VerticalContentAlignment'; Bucket = 'design-system-owned' }

        # EtherInput
        @{ Property = 'EtherInput.AcceptsReturn'; Bucket = 'platform-noop'; Reason = 'Multi-line text entry toggle inherited from TextBox; the EtherInput template is single-line only and does not branch on this DP.' }
        @{ Property = 'EtherInput.BackgroundSizing'; Bucket = 'design-system-owned' }
        @{ Property = 'EtherInput.CharacterCasing'; Bucket = 'platform-noop'; Reason = 'Inherited TextBox input-transform DP; EtherInput does not forward it into any text-transform behavior.' }
        @{ Property = 'EtherInput.CompositeMode'; Bucket = 'platform-noop' }
        @{ Property = 'EtherInput.FontFamily'; Bucket = 'design-system-owned' }
        @{ Property = 'EtherInput.FontStretch'; Bucket = 'design-system-owned' }
        @{ Property = 'EtherInput.HorizontalContentAlignment'; Bucket = 'design-system-owned' }
        @{ Property = 'EtherInput.HorizontalTextAlignment'; Bucket = 'platform-noop'; Reason = 'Inherited TextBox DP; the EtherInput template does not bind the content presenter alignment to this DP.' }
        @{ Property = 'EtherInput.IsReadOnly'; Bucket = 'platform-noop'; Reason = 'Inherited TextBox DP; the EtherInput template does not branch visual or input state on this DP.' }
        @{ Property = 'EtherInput.PlaceholderForeground'; Bucket = 'design-system-owned' }
        @{ Property = 'EtherInput.PlaceholderText'; Bucket = 'platform-noop'; Reason = 'The EtherInput template does not declare a PlaceholderTextContentPresenter binding path for this DP the way the trap-listed EtherDropdown.PlaceholderText does; no placeholder is rendered.' }
        @{ Property = 'EtherInput.VerticalContentAlignment'; Bucket = 'design-system-owned' }

        # EtherIntelligenceButton
        @{ Property = 'EtherIntelligenceButton.Background'; Bucket = 'design-system-owned' }
        @{ Property = 'EtherIntelligenceButton.BackgroundSizing'; Bucket = 'design-system-owned' }
        @{ Property = 'EtherIntelligenceButton.BorderBrush'; Bucket = 'design-system-owned' }
        @{ Property = 'EtherIntelligenceButton.BorderThickness'; Bucket = 'design-system-owned' }
        @{ Property = 'EtherIntelligenceButton.CharacterSpacing'; Bucket = 'design-system-owned' }
        @{ Property = 'EtherIntelligenceButton.CompositeMode'; Bucket = 'platform-noop' }
        @{ Property = 'EtherIntelligenceButton.CornerRadius'; Bucket = 'design-system-owned' }
        @{ Property = 'EtherIntelligenceButton.FontFamily'; Bucket = 'design-system-owned' }
        @{ Property = 'EtherIntelligenceButton.FontSize'; Bucket = 'design-system-owned' }
        @{ Property = 'EtherIntelligenceButton.FontStretch'; Bucket = 'design-system-owned' }
        @{ Property = 'EtherIntelligenceButton.FontWeight'; Bucket = 'design-system-owned' }
        @{ Property = 'EtherIntelligenceButton.Foreground'; Bucket = 'design-system-owned' }
        @{ Property = 'EtherIntelligenceButton.HorizontalContentAlignment'; Bucket = 'design-system-owned' }
        @{ Property = 'EtherIntelligenceButton.VerticalContentAlignment'; Bucket = 'design-system-owned' }

        # EtherMasthead
        @{ Property = 'EtherMasthead.BackgroundSizing'; Bucket = 'design-system-owned' }
        @{ Property = 'EtherMasthead.BorderBrush'; Bucket = 'design-system-owned' }
        @{ Property = 'EtherMasthead.BorderThickness'; Bucket = 'design-system-owned' }
        @{ Property = 'EtherMasthead.CharacterSpacing'; Bucket = 'design-system-owned' }
        @{ Property = 'EtherMasthead.Clip'; Bucket = 'platform-noop' }
        @{ Property = 'EtherMasthead.CompositeMode'; Bucket = 'platform-noop' }
        @{ Property = 'EtherMasthead.CornerRadius'; Bucket = 'design-system-owned' }
        @{ Property = 'EtherMasthead.FontFamily'; Bucket = 'design-system-owned' }
        @{ Property = 'EtherMasthead.FontSize'; Bucket = 'design-system-owned' }
        @{ Property = 'EtherMasthead.FontStretch'; Bucket = 'design-system-owned' }
        @{ Property = 'EtherMasthead.FontStyle'; Bucket = 'design-system-owned' }
        @{ Property = 'EtherMasthead.FontWeight'; Bucket = 'design-system-owned' }
        @{ Property = 'EtherMasthead.Foreground'; Bucket = 'design-system-owned' }
        @{ Property = 'EtherMasthead.HorizontalContentAlignment'; Bucket = 'design-system-owned' }
        @{ Property = 'EtherMasthead.Padding'; Bucket = 'design-system-owned' }
        @{ Property = 'EtherMasthead.VerticalContentAlignment'; Bucket = 'design-system-owned' }

        # EtherProgressBar
        @{ Property = 'EtherProgressBar.Background'; Bucket = 'design-system-owned' }
        @{ Property = 'EtherProgressBar.BackgroundSizing'; Bucket = 'design-system-owned' }
        @{ Property = 'EtherProgressBar.BorderBrush'; Bucket = 'design-system-owned' }
        @{ Property = 'EtherProgressBar.BorderThickness'; Bucket = 'design-system-owned' }
        @{ Property = 'EtherProgressBar.CharacterSpacing'; Bucket = 'design-system-owned' }
        @{ Property = 'EtherProgressBar.Clip'; Bucket = 'platform-noop' }
        @{ Property = 'EtherProgressBar.CompositeMode'; Bucket = 'platform-noop' }
        @{ Property = 'EtherProgressBar.CornerRadius'; Bucket = 'design-system-owned' }
        @{ Property = 'EtherProgressBar.FontFamily'; Bucket = 'design-system-owned' }
        @{ Property = 'EtherProgressBar.FontSize'; Bucket = 'design-system-owned' }
        @{ Property = 'EtherProgressBar.FontStretch'; Bucket = 'design-system-owned' }
        @{ Property = 'EtherProgressBar.FontStyle'; Bucket = 'design-system-owned' }
        @{ Property = 'EtherProgressBar.FontWeight'; Bucket = 'design-system-owned' }
        @{ Property = 'EtherProgressBar.Foreground'; Bucket = 'design-system-owned' }
        @{ Property = 'EtherProgressBar.HorizontalContentAlignment'; Bucket = 'design-system-owned' }
        @{ Property = 'EtherProgressBar.IsEnabled'; Bucket = 'platform-noop'; Reason = 'Inherited Control DP; the EtherProgressBar visual states do not include a Disabled trigger, so the bar renders identically enabled or disabled.' }
        @{ Property = 'EtherProgressBar.VerticalContentAlignment'; Bucket = 'design-system-owned' }

        # EtherRadioButton
        @{ Property = 'EtherRadioButton.Background'; Bucket = 'design-system-owned' }
        @{ Property = 'EtherRadioButton.BackgroundSizing'; Bucket = 'design-system-owned' }
        @{ Property = 'EtherRadioButton.BorderBrush'; Bucket = 'design-system-owned' }
        @{ Property = 'EtherRadioButton.BorderThickness'; Bucket = 'design-system-owned' }
        @{ Property = 'EtherRadioButton.CharacterSpacing'; Bucket = 'design-system-owned' }
        @{ Property = 'EtherRadioButton.Clip'; Bucket = 'platform-noop' }
        @{ Property = 'EtherRadioButton.CompositeMode'; Bucket = 'platform-noop' }
        @{ Property = 'EtherRadioButton.CornerRadius'; Bucket = 'design-system-owned' }
        @{ Property = 'EtherRadioButton.FontSize'; Bucket = 'design-system-owned' }
        @{ Property = 'EtherRadioButton.FontStretch'; Bucket = 'design-system-owned' }
        @{ Property = 'EtherRadioButton.HorizontalContentAlignment'; Bucket = 'design-system-owned' }
        @{ Property = 'EtherRadioButton.Padding'; Bucket = 'design-system-owned' }
        @{ Property = 'EtherRadioButton.VerticalContentAlignment'; Bucket = 'design-system-owned' }

        # EtherSegmentPanel
        @{ Property = 'EtherSegmentPanel.Clip'; Bucket = 'platform-noop' }
        @{ Property = 'EtherSegmentPanel.CompositeMode'; Bucket = 'platform-noop' }

        # EtherSegmentedControl
        @{ Property = 'EtherSegmentedControl.BackgroundSizing'; Bucket = 'design-system-owned' }
        @{ Property = 'EtherSegmentedControl.BorderBrush'; Bucket = 'design-system-owned' }
        @{ Property = 'EtherSegmentedControl.BorderThickness'; Bucket = 'design-system-owned' }
        @{ Property = 'EtherSegmentedControl.CharacterSpacing'; Bucket = 'design-system-owned' }
        @{ Property = 'EtherSegmentedControl.CompositeMode'; Bucket = 'platform-noop' }
        @{ Property = 'EtherSegmentedControl.CornerRadius'; Bucket = 'design-system-owned' }
        @{ Property = 'EtherSegmentedControl.FontFamily'; Bucket = 'design-system-owned' }
        @{ Property = 'EtherSegmentedControl.FontSize'; Bucket = 'design-system-owned' }
        @{ Property = 'EtherSegmentedControl.FontStretch'; Bucket = 'design-system-owned' }
        @{ Property = 'EtherSegmentedControl.Foreground'; Bucket = 'design-system-owned' }
        @{ Property = 'EtherSegmentedControl.HorizontalContentAlignment'; Bucket = 'design-system-owned' }
        @{ Property = 'EtherSegmentedControl.VerticalContentAlignment'; Bucket = 'design-system-owned' }

        # EtherSlider
        @{ Property = 'EtherSlider.Background'; Bucket = 'design-system-owned' }
        @{ Property = 'EtherSlider.BackgroundSizing'; Bucket = 'design-system-owned' }
        @{ Property = 'EtherSlider.BorderBrush'; Bucket = 'design-system-owned' }
        @{ Property = 'EtherSlider.BorderThickness'; Bucket = 'design-system-owned' }
        @{ Property = 'EtherSlider.CharacterSpacing'; Bucket = 'design-system-owned' }
        @{ Property = 'EtherSlider.Clip'; Bucket = 'platform-noop' }
        @{ Property = 'EtherSlider.CompositeMode'; Bucket = 'platform-noop' }
        @{ Property = 'EtherSlider.CornerRadius'; Bucket = 'design-system-owned' }
        @{ Property = 'EtherSlider.FontFamily'; Bucket = 'design-system-owned' }
        @{ Property = 'EtherSlider.FontSize'; Bucket = 'design-system-owned' }
        @{ Property = 'EtherSlider.FontStretch'; Bucket = 'design-system-owned' }
        @{ Property = 'EtherSlider.FontWeight'; Bucket = 'design-system-owned' }
        @{ Property = 'EtherSlider.Foreground'; Bucket = 'design-system-owned' }
        @{ Property = 'EtherSlider.HorizontalContentAlignment'; Bucket = 'design-system-owned' }
        @{ Property = 'EtherSlider.Padding'; Bucket = 'design-system-owned' }
        @{ Property = 'EtherSlider.VerticalContentAlignment'; Bucket = 'design-system-owned' }

        # EtherSteeringBar
        @{ Property = 'EtherSteeringBar.Background'; Bucket = 'design-system-owned' }
        @{ Property = 'EtherSteeringBar.BackgroundSizing'; Bucket = 'design-system-owned' }
        @{ Property = 'EtherSteeringBar.BorderBrush'; Bucket = 'design-system-owned' }
        @{ Property = 'EtherSteeringBar.BorderThickness'; Bucket = 'design-system-owned' }
        @{ Property = 'EtherSteeringBar.CharacterSpacing'; Bucket = 'design-system-owned' }
        @{ Property = 'EtherSteeringBar.Clip'; Bucket = 'platform-noop' }
        @{ Property = 'EtherSteeringBar.CompositeMode'; Bucket = 'platform-noop' }
        @{ Property = 'EtherSteeringBar.CornerRadius'; Bucket = 'design-system-owned' }
        @{ Property = 'EtherSteeringBar.FontFamily'; Bucket = 'design-system-owned' }
        @{ Property = 'EtherSteeringBar.FontSize'; Bucket = 'design-system-owned' }
        @{ Property = 'EtherSteeringBar.FontStretch'; Bucket = 'design-system-owned' }
        @{ Property = 'EtherSteeringBar.FontWeight'; Bucket = 'design-system-owned' }
        @{ Property = 'EtherSteeringBar.Foreground'; Bucket = 'design-system-owned' }
        @{ Property = 'EtherSteeringBar.HorizontalContentAlignment'; Bucket = 'design-system-owned' }
        @{ Property = 'EtherSteeringBar.VerticalContentAlignment'; Bucket = 'design-system-owned' }
    )
}
