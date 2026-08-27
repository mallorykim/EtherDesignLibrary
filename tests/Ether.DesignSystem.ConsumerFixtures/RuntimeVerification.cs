using System.Diagnostics;
using System.Text.Json;
using EtherSandbox.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;

namespace Ether.DesignSystem.ConsumerFixtures;

internal static partial class RuntimeVerification
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
        bool ValuePropertyChangedSubscribed,
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

    internal sealed record RtlControlVerification(
        string Id,
        string FlowDirection,
        string AutomationName);

    internal sealed record RtlVerification(
        string RootFlowDirection,
        RtlControlVerification[] Controls);

    internal sealed record UiaControlSnapshot(
        string Id,
        string AutomationName,
        string ControlType,
        double BoundingWidth,
        double BoundingHeight,
        string BoundingSource);

    internal sealed record UiaVerification(UiaControlSnapshot[] Controls);

    internal sealed record TextScaleControlVerification(
        string Id,
        string AutomationName,
        double ActualWidth,
        double ActualHeight);

    internal sealed record TextScaleVerification(
        double Scale,
        TextScaleControlVerification[] Controls);

    internal sealed record LocalizationVerification(
        string StatusText,
        string ResourceLoaderText);

    internal sealed record ScreenshotVerification(
        string LightPath,
        string DarkPath,
        string HighContrastPath,
        int LightPixelWidth,
        int LightPixelHeight,
        int DarkPixelWidth,
        int DarkPixelHeight,
        int HighContrastPixelWidth,
        int HighContrastPixelHeight);

    internal sealed record HighContrastVerification(
        bool OsHighContrast,
        bool DictionaryForced,
        string BackgroundCanvasColor,
        string InjectedWindowColor,
        string InjectedWindowTextColor,
        string ScreenshotPath,
        bool AutomationNamesIntact,
        int PixelWidth,
        int PixelHeight);

    internal sealed record PerformanceVerification(double ElapsedMilliseconds);

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
        ScrollBarVerification ScrollBar,
        RtlVerification Rtl,
        UiaVerification Uia,
        TextScaleVerification TextScale,
        LocalizationVerification Localization,
        ScreenshotVerification Screenshots,
        HighContrastVerification HighContrast,
        PerformanceVerification Performance);

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
        ScrollViewer scrollViewer,
        TextBlock statusText)
    {
        var stopwatch = Stopwatch.StartNew();
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

        var controlsTuple = new (string Id, FrameworkElement Control, string ExpectedAutomationName)[]
        {
            ("progressBar", progressBar, "Package download progress"),
            ("button", button, "Package button"),
            ("checkbox", checkbox, "Package checkbox"),
            ("radioButton", radioButton, "Package radio button"),
            ("input", input, "Package input"),
            ("dropdown", dropdown, "Package dropdown"),
            ("segmentedControl", segmentedControl, "Package segmented control"),
            ("intelligenceButton", intelligenceButton, "Package intelligence button"),
            ("steeringBar", steeringBar, "Package steering bar"),
            ("slider", slider, "Package slider"),
            ("masthead", masthead, "Package masthead"),
            ("toggleSwitch", toggleSwitch, "Package toggle switch"),
            ("scrollBar", scrollBar, "Package scroll bar"),
        };

        var screenshots = await CaptureThemeScreenshotsAsync(themeRoot);
        var uia = await CaptureUia(controlsTuple);
        var textScale = await VerifyTextScaleAsync(themeRoot, controlsTuple);
        var localization = VerifyLocalization(statusText);
        var rtlResult = await VerifyRtlAsync(themeRoot, controlsTuple);
        var highContrast = await VerifyForcedHighContrastAsync(themeRoot, controlsTuple);
        screenshots = screenshots with
        {
            HighContrastPath = highContrast.ScreenshotPath,
            HighContrastPixelWidth = highContrast.PixelWidth,
            HighContrastPixelHeight = highContrast.PixelHeight,
        };

        stopwatch.Stop();
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
            scrollBarResult,
            rtlResult,
            uia,
            textScale,
            localization,
            screenshots,
            highContrast,
            new PerformanceVerification(stopwatch.Elapsed.TotalMilliseconds));
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
            rtl = result?.Rtl,
            uia = result?.Uia,
            textScale = result?.TextScale,
            localization = result?.Localization,
            screenshots = result?.Screenshots,
            highContrast = result?.HighContrast,
            performance = result?.Performance,
            message = exception?.ToString(),
        });
        File.WriteAllText(path, payload);
    }

}
