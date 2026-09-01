# Consumability review — Card

> Reviewed against
> [the review spec](../2026-09-01-component-consumability-review-spec.md). Findings are grounded in
> source (file:line); the official WinUI baseline is Windows App SDK 2.3.1
> (`Directory.Packages.props:7`).
>
> **Reviewed:** the six keyed Ether Card `Style` resources for stock `Border` —
> [EtherCard.xaml](../../../src/Ether.DesignSystem.Controls/Resources/Foundations/EtherCard.xaml).
> **Package:** `Ether.DesignSystem.Controls`. **Date:** 2026-09-01 (post-remediation, R3).

## Verdict at a glance
- **Base-control parity (B):** `EtherCard` is not a consumable control type. It is six keyed styles,
  all targeting stock `Border`: Normal shell/body, Intelligence shell/body, and Callout shell/body
  (`EtherCard.xaml:95-149`).
- **Design coverage (B):** Normal, Intelligence, and composite Callout treatments are supplied;
  the source explicitly says Callout is a composed structure rather than one Style
  (`EtherCard.xaml:5-19`), and includes the composition example (`EtherCard.xaml:151-185`).
- **Consumability (A):** stock Border content/layout/accessibility semantics remain available, and
  the consumer guide accurately demonstrates nested styles (`getting-started.md:425-434`). Card now
  has a dedicated style-resource fixture: it resolves all six keys as `Border`-targeted `Style`s,
  instantiates them, and proves Light/Dark brush re-resolution plus a High-Contrast body-brush
  re-resolution (`RuntimeVerification.StyleResources.cs:13-30,44-83,91-126`).

**Overall: consumer-ready.** Review B passes; Review A's one follow-up is closed by the harness (`ETHER_CONSUMER_SMOKE` `outcome:"success"`).

---

## Review B — Completeness / Parity matrix

| Property / Event | Source | Expected (consumer) | Present? | Bindable/Observable? | Verdict |
| --- | --- | --- | --- | --- | --- |
| consumable control type | base | determine whether `EtherCard` can be instantiated | ✘; resource file declares styles only `EtherCard.xaml:49-51,95-149` | n/a | 🚫 instantiate `Border`, not `EtherCard` `getting-started.md:425-434` |
| Normal shell/body | design | compose a standard raised card surface | `EtherCardNormal` + `EtherCardNormalBody` `EtherCard.xaml:95-110`; resolution fixture-proven `RuntimeVerification.StyleResources.cs:28-30` | keyed styles | ✅ |
| Intelligence shell/body | design | compose the dark AI-accent card | `EtherCardIntelligence` + `EtherCardIntelligenceBody` `EtherCard.xaml:115-132`; resolution fixture-proven `RuntimeVerification.StyleResources.cs:28-30` | keyed styles | ✅ |
| Callout shell/body | design | compose gradient callout with header and content body | `EtherCardCalloutShell` + `EtherCardCalloutBody` `EtherCard.xaml:134-149`; example `cs:151-185`; resolution fixture-proven `RuntimeVerification.StyleResources.cs:28-30` | keyed styles + consumer composition | ✅ |
| card content | base | host arbitrary UI/content | supplied by the styled Border's `Child`; usage nests content in Borders `EtherCard.xaml:5-19` | stock Border property | ✅ |
| size / padding / corner / border / background | design | obtain the prescribed three card treatments | six styles set their treatment-specific values `EtherCard.xaml:95-149` | style setters; consumer can locally override | ✅ |
| Light / Dark / High Contrast | design | resolve appropriate brushes in all themes | three theme dictionaries `EtherCard.xaml:53-90`; now fixture-proven: Light/Dark brushes differ per style and High Contrast resolves a `SolidColorBrush` body brush `RuntimeVerification.StyleResources.cs:65-75,118` | theme resources | ✅ |
| resource availability | design | resolve styles after merging Controls | Card dictionary is merged by Controls Generic.xaml `Generic.xaml:7-25`; resolution fixture-proven `RuntimeVerification.StyleResources.cs:28-30` | resource lookup | ✅ |
| interaction / selection / command | design | determine whether card itself is actionable | no interaction contract; styles target decorative `Border` `EtherCard.xaml:95-149` | n/a | 🚫 place an appropriate Button/selector inside or around the card |

**Notes / gaps (B):**
- **The answer to the type-or-Style question is Style only.** There is no `x:Class` or control
  declaration in `EtherCard.xaml:49-51`; every public card artifact is a keyed `Style` with
  `TargetType="Border"` (`EtherCard.xaml:95-149`). The consumer guide says the same
  (`getting-started.md:425-434`).
- **Callout is intentionally composite.** The source states that one Style cannot represent the
  visible header plus inner body (`EtherCard.xaml:16-19`) and provides a copyable composition
  (`EtherCard.xaml:151-185`); this is not a missing card property.
- **The consumer-contract registry still has no Card entry**, and the type-only public inventory
  still lists control types only (`RuntimeVerification.PublicPropertyInventory.cs:32-48`) — that is
  expected, since Card has no Ether-owned type to register. What closed the prior gap is the new
  dedicated style-resource fixture, which is Card's executable consumer proof instead of a
  type-inventory entry.

---

## Review A — Consumability report

```
Component: EtherCard keyed Styles (no Ether control type)
Base control: Border   | Package: Ether.DesignSystem.Controls
Declared DPs: 0  | inherited public surface: stock Border

Rubric (styled stock surface):
  R1 DependencyProperty ....... N/A   (no Ether-owned type/DP; all keys target Border,
                                       EtherCard.xaml:95-149)
  R2 Round-trips .............. N/A   (no Ether wrapper/value contract; styles set stock Border
                                       properties only, EtherCard.xaml:95-149)
  R3 Observable ............... N/A   (no Ether-owned state/event; decorative Border styles)
  R4 TwoWay ................... N/A   (no user-editable value property)
  R5 Documented ............... N/A   (no Ether-owned DP/identifier pair; usage and composition
                                       are documented at EtherCard.xaml:5-19,151-185)

Contracts:
  C1 ItemsSource / selection .. N/A   (not a collection control)
  C2 Listenable + adapter ..... N/A   (decorative Border styles have no control event contract)
  C3 Command .................. N/A   (cards are not actions; use an interactive control)
  C4 Automation peer/pattern .. N/A   (no card interaction role; semantics come from child content)

Harness: covered? YES (style-resource fixture). All six keys resolve as Border-targeted Styles,
         all three documented compositions instantiate (including the Callout composition,
         cs:151-185), consumer child content is proven to survive layout, and Light/Dark/High
         Contrast brush re-resolution is asserted
         (RuntimeVerification.StyleResources.cs:13-30,44-83,91-126). Card is absent from the
         type-only public inventory (PublicPropertyInventory.cs:32-48) by design — there is no
         Ether type to add. Runtime marker ETHER_CONSUMER_SMOKE outcome:"success" includes all six
         keys in styleResources.CardStyleKeys with distinct LightBrushes/DarkBrushes and
         HighContrastBrushesResolved:true
         (artifacts/audit-runs/consumer-runtime-evidence-20260901-161450498/runtime-result.json).
x64 build: green (Controls + Interactions + ConsumerFixtures, per the R2.11 runtime run recorded
           in docs/internal/consumability/_RUN-ON-DESKTOP.md).
```

---

## Follow-ups (concrete, actionable)

1. ~~**[harness] Add a style-resource consumer fixture.**~~ DONE — `RuntimeVerification.StyleResources.cs`
   resolves all six keys from `EtherCard.xaml:95-149`, asserts each targets `Border`, instantiates
   all three documented compositions, verifies consumer child content survives, and asserts
   Light/Dark/High Contrast brush re-resolution from `EtherCard.xaml:53-90`
   (`StyleResources.cs:13-30,44-83,91-126`); the style-aware inventory is this fixture, not a
   nonexistent `EtherCard` type.

No remaining follow-ups.
