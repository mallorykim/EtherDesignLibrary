# High Contrast Forced-Dictionary Runtime Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Prove the unpackaged consumer fixture can force in-repo HighContrast `ThemeDictionaries` onto the live tree (without OS contrast themes) and record canvas color, screenshot, and automation-name evidence in the existing `ETHER_CONSUMER_SMOKE` marker.

> **Implementation override (landed):** Task 2 overlay is superseded by Task 2b OS-selected proof. Task 3 docs must describe OS-selected High Contrast (`SPI_GET/SETHIGHCONTRAST` + `AccessibilitySettings`), `osHighContrast: true`, `dictionaryForced: false` — not dictionary-forced / injected `#FF00FF00`. The Global Constraint “Do not toggle Windows High Contrast” was overridden by the user. Aquatic / Desert / Night Sky stay unclaimed unless those `.theme` files exist and were applied. Restore is mandatory (fixture `finally` + script `Restore-OsHighContrastIfOn` after force-kill).

**Architecture:** After Light/Dark proofs, walk `Application.Current.Resources` and nested `MergedDictionaries`. Wherever `ThemeDictionaries` has both `Light` and `HighContrast`, point `Light` at the HighContrast dictionary, inject distinctive `SystemColorWindowColor` / `SystemColorWindowTextColor`, re-evaluate `{ThemeResource}` by toggling `RequestedTheme` Dark→Light, then restore in `finally`. Assert from `scripts/Verify-ConsumerFixtures.ps1` on the unpackaged runtime-smoke path only.

**Tech Stack:** WinUI 3, .NET 8 (`net8.0-windows10.0.19041.0`), PowerShell 5.1, existing `RuntimeVerification` partials and `ETHER_CONSUMER_SMOKE` JSON.

## Global Constraints

- Do not edit files under `src/Ether.DesignSystem.Foundation/Resources/Tokens/`. EtherColors hash must remain `d7c218e5a631e86552ed2b089cbc03bbd571a090`.
- Do not `nuget push`. Do not add Gallery or consumer GUI smoke to `.github/workflows/build.yml` (`-SkipRuntimeSmoke` stays).
- Do not toggle Windows High Contrast / do not apply Aquatic, Desert, or Night Sky `.theme` files.
- Do not mention Aquatic, Desert, or Night Sky in success `Write-Host` text.
- Do not invent UnitTests/UITests, Appium, or Accessibility Insights.
- Do not change PublicAPI, control XAML, or Gallery in this slice.
- `pwsh` is not on PATH. Use `powershell -NoProfile -File`. Stop leftover `Ether.DesignSystem.ConsumerFixtures.Unpackaged` before rebuilds.
- Named fixture `AutomationProperties.Name` values stay contract (`Package button`, etc.).
- Hosted CI never runs this assertion (runtime-smoke only).

## File map

| File | Responsibility |
| --- | --- |
| `scripts/Verify-ConsumerFixtures.ps1` | `Assert-HighContrastMarker`; call it after `Assert-ScreenshotMarker`; mention dictionary-forced HighContrast in the success line. |
| `tests/Ether.DesignSystem.ConsumerFixtures/RuntimeVerification.cs` | `HighContrastVerification` record; extend `ScreenshotVerification` and `VerificationResult`; serialize `highContrast` on the marker. |
| `tests/Ether.DesignSystem.ConsumerFixtures/RuntimeVerification.Infrastructure.cs` | Walk/swap/restore theme dictionaries; inject system colors; capture `consumer-highcontrast.png`; read canvas; check 13 automation names. |
| `docs/architecture/2026-08-26-l4c-in-repo-quality-gates.md` | Record the substitute; keep OS contrast themes red. |
| `HANDOFF.md` | Honest remaining-quality text (Gallery loc is done; HC runtime is dictionary-forced, not OS themes). |

Do not create a new verification host or a new smoke script.

---

### Task 1: Failing HighContrast marker assertion

**Files:**
- Modify: `scripts/Verify-ConsumerFixtures.ps1`

**Interfaces:**
- Consumes: existing `$runtimeResult` from the unpackaged marker JSON; existing `Assert-ScreenshotMarker`
- Produces: `Assert-HighContrastMarker` (PowerShell function). Expected JSON shape (camelCase or PascalCase; PowerShell member access is case-insensitive):
  - `highContrast.osHighContrast` = `$false`
  - `highContrast.dictionaryForced` = `$true`
  - `highContrast.backgroundCanvasColor` = `#FF00FF00`
  - `highContrast.injectedWindowColor` = `#FF00FF00`
  - `highContrast.injectedWindowTextColor` = `#FFFFFF00`
  - `highContrast.screenshotPath` = existing PNG path
  - `highContrast.automationNamesIntact` = `$true`
  - `screenshots.highContrastPath` = same path as `highContrast.screenshotPath`

- [ ] **Step 1: Add `Assert-HighContrastMarker` after `Assert-ScreenshotMarker`**

Insert this function immediately after `Assert-ScreenshotMarker` (after the closing `}` that follows the light/dark hash check) and before `Assert-PerformanceMarker`:

```powershell
function Assert-HighContrastMarker {
    param($RuntimeResult)

    $highContrast = $RuntimeResult.highContrast
    if ($null -eq $highContrast) {
        throw 'Unpackaged runtime marker is missing the highContrast object.'
    }
    if ($highContrast.osHighContrast -ne $false) {
        throw "highContrast.osHighContrast was '$($highContrast.osHighContrast)', expected false (OS contrast themes are not this slice)."
    }
    if ($highContrast.dictionaryForced -ne $true) {
        throw "highContrast.dictionaryForced was '$($highContrast.dictionaryForced)', expected true."
    }
    if ([string]$highContrast.injectedWindowColor -cne '#FF00FF00') {
        throw "highContrast.injectedWindowColor was '$($highContrast.injectedWindowColor)', expected '#FF00FF00'."
    }
    if ([string]$highContrast.injectedWindowTextColor -cne '#FFFFFF00') {
        throw "highContrast.injectedWindowTextColor was '$($highContrast.injectedWindowTextColor)', expected '#FFFFFF00'."
    }
    if ([string]$highContrast.backgroundCanvasColor -cne '#FF00FF00') {
        throw "highContrast.backgroundCanvasColor was '$($highContrast.backgroundCanvasColor)', expected '#FF00FF00'."
    }
    if ($highContrast.automationNamesIntact -ne $true) {
        throw "highContrast.automationNamesIntact was '$($highContrast.automationNamesIntact)', expected true."
    }
    $path = [string]$highContrast.screenshotPath
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        throw "highContrast.screenshotPath is missing: $path"
    }
    $item = Get-Item -LiteralPath $path
    if ($item.Length -lt 2048) {
        throw "highContrast screenshot is too small: $($item.Length) bytes."
    }
    $screenshots = $RuntimeResult.screenshots
    if ([string]$screenshots.highContrastPath -cne $path) {
        throw "screenshots.highContrastPath was '$($screenshots.highContrastPath)', expected '$path'."
    }
    $hcHash = (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash
    $lightHash = (Get-FileHash -LiteralPath $screenshots.lightPath -Algorithm SHA256).Hash
    $darkHash = (Get-FileHash -LiteralPath $screenshots.darkPath -Algorithm SHA256).Hash
    if ($hcHash -eq $lightHash) {
        throw 'HighContrast and Light screenshots are identical; dictionary force did not differ.'
    }
    if ($hcHash -eq $darkHash) {
        throw 'HighContrast and Dark screenshots are identical; dictionary force did not differ.'
    }
}
```

- [ ] **Step 2: Call it from the unpackaged runtime-smoke path**

In the same file, immediately after `Assert-ScreenshotMarker $runtimeResult` and before `Assert-PerformanceMarker $runtimeResult`, add:

```powershell
            Assert-HighContrastMarker $runtimeResult
```

Do not change `-SkipRuntimeSmoke` behavior. Do not add this call to hosted CI.

- [ ] **Step 3: Run the unpackaged runtime smoke and confirm the new assertion fails**

```powershell
Get-Process Ether.DesignSystem.ConsumerFixtures.Unpackaged -ErrorAction SilentlyContinue | Stop-Process -Force
powershell -NoProfile -File .\scripts\Verify-ConsumerFixtures.ps1 -SkipSolutionBuild
```

Expected: FAIL with `Unpackaged runtime marker is missing the highContrast object.` Build of the fixture may still succeed; the throw is after the process writes today's marker (which has no `highContrast`).

- [ ] **Step 4: Commit**

```powershell
git add -- scripts/Verify-ConsumerFixtures.ps1
git commit -m @'
Fail unpackaged consumer smoke until HighContrast runtime evidence is present.

'@
```

---

### Task 2: Force HighContrast dictionaries and write the marker

**Files:**
- Modify: `tests/Ether.DesignSystem.ConsumerFixtures/RuntimeVerification.cs`
- Modify: `tests/Ether.DesignSystem.ConsumerFixtures/RuntimeVerification.Infrastructure.cs`

**Interfaces:**
- Consumes: `VerifyAsync` `themeRoot` and `controlsTuple`; `CapturePngAsync`; `WaitForAppliedThemeAsync`; `FormatColor`; `FrameworkElementAutomationPeer.CreatePeerForElement`
- Produces:
  - `internal sealed record HighContrastVerification(bool OsHighContrast, bool DictionaryForced, string BackgroundCanvasColor, string InjectedWindowColor, string InjectedWindowTextColor, string ScreenshotPath, bool AutomationNamesIntact, int PixelWidth, int PixelHeight)`
  - `ScreenshotVerification` gains `string HighContrastPath`, `int HighContrastPixelWidth`, `int HighContrastPixelHeight`
  - `VerificationResult` gains `HighContrastVerification HighContrast` immediately before `PerformanceVerification Performance`
  - `private static async Task<HighContrastVerification> VerifyForcedHighContrastAsync(FrameworkElement themeRoot, (string Id, FrameworkElement Control, string ExpectedAutomationName)[] controls)`

- [ ] **Step 1: Extend the records and marker payload in `RuntimeVerification.cs`**

Replace `ScreenshotVerification` with:

```csharp
    internal sealed record ScreenshotVerification(
        string LightPath,
        string DarkPath,
        string HighContrastPath,
        int LightPixelWidth,
        int LightPixelHeight,
        int DarkPixelWidth,
        int DarkPixelHeight,
        int HighContrastPixelWidth,
        int HighContrastPixelHeight);
```

Add this record after `ScreenshotVerification`:

```csharp
    internal sealed record HighContrastVerification(
        bool OsHighContrast,
        bool DictionaryForced,
        string BackgroundCanvasColor,
        string InjectedWindowColor,
        string InjectedWindowTextColor,
        string ScreenshotPath,
        bool AutomationNamesIntact,
        int PixelWidth,
        int PixelHeight);
```

Add `HighContrastVerification HighContrast,` as a parameter of `VerificationResult` immediately before `PerformanceVerification Performance`.

The script does not assert pixel size. In `VerifyAsync`, keep capturing Light/Dark first. After `var rtlResult = await VerifyRtlAsync(...)` and **before** `stopwatch.Stop()`, add:

```csharp
        var highContrast = await VerifyForcedHighContrastAsync(themeRoot, controlsTuple);
        screenshots = screenshots with
        {
            HighContrastPath = highContrast.ScreenshotPath,
            HighContrastPixelWidth = highContrast.PixelWidth,
            HighContrastPixelHeight = highContrast.PixelHeight,
        };
```

Pass `highContrast` into `new VerificationResult(...)` immediately before the `PerformanceVerification` argument.

Update `CaptureThemeScreenshotsAsync` to construct the extended `ScreenshotVerification` with empty high-contrast fields that the with-expression overwrites:

```csharp
        return new ScreenshotVerification(
            lightPath,
            darkPath,
            HighContrastPath: string.Empty,
            lightSize.Width,
            lightSize.Height,
            darkSize.Width,
            darkSize.Height,
            HighContrastPixelWidth: 0,
            HighContrastPixelHeight: 0);
```

In `WriteMarker`, add `highContrast = result?.HighContrast,` immediately before `performance = result?.Performance,`. Leave `screenshots = result?.Screenshots` as-is so `HighContrastPath` serializes on the screenshots object.

- [ ] **Step 2: Implement force/restore in `RuntimeVerification.Infrastructure.cs`**

Add `using Windows.UI.ViewManagement;` at the top of the file (keep existing usings).

Add these private methods to the same partial class, below `CapturePngAsync`:

```csharp
    private const string InjectedWindowColor = "#FF00FF00";
    private const string InjectedWindowTextColor = "#FFFFFF00";

    private static async Task<HighContrastVerification> VerifyForcedHighContrastAsync(
        FrameworkElement themeRoot,
        (string Id, FrameworkElement Control, string ExpectedAutomationName)[] controls)
    {
        var osHighContrast = new AccessibilitySettings().HighContrast;
        if (osHighContrast)
        {
            throw new InvalidOperationException(
                "OS high contrast is on; this slice forces HighContrast dictionaries while Light/Dark still resolve. Turn off Windows contrast themes and re-run.");
        }

        var appResources = Application.Current.Resources
            ?? throw new InvalidOperationException("Application.Current.Resources is null.");
        var swaps = new List<(ResourceDictionary Owner, object OriginalLight)>();
        CollectLightHighContrastSwaps(appResources, swaps);
        if (swaps.Count == 0)
        {
            throw new InvalidOperationException(
                "No ResourceDictionary in Application.Resources exposed both Light and HighContrast ThemeDictionaries.");
        }

        var directory = Environment.GetEnvironmentVariable("ETHER_CONSUMER_SMOKE_SCREENSHOT_DIR");
        if (string.IsNullOrWhiteSpace(directory))
        {
            var markerPath = Environment.GetEnvironmentVariable("ETHER_CONSUMER_SMOKE_RESULT_PATH");
            directory = string.IsNullOrWhiteSpace(markerPath)
                ? Path.Combine(Path.GetTempPath(), "ether-consumer-screenshots")
                : Path.Combine(Path.GetDirectoryName(markerPath)!, "screenshots");
        }
        Directory.CreateDirectory(directory);
        var screenshotPath = Path.Combine(directory, "consumer-highcontrast.png");

        object? previousWindow = null;
        object? previousWindowText = null;
        var hadWindow = appResources.ContainsKey("SystemColorWindowColor");
        var hadWindowText = appResources.ContainsKey("SystemColorWindowTextColor");
        if (hadWindow)
        {
            previousWindow = appResources["SystemColorWindowColor"];
        }
        if (hadWindowText)
        {
            previousWindowText = appResources["SystemColorWindowTextColor"];
        }

        Exception? captureException = null;
        HighContrastVerification? verification = null;
        try
        {
            foreach (var (owner, _) in swaps)
            {
                owner.ThemeDictionaries["Light"] = owner.ThemeDictionaries["HighContrast"];
            }

            appResources["SystemColorWindowColor"] = new SolidColorBrush(Windows.UI.Color.FromArgb(0xFF, 0x00, 0xFF, 0x00));
            appResources["SystemColorWindowTextColor"] = new SolidColorBrush(Windows.UI.Color.FromArgb(0xFF, 0xFF, 0xFF, 0x00));

            themeRoot.RequestedTheme = ElementTheme.Dark;
            await WaitForAppliedThemeAsync(themeRoot, ElementTheme.Dark);
            themeRoot.RequestedTheme = ElementTheme.Light;
            await WaitForAppliedThemeAsync(themeRoot, ElementTheme.Light);
            themeRoot.UpdateLayout();

            if (themeRoot is not Panel { Background: SolidColorBrush canvasBrush })
            {
                throw new InvalidOperationException("Fixture theme root does not expose a SolidColorBrush BackgroundCanvas value after HighContrast force.");
            }

            var canvasColor = FormatColor(canvasBrush.Color);
            if (!string.Equals(canvasColor, InjectedWindowColor, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"BackgroundCanvas after HighContrast force was '{canvasColor}', expected '{InjectedWindowColor}'.");
            }

            var pngSize = await CapturePngAsync(themeRoot, ElementTheme.Light, screenshotPath);
            var namesIntact = true;
            foreach (var (id, control, expectedAutomationName) in controls)
            {
                control.UpdateLayout();
                var peer = FrameworkElementAutomationPeer.CreatePeerForElement(control)
                    ?? throw new InvalidOperationException($"{id} did not create an automation peer after HighContrast force.");
                var automationName = peer.GetName();
                if (!string.Equals(automationName, expectedAutomationName, StringComparison.Ordinal))
                {
                    namesIntact = false;
                    throw new InvalidOperationException(
                        $"{id} automation name after HighContrast force was '{automationName}', expected '{expectedAutomationName}'.");
                }
            }

            verification = new HighContrastVerification(
                OsHighContrast: false,
                DictionaryForced: true,
                BackgroundCanvasColor: canvasColor,
                InjectedWindowColor: InjectedWindowColor,
                InjectedWindowTextColor: InjectedWindowTextColor,
                ScreenshotPath: screenshotPath,
                AutomationNamesIntact: namesIntact,
                PixelWidth: pngSize.Width,
                PixelHeight: pngSize.Height);
        }
        catch (Exception exception)
        {
            captureException = exception;
        }
        finally
        {
            Exception? restoreException = null;
            try
            {
                foreach (var (owner, originalLight) in swaps)
                {
                    owner.ThemeDictionaries["Light"] = originalLight;
                }

                if (hadWindow)
                {
                    appResources["SystemColorWindowColor"] = previousWindow;
                }
                else
                {
                    appResources.Remove("SystemColorWindowColor");
                }

                if (hadWindowText)
                {
                    appResources["SystemColorWindowTextColor"] = previousWindowText;
                }
                else
                {
                    appResources.Remove("SystemColorWindowTextColor");
                }

                themeRoot.RequestedTheme = ElementTheme.Light;
                await WaitForAppliedThemeAsync(themeRoot, ElementTheme.Light);
            }
            catch (Exception exception)
            {
                restoreException = exception;
            }

            if (captureException is not null && restoreException is not null)
            {
                throw new AggregateException(captureException, restoreException);
            }

            if (captureException is not null)
            {
                throw captureException;
            }

            if (restoreException is not null)
            {
                throw restoreException;
            }
        }

        return verification
            ?? throw new InvalidOperationException("HighContrast force completed without a verification payload.");
    }

    private static void CollectLightHighContrastSwaps(
        ResourceDictionary dictionary,
        List<(ResourceDictionary Owner, object OriginalLight)> swaps)
    {
        var themes = dictionary.ThemeDictionaries;
        if (themes.ContainsKey("Light") && themes.ContainsKey("HighContrast"))
        {
            swaps.Add((dictionary, themes["Light"]));
        }

        foreach (var merged in dictionary.MergedDictionaries)
        {
            CollectLightHighContrastSwaps(merged, swaps);
        }
    }
```

If assigning `themes["Light"] = themes["HighContrast"]` throws because both keys cannot share one instance, catch that `Exception` inside the `foreach (var (owner, _) in swaps)` loop and instead:

```csharp
                var source = (ResourceDictionary)owner.ThemeDictionaries["HighContrast"];
                var copy = new ResourceDictionary();
                foreach (var key in source.Keys)
                {
                    copy[key] = source[key];
                }
                foreach (var nested in source.MergedDictionaries)
                {
                    copy.MergedDictionaries.Add(nested);
                }
                owner.ThemeDictionaries["Light"] = copy;
```

Only use the copy path when the direct assignment throws. Restore still writes `originalLight` back.

- [ ] **Step 3: Compile the unpackaged fixture**

```powershell
Get-Process Ether.DesignSystem.ConsumerFixtures.Unpackaged -ErrorAction SilentlyContinue | Stop-Process -Force
dotnet build "tests\Ether.DesignSystem.ConsumerFixtures\Unpackaged\Ether.DesignSystem.ConsumerFixtures.Unpackaged.csproj" -c Debug -p:Platform=x64
```

Expected: `Build succeeded. 0 Error(s)`.

- [ ] **Step 4: Run unpackaged runtime smoke and confirm the new assertion passes**

`Verify-ConsumerFixtures.ps1 -SkipSolutionBuild` still packs nupkgs and rebuilds the fixture from those packages. Run the full script so the smoke host loads the new `RuntimeVerification`:

```powershell
Get-Process Ether.DesignSystem.ConsumerFixtures.Unpackaged -ErrorAction SilentlyContinue | Stop-Process -Force
powershell -NoProfile -File .\scripts\Verify-ConsumerFixtures.ps1 -SkipSolutionBuild
```

Expected: success line includes HighContrast dictionary-forced runtime (you will add that phrase in Step 5 if it is not there yet) and no throw from `Assert-HighContrastMarker`. Marker JSON contains `highContrast` with `backgroundCanvasColor` `#FF00FF00`. `git hash-object src/Ether.DesignSystem.Foundation/Resources/Tokens/EtherColors.xaml` still prints `d7c218e5a631e86552ed2b089cbc03bbd571a090`.

If canvas color is not `#FF00FF00`, do not weaken the assertion. Debug `{ThemeResource}` re-evaluation (Dark→Light, inject before swap vs after). Do not fall back to OS contrast themes.

- [ ] **Step 5: Append the success `Write-Host` phrase**

In `scripts/Verify-ConsumerFixtures.ps1`, on the long unpackaged success `Write-Host` line, immediately before `; marker: $markerPath`, insert:

```text
; HighContrast dictionary-forced runtime (osHighContrast=false)
```

Do not write Aquatic, Desert, or Night Sky.

- [ ] **Step 6: Commit**

```powershell
git add -- tests/Ether.DesignSystem.ConsumerFixtures/RuntimeVerification.cs tests/Ether.DesignSystem.ConsumerFixtures/RuntimeVerification.Infrastructure.cs scripts/Verify-ConsumerFixtures.ps1
git commit -m @'
Prove HighContrast dictionaries on the unpackaged fixture without toggling OS contrast.

'@
```

---

### Task 3: Honest docs and Gallery regression

**Files:**
- Modify: `docs/architecture/2026-08-26-l4c-in-repo-quality-gates.md`
- Modify: `HANDOFF.md`

**Interfaces:**
- Consumes: Task 2 marker fields and the substitute vs still-red split
- Produces: docs that say dictionary-forced HighContrast runtime is a green substitute; Aquatic/Desert/Night Sky stay red

- [ ] **Step 1: Update `docs/architecture/2026-08-26-l4c-in-repo-quality-gates.md`**

In **Evidence**, add a bullet (keep Appium / Insights / SkipRuntimeSmoke / arm64 / RightToLeft wording elsewhere in the file so existing honesty contracts still match):

```markdown
- HighContrast runtime on the unpackaged fixture is dictionary-forced: Light
  `ThemeDictionaries` temporarily point at HighContrast, with injected
  `SystemColorWindowColor` `#FF00FF00`. Marker field `highContrast.osHighContrast`
  is false. This is not Aquatic, Desert, Night Sky, or Accessibility Insights.
```

In **Still red**, keep MSIX install, arm64 runtime, Insights, Appium, hosted CI, publish. Add:

```markdown
OS contrast themes (Aquatic / Desert / Night Sky),
```

Do not delete the words `Appium`, `arm64`, or `SkipRuntimeSmoke` from this file.

- [ ] **Step 2: Update `HANDOFF.md` remaining / limitations**

Replace the limitation bullet:

```markdown
- Gallery page-body copy is not fully `.resw` (chrome is)
```

with:

```markdown
- Gallery page-body copy is in `Strings/en-US/Resources.resw` (en-US only; `SourceXaml` stays English)
```

In **§4 Remaining / deliberately unfinished** (Red external / release gates), keep Insights, Appium, hosted-CI, MSIX install, arm64 runtime, publish. Add one line under honest limitations:

```markdown
- HighContrast consumer runtime is dictionary-forced with injected SystemColor*; OS Aquatic / Desert / Night Sky are not proven
```

- [ ] **Step 3: Run docs contract + Gallery smoke + token hash**

```powershell
powershell -NoProfile -File .\scripts\Verify-GalleryControlExample.ps1
git hash-object src/Ether.DesignSystem.Foundation/Resources/Tokens/EtherColors.xaml
Get-Process Ether.DesignSystem.Gallery,Ether.DesignSystem.ConsumerFixtures.Unpackaged -ErrorAction SilentlyContinue | Stop-Process -Force
powershell -NoProfile -File .\scripts\Verify-GallerySmoke.ps1
```

Expected: Gallery localization/ControlExample contract passes; EtherColors hash `d7c218e5a631e86552ed2b089cbc03bbd571a090`; Gallery smoke `Light 20/20; Dark 20/20`.

- [ ] **Step 4: Commit**

```powershell
git add -- docs/architecture/2026-08-26-l4c-in-repo-quality-gates.md HANDOFF.md
git commit -m @'
Record dictionary-forced HighContrast runtime without claiming OS contrast themes.

'@
```

---

## Spec coverage

| Spec section | Task |
| --- | --- |
| Forced Light→HighContrast walk + inject `#FF00FF00` / `#FFFFFF00` | Task 2 |
| Dark→Light re-eval, canvas must match injected window color | Task 2 |
| `consumer-highcontrast.png`, hash ≠ Light/Dark | Task 1 assert + Task 2 capture |
| 13 automation names intact | Task 2 |
| Restore dictionaries and SystemColor in `finally` | Task 2 |
| `highContrast` marker + `screenshots.highContrastPath` | Task 1–2 |
| `Assert-HighContrastMarker` on unpackaged runtime path | Task 1 |
| No OS theme toggle, no Aquatic/Desert/Night Sky claim | Global + Task 1/2 copy + Task 3 |
| No token edits, no CI GUI, no Gallery HC smoke | Global + Task 3 Gallery Light/Dark only |
| HANDOFF + L4-C quality-gates honesty | Task 3 |
| Aggregate restore errors | Task 2 `finally` |
