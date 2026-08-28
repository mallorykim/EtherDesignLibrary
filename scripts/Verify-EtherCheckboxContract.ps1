[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$controlPath = Join-Path $repoRoot 'src\Ether.DesignSystem.Controls\Controls\Inputs\EtherCheckbox.cs'
$xamlPath = Join-Path $repoRoot 'src\Ether.DesignSystem.Controls\Controls\Inputs\EtherCheckbox.xaml'
$fixtureDirectory = Join-Path $repoRoot 'tests\Ether.DesignSystem.ConsumerFixtures'
$ciPath = Join-Path $repoRoot '.github\workflows\build.yml'
$xamlNamespace = 'http://schemas.microsoft.com/winfx/2006/xaml'
$expectedComponentKeys = 'EtherCheckboxCheckedFillDefaultBrush,EtherCheckboxCheckedFillHoverBrush,EtherCheckboxCheckedStrokeBrush,EtherCheckboxDisabledCheckedFillBrush,EtherCheckboxDisabledGlyphBrush,EtherCheckboxDisabledUncheckedFillBrush,EtherCheckboxDisabledUncheckedStrokeBrush,EtherCheckboxFillDefaultBrush,EtherCheckboxFillHoverBrush,EtherCheckboxFillPressedBrush,EtherCheckboxGlyphBrush,EtherCheckboxLabelBrush,EtherCheckboxStrokeBrush'
$templateParts = @('UncheckedFill', 'UncheckedFace', 'CheckedFace', 'Glyph', 'Label')

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
Assert-Contains $control 'DefaultStyleKey\s*=\s*typeof\(EtherCheckbox\)' 'EtherCheckbox constructor'
if ($control -match '(?m)^\s*(IsThreeState|UseSystemFocusVisuals|MinWidth|MinHeight|Padding)\s*=') {
    throw 'EtherCheckbox constructor must not assign its default visual setters; the keyed style owns them.'
}
foreach ($part in $templateParts) {
    Assert-Contains $control "TemplatePart\(Name = .*${part}" "EtherCheckbox TemplatePart contract for $part"
}
foreach ($state in 'Normal', 'PointerOver', 'Pressed', 'Disabled') {
    Assert-Contains $control "TemplateVisualState\(GroupName = CommonStatesGroup, Name = ${state}State\)" "EtherCheckbox TemplateVisualState contract for CommonStates/$state"
}
foreach ($state in 'Unchecked', 'Checked') {
    Assert-Contains $control "TemplateVisualState\(GroupName = CheckStatesGroup, Name = ${state}State\)" "EtherCheckbox TemplateVisualState contract for CheckStates/$state"
}

[xml]$xaml = Get-Content -LiteralPath $xamlPath -Raw
$styles = @($xaml.SelectNodes("//*[local-name()='Style']"))
$keyedStyle = @($styles | Where-Object { $_.GetAttribute('Key', $xamlNamespace) -eq 'DefaultEtherCheckboxStyle' })[0]
if ($null -eq $keyedStyle) {
    throw 'EtherCheckbox is missing keyed DefaultEtherCheckboxStyle.'
}
$implicitStyle = @($styles | Where-Object {
    -not $_.HasAttribute('Key', $xamlNamespace) -and $_.GetAttribute('BasedOn') -match 'DefaultEtherCheckboxStyle'
})[0]
if ($null -eq $implicitStyle) {
    throw 'EtherCheckbox is missing its implicit style BasedOn DefaultEtherCheckboxStyle.'
}
foreach ($setter in 'IsThreeState', 'UseSystemFocusVisuals', 'HighContrastAdjustment', 'MinWidth', 'MinHeight', 'Padding', 'HorizontalAlignment', 'VerticalAlignment', 'Template') {
    if ($null -eq $keyedStyle.SelectSingleNode("./*[local-name()='Setter' and @Property='$setter']")) {
        throw "DefaultEtherCheckboxStyle is missing its $setter setter."
    }
}

$isThreeStateSetter = $keyedStyle.SelectSingleNode("./*[local-name()='Setter' and @Property='IsThreeState']")
if ($isThreeStateSetter.GetAttribute('Value') -ne 'False') {
    throw 'DefaultEtherCheckboxStyle must keep IsThreeState=False.'
}

$rootTransparent = @($xaml.DocumentElement.ChildNodes | Where-Object {
    $_ -is [System.Xml.XmlElement] -and
    $_.LocalName -eq 'SolidColorBrush' -and
    $_.GetAttribute('Key', $xamlNamespace) -eq 'EtherCheckboxTransparentBrush'
})[0]
if ($null -eq $rootTransparent) {
    throw 'EtherCheckbox is missing its unthemed EtherCheckboxTransparentBrush for structural overlay fills.'
}

$templates = @($xaml.SelectNodes("//*[local-name()='ControlTemplate']"))
if ($templates.Count -lt 1) {
    throw 'EtherCheckbox is missing its ControlTemplate.'
}
foreach ($template in $templates) {
    if ($template.OuterXml -match '#[0-9A-Fa-f]{3,8}') {
        throw 'EtherCheckbox ControlTemplate contains a literal hex color; only component resources may carry color values.'
    }
    if ($template.OuterXml -match '(Background|BorderBrush|Foreground|Stroke|Fill)="Transparent"') {
        throw 'EtherCheckbox ControlTemplate contains a literal Transparent color; only component resources may carry color values.'
    }
    foreach ($themeResource in @([regex]::Matches($template.OuterXml, '\{ThemeResource\s+([^}\s]+)') | ForEach-Object { $_.Groups[1].Value })) {
        if ($themeResource -notlike 'EtherCheckbox*') {
            throw "EtherCheckbox template references non-component ThemeResource '$themeResource'."
        }
    }
    foreach ($state in 'Normal', 'PointerOver', 'Pressed', 'Disabled', 'Unchecked', 'Checked') {
        if (@($template.SelectNodes(".//*[local-name()='VisualState']") | Where-Object {
            $_.GetAttribute('Name', $xamlNamespace) -eq $state
        }).Count -ne 1) {
            throw "EtherCheckbox template is missing visual state $state."
        }
    }
}

$themeDictionaries = @($xaml.SelectNodes("//*[local-name()='ResourceDictionary.ThemeDictionaries']/*[local-name()='ResourceDictionary']"))
$keysByTheme = @{}
foreach ($dictionary in $themeDictionaries) {
    $themeAttribute = $dictionary.GetAttributeNode('Key', $xamlNamespace)
    if ($null -eq $themeAttribute) {
        throw 'EtherCheckbox has a theme dictionary without x:Key.'
    }

    $keys = Get-KeyedResources $dictionary
    if ($keys.Count -eq 0 -or @($keys | Where-Object { $_ -notlike 'EtherCheckbox*' }).Count -gt 0) {
        throw "EtherCheckbox $($themeAttribute.Value) resources must be component-scoped EtherCheckbox keys."
    }
    $keysByTheme[$themeAttribute.Value] = $keys
}

foreach ($theme in 'Light', 'Dark', 'HighContrast') {
    if (-not $keysByTheme.ContainsKey($theme)) {
        throw "EtherCheckbox is missing its $theme component theme dictionary."
    }
    if (($keysByTheme[$theme] -join ',') -cne ($keysByTheme.Light -join ',')) {
        throw "EtherCheckbox $theme component resource keys do not match Light."
    }
}
if (($keysByTheme.Light -join ',') -cne $expectedComponentKeys) {
    throw 'EtherCheckbox component resource keys differ from the expected lightweight key set.'
}

$highContrast = @($themeDictionaries | Where-Object { $_.GetAttribute('Key', $xamlNamespace) -eq 'HighContrast' })[0]
foreach ($resource in @($highContrast.ChildNodes | Where-Object { $_ -is [System.Xml.XmlElement] })) {
    if ($resource.OuterXml -notmatch 'SystemColor') {
        throw "EtherCheckbox HighContrast resource '$($resource.GetAttribute('Key', $xamlNamespace))' does not use a Windows SystemColor dynamic resource."
    }
}

$fixture = (Get-ChildItem -Path $fixtureDirectory -Filter 'RuntimeVerification*.cs' -File | Get-Content -Raw) -join [Environment]::NewLine
foreach ($evidence in 'DefaultEtherCheckboxStyle', 'Unchecked', 'Checked', 'CheckboxVerification', 'checkbox = result\?\.Checkbox') {
    Assert-Contains $fixture $evidence "EtherCheckbox consumer runtime evidence for $evidence"
}

$ci = Get-Content -LiteralPath $ciPath -Raw
Assert-Contains $ci 'Verify-EtherCheckboxContract\.ps1' 'CI wiring for EtherCheckbox contract verifier'

Write-Host 'EtherCheckbox contract audit passed: style/defaults, template metadata/states, component theme keys, High Contrast system resources, and runtime evidence are present.'
