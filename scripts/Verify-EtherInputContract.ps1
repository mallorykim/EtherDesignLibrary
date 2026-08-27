[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$controlPath = Join-Path $repoRoot 'src\Ether.DesignSystem.Controls\Controls\Inputs\EtherInput.cs'
$xamlPath = Join-Path $repoRoot 'src\Ether.DesignSystem.Controls\Controls\Inputs\EtherInput.xaml'
$fixturePath = Join-Path $repoRoot 'tests\Ether.DesignSystem.ConsumerFixtures\RuntimeVerification.cs'
$ciPath = Join-Path $repoRoot '.github\workflows\build.yml'
$xamlNamespace = 'http://schemas.microsoft.com/winfx/2006/xaml'
$expectedComponentKeys = 'EtherInputBorderBrush,EtherInputBorderFocusBrush,EtherInputFillDefaultBrush,EtherInputFillHoverBrush,EtherInputForegroundBrush,EtherInputPlaceholderBrush,EtherInputSelectionHighlightBrush'
$templateParts = @('LayoutRoot', 'BorderElement', 'PlaceholderTextContentPresenter', 'ContentElement')

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
Assert-Contains $control 'DefaultStyleKey\s*=\s*typeof\(EtherInput\)' 'EtherInput constructor'
if ($control -match '(?m)^\s*(FontSize|Padding|MinWidth|MinHeight|UseSystemFocusVisuals)\s*=') {
    throw 'EtherInput constructor must not assign its default visual setters; the keyed style owns them.'
}
foreach ($part in $templateParts) {
    Assert-Contains $control "TemplatePart\(Name = .*${part}" "EtherInput TemplatePart contract for $part"
}
foreach ($state in 'Normal', 'PointerOver', 'Focused', 'Disabled') {
    Assert-Contains $control "TemplateVisualState\(GroupName = CommonStatesGroup, Name = ${state}State\)" "EtherInput TemplateVisualState contract for CommonStates/$state"
}

[xml]$xaml = Get-Content -LiteralPath $xamlPath -Raw
$styles = @($xaml.SelectNodes("//*[local-name()='Style']"))
$keyedStyle = @($styles | Where-Object { $_.GetAttribute('Key', $xamlNamespace) -eq 'DefaultEtherInputStyle' })[0]
if ($null -eq $keyedStyle) {
    throw 'EtherInput is missing keyed DefaultEtherInputStyle.'
}
$implicitStyle = @($styles | Where-Object {
    -not $_.HasAttribute('Key', $xamlNamespace) -and $_.GetAttribute('BasedOn') -match 'DefaultEtherInputStyle'
})[0]
if ($null -eq $implicitStyle) {
    throw 'EtherInput is missing its implicit style BasedOn DefaultEtherInputStyle.'
}
foreach ($setter in 'Foreground', 'SelectionHighlightColor', 'FontFamily', 'FontSize', 'FontWeight', 'Padding', 'MinWidth', 'MinHeight', 'UseSystemFocusVisuals', 'HighContrastAdjustment', 'Template') {
    if ($null -eq $keyedStyle.SelectSingleNode("./*[local-name()='Setter' and @Property='$setter']")) {
        throw "DefaultEtherInputStyle is missing its $setter setter."
    }
}

$templates = @($xaml.SelectNodes("//*[local-name()='ControlTemplate']"))
if ($templates.Count -lt 1) {
    throw 'EtherInput is missing its ControlTemplate.'
}
foreach ($template in $templates) {
    if ($template.OuterXml -match '#[0-9A-Fa-f]{3,8}') {
        throw 'EtherInput ControlTemplate contains a literal hex color; only component resources may carry color values.'
    }
    if ($template.OuterXml -match '(Background|BorderBrush|Foreground|Stroke|Fill)="Transparent"') {
        throw 'EtherInput ControlTemplate contains a literal Transparent color; only component resources may carry color values.'
    }
    foreach ($themeResource in @([regex]::Matches($template.OuterXml, '\{ThemeResource\s+([^}\s]+)') | ForEach-Object { $_.Groups[1].Value })) {
        if ($themeResource -notlike 'EtherInput*') {
            throw "EtherInput template references non-component ThemeResource '$themeResource'."
        }
    }
    foreach ($state in 'Normal', 'PointerOver', 'Focused', 'Disabled') {
        if (@($template.SelectNodes(".//*[local-name()='VisualState']") | Where-Object {
            $_.GetAttribute('Name', $xamlNamespace) -eq $state
        }).Count -ne 1) {
            throw "EtherInput template is missing visual state $state."
        }
    }
}

$themeDictionaries = @($xaml.SelectNodes("//*[local-name()='ResourceDictionary.ThemeDictionaries']/*[local-name()='ResourceDictionary']"))
$keysByTheme = @{}
foreach ($dictionary in $themeDictionaries) {
    $themeAttribute = $dictionary.GetAttributeNode('Key', $xamlNamespace)
    if ($null -eq $themeAttribute) {
        throw 'EtherInput has a theme dictionary without x:Key.'
    }

    $keys = Get-KeyedResources $dictionary
    if ($keys.Count -eq 0 -or @($keys | Where-Object { $_ -notlike 'EtherInput*' }).Count -gt 0) {
        throw "EtherInput $($themeAttribute.Value) resources must be component-scoped EtherInput keys."
    }
    $keysByTheme[$themeAttribute.Value] = $keys
}

foreach ($theme in 'Light', 'Dark', 'HighContrast') {
    if (-not $keysByTheme.ContainsKey($theme)) {
        throw "EtherInput is missing its $theme component theme dictionary."
    }
    if (($keysByTheme[$theme] -join ',') -cne ($keysByTheme.Light -join ',')) {
        throw "EtherInput $theme component resource keys do not match Light."
    }
}
if (($keysByTheme.Light -join ',') -cne $expectedComponentKeys) {
    throw 'EtherInput component resource keys differ from the expected lightweight key set.'
}

$highContrast = @($themeDictionaries | Where-Object { $_.GetAttribute('Key', $xamlNamespace) -eq 'HighContrast' })[0]
foreach ($resource in @($highContrast.ChildNodes | Where-Object { $_ -is [System.Xml.XmlElement] })) {
    if ($resource.OuterXml -notmatch 'SystemColor') {
        throw "EtherInput HighContrast resource '$($resource.GetAttribute('Key', $xamlNamespace))' does not use a Windows SystemColor dynamic resource."
    }
}

$fixture = Get-Content -LiteralPath $fixturePath -Raw
foreach ($evidence in 'DefaultEtherInputStyle', 'PointerOver', 'Focused', 'InputVerification', 'input = result\?\.Input') {
    Assert-Contains $fixture $evidence "EtherInput consumer runtime evidence for $evidence"
}

$ci = Get-Content -LiteralPath $ciPath -Raw
Assert-Contains $ci 'Verify-EtherInputContract\.ps1' 'CI wiring for EtherInput contract verifier'

Write-Host 'EtherInput contract audit passed: style/defaults, template metadata/states, component theme keys, High Contrast system resources, and runtime evidence are present.'
