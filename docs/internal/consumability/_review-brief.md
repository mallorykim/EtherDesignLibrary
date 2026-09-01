# Codex audit brief — Component Consumability & API-Completeness reviews

You (Codex) are running the **audit only**. Produce one Markdown report per component. You are
**supervised**: another agent wrote the spec and the gold-standard sample and will review your output.

## Authoritative inputs — read these first, in order
1. **The spec** — `docs/internal/2026-09-01-component-consumability-review-spec.md`. This defines the
   two reviews (A = API Consumability, B = API Completeness/Parity), the Consumability Rubric
   (R1–R5), the per-control contracts (C1–C4), the matrix columns, and the pass/fail gates. Follow it
   exactly.
2. **The gold-standard sample** — `docs/internal/consumability/EtherTabNavigation.md`. Your output for
   every component MUST match this file's structure and section order **exactly**:
   `# Consumability review — <Name>`, `> Reviewed/Package/Date` blockquote, `## Verdict at a glance`,
   `## Review B — Completeness / Parity matrix` (the table + a "Notes / gaps (B)" list),
   `## Review A — Consumability report` (the fenced ``` block in the sample's format), and
   `## Follow-ups`. Same headings, same table columns, same verdict emojis (✅ / ⚠️ / ❌ / 🚫).

## Hard rules
- **Audit + report only.** You may READ any repo file and WRITE only new `.md` files under
  `docs/internal/consumability/`. **Do NOT** edit any `.cs`/`.xaml` product code, the
  `ConsumerFixtures` harness, or the `ControlInteractionAdapter`. **Do NOT build or run anything.**
- **Ground every claim in source** with `file:line` (relative paths, clickable). No claim without
  evidence. If you cannot verify something, say "unverified (no fixture)" — never assert it as PASS.
- **Confirm the base control from the actual `class X : Y` declaration**, not from any guess in this
  brief. The "expected" (parity) column is the base WinUI control's real public property/event set —
  reference the official sources named in AGENTS.md (WinUI-Gallery, microsoft-ui-xaml) at the SDK
  version in the csproj. If you state a base-control property exists, it must actually exist on that
  base type.
- **Be honest about the harness.** For each component check whether it appears in
  `tests/Ether.DesignSystem.ConsumerFixtures/RuntimeVerification.PublicPropertyInventory.cs`
  (`PublicPropertyInventoryTypes`) and `…PropertyConsumption.cs` (`DeclaredPropertyOwnerTypes` +
  the standard-interaction list). Absent ⇒ record it as a harness gap in the report.
- **Verdict discipline:** a component is "consumer-ready" only if it passes BOTH A and B per the
  spec's gates. Most will have follow-ups — that is expected and correct; do not inflate to PASS.
- Each **Follow-up** must be concrete and actionable (name the file, the method, the exact change),
  and tagged with the rubric/contract it closes (e.g. `[C2, backend]`, `[harness]`, `[B decision]`).
  Follow-ups that require code go to Codex later — you only WRITE them down here, you do not do them.

## Component → type → source map (confirm paths via the repo; roots given to save time)
Controls source root: `src/Ether.DesignSystem.Controls/Controls/`. Gallery usage (design intent):
`samples/Ether.DesignSystem.Gallery/Views/`. Harness: `tests/Ether.DesignSystem.ConsumerFixtures/`.
Adapter: `src/Ether.DesignSystem.Interactions/ControlInteractionAdapter.cs`.

| Report file (output) | Primary type(s) | Source (under Controls/ unless noted) | Gallery page |
| --- | --- | --- | --- |
| `EtherButton.md` | `EtherButton` | `Inputs/EtherButton.cs`,`.xaml` | `Controls/ButtonPage.*` |
| `EtherCheckbox.md` | `EtherCheckbox` | `Inputs/EtherCheckbox.cs`,`.xaml` | `Controls/CheckboxPage.*` |
| `EtherDropdown.md` | `EtherDropdown` | `Inputs/EtherDropdown.cs`,`.xaml` | `Controls/DropdownPage.*` |
| `EtherInput.md` | `EtherInput` | `Inputs/EtherInput.cs`,`.xaml` | `Controls/InputPage.*` |
| `EtherIntelligenceButton.md` | `EtherIntelligenceButton` | `Inputs/EtherIntelligenceButton.cs`,`.xaml` | `Controls/IntelligenceButtonPage.*` |
| `EtherRadioButton.md` | `EtherRadioButton` | `Inputs/EtherRadioButton.cs`,`.xaml` | `Controls/RadioButtonPage.*` |
| `EtherScrollBar.md` | `EtherScrollBar` | `Inputs/EtherScrollBar.xaml`,`.xaml.cs` | `Foundations/ScrollBarPage.*` |
| `EtherSegmentedControl.md` | `EtherSegmentedControl` (+`EtherSegmentPanel`,`EtherSegmentRadioButton`,`SegmentedSelectionChangedEventArgs`) | `Inputs/EtherSegmentedControl.cs`,`.xaml`,`Inputs/EtherSegment*.cs` | `Controls/SegmentedControlPage.*` |
| `EtherSlider.md` | `EtherSlider` | `Inputs/EtherSlider.xaml`,`.xaml.cs`,`.Automation.cs`,`.Labels.cs` | `Controls/SliderPage.*` |
| `EtherSteeringBar.md` | `EtherSteeringBar` | `Inputs/EtherSteeringBar.xaml`,`.xaml.cs` | `Controls/SteeringBarPage.*` |
| `EtherSwitch.md` | `EtherSwitch` (Toggle Switch) | `Inputs/EtherSwitch.xaml`,`.xaml.cs` | `Controls/ToggleSwitchPage.*` |
| `EtherCard.md` | `EtherCard` | `Resources/Foundations/EtherCard.xaml` (**XAML-only — first determine whether "Card" is a consumable control TYPE or only an applied Style; that answer shapes the whole report**) | `Surfaces/CardPage.*` |
| `EtherMasthead.md` | `EtherMasthead` | `Navigation/EtherMasthead.xaml`,`.xaml.cs` | `Navigation/MastheadPage.*` |
| `EtherProgressBar.md` | `EtherProgressBar` | `Inputs/EtherProgressBar.cs`,`.xaml` | `DataDisplay/ProgressBarPage.*` |

## Component-specific questions you MUST answer in the report
- **EtherSlider**: does it derive from `Slider`/`RangeBase` (then `Minimum`/`Maximum`/`StepFrequency`/
  `SmallChange`/`LargeChange`/`Value` are inherited — verify parity) **or** is it a `UserControl` that
  must re-declare them (then any missing range API is a ❌ completeness hole)? State the lineage from
  the `class`/`x:Class` declaration and resolve every range knob accordingly.
- **EtherCard**: is it a `Control`/`ContentControl` subclass a consumer can instantiate, or a keyed
  `Style`/resource applied to a stock control? If it is only a Style, the "consumable API" is the set
  of properties it styles + how a consumer references it — say so explicitly.
- **EtherSegmentedControl**: it has a **custom** `SelectionChanged` event with
  `SegmentedSelectionChangedEventArgs` — verify the args are public and carry a consumable selected
  value; check `ItemsSource`/data story vs. inline segments.
- **EtherSteeringBar**: custom `ValueChanged` event — verify args + the range API parity.
- **EtherMasthead**: many `Show*` toggles (`ShowSettings`/`ShowSearch`/`ShowMenuIcon`/`ShowChevron`/
  `EnableWindowCommands`) — confirm each is a bindable DP (R1) and note its adapter/event story.

## Reconcile with the existing consumer contract (added after Wave 1)
The repo already ships a consumer-facing contract. For **every** component, cross-check against it:
- `scripts/UnsupportedProperties.psd1` — the registry of inherited properties declared unsupported /
  needs-review. A property listed there should appear as 🚫 (with this file cited) or ⚠️ if it is
  `needs-review`. **If a property is implemented in code but still listed unsupported/needs-review
  there (or vice-versa), that is a stale-contract follow-up** — flag it (as with `EtherDropdown`
  `PlaceholderText`).
- `docs/consumers/getting-started.md` — the consumer guide. If it names an external alternative for
  an excluded property, cite it in the 🚫 row's reason.
- `docs/handoff/2026-08-29-winui3-full-property-audit-handoff.md` — the prior full-property audit;
  use it for context, but re-verify against live source (do not trust it blindly).

## Output on completion
After writing the per-component files for your assigned wave, print a one-line status per component:
`<EtherType>: <PASS both | follow-ups: N | blocked: reason>`. The final wave also writes
`docs/internal/consumability/_SUMMARY.md`: a rollup table (Component | Base type | B verdict |
A verdict | harness-covered? | # follow-ups) followed by an **aggregated cross-cutting gaps** list
(e.g. every control missing an adapter path, every control absent from the inventory).
