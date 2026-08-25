# Phase 0 Pilot: Progress Bar + Toggle Switch Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Prove the "component owns its own template" authoring pattern from
`docs/superpowers/plans/2026-08-06-self-contained-component-authoring-plan.md` (Phase 0) on the
two lowest-risk controls — Progress Bar and Toggle Switch — before touching Button.

**Architecture:** For each control, move its `ControlTemplate`/`Style` out of
`Resources/Ether<X>.xaml` into a co-located `Controls/Ether<X>.xaml` `ResourceDictionary` (no
`x:Class` — this is not a `UserControl`, the C# class stays exactly what it is today), and
repoint the single `App.xaml` merge line from `Resources/` to `Controls/`. This is the
guaranteed-safe explicit-merge mechanism documented in the parent plan's "Mechanism refinement"
note — no `DefaultStyleKey`, no `Themes/Generic.xaml`, no behavior change to either control
class. Progress Bar's style becomes **implicit** (no `x:Key`, since `EtherProgressBar` is a
custom type — same pattern already proven safe by `EtherDropdown`), which also lets the 5 call
sites in `ProgressBarPage.xaml` drop their now-redundant `Style="{StaticResource
EtherProgressBar}"` attribute. Toggle Switch's style **stays keyed** (`x:Key="EtherSwitch"`)
because `ToggleSwitch` is a stock framework type — making it implicit would silently restyle
any bare `<ToggleSwitch>` anywhere else in the app, the exact class of mistake the parent plan's
review flagged for `EtherScrollBar`.

**Tech Stack:** WinUI3 / .NET 8 (`net8.0-windows10.0.19041.0`), unpackaged WinExe,
`EtherComponentSandbox.csproj`. No test framework in this repo — verification is
`dotnet build` + manual visual/interaction confirmation in the running app, per the parent
plan's Verification section.

---

## Task 1: Relocate Progress Bar's template into `Controls/`

**Files:**
- Create: `Controls/EtherProgressBar.xaml`
- Modify: `App.xaml:24` (merge source path)
- Modify: `Views/DataDisplay/ProgressBarPage.xaml:20,42,53,64,75` (drop redundant `Style=`)
- Delete: `Resources/EtherProgressBar.xaml`

- [ ] **Step 1: Create `Controls/EtherProgressBar.xaml`**

Same template content as the current `Resources/EtherProgressBar.xaml`, relocated, with the
style made implicit (drop `x:Key="EtherProgressBar"` — `TargetType="controls:EtherProgressBar"`
alone is enough for a custom type to resolve automatically once merged):

```xml
<!--
    Ether Design System Progress Bar

    Figma source: ursRC201v8IiVeafliI45F, node 60609:946 (spec-sheet-progressbar)
    Vertical layout, 8 px gap, padding 4 top / 12 bottom. Optional title (left) and
    value (right) labels in Instrument Sans SemiBold 14 (title #000, value 70% black).
    Track: full width, 4 px tall, #E8EDEF, fully rounded. Progress: a 4 px gradient fill
    sized to Value/Maximum via two star columns (see EtherProgressBar.cs).

    Gradient runs deep-blue at the start to cyan at the leading edge — the reverse of the
    spec's textual stop order, matched to the rendered preview (pixel-verified):
      start #0021F3 -> #0015FF -> #0EB2FF -> #40E1FD leading edge.
    It spans the FILLED segment (StartPoint/EndPoint 0..1 over the fill Border), so the
    leading edge stays cyan at any progress value, exactly as the preview shows.

    Colours are literal hex per the spec (this component is not wired to the EtherColors
    token set), consistent with the other spec-sheet components.

    Implicit style (no x:Key): this dictionary is merged once from App.xaml, and any
    EtherProgressBar automatically picks up this template with no Style= at the call site.
-->
<ResourceDictionary
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    xmlns:controls="using:EtherSandbox.Controls">

    <Style TargetType="controls:EtherProgressBar">
        <Setter Property="HorizontalAlignment" Value="Stretch"/>
        <Setter Property="Template">
            <Setter.Value>
                <ControlTemplate TargetType="controls:EtherProgressBar">
                    <!-- Padding 4 top / 12 bottom per spec. -->
                    <Grid Padding="0,4,0,12">
                        <Grid.RowDefinitions>
                            <RowDefinition Height="Auto"/>
                            <RowDefinition Height="Auto"/>
                        </Grid.RowDefinitions>

                        <!-- Label row. Title / ValueContent are object content slots (not fixed
                             Text): a string renders in the styles set here, an element renders
                             as-is. Collapsed wholesale (incl. its 8 px gap) when neither shows;
                             individual label visibility is set in code. -->
                        <Grid x:Name="LabelRow" Grid.Row="0" Margin="0,0,0,8">
                            <ContentPresenter x:Name="TitleText"
                                              Content="{TemplateBinding Title}"
                                              HorizontalAlignment="Left"
                                              FontFamily="{StaticResource InstrumentSans}"
                                              FontSize="14"
                                              FontWeight="SemiBold"
                                              Foreground="#FF000000"/>
                            <ContentPresenter x:Name="ValueLabel"
                                              Content="{TemplateBinding ValueContent}"
                                              HorizontalAlignment="Right"
                                              FontFamily="{StaticResource InstrumentSans}"
                                              FontSize="14"
                                              FontWeight="SemiBold"
                                              Foreground="#B3000000"/>
                        </Grid>

                        <!-- Track (4 px) with the gradient fill overlaid. The fill's width is an
                             actual laid-out star column (not a scale transform) so its 2 px rounded
                             ends stay round at any value; the gradient spans the fill, keeping the
                             cyan leading edge. Motion comes from Value changing over time. -->
                        <Grid Grid.Row="1" Height="4">
                            <Border Background="#FFE8EDEF" CornerRadius="2"/>
                            <Grid>
                                <Grid.ColumnDefinitions>
                                    <ColumnDefinition x:Name="FillColumn" Width="0*"/>
                                    <ColumnDefinition x:Name="RestColumn" Width="1*"/>
                                </Grid.ColumnDefinitions>
                                <Border Grid.Column="0" CornerRadius="2">
                                    <Border.Background>
                                        <LinearGradientBrush StartPoint="0,0.5" EndPoint="1,0.5">
                                            <GradientStop Color="#0021F3" Offset="0.00"/>
                                            <GradientStop Color="#0015FF" Offset="0.33"/>
                                            <GradientStop Color="#0EB2FF" Offset="0.67"/>
                                            <GradientStop Color="#40E1FD" Offset="1.00"/>
                                        </LinearGradientBrush>
                                    </Border.Background>
                                </Border>
                            </Grid>
                        </Grid>
                    </Grid>
                </ControlTemplate>
            </Setter.Value>
        </Setter>
    </Style>

</ResourceDictionary>
```

- [ ] **Step 2: Repoint the `App.xaml` merge**

In `App.xaml`, change:

```xml
                <ResourceDictionary Source="Resources/EtherProgressBar.xaml"/>
```

to:

```xml
                <ResourceDictionary Source="Controls/EtherProgressBar.xaml"/>
```

- [ ] **Step 3: Drop the now-redundant `Style=` at all 5 call sites in `ProgressBarPage.xaml`**

Each of the following 5 occurrences currently reads (attributes may wrap differently — match by
the `Style="{StaticResource EtherProgressBar}"` line and delete just that line, keeping every
other attribute on the element unchanged):

```xml
Style="{StaticResource EtherProgressBar}"
```

Delete that line from:
- Line 20 (`SimBar`, in `InteractiveContent`)
- Line 42 (`TITLE + VALUE` swatch)
- Line 53 (`TITLE ONLY` swatch)
- Line 64 (`VALUE ONLY` swatch)
- Line 75 (`BAR ONLY` swatch)

After deletion, each `<controls:EtherProgressBar ...>` element has no `Style` attribute at all
— it now resolves the implicit style automatically. Do not change any other attribute
(`Title`, `ValueContent`, `Value`, `ShowTitle`, `ShowValue`, `x:Name`, `Maximum`) on these
elements.

- [ ] **Step 4: Delete the old file**

Delete `Resources/EtherProgressBar.xaml`. Confirmed via grep (see parent plan's Consumer
inventory work) that no file other than `ProgressBarPage.xaml` and `App.xaml` referenced it.

- [ ] **Step 5: Build**

Run: `dotnet build EtherComponentSandbox.csproj -c Debug`
Expected: build succeeds, 0 errors. (A missing/misnamed resource here fails at runtime
navigation, not at build — Step 6 is what actually catches that.)

- [ ] **Step 6: Visual verification**

Run the app (see the `run` skill or launch normally), navigate to Data Display → Progress Bar,
and confirm:
- The INTERACTIVE simulator plays and animates identically to before (gradient fill, label
  counting up, Replay button works)
- All 4 STATES swatches (TITLE + VALUE, TITLE ONLY, VALUE ONLY, BAR ONLY) render pixel-identical
  to the pre-change baseline — same gradient, same track color, same label typography/position
- Toggle Light/Dark theme and confirm no visual regression (this component uses literal hex,
  not theme tokens, so it should look identical in both — confirm it does)

- [ ] **Step 7: Full-catalog navigation sweep**

Open every page reachable from the left nav (or `ComponentCatalog.Nodes`) at least once. This
is the exit gate that catches a missed consumer — confirm nothing else in the app broke.

- [ ] **Step 8: Commit**

```bash
git add Controls/EtherProgressBar.xaml App.xaml Views/DataDisplay/ProgressBarPage.xaml
git rm Resources/EtherProgressBar.xaml
git commit -m "$(cat <<'EOF'
Relocate EtherProgressBar template into Controls/, make style implicit

Phase 0 of the self-contained component authoring plan: prove the
co-located-dictionary pattern on the lowest-risk component (one style
key, one consumer) before touching Button. The style becomes implicit
so call sites no longer need Style="{StaticResource EtherProgressBar}".

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
EOF
)"
```

---

## Task 2: Relocate Toggle Switch's template into `Controls/`

**Files:**
- Create: `Controls/EtherSwitch.xaml`
- Modify: `App.xaml:26` (merge source path)
- Delete: `Resources/EtherSwitch.xaml`

No changes to `Views/Controls/ToggleSwitchPage.xaml` or `.xaml.cs` in this task — the style
stays keyed (`x:Key="EtherSwitch"`), so every existing `Style="{StaticResource EtherSwitch}"`
call site keeps working unchanged. `ToggleSwitchPage.xaml.cs` drives 4 of its STATES swatches
via `VisualStateManager.GoToState(...)` — this task makes zero changes to the `ToggleSwitch`
type or its `ControlTemplate`'s `VisualStateGroups`, so that mechanism is unaffected; Step 6
below verifies it explicitly rather than assuming it.

- [ ] **Step 1: Create `Controls/EtherSwitch.xaml`**

Identical content to the current `Resources/EtherSwitch.xaml`, relocated verbatim (keep the
`x:Key="EtherSwitch"` — do not make this implicit, per the Architecture note above):

```xml
<!--
    Ether Design System Toggle Switch

    Figma source: ursRC201v8IiVeafliI45F, node 60360:8017
    Track: 40 x 18, radius 4. Knob: 18 x 10, radius 1.722, 2 px white stroke.
    The label is Inter SemiBold 12 with an 8 px gap.

    Keyed style (x:Key="EtherSwitch"), not implicit: ToggleSwitch is a stock framework
    type, so an implicit style here would silently restyle every bare <ToggleSwitch/>
    anywhere else in the app. Call sites keep Style="{StaticResource EtherSwitch}".
-->
<ResourceDictionary
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    xmlns:controls="using:EtherSandbox.Controls"
    xmlns:primitives="using:Microsoft.UI.Xaml.Controls.Primitives">

    <!-- On track: left=dark navy, right=mint-cyan. Matched from design screenshot. -->
    <LinearGradientBrush x:Key="EtherIntelligenceGradient"
                         StartPoint="0,0.5"
                         EndPoint="1,0.5">
        <GradientStop Color="#091595" Offset="0.00"/>
        <GradientStop Color="#0015FF" Offset="0.35"/>
        <GradientStop Color="#0EB2FF" Offset="0.78"/>
        <GradientStop Color="#8FFFC3" Offset="1.00"/>
    </LinearGradientBrush>

    <!-- Solid white knob with explicit hover and pressed fills. -->
    <SolidColorBrush x:Key="EtherSwitchKnobFill" Color="#FFFFFF"/>
    <SolidColorBrush x:Key="EtherSwitchKnobFillHover" Color="#F5F8F9"/>
    <SolidColorBrush x:Key="EtherSwitchKnobFillPressed" Color="#E2E8EA"/>

    <!--
        Disabled knob. WinUI applies element opacity PER VISUAL, not as an
        isolated group, so fading the whole toggle with SwitchContent.Opacity=0.4
        turned the white knob translucent and let the track (the On-state gradient
        especially) bleed through it. Instead the disabled state fades only the
        track, shadow and labels and keeps the knob OPAQUE, pre-composited to the
        colour white would reach at 40% over the near-white surface — so the knob
        covers the track cleanly instead of revealing it.
    -->
    <SolidColorBrush x:Key="EtherSwitchKnobDisabled" Color="#FDFEFE"/>

    <ControlTemplate x:Key="EtherSwitchTemplate" TargetType="ToggleSwitch">
        <Grid>
            <VisualStateManager.VisualStateGroups>
                <VisualStateGroup x:Name="CommonStates">
                    <VisualStateGroup.Transitions>
                        <VisualTransition From="Normal" To="PointerOver" GeneratedDuration="0:0:0.12"/>
                        <VisualTransition From="PointerOver" To="Normal" GeneratedDuration="0:0:0.12"/>
                        <VisualTransition To="Pressed" GeneratedDuration="0:0:0.08"/>
                    </VisualStateGroup.Transitions>
                    <VisualState x:Name="Normal">
                        <VisualState.Setters>
                            <Setter Target="KnobFill.Background" Value="{StaticResource EtherSwitchKnobFill}"/>
                        </VisualState.Setters>
                    </VisualState>
                    <VisualState x:Name="PointerOver">
                        <VisualState.Setters>
                            <Setter Target="KnobFill.Background" Value="{StaticResource EtherSwitchKnobFillHover}"/>
                        </VisualState.Setters>
                    </VisualState>
                    <VisualState x:Name="Pressed">
                        <VisualState.Setters>
                            <Setter Target="KnobFill.Background" Value="{StaticResource EtherSwitchKnobFillPressed}"/>
                        </VisualState.Setters>
                    </VisualState>
                    <VisualState x:Name="Disabled">
                        <VisualState.Setters>
                            <!-- Fade only the layers with nothing opaque above them — the track,
                                 its glow, the drop shadow and the labels. The knob stays OPAQUE
                                 (pre-composited near-white) so it covers the track instead of
                                 letting it bleed through. See the disabled-knob note above for
                                 why a single SwitchContent.Opacity would not work. -->
                            <Setter Target="TrackOff.Opacity" Value="0.4"/>
                            <Setter Target="TrackOn.Opacity" Value="0.4"/>
                            <Setter Target="TrackGlow.Opacity" Value="0.4"/>
                            <Setter Target="Shadow1.Opacity" Value="0.4"/>
                            <Setter Target="Shadow2.Opacity" Value="0.4"/>
                            <Setter Target="Shadow3.Opacity" Value="0.4"/>
                            <Setter Target="OffLabel.Opacity" Value="0.4"/>
                            <Setter Target="OnLabel.Opacity" Value="0.4"/>
                            <Setter Target="Knob.BorderBrush" Value="{StaticResource EtherSwitchKnobDisabled}"/>
                            <Setter Target="KnobFill.Background" Value="{StaticResource EtherSwitchKnobDisabled}"/>
                        </VisualState.Setters>
                    </VisualState>
                </VisualStateGroup>

                <VisualStateGroup x:Name="ToggleStates">
                    <VisualStateGroup.Transitions>
                        <VisualTransition From="Off" To="On" GeneratedDuration="0:0:0.16"/>
                        <VisualTransition From="On" To="Off" GeneratedDuration="0:0:0.16"/>
                    </VisualStateGroup.Transitions>
                    <VisualState x:Name="Off">
                        <VisualState.Setters>
                            <Setter Target="TrackOff.Visibility" Value="Visible"/>
                            <Setter Target="TrackOn.Visibility" Value="Collapsed"/>
                            <Setter Target="TrackGlow.Visibility" Value="Collapsed"/>
                            <Setter Target="OffLabel.Visibility" Value="Visible"/>
                            <Setter Target="OnLabel.Visibility" Value="Collapsed"/>
                            <Setter Target="KnobTransform.X" Value="0"/>
                        </VisualState.Setters>
                    </VisualState>
                    <VisualState x:Name="On">
                        <VisualState.Setters>
                            <Setter Target="TrackOff.Visibility" Value="Collapsed"/>
                            <Setter Target="TrackOn.Visibility" Value="Visible"/>
                            <Setter Target="TrackGlow.Visibility" Value="Visible"/>
                            <Setter Target="OffLabel.Visibility" Value="Collapsed"/>
                            <Setter Target="OnLabel.Visibility" Value="Visible"/>
                            <!-- 4 px left inset + 14 px travel = x 18. -->
                            <Setter Target="KnobTransform.X" Value="14"/>
                        </VisualState.Setters>
                    </VisualState>
                </VisualStateGroup>

                <VisualStateGroup x:Name="FocusStates">
                    <VisualState x:Name="Focused">
                        <VisualState.Setters>
                            <Setter Target="FocusRing.Visibility" Value="Visible"/>
                        </VisualState.Setters>
                    </VisualState>
                    <VisualState x:Name="Unfocused"/>
                    <VisualState x:Name="PointerFocused"/>
                </VisualStateGroup>
            </VisualStateManager.VisualStateGroups>

            <StackPanel x:Name="SwitchContent"
                        Orientation="Horizontal"
                        Spacing="8"
                        VerticalAlignment="Center">
                <controls:HandContentControl x:Name="SwitchArea"
                                             Width="40"
                                             Height="18"
                                             HorizontalContentAlignment="Stretch"
                                             VerticalContentAlignment="Stretch">
                    <Grid Background="Transparent">
                    <Border x:Name="TrackOff"
                            Background="{ThemeResource BackgroundSwitchOff}"
                            CornerRadius="{StaticResource RadiusControlSm}"
                            Visibility="Visible"/>

                    <Border x:Name="TrackOn"
                            Background="{StaticResource EtherIntelligenceGradient}"
                            CornerRadius="{StaticResource RadiusControlSm}"
                            Visibility="Collapsed"/>

                    <!-- Soft white inset in the On state. -->
                    <Border x:Name="TrackGlow"
                            Background="#0FFFFFFF"
                            BorderBrush="#33FFFFFF"
                            BorderThickness="1"
                            CornerRadius="{StaticResource RadiusControlSm}"
                            IsHitTestVisible="False"
                            Visibility="Collapsed"/>

                    <!--
                        A compact, low-opacity three-layer shadow based on Figma's #183361.
                        It is deliberately used instead of AttachedCardShadow because that
                        effect is inconsistent in this unpackaged WinUI application.
                    -->
                    <Grid x:Name="KnobHost"
                          Width="18"
                          Height="12"
                          HorizontalAlignment="Left"
                          VerticalAlignment="Center"
                          Margin="4,0,0,0">
                        <Grid.RenderTransform>
                            <TranslateTransform x:Name="KnobTransform"/>
                        </Grid.RenderTransform>
                        <Border x:Name="Shadow1"
                                Margin="-2,0,-2,-3"
                                CornerRadius="4"
                                Background="#06183361"
                                IsHitTestVisible="False"/>
                        <Border x:Name="Shadow2"
                                Margin="-1,1,-1,-2"
                                CornerRadius="3"
                                Background="#0D183361"
                                IsHitTestVisible="False"/>
                        <Border x:Name="Shadow3"
                                Margin="0,1,0,-1"
                                CornerRadius="1.722"
                                Background="#1C183361"
                                IsHitTestVisible="False"/>

                        <Border x:Name="Knob"
                                CornerRadius="1.722"
                                BorderBrush="White"
                                BorderThickness="0.8">
                            <Grid>
                                <Border x:Name="KnobBase"
                                        Background="Transparent"
                                        CornerRadius="0.5"
                                        IsHitTestVisible="False"/>
                                <Border x:Name="KnobFill"
                                        Background="{StaticResource EtherSwitchKnobFill}"
                                        CornerRadius="0.5"/>
                            </Grid>
                        </Border>
                    </Grid>

                    <primitives:Thumb x:Name="SwitchThumb"
                                      HorizontalAlignment="Stretch"
                                      VerticalAlignment="Stretch"
                                      MinWidth="0"
                                      MinHeight="0"
                                      Background="Transparent"
                                      BorderThickness="0"
                                      Opacity="0"
                                      IsHitTestVisible="True"
                                      AutomationProperties.AccessibilityView="Raw"/>
                    </Grid>
                </controls:HandContentControl>

                <ContentPresenter x:Name="OffLabel"
                                  Content="{TemplateBinding OffContent}"
                                  Foreground="{ThemeResource TextSecondary}"
                                  FontFamily="{StaticResource InterFont}"
                                  FontSize="12"
                                  FontWeight="SemiBold"
                                  VerticalAlignment="Center"
                                  Visibility="Visible"/>
                <ContentPresenter x:Name="OnLabel"
                                  Content="{TemplateBinding OnContent}"
                                  Foreground="{ThemeResource TextSecondary}"
                                  FontFamily="{StaticResource InterFont}"
                                  FontSize="12"
                                  FontWeight="SemiBold"
                                  VerticalAlignment="Center"
                                  Visibility="Collapsed"/>
            </StackPanel>

            <Border x:Name="FocusRing"
                    Margin="-3"
                    BorderBrush="{ThemeResource BorderFocus}"
                    BorderThickness="2"
                    CornerRadius="5"
                    IsHitTestVisible="False"
                    Visibility="Collapsed"/>
        </Grid>
    </ControlTemplate>

    <Style x:Key="EtherSwitch" TargetType="ToggleSwitch">
        <Setter Property="UseSystemFocusVisuals" Value="False"/>
        <Setter Property="Template" Value="{StaticResource EtherSwitchTemplate}"/>
    </Style>
</ResourceDictionary>
```

- [ ] **Step 2: Repoint the `App.xaml` merge**

In `App.xaml`, change:

```xml
                <ResourceDictionary Source="Resources/EtherSwitch.xaml"/>
```

to:

```xml
                <ResourceDictionary Source="Controls/EtherSwitch.xaml"/>
```

- [ ] **Step 3: Delete the old file**

Delete `Resources/EtherSwitch.xaml`. Confirmed via grep that only `ToggleSwitchPage.xaml`
references `StaticResource EtherSwitch`.

- [ ] **Step 4: Build**

Run: `dotnet build EtherComponentSandbox.csproj -c Debug`
Expected: build succeeds, 0 errors.

- [ ] **Step 5: Visual verification**

Navigate to Controls → Toggle Switch and confirm:
- The INTERACTIVE toggle still renders and toggles correctly on click
- All 8 STATES swatches (OFF/ON × Default/Hover/Pressed/Disabled) render pixel-identical to
  the pre-change baseline — same gradient on-track, same knob shadow layers, same disabled
  fade behavior (track/labels fade, knob stays opaque)
- The Disabled toggle on the ComponentPage header still cascades correctly
- Toggle Light/Dark theme — `TrackOff` uses `{ThemeResource BackgroundSwitchOff}` and the
  focus ring uses `{ThemeResource BorderFocus}`, so confirm both themes still resolve correctly

- [ ] **Step 6: Confirm `GoToState` pinning still works**

This is the specific check for the mechanism risk the parent plan's review flagged. Since this
task made zero changes to the `ToggleSwitch` type or its template's `VisualStateGroups`, this
should pass trivially — confirm it does, don't assume it:
- `OffHoverState` and `OnHoverState` swatches show the `PointerOver` knob fill
  (`EtherSwitchKnobFillHover`, `#F5F8F9`), not the default fill
- `OffPressedState` and `OnPressedState` swatches show the `Pressed` knob fill
  (`EtherSwitchKnobFillPressed`, `#E2E8EA`)

- [ ] **Step 7: Full-catalog navigation sweep**

Open every page reachable from the left nav at least once, confirming nothing else broke.

- [ ] **Step 8: Commit**

```bash
git add Controls/EtherSwitch.xaml App.xaml
git rm Resources/EtherSwitch.xaml
git commit -m "$(cat <<'EOF'
Relocate EtherSwitch template into Controls/

Second half of Phase 0's mechanism pilot. Style stays keyed
(x:Key="EtherSwitch") since ToggleSwitch is a stock framework type —
an implicit style here would silently restyle any bare ToggleSwitch
elsewhere in the app. No change to ToggleSwitchPage.xaml call sites.

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
EOF
)"
```

---

## Task 3: Phase 0 retrospective — decide whether to proceed to Phase 1 (Button)

**Files:** none (decision checkpoint, update the parent plan's revision log if anything learned
here changes Phase 1's approach)

- [ ] **Step 1: Confirm both components meet the parent plan's Definition of Done**

Re-read `docs/superpowers/plans/2026-08-06-self-contained-component-authoring-plan.md`'s
"Definition of done" section and check both Progress Bar and Toggle Switch against every bullet.

- [ ] **Step 2: Note anything that surprised you**

If the co-located-dictionary + explicit-merge mechanism hit any friction (build errors,
resource resolution order issues, anything not anticipated in the parent plan), add a short
note to that plan's "Revision log" section before starting Phase 1 — Button has a much larger
consumer set, so any mechanism issue is cheaper to discover here than there.

- [ ] **Step 3: Report status to the user**

Summarize: both components migrated, verification results, whether Phase 1 (Button) is ready
to start as planned or needs adjustment first.
