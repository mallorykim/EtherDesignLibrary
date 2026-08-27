using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;
using Windows.ApplicationModel.DataTransfer;
using Windows.ApplicationModel.Resources;

namespace EtherSandbox.Views;

/// <summary>
/// Gallery-only WinUI Gallery <c>ControlExample</c> analogue: live specimen, optional
/// output text, source snippet, and clipboard copy. Page-level Options remain
/// <see cref="ComponentPage.InteractiveControls"/> (right of the INTERACTIVE header).
/// </summary>
[ContentProperty(Name = nameof(Example))]
public sealed partial class ControlExample : UserControl
{
    private const double NarrowLayoutWidth = 720;
    private DispatcherTimer? _copiedTimer;

    public static readonly DependencyProperty ExampleProperty =
        DependencyProperty.Register(nameof(Example), typeof(object), typeof(ControlExample), new PropertyMetadata(null));

    public static readonly DependencyProperty OutputTextProperty =
        DependencyProperty.Register(nameof(OutputText), typeof(string), typeof(ControlExample), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty SourceXamlProperty =
        DependencyProperty.Register(nameof(SourceXaml), typeof(string), typeof(ControlExample), new PropertyMetadata(string.Empty, OnLayoutPropertyChanged));

    public static readonly DependencyProperty IsExampleEnabledProperty =
        DependencyProperty.Register(nameof(IsExampleEnabled), typeof(bool), typeof(ControlExample), new PropertyMetadata(true));

    public object Example
    {
        get => GetValue(ExampleProperty);
        set => SetValue(ExampleProperty, value);
    }

    public string OutputText
    {
        get => (string)GetValue(OutputTextProperty);
        set => SetValue(OutputTextProperty, value);
    }

    public string SourceXaml
    {
        get => (string)GetValue(SourceXamlProperty);
        set => SetValue(SourceXamlProperty, value);
    }

    public bool IsExampleEnabled
    {
        get => (bool)GetValue(IsExampleEnabledProperty);
        set => SetValue(IsExampleEnabledProperty, value);
    }

    public ControlExample()
    {
        this.InitializeComponent();
        this.Loaded += (_, _) => ApplyLayout();
        this.SizeChanged += OnSizeChanged;
        this.Unloaded += OnUnloaded;
    }

    /// <summary>Visible when the string has non-whitespace content.</summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Compiled x:Bind invokes this public instance method; changing it to static would break the binding contract.")]
    public Visibility ConvertHasText(string? value) =>
        string.IsNullOrWhiteSpace(value) ? Visibility.Collapsed : Visibility.Visible;

    private void OnSizeChanged(object sender, SizeChangedEventArgs e) => ApplyLayout();

    private static void OnLayoutPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ControlExample example)
            example.ApplyLayout();
    }

    private void ApplyLayout()
    {
        if (SourcePanel is null)
            return;

        var wide = !string.IsNullOrWhiteSpace(SourceXaml) && ActualWidth >= NarrowLayoutWidth;
        VisualStateManager.GoToState(this, wide ? "WideLayout" : "NarrowLayout", false);
    }

    private void OnCopyClicked(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(SourceXaml))
            return;

        var package = new DataPackage();
        package.SetText(SourceXaml.Trim());
        Clipboard.SetContent(package);

        CopyButton.Content = GetChromeString("ControlExampleCopied.Content", "Copied");
        _copiedTimer?.Stop();
        _copiedTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(1500) };
        _copiedTimer.Tick += OnCopiedTimerTick;
        _copiedTimer.Start();
    }

    private void OnCopiedTimerTick(object? sender, object e)
    {
        _copiedTimer?.Stop();
        if (CopyButton is not null)
            CopyButton.Content = GetChromeString("ControlExampleCopy.Content", "Copy");
    }

    private static string GetChromeString(string name, string fallback)
    {
        try
        {
            var value = ResourceLoader.GetForViewIndependentUse().GetString(name);
            return string.IsNullOrWhiteSpace(value) ? fallback : value;
        }
        catch (Exception)
        {
            return fallback;
        }
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        _copiedTimer?.Stop();
        if (_copiedTimer is not null)
            _copiedTimer.Tick -= OnCopiedTimerTick;
        _copiedTimer = null;
    }
}
