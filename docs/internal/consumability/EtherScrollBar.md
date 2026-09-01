# Consumability review — Scroll Bar

> Reviewed against
> [the review spec](../2026-09-01-component-consumability-review-spec.md). Findings are grounded in
> source (file:line); the official WinUI baseline is Windows App SDK 2.3.1
> (`Directory.Packages.props:7`).
>
> **Reviewed:** the implicit Ether `Style` for stock `ScrollBar` —
> [EtherScrollBar.xaml](../../../src/Ether.DesignSystem.Controls/Controls/Inputs/EtherScrollBar.xaml),
> [EtherScrollBar.xaml.cs](../../../src/Ether.DesignSystem.Controls/Controls/Inputs/EtherScrollBar.xaml.cs).
> **Package:** `Ether.DesignSystem.Controls`. **Date:** 2026-09-01 (post-remediation, R3).

## Verdict at a glance
- **Base-control parity (B):** there is no consumable `EtherScrollBar` control type. The dictionary
  applies an implicit style directly to stock `ScrollBar` (`EtherScrollBar.xaml:106-205`), and its
  code-behind explicitly disclaims a control API (`EtherScrollBar.xaml.cs:6-13`).
- **Design coverage (B):** both orientations, range behavior, track large-change hit regions, and
  stock named parts are retained (`EtherScrollBar.xaml:121-199`). Auto-hide, hover expansion,
  disabled fading, and arrows are intentional design exclusions (`EtherScrollBar.xaml:10-24`).
- **Consumability (A):** implicit application to both direct ScrollBar and ScrollViewer descendants
  is fixture-proven (`RuntimeVerification.ScrollBar.cs:24-53`), `ValueChanged` reaches
  `ObserveRange` (`RuntimeVerification.PropertyConsumption.cs:409`), and TwoWay `Value` is now
  proven VM-backed (`RuntimeVerification.R2.cs:158-166`). The style itself now also has a dedicated
  style-resource fixture proving the implicit `ScrollBar` style resolves and its `VerticalRoot`/
  `VerticalThumb` template parts survive, including a High-Contrast thumb-brush re-resolution
  (`RuntimeVerification.StyleResources.cs:36-38,63-64,121-125`). **The RangeValue automation
  pattern is now asserted** by `VerifyScrollBarRangeValueAutomation`
  (`RuntimeVerification.ScrollBar.cs`), called from `VerifyScrollBarAsync`: it requires
  `PatternInterface.RangeValue` to resolve to `IRangeValueProvider` and asserts the provider's
  `Value` tracks two successive owner `Value` assignments in both directions, restoring the probed
  `Value` afterward. **Confirmed by a green run:** `Verify-ConsumerFixtures.ps1` reached
  `{"marker":"ETHER_CONSUMER_SMOKE","outcome":"success", ...}`
  (`artifacts/consumer-fixtures/runtime-result-9a2b552efe3b4c8bb6c9280635e54b77.json`), whose
  `scrollBar` record carries `RangeValuePatternExposed: true`, `RangeValueTracksValue: true`,
  `MinimumRoundTrip: 5`, and `MaximumRoundTrip: 120` — proving both the RangeValue automation
  pattern and the `Minimum`/`Maximum` round-trip actually hold at runtime.

**Overall: consumer-ready.** Review B passes. Review A's TwoWay and style-surface follow-ups are closed; its RangeValue-automation and Minimum/Maximum round-trip follow-ups now have fixture assertions that are confirmed by the green run above (`scrollBar.RangeValuePatternExposed`, `RangeValueTracksValue`, `MinimumRoundTrip`, `MaximumRoundTrip`).

---

## Review B — Completeness / Parity matrix

| Property / Event | Source | Expected (consumer) | Present? | Bindable/Observable? | Verdict |
| --- | --- | --- | --- | --- | --- |
| implicit style application | design | Ether-style direct and ScrollViewer-created bars without `Style=` | implicit stock `ScrollBar` style `EtherScrollBar.xaml:106-205`; merged globally `Generic.xaml:7-13`; resolution now fixture-proven `RuntimeVerification.StyleResources.cs:36-38` | resource lookup | ✅ |
| `Minimum` / `Maximum` / `Value` | base | set/bind scroll range and current position | inherited from stock ScrollBar; orientation templates retain large-decrease/thumb/large-increase parts `EtherScrollBar.xaml:121-199`; `Value` TwoWay round-trip proven `RuntimeVerification.R2.cs:158-166`; `Minimum`/`Maximum` round-trip through `GetValue` + the CLR wrapper is asserted by `VerifyScrollBarMinimumMaximumRoundTrip` (`RuntimeVerification.ScrollBar.cs`) | ✔ DPs + event | ✅ (confirmed by the green run: `scrollBar.MinimumRoundTrip:5`, `MaximumRoundTrip:120`) |
| `ValueChanged` | base | listen to scrolling → backend | inherited; `ObserveRange` accepts the styled ScrollBar `ControlInteractionAdapter.cs:140-149` | event + adapter | ✅ |
| `Orientation` | base | render vertical and horizontal bars | both named roots and thumbs are present `EtherScrollBar.xaml:121-199` | ✔ DP | ✅ |
| arrows / small change buttons | design | determine whether stepper arrows are available | small-decrease/increase parts retained but collapsed in both orientations `EtherScrollBar.xaml:133-160,173-198` | base methods remain; arrows absent visually | 🚫 intentionally no arrows `EtherScrollBar.xaml:23-24` |
| persistent / indicator mode | design | choose persistent versus stock auto-hide/expansion behavior | persistent only; indicator state groups deliberately absent `EtherScrollBar.xaml:10-13` | no Ether mode API | 🚫 use an explicit stock/custom style when auto-hide is required `EtherScrollBar.xaml:10-13`; now called out next to the usage sample in `getting-started.md` |
| thumb pointer states | design | show hover/drag feedback | CommonStates on the Thumb template `EtherScrollBar.xaml:69-103` | visual states | ✅ |
| RangeValue automation | base | expose range and permit assistive-tech value changes | `VerifyScrollBarRangeValueAutomation` requires `PatternInterface.RangeValue` to resolve to `IRangeValueProvider` and asserts `Value` tracks two successive owner assignments (`RuntimeVerification.ScrollBar.cs`), called from `VerifyScrollBarAsync` | asserted by fixture, confirmed by a run | ✅ (confirmed by the green run: `scrollBar.RangeValuePatternExposed:true`, `RangeValueTracksValue:true`) |
| locked appearance | design | override thickness, thumb, track, and arrows | template fixes 6 px thumb, 60 px minimum, no track/arrows `EtherScrollBar.xaml:15-24,123-199` | consumer can replace Style/Template | 🚫 use a different explicit style for a different skin |

**Notes / gaps (B):**
- **This is an implicit Style, not a consumable control type.** The compiled dictionary helper is
  `EditorBrowsable(Never)` (`EtherScrollBar.xaml.cs:6-18`), while the consumer guide correctly says
  no explicit `Style=` is needed after merging the design system (`getting-started.md:431-437`).
  The consumer guide now carries an explicit persistent-only-alternative callout next to that usage
  sample (follow-up 1 below closed, doc text only).
- **The persistent behavior is explicit, not an accidental parity loss.** The XAML calls out the
  omitted indicator groups and their effects (`EtherScrollBar.xaml:10-13`); both orientations and
  the stock range template-part roles remain present (`EtherScrollBar.xaml:121-199`).
- **The consumer registry has no ScrollBar entry.** Its closed list is limited to nine known traps
  on Dropdown/Input/Switch (`scripts/UnsupportedProperties.psd1`), and the runtime type inventory
  contains only Ether control types (`RuntimeVerification.PublicPropertyInventory.cs:32-48`). The
  dedicated ScrollBar fixture plus the new style-resource fixture carry the current style contract.

---

## Review A — Consumability report

```
Component: EtherScrollBar implicit Style (no Ether control type)
Base control: ScrollBar   | Package: Ether.DesignSystem.Controls
Declared DPs: 0  | inherited public surface: stock ScrollBar / RangeBase

Rubric (styled stock surface):
  R1 DependencyProperty ....... N/A   (no Ether-owned type/DP; implicit style targets stock
                                       ScrollBar, EtherScrollBar.xaml:106-205)
  R2 Round-trips .............. PASS  (Value proven VM-backed TwoWay, R2.cs:158-166; the
                                       adapter fixture also writes Value directly,
                                       PropertyConsumption.cs:409; Minimum/Maximum round-trip
                                       through GetValue + the CLR wrapper is asserted by
                                       VerifyScrollBarMinimumMaximumRoundTrip,
                                       RuntimeVerification.ScrollBar.cs — confirmed by the green
                                       run: scrollBar.MinimumRoundTrip:5, MaximumRoundTrip:120)
  R3 Observable ............... PASS for ValueChanged  (ObserveRange,
                                       ControlInteractionAdapter.cs:140-149; exercised at
                                       PropertyConsumption.cs:409)
  R4 TwoWay ................... PASS  (VM-backed Value binding: control receives the VM value and
                                       pushes changes back, RuntimeVerification.R2.cs:158-166)
  R5 Documented ............... N/A   (no Ether-owned DP/identifier pair; implicit usage is
                                       documented at getting-started.md:431-437)

Contracts:
  C1 ItemsSource / selection .. N/A   (not a collection control; Value is the data contract)
  C2 Listenable + adapter ..... PASS  (inherited ValueChanged + ObserveRange,
                                       ControlInteractionAdapter.cs:140-149; exercised at
                                       PropertyConsumption.cs:409)
  C3 Command .................. N/A   (range interaction is Value/event based)
  C4 Automation peer/pattern .. PASS  (VerifyScrollBarRangeValueAutomation requires
                                       PatternInterface.RangeValue to resolve to
                                       IRangeValueProvider and asserts Value tracks owner
                                       assignments, RuntimeVerification.ScrollBar.cs, called from
                                       VerifyScrollBarAsync; confirmed by the green run —
                                       scrollBar.RangeValuePatternExposed:true,
                                       RangeValueTracksValue:true in
                                       runtime-result-9a2b552efe3b4c8bb6c9280635e54b77.json)

Harness: covered? YES for style/TwoWay/adapter/Minimum-Maximum/RangeValue, all confirmed by a run.
         Dedicated implicit-style/runtime coverage and adapter checks exist
         (RuntimeVerification.ScrollBar.cs:14-84; PropertyConsumption.cs:409), the style-resource
         fixture proves the implicit style and its template parts
         (RuntimeVerification.StyleResources.cs:36-38,63-64), and RuntimeVerification.R2.cs proves
         TwoWay Value (R2.cs:158-166) — all previously confirmed in the R2.11 green run
         ("EtherScrollBar.Value" in twoWayBindings.Properties;
         styleResources.ScrollBarStyleResolved:true). The RangeValue-automation and
         Minimum/Maximum-round-trip assertions (VerifyScrollBarRangeValueAutomation,
         VerifyScrollBarMinimumMaximumRoundTrip in RuntimeVerification.ScrollBar.cs, both called
         from VerifyScrollBarAsync, which VerifyAsync already awaits) were added in a follow-up
         session and are now confirmed by a later Verify-ConsumerFixtures.ps1 run whose marker
         (artifacts/consumer-fixtures/runtime-result-9a2b552efe3b4c8bb6c9280635e54b77.json,
         outcome:"success") carries scrollBar.RangeValuePatternExposed:true,
         RangeValueTracksValue:true, MinimumRoundTrip:5, MaximumRoundTrip:120.
x64 build: green — confirmed by a subsequent Verify-ConsumerFixtures.ps1 run
           (artifacts/consumer-fixtures/runtime-result-9a2b552efe3b4c8bb6c9280635e54b77.json,
           outcome:"success"), following the edit-only session that added the assertions above and
           the R2.11 runtime run recorded in docs/internal/consumability/_RUN-ON-DESKTOP.md.
```

---

## Follow-ups (concrete, actionable)

1. ~~**[B, docs] Make the persistent-only alternative explicit in consumer docs.**~~ DONE — the
   exclusion and explicit-stock/custom-style escape hatch from `EtherScrollBar.xaml:10-13` is now
   called out beside the usage sample at `getting-started.md` (doc text only).
2. ~~**[R2, R4, harness] Add style-surface and range-binding coverage.**~~ DONE — the implicit
   Style has a dedicated resolution/template-part/High-Contrast fixture
   (`RuntimeVerification.StyleResources.cs:36-38,63-64,121-125`), a VM-backed TwoWay `Value` update
   is proven (`RuntimeVerification.R2.cs:158-166`), and `Minimum`/`Maximum` round-trip through
   `GetValue` + the CLR wrapper is asserted by `VerifyScrollBarMinimumMaximumRoundTrip`
   (`RuntimeVerification.ScrollBar.cs`), called from `VerifyScrollBarAsync` — confirmed by a
   `Verify-ConsumerFixtures.ps1` run (`scrollBar.MinimumRoundTrip:5`, `MaximumRoundTrip:120` in
   `artifacts/consumer-fixtures/runtime-result-9a2b552efe3b4c8bb6c9280635e54b77.json`).
3. ~~**[C4, harness] Prove stock RangeValue automation survives the reskin.**~~ DONE —
   `VerifyScrollBarRangeValueAutomation` (`RuntimeVerification.ScrollBar.cs`), called from
   `VerifyScrollBarAsync`, requires `PatternInterface.RangeValue` to resolve to
   `IRangeValueProvider` and asserts the provider's `Value` tracks two successive owner `Value`
   assignments in both directions, restoring the probed `Value` afterward so it does not disturb
   the later screenshot/UIA/RTL capture stages that reuse the same shared instance. Disabled/
   read-only behavior and `SetValue` were not added — the residual's own text only asked for
   pattern exposure + value tracking, mirroring the minimum bar Slider/SteeringBar/ProgressBar
   already clear; confirmed by the same run (`scrollBar.RangeValuePatternExposed:true`,
   `RangeValueTracksValue:true`).

Remaining: none. All three items above have fixture assertions wired into `VerifyScrollBarAsync`
(itself already part of `VerifyAsync`'s call path), and a `Verify-ConsumerFixtures.ps1` run reached
`outcome:"success"` with all of the fields cited above present — closed and verified, not merely
asserted.
