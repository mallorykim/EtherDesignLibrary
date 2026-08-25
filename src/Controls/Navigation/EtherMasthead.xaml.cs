using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;

namespace EtherSandbox.Controls;

public sealed partial class EtherMasthead : UserControl
{
    private AppWindow? _appWindow;

    public EtherMasthead()
    {
        this.InitializeComponent();
        Loaded += EtherMasthead_Loaded;
        Unloaded += EtherMasthead_Unloaded;
        UpdateOptionalIconVisibility();
    }

    public bool ShowSettings
    {
        get => (bool)GetValue(ShowSettingsProperty);
        set => SetValue(ShowSettingsProperty, value);
    }

    public static readonly DependencyProperty ShowSettingsProperty =
        DependencyProperty.Register(
            nameof(ShowSettings),
            typeof(bool),
            typeof(EtherMasthead),
            new PropertyMetadata(true, OnOptionalIconPropertyChanged));

    public bool ShowSearch
    {
        get => (bool)GetValue(ShowSearchProperty);
        set => SetValue(ShowSearchProperty, value);
    }

    public static readonly DependencyProperty ShowSearchProperty =
        DependencyProperty.Register(
            nameof(ShowSearch),
            typeof(bool),
            typeof(EtherMasthead),
            new PropertyMetadata(false, OnOptionalIconPropertyChanged));

    public bool ShowMenuIcon
    {
        get => (bool)GetValue(ShowMenuIconProperty);
        set => SetValue(ShowMenuIconProperty, value);
    }

    public static readonly DependencyProperty ShowMenuIconProperty =
        DependencyProperty.Register(
            nameof(ShowMenuIcon),
            typeof(bool),
            typeof(EtherMasthead),
            new PropertyMetadata(false, OnOptionalIconPropertyChanged));

    public bool ShowChevron
    {
        get => (bool)GetValue(ShowChevronProperty);
        set => SetValue(ShowChevronProperty, value);
    }

    public static readonly DependencyProperty ShowChevronProperty =
        DependencyProperty.Register(
            nameof(ShowChevron),
            typeof(bool),
            typeof(EtherMasthead),
            new PropertyMetadata(false, OnOptionalIconPropertyChanged));

    public bool? PreviewIsMaximized
    {
        get => (bool?)GetValue(PreviewIsMaximizedProperty);
        set => SetValue(PreviewIsMaximizedProperty, value);
    }

    public static readonly DependencyProperty PreviewIsMaximizedProperty =
        DependencyProperty.Register(
            nameof(PreviewIsMaximized),
            typeof(bool?),
            typeof(EtherMasthead),
            new PropertyMetadata(null, OnPreviewWindowStateChanged));

    public bool EnableWindowCommands
    {
        get => (bool)GetValue(EnableWindowCommandsProperty);
        set => SetValue(EnableWindowCommandsProperty, value);
    }

    public static readonly DependencyProperty EnableWindowCommandsProperty =
        DependencyProperty.Register(
            nameof(EnableWindowCommands),
            typeof(bool),
            typeof(EtherMasthead),
            new PropertyMetadata(true));

    private static void OnOptionalIconPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is EtherMasthead masthead)
        {
            masthead.UpdateOptionalIconVisibility();
        }
    }

    private static void OnPreviewWindowStateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is EtherMasthead masthead)
        {
            masthead.UpdateMaximizeRestoreIcon();
        }
    }

    private static OverlappedPresenter? GetPresenter()
    {
        return App.MainWindow?.AppWindow.Presenter as OverlappedPresenter;
    }

    private void EtherMasthead_Loaded(object sender, RoutedEventArgs e)
    {
        var appWindow = App.MainWindow?.AppWindow;
        if (_appWindow == appWindow)
        {
            UpdateMaximizeRestoreIcon();
            return;
        }

        if (_appWindow is not null)
        {
            _appWindow.Changed -= AppWindow_Changed;
        }

        _appWindow = appWindow;

        if (_appWindow is not null)
        {
            _appWindow.Changed += AppWindow_Changed;
        }

        UpdateOptionalIconVisibility();
        UpdateMaximizeRestoreIcon();
    }

    private void EtherMasthead_Unloaded(object sender, RoutedEventArgs e)
    {
        if (_appWindow is null)
        {
            return;
        }

        _appWindow.Changed -= AppWindow_Changed;
        _appWindow = null;
    }

    private void AppWindow_Changed(AppWindow sender, AppWindowChangedEventArgs args)
    {
        DispatcherQueue.TryEnqueue(UpdateMaximizeRestoreIcon);
    }

    private void UpdateOptionalIconVisibility()
    {
        if (MenuIconSlot is null)
        {
            return;
        }

        MenuIconSlot.Visibility = ShowMenuIcon ? Visibility.Visible : Visibility.Collapsed;
        SearchIconSlot.Visibility = ShowSearch ? Visibility.Visible : Visibility.Collapsed;
        SettingsButton.Visibility = ShowSettings ? Visibility.Visible : Visibility.Collapsed;
        ChevronSlot.Visibility = ShowChevron ? Visibility.Visible : Visibility.Collapsed;
    }

    private void UpdateMaximizeRestoreIcon()
    {
        var isMaximized = PreviewIsMaximized ?? (GetPresenter()?.State == OverlappedPresenterState.Maximized);

        MaximizeIcon.Visibility = isMaximized ? Visibility.Collapsed : Visibility.Visible;
        RestoreIcon.Visibility = isMaximized ? Visibility.Visible : Visibility.Collapsed;
        AutomationProperties.SetName(MaximizeRestoreButton, isMaximized ? "Restore window" : "Maximize window");
    }

    private void MinimizeButton_Click(object sender, RoutedEventArgs e)
    {
        if (!EnableWindowCommands)
        {
            return;
        }

        GetPresenter()?.Minimize();
    }

    private void MaximizeRestoreButton_Click(object sender, RoutedEventArgs e)
    {
        if (!EnableWindowCommands)
        {
            if (PreviewIsMaximized.HasValue)
            {
                PreviewIsMaximized = !PreviewIsMaximized.Value;
            }
            return;
        }

        var presenter = GetPresenter();
        if (presenter is null)
        {
            return;
        }

        if (presenter.State == OverlappedPresenterState.Maximized)
        {
            presenter.Restore();
        }
        else
        {
            presenter.Maximize();
        }

        UpdateMaximizeRestoreIcon();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        if (!EnableWindowCommands)
        {
            return;
        }

        App.MainWindow?.Close();
    }
}
