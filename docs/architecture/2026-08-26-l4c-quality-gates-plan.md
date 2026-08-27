# L4-C In-Repo Quality Gates Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Close the in-repo L4-C quality substitutes on the unpackaged package-consumer fixture (UIA dump, 225% scale, `.resw` localization, Light/Dark captures, elapsed budget, unsigned MSIX produce, arm64 pack/build) without claiming Insights, Appium, hosted GUI CI, MSIX install, or nuget publish.

**Architecture:** Keep using the existing `ETHER_CONSUMER_SMOKE` marker JSON and `scripts/Verify-ConsumerFixtures.ps1`. Add new marker objects rather than new hosts. Put MSIX produce and arm64 pack in dedicated scripts owned by `scripts/Verify-RuntimeGates.ps1` (local desktop), never `.github/workflows/build.yml`.

**Tech Stack:** WinUI 3, .NET 8 (`net8.0-windows10.0.19041.0`), PowerShell 5.1, Windows App SDK MSIX tooling, `RenderTargetBitmap` + `BitmapEncoder`.

## Global Constraints

- Do not edit any file under `src/Ether.DesignSystem.Foundation/Resources/Tokens/` except the authorized `EtherColors.xaml` HighContrast slash-key mapping. Frozen blob hashes must remain: EtherPrimitives `d6ff0e5301b3672dbb492484c8d0ea584aca12be`, EtherColors `d7c218e5a631e86552ed2b089cbc03bbd571a090`, EtherSpacing `5d631cd2ebde306d389a1fa8999000359441bcd2`, EtherTypography `b24444567ee95467408e004fe7fab622eba79759`, EtherIconGeometries `134c1667934376ba4943cf350b110a02607c81f4`.
- Do not `nuget push`. Do not add Gallery or consumer GUI smoke to `.github/workflows/build.yml`.
- Do not install Accessibility Insights, Appium, or WinAppDriver. Do not claim those engines exist.
- Do not claim MSIX **install/runtime** if the deliverable is only an unsigned package file.
- Preserve every existing `x:Name` proof control and `AutomationProperties.Name` value on both fixture `MainWindow.xaml` files. The 13 RTL ids stay: `progressBar`, `button`, `checkbox`, `radioButton`, `input`, `dropdown`, `segmentedControl`, `intelligenceButton`, `steeringBar`, `slider`, `masthead`, `toggleSwitch`, `scrollBar`.
- `pwsh` is not on PATH. Run `powershell -File` or `.\scripts\...ps1`. Before rebuild, stop leftover `Ether.DesignSystem.ConsumerFixtures.Unpackaged` processes.
- `RuntimeVerification.cs` is shared by Unpackaged and Packaged. Do not split it. Do not add a second verification host.
- Screenshot PNGs belong under `artifacts/` (gitignored). Do not commit binary baselines.
- Hosted CI continues to call `Verify-ConsumerFixtures.ps1 -SkipSolutionBuild -SkipRuntimeSmoke`. New runtime assertions must live behind the existing runtime-smoke branch, not the static package-layout path.

---

### Task 1: Unpackaged fixture UIA, 225%, localization, screenshots, performance

**Files:**
- Modify: `tests/Ether.DesignSystem.ConsumerFixtures/RuntimeVerification.cs`
- Modify: `tests/Ether.DesignSystem.ConsumerFixtures/Unpackaged/App.xaml.cs`
- Modify: `tests/Ether.DesignSystem.ConsumerFixtures/Packaged/App.xaml.cs`
- Modify: `tests/Ether.DesignSystem.ConsumerFixtures/Unpackaged/MainWindow.xaml`
- Modify: `tests/Ether.DesignSystem.ConsumerFixtures/Packaged/MainWindow.xaml`
- Modify: `tests/Ether.DesignSystem.ConsumerFixtures/Unpackaged/MainWindow.xaml.cs`
- Modify: `tests/Ether.DesignSystem.ConsumerFixtures/Packaged/MainWindow.xaml.cs`
- Create: `tests/Ether.DesignSystem.ConsumerFixtures/Unpackaged/Strings/en-US/Resources.resw`
- Create: `tests/Ether.DesignSystem.ConsumerFixtures/Packaged/Strings/en-US/Resources.resw`
- Modify: `scripts/Verify-ConsumerFixtures.ps1`
- Create: `docs/architecture/2026-08-26-l4c-in-repo-quality-gates.md`

**Interfaces:**
- Consumes: existing `VerifyAsync(...)` control list, `WriteMarker`, `Assert-RtlMarker`, Light/Dark `GetBackgroundColorAsync`
- Produces: marker fields `uia`, `textScale`, `localization`, `screenshots`, `performance`; env `ETHER_CONSUMER_SMOKE_SCREENSHOT_DIR`

- [ ] **Step 1: Write the failing script assertions (TDD)**

In `scripts/Verify-ConsumerFixtures.ps1`:

1. After `Assert-RtlMarker $runtimeResult`, call four new functions (names exact): `Assert-UiaMarker`, `Assert-TextScaleMarker`, `Assert-LocalizationMarker`, `Assert-ScreenshotMarker`, `Assert-PerformanceMarker`.
2. Change smoke `WaitForExit(25000)` to `WaitForExit(40000)`.
3. Before `Start-Process`, set process env `ETHER_CONSUMER_SMOKE_SCREENSHOT_DIR` to `Join-Path $workRoot 'screenshots'`. Restore it in `finally` the same way as the other two env vars.
4. Append the new gates to the success `Write-Host` line.

Copy these functions verbatim:

```powershell
function Get-ExpectedAutomationNames {
    return [ordered]@{
        progressBar        = 'Package download progress'
        button             = 'Package button'
        checkbox           = 'Package checkbox'
        radioButton        = 'Package radio button'
        input              = 'Package input'
        dropdown           = 'Package dropdown'
        segmentedControl   = 'Package segmented control'
        intelligenceButton = 'Package intelligence button'
        steeringBar        = 'Package steering bar'
        slider             = 'Package slider'
        masthead           = 'Package masthead'
        toggleSwitch       = 'Package toggle switch'
        scrollBar          = 'Package scroll bar'
    }
}

function Assert-UiaMarker {
    param($RuntimeResult)

    $uia = $RuntimeResult.uia
    if ($null -eq $uia) {
        throw 'Unpackaged runtime marker is missing the uia object.'
    }
    $expected = Get-ExpectedAutomationNames
    $controls = @($uia.controls)
    if ($controls.Count -ne $expected.Count) {
        throw "UIA marker control count was $($controls.Count), expected $($expected.Count)."
    }
    foreach ($id in $expected.Keys) {
        $row = @($controls | Where-Object { $_.id -eq $id })[0]
        if ($null -eq $row) {
            throw "UIA marker is missing control '$id'."
        }
        if ($row.automationName -ne $expected[$id]) {
            throw "UIA '$id' automation name was '$($row.automationName)', expected '$($expected[$id])'."
        }
        if ([string]::IsNullOrWhiteSpace([string]$row.controlType)) {
            throw "UIA '$id' is missing controlType."
        }
        if ([double]$row.boundingWidth -le 0 -or [double]$row.boundingHeight -le 0) {
            throw "UIA '$id' bounding rect was $($row.boundingWidth)x$($row.boundingHeight)."
        }
    }
}

function Assert-TextScaleMarker {
    param($RuntimeResult)

    $textScale = $RuntimeResult.textScale
    if ($null -eq $textScale) {
        throw 'Unpackaged runtime marker is missing the textScale object.'
    }
    if ([double]$textScale.scale -ne 2.25) {
        throw "textScale.scale was '$($textScale.scale)', expected 2.25."
    }
    $expected = Get-ExpectedAutomationNames
    $controls = @($textScale.controls)
    foreach ($id in $expected.Keys) {
        $row = @($controls | Where-Object { $_.id -eq $id })[0]
        if ($null -eq $row) {
            throw "textScale marker is missing control '$id'."
        }
        if ($row.automationName -ne $expected[$id]) {
            throw "textScale '$id' automation name was '$($row.automationName)', expected '$($expected[$id])'."
        }
        if ([double]$row.actualWidth -le 0 -or [double]$row.actualHeight -le 0) {
            throw "textScale '$id' ActualWidth/Height was $($row.actualWidth)x$($row.actualHeight)."
        }
    }
}

function Assert-LocalizationMarker {
    param($RuntimeResult)

    $localization = $RuntimeResult.localization
    if ($null -eq $localization) {
        throw 'Unpackaged runtime marker is missing the localization object.'
    }
    $expected = 'Foundation resource resolved from Ether.DesignSystem.Foundation.'
    if ($localization.statusText -ne $expected) {
        throw "localization.statusText was '$($localization.statusText)', expected '$expected'."
    }
    if ($localization.resourceLoaderText -ne $expected) {
        throw "localization.resourceLoaderText was '$($localization.resourceLoaderText)', expected '$expected'."
    }
}

function Assert-ScreenshotMarker {
    param($RuntimeResult)

    $screenshots = $RuntimeResult.screenshots
    if ($null -eq $screenshots) {
        throw 'Unpackaged runtime marker is missing the screenshots object.'
    }
    foreach ($name in @('lightPath', 'darkPath')) {
        $path = [string]$screenshots.$name
        if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
            throw "Screenshot '$name' is missing: $path"
        }
        $item = Get-Item -LiteralPath $path
        if ($item.Length -lt 2048) {
            throw "Screenshot '$name' is too small: $($item.Length) bytes."
        }
    }
    $lightHash = (Get-FileHash -LiteralPath $screenshots.lightPath -Algorithm SHA256).Hash
    $darkHash = (Get-FileHash -LiteralPath $screenshots.darkPath -Algorithm SHA256).Hash
    if ($lightHash -eq $darkHash) {
        throw 'Light and Dark screenshots are identical; theme capture did not differ.'
    }
}

function Assert-PerformanceMarker {
    param($RuntimeResult)

    $performance = $RuntimeResult.performance
    if ($null -eq $performance) {
        throw 'Unpackaged runtime marker is missing the performance object.'
    }
    $elapsed = [double]$performance.elapsedMilliseconds
    if ($elapsed -le 0) {
        throw "performance.elapsedMilliseconds was '$elapsed', expected > 0."
    }
    if ($elapsed -ge 20000) {
        throw "performance.elapsedMilliseconds was '$elapsed', expected < 20000."
    }
}
```

Also add a static XAML assertion that both fixture windows use `x:Uid="FixtureStatus"`:

```powershell
function Assert-FixtureStatusUid {
    param([Parameter(Mandatory)][string]$XamlPath)

    if (-not (Select-String -LiteralPath $XamlPath -Pattern 'x:Uid="FixtureStatus"' -Quiet)) {
        throw "$XamlPath is missing x:Uid=`"FixtureStatus`"."
    }
}
```

Call it next to the existing `Assert-FixtureAutomationNames` calls.

- [ ] **Step 2: Run the consumer verifier far enough to see the new asserts fail**

Kill leftover smoke hosts first:

```powershell
Get-Process Ether.DesignSystem.ConsumerFixtures.Unpackaged -ErrorAction SilentlyContinue | Stop-Process -Force
```

Then, if a current marker already exists from a previous run you may unit-test the new functions against a JSON file that lacks `uia`. The required failing path is a full runtime smoke after implementation is still missing fields.

Because a full `Verify-ConsumerFixtures.ps1` rebuilds packages, the RED proof for this task is: after Step 1 is committed in the working tree (not yet the C#), running smoke (or feeding an old marker) throws `Unpackaged runtime marker is missing the uia object.` Record that exception text in the report. Do not leave the script assertions uncommitted relative to the C# — implement C# in the same task after RED.

- [ ] **Step 3: Add resw + x:Uid**

Unpackaged `Strings/en-US/Resources.resw`:

```xml
<?xml version="1.0" encoding="utf-8"?>
<root>
  <xsd:schema id="root" xmlns="" xmlns:xsd="http://www.w3.org/2001/XMLSchema" xmlns:msdata="urn:schemas-microsoft-com:xml-msdata">
    <xsd:element name="data">
      <xsd:complexType>
        <xsd:sequence>
          <xsd:element name="value" type="xsd:string" minOccurs="0" msdata:Ordinal="1" />
        </xsd:sequence>
        <xsd:attribute name="name" type="xsd:string" use="required" />
      </xsd:complexType>
    </xsd:element>
  </xsd:schema>
  <data name="FixtureStatus.Text" xml:space="preserve">
    <value>Foundation resource resolved from Ether.DesignSystem.Foundation.</value>
  </data>
</root>
```

Packaged resw uses the packaged copy:

```xml
  <data name="FixtureStatus.Text" xml:space="preserve">
    <value>Packaged fixture resource merge succeeded.</value>
  </data>
```

On both `MainWindow.xaml` subtitle `TextBlock`s, replace the `Text="..."` attribute with `x:Name="StatusText"` and `x:Uid="FixtureStatus"`. Keep Style, Foreground, TextWrapping. Do not change any named proof controls.

Expose `internal TextBlock StatusTextProof => StatusText;` on both `MainWindow.xaml.cs` files (add `using Microsoft.UI.Xaml.Controls;` if needed).

Pass `window.StatusTextProof` as the new last parameter of `VerifyAsync` from both `App.xaml.cs` files.

- [ ] **Step 4: Extend RuntimeVerification**

Add records (place with the existing RTL records):

```csharp
internal sealed record UiaControlSnapshot(
    string Id,
    string AutomationName,
    string ControlType,
    double BoundingWidth,
    double BoundingHeight);

internal sealed record UiaVerification(UiaControlSnapshot[] Controls);

internal sealed record TextScaleControlVerification(
    string Id,
    string AutomationName,
    double ActualWidth,
    double ActualHeight);

internal sealed record TextScaleVerification(
    double Scale,
    TextScaleControlVerification[] Controls);

internal sealed record LocalizationVerification(
    string StatusText,
    string ResourceLoaderText);

internal sealed record ScreenshotVerification(
    string LightPath,
    string DarkPath,
    int LightPixelWidth,
    int LightPixelHeight,
    int DarkPixelWidth,
    int DarkPixelHeight);

internal sealed record PerformanceVerification(double ElapsedMilliseconds);
```

Add those types to `VerificationResult` after `RtlVerification Rtl`.

Change `VerifyAsync` to:

1. Take a final `TextBlock statusText` parameter.
2. Start `var stopwatch = Stopwatch.StartNew();` immediately.
3. Keep the existing control + Light/Dark color work.
4. After Light/Dark colors and **before** RTL:
   - `screenshots = await CaptureThemeScreenshotsAsync(themeRoot);`
   - `uia = CaptureUia(controlsTuple);`
   - `textScale = await VerifyTextScaleAsync(themeRoot, controlsTuple);`
   - `localization = VerifyLocalization(statusText);`
5. Then existing `VerifyRtlAsync`.
6. `stopwatch.Stop();` and include `new PerformanceVerification(stopwatch.Elapsed.TotalMilliseconds)`.

`controlsTuple` is the same 13-item list already passed to `VerifyRtlAsync`. Extract a local array once so RTL/UIA/scale share it.

UIA capture:

```csharp
private static UiaVerification CaptureUia(
    params (string Id, FrameworkElement Control, string ExpectedAutomationName)[] controls)
{
    var results = new List<UiaControlSnapshot>(controls.Length);
    foreach (var (id, control, expectedName) in controls)
    {
        control.UpdateLayout();
        var peer = FrameworkElementAutomationPeer.CreatePeerForElement(control)
            ?? throw new InvalidOperationException($"{id} did not create an automation peer for UIA capture.");
        var name = peer.GetName();
        if (!string.Equals(name, expectedName, StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"{id} UIA name was '{name}', expected '{expectedName}'.");
        }

        var rect = peer.GetBoundingRectangle();
        if (rect.Width <= 0 || rect.Height <= 0)
        {
            throw new InvalidOperationException($"{id} UIA bounding rect was {rect.Width}x{rect.Height}.");
        }

        results.Add(new UiaControlSnapshot(
            id,
            name,
            peer.GetAutomationControlType().ToString(),
            rect.Width,
            rect.Height));
    }

    return new UiaVerification(results.ToArray());
}
```

225% scale (must restore transform even on failure):

```csharp
private static async Task<TextScaleVerification> VerifyTextScaleAsync(
    FrameworkElement themeRoot,
    params (string Id, FrameworkElement Control, string ExpectedAutomationName)[] controls)
{
    const double scale = 2.25;
    var previous = themeRoot.RenderTransform;
    try
    {
        themeRoot.RenderTransformOrigin = new Windows.Foundation.Point(0, 0);
        themeRoot.RenderTransform = new ScaleTransform { ScaleX = scale, ScaleY = scale };
        themeRoot.UpdateLayout();
        await Task.Delay(50);

        var results = new List<TextScaleControlVerification>(controls.Length);
        foreach (var (id, control, expectedName) in controls)
        {
            control.UpdateLayout();
            if (control.ActualWidth <= 0 || control.ActualHeight <= 0)
            {
                throw new InvalidOperationException(
                    $"{id} collapsed under 2.25 scale. Actual {control.ActualWidth}x{control.ActualHeight}.");
            }

            var peer = FrameworkElementAutomationPeer.CreatePeerForElement(control)
                ?? throw new InvalidOperationException($"{id} lost its automation peer under 2.25 scale.");
            var name = peer.GetName();
            if (!string.Equals(name, expectedName, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"{id} 2.25-scale automation name was '{name}', expected '{expectedName}'.");
            }

            results.Add(new TextScaleControlVerification(id, name, control.ActualWidth, control.ActualHeight));
        }

        return new TextScaleVerification(scale, results.ToArray());
    }
    finally
    {
        themeRoot.RenderTransform = previous;
        themeRoot.UpdateLayout();
    }
}
```

Localization:

```csharp
private static LocalizationVerification VerifyLocalization(TextBlock statusText)
{
    const string expected = "Foundation resource resolved from Ether.DesignSystem.Foundation.";
    var status = statusText.Text ?? string.Empty;
    if (!string.Equals(status, expected, StringComparison.Ordinal))
    {
        throw new InvalidOperationException($"StatusText was '{status}', expected '{expected}'.");
    }

    var loader = new Windows.ApplicationModel.Resources.ResourceLoader();
    var loaded = loader.GetString("FixtureStatus/Text");
    if (!string.Equals(loaded, expected, StringComparison.Ordinal))
    {
        throw new InvalidOperationException($"ResourceLoader FixtureStatus/Text was '{loaded}', expected '{expected}'.");
    }

    return new LocalizationVerification(status, loaded);
}
```

If `GetString("FixtureStatus/Text")` returns empty on this host, try `GetString("FixtureStatus.Text")` and then fail with both keys named in the exception. Do not weaken the expected string.

Screenshots (`using Microsoft.UI.Xaml.Media.Imaging; using Windows.Graphics.Imaging; using System.Runtime.InteropServices.WindowsRuntime;`):

```csharp
private static async Task<ScreenshotVerification> CaptureThemeScreenshotsAsync(FrameworkElement themeRoot)
{
    var directory = Environment.GetEnvironmentVariable("ETHER_CONSUMER_SMOKE_SCREENSHOT_DIR");
    if (string.IsNullOrWhiteSpace(directory))
    {
        var markerPath = Environment.GetEnvironmentVariable("ETHER_CONSUMER_SMOKE_RESULT_PATH");
        directory = string.IsNullOrWhiteSpace(markerPath)
            ? Path.Combine(Path.GetTempPath(), "ether-consumer-screenshots")
            : Path.Combine(Path.GetDirectoryName(markerPath)!, "screenshots");
    }

    Directory.CreateDirectory(directory);
    var lightPath = Path.Combine(directory, "consumer-light.png");
    var darkPath = Path.Combine(directory, "consumer-dark.png");
    var lightSize = await CapturePngAsync(themeRoot, ElementTheme.Light, lightPath);
    var darkSize = await CapturePngAsync(themeRoot, ElementTheme.Dark, darkPath);
    return new ScreenshotVerification(
        lightPath,
        darkPath,
        lightSize.Width,
        lightSize.Height,
        darkSize.Width,
        darkSize.Height);
}

private static async Task<(int Width, int Height)> CapturePngAsync(
    FrameworkElement themeRoot,
    ElementTheme theme,
    string path)
{
    themeRoot.RequestedTheme = theme;
    await WaitForAppliedThemeAsync(themeRoot, theme);
    themeRoot.UpdateLayout();
    var bitmap = new RenderTargetBitmap();
    await bitmap.RenderAsync(themeRoot);
    if (bitmap.PixelWidth <= 0 || bitmap.PixelHeight <= 0)
    {
        throw new InvalidOperationException($"RenderTargetBitmap for {theme} was {bitmap.PixelWidth}x{bitmap.PixelHeight}.");
    }

    var pixels = (await bitmap.GetPixelsAsync()).ToArray();
    using var stream = File.Open(path, FileMode.Create, FileAccess.Write);
    var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, stream.AsRandomAccessStream());
    encoder.SetPixelData(
        BitmapPixelFormat.Bgra8,
        BitmapAlphaMode.Premultiplied,
        (uint)bitmap.PixelWidth,
        (uint)bitmap.PixelHeight,
        96,
        96,
        pixels);
    await encoder.FlushAsync();
    return (bitmap.PixelWidth, bitmap.PixelHeight);
}
```

Extend `WriteMarker` anonymous payload with:

```csharp
uia = result?.Uia,
textScale = result?.TextScale,
localization = result?.Localization,
screenshots = result?.Screenshots,
performance = result?.Performance,
```

Restore `RequestedTheme` after screenshots if later RTL/layout needs Default; Light/Dark capture already leaves Dark applied today, so matching that existing behavior is acceptable.

- [ ] **Step 5: Architecture note**

Create `docs/architecture/2026-08-26-l4c-in-repo-quality-gates.md` stating:

- These are in-repo substitutes, not Insights/Appium/hosted GUI/nuget publish.
- Evidence paths: marker fields + `Verify-ConsumerFixtures.ps1`.
- 225% is a `ScaleTransform` on `RootGrid`, not `UISettings.TextScaleFactor`.
- UIA is in-process `AutomationPeer`, not out-of-process UIA.
- Screenshots are generated under `artifacts/`, not golden-image diffs.
- MSIX install, arm64 **runtime**, Insights, Appium, hosted CI, and publish remain red (later tasks may green pack/produce only).

- [ ] **Step 6: Run the covering test**

```powershell
Get-Process Ether.DesignSystem.ConsumerFixtures.Unpackaged -ErrorAction SilentlyContinue | Stop-Process -Force
.\scripts\Verify-ConsumerFixtures.ps1 -SkipSolutionBuild
```

Expected: `Consumer fixtures passed` and the success line mentions UIA, 2.25 scale, localization, screenshots, and performance. StorageFile may still be 0/3.

If the unpackaged exe is locked, kill it and retry once. If ResourceLoader key shape is wrong, fix the key (not the expected English string).

- [ ] **Step 7: Commit**

```powershell
git add -- tests/Ether.DesignSystem.ConsumerFixtures scripts/Verify-ConsumerFixtures.ps1 docs/architecture/2026-08-26-l4c-in-repo-quality-gates.md
git commit -m "Add in-repo UIA, scale, localization, screenshot, and timing gates to the package consumer."
```

---

### Task 2: Unsigned MSIX package produce

**Files:**
- Modify: `tests/Ether.DesignSystem.ConsumerFixtures/Packaged/Ether.DesignSystem.ConsumerFixtures.Packaged.csproj` only if generate-on-build flags or logo paths must change
- Modify: `tests/Ether.DesignSystem.ConsumerFixtures/Packaged/Package.appxmanifest` only if MSIX tooling rejects logos
- Create: `scripts/Verify-MsixPackage.ps1`
- Modify: `scripts/Verify-RuntimeGates.ps1`
- Modify: `docs/architecture/2026-08-26-l4c-in-repo-quality-gates.md`

**Interfaces:**
- Consumes: existing packaged fixture with `WindowsPackageType=MSIX`, `EtherDesignSystemFoundationIncludeHostRootFonts=false`
- Produces: at least one `.msix` or `.msixbundle` under the packaged project's `AppPackages/` directory; script fails if missing. Does **not** call `Add-AppxPackage`.

- [ ] **Step 1: Write `scripts/Verify-MsixPackage.ps1`**

```powershell
[CmdletBinding()]
param(
    [switch]$SkipSolutionBuild
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$project = Join-Path $repoRoot 'tests\Ether.DesignSystem.ConsumerFixtures\Packaged\Ether.DesignSystem.ConsumerFixtures.Packaged.csproj'
$configuration = 'Debug'
$platform = 'x64'

function Invoke-DotNet {
    param([Parameter(Mandatory)][string[]]$Arguments)
    & dotnet @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet $($Arguments -join ' ') failed with exit code $LASTEXITCODE."
    }
}

Push-Location $repoRoot
try {
    if (-not $SkipSolutionBuild) {
        Invoke-DotNet @('build', 'Ether.DesignSystem.slnx', '-c', $configuration, "-p:Platform=$platform")
    }

    Invoke-DotNet @(
        'build', $project,
        '-c', $configuration,
        "-p:Platform=$platform",
        '-p:GenerateAppxPackageOnBuild=true',
        '-p:AppxPackageSigningEnabled=false',
        '-p:EtherDesignSystemFoundationIncludeHostRootFonts=false'
    )

    $appPackages = Join-Path $repoRoot 'tests\Ether.DesignSystem.ConsumerFixtures\Packaged\AppPackages'
    $packages = @(Get-ChildItem -LiteralPath $appPackages -Recurse -Include *.msix, *.msixbundle -ErrorAction SilentlyContinue)
    if ($packages.Count -eq 0) {
        throw "MSIX produce gate found no .msix or .msixbundle under $appPackages."
    }

    $artifact = $packages | Sort-Object Length -Descending | Select-Object -First 1
    if ($artifact.Length -lt 1024) {
        throw "MSIX artifact '$($artifact.FullName)' is too small: $($artifact.Length) bytes."
    }

    Write-Host "MSIX produce gate passed (unsigned, not installed): $($artifact.FullName) ($($artifact.Length) bytes)"
}
finally {
    Pop-Location
}
```

- [ ] **Step 2: Run it and fix packaging errors**

```powershell
.\scripts\Verify-MsixPackage.ps1 -SkipSolutionBuild
```

If logos fail (size/format), reuse `Assets\Icons\Frame 2147253527.png` or generate valid 44/150 placeholders **without** changing frozen tokens. Keep `AppxPackageSigningEnabled=false`. Do not `Add-AppxPackage`.

If comma-named fonts still break MSIX even with `IncludeHostRootFonts=false`, stop and report BLOCKED with the exact MSBuild error.

- [ ] **Step 3: Wire into local runtime gates only**

At the end of `scripts/Verify-RuntimeGates.ps1` (after Gallery smoke), invoke `Verify-MsixPackage.ps1` with the same `$SkipSolutionBuild` hashtable splat pattern already used for consumer fixtures. Do not edit `.github/workflows/build.yml`.

Update `docs/architecture/2026-08-26-l4c-in-repo-quality-gates.md` to say MSIX **produce** is green and MSIX **install/runtime** stays red (unsigned, no `Add-AppxPackage`).

- [ ] **Step 4: Commit**

```powershell
git add -- scripts/Verify-MsixPackage.ps1 scripts/Verify-RuntimeGates.ps1 tests/Ether.DesignSystem.ConsumerFixtures/Packaged docs/architecture/2026-08-26-l4c-in-repo-quality-gates.md
git commit -m "Prove unsigned packaged-fixture MSIX produce without claiming install."
```

---

### Task 3: arm64 pack and fixture compile

**Files:**
- Create: `scripts/Verify-Arm64Packages.ps1`
- Modify: `scripts/Verify-RuntimeGates.ps1`
- Modify: `docs/architecture/2026-08-26-l4c-in-repo-quality-gates.md`

**Interfaces:**
- Consumes: `Directory.Build.props` already lists `Platforms` `x86;x64;arm64` and `RuntimeIdentifiers` `win-x86;win-x64;win-arm64`
- Produces: Foundation and Controls nupkgs packed with `-p:Platform=arm64`; unpackaged fixture **build** (not run) with `-p:Platform=arm64`

- [ ] **Step 1: Write `scripts/Verify-Arm64Packages.ps1`**

Pack to `artifacts/arm64-packages/` (gitignored). Restore the unpackaged fixture from `tests/Ether.DesignSystem.ConsumerFixtures/NuGet.Config` using that feed **or** pack into a temp feed then `dotnet build` the unpackaged csproj with `-p:Platform=arm64`. Assert:

- `Ether.DesignSystem.Foundation.0.1.0-preview.1.nupkg` exists
- `Ether.DesignSystem.Controls.0.1.0-preview.1.nupkg` exists
- unpackaged `build` exit code 0
- Do **not** start the arm64 exe on this x64 machine

Mirror `Invoke-DotNet` from the MSIX script. Kill leftover unpackaged x64 processes before build.

- [ ] **Step 2: Run it**

```powershell
.\scripts\Verify-Arm64Packages.ps1
```

If WASDK/arm64 targeting fails on this machine, report BLOCKED with the compiler error. Do not fake success with AnyCPU.

- [ ] **Step 3: Wire into `Verify-RuntimeGates.ps1` after the MSIX script. Do not edit `build.yml`.**

Document: arm64 **pack/build** is green; arm64 **runtime smoke** stays red.

- [ ] **Step 4: Commit**

```powershell
git add -- scripts/Verify-Arm64Packages.ps1 scripts/Verify-RuntimeGates.ps1 docs/architecture/2026-08-26-l4c-in-repo-quality-gates.md
git commit -m "Prove arm64 package pack and fixture compile without claiming arm64 runtime."
```

---

### Task 4: Documentation honesty

**Files:**
- Modify: `docs/architecture/2026-08-26-l4-gallery-control-example.md`
- Modify: `docs/architecture/2026-08-26-l4c-rtl-package-consumer.md`
- Modify: `docs/architecture/2026-08-26-foundation-refactor-full-review-handoff.md`
- Modify: `docs/releases/0.1.0-preview.1.md`
- Modify: `HANDOFF.md`
- Modify: `scripts/Verify-GalleryControlExample.ps1` only if the L4 doc assertions would break

**Interfaces:**
- Consumes: Task 1–3 evidence notes
- Produces: L4-C table that distinguishes in-repo green substitutes from still-red external gates

- [ ] **Step 1: Rewrite the L4-C table**

In `docs/architecture/2026-08-26-l4-gallery-control-example.md`, replace the deferred-only table with two sections:

**In-repo (package consumer):**

| Gate | Status |
| --- | --- |
| RTL inheritance + automation names | Green. See `docs/architecture/2026-08-26-l4c-rtl-package-consumer.md`. |
| In-process UIA snapshot (13 named controls) | Green substitute. Not Appium / out-of-process UIA. |
| 225% `ScaleTransform` + `.resw` / `x:Uid` | Green substitute. Not OS text-scale or Gallery localization. |
| Light/Dark `RenderTargetBitmap` captures | Green generate-under-artifacts. Not High Contrast, not golden-image CI. |
| Elapsed verification budget `< 20000ms` | Green harness. Not scroll/animation budgets. |
| Unsigned MSIX produce | Green produce. Install/runtime stays red. |
| arm64 pack + fixture compile | Green compile. arm64 runtime stays red. |

**Still red:**

| Gate | Why |
| --- | --- |
| Appium / out-of-process UIA | No Appium host. |
| Accessibility Insights automation | Insights engine not in CI. |
| Hosted CI WinUI smoke | Still `Verify-RuntimeGates.ps1` local-only; `build.yml` keeps `-SkipRuntimeSmoke`. |
| MSIX install/runtime | Unsigned produce only. |
| arm64 runtime smoke | Pack/build only. |
| High Contrast token parity | 145 keys missing. Frozen. |
| Preview package publish | User hold. `docs/releases/0.1.0-preview.1.md`. |

Keep the strings `Appium`, `arm64`, `RightToLeft`, `Verify-RuntimeGates`, `Deferred` (or the word `red`) so `Verify-GalleryControlExample.ps1` still passes. If you remove `Deferred`, update that script to assert `Still red` instead.

- [ ] **Step 2: Align HANDOFF, full-review handoff, release notes, RTL note**

State that RTL/UIA-in-process/scale/loc/screenshots/timing/MSIX-produce/arm64-pack are in-repo quality, and Insights/Appium/hosted GUI/install/arm64-run/publish remain red. Do not say preview is ready to push.

- [ ] **Step 3: Run**

```powershell
.\scripts\Verify-GalleryControlExample.ps1
git diff --check
git hash-object src/Ether.DesignSystem.Foundation/Resources/Tokens/EtherPrimitives.xaml
```

Expected: script pass; frozen hash still `d6ff0e5301b3672dbb492484c8d0ea584aca12be`.

- [ ] **Step 4: Commit**

```powershell
git add -- docs/architecture/2026-08-26-l4-gallery-control-example.md docs/architecture/2026-08-26-l4c-rtl-package-consumer.md docs/architecture/2026-08-26-foundation-refactor-full-review-handoff.md docs/releases/0.1.0-preview.1.md HANDOFF.md scripts/Verify-GalleryControlExample.ps1
git commit -m "Record in-repo L4-C substitutes without claiming Insights, Appium, or publish."
```
