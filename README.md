# Ether Design System

Ether is a preview WinUI 3 design system for internal applications: reusable controls, design
tokens, and an optional backend-telemetry adapter, all skinned from official WinUI control
skeletons per this repo's [AGENTS.md](AGENTS.md) rule.

> **Status:** preview (`0.1.0-preview.5`), distributed **internally only** through a private
> GitHub Packages feed — not nuget.org. `x64` only. See
> [`docs/consumers/getting-started.md`](docs/consumers/getting-started.md) for full setup,
> troubleshooting, and per-control reference; this file is a short map and quick-start.

## The three packages

| Package | What it is for |
| --- | --- |
| `Ether.DesignSystem.Foundation` | Design tokens, fonts, and icon assets. Supported entry point is `Themes/Foundation.xaml`; consumed transitively by Controls. |
| `Ether.DesignSystem.Controls` | The 15 WinUI 3 controls themselves (`EtherButton`, `EtherInput`, `EtherSegmentedControl`, `EtherTabNavigation`, ...) plus the design-system resource dictionary, `Themes/DesignSystem.xaml`. This is the package most applications reference for XAML usage. |
| `Ether.DesignSystem.Interactions` | Optional, UI-only adapter (`ControlInteractionAdapter`) that turns control events into versioned, backend-consumable interaction envelopes for an application's own outbox. Not required for ordinary MVVM data binding — see [§7 of the getting-started guide](docs/consumers/getting-started.md#7-interactions-adapter-optional-backend-telemetry). |

Foundation and Controls are released in lockstep and should be upgraded together; Interactions
should track the version combination announced with a given release.

## Install

```powershell
dotnet add package Ether.DesignSystem.Foundation --version 0.1.0-preview.5
dotnet add package Ether.DesignSystem.Controls --version 0.1.0-preview.5
dotnet add package Ether.DesignSystem.Interactions --version 0.1.0-preview.5
```

These resolve only from the private GitHub Packages feed described in
[§1 of the getting-started guide](docs/consumers/getting-started.md#1-configuring-github-packages-access) —
`dotnet add package` alone will not authenticate you against it.

## Hello button (quick-start)

Merge `DesignSystem.xaml` in `App.xaml`, then use the `ether:` namespace:

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

That's the whole quick-start — full project setup (target framework, `app.manifest`, resource
merging pitfalls) and every control's copy-paste reference, including how to bind data and wire an
action, are in the getting-started guide linked below.

## Where to go next

- [`docs/consumers/getting-started.md`](docs/consumers/getting-started.md) — full consumer guide:
  GitHub Packages access, a minimal WinUI 3 app, troubleshooting, known boundaries, a
  copy-paste "appearance → bind data → wire an action" reference for all 15 controls, MVVM
  binding patterns, and the Interactions adapter.
- [`docs/internal/consumability/`](docs/internal/consumability/) — per-component consumability +
  completeness review reports (evidence-grounded, file:line citations) and
  [`_SUMMARY.md`](docs/internal/consumability/_SUMMARY.md) for the rolled-up status.

## Repository map

- `src/` — the three packages' source (`Ether.DesignSystem.Foundation`, `Ether.DesignSystem.Controls`, `Ether.DesignSystem.Interactions`).
- `samples/Ether.DesignSystem.Gallery/` — a WinUI 3 gallery app exercising every control, including live data-binding demos.
- `tests/Ether.DesignSystem.ConsumerFixtures/` — out-of-repo-shaped consumer fixtures (packaged and unpackaged hosts) that prove the public API surface at runtime.
- `docs/internal/` — planning, review, and release-readiness documents (not consumer-facing).
- `scripts/Gates.psd1` — the single source of truth for this repository's verification gates (CI and local-runtime).
