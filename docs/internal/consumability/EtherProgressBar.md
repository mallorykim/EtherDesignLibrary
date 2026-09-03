# Consumability review — Progress Bar

> Reviewed against
> [the review spec](../2026-09-01-component-consumability-review-spec.md). Findings are grounded in
> source (file:line); the official WinUI baseline is Windows App SDK 2.3.1
> (`Directory.Packages.props:7`).
>
> **Reviewed:** `EtherProgressBar` (`: RangeBase`) —
> [EtherProgressBar.cs](../../../src/Ether.DesignSystem.Controls/Controls/Inputs/EtherProgressBar.cs),
> [EtherProgressBar.xaml](../../../src/Ether.DesignSystem.Controls/Controls/Inputs/EtherProgressBar.xaml).
> **Package:** `Ether.DesignSystem.Controls`. **Date:** 2026-09-01 (post-remediation, R3).

## Verdict at a glance
- **Base-control parity (B):** the confirmed base is `RangeBase`, not stock `ProgressBar`
  (`EtherProgressBar.cs:34`); `Minimum`/`Maximum`/`Value`/`ValueChanged` survive and update layout
  (`EtherProgressBar.cs:103-157`).
- **Design coverage (B):** determinate progress plus independently toggled title/value labels is
  complete (`EtherProgressBar.cs:59-100`; `EtherProgressBar.xaml:68-112`). Indeterminate,
  error, and paused states are explicit exclusions because this type is not `ProgressBar`
  (`EtherProgressBar.cs:19-22`).
- **Consumability (A):** five declared DPs and the range adapter are fixture-covered
  (`RuntimeVerification.PropertyConsumption.cs:270-281,291-294,377`), read-only RangeValue automation is
  comprehensively asserted (`RuntimeVerification.ProgressBar.cs:65-138`), and strict R5
  documentation now passes: both identifier and wrapper XML docs state each registered/effective
  default (`EtherProgressBar.cs:60-100`).

**Overall: consumer-ready.** Review B passes; Review A now passes — its one follow-up is closed by the R5 documentation fix.

---

## Review B — Completeness / Parity matrix

| Property / Event | Source | Expected (consumer) | Present? | Bindable/Observable? | Verdict |
| --- | --- | --- | --- | --- | --- |
| `Minimum` / `Maximum` | base | set/bind progress range | inherited `RangeBase`; overrides update fill `EtherProgressBar.cs:125-136` | ✔ DPs | ✅ |
| `Value` | base | bind backend progress | inherited; fill ratio consumes it `EtherProgressBar.cs:113-123,146-157` | ✔ DP + event | ✅ |
| `ValueChanged` | base | observe data-driven progress → backend | inherited and preserved `EtherProgressBar.cs:113-123`; `ObserveRange` accepts it `ControlInteractionAdapter.cs:140-149` | event + adapter | ✅ |
| `Title` / `ValueFormat` | design | supply left label content / format the live value label | DPs `EtherProgressBar.cs:60-79`; template presents both `EtherProgressBar.xaml:101-111` | ✔ DPs | ✅ |
| `ShowTitle` / `ShowValue` | design | independently show/hide labels | bool DPs `EtherProgressBar.cs:82-100`; four states `EtherProgressBar.xaml:68-98` | ✔ DPs + callbacks | ✅ |
| `IsIndeterminate` / error / paused states | design | show non-determinate or status variants | ✗; explicitly excluded by RangeBase lineage `EtherProgressBar.cs:19-22,34` | n/a | 🚫 use stock WinUI `ProgressBar` when those states are required; EtherProgressBar is determinate-only `EtherProgressBar.cs:11-22` |
| `IsEnabled` | base | expose disabled platform state | inherited and base-handled; visual appearance intentionally unchanged `scripts/UnsupportedProperties.psd1:371` | ✔ DP | ✅ |
| read-only RangeValue automation | design | expose progress value without allowing assistive-tech edits | custom peer implements read-only RangeValue `EtherProgressBar.cs:172-210` | UIA observable | ✅ |
| locked appearance | base | override track/fill typography/chrome | registry marks appearance family design-system-owned `scripts/UnsupportedProperties.psd1:356-370` | writable DPs; no Ether visual effect | 🚫 use range and label APIs; progress chrome remains token-owned `getting-started.md:290-304` |

**Notes / gaps (B):**
- **The lineage is `RangeBase`, not `ProgressBar`.** The source declaration is
  `public sealed class EtherProgressBar : RangeBase` (`EtherProgressBar.cs:34`), so determinate
  range APIs are inherited while stock `ProgressBar.IsIndeterminate` and status states are not
  part of its base surface (`EtherProgressBar.cs:14-22`).
- The Gallery describes exactly the four supported label combinations and determinate design
  (`ProgressBarPage.xaml:8-16,39-83`); the style supplies those four LabelStates
  (`EtherProgressBar.xaml:68-98`).
- The registry's appearance and `IsEnabled` classifications reconcile with live behavior:
  appearance is fixed, while `IsEnabled` remains base-functional without a distinct visual
  (`scripts/UnsupportedProperties.psd1:356-372`).

---

## Review A — Consumability report

```
Component: EtherProgressBar
Base control: RangeBase   | Package: Ether.DesignSystem.Controls
Declared DPs: 5  (Title, ValueFormat, ValueContentConverter, ShowTitle, ShowValue)
                 | inherited public surface: RangeBase

Rubric (declared DPs):
  R1 DependencyProperty ....... PASS  (four public identifiers + wrappers,
                                       EtherProgressBar.cs:60-100)
  R2 Round-trips .............. PASS  (standard wrappers, cs:64-100; fixture asserts both paths,
                                       PropertyConsumption.cs:62-97)
  R3 Observable ............... PASS  (fixture registers callbacks and supplies all four samples,
                                       PropertyConsumption.cs:74-91,291-294)
  R4 TwoWay ................... N/A   (progress Value is producer/backend-driven, not user-edited;
                                       automation is intentionally read-only, EtherProgressBar.cs:172-190)
  R5 Documented ............... PASS  (both identifier and wrapper docs now state the registered
                                       metadata default, and ShowTitle/ShowValue additionally note
                                       the shipping style's effective true default,
                                       EtherProgressBar.cs:60-100)

Contracts:
  C1 ItemsSource / selection .. N/A   (not a collection control; Value is the data contract)
  C2 Listenable + adapter ..... PASS  (inherited ValueChanged + ObserveRange,
                                       ControlInteractionAdapter.cs:140-149; exercised at
                                       PropertyConsumption.cs:377)
  C3 Command .................. N/A   (non-interactive progress display)
  C4 Automation peer/pattern .. PASS  (custom read-only RangeValue peer,
                                       EtherProgressBar.cs:172-210; fixture asserts role, values,
                                       notifications, and rejected SetValue,
                                       RuntimeVerification.ProgressBar.cs:65-138)

Harness: covered? YES. In PublicPropertyInventoryTypes (PublicPropertyInventory.cs:39),
         DeclaredPropertyOwnerTypes (PropertyConsumption.cs:270-281), the standard-interaction
         list (PropertyConsumption.cs:377), and RuntimeVerification.ProgressBar.cs:13-16.
x64 build: green (Controls + Interactions + ConsumerFixtures, per the R2.11 runtime run recorded
           in docs/internal/consumability/_RUN-ON-DESKTOP.md).
```

---

## Follow-ups (concrete, actionable)

1. ~~**[R5] Complete the four declared-DP docs.**~~ DONE — `EtherProgressBar.cs:60-100` now states
   each metadata default on both the `…Property` identifier and wrapper, and distinguishes the
   registered false defaults for `ShowTitle`/`ShowValue` from the shipping style's true values at
   `EtherProgressBar.xaml:48-58`.

No remaining follow-ups.
