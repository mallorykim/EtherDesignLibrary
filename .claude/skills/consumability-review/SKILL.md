---
name: consumability-review
description: >-
  Run the end-to-end consumability + API-completeness review and remediation for Ether WinUI
  components — auditing whether each control is genuinely consumable by third-party developers
  (every property a bindable DependencyProperty, events/commands exposed, backend-wireable via the
  interaction adapter) and whether every base-control + design knob is actually exposed, then
  driving each component to a verified-green ConsumerFixtures run. Use this skill whenever the task
  involves reviewing or auditing Ether components for third-party/package consumption, checking that
  "all properties are exposed" or "can be bound/listened to", API surface/parity/completeness of a
  control, "is this component consumer-ready / ready to ship in the library", closing consumability
  gaps, or getting the ConsumerFixtures runtime verification to pass — even if the user doesn't say
  the word "consumability".
---

# Consumability & API-completeness review + remediation

This skill captures the end-to-end process for making Ether controls **genuinely consumable by
third-party developers** — not just visually correct. The library ships as NuGet packages
(`Ether.DesignSystem.Foundation` / `.Controls` / `.Interactions`); the consumer does
`dotnet add package`, drops `<ether:EtherButton .../>` in their XAML, binds it to a view-model,
wires it to a backend, and ships. Every control must therefore be **wire-able, property/event
complete, and fully bindable/observable**.

Two complementary reviews sit at the core, then a remediation loop drives everything to green:
- **Review A — API Consumability:** is what we exposed genuinely bindable/observable/wireable?
- **Review B — API Completeness/Parity:** did we expose everything a consumer needs?

> **Read the reference files as you go — don't inline everything.** SKILL.md is the map;
> [`references/review-method.md`](references/review-method.md) is the audit method (rubric, matrix,
> report templates), [`references/remediation-to-green.md`](references/remediation-to-green.md) is
> the fix-and-verify loop, and [`references/lessons-learned.md`](references/lessons-learned.md) is
> the hard-won gotchas. **Load `lessons-learned.md` before you touch the runtime harness** — it will
> save you the serial-failure cycles we already paid for.

## When to reach for this

Component work where the question is "can a third party actually *use* this?" — a pre-ship audit,
"are all the properties exposed / bindable", API parity against the base WinUI control, closing a
consumability gap, or getting `scripts/Verify-ConsumerFixtures.ps1` to `outcome:"success"`. It is
not for pure visual/reskin work (that's the reskin rule in `AGENTS.md`).

## The end-to-end workflow

Do these in order. Each phase names its deliverable and its gate. You can run the audit alone, or
the audit + remediation to green.

1. **Freeze the inventory.** The authoritative shipping list is the Gallery catalog
   (`samples/Ether.DesignSystem.Gallery/ComponentCatalog.cs`). Reconcile it with `src/` control
   classes and the `ConsumerFixtures` type lists — every divergence is itself a finding (a control
   that ships but isn't in `PublicPropertyInventoryTypes` is invisible to the harness).

2. **Audit (Review B then A), one report per component.** Do Completeness (B) first — it tells you
   the true target surface — then Consumability (A) over that surface. Ground **every** claim in
   `file:line`; confirm each control's base type from its actual `class X : Y` declaration; be
   honest about what a fixture proves vs. what's only inherited-and-unverified. Output one
   `docs/internal/consumability/<EtherType>.md` per component + a `_SUMMARY.md` rollup. Templates and
   the rubric (R1–R5) / contracts (C1–C4) / property matrix are in `references/review-method.md`.
   **Reconcile against the existing consumer contract** — `scripts/UnsupportedProperties.psd1`
   (properties declared unsupported/needs-review) and `design library handoff/getting-started.md` — a
   property implemented in code but still listed unsupported (or vice-versa) is a stale-contract
   finding.

3. **Write a remediation plan** that drives every red/amber row to green, split into waves:
   **R1 product** (public API/behavior changes), **R2 harness** (fixtures that *prove* each rubric
   /contract + reconcile stale contracts), **R3 docs** (flip reports to the post-fix state). Lock
   any genuine product decisions up front (they change what gets built) — e.g. is a missing knob a
   real gap to add, or an intentional exclusion to document as 🚫 with the consumer's alternative?
   Details + the exact wave breakdown: `references/remediation-to-green.md`.

4. **Execute R1 → R2 → R3, verifying to green** with `scripts/Verify-ConsumerFixtures.ps1` on an
   interactive desktop. "Green" = the change is made, a fixture *proves* it, and the runtime smoke
   reaches `{"marker":"ETHER_CONSUMER_SMOKE","outcome":"success"}`. The run mechanics, baseline-
   regeneration review, and acceptance-constant reconciliation are in `references/remediation-to-green.md`.
   **The gotchas that will otherwise cost you hours are in `references/lessons-learned.md` — read it.**
   When the audit touches anything a consumer sets in **XAML markup** (selection, attribute-set state),
   ALSO run `scripts/Verify-ExternalConsumer.ps1` — the ConsumerFixtures harness drives controls in code
   and can be green while the real markup path is broken (see `lessons-learned.md`).

5. **Commit** in logical groups when the user asks: product (`src/`), harness (`tests/` + `scripts/`
   + curated baselines), docs (`docs/`).

## How to run this without stalling (orchestration)

We learned this the expensive way. The harness build+run is ~8–15 min and **aborts at the first
failing assertion**, so problems surface one at a time in serial cycles. Two rules keep it moving:

- **Split editing from running.** Delegate *edits* to a subagent (edit-only, no build/run — it won't
  stall). Run the long verification yourself via a **background Bash command** (`run_in_background`),
  which reliably notifies you on completion. Subagents that launch a long background run and then
  "wait for the notification" burn tokens in a non-converging loop — don't have a subagent own the
  long run.
- **Supervise, don't trust blindly.** Verify a delegate's `file:line` citations actually exist
  (spot-check with grep/Read — audits can cite plausible-but-wrong lines). Confirm claimed builds by
  re-running one yourself. Review every regenerated baseline PNG before accepting it.

## The honesty discipline (non-negotiable)

This is what makes the green real. Bake these into every delegated task and hold yourself to them:

- **Never weaken, skip, or delete an assertion — or relax a tolerance — to force a pass.** If a check
  fails, find the true cause.
- **Distinguish a test-setup gap from a product bug.** A fixture that presupposes state it never set
  (no selection, a leaked `IsTabStop=false`, an unrealized container) is a *setup* bug — complete the
  setup so the assertion tests the real contract; that is not weakening. A genuine product bug means
  **stop and surface it** for a decision, don't paper over it.
- **Never report green for anything that did not actually run.** Separate passed / unexecuted / failed
  explicitly. A run that aborts early has *not* verified the stages after the abort.
- **Baseline regeneration and acceptance-constant changes are reviewed, not blind.** See
  `references/lessons-learned.md`.

A wave that closes gaps by silently narrowing coverage (or fitting a gate to whatever the code
happens to output) reads as "all green" when it isn't. Name residuals explicitly instead — a green
harness with four honestly-named residuals is worth more than a fake 15/15.
