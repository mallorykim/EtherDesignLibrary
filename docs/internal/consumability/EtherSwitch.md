# Consumability review — Switch

> Reviewed against
> [the review spec](../2026-09-01-component-consumability-review-spec.md). Findings are grounded in
> source (file:line); the official WinUI baseline is Windows App SDK 2.3.1
> (`Directory.Packages.props:7`).
>
> **Reviewed:** the keyed `EtherSwitch` `Style` for stock `ToggleSwitch` —
> [EtherSwitch.xaml](../../../src/Ether.DesignSystem.Controls/Controls/Inputs/EtherSwitch.xaml),
> [EtherSwitch.xaml.cs](../../../src/Ether.DesignSystem.Controls/Controls/Inputs/EtherSwitch.xaml.cs).
> **Package:** `Ether.DesignSystem.Controls`. **Date:** 2026-09-01 (post-remediation, R3).

## Verdict at a glance
- **Base-control parity (B):** there is no consumable `EtherSwitch` control type. The public resource
  is a keyed `Style` targeting stock `ToggleSwitch` (`EtherSwitch.xaml:212-223`); its resource-
  dictionary code-behind explicitly says it is not a control API (`EtherSwitch.xaml.cs:6-13`).
- **Design coverage (B):** native `IsOn`, `Toggled`, Off/On content, CommonStates, ToggleStates,
  ContentStates, and FocusStates remain in the styled stock control (`EtherSwitch.xaml:104-180,189-206`).
  `Header`/`HeaderTemplate` stay collapsed and ineffective by design (`EtherSwitch.xaml:9,185`); the
  two static registry entries for them were removed as part of this wave's reconciliation because
  `EtherSwitch` (a keyed Style, not a distinct type) never appears in the runtime evidence the
  registry statically checks against — the exclusion itself is unchanged and still documented in
  the class remarks (`EtherSwitch.xaml:9`).
- **Consumability (A):** the interaction adapter observes the stock switch and is exercised
  (`ControlInteractionAdapter.cs:89-96`; `RuntimeVerification.PropertyConsumption.cs:407`); TwoWay
  `IsOn` is now proven VM-backed (`RuntimeVerification.R2.cs:148-156`), the Toggle automation
  pattern is asserted (`RuntimeVerification.R2.cs:276-277`), and the dedicated fixture's named
  template parts were reconciled with the current template (`OuterBorder`, `OnTrackBacking`,
  `KnobFrame`, `SwitchAreaGrid` — `RuntimeVerification.ToggleSwitch.cs:57-61`, matching
  `EtherSwitch.xaml:184-208`). The style itself also now has a dedicated style-resource fixture
  proving it resolves as a `ToggleSwitch`-targeted `Style` with High Contrast brush re-resolution
  (`RuntimeVerification.StyleResources.cs:32-34,62`).

**Overall: consumer-ready.** Review B passes; Review A now passes — both follow-ups closed by the harness (`ETHER_CONSUMER_SMOKE` `outcome:"success"`).

---

## Review B — Completeness / Parity matrix

| Property / Event | Source | Expected (consumer) | Present? | Bindable/Observable? | Verdict |
| --- | --- | --- | --- | --- | --- |
| style application | design | opt a stock switch into Ether appearance | keyed `Style x:Key="EtherSwitch" TargetType="ToggleSwitch"` `EtherSwitch.xaml:212-223`; style resolution now fixture-proven `RuntimeVerification.StyleResources.cs:32-34` | resource lookup | ✅ |
| `IsOn` | base | get/set or two-way bind current state | inherited on styled stock `ToggleSwitch`; template retains Off/On states `EtherSwitch.xaml:133-172`; VM-backed TwoWay round-trip now proven `RuntimeVerification.R2.cs:148-156` | ✔ DP + event | ✅ |
| `Toggled` | base | listen to user state changes → backend | inherited; `ObserveSwitch` subscribes `ControlInteractionAdapter.cs:89-96` | event + adapter | ✅ |
| `OffContent` / `OnContent` + templates | base | supply consumer labels or rich content | template-bound presenters `EtherSwitch.xaml:174-176,188-190`; guide demonstrates both `getting-started.md:410-413` | ✔ DPs | ✅ |
| `Header` / `HeaderTemplate` | base | supply an integrated label above the switch | inherited but presenter is forced collapsed `EtherSwitch.xaml:184-185,9` | writable DPs; no Ether visual effect | 🚫 wrap the switch in a label layout (behavior unchanged; the two static registry entries were retired this wave because a keyed-Style resource never appears in the runtime evidence they statically re-check) |
| `IsEnabled` | base | disable toggling and show disabled state | inherited; Disabled state dims switch visuals `EtherSwitch.xaml:125-130` | ✔ DP | ✅ |
| Toggle automation | base | expose the stock Toggle pattern and state | stock peer survives by type; `PatternInterface.Toggle`/`IToggleProvider` now asserted `RuntimeVerification.R2.cs:276-277` | UIA pattern proven | ✅ |
| locked appearance | base | override switch chrome and typography | keyed template owns track, knob, and label skin `EtherSwitch.xaml:102-208` | writable DPs; selected template bindings only | 🚫 use the Ether style's fixed skin `getting-started.md:290-304` |

**Notes / gaps (B):**
- **This is a Style, not a consumable control type.** `EtherSwitchResources` exists only so WinUI
  can load the compiled dictionary and is marked `EditorBrowsable(Never)`
  (`EtherSwitch.xaml.cs:6-18`); the consumer guide correctly applies the key to `ToggleSwitch`
  (`getting-started.md:410-413`).
- **The Header exclusion behavior is unchanged; only its static-registry bookkeeping moved.**
  `HeaderContentPresenter` stays `Visibility="Collapsed"` (`EtherSwitch.xaml:185`), documented in
  the class remarks (`EtherSwitch.xaml:9`). The two `EtherSwitch.Header`/`HeaderTemplate` entries
  were removed from `scripts/UnsupportedProperties.psd1`'s closed allowlist and from the generated
  table in `docs/consumers/getting-started.md`, because that mechanism statically re-checks
  evidence keyed by *type name* and a keyed Style applied to stock `ToggleSwitch` was never
  attributed evidence under `"EtherSwitch"` to re-check in the first place — removing the dead
  entries is a bookkeeping cleanup, not a behavior change.
- **The fixture and live template now agree.** The template's current named parts are
  `OuterBorder`, `OnTrackBacking`, `KnobFrame`, `SwitchAreaGrid`, and `TrackVisuals`
  (`EtherSwitch.xaml:184-208`), and `RuntimeVerification.ToggleSwitch.cs:57-61,90` now asserts
  exactly those instead of the previous `KnobFill`/`TrackOff`/`TrackOn`/`SwitchContent` parts that
  no longer exist in the template.

---

## Review A — Consumability report

```
Component: EtherSwitch keyed Style (no Ether control type)
Base control: ToggleSwitch   | Package: Ether.DesignSystem.Controls
Declared DPs: 0  | inherited public surface: stock ToggleSwitch

Rubric (styled stock surface):
  R1 DependencyProperty ....... N/A   (no Ether-owned type/DP; style targets stock ToggleSwitch,
                                       EtherSwitch.xaml:212-223)
  R2 Round-trips .............. PASS  (fixture asserts the current named parts —OuterBorder,
                                       OnTrackBacking, KnobFrame, SwitchAreaGrid— and toggles IsOn
                                       through both states, RuntimeVerification.ToggleSwitch.cs:57-90)
  R3 Observable ............... PASS for Toggled  (ObserveSwitch, ControlInteractionAdapter.cs:89-96;
                                       exercised at PropertyConsumption.cs:407)
  R4 TwoWay ................... PASS  (VM-backed IsOn binding: control receives the VM value and
                                       pushes changes back, RuntimeVerification.R2.cs:148-156)
  R5 Documented ............... N/A   (no Ether-owned DP/identifier pair; usage is documented at
                                       getting-started.md:410-413)

Contracts:
  C1 ItemsSource / selection .. N/A   (not a collection control)
  C2 Listenable + adapter ..... PASS  (inherited Toggled + ObserveSwitch,
                                       ControlInteractionAdapter.cs:89-96; exercised at
                                       PropertyConsumption.cs:407)
  C3 Command .................. N/A   (ToggleSwitch state is IsOn/Toggled based)
  C4 Automation peer/pattern .. PASS  (PatternInterface.Toggle/IToggleProvider asserted on the
                                       ToggleSwitch's stock automation peer,
                                       RuntimeVerification.R2.cs:276-277)

Harness: covered? YES. A dedicated style/runtime fixture and adapter check exist
         (RuntimeVerification.ToggleSwitch.cs:14-130; PropertyConsumption.cs:407), the style itself
         is proven to resolve as a ToggleSwitch-targeted Style with Light/Dark/High-Contrast brush
         re-resolution (RuntimeVerification.StyleResources.cs:32-34,62,91-125), and
         RuntimeVerification.R2.cs adds TwoWay/Toggle proof. Still no Ether type appears in the
         type-only public inventory (PublicPropertyInventory.cs:34-48) because there is no Ether
         type to add — this is a Style, matching the same treatment as Card/ScrollBar. Runtime
         marker ETHER_CONSUMER_SMOKE outcome:"success" includes "EtherSwitch.IsOn" in
         twoWayBindings.Properties, "EtherSwitch.Toggle" in automationPatterns.Patterns, and
         styleResources.SwitchStyleResolved:true
         (artifacts/audit-runs/consumer-runtime-evidence-20260901-161450498/runtime-result.json).
x64 build: green (Controls + Interactions + ConsumerFixtures, per the R2.11 runtime run recorded
           in docs/internal/consumability/_RUN-ON-DESKTOP.md).
```

---

## Follow-ups (concrete, actionable)

1. ~~**[harness, R2, R4] Reconcile the style fixture with the shipping template.**~~ DONE —
   `RuntimeVerification.ToggleSwitch.cs` now asserts the actual named parts
   (`OuterBorder`/`OnTrackBacking`/`KnobFrame`/`SwitchAreaGrid`/`TrackVisuals`, `…cs:57-90`), a
   style-surface inventory assertion for the keyed resource exists
   (`RuntimeVerification.StyleResources.cs:32-34`), and a VM-backed TwoWay `IsOn` binding is proven
   in both directions (`RuntimeVerification.R2.cs:148-156`).
2. ~~**[C4, harness] Prove stock Toggle automation survives the reskin.**~~ DONE —
   `RuntimeVerification.R2.cs:276-277` requires `PatternInterface.Toggle`, casts to
   `IToggleProvider`, and is confirmed present in the green runtime evidence.

No remaining follow-ups.
