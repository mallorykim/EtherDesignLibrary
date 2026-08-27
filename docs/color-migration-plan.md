# Color Token Migration Plan — Legacy PascalCase → Figma slash keys

Source of truth: `src/Ether.DesignSystem.Foundation/Resources/Tokens/EtherColors.xaml` (Light theme dictionary +
mode-invariant section define the 104 legacy keys; the Figma slash-key layer,
already present in the same file per theme dictionary, is the migration target).
Reference counts were grepped across `src/Ether.DesignSystem.Controls/Controls/**` and `samples/Ether.DesignSystem.Gallery/Views/**`
(`.xaml` + `.xaml.cs`/`.cs`), matching `{ThemeResource <Key>}` / `{StaticResource <Key>}`.

Legend: **SAME** = new slash key resolves to the identical primitive as the legacy
key in that theme. **CHANGES** = new key resolves to a different primitive (shown
as `Old → New`); adopting the slash key is a visual change in that theme.
**ORPHAN** = no Figma slash key covers this role; legacy key must be kept.

Total legacy keys: **104** → A (identical, safe) **44** · B (orphan) **16** · C (maps, value changes) **44**.

---

## Full mapping table

### Background

| Legacy key | Refs | Files | → New key / ORPHAN | Light | Dark |
|---|---|---|---|---|---|
| BackgroundCanvas | 5 | EtherIntelligenceButton.xaml(3), ComponentPage.xaml(1), HomePage.xaml(1) | background/canvas | SAME (Gray0) | CHANGES (Gray1000→Gray850) |
| BackgroundSurface | 3 | ComponentPage.xaml(2), HomePage.xaml(1) | background/surface | SAME (Gray0) | SAME (Gray900) |
| BackgroundSurfaceRaised | 8 | ScrollBarPage.xaml(6), MastheadPage.xaml(2) | background/surface-raised | SAME (Gray25) | CHANGES (Gray800→Gray750) |
| BackgroundSurfaceSunken | 0 (unused) | — | background/surface-sunken | SAME (Gray50) | SAME (Gray975) |
| BackgroundSurfaceMuted | 1 | PrimitiveBadge.xaml(1) | background/surface-muted | SAME (Gray75) | CHANGES (Gray850→Gray700) |
| BackgroundSurfaceInverse | 0 (unused) | — | background/surface-inverse | SAME (Gray1000) | SAME (Gray0) |
| BackgroundInteractionModel | 0 (unused) | — | **ORPHAN** | — | — |
| BackgroundOverlayScrim | 0 (unused) | — | background/overlay-scrim | CHANGES (Gray1000→AlphaBlack60) | CHANGES (Gray1000→AlphaBlack70) |
| BackgroundGlassLight | 0 (unused) | — | background/glass-light | CHANGES (Gray0→AlphaWhite70) | CHANGES (Gray0→AlphaWhite8) |
| BackgroundGlassDark | 0 (unused) | — | background/glass-dark | CHANGES (Gray1000→AlphaBlack40) | CHANGES (Gray1000→AlphaBlack60) |
| BackgroundBrandSubtle | 0 (unused) | — | background/brand-subtle | CHANGES (Blue600→AlphaBrand10) | CHANGES (Blue600→AlphaBrand16) |
| BackgroundDivider | 0 (unused) | — | background/divider | SAME (Gray300) | CHANGES (Gray0→AlphaWhite16) |
| BackgroundPlaceholder | 0 (unused) | — | background/placeholder | SAME (Gray200) | SAME (Gray700) |
| BackgroundTrack | 2 | EtherProgressBar.xaml(1), EtherSteeringBar.xaml(1) | background/track | SAME (Gray300) | SAME (Gray750) |
| BackgroundDropdownScrollThumb | 4 | EtherScrollBar.xaml(2), DropdownPage.xaml(1), ScrollBarPage.xaml(1) | **ORPHAN** | — | — |
| BackgroundDropdownScrollThumbHover | 3 | EtherScrollBar.xaml(2), ScrollBarPage.xaml(1) | **ORPHAN** | — | — |
| BackgroundSwitchOff | 1 | EtherSwitch.xaml(1) | **ORPHAN** | — | — |
| BackgroundSwitchKnobDisabled | 2 | EtherSwitch.xaml(2) | **ORPHAN** | — | — |
| BackgroundSegmentTrack | 1 | EtherSegmentedControl.xaml(1) | segmented-control/background/segment-track | CHANGES (Gray25→AlphaWhite40) | CHANGES (Gray0→AlphaBlack70) |
| BackgroundSegmentHover | 1 | EtherSegmentedControl.xaml(1) | segmented-control/background/hover | CHANGES (Blue600→AlphaBrand6) | CHANGES (Gray0→AlphaBrand10) |
| BackgroundSegmentPressed | 1 | EtherSegmentedControl.xaml(1) | segmented-control/background/pressed | CHANGES (Blue600→AlphaBrand10) | CHANGES (Gray0→AlphaBrand16) |
| BackgroundAiInput | 0 (unused) | — | background/ai-input | CHANGES (Gray300→Gray200) | CHANGES (Gray0→Gray900) |
| BackgroundDropdown | 2 | EtherDropdown.xaml(2) | background/dropdown/default | CHANGES (Gray1000→Gray75) | CHANGES (Gray0→Gray950) |
| BackgroundDropdownHover | 1 | EtherDropdown.xaml(1) | background/dropdown/hover | CHANGES (Gray1000→Gray100) | CHANGES (Gray0→Gray900) |
| BackgroundDropdownPressed | 1 | EtherDropdown.xaml(1) | background/dropdown/pressed | CHANGES (Gray1000→Gray150) | CHANGES (Gray0→Gray850) |
| BackgroundDropdownMenu | 2 | EtherDropdown.xaml(1), DropdownPage.xaml(1) | **ORPHAN** | — | — |
| BackgroundDropdownItemSelected | 3 | EtherDropdown.xaml(3) | **ORPHAN** | — | — |
| BackgroundDropdownItemSelectedHover | 3 | EtherDropdown.xaml(2), DropdownPage.xaml(1) | **ORPHAN** | — | — |
| BackgroundDropdownItemSelectedPressed | 3 | EtherDropdown.xaml(2), DropdownPage.xaml(1) | **ORPHAN** | — | — |

### Text

| Legacy key | Refs | Files | → New key / ORPHAN | Light | Dark |
|---|---|---|---|---|---|
| TextPrimary | 49 | EtherInput/ProgressBar/Slider/SteeringBar/ColorSwatch.xaml(1 ea), EtherMasthead.xaml(10), ColorsPage.xaml(1), IconsPage.xaml(2), RadiusPage.xaml(4), ScrollBarPage.xaml(11), SpacingPage.xaml(4), TypographyPage.xaml(7), MastheadPage.xaml(2), ComponentPage.xaml(1), HomePage.xaml(2) | text/primary | SAME (Gray1000) | SAME (Gray0) |
| TextSecondary | 26 | EtherDropdown.xaml(3), EtherIntelligenceButton.xaml(2), EtherProgressBar.xaml(1), EtherSegmentedControl.xaml(1), EtherSteeringBar.xaml(1), EtherSwitch.xaml(2), PrimitiveBadge.xaml(1), DropdownPage.xaml(1), SteeringBarPage.xaml(1), IconsPage.xaml(2), RadiusPage.xaml(2), SpacingPage.xaml(2), TypographyPage.xaml(2), CardPage.xaml(3), ComponentPage.xaml(1), HomePage.xaml(1) | text/secondary | CHANGES (Gray1000→AlphaBlack70) | CHANGES (Gray0→AlphaWhite70) |
| TextTertiary | 120 | EtherInput.xaml(1), ColorSwatch.xaml(1), ButtonPage.xaml(42), CheckboxPage.xaml(9), DropdownPage.xaml(8), InputPage.xaml(5), IntelligenceButtonPage.xaml(6), RadioButtonPage.xaml(9), SegmentedControlPage.xaml(7), SliderPage.xaml(3), SteeringBarPage.xaml(6), ToggleSwitchPage.xaml(10), ProgressBarPage.xaml(4), IconsPage.xaml(1), ScrollBarPage.xaml(4), CardPage.xaml(1), ComponentPage.xaml(2), HomePage.xaml(1) | text/tertiary | CHANGES (Gray1000→AlphaBlack50) | CHANGES (Gray0→AlphaWhite50) |
| TextDisabled | 0 (unused) | — | text/disabled | CHANGES (Gray1000→AlphaBlack30) | CHANGES (Gray0→AlphaWhite30) |
| TextInverse | 0 (unused) | — | text/inverse | SAME (Gray0) | SAME (Gray1000) |
| TextBrand | 6 | IconsPage.xaml(1), RadiusPage.xaml(2), SpacingPage.xaml(2), TypographyPage.xaml(1) | text/brand | SAME (Blue600) | SAME (Blue500) |
| TextSuccess | 0 (unused) | — | text/success | SAME (Green600) | SAME (Green400) |
| TextWarning | 0 (unused) | — | text/warning | SAME (Orange600) | SAME (Orange300) |
| TextDanger | 0 (unused) | — | text/danger | SAME (Red500) | SAME (Red300) |
| TextAi | 0 (unused) | — | text/ai | SAME (Purple500) | SAME (Purple400) |
| TextNavNarrowInactive | 0 (unused) | — | text/nav-narrow-inactive | CHANGES (Gray1000→AlphaBlack60) | SAME (Gray0) |
| TextNavInactive | 0 (unused) | — | text/nav-inactive | CHANGES (Gray900→AlphaBlack50) | CHANGES (Gray0→AlphaWhite70) |

### Icon

| Legacy key | Refs | Files | → New key / ORPHAN | Light | Dark |
|---|---|---|---|---|---|
| IconPrimary | 2 | IconsPage.xaml(2) | icon/primary | SAME (Gray1000) | SAME (Gray0) |
| IconSecondary | 0 (unused) | — | icon/secondary | CHANGES (Gray1000→AlphaBlack70) | CHANGES (Gray0→AlphaWhite70) |
| IconTertiary | 0 (unused) | — | icon/tertiary | CHANGES (Gray1000→AlphaBlack50) | CHANGES (Gray0→AlphaWhite50) |
| IconDisabled | 0 (unused) | — | icon/disabled | CHANGES (Gray1000→AlphaBlack30) | CHANGES (Gray0→AlphaWhite30) |
| IconInverse | 0 (unused) | — | icon/inverse | SAME (Gray0) | SAME (Gray1000) |
| IconBrand | 0 (unused) | — | icon/brand | SAME (Blue600) | SAME (Blue500) |
| IconAi | 0 (unused) | — | icon/ai | SAME (Purple500) | SAME (Purple400) |

### Border

| Legacy key | Refs | Files | → New key / ORPHAN | Light | Dark |
|---|---|---|---|---|---|
| BorderSubtle | 12 | IconsPage.xaml(1), RadiusPage.xaml(2), ScrollBarPage.xaml(2), SpacingPage.xaml(2), TypographyPage.xaml(2), MastheadPage.xaml(2), HomePage.xaml(1) | border/subtle | CHANGES (Navy500→AlphaNavy12) | CHANGES (Gray0→AlphaWhite8) |
| BorderDefault | 3 | EtherInput.xaml(3) | border/default | CHANGES (Navy500→AlphaNavy20) | CHANGES (Gray0→AlphaWhite12) |
| BorderStrong | 0 (unused) | — | border/strong | CHANGES (Navy500→AlphaNavy40) | CHANGES (Gray0→AlphaWhite25) |
| BorderFocus | 8 | EtherButton.xaml(3), EtherDropdown.xaml(1), EtherInput.xaml(1), EtherSegmentedControl.xaml(1), EtherSwitch.xaml(1), EtherMasthead.xaml(1) | border/focus | SAME (Blue600) | SAME (Blue500) |
| BorderBrand | 0 (unused) | — | border/brand | SAME (Blue600) | SAME (Blue500) |
| BorderInverse | 0 (unused) | — | border/inverse | SAME (Gray1000) | SAME (Gray0) |
| BorderDanger | 0 (unused) | — | border/danger | SAME (Red500) | SAME (Red400) |
| BorderSuccess | 0 (unused) | — | border/success | SAME (Green500) | SAME (Green400) |

### Action / Primary, Secondary, Tertiary, Danger

| Legacy key | Refs | Files | → New key / ORPHAN | Light | Dark |
|---|---|---|---|---|---|
| ActionPrimaryBg | 2 | EtherButton.xaml(1), EtherInput.xaml(1) | action/primary/bg | SAME (Blue600) | SAME (Blue600) |
| ActionPrimaryBgHover | 1 | EtherButton.xaml(1) | action/primary/bg-hover | CHANGES (Blue700→Blue500) | CHANGES (Blue700→Blue500) |
| ActionPrimaryBgPressed | 1 | EtherButton.xaml(1) | action/primary/bg-pressed | CHANGES (Blue800→Blue700) | CHANGES (Blue800→Blue700) |
| ActionPrimaryBgDisabled | 1 | EtherButton.xaml(1) | action/primary/bg-disabled | CHANGES (Blue600→AlphaBlack10) | CHANGES (Blue600→AlphaWhite10) |
| ActionPrimaryFg | 2 | EtherButton.xaml(2) | action/primary/fg | SAME (Gray0) | SAME (Gray0) |
| ActionPrimaryFgDisabled | 2 | EtherButton.xaml(2) | action/primary/fg-disabled | CHANGES (Gray0→AlphaBlack30) | CHANGES (Gray0→AlphaWhite30) |
| ActionSecondaryBg | 1 | EtherButton.xaml(1) | action/secondary/bg | CHANGES (Gray0→AlphaBlack0) | CHANGES (Gray0→AlphaWhite0) |
| ActionSecondaryBgHover | 2 | EtherButton.xaml(1), EtherMasthead.xaml(1) | action/secondary/bg-hover | CHANGES (Gray25→AlphaBlack6) | CHANGES (Gray750→AlphaWhite8) |
| ActionSecondaryBgPressed | 2 | EtherButton.xaml(1), EtherMasthead.xaml(1) | action/secondary/bg-pressed | CHANGES (Gray50→AlphaBlack10) | CHANGES (Gray800→AlphaWhite12) |
| ActionSecondaryBorder | 1 | EtherButton.xaml(1) | action/secondary/border | CHANGES (Navy500→AlphaNavy20) | CHANGES (Gray0→AlphaWhite20) |
| ActionSecondaryFg | 2 | EtherButton.xaml(2) | action/secondary/fg | SAME (Gray1000) | SAME (Gray0) |
| ActionTertiaryBgHover | 0 (unused) | — | action/tertiary/bg-hover | CHANGES (Gray1000→AlphaBlack4) | CHANGES (Gray0→AlphaWhite4) |
| ActionTertiaryFg | 2 | EtherButton.xaml(2) | action/tertiary/fg | CHANGES (Blue800→Blue600) | CHANGES (Blue800→Blue500) |
| ActionTertiaryFgHover | 2 | EtherButton.xaml(2) | **ORPHAN** | — | — |
| ActionTertiaryFgPressed | 2 | EtherButton.xaml(2) | **ORPHAN** | — | — |
| ActionTertiaryFgDisabled | 0 (unused) | — | **ORPHAN** | — | — |
| ActionDangerBg | 0 (unused) | — | action/danger/bg | SAME (Red500) | SAME (Red500) |
| ActionDangerBgHover | 0 (unused) | — | action/danger/bg-hover | SAME (Red600) | SAME (Red400) |
| ActionDangerFg | 0 (unused) | — | action/danger/fg | SAME (Gray0) | SAME (Gray0) |

### Status / Success, Warning, Danger, Info

All Status keys are currently **unused (0 refs)** in Controls/Views.

| Legacy key | → New key | Light | Dark |
|---|---|---|---|
| StatusSuccessFg | status/success/fg | SAME (Green600) | SAME (Green400) |
| StatusSuccessBg | status/success/bg | CHANGES (Green500→AlphaSuccess12) | CHANGES (Green500→AlphaSuccess12) |
| StatusSuccessBorder | status/success/border | SAME (Green500) | SAME (Green400) |
| StatusWarningFg | status/warning/fg | SAME (Orange600) | SAME (Orange300) |
| StatusWarningBg | status/warning/bg | CHANGES (Orange300→AlphaWarning20) | CHANGES (Orange300→AlphaWarning16) |
| StatusWarningBorder | status/warning/border | SAME (Orange500) | SAME (Orange300) |
| StatusDangerFg | status/danger/fg | SAME (Red500) | SAME (Red300) |
| StatusDangerBg | status/danger/bg | CHANGES (Red400→AlphaDanger12) | CHANGES (Red400→AlphaDanger16) |
| StatusDangerBorder | status/danger/border | SAME (Red500) | SAME (Red400) |
| StatusInfoFg | status/info/fg | SAME (Blue600) | SAME (Blue500) |
| StatusInfoBg | status/info/bg | CHANGES (Blue600→AlphaBrand10) | CHANGES (Blue600→AlphaBrand16) |
| StatusInfoBorder | status/info/border | SAME (Blue600) | SAME (Blue500) |

### AI accents

| Legacy key | Refs | Files | → New key | Light | Dark |
|---|---|---|---|---|---|
| AiAccentPurple | 1 | EtherIntelligenceButton.xaml(1) | ai/accent-purple | SAME (Purple500) | SAME (Purple400) |
| AiAccentBlue | 0 (unused) | — | ai/accent-blue | SAME (Blue600) | SAME (Blue500) |

### Mode-invariant section

| Legacy key | Refs | Files | → New key / ORPHAN | Light | Dark |
|---|---|---|---|---|---|
| BackgroundCardIntelligence | 0 (unused) | — | background/card-intelligence | CHANGES (Gray1000→Gray975) | SAME (Gray1000) |
| BackgroundBrand | 5 | EtherSegmentedControl.xaml(1), RadiusPage.xaml(2), SpacingPage.xaml(2) | background/brand | SAME (Blue600) | SAME (Blue600) |
| BackgroundBrandSoft | 0 (unused) | — | background/brand-soft | CHANGES (Blue600→AlphaBrand60) | CHANGES (Blue600→AlphaBrand60) |
| BackgroundBrandGlow | 0 (unused) | — | background/brand-glow | CHANGES (Blue500→AlphaBrandGlow65) | CHANGES (Blue500→AlphaBrandGlow65) |
| TextOnBrand | 3 | EtherSegmentedControl.xaml(3) | text/on-brand | SAME (Gray0) | SAME (Gray0) |
| IconOnBrand | 0 (unused) | — | icon/on-brand | SAME (Gray0) | SAME (Gray0) |
| AiAccentCyan | 0 (unused) | — | ai/accent-cyan | SAME (Cyan500) | SAME (Cyan500) |
| BorderIntelligenceBlue (gradient) | 1 | EtherIntelligenceButton.xaml(1) | **ORPHAN** | — | — |
| BorderIntelligence (gradient) | 1 | EtherIntelligenceButton.xaml(1) | **ORPHAN** | — | — |
| BorderIntelligenceHover (gradient) | 2 | EtherIntelligenceButton.xaml(2) | **ORPHAN** | — | — |
| BorderDropdownActive (gradient) | 1 | EtherDropdown.xaml(1) | **ORPHAN** | — | — |
| Dataviz01 | 0 (unused) | — | dataviz/01 | CHANGES (Navy700→Blue1000) | CHANGES (Navy700→Blue1000) |
| Dataviz02 | 0 (unused) | — | dataviz/02 | SAME (Blue950) | SAME (Blue950) |
| Dataviz03 | 0 (unused) | — | dataviz/03 | SAME (Blue600) | SAME (Blue600) |
| Dataviz04 | 0 (unused) | — | dataviz/04 | SAME (Cyan500) | SAME (Cyan500) |

---

## A. Direct migrate, value identical (safe) — 44 keys

Swapping the legacy key for its slash-key equivalent produces **no visual change**
in either theme (every row below is SAME/SAME in the full table). Split by whether
the key is actually referenced today.

**Referenced (12):** BackgroundSurface, BackgroundTrack, TextPrimary, TextBrand,
IconPrimary, BorderFocus, ActionPrimaryBg, ActionPrimaryFg, ActionSecondaryFg,
AiAccentPurple, BackgroundBrand, TextOnBrand.

**Unused (32):** BackgroundSurfaceSunken, BackgroundSurfaceInverse,
BackgroundPlaceholder, TextInverse, TextSuccess, TextWarning, TextDanger, TextAi,
IconInverse, IconBrand, IconAi, BorderBrand, BorderInverse, BorderDanger,
BorderSuccess, ActionDangerBg, ActionDangerBgHover, ActionDangerFg,
StatusSuccessFg, StatusSuccessBorder, StatusWarningFg, StatusWarningBorder,
StatusDangerFg, StatusDangerBorder, StatusInfoFg, StatusInfoBorder, AiAccentBlue,
IconOnBrand, AiAccentCyan, Dataviz02, Dataviz03, Dataviz04.

---

## B. ORPHANS — 16 keys (no Figma slash key covers this role; keep legacy key)

All 16 candidates named in the migration brief were confirmed as true orphans —
no generic or specials-list slash key matches any of them:

1. **BackgroundInteractionModel** — unused (0 refs). No `background/interaction-model` in Figma set.
2. **BackgroundDropdownScrollThumb** — 4 refs (EtherScrollBar.xaml×2, DropdownPage.xaml, ScrollBarPage.xaml). No scrollbar-thumb slash key; `scrollbar/background/*` exists but is a different role (track, not thumb).
3. **BackgroundDropdownScrollThumbHover** — 3 refs (EtherScrollBar.xaml×2, ScrollBarPage.xaml). Same reasoning as above.
4. **BackgroundSwitchOff** — 1 ref (EtherSwitch.xaml). No `switch/*` group in the Figma slash set; `form-control/background/unchecked/*` is a distinct role/value, not a rename.
5. **BackgroundSwitchKnobDisabled** — 2 refs (EtherSwitch.xaml). Same reasoning.
6. **BackgroundDropdownMenu** — 2 refs (EtherDropdown.xaml, DropdownPage.xaml). Neither `background/dropdown/*` (item states) nor `background/menuitem/*` (item states) represents the dropdown *panel* background.
7. **BackgroundDropdownItemSelected** — 3 refs (EtherDropdown.xaml×3). No "selected" state in `background/dropdown/*` or `background/menuitem/*` (only default/hover/pressed).
8. **BackgroundDropdownItemSelectedHover** — 3 refs (EtherDropdown.xaml×2, DropdownPage.xaml). Same reasoning.
9. **BackgroundDropdownItemSelectedPressed** — 3 refs (EtherDropdown.xaml×2, DropdownPage.xaml). Same reasoning.
10. **ActionTertiaryFgHover** — 2 refs (EtherButton.xaml×2). `action/tertiary/*` only defines `bg-hover` and `fg`; no `fg-hover`.
11. **ActionTertiaryFgPressed** — 2 refs (EtherButton.xaml×2). No `fg-pressed` in `action/tertiary/*`.
12. **ActionTertiaryFgDisabled** — unused (0 refs). No `fg-disabled` in `action/tertiary/*`.
13. **BorderIntelligenceBlue** (RadialGradientBrush) — 1 ref (EtherIntelligenceButton.xaml). No gradient-brush slash keys exist at all in the Figma layer.
14. **BorderIntelligence** (RadialGradientBrush) — 1 ref (EtherIntelligenceButton.xaml). Same.
15. **BorderIntelligenceHover** (LinearGradientBrush) — 2 refs (EtherIntelligenceButton.xaml×2). Same.
16. **BorderDropdownActive** (LinearGradientBrush) — 1 ref (EtherDropdown.xaml). Same.

**13 of 16 orphans have live references** (only BackgroundInteractionModel and
ActionTertiaryFgDisabled are unused) — these keys cannot be deleted when the
PascalCase layer is eventually retired; they need either a new Figma token to be
authored, or a permanent carve-out.

---

## C. Maps but VALUE CHANGES — 44 keys (migrating adopts the Figma value = visual change)

Format: `Legacy → new key : Light old→new | Dark old→new`

1. BackgroundCanvas → background/canvas : Light SAME (Gray0) | Dark Gray1000→Gray850
2. BackgroundSurfaceRaised → background/surface-raised : Light SAME (Gray25) | Dark Gray800→Gray750
3. BackgroundSurfaceMuted → background/surface-muted : Light SAME (Gray75) | Dark Gray850→Gray700
4. BackgroundOverlayScrim → background/overlay-scrim : Light Gray1000→AlphaBlack60 | Dark Gray1000→AlphaBlack70
5. BackgroundGlassLight → background/glass-light : Light Gray0→AlphaWhite70 | Dark Gray0→AlphaWhite8
6. BackgroundGlassDark → background/glass-dark : Light Gray1000→AlphaBlack40 | Dark Gray1000→AlphaBlack60
7. BackgroundBrandSubtle → background/brand-subtle : Light Blue600→AlphaBrand10 | Dark Blue600→AlphaBrand16
8. BackgroundDivider → background/divider : Light SAME (Gray300) | Dark Gray0→AlphaWhite16
9. BackgroundSegmentTrack → segmented-control/background/segment-track : Light Gray25→AlphaWhite40 | Dark Gray0→AlphaBlack70
10. BackgroundSegmentHover → segmented-control/background/hover : Light Blue600→AlphaBrand6 | Dark Gray0→AlphaBrand10
11. BackgroundSegmentPressed → segmented-control/background/pressed : Light Blue600→AlphaBrand10 | Dark Gray0→AlphaBrand16
12. BackgroundAiInput → background/ai-input : Light Gray300→Gray200 | Dark Gray0→Gray900
13. BackgroundDropdown → background/dropdown/default : Light Gray1000→Gray75 | Dark Gray0→Gray950
14. BackgroundDropdownHover → background/dropdown/hover : Light Gray1000→Gray100 | Dark Gray0→Gray900
15. BackgroundDropdownPressed → background/dropdown/pressed : Light Gray1000→Gray150 | Dark Gray0→Gray850
16. TextSecondary → text/secondary : Light Gray1000→AlphaBlack70 | Dark Gray0→AlphaWhite70
17. TextTertiary → text/tertiary : Light Gray1000→AlphaBlack50 | Dark Gray0→AlphaWhite50
18. TextDisabled → text/disabled : Light Gray1000→AlphaBlack30 | Dark Gray0→AlphaWhite30
19. TextNavNarrowInactive → text/nav-narrow-inactive : Light Gray1000→AlphaBlack60 | Dark SAME (Gray0)
20. TextNavInactive → text/nav-inactive : Light Gray900→AlphaBlack50 | Dark Gray0→AlphaWhite70
21. IconSecondary → icon/secondary : Light Gray1000→AlphaBlack70 | Dark Gray0→AlphaWhite70
22. IconTertiary → icon/tertiary : Light Gray1000→AlphaBlack50 | Dark Gray0→AlphaWhite50
23. IconDisabled → icon/disabled : Light Gray1000→AlphaBlack30 | Dark Gray0→AlphaWhite30
24. BorderSubtle → border/subtle : Light Navy500→AlphaNavy12 | Dark Gray0→AlphaWhite8
25. BorderDefault → border/default : Light Navy500→AlphaNavy20 | Dark Gray0→AlphaWhite12
26. BorderStrong → border/strong : Light Navy500→AlphaNavy40 | Dark Gray0→AlphaWhite25
27. ActionPrimaryBgHover → action/primary/bg-hover : Light Blue700→Blue500 | Dark Blue700→Blue500
28. ActionPrimaryBgPressed → action/primary/bg-pressed : Light Blue800→Blue700 | Dark Blue800→Blue700
29. ActionPrimaryBgDisabled → action/primary/bg-disabled : Light Blue600→AlphaBlack10 | Dark Blue600→AlphaWhite10
30. ActionPrimaryFgDisabled → action/primary/fg-disabled : Light Gray0→AlphaBlack30 | Dark Gray0→AlphaWhite30
31. ActionSecondaryBg → action/secondary/bg : Light Gray0→AlphaBlack0 | Dark Gray0→AlphaWhite0
32. ActionSecondaryBgHover → action/secondary/bg-hover : Light Gray25→AlphaBlack6 | Dark Gray750→AlphaWhite8
33. ActionSecondaryBgPressed → action/secondary/bg-pressed : Light Gray50→AlphaBlack10 | Dark Gray800→AlphaWhite12
34. ActionSecondaryBorder → action/secondary/border : Light Navy500→AlphaNavy20 | Dark Gray0→AlphaWhite20
35. ActionTertiaryBgHover → action/tertiary/bg-hover : Light Gray1000→AlphaBlack4 | Dark Gray0→AlphaWhite4
36. ActionTertiaryFg → action/tertiary/fg : Light Blue800→Blue600 | Dark Blue800→Blue500
37. StatusSuccessBg → status/success/bg : Light Green500→AlphaSuccess12 | Dark Green500→AlphaSuccess12
38. StatusWarningBg → status/warning/bg : Light Orange300→AlphaWarning20 | Dark Orange300→AlphaWarning16
39. StatusDangerBg → status/danger/bg : Light Red400→AlphaDanger12 | Dark Red400→AlphaDanger16
40. StatusInfoBg → status/info/bg : Light Blue600→AlphaBrand10 | Dark Blue600→AlphaBrand16
41. BackgroundCardIntelligence → background/card-intelligence : Light Gray1000→Gray975 | Dark SAME (Gray1000)
42. BackgroundBrandSoft → background/brand-soft : Light Blue600→AlphaBrand60 | Dark Blue600→AlphaBrand60
43. BackgroundBrandGlow → background/brand-glow : Light Blue500→AlphaBrandGlow65 | Dark Blue500→AlphaBrandGlow65
44. Dataviz01 → dataviz/01 : Light Navy700→Blue1000 | Dark Navy700→Blue1000

**22 of the 44 C-list keys are live in components today** and need explicit visual
sign-off before migrating (adopting the new value changes what ships):
BackgroundCanvas, BackgroundSurfaceRaised, BackgroundSurfaceMuted,
BackgroundSegmentTrack, BackgroundSegmentHover, BackgroundSegmentPressed,
BackgroundDropdown, BackgroundDropdownHover, BackgroundDropdownPressed,
TextSecondary, TextTertiary, BorderSubtle, BorderDefault, ActionPrimaryBgHover,
ActionPrimaryBgPressed, ActionPrimaryBgDisabled, ActionPrimaryFgDisabled,
ActionSecondaryBg, ActionSecondaryBgHover, ActionSecondaryBgPressed,
ActionSecondaryBorder, ActionTertiaryFg.

**The other 22 are currently unused (0 refs)** — the value change only matters
once/if something starts referencing them: BackgroundOverlayScrim,
BackgroundGlassLight, BackgroundGlassDark, BackgroundBrandSubtle,
BackgroundDivider, BackgroundAiInput, TextDisabled, TextNavNarrowInactive,
TextNavInactive, IconSecondary, IconTertiary, IconDisabled, BorderStrong,
ActionTertiaryBgHover, StatusSuccessBg, StatusWarningBg, StatusDangerBg,
StatusInfoBg, BackgroundCardIntelligence, BackgroundBrandSoft, BackgroundBrandGlow,
Dataviz01.
