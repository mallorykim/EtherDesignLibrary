using System.Text.Json;
using EtherSandbox.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using Windows.Storage;

namespace Ether.DesignSystem.ConsumerFixtures;

internal static class RuntimeVerification
{
    private static readonly string[] RequiredAssetUris =
    {
        // URI escaping is required for the frozen comma-named font files. The
        // corresponding ms-appx paths retain their original comma filenames.
        "ms-appx:///Fonts/Instrument_Sans/InstrumentSans-VariableFont_wdth%2Cwght.ttf",
        "ms-appx:///Fonts/Inter/Inter-VariableFont_opsz%2Cwght.ttf",
        "ms-appx:///Assets/Icons/dds2/dds2_add-cir.svg",
    };

    private static readonly string[] ExpectedProgressGradient =
    {
        "#FF0021F3",
        "#FF0015FF",
        "#FF0EB2FF",
        "#FF40E1FD",
    };

    internal sealed record AssetVerification(string Uri, ulong Size, bool StorageFileResolved, string? StorageFileError);

    internal sealed record ProgressBarVerification(
        string[] TemplateParts,
        string[] LabelStates,
        double FillRatio,
        string AutomationName,
        string FallbackAutomationName,
        string AutomationClassName,
        string AutomationControlType,
        bool RangeValueReadOnly,
        bool SmallChangeIsNaN,
        bool LargeChangeIsNaN,
        double Minimum,
        double Maximum,
        double Value,
        bool SetValueRejected,
        bool ValueChangeExercised,
        string[] LightGradientColors,
        string[] DarkGradientColors,
        string[] LightTemplateBrushColors,
        string[] DarkTemplateBrushColors);

    internal sealed record ButtonVerification(
        bool DefaultStyleResolved,
        double DefaultMinHeight,
        double DefaultFontSize,
        string[] TemplateParts,
        string[] RightIconStates,
        bool RightIconCollapsed,
        string AutomationName,
        string[] LightTemplateBrushColors,
        string[] DarkTemplateBrushColors);

    internal sealed record CheckboxVerification(
        bool DefaultStyleResolved,
        bool DefaultIsThreeState,
        string[] TemplateParts,
        string[] CheckStates,
        string AutomationName,
        string[] LightTemplateBrushColors,
        string[] DarkTemplateBrushColors);

    internal sealed record RadioButtonVerification(
        bool DefaultStyleResolved,
        bool DefaultIsThreeState,
        string[] TemplateParts,
        string[] CheckStates,
        string AutomationName,
        string[] LightTemplateBrushColors,
        string[] DarkTemplateBrushColors);

    internal sealed record InputVerification(
        bool DefaultStyleResolved,
        double DefaultFontSize,
        bool DefaultUseSystemFocusVisuals,
        string[] TemplateParts,
        string[] CommonStates,
        bool DisabledOpacityApplied,
        string AutomationName,
        string[] LightTemplateBrushColors,
        string[] DarkTemplateBrushColors);

    internal sealed record DropdownVerification(
        bool DefaultStyleResolved,
        int DefaultMaxVisibleItems,
        bool DefaultUseSystemFocusVisuals,
        string[] TemplateParts,
        string[] DropDownStates,
        bool TriggerTextShowsSelection,
        bool ContentPresenterCollapsed,
        bool OpenedActiveStrokeVisible,
        bool ClosedActiveStrokeCollapsed,
        string AutomationName,
        string[] LightTemplateBrushColors,
        string[] DarkTemplateBrushColors);

    internal sealed record SegmentedControlVerification(
        bool DefaultStyleResolved,
        double DefaultPadding,
        string[] TemplateParts,
        int SegmentCount,
        string[] CheckStates,
        string AutomationName,
        string[] LightTemplateBrushColors,
        string[] DarkTemplateBrushColors);

    internal sealed record IntelligenceButtonVerification(
        bool DefaultStyleResolved,
        double DefaultFontSize,
        bool DefaultUseSystemFocusVisuals,
        string[] TemplateParts,
        string[] CommonStates,
        bool DisabledOpacityApplied,
        string AutomationName,
        string[] LightTemplateBrushColors,
        string[] DarkTemplateBrushColors);

    internal sealed record SteeringBarVerification(
        bool DefaultStyleResolved,
        bool DefaultUseSystemFocusVisuals,
        string[] TemplateParts,
        string[] LabelStates,
        double FillRatio,
        string AutomationName,
        string FallbackAutomationName,
        string AutomationClassName,
        string AutomationControlType,
        bool RangeValueReadOnly,
        double SmallChange,
        double LargeChange,
        double Minimum,
        double Maximum,
        double Value,
        bool SetValueAccepted,
        bool PreviewStatusLocksAutomation,
        bool ValueChangeExercised,
        string[] LightGradientColors,
        string[] DarkGradientColors,
        string[] LightTemplateBrushColors,
        string[] DarkTemplateBrushColors);

    internal sealed record SliderVerification(
        bool DefaultStyleResolved,
        bool DefaultUseSystemFocusVisuals,
        string[] TemplateParts,
        double FillRatio,
        int HighlightedBarCount,
        string AutomationName,
        string DefaultAutomationName,
        string AutomationClassName,
        string AutomationControlType,
        bool RangeValueReadOnly,
        double SmallChange,
        double LargeChange,
        double Minimum,
        double Maximum,
        double Value,
        bool SetValueAccepted,
        bool DisabledLocksAutomation,
        bool ValueChangeExercised,
        string FormattedValue,
        string[] LightTemplateBrushColors,
        string[] DarkTemplateBrushColors);

    internal sealed record MastheadVerification(
        bool DefaultStyleResolved,
        bool DefaultUseSystemFocusVisuals,
        bool DefaultShowSettings,
        bool DefaultShowSearch,
        string[] TemplateParts,
        string[] OptionalIconStates,
        bool SearchVisible,
        string AutomationName,
        string DefaultAutomationName,
        string[] LightTemplateBrushColors,
        string[] DarkTemplateBrushColors);

    internal sealed record ToggleSwitchVerification(
        bool KeyedStyleResolved,
        bool ImplicitStyleRejected,
        bool DefaultUseSystemFocusVisuals,
        string[] TemplateParts,
        string[] ToggleStates,
        bool DisabledTrackOpacityApplied,
        string AutomationName,
        string DefaultAutomationName,
        string[] LightTemplateBrushColors,
        string[] DarkTemplateBrushColors);

    internal sealed record ScrollBarVerification(
        bool ImplicitStyleApplied,
        bool VerticalRootPresent,
        bool HorizontalRootPresent,
        double Thickness,
        double ThumbMinLength,
        bool ArrowsCollapsed,
        string AutomationName,
        string[] LightTemplateBrushColors,
        string[] DarkTemplateBrushColors);

    internal sealed record VerificationResult(
        string[] ResourceKeys,
        AssetVerification[] Assets,
        string[] FontFamilySources,
        bool SvgImageLoaded,
        string LightBackgroundCanvasColor,
        string DarkBackgroundCanvasColor,
        ProgressBarVerification ProgressBar,
        ButtonVerification Button,
        CheckboxVerification Checkbox,
        RadioButtonVerification RadioButton,
        InputVerification Input,
        DropdownVerification Dropdown,
        SegmentedControlVerification SegmentedControl,
        IntelligenceButtonVerification IntelligenceButton,
        SteeringBarVerification SteeringBar,
        SliderVerification Slider,
        MastheadVerification Masthead,
        ToggleSwitchVerification ToggleSwitch,
        ScrollBarVerification ScrollBar);

    internal static async Task<VerificationResult> VerifyAsync(
        FrameworkElement themeRoot,
        Image svgProofImage,
        EtherProgressBar defaultProgressBar,
        EtherProgressBar progressBar,
        EtherButton defaultButton,
        EtherButton button,
        EtherButton secondaryButton,
        EtherCheckbox defaultCheckbox,
        EtherCheckbox checkbox,
        EtherRadioButton defaultRadioButton,
        EtherRadioButton radioButton,
        EtherInput defaultInput,
        EtherInput input,
        EtherDropdown defaultDropdown,
        EtherDropdown dropdown,
        EtherSegmentedControl defaultSegmentedControl,
        EtherSegmentedControl segmentedControl,
        EtherIntelligenceButton defaultIntelligenceButton,
        EtherIntelligenceButton intelligenceButton,
        EtherSteeringBar defaultSteeringBar,
        EtherSteeringBar steeringBar,
        EtherSlider defaultSlider,
        EtherSlider slider,
        EtherMasthead defaultMasthead,
        EtherMasthead masthead,
        ToggleSwitch bareToggleSwitch,
        ToggleSwitch defaultToggleSwitch,
        ToggleSwitch toggleSwitch,
        ScrollBar scrollBar,
        ScrollViewer scrollViewer)
    {
        var resourceKeys = new[]
        {
            AssertResource("Spacing8", typeof(double)),
            AssertResource("InterFont", typeof(FontFamily)),
            AssertResource("InstrumentSans", typeof(FontFamily)),
            AssertResource("IconAddCir", typeof(Style)),
            AssertResource("DefaultEtherProgressBarStyle", typeof(Style)),
            AssertResource("DefaultEtherButtonStyle", typeof(Style)),
            AssertResource("DefaultEtherCheckboxStyle", typeof(Style)),
            AssertResource("DefaultEtherRadioButtonStyle", typeof(Style)),
            AssertResource("DefaultEtherInputStyle", typeof(Style)),
            AssertResource("DefaultEtherDropdownStyle", typeof(Style)),
            AssertResource("DefaultEtherSegmentedControlStyle", typeof(Style)),
            AssertResource("DefaultEtherIntelligenceButtonStyle", typeof(Style)),
            AssertResource("DefaultEtherSteeringBarStyle", typeof(Style)),
            AssertResource("DefaultEtherSliderStyle", typeof(Style)),
            AssertResource("DefaultEtherMastheadStyle", typeof(Style)),
            AssertResource("EtherSwitch", typeof(Style)),
        };
        var fontFamilySources = new[]
        {
            ((FontFamily)Application.Current.Resources["InstrumentSans"]).Source,
            ((FontFamily)Application.Current.Resources["InterFont"]).Source,
        };

        var assets = new List<AssetVerification>();
        foreach (var uri in RequiredAssetUris)
        {
            assets.Add(await AssertApplicationFileAsync(uri));
        }

        await AssertSvgImageLoadedAsync(svgProofImage, RequiredAssetUris[2]);
        var progressBarResult = await VerifyProgressBarAsync(themeRoot, defaultProgressBar, progressBar);
        var buttonResult = await VerifyButtonAsync(themeRoot, defaultButton, button, secondaryButton);
        var checkboxResult = await VerifyCheckboxAsync(themeRoot, defaultCheckbox, checkbox);
        var radioButtonResult = await VerifyRadioButtonAsync(themeRoot, defaultRadioButton, radioButton);
        var inputResult = await VerifyInputAsync(themeRoot, defaultInput, input);
        var dropdownResult = await VerifyDropdownAsync(themeRoot, defaultDropdown, dropdown);
        var segmentedControlResult = await VerifySegmentedControlAsync(themeRoot, defaultSegmentedControl, segmentedControl);
        var intelligenceButtonResult = await VerifyIntelligenceButtonAsync(themeRoot, defaultIntelligenceButton, intelligenceButton);
        var steeringBarResult = await VerifySteeringBarAsync(themeRoot, defaultSteeringBar, steeringBar);
        var sliderResult = await VerifySliderAsync(themeRoot, defaultSlider, slider);
        var mastheadResult = await VerifyMastheadAsync(themeRoot, defaultMasthead, masthead);
        var toggleSwitchResult = await VerifyToggleSwitchAsync(themeRoot, bareToggleSwitch, defaultToggleSwitch, toggleSwitch);
        var scrollBarResult = await VerifyScrollBarAsync(themeRoot, scrollBar, scrollViewer);

        var light = await GetBackgroundColorAsync(themeRoot, ElementTheme.Light);
        var dark = await GetBackgroundColorAsync(themeRoot, ElementTheme.Dark);
        if (light == dark)
        {
            throw new InvalidOperationException(
                $"BackgroundCanvas did not re-resolve through ThemeResource. Light and Dark both produced {light}.");
        }

        return new VerificationResult(
            resourceKeys,
            assets.ToArray(),
            fontFamilySources,
            true,
            FormatColor(light),
            FormatColor(dark),
            progressBarResult,
            buttonResult,
            checkboxResult,
            radioButtonResult,
            inputResult,
            dropdownResult,
            segmentedControlResult,
            intelligenceButtonResult,
            steeringBarResult,
            sliderResult,
            mastheadResult,
            toggleSwitchResult,
            scrollBarResult);
    }

    internal static void WriteMarker(bool succeeded, VerificationResult? result = null, Exception? exception = null)
    {
        var path = Environment.GetEnvironmentVariable("ETHER_CONSUMER_SMOKE_RESULT_PATH");
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        var payload = JsonSerializer.Serialize(new
        {
            marker = "ETHER_CONSUMER_SMOKE",
            outcome = succeeded ? "success" : "failure",
            resourceKeys = result?.ResourceKeys ?? Array.Empty<string>(),
            attemptedAssetUris = RequiredAssetUris,
            assets = result?.Assets ?? Array.Empty<AssetVerification>(),
            fontFamilySources = result?.FontFamilySources ?? Array.Empty<string>(),
            svgImageLoaded = result?.SvgImageLoaded ?? false,
            lightBackgroundCanvasColor = result?.LightBackgroundCanvasColor,
            darkBackgroundCanvasColor = result?.DarkBackgroundCanvasColor,
            progressBar = result?.ProgressBar,
            button = result?.Button,
            checkbox = result?.Checkbox,
            radioButton = result?.RadioButton,
            input = result?.Input,
            dropdown = result?.Dropdown,
            segmentedControl = result?.SegmentedControl,
            intelligenceButton = result?.IntelligenceButton,
            steeringBar = result?.SteeringBar,
            slider = result?.Slider,
            masthead = result?.Masthead,
            toggleSwitch = result?.ToggleSwitch,
            scrollBar = result?.ScrollBar,
            message = exception?.ToString(),
        });
        File.WriteAllText(path, payload);
    }

    private static async Task<ButtonVerification> VerifyButtonAsync(
        FrameworkElement themeRoot,
        EtherButton defaultButton,
        EtherButton button,
        EtherButton secondaryButton)
    {
        defaultButton.ApplyTemplate();
        button.ApplyTemplate();
        secondaryButton.ApplyTemplate();
        defaultButton.UpdateLayout();
        button.UpdateLayout();
        secondaryButton.UpdateLayout();

        if (defaultButton.MinHeight != 40d || defaultButton.FontSize != 14d || defaultButton.Template is null)
        {
            throw new InvalidOperationException("The keyed EtherButton style did not apply its default Primary medium setters and template.");
        }

        var rightIcon = GetTemplatePart<ContentPresenter>(button, "RightIcon", nameof(EtherButton));
        var secondaryBackground = GetTemplatePart<Border>(secondaryButton, "Bg", nameof(EtherButton));
        var templateParts = new[] { "RightIcon" };
        var rightIconStates = new List<string>();

        button.RightIcon = new FontIcon { Glyph = "\uE72A", FontFamily = new FontFamily("Segoe MDL2 Assets"), FontSize = 10 };
        button.UpdateLayout();
        if (rightIcon.Visibility != Visibility.Visible)
        {
            throw new InvalidOperationException("The 'RightIconVisible' RightIconStates transition did not show the trailing icon slot.");
        }

        rightIconStates.Add("RightIconVisible");

        button.RightIcon = null;
        button.UpdateLayout();
        if (rightIcon.Visibility != Visibility.Collapsed)
        {
            throw new InvalidOperationException("The 'RightIconCollapsed' RightIconStates transition did not collapse the trailing icon slot.");
        }

        rightIconStates.Add("RightIconCollapsed");

        var peer = FrameworkElementAutomationPeer.CreatePeerForElement(button)
            ?? throw new InvalidOperationException("EtherButton did not create an automation peer.");
        if (peer.GetName() != "Package button")
        {
            throw new InvalidOperationException("EtherButton did not expose the required automation name.");
        }

        var lightTemplateBrushColors = await GetButtonTemplateBrushColorsAsync(
            themeRoot, secondaryButton, secondaryBackground, ElementTheme.Light);
        var darkTemplateBrushColors = await GetButtonTemplateBrushColorsAsync(
            themeRoot, secondaryButton, secondaryBackground, ElementTheme.Dark);
        if (lightTemplateBrushColors.SequenceEqual(darkTemplateBrushColors, StringComparer.Ordinal))
        {
            throw new InvalidOperationException("EtherButton Light and Dark template brushes did not re-resolve to distinct values.");
        }

        return new ButtonVerification(
            true,
            defaultButton.MinHeight,
            defaultButton.FontSize,
            templateParts,
            rightIconStates.ToArray(),
            rightIcon.Visibility == Visibility.Collapsed,
            peer.GetName(),
            lightTemplateBrushColors,
            darkTemplateBrushColors);
    }

    private static async Task<CheckboxVerification> VerifyCheckboxAsync(
        FrameworkElement themeRoot,
        EtherCheckbox defaultCheckbox,
        EtherCheckbox checkbox)
    {
        defaultCheckbox.ApplyTemplate();
        checkbox.ApplyTemplate();
        defaultCheckbox.UpdateLayout();
        checkbox.UpdateLayout();

        if (defaultCheckbox.IsThreeState || defaultCheckbox.Template is null)
        {
            throw new InvalidOperationException("The keyed EtherCheckbox style did not apply its default IsThreeState=False setter and template.");
        }

        GetTemplatePart<Border>(checkbox, "UncheckedFill", nameof(EtherCheckbox));
        var uncheckedFace = GetTemplatePart<Grid>(checkbox, "UncheckedFace", nameof(EtherCheckbox));
        var checkedFace = GetTemplatePart<Grid>(checkbox, "CheckedFace", nameof(EtherCheckbox));
        GetTemplatePart<Microsoft.UI.Xaml.Shapes.Path>(checkbox, "Glyph", nameof(EtherCheckbox));
        var label = GetTemplatePart<ContentPresenter>(checkbox, "Label", nameof(EtherCheckbox));
        var templateParts = new[] { "UncheckedFill", "UncheckedFace", "CheckedFace", "Glyph", "Label" };
        var checkStates = new List<string>();

        checkbox.IsChecked = false;
        checkbox.UpdateLayout();
        AssertCheckFaces(uncheckedFace, checkedFace, false, "Unchecked", nameof(EtherCheckbox));
        checkStates.Add("Unchecked");

        var lightTemplateBrushColors = await GetToggleLabelBrushColorsAsync(
            themeRoot, checkbox, label, ElementTheme.Light, nameof(EtherCheckbox));
        var darkTemplateBrushColors = await GetToggleLabelBrushColorsAsync(
            themeRoot, checkbox, label, ElementTheme.Dark, nameof(EtherCheckbox));
        if (lightTemplateBrushColors.SequenceEqual(darkTemplateBrushColors, StringComparer.Ordinal))
        {
            throw new InvalidOperationException("EtherCheckbox Light and Dark template brushes did not re-resolve to distinct values.");
        }

        checkbox.IsChecked = true;
        checkbox.UpdateLayout();
        AssertCheckFaces(uncheckedFace, checkedFace, true, "Checked", nameof(EtherCheckbox));
        checkStates.Add("Checked");

        var peer = FrameworkElementAutomationPeer.CreatePeerForElement(checkbox)
            ?? throw new InvalidOperationException("EtherCheckbox did not create an automation peer.");
        if (peer.GetName() != "Package checkbox")
        {
            throw new InvalidOperationException("EtherCheckbox did not expose the required automation name.");
        }

        return new CheckboxVerification(
            true,
            defaultCheckbox.IsThreeState,
            templateParts,
            checkStates.ToArray(),
            peer.GetName(),
            lightTemplateBrushColors,
            darkTemplateBrushColors);
    }

    private static async Task<RadioButtonVerification> VerifyRadioButtonAsync(
        FrameworkElement themeRoot,
        EtherRadioButton defaultRadioButton,
        EtherRadioButton radioButton)
    {
        defaultRadioButton.ApplyTemplate();
        radioButton.ApplyTemplate();
        defaultRadioButton.UpdateLayout();
        radioButton.UpdateLayout();

        if (defaultRadioButton.IsThreeState || defaultRadioButton.Template is null)
        {
            throw new InvalidOperationException("The keyed EtherRadioButton style did not apply its default IsThreeState=False setter and template.");
        }

        GetTemplatePart<Border>(radioButton, "UncheckedFill", nameof(EtherRadioButton));
        var uncheckedFace = GetTemplatePart<Grid>(radioButton, "UncheckedFace", nameof(EtherRadioButton));
        var checkedFace = GetTemplatePart<Grid>(radioButton, "CheckedFace", nameof(EtherRadioButton));
        GetTemplatePart<Microsoft.UI.Xaml.Shapes.Ellipse>(radioButton, "Dot", nameof(EtherRadioButton));
        var label = GetTemplatePart<ContentPresenter>(radioButton, "Label", nameof(EtherRadioButton));
        var templateParts = new[] { "UncheckedFill", "UncheckedFace", "CheckedFace", "Dot", "Label" };
        var checkStates = new List<string>();

        radioButton.IsChecked = false;
        radioButton.UpdateLayout();
        AssertCheckFaces(uncheckedFace, checkedFace, false, "Unchecked", nameof(EtherRadioButton));
        checkStates.Add("Unchecked");

        var lightTemplateBrushColors = await GetToggleLabelBrushColorsAsync(
            themeRoot, radioButton, label, ElementTheme.Light, nameof(EtherRadioButton));
        var darkTemplateBrushColors = await GetToggleLabelBrushColorsAsync(
            themeRoot, radioButton, label, ElementTheme.Dark, nameof(EtherRadioButton));
        if (lightTemplateBrushColors.SequenceEqual(darkTemplateBrushColors, StringComparer.Ordinal))
        {
            throw new InvalidOperationException("EtherRadioButton Light and Dark template brushes did not re-resolve to distinct values.");
        }

        radioButton.IsChecked = true;
        radioButton.UpdateLayout();
        AssertCheckFaces(uncheckedFace, checkedFace, true, "Checked", nameof(EtherRadioButton));
        checkStates.Add("Checked");

        var peer = FrameworkElementAutomationPeer.CreatePeerForElement(radioButton)
            ?? throw new InvalidOperationException("EtherRadioButton did not create an automation peer.");
        if (peer.GetName() != "Package radio button")
        {
            throw new InvalidOperationException("EtherRadioButton did not expose the required automation name.");
        }

        return new RadioButtonVerification(
            true,
            defaultRadioButton.IsThreeState,
            templateParts,
            checkStates.ToArray(),
            peer.GetName(),
            lightTemplateBrushColors,
            darkTemplateBrushColors);
    }

    private static async Task<InputVerification> VerifyInputAsync(
        FrameworkElement themeRoot,
        EtherInput defaultInput,
        EtherInput input)
    {
        defaultInput.ApplyTemplate();
        input.ApplyTemplate();
        defaultInput.UpdateLayout();
        input.UpdateLayout();

        if (defaultInput.FontSize != 14d || defaultInput.UseSystemFocusVisuals || defaultInput.Template is null)
        {
            throw new InvalidOperationException("The keyed EtherInput style did not apply its default FontSize, UseSystemFocusVisuals=False setter, and template.");
        }

        var layoutRoot = GetTemplatePart<Grid>(input, "LayoutRoot", nameof(EtherInput));
        var borderElement = GetTemplatePart<Border>(input, "BorderElement", nameof(EtherInput));
        var placeholder = GetTemplatePart<ContentControl>(input, "PlaceholderTextContentPresenter", nameof(EtherInput));
        GetTemplatePart<ScrollViewer>(input, "ContentElement", nameof(EtherInput));
        var templateParts = new[] { "LayoutRoot", "BorderElement", "PlaceholderTextContentPresenter", "ContentElement" };
        var commonStates = new List<string>();

        input.IsHitTestVisible = false;
        input.IsTabStop = false;

        VisualStateManager.GoToState(input, "Normal", false);
        input.UpdateLayout();
        var normalFill = GetSolidBrushColor(borderElement.Background, "fill", themeRoot.ActualTheme, nameof(EtherInput));
        var normalBorder = GetSolidBrushColor(borderElement.BorderBrush, "border", themeRoot.ActualTheme, nameof(EtherInput));
        commonStates.Add("Normal");

        var lightTemplateBrushColors = await GetInputPlaceholderBrushColorsAsync(
            themeRoot, input, placeholder, ElementTheme.Light);
        var darkTemplateBrushColors = await GetInputPlaceholderBrushColorsAsync(
            themeRoot, input, placeholder, ElementTheme.Dark);
        if (lightTemplateBrushColors.SequenceEqual(darkTemplateBrushColors, StringComparer.Ordinal))
        {
            throw new InvalidOperationException("EtherInput Light and Dark template brushes did not re-resolve to distinct values.");
        }

        VisualStateManager.GoToState(input, "PointerOver", false);
        input.UpdateLayout();
        var hoverFill = GetSolidBrushColor(borderElement.Background, "hover fill", themeRoot.ActualTheme, nameof(EtherInput));
        if (hoverFill == normalFill)
        {
            throw new InvalidOperationException("The 'PointerOver' CommonStates transition did not apply the hover fill on EtherInput.");
        }

        commonStates.Add("PointerOver");

        VisualStateManager.GoToState(input, "Focused", false);
        input.UpdateLayout();
        var focusedBorder = GetSolidBrushColor(borderElement.BorderBrush, "focus border", themeRoot.ActualTheme, nameof(EtherInput));
        if (focusedBorder == normalBorder)
        {
            throw new InvalidOperationException("The 'Focused' CommonStates transition did not apply the focus border on EtherInput.");
        }

        commonStates.Add("Focused");

        input.IsEnabled = false;
        VisualStateManager.GoToState(input, "Disabled", false);
        input.UpdateLayout();
        if (layoutRoot.Opacity != 0.5)
        {
            throw new InvalidOperationException("The 'Disabled' CommonStates transition did not apply 50% opacity on EtherInput.");
        }

        commonStates.Add("Disabled");

        var peer = FrameworkElementAutomationPeer.CreatePeerForElement(input)
            ?? throw new InvalidOperationException("EtherInput did not create an automation peer.");
        if (peer.GetName() != "Package input")
        {
            throw new InvalidOperationException("EtherInput did not expose the required automation name.");
        }

        return new InputVerification(
            true,
            defaultInput.FontSize,
            defaultInput.UseSystemFocusVisuals,
            templateParts,
            commonStates.ToArray(),
            layoutRoot.Opacity == 0.5,
            peer.GetName(),
            lightTemplateBrushColors,
            darkTemplateBrushColors);
    }

    private static async Task<DropdownVerification> VerifyDropdownAsync(
        FrameworkElement themeRoot,
        EtherDropdown defaultDropdown,
        EtherDropdown dropdown)
    {
        defaultDropdown.ApplyTemplate();
        dropdown.ApplyTemplate();
        defaultDropdown.UpdateLayout();
        dropdown.UpdateLayout();

        if (defaultDropdown.MaxVisibleItems != 6 ||
            defaultDropdown.UseSystemFocusVisuals ||
            defaultDropdown.Template is null ||
            defaultDropdown.ItemContainerStyle is null)
        {
            throw new InvalidOperationException("The keyed EtherDropdown style did not apply its default MaxVisibleItems, UseSystemFocusVisuals=False setter, item style, and template.");
        }

        var triggerText = GetTemplatePart<TextBlock>(dropdown, "TriggerText", nameof(EtherDropdown));
        GetTemplatePart<FrameworkElement>(dropdown, "Arrow", nameof(EtherDropdown));
        GetTemplatePart<Microsoft.UI.Xaml.Controls.Primitives.Popup>(dropdown, "Popup", nameof(EtherDropdown));
        var contentPresenter = GetTemplatePart<ContentPresenter>(dropdown, "ContentPresenter", nameof(EtherDropdown));
        var activeStroke = GetTemplatePart<Border>(dropdown, "ActiveStroke", nameof(EtherDropdown));
        var stateFill = GetTemplatePart<Border>(dropdown, "StateFill", nameof(EtherDropdown));
        var openFill = GetTemplatePart<Border>(dropdown, "OpenFill", nameof(EtherDropdown));
        // PopupBorder and ScrollViewer live inside ComboBox's popup host. They are not in the
        // closed visual tree and Popup.Child is not the named Border until the menu opens.
        // Do not open the popup here: live open timing is flaky. OnApplyTemplate still binds
        // those parts via GetTemplateChild.
        var templateParts = new[] { "TriggerText", "Arrow", "Popup" };

        if (contentPresenter.Visibility != Visibility.Collapsed)
        {
            throw new InvalidOperationException("EtherDropdown ContentPresenter must stay collapsed so ComboBox can own it while TriggerText shows the selection.");
        }

        if (triggerText.Text != "10 Minutes")
        {
            throw new InvalidOperationException($"EtherDropdown TriggerText did not show the selected item. Observed '{triggerText.Text}'.");
        }

        dropdown.IsHitTestVisible = false;
        dropdown.IsTabStop = false;

        var dropDownStates = new List<string>();
        VisualStateManager.GoToState(dropdown, "Opened", false);
        dropdown.UpdateLayout();
        if (activeStroke.Visibility != Visibility.Visible ||
            stateFill.Visibility != Visibility.Collapsed ||
            openFill.Visibility != Visibility.Visible)
        {
            throw new InvalidOperationException("The 'Opened' DropDownStates transition did not swap to OpenFill and show the active stroke on EtherDropdown.");
        }

        dropDownStates.Add("Opened");

        var lightTemplateBrushColors = await GetDropdownForegroundBrushColorsAsync(
            themeRoot, dropdown, triggerText, ElementTheme.Light);
        var darkTemplateBrushColors = await GetDropdownForegroundBrushColorsAsync(
            themeRoot, dropdown, triggerText, ElementTheme.Dark);
        if (lightTemplateBrushColors.SequenceEqual(darkTemplateBrushColors, StringComparer.Ordinal))
        {
            throw new InvalidOperationException("EtherDropdown Light and Dark template brushes did not re-resolve to distinct values.");
        }

        VisualStateManager.GoToState(dropdown, "Closed", false);
        dropdown.UpdateLayout();
        if (activeStroke.Visibility != Visibility.Collapsed ||
            stateFill.Visibility != Visibility.Visible ||
            openFill.Visibility != Visibility.Collapsed)
        {
            throw new InvalidOperationException("The 'Closed' DropDownStates transition did not restore StateFill and hide the active stroke on EtherDropdown.");
        }

        dropDownStates.Add("Closed");

        var peer = FrameworkElementAutomationPeer.CreatePeerForElement(dropdown)
            ?? throw new InvalidOperationException("EtherDropdown did not create an automation peer.");
        if (peer.GetName() != "Package dropdown")
        {
            throw new InvalidOperationException("EtherDropdown did not expose the required automation name.");
        }

        return new DropdownVerification(
            true,
            defaultDropdown.MaxVisibleItems,
            defaultDropdown.UseSystemFocusVisuals,
            templateParts,
            dropDownStates.ToArray(),
            triggerText.Text == "10 Minutes",
            contentPresenter.Visibility == Visibility.Collapsed,
            true,
            activeStroke.Visibility == Visibility.Collapsed,
            peer.GetName(),
            lightTemplateBrushColors,
            darkTemplateBrushColors);
    }

    private static async Task<SegmentedControlVerification> VerifySegmentedControlAsync(
        FrameworkElement themeRoot,
        EtherSegmentedControl defaultSegmentedControl,
        EtherSegmentedControl segmentedControl)
    {
        defaultSegmentedControl.ApplyTemplate();
        segmentedControl.ApplyTemplate();
        defaultSegmentedControl.UpdateLayout();
        segmentedControl.UpdateLayout();

        if (defaultSegmentedControl.Padding != new Thickness(4) ||
            defaultSegmentedControl.HorizontalContentAlignment != HorizontalAlignment.Left ||
            defaultSegmentedControl.VerticalContentAlignment != VerticalAlignment.Center ||
            defaultSegmentedControl.Template is null)
        {
            throw new InvalidOperationException("The keyed EtherSegmentedControl style did not apply its default Padding, content alignment, and template.");
        }

        GetTemplatePart<Canvas>(segmentedControl, "ShadowHost", nameof(EtherSegmentedControl));
        var trackSurface = GetTemplatePart<Border>(segmentedControl, "TrackSurface", nameof(EtherSegmentedControl));
        var templateParts = new[] { "ShadowHost", "TrackSurface" };
        var segments = GetSegmentRadioButtons(segmentedControl);
        if (segments.Length < 2)
        {
            throw new InvalidOperationException("EtherSegmentedControl did not host at least two EtherSegment radio buttons.");
        }

        foreach (var segment in segments)
        {
            segment.ApplyTemplate();
            segment.UpdateLayout();
            if (segment.Style is null)
            {
                throw new InvalidOperationException("EtherSegmentedControl segments did not resolve the keyed EtherSegment style.");
            }
        }

        var proofSegment = segments[0];
        var checkStates = new List<string>();

        var lightTemplateBrushColors = await GetSegmentedControlTrackBrushColorsAsync(
            themeRoot, segmentedControl, trackSurface, ElementTheme.Light);
        var darkTemplateBrushColors = await GetSegmentedControlTrackBrushColorsAsync(
            themeRoot, segmentedControl, trackSurface, ElementTheme.Dark);
        if (lightTemplateBrushColors.SequenceEqual(darkTemplateBrushColors, StringComparer.Ordinal))
        {
            throw new InvalidOperationException("EtherSegmentedControl Light and Dark template brushes did not re-resolve to distinct values.");
        }

        proofSegment.IsChecked = false;
        proofSegment.UpdateLayout();
        VisualStateManager.GoToState(proofSegment, "Normal", false);
        proofSegment.UpdateLayout();
        if (GetCurrentVisualStateName(proofSegment, "CommonStates", nameof(EtherSegmentedControl)) != "Normal")
        {
            throw new InvalidOperationException("The 'Normal' EtherSegment CommonStates transition did not become current after unchecking.");
        }

        checkStates.Add("Unchecked");

        proofSegment.IsChecked = true;
        proofSegment.UpdateLayout();
        VisualStateManager.GoToState(proofSegment, "Checked", false);
        proofSegment.UpdateLayout();
        if (GetCurrentVisualStateName(proofSegment, "CommonStates", nameof(EtherSegmentedControl)) != "Checked")
        {
            throw new InvalidOperationException("The 'Checked' EtherSegment CommonStates transition did not become current after checking.");
        }

        checkStates.Add("Checked");

        var peer = FrameworkElementAutomationPeer.CreatePeerForElement(segmentedControl)
            ?? throw new InvalidOperationException("EtherSegmentedControl did not create an automation peer.");
        if (peer.GetName() != "Package segmented control")
        {
            throw new InvalidOperationException("EtherSegmentedControl did not expose the required automation name.");
        }

        return new SegmentedControlVerification(
            true,
            defaultSegmentedControl.Padding.Left,
            templateParts,
            segments.Length,
            checkStates.ToArray(),
            peer.GetName(),
            lightTemplateBrushColors,
            darkTemplateBrushColors);
    }

    private static async Task<IntelligenceButtonVerification> VerifyIntelligenceButtonAsync(
        FrameworkElement themeRoot,
        EtherIntelligenceButton defaultIntelligenceButton,
        EtherIntelligenceButton intelligenceButton)
    {
        defaultIntelligenceButton.ApplyTemplate();
        intelligenceButton.ApplyTemplate();
        defaultIntelligenceButton.UpdateLayout();
        intelligenceButton.UpdateLayout();

        if (defaultIntelligenceButton.FontSize != 14d ||
            defaultIntelligenceButton.UseSystemFocusVisuals ||
            defaultIntelligenceButton.Padding != new Thickness(8, 8, 12, 8) ||
            defaultIntelligenceButton.Template is null)
        {
            throw new InvalidOperationException("The keyed EtherIntelligenceButton style did not apply its default FontSize, padding, UseSystemFocusVisuals=False setter, and template.");
        }

        var background = GetTemplatePart<Border>(intelligenceButton, "Bg", nameof(EtherIntelligenceButton));
        var intelligenceBlue = GetTemplatePart<Border>(intelligenceButton, "BorderIntelligenceBlue", nameof(EtherIntelligenceButton));
        GetTemplatePart<Border>(intelligenceButton, "BorderIntelligenceGradient", nameof(EtherIntelligenceButton));
        var blueGlowOuter = GetTemplatePart<Border>(intelligenceButton, "BlueGlowOuter", nameof(EtherIntelligenceButton));
        var blueGlowMiddle = GetTemplatePart<Border>(intelligenceButton, "BlueGlowMiddle", nameof(EtherIntelligenceButton));
        var blueGlowCore = GetTemplatePart<Border>(intelligenceButton, "BlueGlowCore", nameof(EtherIntelligenceButton));
        var purpleGlow = GetTemplatePart<Border>(intelligenceButton, "PurpleGlow", nameof(EtherIntelligenceButton));
        GetTemplatePart<ContentPresenter>(intelligenceButton, "Cp", nameof(EtherIntelligenceButton));
        GetTemplatePart<Border>(intelligenceButton, "FocusRing", nameof(EtherIntelligenceButton));
        var templateParts = new[]
        {
            "Bg",
            "BorderIntelligenceBlue",
            "BorderIntelligenceGradient",
            "BlueGlowOuter",
            "BlueGlowMiddle",
            "BlueGlowCore",
            "PurpleGlow",
            "Cp",
            "FocusRing",
        };
        var commonStates = new List<string>();

        intelligenceButton.IsHitTestVisible = false;
        intelligenceButton.IsTabStop = false;

        VisualStateManager.GoToState(intelligenceButton, "Normal", false);
        intelligenceButton.UpdateLayout();
        if (intelligenceBlue.BorderBrush is not RadialGradientBrush)
        {
            throw new InvalidOperationException("The 'Normal' CommonStates transition did not keep the radial intelligence stroke on EtherIntelligenceButton.");
        }

        var normalGlow = GetSolidBrushColor(blueGlowCore.Background, "glow", themeRoot.ActualTheme, nameof(EtherIntelligenceButton));
        commonStates.Add("Normal");

        var lightTemplateBrushColors = await GetIntelligenceButtonForegroundBrushColorsAsync(
            themeRoot, intelligenceButton, ElementTheme.Light);
        var darkTemplateBrushColors = await GetIntelligenceButtonForegroundBrushColorsAsync(
            themeRoot, intelligenceButton, ElementTheme.Dark);
        if (lightTemplateBrushColors.SequenceEqual(darkTemplateBrushColors, StringComparer.Ordinal))
        {
            throw new InvalidOperationException("EtherIntelligenceButton Light and Dark template brushes did not re-resolve to distinct values.");
        }

        VisualStateManager.GoToState(intelligenceButton, "PointerOver", false);
        intelligenceButton.UpdateLayout();
        if (intelligenceBlue.BorderBrush is not LinearGradientBrush)
        {
            throw new InvalidOperationException("The 'PointerOver' CommonStates transition did not apply the hover intelligence stroke on EtherIntelligenceButton.");
        }

        commonStates.Add("PointerOver");

        VisualStateManager.GoToState(intelligenceButton, "Pressed", false);
        intelligenceButton.UpdateLayout();
        var pressedGlow = GetSolidBrushColor(blueGlowCore.Background, "pressed glow", themeRoot.ActualTheme, nameof(EtherIntelligenceButton));
        if (pressedGlow == normalGlow)
        {
            throw new InvalidOperationException("The 'Pressed' CommonStates transition did not apply the pressed core glow on EtherIntelligenceButton.");
        }

        commonStates.Add("Pressed");

        VisualStateManager.GoToState(intelligenceButton, "Disabled", false);
        intelligenceButton.UpdateLayout();
        intelligenceButton.IsEnabled = false;
        intelligenceButton.UpdateLayout();
        if (Math.Abs(background.Opacity - 0.4) > 0.02 ||
            blueGlowOuter.Visibility != Visibility.Collapsed ||
            blueGlowMiddle.Visibility != Visibility.Collapsed ||
            blueGlowCore.Visibility != Visibility.Collapsed ||
            purpleGlow.Visibility != Visibility.Collapsed)
        {
            throw new InvalidOperationException("The 'Disabled' CommonStates transition did not apply 40% opacity and collapse glow layers on EtherIntelligenceButton.");
        }

        commonStates.Add("Disabled");

        var peer = FrameworkElementAutomationPeer.CreatePeerForElement(intelligenceButton)
            ?? throw new InvalidOperationException("EtherIntelligenceButton did not create an automation peer.");
        if (peer.GetName() != "Package intelligence button")
        {
            throw new InvalidOperationException("EtherIntelligenceButton did not expose the required automation name.");
        }

        return new IntelligenceButtonVerification(
            true,
            defaultIntelligenceButton.FontSize,
            defaultIntelligenceButton.UseSystemFocusVisuals,
            templateParts,
            commonStates.ToArray(),
            Math.Abs(background.Opacity - 0.4) <= 0.02,
            peer.GetName(),
            lightTemplateBrushColors,
            darkTemplateBrushColors);
    }

    private static RadioButton[] GetSegmentRadioButtons(EtherSegmentedControl control)
    {
        if (control.Content is not Panel panel)
        {
            throw new InvalidOperationException("EtherSegmentedControl content must be a panel of EtherSegment radio buttons.");
        }

        return panel.Children.OfType<RadioButton>().ToArray();
    }

    private static string GetCurrentVisualStateName(FrameworkElement control, string groupName, string ownerName)
    {
        if (VisualTreeHelper.GetChildrenCount(control) < 1 ||
            VisualTreeHelper.GetChild(control, 0) is not FrameworkElement root)
        {
            throw new InvalidOperationException($"{ownerName} has no template root for visual-state inspection.");
        }

        var group = VisualStateManager.GetVisualStateGroups(root)
            .OfType<VisualStateGroup>()
            .FirstOrDefault(candidate => candidate.Name == groupName)
            ?? throw new InvalidOperationException($"{ownerName} is missing VisualStateGroup '{groupName}'.");

        return group.CurrentState?.Name
            ?? throw new InvalidOperationException($"{ownerName} VisualStateGroup '{groupName}' has no current state.");
    }

    private static void AssertCheckFaces(
        FrameworkElement uncheckedFace,
        FrameworkElement checkedFace,
        bool isChecked,
        string expectedState,
        string ownerName)
    {
        var expectedUnchecked = isChecked ? Visibility.Collapsed : Visibility.Visible;
        var expectedChecked = isChecked ? Visibility.Visible : Visibility.Collapsed;
        if (uncheckedFace.Visibility != expectedUnchecked || checkedFace.Visibility != expectedChecked)
        {
            throw new InvalidOperationException($"The '{expectedState}' CheckStates transition did not apply the expected face visibility on {ownerName}.");
        }
    }

    private static async Task<ProgressBarVerification> VerifyProgressBarAsync(
        FrameworkElement themeRoot,
        EtherProgressBar defaultProgressBar,
        EtherProgressBar progressBar)
    {
        defaultProgressBar.ApplyTemplate();
        progressBar.ApplyTemplate();
        progressBar.UpdateLayout();

        if (defaultProgressBar.Minimum != 0d || defaultProgressBar.Maximum != 100d ||
            defaultProgressBar.Value != 0d || !defaultProgressBar.ShowTitle || !defaultProgressBar.ShowValue ||
            defaultProgressBar.Template is null)
        {
            throw new InvalidOperationException("The keyed EtherProgressBar style did not apply its default range, labels, and template.");
        }

        var labelRow = GetTemplatePart<Grid>(progressBar, "LabelRow");
        var titleText = GetTemplatePart<ContentPresenter>(progressBar, "TitleText");
        var valueLabel = GetTemplatePart<ContentPresenter>(progressBar, "ValueLabel");
        var track = GetTemplatePart<Grid>(progressBar, "ProgressTrack");
        var fill = GetTemplatePart<Border>(progressBar, "ProgressFill");
        var trackBackground = GetTemplatePart<Border>(progressBar, "ProgressTrackBackground");
        var fillGrid = fill.Parent as Grid
            ?? throw new InvalidOperationException("EtherProgressBar fill was not hosted by its two-column template grid.");
        if (fillGrid.ColumnDefinitions.Count != 2)
        {
            throw new InvalidOperationException("EtherProgressBar did not create FillColumn and RestColumn template columns.");
        }

        var templateParts = new[] { "FillColumn", "RestColumn", "LabelRow", "TitleText", "ValueLabel" };
        var labelStates = new List<string>();
        AssertLabelState(progressBar, labelRow, titleText, valueLabel, true, true, "BothLabelsVisible", labelStates);
        AssertLabelState(progressBar, labelRow, titleText, valueLabel, true, false, "TitleOnly", labelStates);
        AssertLabelState(progressBar, labelRow, titleText, valueLabel, false, true, "ValueOnly", labelStates);
        AssertLabelState(progressBar, labelRow, titleText, valueLabel, false, false, "LabelsHidden", labelStates);
        progressBar.ShowTitle = true;
        progressBar.ShowValue = true;

        progressBar.Value = 65d;
        progressBar.UpdateLayout();
        if (track.ActualWidth <= 0d || fill.ActualWidth <= 0d)
        {
            throw new InvalidOperationException("The progress-bar template did not receive a measurable track and fill layout.");
        }

        var fillRatio = fill.ActualWidth / track.ActualWidth;
        if (Math.Abs(fillRatio - 0.65d) > 0.02d || fillGrid.ColumnDefinitions[0].ActualWidth <= 0d || fillGrid.ColumnDefinitions[1].ActualWidth <= 0d)
        {
            throw new InvalidOperationException($"Expected a 65% progress fill, but observed {fillRatio:P2}.");
        }

        var peer = FrameworkElementAutomationPeer.CreatePeerForElement(progressBar)
            ?? throw new InvalidOperationException("EtherProgressBar did not create an automation peer.");
        var rangeValue = peer.GetPattern(PatternInterface.RangeValue) as IRangeValueProvider
            ?? throw new InvalidOperationException("EtherProgressBar automation peer GetPattern(RangeValue) did not return IRangeValueProvider.");
        if (peer is not IRangeValueProvider)
        {
            throw new InvalidOperationException("EtherProgressBar automation peer did not expose IRangeValueProvider.");
        }

        var fallbackPeer = FrameworkElementAutomationPeer.CreatePeerForElement(defaultProgressBar)
            ?? throw new InvalidOperationException("Default EtherProgressBar did not create an automation peer.");

        if (peer.GetAutomationControlType() != Microsoft.UI.Xaml.Automation.Peers.AutomationControlType.ProgressBar ||
            peer.GetClassName() != nameof(EtherProgressBar) ||
            peer.GetName() != "Package download progress" ||
            fallbackPeer.GetName() != "Default package progress" ||
            !rangeValue.IsReadOnly || !double.IsNaN(rangeValue.SmallChange) || !double.IsNaN(rangeValue.LargeChange) ||
            rangeValue.Minimum != 0d || rangeValue.Maximum != 100d || rangeValue.Value != 65d)
        {
            throw new InvalidOperationException("EtherProgressBar did not expose the required read-only ProgressBar RangeValue automation contract.");
        }

        var setValueRejected = false;
        try
        {
            rangeValue.SetValue(12d);
        }
        catch (InvalidOperationException)
        {
            setValueRejected = true;
        }

        if (!setValueRejected || progressBar.Value != 65d)
        {
            throw new InvalidOperationException("The read-only progress bar accepted a UI Automation SetValue request.");
        }

        // WinUI AutomationPeer has no public in-process subscription for
        // RangeValuePatternIdentifiers.ValueProperty (RaisePropertyChangedEvent is protected;
        // ListenerExists only reports out-of-process UIA clients). Observe the live GetPattern
        // provider Value instead of hardcoding the marker after assignments.
        progressBar.Value = 64d;
        var observedAfterFirstChange = rangeValue.Value;
        progressBar.Value = 65d;
        var observedAfterSecondChange = rangeValue.Value;
        var valueChangeExercised = observedAfterFirstChange == 64d && observedAfterSecondChange == 65d;
        if (!valueChangeExercised)
        {
            throw new InvalidOperationException("The RangeValue provider from GetPattern did not track owner Value assignments.");
        }

        var lightGradient = await GetProgressGradientAsync(themeRoot, progressBar, fill, ElementTheme.Light);
        var darkGradient = await GetProgressGradientAsync(themeRoot, progressBar, fill, ElementTheme.Dark);
        var lightTemplateBrushColors = await GetProgressTemplateBrushColorsAsync(
            themeRoot, progressBar, trackBackground, titleText, valueLabel, ElementTheme.Light);
        var darkTemplateBrushColors = await GetProgressTemplateBrushColorsAsync(
            themeRoot, progressBar, trackBackground, titleText, valueLabel, ElementTheme.Dark);
        AssertGradient(lightGradient, "Light");
        AssertGradient(darkGradient, "Dark");
        if (lightTemplateBrushColors.SequenceEqual(darkTemplateBrushColors, StringComparer.Ordinal))
        {
            throw new InvalidOperationException("EtherProgressBar Light and Dark template brushes did not re-resolve to distinct values.");
        }

        return new ProgressBarVerification(
            templateParts,
            labelStates.ToArray(),
            fillRatio,
            peer.GetName(),
            fallbackPeer.GetName(),
            peer.GetClassName(),
            peer.GetAutomationControlType().ToString(),
            rangeValue.IsReadOnly,
            double.IsNaN(rangeValue.SmallChange),
            double.IsNaN(rangeValue.LargeChange),
            rangeValue.Minimum,
            rangeValue.Maximum,
            rangeValue.Value,
            setValueRejected,
            valueChangeExercised,
            lightGradient,
            darkGradient,
            lightTemplateBrushColors,
            darkTemplateBrushColors);
    }

    private static void AssertLabelState(
        EtherProgressBar progressBar,
        FrameworkElement labelRow,
        FrameworkElement titleText,
        FrameworkElement valueLabel,
        bool showTitle,
        bool showValue,
        string expectedState,
        List<string> observedStates)
    {
        progressBar.ShowTitle = showTitle;
        progressBar.ShowValue = showValue;

        if (labelRow.Visibility != (showTitle || showValue ? Visibility.Visible : Visibility.Collapsed) ||
            titleText.Visibility != (showTitle ? Visibility.Visible : Visibility.Collapsed) ||
            valueLabel.Visibility != (showValue ? Visibility.Visible : Visibility.Collapsed))
        {
            throw new InvalidOperationException($"The '{expectedState}' LabelStates transition did not apply the expected visibility.");
        }

        observedStates.Add(expectedState);
    }

    private static async Task<SteeringBarVerification> VerifySteeringBarAsync(
        FrameworkElement themeRoot,
        EtherSteeringBar defaultSteeringBar,
        EtherSteeringBar steeringBar)
    {
        defaultSteeringBar.ApplyTemplate();
        steeringBar.ApplyTemplate();
        defaultSteeringBar.UpdateLayout();
        steeringBar.UpdateLayout();

        var defaultStyleResolved = defaultSteeringBar.Minimum == 0d &&
            defaultSteeringBar.Maximum == 100d &&
            defaultSteeringBar.Value == 0d &&
            defaultSteeringBar.ShowTitle &&
            defaultSteeringBar.ShowValue &&
            !defaultSteeringBar.UseSystemFocusVisuals &&
            defaultSteeringBar.IsTabStop &&
            defaultSteeringBar.Template is not null;
        if (!defaultStyleResolved)
        {
            throw new InvalidOperationException("The keyed EtherSteeringBar style did not apply its default range, labels, UseSystemFocusVisuals=False, IsTabStop, and template.");
        }

        var interactionSurface = GetTemplatePart<Grid>(steeringBar, "InteractionSurface", nameof(EtherSteeringBar));
        var fillBorder = GetTemplatePart<Border>(steeringBar, "FillBorder", nameof(EtherSteeringBar));
        GetTemplatePart<Grid>(steeringBar, "ThumbHost", nameof(EtherSteeringBar));
        var labelRow = GetTemplatePart<Grid>(steeringBar, "LabelRow", nameof(EtherSteeringBar));
        var titleText = GetTemplatePart<ContentPresenter>(steeringBar, "TitleText", nameof(EtherSteeringBar));
        var valueLabel = GetTemplatePart<ContentPresenter>(steeringBar, "ValueLabel", nameof(EtherSteeringBar));
        var trackBackground = GetTemplatePart<Border>(steeringBar, "TrackBackground", nameof(EtherSteeringBar));

        var templateParts = new[] { "InteractionSurface", "FillBorder", "ThumbHost", "LabelRow", "TitleText", "ValueLabel" };
        var labelStates = new List<string>();
        AssertSteeringBarLabelState(steeringBar, labelRow, titleText, valueLabel, true, true, "BothLabelsVisible", labelStates);
        AssertSteeringBarLabelState(steeringBar, labelRow, titleText, valueLabel, true, false, "TitleOnly", labelStates);
        AssertSteeringBarLabelState(steeringBar, labelRow, titleText, valueLabel, false, true, "ValueOnly", labelStates);
        AssertSteeringBarLabelState(steeringBar, labelRow, titleText, valueLabel, false, false, "LabelsHidden", labelStates);
        steeringBar.ShowTitle = true;
        steeringBar.ShowValue = true;

        steeringBar.Value = 65d;
        steeringBar.UpdateLayout();
        if (interactionSurface.ActualWidth <= 0d || fillBorder.ActualWidth <= 0d)
        {
            throw new InvalidOperationException("The steering-bar template did not receive a measurable track and fill layout.");
        }

        var fillRatio = fillBorder.ActualWidth / interactionSurface.ActualWidth;
        if (Math.Abs(fillRatio - 0.65d) > 0.02d)
        {
            throw new InvalidOperationException($"Expected a 65% steering-bar fill, but observed {fillRatio:P2}.");
        }

        var peer = FrameworkElementAutomationPeer.CreatePeerForElement(steeringBar)
            ?? throw new InvalidOperationException("EtherSteeringBar did not create an automation peer.");
        var rangeValue = peer.GetPattern(PatternInterface.RangeValue) as IRangeValueProvider
            ?? throw new InvalidOperationException("EtherSteeringBar automation peer GetPattern(RangeValue) did not return IRangeValueProvider.");
        if (peer is not IRangeValueProvider)
        {
            throw new InvalidOperationException("EtherSteeringBar automation peer did not expose IRangeValueProvider.");
        }

        var fallbackPeer = FrameworkElementAutomationPeer.CreatePeerForElement(defaultSteeringBar)
            ?? throw new InvalidOperationException("Default EtherSteeringBar did not create an automation peer.");

        if (peer.GetAutomationControlType() != Microsoft.UI.Xaml.Automation.Peers.AutomationControlType.Slider ||
            peer.GetClassName() != nameof(EtherSteeringBar) ||
            peer.GetName() != "Package steering bar" ||
            fallbackPeer.GetName() != "Default package steering bar" ||
            rangeValue.IsReadOnly ||
            rangeValue.SmallChange != 1d ||
            rangeValue.LargeChange != 10d ||
            rangeValue.Minimum != 0d ||
            rangeValue.Maximum != 100d ||
            rangeValue.Value != 65d)
        {
            throw new InvalidOperationException("EtherSteeringBar did not expose the required interactive Slider RangeValue automation contract.");
        }

        rangeValue.SetValue(70d);
        var setValueAccepted = steeringBar.Value == 70d;
        if (!setValueAccepted)
        {
            throw new InvalidOperationException("The interactive steering bar rejected a UI Automation SetValue request.");
        }

        steeringBar.Value = 64d;
        var observedAfterFirstChange = rangeValue.Value;
        steeringBar.Value = 65d;
        var observedAfterSecondChange = rangeValue.Value;
        var valueChangeExercised = observedAfterFirstChange == 64d && observedAfterSecondChange == 65d;
        if (!valueChangeExercised)
        {
            throw new InvalidOperationException("The RangeValue provider from GetPattern did not track owner Value assignments.");
        }

        steeringBar.PreviewStatus = SteeringBarPreviewStatus.Default;
        var previewStatusLocksAutomation = false;
        try
        {
            rangeValue.SetValue(12d);
        }
        catch (InvalidOperationException)
        {
            previewStatusLocksAutomation = steeringBar.Value == 65d && rangeValue.IsReadOnly;
        }

        if (!previewStatusLocksAutomation)
        {
            throw new InvalidOperationException("PreviewStatus did not lock the steering bar RangeValue provider.");
        }

        steeringBar.PreviewStatus = SteeringBarPreviewStatus.None;

        var lightGradient = await GetSteeringBarGradientAsync(themeRoot, steeringBar, fillBorder, ElementTheme.Light);
        var darkGradient = await GetSteeringBarGradientAsync(themeRoot, steeringBar, fillBorder, ElementTheme.Dark);
        var lightTemplateBrushColors = await GetSteeringBarTemplateBrushColorsAsync(
            themeRoot, steeringBar, trackBackground, titleText, valueLabel, ElementTheme.Light);
        var darkTemplateBrushColors = await GetSteeringBarTemplateBrushColorsAsync(
            themeRoot, steeringBar, trackBackground, titleText, valueLabel, ElementTheme.Dark);
        AssertSteeringBarGradient(lightGradient, "Light");
        AssertSteeringBarGradient(darkGradient, "Dark");
        if (lightTemplateBrushColors.SequenceEqual(darkTemplateBrushColors, StringComparer.Ordinal))
        {
            throw new InvalidOperationException("EtherSteeringBar Light and Dark template brushes did not re-resolve to distinct values.");
        }

        return new SteeringBarVerification(
            defaultStyleResolved,
            defaultSteeringBar.UseSystemFocusVisuals,
            templateParts,
            labelStates.ToArray(),
            fillRatio,
            peer.GetName(),
            fallbackPeer.GetName(),
            peer.GetClassName(),
            peer.GetAutomationControlType().ToString(),
            rangeValue.IsReadOnly,
            rangeValue.SmallChange,
            rangeValue.LargeChange,
            rangeValue.Minimum,
            rangeValue.Maximum,
            rangeValue.Value,
            setValueAccepted,
            previewStatusLocksAutomation,
            valueChangeExercised,
            lightGradient,
            darkGradient,
            lightTemplateBrushColors,
            darkTemplateBrushColors);
    }

    private static async Task<SliderVerification> VerifySliderAsync(
        FrameworkElement themeRoot,
        EtherSlider defaultSlider,
        EtherSlider slider)
    {
        defaultSlider.ApplyTemplate();
        slider.ApplyTemplate();
        defaultSlider.UpdateLayout();
        slider.UpdateLayout();

        var defaultStyleResolved = defaultSlider.Minimum == 0d &&
            defaultSlider.Maximum == 100d &&
            defaultSlider.Value == 50d &&
            defaultSlider.SmallChange == 1d &&
            defaultSlider.LargeChange == 10d &&
            !defaultSlider.UseSystemFocusVisuals &&
            defaultSlider.IsTabStop &&
            defaultSlider.Template is not null;
        if (!defaultStyleResolved)
        {
            throw new InvalidOperationException("The keyed EtherSlider style did not apply its default range, change steps, UseSystemFocusVisuals=False, IsTabStop, and template.");
        }

        var valueText = GetTemplatePart<TextBlock>(slider, "ValueText", nameof(EtherSlider));
        var barCanvas = GetTemplatePart<Canvas>(slider, "BarCanvas", nameof(EtherSlider));
        var templateParts = new[] { "ValueText", "BarCanvas" };

        slider.Value = 65d;
        slider.UpdateLayout();
        var highlightedBarCount = CountHighlightedBars(barCanvas);
        var fillRatio = highlightedBarCount / 63d;
        if (Math.Abs(fillRatio - 0.65d) > 0.02d || highlightedBarCount <= 0)
        {
            throw new InvalidOperationException($"Expected a 65% slider fill across 63 bars, but observed {highlightedBarCount} highlighted bars ({fillRatio:P2}).");
        }

        var peer = FrameworkElementAutomationPeer.CreatePeerForElement(slider)
            ?? throw new InvalidOperationException("EtherSlider did not create an automation peer.");
        var rangeValue = peer.GetPattern(PatternInterface.RangeValue) as IRangeValueProvider
            ?? throw new InvalidOperationException("EtherSlider automation peer GetPattern(RangeValue) did not return IRangeValueProvider.");
        if (peer is not IRangeValueProvider)
        {
            throw new InvalidOperationException("EtherSlider automation peer did not expose IRangeValueProvider.");
        }

        var defaultPeer = FrameworkElementAutomationPeer.CreatePeerForElement(defaultSlider)
            ?? throw new InvalidOperationException("Default EtherSlider did not create an automation peer.");

        if (peer.GetAutomationControlType() != Microsoft.UI.Xaml.Automation.Peers.AutomationControlType.Slider ||
            peer.GetClassName() != nameof(EtherSlider) ||
            peer.GetName() != "Package slider" ||
            defaultPeer.GetName() != "Default package slider" ||
            rangeValue.IsReadOnly ||
            rangeValue.SmallChange != 1d ||
            rangeValue.LargeChange != 10d ||
            rangeValue.Minimum != 0d ||
            rangeValue.Maximum != 100d ||
            rangeValue.Value != 65d)
        {
            throw new InvalidOperationException("EtherSlider did not expose the required interactive Slider RangeValue automation contract.");
        }

        rangeValue.SetValue(70d);
        var setValueAccepted = slider.Value == 70d;
        if (!setValueAccepted)
        {
            throw new InvalidOperationException("The interactive slider rejected a UI Automation SetValue request.");
        }

        slider.Value = 64d;
        var observedAfterFirstChange = rangeValue.Value;
        slider.Value = 65d;
        var observedAfterSecondChange = rangeValue.Value;
        var valueChangeExercised = observedAfterFirstChange == 64d && observedAfterSecondChange == 65d;
        if (!valueChangeExercised)
        {
            throw new InvalidOperationException("The RangeValue provider from GetPattern did not track owner Value assignments.");
        }

        var formattedValue = slider.FormatValue(slider.Value);
        if (formattedValue != "65")
        {
            throw new InvalidOperationException($"EtherSlider.FormatValue did not return the expected public-API string. Observed '{formattedValue}'.");
        }

        slider.IsEnabled = false;
        var disabledLocksAutomation = false;
        try
        {
            rangeValue.SetValue(12d);
        }
        catch (InvalidOperationException)
        {
            disabledLocksAutomation = slider.Value == 65d && rangeValue.IsReadOnly;
        }

        if (!disabledLocksAutomation)
        {
            throw new InvalidOperationException("Disabled EtherSlider did not lock the RangeValue provider.");
        }

        slider.IsEnabled = true;

        var lightTemplateBrushColors = await GetSliderTemplateBrushColorsAsync(themeRoot, slider, valueText, barCanvas, ElementTheme.Light);
        var darkTemplateBrushColors = await GetSliderTemplateBrushColorsAsync(themeRoot, slider, valueText, barCanvas, ElementTheme.Dark);
        if (lightTemplateBrushColors.SequenceEqual(darkTemplateBrushColors, StringComparer.Ordinal))
        {
            throw new InvalidOperationException("EtherSlider Light and Dark template brushes did not re-resolve to distinct values.");
        }

        return new SliderVerification(
            defaultStyleResolved,
            defaultSlider.UseSystemFocusVisuals,
            templateParts,
            fillRatio,
            highlightedBarCount,
            peer.GetName(),
            defaultPeer.GetName(),
            peer.GetClassName(),
            peer.GetAutomationControlType().ToString(),
            rangeValue.IsReadOnly,
            rangeValue.SmallChange,
            rangeValue.LargeChange,
            rangeValue.Minimum,
            rangeValue.Maximum,
            rangeValue.Value,
            setValueAccepted,
            disabledLocksAutomation,
            valueChangeExercised,
            formattedValue,
            lightTemplateBrushColors,
            darkTemplateBrushColors);
    }

    private static async Task<MastheadVerification> VerifyMastheadAsync(
        FrameworkElement themeRoot,
        EtherMasthead defaultMasthead,
        EtherMasthead masthead)
    {
        defaultMasthead.ApplyTemplate();
        masthead.ApplyTemplate();
        defaultMasthead.UpdateLayout();
        masthead.UpdateLayout();

        var defaultStyleResolved = defaultMasthead.ShowSettings &&
            !defaultMasthead.ShowSearch &&
            !defaultMasthead.ShowMenuIcon &&
            !defaultMasthead.ShowChevron &&
            !defaultMasthead.UseSystemFocusVisuals &&
            defaultMasthead.Template is not null;
        if (!defaultStyleResolved)
        {
            throw new InvalidOperationException("The keyed EtherMasthead style did not apply its default optional-icon metadata, UseSystemFocusVisuals=False, and template.");
        }

        var defaultSearchSlot = GetTemplatePart<Grid>(defaultMasthead, "SearchIconSlot", nameof(EtherMasthead));
        var defaultSettingsSlot = GetTemplatePart<Grid>(defaultMasthead, "SettingsButton", nameof(EtherMasthead));
        GetTemplatePart<EtherButton>(defaultMasthead, "MinimizeButton", nameof(EtherMasthead));
        GetTemplatePart<EtherButton>(defaultMasthead, "MaximizeRestoreButton", nameof(EtherMasthead));
        GetTemplatePart<EtherButton>(defaultMasthead, "CloseButton", nameof(EtherMasthead));

        var searchSlot = GetTemplatePart<Grid>(masthead, "SearchIconSlot", nameof(EtherMasthead));
        var settingsSlot = GetTemplatePart<Grid>(masthead, "SettingsButton", nameof(EtherMasthead));
        var templateParts = new[] { "SearchIconSlot", "SettingsButton", "MinimizeButton", "MaximizeRestoreButton", "CloseButton" };

        if (defaultSearchSlot.Visibility != Visibility.Collapsed || defaultSettingsSlot.Visibility != Visibility.Visible)
        {
            throw new InvalidOperationException("Default EtherMasthead did not apply SearchCollapsed and SettingsVisible.");
        }

        var optionalIconStates = new List<string>();
        if (searchSlot.Visibility != Visibility.Collapsed)
        {
            throw new InvalidOperationException("The 'SearchCollapsed' SearchIconStates transition did not hide the search slot.");
        }

        optionalIconStates.Add("SearchCollapsed");
        masthead.ShowSearch = true;
        masthead.UpdateLayout();
        if (searchSlot.Visibility != Visibility.Visible)
        {
            throw new InvalidOperationException("The 'SearchVisible' SearchIconStates transition did not show the search slot.");
        }

        optionalIconStates.Add("SearchVisible");

        var peer = FrameworkElementAutomationPeer.CreatePeerForElement(masthead)
            ?? throw new InvalidOperationException("EtherMasthead did not create an automation peer.");
        var defaultPeer = FrameworkElementAutomationPeer.CreatePeerForElement(defaultMasthead)
            ?? throw new InvalidOperationException("Default EtherMasthead did not create an automation peer.");
        if (peer.GetName() != "Package masthead" || defaultPeer.GetName() != "Default package masthead")
        {
            throw new InvalidOperationException("EtherMasthead gallery-equivalent automation names were not applied on the fixture instances.");
        }

        var settingsIcon = GetNamedSlotIcon(settingsSlot, "settings");
        var lightTemplateBrushColors = await GetMastheadTemplateBrushColorsAsync(themeRoot, masthead, settingsIcon, ElementTheme.Light);
        var darkTemplateBrushColors = await GetMastheadTemplateBrushColorsAsync(themeRoot, masthead, settingsIcon, ElementTheme.Dark);
        if (lightTemplateBrushColors.SequenceEqual(darkTemplateBrushColors, StringComparer.Ordinal))
        {
            throw new InvalidOperationException("EtherMasthead Light and Dark template brushes did not re-resolve to distinct values.");
        }

        return new MastheadVerification(
            defaultStyleResolved,
            defaultMasthead.UseSystemFocusVisuals,
            defaultMasthead.ShowSettings,
            defaultMasthead.ShowSearch,
            templateParts,
            optionalIconStates.ToArray(),
            searchSlot.Visibility == Visibility.Visible,
            peer.GetName(),
            defaultPeer.GetName(),
            lightTemplateBrushColors,
            darkTemplateBrushColors);
    }

    private static async Task<ToggleSwitchVerification> VerifyToggleSwitchAsync(
        FrameworkElement themeRoot,
        ToggleSwitch bareToggleSwitch,
        ToggleSwitch defaultToggleSwitch,
        ToggleSwitch toggleSwitch)
    {
        defaultToggleSwitch.ApplyTemplate();
        toggleSwitch.ApplyTemplate();
        bareToggleSwitch.ApplyTemplate();
        defaultToggleSwitch.UpdateLayout();
        toggleSwitch.UpdateLayout();
        bareToggleSwitch.UpdateLayout();

        if (!Application.Current.Resources.TryGetValue("EtherSwitch", out var keyedStyleObject) ||
            keyedStyleObject is not Style keyedStyle ||
            keyedStyle.TargetType != typeof(ToggleSwitch))
        {
            throw new InvalidOperationException("The keyed EtherSwitch style was not resolved from the Controls public merge graph.");
        }

        var keyedStyleResolved = defaultToggleSwitch.Style is not null &&
            ReferenceEquals(defaultToggleSwitch.Style, keyedStyle) &&
            ReferenceEquals(toggleSwitch.Style, keyedStyle) &&
            !defaultToggleSwitch.UseSystemFocusVisuals &&
            defaultToggleSwitch.Template is not null &&
            toggleSwitch.Template is not null &&
            TryGetNamedDescendant<Border>(defaultToggleSwitch, "KnobFill");
        if (!keyedStyleResolved)
        {
            throw new InvalidOperationException("The keyed EtherSwitch style did not apply UseSystemFocusVisuals=False and EtherSwitchTemplate.");
        }

        var implicitStyleRejected = !TryGetNamedDescendant<Border>(bareToggleSwitch, "KnobFill") &&
            !TryGetNamedDescendant<Border>(bareToggleSwitch, "TrackOn");
        if (!implicitStyleRejected)
        {
            throw new InvalidOperationException("A bare ToggleSwitch received EtherSwitch chrome; the style must stay keyed, not implicit.");
        }

        var trackOff = GetTemplatePart<Border>(toggleSwitch, "TrackOff", "EtherSwitch");
        GetTemplatePart<Border>(toggleSwitch, "TrackOn", "EtherSwitch");
        GetTemplatePart<Border>(toggleSwitch, "KnobFill", "EtherSwitch");
        GetTemplatePart<Border>(defaultToggleSwitch, "TrackOff", "EtherSwitch");
        var templateParts = new[] { "TrackOff", "TrackOn", "KnobFill" };

        var toggleStates = new List<string>();
        defaultToggleSwitch.IsOn = false;
        defaultToggleSwitch.UpdateLayout();
        var defaultTrackOff = GetTemplatePart<Border>(defaultToggleSwitch, "TrackOff", "EtherSwitch");
        var defaultTrackOn = GetTemplatePart<Border>(defaultToggleSwitch, "TrackOn", "EtherSwitch");
        if (defaultTrackOff.Visibility != Visibility.Visible || defaultTrackOn.Visibility != Visibility.Collapsed)
        {
            throw new InvalidOperationException("EtherSwitch Off ToggleStates did not show TrackOff and collapse TrackOn.");
        }

        toggleStates.Add("Off");
        toggleSwitch.IsOn = true;
        toggleSwitch.UpdateLayout();
        var onTrackOff = GetTemplatePart<Border>(toggleSwitch, "TrackOff", "EtherSwitch");
        var onTrackOn = GetTemplatePart<Border>(toggleSwitch, "TrackOn", "EtherSwitch");
        if (onTrackOff.Visibility != Visibility.Collapsed || onTrackOn.Visibility != Visibility.Visible)
        {
            throw new InvalidOperationException("EtherSwitch On ToggleStates did not show TrackOn and collapse TrackOff.");
        }

        toggleStates.Add("On");

        toggleSwitch.IsEnabled = false;
        toggleSwitch.UpdateLayout();
        VisualStateManager.GoToState(toggleSwitch, "Disabled", false);
        var disabledTrackOpacityApplied = Math.Abs(onTrackOff.Opacity - 0.4d) < 0.01d;
        if (!disabledTrackOpacityApplied)
        {
            throw new InvalidOperationException("Disabled EtherSwitch did not fade TrackOff to 0.4 opacity while keeping the knob opaque.");
        }

        toggleSwitch.IsEnabled = true;
        toggleSwitch.IsOn = false;
        toggleSwitch.UpdateLayout();

        var peer = FrameworkElementAutomationPeer.CreatePeerForElement(toggleSwitch)
            ?? throw new InvalidOperationException("EtherSwitch-styled ToggleSwitch did not create an automation peer.");
        var defaultPeer = FrameworkElementAutomationPeer.CreatePeerForElement(defaultToggleSwitch)
            ?? throw new InvalidOperationException("Default EtherSwitch-styled ToggleSwitch did not create an automation peer.");
        if (peer.GetName() != "Package toggle switch" || defaultPeer.GetName() != "Default package toggle switch")
        {
            throw new InvalidOperationException("EtherSwitch fixture automation names were not applied.");
        }

        var lightTemplateBrushColors = await GetToggleSwitchTemplateBrushColorsAsync(themeRoot, toggleSwitch, trackOff, ElementTheme.Light);
        var darkTemplateBrushColors = await GetToggleSwitchTemplateBrushColorsAsync(themeRoot, toggleSwitch, trackOff, ElementTheme.Dark);
        if (lightTemplateBrushColors.SequenceEqual(darkTemplateBrushColors, StringComparer.Ordinal))
        {
            throw new InvalidOperationException("EtherSwitch Light and Dark template brushes did not re-resolve to distinct values.");
        }

        return new ToggleSwitchVerification(
            keyedStyleResolved,
            implicitStyleRejected,
            defaultToggleSwitch.UseSystemFocusVisuals,
            templateParts,
            toggleStates.ToArray(),
            disabledTrackOpacityApplied,
            peer.GetName(),
            defaultPeer.GetName(),
            lightTemplateBrushColors,
            darkTemplateBrushColors);
    }

    private static async Task<ScrollBarVerification> VerifyScrollBarAsync(
        FrameworkElement themeRoot,
        ScrollBar scrollBar,
        ScrollViewer scrollViewer)
    {
        scrollBar.ApplyTemplate();
        scrollViewer.ApplyTemplate();
        scrollViewer.UpdateLayout();
        scrollBar.UpdateLayout();

        var verticalRoot = GetTemplatePart<Grid>(scrollBar, "VerticalRoot", "EtherScrollBar");
        var horizontalRoot = GetTemplatePart<Grid>(scrollBar, "HorizontalRoot", "EtherScrollBar");
        var verticalThumb = GetTemplatePart<Thumb>(scrollBar, "VerticalThumb", "EtherScrollBar");
        GetTemplatePart<Thumb>(scrollBar, "HorizontalThumb", "EtherScrollBar");
        var verticalSmallDecrease = GetTemplatePart<RepeatButton>(scrollBar, "VerticalSmallDecrease", "EtherScrollBar");
        var verticalSmallIncrease = GetTemplatePart<RepeatButton>(scrollBar, "VerticalSmallIncrease", "EtherScrollBar");
        verticalThumb.ApplyTemplate();
        verticalThumb.UpdateLayout();
        GetTemplatePart<Border>(verticalThumb, "ThumbFill", "EtherScrollBar");

        var implicitStyleApplied = scrollBar.Template is not null &&
            verticalRoot.Width == 6d &&
            horizontalRoot.Height == 6d;
        if (!implicitStyleApplied)
        {
            throw new InvalidOperationException("The implicit Ether ScrollBar style did not apply its 6 px vertical and horizontal templates.");
        }

        var viewerScrollBar = FindDescendant<ScrollBar>(scrollViewer);
        if (viewerScrollBar is null || viewerScrollBar.Template is null)
        {
            throw new InvalidOperationException("ScrollViewer did not receive an implicitly styled ScrollBar.");
        }

        var arrowsCollapsed = verticalSmallDecrease.Visibility == Visibility.Collapsed &&
            verticalSmallIncrease.Visibility == Visibility.Collapsed;
        if (!arrowsCollapsed)
        {
            throw new InvalidOperationException("Ether ScrollBar stepper arrows were visible.");
        }

        var peer = FrameworkElementAutomationPeer.CreatePeerForElement(scrollBar)
            ?? throw new InvalidOperationException("Implicit ScrollBar did not create an automation peer.");
        if (peer.GetName() != "Package scroll bar")
        {
            throw new InvalidOperationException("Implicit ScrollBar fixture automation name was not applied.");
        }

        var lightTemplateBrushColors = await GetScrollBarTemplateBrushColorsAsync(themeRoot, scrollBar, verticalThumb, ElementTheme.Light);
        var darkTemplateBrushColors = await GetScrollBarTemplateBrushColorsAsync(themeRoot, scrollBar, verticalThumb, ElementTheme.Dark);
        if (lightTemplateBrushColors.SequenceEqual(darkTemplateBrushColors, StringComparer.Ordinal))
        {
            throw new InvalidOperationException("EtherScrollBar Light and Dark thumb brushes did not re-resolve to distinct values.");
        }

        return new ScrollBarVerification(
            implicitStyleApplied,
            true,
            true,
            verticalRoot.Width,
            verticalThumb.MinHeight,
            arrowsCollapsed,
            peer.GetName(),
            lightTemplateBrushColors,
            darkTemplateBrushColors);
    }

    private static int CountHighlightedBars(Canvas canvas)
    {
        var knob = canvas.Children.OfType<Microsoft.UI.Xaml.Shapes.Rectangle>().FirstOrDefault(rectangle => rectangle.Height == 45d)
            ?? throw new InvalidOperationException("EtherSlider BarCanvas did not contain a knob rectangle.");
        var knobLeft = Canvas.GetLeft(knob);
        return canvas.Children.OfType<Microsoft.UI.Xaml.Shapes.Rectangle>().Count(rectangle => rectangle.Height == 40d && Canvas.GetLeft(rectangle) < knobLeft);
    }

    private static void AssertSteeringBarLabelState(
        EtherSteeringBar steeringBar,
        FrameworkElement labelRow,
        FrameworkElement titleText,
        FrameworkElement valueLabel,
        bool showTitle,
        bool showValue,
        string expectedState,
        List<string> observedStates)
    {
        steeringBar.ShowTitle = showTitle;
        steeringBar.ShowValue = showValue;

        if (labelRow.Visibility != (showTitle || showValue ? Visibility.Visible : Visibility.Collapsed) ||
            titleText.Visibility != (showTitle ? Visibility.Visible : Visibility.Collapsed) ||
            valueLabel.Visibility != (showValue ? Visibility.Visible : Visibility.Collapsed))
        {
            throw new InvalidOperationException($"The '{expectedState}' LabelStates transition did not apply the expected visibility on EtherSteeringBar.");
        }

        observedStates.Add(expectedState);
    }

    private static T GetTemplatePart<T>(FrameworkElement root, string name, string ownerName = "EtherProgressBar")
        where T : class
    {
        if (root is T rootPart && root.Name == name)
        {
            return rootPart;
        }

        for (var index = 0; index < VisualTreeHelper.GetChildrenCount(root); index++)
        {
            if (VisualTreeHelper.GetChild(root, index) is FrameworkElement child)
            {
                try
                {
                    return GetTemplatePart<T>(child, name, ownerName);
                }
                catch (InvalidOperationException)
                {
                    // Search the next branch of the visual tree.
                }
            }
        }

        throw new InvalidOperationException($"{ownerName} template part '{name}' was missing or had the wrong type.");
    }

    private static async Task<string[]> GetProgressGradientAsync(
        FrameworkElement themeRoot,
        EtherProgressBar progressBar,
        Border fill,
        ElementTheme theme)
    {
        themeRoot.RequestedTheme = theme;
        await WaitForAppliedThemeAsync(themeRoot, theme);
        progressBar.UpdateLayout();

        if (fill.Background is not LinearGradientBrush brush)
        {
            throw new InvalidOperationException($"EtherProgressBar fill did not resolve a LinearGradientBrush in {theme} theme.");
        }

        return brush.GradientStops.Select(stop => FormatColor(stop.Color)).ToArray();
    }

    private static void AssertGradient(IReadOnlyList<string> colors, string theme)
    {
        if (!colors.SequenceEqual(ExpectedProgressGradient, StringComparer.Ordinal))
        {
            throw new InvalidOperationException(
                $"EtherProgressBar {theme} gradient did not re-resolve to the expected component resources: {string.Join(", ", colors)}.");
        }
    }

    private static async Task<string[]> GetProgressTemplateBrushColorsAsync(
        FrameworkElement themeRoot,
        EtherProgressBar progressBar,
        Border trackBackground,
        ContentPresenter titleText,
        ContentPresenter valueLabel,
        ElementTheme theme)
    {
        themeRoot.RequestedTheme = theme;
        await WaitForAppliedThemeAsync(themeRoot, theme);
        progressBar.UpdateLayout();

        return new[]
        {
            GetSolidBrushColor(trackBackground.Background, "track", theme),
            GetSolidBrushColor(titleText.Foreground, "title", theme),
            GetSolidBrushColor(valueLabel.Foreground, "value", theme),
        };
    }

    private static async Task<string[]> GetButtonTemplateBrushColorsAsync(
        FrameworkElement themeRoot,
        EtherButton button,
        Border background,
        ElementTheme theme)
    {
        themeRoot.RequestedTheme = theme;
        await WaitForAppliedThemeAsync(themeRoot, theme);
        button.UpdateLayout();

        return new[]
        {
            GetSolidBrushColor(background.Background, "secondary background", theme, nameof(EtherButton)),
            GetSolidBrushColor(background.BorderBrush, "secondary border", theme, nameof(EtherButton)),
        };
    }

    private static async Task<string[]> GetToggleLabelBrushColorsAsync(
        FrameworkElement themeRoot,
        FrameworkElement control,
        ContentPresenter label,
        ElementTheme theme,
        string ownerName)
    {
        themeRoot.RequestedTheme = theme;
        await WaitForAppliedThemeAsync(themeRoot, theme);
        control.UpdateLayout();

        return new[]
        {
            GetSolidBrushColor(label.Foreground, "label", theme, ownerName),
        };
    }

    private static async Task<string[]> GetInputPlaceholderBrushColorsAsync(
        FrameworkElement themeRoot,
        EtherInput input,
        ContentControl placeholder,
        ElementTheme theme)
    {
        themeRoot.RequestedTheme = theme;
        await WaitForAppliedThemeAsync(themeRoot, theme);
        input.UpdateLayout();

        return new[]
        {
            GetSolidBrushColor(placeholder.Foreground, "placeholder", theme, nameof(EtherInput)),
        };
    }

    private static async Task<string[]> GetDropdownForegroundBrushColorsAsync(
        FrameworkElement themeRoot,
        EtherDropdown dropdown,
        TextBlock triggerText,
        ElementTheme theme)
    {
        themeRoot.RequestedTheme = theme;
        await WaitForAppliedThemeAsync(themeRoot, theme);
        dropdown.UpdateLayout();

        return new[]
        {
            GetSolidBrushColor(triggerText.Foreground, "trigger foreground", theme, nameof(EtherDropdown)),
        };
    }

    private static async Task<string[]> GetSegmentedControlTrackBrushColorsAsync(
        FrameworkElement themeRoot,
        EtherSegmentedControl segmentedControl,
        Border trackSurface,
        ElementTheme theme)
    {
        themeRoot.RequestedTheme = theme;
        await WaitForAppliedThemeAsync(themeRoot, theme);
        segmentedControl.UpdateLayout();

        return new[]
        {
            GetSolidBrushColor(trackSurface.Background, "track background", theme, nameof(EtherSegmentedControl)),
        };
    }

    private static async Task<string[]> GetIntelligenceButtonForegroundBrushColorsAsync(
        FrameworkElement themeRoot,
        EtherIntelligenceButton intelligenceButton,
        ElementTheme theme)
    {
        themeRoot.RequestedTheme = theme;
        await WaitForAppliedThemeAsync(themeRoot, theme);
        intelligenceButton.UpdateLayout();
        var content = GetTemplatePart<ContentPresenter>(intelligenceButton, "Cp", nameof(EtherIntelligenceButton));

        return new[]
        {
            GetSolidBrushColor(content.Foreground, "label", theme, nameof(EtherIntelligenceButton)),
        };
    }

    private static async Task<string[]> GetSteeringBarGradientAsync(
        FrameworkElement themeRoot,
        EtherSteeringBar steeringBar,
        Border fill,
        ElementTheme theme)
    {
        themeRoot.RequestedTheme = theme;
        await WaitForAppliedThemeAsync(themeRoot, theme);
        steeringBar.UpdateLayout();

        if (fill.Background is not LinearGradientBrush brush)
        {
            throw new InvalidOperationException($"EtherSteeringBar fill did not resolve a LinearGradientBrush in {theme} theme.");
        }

        return brush.GradientStops.Select(stop => FormatColor(stop.Color)).ToArray();
    }

    private static void AssertSteeringBarGradient(IReadOnlyList<string> colors, string theme)
    {
        if (!colors.SequenceEqual(ExpectedProgressGradient, StringComparer.Ordinal))
        {
            throw new InvalidOperationException(
                $"EtherSteeringBar {theme} gradient did not re-resolve to the expected component resources: {string.Join(", ", colors)}.");
        }
    }

    private static async Task<string[]> GetSteeringBarTemplateBrushColorsAsync(
        FrameworkElement themeRoot,
        EtherSteeringBar steeringBar,
        Border trackBackground,
        ContentPresenter titleText,
        ContentPresenter valueLabel,
        ElementTheme theme)
    {
        themeRoot.RequestedTheme = theme;
        await WaitForAppliedThemeAsync(themeRoot, theme);
        steeringBar.UpdateLayout();

        return new[]
        {
            GetSolidBrushColor(trackBackground.Background, "track", theme, nameof(EtherSteeringBar)),
            GetSolidBrushColor(titleText.Foreground, "title", theme, nameof(EtherSteeringBar)),
            GetSolidBrushColor(valueLabel.Foreground, "value", theme, nameof(EtherSteeringBar)),
        };
    }

    private static async Task<string[]> GetSliderTemplateBrushColorsAsync(
        FrameworkElement themeRoot,
        EtherSlider slider,
        TextBlock valueText,
        Canvas barCanvas,
        ElementTheme theme)
    {
        themeRoot.RequestedTheme = theme;
        await WaitForAppliedThemeAsync(themeRoot, theme);
        var deadline = DateTime.UtcNow.AddSeconds(5);
        while (slider.ActualTheme != theme && DateTime.UtcNow < deadline)
        {
            await Task.Delay(25);
        }

        slider.UpdateLayout();
        var inactiveBar = barCanvas.Children.OfType<Microsoft.UI.Xaml.Shapes.Rectangle>().LastOrDefault(rectangle => rectangle.Height == 40d)
            ?? throw new InvalidOperationException($"EtherSlider did not generate an inactive bar in {theme} theme.");

        return new[]
        {
            GetSolidBrushColor(inactiveBar.Fill, "inactive bar", theme, nameof(EtherSlider)),
            GetSolidBrushColor(valueText.Foreground, "value", theme, nameof(EtherSlider)),
        };
    }

    private static FontIcon GetNamedSlotIcon(Grid slot, string role)
    {
        for (var index = 0; index < VisualTreeHelper.GetChildrenCount(slot); index++)
        {
            if (VisualTreeHelper.GetChild(slot, index) is FontIcon icon)
            {
                return icon;
            }
        }

        throw new InvalidOperationException($"EtherMasthead {role} slot did not contain a FontIcon.");
    }

    private static async Task<string[]> GetMastheadTemplateBrushColorsAsync(
        FrameworkElement themeRoot,
        EtherMasthead masthead,
        FontIcon settingsIcon,
        ElementTheme theme)
    {
        themeRoot.RequestedTheme = theme;
        await WaitForAppliedThemeAsync(themeRoot, theme);
        var deadline = DateTime.UtcNow.AddSeconds(5);
        while (masthead.ActualTheme != theme && DateTime.UtcNow < deadline)
        {
            await Task.Delay(25);
        }

        masthead.UpdateLayout();
        return new[]
        {
            GetSolidBrushColor(settingsIcon.Foreground, "settings icon", theme, nameof(EtherMasthead)),
        };
    }

    private static bool TryGetNamedDescendant<T>(FrameworkElement root, string name)
        where T : class
    {
        try
        {
            _ = GetTemplatePart<T>(root, name, "probe");
            return true;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }

    private static T? FindDescendant<T>(DependencyObject root)
        where T : DependencyObject
    {
        if (root is T match)
        {
            return match;
        }

        for (var index = 0; index < VisualTreeHelper.GetChildrenCount(root); index++)
        {
            var found = FindDescendant<T>(VisualTreeHelper.GetChild(root, index));
            if (found is not null)
            {
                return found;
            }
        }

        return null;
    }

    private static async Task<string[]> GetToggleSwitchTemplateBrushColorsAsync(
        FrameworkElement themeRoot,
        ToggleSwitch toggleSwitch,
        Border trackOff,
        ElementTheme theme)
    {
        themeRoot.RequestedTheme = theme;
        await WaitForAppliedThemeAsync(themeRoot, theme);
        var deadline = DateTime.UtcNow.AddSeconds(5);
        while (toggleSwitch.ActualTheme != theme && DateTime.UtcNow < deadline)
        {
            await Task.Delay(25);
        }

        toggleSwitch.UpdateLayout();
        return new[]
        {
            GetSolidBrushColor(trackOff.Background, "off track", theme, "EtherSwitch"),
        };
    }

    private static async Task<string[]> GetScrollBarTemplateBrushColorsAsync(
        FrameworkElement themeRoot,
        ScrollBar scrollBar,
        Thumb verticalThumb,
        ElementTheme theme)
    {
        themeRoot.RequestedTheme = theme;
        scrollBar.RequestedTheme = theme;
        await WaitForAppliedThemeAsync(themeRoot, theme);
        var deadline = DateTime.UtcNow.AddSeconds(5);
        while (scrollBar.ActualTheme != theme && DateTime.UtcNow < deadline)
        {
            await Task.Delay(25);
        }

        verticalThumb.ApplyTemplate();
        scrollBar.UpdateLayout();
        var thumbFill = GetTemplatePart<Border>(verticalThumb, "ThumbFill", "EtherScrollBar");
        return new[]
        {
            GetSolidBrushColor(thumbFill.Background, "thumb", theme, "EtherScrollBar"),
        };
    }

    private static string GetSolidBrushColor(Brush? brush, string role, ElementTheme theme, string ownerName = "EtherProgressBar")
    {
        if (brush is not SolidColorBrush solidColorBrush)
        {
            throw new InvalidOperationException($"{ownerName} {role} brush did not resolve to a SolidColorBrush in {theme} theme.");
        }

        return FormatColor(solidColorBrush.Color);
    }

    private static string AssertResource(string key, Type expectedType)
    {
        if (!Application.Current.Resources.TryGetValue(key, out var value) || !expectedType.IsInstanceOfType(value))
        {
            throw new InvalidOperationException(
                $"Expected resource '{key}' of type '{expectedType.Name}' was not resolved from the Controls public merge graph.");
        }

        return key;
    }

    private static async Task<AssetVerification> AssertApplicationFileAsync(string uri)
    {
        var storageFileResolved = false;
        string? storageFileError = null;
        try
        {
            var file = await StorageFile.GetFileFromApplicationUriAsync(new Uri(uri));
            var properties = await file.GetBasicPropertiesAsync();
            if (properties.Size == 0)
            {
                throw new InvalidOperationException($"Application asset URI '{uri}' resolved to an empty file.");
            }

            storageFileResolved = true;
        }
        catch (ArgumentException exception)
        {
            // Windows.Storage does not resolve ms-appx host-root content for an
            // unpackaged WinUI host. Retain the exact diagnostic in the marker and
            // prove the same URI through XAML font/SVG loading below.
            storageFileError = exception.Message;
        }

        var relativePath = Uri.UnescapeDataString(new Uri(uri).AbsolutePath.TrimStart('/'))
            .Replace('/', Path.DirectorySeparatorChar);
        var outputPath = Path.Combine(AppContext.BaseDirectory, relativePath);
        var fileInfo = new FileInfo(outputPath);
        if (!fileInfo.Exists || fileInfo.Length == 0)
        {
            throw new InvalidOperationException($"Application asset URI '{uri}' has no non-empty host output file at '{outputPath}'.");
        }

        return new AssetVerification(uri, checked((ulong)fileInfo.Length), storageFileResolved, storageFileError);
    }

    private static async Task AssertSvgImageLoadedAsync(Image image, string uri)
    {
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        if (image.Source is not Microsoft.UI.Xaml.Media.Imaging.SvgImageSource source)
        {
            throw new InvalidOperationException("The visible SVG proof Image is not backed by SvgImageSource.");
        }

        source.Opened += (_, _) => completion.TrySetResult();
        source.OpenFailed += (_, args) => completion.TrySetException(new InvalidOperationException($"SVG load status: {args.Status}"));
        var completed = await Task.WhenAny(completion.Task, Task.Delay(TimeSpan.FromSeconds(5)));
        if (completed != completion.Task)
        {
            throw new InvalidOperationException($"SVG ImageSource did not load '{uri}' within five seconds.");
        }

        await completion.Task;
    }

    private static async Task<Windows.UI.Color> GetBackgroundColorAsync(FrameworkElement themeRoot, ElementTheme theme)
    {
        themeRoot.RequestedTheme = theme;
        await WaitForAppliedThemeAsync(themeRoot, theme);
        if (themeRoot is not Panel { Background: SolidColorBrush brush })
        {
            throw new InvalidOperationException("Fixture theme root does not expose a SolidColorBrush BackgroundCanvas value.");
        }

        return brush.Color;
    }

    private static async Task WaitForAppliedThemeAsync(FrameworkElement themeRoot, ElementTheme requestedTheme)
    {
        var deadline = DateTime.UtcNow.AddSeconds(5);
        while (themeRoot.ActualTheme != requestedTheme && DateTime.UtcNow < deadline)
        {
            await Task.Delay(25);
        }

        if (themeRoot.ActualTheme != requestedTheme)
        {
            throw new InvalidOperationException(
                $"Theme application timed out. Requested '{requestedTheme}', actual '{themeRoot.ActualTheme}'.");
        }
    }

    private static string FormatColor(Windows.UI.Color color) =>
        $"#{color.A:X2}{color.R:X2}{color.G:X2}{color.B:X2}";
}
