# Consumability review — Intelligence Button

> Reviewed against
> [the review spec](../2026-09-01-component-consumability-review-spec.md). Findings are grounded in
> source (file:line); the official WinUI baseline is Windows App SDK 2.3.1
> (`Directory.Packages.props:7`).
>
> **Reviewed:** `EtherIntelligenceButton` (`: Button`) —
> [EtherIntelligenceButton.cs](../../../src/Ether.DesignSystem.Controls/Controls/Inputs/EtherIntelligenceButton.cs),
> [EtherIntelligenceButton.xaml](../../../src/Ether.DesignSystem.Controls/Controls/Inputs/EtherIntelligenceButton.xaml).
> **Package:** `Ether.DesignSystem.Controls`. **Date:** 2026-09-01 (post-remediation, R3).

## Verdict at a glance
- **Base-control parity (B):** the confirmed base is `Button` (`EtherIntelligenceButton.cs:33`),
  and the stock content, Click, command, CommonStates, and FocusStates surface remains inherited
  (`EtherIntelligenceButton.cs:7-33`).
- **Design coverage (B):** the single intelligence treatment is intentionally template-owned; the
  class explicitly adds no variants, trailing-icon, or animation API
  (`EtherIntelligenceButton.cs:12-15`), while the Gallery exposes the intended functional Click
  usage (`IntelligenceButtonPage.xaml:24-37`).
- **Consumability (A):** Click is routed through `ObserveButton` and exercised
  (`RuntimeVerification.PropertyConsumption.cs:375`), inherited `Command`/`CommandParameter` now
  executes through UIA Invoke (`RuntimeVerification.R2.cs:378,388-397`), and the Invoke automation
  pattern is asserted (`RuntimeVerification.R2.cs:263-264`) — both confirmed in the green runtime
  evidence (`commands.Controls` includes `"EtherIntelligenceButton"`,
  `automationPatterns.Patterns` includes `"EtherIntelligenceButton.Invoke"`,
  `artifacts/audit-runs/consumer-runtime-evidence-20260901-161450498/runtime-result.json`).

**Overall: consumer-ready.** Review B passes; Review A now passes — its one follow-up is closed by the harness (`ETHER_CONSUMER_SMOKE` `outcome:"success"`).

---

## Review B — Completeness / Parity matrix

| Property / Event | Source | Expected (consumer) | Present? | Bindable/Observable? | Verdict |
| --- | --- | --- | --- | --- | --- |
| `Content` / `ContentTemplate` / selector | base | set text or rich button content | inherited and passed through `EtherIntelligenceButton.xaml:216-219` | ✔ DP | ✅ |
| `Command` / `CommandParameter` | base | invoke an MVVM command | inherited from `Button`; class adds no shadowing surface `EtherIntelligenceButton.cs:33-61`; now proven to execute once with the expected parameter via UIA Invoke `RuntimeVerification.R2.cs:388-397` | ✔ DP | ✅ |
| `Click` | base | listen to activation → backend | inherited; Gallery handles it `IntelligenceButtonPage.xaml:24-37`; `ObserveButton` accepts it `ControlInteractionAdapter.cs:21-28` | event + adapter | ✅ |
| intelligence visual variant | design | receive the gradient border/glow skin | implicit default style/template `EtherIntelligenceButton.xaml:239-248` | style | ✅ |
| variants / trailing icon / animation | design | determine whether additional design knobs exist | explicitly outside this component's design contract `EtherIntelligenceButton.cs:12-15` | n/a | 🚫 use `EtherButton` for variants/icons `EtherButton.cs:9-19` |
| `IsEnabled` | base | disable activation and dim effects | inherited; disabled behavior asserted `RuntimeVerification.IntelligenceButton.cs:96-109` | ✔ DP | ✅ |
| locked appearance (`Background`, `Border*`, `CornerRadius`, font/foreground family) | base | override intelligence chrome | inherited but deliberately not template-consumed `scripts/UnsupportedProperties.psd1:321-333` | writable DPs; no Ether visual effect | 🚫 use the fixed intelligence skin `docs/consumers/getting-started.md:290-304` |
| content alignment | base | position supplied content | inherited and template-bound `EtherIntelligenceButton.xaml:205-219` | ✔ DP | ✅ |

**Notes / gaps (B):**
- **This is a semantic Button reskin, not a bespoke interaction control.** Its declaration is
  `: Button`, the class retains stock state group names, and it adds no public members
  (`EtherIntelligenceButton.cs:12-33,54-61`).
- **The consumer registry agrees with live XAML.** It records content alignment as consumed and
  the surrounding chrome/font/foreground properties as design-system-owned
  (`scripts/UnsupportedProperties.psd1:321-335`); the template binds the former
  (`EtherIntelligenceButton.xaml:205-219`) and owns the latter through its keyed style/template
  (`EtherIntelligenceButton.xaml:239-248`).

---

## Review A — Consumability report

```
Component: EtherIntelligenceButton
Base control: Button   | Package: Ether.DesignSystem.Controls
Declared DPs: 0  | inherited public surface: full Button

Rubric (inherited button surface):
  R1 DependencyProperty ....... PASS-by-inheritance  (Button lineage,
                                       EtherIntelligenceButton.cs:33)
  R2 Round-trips .............. UNVERIFIED  (fixture tests visual state/defaults, not inherited
                                       DP round-trips, RuntimeVerification.IntelligenceButton.cs:14-127)
  R3 Observable ............... PASS for Click  (ObserveButton, ControlInteractionAdapter.cs:21-28;
                                       exercised at PropertyConsumption.cs:375)
  R4 TwoWay ................... N/A   (button has no user-edited value property)
  R5 Documented ............... N/A   (no public Ether-owned property/identifier pair)

Contracts:
  C1 ItemsSource / selection .. N/A   (not a collection control)
  C2 Listenable + adapter ..... PASS  (inherited Click + ObserveButton,
                                       ControlInteractionAdapter.cs:21-28; exercised at
                                       PropertyConsumption.cs:375)
  C3 Command .................. PASS  (VerifyButtonCommand assigns Command/CommandParameter,
                                       invokes through UIA IInvokeProvider, and asserts the command
                                       executed once with the expected parameter,
                                       RuntimeVerification.R2.cs:378,388-397)
  C4 Automation peer/pattern .. PASS  (PatternInterface.Invoke/IInvokeProvider asserted on the
                                       ButtonAutomationPeer, RuntimeVerification.R2.cs:263-264)

Harness: covered? YES. In PublicPropertyInventoryTypes (PublicPropertyInventory.cs:38),
         the standard-interaction list (PropertyConsumption.cs:375), and
         RuntimeVerification.IntelligenceButton.cs:14-17 + RuntimeVerification.R2.cs
         (Command/Invoke); no Ether-owned public DPs require DeclaredPropertyOwnerTypes. Runtime
         marker ETHER_CONSUMER_SMOKE outcome:"success" includes "EtherIntelligenceButton" in
         commands.Controls and "EtherIntelligenceButton.Invoke" in automationPatterns.Patterns
         (artifacts/audit-runs/consumer-runtime-evidence-20260901-161450498/runtime-result.json).
x64 build: green (Controls + Interactions + ConsumerFixtures, per the R2.11 runtime run recorded
           in docs/internal/consumability/_RUN-ON-DESKTOP.md).
```

---

## Follow-ups (concrete, actionable)

1. ~~**[C3, C4, harness] Prove the inherited Button contract.**~~ DONE — `RuntimeVerification.R2.cs`
   now assigns a test `ICommand`/`CommandParameter`, activates the button through
   `PatternInterface.Invoke`/`IInvokeProvider`, and asserts one execution with the expected
   parameter (`R2.cs:378,388-397`); the Click-to-backend assertion at `PropertyConsumption.cs:375`
   is retained.

No remaining follow-ups. (R2 "Round-trips" for the inherited surface stays informally
`UNVERIFIED` beyond Click/Command — the same status every base-inheriting-only control in this
review carries; it is not a Review B gap and was not part of the closed follow-up.)
