# SPEC — Tab Navigation (EtherTabNavigation / EtherTabItem)

Date 2026-08-31 · Goal: add a new "Tab Navigation" component. **Reskin an official WinUI control skeleton — if it can be copied, don't hand-write it from scratch.**
Reference: https://github.com/microsoft/WinUI-Gallery, microsoft-ui-xaml (ListView / ListViewItem / ListViewItemPresenter official templates).

Figma (file `ursRC201v8IiVeafliI45F`):
- Single tab component `Tab_buttons`: `Status=Default` node 62517:7077; variant set (Default/Selected/Hover/Pressed) node 62517:7076 (light) / 62517:8474 (dark)
- Assembled bar `Tab nav`: node 62597:5204

---

## 0. Decisions (final, do not change again)

- **Base = reskin the official single-select ItemsControl (ListView)**. The user has finalized this: "ListView/Selector selector."
  - Only handles **selection** (exposing `SelectedIndex`/`SelectedItem`/`SelectionChanged`, all provided free by ListView), **does not host/switch content** — content is handled by the app itself, consistent with the existing `EtherSegmentedControl` positioning.
- **Fill in hover/pressed**: the Figma variant set already draws the four states Default/Selected/Hover/Pressed (see the §2 token table — all have precise variable values, not invented).
- **disabled**: Figma does **not** define this → per library convention, fill it in with "overall opacity reduced to ~0.4" (consistent with `EtherSegmentedControl`'s `Disabled`).
- **Icon optional**: `EtherTabItem.Icon` (`IconElement`, default `null` → the icon area is entirely hidden with no placeholder gap).

## 1. Naming and placement

- Control classes (typed subclass, consistent with other Ether* controls in the library):
  - `EtherTabNavigation : ListView` — `Controls/Navigation/EtherTabNavigation.cs`
  - `EtherTabItem : ListViewItem` — same file or `Controls/Navigation/EtherTabItem.cs`
  - Naming aligned with WinUI convention (TabView/TabViewItem, NavigationView/NavigationViewItem).
- Template dictionary: `Controls/Navigation/EtherTabNavigation.xaml` (a pure XAML `ResourceDictionary`, organized the same way as `EtherSegmentedControl.xaml`).
- Registration: add one line inside `MergedDictionaries` in `src/Ether.DesignSystem.Controls/Themes/Generic.xaml`
  `<ResourceDictionary Source="ms-appx:///Ether.DesignSystem.Controls/Controls/Navigation/EtherTabNavigation.xaml" />`
  (placing it near the Masthead line is fine).
- Gallery: see §6.

## 2. Exact specification (all from Figma, do not change the values)

**Single tab (pill)**
| Item | Value | Library token |
|---|---|---|
| Padding | 8 (horizontal) × 6 (vertical) | `Spacing8` / `Spacing6`, or literal `8,6` |
| Corner radius | 8 | `RadiusMd` (= `radius/control`) |
| Icon↔text spacing | 4 | `Spacing4` |
| Icon size | 12 × 12 | `icon-size-2xs` (literal 12 also acceptable) |
| Font | Instrument Sans **SemiBold 12**, line height 1, centered | `InstrumentSans` / `Size12` / `WeightSemibold` |

**Tab bar container**
| Item | Value | token |
|---|---|---|
| Layout | Horizontal, **left-aligned**, content width (**not equal-width**) | — |
| Tab spacing | 10 | `Spacing10` (StackPanel `Spacing="10"`) |
| Container padding | 8 × 6 | `8,6` |
| Count | Dynamic, **minimum 2** | — |

**Four-state colors (light / dark — all exact Figma variable values, verified against primitives)**
| State | Background light | Background dark | Text/icon light | Text/icon dark |
|---|---|---|---|---|
| Default | Transparent | Transparent | `AlphaBlack70` | `AlphaWhite70` |
| Selected | `Gray1000`(#000) | `Gray0`(#fff) | `Gray0` | `Gray1000` |
| Hover | `AlphaBlack4`(#0A000000) | `AlphaWhite4`(#0AFFFFFF) | same as Default | same as Default |
| Pressed | `AlphaBlack6`(#0F000000) | `AlphaWhite6`(#0FFFFFFF) | same as Default | same as Default |

Key points:
- **Selected background = Figma `background/Tabs/Selected`, whose value exactly equals the existing semantic token `background/surface-inverse`** (light Gray1000 / dark Gray0). Selected text/icon = the existing `text/inverse` / `icon/inverse` (light Gray0 / dark Gray1000). Default text/icon = the existing `text/secondary` / `icon/secondary` (light AlphaBlack70 / dark AlphaWhite70). These semantic tokens **already exist**, no new ones are needed.
- **The Selected state stays unchanged under hover/pressed** (Figma has no Selected+Hover variant) — i.e. `SelectedPointerOver`/`SelectedPressed` reuse the Selected appearance and do not layer a hover/pressed background on top.
- The values in the table above have been checked one by one against the Figma variables: Default text `#000000b2`=AlphaBlack70 / `#ffffffb2`=AlphaWhite70; Hover `#0000000a`=AlphaBlack4 / `#ffffff0a`=AlphaWhite4; Pressed `#0000000f`=AlphaBlack6 / `#ffffff0f`=AlphaWhite6; Selected `#000000`=Gray1000 / `#ffffff`=Gray0 (dark). All primitive keys already exist in `EtherPrimitives.xaml`.

## 3. Tokens (following `EtherSegmentedControl`'s two-layer approach)

`EtherSegmentedControl`'s existing approach is: semantic tokens go into `EtherColors.xaml` (aligned with Figma naming), and control-private brushes go into the control dictionary's own `ThemeDictionaries` (including HighContrast). **Follow the same approach.**

### 3a. Semantic tokens (add to `src/Ether.DesignSystem.Foundation/Resources/Tokens/EtherColors.xaml`)
Add to both the Light and Dark `ThemeDictionary` (named per the repo's lowercase-slash convention for Figma `background/Tabs/*`, following the existing `background/menuitem/*`):
```
<!-- background / Tabs -->
background default : Transparent (both modes)
background selected: Light Gray1000 / Dark Gray0
background hover   : Light AlphaBlack4 / Dark AlphaWhite4
background pressed : Light AlphaBlack6 / Dark AlphaWhite6
```
key: `background/tabs/default` `background/tabs/selected` `background/tabs/hover` `background/tabs/pressed`.
(This is the "token names match source" memory requirement; even though selected is equivalent to `surface-inverse`, build it separately under the Figma name for traceability.)

### 3b. Control-private brushes (in `EtherTabNavigation.xaml`'s `ThemeDictionaries`: Light / Dark / HighContrast)
| brush key | Light | Dark | HighContrast |
|---|---|---|---|
| `EtherTabItemBackgroundSelectedBrush` | `background/tabs/selected`(Gray1000) | (Gray0) | `SystemColorHighlightColor` |
| `EtherTabItemBackgroundHoverBrush` | `background/tabs/hover`(AlphaBlack4) | (AlphaWhite4) | `SystemColorHighlightColor` |
| `EtherTabItemBackgroundPressedBrush` | `background/tabs/pressed`(AlphaBlack6) | (AlphaWhite6) | `SystemColorHighlightColor` |
| `EtherTabItemForegroundBrush` (default text/icon) | `AlphaBlack70` | `AlphaWhite70` | `SystemColorWindowTextColor` |
| `EtherTabItemForegroundSelectedBrush` | `Gray0` | `Gray1000` | `SystemColorHighlightTextColor` |
| `EtherTabNavigationFocusStrokeBrush` | `Blue600` | `Blue500` | `SystemColorHighlightColor` |

(Under HC, the hover/pressed/selected background all use `SystemColorHighlightColor`, and text uses `SystemColorHighlightTextColor` — consistent with `EtherSegmentedControl`'s HC handling; `HighContrastAdjustment="None"`.)

## 4. Skeleton implementation (key: copy the official one, don't hand-write from 0)

### 4a. `EtherTabNavigation : ListView`
- `DefaultStyleKey = typeof(EtherTabNavigation)`.
- Container generation (so children automatically become EtherTabItem, supporting both explicit children and `ItemsSource` plain-text tabs):
  - `protected override bool IsItemItsOwnContainerOverride(object item) => item is EtherTabItem;`
  - `protected override DependencyObject GetContainerForItemOverride() => new EtherTabItem();`
- Default properties (as Setters in the default Style):
  - `SelectionMode = Single` (single-select; a single-select ListView does not deselect on a repeat click — matches tab semantics).
  - `IsItemClickEnabled = False` (goes through selection, not click).
  - `Padding = "8,6"`, `HorizontalAlignment=Left`, `Background=Transparent`.
  - `IsTabStop=False`, `UseSystemFocusVisuals=False`, `HighContrastAdjustment=None`.
  - `ItemsPanel` = horizontal `StackPanel` (`Orientation=Horizontal` `Spacing=10` `HorizontalAlignment=Left`).
- Template (**start from the official ListView default template and modify**, keep the official part names): `Border(Padding) > ScrollViewer > ItemsPresenter`.
  - `ScrollViewer`: `HorizontalScrollMode=Disabled` `HorizontalScrollBarVisibility=Hidden` `VerticalScrollMode=Disabled` `VerticalScrollBarVisibility=Disabled` (the tab bar has no scrollbar).
  - Zero out ListView's built-in whitespace (Padding is delegated to the Border above).

### 4b. `EtherTabItem : ListViewItem`
- `DefaultStyleKey = typeof(EtherTabItem)`.
- **New dependency property `Icon` (`IconElement`, default `null`)**: leading icon, 12×12; when `null`, the icon area is `Collapsed` and leaves no 4px gap.
  - Icon coloring follows the text: bind the `Foreground` of the icon's host (`ContentPresenter`/container) to the same brush as the text, so the icon also switches to `inverse` when Selected.
  - `null→Collapsed`: use `OnApplyTemplate` to get `IconPresenter` + an `Icon` property-changed callback to switch `Visibility` and assign `Content` (x:Bind cannot be used on an IconElement in the template; avoid relying on a string converter).
- Style Setters: `MinHeight=0` `MinWidth=0` `Margin=0` `HorizontalAlignment=Left` (content width, no stretch) `HorizontalContentAlignment=Center` `VerticalContentAlignment=Center` `Padding="8,6"` `FontFamily=InstrumentSans` `FontSize=Size12` `FontWeight=WeightSemibold` `UseSystemFocusVisuals=False` `HighContrastAdjustment=None`.

- **Reskinning approach (pick one, in priority order)**:
  1. **Preferred: retarget the brush properties of the official `ListViewItemPresenter`** (the most "copy the official one", smallest change, state logic stays native).
     That is, keep the `<ListViewItemPresenter .../>` from the official ListViewItem template, and only retarget its
     `SelectedBackground`/`SelectedPointerOverBackground`/`SelectedPressedBackground` → `EtherTabItemBackgroundSelectedBrush`,
     `PointerOverBackground` → Hover brush, `PressedBackground` → Pressed brush,
     `SelectedForeground` → `EtherTabItemForegroundSelectedBrush`, default `Foreground` → `EtherTabItemForegroundBrush`,
     `CornerRadius=8`, `ContentMargin="8,6"`, and **remove the selection indicator bar** (new SDK: `SelectionIndicatorMode="None"`; turn off the check mark via `CheckMode`).
     **Prerequisite**: first confirm whether WindowsAppSDK **2.3.1**'s `Microsoft.UI.Xaml.Controls.Primitives.ListViewItemPresenter` has the properties above (especially `SelectionIndicatorMode`, `SelectedForeground`). If it does, use this approach.
  2. **Fallback: a fully hand-drawn `ControlTemplate` + layered opacity backgrounds** (used when 2.3.1's presenter lacks the key properties).
     Copy the proven approach from `EtherSegmentTemplate` in `EtherSegmentedControl.xaml` (**this approach has already solved the "ThemeResource setter doesn't revert on leaving a state" pitfall in this repo**, by toggling Opacity instead of changing Background directly):
     - Root `Grid`, containing stacked `Border`s: `HoverLayer`(Hover brush), `PressedLayer`(Pressed brush), `SelectedLayer`(Selected brush), each with `CornerRadius=8`, `Opacity=0`.
     - Content: `StackPanel Orientation=Horizontal Spacing=4` = `IconPresenter`(12×12, `Icon`) + text `ContentPresenter`/`TextBlock`; `Foreground` defaults to `EtherTabItemForegroundBrush`.
     - `FocusRing` `Border` (`IsHitTestVisible=False` `Opacity=0`, `BorderBrush=EtherTabNavigationFocusStrokeBrush` `BorderThickness=2` `CornerRadius` slightly larger than 8, `Margin=-3`).
     - VSM group **CommonStates** (use the official ListViewItem state names, driven by ListView):
       `Normal`(all 0) · `PointerOver`(HoverLayer=1) · `Pressed`(PressedLayer=1) ·
       `Selected` / `SelectedUnfocused`(SelectedLayer=1 + text & icon Foreground=Selected brush) ·
       `SelectedPointerOver` / `SelectedPressed`(same as Selected, **does not layer a hover/pressed background**) ·
       `Disabled` / `SelectedDisabled`(root `Opacity=0.4`, otherwise same as their respective base state).
       Use `Setter` (which cleanly reverts for both Opacity/Foreground) rather than changing Background.
     - VSM group **FocusStates**: `Focused`(FocusRing.Opacity=1) · `Unfocused` · `PointerFocused`(empty).
       Note: ListViewItem's FocusStates are driven by the framework (unlike the dead group on CheckBox/RadioButton), but it still needs **runtime** confirmation that the focus ring only appears on keyboard focus.
- Keep the other VSM groups in the official template (`DisabledStates`/`MultiSelectStates`/`ReorderHintStates`/`DragStates`/`DataVirtualizationStates`, etc.) unchanged as-is; only reshape `CommonStates` + `FocusStates`.

### 4c. Keyboard / accessibility
- ListView already provides arrow-key movement + selection (`SingleSelectionFollowsFocus` defaults to true → arrow keys switch selection directly, matching tab semantics). Keep the default.
- Accessibility: ListView exposes the List + SelectionItem patterns, acceptable for v1. **Optional enhancement** (not required): provide an AutomationPeer for EtherTabItem that reports a `Tab`/`TabItem` control type; if implementation carries risk, skip it and record a TODO.

## 5. Usage (target API, for Gallery/docs)
```xml
<controls:EtherTabNavigation SelectedIndex="0" SelectionChanged="...">
    <controls:EtherTabItem Content="Overview">
        <controls:EtherTabItem.Icon><FontIcon Glyph="&#xE80F;"/></controls:EtherTabItem.Icon>
    </controls:EtherTabItem>
    <controls:EtherTabItem Content="Details"/>
    <controls:EtherTabItem Content="History"/>
</controls:EtherTabNavigation>
```
- Explicit `EtherTabItem` children are the primary usage (icon via `Icon`). Binding `ItemsSource` to plain-text tabs also works out of the box (no icon).

## 6. Gallery (following the `SegmentedControlPage` template)
- `samples/.../ComponentCatalog.cs`: add one line under the **Navigation** category (next to Masthead)
  `new ComponentEntry("Tab Navigation", typeof(TabNavigationPage), IsUpdated: true),`
- New page `samples/.../Views/Navigation/TabNavigationPage.xaml`(+`.cs`), namespace `EtherSandbox.Views.Navigation`, using `views:ComponentPage` + `views:ControlExample`:
  - **InteractiveContent**: instances with 2 / 3 / 5 tabs; at least one group with a leading icon; `SelectionChanged` writes the `SelectedItem` text to `OutputText`; `SourceXaml` bound to `SpecimenXaml`.
  - **StatesContent**: four static samples for Default / Selected / Hover / Pressed. Follow that page's existing `HandRadioButton.PreviewState` mechanism — use a Gallery-only `EtherTabItem` subclass (e.g. `HandTabItem`) with a `PreviewState` to force the visual state, plus `IsHitTestVisible=False`; if that's too costly, force it (non-interactively) via `VisualStateManager.GoToState` in code-behind on Loaded.
  - `HasDisabledToggle="True"` (demonstrates the disabled opacity reduction).
- The x:Uid localization keys can simply follow the same pattern used by similar pages.

## 7. Acceptance
- x64 / Debug build with 0 errors (first `dotnet workload restore Ether.DesignSystem.slnx`, then
  `dotnet build src/Ether.DesignSystem.Controls/Ether.DesignSystem.Controls.csproj -p:Platform=x64 -p:Configuration=Debug -restore`; same method for Gallery).
- Runtime (self-verify, don't make the user click): navigating the Gallery to the Tab Navigation page does not crash; the four states, light/dark, and with/without-icon appearance match the table above; keyboard arrow keys switch selection, the focus ring appears only on keyboard focus; disabled reduces opacity.
- Do not modify SegmentedControl or other components.

## 8. Explicitly out of scope (unless decided otherwise)
- Does not host/switch content (no content area).
- No overflow/"more" menu, no close/add/drag (that's the domain of TabView/NavigationView).
- Per-item icons on `ItemsSource` data items (would require an ItemTemplate) is out of scope — use explicit `EtherTabItem` for icon scenarios.
