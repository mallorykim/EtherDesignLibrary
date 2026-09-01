using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;

namespace Ether.DesignSystem.Controls;

public sealed partial class EtherSlider
{
    private sealed class EtherSliderAutomationPeer(EtherSlider owner)
        : FrameworkElementAutomationPeer(owner), IRangeValueProvider
    {
        private EtherSlider OwnerControl => (EtherSlider)Owner;

        public bool IsReadOnly => !OwnerControl.CanInteract;

        public double LargeChange => OwnerControl.AutomationLargeChange;

        public double Maximum => OwnerControl.RangeMaximum;

        public double Minimum => OwnerControl.RangeMinimum;

        public double SmallChange => OwnerControl.AutomationSmallChange;

        public double Value => OwnerControl.Value;

        public void SetValue(double value)
        {
            if (!OwnerControl.SetValueFromAutomation(value))
                throw new ElementNotEnabledException("The slider cannot accept automation-driven value changes in its current state.");
        }

        internal void RaiseValueChanged(double oldValue, double newValue)
            => RaisePropertyChangedEvent(RangeValuePatternIdentifiers.ValueProperty, oldValue, newValue);

        protected override string GetClassNameCore()
            => nameof(EtherSlider);

        // Surface the slider's Title (its header) as the accessible name when the framework has
        // no other name, matching stock Slider (Header labels the control) and the sibling
        // EtherSteeringBar / EtherProgressBar peers, which also fall back to Title.
        protected override string GetNameCore()
        {
            var baseName = base.GetNameCore();
            return string.IsNullOrEmpty(baseName) ? OwnerControl.Title ?? string.Empty : baseName;
        }

        protected override AutomationControlType GetAutomationControlTypeCore()
            => AutomationControlType.Slider;

        protected override object? GetPatternCore(PatternInterface patternInterface)
            => patternInterface == PatternInterface.RangeValue ? this : base.GetPatternCore(patternInterface);
    }
}
