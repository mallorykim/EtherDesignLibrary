[CmdletBinding()]
param(
    [switch]$SkipRuntimeSmoke,
    [switch]$SkipSolutionBuild
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$artifactsRoot = [System.IO.Path]::GetFullPath((Join-Path $repoRoot 'artifacts'))
$workRoot = [System.IO.Path]::GetFullPath((Join-Path $artifactsRoot 'consumer-fixtures'))
$localFeed = Join-Path $workRoot 'local-feed'
$packageCache = Join-Path $workRoot 'packages'
$extractRoot = Join-Path $workRoot 'extracted'
$configuration = 'Debug'
$platform = 'x64'
$tfm = 'net8.0-windows10.0.19041.0'
$packageTfm = 'net8.0-windows10.0.19041'
$packageVersion = '0.1.0-preview.1'
$platformProperty = '-p:Platform=' + $platform

if (-not $workRoot.StartsWith($artifactsRoot + [System.IO.Path]::DirectorySeparatorChar, [System.StringComparison]::OrdinalIgnoreCase)) {
    throw "Refusing to clear an output path outside artifacts: $workRoot"
}

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

function Assert-NoProjectReference {
    param([Parameter(Mandatory)][string]$ProjectPath)

    if (Select-String -LiteralPath $ProjectPath -Pattern '<ProjectReference(?:\s|>)' -Quiet) {
        throw "Consumer fixture '$ProjectPath' must restore only NuGet packages, not a ProjectReference."
    }
}

function Assert-ControlsOnlyPackageReference {
    param([Parameter(Mandatory)][string]$ProjectPath)

    [xml]$project = Get-Content -LiteralPath $ProjectPath -Raw
    $etherReferences = @($project.SelectNodes('//PackageReference') | Where-Object { $_.Include -like 'Ether.DesignSystem.*' })
    if ($etherReferences.Count -ne 1 -or
        $etherReferences[0].Include -ne 'Ether.DesignSystem.Controls' -or
        $etherReferences[0].VersionOverride -ne $packageVersion) {
        throw "Consumer fixture '$ProjectPath' must reference only Ether.DesignSystem.Controls $packageVersion."
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

Push-Location $repoRoot
try {
    if (-not $SkipSolutionBuild) {
        Invoke-DotNet @('build', 'Ether.DesignSystem.slnx', '-c', $configuration, $platformProperty)
        Invoke-DotNet @('build', 'Ether.DesignSystem.slnx', '-c', 'Release', $platformProperty)
    }

    $foundationProject = 'src/Ether.DesignSystem.Foundation/Ether.DesignSystem.Foundation.csproj'
    $controlsProject = 'src/Ether.DesignSystem.Controls/Ether.DesignSystem.Controls.csproj'
    Invoke-DotNet @('pack', $foundationProject, '-c', $configuration, $platformProperty, '-o', $localFeed)
    Invoke-DotNet @('pack', $controlsProject, '-c', $configuration, $platformProperty, '-o', $localFeed)

    $foundationPackage = Join-Path $localFeed "Ether.DesignSystem.Foundation.$packageVersion.nupkg"
    $controlsPackage = Join-Path $localFeed "Ether.DesignSystem.Controls.$packageVersion.nupkg"
    $foundationEntries = Get-PackageEntries $foundationPackage
    $controlsEntries = Get-PackageEntries $controlsPackage

    $foundationLib = "lib/$packageTfm/Ether.DesignSystem.Foundation.dll"
    $foundationPri = "lib/$packageTfm/Ether.DesignSystem.Foundation.pri"
    Assert-PackageEntry $foundationEntries $foundationLib 'Ether.DesignSystem.Foundation'
    Assert-PackageEntry $foundationEntries $foundationPri 'Ether.DesignSystem.Foundation'
    Assert-PackageEntry $foundationEntries "lib/$packageTfm/Ether.DesignSystem.Foundation/Themes/Foundation.xaml" 'Ether.DesignSystem.Foundation'
    Assert-PackageEntry $foundationEntries "lib/$packageTfm/Ether.DesignSystem.Foundation/Themes/Foundation.xbf" 'Ether.DesignSystem.Foundation'
    Assert-PackageEntry $foundationEntries "lib/$packageTfm/Resources/Tokens/EtherTypography.xaml" 'Ether.DesignSystem.Foundation'
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
        Assert-ControlsOnlyPackageReference $fixtureProject
        Invoke-DotNet @('restore', $fixtureProject, '--configfile', $fixtureNuGetConfig, '--packages', $packageCache)
        Assert-FoundationFlowsTransitively (Join-Path (Split-Path -Parent $fixtureProject) 'obj/project.assets.json')
        Invoke-DotNet @('build', $fixtureProject, '-c', $configuration, $platformProperty, '--no-restore')
    }

    $unpackagedOutput = Join-Path $repoRoot "tests/Ether.DesignSystem.ConsumerFixtures/Unpackaged/bin/$platform/$configuration/$tfm"
    Assert-OutputFile (Join-Path $unpackagedOutput 'Fonts/Instrument_Sans/InstrumentSans-VariableFont_wdth,wght.ttf')
    Assert-OutputFile (Join-Path $unpackagedOutput 'Fonts/Inter/Inter-VariableFont_opsz,wght.ttf')
    Assert-OutputFile (Join-Path $unpackagedOutput 'Assets/Icons/dds2/dds2_add-cir.svg')
    Assert-OutputFile (Join-Path $unpackagedOutput 'Ether.DesignSystem.Foundation.dll')
    Assert-OutputFile (Join-Path $unpackagedOutput 'Ether.DesignSystem.Controls.dll')

    $packagedOutput = Join-Path $repoRoot "tests/Ether.DesignSystem.ConsumerFixtures/Packaged/bin/$platform/$configuration/$tfm"
    Assert-OutputFile (Join-Path $packagedOutput 'Ether.DesignSystem.Foundation.dll')
    Assert-OutputFile (Join-Path $packagedOutput 'Ether.DesignSystem.Controls.dll')
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
        $smokeProcess = $null
        try {
            [Environment]::SetEnvironmentVariable('ETHER_CONSUMER_SMOKE', '1', 'Process')
            [Environment]::SetEnvironmentVariable('ETHER_CONSUMER_SMOKE_RESULT_PATH', $markerPath, 'Process')
            $smokeProcess = Start-Process -FilePath $unpackagedExe -WorkingDirectory $unpackagedOutput -PassThru -WindowStyle Hidden
            if (-not $smokeProcess.WaitForExit(15000)) {
                Stop-Process -Id $smokeProcess.Id -Force
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
            $expectedProgressTemplateParts = @('FillColumn', 'RestColumn', 'LabelRow', 'TitleText', 'ValueLabel')
            $expectedLabelStates = @('BothLabelsVisible', 'TitleOnly', 'ValueOnly', 'LabelsHidden')
            $expectedProgressGradient = @('#FF0021F3', '#FF0015FF', '#FF0EB2FF', '#FF40E1FD')
            $expectedButtonTemplateParts = @('RightIcon')
            $expectedRightIconStates = @('RightIconVisible', 'RightIconCollapsed')
            $expectedCheckboxTemplateParts = @('UncheckedFill', 'UncheckedFace', 'CheckedFace', 'Glyph', 'Label')
            $expectedRadioButtonTemplateParts = @('UncheckedFill', 'UncheckedFace', 'CheckedFace', 'Dot', 'Label')
            $expectedCheckStates = @('Unchecked', 'Checked')
            $expectedInputTemplateParts = @('LayoutRoot', 'BorderElement', 'PlaceholderTextContentPresenter', 'ContentElement')
            $expectedInputCommonStates = @('Normal', 'PointerOver', 'Focused', 'Disabled')
            $expectedDropdownTemplateParts = @('TriggerText', 'Arrow', 'Popup')
            $expectedDropDownStates = @('Opened', 'Closed')
            $expectedSegmentedControlTemplateParts = @('ShadowHost', 'TrackSurface')
            $expectedSegmentCheckStates = @('Unchecked', 'Checked')
            $expectedIntelligenceButtonTemplateParts = @('Bg', 'BorderIntelligenceBlue', 'BorderIntelligenceGradient', 'BlueGlowOuter', 'BlueGlowMiddle', 'BlueGlowCore', 'PurpleGlow', 'Cp', 'FocusRing')
            $expectedIntelligenceButtonCommonStates = @('Normal', 'PointerOver', 'Pressed', 'Disabled')
            $expectedSteeringBarTemplateParts = @('InteractionSurface', 'FillBorder', 'ThumbHost', 'LabelRow', 'TitleText', 'ValueLabel')
            $expectedSteeringBarLabelStates = @('BothLabelsVisible', 'TitleOnly', 'ValueOnly', 'LabelsHidden')
            $expectedSteeringBarGradient = @('#FF0021F3', '#FF0015FF', '#FF0EB2FF', '#FF40E1FD')
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
                (@($progressBar.lightGradientColors) -join ',') -cne ($expectedProgressGradient -join ',') -or
                (@($progressBar.darkGradientColors) -join ',') -cne ($expectedProgressGradient -join ',') -or
                (@($progressBar.lightTemplateBrushColors) -join ',') -ceq (@($progressBar.darkTemplateBrushColors) -join ',') -or
                $null -eq $button -or
                $button.defaultStyleResolved -ne $true -or
                [double]$button.defaultMinHeight -ne 40 -or
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
                [double]$etherInput.defaultFontSize -ne 14 -or
                $etherInput.defaultUseSystemFocusVisuals -ne $false -or
                @($expectedInputTemplateParts | Where-Object { $_ -cnotin @($etherInput.templateParts) }).Count -ne 0 -or
                @($expectedInputCommonStates | Where-Object { $_ -cnotin @($etherInput.commonStates) }).Count -ne 0 -or
                $etherInput.disabledOpacityApplied -ne $true -or
                $etherInput.automationName -ne 'Package input' -or
                (@($etherInput.lightTemplateBrushColors) -join ',') -ceq (@($etherInput.darkTemplateBrushColors) -join ',') -or
                $null -eq $dropdown -or
                $dropdown.defaultStyleResolved -ne $true -or
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
                $steeringBar.previewStatusLocksAutomation -ne $true -or
                $steeringBar.valueChangeExercised -ne $true -or
                (@($steeringBar.lightGradientColors) -join ',') -cne ($expectedSteeringBarGradient -join ',') -or
                (@($steeringBar.darkGradientColors) -join ',') -cne ($expectedSteeringBarGradient -join ',') -or
                (@($steeringBar.lightTemplateBrushColors) -join ',') -ceq (@($steeringBar.darkTemplateBrushColors) -join ',')) {
                throw "The unpackaged runtime smoke fixture did not verify Foundation resources, assets, and theme re-resolution: $(Get-Content -LiteralPath $markerPath -Raw)"
            }
            $storageFileResolvedCount = @($reportedAssets | Where-Object { $_.storageFileResolved -eq $true }).Count
            Write-Host "Unpackaged consumer runtime smoke passed: resources $($reportedResourceKeys -join ', '); EtherProgressBar LabelStates and read-only RangeValue verified; EtherButton default style, RightIconStates, and Light/Dark brushes verified; EtherCheckbox and EtherRadioButton default style, CheckStates, and Light/Dark brushes verified; EtherInput default style, CommonStates, and Light/Dark brushes verified; EtherDropdown default style, DropDownStates, TriggerText, and Light/Dark brushes verified; EtherSegmentedControl default style, ShadowHost/TrackSurface, segment CheckStates, and Light/Dark brushes verified; EtherIntelligenceButton default style, CommonStates, and Light/Dark brushes verified; EtherSteeringBar default style, LabelStates, interactive RangeValue GetPattern, and Light/Dark brushes verified; SVG ImageSource loaded; StorageFile $storageFileResolvedCount/$($reportedAssets.Count) (unpackaged host-root limitation is recorded in marker); BackgroundCanvas $($runtimeResult.lightBackgroundCanvasColor) -> $($runtimeResult.darkBackgroundCanvasColor); marker: $markerPath"
        }
        finally {
            if ($null -ne $smokeProcess -and -not $smokeProcess.HasExited) {
                Stop-Process -Id $smokeProcess.Id -Force
            }
            [Environment]::SetEnvironmentVariable('ETHER_CONSUMER_SMOKE', $previousSmokeValue, 'Process')
            [Environment]::SetEnvironmentVariable('ETHER_CONSUMER_SMOKE_RESULT_PATH', $previousMarkerPath, 'Process')
        }
    }

    & git diff --check
    if ($LASTEXITCODE -ne 0) {
        throw 'git diff --check failed.'
    }

    $expectedTokenHashes = @{
        'src/Resources/Tokens/EtherPrimitives.xaml' = 'd6ff0e5301b3672dbb492484c8d0ea584aca12be'
        'src/Resources/Tokens/EtherColors.xaml' = '794404850d4eb20a2fc3ec43c3480be8f318ac64'
        'src/Resources/Tokens/EtherSpacing.xaml' = '5d631cd2ebde306d389a1fa8999000359441bcd2'
        'src/Resources/Tokens/EtherTypography.xaml' = 'b24444567ee95467408e004fe7fab622eba79759'
        'src/Resources/Tokens/EtherIconGeometries.xaml' = '134c1667934376ba4943cf350b110a02607c81f4'
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
