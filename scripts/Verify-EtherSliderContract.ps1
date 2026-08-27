[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$controlPath = Join-Path $repoRoot 'src\Ether.DesignSystem.Controls\Controls\Inputs\EtherSlider.xaml.cs'
$xamlPath = Join-Path $repoRoot 'src\Ether.DesignSystem.Controls\Controls\Inputs\EtherSlider.xaml'
$galleryPath = Join-Path $repoRoot 'samples\Ether.DesignSystem.Gallery\Views\Controls\SliderPage.xaml'
$fixturePath = Join-Path $repoRoot 'tests\Ether.DesignSystem.ConsumerFixtures\RuntimeVerification.cs'
$ciPath = Join-Path $repoRoot '.github\workflows\build.yml'
$xamlNamespace = 'http://schemas.microsoft.com/winfx/2006/xaml'
$expectedComponentKeys = 'EtherSliderHighlightBrush,EtherSliderInactiveBrush,EtherSliderKnobBrush,EtherSliderKnobPressedBrush,EtherSliderValueForegroundBrush'
$templateParts = @('ValueText', 'BarCanvas', 'Knob', 'HighlightBrushSource', 'InactiveBrushSource', 'KnobBrushSource', 'KnobPressedBrushSource')

function Assert-Contains {
    param([string]$Text, [string]$Pattern, [string]$Description)

    if ($Text -notmatch $Pattern) {
        throw "$Description is missing required pattern '$Pattern'."
    }
}

function Get-KeyedResources {
    param([System.Xml.XmlElement]$Dictionary)

    return @($Dictionary.ChildNodes |
        Where-Object { $_ -is [System.Xml.XmlElement] } |
        ForEach-Object {
            $key = $_.GetAttributeNode('Key', $xamlNamespace)
            if ($null -ne $key) { $key.Value }
        } |
        Where-Object { -not [string]::IsNullOrWhiteSpace($_) } |
        Sort-Object)
}

$control = Get-Content -LiteralPath $controlPath -Raw
Assert-Contains $control 'class EtherSlider\s*:\s*RangeBase' 'EtherSlider RangeBase conversion'
Assert-Contains $control 'DefaultStyleKey\s*=\s*typeof\(EtherSlider\)' 'EtherSlider constructor'
if ($control -notmatch '(?s)public EtherSlider\(\)\s*\{(?<ctor>.*?)\n    \}') {
    throw 'EtherSlider constructor body could not be isolated for default-setter audit.'
}
if ($Matches['ctor'] -match '(Minimum|Maximum|Value|SmallChange|LargeChange|UseSystemFocusVisuals|IsTabStop)\s*=') {
    throw 'EtherSlider constructor must not assign its default visual setters; the keyed style owns them.'
}
foreach ($part in $templateParts) {
    Assert-Contains $control "TemplatePart\(Name = .*${part}" "EtherSlider TemplatePart contract for $part"
}
Assert-Contains $control 'AutomationControlType\.Slider' 'EtherSlider automation control type'
Assert-Contains $control 'PatternInterface\.RangeValue' 'EtherSlider RangeValue pattern'
Assert-Contains $control 'RangeValuePatternIdentifiers\.ValueProperty' 'EtherSlider RangeValue value-changed event'
Assert-Contains $control 'public string FormatValue\(double value\)' 'EtherSlider FormatValue public API'
Assert-Contains $control 'BindBarFill\(bar, _highlightBrushSource, "EtherSliderHighlightBrush"\)' 'EtherSlider template bars consume highlight component key'
Assert-Contains $control 'BindBarFill\(bar, _inactiveBrushSource, "EtherSliderInactiveBrush"\)' 'EtherSlider template bars consume inactive component key'
if ($control -match 'new\s+Rectangle\b') {
    throw 'EtherSlider must not construct bar or knob rectangles in code; they are template-declared.'
}
if ($control -match 'Children\.Clear\(') {
    throw 'EtherSlider must not clear BarCanvas children; template-declared bars and knob stay in the tree.'
}
if ($control -match 'GetThemeColor\(') {
    throw 'EtherSlider must not resolve colors through GetThemeColor string lookups.'
}
if ($control -match 'InitializeComponent\(') {
    throw 'EtherSlider must not call InitializeComponent after the UserControl conversion.'
}

[xml]$xaml = Get-Content -LiteralPath $xamlPath -Raw
$styles = @($xaml.SelectNodes("//*[local-name()='Style']"))
$keyedStyle = @($styles | Where-Object { $_.GetAttribute('Key', $xamlNamespace) -eq 'DefaultEtherSliderStyle' })[0]
if ($null -eq $keyedStyle) {
    throw 'EtherSlider is missing keyed DefaultEtherSliderStyle.'
}
$implicitStyle = @($styles | Where-Object {
    -not $_.HasAttribute('Key', $xamlNamespace) -and $_.GetAttribute('BasedOn') -match 'DefaultEtherSliderStyle'
})[0]
if ($null -eq $implicitStyle) {
    throw 'EtherSlider is missing its implicit style BasedOn DefaultEtherSliderStyle.'
}
foreach ($setter in 'IsTabStop', 'UseSystemFocusVisuals', 'HighContrastAdjustment', 'Minimum', 'Maximum', 'Value', 'SmallChange', 'LargeChange', 'Template') {
    if ($null -eq $keyedStyle.SelectSingleNode("./*[local-name()='Setter' and @Property='$setter']")) {
        throw "DefaultEtherSliderStyle is missing its $setter setter."
    }
}

$template = $keyedStyle.SelectSingleNode(".//*[local-name()='ControlTemplate']")
if ($null -eq $template) {
    throw 'DefaultEtherSliderStyle is missing its ControlTemplate.'
}
if ($template.OuterXml -match '#[0-9A-Fa-f]{3,8}') {
    throw 'EtherSlider ControlTemplate contains a literal hex color; only component resources may carry color values.'
}
if ($template.OuterXml -notmatch 'x:Name="Knob"') {
    throw 'EtherSlider ControlTemplate is missing the named Knob part.'
}
$declaredBarCount = @($template.SelectNodes(".//*[local-name()='Rectangle' and @Height='40']")).Count
if ($declaredBarCount -ne 63) {
    throw "EtherSlider ControlTemplate must declare 63 Height=40 bar rectangles. Observed $declaredBarCount."
}
foreach ($themeResource in @([regex]::Matches($template.OuterXml, '\{ThemeResource\s+([^}\s]+)') | ForEach-Object { $_.Groups[1].Value })) {
    if ($themeResource -notlike 'EtherSlider*') {
        throw "EtherSlider template references non-component ThemeResource '$themeResource'."
    }
}

$themeDictionaries = @($xaml.SelectNodes("//*[local-name()='ResourceDictionary.ThemeDictionaries']/*[local-name()='ResourceDictionary']"))
$keysByTheme = @{}
foreach ($dictionary in $themeDictionaries) {
    $themeAttribute = $dictionary.GetAttributeNode('Key', $xamlNamespace)
    if ($null -eq $themeAttribute) {
        throw 'EtherSlider has a theme dictionary without x:Key.'
    }

    $keys = Get-KeyedResources $dictionary
    if ($keys.Count -eq 0 -or @($keys | Where-Object { $_ -notlike 'EtherSlider*' }).Count -gt 0) {
        throw "EtherSlider $($themeAttribute.Value) resources must be component-scoped EtherSlider keys."
    }
    $keysByTheme[$themeAttribute.Value] = $keys
}

foreach ($theme in 'Light', 'Dark', 'HighContrast') {
    if (-not $keysByTheme.ContainsKey($theme)) {
        throw "EtherSlider is missing its $theme component theme dictionary."
    }
    if (($keysByTheme[$theme] -join ',') -cne ($keysByTheme.Light -join ',')) {
        throw "EtherSlider $theme component resource keys do not match Light."
    }
}
if (($keysByTheme.Light -join ',') -cne $expectedComponentKeys) {
    throw 'EtherSlider component resource keys differ from the expected lightweight key set.'
}

$highContrast = @($themeDictionaries | Where-Object { $_.GetAttribute('Key', $xamlNamespace) -eq 'HighContrast' })[0]
foreach ($resource in @($highContrast.ChildNodes | Where-Object { $_ -is [System.Xml.XmlElement] })) {
    if ($resource.OuterXml -notmatch 'SystemColor') {
        throw "EtherSlider HighContrast resource '$($resource.GetAttribute('Key', $xamlNamespace))' does not use a Windows SystemColor dynamic resource."
    }
}

$gallery = Get-Content -LiteralPath $galleryPath -Raw
foreach ($name in 'Interactive slider', 'Slider at 25 percent', 'Slider at 75 percent') {
    Assert-Contains $gallery ([regex]::Escape($name)) "EtherSlider gallery automation name '$name'"
}

$fixture = Get-Content -LiteralPath $fixturePath -Raw
foreach ($evidence in 'DefaultEtherSliderStyle', 'GetPattern\(PatternInterface\.RangeValue\)', 'SliderVerification', 'slider = result\?\.Slider', 'DisabledLocksAutomation', 'SetValueAccepted') {
    Assert-Contains $fixture $evidence "EtherSlider consumer runtime evidence for $evidence"
}

$ci = Get-Content -LiteralPath $ciPath -Raw
Assert-Contains $ci 'Verify-EtherSliderContract\.ps1' 'CI wiring for EtherSlider contract verifier'

Write-Host 'EtherSlider contract audit passed: RangeBase conversion, style/defaults, template metadata, component theme keys, High Contrast system resources, gallery automation names, RangeValue GetPattern evidence, FormatValue, and CI wiring are present.'
