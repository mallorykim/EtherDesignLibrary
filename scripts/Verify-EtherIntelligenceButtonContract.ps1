[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$controlPath = Join-Path $repoRoot 'src\Controls\Inputs\EtherIntelligenceButton.cs'
$xamlPath = Join-Path $repoRoot 'src\Controls\Inputs\EtherIntelligenceButton.xaml'
$galleryPath = Join-Path $repoRoot 'src\Views\Controls\IntelligenceButtonPage.xaml'
$fixturePath = Join-Path $repoRoot 'tests\Ether.DesignSystem.ConsumerFixtures\RuntimeVerification.cs'
$ciPath = Join-Path $repoRoot '.github\workflows\build.yml'
$xamlNamespace = 'http://schemas.microsoft.com/winfx/2006/xaml'
$expectedComponentKeys = 'EtherIntelligenceButtonBackgroundBrush,EtherIntelligenceButtonBlueGlowCoreBrush,EtherIntelligenceButtonBlueGlowMiddleBrush,EtherIntelligenceButtonBlueGlowOuterBrush,EtherIntelligenceButtonBlueGlowPressedCoreBrush,EtherIntelligenceButtonBlueGlowPressedMiddleBrush,EtherIntelligenceButtonBlueGlowPressedOuterBrush,EtherIntelligenceButtonBorderBlueBrush,EtherIntelligenceButtonBorderBrush,EtherIntelligenceButtonBorderHoverBrush,EtherIntelligenceButtonFocusStrokeBrush,EtherIntelligenceButtonForegroundBrush,EtherIntelligenceButtonPurpleGlowBrush'
$templateParts = @(
    'BgPart',
    'BorderIntelligenceBluePart',
    'BorderIntelligenceGradientPart',
    'BlueGlowOuterPart',
    'BlueGlowMiddlePart',
    'BlueGlowCorePart',
    'PurpleGlowPart',
    'ContentPresenterPart',
    'FocusRingPart'
)

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
Assert-Contains $control 'DefaultStyleKey\s*=\s*typeof\(EtherIntelligenceButton\)' 'EtherIntelligenceButton constructor'
if ($control -match '(?m)^\s*(MinHeight|Padding|FontSize)\s*=') {
    throw 'EtherIntelligenceButton constructor must not assign its default visual setters; the keyed style owns them.'
}
foreach ($part in $templateParts) {
    Assert-Contains $control "TemplatePart\(Name = ${part}" "EtherIntelligenceButton TemplatePart contract for $part"
}
foreach ($state in 'Normal', 'PointerOver', 'Pressed', 'Disabled') {
    Assert-Contains $control "TemplateVisualState\(GroupName = CommonStatesGroup, Name = ${state}State\)" "EtherIntelligenceButton TemplateVisualState contract for CommonStates/$state"
}
foreach ($state in 'Focused', 'Unfocused', 'PointerFocused') {
    Assert-Contains $control "TemplateVisualState\(GroupName = FocusStatesGroup, Name = ${state}State\)" "EtherIntelligenceButton TemplateVisualState contract for FocusStates/$state"
}

[xml]$xaml = Get-Content -LiteralPath $xamlPath -Raw
$styles = @($xaml.SelectNodes("//*[local-name()='Style']"))
$keyedStyle = @($styles | Where-Object { $_.GetAttribute('Key', $xamlNamespace) -eq 'DefaultEtherIntelligenceButtonStyle' })[0]
if ($null -eq $keyedStyle) {
    throw 'EtherIntelligenceButton is missing keyed DefaultEtherIntelligenceButtonStyle.'
}
$implicitStyle = @($styles | Where-Object {
    -not $_.HasAttribute('Key', $xamlNamespace) -and $_.GetAttribute('BasedOn') -match 'DefaultEtherIntelligenceButtonStyle'
})[0]
if ($null -eq $implicitStyle) {
    throw 'EtherIntelligenceButton is missing its implicit style BasedOn DefaultEtherIntelligenceButtonStyle.'
}
foreach ($setter in 'Padding', 'FontSize', 'HorizontalContentAlignment', 'VerticalContentAlignment', 'UseSystemFocusVisuals', 'Template') {
    if ($null -eq $keyedStyle.SelectSingleNode("./*[local-name()='Setter' and @Property='$setter']")) {
        throw "DefaultEtherIntelligenceButtonStyle is missing its $setter setter."
    }
}

$templates = @($xaml.SelectNodes("//*[local-name()='ControlTemplate']"))
if ($templates.Count -lt 1) {
    throw 'EtherIntelligenceButton is missing its ControlTemplate.'
}
foreach ($template in $templates) {
    if ($template.OuterXml -match '#[0-9A-Fa-f]{3,8}') {
        throw 'EtherIntelligenceButton ControlTemplate contains a literal hex color; only component resources may carry color values.'
    }
    if ($template.OuterXml -match 'StaticResource\s+(Blue|Gray)\d') {
        throw 'EtherIntelligenceButton ControlTemplate must not reference primitive color StaticResources; use component ThemeResources.'
    }
    if ($template.OuterXml -match '(Background|BorderBrush|Foreground|Value)="Transparent"') {
        throw 'EtherIntelligenceButton ControlTemplate contains a literal Transparent color; only component resources may carry color values.'
    }
    foreach ($themeResource in @([regex]::Matches($template.OuterXml, '\{ThemeResource\s+([^}\s]+)') | ForEach-Object { $_.Groups[1].Value })) {
        if ($themeResource -notlike 'EtherIntelligenceButton*') {
            throw "EtherIntelligenceButton template references non-component ThemeResource '$themeResource'."
        }
    }
    foreach ($state in 'Normal', 'PointerOver', 'Pressed', 'Disabled') {
        if (@($template.SelectNodes(".//*[local-name()='VisualState']") | Where-Object {
            $_.GetAttribute('Name', $xamlNamespace) -eq $state
        }).Count -ne 1) {
            throw "EtherIntelligenceButton template is missing CommonStates/$state."
        }
    }
    foreach ($state in 'Focused', 'Unfocused', 'PointerFocused') {
        if (@($template.SelectNodes(".//*[local-name()='VisualState']") | Where-Object {
            $_.GetAttribute('Name', $xamlNamespace) -eq $state
        }).Count -ne 1) {
            throw "EtherIntelligenceButton template is missing FocusStates/$state."
        }
    }
}

$themeDictionaries = @($xaml.SelectNodes("//*[local-name()='ResourceDictionary.ThemeDictionaries']/*[local-name()='ResourceDictionary']"))
$keysByTheme = @{}
foreach ($dictionary in $themeDictionaries) {
    $themeAttribute = $dictionary.GetAttributeNode('Key', $xamlNamespace)
    if ($null -eq $themeAttribute) {
        throw 'EtherIntelligenceButton has a theme dictionary without x:Key.'
    }

    $keys = Get-KeyedResources $dictionary
    if ($keys.Count -eq 0 -or @($keys | Where-Object { $_ -notlike 'EtherIntelligenceButton*' }).Count -gt 0) {
        throw "EtherIntelligenceButton $($themeAttribute.Value) resources must be component-scoped EtherIntelligenceButton keys."
    }
    $keysByTheme[$themeAttribute.Value] = $keys
}

foreach ($theme in 'Light', 'Dark', 'HighContrast') {
    if (-not $keysByTheme.ContainsKey($theme)) {
        throw "EtherIntelligenceButton is missing its $theme component theme dictionary."
    }
    if (($keysByTheme[$theme] -join ',') -cne ($keysByTheme.Light -join ',')) {
        throw "EtherIntelligenceButton $theme component resource keys do not match Light."
    }
}
if (($keysByTheme.Light -join ',') -cne $expectedComponentKeys) {
    throw 'EtherIntelligenceButton component resource keys differ from the expected lightweight key set.'
}

$highContrast = @($themeDictionaries | Where-Object { $_.GetAttribute('Key', $xamlNamespace) -eq 'HighContrast' })[0]
foreach ($resource in @($highContrast.ChildNodes | Where-Object { $_ -is [System.Xml.XmlElement] })) {
    if ($resource.OuterXml -notmatch 'SystemColor') {
        throw "EtherIntelligenceButton HighContrast resource '$($resource.GetAttribute('Key', $xamlNamespace))' does not use a Windows SystemColor dynamic resource."
    }
}

$gallery = Get-Content -LiteralPath $galleryPath -Raw
foreach ($name in 'Interactive intelligence button', 'Default intelligence button', 'Hover intelligence button', 'Pressed intelligence button', 'Disabled intelligence button') {
    Assert-Contains $gallery ([regex]::Escape($name)) "EtherIntelligenceButton gallery automation name '$name'"
}

$fixture = Get-Content -LiteralPath $fixturePath -Raw
foreach ($evidence in 'DefaultEtherIntelligenceButtonStyle', 'IntelligenceButtonVerification', 'intelligenceButton = result\?\.IntelligenceButton') {
    Assert-Contains $fixture $evidence "EtherIntelligenceButton consumer runtime evidence for $evidence"
}

$ci = Get-Content -LiteralPath $ciPath -Raw
Assert-Contains $ci 'Verify-EtherIntelligenceButtonContract\.ps1' 'CI wiring for EtherIntelligenceButton contract verifier'

Write-Host 'EtherIntelligenceButton contract audit passed: style/defaults, template metadata/states, component theme keys, High Contrast system resources, gallery automation names, and runtime evidence are present.'
