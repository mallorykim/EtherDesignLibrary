# EtherTabNavigation Icons Disappearing on Re-navigation: Root Cause and Control-Level Fix

Date: 2026-09-01
Target version: WindowsAppSDK 2.3.1 / Microsoft.WindowsAppSDK.WinUI 2.3.0 / .NET 8

## Root cause

Page reconstruction and the ListView container lifecycle are the stable trigger; the real resource-ownership bug is that, although the old implementation created a new `PathIcon` each time, it still obtained and reused the already-coerced `Geometry` object from the same resource `Style`.

WinUI's `Geometry` inherits from `DependencyObject`, but has no `Clone`/`Freeze` sharing mechanism like WPF's `Freezable`. [Microsoft's Geometry API](https://learn.microsoft.com/en-us/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.media.geometry) lists its object model; microsoft-ui-xaml's [shared-Geometry issue #827](https://github.com/microsoft/microsoft-ui-xaml/issues/827) directly documents that using the same Geometry across multiple PathIcon/Path elements fails, and that WinUI has no platform clone API. ListViewBase also has a well-defined container-recycling phase; the [`InRecycleQueue` documentation](https://learn.microsoft.com/en-us/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.controls.containercontentchangingeventargs.inrecyclequeue) explains that a control can release related references when its container enters the recycle queue.

Based on this, the full chain of this bug is:

1. The Data Setter of a Style such as `IconHome` coerces `"F1 M..."` into a Geometry dependency object at load time, and it persists for as long as the app resource does.
2. The first batch of ListViewItem/PathIcon elements that use this Geometry establishes a WinUI internal rendering association.
3. When the page is navigated away from, the ListView items and template tree are cleaned up; the app-level Style/Geometry still exists.
4. When the page is reconstructed, the new PathIcon obtains the same Geometry again. WinUI cannot safely share this object, which in the current version manifests as the slot existing but the glyph being empty.

Button not triggering the same failure only shows that it doesn't hit this particular ListView cleanup/reconstruction timing — it does not make shared Geometry a supported usage. Within this conclusion, "sharing is unsupported" has direct official evidence; "2.3.1 specifically manifests as blank in this timing" is a version-specific judgment derived from a stable reproduction, and still needs runtime screenshot verification.

## Fix

`EtherTabItem.BuildIcon` now reads Setter.Value as its correct runtime type `Geometry`, and deep-copies the entire object graph:

- Geometry: PathGeometry, GeometryGroup, EllipseGeometry, LineGeometry, RectangleGeometry;
- Path: PathFigure, all WinUI PathSegment-derived types, PointCollection;
- Transform: all WinUI 2.3 Transform-derived types and TransformGroup.

It then creates `PathIcon { Data = clonedGeometry, Width = 14, Height = 14 }`. No fallback path ever hands the resource Style or its underlying Geometry to EtherTabItem's visual tree again.

This establishes a lifecycle-independent invariant: every rendered EtherTabItem icon exclusively owns its own PathIcon and complete Geometry object graph. Page reconstruction creates a new graph; when a container is reused, an Icon DP change creates a new graph; template reapplication also creates a new graph. The container hooks no longer bear the responsibility of "fixing a shared object", so there is no need to change ListView's selection, keyboard, focus, or automation semantics.

## Compatibility and verification

- The `Icon="Home"` string API, 14×14 size, 8 EPX gap, and the foreground color inverting with the selected state are all preserved.
- `EtherIconGeometries.xaml` is unchanged; a structural check confirms 430 Styles correspond to 430 Data Setters, and there are no other Setters that would be lost by not applying the Style.
- The official ListView/ListViewItem control skeleton, VisualState names, and selection semantics are unchanged.
- The Gallery's `NavigationCacheMode.Required` workaround has been removed, restoring genuine page-reconstruction conditions.
- Targeted Controls build: 0 warnings, 0 errors.
- Gallery built in an isolated output directory with the same x64/Debug/restore/`-m:1` parameters: 0 warnings, 0 errors. The default output directory was locked by a running Gallery process at the time; the only failure occurred at the DLL-copy stage.

## Still needs runtime verification

Use UI Automation to perform: open Tab Navigation → screenshot the three icons → navigate away → return → screenshot again, repeating at least two rounds. Also switch across the three tabs to confirm Home/Document/Clock are all present and the foreground color inverts on the selected pill; this part cannot be replaced by a static build.
