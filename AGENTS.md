# Agent Guidelines — Ether Design System

> Instructions for any AI agent working in this repo (Claude Code, Codex, etc.).
> These rules override default behavior. Follow them for every task.

## ⚠️ Hard rule: never re-pack the same package version

When packing the design-system NuGet packages (`Ether.DesignSystem.Foundation` / `.Controls` /
`.Interactions`), **bump the version on every content change** — `0.1.0-preview.1` → `-preview.2`
→ `-preview.3`, and so on. **Never re-pack a changed assembly under a version number that was
already produced.** Treat a version, once packed, as immutable.

**Why (verified 2026-09-02 — do not relearn this the hard way).** NuGet's *only* identity for a
package is its version string, and both the global cache (`~/.nuget/packages`) and every consumer
cache by that string. Re-pack the **same** version with new content and anyone who already restored
the old build keeps getting the **stale API** from their cache — the new members are simply
invisible to them. This bit a real third-party consumer during package verification: the XAML
compiler rejected `EtherSegmentedControl.ItemsSource` / `SelectedIndex` / `SelectionCommand` and
`EtherMasthead.ActionInvoked` with `WMC0011: Unknown member`, purely because a stale
`0.1.0-preview.1` sat in the global cache while the current package had them. The same trap also
silently blocks the unsigned-MSIX gate (`CS0246` for a control that exists).

- **Publishing:** increment the version for every publish. A published/consumed version is frozen forever.
- **Local verification with a fixed pre-release version:** clear the stale copies first —
  `rm -rf ~/.nuget/packages/ether.designsystem.{controls,foundation,interactions}/<version>` — or
  restore into an isolated `--packages` cache (the way `scripts/Verify-ConsumerFixtures.ps1` does).

## Release & verification gates — read the lessons first

Before running `scripts/Publish-Internal.ps1`, and after **any** reskin, token/font edit, new
component, or new public dependency property, read **[LESSONS.md](LESSONS.md)**. It records the
release-gate pitfalls that only surface at rehearsal time — the DPI determinism flake, the
`SilentPropertyCoverage` accounting drift, and the reskin ripples across otherwise-unrelated CI
gates — each with a fix and a pre-flight checklist so they do not recur. These gates are **not run
by hosted CI**, so a fully green PR can still fail the release rehearsal. Runtime-harness gotchas
(WinUI UIA patterns, shared-fixture state, external-consumer XAML paths) live in
[`.claude/skills/consumability-review/references/lessons-learned.md`](.claude/skills/consumability-review/references/lessons-learned.md).
WinUI control-authoring + build gotchas (FocusStates per base type, selector item VSM state names,
shared-`Geometry` recycle, keyed-style min-size leak, the "design-system-owned ≠ no effect" rule,
the Gallery `x64` build/`MSB4276` fix, the UIA/`CopyFromScreen` self-verify recipe) live in
[`docs/internal/technical-gotchas.md`](docs/internal/technical-gotchas.md).

## Core principle: Reuse the official WinUI skeleton — reskin, don't rewrite

Every component in this library is a WinUI 3 control that should **behave identically to its
official WinUI counterpart and differ only in appearance**. So for **any** component work —
whether you are **creating**, **rebuilding**, or **reviewing** one — the default rule is:

> **If an official WinUI control's architecture/template/skeleton can be copied and reskinned,
> copy the skeleton and swap the skin. Do not write it from scratch.**

### Always reference the official sources first

Before writing or changing a component, consult the official implementation:

- **WinUI-Gallery** — usage, patterns, expected behavior: https://github.com/microsoft/WinUI-Gallery
- **microsoft-ui-xaml** — the actual default styles, `ControlTemplate`s, template part names,
  `VisualStateManager` groups/states, and automation peers: https://github.com/microsoft/microsoft-ui-xaml

Match the version this repo targets (WindowsAppSDK / WinUI 3, see the csproj) when pulling a template.

### How to apply it

1. **Start from the closest official control.** Subclass or restyle it (e.g. `X : ListView`,
   a keyed `Style` + `ControlTemplate` over the stock control). Pick the control whose semantics
   already match the design, not just its looks.
2. **Keep the official skeleton intact.** Preserve the official **template part names**, the
   **VisualState group and state names**, and the **automation semantics**. Change only the
   *skin*: brushes/tokens, geometry, corner radius, spacing, typography.
3. **Change the minimum necessary.** Prefer retargeting existing template brushes/parts over
   rewriting the template. Do **not** reimplement behavior the platform already provides —
   selection, keyboard navigation, focus, accessibility, and state transitions come for free
   when you keep the skeleton.
4. **Only hand-author a full custom template when no official control fits** the required
   behavior — and state explicitly (in the spec/PR) why nothing official was suitable.
5. **Document any deviation.** If you must diverge from the official skeleton (e.g. a platform
   property is missing in the pinned SDK version), write down what you changed and why.

### Why

These are basic controls. Rewriting them from scratch silently reintroduces bugs the platform
has already solved and drifts from WinUI's contract. (Example: the ToggleSwitch On→Off flicker
was caused by a hand-written template that broke the native control's template-part contract;
re-deriving it from the official ToggleSwitch skeleton fixed it.) Reskinning an official
skeleton keeps behavior correct by construction and keeps our controls a faithful, on-brand
reskin of WinUI.
