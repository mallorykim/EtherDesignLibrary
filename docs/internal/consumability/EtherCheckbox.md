# Consumability review — Checkbox

> Reviewed against
> [the review spec](../2026-09-01-component-consumability-review-spec.md). Findings are grounded in
> source (file:line); the official WinUI baseline is Windows App SDK 2.3.1
> (`Directory.Packages.props:7`).
>
> **Reviewed:** `EtherCheckbox` (`: CheckBox`) —
> [EtherCheckbox.cs](../../../src/Ether.DesignSystem.Controls/Controls/Inputs/EtherCheckbox.cs),
> [EtherCheckbox.xaml](../../../src/Ether.DesignSystem.Controls/Controls/Inputs/EtherCheckbox.xaml).
> **Package:** `Ether.DesignSystem.Controls`. **Date:** 2026-09-01 (post-remediation, R3).

## Verdict at a glance
- **Base-control parity (B):** the confirmed base is `CheckBox` (`EtherCheckbox.cs:30`); content,
  two-state `IsChecked`, Checked/Unchecked events, and the stock Common/Check state names survive
  (`EtherCheckbox.cs:6-30`; `EtherCheckbox.xaml:121-162`).
- **Design coverage (B):** the control is deliberately two-state (D4): `IsThreeState=true` and null
  `IsChecked` are coerced back to false (`EtherCheckbox.cs:75-104`). This is now a documented,
  🚫-marked design decision, not a stale conflict — the registry carries a D4 note
  (`scripts/UnsupportedProperties.psd1` Checkbox section) and the fixture asserts the coercion
  directly instead of expecting an exception (`RuntimeVerification.Checkbox.cs:67,85-94`,
  `VerifyTwoStateCoercion`).
- **Consumability (A):** the type is in the public inventory and its toggle adapter is exercised
  (`RuntimeVerification.PublicPropertyInventory.cs:35`; `RuntimeVerification.PropertyConsumption.cs:362`);
  TwoWay `IsChecked` is now proven VM-backed in both directions
  (`RuntimeVerification.R2.cs:68-76`), inherited `Command`/`CommandParameter` executes through UIA
  Toggle (`RuntimeVerification.R2.cs:379,400-411`), and the Toggle automation pattern is asserted
  (`RuntimeVerification.R2.cs:265-266`) — all confirmed in the green runtime evidence
  (`twoWayBindings.Properties` includes `"EtherCheckbox.IsChecked"`, `commands.Controls` includes
  `"EtherCheckbox"`, `automationPatterns.Patterns` includes `"EtherCheckbox.Toggle"`).

**Overall: consumer-ready.** Review B's D4 exclusion is documented 🚫 (not a stale-contract conflict); Review A now passes — both follow-ups closed by the harness (`ETHER_CONSUMER_SMOKE` `outcome:"success"`).

---

## Review B — Completeness / Parity matrix

| Property / Event | Source | Expected (consumer) | Present? | Bindable/Observable? | Verdict |
| --- | --- | --- | --- | --- | --- |
| `Content` / `ContentTemplate` / selector | base | set text or arbitrary label content | inherited; template passes all three `EtherCheckbox.xaml:245-247` | ✔ DP | ✅ |
| `IsChecked` | base | two-way bind checked state | inherited; callback keeps it boolean `EtherCheckbox.cs:70-82`; VM-backed TwoWay round-trip proven `RuntimeVerification.R2.cs:68-76` | ✔ DP + events | ✅ |
| `Checked` / `Unchecked` | base | listen to state → backend | inherited; `ObserveToggle` subscribes both `ControlInteractionAdapter.cs:71-86` | events + adapter | ✅ |
| `Command` / `CommandParameter` | base | invoke an MVVM command on activation | inherited from the ToggleButton lineage; not shadowed `EtherCheckbox.cs:30-71`; now proven to execute once with the expected parameter via UIA Toggle `RuntimeVerification.R2.cs:400-411` | ✔ DP | ✅ |
| `IsThreeState` / indeterminate | base | optionally expose a third state | inherited DP is forced back to false and null `IsChecked` is forced to false `EtherCheckbox.cs:75-104` | DP is observable but true/null cannot persist | 🚫 use boolean `IsChecked`; Ether design is two-state (D4) `EtherCheckbox.cs:6-13` |
| `IsEnabled` | base | disable interaction | inherited; Disabled state retained `EtherCheckbox.cs:24-30` | ✔ DP | ✅ |
| locked appearance (`Background`, `Border*`, `CornerRadius`, `FontSize`, `Padding`, content alignment) | base | customize stock chrome | inherited but deliberately not template-consumed `scripts/UnsupportedProperties.psd1:280-293` | writable DPs; no Ether visual effect | 🚫 use the fixed Ether checkbox skin `docs/consumers/getting-started.md:290-304` |

**Notes / gaps (B):**
- **Two-state is a documented design deviation (D4), not a missing accidental state.** The class
  remarks say no indeterminate visual exists (`EtherCheckbox.cs:6-13`), and the style fixes
  `IsThreeState=False` (`EtherCheckbox.xaml:270-282`).
- **The enforcement contract is now reconciled, not stale.** Live code silently resets
  `IsThreeState` and coerces null `IsChecked` to false (`EtherCheckbox.cs:93-104`); the fixture no
  longer expects a thrown `NotSupportedException` — it asserts both coercions directly
  (`RuntimeVerification.Checkbox.cs:67,85-94`, `VerifyTwoStateCoercion`), and the registry carries a
  D4 note recording the intentional behavior instead of the old "intercept + throw" description
  (`scripts/UnsupportedProperties.psd1` Checkbox section header comment).
- The unsupported-property registry's Checkbox appearance entries are all explicit
  design-system-owned or platform-noop appearance decisions
  (`scripts/UnsupportedProperties.psd1:280-293`); no live template binding contradicts those
  recorded entries.

---

## Review A — Consumability report

```
Component: EtherCheckbox
Base control: CheckBox   | Package: Ether.DesignSystem.Controls
Declared DPs: 0  | inherited public surface: full CheckBox/ToggleButton (with IsThreeState excluded)

Rubric (inherited state surface):
  R1 DependencyProperty ....... PASS-by-inheritance  (IsChecked/IsThreeState are inherited;
                                       class declaration and callbacks, EtherCheckbox.cs:30,70-82)
  R2 Round-trips .............. PASS for supported boolean states  (fixture sets false/true and
                                       observes both faces, RuntimeVerification.Checkbox.cs:41-58)
  R3 Observable ............... PASS-by-inheritance  (Checked/Unchecked are adapted,
                                       ControlInteractionAdapter.cs:71-86)
  R4 TwoWay ................... PASS  (VM-backed nullable-boolean binding: control receives the VM
                                       value and pushes changes back, RuntimeVerification.R2.cs:68-76)
  R5 Documented ............... N/A   (no public Ether-owned property/identifier pair)

Contracts:
  C1 ItemsSource / selection .. N/A   (not a collection control)
  C2 Listenable + adapter ..... PASS  (Checked/Unchecked + ObserveToggle,
                                       ControlInteractionAdapter.cs:71-86; exercised at
                                       PropertyConsumption.cs:362)
  C3 Command .................. PASS  (VerifyToggleCommand assigns Command/CommandParameter,
                                       toggles through UIA IToggleProvider, and asserts the command
                                       executed once with the expected parameter,
                                       RuntimeVerification.R2.cs:379,400-411)
  C4 Automation peer/pattern .. PASS  (PatternInterface.Toggle/IToggleProvider asserted on the
                                       CheckBoxAutomationPeer, RuntimeVerification.R2.cs:265-266)

Harness: covered? YES. In PublicPropertyInventoryTypes (PublicPropertyInventory.cs:35), the
         standard-interaction list (PropertyConsumption.cs:362), and RuntimeVerification.Checkbox.cs:14-17
         + RuntimeVerification.R2.cs (TwoWay/Command/Toggle); no declared Ether DPs require
         DeclaredPropertyOwnerTypes. The IsThreeState fixture now matches live code (both assert
         coercion to false: EtherCheckbox.cs:93-104 and RuntimeVerification.Checkbox.cs:67,85-94).
         Runtime marker ETHER_CONSUMER_SMOKE outcome:"success" includes "EtherCheckbox.IsChecked"
         in twoWayBindings.Properties, "EtherCheckbox" in commands.Controls, and
         "EtherCheckbox.Toggle" in automationPatterns.Patterns
         (artifacts/audit-runs/consumer-runtime-evidence-20260901-161450498/runtime-result.json).
x64 build: green (Controls + Interactions + ConsumerFixtures, per the R2.11 runtime run recorded
           in docs/internal/consumability/_RUN-ON-DESKTOP.md).
```

---

## Follow-ups (concrete, actionable)

1. ~~**[B, harness, docs] Reconcile the two-state enforcement contract.**~~ DONE (D4) — the control
   silently coerces (`EtherCheckbox.cs:93-104`); `RuntimeVerification.Checkbox.cs`'s
   `VerifyTwoStateCoercion` (renamed from `VerifyIsThreeStateRejected`) now asserts
   `IsChecked=null ⇒ false` and `IsThreeState=true ⇒ false` instead of expecting an exception
   (`Checkbox.cs:64-88`), and the registry/consumer-guide description was updated to match
   (`scripts/UnsupportedProperties.psd1`; `docs/consumers/getting-started.md:254-260`).
2. ~~**[R4, C3, C4, harness] Prove the inherited consumer paths.**~~ DONE — `RuntimeVerification.R2.cs`
   adds a TwoWay-bound nullable-boolean VM (`R2.cs:68-76`), an `ICommand`/parameter activation
   assertion through UIA Toggle (`R2.cs:400-411`), and a `PatternInterface.Toggle`/`IToggleProvider`
   check (`R2.cs:265-266`); the backend envelope assertion at `PropertyConsumption.cs:362` is
   retained.

No remaining follow-ups.
