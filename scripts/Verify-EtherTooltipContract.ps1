[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# EtherTooltip is a STYLE-ONLY component (a keyed Style applied on top of Border via
# Style="{StaticResource EtherTooltip}"; there is no EtherTooltip C# class). Its consumer
# contract is therefore: the keyed style resolves as a Border style with the expected setters,
# the three themed brushes exist in Light/Dark/HighContrast with matching key sets, High Contrast
# uses Windows SystemColor dynamic resources, the dictionary is merged into Generic.xaml, a Gallery
# page demonstrates it, and the consumer runtime fixture proves it.
$repoRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$xamlPath = Join-Path $repoRoot 'src\Ether.DesignSystem.Controls\Resources\Foundations\EtherTooltip.xaml'
$genericPath = Join-Path $repoRoot 'src\Ether.DesignSystem.Controls\Themes\Generic.xaml'
$galleryPath = Join-Path $repoRoot 'samples\Ether.DesignSystem.Gallery\Views\Surfaces\TooltipPage.xaml'
$fixtureDirectory = Join-Path $repoRoot 'tests\Ether.DesignSystem.ConsumerFixtures'
$ciPath = Join-Path $repoRoot '.github\workflows\build.yml'
$xamlNamespace = 'http://schemas.microsoft.com/winfx/2006/xaml'
$expectedKeys = @(
    'EtherTooltipBackgroundBrush',
    'EtherTooltipForegroundBrush',
    'EtherTooltipStrokeBrush'
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
$tooltipStyle = @($styles | Where-Object { $_.GetAttribute('Key', $xamlNamespace) -eq 'EtherTooltip' })[0]
if ($null -eq $tooltipStyle) {
    throw 'EtherTooltip is missing its keyed EtherTooltip style.'
}
if ($tooltipStyle.GetAttribute('TargetType') -notmatch 'Border') {
    throw 'EtherTooltip style TargetType must be Border.'
}
foreach ($setter in 'Background', 'BorderBrush', 'BorderThickness', 'CornerRadius', 'Padding') {
    if ($null -eq $tooltipStyle.SelectSingleNode("./*[local-name()='Setter' and @Property='$setter']")) {
        throw "EtherTooltip style is missing its $setter setter."
    }
}
if ($tooltipStyle.OuterXml -match '#[0-9A-Fa-f]{3,8}') {
    throw 'EtherTooltip style contains a literal hex color; only component theme resources may carry color values.'
}
foreach ($themeResource in @([regex]::Matches($tooltipStyle.OuterXml, '\{ThemeResource\s+([^}\s]+)') | ForEach-Object { $_.Groups[1].Value })) {
    if ($themeResource -notlike 'EtherTooltip*') {
        throw "EtherTooltip style references non-component ThemeResource '$themeResource'."
    }
}

$themeDictionaries = @($xaml.SelectNodes("//*[local-name()='ResourceDictionary.ThemeDictionaries']/*[local-name()='ResourceDictionary']"))
$keysByTheme = @{}
foreach ($dictionary in $themeDictionaries) {
    $themeAttribute = $dictionary.GetAttributeNode('Key', $xamlNamespace)
    if ($null -eq $themeAttribute) {
        throw 'EtherTooltip has a theme dictionary without x:Key.'
    }

    $keys = Get-KeyedResources $dictionary
    if ($keys.Count -eq 0 -or @($keys | Where-Object { $_ -notlike 'EtherTooltip*' }).Count -gt 0) {
        throw "EtherTooltip $($themeAttribute.Value) resources must be component-scoped EtherTooltip keys."
    }
    $keysByTheme[$themeAttribute.Value] = $keys
}

foreach ($theme in 'Light', 'Dark', 'HighContrast') {
    if (-not $keysByTheme.ContainsKey($theme)) {
        throw "EtherTooltip is missing its $theme component theme dictionary."
    }
    if (($keysByTheme[$theme] -join ',') -cne ($keysByTheme.Light -join ',')) {
        throw "EtherTooltip $theme component resource keys do not match Light."
    }
}
if (($keysByTheme.Light -join ',') -cne (($expectedKeys | Sort-Object) -join ',')) {
    throw 'EtherTooltip component resource keys differ from the expected lightweight key set.'
}

$highContrast = @($themeDictionaries | Where-Object { $_.GetAttribute('Key', $xamlNamespace) -eq 'HighContrast' })[0]
foreach ($resource in @($highContrast.ChildNodes | Where-Object { $_ -is [System.Xml.XmlElement] })) {
    if ($resource.OuterXml -notmatch 'SystemColor') {
        throw "EtherTooltip HighContrast resource '$($resource.GetAttribute('Key', $xamlNamespace))' does not use a Windows SystemColor dynamic resource."
    }
}

$generic = Get-Content -LiteralPath $genericPath -Raw
Assert-Contains $generic 'Resources/Foundations/EtherTooltip\.xaml' 'Generic.xaml merge of EtherTooltip.xaml'

if (-not (Test-Path -LiteralPath $galleryPath -PathType Leaf)) {
    throw 'EtherTooltip is missing its Gallery page (samples/.../Views/Surfaces/TooltipPage.xaml).'
}

$fixture = (Get-ChildItem -Path $fixtureDirectory -Filter 'RuntimeVerification*.cs' -File | Get-Content -Raw) -join [Environment]::NewLine
foreach ($evidence in 'EtherTooltip', 'TooltipStyleResolved') {
    Assert-Contains $fixture $evidence "EtherTooltip consumer runtime evidence for $evidence"
}

$ci = Get-Content -LiteralPath $ciPath -Raw
Assert-Contains $ci 'Verify-EtherTooltipContract\.ps1' 'CI wiring for EtherTooltip contract verifier'

Write-Host 'EtherTooltip contract audit passed: Border-targeted keyed style, themed Background/Border brushes, Light/Dark/HighContrast key parity, High Contrast system resources, Generic.xaml merge, Gallery page, and consumer runtime evidence are present.'
