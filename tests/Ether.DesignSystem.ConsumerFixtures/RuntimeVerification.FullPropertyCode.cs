using System.Reflection;
using Ether.DesignSystem.Controls;
using Microsoft.UI.Xaml;

namespace Ether.DesignSystem.ConsumerFixtures;

internal static partial class RuntimeVerification
{
    internal sealed record PublicPropertyCodeVerification(
        int GetterReadCount,
        int SetterInvocationCount,
        string[] PlatformManagedProperties,
        string[] LifecycleBoundProperties);

    private static PublicPropertyCodeVerification VerifyAllPublicPropertyCode(
        EtherButton button,
        EtherCheckbox checkbox,
        EtherDropdown dropdown,
        EtherInput input,
        EtherIntelligenceButton intelligenceButton,
        EtherProgressBar progressBar,
        EtherRadioButton radioButton,
        EtherSegmentedControl segmentedControl,
        EtherSegmentPanel segmentPanel,
        EtherSlider slider,
        EtherSteeringBar steeringBar,
        EtherMasthead masthead)
    {
        var controls = new FrameworkElement[]
        {
            button, checkbox, dropdown, input, intelligenceButton, progressBar, radioButton,
            segmentedControl, segmentPanel, slider, steeringBar, masthead,
            new EtherSegmentedTrack(),
        };
        var readCount = 0;
        var setterCount = 0;
        var platformManaged = new List<string>();
        var lifecycleBound = new List<string>();

        foreach (var control in controls)
        {
            foreach (var property in control.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance)
                         .Where(property => property.CanRead && property.CanWrite && property.GetIndexParameters().Length == 0)
                         .OrderBy(property => property.Name, StringComparer.Ordinal))
            {
                var key = $"{control.GetType().Name}.{property.Name}";
                _ = property.GetValue(control);
                readCount++;
                // An attached element can acquire a local value when an inheritable property
                // (for example FlowDirection) is assigned its current value. Exercise setters
                // on an equivalent unattached instance so the code audit cannot mutate or
                // disconnect the visual tree under the separate visual/RTL audit.
                if (TryInvokeOnUnattachedInstance(control.GetType(), property))
                {
                    setterCount++;
                    continue;
                }

                if (PlatformManagedPropertyNames.Contains(property.Name, StringComparer.Ordinal))
                {
                    platformManaged.Add(key);
                    continue;
                }

                throw new InvalidOperationException($"Public property '{key}' could not be invoked on an unattached {control.GetType().Name} instance.");
            }
        }

        if (readCount == 0 || setterCount + platformManaged.Count != readCount)
        {
            throw new InvalidOperationException("Full public-property code verification did not account for every getter/setter.");
        }

        return new PublicPropertyCodeVerification(readCount, setterCount, platformManaged.ToArray(), lifecycleBound.ToArray());
    }

    private static bool TryInvokeOnUnattachedInstance(Type controlType, PropertyInfo property)
    {
        try
        {
            if (Activator.CreateInstance(controlType) is not FrameworkElement detached)
            {
                return false;
            }

            var value = property.GetValue(detached);
            property.SetValue(detached, value);
            return true;
        }
        catch
        {
            return false;
        }
    }

    // WinUI owns the visual root connection. It is public for framework composition, but an
    // application must not replace it after the element is attached to a Window.
    private static readonly string[] PlatformManagedPropertyNames = [ nameof(FrameworkElement.XamlRoot) ];
}
