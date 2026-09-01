using System.Reflection;
using Ether.DesignSystem.Controls;

namespace Ether.DesignSystem.ConsumerFixtures;

internal static partial class RuntimeVerification
{
    // This is the authoritative public-property scope for the full acceptance work.
    // It deliberately includes inherited writable WinUI properties; exclusions must be
    // classified explicitly by a later code/visual acceptance gate, never omitted.
    internal sealed record PublicPropertyInventoryVerification(
        int WritablePropertyCount,
        string[] PropertyKeys);

    private static PublicPropertyInventoryVerification CapturePublicPropertyInventory()
    {
        var keys = PublicPropertyInventoryTypes
            .SelectMany(type => type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(property => property.CanRead && property.CanWrite && property.GetIndexParameters().Length == 0)
                .Select(property => $"{type.Name}.{property.Name}"))
            .OrderBy(key => key, StringComparer.Ordinal)
            .ToArray();

        if (keys.Length == 0 || keys.Any(string.IsNullOrWhiteSpace))
        {
            throw new InvalidOperationException("The full public-property inventory was not populated.");
        }

        return new PublicPropertyInventoryVerification(keys.Length, keys);
    }

    private static readonly Type[] PublicPropertyInventoryTypes =
    [
        typeof(EtherButton),
        typeof(EtherCheckbox),
        typeof(EtherDropdown),
        typeof(EtherInput),
        typeof(EtherIntelligenceButton),
        typeof(EtherProgressBar),
        typeof(EtherRadioButton),
        typeof(EtherSegmentedControl),
        typeof(EtherSegmentPanel),
        typeof(EtherSegmentRadioButton),
        typeof(EtherSlider),
        typeof(EtherSteeringBar),
        typeof(EtherMasthead),
        typeof(EtherTabNavigation),
        typeof(EtherTabItem),
    ];
}
