using System.ComponentModel;
using System.Windows.Input;
using Ether.DesignSystem.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;

namespace Ether.DesignSystem.ConsumerFixtures;

internal static partial class RuntimeVerification
{
    private sealed class TwoWayViewModel : INotifyPropertyChanged
    {
        private bool? _checked;
        private string _text = string.Empty;
        private object? _selectedItem;
        private object? _selectedValue;
        private int _selectedIndex = -1;
        private double _value;
        private bool _isOn;

        public bool? Checked { get => _checked; set => Set(ref _checked, value); }
        public string Text { get => _text; set => Set(ref _text, value); }
        public object? SelectedItem { get => _selectedItem; set => Set(ref _selectedItem, value); }
        public object? SelectedValue { get => _selectedValue; set => Set(ref _selectedValue, value); }
        public int SelectedIndex { get => _selectedIndex; set => Set(ref _selectedIndex, value); }
        public double Value { get => _value; set => Set(ref _value, value); }
        public bool IsOn { get => _isOn; set => Set(ref _isOn, value); }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void Set<T>(ref T field, T value, [System.Runtime.CompilerServices.CallerMemberName] string? name = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
                return;

            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }

    private sealed record SegmentData(string Label);

    private static void BindTwoWay(FrameworkElement target, DependencyProperty property, TwoWayViewModel source, string path)
        => target.SetBinding(property, new Microsoft.UI.Xaml.Data.Binding
        {
            Source = source,
            Path = new PropertyPath(path),
            Mode = Microsoft.UI.Xaml.Data.BindingMode.TwoWay,
        });

    private static TwoWayBindingVerification VerifyTwoWayBindings(FrameworkElement themeRoot)
    {
        if (themeRoot is not Panel host)
            throw new InvalidOperationException("Wave R2 TwoWay probes require a panel-based theme root.");

        var scratch = new Canvas { Opacity = 0d, IsHitTestVisible = false };
        host.Children.Add(scratch);
        try
        {
            var verified = new List<string>();

            var checkboxVm = new TwoWayViewModel();
            var checkbox = new EtherCheckbox();
            BindTwoWay(checkbox, ToggleButton.IsCheckedProperty, checkboxVm, nameof(TwoWayViewModel.Checked));
            scratch.Children.Add(checkbox);
            checkboxVm.Checked = true;
            Assert(checkbox.IsChecked == true, "EtherCheckbox did not receive the VM IsChecked value.");
            checkbox.IsChecked = false;
            Assert(checkboxVm.Checked == false, "EtherCheckbox did not push IsChecked back to the VM.");
            verified.Add("EtherCheckbox.IsChecked");

            var radioVm = new TwoWayViewModel();
            var radio = new EtherRadioButton();
            BindTwoWay(radio, ToggleButton.IsCheckedProperty, radioVm, nameof(TwoWayViewModel.Checked));
            scratch.Children.Add(radio);
            radioVm.Checked = true;
            Assert(radio.IsChecked == true, "EtherRadioButton did not receive the VM IsChecked value.");
            radio.IsChecked = false;
            Assert(radioVm.Checked == false, "EtherRadioButton did not push IsChecked back to the VM.");
            verified.Add("EtherRadioButton.IsChecked");

            var inputVm = new TwoWayViewModel();
            var input = new EtherInput();
            BindTwoWay(input, TextBox.TextProperty, inputVm, nameof(TwoWayViewModel.Text));
            scratch.Children.Add(input);
            inputVm.Text = "from VM";
            Assert(input.Text == "from VM", "EtherInput did not receive the VM Text value.");
            input.Text = "from control";
            Assert(inputVm.Text == "from control", "EtherInput did not push Text back to the VM.");
            verified.Add("EtherInput.Text");

            var dropdownItemVm = new TwoWayViewModel { SelectedItem = "A" };
            var dropdownItem = new EtherDropdown { ItemsSource = new[] { "A", "B" } };
            BindTwoWay(dropdownItem, Selector.SelectedItemProperty, dropdownItemVm, nameof(TwoWayViewModel.SelectedItem));
            scratch.Children.Add(dropdownItem);
            dropdownItemVm.SelectedItem = "B";
            Assert(Equals(dropdownItem.SelectedItem, "B"), "EtherDropdown did not receive the VM SelectedItem value.");
            dropdownItem.SelectedItem = "A";
            Assert(Equals(dropdownItemVm.SelectedItem, "A"), "EtherDropdown did not push SelectedItem back to the VM.");
            verified.Add("EtherDropdown.SelectedItem");

            var dropdownValueVm = new TwoWayViewModel { SelectedValue = "A" };
            var dropdownValue = new EtherDropdown { ItemsSource = new[] { "A", "B" } };
            BindTwoWay(dropdownValue, Selector.SelectedValueProperty, dropdownValueVm, nameof(TwoWayViewModel.SelectedValue));
            scratch.Children.Add(dropdownValue);
            dropdownValueVm.SelectedValue = "B";
            Assert(Equals(dropdownValue.SelectedValue, "B"), "EtherDropdown did not receive the VM SelectedValue value.");
            dropdownValue.SelectedValue = "A";
            Assert(Equals(dropdownValueVm.SelectedValue, "A"), "EtherDropdown did not push SelectedValue back to the VM.");
            verified.Add("EtherDropdown.SelectedValue");

            var segmentedValueVm = new TwoWayViewModel { SelectedValue = "A" };
            var segmentedValue = CreateStringSegmentedControl();
            BindTwoWay(segmentedValue, EtherSegmentedControl.SelectedValueProperty, segmentedValueVm, nameof(TwoWayViewModel.SelectedValue));
            scratch.Children.Add(segmentedValue);
            segmentedValueVm.SelectedValue = "B";
            Assert(Equals(segmentedValue.SelectedValue, "B"), "EtherSegmentedControl did not receive the VM SelectedValue value.");
            segmentedValue.SelectedValue = "A";
            Assert(Equals(segmentedValueVm.SelectedValue, "A"), "EtherSegmentedControl did not push SelectedValue back to the VM.");
            verified.Add("EtherSegmentedControl.SelectedValue");

            var segmentedIndexVm = new TwoWayViewModel { SelectedIndex = 0 };
            var segmentedIndex = CreateStringSegmentedControl();
            BindTwoWay(segmentedIndex, EtherSegmentedControl.SelectedIndexProperty, segmentedIndexVm, nameof(TwoWayViewModel.SelectedIndex));
            scratch.Children.Add(segmentedIndex);
            segmentedIndexVm.SelectedIndex = 1;
            Assert(segmentedIndex.SelectedIndex == 1, "EtherSegmentedControl did not receive the VM SelectedIndex value.");
            segmentedIndex.SelectedIndex = 0;
            Assert(segmentedIndexVm.SelectedIndex == 0, "EtherSegmentedControl did not push SelectedIndex back to the VM.");
            verified.Add("EtherSegmentedControl.SelectedIndex");

            var steeringVm = new TwoWayViewModel();
            var steering = new EtherSteeringBar();
            BindTwoWay(steering, EtherSteeringBar.ValueProperty, steeringVm, nameof(TwoWayViewModel.Value));
            scratch.Children.Add(steering);
            steeringVm.Value = 42;
            Assert(Math.Abs(steering.Value - 42) < 0.001, "EtherSteeringBar did not receive the VM Value.");
            steering.Value = 17;
            Assert(Math.Abs(steeringVm.Value - 17) < 0.001, "EtherSteeringBar did not push Value back to the VM.");
            verified.Add("EtherSteeringBar.Value");

            var switchVm = new TwoWayViewModel();
            var switchControl = new ToggleSwitch();
            BindTwoWay(switchControl, ToggleSwitch.IsOnProperty, switchVm, nameof(TwoWayViewModel.IsOn));
            scratch.Children.Add(switchControl);
            switchVm.IsOn = true;
            Assert(switchControl.IsOn, "EtherSwitch-styled ToggleSwitch did not receive the VM IsOn value.");
            switchControl.IsOn = false;
            Assert(!switchVm.IsOn, "EtherSwitch-styled ToggleSwitch did not push IsOn back to the VM.");
            verified.Add("EtherSwitch.IsOn");

            var scrollVm = new TwoWayViewModel();
            var scrollBar = new ScrollBar { Maximum = 100 };
            BindTwoWay(scrollBar, RangeBase.ValueProperty, scrollVm, nameof(TwoWayViewModel.Value));
            scratch.Children.Add(scrollBar);
            scrollVm.Value = 24;
            Assert(Math.Abs(scrollBar.Value - 24) < 0.001, "EtherScrollBar did not receive the VM Value.");
            scrollBar.Value = 11;
            Assert(Math.Abs(scrollVm.Value - 11) < 0.001, "EtherScrollBar did not push Value back to the VM.");
            verified.Add("EtherScrollBar.Value");

            var tabIndexVm = new TwoWayViewModel { SelectedIndex = 0 };
            var tabIndex = CreateTabNavigation();
            BindTwoWay(tabIndex, Selector.SelectedIndexProperty, tabIndexVm, nameof(TwoWayViewModel.SelectedIndex));
            scratch.Children.Add(tabIndex);
            tabIndexVm.SelectedIndex = 1;
            Assert(tabIndex.SelectedIndex == 1, "EtherTabNavigation did not receive the VM SelectedIndex value.");
            tabIndex.SelectedIndex = 0;
            Assert(tabIndexVm.SelectedIndex == 0, "EtherTabNavigation did not push SelectedIndex back to the VM.");
            verified.Add("EtherTabNavigation.SelectedIndex");

            var tabItemVm = new TwoWayViewModel { SelectedItem = "Home" };
            var tabItem = CreateTabNavigation();
            BindTwoWay(tabItem, Selector.SelectedItemProperty, tabItemVm, nameof(TwoWayViewModel.SelectedItem));
            scratch.Children.Add(tabItem);
            tabItemVm.SelectedItem = "Settings";
            Assert(Equals(tabItem.SelectedItem, "Settings"), "EtherTabNavigation did not receive the VM SelectedItem value.");
            tabItem.SelectedItem = "Home";
            Assert(Equals(tabItemVm.SelectedItem, "Home"), "EtherTabNavigation did not push SelectedItem back to the VM.");
            verified.Add("EtherTabNavigation.SelectedItem");

            return new TwoWayBindingVerification(verified.ToArray());
        }
        finally
        {
            host.Children.Remove(scratch);
        }
    }

    private static DataPathVerification VerifyDataPaths(FrameworkElement themeRoot)
    {
        if (themeRoot is not Panel host)
            throw new InvalidOperationException("Wave R2 data probes require a panel-based theme root.");

        var scratch = new Canvas { Opacity = 0d, IsHitTestVisible = false };
        host.Children.Add(scratch);
        try
        {
            var tabs = new[] { "Home", "Settings", "About" };
            var tabNavigation = new EtherTabNavigation { ItemsSource = tabs, SelectedIndex = 1 };
            scratch.Children.Add(tabNavigation);
            Assert(tabNavigation.Items.Count == tabs.Length, "EtherTabNavigation ItemsSource did not drive the item count.");
            Assert(Equals(tabNavigation.SelectedItem, "Settings"), "EtherTabNavigation ItemsSource selection did not resolve SelectedItem.");
            tabNavigation.SelectedIndex = 2;
            Assert(Equals(tabNavigation.SelectedItem, "About"), "EtherTabNavigation selection did not synchronize after a data-driven change.");

            var segments = new[] { new SegmentData("Alpha"), new SegmentData("Beta") };
            var segmented = new EtherSegmentedControl
            {
                ItemsSource = segments,
                DisplayMemberPath = nameof(SegmentData.Label),
            };
            scratch.Children.Add(segmented);
            segmented.SelectedIndex = 1;
            var panel = segmented.Content as EtherSegmentPanel;
            Assert(panel is not null && panel.Children.Count == segments.Length, "EtherSegmentedControl ItemsSource did not generate the expected segments.");
            Assert(ReferenceEquals(segmented.SelectedItem, segments[1]) && ReferenceEquals(segmented.SelectedValue, segments[1]), "EtherSegmentedControl data selection did not synchronize SelectedItem and SelectedValue.");
            Assert((panel!.Children[1] as RadioButton)?.Content as string == "Beta", "EtherSegmentedControl DisplayMemberPath did not shape generated content.");

            return new DataPathVerification(
                tabNavigation.Items.Count,
                panel.Children.Count,
                true,
                true);
        }
        finally
        {
            host.Children.Remove(scratch);
        }
    }

    private static EtherTabNavigation CreateTabNavigation() => new()
    {
        ItemsSource = new[] { "Home", "Settings" },
        SelectionMode = ListViewSelectionMode.Single,
    };

    private static EtherSegmentedControl CreateStringSegmentedControl() => new()
    {
        ItemsSource = new[] { "A", "B" },
    };

    private static AutomationPatternVerification VerifyAutomationPatterns(
        EtherButton button,
        EtherCheckbox checkbox,
        EtherRadioButton radioButton,
        EtherInput input,
        EtherDropdown dropdown,
        EtherSegmentedControl segmentedControl,
        EtherIntelligenceButton intelligenceButton,
        ToggleSwitch toggleSwitch,
        FrameworkElement themeRoot)
    {
        var verified = new List<string>();
        Assert(FrameworkElementAutomationPeer.CreatePeerForElement(button)?.GetPattern(PatternInterface.Invoke) is IInvokeProvider, "EtherButton did not expose UIA Invoke.");
        verified.Add("EtherButton.Invoke");
        Assert(FrameworkElementAutomationPeer.CreatePeerForElement(intelligenceButton)?.GetPattern(PatternInterface.Invoke) is IInvokeProvider, "EtherIntelligenceButton did not expose UIA Invoke.");
        verified.Add("EtherIntelligenceButton.Invoke");
        Assert(FrameworkElementAutomationPeer.CreatePeerForElement(checkbox)?.GetPattern(PatternInterface.Toggle) is IToggleProvider, "EtherCheckbox did not expose UIA Toggle.");
        verified.Add("EtherCheckbox.Toggle");
        var radioPeer = FrameworkElementAutomationPeer.CreatePeerForElement(radioButton)
            ?? throw new InvalidOperationException("EtherRadioButton did not create a peer for UIA SelectionItem.");
        var radioSelectionItem = radioPeer.GetPattern(PatternInterface.SelectionItem) as ISelectionItemProvider
            ?? throw new InvalidOperationException("EtherRadioButton did not expose UIA SelectionItem.");
        radioButton.IsChecked = false;
        Assert(!radioSelectionItem.IsSelected && radioButton.IsChecked == false, "EtherRadioButton UIA SelectionItem.IsSelected did not reflect IsChecked=false.");
        radioButton.IsChecked = true;
        Assert(radioSelectionItem.IsSelected && radioButton.IsChecked == true, "EtherRadioButton UIA SelectionItem.IsSelected did not reflect IsChecked=true.");
        verified.Add("EtherRadioButton.SelectionItem");
        Assert(FrameworkElementAutomationPeer.CreatePeerForElement(toggleSwitch)?.GetPattern(PatternInterface.Toggle) is IToggleProvider, "EtherSwitch did not expose UIA Toggle.");
        verified.Add("EtherSwitch.Toggle");
        input.IsEnabled = true;
        // Restore EtherInput's genuine defaults: an earlier visual-state proof
        // (RuntimeVerification.Input.cs:36-37) leaves IsTabStop/IsHitTestVisible = false on this
        // shared instance and never restores them. A TextBox defaults to IsTabStop=true, so restore
        // it here (mirroring the intelligenceButton restore in PropertyConsumption.cs) so the
        // focusability assertion below tests EtherInput's real contract, not leaked fixture state.
        input.IsTabStop = true;
        input.IsHitTestVisible = true;
        var inputPeer = FrameworkElementAutomationPeer.CreatePeerForElement(input)
            ?? throw new InvalidOperationException("EtherInput did not create an automation peer.");
        Assert(inputPeer.GetAutomationControlType() == AutomationControlType.Edit, "EtherInput UIA control type was not Edit.");
        Assert(inputPeer.IsKeyboardFocusable(), "EtherInput UIA peer was not keyboard focusable.");
        Assert(inputPeer.IsEnabled(), "EtherInput UIA peer was not enabled.");
        // WinUI TextBox provides the editable Text pattern natively to out-of-process UIA
        // clients such as Narrator, not through managed AutomationPeer.GetPattern.
        verified.Add("EtherInput.Edit");

        var dropdownPeer = FrameworkElementAutomationPeer.CreatePeerForElement(dropdown)
            ?? throw new InvalidOperationException("EtherDropdown did not create a peer for UIA ExpandCollapse.");
        var expandCollapse = dropdownPeer.GetPattern(PatternInterface.ExpandCollapse) as IExpandCollapseProvider
            ?? throw new InvalidOperationException("EtherDropdown did not expose UIA ExpandCollapse.");
        dropdown.IsHitTestVisible = true;
        dropdown.IsDropDownOpen = false;
        expandCollapse.Expand();
        Assert(dropdown.IsDropDownOpen && expandCollapse.ExpandCollapseState == ExpandCollapseState.Expanded, "EtherDropdown UIA ExpandCollapse did not track IsDropDownOpen=true.");
        expandCollapse.Collapse();
        Assert(!dropdown.IsDropDownOpen && expandCollapse.ExpandCollapseState == ExpandCollapseState.Collapsed, "EtherDropdown UIA ExpandCollapse did not track IsDropDownOpen=false.");
        verified.Add("EtherDropdown.ExpandCollapse");

        var tabNavigation = CreateTabNavigation();
        if (themeRoot is Panel host)
        {
            var scratch = new Canvas { Opacity = 0d, IsHitTestVisible = false };
            host.Children.Add(scratch);
            try
            {
                scratch.Children.Add(tabNavigation);
                // The single-selection assertion below presupposes a selected tab. CreateTabNavigation()
                // sets ItemsSource + SelectionMode=Single but selects nothing, so WinUI's
                // ListViewAutomationPeer.GetSelection() returns null (not an empty array) -> the
                // GetSelection().Length dereference threw NullReferenceException. Complete the setup the
                // contract presupposes: select the first tab and realize its container (ScrollIntoView +
                // UpdateLayout) so the peer can report exactly one selected item. This does not change
                // what the assertion verifies (single-select, exactly one selected item).
                tabNavigation.SelectedIndex = 0;
                tabNavigation.UpdateLayout();
                tabNavigation.ScrollIntoView(tabNavigation.SelectedItem);
                tabNavigation.UpdateLayout();
                var tabPeer = FrameworkElementAutomationPeer.CreatePeerForElement(tabNavigation)
                    ?? throw new InvalidOperationException("EtherTabNavigation did not create a peer for UIA Selection.");
                var selection = tabPeer.GetPattern(PatternInterface.Selection) as ISelectionProvider
                    ?? throw new InvalidOperationException("EtherTabNavigation did not expose UIA Selection.");
                Assert(!selection.CanSelectMultiple && selection.GetSelection().Length == 1, "EtherTabNavigation UIA Selection did not report single selection.");
                verified.Add("EtherTabNavigation.Selection");
            }
            finally
            {
                host.Children.Remove(scratch);
            }
        }

        var segmentedPeer = FrameworkElementAutomationPeer.CreatePeerForElement(segmentedControl)
            ?? throw new InvalidOperationException("EtherSegmentedControl did not create a peer for UIA segment SelectionItem verification.");
        Assert(segmentedPeer.GetPattern(PatternInterface.Selection) is not ISelectionProvider, "EtherSegmentedControl incorrectly exposed a host-level UIA Selection provider.");

        var segments = GetSegmentRadioButtons(segmentedControl);
        if (segments.Length == 0)
            throw new InvalidOperationException("EtherSegmentedControl did not expose any EtherSegmentRadioButton segments for UIA SelectionItem verification.");

        for (var index = 0; index < segments.Length; index++)
        {
            if (segments[index] is not EtherSegmentRadioButton)
                throw new InvalidOperationException($"EtherSegmentedControl segment {index} was not an EtherSegmentRadioButton.");

            var segmentPeer = FrameworkElementAutomationPeer.CreatePeerForElement(segments[index])
                ?? throw new InvalidOperationException($"EtherSegmentedControl segment {index} did not create a peer for UIA SelectionItem.");
            var segmentSelectionItem = segmentPeer.GetPattern(PatternInterface.SelectionItem) as ISelectionItemProvider
                ?? throw new InvalidOperationException($"EtherSegmentedControl segment {index} did not expose UIA SelectionItem.");
            segments[index].IsChecked = index == 0;
            Assert(segmentSelectionItem.IsSelected == (segments[index].IsChecked == true), $"EtherSegmentedControl segment {index} UIA SelectionItem.IsSelected did not reflect IsChecked.");
            verified.Add($"EtherSegmentedControl.Segment[{index}].SelectionItem");
        }

        return new AutomationPatternVerification(verified.ToArray());
    }

    private sealed class RecordingCommand : ICommand
    {
        public int ExecutionCount { get; private set; }
        public object? LastParameter { get; private set; }
        public bool CanExecute(object? parameter) => true;
        public event EventHandler? CanExecuteChanged;
        public void Execute(object? parameter) { ExecutionCount++; LastParameter = parameter; }
        public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }

    private static CommandVerification VerifyCommands()
    {
        var verified = new List<string>();
        VerifyButtonCommand(new EtherButton(), "EtherButton", verified);
        VerifyButtonCommand(new EtherIntelligenceButton(), "EtherIntelligenceButton", verified);
        VerifyToggleCommand(new EtherCheckbox(), "EtherCheckbox", verified);
        // RadioButton is a ToggleButton, but its automation peer exposes ISelectionItemProvider
        // (SelectionItem), not IToggleProvider (Toggle): VerifyToggleCommand asked RadioButton for a
        // Toggle pattern it structurally never provides (WinUI design; the same file already verifies
        // the radio via PatternInterface.SelectionItem). Drive it through its real UIA pattern instead.
        VerifySelectionItemCommand(new EtherRadioButton(), "EtherRadioButton", verified);
        return new CommandVerification(verified.ToArray());
    }

    private static void VerifyButtonCommand(Button control, string name, List<string> verified)
    {
        var command = new RecordingCommand();
        control.Command = command;
        control.CommandParameter = name + "-parameter";
        var peer = FrameworkElementAutomationPeer.CreatePeerForElement(control)
            ?? throw new InvalidOperationException($"{name} did not create an automation peer for Command.");
        (peer.GetPattern(PatternInterface.Invoke) as IInvokeProvider)?.Invoke();
        Assert(command.ExecutionCount == 1 && Equals(command.LastParameter, name + "-parameter"), $"{name} Command/CommandParameter did not execute through UIA Invoke.");
        verified.Add(name);
    }

    private static void VerifyToggleCommand(ToggleButton control, string name, List<string> verified)
    {
        var command = new RecordingCommand();
        control.Command = command;
        control.CommandParameter = name + "-parameter";
        var peer = FrameworkElementAutomationPeer.CreatePeerForElement(control)
            ?? throw new InvalidOperationException($"{name} did not create an automation peer for Command.");
        var toggle = peer.GetPattern(PatternInterface.Toggle) as IToggleProvider
            ?? throw new InvalidOperationException($"{name} did not expose UIA Toggle for Command.");
        toggle.Toggle();
        Assert(command.ExecutionCount == 1 && Equals(command.LastParameter, name + "-parameter"), $"{name} Command/CommandParameter did not execute through UIA Toggle.");
        verified.Add(name);
    }

    // Command verification for RadioButton, whose UIA actuation is SelectionItem.Select() (not
    // Toggle). WinUI routes Select() through the same click path that raises ButtonBase.Command, so
    // this checks the identical Command/CommandParameter contract VerifyToggleCommand checks for
    // Checkbox (executes exactly once with the expected parameter); only the actuation pattern
    // differs to match the control's real UIA surface.
    private static void VerifySelectionItemCommand(RadioButton control, string name, List<string> verified)
    {
        var command = new RecordingCommand();
        control.Command = command;
        control.CommandParameter = name + "-parameter";
        var peer = FrameworkElementAutomationPeer.CreatePeerForElement(control)
            ?? throw new InvalidOperationException($"{name} did not create an automation peer for Command.");
        var selectionItem = peer.GetPattern(PatternInterface.SelectionItem) as ISelectionItemProvider
            ?? throw new InvalidOperationException($"{name} did not expose UIA SelectionItem for Command.");
        selectionItem.Select();
        Assert(command.ExecutionCount == 1 && Equals(command.LastParameter, name + "-parameter"), $"{name} Command/CommandParameter did not execute through UIA SelectionItem.");
        verified.Add(name);
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }
}
