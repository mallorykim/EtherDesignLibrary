# Consumability review — Radio Button

> Reviewed against
> [the review spec](../2026-09-01-component-consumability-review-spec.md). Findings are grounded in
> source (file:line); the official WinUI baseline is Windows App SDK 2.3.1
> (`Directory.Packages.props:7`).
>
> **Reviewed:** `EtherRadioButton` (`: RadioButton`) —
> [EtherRadioButton.cs](../../../src/Ether.DesignSystem.Controls/Controls/Inputs/EtherRadioButton.cs),
> [EtherRadioButton.xaml](../../../src/Ether.DesignSystem.Controls/Controls/Inputs/EtherRadioButton.xaml).
> **Package:** `Ether.DesignSystem.Controls`. **Date:** 2026-09-01 (post-remediation, R3).

## Verdict at a glance
- **Base-control parity (B):** the confirmed base is `RadioButton` (`EtherRadioButton.cs:30`), and
  the class explicitly retains native `GroupName` mutual exclusion plus Common/Check states
  (`EtherRadioButton.cs:6-30`).
- **Design coverage (B):** the Ether skin is deliberately two-state (D4) and coerces both
  `IsThreeState=true` and null `IsChecked` to false (`EtherRadioButton.cs:75-103`); the fixture now
  asserts that coercion directly instead of expecting an exception
  (`RuntimeVerification.RadioButton.cs:67,85-94`, `VerifyRadioButtonTwoStateCoercion`).
- **Consumability (A):** radio state reaches `ObserveToggle`
  (`RuntimeVerification.PropertyConsumption.cs:364`); TwoWay `IsChecked` is now proven VM-backed
  (`RuntimeVerification.R2.cs:78-86`), inherited `Command`/`CommandParameter` executes through the
  control's real UIA pattern — `SelectionItem`, not `Toggle` — via `ISelectionItemProvider.Select()`
  (`RuntimeVerification.R2.cs:384,419-430`), and the SelectionItem automation pattern itself is
  asserted with `IsSelected` tracking `IsChecked` (`RuntimeVerification.R2.cs:267-275`) — all
  confirmed in the green runtime evidence (`twoWayBindings.Properties` includes
  `"EtherRadioButton.IsChecked"`, `commands.Controls` includes `"EtherRadioButton"`,
  `automationPatterns.Patterns` includes `"EtherRadioButton.SelectionItem"`).

**Overall: consumer-ready.** Review B's D4 exclusion is documented 🚫 (not a stale-contract conflict); Review A now passes — all follow-ups closed by the harness (`ETHER_CONSUMER_SMOKE` `outcome:"success"`). `GroupName` mutual exclusion across peers now has a dedicated fixture, `VerifyRadioButtonGroupNameMutualExclusionAsync` (`RuntimeVerification.RadioButton.cs`), called from `VerifyRadioButtonAsync` — CLOSED, confirmed by a green `Verify-ConsumerFixtures.ps1` run whose marker (`artifacts/consumer-fixtures/runtime-result-9a2b552efe3b4c8bb6c9280635e54b77.json`) carries `radioButton.GroupNameMutualExclusionVerified: true`.

---

## Review B — Completeness / Parity matrix

| Property / Event | Source | Expected (consumer) | Present? | Bindable/Observable? | Verdict |
| --- | --- | --- | --- | --- | --- |
| `Content` / `ContentTemplate` / selector | base | set text or arbitrary option label | inherited; template passes all three `EtherRadioButton.xaml:281-283` | ✔ DP | ✅ |
| `GroupName` | base | enforce mutual exclusion among peers | inherited and named as the native mechanism `EtherRadioButton.cs:10-13`; mutual exclusion across two same-`GroupName` instances is asserted by `VerifyRadioButtonGroupNameMutualExclusionAsync` (`RuntimeVerification.RadioButton.cs`) | ✔ DP | ✅ (confirmed by the green run: `radioButton.GroupNameMutualExclusionVerified:true`) |
| `IsChecked` | base | two-way bind selected state | inherited; callback keeps it boolean `EtherRadioButton.cs:70-81`; VM-backed TwoWay round-trip proven `RuntimeVerification.R2.cs:78-86` | ✔ DP + events | ✅ |
| `Checked` / `Unchecked` | base | listen to choice → backend | inherited; `ObserveToggle` subscribes both `ControlInteractionAdapter.cs:71-86` | events + adapter | ✅ |
| `Command` / `CommandParameter` | base | invoke an MVVM command on activation | inherited from the ToggleButton lineage; not shadowed `EtherRadioButton.cs:30-71`; now proven to execute once with the expected parameter via UIA SelectionItem.Select() `RuntimeVerification.R2.cs:419-430` | ✔ DP | ✅ |
| `IsThreeState` / indeterminate | base | optionally expose a third state | inherited DP is forced back to false; null is also forced false `EtherRadioButton.cs:75-103` | DP is observable but true/null cannot persist | 🚫 use boolean `IsChecked`; Ether radio is two-state (D4) `EtherRadioButton.cs:6-13` |
| `IsEnabled` | base | disable interaction | inherited; Disabled state retained `EtherRadioButton.cs:24-30` | ✔ DP | ✅ |
| locked appearance (`Background`, `Border*`, `CornerRadius`, `FontSize`, `Padding`, content alignment) | base | customize stock chrome | inherited but deliberately not template-consumed `scripts/UnsupportedProperties.psd1:374-387` | writable DPs; no Ether visual effect | 🚫 use the fixed Ether radio skin `docs/consumers/getting-started.md:290-304` |

**Notes / gaps (B):**
- **Native grouping remains the selection mechanism.** The class identifies `GroupName` as the
  base behavior (`EtherRadioButton.cs:10-13`), while the Gallery supplies explicit group names
  (`RadioButtonPage.xaml:18-28`); `VerifyRadioButtonGroupNameMutualExclusionAsync`
  (`RuntimeVerification.RadioButton.cs`), called from `VerifyRadioButtonAsync`, hosts two
  isolated same-`GroupName` `EtherRadioButton` instances under a shared scratch-Canvas parent,
  checks the first, then checks the second, and asserts the first auto-unchecks — CLOSED,
  confirmed by a green `Verify-ConsumerFixtures.ps1` run
  (`radioButton.GroupNameMutualExclusionVerified: true` in
  `artifacts/consumer-fixtures/runtime-result-9a2b552efe3b4c8bb6c9280635e54b77.json`).
- **The enforcement contract is now reconciled, not stale (D4).** Live code silently resets
  `IsThreeState` and coerces null `IsChecked` (`EtherRadioButton.cs:92-103`); the fixture asserts
  that coercion directly (`RuntimeVerification.RadioButton.cs:67,85-94`), and the consumer guide
  describes the two-state contract instead of the old "intercept + throw" behavior
  (`docs/consumers/getting-started.md:254-260`).
- The registry's RadioButton entries consistently classify fixed appearance as
  design-system-owned/platform-noop (`scripts/UnsupportedProperties.psd1:374-387`), matching the
  consumer guide's locked-token policy (`docs/consumers/getting-started.md:290-304`).

---

## Review A — Consumability report

```
Component: EtherRadioButton
Base control: RadioButton   | Package: Ether.DesignSystem.Controls
Declared DPs: 0  | inherited public surface: full RadioButton/ToggleButton (with IsThreeState excluded)

Rubric (inherited state surface):
  R1 DependencyProperty ....... PASS-by-inheritance  (IsChecked/IsThreeState/GroupName inherited;
                                       class declaration and callbacks, EtherRadioButton.cs:30,70-81)
  R2 Round-trips .............. PASS for supported boolean states  (fixture sets false/true and
                                       observes both faces, RuntimeVerification.RadioButton.cs:41-58)
  R3 Observable ............... PASS-by-inheritance  (Checked/Unchecked are adapted,
                                       ControlInteractionAdapter.cs:71-86)
  R4 TwoWay ................... PASS  (VM-backed boolean binding: control receives the VM value and
                                       pushes changes back, RuntimeVerification.R2.cs:78-86)
  R5 Documented ............... N/A   (no public Ether-owned property/identifier pair)

Contracts:
  C1 ItemsSource / selection .. N/A   (not a collection control; GroupName coordinates peers)
  C2 Listenable + adapter ..... PASS  (Checked/Unchecked + ObserveToggle,
                                       ControlInteractionAdapter.cs:71-86; exercised at
                                       PropertyConsumption.cs:364)
  C3 Command .................. PASS  (VerifySelectionItemCommand assigns Command/CommandParameter,
                                       actuates through the control's real UIA pattern —
                                       ISelectionItemProvider.Select(), not Toggle — and asserts the
                                       command executed once with the expected parameter,
                                       RuntimeVerification.R2.cs:384,419-430)
  C4 Automation peer/pattern .. PASS  (PatternInterface.SelectionItem/ISelectionItemProvider
                                       asserted on the RadioButtonAutomationPeer, with IsSelected
                                       tracking IsChecked in both directions — NOT Toggle,
                                       RuntimeVerification.R2.cs:267-275)

Harness: covered? YES. In PublicPropertyInventoryTypes (PublicPropertyInventory.cs:40), the
         standard-interaction list (PropertyConsumption.cs:364), and RuntimeVerification.RadioButton.cs:14-17
         + RuntimeVerification.R2.cs (TwoWay/Command/SelectionItem); no declared Ether DPs require
         DeclaredPropertyOwnerTypes. The IsThreeState fixture now matches live code (both assert
         coercion to false: EtherRadioButton.cs:92-103 and RuntimeVerification.RadioButton.cs:67,85-94).
         Runtime marker ETHER_CONSUMER_SMOKE outcome:"success" includes "EtherRadioButton.IsChecked"
         in twoWayBindings.Properties, "EtherRadioButton" in commands.Controls, and
         "EtherRadioButton.SelectionItem" in automationPatterns.Patterns
         (artifacts/audit-runs/consumer-runtime-evidence-20260901-161450498/runtime-result.json).
         A follow-up session added VerifyRadioButtonGroupNameMutualExclusionAsync to
         RuntimeVerification.RadioButton.cs, called from VerifyRadioButtonAsync (itself already
         part of VerifyAsync's call path); a later Verify-ConsumerFixtures.ps1 run confirmed it —
         its marker (artifacts/consumer-fixtures/runtime-result-9a2b552efe3b4c8bb6c9280635e54b77.json,
         outcome:"success") carries radioButton.GroupNameMutualExclusionVerified:true.
x64 build: green — confirmed by that same subsequent Verify-ConsumerFixtures.ps1 run, following
           the edit-only session that added the assertion above and the R2.11 runtime run recorded
           in docs/internal/consumability/_RUN-ON-DESKTOP.md.
```

---

## Follow-ups (concrete, actionable)

1. ~~**[B, harness, docs] Reconcile the two-state enforcement contract.**~~ DONE (D4) — see the
   Checkbox report's identical fix; `RuntimeVerification.RadioButton.cs`'s
   `VerifyRadioButtonTwoStateCoercion` now asserts both coercions instead of expecting an exception
   (`RadioButton.cs:67,85-94`), and the consumer guide matches
   (`docs/consumers/getting-started.md:254-260`).
2. ~~**[B, R4, C3, C4, harness] Prove inherited consumer paths.**~~ DONE for TwoWay/Command/
   automation — `RuntimeVerification.R2.cs` adds a TwoWay-bound boolean VM (`R2.cs:78-86`), an
   `ICommand`/parameter assertion through the control's real SelectionItem UIA actuation
   (`R2.cs:419-430`), and the SelectionItem automation pattern with `IsSelected`/`IsChecked`
   tracking (`R2.cs:267-275`); the adapter check at `PropertyConsumption.cs:364` is retained.
   ~~Mutual-exclusion across two same-`GroupName` instances is still not exercised by a dedicated
   fixture~~ DONE — `VerifyRadioButtonGroupNameMutualExclusionAsync`
   (`RuntimeVerification.RadioButton.cs`), called from `VerifyRadioButtonAsync`, hosts two
   `EtherRadioButton` instances sharing a `GroupName` under a common scratch-Canvas parent, checks
   the first, then checks the second, and asserts the first auto-unchecks. Harness-only; no
   product code change. Confirmed by a green `Verify-ConsumerFixtures.ps1` run
   (`radioButton.GroupNameMutualExclusionVerified: true` in
   `artifacts/consumer-fixtures/runtime-result-9a2b552efe3b4c8bb6c9280635e54b77.json`).

Remaining: none. The `GroupName` mutual-exclusion fixture is wired into `VerifyRadioButtonAsync`
(already part of `VerifyAsync`'s call path) and has been exercised by an actual
`Verify-ConsumerFixtures.ps1` run, which reached `outcome:"success"` — closed and verified.
