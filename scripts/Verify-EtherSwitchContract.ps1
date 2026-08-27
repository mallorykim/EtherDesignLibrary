[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$controlPath = Join-Path $repoRoot 'src\Controls\Inputs\EtherSwitch.xaml.cs'
$xamlPath = Join-Path $repoRoot 'src\Controls\Inputs\EtherSwitch.xaml'
$galleryPath = Join-Path $repoRoot 'src\Views\Controls\ToggleSwitchPage.xaml'
$fixturePath = Join-Path $repoRoot 'tests\Ether.DesignSystem.ConsumerFixtures\RuntimeVerification.cs'
$ciPath = Join-Path $repoRoot '.github\workflows\build.yml'
$xamlNamespace = 'http://schemas.microsoft.com/winfx/2006/xaml'
$expectedComponentKeys = 'EtherSwitchFocusBrush,EtherSwitchKnobDisabledBrush,EtherSwitchKnobFillBrush,EtherSwitchKnobFillHoverBrush,EtherSwitchKnobFillPressedBrush,EtherSwitchKnobShadow1Brush,EtherSwitchKnobShadow2Brush,EtherSwitchKnobShadow3Brush,EtherSwitchKnobStrokeBrush,EtherSwitchLabelForegroundBrush,EtherSwitchOffTrackBrush,EtherSwitchOnTrackBrush,EtherSwitchOnTrackGlowFillBrush,EtherSwitchOnTrackGlowStrokeBrush'

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
Assert-Contains $control 'class EtherSwitch\s*:\s*ResourceDictionary' 'EtherSwitch ResourceDictionary contract'
if ($control -match 'DefaultStyleKey') {
    throw 'EtherSwitch must stay a keyed ResourceDictionary style contract; do not introduce DefaultStyleKey.'
}
if ($control -match 'class EtherSwitch\s*:\s*(Control|ToggleSwitch)') {
    throw 'EtherSwitch must not become a custom ToggleSwitch subclass.'
}

[xml]$xaml = Get-Content -LiteralPath $xamlPath -Raw
$styles = @($xaml.SelectNodes("//*[local-name()='Style']"))
$keyedStyle = @($styles | Where-Object { $_.GetAttribute('Key', $xamlNamespace) -eq 'EtherSwitch' })[0]
if ($null -eq $keyedStyle) {
    throw 'EtherSwitch is missing keyed Style x:Key="EtherSwitch".'
}
if ($keyedStyle.GetAttribute('TargetType') -ne 'ToggleSwitch') {
    throw 'Keyed EtherSwitch must target the stock ToggleSwitch type.'
}
$implicitToggle = @($styles | Where-Object {
    -not $_.HasAttribute('Key', $xamlNamespace) -and $_.GetAttribute('TargetType') -eq 'ToggleSwitch'
})
if ($implicitToggle.Count -ne 0) {
    throw 'EtherSwitch must stay keyed; an implicit ToggleSwitch style would restyle every bare ToggleSwitch.'
}
foreach ($setter in 'UseSystemFocusVisuals', 'Template') {
    if ($null -eq $keyedStyle.SelectSingleNode("./*[local-name()='Setter' and @Property='$setter']")) {
        throw "EtherSwitch style is missing its $setter setter."
    }
}

$templates = @($xaml.SelectNodes("//*[local-name()='ControlTemplate']"))
if ($templates.Count -lt 1) {
    throw 'EtherSwitch is missing EtherSwitchTemplate.'
}
$namedTemplate = @($templates | Where-Object { $_.GetAttribute('Key', $xamlNamespace) -eq 'EtherSwitchTemplate' })[0]
if ($null -eq $namedTemplate) {
    throw 'EtherSwitch is missing keyed EtherSwitchTemplate.'
}
foreach ($template in $templates) {
    if ($template.OuterXml -match '#[0-9A-Fa-f]{3,8}') {
        throw 'EtherSwitch ControlTemplate contains a literal hex color; only component resources may carry color values.'
    }
    foreach ($themeResource in @([regex]::Matches($template.OuterXml, '\{ThemeResource\s+([^}\s]+)') | ForEach-Object { $_.Groups[1].Value })) {
        if ($themeResource -notlike 'EtherSwitch*') {
            throw "EtherSwitch template references non-component ThemeResource '$themeResource'."
        }
    }
}

$themeDictionaries = @($xaml.SelectNodes("//*[local-name()='ResourceDictionary.ThemeDictionaries']/*[local-name()='ResourceDictionary']"))
$keysByTheme = @{}
foreach ($dictionary in $themeDictionaries) {
    $themeAttribute = $dictionary.GetAttributeNode('Key', $xamlNamespace)
    if ($null -eq $themeAttribute) {
        throw 'EtherSwitch has a theme dictionary without x:Key.'
    }

    $keys = Get-KeyedResources $dictionary
    if ($keys.Count -eq 0 -or @($keys | Where-Object { $_ -notlike 'EtherSwitch*' }).Count -gt 0) {
        throw "EtherSwitch $($themeAttribute.Value) resources must be component-scoped EtherSwitch keys."
    }
    $keysByTheme[$themeAttribute.Value] = $keys
}

foreach ($theme in 'Light', 'Dark', 'HighContrast') {
    if (-not $keysByTheme.ContainsKey($theme)) {
        throw "EtherSwitch is missing its $theme component theme dictionary."
    }
    if (($keysByTheme[$theme] -join ',') -cne ($keysByTheme.Light -join ',')) {
        throw "EtherSwitch $theme component resource keys do not match Light."
    }
}
if (($keysByTheme.Light -join ',') -cne $expectedComponentKeys) {
    throw 'EtherSwitch component resource keys differ from the expected lightweight key set.'
}

$highContrast = @($themeDictionaries | Where-Object { $_.GetAttribute('Key', $xamlNamespace) -eq 'HighContrast' })[0]
foreach ($resource in @($highContrast.ChildNodes | Where-Object { $_ -is [System.Xml.XmlElement] })) {
    if ($resource.OuterXml -notmatch 'SystemColor') {
        throw "EtherSwitch HighContrast resource '$($resource.GetAttribute('Key', $xamlNamespace))' does not use a Windows SystemColor dynamic resource."
    }
}

$gallery = Get-Content -LiteralPath $galleryPath -Raw
foreach ($name in 'Interactive toggle switch', 'Toggle switch off', 'Toggle switch on') {
    Assert-Contains $gallery ([regex]::Escape($name)) "EtherSwitch gallery automation name '$name'"
}

$fixture = Get-Content -LiteralPath $fixturePath -Raw
foreach ($evidence in 'ToggleSwitchVerification', 'toggleSwitch = result\?\.ToggleSwitch', 'KeyedStyleResolved', 'ImplicitStyleRejected', 'EtherSwitch') {
    Assert-Contains $fixture $evidence "EtherSwitch consumer runtime evidence for $evidence"
}

$ci = Get-Content -LiteralPath $ciPath -Raw
Assert-Contains $ci 'Verify-EtherSwitchContract\.ps1' 'CI wiring for EtherSwitch contract verifier'

Write-Host 'EtherSwitch contract audit passed: keyed ResourceDictionary style, no implicit ToggleSwitch restyle, EtherSwitchTemplate, component theme keys, High Contrast system resources, gallery automation names, consumer evidence, and CI wiring are present.'
