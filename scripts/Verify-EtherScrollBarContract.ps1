[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$controlPath = Join-Path $repoRoot 'src\Ether.DesignSystem.Controls\Controls\Inputs\EtherScrollBar.xaml.cs'
$xamlPath = Join-Path $repoRoot 'src\Ether.DesignSystem.Controls\Controls\Inputs\EtherScrollBar.xaml'
$galleryPath = Join-Path $repoRoot 'samples\Ether.DesignSystem.Gallery\Views\Foundations\ScrollBarPage.xaml'
$fixtureDirectory = Join-Path $repoRoot 'tests\Ether.DesignSystem.ConsumerFixtures'
$ciPath = Join-Path $repoRoot '.github\workflows\build.yml'
$xamlNamespace = 'http://schemas.microsoft.com/winfx/2006/xaml'
$expectedComponentKeys = 'EtherScrollBarThumbBrush,EtherScrollBarThumbHoverBrush'

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
Assert-Contains $control 'class EtherScrollBarResources\s*:\s*ResourceDictionary' 'EtherScrollBar resources ResourceDictionary contract'
if ($control -match 'DefaultStyleKey') {
    throw 'EtherScrollBar must stay an implicit ResourceDictionary style contract; do not introduce DefaultStyleKey.'
}
if ($control -match 'class EtherScrollBarResources\s*:\s*(Control|ScrollBar)') {
    throw 'EtherScrollBar must not become a custom ScrollBar subclass.'
}

[xml]$xaml = Get-Content -LiteralPath $xamlPath -Raw
$xamlRaw = Get-Content -LiteralPath $xamlPath -Raw
$styles = @($xaml.SelectNodes("//*[local-name()='Style']"))
$implicitStyle = @($styles | Where-Object {
    -not $_.HasAttribute('Key', $xamlNamespace) -and $_.GetAttribute('TargetType') -eq 'ScrollBar'
})[0]
if ($null -eq $implicitStyle) {
    throw 'EtherScrollBar is missing its implicit Style TargetType="ScrollBar".'
}
$keyedScrollBar = @($styles | Where-Object {
    $_.HasAttribute('Key', $xamlNamespace) -and $_.GetAttribute('TargetType') -eq 'ScrollBar'
})
if ($keyedScrollBar.Count -ne 0) {
    throw 'EtherScrollBar must stay implicit; a keyed ScrollBar style would drop app-wide ScrollViewer coverage.'
}
foreach ($setter in 'Background', 'IsTabStop', 'UseSystemFocusVisuals', 'HighContrastAdjustment', 'Template') {
    if ($null -eq $implicitStyle.SelectSingleNode("./*[local-name()='Setter' and @Property='$setter']")) {
        throw "Implicit ScrollBar style is missing its $setter setter."
    }
}

$rootTransparent = @($xaml.DocumentElement.ChildNodes | Where-Object {
    $_ -is [System.Xml.XmlElement] -and
    $_.LocalName -eq 'SolidColorBrush' -and
    $_.GetAttribute('Key', $xamlNamespace) -eq 'EtherScrollBarTransparentBrush'
})[0]
if ($null -eq $rootTransparent) {
    throw 'EtherScrollBar is missing its unthemed EtherScrollBarTransparentBrush for structural overlay fills.'
}

$templates = @($xaml.SelectNodes("//*[local-name()='ControlTemplate']"))
if ($templates.Count -lt 3) {
    throw 'EtherScrollBar is missing its RepeatButton, Thumb, or ScrollBar ControlTemplate.'
}
$repeatTemplate = @($templates | Where-Object { $_.GetAttribute('Key', $xamlNamespace) -eq 'EtherScrollBarRepeatButtonTemplate' })[0]
$thumbTemplate = @($templates | Where-Object { $_.GetAttribute('Key', $xamlNamespace) -eq 'EtherScrollBarThumbTemplate' })[0]
if ($null -eq $repeatTemplate -or $null -eq $thumbTemplate) {
    throw 'EtherScrollBar is missing keyed RepeatButton or Thumb templates.'
}

$scrollBarTemplate = $implicitStyle.SelectSingleNode(".//*[local-name()='ControlTemplate']")
if ($null -eq $scrollBarTemplate) {
    throw 'Implicit ScrollBar style is missing its ControlTemplate.'
}
foreach ($part in 'VerticalRoot', 'HorizontalRoot', 'VerticalThumb', 'HorizontalThumb', 'VerticalSmallDecrease', 'VerticalSmallIncrease', 'VerticalLargeDecrease', 'VerticalLargeIncrease', 'HorizontalSmallDecrease', 'HorizontalSmallIncrease', 'HorizontalLargeDecrease', 'HorizontalLargeIncrease') {
    if ($scrollBarTemplate.OuterXml -notmatch "x:Name=`"$part`"") {
        throw "EtherScrollBar template is missing named part '$part'."
    }
}

foreach ($template in $templates) {
    if ($template.OuterXml -match '#[0-9A-Fa-f]{3,8}') {
        throw 'EtherScrollBar ControlTemplate contains a literal hex color; only component resources may carry color values.'
    }
    if ($template.OuterXml -match '(Background|BorderBrush|Foreground|Stroke|Fill)="Transparent"') {
        throw 'EtherScrollBar ControlTemplate contains a literal Transparent color; only component resources may carry color values.'
    }
    foreach ($themeResource in @([regex]::Matches($template.OuterXml, '\{ThemeResource\s+([^}\s]+)') | ForEach-Object { $_.Groups[1].Value })) {
        if ($themeResource -notlike 'EtherScrollBar*') {
            throw "EtherScrollBar template references non-component ThemeResource '$themeResource'."
        }
    }
}

Assert-Contains $xamlRaw 'Color="\{StaticResource Gray400\}"' 'EtherScrollBar Default fill binds Gray400'
Assert-Contains $xamlRaw 'Color="\{StaticResource Gray500\}"' 'EtherScrollBar Hover fill binds Gray500'
Assert-Contains $xamlRaw 'CornerRadius="3"' 'EtherScrollBar 6px thumb uses geometric radius 3'
if ($xamlRaw -match 'BackgroundDropdownScrollThumb') {
    throw 'EtherScrollBar Light/Dark fills must bind primitive Gray400/Gray500, not dropdown aliases.'
}
if ($xamlRaw -match 'Spacing6') {
    throw 'EtherScrollBar thickness must be a literal 6, not Spacing6.'
}

$themeDictionaries = @($xaml.SelectNodes("//*[local-name()='ResourceDictionary.ThemeDictionaries']/*[local-name()='ResourceDictionary']"))
$keysByTheme = @{}
foreach ($dictionary in $themeDictionaries) {
    $themeAttribute = $dictionary.GetAttributeNode('Key', $xamlNamespace)
    if ($null -eq $themeAttribute) {
        throw 'EtherScrollBar has a theme dictionary without x:Key.'
    }

    $keys = Get-KeyedResources $dictionary
    if ($keys.Count -eq 0 -or @($keys | Where-Object { $_ -notlike 'EtherScrollBar*' }).Count -gt 0) {
        throw "EtherScrollBar $($themeAttribute.Value) resources must be component-scoped EtherScrollBar keys."
    }
    $keysByTheme[$themeAttribute.Value] = $keys
}

foreach ($theme in 'Light', 'Dark', 'HighContrast') {
    if (-not $keysByTheme.ContainsKey($theme)) {
        throw "EtherScrollBar is missing its $theme component theme dictionary."
    }
    if (($keysByTheme[$theme] -join ',') -cne ($keysByTheme.Light -join ',')) {
        throw "EtherScrollBar $theme component resource keys do not match Light."
    }
}
if (($keysByTheme.Light -join ',') -cne $expectedComponentKeys) {
    throw 'EtherScrollBar component resource keys differ from the expected lightweight key set.'
}

$highContrast = @($themeDictionaries | Where-Object { $_.GetAttribute('Key', $xamlNamespace) -eq 'HighContrast' })[0]
foreach ($resource in @($highContrast.ChildNodes | Where-Object { $_ -is [System.Xml.XmlElement] })) {
    if ($resource.OuterXml -notmatch 'SystemColor') {
        throw "EtherScrollBar HighContrast resource '$($resource.GetAttribute('Key', $xamlNamespace))' does not use a Windows SystemColor dynamic resource."
    }
}

$gallery = Get-Content -LiteralPath $galleryPath -Raw
foreach ($name in 'Vertical scroll viewer', 'Horizontal scroll viewer') {
    Assert-Contains $gallery ([regex]::Escape($name)) "EtherScrollBar gallery automation name '$name'"
}

$fixture = (Get-ChildItem -Path $fixtureDirectory -Filter 'RuntimeVerification*.cs' -File | Get-Content -Raw) -join [Environment]::NewLine
foreach ($evidence in 'ScrollBarVerification', 'scrollBar = result\?\.ScrollBar', 'ImplicitStyleApplied', 'VerticalRootPresent', 'HorizontalRootPresent') {
    Assert-Contains $fixture $evidence "EtherScrollBar consumer runtime evidence for $evidence"
}

$ci = Get-Content -LiteralPath $ciPath -Raw
Assert-Contains $ci 'Verify-EtherScrollBarContract\.ps1' 'CI wiring for EtherScrollBar contract verifier'

Write-Host 'EtherScrollBar contract audit passed: implicit ResourceDictionary style, vertical and horizontal templates, component theme keys, High Contrast system resources, gallery automation names, consumer evidence, and CI wiring are present.'
