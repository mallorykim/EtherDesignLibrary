# L3 EtherSwitch + EtherScrollBar style contracts

Date: 2026-08-26  
Status: preview implementation evidence, not stable-release readiness

`EtherSwitch` and `EtherScrollBar` are the ninth L3 slice. They remain
**ResourceDictionary style contracts for stock WinUI types** — not custom
`EtherToggleSwitch` / `EtherScrollBar` controls with `DefaultStyleKey`.

**EtherSwitch** stays **keyed** (`x:Key="EtherSwitch"`). An implicit
`ToggleSwitch` style would restyle every bare switch in a consumer app. Call
sites keep `Style="{StaticResource EtherSwitch}"`.

**EtherScrollBar** stays **implicit** (`Style TargetType="ScrollBar"` with no
key) so every `ScrollViewer` — Gallery pages, dropdown menus, consumer hosts —
receives the 6 px thumb with no track and no stepper arrows.

## Implemented contract

### ToggleSwitch (`EtherSwitch`)

- Compiled `ResourceDictionary` class; constructor still calls
  `InitializeComponent()`. No `DefaultStyleKey`, no `ToggleSwitch` subclass.
- Keyed `EtherSwitch` style owns `UseSystemFocusVisuals=False` and
  `EtherSwitchTemplate`. There is no implicit `ToggleSwitch` style.
- Light, Dark, and High Contrast expose the same fourteen `EtherSwitch*`
  component resources. The template consumes those component keys only for
  color-bearing ThemeResources. Light and Dark alias previous product tokens
  (`BackgroundSwitchOff`, `BackgroundSwitchKnobDisabled`, `TextSecondary`,
  `border/focus`) and keep the navy-to-mint On-track gradient, white knob
  fills, glow, and three-layer knob shadow. High Contrast uses Windows
  `SystemColor*` dynamic resources rather than changing frozen Foundation
  tokens.
- Track/knob/gradient/disabled layering is preserved: Disabled fades the
  track, glow, shadows, and labels to 0.4; the knob stays opaque so the track
  cannot bleed through.

### ScrollBar (`EtherScrollBar`)

- Compiled `ResourceDictionary` class; no `DefaultStyleKey`, no `ScrollBar`
  subclass.
- Implicit `Style TargetType="ScrollBar"` owns transparent background,
  `IsTabStop=False`, and both vertical and horizontal templates (`VerticalRoot`
  / `HorizontalRoot`, 6 px thickness, 60 px thumb floor, stepper arrows
  collapsed).
- Thumb `CommonStates` PointerOver/Pressed swap to
  `EtherScrollBarThumbHoverBrush`. The Normal state does not overwrite
  `ThumbFill.Background`; the template `{ThemeResource EtherScrollBarThumbBrush}`
  stays live so Light/Dark rebind through nested Thumb templates. Hover still
  restores the default fill when the pointer leaves because the base
  ThemeResource remains the locally set value.
- Light, Dark, and High Contrast expose the same two `EtherScrollBar*`
  component resources. Thumb fills alias `BackgroundDropdownScrollThumb` and
  `BackgroundDropdownScrollThumbHover`. High Contrast uses
  `SystemColorWindowTextColor` / `SystemColorHighlightColor`.

## Gallery

- `ToggleSwitchPage` keeps Off/On default, hover, pressed, and disabled
  specimens and adds identifiable automation names on the interactive switch
  plus the Off and On defaults.
- Foundations `ScrollBarPage` keeps live vertical and horizontal
  `ScrollViewer` specimens (automation names) and state-preview swatches drawn
  with `EtherScrollBarThumbBrush` / `EtherScrollBarThumbHoverBrush`.

## Evidence and remaining exceptions

The unpackaged NuGet consumer fixture mounts:

- a **bare** `ToggleSwitch` (must not receive Ether chrome — keyed-not-implicit
  proof)
- default and On `ToggleSwitch` instances with `Style="{StaticResource
  EtherSwitch}"` (`toggleSwitch` marker)
- a standalone implicit `ScrollBar` plus a `ScrollViewer` (`scrollBar` marker)

`toggleSwitch` records keyed style resolution, rejected implicit restyle,
`TrackOff` / `TrackOn` / `KnobFill` parts, Off → On, disabled 0.4 track
opacity, and Light/Dark Off-track rebind. `scrollBar` records implicit 6 px
templates, both orientation roots, 60 px thumb floor, collapsed arrows, and
Light/Dark thumb rebind.

`Verify-EtherSwitchContract.ps1` and `Verify-EtherScrollBarContract.ps1`
additionally verify ResourceDictionary (not templated-control) metadata, keyed
vs implicit style shape, theme-key symmetry, High Contrast system resources,
no literal template hex colors, gallery automation names, runtime evidence
fields, and CI wiring. ResourceGraph continues to ignore Source-less compiled
dictionaries for merge-order equality and now asserts that Controls
`Generic.xaml` still instantiates `EtherScrollBar` and `EtherSwitch`.

This is structured runtime evidence, not a pixel screenshot baseline. The
High Contrast check is a static resource contract, not an on-device
verification across all Windows contrast themes. Formal Appium and
Accessibility Insights coverage remain L4 work. The repository's known
frozen global-token High Contrast deficit remains a release blocker; this
slice does not alter those tokens or claim stable readiness.
