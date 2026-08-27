[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$controlXaml = Join-Path $repoRoot 'src\Views\ControlExample.xaml'
$controlCode = Join-Path $repoRoot 'src\Views\ControlExample.xaml.cs'
$componentPage = Join-Path $repoRoot 'src\Views\ComponentPage.xaml'
$componentPageCode = Join-Path $repoRoot 'src\Views\ComponentPage.xaml.cs'
$buttonPage = Join-Path $repoRoot 'src\Views\Controls\ButtonPage.xaml'
$buttonPageCode = Join-Path $repoRoot 'src\Views\Controls\ButtonPage.xaml.cs'
$progressPage = Join-Path $repoRoot 'src\Views\DataDisplay\ProgressBarPage.xaml'
$progressPageCode = Join-Path $repoRoot 'src\Views\DataDisplay\ProgressBarPage.xaml.cs'
$checkboxPage = Join-Path $repoRoot 'src\Views\Controls\CheckboxPage.xaml'
$architectureDoc = Join-Path $repoRoot 'docs\architecture\2026-08-26-l4-gallery-control-example.md'
$ciPath = Join-Path $repoRoot '.github\workflows\build.yml'

function Assert-Contains {
    param([string]$Text, [string]$Pattern, [string]$Description)

    if ($Text -notmatch $Pattern) {
        throw "$Description is missing required pattern '$Pattern'."
    }
}

function Assert-NotContains {
    param([string]$Text, [string]$Pattern, [string]$Description)

    if ($Text -match $Pattern) {
        throw "$Description must not contain pattern '$Pattern'."
    }
}

foreach ($path in @(
        $controlXaml, $controlCode, $componentPage, $componentPageCode,
        $buttonPage, $buttonPageCode, $progressPage, $progressPageCode,
        $checkboxPage, $architectureDoc, $ciPath)) {
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        throw "Required L4 Gallery ControlExample file is missing: $path"
    }
}

$code = Get-Content -LiteralPath $controlCode -Raw
Assert-Contains $code 'class ControlExample' 'ControlExample type'
Assert-Contains $code 'ContentProperty\(Name = nameof\(Example\)\)' 'ControlExample ContentProperty(Example)'
Assert-Contains $code 'ExampleProperty' 'ControlExample Example DP'
Assert-Contains $code 'OutputTextProperty' 'ControlExample OutputText DP'
Assert-Contains $code 'SourceXamlProperty' 'ControlExample SourceXaml DP'
Assert-Contains $code 'Clipboard\.SetContent' 'ControlExample clipboard copy'
Assert-Contains $code 'NarrowLayoutWidth = 720' 'ControlExample narrow breakpoint'
Assert-Contains $code 'WideLayout' 'ControlExample WideLayout state'
Assert-Contains $code 'NarrowLayout' 'ControlExample NarrowLayout state'

$xaml = Get-Content -LiteralPath $controlXaml -Raw
Assert-Contains $xaml 'x:Name="CopyButton"' 'ControlExample Copy button'
Assert-Contains $xaml 'AutomationProperties.Name="Copy source"' 'ControlExample Copy automation name'
Assert-Contains $xaml 'AutomationProperties.Name="Control example source"' 'ControlExample source automation name'
Assert-Contains $xaml 'AutomationProperties.Name="Control example output"' 'ControlExample output automation name'
Assert-Contains $xaml 'x:Name="LayoutStates"' 'ControlExample LayoutStates group'

$chrome = Get-Content -LiteralPath $componentPage -Raw
Assert-Contains $chrome 'InteractiveControls' 'ComponentPage Options slot (InteractiveControls)'
Assert-Contains $chrome 'INTERACTIVE' 'ComponentPage INTERACTIVE header'

$chromeCode = Get-Content -LiteralPath $componentPageCode -Raw
Assert-Contains $chromeCode 'InteractiveContent is ControlExample' 'ComponentPage ControlExample disable isolation'
Assert-Contains $chromeCode 'example.IsExampleEnabled' 'ComponentPage disables ControlExample specimen only'

$buttonXaml = Get-Content -LiteralPath $buttonPage -Raw
Assert-Contains $buttonXaml '<views:ControlExample' 'ButtonPage ControlExample pilot'
Assert-Contains $buttonXaml 'SourceXaml="\{x:Bind SpecimenXaml' 'ButtonPage SourceXaml binding'
Assert-Contains $buttonXaml 'HasDisabledToggle="True"' 'ButtonPage Disabled options still on ComponentPage'

$buttonCode = Get-Content -LiteralPath $buttonPageCode -Raw
Assert-Contains $buttonCode 'SpecimenXaml' 'ButtonPage specimen XAML snippet'
Assert-Contains $buttonCode 'EtherButton' 'ButtonPage EtherButton snippet'
Assert-Contains $buttonCode 'LiveExample.OutputText' 'ButtonPage live output'

$progressXaml = Get-Content -LiteralPath $progressPage -Raw
Assert-Contains $progressXaml '<views:ControlExample' 'ProgressBarPage ControlExample pilot'
Assert-Contains $progressXaml 'SourceXaml="\{x:Bind SpecimenXaml' 'ProgressBarPage SourceXaml binding'

$progressCode = Get-Content -LiteralPath $progressPageCode -Raw
Assert-Contains $progressCode 'SpecimenXaml' 'ProgressBarPage specimen XAML snippet'
Assert-Contains $progressCode 'EtherProgressBar' 'ProgressBarPage EtherProgressBar snippet'
Assert-Contains $progressCode 'LiveExample.OutputText' 'ProgressBarPage live output'

$checkboxXaml = Get-Content -LiteralPath $checkboxPage -Raw
Assert-Contains $checkboxXaml '<views:ComponentPage' 'CheckboxPage remains on ComponentPage'
Assert-NotContains $checkboxXaml '<views:ControlExample' 'CheckboxPage must stay on the pre-ControlExample ComponentPage API'

$doc = Get-Content -LiteralPath $architectureDoc -Raw
Assert-Contains $doc 'ControlExample' 'L4 architecture note names ControlExample'
Assert-Contains $doc 'Deferred' 'L4 architecture note documents deferrals'
Assert-Contains $doc 'Appium' 'L4 architecture note names Appium deferral'
Assert-Contains $doc 'arm64' 'L4 architecture note names arm64 deferral'

$ci = Get-Content -LiteralPath $ciPath -Raw
Assert-Contains $ci 'Verify-GalleryControlExample.ps1' 'CI wires Verify-GalleryControlExample.ps1'

Write-Host 'Gallery ControlExample contract passed.'
