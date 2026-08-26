# Semantic Colors — Figma intake (Light / Dark)

Recorded verbatim from the company Figma **Semantic** color collection.
Not yet applied to code. Values are the Figma references each semantic token points at:
a **primitive** (`color/…`), another **semantic** token, a raw **hex**, or **Transparent**.

Full key = `group / name` (lowercase, hyphenated — matching the space/radius/typography convention already used, e.g. `background/surface-raised`, `action/primary/bg-hover`). Group headings below show the Figma path; the Name column is the row (leaf) name.

Primitive refs use Figma paths like `color/gray/0`, `color/alpha/black/black-60`. These map to the existing `EtherPrimitives.xaml` keys (`Gray0`, `AlphaBlack60`, …) — see Notes for coverage to verify.

---

## background

| Name | Light | Dark |
|---|---|---|
| canvas | color/gray/0 | color/gray/850 |
| surface | color/gray/0 | color/gray/900 |
| surface-raised | color/gray/25 | color/gray/750 |
| surface-transparant | Transparent | Transparent |
| surface-sunken | color/gray/50 | color/gray/975 |
| surface-inverse | color/gray/1000 | color/gray/0 |
| surface-muted | color/gray/75 | color/gray/700 |
| overlay-scrim | color/alpha/black/black-60 | color/alpha/black/black-70 |
| overlay-hover | color/alpha/black/black-5 | color/alpha/white/white-4 |
| overlay-pressed | color/alpha/black/black-10 | color/alpha/white/white-6 |
| glass-light | color/alpha/white/white-70 | color/alpha/white/white-8 |
| glass-dark | color/alpha/black/black-40 | color/alpha/black/black-60 |
| brand | color/blue/600 | color/blue/600 |
| brand-subtle | color/alpha/brand/brand-10 | color/alpha/brand/brand-16 |
| divider | color/gray/300 | color/alpha/white/white-16 |
| placeholder | color/gray/200 | color/gray/700 |
| track | color/gray/300 | color/gray/750 |
| brand-soft | color/alpha/brand/brand-60 | color/alpha/brand/brand-60 |
| brand-glow | color/alpha/brand/brand-glow-65 | color/alpha/brand/brand-glow-65 |
| control-subtle | color/alpha/black/black-8 | color/alpha/white/white-12 |
| card-body | color/gray/0 | color/gray/750 |
| ai-input | color/gray/200 | color/gray/900 |
| ai-track | color/gray/50 | color/gray/850 |
| card-intelligence | color/gray/975 | color/gray/1000 |
| progress-fill | color/blue/700 | color/blue/400 |

## background / Dropdown

| Name | Light | Dark |
|---|---|---|
| default | color/gray/75 | color/gray/950 |
| hover | color/gray/100 | color/gray/900 |
| pressed | color/gray/150 | color/gray/850 |

## background / Menuitem

| Name | Light | Dark |
|---|---|---|
| default | color/gray/75 | color/gray/950 |
| hover | color/gray/50 | color/gray/850 |
| pressed | color/gray/25 | color/gray/800 |

---

## text

| Name | Light | Dark |
|---|---|---|
| primary | color/gray/1000 | color/gray/0 |
| secondary | color/alpha/black/black-70 | color/alpha/white/white-70 |
| tertiary | color/alpha/black/black-50 | color/alpha/white/white-50 |
| disabled | color/alpha/black/black-30 | color/alpha/white/white-30 |
| inverse | color/gray/0 | color/gray/1000 |
| brand | color/blue/600 | color/blue/500 |
| on-brand | color/gray/0 | color/gray/0 |
| success | color/green/600 | color/green/400 |
| warning | color/orange/600 | color/orange/300 |
| danger | color/red/500 | color/red/300 |
| ai | color/purple/500 | color/purple/400 |
| nav-item | color/gray/0 | color/gray/0 |
| nav-inactive | color/alpha/black/black-50 | color/alpha/white/white-70 |
| nav-narrow-inactive | color/alpha/black/black-60 | color/gray/0 |
| ai-pill | color/gray/0 | color/alpha/white/white-70 |
| ai-secondary | color/alpha/black/black-60 | color/alpha/white/white-60 |
| on-static-dark | color/gray/0 | color/gray/0 |

---

## icon

| Name | Light | Dark |
|---|---|---|
| primary | color/gray/1000 | color/gray/0 |
| secondary | color/alpha/black/black-70 | color/alpha/white/white-70 |
| tertiary | color/alpha/black/black-50 | color/alpha/white/white-50 |
| disabled | color/alpha/black/black-30 | color/alpha/white/white-30 |
| inverse | color/gray/0 | color/gray/1000 |
| brand | color/blue/600 | color/blue/500 |
| on-brand | color/gray/0 | color/gray/0 |
| ai | color/purple/500 | color/purple/400 |

---

## border

| Name | Light | Dark |
|---|---|---|
| subtle | color/alpha/navy/navy-12 | color/alpha/white/white-8 |
| default | color/alpha/navy/navy-20 | color/alpha/white/white-12 |
| strong | color/alpha/navy/navy-40 | color/alpha/white/white-25 |
| focus | color/blue/600 | color/blue/500 |
| brand | color/blue/600 | color/blue/500 |
| inverse | color/gray/1000 | color/gray/0 |
| danger | color/red/500 | color/red/400 |
| success | color/green/500 | color/green/400 |
| ai-tick | color/gray/300 | color/gray/600 |

---

## action / primary

| Name | Light | Dark |
|---|---|---|
| bg | color/blue/600 | color/blue/600 |
| bg-hover | color/blue/500 | color/blue/500 |
| bg-pressed | color/blue/700 | color/blue/700 |
| bg-disabled | color/alpha/black/black-10 | color/alpha/white/white-10 |
| fg | color/gray/0 | color/gray/0 |
| fg-disabled | color/alpha/black/black-30 | color/alpha/white/white-30 |

## action / secondary

| Name | Light | Dark |
|---|---|---|
| bg | color/alpha/black/black-0 | color/alpha/white/white-0 |
| bg-hover | color/alpha/black/black-6 | color/alpha/white/white-8 |
| bg-pressed | color/alpha/black/black-10 | color/alpha/white/white-12 |
| border | color/alpha/navy/navy-20 | color/alpha/white/white-20 |
| fg | color/gray/1000 | color/gray/0 |

## action / tertiary

| Name | Light | Dark |
|---|---|---|
| bg-hover | color/alpha/black/black-4 | color/alpha/white/white-4 |
| fg | color/blue/600 | color/blue/500 |

## action / danger

| Name | Light | Dark |
|---|---|---|
| bg | color/red/500 | color/red/500 |
| bg-hover | color/red/600 | color/red/400 |
| fg | color/gray/0 | color/gray/0 |

---

## status / success

| Name | Light | Dark |
|---|---|---|
| fg | color/green/600 | color/green/400 |
| bg | color/alpha/status/success-12 | color/alpha/status/success-12 |
| border | color/green/500 | color/green/400 |

## status / warning

| Name | Light | Dark |
|---|---|---|
| fg | color/orange/600 | color/orange/300 |
| bg | color/alpha/status/warning-20 | color/alpha/status/warning-16 |
| border | color/orange/500 | color/orange/300 |

## status / danger

| Name | Light | Dark |
|---|---|---|
| fg | color/red/500 | color/red/300 |
| bg | color/alpha/status/danger-12 | color/alpha/status/danger-16 |
| border | color/red/500 | color/red/400 |

## status / info

| Name | Light | Dark |
|---|---|---|
| fg | color/blue/600 | color/blue/500 |
| bg | color/alpha/brand/brand-10 | color/alpha/brand/brand-16 |
| border | color/blue/600 | color/blue/500 |

---

## dataviz

| Name | Light | Dark |
|---|---|---|
| 01 | color/blue/1000 | color/blue/1000 |
| 02 | color/blue/950 | color/blue/950 |
| 03 | color/blue/600 | color/blue/600 |
| 04 | color/cyan/500 | color/cyan/500 |

## ai

| Name | Light | Dark |
|---|---|---|
| accent-purple | color/purple/500 | color/purple/400 |
| accent-cyan | color/cyan/500 | color/cyan/500 |
| accent-blue | color/blue/600 | color/blue/500 |

---

## Buttons / Primary / Background

| Name | Light | Dark |
|---|---|---|
| default | color/blue/600 | color/blue/600 |
| hover | color/blue/700 | color/blue/700 |
| pressed | color/blue/800 | color/blue/800 |
| disabled | color/blue/600 | color/blue/600 |

## Buttons / Primary / border

| Name | Light | Dark |
|---|---|---|
| default | color/alpha/white/white-25 | color/alpha/white/white-16 |

## Buttons / Secondary / Background

| Name | Light | Dark |
|---|---|---|
| default | Transparent | Transparent |
| hover | color/gray/25 | color/gray/800 |
| pressed | color/gray/50 | color/gray/850 |
| disabled | Transparent | Transparent |

## Buttons / Tertiary / Text

| Name | Light | Dark |
|---|---|---|
| default | color/blue/600 | color/blue/300 |
| hover | color/blue/700 | color/blue/200 |
| pressed | color/blue/800 | color/blue/100 |
| disabled | color/blue/600 | color/blue/300 |

---

## Form-control / border

| Name | Light | Dark |
|---|---|---|
| default | color/gray/400 | color/gray/500 |

## Form-control / background / checked

| Name | Light | Dark |
|---|---|---|
| default | color/blue/500 | color/blue/500 |
| hover | color/blue/600 | color/blue/600 |
| pressed | color/blue/700 | color/blue/700 |

## Form-control / background / unchecked

| Name | Light | Dark |
|---|---|---|
| default | color/alpha/gray/gray-40 | color/alpha/gray/gray-5 |
| hover | color/alpha/gray/gray-90 | color/alpha/gray/gray-15 |
| pressed | color/alpha/gray/gray-20 | color/alpha/gray/gray-25 |

## Input / Background

| Name | Light | Dark |
|---|---|---|
| default | color/alpha/gray/gray-40 | color/alpha/black/black-40 |
| hover | color/alpha/gray/gray-15 | color/alpha/black/black-50 |

## Input / Border

| Name | Light | Dark |
|---|---|---|
| active | color/blue/500 | color/blue/400 |

---

## Slider

| Name | Light | Dark |
|---|---|---|
| text | color/gray/0 | color/gray/0 |

## Slider / Knob / fill

| Name | Light | Dark |
|---|---|---|
| default | color/blue/600 | color/blue/500 |
| highlight | color/alpha/brand/brand-glow-65 | color/alpha/brand/brand-glow-65 |
| unselected | color/gray/300 | color/gray/600 |
| hover | color/blue/600 | color/blue/500 |
| pressed | color/blue/700 | color/blue/600 |

## Slider / Text

| Name | Light | Dark |
|---|---|---|
| title | **text/primary** (semantic) | **text/primary** (semantic) |
| label | **text/secondary** (semantic) | **text/secondary** (semantic) |

---

## Scrollbar

| Name | Light | Dark |
|---|---|---|
| text | FFFFFF (raw hex) | FFFFFF (raw hex) |

## Scrollbar / Background

| Name | Light | Dark |
|---|---|---|
| default | color/gray/400 | color/gray/600 |
| hover | color/gray/500 | color/gray/700 |

---

## Segmented control

| Name | Light | Dark |
|---|---|---|
| text | FFFFFF (raw hex) | FFFFFF (raw hex) |

## Segmented control / Background

| Name | Light | Dark |
|---|---|---|
| segment-track | color/alpha/white/white-40 | color/alpha/black/black-70 |
| selected | color/blue/600 | color/blue/600 |
| default | Transparent | Transparent |
| hover | color/alpha/brand/brand-6 | color/alpha/brand/brand-10 |
| pressed | color/alpha/brand/brand-10 | color/alpha/brand/brand-16 |

---

## Steering / background

| Name | Light | Dark |
|---|---|---|
| track | color/gray/200 | color/gray/600 |
| knob | color/gray/0 | color/gray/700 |

---

## Tooltip

| Name | Light | Dark |
|---|---|---|
| text | color/gray/0 | color/gray/0 |

## Tooltip / Background

| Name | Light | Dark |
|---|---|---|
| default | **background/surface-inverse** (semantic) | **background/surface-inverse** (semantic) |

## Tooltip / Stroke

| Name | Light | Dark |
|---|---|---|
| default | color/gray/600 | color/gray/150 |

---

## Notes / to verify before applying to code

1. **Some semantics reference other semantics, not primitives** (alias-to-semantic):
   - `tooltip/background/default` → `background/surface-inverse`
   - `slider/text/title` → `text/primary`; `slider/text/label` → `text/secondary`
   These should chain (`{StaticResource background/surface-inverse}`) rather than duplicate a primitive.
2. **Raw hex, not a token:** `scrollbar/text` and `segmented-control/text` = `FFFFFF`. Likely should be a token (e.g. `text/on-brand` / `text/inverse`) — confirm intent.
3. **Figma spelling kept verbatim:** `background/surface-transparant` (missing the second "e"). Keep as-is per your "match the source exactly" rule, or fix — your call.
4. **Primitive coverage: complete — nothing missing.** Every primitive these semantics
   reference already exists in `EtherPrimitives.xaml`: the full blue ramp (incl.
   blue/100·200·300·1000), the alpha black/white/gray/navy/brand/status ramps, and
   gray/150·950·975. No primitives need adding.
5. **Existing EtherColors.xaml** already defines a PascalCase semantic layer (TextPrimary, BackgroundCanvas, ActionPrimaryBg, …). This intake is the Figma-authoritative version; applying it means replacing/aligning that layer with these slash keys + these exact Light/Dark mappings (to be decided when we move from intake to code).
