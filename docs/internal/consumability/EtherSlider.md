# Consumability review — Slider

> Reviewed against
> [the review spec](../2026-09-01-component-consumability-review-spec.md). Findings are grounded in
> source (file:line); the official WinUI baseline is Windows App SDK 2.3.1
> (`Directory.Packages.props:7`).
>
> **Reviewed:** `EtherSlider` (`: RangeBase`) —
> [EtherSlider.xaml.cs](../../../src/Ether.DesignSystem.Controls/Controls/Inputs/EtherSlider.xaml.cs),
> [EtherSlider.xaml](../../../src/Ether.DesignSystem.Controls/Controls/Inputs/EtherSlider.xaml),
> [EtherSlider.Automation.cs](../../../src/Ether.DesignSystem.Controls/Controls/Inputs/EtherSlider.Automation.cs),
> [EtherSlider.Labels.cs](../../../src/Ether.DesignSystem.Controls/Controls/Inputs/EtherSlider.Labels.cs).
> **Package:** `Ether.DesignSystem.Controls`. **Date:** 2026-09-01 (post-remediation, R3).

## Verdict at a glance
- **Base-control parity (B):** complete for its actual `RangeBase` lineage: `Minimum`, `Maximum`,
  `Value`, `SmallChange`, `LargeChange`, and `ValueChanged` are inherited (`EtherSlider.xaml.cs:23-24,45`),
  exercised by the slider fixture (`RuntimeVerification.Slider.cs:24-38,88-101`), and routed through
  `ObserveRange` (`ControlInteractionAdapter.cs:140-149`).
- **Design coverage (B):** the prior completeness hole is closed — `StepFrequency` is now a public
  DP (`EtherSlider.xaml.cs:159-164,227-231`), and `NormalizeInputValue` snaps pointer/keyboard/
  automation input to it (step-relative rounding, not fixed-integer rounding) whenever
  `SnapToStops` is off (`EtherSlider.xaml.cs:801-813`); `Stops`/`SnapToStops` still take precedence
  when snapping is on, matching WinUI `Slider.StepFrequency` semantics.
- **Consumability (A):** all seven declared properties (including the new `StepFrequency`) are
  DP-backed and fixture-covered (`RuntimeVerification.PropertyConsumption.cs:270-281,296-308`),
  `ValueChanged` reaches the backend (`RuntimeVerification.PropertyConsumption.cs:381`), and
  RangeValue automation is asserted (`RuntimeVerification.Slider.cs:104-135`). Strict R5
  documentation now passes: every declared property's identifier and wrapper doc states its
  registered default, and where the shipping style overrides it, that effective default too
  (`EtherSlider.xaml.cs:117-231`).

**Overall: consumer-ready.** Review B's `StepFrequency` gap is closed; Review A's two follow-ups are both closed by the harness (`ETHER_CONSUMER_SMOKE` `outcome:"success"`).

---

## Review B — Completeness / Parity matrix

| Property/Event | Source | Expected | Present? | Bindable? | Verdict |
| --- | --- | --- | --- | --- | --- |
| `Minimum` | base | set/bind the lower range bound | inherited `RangeBase.Minimum`; used by layout/coercion `EtherSlider.xaml.cs:330-335` | ✔ DP | ✅ |
| `Maximum` | base | set/bind the upper range bound | inherited `RangeBase.Maximum`; used by layout/coercion `EtherSlider.xaml.cs:338-343` | ✔ DP | ✅ |
| `Value` | base | two-way bind current value | inherited `RangeBase.Value`; Gallery uses `Mode=TwoWay` `SliderPage.xaml:29-33` | ✔ DP + `ValueChanged` | ✅ |
| `SmallChange` | base | configure arrow/wheel increment | inherited; wheel uses it `EtherSlider.xaml.cs:631-639`, arrows use it `…xaml.cs:731-741` | ✔ DP | ✅ |
| `LargeChange` | base | configure PageUp/PageDown increment | inherited; Page keys select it `EtherSlider.xaml.cs:716-736` | ✔ DP | ✅ |
| `StepFrequency` | design/base | choose the uniform pointer/keyboard/automation step | now a public DP, default `1.0` `EtherSlider.xaml.cs:159-164,227-231`; drives step-relative snapping in `NormalizeInputValue` `…xaml.cs:801-813` | ✔ DP + callback | ✅ **closed this wave** |
| `ValueChanged` | base | listen to old/new value → backend | inherited; override preserves base event `EtherSlider.xaml.cs:284-320` | event + `ObserveRange` `ControlInteractionAdapter.cs:140-149` | ✅ |
| `Title` / `ShowTitle` | design | set header and independently show/hide it | DPs `EtherSlider.xaml.cs:117-129,166-178` | ✔ DP + callbacks | ✅ |
| `Labels` / `ShowLabels` | design | provide tick captions and show/hide them | DPs `EtherSlider.xaml.cs:131-143,180-198` | ✔ DP + collection callback | ✅ |
| `Stops` / `SnapToStops` | design | define irregular discrete values and opt into snapping | DPs `EtherSlider.xaml.cs:145-157,200-220`; next/previous stop API `…xaml.cs:744-774` | ✔ DP + callbacks | ✅ |
| `IsEnabled` | base | disable pointer, keyboard, and automation changes | inherited; `CanInteract` gates input `EtherSlider.xaml.cs:375-383,694-697,711-714` | ✔ DP | ✅ |

**Notes / gaps (B):**
- **The lineage is unambiguously `RangeBase`, not `Slider` or `UserControl`.** The declaration is
  `public sealed partial class EtherSlider : RangeBase` (`EtherSlider.xaml.cs:45`). Therefore
  `Minimum`/`Maximum`/`Value`/`SmallChange`/`LargeChange`/`ValueChanged` are real inherited range APIs.
- **`StepFrequency` is now a first-class local DP, not an inherited one** (WinUI's `StepFrequency`
  belongs to `Slider`, which `EtherSlider` does not derive from). `EtherSlider.xaml.cs:227-231`
  documents it explicitly: "values ≤ 0 disable step snapping" and "`SnapToStops` takes precedence
  when enabled." `NormalizeInputValue` computes
  `Minimum + Math.Round((clamped - Minimum) / step) * step` (`…xaml.cs:807-812`), replacing the
  prior unconditional `Math.Round(clamped)` integer snap.
- `Stops` remains the irregular-stop alternative, active only with `SnapToStops`
  (`EtherSlider.xaml.cs:786-813`), and is unaffected by the `StepFrequency` addition — the two
  mechanisms compose exactly as documented (snap-to-stops wins when on).

---

## Review A — Consumability report

```
Component: EtherSlider
Base control: RangeBase   | Package: Ether.DesignSystem.Controls
Declared DPs: 7  (ShowTitle, Title, ShowLabels, Labels, Stops, SnapToStops, StepFrequency)
                 | inherited public surface: full RangeBase

Rubric (declared DPs):
  R1 DependencyProperty ....... PASS  (seven public identifiers + CLR wrappers,
                                       EtherSlider.xaml.cs:117-231)
  R2 Round-trips .............. PASS  (standard GetValue/SetValue wrappers, cs:166-231;
                                       fixture asserts both paths, PropertyConsumption.cs:62-97)
  R3 Observable ............... PASS  (metadata callbacks, cs:117-164,233-251;
                                       fixture registers callbacks, PropertyConsumption.cs:74-91,
                                       270-281,296-308)
  R4 TwoWay ................... PASS-by-inheritance  (RangeBase.Value; Gallery consumes it with
                                       Mode=TwoWay, SliderPage.xaml:29-33)
  R5 Documented ............... PASS  (every declared identifier/wrapper pair now states its
                                       registered default, and Title/Labels/ShowTitle/ShowLabels
                                       additionally note the shipping style's effective default,
                                       cs:117-231; style overrides at EtherSlider.xaml:79-90)

Contracts:
  C1 ItemsSource / selection .. N/A   (not a collection control; numeric Value is the data contract)
  C2 Listenable + adapter ..... PASS  (inherited ValueChanged preserved at cs:284-320;
                                       ObserveRange at ControlInteractionAdapter.cs:140-149;
                                       standard adapter fixture at PropertyConsumption.cs:381)
  C3 Command .................. N/A   (range controls interact through Value/ValueChanged)
  C4 Automation peer/pattern .. PASS  (custom IRangeValueProvider, EtherSlider.Automation.cs:9-51;
                                       fixture asserts RangeValue and SetValue,
                                       RuntimeVerification.Slider.cs:104-135)

Harness: covered? YES. In PublicPropertyInventoryTypes (PublicPropertyInventory.cs:44),
         DeclaredPropertyOwnerTypes (PropertyConsumption.cs:270-281), the standard-interaction
         list (PropertyConsumption.cs:381), and RuntimeVerification.Slider.cs:14-17. Runtime marker
         ETHER_CONSUMER_SMOKE outcome:"success" carries the StepFrequency=0.5 sample through
         propertyConsumption's declared-DP verification
         (artifacts/audit-runs/consumer-runtime-evidence-20260901-161450498/runtime-result.json).
x64 build: green (Controls + Interactions + ConsumerFixtures, per the R2.11 runtime run recorded
           in docs/internal/consumability/_RUN-ON-DESKTOP.md).
```

---

## Follow-ups (concrete, actionable)

1. ~~**[B, R1–R4] Add a configurable uniform step to `EtherSlider`.**~~ DONE — `StepFrequencyProperty`
   (`double`, default `1.0`, XML-doc'd) was added (`EtherSlider.xaml.cs:159-164,227-231`), and
   `NormalizeInputValue` now does step-relative rounding
   (`Minimum + Math.Round((clamped - Minimum) / step) * step`) instead of the unconditional
   `Math.Round(clamped)` (`…xaml.cs:801-813`); `Stops`/`SnapToStops` still take precedence when
   snapping is on. The property is inventoried and sampled in
   `RuntimeVerification.PropertyConsumption.cs:308`.
2. ~~**[R5] Complete the declared-property XML docs.**~~ DONE — `EtherSlider.xaml.cs:117-231` now
   states each registered default on both identifier and wrapper, including the style overrides
   `ShowTitle=True`/`ShowLabels=True` and range/change defaults from `EtherSlider.xaml:79-90`.

No remaining follow-ups.
