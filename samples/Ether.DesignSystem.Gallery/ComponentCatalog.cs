using EtherSandbox.Views;
using EtherSandbox.Views.Controls;
using EtherSandbox.Views.DataDisplay;
using EtherSandbox.Views.Navigation;
using EtherSandbox.Views.Surfaces;
using Foundations = EtherSandbox.Views.Foundations;

namespace EtherSandbox;

/// <summary>
/// One entry in the catalog: a display name, the Page type that shows it, whether it
/// belongs to the AI family of components (informational only — see <see cref="CatalogLeaf"/>
/// remarks), whether it has been reworked in its own branch (<see cref="IsUpdated"/>,
/// surfaced as an "UPDATED" badge in the nav, the Home cards and the component page), and
/// whether it's a raw primitive-token page rather than a component (<see cref="IsPrimitive"/>,
/// surfaced the same way as a "PRIMITIVE TOKEN" badge).
/// </summary>
public sealed record ComponentEntry(string Name, Type PageType, bool IsAiFamily = false, bool IsUpdated = false, bool IsPrimitive = false)
{
    /// <summary>Localized nav/home label. <see cref="Name"/> stays the English fallback key.</summary>
    public string DisplayName => GalleryStrings.Get(GalleryStrings.CatalogKey(Name), Name);
}

/// <summary>Base type for a top-level slot in the navigation tree.</summary>
public abstract record CatalogNode;

/// <summary>A category header (e.g. "Controls") with its component pages underneath it.</summary>
public sealed record CatalogCategory(string Header, IReadOnlyList<ComponentEntry> Items) : CatalogNode
{
    /// <summary>Localized category header. <see cref="Header"/> stays the English fallback key.</summary>
    public string DisplayHeader => GalleryStrings.Get(GalleryStrings.CatalogKey(Header), Header);
}

/// <summary>
/// A single top-level entry with no header of its own, e.g. "AI Design Language" —
/// it sits alongside the category headers rather than inside one.
/// </summary>
public sealed record CatalogLeaf(ComponentEntry Entry) : CatalogNode;

/// <summary>
/// Single source of truth for the sandbox's navigation tree: category → component name →
/// page Type. Both the NavigationView's menu items (MainWindow) and the Home page index
/// are generated from <see cref="Nodes"/>, so they cannot drift from each other. Adding a
/// component later is one line here plus one page file — no edit to MainWindow.
/// </summary>
public static class ComponentCatalog
{
    /// <summary>The index/landing page. Not part of <see cref="Nodes"/> — it is always first.</summary>
    public static ComponentEntry Home { get; } = new("Home", typeof(HomePage));

    public static IReadOnlyList<CatalogNode> Nodes { get; } = new CatalogNode[]
    {
        new CatalogCategory("Foundations", new[]
        {
            new ComponentEntry("Colors", typeof(Foundations.ColorsPage), IsPrimitive: true),
            new ComponentEntry("Typography", typeof(Foundations.TypographyPage), IsPrimitive: true),
            new ComponentEntry("Spacing", typeof(Foundations.SpacingPage), IsPrimitive: true),
            new ComponentEntry("Radius", typeof(Foundations.RadiusPage), IsPrimitive: true),
            new ComponentEntry("Icons", typeof(Foundations.IconsPage), IsPrimitive: true),
        }),
        new CatalogCategory("Controls", new[]
        {
            new ComponentEntry("Button", typeof(ButtonPage), IsUpdated: true),
            new ComponentEntry("Checkbox", typeof(CheckboxPage), IsUpdated: true),
            new ComponentEntry("Dropdown", typeof(DropdownPage), IsUpdated: true),
            new ComponentEntry("Input", typeof(InputPage), IsUpdated: true),
            new ComponentEntry("Intelligence Button", typeof(IntelligenceButtonPage), IsAiFamily: true, IsUpdated: true),
            new ComponentEntry("Radio Button", typeof(RadioButtonPage), IsUpdated: true),
            new ComponentEntry("Scroll Bar", typeof(Foundations.ScrollBarPage), IsUpdated: true),
            new ComponentEntry("Segmented Control", typeof(SegmentedControlPage), IsUpdated: true),
            new ComponentEntry("Slider", typeof(SliderPage), IsUpdated: true),
            new ComponentEntry("Steering Bar", typeof(SteeringBarPage), IsUpdated: true),
            new ComponentEntry("Toggle Switch", typeof(ToggleSwitchPage), IsUpdated: true),
        }),
        new CatalogCategory("Surfaces", new[]
        {
            new ComponentEntry("Card", typeof(CardPage), IsUpdated: true),
            new ComponentEntry("Tooltip", typeof(TooltipPage), IsUpdated: true),
        }),
        new CatalogCategory("Navigation", new[]
        {
            new ComponentEntry("Masthead", typeof(MastheadPage), IsUpdated: true),
            new ComponentEntry("Tab Navigation", typeof(TabNavigationPage), IsUpdated: true),
        }),
        new CatalogCategory("Data Display", new[]
        {
            new ComponentEntry("Progress Bar", typeof(ProgressBarPage), IsUpdated: true),
        }),
    };

    /// <summary>Whether the component shown by <paramref name="pageType"/> is flagged
    /// <see cref="ComponentEntry.IsUpdated"/>. Lets a component page surface its own badge
    /// without each page hard-coding the flag — the catalog stays the single source.</summary>
    public static bool IsUpdated(Type pageType) => Find(pageType)?.IsUpdated ?? false;

    /// <summary>Whether the page shown by <paramref name="pageType"/> is flagged
    /// <see cref="ComponentEntry.IsPrimitive"/>. Same lookup as <see cref="IsUpdated"/>.</summary>
    public static bool IsPrimitive(Type pageType) => Find(pageType)?.IsPrimitive ?? false;

    private static ComponentEntry? Find(Type pageType)
    {
        if (Home.PageType == pageType) return Home;
        foreach (var node in Nodes)
        {
            switch (node)
            {
                case CatalogCategory category:
                    foreach (var entry in category.Items)
                        if (entry.PageType == pageType) return entry;
                    break;
                case CatalogLeaf leaf:
                    if (leaf.Entry.PageType == pageType) return leaf.Entry;
                    break;
                default:
                    throw new InvalidOperationException($"Unhandled catalog node '{node.GetType().FullName}'.");
            }
        }
        return null;
    }
}
