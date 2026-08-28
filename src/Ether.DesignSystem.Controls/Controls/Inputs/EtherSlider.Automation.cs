using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;

namespace EtherSandbox.Controls;

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
                throw new InvalidOperationException("The slider cannot accept automation-driven value changes in its current state.");
        }

        internal void RaiseValueChanged(double oldValue, double newValue)
            => RaisePropertyChangedEvent(RangeValuePatternIdentifiers.ValueProperty, oldValue, newValue);

        protected override string GetClassNameCore()
            => nameof(EtherSlider);

        protected override AutomationControlType GetAutomationControlTypeCore()
            => AutomationControlType.Slider;

        protected override object? GetPatternCore(PatternInterface patternInterface)
            => patternInterface == PatternInterface.RangeValue ? this : base.GetPatternCore(patternInterface);
    }
}
