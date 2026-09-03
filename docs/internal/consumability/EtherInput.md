# Consumability review — Input

> Reviewed against
> [the review spec](../2026-09-01-component-consumability-review-spec.md). Findings are grounded in
> source (file:line); the official WinUI baseline is Windows App SDK 2.3.1
> (`Directory.Packages.props:7`).
>
> **Reviewed:** `EtherInput` (`: TextBox`) —
> [EtherInput.cs](../../../src/Ether.DesignSystem.Controls/Controls/Inputs/EtherInput.cs),
> [EtherInput.xaml](../../../src/Ether.DesignSystem.Controls/Controls/Inputs/EtherInput.xaml).
> **Package:** `Ether.DesignSystem.Controls`. **Date:** 2026-09-01 (post-remediation, R3).

## Verdict at a glance
- **Base-control parity (B):** the confirmed base is `TextBox` (`EtherInput.cs:28`); the required
  `ContentElement` part and bindings for text presentation, placeholder, selection color, wrapping,
  and scroll behavior remain in the template/style (`EtherInput.xaml:128-156,166-189`).
- **Design coverage (B):** the intended single-line input is present, and
  `Header`/`HeaderTemplate`/`Description` are documented exclusions with external-layout
  alternatives (`EtherInput.cs:15-19`; `design library handoff/getting-started.md:273-275`).
  `HorizontalTextAlignment` is now wired, not `needs-review`: the template binds the placeholder's
  `TextAlignment` to `HorizontalTextAlignment` (`EtherInput.xaml:151`), and the property was
  removed from the `needs-review` list (`scripts/UnsupportedProperties.psd1`, previously
  line 315).
- **Consumability (A):** `Text` reaches `ObserveInput` and the standard adapter fixture
  (`ControlInteractionAdapter.cs:31-41`; `RuntimeVerification.PropertyConsumption.cs:367`); TwoWay
  `Text` is now proven VM-backed in both directions (`RuntimeVerification.R2.cs:88-96`), and the
  peer contract is asserted honestly per the spec's C4 guidance for `TextBox` — control type
  `Edit`, keyboard-focusable, enabled — because WinUI's `TextBoxAutomationPeer` provides neither a
  managed `IValueProvider` nor `ITextProvider` in-process (`RuntimeVerification.R2.cs:278-293`).

**Overall: consumer-ready.** Review B's one parity unknown is resolved; Review A's follow-ups are closed by the harness (`ETHER_CONSUMER_SMOKE` `outcome:"success"`).

---

## Review B — Completeness / Parity matrix

| Property / Event | Source | Expected (consumer) | Present? | Bindable/Observable? | Verdict |
| --- | --- | --- | --- | --- | --- |
| `Text` | base | two-way bind the entered value | inherited `TextBox.Text`; Gallery handles changes `InputPage.xaml:18-22`; VM-backed TwoWay round-trip now proven `RuntimeVerification.R2.cs:88-96` | ✔ DP + events | ✅ |
| `TextChanged` / `TextChanging` / `BeforeTextChanging` | base | validate/react to edits | inherited; Gallery uses `TextChanged` `InputPage.xaml:18-22` | events | ✅ |
| `PlaceholderText` / `PlaceholderForeground` | base | show and style empty-field guidance | inherited; template binds both `EtherInput.xaml:145-155` | ✔ DPs | ✅ |
| selection API (`SelectedText`, `SelectionStart`, `SelectionLength`, `SelectionHighlightColor`) | base | read/set selection and selection color | inherited; style sets highlight brush `EtherInput.xaml:166-175` | ✔ DPs | ✅ (unverified by fixture) |
| `AcceptsReturn` / `TextWrapping` | base | choose single- vs multi-line input | inherited; default style fixes false/no-wrap but consumer can override `EtherInput.xaml:181-188` | ✔ DPs | ✅ (behavior unverified) |
| `IsReadOnly` / `CharacterCasing` | base | constrain editing | inherited; registry classifies base-handled behavior `scripts/UnsupportedProperties.psd1:308-318` | ✔ DPs | ✅ (unverified by fixture) |
| `HorizontalTextAlignment` | base | align entered text | inherited; the placeholder presenter's `TextAlignment` is now template-bound to it, and the property was removed from `needs-review` `EtherInput.xaml:151` | ✔ DP; template-bound | ✅ (resolved — no longer unverified) |
| `Header` / `HeaderTemplate` / `Description` | base | label and describe the field | inherited but template omits them `EtherInput.cs:15-19` | base DPs ignored by template | 🚫 use external label/helper layout `getting-started.md:273-275` |
| delete/clear button | base | clear current text through stock affordance | omitted by the simplified template `EtherInput.cs:15-18`; no public Ether replacement | n/a | 🚫 clear through bound `Text` or an external button `EtherInput.cs:15-19` |
| `IsEnabled` | base | disable editing | inherited; Disabled state dims layout `RuntimeVerification.Input.cs:74-82` | ✔ DP | ✅ |

**Notes / gaps (B):**
- **The TextBox lineage and essential editing part are explicit.** The class derives from
  `TextBox` (`EtherInput.cs:28`), declares `ContentElement` as a `ScrollViewer` part
  (`EtherInput.cs:20-27`), and the template supplies it with text/scroll bindings
  (`EtherInput.xaml:128-156`).
- **The three slot exclusions reconcile with the shipped contract.** The registry lists
  `Header`/`HeaderTemplate`/`Description` with external-label alternatives
  (`scripts/UnsupportedProperties.psd1:214-237`), and the consumer guide publishes the same
  alternatives (`design library handoff/getting-started.md:273-275`).
- **`HorizontalTextAlignment` is resolved, not a genuine unknown anymore.** The template's
  placeholder `TextBlock` now binds `TextAlignment="{TemplateBinding HorizontalTextAlignment}"`
  (`EtherInput.xaml:151`, previously bound to the unrelated `TextAlignment` property), and the
  entry was removed from `scripts/UnsupportedProperties.psd1`'s `needs-review` list (previously at
  line 315).

---

## Review A — Consumability report

```
Component: EtherInput
Base control: TextBox   | Package: Ether.DesignSystem.Controls
Declared DPs: 0  | inherited public surface: TextBox (three documented slot exclusions)

Rubric (inherited text surface):
  R1 DependencyProperty ....... PASS-by-inheritance  (TextBox lineage, EtherInput.cs:28)
  R2 Round-trips .............. PASS for Text  (VM-backed TwoWay binding round-trips in both
                                       directions, RuntimeVerification.R2.cs:88-96)
  R3 Observable ............... PASS for Text  (ObserveInput uses TextProperty callback,
                                       ControlInteractionAdapter.cs:31-41; exercised at
                                       PropertyConsumption.cs:367)
  R4 TwoWay ................... PASS  (VM-backed Text binding: control receives the VM value and
                                       pushes changes back, RuntimeVerification.R2.cs:88-96)
  R5 Documented ............... N/A   (no public Ether-owned property/identifier pair; exclusions
                                       are documented in EtherInput.cs:15-19)

Contracts:
  C1 ItemsSource / selection .. N/A   (not a collection control; Text is the data contract)
  C2 Listenable + adapter ..... PASS  (Text changes + ObserveInput,
                                       ControlInteractionAdapter.cs:31-41; standard fixture at
                                       PropertyConsumption.cs:367)
  C3 Command .................. N/A   (TextBox editing is value/event based)
  C4 Automation peer/pattern .. PASS  (honest C4 per spec: WinUI's TextBoxAutomationPeer returns
                                       neither a managed IValueProvider nor ITextProvider
                                       in-process — the editable Text pattern is native/
                                       out-of-process to clients like Narrator, confirmed
                                       empirically. The in-process proof is
                                       GetAutomationControlType()==Edit, IsKeyboardFocusable(),
                                       and IsEnabled(), RuntimeVerification.R2.cs:278-293)

Harness: covered? YES. In PublicPropertyInventoryTypes (PublicPropertyInventory.cs:37),
         the standard-interaction list (PropertyConsumption.cs:367), and
         RuntimeVerification.Input.cs:14-17 + RuntimeVerification.R2.cs (TwoWay/Edit peer); no
         Ether-owned public DPs require DeclaredPropertyOwnerTypes. Runtime marker
         ETHER_CONSUMER_SMOKE outcome:"success" includes "EtherInput.Text" in
         twoWayBindings.Properties and "EtherInput.Edit" in automationPatterns.Patterns
         (artifacts/audit-runs/consumer-runtime-evidence-20260901-161450498/runtime-result.json).
x64 build: green (Controls + Interactions + ConsumerFixtures, per the R2.11 runtime run recorded
           in docs/internal/consumability/_RUN-ON-DESKTOP.md).
```

---

## Follow-ups (concrete, actionable)

1. ~~**[B, harness] Resolve `HorizontalTextAlignment`.**~~ DONE — the placeholder presenter's
   `TextAlignment` is now template-bound to `HorizontalTextAlignment`
   (`EtherInput.xaml:151`), and the property was removed from `needs-review` in
   `scripts/UnsupportedProperties.psd1`.
2. ~~**[R2, R4, harness] Prove the text/selection data path.**~~ DONE for `Text` — `RuntimeVerification.R2.cs:88-96`
   proves a VM-backed TwoWay `Text` binding round-trips in both directions. `SelectedText`/
   `SelectionStart`/`SelectionLength`, `IsReadOnly`, and multiline behavior still have no dedicated
   probe (minor residual, not a rubric failure — these are inherited TextBox behaviors, not
   Ether-owned state).
3. ~~**[C4, harness] Assert TextBox automation.**~~ DONE, honestly reframed — the R2 rehearsal
   established that `TextBoxAutomationPeer.GetPattern` returns neither `IValueProvider` nor a
   managed `ITextProvider` in-process (confirmed twice; the editable Text pattern is native to
   out-of-process UIA clients such as Narrator). `RuntimeVerification.R2.cs:278-293` asserts the
   honest in-process contract instead: `AutomationControlType.Edit`, `IsKeyboardFocusable()`, and
   `IsEnabled()`. This is not a product gap; no custom automation peer was added, matching the
   spec's explicit guidance.

No remaining follow-ups.
