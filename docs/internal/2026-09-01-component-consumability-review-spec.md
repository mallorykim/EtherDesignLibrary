# Component Consumability & API-Completeness Review — Spec

> **Status:** Draft spec (2026-09-01). Defines *two* review passes over every shipping Ether
> control, so the library can be handed to third-party developers as a real, fully-wired control
> library — not just a visually-correct one.
>
> **For agentic workers:** this is a spec, not a task list you run blindly. Each review below
> ends with a **deliverable format** and a **pass/fail gate**. Produce one report per component,
> using the templates here. Reference the official sources first (see [AGENTS.md](../../AGENTS.md)).
>
> **Status (2026-09-01): the audit has been executed** over all 15 shipping components by Codex
> GPT-5.6-SOL (supervised). Per-component reports + an aggregated rollup live in
> [`consumability/`](consumability/) (start at [`_SUMMARY.md`](consumability/_SUMMARY.md)); the audit
> method Codex followed is [`consumability/_review-brief.md`](consumability/_review-brief.md).
>
> **A consumer contract already exists in the repo** and both reviews reconcile with it, rather than
> running parallel to it: `scripts/UnsupportedProperties.psd1` (inherited properties declared
> unsupported / needs-review) and `docs/consumers/getting-started.md` (the consumer guide). A property
> implemented in code but still listed unsupported there — or vice-versa — is a **stale-contract**
> finding (Review B), e.g. `EtherDropdown.PlaceholderText`.

---

## 0. Orientation — what we are actually reviewing, and why

Ether is a **WinUI 3 control library distributed as NuGet packages**
(`Ether.DesignSystem.Foundation`, `Ether.DesignSystem.Controls`, `Ether.DesignSystem.Interactions`).
The consumer is **another developer** who does `dotnet add package`, drops `<ether:EtherButton .../>`
into their XAML or `new EtherSlider()` into their code, binds it to their own view-model, wires it
to their backend, and ships. That means each control must be:

1. **Wire-able to a backend** — it can be data-bound to a view-model and its interactions can be
   observed and forwarded to a backend (not merely display static content).
2. **Property- and event-complete** — it exposes the properties (icon / icon side, min / max /
   step, title, data, item count, …) and the events (click, value-changed, selection-changed,
   data-changed, …) a consumer expects.
3. **Fully bindable/observable** — every one of those APIs is *reachable from the package*:
   public, and (for XAML) a `DependencyProperty` so it can be a binding target, styled, animated,
   and observed.

We split the verification of this into **two complementary reviews**. Read §1 for the naming/theory,
§2–§3 for the actual specs, §4 for process, §5 for the per-component backlog.

> The two reviews are complementary, not redundant:
> **Review A asks "is what we exposed genuinely consumable?"** (quality of the surface).
> **Review B asks "did we expose everything a consumer needs?"** (completeness of the surface).
> A control can pass A and fail B (every property it has is bindable, but it's missing `StepFrequency`),
> or pass B and fail A (it has every property, but they're plain CLR properties you can't bind to).

---

## 1. What these reviews are called (terminology)

The user asked what this kind of review is named in industry. Precise names, so we can talk about
it the same way the rest of the ecosystem does:

### Review A — "可被消费性" → **API Consumability Review**
Also called a **Public API Design Review** / **API surface review**, and — at the level of the
whole hand-off experience — a **Developer Experience (DX) review**. On .NET it is governed by
Microsoft's **Framework Design Guidelines** (Cwalina & Abrams) and, for any public-surface change,
an **API Review** gate (the same practice `dotnet/runtime` runs on its `api-review` label).

For a **WinUI/XAML control library specifically**, "consumable" decomposes into four contracts:

| Contract | The question it answers | The WinUI mechanism |
| --- | --- | --- |
| **Dependency-Property contract** | Can a consumer *bind / style / animate / observe* this property, or only assign it once in code? | Each public property must be backed by a `DependencyProperty` with a public read/write CLR wrapper (not a plain CLR property). |
| **Data-binding contract** | Can the consumer drive the control from a view-model / backend data? | Two-way binding on value/selection props; `ItemsSource` + `DataTemplate` + `SelectedItem`/`SelectedValue`/`SelectedIndex` on collection controls; `DataContext` flows through. |
| **Event / Command contract** | Can the consumer *listen* and react (and route to a backend)? | Native or custom `RoutedEvent` / `EventHandler<T>` (Click, ValueChanged, SelectionChanged, …), `ICommand` (`Command`/`CommandParameter`) where the base control supports it, and observability via `RegisterPropertyChangedCallback`. |
| **Automation / Accessibility contract** | Can assistive tech + UI tests reach it? | An `AutomationPeer` and the right UIA pattern (Invoke, Toggle, RangeValue, Selection, Value…). |

The **backend hook** for the Event/Command contract already exists in this repo as
`Ether.DesignSystem.Interactions` (`ControlInteractionAdapter` → `InteractionEvent` → `IInteractionSink`
outbox). Review A must confirm every control is reachable through that adapter *or* explains why it
isn't.

### Review B — "属性是否完整暴露 / 能否被全面引用" → **API Completeness Audit**
Also called an **API Coverage Review**, an **API Parity Review** (parity against the control you
reskin), or a **feature-/property-matrix audit**. It answers "complete *against what?*" with two
baselines:

1. **Base-control parity** — the official WinUI control we subclass/reskin (`Button`, `ListView`,
   `RangeBase`/`Slider`, `ComboBox`, `ToggleSwitch`, `TextBox`, `RangeBase`…). Every meaningful
   inherited property/event must **survive** and stay settable/bindable — not be shadowed, hidden,
   or broken by our template. This is the floor, and it is free *if* we kept the official skeleton
   (see [AGENTS.md](../../AGENTS.md)).
2. **Design-spec coverage** — the Figma design: variants, states, and slots (icon / icon side,
   min / max / step, title, labels, item count, show/hide toggles…). Every design knob must have a
   corresponding public API a consumer can set.

---

## 2. Review A spec — API Consumability Review

### 2.1 Goal
For every shipping control, prove that **every public property is bindable/observable**, that the
control can be **driven from data and a view-model**, and that its **interactions can be listened to
and forwarded to a backend**. Turn "it renders correctly" into "a third party can wire it up."

### 2.2 The Consumability Rubric (apply to every public property)
A public, settable property **passes** only if all of the following hold. (Inherited platform
properties that are already `DependencyProperty`s inherit their pass automatically; our job is to
prove we didn't break them — see Review B parity.)

- [ ] **R1 — Is a `DependencyProperty`.** Backed by a `public static readonly DependencyProperty
  XxxProperty` with a matching public `Xxx { get; set; }` CLR wrapper. *(Rationale: a plain CLR
  property can be set in code but cannot be a `{Binding}`/`{x:Bind}` target, cannot be set by a
  `Style`/`Setter`, cannot be animated, and cannot be observed. It is invisible to XAML consumers.)*
- [ ] **R2 — Round-trips.** `GetValue`/`SetValue` and the CLR wrapper agree; setting via one is
  visible via the other.
- [ ] **R3 — Is observable.** A `RegisterPropertyChangedCallback` on it fires when it changes.
- [ ] **R4 — Two-way where it represents user/data state.** Value/selection/checked/text props
  support `Mode=TwoWay` and push changes back (`Value`, `IsChecked`, `IsOn`, `Text`,
  `SelectedItem`/`SelectedIndex`/`SelectedValue`).
- [ ] **R5 — Documented.** XML-doc on the property (and the `…Property` field) says what it does
  and its default, so IntelliSense is useful at the call site.

### 2.3 Per-control contracts (beyond individual properties)
- [ ] **C1 — Data/collection controls accept data.** Collection-backed controls
  (`EtherDropdown`, `EtherSegmentedControl`, `EtherTabNavigation`) expose `ItemsSource`,
  `DisplayMemberPath`/`SelectedValuePath` or a `DataTemplate`, and a selection API
  (`SelectedItem`/`SelectedIndex`/`SelectedValue`) that round-trips.
- [ ] **C2 — Interactions are listenable.** The control raises a public event (native or custom)
  for its primary interaction, and that event is reachable through
  `ControlInteractionAdapter.Observe…` producing a backend-consumable `InteractionEvent`.
- [ ] **C3 — Commands where applicable.** Button-family controls honor `Command`/`CommandParameter`.
- [ ] **C4 — Automation peer + UIA pattern present** for the control's role (Invoke/Toggle/
  RangeValue/Selection/Value).

### 2.4 How this maps to what already exists (do not rebuild it)
This repo already implements most of Review A as an **automated consumer-fixture harness** that
consumes the built packages exactly as a third party would. Review A's job is to (a) confirm the
harness is green, and (b) **close its coverage gaps**, not to re-author it.

| Rubric / contract | Existing enforcement | File |
| --- | --- | --- |
| R1, R2, R3 | `VerifyPropertyConsumption` asserts DP wrapper read/write, change callback, and GetValue↔wrapper round-trip for every declared DP | [`RuntimeVerification.PropertyConsumption.cs`](../../tests/Ether.DesignSystem.ConsumerFixtures/RuntimeVerification.PropertyConsumption.cs) |
| R1 (full surface) | `CapturePublicPropertyInventory` enumerates the whole writable public surface; `ClassifyAllPublicProperties` buckets it Visual / Semantic / Platform | [`…PublicPropertyInventory.cs`](../../tests/Ether.DesignSystem.ConsumerFixtures/RuntimeVerification.PublicPropertyInventory.cs), [`…PublicPropertyClassification.cs`](../../tests/Ether.DesignSystem.ConsumerFixtures/RuntimeVerification.PublicPropertyClassification.cs) |
| C2 (backend) | `ControlInteractionAdapter.Observe{Button,Toggle,Input,Dropdown,SegmentedControl,Range,SteeringBar,Switch}` → `InteractionEvent` envelope → `IInteractionSink` outbox; `VerifyStandardInteractionAdapters` exercises 12 controls | [`ControlInteractionAdapter.cs`](../../src/Ether.DesignSystem.Interactions/ControlInteractionAdapter.cs), [`InteractionContracts.cs`](../../src/Ether.DesignSystem.Interactions/InteractionContracts.cs) |
| C4 | per-control fixtures assert automation peers/patterns | `RuntimeVerification.{Button,Slider,…}.cs` |

**Known coverage gaps to close as part of Review A** (verified 2026-09-01):
- `PublicPropertyInventoryTypes` (12 types) **omits** `EtherSwitch`, `EtherScrollBar`,
  `EtherTabNavigation` / `EtherTabItem`, and `EtherCard`. → the full-surface inventory does not
  see them at all.
- `DeclaredPropertyOwnerTypes` (8 types, in PropertyConsumption) **omits** `EtherTabItem` (whose
  `Icon` DP is otherwise unexercised) and any declared DPs on Checkbox/Radio/Input/Switch/
  IntelligenceButton.
- `EtherTabNavigation : ListView` inherits `ItemsSource`/`SelectedItem`/`SelectionChanged` — these
  must be added to a C1 fixture so we prove the data path, not just assume it.
- `EtherCard` is currently a **XAML style/resource, not a control class** — Review A must record
  whether "Card" is a consumable *type* or only an applied `Style`, because that changes how a
  consumer references it.

### 2.5 Pass/fail gate for Review A
A control **passes Review A** when: every public settable property passes R1–R5 (or is explicitly
classified as an intentional non-DP with a written reason); its per-control contracts C1–C4 hold or
are explicitly N/A with a reason; and it is represented in the `ConsumerFixtures` harness (added if
it was in a gap above) with the harness green on x64.

### 2.6 Deliverable — one `consumability` report per control
```
Component: EtherXxx
Base control: <official WinUI type>  | Package: Ether.DesignSystem.Controls
Public settable properties: N   (DP-backed: N  | plain-CLR: 0 expected)
Rubric:
  R1 DependencyProperty ....... PASS / FAIL(list)   
  R2 Round-trips .............. PASS / FAIL(list)
  R3 Observable ............... PASS / FAIL(list)
  R4 TwoWay (state props) ..... PASS / N/A / FAIL(list)
  R5 Documented ............... PASS / FAIL(list)
Contracts:
  C1 ItemsSource/selection .... PASS / N/A(reason)
  C2 Listenable + adapter ..... PASS / FAIL(reason)      adapter: Observe…()
  C3 Command .................. PASS / N/A(reason)
  C4 Automation peer/pattern .. PASS / FAIL(reason)      pattern: <Invoke/Toggle/…>
Harness: covered? yes/no   fixture: <file>   x64 build: green/red
Gaps / follow-ups: <list, each with a concrete fix>
```

---

## 3. Review B spec — API Completeness / Parity Audit

### 3.1 Goal
For every control, prove **nothing a consumer needs is missing**. Compare the actual public surface
against the two baselines (base-control parity + Figma design coverage) and list every gap.

### 3.2 The Property Matrix (the core artifact of Review B)
For each control, fill one matrix. Columns:

| Column | Meaning |
| --- | --- |
| **Property / Event** | The API name. |
| **Source** | `base` (inherited from the WinUI control) · `design` (a Figma knob) · `both`. |
| **Expected** | What a consumer should be able to do (bind title, set icon side, set min/max/step, bind item list, listen to change…). |
| **Present?** | Is it publicly exposed today? (name it, or ✗). |
| **Bindable/Observable?** | Passes Review A's R1/R3? (only meaningful if Present). |
| **Verdict** | ✅ complete · ⚠️ present-but-not-bindable · ❌ missing · 🚫 intentionally-excluded (needs written reason). |

The matrix is **design-driven**: start from what the Figma spec and the base control offer, *then*
check the code — never start from the code (that only re-confirms what exists and hides the holes).

### 3.3 Worked examples (method illustration — verify, don't trust these cells)
These show *how* to fill the matrix; the Present/Verdict cells below are to be confirmed by the audit,
not asserted here.

**EtherButton** (`: Button`)

| Property/Event | Source | Expected | Present? | Bindable? | Verdict |
| --- | --- | --- | --- | --- | --- |
| Content | base | set/bind button text or arbitrary content | `Content` | verify R1/R3 | verify |
| LeftIcon | design | optional leading icon | `LeftIcon` (DP) | ✔ verified in code | ✅ |
| RightIcon | design | optional trailing icon (icon side) | `RightIcon` (DP) | ✔ | ✅ |
| Variant | design | Primary/Secondary/Tertiary | `Variant` (DP) | ✔ | ✅ |
| Size | design | Large/Small | `Size` (DP) | ✔ | ✅ |
| Command / CommandParameter | base | MVVM command binding | inherited | verify | verify |
| Click | base | listen to taps → backend | inherited `Click` | adapter `ObserveButton` | ✅ |
| IsEnabled | base | disable | inherited | verify | verify |

**EtherSlider** (range control — the user's min/max/step example)

| Property/Event | Source | Expected | Present? | Bindable? | Verdict |
| --- | --- | --- | --- | --- | --- |
| Minimum | base | set/bind min | verify (via base `RangeBase`) | verify R1 | **must verify** |
| Maximum | base | set/bind max | verify | verify | **must verify** |
| StepFrequency / SmallChange / LargeChange | base | step size | verify | verify | **must verify** |
| Value | base | two-way bind current value | `Value` (used by `ObserveRange`) | verify R4 | verify |
| Title / ShowTitle | design | header + toggle | `Title`,`ShowTitle` (DP) | ✔ | ✅ |
| Labels / ShowLabels | design | tick labels + toggle | `Labels`,`ShowLabels` (DP) | ✔ | ✅ |
| Stops / SnapToStops | design | discrete stops | `Stops`,`SnapToStops` (DP) | ✔ | ✅ |
| ValueChanged | base | listen to value → backend | verify (base event) + adapter `ObserveRange` | verify | verify |

> Slider note: confirm whether `EtherSlider` derives from `Slider`/`RangeBase` (then Minimum/Maximum/
> StepFrequency/Value are inherited and just need parity verification) or is a `UserControl` that must
> **re-declare** those as DPs. Its use with `ObserveRange(RangeBase)` implies a RangeBase lineage —
> confirm, because if it's a UserControl the range API is a completeness hole.

**EtherTabNavigation** (`: ListView` — the user's top-nav example)

| Property/Event | Source | Expected | Present? | Bindable? | Verdict |
| --- | --- | --- | --- | --- | --- |
| ItemsSource | base | bind the list of tabs (drives count) | inherited `ListView.ItemsSource` | verify R1 | **must verify** |
| SelectedItem / SelectedIndex | base | two-way current tab | inherited | verify R4 | **must verify** |
| SelectionChanged | base | listen to tab switch → backend | inherited | needs adapter path | ⚠️ no `ObserveSelection` yet |
| Icon (per `EtherTabItem`) | design | per-tab icon, on/off | `EtherTabItem.Icon` (DP, string) | verify R1 | verify |
| ItemTemplate | base | custom tab content | inherited | verify | verify |

### 3.4 Pass/fail gate for Review B
A control **passes Review B** when its matrix has **no ❌ (missing)** and **no ⚠️ (present-but-not-
bindable)** rows, except rows explicitly marked 🚫 with a written justification (e.g. a platform
property deliberately hidden, per [AGENTS.md](../../AGENTS.md) "document any deviation"). Every 🚫
must name the alternative the consumer uses instead.

### 3.5 Deliverable — one `completeness` matrix per control
The filled §3.2 table + a short "Gaps" list, where each gap is a concrete, actionable item
("add `StepFrequency` passthrough", "add `ObserveSelection` to the adapter for `EtherTabNavigation`",
"decide whether Card is a control type or a Style").

---

## 4. Process & workflow

1. **Freeze the inventory.** The authoritative shipping list is the Gallery catalog
   ([`ComponentCatalog.cs`](../../samples/Ether.DesignSystem.Gallery/ComponentCatalog.cs)) — 15
   components: Button, Checkbox, Dropdown, Input, Intelligence Button, Radio Button, Scroll Bar,
   Segmented Control, Slider, Steering Bar, Toggle Switch, Card, Masthead, Tab Navigation,
   Progress Bar. Reconcile it with the `src/` control list and the `ConsumerFixtures` type lists;
   every divergence is itself a finding.
2. **Per component, run B then A.** Do Completeness (B) first — it tells you the true target
   surface — then Consumability (A) over that surface. (You *can* parallelize across components.)
3. **Reference official sources first** for every "expected" cell: WinUI-Gallery for behavior,
   microsoft-ui-xaml for the base control's real property/event set. Match the SDK version in the
   csproj.
4. **Record results** as one report + one matrix per component under
   `docs/internal/consumability/EtherXxx.md` (create the folder), and roll gaps up into the
   backlog in §5.
5. **Close gaps** by extending the existing harness (fixtures + adapter) and the controls — do not
   fork a parallel verification system. Coding changes go through the normal delegate-to-Codex flow.
6. **Gate:** a component is "consumer-ready" only when it passes **both** A and B and is green in
   the `ConsumerFixtures` harness on x64.

---

## 5. Execution backlog (per component)

Legend: ☐ = to do. Each component needs one B-matrix + one A-report. "Harness gap" flags a control
missing from the fixture type lists (§2.4).

- [ ] **Button** — A + B.
- [ ] **Checkbox** — A + B. (declared-DP coverage in PropertyConsumption: verify)
- [ ] **Radio Button** — A + B. (declared-DP coverage: verify)
- [ ] **Dropdown** — A + B. C1 data path (ItemsSource/SelectedValue) is the focus.
- [ ] **Input** — A + B. (declared-DP coverage: verify; Text two-way)
- [ ] **Intelligence Button** — A + B.
- [ ] **Segmented Control** — A + B. Has custom `SelectionChanged` event — verify args are consumable.
- [ ] **Slider** — A + B. **Resolve RangeBase-vs-UserControl** (min/max/step parity, §3.3).
- [ ] **Steering Bar** — A + B. Has custom `ValueChanged` event.
- [ ] **Toggle Switch** — A + B. **Harness gap:** not in `PublicPropertyInventoryTypes`.
- [ ] **Scroll Bar** — A + B. **Harness gap:** not in inventory; confirm it's a consumable API vs. an app-wide implicit style.
- [ ] **Progress Bar** — A + B.
- [ ] **Masthead** — A + B. Many `Show*` toggles — verify each is a bindable DP.
- [ ] **Tab Navigation** — A + B. **Harness gap:** `EtherTabNavigation`/`EtherTabItem` absent from inventory *and* PropertyConsumption; add C1 ItemsSource/selection path + adapter `ObserveSelection`.
- [ ] **Card** — A + B. **Decide type vs. style** first (§2.4); the answer changes both reviews.
- [ ] **Cross-cutting:** extend `ControlInteractionAdapter` with any missing `Observe…` (e.g. a
  `Selection`/`ListView` observer) surfaced by the per-component C2 checks.

---

## 6. Glossary
- **DependencyProperty** — the WinUI property system entry that makes a property bindable, styleable,
  animatable, and observable. The single most important thing Review A checks.
- **CLR wrapper** — the plain `Xxx { get => (T)GetValue(XxxProperty); set => SetValue(XxxProperty, value); }`
  that sits on top of a `DependencyProperty` so C# call sites and XAML both work.
- **Parity** — the invariant that reskinning an official control must not remove or break any of the
  base control's public properties/events (they should still work through our template).
- **InteractionEvent / IInteractionSink** — the backend-consumable envelope + outbox sink in
  `Ether.DesignSystem.Interactions`; the concrete "connect to a backend" mechanism referenced by C2.
