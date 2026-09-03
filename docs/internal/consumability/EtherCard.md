# Consumability review — Card

> Reviewed against
> [the review spec](../2026-09-01-component-consumability-review-spec.md). Findings are grounded in
> source (file:line); the official WinUI baseline is Windows App SDK 2.3.1
> (`Directory.Packages.props:7`).
>
> **Reviewed:** the six keyed Ether Card `Style` resources for stock `Border` —
> [EtherCard.xaml](../../../src/Ether.DesignSystem.Controls/Resources/Foundations/EtherCard.xaml).
> **Package:** `Ether.DesignSystem.Controls`. **Date:** 2026-09-03 (re-cited after the Figma reskin,
> commit `af40e34`; base verdict per 2026-09-01 R3).

## Verdict at a glance
- **Base-control parity (B):** `EtherCard` is not a consumable control type. It is six keyed styles,
  all targeting stock `Border`: Normal shell/body, Intelligence shell/body, and Callout shell/body
  (`EtherCard.xaml:80-128`).
- **Design coverage (B) — reskinned this build:** the `af40e34` reskin kept the same six keys but
  moved to **outer-fill borders** (the shell paints the border colour/gradient and a 1–2px `Padding`
  lets it show around the inner body, `EtherCard.xaml:80-86,98-104,113-121`) and made the **Callout
  gradient theme-aware** (distinct Light/Dark/HighContrast `EtherCardCalloutOuterGradient`,
  `EtherCard.xaml:32-39,51-58,69-73`). The source still notes Callout is a composed structure rather
  than one Style (`EtherCard.xaml:1-16`) and includes the composition example (`EtherCard.xaml:130-164`).
- **Consumability (A):** stock Border content/layout/accessibility semantics remain available, and
  the consumer guide demonstrates nested styles (`getting-started.md:727-730`). Card has a dedicated
  style-resource fixture: it resolves all six keys as `Border`-targeted `Style`s, instantiates them,
  and proves Light/Dark brush re-resolution plus a High-Contrast body-brush re-resolution
  (`RuntimeVerification.StyleResources.cs:13-30,44-83,91-126`). **The reskin did not invalidate that
  fixture** — its `CardStyleKeys` array still matches the six live keys exactly, its
  `TargetType==Border` assertion still holds, and the Light≠Dark brush check still passes because the
  reskin made the theme values *more* distinct, not less.

**Overall: consumer-ready.** Review B passes; Review A's one follow-up is closed by the harness
(`ETHER_CONSUMER_SMOKE` `outcome:"success"`). The reskin is a value/skin change only — no key added,
removed, or retargeted — so no new consumability work is required.

---

## Review B — Completeness / Parity matrix

| Property / Event | Source | Expected (consumer) | Present? | Bindable/Observable? | Verdict |
| --- | --- | --- | --- | --- | --- |
| consumable control type | base | determine whether `EtherCard` can be instantiated | ✘; resource file declares styles only `EtherCard.xaml:44-46,80-128` | n/a | 🚫 instantiate `Border`, not `EtherCard` `getting-started.md:727-730` |
| Normal shell/body | design | compose a standard raised card surface | `EtherCardNormal` + `EtherCardNormalBody` `EtherCard.xaml:80-93`; resolution fixture-proven `RuntimeVerification.StyleResources.cs:28-30` | keyed styles | ✅ |
| Intelligence shell/body | design | compose the AI-accent gradient-border card | `EtherCardIntelligence` + `EtherCardIntelligenceBody` `EtherCard.xaml:98-111`; resolution fixture-proven `RuntimeVerification.StyleResources.cs:28-30` | keyed styles | ✅ |
| Callout shell/body | design | compose the themed iridescent callout with header + content body | `EtherCardCalloutShell` + `EtherCardCalloutBody` `EtherCard.xaml:113-128`; example `EtherCard.xaml:130-164`; resolution fixture-proven `RuntimeVerification.StyleResources.cs:28-30` | keyed styles + consumer composition | ✅ |
| card content | base | host arbitrary UI/content | supplied by the styled Border's `Child`; usage nests content in Borders `EtherCard.xaml:1-16` | stock Border property | ✅ |
| size / padding / corner / border / background | design | obtain the prescribed three card treatments (now outer-fill borders) | six styles set their treatment-specific values `EtherCard.xaml:80-128` | style setters; consumer can locally override | ✅ |
| Light / Dark / High Contrast | design | resolve appropriate brushes in all themes | three theme dictionaries incl. the newly theme-aware callout gradient `EtherCard.xaml:21-75`; fixture-proven: Light/Dark brushes differ per style and High Contrast resolves a `SolidColorBrush` body brush `RuntimeVerification.StyleResources.cs:65-75,118` | theme resources | ✅ |
| resource availability | design | resolve styles after merging Controls | Card dictionary merged by Controls `Generic.xaml:25`; resolution fixture-proven `RuntimeVerification.StyleResources.cs:28-30` | resource lookup | ✅ |
| interaction / selection / command | design | determine whether the card is actionable | no interaction contract; styles target decorative `Border` `EtherCard.xaml:80-128` | n/a | 🚫 place an appropriate Button/selector inside or around the card |

**Notes / gaps (B):**
- **The answer to the type-or-Style question is still Style only.** There is no `x:Class` or control
  declaration in `EtherCard.xaml:44-46`; every public card artifact is a keyed `Style` with
  `TargetType="Border"` (`EtherCard.xaml:80-128`).
- **The reskin is a skin/value change, not a surface change.** All six keys
  (`EtherCardNormal`/`EtherCardNormalBody`/`EtherCardIntelligence`/`EtherCardIntelligenceBody`/
  `EtherCardCalloutShell`/`EtherCardCalloutBody`) are unchanged in name and `TargetType`; only their
  colours/gradients/radii/padding moved (`af40e34`). The fixture's `CardStyleKeys`
  (`RuntimeVerification.StyleResources.cs:13-21`) therefore still enumerates exactly the live set.
- **Callout is intentionally composite.** The source states one Style cannot represent the visible
  header plus inner body (`EtherCard.xaml:1-16`) and provides a copyable composition
  (`EtherCard.xaml:130-164`); this is not a missing card property.
- **The consumer-contract registry still has no Card entry**, and the type-only public inventory
  lists control types only (`RuntimeVerification.PublicPropertyInventory.cs:32-49`) — expected, since
  Card has no Ether-owned type to register. The dedicated style-resource fixture is Card's executable
  consumer proof instead of a type-inventory entry.

---

## Review A — Consumability report

```
Component: EtherCard keyed Styles (no Ether control type)
Base control: Border   | Package: Ether.DesignSystem.Controls
Declared DPs: 0  | inherited public surface: stock Border

Rubric (styled stock surface):
  R1 DependencyProperty ....... N/A   (no Ether-owned type/DP; all keys target Border,
                                       EtherCard.xaml:80-128)
  R2 Round-trips .............. N/A   (no Ether wrapper/value contract; styles set stock Border
                                       properties only, EtherCard.xaml:80-128)
  R3 Observable ............... N/A   (no Ether-owned state/event; decorative Border styles)
  R4 TwoWay ................... N/A   (no user-editable value property)
  R5 Documented ............... N/A   (no Ether-owned DP/identifier pair; usage and composition
                                       are documented at EtherCard.xaml:1-16,130-164)

Contracts:
  C1 ItemsSource / selection .. N/A   (not a collection control)
  C2 Listenable + adapter ..... N/A   (decorative Border styles have no control event contract)
  C3 Command .................. N/A   (cards are not actions; use an interactive control)
  C4 Automation peer/pattern .. N/A   (no card interaction role; semantics come from child content)

Harness: covered? YES (style-resource fixture). All six keys resolve as Border-targeted Styles, all
         three documented compositions instantiate (including the Callout composition,
         EtherCard.xaml:130-164), consumer child content is proven to survive layout, and
         Light/Dark/High Contrast brush re-resolution is asserted
         (RuntimeVerification.StyleResources.cs:13-30,44-83,91-126). The af40e34 reskin left every
         asserted key/target/theme-distinctness fact true. Card is absent from the type-only public
         inventory (PublicPropertyInventory.cs:32-49) by design. Runtime marker ETHER_CONSUMER_SMOKE
         outcome:"success" includes all six keys in styleResources.CardStyleKeys with distinct
         LightBrushes/DarkBrushes and HighContrastBrushesResolved:true.
x64 build: green (Controls + Interactions + ConsumerFixtures, per the R2.11 runtime run recorded in
           docs/internal/consumability/_RUN-ON-DESKTOP.md). NOTE: that recorded run predates today's
           reskin commits; a fresh Verify-ConsumerFixtures.ps1 run is advisable to re-attest the Card
           style-resource fields against the reskinned brushes (no Card-specific regression is
           expected, but the run has not been repeated post-reskin).
```

---

## Follow-ups (concrete, actionable)

1. ~~**[harness] Add a style-resource consumer fixture.**~~ DONE — `RuntimeVerification.StyleResources.cs`
   resolves all six keys from `EtherCard.xaml:80-128`, asserts each targets `Border`, instantiates the
   documented compositions, verifies consumer child content survives, and asserts Light/Dark/High
   Contrast brush re-resolution from `EtherCard.xaml:21-75`
   (`StyleResources.cs:13-30,44-83,91-126`).
2. **[harness, re-attest only] Re-run `Verify-ConsumerFixtures.ps1` post-reskin.** The reskin
   (`af40e34`) did not change keys or targets, so no fixture edit is needed, but the recorded green run
   predates it — a fresh run should confirm the Card `styleResources` fields still hold against the new
   themed callout gradient (and, per the Tooltip/Panel Tabs reports, other today-commits also need a
   fresh run). No Card-specific regression is expected.
