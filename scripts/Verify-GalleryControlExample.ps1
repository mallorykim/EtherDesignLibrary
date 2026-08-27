[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$controlXaml = Join-Path $repoRoot 'samples\Ether.DesignSystem.Gallery\Views\ControlExample.xaml'
$controlCode = Join-Path $repoRoot 'samples\Ether.DesignSystem.Gallery\Views\ControlExample.xaml.cs'
$componentPage = Join-Path $repoRoot 'samples\Ether.DesignSystem.Gallery\Views\ComponentPage.xaml'
$componentPageCode = Join-Path $repoRoot 'samples\Ether.DesignSystem.Gallery\Views\ComponentPage.xaml.cs'
$architectureDoc = Join-Path $repoRoot 'docs\architecture\2026-08-26-l4-gallery-control-example.md'
$ciPath = Join-Path $repoRoot '.github\workflows\build.yml'

$controlExamplePages = @(
    @{ Rel = 'samples\Ether.DesignSystem.Gallery\Views\Controls\ButtonPage.xaml'; Name = 'ButtonPage'; Snippet = 'EtherButton' },
    @{ Rel = 'samples\Ether.DesignSystem.Gallery\Views\DataDisplay\ProgressBarPage.xaml'; Name = 'ProgressBarPage'; Snippet = 'EtherProgressBar' },
    @{ Rel = 'samples\Ether.DesignSystem.Gallery\Views\Controls\CheckboxPage.xaml'; Name = 'CheckboxPage'; Snippet = 'EtherCheckbox' },
    @{ Rel = 'samples\Ether.DesignSystem.Gallery\Views\Controls\RadioButtonPage.xaml'; Name = 'RadioButtonPage'; Snippet = 'EtherRadioButton' },
    @{ Rel = 'samples\Ether.DesignSystem.Gallery\Views\Controls\InputPage.xaml'; Name = 'InputPage'; Snippet = 'EtherInput' },
    @{ Rel = 'samples\Ether.DesignSystem.Gallery\Views\Controls\DropdownPage.xaml'; Name = 'DropdownPage'; Snippet = 'EtherDropdown' },
    @{ Rel = 'samples\Ether.DesignSystem.Gallery\Views\Controls\SegmentedControlPage.xaml'; Name = 'SegmentedControlPage'; Snippet = 'EtherSegmentedControl' },
    @{ Rel = 'samples\Ether.DesignSystem.Gallery\Views\Controls\IntelligenceButtonPage.xaml'; Name = 'IntelligenceButtonPage'; Snippet = 'EtherIntelligenceButton' },
    @{ Rel = 'samples\Ether.DesignSystem.Gallery\Views\Controls\SteeringBarPage.xaml'; Name = 'SteeringBarPage'; Snippet = 'EtherSteeringBar' },
    @{ Rel = 'samples\Ether.DesignSystem.Gallery\Views\Controls\SliderPage.xaml'; Name = 'SliderPage'; Snippet = 'EtherSlider' },
    @{ Rel = 'samples\Ether.DesignSystem.Gallery\Views\Controls\ToggleSwitchPage.xaml'; Name = 'ToggleSwitchPage'; Snippet = 'EtherSwitch' },
    @{ Rel = 'samples\Ether.DesignSystem.Gallery\Views\Foundations\ScrollBarPage.xaml'; Name = 'ScrollBarPage'; Snippet = 'ScrollViewer' },
    @{ Rel = 'samples\Ether.DesignSystem.Gallery\Views\Navigation\MastheadPage.xaml'; Name = 'MastheadPage'; Snippet = 'EtherMasthead' },
    @{ Rel = 'samples\Ether.DesignSystem.Gallery\Views\Surfaces\CardPage.xaml'; Name = 'CardPage'; Snippet = 'EtherCardNormal' }
)

$foundationPrimitivePages = @(
    @{ Rel = 'samples\Ether.DesignSystem.Gallery\Views\Foundations\ColorsPage.xaml'; Name = 'ColorsPage' },
    @{ Rel = 'samples\Ether.DesignSystem.Gallery\Views\Foundations\TypographyPage.xaml'; Name = 'TypographyPage' },
    @{ Rel = 'samples\Ether.DesignSystem.Gallery\Views\Foundations\SpacingPage.xaml'; Name = 'SpacingPage' },
    @{ Rel = 'samples\Ether.DesignSystem.Gallery\Views\Foundations\RadiusPage.xaml'; Name = 'RadiusPage' },
    @{ Rel = 'samples\Ether.DesignSystem.Gallery\Views\Foundations\IconsPage.xaml'; Name = 'IconsPage' }
)

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
        $architectureDoc, $ciPath)) {
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
Assert-Contains $xaml 'x:Uid="ControlExampleCopy"' 'ControlExample Copy x:Uid'
Assert-Contains $xaml 'x:Uid="ControlExampleExampleLabel"' 'ControlExample EXAMPLE x:Uid'
Assert-Contains $xaml 'x:Uid="ControlExampleOutputLabel"' 'ControlExample OUTPUT x:Uid'
Assert-Contains $xaml 'x:Uid="ControlExampleSourceLabel"' 'ControlExample SOURCE x:Uid'
Assert-Contains $xaml 'AutomationProperties.Name="Copy source"' 'ControlExample Copy automation name'
Assert-Contains $xaml 'AutomationProperties.Name="Control example source"' 'ControlExample source automation name'
Assert-Contains $xaml 'AutomationProperties.Name="Control example output"' 'ControlExample output automation name'
Assert-Contains $xaml 'x:Name="LayoutStates"' 'ControlExample LayoutStates group'
Assert-Contains $xaml 'x:Name="ExamplePanel"' 'ControlExample EXAMPLE panel'

$chrome = Get-Content -LiteralPath $componentPage -Raw
Assert-Contains $chrome 'InteractiveControls' 'ComponentPage Options slot (InteractiveControls)'
Assert-Contains $chrome 'x:Uid="ComponentPageInteractive"' 'ComponentPage INTERACTIVE x:Uid'
Assert-Contains $chrome 'x:Uid="ComponentPageDisabled"' 'ComponentPage Disabled x:Uid'

$galleryResw = Join-Path $repoRoot 'samples\Ether.DesignSystem.Gallery\Strings\en-US\Resources.resw'
if (-not (Test-Path -LiteralPath $galleryResw -PathType Leaf)) {
    throw "Gallery localization file is missing: $galleryResw"
}
$resw = Get-Content -LiteralPath $galleryResw -Raw
Assert-Contains $resw 'ControlExampleExampleLabel\.Text' 'Gallery resw EXAMPLE label'
Assert-Contains $resw 'ControlExampleOutputLabel\.Text' 'Gallery resw OUTPUT label'
Assert-Contains $resw 'ControlExampleSourceLabel\.Text' 'Gallery resw SOURCE label'
Assert-Contains $resw 'ControlExampleCopy\.Content' 'Gallery resw Copy'
Assert-Contains $resw 'ControlExampleCopied\.Content' 'Gallery resw Copied'
Assert-Contains $resw 'ComponentPageInteractive\.Text' 'Gallery resw INTERACTIVE'
Assert-Contains $resw 'ComponentPageDisabled\.Content' 'Gallery resw Disabled'

$chromeCode = Get-Content -LiteralPath $componentPageCode -Raw
Assert-Contains $chromeCode 'InteractiveContent is ControlExample' 'ComponentPage ControlExample disable isolation'
Assert-Contains $chromeCode 'example.IsExampleEnabled' 'ComponentPage disables ControlExample specimen only'

foreach ($page in $controlExamplePages) {
    $pageXamlPath = Join-Path $repoRoot $page.Rel
    $pageCodePath = $pageXamlPath + '.cs'
    if (-not (Test-Path -LiteralPath $pageXamlPath -PathType Leaf)) {
        throw "Required ControlExample page is missing: $pageXamlPath"
    }
    if (-not (Test-Path -LiteralPath $pageCodePath -PathType Leaf)) {
        throw "Required ControlExample page code-behind is missing: $pageCodePath"
    }

    $pageXaml = Get-Content -LiteralPath $pageXamlPath -Raw
    Assert-Contains $pageXaml '<views:ComponentPage' "$($page.Name) remains on ComponentPage"
    Assert-Contains $pageXaml '<views:ControlExample' "$($page.Name) wraps ControlExample"
    Assert-Contains $pageXaml 'SourceXaml="\{x:Bind SpecimenXaml' "$($page.Name) SourceXaml binding"

    $pageCode = Get-Content -LiteralPath $pageCodePath -Raw
    Assert-Contains $pageCode 'SpecimenXaml' "$($page.Name) specimen XAML snippet"
    Assert-Contains $pageCode $page.Snippet "$($page.Name) snippet names $($page.Snippet)"
}

foreach ($page in $foundationPrimitivePages) {
    $pageXamlPath = Join-Path $repoRoot $page.Rel
    if (-not (Test-Path -LiteralPath $pageXamlPath -PathType Leaf)) {
        throw "Required Foundations primitive page is missing: $pageXamlPath"
    }

    $pageXaml = Get-Content -LiteralPath $pageXamlPath -Raw
    Assert-Contains $pageXaml '<views:ComponentPage' "$($page.Name) remains on ComponentPage"
    Assert-NotContains $pageXaml '<views:ControlExample' "$($page.Name) must stay on the plain ComponentPage API"
}

$doc = Get-Content -LiteralPath $architectureDoc -Raw
Assert-Contains $doc 'ControlExample' 'L4 architecture note names ControlExample'
Assert-Contains $doc 'Still red' 'L4 architecture note documents still-red gates'
Assert-Contains $doc 'Appium' 'L4 architecture note names Appium deferral'
Assert-Contains $doc 'arm64' 'L4 architecture note names arm64 deferral'
Assert-Contains $doc 'RightToLeft' 'L4 architecture note records package-consumer RTL'
Assert-Contains $doc 'CheckboxPage' 'L4 architecture note lists CheckboxPage'
Assert-Contains $doc 'CardPage' 'L4 architecture note lists CardPage'

$ci = Get-Content -LiteralPath $ciPath -Raw
Assert-Contains $ci 'Verify-GalleryControlExample.ps1' 'CI wires Verify-GalleryControlExample.ps1'
Assert-Contains $ci 'Verify-GalleryLocalization\.ps1' 'CI wires Verify-GalleryLocalization.ps1'
Assert-Contains $ci 'SkipRuntimeSmoke' 'Hosted CI skips consumer GUI runtime smoke'
Assert-NotContains $ci 'Verify-GallerySmoke' 'Hosted CI does not launch Gallery GUI smoke'
Assert-Contains $doc 'Verify-RuntimeGates' 'L4 architecture note names the local runtime-gate owner'

$runtimeGates = Join-Path $repoRoot 'scripts\Verify-RuntimeGates.ps1'
if (-not (Test-Path -LiteralPath $runtimeGates -PathType Leaf)) {
    throw "Required local runtime-gate script is missing: $runtimeGates"
}

Write-Host 'Gallery ControlExample contract passed.'
