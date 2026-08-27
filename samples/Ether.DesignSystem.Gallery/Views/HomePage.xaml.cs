using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace EtherSandbox.Views;

/// <summary>One rendered group on the Home index: a header and the entries under it.</summary>
public sealed record HomeSection(string Header, IReadOnlyList<ComponentEntry> Items);

/// <summary>
/// Landing page: the full component index, grouped by category, generated from
/// <see cref="ComponentCatalog.Nodes"/> — see that type's remarks for why this can't
/// drift from the NavigationView pane.
/// </summary>
public sealed partial class HomePage : Page
{
    public IReadOnlyList<HomeSection> Sections { get; }

    public HomePage()
    {
        Sections = BuildSections();
        this.InitializeComponent();
    }

    private static List<HomeSection> BuildSections()
    {
        var sections = new List<HomeSection>();
        foreach (var node in ComponentCatalog.Nodes)
        {
            switch (node)
            {
                case CatalogCategory category:
                    sections.Add(new HomeSection(category.Header, category.Items));
                    break;
                case CatalogLeaf leaf:
                    sections.Add(new HomeSection(leaf.Entry.Name, new[] { leaf.Entry }));
                    break;
                default:
                    throw new InvalidOperationException($"Unhandled catalog node '{node.GetType().FullName}'.");
            }
        }
        return sections;
    }

    private void Card_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { Tag: Type pageType })
            Frame.Navigate(pageType);
    }
}
