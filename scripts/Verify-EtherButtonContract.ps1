[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$controlPath = Join-Path $repoRoot 'src\Ether.DesignSystem.Controls\Controls\Inputs\EtherButton.cs'
$xamlPath = Join-Path $repoRoot 'src\Ether.DesignSystem.Controls\Controls\Inputs\EtherButton.xaml'
$fixtureDirectory = Join-Path $repoRoot 'tests\Ether.DesignSystem.ConsumerFixtures'
$ciPath = Join-Path $repoRoot '.github\workflows\build.yml'
$xamlNamespace = 'http://schemas.microsoft.com/winfx/2006/xaml'
$expectedComponentKeys = 'EtherButtonFocusStrokeBrush,EtherButtonPrimaryBackgroundBrush,EtherButtonPrimaryBackgroundDisabledBrush,EtherButtonPrimaryBackgroundHoverBrush,EtherButtonPrimaryBackgroundPressedBrush,EtherButtonPrimaryBorderBrush,EtherButtonPrimaryBorderDisabledBrush,EtherButtonPrimaryForegroundBrush,EtherButtonPrimaryForegroundDisabledBrush,EtherButtonSecondaryBackgroundBrush,EtherButtonSecondaryBackgroundHoverBrush,EtherButtonSecondaryBackgroundPressedBrush,EtherButtonSecondaryBorderBrush,EtherButtonSecondaryForegroundBrush,EtherButtonTertiaryBackgroundBrush,EtherButtonTertiaryForegroundBrush,EtherButtonTertiaryForegroundHoverBrush,EtherButtonTertiaryForegroundPressedBrush'
$namedStyles = @(
    'EtherButtonPrimary',
    'EtherButtonPrimarySmall',
    'EtherButtonSecondary',
    'EtherButtonSecondarySmall',
    'EtherButtonTertiary',
    'EtherButtonTertiarySmall'
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
Assert-Contains $control 'DefaultStyleKey\s*=\s*typeof\(EtherButton\)' 'EtherButton constructor'
if ($control -match '(?m)^\s*(MinHeight|Padding|FontSize)\s*=') {
    throw 'EtherButton constructor must not assign its default visual setters; the keyed style owns them.'
}
Assert-Contains $control "TemplatePart\(Name = .*RightIcon" 'EtherButton TemplatePart contract for RightIcon'
Assert-Contains $control "TemplatePart\(Name = .*Cp" 'EtherButton TemplatePart contract for Cp'
foreach ($state in 'Normal', 'PointerOver', 'Pressed', 'Disabled') {
    Assert-Contains $control "TemplateVisualState\(GroupName = CommonStatesGroup, Name = ${state}State\)" "EtherButton TemplateVisualState contract for CommonStates/$state"
}
foreach ($state in 'Focused', 'Unfocused', 'PointerFocused') {
    Assert-Contains $control "TemplateVisualState\(GroupName = FocusStatesGroup, Name = ${state}State\)" "EtherButton TemplateVisualState contract for FocusStates/$state"
}
foreach ($state in 'RightIconVisible', 'RightIconCollapsed') {
    Assert-Contains $control "TemplateVisualState\(GroupName = RightIconStatesGroup, Name = ${state}State\)" "EtherButton TemplateVisualState contract for RightIconStates/$state"
}
Assert-Contains $control 'VisualStateManager\.GoToState\(this, state, false\)' 'EtherButton RightIcon state transition'
if ($control -match '\.Visibility\s*=') {
    throw 'EtherButton must drive RightIcon visibility with VisualStateManager, not direct Visibility assignments.'
}
if ($control -match 'RightIconVisibility') {
    throw 'EtherButton must not keep a code-driven RightIconVisibility dependency property; RightIconStates owns the slot.'
}

[xml]$xaml = Get-Content -LiteralPath $xamlPath -Raw
$styles = @($xaml.SelectNodes("//*[local-name()='Style']"))
$keyedStyle = @($styles | Where-Object { $_.GetAttribute('Key', $xamlNamespace) -eq 'DefaultEtherButtonStyle' })[0]
if ($null -eq $keyedStyle) {
    throw 'EtherButton is missing keyed DefaultEtherButtonStyle.'
}
$implicitStyle = @($styles | Where-Object {
    -not $_.HasAttribute('Key', $xamlNamespace) -and $_.GetAttribute('BasedOn') -match 'DefaultEtherButtonStyle'
})[0]
if ($null -eq $implicitStyle) {
    throw 'EtherButton is missing its implicit style BasedOn DefaultEtherButtonStyle.'
}
foreach ($setter in 'MinHeight', 'Padding', 'FontSize', 'HorizontalContentAlignment', 'VerticalContentAlignment', 'UseSystemFocusVisuals', 'HighContrastAdjustment', 'Template') {
    if ($null -eq $keyedStyle.SelectSingleNode("./*[local-name()='Setter' and @Property='$setter']")) {
        throw "DefaultEtherButtonStyle is missing its $setter setter."
    }
}

$styleKeys = @($styles | ForEach-Object { $_.GetAttribute('Key', $xamlNamespace) } | Where-Object { $_ })
foreach ($namedStyle in $namedStyles) {
    if ($namedStyle -cnotin $styleKeys) {
        throw "EtherButton is missing named style '$namedStyle'."
    }
}

$templates = @($xaml.SelectNodes("//*[local-name()='ControlTemplate']"))
if ($templates.Count -lt 3) {
    throw 'EtherButton must keep its Primary, Secondary, and Tertiary control templates.'
}
$xamlText = Get-Content -LiteralPath $xamlPath -Raw
if ($xamlText -match 'FontFamily="Inter"') {
    throw 'EtherButton templates must use {StaticResource InterFont}, not the system family name Inter.'
}
Assert-Contains $xamlText 'FontFamily="\{StaticResource InterFont\}"' 'EtherButton primary/secondary templates use packaged InterFont'
foreach ($template in $templates) {
    if ($template.OuterXml -match '#[0-9A-Fa-f]{3,8}') {
        throw 'EtherButton ControlTemplate contains a literal hex color; only component resources may carry color values.'
    }
    if ($template.OuterXml -match 'StaticResource\s+(Blue|Gray)\d') {
        throw 'EtherButton ControlTemplate must not reference primitive color StaticResources; use component ThemeResources.'
    }
    if ($template.OuterXml -match '(Background|BorderBrush|Foreground|Value)="Transparent"') {
        throw 'EtherButton ControlTemplate contains a literal Transparent color; only component resources may carry color values.'
    }
    foreach ($themeResource in @([regex]::Matches($template.OuterXml, '\{ThemeResource\s+([^}\s]+)') | ForEach-Object { $_.Groups[1].Value })) {
        if ($themeResource -notlike 'EtherButton*') {
            throw "EtherButton template references non-component ThemeResource '$themeResource'."
        }
    }
    foreach ($state in 'Normal', 'PointerOver', 'Pressed', 'Disabled') {
        if (@($template.SelectNodes(".//*[local-name()='VisualState']") | Where-Object {
            $_.GetAttribute('Name', $xamlNamespace) -eq $state
        }).Count -ne 1) {
            throw "EtherButton template is missing CommonStates/$state."
        }
    }
    foreach ($state in 'Focused', 'Unfocused', 'PointerFocused') {
        if (@($template.SelectNodes(".//*[local-name()='VisualState']") | Where-Object {
            $_.GetAttribute('Name', $xamlNamespace) -eq $state
        }).Count -ne 1) {
            throw "EtherButton template is missing FocusStates/$state."
        }
    }
    foreach ($state in 'RightIconVisible', 'RightIconCollapsed') {
        if (@($template.SelectNodes(".//*[local-name()='VisualState']") | Where-Object {
            $_.GetAttribute('Name', $xamlNamespace) -eq $state
        }).Count -ne 1) {
            throw "EtherButton template is missing RightIconStates/$state."
        }
    }
}

$themeDictionaries = @($xaml.SelectNodes("//*[local-name()='ResourceDictionary.ThemeDictionaries']/*[local-name()='ResourceDictionary']"))
$keysByTheme = @{}
foreach ($dictionary in $themeDictionaries) {
    $themeAttribute = $dictionary.GetAttributeNode('Key', $xamlNamespace)
    if ($null -eq $themeAttribute) {
        throw 'EtherButton has a theme dictionary without x:Key.'
    }

    $keys = Get-KeyedResources $dictionary
    if ($keys.Count -eq 0 -or @($keys | Where-Object { $_ -notlike 'EtherButton*' }).Count -gt 0) {
        throw "EtherButton $($themeAttribute.Value) resources must be component-scoped EtherButton keys."
    }
    $keysByTheme[$themeAttribute.Value] = $keys
}

foreach ($theme in 'Light', 'Dark', 'HighContrast') {
    if (-not $keysByTheme.ContainsKey($theme)) {
        throw "EtherButton is missing its $theme component theme dictionary."
    }
    if (($keysByTheme[$theme] -join ',') -cne ($keysByTheme.Light -join ',')) {
        throw "EtherButton $theme component resource keys do not match Light."
    }
}
if (($keysByTheme.Light -join ',') -cne $expectedComponentKeys) {
    throw 'EtherButton component resource keys differ from the expected lightweight key set.'
}

$highContrast = @($themeDictionaries | Where-Object { $_.GetAttribute('Key', $xamlNamespace) -eq 'HighContrast' })[0]
foreach ($resource in @($highContrast.ChildNodes | Where-Object { $_ -is [System.Xml.XmlElement] })) {
    if ($resource.OuterXml -notmatch 'SystemColor') {
        throw "EtherButton HighContrast resource '$($resource.GetAttribute('Key', $xamlNamespace))' does not use a Windows SystemColor dynamic resource."
    }
}

$fixture = (Get-ChildItem -Path $fixtureDirectory -Filter 'RuntimeVerification*.cs' -File | Get-Content -Raw) -join [Environment]::NewLine
foreach ($evidence in 'DefaultEtherButtonStyle', 'RightIconVisible', 'RightIconCollapsed', 'ButtonVerification', 'button = result\?\.Button') {
    Assert-Contains $fixture $evidence "EtherButton consumer runtime evidence for $evidence"
}

$ci = Get-Content -LiteralPath $ciPath -Raw
Assert-Contains $ci 'Verify-EtherButtonContract\.ps1' 'CI wiring for EtherButton contract verifier'

Write-Host 'EtherButton contract audit passed: style/defaults, named styles, template metadata/states, component theme keys, High Contrast system resources, and runtime evidence are present.'
