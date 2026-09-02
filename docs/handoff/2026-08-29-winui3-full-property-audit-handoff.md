# WinUI 3 Design Library: Full-Property Consumable Audit Handoff

**Handoff date:** 2026-08-29 (this document has been revised, see §10 revision log; revised again on 2026-08-30, adding §4.7 and §11, see §10 items 5-6)
**Audited branch:** `codex/refine-components`
**Final status:** Passed (the full property-code, visual, component-DP backend-consumption, and WinUI 3 convention gates, plus the newly added High Contrast foreground/background pairing gate), with the visual evidence distribution now verified to be reproducibly deterministic across runs
**Primary evidence:** `artifacts/audit-runs/consumer-runtime-evidence-20260830-002407286/` (two consecutive independent full runs against the final code state after the 2026-08-30 revision — `...20260830-001800579`, `...20260830-002407286` — with per-property evidence Method fully consistent; see §4.7 and §6). At the time the document was finalized on 2026-08-29, there were also five independent runs consistent with each other, recorded pre-revision (`...230136589`, `...230519527`, `...224425998`, `...224946229`, `...225342824`) — the code state of those five runs has since been superseded by the sensitivity regression described in §4.7, so they are now kept only as historical archive and no longer represent the current branch state.

## 1. Handoff Conclusion

This round completed the full-property consumable acceptance review of the Ether WinUI 3 design component library. Verification was not performed via a source project reference; instead, `Ether.DesignSystem.Foundation`, `Ether.DesignSystem.Controls`, and `Ether.DesignSystem.Interactions` were packaged first, then restored and loaded by an independent unpackaged consumer and executed.

The final run is marked `success`, and proves:

| Acceptance item | Result | Method of proof |
| --- | ---: | --- |
| Public writable properties | 1,501 / 1,501 | Per-item CLR getter on a real control instance; per-item setter on a detached instance of the same type — see §4.2 for the precise wording |
| Visual properties | 482 / 482 | Attached to the real WinUI visual tree, changed, laid out, `RenderTargetBitmap` rendered, with per-item evidence |
| Ether-owned DPs | 37 / 37 | `SetValue`, CLR getter, `GetValue`, change callback, JSON envelope |
| Standard interaction adapters | 12 / 12 | Standard WinUI business-interaction envelope |
| Control screenshot matrix | 13 controls × Light/Dark | 26 independent per-control PNGs, plus full-page Light/Dark/OS High Contrast PNGs |
| WinUI 3 convention gate | Passed | `Verify-WinUiConventions.ps1` |
| RTL, UIA, scaling, localization, high contrast | Passed | Consumer runtime gate |
| Formatting check | Passed | `git diff --check` |

## 2. Original Goal and Scope Boundary

User's request: audit the components refined on the AUDIT branch, ensure every control and every property can be invoked, read, and visually verified, and — where business semantics exist — can be listened to and consumed by the backend; the implementation and sample architecture should align with the official WinUI 3 conventions and the WinUI 3 Gallery.

"Full-property backend consumption" adopts the following necessary boundaries:

1. Ether-owned public DPs must be observable and produce a JSON-safe backend envelope.
2. Standard WinUI business state (click, text, selection, checked, on/off, range value) is consumed by the standard interaction adapters.
3. `FrameworkElement`/`UIElement`'s generic visual and layout properties must be settable, readable, attachable, laid out, and rendered per item, but are not uploaded to the backend by default; they carry no stable business meaning, and uploading them would leak UI implementation details.
4. Visual/layout configuration that needs to be recorded for business purposes can be explicitly observed via `ObserveProperty`, rather than serializing the entire UI object graph to the backend.

For the full rules and backend reliability requirements, see:

- [Audit specification](../architecture/2026-08-29-winui3-backend-consumable-component-audit.md)

## 3. Research and Benchmarking Content

### Official WinUI 3 Conventions

The audit and automated gates were implemented against the following Microsoft conventions:

- Custom bindable properties use `public static readonly DependencyProperty <Name>Property` and a same-named CLR `GetValue`/`SetValue` wrapper.
- Controls with a default template set `DefaultStyleKey` in the constructor.
- Controls that read template parts override `OnApplyTemplate()` and call `base.OnApplyTemplate()` first.
- XAML template parts and states are declared via `TemplatePart` and `TemplateVisualState`.
- Theme resources, high-contrast resources, UIA, RTL, and non-instant animation easing are all covered by automated checks.

References:

- [Microsoft Learn: Custom dependency properties](https://learn.microsoft.com/en-us/windows/apps/develop/platform/xaml/custom-dependency-properties)
- [Microsoft Learn: WinUI 3 templated controls](https://learn.microsoft.com/en-us/windows/apps/winui/winui3/xaml-templated-controls-csharp-winui-3)
- [Microsoft Learn: Control templates](https://learn.microsoft.com/en-us/windows/apps/develop/platform/xaml/xaml-control-templates)
- [Microsoft WinUI Gallery source](https://github.com/microsoft/WinUI-Gallery)

### WinUI 3 Gallery Benchmarking Method

The WinUI Gallery was used as the baseline for platform behavior, accessibility, and sample architecture — not as a substitute for Ether's visual tokens. Each control was checked at three layers:

| Layer | Performed this round |
| --- | --- |
| Platform behavior | DP, template, VisualState, UIA, RTL, keyboard/state convention gates |
| Gallery sample architecture | Independent samples, explicit states, AutomationProperties, XAML/code-behind readability |
| Ether visual spec | Light/Dark/OS High Contrast, LTR/RTL, Disabled/Pressed/Hover, screenshot and state assertions |

## 4. Execution Process

1. **Inventory and classification**: reflection enumerated the 1,501 public writable properties of the **13** audited types (`PublicPropertyInventoryTypes` in `RuntimeVerification.PublicPropertyInventory.cs`: `EtherButton`, `EtherCheckbox`, `EtherDropdown`, `EtherInput`, `EtherIntelligenceButton`, `EtherProgressBar`, `EtherRadioButton`, `EtherSegmentedControl`, `EtherSegmentedTrack`, `EtherSegmentPanel`, `EtherSlider`, `EtherSteeringBar`, `EtherMasthead`), and classified them into 482 visual, 73 semantic, and 946 platform properties.
2. **Code callability**: for every property, first execute the CLR getter on a real control instance (a single read, counted toward `getterReadCount`); then `TryInvokeOnUnattachedInstance` (`RuntimeVerification.FullPropertyCode.cs`) performs another `GetValue` followed by a `SetValue` on a **freshly created, detached instance of the same type** — what gets written back is **the same current value that was just read** (for most reference types this is `SetValue(null)`), not a new value different from the current one. This step verifies that "the property can be invoked at the code level (neither getter nor setter throws)," not that "the property accepts an arbitrary new value and takes effect"; the latter is covered by the attached visual/DP assertions in steps 3-5. This gate does not depend on design-time XAML or a project reference.
3. **Attached visual verification**: each visual property uses an independent control specimen, added to a real `Canvas`/`Border` visual tree, changed, re-laid-out, and fingerprinted with `RenderTargetBitmap`.
4. **Evidence hardening**: fixed test specimens that had previously been stretched in a way that masked alignment/min-max size issues; a 160×80 constraint baseline is now used so layout changes are visible. Fixed the previous `IsDropDownOpen` sample of `false -> false`, changing it to a genuinely opened state.
5. **Per-property evidence**: every visual property must carry one unique piece of evidence, using the following methods:
   - `pixel-difference`: the rendered bitmap changed;
   - `layout-difference`: the actual/desired size or the origin relative to the surface changed;
   - `visibility-transition`: a loaded control enters `Collapsed` then returns to `Visible` and renders;
   - `ether-component-dp-contract`: the runtime `GetValue` contract of an Ether-owned DP;
   - `platform-dp-contract`: the runtime `DependencyObject.GetValue` contract of a WinUI platform DP itself (e.g. `Control.BackgroundProperty`, `FrameworkElement.WidthProperty`);
   - `platform-clr-visual-contract`: **in theory**, reserved for the case of "WinUI exposes a writable visual CLR property, but there genuinely is no `DependencyProperty` identifier of any form (field or property)." After fixing `ResolveDependencyProperty` (see the "4.5 revision: DP-resolution defect" section below), the only member of this component set that falls into this bucket is `EtherMasthead.Scale` — verified via reflection against `Microsoft.UI.Xaml.UIElement`: this type has **no static fields at all** in this WinUI 3 metadata, and Composition pass-through properties such as `Scale`/`Rotation`/`RotationAxis` genuinely don't go through a `DependencyProperty`, so this is a real, verifiable classification, not a misjudgment.
6. **Backend consumption**: 37 Ether-owned DPs write, read, callback, and JSON-envelope; 12 standard interaction adapters emit business envelopes.
7. **Visual-regression artifacts**: in addition to the full-page Light/Dark/High Contrast images, added an independent Light/Dark screenshot matrix for 13 controls; the acceptance script enforces checks on control ID, file existence, positive dimensions, images not being truncated/empty, **and (newly added in this revision) each control's Light/Dark screenshot SHA256 must differ**, to prevent a control whose theme isn't actually propagated to the specific instance from producing two byte-identical images (a real regression in EtherScrollBar, see "4.6 revision: ScrollBar theme-propagation defect" below).
8. **Official convention and cross-platform gates**: ran `Verify-WinUiConventions.ps1`; the consumer runtime verification covers theme, OS High Contrast, RTL, UIA, 2.25x text scaling, localization, SVG, and resource recovery.

### 4.5 Revision: DP-Resolution Defect (fixed)

`ResolveDependencyProperty` in `RuntimeVerification.AttachedVisualProperties.cs` (whose original implementation only called `Type.GetField($"{propertyName}Property", ...)`) could only resolve DPs declared as a `public static readonly DependencyProperty XProperty` **field** — the pattern used by Ether's own controls. But WinUI 3 goes through the C#/WinRT projection, and the platform's own DP identifiers (`Control.BackgroundProperty`, `FrameworkElement.WidthProperty`, `Control.FontSizeProperty`, etc.) are, in this metadata, **`static DependencyProperty` properties** (a `get_XProperty` accessor), not fields — verified by reflecting against `Microsoft.WinUI.dll`. The original implementation therefore failed to resolve almost every platform DP, incorrectly filing them under the weak-evidence bucket `platform-clr-visual-contract`, along with a misleading explanation claiming "not every visual property WinUI exposes has a public DP field."

Fix: `ResolveDependencyProperty` now looks up, in order, `Type.GetField` then `Type.GetProperty`, walking both declaration forms up the `BaseType` chain. After the fix, `platform-clr-visual-contract` dropped from 172 entries to 0-1 (the only legitimate holdout is `EtherMasthead.Scale`, see above), and `platform-dp-contract` rose from 0 entries to the 165-184 range (fluctuating with the architectural adjustments made after §4.6, see §6). After the fix, the DP-layer read-back assertion (`DependencyObject.GetValue` matching the CLR value) passed on every affected property, **and exposed no new real failures**.

### 4.6 Revision: ScrollBar Theme-Propagation Defect (fixed)

The `scrollBar` control's screenshot matrix had `scrollBar-light.png` and `scrollBar-dark.png` with byte-identical content. Two issues were traced:

1. Lines 41-44 of `src/Ether.DesignSystem.Controls/Controls/Inputs/EtherScrollBar.xaml` set the Dark `ThemeDictionary`'s `EtherScrollBarThumbBrush`/`EtherScrollBarThumbHoverBrush` to the same `Gray400`/`Gray500` as Light, instead of the Dark values `Gray600`/`Gray700` already recorded for the Figma-aligned `scrollbar/background/default`/`scrollbar/background/hover` tokens in `src/Ether.DesignSystem.Foundation/Resources/Tokens/EtherColors.xaml`. This has been changed to `Gray600`/`Gray700`.
2. The real root cause of the two screenshots being **byte-for-byte identical** (not just wrong colors) was `GetScrollBarTemplateBrushColorsAsync` in `RuntimeVerification.ScrollBar.cs`: besides setting `themeRoot.RequestedTheme` (the only step the equivalent methods for all 12 other controls perform), it additionally set `scrollBar.RequestedTheme = theme` directly. Once `RequestedTheme` is explicitly assigned, it "sticks" and no longer re-inherits as the ancestor theme changes. Because this method probes Light first and then Dark, once probing was done, the `RequestedTheme` of `ScrollBarProof` — the live control instance actually used for the screenshot — was permanently stuck at `Dark`. So when `CaptureThemeScreenshotsAsync` subsequently flipped `themeRoot.RequestedTheme` to Light, this control no longer followed, and both screenshots ended up rendered in the same (Dark) theme. This extra assignment line has been removed, bringing it in line with every other control — relying solely on theme inheritance plus the existing `ActualTheme` polling wait.
3. Also found and fixed two assertions with inverted logic that had been treating this bug as an "expected contract" rather than something to catch: `RuntimeVerification.ScrollBar.cs` had `if (!lightTemplateBrushColors.SequenceEqual(...))` which should be `if (lightTemplateBrushColors.SequenceEqual(...))` (every other control uses the latter form, requiring Light≠Dark); `scripts/Verify-ConsumerFixtures.ps1` used `-cne` on the scrollBar line, which should be `-ceq` (all 12 other controls use `-ceq`).

After the fix, pixels resolved directly via reflection confirm: `scrollBar-light.png` is RGB(139,150,161) = `Gray400`, `scrollBar-dark.png` is RGB(65,69,83) = `Gray600`, both matching the expected tokens exactly. `scripts/Verify-ConsumerFixtures.ps1` also gained a Light/Dark SHA256-inequality assertion for all 13 controls (not just the full-page screenshots), to prevent the same class of regression from slipping through again.

### 4.7 Revision: Visual-Gate Sensitivity Regression (fixed, 2026-08-30)

After §4.6 was finalized, the same `CaptureVisualFingerprintAsync` underwent another change: to suppress the compositor anti-aliasing jitter mentioned in §6's "determinism proof," each BGRA8 channel byte was masked with `& 0xF0` before hashing (masking off the low 4 bits, taking it from 256 levels down to 16). This change had three problems on its own: the comment said "masks off the low 2 bits" but `0xF0` actually masks off the low 4 bits; truncation is not a tolerance (`0x0F` and `0x10` differ by only 1 level, and after truncation become `0x00` and `0x10` — still different — so it doesn't achieve the goal of suppressing ±1 jitter); more seriously, it dropped the hash-based count from 265 `pixel-difference` entries to 232, with 20 properties (`EtherCheckbox`/`EtherRadioButton`'s `Content`/`ContentTemplate`/`FontFamily`/`FontStyle`/`FontWeight`, `EtherMasthead.IsEnabled`/`Opacity`/`Scale`, `EtherSlider.Labels`/`ShowLabels`, `EtherSteeringBar.Minimum`/`Title`/`ValueContent`/`FontStyle`) falling from `pixel-difference` into the weak-evidence bucket — properties covering text content, font, opacity, scale, and disabled state, which ought to produce a detectable pixel change.

**Root-cause diagnosis**: temporarily added diagnostics to `AreVisuallyEquivalent` (see below) that output both "how many bytes actually differ at zero tolerance" and "the maximum single-channel difference," and after running a full audit found two clearly separated populations:

- Of the 217 properties currently filed in the weak-evidence bucket, 188 had before/after bitmaps that, after settling, were **byte-for-byte identical** (a difference of 0) — the settling loop in `CaptureSettledVisualFingerprintAsync` had already suppressed compositor noise to zero on its own, so the "jitter" the `0xF0` mask was meant to address was already gone by this point.
- The aforementioned 20 properties, plus a few more (`EtherMasthead.Scale`, `EtherSteeringBar.Title`), had a maximum single-channel difference of only 2-5 levels, but the number of affected bytes ranged from 435 to 9,708 — this is real signal from small-scale sub-pixel anti-aliasing/gamma-blending shifts in text/glyphs/opacity, just of small magnitude; `0xF0` (an effective tolerance of 15) swallowed all of it.

**Fix**: replaced "mask then hash" with "keep the raw BGRA8 buffer, compare per-pixel with tolerance + significance" (`AreVisuallyEquivalent(VisualSnapshot, VisualSnapshot, out int)`, `tests/Ether.DesignSystem.ConsumerFixtures/RuntimeVerification.AttachedVisualProperties.cs`): a single-channel byte difference `> 1` level counts as "changed" (`ChannelToleranceLevels = 1`, chosen as the lower bound that still captures the smallest real signal — `EtherMasthead.Opacity`'s difference of 2 — since the diagnostics showed the post-settling noise floor to be exactly 0, so no larger tolerance is needed); a bitmap is judged to have "changed" only once the number of changed pixels exceeds `Math.Max(12, total pixel count × 0.0002)` (`MinimumSignificantPixelCount`/`SignificantPixelFraction`), a floor set far above the noise baseline (0) and far below the smallest real signal (roughly 100+ pixels). The settling loop (`CaptureSettledVisualFingerprintAsync`) is unchanged — only the comparison function switched from hash equality to this tolerance comparison.

**Related fix: the settling window wasn't wide enough**. After switching to the tolerance comparison, `EtherInput.FontFamily`/`EtherInput.HeaderTemplate` were found to flip between `pixel-difference` (132 pixels changed) and the weak-evidence bucket (0 pixels changed) across two consecutive independent runs — not a boundary flicker around the tolerance/significance threshold, but the underlying "did the change actually get rendered" outcome itself being unstable. The cause is that TextBox's text reflow/font substitution is asynchronous, sometimes going quiet first (several frames of an unchanged bitmap) before belatedly rendering the change; the old `RequiredStableFingerprintReadings = 3` (3 consecutive unchanged frames counted as settled) sometimes misjudged this "quiet period" as the final settled state, locking in on an intermediate frame where the change hadn't landed yet. This problem was invisible in the `0xF0`-mask era — it swallowed the real signal for these two properties as well, and the two runs happened to both land in the "appears unchanged" bucket, producing false determinism. The fix raises `RequiredStableFingerprintReadings` from 3 to 6; verified with zero flips across two independent full runs (see §6).

**Acceptance result**: all 20 target properties are back to `pixel-difference` (including `EtherMasthead.Scale` — verified via reflection that it genuinely has no `Microsoft.UI.Xaml.UIElement.ScaleProperty` static identifier; the `platform-clr-visual-contract` classification itself was never wrong, but its pixel change should have been detected, and after the fix it is, so it no longer needs to fall back to the weak-evidence bucket); `pixelChangedPropertyCount` is back to 265 (matching the baseline from before the `0xF0` mask was introduced, pre-§4.5-revision); per-property evidence Method is fully consistent (0 differences) across two consecutive independent full runs (see §6).

## 5. Key Code and Test Changes

### Components and Templates

- `EtherButton.xaml`: the Default, Secondary, and Tertiary styles and templates were changed to consume the common visual properties `Background`, `BorderBrush`, `Foreground`, `CornerRadius`, font, and left/right icons.
- `EtherCheckbox.xaml`, `EtherRadioButton.xaml`: label font/foreground and the default style are consumed via template bindings and theme resources.
- `EtherInput.xaml`: the input box's background, border, placeholder foreground, and default style are wired into template bindings.
- `EtherDropdown.xaml`: the trigger's background, corner radius, foreground, font, arrow, and default style are wired into template bindings; runtime verification exercises a real dropdown open/close.
- `EtherSegmentPanel.cs`: arranges children by `FlowDirection`, fixing the logical-first-item order under RTL; `EtherSegmentedControl.cs` and the sample now use this panel.
- `EtherSlider.xaml.cs`, `EtherSteeringBar.xaml.cs`, `HandContentControl.cs`: added control contracts, state, and supporting behavior; `PublicAPI.Unshipped.txt` synced with the new public API.

### Acceptance Infrastructure

- `RuntimeVerification.AttachedVisualProperties.cs`: added full attached-visual-property verification, bitmap fingerprinting, layout snapshots, visibility round-trips, DP/CLR contracts, and per-item evidence output.
- `RuntimeVerification.PublicPropertyClassification.cs`: fixed the full-property classification at `482 / 73 / 946`, totaling 1,501.
- `RuntimeVerification.cs`, `RuntimeVerification.Infrastructure.cs`: output the complete marker, capturing both the full-page and control-level screenshot matrix.
- `Verify-ConsumerFixtures.ps1`: enforces a one-to-one match between each visual property key and its evidence key; only allows defined evidence methods; keeps the JSON and screenshot evidence from every successful run, without deleting it on the next run's temp-directory cleanup.
- `Verify-WinUiConventions.ps1`: strengthened checks for official DP, template, state, high-contrast, and animation conventions.
- Packaged/Unpackaged consumer projects and samples: ensure the runtime always restores strictly from the NuGet packages, with no project-reference bypass.

### Related Build, CI, and Sample Changes

The changes also touch `Ether.DesignSystem.slnx`, the CI workflow, the preview-package/ARM64/MSIX/component-contract scripts, and the Segmented/Slider Gallery samples, to bring the new control contracts into the solution and the release-verification path.

**A precise note on the scale of the changes**: `git diff --name-only` only counts changes to **tracked** files; this round's tally of 31 tracked files, ~640 lines added, 100 lines removed (excluding this handoff document itself) does not include the following content, which is still **untracked** — it doesn't show up in the `git diff` tally, but is equally a core deliverable of this round's audit, and is only visible via `git status`:

- The entire new project `src/Ether.DesignSystem.Interactions/`;
- The entire new test project `tests/Ether.DesignSystem.Interactions.ContractTests/`;
- 5 core audit source files: `RuntimeVerification.AttachedVisualProperties.cs`, `RuntimeVerification.FullPropertyCode.cs`, `RuntimeVerification.PropertyConsumption.cs`, `RuntimeVerification.PublicPropertyInventory.cs`, `RuntimeVerification.PublicPropertyClassification.cs`.

In other words, the actual volume of new code is significantly larger than what the "31 tracked files / 640 lines" figure implies; before merging, both `git status` (listing untracked content) and `git diff --stat` (line counts for tracked content) should be used together to assess the scale of the change.

## 6. Final Run Results

**This section has been updated along with the 2026-08-30 revisions to §4.7 and §11.** Marker of the final successful run (the last of two consecutive independent full runs; the earlier one is under "determinism proof" below):

```text
artifacts/audit-runs/consumer-runtime-evidence-20260830-002407286/runtime-result.json
```

Key fields (identical across both runs):

| Field | Final value |
| --- | ---: |
| `outcome` | `success` |
| `publicPropertyCode.getterReadCount` | 1,501 |
| `publicPropertyCode.setterInvocationCount` | 1,501 |
| `attachedVisualProperties.visualPropertyCount` | 482 |
| `attachedVisualProperties.evidence.Count` | 482 |
| `attachedVisualProperties.pixelChangedPropertyCount` | 265 |
| `propertyConsumption.propertyChangedCallbackCount` | 37 |
| `propertyConsumption.backendPropertyEventCount` | 37 |
| `propertyConsumption.standardInteractionEventCount` | 12 |
| `screenshots.controls.Count` | 13 |
| Control Light/Dark PNG count | 26 |

This run's visual-evidence distribution: 265 pixel-difference entries, 12 layout-difference entries, 13 visibility-state round-trips, 22 Ether component DP contracts, 170 WinUI platform DP contracts, 0 WinUI platform CLR visual contracts. Total is 482, with no missing property keys. `platform-clr-visual-contract` went from "1 entry (`EtherMasthead.Scale`)" after the §4.5 revision down to 0: after the §4.7 tolerance-comparison fix, `EtherMasthead.Scale`'s real pixel change is now correctly detected and falls under `pixel-difference` instead; the `platform-clr-visual-contract` classification bucket itself still exists (for future use should a genuine "visual CLR property with no DP" case appear), it's just that no property in the current component set falls into it any longer. Layout-difference dropped from 25 to 12, a side effect of the same fix: previously, because the pixel layer under-detected due to the mask, some properties that had "both a layout change and a pixel change" fell back to `layout-difference` as second choice; after the tolerance-comparison fix, they are now correctly determined by `pixel-difference` first (in the code, `pixel-difference` takes priority over `layout-difference`, see `VerifyAttachedVisualPropertiesAsync`).

### Determinism Proof

An earlier version of the attached-visual-property audit (`VerifyAttachedVisualPropertiesAsync`) captured the "after" fingerprint right after `control.UpdateLayout(); await Task.Yield();` following a mutation, which raced against asynchronous rendering paths like VisualState CubicEase transitions and TextBox font reflow, causing the same code to produce different evidence Methods across different runs (e.g. `EtherIntelligenceButton.IsEnabled` and `EtherSteeringBar.IsEnabled` flipped from weak evidence to `pixel-difference` between two rounds).

The fix at the time the document was finalized on 2026-08-29 had two layers:

1. **Constructive settling, instead of guessing a wait duration**: `CaptureSettledVisualFingerprintAsync` samples `RenderTargetBitmap` fingerprints repeatedly (once per real composited frame) until N consecutive samples are exactly identical, at which point it's considered settled (N=3 at the time, changed to 6 on 2026-08-30, see below); both the `before` and `after` sides go through the same settling logic (the starting point itself must not be jittery either). If it fails to settle within 60 samples, the run throws and terminates immediately, instead of silently degrading to weak evidence — this way "jitter" can never be silently waved through.
2. **Quantized fingerprint to filter compositor sub-pixel noise**: at the time, an `& 0xF0` channel mask was used to suppress jitter — **§4.7 later proved this specific mechanism was wrong (though the underlying motivation, "noise needs to be filtered," was correct)** — it has since been replaced with a per-pixel tolerance + significance comparison, see §4.7.

**2026-08-30 follow-up fix (verified alongside §4.7)**: switching to a tolerance comparison exposed that `RequiredStableFingerprintReadings = 3` (3 consecutive unchanged frames counted as settled) was not enough to get past the TextBox asynchronous-reflow "quiet period" for `EtherInput.FontFamily`/`HeaderTemplate`, causing these two properties to flip between `pixel-difference` (132 pixels changed) and the weak-evidence bucket (0 pixels changed) across consecutive runs. `RequiredStableFingerprintReadings` has been raised to 6, verified with zero flips across two independent full runs (see below).

After the fix, per-property evidence Method was **fully consistent** (0 differences, `pixelChangedPropertyCount` = 265 in both) across two consecutive independent full runs against the final code state after the 2026-08-30 revision (`...20260830-001800579`, `...20260830-002407286`). At the time the document was finalized on 2026-08-29, five independent runs against the code state as it stood then were likewise fully consistent with each other (`...230136589`, `...230519527`, `...224425998`, `...224946229`, `...225342824`) — but that code state has since been superseded by the `0xF0`-mask regression described in §4.7, so it is now kept only as historical archive and no longer represents the current branch state. Every run was an independent-process execution of `Verify-ConsumerFixtures.ps1 -SkipSolutionBuild`, not a replay within the same process.

## 7. Final Evidence and Re-Verification Entry Points

| Artifact | Path |
| --- | --- |
| Full-run marker | `artifacts/audit-runs/consumer-runtime-evidence-20260830-002407286/runtime-result.json` |
| Full-page screenshots | `artifacts/audit-runs/consumer-runtime-evidence-20260830-002407286/screenshots/consumer-light.png`, `consumer-dark.png`, `consumer-highcontrast.png` |
| Control screenshot matrix | `artifacts/audit-runs/consumer-runtime-evidence-20260830-002407286/screenshots/controls/` |
| Determinism proof: another run on the final state | `artifacts/audit-runs/consumer-runtime-evidence-20260830-001800579/` |
| Historical archive (as finalized 2026-08-29, code state since superseded by §4.7) | `artifacts/audit-runs/consumer-runtime-evidence-20260829-230519527/`, `...230136589/`, `...224425998/`, `...224946229/`, `...225342824/` |
| Audit specification | `docs/architecture/2026-08-29-winui3-backend-consumable-component-audit.md` |
| Consumer acceptance script | `scripts/Verify-ConsumerFixtures.ps1` |
| WinUI convention gate | `scripts/Verify-WinUiConventions.ps1` |

Re-verification commands (PowerShell):

```powershell
& 'C:\Users\yiqizhong\.cache\codex-runtimes\codex-primary-runtime\dependencies\native\powershell\pwsh.exe' `
  -NoProfile -ExecutionPolicy Bypass `
  -File 'C:\Ether lib\scripts\Verify-ConsumerFixtures.ps1' -SkipSolutionBuild

& 'C:\Users\yiqizhong\.cache\codex-runtimes\codex-primary-runtime\dependencies\native\powershell\pwsh.exe' `
  -NoProfile -ExecutionPolicy Bypass `
  -File 'C:\Ether lib\scripts\Verify-WinUiConventions.ps1'
```

A successful consumer acceptance run will re-pack on its own, restore the NuGet consumer, launch the unpackaged host, and keep a fresh evidence bundle under `artifacts/audit-runs/consumer-runtime-evidence-<timestamp>/`.

## 8. Current Progress and Ongoing Maintenance Rules

**Current progress: 100% — the full-property acceptance defined for this round is complete and passing.**

Going forward, any addition or change to a component/property must satisfy:

1. Update the public API and property classification; any change to the totals or classification counts must be accompanied by updates to the acceptance script and runtime samples.
2. New Ether-owned DPs must add `SetValue`, getter, `GetValue`, callback, and JSON-envelope evidence.
3. New visual properties must have a piece of per-item visual evidence — relying solely on "no exception" is not acceptable.
4. New business interactions must use the standard adapters or an explicit `ObserveProperty` — controls must not directly access the HTTP/auth/business layer.
5. Update the control-level Light/Dark visual artifacts; when direction or accessibility is involved, update the RTL, High Contrast, and UIA assertions.
6. Run the consumer acceptance script, the WinUI convention gate, and `git diff --check` before merging.

## 9. Known Boundaries (Not Open Issues)

- Generic platform visual/layout properties do not enter the backend event stream by default; this is a deliberate architectural boundary to prevent leaking UI implementation details.
- Screenshots alone cannot prove full accessibility compliance, so the runtime assertions for UIA, RTL, text scaling, and OS High Contrast are kept.
- The evidence directory is a build artifact and may not enter Git; when long-term archiving is needed, CI should upload it as a build artifact.
- There are uncommitted changes in the workspace related to this round; do not use `git reset --hard` or an overwriting cleanup before handoff, to avoid losing the audit and component fixes.
- **CI currently does not run the runtime gate described in this document, and this is not an oversight — it is an existing, deliberate design choice in this repo**: the `package-consumers` job in `.github/workflows/build.yml`, at line 102, calls `./scripts/Verify-ConsumerFixtures.ps1 -SkipSolutionBuild -SkipRuntimeSmoke`, and `-SkipRuntimeSmoke` skips the entire runtime verification section — i.e. the full-property check of 1,501/482/37/12 described in §1/§6 of this document, the control screenshot matrix, and RTL/UIA/scaling/localization/high-contrast all do not run on a PR. The workflow's comment explains why: a hosted Windows runner reliably launching a real WinUI window is not reliable, so this class of runtime gate is deliberately kept on a local/self-hosted environment, owned by `scripts/Verify-RuntimeGates.ps1`. In other words, the acceptance results recorded in this document are the result of the **local/self-hosted gate, not a PR-blocking gate**; to ensure these property/visual/theme regressions are caught before a PR merges, someone must manually — or via a self-hosted runner — run `Verify-ConsumerFixtures.ps1` (without `-SkipRuntimeSmoke`).
- `CreateVisualFixture` in `RuntimeVerification.AttachedVisualProperties.cs` substitutes `EtherSegmentedControl` for `EtherSegmentedTrack` when running the visual verification (the method has a comment explaining this): `EtherSegmentedTrack` is a source-compatible subclass with no independent template contract — WinUI's keyed `ControlTemplate` is attached to `EtherSegmentedControl` (the base class) — so inherited properties need to be rendered against the type that actually resolves a real template in order to produce meaningful visual evidence; meanwhile, the independent "code callability" gate (step 4.2) still genuinely instantiates `EtherSegmentedTrack` itself. In other words, `EtherSegmentedTrack`'s public properties are verified against its own type at the code level, but against `EtherSegmentedControl`'s rendered output at the visual level.

## 10. Revision Log (2026-08-29, same-day revision)

After the first version of this handoff document was issued, a re-check found 4 substantive defects (all fixed and re-verified) and a number of inaccurate statements, all corrected in this revision:

1. **DP-resolution defect** (§4.5): `ResolveDependencyProperty` originally only checked `GetField`, missing the platform DPs exposed as a `static property` under WinUI 3's WinRT projection, causing 172 platform properties to be misjudged into the weak-evidence bucket, with an incorrect accompanying explanation. Fixed; `platform-clr-visual-contract` dropped from 172 to 0-1 (the sole remaining `EtherMasthead.Scale` has been confirmed via reflection to be a genuine classification, not a bug).
2. **Cross-run non-determinism in visual evidence** (§6 "Determinism Proof"): the original fixed-duration wait for animation/compositor settling was replaced with a constructive scheme of "sample until the fingerprint settles + throw on settling failure + a quantized fingerprint to filter compositor noise," and per-property evidence Method was fully consistent across three consecutive independent full runs.
3. **ScrollBar theme-propagation defect** (§4.6): `EtherScrollBar.xaml`'s incorrect Dark theme color values, an explicit `RequestedTheme` assignment in the verification code that "stuck" a control instance to the wrong theme, and two assertions with inverted logic (treating the bug as the contract) — all four fixed together; `scripts/Verify-ConsumerFixtures.ps1` gained a Light/Dark SHA256-inequality assertion for all 13 controls to prevent the same class of regression.
4. **Documentation-wording corrections**: §4.1's audited-type count corrected from "11" to the actual 13; the wording in §1/§4.2 about how the setter is verified was changed to accurately describe "read the current value on a detached instance, then write it back unchanged," rather than implying validation against a new value; §5 adds a note that the actual changes include a large amount of untracked new projects/files, beyond the 31 tracked files; §9 adds a note that CI in fact skips the entire runtime gate described in this document via `-SkipRuntimeSmoke` (it is not a PR-blocking gate), plus the explanation of the `EtherSegmentedTrack`→`EtherSegmentedControl` visual-specimen substitution.
5. **Visual-gate sensitivity regression** (§4.7, 2026-08-30): the "quantized fingerprint to filter compositor noise" fix described in item 2 of §10 was, in its implementation, an `& 0xF0` channel mask, which was later proven to be the wrong specific mechanism — it is not a tolerance, and it dragged 20 properties with genuine visual changes (text/font/opacity/scale/disabled state) down into the weak-evidence bucket along with it, dropping `pixelChangedPropertyCount` from 265 to 232. It has been replaced with a scheme that keeps the raw pixels and compares per-pixel with tolerance + significance; also found and fixed that, after switching to the more sensitive comparison, `RequiredStableFingerprintReadings` (the number of consecutive stable readings required to settle) was not enough to cover the TextBox asynchronous-reflow quiet period for `EtherInput.FontFamily`/`HeaderTemplate`, causing these two properties to flip across consecutive runs (raised from 3 to 6). After the fix, the 20 properties are back to `pixel-difference`, `pixelChangedPropertyCount` is back to 265, and per-property evidence Method is fully consistent across two consecutive independent full runs.
6. **Added §11: High Contrast foreground/background pairing defect** (2026-08-30): an independent accessibility-defect fix running parallel to this document's main thread (the property-consumable audit) — `EtherSegmentedControl`/`EtherDropdown`/`EtherButton`/`EtherMasthead`/`EtherSwitch` repainted the Hover/Pressed/Selected background as the opaque `SystemColorHighlightColor` under the High Contrast theme, but the foreground stayed at `SystemColorWindowTextColor`/`SystemColorButtonTextColor` — colors meant for a Window/ButtonFace background — leaving text/icons unreadable under high contrast. Details in §11.
7. **§11 re-check correction** (2026-08-30, same day): `EtherMasthead`'s icon recoloring had at one point been changed to code-behind subscribing to Pointer events and manually assigning `Fill`/`Stroke`; this implementation had a genuine regression (the color didn't refresh on a theme switch after a hover). An attempt was made to switch to a pure-XAML approach — a `Setter Target="Foreground"` (with no `ElementName`) plus an icon `{Binding Foreground, ElementName=...}` — but testing showed that on this WinUI 3 version, `VisualStateManager.GoToState` entering `PointerOver` threw a runtime `COMException (0x800F1000)`, so this version was reverted. The final fix keeps the Pointer-event subscription, and adds an `ActualThemeChanged` handler that, on a theme change, re-pushes the color already resolved for the new theme's palette, based on each button's current `CommonStates` state, and a regression test was added. Also found that `EtherDropdown.xaml` had picked up an unrelated refactor outside the scope of the §11 task (the trigger's `Background`/`CornerRadius`/`Foreground`/`FontWeight` switched to `TemplateBinding`); the re-check confirmed this refactor itself introduces no genuine regression — the observed difference was in fact the existing screenshot-pipeline noise — and corrected the overly broad "zero regression" wording in §11's "regression verification" section. Details in §12.

## 11. High Contrast Foreground/Background Pairing Defect (fixed, 2026-08-30)

### Problem

Under Windows' High Contrast theme, a foreground color must be paired with the background color it sits on according to a fixed rule and must not be mixed: `SystemColorWindowColor` pairs with `SystemColorWindowTextColor`; `SystemColorButtonFaceColor` pairs with `SystemColorButtonTextColor`; **`SystemColorHighlightColor` pairs with `SystemColorHighlightTextColor`**; the disabled state on any background pairs with `SystemColorGrayTextColor`.

In this repo's Light/Dark themes, Hover/Pressed/Selected states are mostly implemented with semi-transparent overlay colors (e.g. `AlphaBrand6`, `Gray25`), and since the overlay is very light, it's reasonable for the template to hardcode a single foreground value. But the HighContrast dictionary mapped these same tokens to an **opaque** `SystemColorHighlightColor`, breaking that premise — and the foreground wasn't switched to the paired `SystemColorHighlightTextColor` to match, so a selected-state background lit up while the text stayed the color originally tuned for a Window/ButtonFace background, becoming nearly unreadable against the bright Highlight background. `EtherCheckbox`/`EtherRadioButton`'s Checked state had already gotten this right (`CheckedFill`=Highlight paired with `Glyph`/`InnerDot`=HighlightText); this defect was the Hover/Pressed tier missing that pairing.

### Fix

The principle is **add the foreground, don't lower the background** — consistent with WinUI's own `ComboBoxItem`/`Button` approach; the newly added foreground keys use the same value as the existing foreground in Light/Dark (zero visual change), and only switch to `SystemColorHighlightTextColor` in the HighContrast dictionary. Changes by file:

| File | Addition/Change |
| --- | --- |
| `src/Ether.DesignSystem.Controls/Controls/Inputs/EtherSegmentedControl.xaml` | Added `EtherSegmentedControlSegmentForegroundHoverBrush`/`PressedBrush`; the `PointerOver`/`Pressed` states of `EtherSegmentTemplate` gained a `Cp.Foreground` Setter (previously only the `Checked`/`CheckedPointerOver`/`CheckedPressed` states had one) |
| `src/Ether.DesignSystem.Controls/Controls/Inputs/EtherDropdown.xaml` | Added `EtherDropdownForegroundHoverBrush`/`PressedBrush` (trigger text + arrow) and `EtherDropdownItemForegroundActiveBrush` (menu items, covering the seven states `PointerOver`/`Pressed`/`Selected`/`SelectedUnfocused`/`SelectedDisabled`/`SelectedPointerOver`/`SelectedPressed`); the trigger template gained `Foreground`/`Fill` Setters for `TriggerText`/`Arrow`, and the menu item's `ContentPresenter` got an `x:Name="ItemContent"` plus the corresponding Setters |
| `src/Ether.DesignSystem.Controls/Controls/Inputs/EtherButton.xaml` | Added `EtherButtonSecondaryForegroundHoverBrush`/`PressedBrush`; the Secondary template's `PointerOver`/`Pressed` states gained three `Foreground` Setters for `LeftIcon`/`Cp`/`RightIcon` (Primary's background is the same Highlight across all three states, so it never needed changing; Tertiary was already handled correctly) |
| `src/Ether.DesignSystem.Controls/Controls/Navigation/EtherMasthead.xaml`/`.xaml.cs` | Added `EtherMastheadIconForegroundHoverBrush`/`PressedBrush`. The Minimize/Maximize/Restore/Close icons are `Shape` (`Rectangle`/`Path`) content, whose `Fill`/`Stroke` don't participate in WinUI's Foreground property-value inheritance, and a VisualState.Setter can't reach across templates (the states of `EtherMastheadCaptionButtonStyle` can't touch the host icon). **The first implementation** (as originally written in this section) subscribed to the three buttons' `PointerEntered`/`Exited`/`Pressed`/`Released`/`Canceled`/`CaptureLost` events in `EtherMasthead.xaml.cs`, reading the already-resolved color from three hidden `{ThemeResource}` swatch elements and assigning it directly to the icon's `Fill`/`Stroke` — **this implementation had a genuine regression, found and corrected in the 2026-08-30 re-check**: once manually assigned, `Shape.Fill`/`Stroke` stop tracking their original `{ThemeResource}` XAML expression, so after a user hovers any one of the title-bar buttons and then switches Light/Dark/HighContrast theme, the icon color stays stuck at the old theme's color until the next hover refreshes it. **Fix attempt 1 (pure XAML, abandoned)**: replaced the Pointer-event subscription with a `<Setter Target="Foreground" .../>` with no `ElementName` prefix in the `PointerOver`/`Pressed` states of `EtherMastheadCaptionButtonStyle` (the intent being that an unqualified property name would resolve to "the control this template is applied to itself"), paired with the icon's `{Binding Foreground, ElementName=<CaptionButton>}`. This version **compiled successfully** (`dotnet build`, 0 errors), but **crashed at runtime**: calling `VisualStateManager.GoToState` to enter `PointerOver` threw a `System.Runtime.InteropServices.COMException (0x800F1000)`, showing that on this WinUI 3 version a bare property name in `Setter Target` does not resolve to a property on the template's host control the way expected — this version has been fully reverted and is not in the final code. **Final fix (landed)**: kept the Pointer-event subscription and the three hidden swatch elements (`IconForegroundDefaultSwatch`/`HoverSwatch`/`PressedSwatch`, whose `Fill` is a live `{ThemeResource}` that automatically re-resolves on a theme switch), and only added an `ActualThemeChanged` subscription: on a theme change, for each of the three title-bar buttons, read its `CommonStates` VisualStateGroup's `CurrentState.Name` (already maintained by `ButtonBase`, no extra bookkeeping needed), use it to pick the matching swatch `Fill`, and reassign it to that button's icon `Fill`/`Stroke` (reusing the existing `SetCaptionIconColor`). This way, regardless of which of Normal/Hover/Pressed a button is currently in, a theme switch refreshes the icon color to the correct color for that state under the new theme, no longer depending on "the next hover." **A second pitfall hit during implementation**: initially, reading the swatch `Fill` and reassigning it synchronously inside the `ActualThemeChanged` handler had no effect — a temporary diagnostic (on the same assertion failure, manually calling the refresh method a second time and putting both results into the exception message) confirmed that the swatch Rectangle's `Fill` (a `{ThemeResource}` expression) has **not yet** finished re-resolving for the new theme at the exact moment the `ActualThemeChanged` event fires — the synchronous read still got the old theme's color, so the "refresh" ended up reassigning the same old color again; whereas calling the same refresh method outside the event handler (e.g. in test code after a few `await Task.Delay` calls) correctly picked up the new theme's color. The fix defers the refresh logic by one `DispatcherQueue` tick (`DispatcherQueue.TryEnqueue(...)`, matching the pattern this file already uses for `AppWindow_Changed`), letting the resource re-resolution triggered by `ActualThemeChanged` land first, before reading the swatch. To let consumer tests reproduce this scenario without synthesizing real pointer input, the internal method `RefreshCaptionIconColorForCurrentState` (originally `private`) was changed to `internal` — `Ether.DesignSystem.Controls.csproj` already declares `InternalsVisibleTo` for the two ConsumerFixtures assemblies, so this adds no new public API surface. The new regression test is `VerifyMastheadHoverThemeTrackingAsync` in `tests/Ether.DesignSystem.ConsumerFixtures/RuntimeVerification.Masthead.cs`: it uses `VisualStateManager.GoToState(minimizeButton, "PointerOver", false)` to put the button into the Hover state, then calls `masthead.RefreshCaptionIconColorForCurrentState(minimizeButton)` (the same method the `ActualThemeChanged` handler ultimately calls in production) to "pre-set" the icon to the Light-theme Hover color, simulating the effect a real hover would have produced; then, **without calling either of the above again**, switches the theme directly from Light to Dark, waits for `masthead.ActualTheme` to propagate plus a few more dispatcher ticks, and asserts that the color resolved from the icon's `Fill` must have changed — on the pre-fix code (without the `ActualThemeChanged` subscription), this assertion fails, because `Fill` stays at the Light Hover color; on the final code, this regression test has been confirmed to pass (it runs, and passes, as part of `Verify-ConsumerFixtures.ps1 -SkipSolutionBuild`'s runtime smoke test) |
| `src/Ether.DesignSystem.Controls/Controls/Inputs/EtherSwitch.xaml` | Added `EtherSwitchKnobStrokeHoverBrush`/`PressedBrush`; the `PointerOver`/`Pressed` states of `EtherSwitchTemplate` gained a `Knob.BorderBrush` Setter (there's no text here, but the stroke still needs to stay distinguishable against the Highlight fill) |

`src/Ether.DesignSystem.Foundation/Resources/Tokens/EtherColors.xaml` was not changed: it is a pure token catalog (Figma-semantic-layer tokens like `background/dropdown/hover`, `text/primary`), and does not pair background and foreground together itself — the pairing defect only occurs when a **consumer** uses both classes of token on the same visual element (the 5 components fixed here each maintain their own local set of `Ether<Component>*Brush` tokens, and do not consume the semantic-layer tokens in `EtherColors.xaml`); per the hard token-naming rule, nothing was renamed and no new color value was invented without basis — every new key follows each file's existing naming style, with color values reusing the existing Light/Dark values already in that file, or `SystemColorHighlightTextColor`.

### New Gate: `scripts/Verify-HighContrastPairing.ps1`

Statically regex-scans every XAML file under `src/Ether.DesignSystem.Controls/Controls` that contains a `<ControlTemplate` (skipping the pure-token directory), with two rules:

- **Rule A (precise)**: within a single `<VisualState>` block, if a `Setter` sets `*.Background` to a brush that resolves to `SystemColorHighlightColor`, then no `*.Foreground`/`*.Stroke`/`*.BorderBrush`/`*.Fill` Setter in the same block may resolve to `SystemColorWindowTextColor`/`SystemColorButtonTextColor`.
- **Rule B (coarse, file-level)**: if a file has a brush key whose name contains `Hover`/`Pressed`/`Selected`/`Checked`/`Active` and resolves to `SystemColorHighlightColor`, then the file must contain at least one brush key whose name contains `Foreground`/`Stroke`/`Glyph`/`Text`/`Icon`/`Fg`/`Dot` and resolves to `SystemColorHighlightTextColor`. This rule exists to catch cases Rule A can't see — where the background becomes Highlight via a static XAML property or code-behind (rather than a Setter within the same state block), e.g. `EtherSegmentedControl`'s `HoverLayer`/`PressedLayer` opacity being code-driven, or `EtherMasthead`'s icon recoloring being code-behind-driven (a pure-XAML approach was re-examined on 2026-08-30 but reverted after it crashed at runtime — it remains code-behind-driven, just with a new `ActualThemeChanged` handler added to fix the stale-color-on-theme-switch regression — see the table before §12 for details).

Rule B has a known, explicitly listed exemption list (`$exemptHighlightKeys`), with each entry's purpose individually verified: `EtherScrollBarThumbHoverBrush` (a pure drag thumb, no text), `EtherSliderKnobBrush`/`KnobPressedBrush` (a pure round drag handle, no text), `EtherSteeringBarThumbTopHighlightBrush`/`StopMarkerActiveBrush` (a decorative highlight on the glass thumb / a track marker dot, no text), `EtherIntelligenceButtonBorderHoverBrush` (a hover focus stroke — the button text itself sits on an unchanged `ButtonFace` background), `EtherIntelligenceButtonBlueGlowPressedCoreBrush` (a decorative blur glow layer, carries no text). Wired into `.github/workflows/build.yml` (after the "Verify WinUI 3 control conventions" step). After running `Verify-HighContrastPairing.ps1`: confirmed that none of the 13 component files with a `ControlTemplate` in the whole repo still have a "Highlight-bright background + non-HighlightText foreground" pairing.

### Regression Verification

- `Verify-ConsumerFixtures.ps1 -SkipSolutionBuild` passes (including the Light/Dark template-brush assertions, the 13-control Light/Dark screenshot SHA256-inequality assertion, and the assertion that the OS High Contrast screenshot differs from both Light and Dark).
- Light/Dark visual zero-regression: **this claim's scope was originally limited to the new foreground/stroke keys added in this section themselves** — their values in the Light/Dark dictionaries are identical to the original foreground/stroke keys (the same `StaticResource` reference), so looking at just these new keys, it is indeed zero visual change. But the 2026-08-30 re-check found that `EtherDropdown.xaml`, in the same batch of uncommitted changes, also contained a **refactor outside this section's scope** (the trigger's `Background`/`CornerRadius`/`Foreground`/`FontWeight` changed from hardcoded `{ThemeResource}`/`{StaticResource}` to `{TemplateBinding ...}`, with the values now supplied by new Setters on `DefaultEtherDropdownStyle`) — this "zero regression" claim did not cover that refactor and offered no evidence for it; see §12 for details — the conclusion is that this refactor itself introduces no genuine pixel-level regression, but the original wording was easy to misread as "the entire `EtherDropdown.xaml` change is zero regression," and the scope is corrected here.
- `Verify-WinUiConventions.ps1` passes.
- `Verify-HighContrastPairing.ps1` passes.
- `dotnet build src/Ether.DesignSystem.Controls`: 0 warnings, 0 errors.

## 12. EtherDropdown Trigger Property-Source Refactor and Pixel-Level Regression Check (2026-08-30 re-check)

### Background

During the 2026-08-30 re-check of §11's deliverable, it was found that `src/Ether.DesignSystem.Controls/Controls/Inputs/EtherDropdown.xaml` had, besides the Hover/Pressed foreground-pairing fix described in §11, also picked up a **refactor outside that task's scope**: the trigger (closed state) Border `StateFill`/`OpenFill`'s `Background`/`CornerRadius`, `TriggerText`'s `Foreground`/`FontWeight`, and `Arrow`'s `Fill`, were changed from the template's hardcoded `{ThemeResource EtherDropdownFillDefaultBrush}`/`{StaticResource RadiusSm}`/`{ThemeResource EtherDropdownForegroundBrush}`/`{StaticResource WeightSemibold}` to `{TemplateBinding Background}`/`{TemplateBinding CornerRadius}`/`{TemplateBinding Foreground}`/`{TemplateBinding FontWeight}`, with the original values moved to new `Setter Property="Background"/"CornerRadius"/"Foreground"/"FontWeight"` entries on `DefaultEtherDropdownStyle` as defaults.

**The direction of this refactor is correct and should not be reverted**: it's consistent with the direction this entire audit round has been pushing — a value hardcoded in the template means a consumer setting the same-named public property (`Background`/`CornerRadius`/`Foreground`/`FontWeight`) has it visually ignored, whereas after switching to `TemplateBinding`, a value the consumer sets actually takes effect. But it was done in passing while completing the §11 High Contrast task, and the "Light/Dark visual zero regression" conclusion was written down without independent verification (see the correction in the previous section).

### Measured: Root Cause of the 58-Pixel Difference

Following the re-check record's method — pixel-by-pixel comparison, using `PIL`, of `dropdown-light.png` (the `dropdown` entry in the `consumer-light` screenshot matrix, 426×80 in size, produced by `CaptureCurrentPngAsync`/`CaptureThemeScreenshotsAsync` in `RuntimeVerification.Infrastructure.cs` — a single-frame screenshot with none of the "sample until the fingerprint settles" tolerance/settling mechanism from §4.7) — as produced by the same run of `Verify-ConsumerFixtures.ps1` — the re-check found the following:

1. The repo still retains 39 `artifacts/audit-runs/consumer-runtime-evidence-*` evidence directories produced during this round's uncommitted-change process, each containing `screenshots/controls/dropdown-light.png`, all from **the same code** (the TemplateBinding refactor and the High Contrast pairing fix were both already in the working tree, and none of these 39 runs made any change to `EtherDropdown.xaml` between them). Taking the SHA256 of all 39 files found they land on only two distinct hash values (call them A/B), and A/B don't appear as a single monotonic one-time jump across the 39 runs — they alternate repeatedly (e.g. …A, A, B, B, …, A, B, B, …, A, …), spanning the entire window from 18:25 to 00:24 the next day. This pattern of "the same code, randomly binarized alternation across many runs" is the signature of cross-run rendering non-determinism, not of a code change (a code change would only produce a single one-time jump at the moment it happened, not oscillate back and forth over dozens of subsequent runs).
2. Pixel-by-pixel comparison of the A and B variants (the `dropdown-light.png` from `consumer-runtime-evidence-20260829-182500723` and `consumer-runtime-evidence-20260829-192237053`) found: out of 34,080 total pixels, 58 pixels differ, with a maximum single-channel difference of 46, and all changed pixels fall within rows 35–48 — matching, digit for digit, the re-check record's "58 / 34080 pixels differ, maximum single-channel difference 46, concentrated in the text band at rows 35-46." In other words, the very pixel differences the re-check record used to argue "the TemplateBinding refactor caused a regression" are indistinguishable, in pixel count, maximum difference, and location, from **screenshot noise between runs of the exact same code**.
3. As a cross-check, taking the SHA256 of all 39 `dropdown-dark.png` files: all 39 hashes are identical (only 1 distinct hash value) — no such binarized noise was observed under the Dark theme. This is consistent with the explanation that "this noise comes from sub-pixel anti-aliasing near some specific compositing/rounding boundary under the Light theme" (the specific color values under the Dark theme may not land on the same rounding boundary), rather than screenshots being generally unstable across the board.
4. As an additional check, `EtherDropdown.xaml` was temporarily swapped back to `git show HEAD:...` (i.e. the version before any of today's uncommitted changes — before both the TemplateBinding refactor and the §11 pairing fix existed), confirming `dotnet build` passes (the HEAD version itself compiles cleanly; it just doesn't satisfy the current runtime/contract assertions — a full screenshot comparison wasn't run separately on it, since steps 1-3 had already established the existence of the noise, and the HEAD version's template doesn't fully match the assumptions of the current `RuntimeVerification.Dropdown.cs` and other test code, so running it would have limited value); the working tree was then restored to its current version (`git diff --stat` confirmed it matched the pre-swap state).

**Conclusion**: the 58-pixel difference is inherent cross-run rendering noise in `CaptureCurrentPngAsync` (used for the Light/Dark control screenshot matrix), belonging to the same class of issue that once affected the other screenshot pipeline (`CaptureSettledVisualFingerprintAsync`) recorded in §4.7 — it's just that §4.7's settling+tolerance fix was only applied to the latter, not to this simpler single-frame screenshot path, `CaptureThemeScreenshotsAsync`/`CaptureCurrentPngAsync`. **There is no evidence that the `TemplateBinding` refactor of `EtherDropdown.xaml` itself introduced a genuine color/weight/position regression**: after `Background`/`CornerRadius`/`Foreground`/`FontWeight` switched to `TemplateBinding`, their values come from the new Setters added to `DefaultEtherDropdownStyle`, and those values (`EtherDropdownFillDefaultBrush`/`RadiusSm`/`EtherDropdownForegroundBrush`/`WeightSemibold`) are exactly the same as the values hardcoded in the pre-refactor template; and during the re-check, no consumer (the Gallery's `DropdownPage.xaml`, or ConsumerFixtures' `DropdownProof`/`DefaultDropdownProof`) was found to explicitly set these four properties on a concrete `EtherDropdown` instance in a way that could change the rendered result because "the pre-refactor template ignored the local value, while the post-refactor template now honors it" — so this re-check found nothing that needs fixing.

### Conclusion and Follow-Up

- The `TemplateBinding` refactor of `EtherDropdown.xaml` is not being reverted.
- The previous section's "Light/Dark visual zero regression" statement has been corrected in scope (see above), so that it no longer implies coverage of this previously undocumented refactor.
- **Known limitation, not fixed**: `CaptureThemeScreenshotsAsync`/`CaptureCurrentPngAsync` (`tests/Ether.DesignSystem.ConsumerFixtures/RuntimeVerification.Infrastructure.cs`), used for the Light/Dark control screenshot matrix, has none of `CaptureSettledVisualFingerprintAsync`'s settling wait or tolerance comparison, so this screenshot path is itself unstable under exact byte-for-byte comparison (the binarized noise measured in this section is an example). The current `Verify-ConsumerFixtures.ps1` only uses these screenshots for "existence + dimensions + Light/Dark hash inequality" assertions, and does not do a cross-run byte-for-byte comparison, so this instability does not currently cause a false gate failure; but if anyone in future wants to use these screenshots as "zero pixel-level regression before/after a change" evidence (as this section's re-check attempted to do), the settling/tolerance mechanism from §4.7 needs to be added to this path first — otherwise any such comparison will either misjudge noise as a real difference, or misjudge a real difference as noise. This is outside the scope of this fix and is recorded here so the same mistake isn't repeated next time.

## 13. Change in Total Property Count and Audited-Type Count (2026-08-30, a later commit outside this document's recorded scope)

The `1,501` public writable properties, `482` visual properties, `37` Ether-owned DPs, and "13 audited types" (including `EtherSegmentedTrack`) recorded in §1, §4.1, and §9 of this document are the **accurate state as of the 2026-08-29 audit**; what follows is not a correction to that audit, but a record of a subsequent change to the code itself:

Commit `af909c93` ("refactor(controls): tighten the published surface before
release", 2026-08-30) removed `EtherSegmentedTrack` from the public surface entirely — neither the library nor the Gallery ever constructed it; only the audit-acceptance code described in this document constructed it, and its type name collided with a style key of the same name. After the removal, `RuntimeVerification.PublicPropertyInventory.cs`'s
`PublicPropertyInventoryTypes` dropped from 13 types to 12 (no longer
including `EtherSegmentedTrack`), and accordingly:

| Item | 2026-08-29 (as recorded in this document) | As of 2026-08-30 (current) |
| --- | ---: | ---: |
| Audited-type count | 13 | 12 |
| Total public writable properties | 1,501 | 1,388 |
| Visual properties (per-item observable evidence) | 482 | 445 |
| Semantic properties | 73 | 69 |
| Platform properties | 946 | 874 |
| Ether-owned DPs | 37 | 35 |
| Standard interaction adapters | 12 | 12 (unchanged) |
| Control screenshot matrix | 13 controls × Light/Dark | 13 controls × Light/Dark (unchanged — the set of control types itself didn't change; `EtherSegmentedTrack` is simply no longer listed separately, since it had no independent visual contract) |

The §9 note about `CreateVisualFixture` substituting `EtherSegmentedControl` for
`EtherSegmentedTrack` when rendering, and the description of "public-property
code callability still verified against its own type," no longer applies
once `EtherSegmentedTrack` has been removed (there is no longer a subclass
needing a substitute rendering specimen).

For the current authoritative numbers, see
`$expectedWritablePublicProperties` / `$expectedVisualPublicProperties` /
`$expectedSemanticPublicProperties` / `$expectedPlatformPublicProperties` /
`$expectedConsumedProperties` (35 entries) in
`scripts/Verify-ConsumerFixtures.ps1`, and `PublicPropertyInventoryTypes`
(12 entries) in
`tests/Ether.DesignSystem.ConsumerFixtures/RuntimeVerification.PublicPropertyInventory.cs`.
The same round of commits also added 26 golden-baseline control PNGs
(`tests/Ether.DesignSystem.ConsumerFixtures/VisualBaselines/controls/`, 13 controls ×
Light/Dark) and an external-consumer verification script that genuinely runs
outside the repo and does not inherit this repo's build configuration
(`scripts/Verify-ExternalConsumer.ps1`) — both postdate the audit scope
recorded in this document and are not part of the acceptance results in
§1–§12 of this document.
