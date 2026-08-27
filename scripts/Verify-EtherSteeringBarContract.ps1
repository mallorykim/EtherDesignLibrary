[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$controlPath = Join-Path $repoRoot 'src\Ether.DesignSystem.Controls\Controls\Inputs\EtherSteeringBar.xaml.cs'
$xamlPath = Join-Path $repoRoot 'src\Ether.DesignSystem.Controls\Controls\Inputs\EtherSteeringBar.xaml'
$galleryPath = Join-Path $repoRoot 'samples\Ether.DesignSystem.Gallery\Views\Controls\SteeringBarPage.xaml'
$fixturePath = Join-Path $repoRoot 'tests\Ether.DesignSystem.ConsumerFixtures\RuntimeVerification.cs'
$ciPath = Join-Path $repoRoot '.github\workflows\build.yml'
$xamlNamespace = 'http://schemas.microsoft.com/winfx/2006/xaml'
$expectedComponentKeys = 'EtherSteeringBarFillBrush,EtherSteeringBarStopMarkerActiveBrush,EtherSteeringBarStopMarkerDisabledActiveBrush,EtherSteeringBarStopMarkerDisabledInactiveBrush,EtherSteeringBarStopMarkerInactiveBrush,EtherSteeringBarThumbBorderBrush,EtherSteeringBarThumbDisabledFillBrush,EtherSteeringBarThumbFillBrush,EtherSteeringBarThumbHighlightBrush,EtherSteeringBarThumbShadowFarBrush,EtherSteeringBarThumbShadowNearBrush,EtherSteeringBarThumbSheenBrush,EtherSteeringBarThumbTopHighlightBrush,EtherSteeringBarTitleForegroundBrush,EtherSteeringBarTrackBrush,EtherSteeringBarValueForegroundBrush'
$templateParts = @('InteractionSurface', 'TrackBackground', 'FillBorder', 'StopMarkersHost', 'ThumbHost', 'ThumbBorder', 'ThumbFill', 'ThumbDisabledShell', 'ShadowFar', 'ShadowNear', 'LabelRow', 'TitleText', 'ValueLabel')

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
Assert-Contains $control 'DefaultStyleKey\s*=\s*typeof\(EtherSteeringBar\)' 'EtherSteeringBar constructor'
if ($control -notmatch '(?s)public EtherSteeringBar\(\)\s*\{(?<ctor>.*?)\n    \}') {
    throw 'EtherSteeringBar constructor body could not be isolated for default-setter audit.'
}
if ($Matches['ctor'] -match '(Minimum|Maximum|Value|ShowTitle|ShowValue|UseSystemFocusVisuals|IsTabStop)\s*=') {
    throw 'EtherSteeringBar constructor must not assign its default visual setters; the keyed style owns them.'
}
foreach ($part in $templateParts) {
    Assert-Contains $control "TemplatePart\(Name = .*${part}" "EtherSteeringBar TemplatePart contract for $part"
}
foreach ($state in 'BothLabelsVisible', 'TitleOnly', 'ValueOnly', 'LabelsHidden') {
    Assert-Contains $control "TemplateVisualState\(GroupName = LabelStatesGroup, Name = ${state}State\)" "EtherSteeringBar TemplateVisualState contract for $state"
}
Assert-Contains $control 'VisualStateManager\.GoToState\(this, state, false\)' 'EtherSteeringBar label state transition'
if ($control -match '\.Visibility\s*=') {
    throw 'EtherSteeringBar must drive label visibility with VisualStateManager, not direct Visibility assignments.'
}
Assert-Contains $control 'PreviewStatus' 'EtherSteeringBar Gallery-oriented PreviewStatus surface'
Assert-Contains $control 'AutomationControlType\.Slider' 'EtherSteeringBar automation control type'
Assert-Contains $control 'PatternInterface\.RangeValue' 'EtherSteeringBar RangeValue pattern'
Assert-Contains $control 'RangeValuePatternIdentifiers\.ValueProperty' 'EtherSteeringBar RangeValue value-changed event'

[xml]$xaml = Get-Content -LiteralPath $xamlPath -Raw
$styles = @($xaml.SelectNodes("//*[local-name()='Style']"))
$keyedStyle = @($styles | Where-Object { $_.GetAttribute('Key', $xamlNamespace) -eq 'DefaultEtherSteeringBarStyle' })[0]
if ($null -eq $keyedStyle) {
    throw 'EtherSteeringBar is missing keyed DefaultEtherSteeringBarStyle.'
}
$implicitStyle = @($styles | Where-Object {
    -not $_.HasAttribute('Key', $xamlNamespace) -and $_.GetAttribute('BasedOn') -match 'DefaultEtherSteeringBarStyle'
})[0]
if ($null -eq $implicitStyle) {
    throw 'EtherSteeringBar is missing its implicit style BasedOn DefaultEtherSteeringBarStyle.'
}
foreach ($setter in 'IsTabStop', 'UseSystemFocusVisuals', 'HighContrastAdjustment', 'Minimum', 'Maximum', 'Value', 'ShowTitle', 'ShowValue', 'Template') {
    if ($null -eq $keyedStyle.SelectSingleNode("./*[local-name()='Setter' and @Property='$setter']")) {
        throw "DefaultEtherSteeringBarStyle is missing its $setter setter."
    }
}

$template = $keyedStyle.SelectSingleNode(".//*[local-name()='ControlTemplate']")
if ($null -eq $template) {
    throw 'DefaultEtherSteeringBarStyle is missing its ControlTemplate.'
}
if ($template.OuterXml -match '#[0-9A-Fa-f]{3,8}') {
    throw 'EtherSteeringBar ControlTemplate contains a literal hex color; only component resources may carry color values.'
}
foreach ($state in 'BothLabelsVisible', 'TitleOnly', 'ValueOnly', 'LabelsHidden') {
    if (@($template.SelectNodes(".//*[local-name()='VisualState']") | Where-Object {
        $_.GetAttribute('Name', $xamlNamespace) -eq $state
    }).Count -ne 1) {
        throw "EtherSteeringBar template is missing LabelStates/$state."
    }
}
foreach ($themeResource in @([regex]::Matches($template.OuterXml, '\{ThemeResource\s+([^}\s]+)') | ForEach-Object { $_.Groups[1].Value })) {
    if ($themeResource -notlike 'EtherSteeringBar*') {
        throw "EtherSteeringBar template references non-component ThemeResource '$themeResource'."
    }
}

$themeDictionaries = @($xaml.SelectNodes("//*[local-name()='ResourceDictionary.ThemeDictionaries']/*[local-name()='ResourceDictionary']"))
$keysByTheme = @{}
foreach ($dictionary in $themeDictionaries) {
    $themeAttribute = $dictionary.GetAttributeNode('Key', $xamlNamespace)
    if ($null -eq $themeAttribute) {
        throw 'EtherSteeringBar has a theme dictionary without x:Key.'
    }

    $keys = Get-KeyedResources $dictionary
    if ($keys.Count -eq 0 -or @($keys | Where-Object { $_ -notlike 'EtherSteeringBar*' }).Count -gt 0) {
        throw "EtherSteeringBar $($themeAttribute.Value) resources must be component-scoped EtherSteeringBar keys."
    }
    $keysByTheme[$themeAttribute.Value] = $keys
}

foreach ($theme in 'Light', 'Dark', 'HighContrast') {
    if (-not $keysByTheme.ContainsKey($theme)) {
        throw "EtherSteeringBar is missing its $theme component theme dictionary."
    }
    if (($keysByTheme[$theme] -join ',') -cne ($keysByTheme.Light -join ',')) {
        throw "EtherSteeringBar $theme component resource keys do not match Light."
    }
}
if (($keysByTheme.Light -join ',') -cne $expectedComponentKeys) {
    throw 'EtherSteeringBar component resource keys differ from the expected lightweight key set.'
}

$highContrast = @($themeDictionaries | Where-Object { $_.GetAttribute('Key', $xamlNamespace) -eq 'HighContrast' })[0]
foreach ($resource in @($highContrast.ChildNodes | Where-Object { $_ -is [System.Xml.XmlElement] })) {
    if ($resource.OuterXml -notmatch 'SystemColor') {
        throw "EtherSteeringBar HighContrast resource '$($resource.GetAttribute('Key', $xamlNamespace))' does not use a Windows SystemColor dynamic resource."
    }
}

$gallery = Get-Content -LiteralPath $galleryPath -Raw
foreach ($name in 'Interactive steering bar', 'Interactive steering bar with stops', 'Default steering bar', 'Hover steering bar', 'Pressed steering bar', 'Disabled steering bar') {
    Assert-Contains $gallery ([regex]::Escape($name)) "EtherSteeringBar gallery automation name '$name'"
}

$fixture = Get-Content -LiteralPath $fixturePath -Raw
foreach ($evidence in 'DefaultEtherSteeringBarStyle', 'GetPattern\(PatternInterface\.RangeValue\)', 'SteeringBarVerification', 'steeringBar = result\?\.SteeringBar', 'PreviewStatusLocksAutomation', 'SetValueAccepted') {
    Assert-Contains $fixture $evidence "EtherSteeringBar consumer runtime evidence for $evidence"
}

$ci = Get-Content -LiteralPath $ciPath -Raw
Assert-Contains $ci 'Verify-EtherSteeringBarContract\.ps1' 'CI wiring for EtherSteeringBar contract verifier'

Write-Host 'EtherSteeringBar contract audit passed: style/defaults, template metadata/states, component theme keys, High Contrast system resources, gallery automation names, PreviewStatus, RangeValue GetPattern evidence, and CI wiring are present.'
