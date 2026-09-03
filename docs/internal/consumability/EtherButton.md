# Consumability review — Button

> Reviewed against
> [the review spec](../2026-09-01-component-consumability-review-spec.md). Findings are grounded in
> source (file:line); the official WinUI baseline is Windows App SDK 2.3.1
> (`Directory.Packages.props:7`).
>
> **Reviewed:** `EtherButton` (`: Button`) —
> [EtherButton.cs](../../../src/Ether.DesignSystem.Controls/Controls/Inputs/EtherButton.cs),
> [EtherButton.xaml](../../../src/Ether.DesignSystem.Controls/Controls/Inputs/EtherButton.xaml).
> **Package:** `Ether.DesignSystem.Controls`. **Date:** 2026-09-01 (post-remediation, R3).

## Verdict at a glance
- **Base-control parity (B):** the confirmed base is `Button` (`EtherButton.cs:38`); arbitrary
  `Content`/consumer templates survive (`RuntimeVerification.Button.cs:92-150`), and inherited
  `Click` reaches `ObserveButton` (`ControlInteractionAdapter.cs:21-28`).
- **Design coverage (B):** both `IconElement` slots and the six `Variant`/`Size` combinations are
  public DPs (`EtherButton.cs:87-145`), while the keyed styles supply the matching
  Primary/Secondary/Tertiary large/small skins (`EtherButton.xaml:562-607`).
- **Consumability (A):** all four declared DPs are inventory/round-trip covered
  (`RuntimeVerification.PropertyConsumption.cs:285-288`), the Click adapter is exercised
  (`…PropertyConsumption.cs:361`), inherited `Command`/`CommandParameter` now executes through
  UIA Invoke (`RuntimeVerification.R2.cs:377,388-397`), and the Invoke automation pattern is
  asserted (`RuntimeVerification.R2.cs:261-262`) — both confirmed in the green runtime evidence
  (`commands.Controls` includes `"EtherButton"`, `automationPatterns.Patterns` includes
  `"EtherButton.Invoke"`, `artifacts/audit-runs/consumer-runtime-evidence-20260901-161450498/runtime-result.json`).

**Overall: consumer-ready.** Review B passes; Review A now passes — both follow-ups closed by the harness (`ETHER_CONSUMER_SMOKE` `outcome:"success"`).

---

## Review B — Completeness / Parity matrix

| Property / Event | Source | Expected (consumer) | Present? | Bindable/Observable? | Verdict |
| --- | --- | --- | --- | --- | --- |
| `Content` | base | set text or arbitrary button content | inherited; shape and string paths are both asserted `RuntimeVerification.Button.cs:92-135` | ✔ DP | ✅ |
| `ContentTemplate` / `ContentTemplateSelector` | base | render consumer-owned content | inherited and passed through `EtherButton.xaml:224-227`; consumer template asserted `RuntimeVerification.Button.cs:137-146` | ✔ DP | ✅ |
| `Command` / `CommandParameter` | base | invoke an MVVM command | inherited from `Button`; class does not shadow them `EtherButton.cs:38-145`; now proven to execute once with the expected parameter via UIA Invoke `RuntimeVerification.R2.cs:388-397` | ✔ DP | ✅ |
| `Click` | base | listen to activation → backend | inherited; Gallery handles it `ButtonPage.xaml:49-50`; adapter listens `ControlInteractionAdapter.cs:21-28` | event + adapter | ✅ |
| `LeftIcon` / `RightIcon` | design | independently supply leading/trailing icons | nullable `IconElement` DPs `EtherButton.cs:87-115`; visibility exercised `RuntimeVerification.Button.cs:48-90` | ✔ DP + callback | ✅ |
| `Variant` | design | choose Primary/Secondary/Tertiary | nullable enum DP `EtherButton.cs:117-132`; resolves three style families `…Button.cs:171-180` | ✔ DP + callback | ✅ |
| `Size` | design | choose Large/Small | nullable enum DP `EtherButton.cs:134-145`; styles define both sizes `EtherButton.xaml:562-607` | ✔ DP + callback | ✅ |
| `IsEnabled` | base | disable pointer/keyboard activation | inherited; disabled state retained `EtherButton.cs:27-30` | ✔ DP | ✅ |
| `BackgroundSizing` / `CharacterSpacing` / `CompositeMode` / `FontStretch` | base | customize low-level/locked appearance | inherited but deliberately not template-consumed `scripts/UnsupportedProperties.psd1:272-276` | writable DPs; no Ether visual effect | 🚫 use the supported `Variant`/`Size` and icon surface instead `EtherButton.cs:15-19` |

**Notes / gaps (B):**
- **The official Button skeleton remains the behavioral base.** The class derives from `Button`,
  declares the stock Common/Focus state names, and adds only icon-state groups
  (`EtherButton.cs:18-38`); its template binds content and consumer content templates
  (`EtherButton.xaml:224-227`).
- **The consumer-contract registry is consistent for the four recorded silent properties.** It
  labels `BackgroundSizing`/`CompositeMode` platform no-ops and `CharacterSpacing`/`FontStretch`
  design-system-owned (`scripts/UnsupportedProperties.psd1:272-276`); the general consumer guide
  explains that fixed-token appearance properties are deliberately locked
  (`design library handoff/getting-started.md:290-304`).

---

## Review A — Consumability report

```
Component: EtherButton
Base control: Button   | Package: Ether.DesignSystem.Controls
Declared DPs: 4  (LeftIcon, RightIcon, Variant, Size)  | inherited public surface: full Button

Rubric (declared DPs):
  R1 DependencyProperty ....... PASS  (four public identifiers + CLR wrappers,
                                       EtherButton.cs:87-145)
  R2 Round-trips .............. PASS  (standard GetValue/SetValue wrappers, cs:95-145;
                                       fixture asserts both paths, PropertyConsumption.cs:62-97)
  R3 Observable ............... PASS  (metadata callbacks, cs:88-145; fixture registers callbacks
                                       and supplies all four samples, PropertyConsumption.cs:74-91,285-288)
  R4 TwoWay ................... N/A   (icons/variant/size are host configuration, not user-edited state)
  R5 Documented ............... PASS  (both identifier and wrapper now state the registered null
                                       default, and LeftIcon/RightIcon/Variant/Size identifiers also
                                       state the effective shipping-style default, cs:87-145)

Contracts:
  C1 ItemsSource / selection .. N/A   (not a collection control)
  C2 Listenable + adapter ..... PASS  (inherited Click + ObserveButton,
                                       ControlInteractionAdapter.cs:21-28; exercised at
                                       PropertyConsumption.cs:361)
  C3 Command .................. PASS  (VerifyButtonCommand assigns Command/CommandParameter,
                                       invokes through UIA IInvokeProvider, and asserts the command
                                       executed once with the expected parameter,
                                       RuntimeVerification.R2.cs:377,388-397)
  C4 Automation peer/pattern .. PASS  (PatternInterface.Invoke/IInvokeProvider asserted on the
                                       ButtonAutomationPeer, RuntimeVerification.R2.cs:261-262)

Harness: covered? YES. In PublicPropertyInventoryTypes (PublicPropertyInventory.cs:34),
         DeclaredPropertyOwnerTypes (PropertyConsumption.cs:270-281), the standard-interaction
         list (PropertyConsumption.cs:361), and RuntimeVerification.Button.cs:15-19 +
         RuntimeVerification.R2.cs (Command/Invoke). Runtime marker
         ETHER_CONSUMER_SMOKE outcome:"success" includes "EtherButton" in commands.Controls and
         "EtherButton.Invoke" in automationPatterns.Patterns
         (artifacts/audit-runs/consumer-runtime-evidence-20260901-161450498/runtime-result.json).
x64 build: green (Controls + Interactions + ConsumerFixtures, per the R2.11 runtime run recorded
           in docs/internal/consumability/_RUN-ON-DESKTOP.md).
```

---

## Follow-ups (concrete, actionable)

Both prior follow-ups are DONE:
1. ~~**[C3, C4, harness] Prove command and Invoke behavior.**~~ DONE — `RuntimeVerification.R2.cs`
   now assigns a test `ICommand` + `CommandParameter`, activates the button through
   `PatternInterface.Invoke`/`IInvokeProvider`, and asserts one execution with the expected
   parameter (`R2.cs:388-397`); the existing `ObserveButton` envelope check at
   `PropertyConsumption.cs:376-377` is retained.
2. ~~**[R5] Complete the four declared-DP docs.**~~ DONE — `EtherButton.cs:87-145` now states each
   registered null default on both the `…Property` identifier and CLR wrapper, and the
   `LeftIcon`/`RightIcon`/`Variant`/`Size` identifiers additionally note the Primary/Large effective
   shipping-style default (`EtherButton.xaml:541-557`).

No remaining follow-ups.
