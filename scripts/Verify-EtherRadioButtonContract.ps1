[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$controlPath = Join-Path $repoRoot 'src\Ether.DesignSystem.Controls\Controls\Inputs\EtherRadioButton.cs'
$xamlPath = Join-Path $repoRoot 'src\Ether.DesignSystem.Controls\Controls\Inputs\EtherRadioButton.xaml'
$fixtureDirectory = Join-Path $repoRoot 'tests\Ether.DesignSystem.ConsumerFixtures'
$ciPath = Join-Path $repoRoot '.github\workflows\build.yml'
$xamlNamespace = 'http://schemas.microsoft.com/winfx/2006/xaml'
$expectedComponentKeys = 'EtherRadioButtonCheckedFillDefaultBrush,EtherRadioButtonCheckedFillHoverBrush,EtherRadioButtonCheckedStrokeBrush,EtherRadioButtonCircleFillDefaultBrush,EtherRadioButtonCircleFillHoverBrush,EtherRadioButtonCircleFillPressedBrush,EtherRadioButtonCircleStrokeBrush,EtherRadioButtonDisabledCheckedDotBrush,EtherRadioButtonDisabledCheckedFillBrush,EtherRadioButtonDisabledUncheckedFillBrush,EtherRadioButtonDisabledUncheckedStrokeBrush,EtherRadioButtonInnerDotBrush,EtherRadioButtonLabelBrush'
$templateParts = @('UncheckedFill', 'UncheckedFace', 'CheckedFace', 'Dot', 'Label')

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
Assert-Contains $control 'DefaultStyleKey\s*=\s*typeof\(EtherRadioButton\)' 'EtherRadioButton constructor'
if ($control -match '(?m)^\s*(IsThreeState|UseSystemFocusVisuals|MinWidth|MinHeight|Padding)\s*=') {
    throw 'EtherRadioButton constructor must not assign its default visual setters; the keyed style owns them.'
}
foreach ($part in $templateParts) {
    Assert-Contains $control "TemplatePart\(Name = .*${part}" "EtherRadioButton TemplatePart contract for $part"
}
foreach ($state in 'Normal', 'PointerOver', 'Pressed', 'Disabled') {
    Assert-Contains $control "TemplateVisualState\(GroupName = CommonStatesGroup, Name = ${state}State\)" "EtherRadioButton TemplateVisualState contract for CommonStates/$state"
}
foreach ($state in 'Unchecked', 'Checked') {
    Assert-Contains $control "TemplateVisualState\(GroupName = CheckStatesGroup, Name = ${state}State\)" "EtherRadioButton TemplateVisualState contract for CheckStates/$state"
}

[xml]$xaml = Get-Content -LiteralPath $xamlPath -Raw
$styles = @($xaml.SelectNodes("//*[local-name()='Style']"))
$keyedStyle = @($styles | Where-Object { $_.GetAttribute('Key', $xamlNamespace) -eq 'DefaultEtherRadioButtonStyle' })[0]
if ($null -eq $keyedStyle) {
    throw 'EtherRadioButton is missing keyed DefaultEtherRadioButtonStyle.'
}
$implicitStyle = @($styles | Where-Object {
    -not $_.HasAttribute('Key', $xamlNamespace) -and $_.GetAttribute('BasedOn') -match 'DefaultEtherRadioButtonStyle'
})[0]
if ($null -eq $implicitStyle) {
    throw 'EtherRadioButton is missing its implicit style BasedOn DefaultEtherRadioButtonStyle.'
}
foreach ($setter in 'IsThreeState', 'UseSystemFocusVisuals', 'HighContrastAdjustment', 'MinWidth', 'MinHeight', 'Padding', 'HorizontalAlignment', 'VerticalAlignment', 'Template') {
    if ($null -eq $keyedStyle.SelectSingleNode("./*[local-name()='Setter' and @Property='$setter']")) {
        throw "DefaultEtherRadioButtonStyle is missing its $setter setter."
    }
}

$isThreeStateSetter = $keyedStyle.SelectSingleNode("./*[local-name()='Setter' and @Property='IsThreeState']")
if ($isThreeStateSetter.GetAttribute('Value') -ne 'False') {
    throw 'DefaultEtherRadioButtonStyle must keep IsThreeState=False.'
}

$rootTransparent = @($xaml.DocumentElement.ChildNodes | Where-Object {
    $_ -is [System.Xml.XmlElement] -and
    $_.LocalName -eq 'SolidColorBrush' -and
    $_.GetAttribute('Key', $xamlNamespace) -eq 'EtherRadioButtonTransparentBrush'
})[0]
if ($null -eq $rootTransparent) {
    throw 'EtherRadioButton is missing its unthemed EtherRadioButtonTransparentBrush for structural overlay fills.'
}

$templates = @($xaml.SelectNodes("//*[local-name()='ControlTemplate']"))
if ($templates.Count -lt 1) {
    throw 'EtherRadioButton is missing its ControlTemplate.'
}
foreach ($template in $templates) {
    if ($template.OuterXml -match '#[0-9A-Fa-f]{3,8}') {
        throw 'EtherRadioButton ControlTemplate contains a literal hex color; only component resources may carry color values.'
    }
    if ($template.OuterXml -match '(Background|BorderBrush|Foreground|Stroke|Fill)="Transparent"') {
        throw 'EtherRadioButton ControlTemplate contains a literal Transparent color; only component resources may carry color values.'
    }
    foreach ($themeResource in @([regex]::Matches($template.OuterXml, '\{ThemeResource\s+([^}\s]+)') | ForEach-Object { $_.Groups[1].Value })) {
        if ($themeResource -notlike 'EtherRadioButton*') {
            throw "EtherRadioButton template references non-component ThemeResource '$themeResource'."
        }
    }
    foreach ($state in 'Normal', 'PointerOver', 'Pressed', 'Disabled', 'Unchecked', 'Checked') {
        if (@($template.SelectNodes(".//*[local-name()='VisualState']") | Where-Object {
            $_.GetAttribute('Name', $xamlNamespace) -eq $state
        }).Count -ne 1) {
            throw "EtherRadioButton template is missing visual state $state."
        }
    }
}

$themeDictionaries = @($xaml.SelectNodes("//*[local-name()='ResourceDictionary.ThemeDictionaries']/*[local-name()='ResourceDictionary']"))
$keysByTheme = @{}
foreach ($dictionary in $themeDictionaries) {
    $themeAttribute = $dictionary.GetAttributeNode('Key', $xamlNamespace)
    if ($null -eq $themeAttribute) {
        throw 'EtherRadioButton has a theme dictionary without x:Key.'
    }

    $keys = Get-KeyedResources $dictionary
    if ($keys.Count -eq 0 -or @($keys | Where-Object { $_ -notlike 'EtherRadioButton*' }).Count -gt 0) {
        throw "EtherRadioButton $($themeAttribute.Value) resources must be component-scoped EtherRadioButton keys."
    }
    $keysByTheme[$themeAttribute.Value] = $keys
}

foreach ($theme in 'Light', 'Dark', 'HighContrast') {
    if (-not $keysByTheme.ContainsKey($theme)) {
        throw "EtherRadioButton is missing its $theme component theme dictionary."
    }
    if (($keysByTheme[$theme] -join ',') -cne ($keysByTheme.Light -join ',')) {
        throw "EtherRadioButton $theme component resource keys do not match Light."
    }
}
if (($keysByTheme.Light -join ',') -cne $expectedComponentKeys) {
    throw 'EtherRadioButton component resource keys differ from the expected lightweight key set.'
}

$highContrast = @($themeDictionaries | Where-Object { $_.GetAttribute('Key', $xamlNamespace) -eq 'HighContrast' })[0]
foreach ($resource in @($highContrast.ChildNodes | Where-Object { $_ -is [System.Xml.XmlElement] })) {
    if ($resource.OuterXml -notmatch 'SystemColor') {
        throw "EtherRadioButton HighContrast resource '$($resource.GetAttribute('Key', $xamlNamespace))' does not use a Windows SystemColor dynamic resource."
    }
}

$fixture = (Get-ChildItem -Path $fixtureDirectory -Filter 'RuntimeVerification*.cs' -File | Get-Content -Raw) -join [Environment]::NewLine
foreach ($evidence in 'DefaultEtherRadioButtonStyle', 'Unchecked', 'Checked', 'RadioButtonVerification', 'radioButton = result\?\.RadioButton') {
    Assert-Contains $fixture $evidence "EtherRadioButton consumer runtime evidence for $evidence"
}

$ci = Get-Content -LiteralPath $ciPath -Raw
Assert-Contains $ci 'Verify-EtherRadioButtonContract\.ps1' 'CI wiring for EtherRadioButton contract verifier'

Write-Host 'EtherRadioButton contract audit passed: style/defaults, template metadata/states, component theme keys, High Contrast system resources, and runtime evidence are present.'
