[CmdletBinding()]
param(
    [switch]$SkipRuntimeSmoke,
    [switch]$SkipSolutionBuild,
    # Default verification compares against committed PNGs. This explicit switch is the only
    # path that copies newly captured images into the tracked baseline directory, so UI changes
    # remain visible in git diff and code review rather than being silently accepted at runtime.
    [switch]$UpdateVisualBaselines
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$artifactsRoot = [System.IO.Path]::GetFullPath((Join-Path $repoRoot 'artifacts'))
$workRoot = [System.IO.Path]::GetFullPath((Join-Path $artifactsRoot 'consumer-fixtures'))
$visualBaselinesRoot = [System.IO.Path]::GetFullPath((Join-Path $repoRoot 'tests\Ether.DesignSystem.ConsumerFixtures\VisualBaselines'))
$localFeed = Join-Path $workRoot 'local-feed'
$packageCache = Join-Path $workRoot 'packages'
$extractRoot = Join-Path $workRoot 'extracted'
$configuration = 'Debug'
$platform = 'x64'
$tfm = 'net8.0-windows10.0.19041.0'
$packageTfm = 'net8.0-windows10.0.19041'
$packageVersion = '0.1.0-preview.1'
$platformProperty = '-p:Platform=' + $platform

function Stop-LeftoverUnpackagedFixture {
    $fixtures = @(Get-Process -Name 'Ether.DesignSystem.ConsumerFixtures.Unpackaged' -ErrorAction SilentlyContinue)
    foreach ($fixture in $fixtures) {
        $null = $fixture.CloseMainWindow()
    }

    if ($fixtures.Count -gt 0) {
        $fixtures | Wait-Process -Timeout 5 -ErrorAction SilentlyContinue
        Get-Process -Name 'Ether.DesignSystem.ConsumerFixtures.Unpackaged' -ErrorAction SilentlyContinue |
            Stop-Process -Force
    }
}

if (-not $workRoot.StartsWith($artifactsRoot + [System.IO.Path]::DirectorySeparatorChar, [System.StringComparison]::OrdinalIgnoreCase)) {
    throw "Refusing to clear an output path outside artifacts: $workRoot"
}

if (-not $visualBaselinesRoot.StartsWith($repoRoot + [System.IO.Path]::DirectorySeparatorChar, [System.StringComparison]::OrdinalIgnoreCase)) {
    throw "Refusing to update a visual baseline path outside the repository: $visualBaselinesRoot"
}

Stop-LeftoverUnpackagedFixture

if (Test-Path -LiteralPath $workRoot) {
    Remove-Item -LiteralPath $workRoot -Recurse -Force
}

New-Item -ItemType Directory -Path $localFeed, $packageCache, $extractRoot -Force | Out-Null

function Invoke-DotNet {
    param([Parameter(Mandatory)][string[]]$Arguments)

    & dotnet @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet $($Arguments -join ' ') failed with exit code $LASTEXITCODE."
    }
}

function Get-PackageEntries {
    param([Parameter(Mandatory)][string]$PackagePath)

    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $archive = [System.IO.Compression.ZipFile]::OpenRead($PackagePath)
    try {
        return @($archive.Entries | ForEach-Object FullName)
    }
    finally {
        $archive.Dispose()
    }
}

function Expand-Package {
    param(
        [Parameter(Mandatory)][string]$PackagePath,
        [Parameter(Mandatory)][string]$DestinationPath
    )

    Add-Type -AssemblyName System.IO.Compression.FileSystem
    New-Item -ItemType Directory -Path $DestinationPath -Force | Out-Null

    $archive = [System.IO.Compression.ZipFile]::OpenRead($PackagePath)
    try {
        foreach ($entry in $archive.Entries) {
            if ([string]::IsNullOrEmpty($entry.Name)) {
                continue
            }

            $relativePath = $entry.FullName -replace '/', [System.IO.Path]::DirectorySeparatorChar
            $targetPath = Join-Path $DestinationPath $relativePath
            $targetDirectory = Split-Path -Parent $targetPath
            if (-not (Test-Path -LiteralPath $targetDirectory)) {
                New-Item -ItemType Directory -Path $targetDirectory -Force | Out-Null
            }

            [System.IO.Compression.ZipFileExtensions]::ExtractToFile($entry, $targetPath, $true)
        }
    }
    finally {
        $archive.Dispose()
    }
}

function Assert-PackageEntry {
    param(
        [Parameter(Mandatory)][string[]]$Entries,
        [Parameter(Mandatory)][string]$Path,
        [Parameter(Mandatory)][string]$PackageName
    )

    if ($Entries -notcontains $Path) {
        throw "$PackageName is missing required package entry '$Path'."
    }
}

function Assert-OutputFile {
    param([Parameter(Mandatory)][string]$Path)

    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        throw "Consumer output is missing required package asset '$Path'."
    }
}

function Initialize-OsHighContrastNative {
    if ('EtherConsumerFixtureHighContrast' -as [type]) {
        return
    }

    Add-Type -TypeDefinition @'
using System;
using System.Runtime.InteropServices;

public sealed class EtherConsumerFixtureHighContrastSnapshot
{
    public int DwFlags { get; set; }
    public string Scheme { get; set; }
}

public static class EtherConsumerFixtureHighContrast
{
    private const uint SpiGetHighContrast = 0x0042;
    private const uint SpiSetHighContrast = 0x0043;
    private const uint SpifUpdateIniFile = 0x0001;
    private const uint SpifSendChange = 0x0002;
    private const int SchemeBufferChars = 512;

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct HIGHCONTRAST
    {
        public int cbSize;
        public int dwFlags;
        public IntPtr lpszDefaultScheme;
    }

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool SystemParametersInfo(uint uiAction, uint uiParam, ref HIGHCONTRAST pvParam, uint fWinIni);

    public static EtherConsumerFixtureHighContrastSnapshot Get()
    {
        IntPtr buffer = Marshal.AllocHGlobal(SchemeBufferChars * 2);
        try
        {
            HIGHCONTRAST hc = new HIGHCONTRAST();
            hc.cbSize = Marshal.SizeOf(typeof(HIGHCONTRAST));
            hc.lpszDefaultScheme = buffer;
            if (!SystemParametersInfo(SpiGetHighContrast, (uint)hc.cbSize, ref hc, 0))
            {
                throw new InvalidOperationException("SPI_GETHIGHCONTRAST failed. Win32 error " + Marshal.GetLastWin32Error() + ".");
            }

            string scheme = string.Empty;
            if (hc.lpszDefaultScheme != IntPtr.Zero)
            {
                string value = Marshal.PtrToStringUni(hc.lpszDefaultScheme);
                if (value != null)
                {
                    scheme = value.TrimEnd('\0');
                }
            }

            return new EtherConsumerFixtureHighContrastSnapshot
            {
                DwFlags = hc.dwFlags,
                Scheme = scheme
            };
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    public static void Set(int dwFlags, string scheme)
    {
        IntPtr schemePtr = Marshal.StringToHGlobalUni(scheme ?? string.Empty);
        try
        {
            HIGHCONTRAST hc = new HIGHCONTRAST();
            hc.cbSize = Marshal.SizeOf(typeof(HIGHCONTRAST));
            hc.dwFlags = dwFlags;
            hc.lpszDefaultScheme = schemePtr;
            if (!SystemParametersInfo(SpiSetHighContrast, (uint)hc.cbSize, ref hc, SpifUpdateIniFile | SpifSendChange))
            {
                throw new InvalidOperationException("SPI_SETHIGHCONTRAST failed. Win32 error " + Marshal.GetLastWin32Error() + ".");
            }
        }
        finally
        {
            Marshal.FreeHGlobal(schemePtr);
        }
    }
}
'@
}

function Get-OsCurrentThemePath {
    [Microsoft.Win32.Registry]::GetValue(
        'HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Themes',
        'CurrentTheme',
        $null)
}

function Test-OsContrastThemeForbidden {
    param([string]$ThemePath)

    if ([string]::IsNullOrWhiteSpace($ThemePath)) {
        return $false
    }

    $name = [System.IO.Path]::GetFileNameWithoutExtension($ThemePath)
    $normalized = (($name -replace '\s+', ' ').Trim())
    return $normalized -match '^(?i)(aquatic|desert|night sky)$'
}

function Invoke-OsThemeFile {
    param([Parameter(Mandatory)][string]$ThemePath)

    $null = Start-Process -FilePath $ThemePath
    Start-Sleep -Seconds 2
    Get-Process -Name SystemSettings -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
}

function Get-OsHighContrastSnapshot {
    Initialize-OsHighContrastNative
    $spi = [EtherConsumerFixtureHighContrast]::Get()
    $currentTheme = Get-OsCurrentThemePath
    [pscustomobject]@{
        DwFlags = [int]$spi.DwFlags
        Scheme = [string]$spi.Scheme
        CurrentTheme = $(if ($null -eq $currentTheme) { $null } else { [string]$currentTheme })
    }
}

function Restore-OsHighContrastSnapshot {
    param($Snapshot)

    if ($null -eq $Snapshot) {
        return
    }

    try {
        Initialize-OsHighContrastNative
        [EtherConsumerFixtureHighContrast]::Set([int]$Snapshot.DwFlags, [string]$Snapshot.Scheme)

        $snapshotTheme = [string]$Snapshot.CurrentTheme
        if ([string]::IsNullOrWhiteSpace($snapshotTheme)) {
            return
        }

        $currentTheme = Get-OsCurrentThemePath
        if ([string]::Equals([string]$currentTheme, $snapshotTheme, [StringComparison]::OrdinalIgnoreCase)) {
            return
        }

        if (-not (Test-Path -LiteralPath $snapshotTheme -PathType Leaf)) {
            return
        }

        if (Test-OsContrastThemeForbidden $snapshotTheme) {
            return
        }

        Invoke-OsThemeFile $snapshotTheme
    }
    catch {
        Write-Warning "Failed to restore OS High Contrast snapshot: $_"
    }
}

function Assert-NoProjectReference {
    param([Parameter(Mandatory)][string]$ProjectPath)

    if (Select-String -LiteralPath $ProjectPath -Pattern '<ProjectReference(?:\s|>)' -Quiet) {
        throw "Consumer fixture '$ProjectPath' must restore only NuGet packages, not a ProjectReference."
    }
}

function Assert-EtherPackageReferences {
    param([Parameter(Mandatory)][string]$ProjectPath)

    [xml]$project = Get-Content -LiteralPath $ProjectPath -Raw
    $etherReferences = @($project.SelectNodes('//PackageReference') | Where-Object { $_.Include -like 'Ether.DesignSystem.*' })
    $expected = @('Ether.DesignSystem.Controls', 'Ether.DesignSystem.Interactions')
    if ($etherReferences.Count -ne $expected.Count -or
        @($etherReferences | Where-Object { $_.Include -notin $expected -or $_.VersionOverride -ne $packageVersion }).Count -ne 0) {
        throw "Consumer fixture '$ProjectPath' must reference Ether.DesignSystem.Controls and Ether.DesignSystem.Interactions $packageVersion."
    }
}

function Assert-FoundationFlowsTransitively {
    param([Parameter(Mandatory)][string]$AssetsPath)

    $assets = Get-Content -LiteralPath $AssetsPath -Raw | ConvertFrom-Json
    $framework = @($assets.project.frameworks.PSObject.Properties | Select-Object -First 1)[0].Value
    if ($framework.dependencies.PSObject.Properties.Name -contains 'Ether.DesignSystem.Foundation') {
        throw "Consumer assets file '$AssetsPath' contains a direct Ether.DesignSystem.Foundation dependency."
    }

    if ($assets.libraries.PSObject.Properties.Name -notcontains "Ether.DesignSystem.Foundation/$packageVersion") {
        throw "Consumer assets file '$AssetsPath' is missing the transitive Ether.DesignSystem.Foundation package."
    }
}

function Get-ZipEntrySha256 {
    param(
        [Parameter(Mandatory)][string]$PackagePath,
        [Parameter(Mandatory)][string]$EntryPath
    )

    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $archive = [System.IO.Compression.ZipFile]::OpenRead($PackagePath)
    try {
        $entry = @($archive.Entries | Where-Object { $_.FullName -ceq $EntryPath })[0]
        if ($null -eq $entry) {
            throw "Package '$PackagePath' does not contain expected entry '$EntryPath'."
        }

        $stream = $entry.Open()
        try {
            $sha256 = [System.Security.Cryptography.SHA256]::Create()
            try {
                return [System.Convert]::ToHexString($sha256.ComputeHash($stream)).ToLowerInvariant()
            }
            finally {
                $sha256.Dispose()
            }
        }
        finally {
            $stream.Dispose()
        }
    }
    finally {
        $archive.Dispose()
    }
}

# Guards against the stale-artifact trap that has bitten this project three times before
# (a silently reused yesterday's package, a --no-restore build masking an uncleaned
# dependency, and a fixture that kept compiling against a private member because it was
# still linked to an old package). The isolated $packageCache restore directory already
# prevents NuGet from reusing a previously cached Ether package for this version number,
# but that only guarantees the *mechanism* took a fresh path - it does not by itself prove
# the DLL the fixture actually links against is byte-for-byte the one this run just packed
# into $localFeed. Comparing SHA-256 hashes of the same compile asset in both places proves
# the *result*, not just the mechanism.
function Assert-RestoredControlsPackageMatchesLocalFeed {
    param(
        [Parameter(Mandatory)][string]$Variant,
        [Parameter(Mandatory)][string]$ProjectRoot
    )

    $assetsPath = Join-Path $ProjectRoot 'obj/project.assets.json'
    if (-not (Test-Path -LiteralPath $assetsPath -PathType Leaf)) {
        throw "$Variant restore did not produce '$assetsPath'."
    }

    $assets = Get-Content -LiteralPath $assetsPath -Raw | ConvertFrom-Json
    $libraryKey = "Ether.DesignSystem.Controls/$packageVersion"
    $library = $assets.libraries.PSObject.Properties[$libraryKey].Value
    if ($null -eq $library) {
        throw "$Variant assets file did not resolve '$libraryKey'."
    }

    $target = @($assets.targets.PSObject.Properties |
        Where-Object { $_.Name -like 'net8.0-windows10.0.19041*' } |
        Select-Object -First 1)[0]
    if ($null -eq $target) {
        throw "$Variant assets file has no net8.0-windows10.0.19041 target."
    }

    $targetLibrary = $target.Value.PSObject.Properties[$libraryKey].Value
    $libraryAsset = @($targetLibrary.compile.PSObject.Properties.Name |
        Where-Object { $_ -like 'lib/*/Ether.DesignSystem.Controls.dll' } |
        Select-Object -First 1)[0]
    if ([string]::IsNullOrWhiteSpace($libraryAsset)) {
        throw "$Variant assets file has no compile asset for '$libraryKey'."
    }

    $restoredDll = Join-Path (Join-Path $packageCache $library.path) ($libraryAsset -replace '/', '\\')
    if (-not (Test-Path -LiteralPath $restoredDll -PathType Leaf)) {
        throw "$Variant resolved Controls assembly is absent from this run's isolated package cache: '$restoredDll'."
    }

    $packagePath = Join-Path $localFeed "Ether.DesignSystem.Controls.$packageVersion.nupkg"
    if (-not (Test-Path -LiteralPath $packagePath -PathType Leaf)) {
        throw "Current-run Controls package was not found in local feed: '$packagePath'."
    }

    $expectedHash = Get-ZipEntrySha256 $packagePath $libraryAsset
    $restoredHash = (Get-FileHash -LiteralPath $restoredDll -Algorithm SHA256).Hash.ToLowerInvariant()
    if ($restoredHash -cne $expectedHash) {
        throw "$Variant resolved Controls DLL does not match this run's freshly packed local feed package. " +
            "Expected SHA-256 (from '$packagePath' entry '$libraryAsset') = $expectedHash; " +
            "Actual SHA-256 (from '$restoredDll') = $restoredHash."
    }

    Write-Host "$Variant verified current-run Controls package: $libraryAsset SHA-256=$restoredHash"
}

function Assert-NoPackageEntriesMatching {
    param(
        [Parameter(Mandatory)][string[]]$Entries,
        [Parameter(Mandatory)][string]$Pattern,
        [Parameter(Mandatory)][string]$PackageName
    )

    if (@($Entries | Where-Object { $_ -match $Pattern }).Count -ne 0) {
        throw "$PackageName contains unexpected package entries matching '$Pattern'."
    }
}

function Assert-FileContains {
    param(
        [Parameter(Mandatory)][string]$Path,
        [Parameter(Mandatory)][string]$Pattern,
        [Parameter(Mandatory)][string]$Description
    )

    if (-not (Select-String -LiteralPath $Path -Pattern $Pattern -Quiet)) {
        throw "$Description is missing expected content matching '$Pattern'."
    }
}

function Assert-FileDoesNotContain {
    param(
        [Parameter(Mandatory)][string]$Path,
        [Parameter(Mandatory)][string]$Pattern,
        [Parameter(Mandatory)][string]$Description
    )

    if (Select-String -LiteralPath $Path -Pattern $Pattern -Quiet) {
        throw "$Description contains forbidden content matching '$Pattern'."
    }
}

function Assert-FixtureAutomationNames {
    param([Parameter(Mandatory)][string]$XamlPath)

    $xaml = Get-Content -LiteralPath $XamlPath -Raw
    $requiredNames = @(
        'Package download progress',
        'Package button',
        'Package checkbox',
        'Package radio button',
        'Package input',
        'Package dropdown',
        'Package segmented control',
        'Package intelligence button',
        'Package steering bar',
        'Package slider',
        'Package masthead',
        'Package toggle switch',
        'Package scroll bar'
    )
    foreach ($name in $requiredNames) {
        $pattern = 'AutomationProperties.Name="' + [regex]::Escape($name) + '"'
        if ($xaml -notmatch $pattern) {
            throw "$XamlPath is missing required AutomationProperties.Name '$name'."
        }
    }
}

function Assert-RtlMarker {
    param($RuntimeResult)

    $rtl = $RuntimeResult.rtl
    if ($null -eq $rtl) {
        throw 'Unpackaged runtime marker is missing the rtl object.'
    }
    if ($rtl.rootFlowDirection -ne 'RightToLeft') {
        throw "RTL root FlowDirection was '$($rtl.rootFlowDirection)', expected RightToLeft."
    }

    $expected = @{
        progressBar          = 'Package download progress'
        button               = 'Package button'
        checkbox             = 'Package checkbox'
        radioButton          = 'Package radio button'
        input                = 'Package input'
        dropdown             = 'Package dropdown'
        segmentedControl     = 'Package segmented control'
        intelligenceButton   = 'Package intelligence button'
        steeringBar          = 'Package steering bar'
        slider               = 'Package slider'
        masthead             = 'Package masthead'
        toggleSwitch         = 'Package toggle switch'
        scrollBar            = 'Package scroll bar'
    }
    $controls = @($rtl.controls)
    foreach ($id in $expected.Keys) {
        $row = @($controls | Where-Object { $_.id -eq $id })[0]
        if ($null -eq $row) {
            throw "RTL marker is missing control '$id'."
        }
        if ($row.flowDirection -ne 'RightToLeft') {
            throw "RTL '$id' FlowDirection was '$($row.flowDirection)', expected RightToLeft."
        }
        if ($row.automationName -ne $expected[$id]) {
            throw "RTL '$id' automation name was '$($row.automationName)', expected '$($expected[$id])'."
        }
    }
}

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
        $expectedSource = if ($id -eq 'dropdown') { 'LayoutFallback' } else { 'Uia' }
        if ($row.boundingSource -ne $expectedSource) {
            throw "UIA '$id' boundingSource was '$($row.boundingSource)', expected '$expectedSource'."
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
        if ([double]$row.desiredWidth -gt ([double]$row.actualWidth + 0.5) -or
            [double]$row.desiredHeight -gt ([double]$row.actualHeight + 0.5)) {
            throw "textScale '$id' content was clipped: DesiredSize=$($row.desiredWidth)x$($row.desiredHeight), ActualSize=$($row.actualWidth)x$($row.actualHeight)."
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

    $expectedControlIds = @(
        'progressBar', 'button', 'checkbox', 'radioButton', 'input', 'dropdown',
        'segmentedControl', 'intelligenceButton', 'steeringBar', 'slider', 'masthead',
        'toggleSwitch', 'scrollBar')
    $controlCaptures = @($screenshots.controls)
    if ($controlCaptures.Count -ne $expectedControlIds.Count -or
        @($expectedControlIds | Where-Object { $_ -cnotin @($controlCaptures | ForEach-Object { $_.id }) }).Count -ne 0) {
        throw "Control screenshot matrix did not contain the expected $($expectedControlIds.Count) component ids."
    }
    foreach ($capture in $controlCaptures) {
        foreach ($name in @('lightPath', 'darkPath')) {
            $path = [string]$capture.$name
            if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
                throw "Control screenshot '$($capture.id)/$name' is missing: $path"
            }
            # A 15 x 150 ScrollBar image is legitimately highly compressible (451 bytes
            # in the current fixture), so only reject truly empty/truncated PNG outputs.
            if ((Get-Item -LiteralPath $path).Length -lt 128) {
                throw "Control screenshot '$($capture.id)/$name' is too small."
            }
        }
        if ([int]$capture.lightPixelWidth -le 0 -or [int]$capture.lightPixelHeight -le 0 -or
            [int]$capture.darkPixelWidth -le 0 -or [int]$capture.darkPixelHeight -le 0) {
            throw "Control screenshot '$($capture.id)' has non-positive dimensions."
        }
        # Byte-identical Light/Dark captures mean a control silently failed to react to
        # theme change (a hardcoded brush, or a theme dictionary duplicated across Light
        # and Dark). Catch that regression here instead of only at the whole-page level.
        $controlLightHash = (Get-FileHash -LiteralPath $capture.lightPath -Algorithm SHA256).Hash
        $controlDarkHash = (Get-FileHash -LiteralPath $capture.darkPath -Algorithm SHA256).Hash
        if ($controlLightHash -eq $controlDarkHash) {
            throw "Control screenshot '$($capture.id)' Light and Dark captures are byte-identical; the control did not react to theme change."
        }
    }
}

function Assert-NoConsumerFixtureInternalsVisibleTo {
    $violations = @(
        Get-ChildItem -LiteralPath (Join-Path $repoRoot 'src') -Filter '*.csproj' -Recurse |
            Select-String -Pattern '<InternalsVisibleTo\s+Include\s*=\s*["''].*ConsumerFixtures' |
            ForEach-Object { "$($_.Path):$($_.LineNumber): $($_.Line.Trim())" })
    if ($violations.Count -gt 0) {
        throw "Published projects must not grant InternalsVisibleTo to ConsumerFixtures. Found: $($violations -join '; ')"
    }
}

function Update-VisualBaselines {
    param($RuntimeResult)

    # Assert-ScreenshotMarker has already verified the complete Light/Dark matrix. Copy exactly
    # those accepted captures; never clear this source-controlled directory as part of a normal
    # gate run. The resulting PNG changes are intentionally left in git diff for review.
    $controlsDirectory = Join-Path $visualBaselinesRoot 'controls'
    New-Item -ItemType Directory -Path $controlsDirectory -Force | Out-Null
    foreach ($capture in @($RuntimeResult.screenshots.controls)) {
        foreach ($theme in @('light', 'dark')) {
            $sourcePath = [string]$capture.($theme + 'Path')
            $destinationPath = Join-Path $controlsDirectory ("{0}-{1}.png" -f $capture.id, $theme)
            Copy-Item -LiteralPath $sourcePath -Destination $destinationPath -Force
        }
    }

    Write-Warning "Updated 26 visual baselines under $visualBaselinesRoot. Review the PNG changes in git diff before committing; ordinary runs only compare and never overwrite them."
}

function Assert-HighContrastMarker {
    param($RuntimeResult)

    if (-not ($RuntimeResult.PSObject.Properties.Name -contains 'highContrast')) {
        throw 'Unpackaged runtime marker is missing the highContrast object.'
    }
    $highContrast = $RuntimeResult.highContrast
    if ($null -eq $highContrast) {
        throw 'Unpackaged runtime marker is missing the highContrast object.'
    }
    if (-not ($highContrast.PSObject.Properties.Name -contains 'scheme')) {
        throw 'Unpackaged runtime marker is missing highContrast.scheme.'
    }
    if ($highContrast.osHighContrast -ne $true) {
        throw "highContrast.osHighContrast was '$($highContrast.osHighContrast)', expected true."
    }
    if ($highContrast.dictionaryForced -ne $false) {
        throw "highContrast.dictionaryForced was '$($highContrast.dictionaryForced)', expected false."
    }
    $scheme = [string]$highContrast.scheme
    if ([string]::IsNullOrWhiteSpace($scheme)) {
        throw "highContrast.scheme was '$scheme', expected a non-empty scheme or theme file name."
    }
    $canvas = [string]$highContrast.backgroundCanvasColor
    if ($canvas -notmatch '^#[0-9A-Fa-f]{8}$') {
        throw "highContrast.backgroundCanvasColor was '$canvas', expected a #AARRGGBB hex string."
    }
    $lightCanvas = [string]$RuntimeResult.lightBackgroundCanvasColor
    $darkCanvas = [string]$RuntimeResult.darkBackgroundCanvasColor
    if (-not ($canvas -cne $lightCanvas -and $canvas -cne $darkCanvas)) {
        throw "highContrast.backgroundCanvasColor was '$canvas', expected a # hex string different from Light '$lightCanvas' and Dark '$darkCanvas'."
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
        throw 'HighContrast and Light screenshots are identical; OS-selected HighContrast did not differ.'
    }
    if ($hcHash -eq $darkHash) {
        throw 'HighContrast and Dark screenshots are identical; OS-selected HighContrast did not differ.'
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

function Assert-FixtureStatusUid {
    param([Parameter(Mandatory)][string]$XamlPath)

    if (-not (Select-String -LiteralPath $XamlPath -Pattern 'x:Uid="FixtureStatus"' -Quiet)) {
        throw "$XamlPath is missing x:Uid=`"FixtureStatus`"."
    }
}

Push-Location $repoRoot
try {
    Assert-NoConsumerFixtureInternalsVisibleTo
    Assert-FixtureAutomationNames (Join-Path $repoRoot 'tests\Ether.DesignSystem.ConsumerFixtures\Unpackaged\MainWindow.xaml')
    Assert-FixtureAutomationNames (Join-Path $repoRoot 'tests\Ether.DesignSystem.ConsumerFixtures\Packaged\MainWindow.xaml')
    Assert-FixtureStatusUid (Join-Path $repoRoot 'tests\Ether.DesignSystem.ConsumerFixtures\Unpackaged\MainWindow.xaml')
    Assert-FixtureStatusUid (Join-Path $repoRoot 'tests\Ether.DesignSystem.ConsumerFixtures\Packaged\MainWindow.xaml')

    if (-not $SkipSolutionBuild) {
        Invoke-DotNet @('build', 'Ether.DesignSystem.slnx', '-c', $configuration, $platformProperty)
        Invoke-DotNet @('build', 'Ether.DesignSystem.slnx', '-c', 'Release', $platformProperty)
    }

    $foundationProject = 'src/Ether.DesignSystem.Foundation/Ether.DesignSystem.Foundation.csproj'
    $controlsProject = 'src/Ether.DesignSystem.Controls/Ether.DesignSystem.Controls.csproj'
    $interactionsProject = 'src/Ether.DesignSystem.Interactions/Ether.DesignSystem.Interactions.csproj'
    Invoke-DotNet @('pack', $foundationProject, '-c', $configuration, $platformProperty, '-o', $localFeed)
    Invoke-DotNet @('pack', $controlsProject, '-c', $configuration, $platformProperty, '-o', $localFeed)
    Invoke-DotNet @('pack', $interactionsProject, '-c', $configuration, $platformProperty, '-o', $localFeed)

    $foundationPackage = Join-Path $localFeed "Ether.DesignSystem.Foundation.$packageVersion.nupkg"
    $controlsPackage = Join-Path $localFeed "Ether.DesignSystem.Controls.$packageVersion.nupkg"
    $interactionsPackage = Join-Path $localFeed "Ether.DesignSystem.Interactions.$packageVersion.nupkg"
    $foundationEntries = Get-PackageEntries $foundationPackage
    $controlsEntries = Get-PackageEntries $controlsPackage
    $interactionsEntries = Get-PackageEntries $interactionsPackage

    $foundationLib = "lib/$packageTfm/Ether.DesignSystem.Foundation.dll"
    $foundationPri = "lib/$packageTfm/Ether.DesignSystem.Foundation.pri"
    Assert-PackageEntry $foundationEntries $foundationLib 'Ether.DesignSystem.Foundation'
    Assert-PackageEntry $foundationEntries $foundationPri 'Ether.DesignSystem.Foundation'
    Assert-PackageEntry $foundationEntries "lib/$packageTfm/Ether.DesignSystem.Foundation/Themes/Foundation.xaml" 'Ether.DesignSystem.Foundation'
    Assert-PackageEntry $foundationEntries "lib/$packageTfm/Ether.DesignSystem.Foundation/Themes/Foundation.xbf" 'Ether.DesignSystem.Foundation'
    Assert-PackageEntry $foundationEntries "lib/$packageTfm/Ether.DesignSystem.Foundation/Resources/Tokens/EtherTypography.xaml" 'Ether.DesignSystem.Foundation'
    Assert-PackageEntry $foundationEntries "lib/$packageTfm/Ether.DesignSystem.Foundation/Resources/Tokens/EtherTypography.xbf" 'Ether.DesignSystem.Foundation'
    Assert-PackageEntry $foundationEntries 'contentFiles/any/any/Fonts/Instrument_Sans/InstrumentSans-VariableFont_wdth,wght.ttf' 'Ether.DesignSystem.Foundation'
    Assert-PackageEntry $foundationEntries 'contentFiles/any/any/Assets/Icons/dds2/dds2_add-cir.svg' 'Ether.DesignSystem.Foundation'
    Assert-NoPackageEntriesMatching $foundationEntries '^content/' 'Ether.DesignSystem.Foundation'
    Assert-PackageEntry $foundationEntries 'buildTransitive/Ether.DesignSystem.Foundation.targets' 'Ether.DesignSystem.Foundation'

    $controlsLib = "lib/$packageTfm/Ether.DesignSystem.Controls.dll"
    $controlsPri = "lib/$packageTfm/Ether.DesignSystem.Controls.pri"
    Assert-PackageEntry $controlsEntries $controlsLib 'Ether.DesignSystem.Controls'
    Assert-PackageEntry $controlsEntries $controlsPri 'Ether.DesignSystem.Controls'
    Assert-PackageEntry $controlsEntries "lib/$packageTfm/Ether.DesignSystem.Controls/Themes/DesignSystem.xaml" 'Ether.DesignSystem.Controls'
    Assert-PackageEntry $controlsEntries "lib/$packageTfm/Ether.DesignSystem.Controls/Themes/Generic.xaml" 'Ether.DesignSystem.Controls'
    Assert-PackageEntry $controlsEntries "lib/$packageTfm/Ether.DesignSystem.Controls/Themes/DesignSystem.xbf" 'Ether.DesignSystem.Controls'
    Assert-NoPackageEntriesMatching $controlsEntries '^buildTransitive/' 'Ether.DesignSystem.Controls'
    Assert-PackageEntry $interactionsEntries "lib/$packageTfm/Ether.DesignSystem.Interactions.dll" 'Ether.DesignSystem.Interactions'

    Expand-Package $foundationPackage (Join-Path $extractRoot 'Foundation')
    Expand-Package $controlsPackage (Join-Path $extractRoot 'Controls')
    $foundationAssetTarget = Join-Path $extractRoot 'Foundation/buildTransitive/Ether.DesignSystem.Foundation.targets'
    Assert-FileContains $foundationAssetTarget 'IncludeEtherDesignSystemFoundationHostRootAssets' 'Foundation buildTransitive asset target'
    Assert-FileContains $foundationAssetTarget 'contentFiles\\any\\any\\' 'Foundation buildTransitive asset target'
    Assert-FileContains $foundationAssetTarget 'Assets\\' 'Foundation buildTransitive asset target'
    Assert-FileContains $foundationAssetTarget 'Fonts\\' 'Foundation buildTransitive asset target'
    Assert-FileDoesNotContain $foundationAssetTarget 'ReferencePath' 'Foundation buildTransitive asset target'
    [xml]$controlsNuspec = Get-Content -LiteralPath (Join-Path $extractRoot 'Controls/Ether.DesignSystem.Controls.nuspec') -Raw
    $dependency = @($controlsNuspec.package.metadata.dependencies.group.dependency | Where-Object { $_.id -eq 'Ether.DesignSystem.Foundation' })
    if ($dependency.Count -ne 1 -or $dependency[0].version -notmatch '0.1.0-preview.1') {
        throw 'Ether.DesignSystem.Controls does not declare the expected Ether.DesignSystem.Foundation preview dependency.'
    }

    $fixtureProjects = @(
        'tests/Ether.DesignSystem.ConsumerFixtures/Unpackaged/Ether.DesignSystem.ConsumerFixtures.Unpackaged.csproj',
        'tests/Ether.DesignSystem.ConsumerFixtures/Packaged/Ether.DesignSystem.ConsumerFixtures.Packaged.csproj'
    )
    $fixtureNuGetConfig = 'tests/Ether.DesignSystem.ConsumerFixtures/NuGet.Config'
    foreach ($fixtureProject in $fixtureProjects) {
        Assert-NoProjectReference $fixtureProject
        Assert-EtherPackageReferences $fixtureProject
        Invoke-DotNet @('restore', $fixtureProject, '--configfile', $fixtureNuGetConfig, '--packages', $packageCache)
        Assert-FoundationFlowsTransitively (Join-Path (Split-Path -Parent $fixtureProject) 'obj/project.assets.json')
        Assert-RestoredControlsPackageMatchesLocalFeed (Split-Path -Leaf (Split-Path -Parent $fixtureProject)) (Split-Path -Parent $fixtureProject)
        Invoke-DotNet @('build', $fixtureProject, '-c', $configuration, $platformProperty, '--no-restore')
    }

    $unpackagedOutput = Join-Path $repoRoot "tests/Ether.DesignSystem.ConsumerFixtures/Unpackaged/bin/$platform/$configuration/$tfm"
    Assert-OutputFile (Join-Path $unpackagedOutput 'Fonts/Instrument_Sans/InstrumentSans-VariableFont_wdth,wght.ttf')
    Assert-OutputFile (Join-Path $unpackagedOutput 'Fonts/Inter/Inter-VariableFont_opsz,wght.ttf')
    Assert-OutputFile (Join-Path $unpackagedOutput 'Assets/Icons/dds2/dds2_add-cir.svg')
    Assert-OutputFile (Join-Path $unpackagedOutput 'Ether.DesignSystem.Foundation.dll')
    Assert-OutputFile (Join-Path $unpackagedOutput 'Ether.DesignSystem.Controls.dll')
    Assert-OutputFile (Join-Path $unpackagedOutput 'Ether.DesignSystem.Interactions.dll')

    $packagedOutput = Join-Path $repoRoot "tests/Ether.DesignSystem.ConsumerFixtures/Packaged/bin/$platform/$configuration/$tfm"
    Assert-OutputFile (Join-Path $packagedOutput 'Ether.DesignSystem.Foundation.dll')
    Assert-OutputFile (Join-Path $packagedOutput 'Ether.DesignSystem.Controls.dll')
    Assert-OutputFile (Join-Path $packagedOutput 'Ether.DesignSystem.Interactions.dll')
    Assert-OutputFile (Join-Path $packagedOutput 'Ether.DesignSystem.Foundation/Resources/Tokens/EtherTypography.xbf')
    Assert-OutputFile (Join-Path $packagedOutput 'Ether.DesignSystem.Controls/Themes/DesignSystem.xbf')

    if ($SkipRuntimeSmoke) {
        Write-Host 'Unpackaged runtime smoke skipped by explicit -SkipRuntimeSmoke.'
    }
    else {
        $unpackagedExe = Join-Path $unpackagedOutput 'Ether.DesignSystem.ConsumerFixtures.Unpackaged.exe'
        $markerPath = Join-Path $workRoot ("runtime-result-{0}.json" -f [guid]::NewGuid().ToString('N'))
        $previousSmokeValue = [Environment]::GetEnvironmentVariable('ETHER_CONSUMER_SMOKE', 'Process')
        $previousMarkerPath = [Environment]::GetEnvironmentVariable('ETHER_CONSUMER_SMOKE_RESULT_PATH', 'Process')
        $previousScreenshotDir = [Environment]::GetEnvironmentVariable('ETHER_CONSUMER_SMOKE_SCREENSHOT_DIR', 'Process')
        $previousUpdateVisualBaselines = [Environment]::GetEnvironmentVariable('ETHER_CONSUMER_UPDATE_VISUAL_BASELINES', 'Process')
        $smokeProcess = $null
        $osHighContrastSnapshot = Get-OsHighContrastSnapshot
        try {
            [Environment]::SetEnvironmentVariable('ETHER_CONSUMER_SMOKE', '1', 'Process')
            [Environment]::SetEnvironmentVariable('ETHER_CONSUMER_SMOKE_RESULT_PATH', $markerPath, 'Process')
            [Environment]::SetEnvironmentVariable('ETHER_CONSUMER_SMOKE_SCREENSHOT_DIR', (Join-Path $workRoot 'screenshots'), 'Process')
            $updateVisualBaselinesValue = if ($UpdateVisualBaselines) { '1' } else { $null }
            [Environment]::SetEnvironmentVariable('ETHER_CONSUMER_UPDATE_VISUAL_BASELINES', $updateVisualBaselinesValue, 'Process')
            $smokeProcess = Start-Process -FilePath $unpackagedExe -WorkingDirectory $unpackagedOutput -PassThru -WindowStyle Hidden
            # The attached visual-property audit (482 properties) now waits deterministically
            # for compositor settle after each mutation: it samples the rendered bitmap
            # fingerprint a composition frame apart until 3 consecutive samples match (throwing
            # if that never happens within 60 samples) instead of a single Task.Yield(), so the
            # fixture legitimately takes longer than the 90 s this budget originally assumed.
            # Observed full runs land around 220-230 s; 560 s keeps generous headroom above that
            # plus the rest of the runtime marker, without being so tight that routine machine
            # jitter trips a false timeout.
            if (-not $smokeProcess.WaitForExit(560000)) {
                Stop-Process -Id $smokeProcess.Id -Force
                $null = $smokeProcess.WaitForExit(10000)
                Restore-OsHighContrastSnapshot $osHighContrastSnapshot
                throw "The unpackaged runtime smoke fixture timed out before writing its result marker: $markerPath"
            }
            if ($smokeProcess.ExitCode -ne 0) {
                throw "The unpackaged runtime smoke fixture exited with code $($smokeProcess.ExitCode)."
            }
            if (-not (Test-Path -LiteralPath $markerPath -PathType Leaf)) {
                throw "The unpackaged runtime smoke fixture exited without its required result marker: $markerPath"
            }
            $runtimeResult = Get-Content -LiteralPath $markerPath -Raw | ConvertFrom-Json
            $expectedResourceKeys = @('Spacing8', 'InterFont', 'InstrumentSans', 'IconAddCir')
            $expectedAssetUris = @(
                'ms-appx:///Fonts/Instrument_Sans/InstrumentSans-VariableFont_wdth%2Cwght.ttf',
                'ms-appx:///Fonts/Inter/Inter-VariableFont_opsz%2Cwght.ttf',
                'ms-appx:///Assets/Icons/dds2/dds2_add-cir.svg'
            )
            $reportedResourceKeys = @($runtimeResult.resourceKeys)
            $reportedAssets = @($runtimeResult.assets)
            $reportedAssetUris = @($reportedAssets | ForEach-Object uri)
            $attemptedAssetUris = @($runtimeResult.attemptedAssetUris)
            $expectedFontSources = @(
                'ms-appx:///Fonts/Instrument_Sans/InstrumentSans-VariableFont_wdth,wght.ttf#Instrument Sans',
                'ms-appx:///Fonts/Inter/Inter-VariableFont_opsz,wght.ttf#Inter'
            )
            $reportedFontSources = @($runtimeResult.fontFamilySources)
            $progressBar = $runtimeResult.progressBar
            $button = $runtimeResult.button
            $checkbox = $runtimeResult.checkbox
            $radioButton = $runtimeResult.radioButton
            $etherInput = $runtimeResult.input
            $dropdown = $runtimeResult.dropdown
            $segmentedControl = $runtimeResult.segmentedControl
            $intelligenceButton = $runtimeResult.intelligenceButton
            $steeringBar = $runtimeResult.steeringBar
            $slider = $runtimeResult.slider
            $masthead = $runtimeResult.masthead
            $toggleSwitch = $runtimeResult.toggleSwitch
            $scrollBar = $runtimeResult.scrollBar
            $expectedProgressTemplateParts = @('LayoutRoot', 'FillColumn', 'RestColumn', 'LabelRow', 'TitleText', 'ValueLabel')
            $expectedLabelStates = @('BothLabelsVisible', 'TitleOnly', 'ValueOnly', 'LabelsHidden')
            $expectedProgressFillLight = @('#FF0055FF')
            $expectedProgressFillDark = @('#FF2576FF')
            $expectedButtonTemplateParts = @('RightIcon')
            $expectedRightIconStates = @('RightIconVisible', 'RightIconCollapsed')
            $expectedCheckboxTemplateParts = @('UncheckedFill', 'UncheckedFace', 'CheckedFace', 'Glyph', 'Label')
            $expectedRadioButtonTemplateParts = @('UncheckedFill', 'UncheckedFace', 'CheckedFace', 'Dot', 'Label')
            $expectedCheckStates = @('Unchecked', 'Checked')
            $expectedInputTemplateParts = @('LayoutRoot', 'BorderElement', 'PlaceholderTextContentPresenter', 'ContentElement')
            $expectedInputCommonStates = @('Normal', 'PointerOver', 'Focused', 'Disabled')
            $expectedDropdownTemplateParts = @('TriggerText', 'Arrow', 'Popup', 'PopupBorder', 'ScrollViewer')
            $expectedDropDownStates = @('Opened', 'Closed')
            $expectedSegmentedControlTemplateParts = @('ShadowHost', 'TrackSurface')
            $expectedSegmentCheckStates = @('Unchecked', 'Checked')
            $expectedIntelligenceButtonTemplateParts = @('Bg', 'BorderIntelligenceBlue', 'BorderIntelligenceGradient', 'BlueGlowOuter', 'BlueGlowMiddle', 'BlueGlowCore', 'PurpleGlow', 'Cp', 'FocusRing')
            $expectedIntelligenceButtonCommonStates = @('Normal', 'PointerOver', 'Pressed', 'Disabled')
            $expectedSteeringBarTemplateParts = @('LayoutRoot', 'InteractionSurface', 'FillBorder', 'ThumbHost', 'LabelRow', 'TitleText', 'ValueLabel')
            $expectedSteeringBarLabelStates = @('BothLabelsVisible', 'TitleOnly', 'ValueOnly', 'LabelsHidden')
            $expectedSteeringBarGradient = @('#FF0021F3', '#FF0015FF', '#FF0EB2FF', '#FF40E1FD')
            $expectedSliderTemplateParts = @('ValueText', 'BarCanvas', 'Knob', 'LabelRow')
            $expectedMastheadTemplateParts = @('SearchIconSlot', 'SettingsButton', 'MinimizeButton', 'MaximizeRestoreButton', 'CloseButton')
            $expectedMastheadOptionalIconStates = @('SearchCollapsed', 'SearchVisible')
            $expectedToggleSwitchTemplateParts = @('TrackOff', 'TrackOn', 'KnobFill')
            $expectedToggleSwitchStates = @('Off', 'On')
            $expectedConsumedProperties = @(
                'EtherButton.LeftIcon', 'EtherButton.RightIcon', 'EtherButton.Size', 'EtherButton.Variant',
                'EtherDropdown.MaxVisibleItems', 'EtherDropdown.MenuGap',
                'EtherProgressBar.ShowTitle', 'EtherProgressBar.ShowValue', 'EtherProgressBar.Title', 'EtherProgressBar.ValueContent',
                'EtherSegmentPanel.Spacing', 'EtherSegmentedControl.SelectedValue',
                'EtherSlider.Labels', 'EtherSlider.ShowLabels', 'EtherSlider.ShowTitle', 'EtherSlider.SnapToStops', 'EtherSlider.Stops', 'EtherSlider.Title',
                'EtherSteeringBar.LargeChange', 'EtherSteeringBar.Maximum', 'EtherSteeringBar.Minimum', 'EtherSteeringBar.ShowStops', 'EtherSteeringBar.ShowTitle', 'EtherSteeringBar.ShowValue', 'EtherSteeringBar.SmallChange', 'EtherSteeringBar.SnapToStops', 'EtherSteeringBar.Stops', 'EtherSteeringBar.Title', 'EtherSteeringBar.Value', 'EtherSteeringBar.ValueContent',
                'EtherMasthead.EnableWindowCommands', 'EtherMasthead.ShowChevron', 'EtherMasthead.ShowMenuIcon', 'EtherMasthead.ShowSearch', 'EtherMasthead.ShowSettings'
            )
            $expectedWritablePublicProperties = 1388
            $expectedVisualPublicProperties = 445
            $expectedSemanticPublicProperties = 69
            $expectedPlatformPublicProperties = 874
            $allowedVisualEvidenceMethods = @('pixel-difference', 'layout-difference', 'visibility-transition', 'platform-dp-contract', 'ether-component-dp-contract', 'platform-clr-visual-contract')
            $missingResources = @($expectedResourceKeys | Where-Object { $_ -cnotin $reportedResourceKeys })
            $missingAssets = @($expectedAssetUris | Where-Object { $_ -cnotin $reportedAssetUris })
            $emptyAssets = @($reportedAssets | Where-Object { [uint64]$_.size -eq 0 })
            $assetsWithoutStorageFileDiagnosis = @($reportedAssets | Where-Object {
                $_.storageFileResolved -ne $true -and [string]::IsNullOrWhiteSpace($_.storageFileError)
            })
            if ($runtimeResult.marker -ne 'ETHER_CONSUMER_SMOKE' -or
                $runtimeResult.outcome -ne 'success' -or
                $missingResources.Count -ne 0 -or
                $missingAssets.Count -ne 0 -or
                @($expectedAssetUris | Where-Object { $_ -cnotin $attemptedAssetUris }).Count -ne 0 -or
                $emptyAssets.Count -ne 0 -or
                $assetsWithoutStorageFileDiagnosis.Count -ne 0 -or
                @($expectedFontSources | Where-Object { $_ -cnotin $reportedFontSources }).Count -ne 0 -or
                $runtimeResult.svgImageLoaded -ne $true -or
                [string]::IsNullOrWhiteSpace($runtimeResult.lightBackgroundCanvasColor) -or
                [string]::IsNullOrWhiteSpace($runtimeResult.darkBackgroundCanvasColor) -or
                $runtimeResult.lightBackgroundCanvasColor -eq $runtimeResult.darkBackgroundCanvasColor -or
                $null -eq $progressBar -or
                @($expectedProgressTemplateParts | Where-Object { $_ -cnotin @($progressBar.templateParts) }).Count -ne 0 -or
                @($expectedLabelStates | Where-Object { $_ -cnotin @($progressBar.labelStates) }).Count -ne 0 -or
                [Math]::Abs([double]$progressBar.fillRatio - 0.65) -gt 0.02 -or
                $progressBar.automationName -ne 'Package download progress' -or
                $progressBar.fallbackAutomationName -ne 'Default package progress' -or
                $progressBar.automationClassName -ne 'EtherProgressBar' -or
                $progressBar.automationControlType -ne 'ProgressBar' -or
                $progressBar.rangeValueReadOnly -ne $true -or
                $progressBar.smallChangeIsNaN -ne $true -or
                $progressBar.largeChangeIsNaN -ne $true -or
                [double]$progressBar.minimum -ne 0 -or
                [double]$progressBar.maximum -ne 100 -or
                [double]$progressBar.value -ne 65 -or
                $progressBar.setValueRejected -ne $true -or
                $progressBar.valueChangeExercised -ne $true -or
                $progressBar.valuePropertyChangedSubscribed -ne $true -or
                (@($progressBar.lightFillColors) -join ',') -cne ($expectedProgressFillLight -join ',') -or
                (@($progressBar.darkFillColors) -join ',') -cne ($expectedProgressFillDark -join ',') -or
                (@($progressBar.lightTemplateBrushColors) -join ',') -ceq (@($progressBar.darkTemplateBrushColors) -join ',') -or
                $null -eq $button -or
                $button.defaultStyleResolved -ne $true -or
                [double]$button.defaultMinWidth -ne 108 -or
                [double]$button.defaultMinHeight -ne 46 -or
                [double]$button.defaultFontSize -ne 14 -or
                @($expectedButtonTemplateParts | Where-Object { $_ -cnotin @($button.templateParts) }).Count -ne 0 -or
                @($expectedRightIconStates | Where-Object { $_ -cnotin @($button.rightIconStates) }).Count -ne 0 -or
                $button.rightIconCollapsed -ne $true -or
                $button.automationName -ne 'Package button' -or
                (@($button.lightTemplateBrushColors) -join ',') -ceq (@($button.darkTemplateBrushColors) -join ',') -or
                $null -eq $checkbox -or
                $checkbox.defaultStyleResolved -ne $true -or
                $checkbox.defaultIsThreeState -ne $false -or
                @($expectedCheckboxTemplateParts | Where-Object { $_ -cnotin @($checkbox.templateParts) }).Count -ne 0 -or
                @($expectedCheckStates | Where-Object { $_ -cnotin @($checkbox.checkStates) }).Count -ne 0 -or
                $checkbox.automationName -ne 'Package checkbox' -or
                (@($checkbox.lightTemplateBrushColors) -join ',') -ceq (@($checkbox.darkTemplateBrushColors) -join ',') -or
                $null -eq $radioButton -or
                $radioButton.defaultStyleResolved -ne $true -or
                $radioButton.defaultIsThreeState -ne $false -or
                @($expectedRadioButtonTemplateParts | Where-Object { $_ -cnotin @($radioButton.templateParts) }).Count -ne 0 -or
                @($expectedCheckStates | Where-Object { $_ -cnotin @($radioButton.checkStates) }).Count -ne 0 -or
                $radioButton.automationName -ne 'Package radio button' -or
                (@($radioButton.lightTemplateBrushColors) -join ',') -ceq (@($radioButton.darkTemplateBrushColors) -join ',') -or
                $null -eq $etherInput -or
                $etherInput.defaultStyleResolved -ne $true -or
                [double]$etherInput.defaultMinWidth -ne 130 -or
                [double]$etherInput.defaultFontSize -ne 14 -or
                $etherInput.defaultUseSystemFocusVisuals -ne $false -or
                @($expectedInputTemplateParts | Where-Object { $_ -cnotin @($etherInput.templateParts) }).Count -ne 0 -or
                @($expectedInputCommonStates | Where-Object { $_ -cnotin @($etherInput.commonStates) }).Count -ne 0 -or
                $etherInput.disabledOpacityApplied -ne $true -or
                $etherInput.automationName -ne 'Package input' -or
                (@($etherInput.lightTemplateBrushColors) -join ',') -ceq (@($etherInput.darkTemplateBrushColors) -join ',') -or
                $null -eq $dropdown -or
                $dropdown.defaultStyleResolved -ne $true -or
                [double]$dropdown.defaultMinWidth -ne 130 -or
                [int]$dropdown.defaultMaxVisibleItems -ne 6 -or
                $dropdown.defaultUseSystemFocusVisuals -ne $false -or
                @($expectedDropdownTemplateParts | Where-Object { $_ -cnotin @($dropdown.templateParts) }).Count -ne 0 -or
                @($expectedDropDownStates | Where-Object { $_ -cnotin @($dropdown.dropDownStates) }).Count -ne 0 -or
                $dropdown.triggerTextShowsSelection -ne $true -or
                $dropdown.contentPresenterCollapsed -ne $true -or
                $dropdown.openedActiveStrokeVisible -ne $true -or
                $dropdown.closedActiveStrokeCollapsed -ne $true -or
                $dropdown.automationName -ne 'Package dropdown' -or
                (@($dropdown.lightTemplateBrushColors) -join ',') -ceq (@($dropdown.darkTemplateBrushColors) -join ',') -or
                $null -eq $segmentedControl -or
                $segmentedControl.defaultStyleResolved -ne $true -or
                [double]$segmentedControl.defaultPadding -ne 4 -or
                @($expectedSegmentedControlTemplateParts | Where-Object { $_ -cnotin @($segmentedControl.templateParts) }).Count -ne 0 -or
                [int]$segmentedControl.segmentCount -lt 2 -or
                @($expectedSegmentCheckStates | Where-Object { $_ -cnotin @($segmentedControl.checkStates) }).Count -ne 0 -or
                $segmentedControl.automationName -ne 'Package segmented control' -or
                (@($segmentedControl.lightTemplateBrushColors) -join ',') -ceq (@($segmentedControl.darkTemplateBrushColors) -join ',') -or
                $null -eq $intelligenceButton -or
                $intelligenceButton.defaultStyleResolved -ne $true -or
                [double]$intelligenceButton.defaultFontSize -ne 14 -or
                $intelligenceButton.defaultUseSystemFocusVisuals -ne $false -or
                @($expectedIntelligenceButtonTemplateParts | Where-Object { $_ -cnotin @($intelligenceButton.templateParts) }).Count -ne 0 -or
                @($expectedIntelligenceButtonCommonStates | Where-Object { $_ -cnotin @($intelligenceButton.commonStates) }).Count -ne 0 -or
                $intelligenceButton.disabledOpacityApplied -ne $true -or
                $intelligenceButton.automationName -ne 'Package intelligence button' -or
                (@($intelligenceButton.lightTemplateBrushColors) -join ',') -ceq (@($intelligenceButton.darkTemplateBrushColors) -join ',') -or
                $null -eq $steeringBar -or
                $steeringBar.defaultStyleResolved -ne $true -or
                $steeringBar.defaultUseSystemFocusVisuals -ne $false -or
                @($expectedSteeringBarTemplateParts | Where-Object { $_ -cnotin @($steeringBar.templateParts) }).Count -ne 0 -or
                @($expectedSteeringBarLabelStates | Where-Object { $_ -cnotin @($steeringBar.labelStates) }).Count -ne 0 -or
                [Math]::Abs([double]$steeringBar.fillRatio - 0.65) -gt 0.02 -or
                $steeringBar.automationName -ne 'Package steering bar' -or
                $steeringBar.fallbackAutomationName -ne 'Default package steering bar' -or
                $steeringBar.automationClassName -ne 'EtherSteeringBar' -or
                $steeringBar.automationControlType -ne 'Slider' -or
                $steeringBar.rangeValueReadOnly -ne $false -or
                [double]$steeringBar.smallChange -ne 1 -or
                [double]$steeringBar.largeChange -ne 10 -or
                [double]$steeringBar.minimum -ne 0 -or
                [double]$steeringBar.maximum -ne 100 -or
                [double]$steeringBar.value -ne 65 -or
                $steeringBar.setValueAccepted -ne $true -or
                $steeringBar.disabledLocksAutomation -ne $true -or
                $steeringBar.valueChangeExercised -ne $true -or
                (@($steeringBar.lightGradientColors) -join ',') -cne ($expectedSteeringBarGradient -join ',') -or
                (@($steeringBar.darkGradientColors) -join ',') -cne ($expectedSteeringBarGradient -join ',') -or
                (@($steeringBar.lightTemplateBrushColors) -join ',') -ceq (@($steeringBar.darkTemplateBrushColors) -join ',') -or
                $null -eq $slider -or
                $slider.defaultStyleResolved -ne $true -or
                $slider.defaultUseSystemFocusVisuals -ne $false -or
                @($expectedSliderTemplateParts | Where-Object { $_ -cnotin @($slider.templateParts) }).Count -ne 0 -or
                [Math]::Abs([double]$slider.fillRatio - 0.65) -gt 0.06 -or
                [int]$slider.highlightedBarCount -lt 1 -or
                $slider.automationName -ne 'Package slider' -or
                $slider.defaultAutomationName -ne 'Default package slider' -or
                $slider.automationClassName -ne 'EtherSlider' -or
                $slider.automationControlType -ne 'Slider' -or
                $slider.rangeValueReadOnly -ne $false -or
                [double]$slider.smallChange -ne 1 -or
                [double]$slider.largeChange -ne 10 -or
                [double]$slider.minimum -ne 0 -or
                [double]$slider.maximum -ne 100 -or
                [double]$slider.value -ne 65 -or
                $slider.setValueAccepted -ne $true -or
                $slider.disabledLocksAutomation -ne $true -or
                $slider.valueChangeExercised -ne $true -or
                $slider.formattedValue -ne '65' -or
                (@($slider.lightTemplateBrushColors) -join ',') -ceq (@($slider.darkTemplateBrushColors) -join ',') -or
                $null -eq $masthead -or
                $masthead.defaultStyleResolved -ne $true -or
                $masthead.defaultUseSystemFocusVisuals -ne $false -or
                $masthead.defaultShowSettings -ne $true -or
                $masthead.defaultShowSearch -ne $false -or
                @($expectedMastheadTemplateParts | Where-Object { $_ -cnotin @($masthead.templateParts) }).Count -ne 0 -or
                @($expectedMastheadOptionalIconStates | Where-Object { $_ -cnotin @($masthead.optionalIconStates) }).Count -ne 0 -or
                $masthead.searchVisible -ne $true -or
                $masthead.automationName -ne 'Package masthead' -or
                $masthead.defaultAutomationName -ne 'Default package masthead' -or
                (@($masthead.lightTemplateBrushColors) -join ',') -ceq (@($masthead.darkTemplateBrushColors) -join ',') -or
                $null -eq $toggleSwitch -or
                $toggleSwitch.keyedStyleResolved -ne $true -or
                $toggleSwitch.implicitStyleRejected -ne $true -or
                $toggleSwitch.defaultUseSystemFocusVisuals -ne $false -or
                @($expectedToggleSwitchTemplateParts | Where-Object { $_ -cnotin @($toggleSwitch.templateParts) }).Count -ne 0 -or
                @($expectedToggleSwitchStates | Where-Object { $_ -cnotin @($toggleSwitch.toggleStates) }).Count -ne 0 -or
                $toggleSwitch.disabledTrackOpacityApplied -ne $true -or
                $toggleSwitch.automationName -ne 'Package toggle switch' -or
                $toggleSwitch.defaultAutomationName -ne 'Default package toggle switch' -or
                (@($toggleSwitch.lightTemplateBrushColors) -join ',') -ceq (@($toggleSwitch.darkTemplateBrushColors) -join ',') -or
                $null -eq $scrollBar -or
                $scrollBar.implicitStyleApplied -ne $true -or
                $scrollBar.verticalRootPresent -ne $true -or
                $scrollBar.horizontalRootPresent -ne $true -or
                [double]$scrollBar.thickness -ne 6 -or
                [double]$scrollBar.thumbMinLength -ne 60 -or
                $scrollBar.arrowsCollapsed -ne $true -or
                $scrollBar.automationName -ne 'Package scroll bar' -or
                (@($scrollBar.lightTemplateBrushColors) -join ',') -ceq (@($scrollBar.darkTemplateBrushColors) -join ',') -or
                $null -eq $runtimeResult.propertyConsumption -or
                [int]$runtimeResult.propertyConsumption.componentOwnedPropertyCount -ne $expectedConsumedProperties.Count -or
                [int]$runtimeResult.propertyConsumption.propertyChangedCallbackCount -ne $expectedConsumedProperties.Count -or
                [int]$runtimeResult.propertyConsumption.backendPropertyEventCount -ne $expectedConsumedProperties.Count -or
                [int]$runtimeResult.propertyConsumption.standardInteractionEventCount -ne 12 -or
                @($expectedConsumedProperties | Where-Object { $_ -cnotin @($runtimeResult.propertyConsumption.verifiedProperties) }).Count -ne 0 -or
                $null -eq $runtimeResult.publicPropertyInventory -or
                [int]$runtimeResult.publicPropertyInventory.writablePropertyCount -ne $expectedWritablePublicProperties -or
                @($runtimeResult.publicPropertyInventory.propertyKeys).Count -ne $expectedWritablePublicProperties -or
                $null -eq $runtimeResult.publicPropertyCode -or
                [int]$runtimeResult.publicPropertyCode.getterReadCount -ne $expectedWritablePublicProperties -or
                [int]$runtimeResult.publicPropertyCode.setterInvocationCount -ne $expectedWritablePublicProperties -or
                $null -eq $runtimeResult.publicPropertyClassification -or
                [int]$runtimeResult.publicPropertyClassification.visualPropertyCount -ne $expectedVisualPublicProperties -or
                [int]$runtimeResult.publicPropertyClassification.semanticPropertyCount -ne $expectedSemanticPublicProperties -or
                [int]$runtimeResult.publicPropertyClassification.platformPropertyCount -ne $expectedPlatformPublicProperties -or
                $null -eq $runtimeResult.attachedVisualProperties -or
                [int]$runtimeResult.attachedVisualProperties.visualPropertyCount -ne $expectedVisualPublicProperties -or
                [int]$runtimeResult.attachedVisualProperties.attachedMutationCount -ne $expectedVisualPublicProperties -or
                [int]$runtimeResult.attachedVisualProperties.renderedControlCount -ne $expectedVisualPublicProperties -or
                [int]$runtimeResult.attachedVisualProperties.stateObservedPropertyCount -ne $expectedVisualPublicProperties -or
                @($runtimeResult.attachedVisualProperties.verifiedProperties).Count -ne $expectedVisualPublicProperties -or
                @($runtimeResult.attachedVisualProperties.evidence).Count -ne $expectedVisualPublicProperties -or
                @($runtimeResult.attachedVisualProperties.evidence | Where-Object { [string]::IsNullOrWhiteSpace($_.property) -or [string]::IsNullOrWhiteSpace($_.method) -or [string]::IsNullOrWhiteSpace($_.observation) -or $_.method -cnotin $allowedVisualEvidenceMethods }).Count -ne 0 -or
                @($runtimeResult.publicPropertyClassification.visualProperties | Where-Object { $_ -cnotin @($runtimeResult.attachedVisualProperties.verifiedProperties) }).Count -ne 0 -or
                @($runtimeResult.publicPropertyClassification.visualProperties | Where-Object { $_ -cnotin @($runtimeResult.attachedVisualProperties.evidence | ForEach-Object { $_.property }) }).Count -ne 0 -or
                @($runtimeResult.attachedVisualProperties.evidence | ForEach-Object { $_.property } | Select-Object -Unique).Count -ne $expectedVisualPublicProperties) {
                throw "The unpackaged runtime smoke fixture did not verify Foundation resources, assets, and theme re-resolution: $(Get-Content -LiteralPath $markerPath -Raw)"
            }
            Assert-RtlMarker $runtimeResult
            Assert-UiaMarker $runtimeResult
            Assert-TextScaleMarker $runtimeResult
            Assert-LocalizationMarker $runtimeResult
            Assert-ScreenshotMarker $runtimeResult
            if ($UpdateVisualBaselines) {
                Update-VisualBaselines $runtimeResult
            }
            Assert-HighContrastMarker $runtimeResult
            Assert-PerformanceMarker $runtimeResult
            # $workRoot is intentionally cleared on the next run. Publish the accepted marker
            # and exact Light/Dark/High Contrast bitmaps before leaving this scope so a visual
            # audit can inspect the same artifacts the gate just verified.
            $evidenceDirectory = Join-Path $artifactsRoot (Join-Path 'audit-runs' ("consumer-runtime-evidence-{0}" -f (Get-Date -Format 'yyyyMMdd-HHmmssfff')))
            New-Item -ItemType Directory -Path $evidenceDirectory -Force | Out-Null
            Copy-Item -LiteralPath $markerPath -Destination (Join-Path $evidenceDirectory 'runtime-result.json') -Force
            Copy-Item -LiteralPath (Join-Path $workRoot 'screenshots') -Destination (Join-Path $evidenceDirectory 'screenshots') -Recurse -Force
            $storageFileResolvedCount = @($reportedAssets | Where-Object { $_.storageFileResolved -eq $true }).Count
            Write-Host "Unpackaged consumer runtime smoke passed: all $expectedWritablePublicProperties public writable properties were read and invoked on detached component instances; all $expectedVisualPublicProperties visual properties were mutated, laid out, rendered, and emitted a per-property observation on attached WinUI controls; all $($expectedConsumedProperties.Count) declared Ether dependency properties wrote/read/raised callbacks and emitted JSON backend envelopes; 12 standard interaction adapters emitted business envelopes; resources $($reportedResourceKeys -join ', '); templates, states, Light/Dark, RTL, UIA, 2.25 scale, localization, screenshots, SVG loading, and OS-selected High Contrast verified; marker: $markerPath; retained audit evidence: $evidenceDirectory"
        }
        finally {
            if ($null -ne $smokeProcess -and -not $smokeProcess.HasExited) {
                Stop-Process -Id $smokeProcess.Id -Force
                $null = $smokeProcess.WaitForExit(10000)
            }
            Restore-OsHighContrastSnapshot $osHighContrastSnapshot
            [Environment]::SetEnvironmentVariable('ETHER_CONSUMER_SMOKE', $previousSmokeValue, 'Process')
            [Environment]::SetEnvironmentVariable('ETHER_CONSUMER_SMOKE_RESULT_PATH', $previousMarkerPath, 'Process')
            [Environment]::SetEnvironmentVariable('ETHER_CONSUMER_SMOKE_SCREENSHOT_DIR', $previousScreenshotDir, 'Process')
            [Environment]::SetEnvironmentVariable('ETHER_CONSUMER_UPDATE_VISUAL_BASELINES', $previousUpdateVisualBaselines, 'Process')
        }
    }

    & git diff --check
    if ($LASTEXITCODE -ne 0) {
        throw 'git diff --check failed.'
    }

    $expectedTokenHashes = @{
        'src/Ether.DesignSystem.Foundation/Resources/Tokens/EtherPrimitives.xaml' = 'd6ff0e5301b3672dbb492484c8d0ea584aca12be'
        'src/Ether.DesignSystem.Foundation/Resources/Tokens/EtherColors.xaml' = 'd7c218e5a631e86552ed2b089cbc03bbd571a090'
        'src/Ether.DesignSystem.Foundation/Resources/Tokens/EtherSpacing.xaml' = '5d631cd2ebde306d389a1fa8999000359441bcd2'
        'src/Ether.DesignSystem.Foundation/Resources/Tokens/EtherTypography.xaml' = 'b24444567ee95467408e004fe7fab622eba79759'
        'src/Ether.DesignSystem.Foundation/Resources/Tokens/EtherIconGeometries.xaml' = '134c1667934376ba4943cf350b110a02607c81f4'
    }
    foreach ($tokenPath in $expectedTokenHashes.Keys) {
        $actualHash = (& git hash-object $tokenPath).Trim()
        if ($actualHash -ne $expectedTokenHashes[$tokenPath]) {
            throw "Frozen token hash changed for $tokenPath. Expected $($expectedTokenHashes[$tokenPath]), got $actualHash."
        }
    }

    Write-Host "Consumer fixtures passed. Packages: $localFeed"
    Write-Host "Extracted package evidence: $extractRoot"
}
finally {
    Pop-Location
}
