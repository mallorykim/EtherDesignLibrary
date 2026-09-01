# Agent Guidelines — Ether Design System

> Instructions for any AI agent working in this repo (Claude Code, Codex, etc.).
> These rules override default behavior. Follow them for every task.

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
