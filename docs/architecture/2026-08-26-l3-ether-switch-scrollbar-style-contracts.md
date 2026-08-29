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
- Keyed `EtherSwitch` style owns `UseSystemFocusVisuals=False`,
  `HighContrastAdjustment=None`, and `EtherSwitchTemplate`. There is no
  implicit `ToggleSwitch` style. The outer template root is `LayoutRoot`.
  Structural empty fills use unthemed `EtherSwitchTransparentBrush` so High
  Contrast dictionaries stay SystemColor-only. ToggleStates includes empty
  `Dragging` because stock `ToggleSwitch` goes there while the thumb is
  captured; chrome stays on the current Off/On setters.
- Light, Dark, and High Contrast expose the same nineteen `EtherSwitch*`
  component resources. The template consumes those component keys only for
  color-bearing ThemeResources. Light and Dark bind off-track, label, knob
  stroke, knob fill, and focus `Color` to primitives (`AlphaBlack10` / `AlphaWhite10`,
  `AlphaBlack70` / `AlphaWhite70`, `Gray0`, `Gray25` / `Gray50` / `Gray75`,
  `Blue50` / `Blue100` / `Blue200`, `Blue600` / `Blue500`). White stroke is 2 px
  OUTSIDE, painted 22×14, except Dark Disabled which uses `Gray500` fill and
  stroke (`EtherSwitchKnobDisabledBrush` / `EtherSwitchKnobDisabledStrokeBrush`).
  Drop shadow peeks below the knob only (navy at 21%, no navy ring). Off fill inset
  is 5 (host margin 3). On-track cyan-to-navy stays as
  component-dictionary hex (Figma `62138:28454` / `62138:28495` / `62138:28796`).
  High Contrast uses Windows `SystemColor*` dynamic resources.
- Product layout keeps `OffContent` / `OnContent` to the **left** of the 40×20
  track even though Figma draws the caption on the right. The control hugs
  **20** epx tall (`MinHeight=0`). Track uses corner radius 6. Knob **fill** is 18×10
  with a 2 px stroke **outside** (painted 22×14; Figma `strokeAlign`
  OUTSIDE). Knob host inset is 3 px left and right (On travel 12). Off knob fill is opaque `Gray25` (hover `Gray50`, pressed `Gray75`); On knob fill is opaque `Blue50` (hover `Blue100`, pressed `Blue200`). Dark Disabled knob fill and stroke are `Gray500`; Light Disabled fill is `Gray50`.
- Disabled fades the track and labels to **0.4** opacity (Figma Opacity
  row). Knob fill and stroke rebind through `EtherSwitchKnobDisabledBrush`
  / `EtherSwitchKnobDisabledStrokeBrush` (`Gray50` / `Gray0` in Light,
  `Gray500` in Dark) so Dark Disabled is not a near-white disc. Shadows
  and On-track glow collapse.

### ScrollBar (`EtherScrollBar`)

- Compiled `ResourceDictionary` class; no `DefaultStyleKey`, no `ScrollBar`
  subclass.
- Implicit `Style TargetType="ScrollBar"` owns transparent background via unthemed
  `EtherScrollBarTransparentBrush`, `IsTabStop=False`, `UseSystemFocusVisuals=False`,
  and both vertical and horizontal templates (`VerticalRoot` / `HorizontalRoot`,
  6 px thickness, 60 px thumb floor, stepper arrows collapsed). Track RepeatButton
  templates also consume that transparent brush so High Contrast dictionaries stay
  SystemColor-only.
- Thumb `CommonStates` PointerOver/Pressed swap to
  `EtherScrollBarThumbHoverBrush`. The Normal state does not overwrite
  `ThumbFill.Background`; the template `{ThemeResource EtherScrollBarThumbBrush}`
  stays live so Light/Dark rebind through nested Thumb templates. Hover still
  restores the default fill when the pointer leaves because the base
  ThemeResource remains the locally set value.
- Light, Dark, and High Contrast expose the same two `EtherScrollBar*`
  component resources. Light and Dark bind `Color` to primitive `Gray400` /
  `Gray500` (Figma Light spec `62110:22970`, Dark spec `62124:312` use the
  same hexes). High Contrast uses `SystemColorWindowTextColor` /
  `SystemColorHighlightColor`.

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
`TrackOff` / `TrackOn` / `KnobFill` parts, Off → On, labels left of the track,
disabled 0.4 track/label opacity with opaque Gray50 knobs, and Light/Dark Off-track rebind.
`scrollBar` records implicit 6 px
templates, both orientation roots, 60 px thumb floor, collapsed arrows, and
Light/Dark Gray400 Default fills (Figma uses the same primitive in both themes).

`Verify-EtherSwitchContract.ps1` and `Verify-EtherScrollBarContract.ps1`
additionally verify ResourceDictionary (not templated-control) metadata, keyed
vs implicit style shape, unthemed transparent overlay brushes, `LayoutRoot` and
empty `Dragging`, theme-key symmetry, High Contrast system resources,
no literal template hex colors, gallery automation names, runtime evidence
fields, and CI wiring. ResourceGraph continues to ignore Source-less compiled
dictionaries for merge-order equality and now asserts that Controls
`Generic.xaml` still instantiates `EtherScrollBar` and `EtherSwitch`.

WinUI convention AUDIT keeps these Switch exceptions on purpose (no pixel
change): custom part names (`OffLabel` / `OnLabel`, `KnobTransform`) instead of
stock `OffContentPresenter` / `OnContentPresenter` / `KnobTranslateTransform`,
because product layout puts captions left of the track and On travel is 12 px;
stock drag plumbing is unused. `KnobBase` stays a transparent layer and
`KnobSheen` stays Collapsed; `EtherSwitchKnobSheenBrush` remains a published
theme key. The keyed style does not set `IsTabStop=False` — `ToggleSwitch` must
remain a tab stop. Disabled does not fade `LayoutRoot` / `SwitchContent`
opacity (WinUI applies parent opacity per layer and would tint the On knob).

This is structured runtime evidence, not a pixel screenshot baseline. The
High Contrast check is a static resource contract, not an on-device
verification across all Windows contrast themes. Formal Appium and
Accessibility Insights coverage remain L4 work. The repository's known
frozen global-token High Contrast deficit remains a release blocker; this
slice does not alter those tokens or claim stable readiness.
