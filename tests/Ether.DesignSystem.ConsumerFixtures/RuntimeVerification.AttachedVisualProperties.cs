using System.Reflection;
using System.Runtime.InteropServices.WindowsRuntime;
using EtherSandbox.Controls;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;

namespace Ether.DesignSystem.ConsumerFixtures;

internal static partial class RuntimeVerification
{
    // This gate deliberately uses an attached element instead of a reflection-only object.
    // It proves that every property classified as visual can be set after the control has
    // loaded, participates in a fresh layout pass, and leaves a renderable WinUI visual.
    // Pixel-difference assertions remain component-specific because a platform property such
    // as HorizontalAlignment can legitimately preserve pixels for a full-width specimen.
    internal sealed record AttachedVisualPropertyVerification(
        int VisualPropertyCount,
        int AttachedMutationCount,
        int RenderedControlCount,
        int PixelChangedPropertyCount,
        int StateObservedPropertyCount,
        double ElapsedMilliseconds,
        string[] VerifiedProperties,
        string[] PixelChangedProperties,
        string[] PixelStableProperties,
        VisualPropertyEvidence[] Evidence);

    // A visible control property does not always produce a different bitmap for one isolated
    // specimen. For example, MinWidth may be lower than the arranged width, or Visibility must
    // be restored before the final render can be captured. Keep that distinction explicit in
    // the evidence rather than treating "set without an exception" as visual verification.
    internal sealed record VisualPropertyEvidence(
        string Property,
        string Method,
        string Observation,
        int BeforeConvergenceReads,
        int AfterConvergenceReads);

    private static async Task<AttachedVisualPropertyVerification> VerifyAttachedVisualPropertiesAsync(
        FrameworkElement themeRoot)
    {
        if (themeRoot is not Grid root)
        {
            throw new InvalidOperationException("The consumer fixture root must be a Grid for the attached visual-property audit host.");
        }

        var host = new Canvas
        {
            IsHitTestVisible = false,
        };
        Canvas.SetLeft(host, -1000d);
        root.Children.Add(host);

        var verified = new List<string>();
        var pixelChanged = new List<string>();
        var pixelStable = new List<string>();
        var evidence = new List<VisualPropertyEvidence>();
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var rendered = 0;
        try
        {
            foreach (var type in PublicPropertyInventoryTypes)
            {
                foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                             .Where(property => property.CanRead && property.CanWrite && property.GetIndexParameters().Length == 0)
                             .Where(property => VisualPropertyNames.Contains(property.Name, StringComparer.Ordinal))
                             .OrderBy(property => property.Name, StringComparer.Ordinal))
                {
                    if (CreateVisualFixture(type) is not FrameworkElement control)
                    {
                        throw new InvalidOperationException($"Could not create an attached visual fixture for {type.Name}.{property.Name}.");
                    }

                    var surface = new Border
                    {
                        Width = 420d,
                        Height = 180d,
                        Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 250, 250, 250)),
                        Child = control,
                    };
                    host.Children.Add(surface);
                    if (control is Control templatedControl)
                    {
                        templatedControl.ApplyTemplate();
                    }
                    control.UpdateLayout();
                    var beforeLayout = CaptureLayoutSnapshot(control, surface);
                    // Rendering the child itself avoids counting unrelated gallery pixels and
                    // catches values that make the control unload or collapse unexpectedly.
                    // Convergence, not a guessed duration, is what makes this deterministic: keep
                    // sampling a frame apart until the fingerprint itself stops changing, and throw
                    // rather than silently accept an unsettled reading if it never does.
                    var (before, beforeConvergenceReads) = await CaptureSettledVisualFingerprintAsync(control, surface, $"{type.Name}.{property.Name}:before");

                    var key = $"{type.Name}.{property.Name}";
                    var mutation = ApplyAttachedVisualMutation(control, property, key);
                    control.UpdateLayout();
                    var afterLayout = CaptureLayoutSnapshot(control, surface);
                    var (after, afterConvergenceReads) = await CaptureSettledVisualFingerprintAsync(control, surface, $"{key}:after");

                    rendered++;
                    verified.Add(key);
                    if (!AreVisuallyEquivalent(before, after, out var changedPixelCount))
                    {
                        pixelChanged.Add(key);
                        evidence.Add(new VisualPropertyEvidence(
                            key,
                            "pixel-difference",
                            $"The attached control's RenderTargetBitmap fingerprint changed after the property mutation ({changedPixelCount} pixel(s) exceeded the {ChannelToleranceLevels}-level channel tolerance; before converged after {beforeConvergenceReads} sample(s), after converged after {afterConvergenceReads} sample(s)).",
                            beforeConvergenceReads,
                            afterConvergenceReads));
                    }
                    else
                    {
                        pixelStable.Add(key);
                        evidence.Add(CreateStableVisualEvidence(control, property, key, mutation, beforeLayout, afterLayout, beforeConvergenceReads, afterConvergenceReads, changedPixelCount));
                    }
                    host.Children.Remove(surface);
                }
            }
        }
        finally
        {
            host.Children.Clear();
            root.Children.Remove(host);
        }

        var expected = ClassifyAllPublicProperties().VisualPropertyCount;
        if (verified.Count != expected || rendered != expected || evidence.Count != expected)
        {
            throw new InvalidOperationException($"Attached visual-property audit covered {verified.Count}/{rendered}/{evidence.Count} properties, expected {expected}.");
        }

        stopwatch.Stop();
        return new AttachedVisualPropertyVerification(
            expected, verified.Count, rendered, pixelChanged.Count, evidence.Count, stopwatch.Elapsed.TotalMilliseconds, verified.ToArray(),
            pixelChanged.ToArray(), pixelStable.ToArray(), evidence.ToArray());
    }

    // Determinism here is constructed, not guessed: instead of waiting a fixed duration and
    // hoping any running VisualTransition/animation/deferred layout pass has finished, keep
    // sampling the actual rendered bitmap fingerprint a composition frame apart until it stops
    // changing for RequiredStableFingerprintReadings consecutive samples. A candidate CubicEase
    // transition (EtherSwitch's Off<->On track is the longest declared, at 0:0:0.16) or a
    // deferred content-presenter re-measure (observed on EtherInput.Header/HeaderTemplate) both
    // show up here as the fingerprint changing on consecutive samples; once it stops changing,
    // that is the true final rendered state, independent of exactly how many frames or
    // milliseconds it took to get there. If a specimen never converges within
    // MaxFingerprintSettleAttempts samples, that is a real bug (an animation that never
    // terminates, or a template that keeps invalidating itself) and must fail loudly here rather
    // than silently falling back to a weaker evidence bucket, which is what produced the
    // between-run flakiness this replaces.
    //
    // 3 consecutive matches turned out not to be enough of a guard band: with the coarse 0xF0
    // channel mask this file used to hash pixels with (see AreVisuallyEquivalent's history),
    // that was invisible, because the mask also swallowed the real signal it was racing with.
    // Once the comparison became pixel-tolerant instead of hash-truncated (Task A), a
    // reproducible flip surfaced on EtherInput.FontFamily/HeaderTemplate specifically: TextBox's
    // text layout can go quiet — three or more identical frames in a row — before a deferred
    // font-substitution/re-layout pass actually lands, so a 3-frame settle window sometimes
    // locks onto that stale intermediate frame as "converged" instead of the true final one
    // (observed swinging between 0 and 132 changed pixels across otherwise-identical runs, i.e.
    // "did the mutation visually land at all", not a borderline tolerance/significance call).
    // Doubling the window to 6 consecutive matches cleared the previously observed TextBox
    // quiet gap. A later EtherInput.AcceptsReturn run showed that TextBox can still emit six
    // identical frames before its multiline re-layout lands, so EtherInput gets an 8-reading
    // window: seven readings are needed to observe the known late change and the eighth is a
    // one-frame guard band. Keep the established 6-reading window for every other control so
    // this targeted TextBox safeguard does not add two RenderTargetBitmap captures to both sides
    // of all 482 visual-property mutations.
    private const int RequiredStableFingerprintReadings = 6;
    private const int EtherInputRequiredStableFingerprintReadings = 8;
    private const int MaxFingerprintSettleAttempts = 60;

    // Tolerance and significance thresholds for treating two rendered bitmaps as "the same
    // visual". Calibrated empirically against this exact fixture set (see the Task A audit trail
    // in docs/handoff/2026-08-29-winui3-full-property-audit-handoff.md §10) by instrumenting
    // AreVisuallyEquivalent to also report the true zero-tolerance differing-byte count and the
    // largest single-channel delta, then comparing the two populations across a full run:
    //   - 188 of 217 properties with no template binding for the mutated value (booleans/enums
    //     with genuinely no visual effect, e.g. EtherSteeringBar.ShowStops/SnapToStops/Maximum)
    //     converged to a BYTE-IDENTICAL "before" and "after" bitmap: maxDelta 0, 0 differing bytes.
    //     Once CaptureSettledVisualFingerprintAsync has converged both samples, this fixture's
    //     RenderTargetBitmap output is fully reproducible — there is no measurable compositor
    //     dither left to tolerate at this stage of the pipeline.
    //   - Every genuine small-but-real visual change measured (recolored/re-fonted/re-toggled
    //     text and glyphs: EtherCheckbox/EtherRadioButton Content/ContentTemplate/FontFamily/
    //     FontStyle/FontWeight, EtherMasthead.Opacity/IsEnabled/Scale, EtherSlider.Labels/
    //     ShowLabels, EtherSteeringBar.Minimum/Title/ValueContent/FontStyle, ...) moved between 435
    //     and 9708 individual BGRA8 bytes, but by only 2-5 levels per channel — these are subpixel
    //     antialiasing/gamma-blend shifts on small glyph regions, not the large flat-color repaints
    //     larger mutations produce (Background/BorderBrush recolors moved pixels by 60-255 levels).
    // A single channel byte is therefore treated as changed once it moves by more than 1 level
    // (absorbs literal off-by-one rounding without needing to, since no rounding was observed) —
    // low enough to catch the smallest real signal seen (EtherMasthead.Opacity, delta 2).
    private const int ChannelToleranceLevels = 1;
    // A frame counts as visually different once more than this many pixels exceed the channel
    // tolerance. The observed populations are not close: the "no real effect" population sits at
    // exactly 0 changed pixels/bytes, while the smallest real signal (EtherMasthead.Opacity/
    // IsEnabled, 675 differing bytes) is already in the low hundreds of pixels. This floor sits
    // far above the former and far below the latter, so it is a significance gate against a
    // stray pixel or two, not a masking device.
    private const double SignificantPixelFraction = 0.0002d;
    private const int MinimumSignificantPixelCount = 12;

    private sealed record VisualSnapshot(byte[] Pixels, int Width, int Height);

    // Pixel-tolerant equality: true only when the two frames differ by no more than compositor
    // noise. changedPixelCount is always the exact count (no early exit) so callers can attach it
    // to evidence for calibration/diagnostics, not just a boolean verdict.
    private static bool AreVisuallyEquivalent(VisualSnapshot before, VisualSnapshot after) =>
        AreVisuallyEquivalent(before, after, out _);

    private static bool AreVisuallyEquivalent(VisualSnapshot before, VisualSnapshot after, out int changedPixelCount)
    {
        if (before.Width != after.Width || before.Height != after.Height ||
            before.Pixels.Length != after.Pixels.Length)
        {
            changedPixelCount = Math.Max(before.Width * before.Height, after.Width * after.Height);
            return false;
        }

        var pixelCount = before.Width * before.Height;
        if (pixelCount <= 0)
        {
            changedPixelCount = 0;
            return true;
        }

        var beforePixels = before.Pixels;
        var afterPixels = after.Pixels;
        var changedPixels = 0;
        for (var offset = 0; offset + 3 < beforePixels.Length; offset += 4)
        {
            if (Math.Abs(beforePixels[offset] - afterPixels[offset]) > ChannelToleranceLevels ||
                Math.Abs(beforePixels[offset + 1] - afterPixels[offset + 1]) > ChannelToleranceLevels ||
                Math.Abs(beforePixels[offset + 2] - afterPixels[offset + 2]) > ChannelToleranceLevels ||
                Math.Abs(beforePixels[offset + 3] - afterPixels[offset + 3]) > ChannelToleranceLevels)
            {
                changedPixels++;
            }
        }

        changedPixelCount = changedPixels;
        var significanceThreshold = Math.Max(MinimumSignificantPixelCount, (int)(pixelCount * SignificantPixelFraction));
        return changedPixels <= significanceThreshold;
    }

    private static Task<bool> WaitForCompositionFrameAsync()
    {
        var completion = new TaskCompletionSource<bool>();
        void OnRendering(object? sender, object e)
        {
            CompositionTarget.Rendering -= OnRendering;
            completion.TrySetResult(true);
        }

        CompositionTarget.Rendering += OnRendering;
        return completion.Task;
    }

    private static async Task<(VisualSnapshot Fingerprint, int ConvergenceReads)> CaptureSettledVisualFingerprintAsync(FrameworkElement control, FrameworkElement surface, string label)
    {
        var requiredStableReadings = control is EtherInput
            ? EtherInputRequiredStableFingerprintReadings
            : RequiredStableFingerprintReadings;
        var fingerprint = await CaptureVisualSnapshotAsync(surface, label);
        var stableReadings = 1;
        var samples = 1;
        while (stableReadings < requiredStableReadings)
        {
            if (samples >= MaxFingerprintSettleAttempts)
            {
                throw new InvalidOperationException(
                    $"{label} did not converge to a stable rendered fingerprint after {samples} samples " +
                    $"(reached {stableReadings}/{requiredStableReadings} required consecutive matches). " +
                    "This means the visual is still changing on its own — a running animation/transition that " +
                    "never terminates, or a template that keeps invalidating itself — rather than a timing gap " +
                    "in the audit; it must be fixed at the source, not waited out longer.");
            }

            await WaitForCompositionFrameAsync();
            control.UpdateLayout();
            var next = await CaptureVisualSnapshotAsync(surface, label);
            samples++;
            if (AreVisuallyEquivalent(fingerprint, next))
            {
                stableReadings++;
            }
            else
            {
                stableReadings = 1;
                fingerprint = next;
            }
        }

        return (fingerprint, samples);
    }

    private static async Task<VisualSnapshot> CaptureVisualSnapshotAsync(FrameworkElement control, string label)
    {
        var bitmap = new RenderTargetBitmap();
        await bitmap.RenderAsync(control);
        if (bitmap.PixelWidth <= 0 || bitmap.PixelHeight <= 0)
        {
            throw new InvalidOperationException($"{label} did not produce a renderable attached WinUI visual ({bitmap.PixelWidth}x{bitmap.PixelHeight}).");
        }

        // Keep the raw BGRA8 buffer rather than folding it into a hash: a hash (even of masked
        // pixels) can only ever say "identical" or "different", so telling compositor dither apart
        // from a genuine but localized visual change requires comparing the actual pixels with a
        // tolerance and a significance threshold (see AreVisuallyEquivalent), not truncating them
        // before the fact.
        var pixels = (await bitmap.GetPixelsAsync()).ToArray();
        return new VisualSnapshot(pixels, bitmap.PixelWidth, bitmap.PixelHeight);
    }

    private static FrameworkElement CreateVisualFixture(Type type)
    {
        // EtherSegmentedTrack is a source-compatibility subclass with no independent
        // template contract. WinUI's keyed ControlTemplate is targeted at the base type;
        // render inherited properties against that canonical template while the separate
        // public-property code gate still instantiates the compatibility type itself.
        if (type == typeof(EtherSegmentedTrack))
        {
            type = typeof(EtherSegmentedControl);
        }

        var control = Activator.CreateInstance(type) as FrameworkElement
            ?? throw new InvalidOperationException($"Could not instantiate {type.Name}.");

        // Give each specimen a constrained, non-stretched baseline. The previous 360 x 180
        // stretch baseline hid valid changes to alignment and min/max dimensions: a centered
        // full-width child has the same pixels as a left-aligned child. These dimensions leave
        // room for every mutation sample below to affect the actual arranged visual.
        control.Width = 160d;
        control.Height = 80d;
        control.HorizontalAlignment = HorizontalAlignment.Left;
        control.VerticalAlignment = VerticalAlignment.Top;

        switch (control)
        {
            case ContentControl contentControl:
                contentControl.Content = "Visual audit specimen";
                break;
            case TextBox textBox:
                textBox.Text = "Visual audit specimen";
                textBox.PlaceholderText = "Visual audit placeholder";
                // WinUI's TextBox runs spell-check/proofing on a background service that can
                // redraw squiggle decorations an unpredictable, cold-start-dependent amount of
                // time after the control is attached or its text/font changes — independent of
                // whether anything is actually misspelled. That was the source of EtherInput's
                // remaining before/after fingerprint flakiness (FontFamily, Header, IsReadOnly,
                // ...) surviving even a generous settle-and-poll wait: the settle loop can only
                // wait out a source of change it can observe, and a proofing pass that lands
                // after the loop already saw enough stable reads is invisible to it. Disabling
                // spell-check removes that async source entirely instead of trying to out-wait it.
                textBox.IsSpellCheckEnabled = false;
                break;
            case ComboBox comboBox:
                comboBox.SelectedValuePath = "Tag";
                comboBox.Items.Add(new ComboBoxItem { Content = "First visual choice", Tag = "first" });
                comboBox.Items.Add(new ComboBoxItem { Content = "Second visual choice", Tag = "second" });
                comboBox.SelectedIndex = 0;
                break;
            case RangeBase rangeBase:
                rangeBase.Minimum = 0d;
                rangeBase.Maximum = 100d;
                rangeBase.Value = 25d;
                break;
        }

        if (control is EtherSegmentedControl segmented)
        {
            segmented.Content = new EtherSegmentPanel
            {
                Children =
                {
                    new RadioButton { Content = "First", IsChecked = true },
                    new RadioButton { Content = "Second" },
                },
            };
        }
        else if (control is EtherSegmentPanel segmentPanel)
        {
            segmentPanel.Children.Add(new RadioButton { Content = "First", IsChecked = true });
            segmentPanel.Children.Add(new RadioButton { Content = "Second" });
        }

        return control;
    }

    private static AttachedVisualMutation ApplyAttachedVisualMutation(FrameworkElement control, PropertyInfo property, string key)
    {
        var current = property.GetValue(control);
        var value = CreateAttachedVisualSample(control, property, current, key);

        // Visibility needs both state transitions exercised, but the final state must be
        // visible so RenderTargetBitmap can prove the loaded control still renders.
        if (property.Name == nameof(UIElement.Visibility))
        {
            property.SetValue(control, Visibility.Collapsed);
            control.UpdateLayout();
            if (control.Visibility != Visibility.Collapsed)
            {
                throw new InvalidOperationException($"{key} did not enter the required Collapsed visual state.");
            }
            property.SetValue(control, Visibility.Visible);
            return new AttachedVisualMutation(value, true);
        }
        else
        {
            try
            {
                property.SetValue(control, value);
            }
            catch (Exception exception)
            {
                throw new InvalidOperationException($"{key} rejected attached visual mutation value '{value ?? "null"}' ({value?.GetType().FullName ?? "null"}) for property type {property.PropertyType.FullName}.", exception);
            }
        }

        if (!AttachedValuesMatch(value, property.GetValue(control), property.PropertyType) && property.Name != nameof(UIElement.Visibility))
        {
            throw new InvalidOperationException($"{key} did not round-trip its attached visual mutation through the CLR getter.");
        }

        return new AttachedVisualMutation(value, false);
    }

    private sealed record AttachedVisualMutation(object? Value, bool VisibilityTransitionObserved);

    private readonly record struct VisualLayoutSnapshot(
        double ActualWidth,
        double ActualHeight,
        double DesiredWidth,
        double DesiredHeight,
        double X,
        double Y);

    private static VisualLayoutSnapshot CaptureLayoutSnapshot(FrameworkElement control, FrameworkElement surface)
    {
        var origin = control.TransformToVisual(surface).TransformPoint(new Windows.Foundation.Point(0d, 0d));
        return new VisualLayoutSnapshot(
            control.ActualWidth,
            control.ActualHeight,
            control.DesiredSize.Width,
            control.DesiredSize.Height,
            origin.X,
            origin.Y);
    }

    private static VisualPropertyEvidence CreateStableVisualEvidence(
        FrameworkElement control,
        PropertyInfo property,
        string key,
        AttachedVisualMutation mutation,
        VisualLayoutSnapshot beforeLayout,
        VisualLayoutSnapshot afterLayout,
        int beforeConvergenceReads,
        int afterConvergenceReads,
        int changedPixelCount)
    {
        if (mutation.VisibilityTransitionObserved)
        {
            return new VisualPropertyEvidence(
                key,
                "visibility-transition",
                "Collapsed state was observed on the loaded control, then Visible was restored and rendered successfully.",
                beforeConvergenceReads,
                afterConvergenceReads);
        }

        var returned = property.GetValue(control);
        if (!AttachedValuesMatch(mutation.Value, returned, property.PropertyType))
        {
            throw new InvalidOperationException($"{key} lost its mutated visual value before the post-layout observation.");
        }

        // Verify the same state at the WinUI DP layer, not only through the CLR wrapper.
        // This distinguishes a real platform-consumable property from a wrapper that merely
        // stores a value in user code. It also covers compositor/layout DPs whose effect is
        // intentionally not distinguishable in a small, opaque bitmap specimen.
        var dependencyProperty = ResolveDependencyProperty(control.GetType(), property.Name);
        if (dependencyProperty is not null &&
            !AttachedValuesMatch(mutation.Value, control.GetValue(dependencyProperty), property.PropertyType))
        {
            throw new InvalidOperationException($"{key} did not retain its mutation through DependencyObject.GetValue.");
        }

        if (LayoutChanged(beforeLayout, afterLayout))
        {
            return new VisualPropertyEvidence(
                key,
                "layout-difference",
                $"The rendered bitmap was stable ({changedPixelCount} pixel(s) exceeded the {ChannelToleranceLevels}-level channel tolerance, at or below the significance threshold), but the attached control's layout changed from {FormatLayout(beforeLayout)} to {FormatLayout(afterLayout)}.",
                beforeConvergenceReads,
                afterConvergenceReads);
        }

        var dimensions = FormatLayout(afterLayout);
        // Every platform DP identifier this audit has needed so far resolves through
        // ResolveDependencyProperty below (checking both the static-field and static-property
        // shapes WinRT projections use — see its own comment). dependencyProperty is therefore
        // expected to always be non-null in current practice; "platform-clr-visual-contract" is
        // kept only as an honest label for the theoretical case where WinUI exposes a settable
        // visual CLR property with genuinely no backing DP of either shape, not as a description
        // of anything actually observed in this component set.
        var ownership = dependencyProperty is null
            ? "platform-clr-visual-contract"
            : property.DeclaringType?.Namespace == typeof(EtherButton).Namespace
                ? "ether-component-dp-contract"
                : "platform-dp-contract";
        return new VisualPropertyEvidence(
            key,
            ownership,
            dependencyProperty is null
                ? $"WinUI exposes this visual setting as a CLR property with no discoverable backing DependencyProperty field or static property. Its mutation round-tripped after layout and the attached control remained renderable ({dimensions}). Bitmap pixels were unchanged for this specimen ({changedPixelCount} pixel(s) exceeded the {ChannelToleranceLevels}-level channel tolerance, at or below the significance threshold)."
                : $"The mutation round-tripped through DependencyObject.GetValue after layout and the attached control remained renderable ({dimensions}). Bitmap pixels were unchanged for this specimen ({changedPixelCount} pixel(s) exceeded the {ChannelToleranceLevels}-level channel tolerance, at or below the significance threshold).",
            beforeConvergenceReads,
            afterConvergenceReads);
    }

    private static bool LayoutChanged(VisualLayoutSnapshot before, VisualLayoutSnapshot after) =>
        Math.Abs(before.ActualWidth - after.ActualWidth) > 0.1d ||
        Math.Abs(before.ActualHeight - after.ActualHeight) > 0.1d ||
        Math.Abs(before.DesiredWidth - after.DesiredWidth) > 0.1d ||
        Math.Abs(before.DesiredHeight - after.DesiredHeight) > 0.1d ||
        Math.Abs(before.X - after.X) > 0.1d ||
        Math.Abs(before.Y - after.Y) > 0.1d;

    private static string FormatLayout(VisualLayoutSnapshot value) =>
        $"actual={value.ActualWidth:0.##}x{value.ActualHeight:0.##}, desired={value.DesiredWidth:0.##}x{value.DesiredHeight:0.##}, origin=({value.X:0.##},{value.Y:0.##})";

    private static DependencyProperty? ResolveDependencyProperty(Type type, string propertyName)
    {
        // Ether's own DPs are declared the WPF-style way: a public static readonly
        // DependencyProperty field. WinUI 3's C#/WinRT projection exposes the platform's
        // own identifiers (Control.BackgroundProperty, FrameworkElement.WidthProperty, ...)
        // as a static *property* with a get_XProperty accessor instead, because WinRT has
        // no concept of a public static field. Checking only GetField silently failed to
        // resolve every platform DP, which made the DP-layer GetValue assertion below a
        // no-op for those properties and pushed them into the weaker CLR-only evidence
        // bucket. Check both shapes, field first (the more common Ether-side declaration).
        const BindingFlags flags = BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly;
        var fieldName = $"{propertyName}Property";
        for (var candidate = type; candidate is not null; candidate = candidate.BaseType)
        {
            if (candidate.GetField(fieldName, flags)?.GetValue(null) is DependencyProperty fieldDependencyProperty)
            {
                return fieldDependencyProperty;
            }
        }

        for (var candidate = type; candidate is not null; candidate = candidate.BaseType)
        {
            if (candidate.GetProperty(fieldName, flags)?.GetValue(null) is DependencyProperty propertyDependencyProperty)
            {
                return propertyDependencyProperty;
            }
        }

        return null;
    }

    private static object? CreateAttachedVisualSample(FrameworkElement control, PropertyInfo property, object? current, string key)
    {
        var propertyType = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;

        if (property.Name == nameof(FrameworkElement.Width)) return 320d;
        if (property.Name == nameof(FrameworkElement.MaxWidth)) return 120d;
        if (property.Name == nameof(FrameworkElement.Height)) return 96d;
        if (property.Name == nameof(FrameworkElement.MaxHeight)) return 56d;
        if (property.Name == nameof(FrameworkElement.MinWidth)) return 320d;
        if (property.Name == nameof(FrameworkElement.MinHeight)) return 120d;
        if (property.Name == nameof(UIElement.Opacity)) return 0.65d;
        if (property.Name == nameof(RangeBase.Minimum)) return 10d;
        if (property.Name == nameof(RangeBase.Maximum)) return 120d;
        if (property.Name == nameof(RangeBase.Value)) return 42d;
        if (property.Name == nameof(ComboBox.SelectedIndex)) return 1;
        if (property.Name == nameof(ComboBox.SelectedItem) && control is ComboBox comboBox) return comboBox.Items[1];
        if (property.Name == nameof(ComboBox.SelectedValue)) return "second";
        if (property.Name == nameof(TextBox.CharacterSpacing)) return 100;
        if (property.Name == nameof(ComboBox.MaxDropDownHeight)) return 160d;
        if (property.Name is nameof(Control.HorizontalContentAlignment) or nameof(Control.VerticalContentAlignment)) return property.Name.StartsWith("Horizontal", StringComparison.Ordinal) ? HorizontalAlignment.Center : VerticalAlignment.Center;
        if (property.Name == nameof(FrameworkElement.HorizontalAlignment)) return HorizontalAlignment.Center;
        if (property.Name == nameof(FrameworkElement.VerticalAlignment)) return VerticalAlignment.Center;
        if (property.Name == nameof(Control.Padding)) return new Thickness(12d, 8d, 12d, 8d);
        if (property.Name == nameof(FrameworkElement.Margin)) return new Thickness(6d);
        if (property.Name == nameof(Control.Background)) return new SolidColorBrush(Windows.UI.Color.FromArgb(255, 255, 69, 0));
        if (property.Name is nameof(Control.Foreground) or nameof(TextBox.PlaceholderForeground)) return new SolidColorBrush(Windows.UI.Color.FromArgb(255, 148, 0, 211));
        if (property.Name == nameof(Control.BorderBrush)) return new SolidColorBrush(Windows.UI.Color.FromArgb(255, 255, 215, 0));
        if (property.Name == nameof(Control.BorderThickness)) return new Thickness(3d);
        if (property.Name == nameof(Control.CornerRadius)) return new CornerRadius(10d);
        if (property.Name == nameof(Control.FontFamily)) return new FontFamily("Segoe UI");
        if (property.Name == nameof(Control.FontSize)) return 22d;
        if (property.Name == nameof(Control.FontWeight)) return FontWeights.Bold;
        if (property.Name == nameof(UIElement.Clip)) return new RectangleGeometry { Rect = new Windows.Foundation.Rect(0d, 0d, 180d, 60d) };
        if (property.Name == nameof(UIElement.RenderTransform)) return new CompositeTransform { TranslateX = 6d };
        if (property.Name == nameof(UIElement.Rotation)) return 2f;
        if (property.Name == nameof(UIElement.Scale)) return new System.Numerics.Vector3(0.96f, 0.96f, 1f);
        if (property.Name == nameof(EtherSegmentPanel.Spacing)) return 12d;
        if (property.Name == nameof(EtherButton.LeftIcon) || property.Name == nameof(EtherButton.RightIcon)) return new SymbolIcon(Symbol.Accept);
        if (property.Name == nameof(EtherSlider.Labels)) return new EtherSliderLabelCollection { "0", "50", "100" };
        if (property.Name == nameof(EtherSlider.Stops) || property.Name == nameof(EtherSteeringBar.Stops)) return new DoubleCollection { 0d, 50d, 100d };
        if (property.Name == nameof(EtherSlider.Title) || property.Name == nameof(EtherSteeringBar.Title) || property.Name == nameof(EtherProgressBar.Title)) return "Visual audit title";
        if (property.Name == nameof(EtherSteeringBar.ValueContent) || property.Name == nameof(EtherProgressBar.ValueContent)) return "42%";
        if (property.Name is "Content" or "Header") return "Visual audit content";
        if (property.Name is nameof(TextBox.Text) or nameof(TextBox.PlaceholderText) or nameof(ComboBox.Text) or nameof(ComboBox.Description) or nameof(ComboBox.DisplayMemberPath)) return "Visual audit text";
        if (property.Name is nameof(TextBox.TextAlignment) or nameof(TextBox.HorizontalTextAlignment)) return TextAlignment.Center;
        if (property.Name == nameof(TextBox.TextWrapping)) return TextWrapping.Wrap;
        if (property.Name == nameof(TextBox.CharacterCasing)) return CharacterCasing.Upper;
        if (property.Name is "ContentTemplate" or "HeaderTemplate") return new DataTemplate();

        if (propertyType == typeof(bool)) return current is bool flag ? !flag : true;
        if (propertyType == typeof(string)) return "Visual audit specimen";
        if (propertyType == typeof(double)) return 8d;
        if (propertyType == typeof(float)) return 0.5f;
        if (propertyType == typeof(System.Numerics.Vector3)) return new System.Numerics.Vector3(0.96f, 0.96f, 1f);
        if (propertyType == typeof(int)) return 1;
        if (propertyType == typeof(object)) return "Visual audit specimen";
        if (propertyType == typeof(Thickness)) return new Thickness(8d);
        if (propertyType == typeof(CornerRadius)) return new CornerRadius(8d);
        if (propertyType == typeof(Brush)) return new SolidColorBrush(Windows.UI.Color.FromArgb(255, 255, 69, 0));
        if (propertyType == typeof(FontFamily)) return new FontFamily("Segoe UI");
        if (propertyType == typeof(Transform)) return new CompositeTransform { TranslateX = 6d };
        if (propertyType == typeof(Geometry)) return new RectangleGeometry { Rect = new Windows.Foundation.Rect(0d, 0d, 180d, 60d) };
        if (propertyType == typeof(DoubleCollection)) return new DoubleCollection { 0d, 50d, 100d };
        if (propertyType == typeof(DataTemplate)) return new DataTemplate();
        if (propertyType.IsEnum)
        {
            return Enum.GetValues(propertyType).Cast<object>().FirstOrDefault(candidate => !Equals(candidate, current))
                ?? Enum.GetValues(propertyType).GetValue(0)!;
        }

        throw new InvalidOperationException($"No attached visual mutation sample is registered for {key} ({property.PropertyType.FullName}).");
    }

    private static bool AttachedValuesMatch(object? expected, object? actual, Type propertyType)
    {
        if (ReferenceEquals(expected, actual) || Equals(expected, actual))
        {
            return true;
        }

        // XAML stores several visual doubles (notably Opacity) as single-precision
        // compositor values before exposing them back through the CLR wrapper.
        return expected is double expectedDouble && actual is double actualDouble &&
            Math.Abs(expectedDouble - actualDouble) < 0.0001d;
    }
}
