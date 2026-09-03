# Ether Design System — handoff

Everything a consuming WinUI 3 application team needs to adopt the Ether Design System, in one
place. Ether is a preview library of reusable, WinUI-skeleton-based controls distributed **internally
only** through a private GitHub Packages feed (`0.1.0-preview.7`, `x64` only).

## Start here

1. **[getting-started.md](getting-started.md)** — the full setup + consumer guide:
   - §1 GitHub Packages / PAT access, §2 a minimal WinUI 3 app + the resource dictionary you must
     merge, §3 troubleshooting, §4 known boundaries (what the design system owns), §6 MVVM binding
     patterns, §7 the optional Interactions telemetry adapter, §8 versioning.
2. **[components/](components/README.md)** — one usage page per control (all 17): what it is,
   copy-paste markup, the consumer API (Ether-specific + the inherited WinUI members you'll use), how
   to bind and wire actions, and which appearance overrides take effect vs. are locked.

## The 30-second version

```powershell
dotnet add package Ether.DesignSystem.Foundation --version 0.1.0-preview.7
dotnet add package Ether.DesignSystem.Controls --version 0.1.0-preview.7
dotnet add package Ether.DesignSystem.Interactions --version 0.1.0-preview.7
```

Merge the design-system dictionary in `App.xaml`, then use the `ether:` namespace:

```xml
<Application.Resources>
    <ResourceDictionary>
        <ResourceDictionary.MergedDictionaries>
            <XamlControlsResources xmlns="using:Microsoft.UI.Xaml.Controls" />
            <ResourceDictionary Source="ms-appx:///Ether.DesignSystem.Controls/Themes/DesignSystem.xaml" />
        </ResourceDictionary.MergedDictionaries>
    </ResourceDictionary>
</Application.Resources>
```

```xml
<Window xmlns:ether="using:Ether.DesignSystem.Controls">
    <ether:EtherButton Content="Save"
                       Command="{x:Bind ViewModel.SaveCommand}"
                       Style="{StaticResource EtherButtonPrimary}"/>
</Window>
```

The packages resolve only from the private feed — `dotnet add package` alone won't authenticate you.
See [getting-started.md §1](getting-started.md#1-configuring-github-packages-access).

## The controls (17)

| Group | Controls |
|-------|----------|
| Actions | [EtherButton](components/EtherButton.md), [EtherIntelligenceButton](components/EtherIntelligenceButton.md) |
| Inputs & selection | [EtherInput](components/EtherInput.md), [EtherDropdown](components/EtherDropdown.md), [EtherCheckbox](components/EtherCheckbox.md), [EtherRadioButton](components/EtherRadioButton.md), [EtherSwitch](components/EtherSwitch.md), [EtherSlider](components/EtherSlider.md), [EtherSegmentedControl](components/EtherSegmentedControl.md), [EtherPanelTabs](components/EtherPanelTabs.md) |
| Data display | [EtherProgressBar](components/EtherProgressBar.md), [EtherSteeringBar](components/EtherSteeringBar.md), [EtherScrollBar](components/EtherScrollBar.md) |
| Navigation & surfaces | [EtherMasthead](components/EtherMasthead.md), [EtherTabNavigation](components/EtherTabNavigation.md), [EtherCard](components/EtherCard.md), [EtherTooltip](components/EtherTooltip.md) |

## Also useful

- Release notes: [`docs/releases/0.1.0-preview.7.md`](../docs/releases/0.1.0-preview.7.md) — what
  changed in this preview (new components, `ValueContent` → `ValueFormat`, reskins, Roboto).
- The exhaustive per-property boundary matrix for each control lives in
  [`docs/internal/consumability/`](../docs/internal/consumability/) (internal, evidence-grounded).

> These guides are verified against the shipped `PublicAPI.Unshipped.txt`, the control source, and
> `scripts/UnsupportedProperties.psd1`; the getting-started §4 boundary table is CI-gate-checked
> against that same source of truth.
