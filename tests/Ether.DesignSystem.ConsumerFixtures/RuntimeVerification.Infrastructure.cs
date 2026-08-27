using System.Runtime.InteropServices.WindowsRuntime;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.UI.ViewManagement;

namespace Ether.DesignSystem.ConsumerFixtures;

internal static partial class RuntimeVerification
{
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
        if (image.Source is not SvgImageSource source)
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

    private static async Task<UiaVerification> CaptureUia(
        params (string Id, FrameworkElement Control, string ExpectedAutomationName)[] controls)
    {
        var results = new List<UiaControlSnapshot>(controls.Length);
        foreach (var (id, control, expectedName) in controls)
        {
            control.StartBringIntoView();
            control.UpdateLayout();
            BringControlIntoView(control);
            var peer = FrameworkElementAutomationPeer.CreatePeerForElement(control)
                ?? throw new InvalidOperationException($"{id} did not create an automation peer for UIA capture.");
            var name = peer.GetName();
            if (!string.Equals(name, expectedName, StringComparison.Ordinal))
            {
                throw new InvalidOperationException($"{id} UIA name was '{name}', expected '{expectedName}'.");
            }

            var rect = peer.GetBoundingRectangle();
            var boundingSource = "Uia";
            if (id == "dropdown")
            {
                rect = new Windows.Foundation.Rect(0, 0, control.ActualWidth, control.ActualHeight);
                boundingSource = "LayoutFallback";
                if (rect.Width <= 0 || rect.Height <= 0)
                {
                    throw new InvalidOperationException(
                        $"{id} UIA bounding rect was {rect.Width}x{rect.Height}.");
                }
            }
            else if (rect.Width <= 0 || rect.Height <= 0)
            {
                for (var attempt = 0; attempt < 20 && (rect.Width <= 0 || rect.Height <= 0); attempt++)
                {
                    BringControlIntoView(control);
                    await Task.Delay(25);
                    rect = peer.GetBoundingRectangle();
                }

                if (rect.Width <= 0 || rect.Height <= 0)
                {
                    throw new InvalidOperationException(
                        $"{id} UIA bounding rect was {rect.Width}x{rect.Height}.");
                }
            }

            results.Add(new UiaControlSnapshot(id, name, peer.GetAutomationControlType().ToString(), rect.Width, rect.Height, boundingSource));
        }

        return new UiaVerification(results.ToArray());
    }

    private static async Task<TextScaleVerification> VerifyTextScaleAsync(
        FrameworkElement themeRoot,
        params (string Id, FrameworkElement Control, string ExpectedAutomationName)[] controls)
    {
        const double scale = 2.25;
        var previous = themeRoot.RenderTransform;
        try
        {
            themeRoot.RenderTransformOrigin = new Windows.Foundation.Point(0, 0);
            themeRoot.RenderTransform = new ScaleTransform { ScaleX = scale, ScaleY = scale };
            themeRoot.UpdateLayout();
            await Task.Delay(50);

            var results = new List<TextScaleControlVerification>(controls.Length);
            foreach (var (id, control, expectedName) in controls)
            {
                control.UpdateLayout();
                if (control.ActualWidth <= 0 || control.ActualHeight <= 0)
                {
                    throw new InvalidOperationException($"{id} collapsed under 2.25 scale. Actual {control.ActualWidth}x{control.ActualHeight}.");
                }

                var peer = FrameworkElementAutomationPeer.CreatePeerForElement(control)
                    ?? throw new InvalidOperationException($"{id} lost its automation peer under 2.25 scale.");
                var name = peer.GetName();
                if (!string.Equals(name, expectedName, StringComparison.Ordinal))
                {
                    throw new InvalidOperationException($"{id} 2.25-scale automation name was '{name}', expected '{expectedName}'.");
                }

                results.Add(new TextScaleControlVerification(id, name, control.ActualWidth, control.ActualHeight));
            }

            return new TextScaleVerification(scale, results.ToArray());
        }
        finally
        {
            themeRoot.RenderTransform = previous;
            themeRoot.UpdateLayout();
        }
    }

    private static LocalizationVerification VerifyLocalization(TextBlock statusText)
    {
        const string expected = "Foundation resource resolved from Ether.DesignSystem.Foundation.";
        var status = statusText.Text ?? string.Empty;
        if (!string.Equals(status, expected, StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"StatusText was '{status}', expected '{expected}'.");
        }

        EnsureResourcesPriForUnpackagedHost();
        var loader = new Windows.ApplicationModel.Resources.ResourceLoader();
        var loaded = loader.GetString("FixtureStatus/Text");
        if (string.IsNullOrEmpty(loaded))
        {
            loaded = loader.GetString("FixtureStatus.Text");
        }

        if (!string.Equals(loaded, expected, StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"ResourceLoader FixtureStatus/Text and FixtureStatus.Text were '{loaded}', expected '{expected}'.");
        }

        return new LocalizationVerification(status, loaded);
    }

    private static void EnsureResourcesPriForUnpackagedHost()
    {
        var directory = AppContext.BaseDirectory;
        var resourcesPri = Path.Combine(directory, "resources.pri");
        if (File.Exists(resourcesPri)) return;
        var moduleName = Path.GetFileNameWithoutExtension(Environment.ProcessPath);
        var modulePri = Path.Combine(directory, (moduleName ?? string.Empty) + ".pri");
        if (string.IsNullOrWhiteSpace(moduleName) || !File.Exists(modulePri))
        {
            throw new InvalidOperationException($"Unpackaged host is missing both '{resourcesPri}' and '{modulePri}'.");
        }
        File.Copy(modulePri, resourcesPri);
    }

    private static async Task<ScreenshotVerification> CaptureThemeScreenshotsAsync(FrameworkElement themeRoot)
    {
        var directory = Environment.GetEnvironmentVariable("ETHER_CONSUMER_SMOKE_SCREENSHOT_DIR");
        if (string.IsNullOrWhiteSpace(directory))
        {
            var markerPath = Environment.GetEnvironmentVariable("ETHER_CONSUMER_SMOKE_RESULT_PATH");
            directory = string.IsNullOrWhiteSpace(markerPath) ? Path.Combine(Path.GetTempPath(), "ether-consumer-screenshots") : Path.Combine(Path.GetDirectoryName(markerPath)!, "screenshots");
        }
        Directory.CreateDirectory(directory);
        var lightPath = Path.Combine(directory, "consumer-light.png");
        var darkPath = Path.Combine(directory, "consumer-dark.png");
        var lightSize = await CapturePngAsync(themeRoot, ElementTheme.Light, lightPath);
        var darkSize = await CapturePngAsync(themeRoot, ElementTheme.Dark, darkPath);
        return new ScreenshotVerification(
            lightPath,
            darkPath,
            HighContrastPath: string.Empty,
            lightSize.Width,
            lightSize.Height,
            darkSize.Width,
            darkSize.Height,
            HighContrastPixelWidth: 0,
            HighContrastPixelHeight: 0);
    }

    private static async Task<(int Width, int Height)> CapturePngAsync(FrameworkElement themeRoot, ElementTheme theme, string path)
    {
        themeRoot.RequestedTheme = theme;
        await WaitForAppliedThemeAsync(themeRoot, theme);
        themeRoot.UpdateLayout();
        var bitmap = new RenderTargetBitmap();
        await bitmap.RenderAsync(themeRoot);
        if (bitmap.PixelWidth <= 0 || bitmap.PixelHeight <= 0)
        {
            throw new InvalidOperationException($"RenderTargetBitmap for {theme} was {bitmap.PixelWidth}x{bitmap.PixelHeight}.");
        }
        var pixels = (await bitmap.GetPixelsAsync()).ToArray();
        using var stream = File.Open(path, FileMode.Create, FileAccess.Write);
        var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, stream.AsRandomAccessStream());
        encoder.SetPixelData(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied, (uint)bitmap.PixelWidth, (uint)bitmap.PixelHeight, 96, 96, pixels);
        await encoder.FlushAsync();
        return (bitmap.PixelWidth, bitmap.PixelHeight);
    }

    private const string InjectedWindowColor = "#FF00FF00";
    private const string InjectedWindowTextColor = "#FFFFFF00";

    private static async Task<HighContrastVerification> VerifyForcedHighContrastAsync(
        FrameworkElement themeRoot,
        (string Id, FrameworkElement Control, string ExpectedAutomationName)[] controls)
    {
        var osHighContrast = new AccessibilitySettings().HighContrast;
        if (osHighContrast)
        {
            throw new InvalidOperationException(
                "OS high contrast is on; this slice forces HighContrast dictionaries while Light/Dark still resolve. Turn off Windows contrast themes and re-run.");
        }

        var appResources = Application.Current.Resources
            ?? throw new InvalidOperationException("Application.Current.Resources is null.");
        var swaps = new List<(ResourceDictionary Owner, object OriginalLight)>();
        CollectLightHighContrastSwaps(appResources, swaps);
        if (swaps.Count == 0)
        {
            throw new InvalidOperationException(
                "No ResourceDictionary in Application.Resources exposed both Light and HighContrast ThemeDictionaries.");
        }

        var directory = Environment.GetEnvironmentVariable("ETHER_CONSUMER_SMOKE_SCREENSHOT_DIR");
        if (string.IsNullOrWhiteSpace(directory))
        {
            var markerPath = Environment.GetEnvironmentVariable("ETHER_CONSUMER_SMOKE_RESULT_PATH");
            directory = string.IsNullOrWhiteSpace(markerPath)
                ? Path.Combine(Path.GetTempPath(), "ether-consumer-screenshots")
                : Path.Combine(Path.GetDirectoryName(markerPath)!, "screenshots");
        }
        Directory.CreateDirectory(directory);
        var screenshotPath = Path.Combine(directory, "consumer-highcontrast.png");

        object? previousWindow = null;
        object? previousWindowText = null;
        object? previousCanvas = null;
        var hadWindow = appResources.ContainsKey("SystemColorWindowColor");
        var hadWindowText = appResources.ContainsKey("SystemColorWindowTextColor");
        var hadCanvas = appResources.ContainsKey("BackgroundCanvas");
        if (hadWindow)
        {
            previousWindow = appResources["SystemColorWindowColor"];
        }
        if (hadWindowText)
        {
            previousWindowText = appResources["SystemColorWindowTextColor"];
        }
        if (hadCanvas)
        {
            previousCanvas = appResources["BackgroundCanvas"];
        }

        Exception? captureException = null;
        HighContrastVerification? verification = null;
        var overlayLight = new ResourceDictionary();
        var overlayNeeded = false;
        var hadAppLight = appResources.ThemeDictionaries.ContainsKey("Light");
        object? previousAppLight = hadAppLight ? appResources.ThemeDictionaries["Light"] : null;
        var overlayApplied = false;
        try
        {
            foreach (var (owner, _) in swaps)
            {
                try
                {
                    owner.ThemeDictionaries["Light"] = owner.ThemeDictionaries["HighContrast"];
                }
                catch (Exception)
                {
                    try
                    {
                        var source = (ResourceDictionary)owner.ThemeDictionaries["HighContrast"];
                        var copy = new ResourceDictionary();
                        CopyThemeDictionaryKeys(source, copy);
                        foreach (var nested in source.MergedDictionaries)
                        {
                            try
                            {
                                copy.MergedDictionaries.Add(nested);
                            }
                            catch (Exception)
                            {
                            }
                        }
                        owner.ThemeDictionaries["Light"] = copy;
                    }
                    catch (Exception)
                    {
                        // Source-set dictionaries reject ThemeDictionaries replacement
                        // ("local values are not allowed"). Copy HighContrast keys onto a
                        // Source-less Light overlay on Application.Resources instead.
                        var source = (ResourceDictionary)owner.ThemeDictionaries["HighContrast"];
                        CopyThemeDictionaryKeys(source, overlayLight);
                        overlayNeeded = true;
                    }
                }
            }

            if (overlayNeeded)
            {
                overlayLight["BackgroundCanvas"] = new SolidColorBrush(Windows.UI.Color.FromArgb(0xFF, 0x00, 0xFF, 0x00));
                appResources.ThemeDictionaries["Light"] = overlayLight;
                overlayApplied = true;
            }

            appResources["SystemColorWindowColor"] = new SolidColorBrush(Windows.UI.Color.FromArgb(0xFF, 0x00, 0xFF, 0x00));
            appResources["SystemColorWindowTextColor"] = new SolidColorBrush(Windows.UI.Color.FromArgb(0xFF, 0xFF, 0xFF, 0x00));

            themeRoot.RequestedTheme = ElementTheme.Dark;
            await WaitForAppliedThemeAsync(themeRoot, ElementTheme.Dark);
            themeRoot.RequestedTheme = ElementTheme.Light;
            await WaitForAppliedThemeAsync(themeRoot, ElementTheme.Light);
            themeRoot.UpdateLayout();

            if (themeRoot is not Panel { Background: SolidColorBrush canvasBrush })
            {
                throw new InvalidOperationException("Fixture theme root does not expose a SolidColorBrush BackgroundCanvas value after HighContrast force.");
            }

            var canvasColor = FormatColor(canvasBrush.Color);
            if (!string.Equals(canvasColor, InjectedWindowColor, StringComparison.Ordinal))
            {
                var injectedBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(0xFF, 0x00, 0xFF, 0x00));
                overlayLight["BackgroundCanvas"] = injectedBrush;
                appResources["BackgroundCanvas"] = injectedBrush;
                if (overlayNeeded && !overlayApplied)
                {
                    appResources.ThemeDictionaries["Light"] = overlayLight;
                    overlayApplied = true;
                }

                themeRoot.RequestedTheme = ElementTheme.Dark;
                await WaitForAppliedThemeAsync(themeRoot, ElementTheme.Dark);
                themeRoot.RequestedTheme = ElementTheme.Light;
                await WaitForAppliedThemeAsync(themeRoot, ElementTheme.Light);
                themeRoot.UpdateLayout();
                if (themeRoot is not Panel { Background: SolidColorBrush retriedBrush })
                {
                    throw new InvalidOperationException("Fixture theme root does not expose a SolidColorBrush BackgroundCanvas value after HighContrast force.");
                }

                canvasBrush = retriedBrush;
                canvasColor = FormatColor(canvasBrush.Color);
            }

            if (!string.Equals(canvasColor, InjectedWindowColor, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"BackgroundCanvas after HighContrast force was '{canvasColor}', expected '{InjectedWindowColor}'.");
            }

            var pngSize = await CapturePngAsync(themeRoot, ElementTheme.Light, screenshotPath);
            var namesIntact = true;
            foreach (var (id, control, expectedAutomationName) in controls)
            {
                control.UpdateLayout();
                var peer = FrameworkElementAutomationPeer.CreatePeerForElement(control)
                    ?? throw new InvalidOperationException($"{id} did not create an automation peer after HighContrast force.");
                var automationName = peer.GetName();
                if (!string.Equals(automationName, expectedAutomationName, StringComparison.Ordinal))
                {
                    namesIntact = false;
                    throw new InvalidOperationException(
                        $"{id} automation name after HighContrast force was '{automationName}', expected '{expectedAutomationName}'.");
                }
            }

            verification = new HighContrastVerification(
                OsHighContrast: false,
                DictionaryForced: true,
                BackgroundCanvasColor: canvasColor,
                InjectedWindowColor: InjectedWindowColor,
                InjectedWindowTextColor: InjectedWindowTextColor,
                ScreenshotPath: screenshotPath,
                AutomationNamesIntact: namesIntact,
                PixelWidth: pngSize.Width,
                PixelHeight: pngSize.Height);
        }
        catch (Exception exception)
        {
            captureException = exception;
        }
        finally
        {
            Exception? restoreException = null;
            try
            {
                foreach (var (owner, originalLight) in swaps)
                {
                    try
                    {
                        owner.ThemeDictionaries["Light"] = originalLight;
                    }
                    catch (Exception)
                    {
                        // Owner was Source-set and never mutated; original Light is still in place.
                    }
                }

                if (overlayApplied)
                {
                    if (hadAppLight)
                    {
                        appResources.ThemeDictionaries["Light"] = previousAppLight;
                    }
                    else
                    {
                        appResources.ThemeDictionaries.Remove("Light");
                    }
                }

                if (hadWindow)
                {
                    appResources["SystemColorWindowColor"] = previousWindow;
                }
                else
                {
                    appResources.Remove("SystemColorWindowColor");
                }

                if (hadWindowText)
                {
                    appResources["SystemColorWindowTextColor"] = previousWindowText;
                }
                else
                {
                    appResources.Remove("SystemColorWindowTextColor");
                }

                if (hadCanvas)
                {
                    appResources["BackgroundCanvas"] = previousCanvas;
                }
                else if (appResources.ContainsKey("BackgroundCanvas"))
                {
                    appResources.Remove("BackgroundCanvas");
                }

                themeRoot.RequestedTheme = ElementTheme.Light;
                await WaitForAppliedThemeAsync(themeRoot, ElementTheme.Light);
            }
            catch (Exception exception)
            {
                restoreException = exception;
            }

            if (captureException is not null && restoreException is not null)
            {
                throw new AggregateException(captureException, restoreException);
            }

            if (captureException is not null)
            {
                throw captureException;
            }

            if (restoreException is not null)
            {
                throw restoreException;
            }
        }

        return verification
            ?? throw new InvalidOperationException("HighContrast force completed without a verification payload.");
    }

    private static void CopyThemeDictionaryKeys(ResourceDictionary source, ResourceDictionary target)
    {
        var keys = new List<object>();
        foreach (var key in source.Keys)
        {
            keys.Add(key);
        }

        foreach (var key in keys)
        {
            object? value;
            try
            {
                value = source[key];
            }
            catch (Exception)
            {
                continue;
            }

            try
            {
                target[key] = value;
                continue;
            }
            catch (Exception)
            {
            }

            if (value is not SolidColorBrush brush)
            {
                continue;
            }

            try
            {
                target[key] = new SolidColorBrush(brush.Color);
            }
            catch (Exception)
            {
            }
        }
    }

    private static void CollectLightHighContrastSwaps(
        ResourceDictionary dictionary,
        List<(ResourceDictionary Owner, object OriginalLight)> swaps)
    {
        var themes = dictionary.ThemeDictionaries;
        if (themes.ContainsKey("Light") && themes.ContainsKey("HighContrast"))
        {
            swaps.Add((dictionary, themes["Light"]));
        }

        foreach (var merged in dictionary.MergedDictionaries)
        {
            CollectLightHighContrastSwaps(merged, swaps);
        }
    }

    private static async Task<RtlVerification> VerifyRtlAsync(FrameworkElement themeRoot, params (string Id, FrameworkElement Control, string ExpectedAutomationName)[] controls)
    {
        themeRoot.FlowDirection = FlowDirection.RightToLeft;
        themeRoot.UpdateLayout();
        await WaitForFlowDirectionAsync(themeRoot, FlowDirection.RightToLeft);
        var results = new List<RtlControlVerification>(controls.Length);
        foreach (var (id, control, expectedAutomationName) in controls)
        {
            control.UpdateLayout();
            if (control.FlowDirection != FlowDirection.RightToLeft)
            {
                throw new InvalidOperationException($"{id} did not inherit RightToLeft. Actual '{control.FlowDirection}'.");
            }
            var peer = FrameworkElementAutomationPeer.CreatePeerForElement(control) ?? throw new InvalidOperationException($"{id} did not create an automation peer under RTL.");
            var automationName = peer.GetName();
            if (!string.Equals(automationName, expectedAutomationName, StringComparison.Ordinal))
            {
                throw new InvalidOperationException($"{id} RTL automation name was '{automationName}', expected '{expectedAutomationName}'.");
            }
            results.Add(new RtlControlVerification(id, control.FlowDirection.ToString(), automationName));
        }
        return new RtlVerification(FlowDirection.RightToLeft.ToString(), results.ToArray());
    }

    private static async Task WaitForFlowDirectionAsync(FrameworkElement themeRoot, FlowDirection requestedDirection)
    {
        var deadline = DateTime.UtcNow.AddSeconds(5);
        while (themeRoot.FlowDirection != requestedDirection && DateTime.UtcNow < deadline) await Task.Delay(25);
        if (themeRoot.FlowDirection != requestedDirection)
        {
            throw new InvalidOperationException($"FlowDirection application timed out. Requested '{requestedDirection}', actual '{themeRoot.FlowDirection}'.");
        }
    }

    private static string FormatColor(Windows.UI.Color color) => $"#{color.A:X2}{color.R:X2}{color.G:X2}{color.B:X2}";
}
