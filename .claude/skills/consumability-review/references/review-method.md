# The audit method — Review A & Review B

Terminology (so we talk about it the way the ecosystem does):
- **Review A = API Consumability Review** (a.k.a. Public API Design / DX review). Governed by .NET
  Framework Design Guidelines; the WinUI-specific core is **DependencyProperty** (only a DP is
  bindable, styleable, animatable, observable — a plain CLR property is invisible to XAML consumers).
- **Review B = API Completeness / Parity Audit** (a.k.a. coverage / feature-matrix audit). "Complete
  against what?" → two baselines: **base-control parity** (the official WinUI control we reskin — its
  properties/events must survive our template) and **design-spec coverage** (every Figma variant/
  state/slot has a public API).

They are complementary: **A asks "is what we exposed genuinely consumable?"; B asks "did we expose
everything a consumer needs?"** A control can pass one and fail the other.

## Review A — the Consumability Rubric (per public settable property)
A property passes only if all hold. Inherited platform DPs inherit their pass; prove we didn't break them.
- **R1 — is a `DependencyProperty`** (public `static readonly DependencyProperty XxxProperty` + public
  `Xxx {get;set;}` wrapper). The single most important check.
- **R2 — round-trips** (GetValue/SetValue and the CLR wrapper agree).
- **R3 — observable** (`RegisterPropertyChangedCallback` fires on change).
- **R4 — TwoWay** where it represents user/data state (Value/IsChecked/IsOn/Text/Selected*).
- **R5 — documented** (XML-doc on property + `…Property` field, states the default).

## Review A — per-control contracts (beyond individual properties)
- **C1 — data/collection controls accept data**: `ItemsSource` + `DisplayMemberPath`/`DataTemplate` +
  a selection API that round-trips.
- **C2 — interactions are listenable + backend-wireable**: raises a public event (native or custom),
  reachable through `ControlInteractionAdapter.Observe…` producing an `InteractionEvent` envelope.
- **C3 — commands** where the base supports it (`Command`/`CommandParameter`).
- **C4 — automation peer + UIA pattern** for the control's role (Invoke/Toggle/SelectionItem/Value/
  ExpandCollapse/Selection/RangeValue). See `lessons-learned.md` — the right pattern per control is
  NOT uniform, and TextBox's text pattern is native-only.

## Review B — the property matrix (the core artifact)
One table per control. Columns: **Property/Event | Source (base/design/both) | Expected (what a
consumer should be able to do) | Present? (name it, or ✗) | Bindable/Observable? (R1/R3) | Verdict**.
Verdicts: ✅ complete · ⚠️ present-but-not-bindable · ❌ missing · 🚫 intentionally-excluded (needs a
written reason + the consumer's alternative). **Start design-driven** — from the Figma spec + base
control, *then* check the code. Starting from code only re-confirms what exists and hides the holes.

## Pass/fail gates
- **A passes**: every public settable property passes R1–R5 (or is a written non-DP exception); C1–C4
  hold or are explicit N/A; the control is represented in the `ConsumerFixtures` harness and green on x64.
- **B passes**: no ❌ and no ⚠️ rows, except rows explicitly 🚫 with justification. Every 🚫 names the
  alternative.
- A component is **consumer-ready** only when it passes BOTH and the harness is green on x64.

## Deliverable formats
**Per-component report** (`docs/internal/consumability/<EtherType>.md`) sections, in this order:
`# Consumability review — <Name>` → a `> Reviewed/Package/Date` blockquote → `## Verdict at a glance`
→ `## Review B — Completeness / Parity matrix` (the table + a "Notes / gaps (B)" list) →
`## Review A — Consumability report` (a fenced block: base control, declared DPs, R1–R5 lines, C1–C4
lines, Harness coverage, x64 build) → `## Follow-ups` (each concrete, actionable, tagged with the
rubric/contract it closes, e.g. `[C2, backend]`).

**Rollup** (`_SUMMARY.md`): a table `Component | Base type | B verdict | A verdict | harness-covered? |
# follow-ups`, then an **aggregated cross-cutting gaps** ledger (each gap: CLOSED with a citation, or
a named residual). Honesty beats optimism — name every residual explicitly.

## Grounding rules
Confirm each base type from the actual `class X : Y` (don't trust a guess). Reference the official
sources for "expected": WinUI-Gallery (behavior) + microsoft-ui-xaml (real property/event set), at
the SDK version in the csproj. Never assert a fixture proved something it didn't run.
