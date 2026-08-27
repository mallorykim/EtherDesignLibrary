# High Contrast runtime (forced dictionary) — design

> **Implementation override (landed):** Runtime proof uses OS High Contrast
> (`SPI_GET/SETHIGHCONTRAST` + `AccessibilitySettings`), `osHighContrast: true`,
> `dictionaryForced: false`. Dictionary overlay / injected `#FF00FF00` was
> attempted and rejected. Restore is mandatory (fixture `finally` + script
> `Restore-OsHighContrastIfOn` after force-kill). Aquatic / Desert / Night Sky
> stay unclaimed unless those `.theme` files exist and were applied.

Date: 2026-08-27  
Branch: `codex/refactor`  
Status: approved for implementation planning  
Host: unpackaged package-consumer fixture only

## Claim

After the existing Light/Dark consumer smoke proofs, the unpackaged fixture applies the in-repo **HighContrast** `ThemeDictionaries` to the live tree **without** turning on Windows contrast themes, then records evidence that Foundation `BackgroundCanvas` resolved through the HighContrast mapping to an injected `SystemColorWindowColor`.

This is runtime evidence that the HighContrast dictionaries participate in `{ThemeResource}` lookup. It is **not** Aquatic, Desert, or Night Sky. Marker field `osHighContrast` is `false`.

## Non-goals

- Do not call `SystemParametersInfo` / do not apply `.theme` files.
- Do not claim Accessibility Insights, Appium, MSIX install, arm64 runtime, or nuget publish.
- Do not add Gallery GUI or consumer runtime smoke to `.github/workflows/build.yml` (`-SkipRuntimeSmoke` stays).
- Do not edit frozen token files under `src/Ether.DesignSystem.Foundation/Resources/Tokens/`.
- Do not add Gallery HighContrast smoke in this slice (Gallery stays Light/Dark).
- Do not invent UnitTests/UITests projects.
- Do not assert every control template brush against the injected colors. Canvas + screenshot hash + 13 automation names are the gate.

## Why this mechanism

WinUI selects the `HighContrast` theme dictionary only when OS high contrast is on. `FrameworkElement.RequestedTheme` is Light/Dark/Default and is ignored when OS high contrast is on. Overlaying a dictionary on `RootGrid.Resources` does not switch per-control `ThemeDictionaries` in `EtherButton.xaml` and siblings.

Existing unpackaged smoke already requires Light and Dark `BackgroundCanvas` colors to differ. If the OS is already in high contrast, that gate fails first. This slice therefore assumes OS high contrast is off.

**Forced dictionary:** after Light/Dark proofs, walk `Application.Current.Resources` (and nested `MergedDictionaries`). For every `ResourceDictionary` whose `ThemeDictionaries` contains both `Light` and `HighContrast`, save the `Light` reference and assign `ThemeDictionaries["Light"] = ThemeDictionaries["HighContrast"]`. Then overlay distinctive system colors on `Application.Current.Resources`:

| Key | Color |
| --- | --- |
| `SystemColorWindowColor` | `#FF00FF00` |
| `SystemColorWindowTextColor` | `#FFFFFF00` |

Set `themeRoot.RequestedTheme` to Dark then Light so `{ThemeResource}` re-evaluates. Read `RootGrid` `Background` as `SolidColorBrush`. It must equal `#FF00FF00` (HighContrast `BackgroundCanvas` → `SystemColorWindowColor`). Capture `consumer-highcontrast.png`. Confirm the 13 named fixture controls still expose their contract automation names. Restore Light dictionaries and remove the system-color overlays in `finally`, then set `RequestedTheme` back to Light.

If indexer assignment cannot point two theme keys at the same dictionary instance, copy HighContrast entries into a new `ResourceDictionary` and assign that to `Light`. Restore must still put the original Light instance back.

## Marker

Add `highContrast` to the existing `ETHER_CONSUMER_SMOKE` JSON (do not add a second host):

```json
"highContrast": {
  "osHighContrast": false,
  "dictionaryForced": true,
  "backgroundCanvasColor": "#FF00FF00",
  "injectedWindowColor": "#FF00FF00",
  "injectedWindowTextColor": "#FFFFFF00",
  "screenshotPath": "<workRoot>/screenshots/consumer-highcontrast.png",
  "automationNamesIntact": true
}
```

`screenshots` keeps `lightPath` / `darkPath` and adds `highContrastPath` (same file as `highContrast.screenshotPath`).

## Script gate

In `scripts/Verify-ConsumerFixtures.ps1`, on the unpackaged runtime-smoke path after `Assert-ScreenshotMarker`, call `Assert-HighContrastMarker`:

- `highContrast` object present
- `osHighContrast` is false
- `dictionaryForced` is true
- `backgroundCanvasColor` equals `injectedWindowColor` equals `#FF00FF00`
- `injectedWindowTextColor` equals `#FFFFFF00`
- screenshot file exists, length > 0
- SHA256 of high-contrast PNG differs from Light and from Dark
- `automationNamesIntact` is true

Success `Write-Host` mentions HighContrast dictionary-forced runtime. Do not mention Aquatic/Desert/Night Sky.

Hosted CI continues `-SkipRuntimeSmoke`; this assertion never runs on `windows-latest`.

## Files

- Modify: `tests/Ether.DesignSystem.ConsumerFixtures/RuntimeVerification.cs` (result record, marker payload)
- Modify: `tests/Ether.DesignSystem.ConsumerFixtures/RuntimeVerification.Infrastructure.cs` (force/restore, capture, canvas read)
- Modify: `scripts/Verify-ConsumerFixtures.ps1` (`Assert-HighContrastMarker`)
- Modify: `docs/architecture/2026-08-26-l4c-in-repo-quality-gates.md` (green substitute; OS contrast themes stay red)
- Modify: `HANDOFF.md` remaining-quality bullet so it does not say Gallery body loc is unfinished; High Contrast runtime is dictionary-forced, not OS themes

Do not change PublicAPI, control XAML, or Gallery.

## Error handling

- Wrap force + capture in `try/finally`. Restore Light dictionaries even on failure.
- Remove injected `SystemColor*` keys if they were absent before; otherwise restore previous values.
- If no Light+HighContrast pair is found in the merge tree, fail with a clear message (DesignSystem.xaml graph not loaded).
- If canvas color ≠ injected window color, fail: HighContrast mapping did not win `{ThemeResource}`.
- Do not catch-and-ignore restore exceptions; surface them after the original exception.

## Verification

Local only:

```powershell
Get-Process Ether.DesignSystem.ConsumerFixtures.Unpackaged -ErrorAction SilentlyContinue | Stop-Process -Force
powershell -NoProfile -File .\scripts\Verify-ConsumerFixtures.ps1 -SkipSolutionBuild
powershell -NoProfile -File .\scripts\Verify-GallerySmoke.ps1
git hash-object src/Ether.DesignSystem.Foundation/Resources/Tokens/EtherColors.xaml
```

Expect: consumer runtime success line includes HighContrast; Gallery Light/Dark still 20/20; EtherColors hash unchanged (`d7c218e5a631e86552ed2b089cbc03bbd571a090`).

## Still red after this slice

OS contrast themes (Aquatic / Desert / Night Sky), Accessibility Insights, out-of-process UIA, MSIX install/runtime, arm64 runtime, hosted GUI CI, nuget publish.
