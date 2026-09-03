# Consumability review — Intelligence Button

> Reviewed against
> [the review spec](../2026-09-01-component-consumability-review-spec.md). Findings are grounded in
> source (file:line); the official WinUI baseline is Windows App SDK 2.3.1
> (`Directory.Packages.props:7`).
>
> **Reviewed:** `EtherIntelligenceButton` (`: Button`) —
> [EtherIntelligenceButton.cs](../../../src/Ether.DesignSystem.Controls/Controls/Inputs/EtherIntelligenceButton.cs),
> [EtherIntelligenceButton.xaml](../../../src/Ether.DesignSystem.Controls/Controls/Inputs/EtherIntelligenceButton.xaml).
> **Package:** `Ether.DesignSystem.Controls`. **Date:** 2026-09-03 (refreshed for the new
> `LeftIcon`/`RightIcon` API, commit `2a682be`; base parity per 2026-09-01 R3).

## Verdict at a glance
- **Base-control parity (B):** the confirmed base is `Button` (`EtherIntelligenceButton.cs:43`), and
  the stock content, Click, command, CommonStates, and FocusStates surface remains inherited.
- **Design coverage (B) — CHANGED this build:** the control **now exposes an icon API mirroring
  `EtherButton`**: two strongly-typed `LeftIcon`/`RightIcon` `IconElement` DPs
  (`EtherIntelligenceButton.cs:98-125`) with `LeftIconStates`/`RightIconStates` visual-state groups
  (`cs:39-42`; template `EtherIntelligenceButton.xaml:163-186,234-267`). `LeftIcon` **defaults to the
  sparkles glyph** (an `ImageIcon` seeded in the constructor, `cs:94,135-140`) and can be replaced
  with any `IconElement` or cleared with `LeftIcon="{x:Null}"`; `RightIcon` defaults to `null`. The
  single intelligence skin is still template-owned; the class still adds **no** `Variant`/`Size` and
  stays `sealed` (`cs:16-20,43`).
- **Consumability (A):** Click routes through `ObserveButton` and is exercised
  (`RuntimeVerification.PropertyConsumption.cs:375`); inherited `Command`/`CommandParameter` executes
  through UIA Invoke and the Invoke pattern is asserted (`RuntimeVerification.R2.cs`). The two new
  `LeftIcon`/`RightIcon` DPs pass R1 (registered DPs, `cs:98-125`;
  `PublicAPI.Unshipped.txt:203-204`) and are exercised **generically** by the reflection fixtures
  (getter/setter round-trip in `RuntimeVerification.FullPropertyCode.cs`; attached mutation to a
  `SymbolIcon` in `RuntimeVerification.AttachedVisualProperties.cs:665`, matched by name). They are
  **now also proven the way `EtherButton`'s are** — the Intelligence-specific fixture
  (`RuntimeVerification.IntelligenceButton.cs`) probes the `LeftIcon`/`RightIcon` slots and toggles
  the `LeftIconStates`/`RightIconStates` transitions (default sparkles `LeftIcon` shows the leading
  slot; clearing collapses it; setting `RightIcon` shows the trailing slot; clearing collapses it; the
  default sparkles icon is restored afterward).

**Overall: consumer-ready surface, harness caught up.** Review B passes — the trailing-icon
gap the prior report recorded as 🚫 is now a shipped `RightIcon` DP. Review A's **two harness residuals
are now CLOSED** (both were R2/harness, not product): (1) component-specific proof of the
`LeftIcon`/`RightIcon` slots/states now exists (parallel to `EtherButton`'s,
`RuntimeVerification.IntelligenceButton.cs`); (2) the acceptance constants in
`scripts/Verify-ConsumerFixtures.ps1` were reconciled for these two new public DPs to 1770/1401/557/346,
so the reflection inventory/classification/visual gates pass. Confirmed by a green run
(`outcome:"success"`, `artifacts/consumer-fixtures/runtime-result-101ef2918b2143728697bbf6cd4c90c5.json`,
2026-09-03). The row moves from PARTIAL to ✅ PASS.

---

## Review B — Completeness / Parity matrix

| Property / Event | Source | Expected (consumer) | Present? | Bindable/Observable? | Verdict |
| --- | --- | --- | --- | --- | --- |
| `Content` / `ContentTemplate` / selector | base | set text or rich button content | inherited; single-line string path + explicit `TextContent` ellipsis mirror `EtherButton` `EtherIntelligenceButton.xaml:241-260`; `cs:73-92,145-151` | ✔ DP | ✅ |
| `LeftIcon` (`IconElement`) | design/both | set/replace/clear the leading icon; defaults to sparkles | ✔ new DP `cs:98-110`; default sparkles `ImageIcon` `cs:94,135-140`; slot `EtherIntelligenceButton.xaml:234-240`; `LeftIconStates` `xaml:163-174` | ✔ DP + `OnIconChanged` callback `cs:142-143` | ✅ **new this build** |
| `RightIcon` (`IconElement`) | design/both | add an optional trailing icon | ✔ new DP `cs:113-125`; default `null`; slot `EtherIntelligenceButton.xaml:261-267`; `RightIconStates` `xaml:175-186` | ✔ DP + `OnIconChanged` callback `cs:142-143` | ✅ **new this build** |
| `Command` / `CommandParameter` | base | invoke an MVVM command | inherited from `Button`; proven to execute once with the expected parameter via UIA Invoke `RuntimeVerification.R2.cs` | ✔ DP | ✅ |
| `Click` | base | listen to activation → backend | inherited; `ObserveButton` accepts it `ControlInteractionAdapter.cs:21-28`; exercised `PropertyConsumption.cs:375` | event + adapter | ✅ |
| intelligence visual variant | design | receive the gradient border/glow skin | implicit default style/template `EtherIntelligenceButton.xaml:282-299` | style | ✅ |
| `Variant` / `Size` | design | pick a variant/size | ✘ intentionally absent — single design, class `sealed` `cs:16-20,43` | n/a | 🚫 use `EtherButton` for variants/sizes `EtherButton.cs:9-19` |
| `IsEnabled` | base | disable activation and dim effects | inherited; disabled behavior asserted `RuntimeVerification.IntelligenceButton.cs:96-109` | ✔ DP | ✅ |
| locked chrome (`Background`, `Border*`, `CornerRadius`, `Font*`, `Foreground`) | base | override intelligence chrome | inherited but deliberately not template-consumed `scripts/UnsupportedProperties.psd1:302-313` | writable DPs; no Ether visual effect | 🚫 use the fixed intelligence skin `getting-started.md:597-610` |
| content alignment | base | position supplied content | inherited and template-bound `EtherIntelligenceButton.xaml:238-240,265-267`; registry marks `consumed-visually-stable` `UnsupportedProperties.psd1:314-315` | ✔ DP | ✅ |

**Notes / gaps (B):**
- **The trailing-icon exclusion is retired.** The prior report's row "variants / trailing icon /
  animation → 🚫 use EtherButton for variants/icons" is **superseded**: `RightIcon` (and a
  replaceable `LeftIcon`) now exist as first-class `IconElement` DPs, exactly mirroring `EtherButton`
  (`cs:9-14,98-125`). Only `Variant`/`Size` remain deliberately excluded.
- **`UnsupportedProperties.psd1` needs no new entry for the icons.** Its verifier only validates the
  listed entries against templates (`Verify-UnsupportedProperties.ps1:76-101`) — it does not
  enumerate all public DPs — and `LeftIcon`/`RightIcon` are template-bound
  (`EtherIntelligenceButton.xaml:235,262`), so they correctly stay off the unsupported list. No
  contradiction to reconcile there.
- **`getting-started.md` §5 is now understated, not wrong.** The IntelligenceButton sample
  (`getting-started.md:597-610`) shows `Content`/`Command` only and does not mention the new
  `LeftIcon`/`RightIcon` slots or the default sparkles glyph. Enriching it is a doc follow-up, not a
  failing contract.

---

## Review A — Consumability report

```
Component: EtherIntelligenceButton
Base control: Button   | Package: Ether.DesignSystem.Controls
Declared DPs: 2 public (LeftIcon, RightIcon) + 1 internal (UsesTextContentPath)
              | inherited public surface: full Button

Rubric (declared DPs LeftIcon/RightIcon; inherited button surface):
  R1 DependencyProperty ....... PASS  (public static readonly LeftIconProperty/RightIconProperty +
                                       CLR wrappers, cs:98-125; registered in PublicAPI.Unshipped.txt:203-204)
  R2 Round-trips .............. PASS  (FullPropertyCode getter/setter round-trip on a detached
                                       instance, RuntimeVerification.FullPropertyCode.cs:39-73; attached
                                       SymbolIcon mutation round-trips through GetValue,
                                       AttachedVisualProperties.cs:468-471,665) — AND now via a
                                       component-specific IntelligenceButton slot/state assertion
                                       (RuntimeVerification.IntelligenceButton.cs)
  R3 Observable ............... PASS  (metadata has OnIconChanged -> UpdateIconStates, cs:142-143; the
                                       IntelligenceButton fixture now toggles LeftIcon/RightIcon and asserts
                                       the LeftIconStates/RightIconStates slot Visibility transitions
                                       (leading slot shows on default sparkles LeftIcon, collapses when
                                       cleared; trailing slot shows when RightIcon is set, collapses when
                                       cleared) — the equivalent of EtherButton's proof, Button.cs:49-91)
  R4 TwoWay ................... N/A   (icon slots are presentation, not user-edited value state)
  R5 Documented ............... PASS  (XML docs on both properties + ...Property fields state defaults:
                                       LeftIcon default null seeded to sparkles, RightIcon default null,
                                       cs:97,105,112,120)
  R1-R5 inherited button surface . PASS-by-inheritance for R1; Click/Command proven (below); other inherited
                                       DP round-trips remain informally UNVERIFIED (same status as every
                                       base-inheriting control in this review)

Contracts:
  C1 ItemsSource / selection .. N/A   (not a collection control)
  C2 Listenable + adapter ..... PASS  (inherited Click + ObserveButton, ControlInteractionAdapter.cs:21-28;
                                       exercised PropertyConsumption.cs:375)
  C3 Command .................. PASS  (Command/CommandParameter invoked through UIA IInvokeProvider,
                                       RuntimeVerification.R2.cs; commands.Controls includes
                                       "EtherIntelligenceButton")
  C4 Automation peer/pattern .. PASS  (PatternInterface.Invoke/IInvokeProvider asserted,
                                       automationPatterns.Patterns includes "EtherIntelligenceButton.Invoke")

Harness: covered? YES (CLOSED). In PublicPropertyInventoryTypes (PublicPropertyInventory.cs:38), so the
         two new DPs ARE enumerated by the reflection inventory/classification/visual gates — and the
         hardcoded acceptance constants in scripts/Verify-ConsumerFixtures.ps1 are now reconciled to
         1770/1401/557/346 (the 2 new visual icon DPs land in the observable bucket; see Follow-up 2).
         RuntimeVerification.IntelligenceButton.cs asserts style/defaults, CommonStates, disabled opacity,
         Light/Dark brushes, the Invoke automation name, AND now the LeftIcon/RightIcon slots +
         LeftIconStates/RightIconStates transitions (default sparkles leading slot shows, collapses when
         cleared; trailing slot shows when RightIcon is set, collapses when cleared; default restored),
         matching EtherButton's RuntimeVerification.Button.cs:49-91. (Verify-EtherIntelligenceButtonContract.ps1
         still does not assert the LeftIconPart/RightIconPart TemplateParts or the icon state groups
         (script lines 15-25,56-61) — an optional completeness follow-up; the ConsumerFixtures fixture
         already proves them.)
x64 build: green; the icon DPs compile and the PublicAPI analyzer passes (PublicAPI.Unshipped.txt:203-204).
           A Verify-ConsumerFixtures.ps1 run now reaches outcome:"success" with the reconciled constants —
           evidence artifacts/consumer-fixtures/runtime-result-101ef2918b2143728697bbf6cd4c90c5.json (2026-09-03).
```

---

## Follow-ups (concrete, actionable)

1. **[R2, R3, harness] Prove the `LeftIcon`/`RightIcon` slots + states in the component fixture,
   mirroring `EtherButton` — DONE (CLOSED).**
   `tests/Ether.DesignSystem.ConsumerFixtures/RuntimeVerification.IntelligenceButton.cs` now resolves
   the `LeftIcon`/`RightIcon` slots, asserts the default sparkles `LeftIcon` renders the leading slot
   `Visible` and `RightIcon==null` collapses the trailing slot, sets `RightIcon` and asserts it shows,
   clears `LeftIcon` and asserts it collapses, and restores the default sparkles icon afterward —
   parallel to `RuntimeVerification.Button.cs:49-91`. The `IntelligenceButtonVerification` record
   carries the `LeftIconStates`/`RightIconStates` slot fields, asserted in the `intelligenceButton`
   marker block of `scripts/Verify-ConsumerFixtures.ps1`. Confirmed by the green run
   (`artifacts/consumer-fixtures/runtime-result-101ef2918b2143728697bbf6cd4c90c5.json`, 2026-09-03).
   [closed the R2/R3 residual specific to these DPs]
2. **[harness, stale constants] Reconcile the reflection acceptance constants for the two new public
   DPs — DONE (CLOSED).** Commit `2a682be` added `LeftIcon`/`RightIcon` to the public surface; the
   reflection acceptance constants in `scripts/Verify-ConsumerFixtures.ps1` have since been reconciled:
   `$expectedInventoryPublicProperties` 1768→1770, `$expectedWritablePublicProperties` 1399→1401,
   `$expectedVisualPublicProperties` 555→557, with the observable/contract-only split re-derived from
   the deterministic run to 346/211 (both new `Visibility`-toggling icon DPs landed in the observable
   bucket, as expected). `$expectedSemanticPublicProperties` (87) and `$expectedPlatformPublicProperties`
   (1126) were unchanged. The gates now pass — the green run reports 1770 public properties (1401
   writable), 557 visual (346 observable + 211 contract-only)
   (`runtime-result-101ef2918b2143728697bbf6cd4c90c5.json`, 2026-09-03).
3. **[docs] Enrich the consumer guide.** Add a `LeftIcon`/`RightIcon` example (and note the default
   sparkles glyph + `LeftIcon="{x:Null}"` clear) to the IntelligenceButton section of
   `design library handoff/getting-started.md:597-610`, matching the `EtherButton` icon example at
   `getting-started.md:174-179`.
4. **[harness, optional] Extend `Verify-EtherIntelligenceButtonContract.ps1`** to assert the new
   `LeftIconPart`/`RightIconPart` `TemplatePart`s and the `LeftIconStates`/`RightIconStates`
   `TemplateVisualState`s (script `templateParts` list at lines 15-25 and the state loops at
   lines 56-61), so the static contract gate covers the icon surface too.
