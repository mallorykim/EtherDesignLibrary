# Consumability review — Steering Bar

> Reviewed against
> [the review spec](../2026-09-01-component-consumability-review-spec.md). Findings are grounded in
> source (file:line); the official WinUI baseline is Windows App SDK 2.3.1
> (`Directory.Packages.props:7`).
>
> **Reviewed:** `EtherSteeringBar` (`: Control`) + `SteeringBarValueChangedEventArgs` —
> [EtherSteeringBar.xaml.cs](../../../src/Ether.DesignSystem.Controls/Controls/Inputs/EtherSteeringBar.xaml.cs),
> [EtherSteeringBar.xaml](../../../src/Ether.DesignSystem.Controls/Controls/Inputs/EtherSteeringBar.xaml).
> **Package:** `Ether.DesignSystem.Controls`. **Date:** 2026-09-01 (post-remediation, R3).

## Verdict at a glance
- **Base-control parity (B):** the confirmed base is bare `Control`, not `RangeBase`/`Slider`
  (`EtherSteeringBar.xaml.cs:78`), so all range behavior is custom. Minimum/Maximum/Value,
  Small/Large change, stops, clamping, keyboard input, and RangeValue automation are implemented
  (`EtherSteeringBar.xaml.cs:133-303,459-629,915-956`).
- **Design coverage (B):** the prior completeness hole is closed — `StepFrequency` is now a public
  DP (`EtherSteeringBar.xaml.cs:163-166,250-254`), and `NormalizeValue` snaps input to it
  (step-relative rounding) whenever `SnapToStops` is off (`EtherSteeringBar.xaml.cs:468-474`);
  irregular `Stops` continue to take precedence when snapping is on.
- **Consumability (A):** all 14 declared DPs (including `ValueContentConverter`) are in the generic
  property fixture (`RuntimeVerification.PropertyConsumption.cs:270-281,309-320`), the public
  custom event args carry old/new doubles (`EtherSteeringBar.xaml.cs:32-48`), the adapter is
  exercised (`RuntimeVerification.PropertyConsumption.cs:379`), TwoWay `Value` is now proven
  VM-backed (`RuntimeVerification.R2.cs:138-146`), and RangeValue automation is fully tested
  (`RuntimeVerification.SteeringBar.cs:81-149`). Strict R5 documentation now passes.

**Overall: consumer-ready.** Review B's `StepFrequency` gap is closed; all three of Review A's follow-ups are closed by the harness (`ETHER_CONSUMER_SMOKE` `outcome:"success"`).

---

## Review B — Completeness / Parity matrix

| Property/Event | Source | Expected | Present? | Bindable? | Verdict |
| --- | --- | --- | --- | --- | --- |
| `Minimum` / `Maximum` | design | set/bind inclusive range bounds | DPs `EtherSteeringBar.xaml.cs:133-141,204-215`; normalized min/max `…xaml.cs:455-457` | ✔ DPs + callbacks | ✅ |
| `Value` | design | two-way bind current position | DP `EtherSteeringBar.xaml.cs:143-146,217-222`; clamped before notification `…xaml.cs:359-385`; VM-backed TwoWay round-trip now proven `RuntimeVerification.R2.cs:138-146` | ✔ DP + event | ✅ |
| `SmallChange` / `LargeChange` | design | configure arrow/wheel and Page increments | DPs `EtherSteeringBar.xaml.cs:173-181,264-275`; keyboard uses them `…xaml.cs:579-608` | ✔ DPs | ✅ |
| `StepFrequency` | design | choose a uniform pointer/keyboard/automation snap step | now a public DP, default `1.0` `EtherSteeringBar.xaml.cs:163-166,250-254`; drives step-relative snapping in `NormalizeValue` `…xaml.cs:468-474` | ✔ DP + callback | ✅ **closed this wave** |
| `Stops` / `SnapToStops` / `ShowStops` | design | provide irregular steps, snap, and show markers | DPs `EtherSteeringBar.xaml.cs:153-171,232-261`; normalization uses them `…xaml.cs:463-474` | ✔ DPs + callbacks | ✅ |
| `MoveToPreviousStop` / `MoveToNextStop` | design | move programmatically between irregular stops | public methods `EtherSteeringBar.xaml.cs:529-577` | callable methods | ✅ |
| `Title` / `ValueFormat` | design | supply optional title content / format the live value label | DPs `EtherSteeringBar.xaml.cs:183-191,277-289` | ✔ DPs + callbacks | ✅ |
| `ShowTitle` / `ShowValue` | design | independently show labels | bool DPs `EtherSteeringBar.xaml.cs:193-201,291-303`; states in style `EtherSteeringBar.xaml:161-170` | ✔ DPs + callbacks | ✅ |
| `ValueChanged` | design | listen to old/new value → backend | public event + public args `EtherSteeringBar.xaml.cs:32-48,130-131,476-487` | event + `ObserveSteeringBar` `ControlInteractionAdapter.cs:129-138` | ✅ |
| `IsEnabled` | base | disable pointer/keyboard/automation edits | inherited; gates input and automation `EtherSteeringBar.xaml.cs:596-629` | ✔ DP | ✅ |
| RangeValue automation | design | expose an interactive slider role | custom peer implements RangeValue `EtherSteeringBar.xaml.cs:915-956` | UIA observable | ✅ |
| locked appearance | base | override track/thumb typography/chrome | registry marks the inherited appearance family fixed `scripts/UnsupportedProperties.psd1:425-440` | writable DPs; no Ether visual effect | 🚫 use title/value/range/stops surface; chrome remains token-owned `getting-started.md:290-304` |

**Notes / gaps (B):**
- **The lineage is unambiguously `Control`.** The declaration is
  `public sealed class EtherSteeringBar : Control` (`EtherSteeringBar.xaml.cs:78`), so none of the
  range DPs/events are inherited; they are all Ether-owned declarations
  (`EtherSteeringBar.xaml.cs:133-303`).
- **`StepFrequency` mirrors `EtherSlider`'s fix.** `NormalizeValue` computes
  `RangeMinimum + Math.Round((clampedValue - RangeMinimum) / step) * step`
  (`EtherSteeringBar.xaml.cs:468-473`), replacing the prior behavior where the public range surface
  had only small/large changes and irregular stops with no uniform snap.
- **The custom event args are consumable.** `SteeringBarValueChangedEventArgs` is public and exposes
  immutable `OldValue`/`NewValue` doubles (`EtherSteeringBar.xaml.cs:32-48`), and the adapter emits
  both (`ControlInteractionAdapter.cs:129-138`).
- Irregular `Stops` continue to compose with `StepFrequency` exactly as `EtherSlider` does: snapping
  to stops occurs only when `SnapToStops` is true and a collection is populated, otherwise the
  uniform `StepFrequency` snap applies (`EtherSteeringBar.xaml.cs:463-474`).

---

## Review A — Consumability report

```
Component: EtherSteeringBar (+ SteeringBarValueChangedEventArgs)
Base control: Control   | Package: Ether.DesignSystem.Controls
Declared DPs: 14  (Minimum, Maximum, Value, Stops, SnapToStops, StepFrequency, ShowStops,
                   SmallChange, LargeChange, Title, ValueFormat, ValueContentConverter, ShowTitle, ShowValue)
                   | inherited public surface: Control

Rubric (declared DPs):
  R1 DependencyProperty ....... PASS  (13 public identifiers + wrappers,
                                       EtherSteeringBar.xaml.cs:133-303)
  R2 Round-trips .............. PASS  (standard wrappers, cs:204-303; generic fixture asserts
                                       both paths, PropertyConsumption.cs:62-97)
  R3 Observable ............... PASS  (metadata callbacks, cs:133-201; all 13 samples exist at
                                       PropertyConsumption.cs:309-320)
  R4 TwoWay ................... PASS  (VM-backed Value binding: control receives the VM value and
                                       pushes changes back, RuntimeVerification.R2.cs:138-146)
  R5 Documented ............... PASS  (every declared identifier/wrapper pair now states its
                                       registered default, and the shipping style overrides at
                                       EtherSteeringBar.xaml:139-151 are noted where they diverge)

Contracts:
  C1 ItemsSource / selection .. N/A   (not a collection control; Value is the data contract)
  C2 Listenable + adapter ..... PASS  (public args/event + ObserveSteeringBar,
                                       ControlInteractionAdapter.cs:129-138; exercised at
                                       PropertyConsumption.cs:379)
  C3 Command .................. N/A   (range interaction is Value/event based)
  C4 Automation peer/pattern .. PASS  (custom RangeValue peer, cs:915-956; fixture asserts role,
                                       values, SetValue, and disabled lock,
                                       RuntimeVerification.SteeringBar.cs:81-149)

Harness: covered? YES. In PublicPropertyInventoryTypes (PublicPropertyInventory.cs:46),
         DeclaredPropertyOwnerTypes (PropertyConsumption.cs:270-281), the standard-interaction
         list (PropertyConsumption.cs:379), and RuntimeVerification.SteeringBar.cs:14-17. Runtime
         marker ETHER_CONSUMER_SMOKE outcome:"success" includes "EtherSteeringBar.Value" in
         twoWayBindings.Properties and carries the StepFrequency=0.5 sample through
         propertyConsumption's declared-DP verification
         (artifacts/audit-runs/consumer-runtime-evidence-20260901-161450498/runtime-result.json).
x64 build: green (Controls + Interactions + ConsumerFixtures, per the R2.11 runtime run recorded
           in docs/internal/consumability/_RUN-ON-DESKTOP.md).
```

---

## Follow-ups (concrete, actionable)

1. ~~**[B, R1–R4] Add uniform stepping.**~~ DONE — `StepFrequencyProperty` + wrapper were added
   (`EtherSteeringBar.xaml.cs:163-166,250-254`) and incorporated into `NormalizeValue`
   (`…xaml.cs:468-474`) while preserving irregular `Stops`; the property is inventoried and sampled
   in `RuntimeVerification.PropertyConsumption.cs:314`.
2. ~~**[R4, harness] Prove TwoWay `Value`.**~~ DONE — `RuntimeVerification.R2.cs:138-146` binds a
   VM-backed TwoWay `Value` and asserts both VM → control and control → VM propagation, alongside
   the existing event/RangeValue checks (`RuntimeVerification.SteeringBar.cs:81-149`).
3. ~~**[R5] Document all 13 effective defaults.**~~ DONE — identifier and wrapper XML docs in
   `EtherSteeringBar.xaml.cs:133-303` now state metadata defaults, with the shipping style overrides
   at `EtherSteeringBar.xaml:139-151` noted where they diverge.

No remaining follow-ups.
