using System.Reflection;
using Ether.DesignSystem.Controls;

namespace Ether.DesignSystem.ConsumerFixtures;

internal static partial class RuntimeVerification
{
    internal sealed record PublicPropertyClassificationVerification(
        int VisualPropertyCount,
        int SemanticPropertyCount,
        int PlatformPropertyCount,
        string[] VisualProperties,
        string[] SemanticProperties,
        string[] PlatformProperties);

    private static PublicPropertyClassificationVerification ClassifyAllPublicProperties()
    {
        var visual = new List<string>();
        var semantic = new List<string>();
        var platform = new List<string>();

        foreach (var type in PublicPropertyInventoryTypes)
        {
            foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                         .Where(property => property.CanRead && property.CanWrite && property.GetIndexParameters().Length == 0))
            {
                var key = $"{type.Name}.{property.Name}";
                if (VisualPropertyNames.Contains(property.Name, StringComparer.Ordinal))
                    visual.Add(key);
                else if (SemanticPropertyNames.Contains(property.Name, StringComparer.Ordinal))
                    semantic.Add(key);
                else
                    platform.Add(key);
            }
        }

        var all = visual.Count + semantic.Count + platform.Count;
        if (all != CapturePublicPropertyInventory().WritablePropertyCount)
            throw new InvalidOperationException($"Public-property classification counted {all} properties, expected the full runtime inventory.");

        return new PublicPropertyClassificationVerification(
            visual.Count, semantic.Count, platform.Count,
            visual.OrderBy(key => key, StringComparer.Ordinal).ToArray(),
            semantic.OrderBy(key => key, StringComparer.Ordinal).ToArray(),
            platform.OrderBy(key => key, StringComparer.Ordinal).ToArray());
    }

    // These properties can alter pixels, geometry, text presentation, or the visible control
    // state. They require the attached-control visual gate in addition to the code gate.
    private static readonly string[] VisualPropertyNames =
    [
        "AcceptsReturn", "Background", "BackgroundSizing", "BorderBrush", "BorderThickness",
        "CharacterCasing", "CharacterSpacing", "Clip", "CompositeMode", "Content", "ContentTemplate",
        "CornerRadius", "Description", "DisplayMemberPath", "FontFamily", "FontSize", "FontStretch",
        "FontStyle", "FontWeight", "Foreground", "Height", "Header", "HeaderTemplate",
        "HorizontalAlignment", "HorizontalContentAlignment", "HorizontalTextAlignment", "IsChecked",
        "IsDropDownOpen", "IsEditable", "IsEnabled", "IsOn", "IsReadOnly", "Labels", "LeftIcon", "Margin", "MaxDropDownHeight",
        "MaxHeight", "MaxVisibleItems", "MaxWidth", "MenuGap", "MinHeight", "Minimum", "MinWidth", "Maximum", "Opacity", "Padding",
        "PlaceholderForeground", "PlaceholderText", "PreviewIsMaximized", "PreviewStatus", "RenderTransform", "RightIcon", "Rotation", "Scale", "SelectedIndex",
        "SelectedItem", "SelectedValue", "ShowChevron", "ShowLabels", "ShowMenuIcon", "ShowSearch",
        "ShowSettings", "ShowStops", "ShowTitle", "ShowValue", "Size", "SnapToStops", "Spacing", "Stops", "EnableWindowCommands",
        "Text", "TextAlignment", "TextWrapping", "Title", "Value", "ValueContent", "Variant", "VerticalAlignment",
        "VerticalContentAlignment", "Visibility", "Width"
    ];

    // These values express business or interaction state but are not necessarily visible by
    // themselves. They are covered by adapter/event/UIA assertions rather than pixel checks.
    private static readonly string[] SemanticPropertyNames =
    [
        "AccessKey", "Command", "CommandParameter", "DataContext", "GroupName", "IsTabStop", "ItemsSource",
        "LargeChange", "MaxLength", "SelectedValuePath", "SelectionLength", "SelectionStart", "SmallChange",
        "Tag", "TextReadingOrder"
    ];
}
