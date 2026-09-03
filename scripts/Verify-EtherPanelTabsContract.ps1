[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# EtherPanelTabs is a STYLE-ONLY re-skin (no EtherPanelTabs C# class): it REUSES the
# EtherSegmentedControl host + RadioButton segments and swaps only the skin, applied via
# Style="{StaticResource EtherPanelTabs}" / Style="{StaticResource EtherPanelTabSegment}".
# Its consumer contract is therefore: both keyed styles resolve to the right TargetType and
# carry a Template setter, the two ControlTemplates carry no literal colors, the nine themed
# brushes exist in Light/Dark/HighContrast with matching key sets, High Contrast uses Windows
# SystemColor dynamic resources, the dictionary is merged into Generic.xaml, a Gallery page
# demonstrates it, and the consumer runtime fixture proves it.
$repoRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$xamlPath = Join-Path $repoRoot 'src\Ether.DesignSystem.Controls\Controls\Inputs\EtherPanelTabs.xaml'
$genericPath = Join-Path $repoRoot 'src\Ether.DesignSystem.Controls\Themes\Generic.xaml'
$galleryPath = Join-Path $repoRoot 'samples\Ether.DesignSystem.Gallery\Views\Navigation\PanelTabsPage.xaml'
$fixtureDirectory = Join-Path $repoRoot 'tests\Ether.DesignSystem.ConsumerFixtures'
$ciPath = Join-Path $repoRoot '.github\workflows\build.yml'
$xamlNamespace = 'http://schemas.microsoft.com/winfx/2006/xaml'
$expectedKeys = @(
    'EtherPanelTabsFocusStrokeBrush',
    'EtherPanelTabsSegmentCheckedBrush',
    'EtherPanelTabsSegmentForegroundBrush',
    'EtherPanelTabsSegmentForegroundCheckedBrush',
    'EtherPanelTabsSegmentForegroundHoverBrush',
    'EtherPanelTabsSegmentForegroundPressedBrush',
    'EtherPanelTabsSegmentHoverBrush',
    'EtherPanelTabsSegmentPressedBrush',
    'EtherPanelTabsTrackBackgroundBrush'
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

[xml]$xaml = Get-Content -LiteralPath $xamlPath -Raw
$styles = @($xaml.SelectNodes("//*[local-name()='Style']"))

$panelTabsStyle = @($styles | Where-Object { $_.GetAttribute('Key', $xamlNamespace) -eq 'EtherPanelTabs' })[0]
if ($null -eq $panelTabsStyle) {
    throw 'EtherPanelTabs is missing its keyed EtherPanelTabs style.'
}
if ($panelTabsStyle.GetAttribute('TargetType') -notmatch 'EtherSegmentedControl') {
    throw 'EtherPanelTabs style TargetType must be EtherSegmentedControl (it re-skins the reused host).'
}

$panelTabSegmentStyle = @($styles | Where-Object { $_.GetAttribute('Key', $xamlNamespace) -eq 'EtherPanelTabSegment' })[0]
if ($null -eq $panelTabSegmentStyle) {
    throw 'EtherPanelTabs is missing its keyed EtherPanelTabSegment style.'
}
if ($panelTabSegmentStyle.GetAttribute('TargetType') -notmatch 'RadioButton') {
    throw 'EtherPanelTabSegment style TargetType must be RadioButton (the reused segment base type).'
}

foreach ($style in @(@{ Name = 'EtherPanelTabs'; Node = $panelTabsStyle }, @{ Name = 'EtherPanelTabSegment'; Node = $panelTabSegmentStyle })) {
    if ($null -eq $style.Node.SelectSingleNode("./*[local-name()='Setter' and @Property='Template']")) {
        throw "$($style.Name) style is missing its Template setter."
    }
}

$templates = @($xaml.SelectNodes("//*[local-name()='ControlTemplate']"))
if ($templates.Count -lt 2) {
    throw 'EtherPanelTabs is missing one of its two ControlTemplates (track + segment).'
}
foreach ($template in $templates) {
    if ($template.OuterXml -match '#[0-9A-Fa-f]{3,8}') {
        throw 'An EtherPanelTabs ControlTemplate contains a literal hex color; only component theme resources may carry color values.'
    }
    if ($template.OuterXml -match 'StaticResource\s+(Blue|Gray)\d') {
        throw 'An EtherPanelTabs ControlTemplate must not reference primitive color StaticResources; use component ThemeResources.'
    }
    foreach ($themeResource in @([regex]::Matches($template.OuterXml, '\{ThemeResource\s+([^}\s]+)') | ForEach-Object { $_.Groups[1].Value })) {
        if ($themeResource -notlike 'EtherPanelTabs*') {
            throw "EtherPanelTabs template references non-component ThemeResource '$themeResource'."
        }
    }
}

$themeDictionaries = @($xaml.SelectNodes("//*[local-name()='ResourceDictionary.ThemeDictionaries']/*[local-name()='ResourceDictionary']"))
$keysByTheme = @{}
foreach ($dictionary in $themeDictionaries) {
    $themeAttribute = $dictionary.GetAttributeNode('Key', $xamlNamespace)
    if ($null -eq $themeAttribute) {
        throw 'EtherPanelTabs has a theme dictionary without x:Key.'
    }

    $keys = Get-KeyedResources $dictionary
    if ($keys.Count -eq 0 -or @($keys | Where-Object { $_ -notlike 'EtherPanelTabs*' }).Count -gt 0) {
        throw "EtherPanelTabs $($themeAttribute.Value) resources must be component-scoped EtherPanelTabs keys."
    }
    $keysByTheme[$themeAttribute.Value] = $keys
}

foreach ($theme in 'Light', 'Dark', 'HighContrast') {
    if (-not $keysByTheme.ContainsKey($theme)) {
        throw "EtherPanelTabs is missing its $theme component theme dictionary."
    }
    if (($keysByTheme[$theme] -join ',') -cne ($keysByTheme.Light -join ',')) {
        throw "EtherPanelTabs $theme component resource keys do not match Light."
    }
}
if (($keysByTheme.Light -join ',') -cne (($expectedKeys | Sort-Object) -join ',')) {
    throw 'EtherPanelTabs component resource keys differ from the expected lightweight key set.'
}

$highContrast = @($themeDictionaries | Where-Object { $_.GetAttribute('Key', $xamlNamespace) -eq 'HighContrast' })[0]
foreach ($resource in @($highContrast.ChildNodes | Where-Object { $_ -is [System.Xml.XmlElement] })) {
    if ($resource.OuterXml -notmatch 'SystemColor') {
        throw "EtherPanelTabs HighContrast resource '$($resource.GetAttribute('Key', $xamlNamespace))' does not use a Windows SystemColor dynamic resource."
    }
}

$generic = Get-Content -LiteralPath $genericPath -Raw
Assert-Contains $generic 'Controls/Inputs/EtherPanelTabs\.xaml' 'Generic.xaml merge of EtherPanelTabs.xaml'

if (-not (Test-Path -LiteralPath $galleryPath -PathType Leaf)) {
    throw 'EtherPanelTabs is missing its Gallery page (samples/.../Views/Navigation/PanelTabsPage.xaml).'
}

$fixture = (Get-ChildItem -Path $fixtureDirectory -Filter 'RuntimeVerification*.cs' -File | Get-Content -Raw) -join [Environment]::NewLine
foreach ($evidence in 'EtherPanelTabs', 'PanelTabsStyleResolved') {
    Assert-Contains $fixture $evidence "EtherPanelTabs consumer runtime evidence for $evidence"
}

$ci = Get-Content -LiteralPath $ciPath -Raw
Assert-Contains $ci 'Verify-EtherPanelTabsContract\.ps1' 'CI wiring for EtherPanelTabs contract verifier'

Write-Host 'EtherPanelTabs contract audit passed: EtherPanelTabs (EtherSegmentedControl) + EtherPanelTabSegment (RadioButton) keyed styles with Template setters, color-free ControlTemplates, nine themed brushes with Light/Dark/HighContrast key parity, High Contrast system resources, Generic.xaml merge, Gallery page, and consumer runtime evidence are present.'
