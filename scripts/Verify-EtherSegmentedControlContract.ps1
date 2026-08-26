[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$controlPath = Join-Path $repoRoot 'src\Controls\Inputs\EtherSegmentedControl.cs'
$xamlPath = Join-Path $repoRoot 'src\Controls\Inputs\EtherSegmentedControl.xaml'
$fixturePath = Join-Path $repoRoot 'tests\Ether.DesignSystem.ConsumerFixtures\RuntimeVerification.cs'
$ciPath = Join-Path $repoRoot '.github\workflows\build.yml'
$xamlNamespace = 'http://schemas.microsoft.com/winfx/2006/xaml'
$expectedComponentKeys = 'EtherSegmentedControlFocusStrokeBrush,EtherSegmentedControlSegmentCheckedBrush,EtherSegmentedControlSegmentForegroundBrush,EtherSegmentedControlSegmentForegroundCheckedBrush,EtherSegmentedControlSegmentHoverBrush,EtherSegmentedControlSegmentPressedBrush,EtherSegmentedControlTrackBackgroundBrush'
$templateParts = @('ShadowHost', 'TrackSurface')
$segmentCommonStates = @('Normal', 'PointerOver', 'Pressed', 'Disabled', 'Checked', 'CheckedPointerOver', 'CheckedPressed', 'Indeterminate')
$segmentFocusStates = @('Focused', 'Unfocused', 'PointerFocused')

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
Assert-Contains $control 'DefaultStyleKey\s*=\s*typeof\(EtherSegmentedControl\)' 'EtherSegmentedControl constructor'
if ($control -notmatch '(?s)public EtherSegmentedControl\(\)\s*\{(?<ctor>.*?)\n    \}') {
    throw 'EtherSegmentedControl constructor body could not be isolated for default-setter audit.'
}
if ($Matches['ctor'] -match '(Padding|Background|HorizontalContentAlignment|VerticalContentAlignment|Template)\s*=') {
    throw 'EtherSegmentedControl constructor must not assign its default visual setters; the keyed style owns them.'
}
foreach ($part in $templateParts) {
    Assert-Contains $control "TemplatePart\(Name = .*${part}" "EtherSegmentedControl TemplatePart contract for $part"
}
Assert-Contains $control 'GetTemplateChild\(ShadowHostPartName\)' 'EtherSegmentedControl composition shadow host lookup'
Assert-Contains $control 'GetTemplateChild\(TrackSurfacePartName\)' 'EtherSegmentedControl composition track surface lookup'
Assert-Contains $control 'CreateDropShadow' 'EtherSegmentedControl composition drop-shadow layers'
if ($control -match 'TemplateVisualState') {
    throw 'EtherSegmentedControl host template has no VisualState groups; do not declare TemplateVisualState metadata on the host.'
}

[xml]$xaml = Get-Content -LiteralPath $xamlPath -Raw
$styles = @($xaml.SelectNodes("//*[local-name()='Style']"))
$keyedStyle = @($styles | Where-Object { $_.GetAttribute('Key', $xamlNamespace) -eq 'DefaultEtherSegmentedControlStyle' })[0]
if ($null -eq $keyedStyle) {
    throw 'EtherSegmentedControl is missing keyed DefaultEtherSegmentedControlStyle.'
}
$implicitStyle = @($styles | Where-Object {
    -not $_.HasAttribute('Key', $xamlNamespace) -and $_.GetAttribute('BasedOn') -match 'DefaultEtherSegmentedControlStyle'
})[0]
if ($null -eq $implicitStyle) {
    throw 'EtherSegmentedControl is missing its implicit style BasedOn DefaultEtherSegmentedControlStyle.'
}
$trackAlias = @($styles | Where-Object { $_.GetAttribute('Key', $xamlNamespace) -eq 'EtherSegmentedTrack' })[0]
if ($null -eq $trackAlias -or $trackAlias.GetAttribute('BasedOn') -notmatch 'DefaultEtherSegmentedControlStyle') {
    throw 'EtherSegmentedControl is missing its keyed EtherSegmentedTrack alias BasedOn DefaultEtherSegmentedControlStyle.'
}
$segmentStyle = @($styles | Where-Object { $_.GetAttribute('Key', $xamlNamespace) -eq 'EtherSegment' })[0]
if ($null -eq $segmentStyle) {
    throw 'EtherSegmentedControl is missing keyed EtherSegment RadioButton style.'
}
foreach ($setter in 'Padding', 'Background', 'HorizontalContentAlignment', 'VerticalContentAlignment', 'Template') {
    if ($null -eq $keyedStyle.SelectSingleNode("./*[local-name()='Setter' and @Property='$setter']")) {
        throw "DefaultEtherSegmentedControlStyle is missing its $setter setter."
    }
}

$templates = @($xaml.SelectNodes("//*[local-name()='ControlTemplate']"))
if ($templates.Count -lt 2) {
    throw 'EtherSegmentedControl is missing its track and segment ControlTemplates.'
}
$trackTemplate = @($templates | Where-Object { $_.GetAttribute('Key', $xamlNamespace) -eq 'EtherSegmentedTrackTemplate' })[0]
if ($null -eq $trackTemplate) {
    throw 'EtherSegmentedControl is missing keyed EtherSegmentedTrackTemplate.'
}
$segmentTemplate = @($templates | Where-Object { $_.GetAttribute('Key', $xamlNamespace) -eq 'EtherSegmentTemplate' })[0]
if ($null -eq $segmentTemplate) {
    throw 'EtherSegmentedControl is missing keyed EtherSegmentTemplate.'
}

foreach ($part in $templateParts) {
    if (@($trackTemplate.SelectNodes(".//*[@Name='$part' or @*[local-name()='Name']='$part']")).Count -lt 1) {
        throw "EtherSegmentedTrackTemplate is missing named part $part."
    }
}

foreach ($template in $templates) {
    if ($template.OuterXml -match '#[0-9A-Fa-f]{3,8}') {
        throw 'EtherSegmentedControl ControlTemplate contains a literal hex color; only component resources may carry color values.'
    }
    if ($template.OuterXml -match '(Background|BorderBrush|Foreground|Stroke|Fill)="Transparent"') {
        throw 'EtherSegmentedControl ControlTemplate contains a literal Transparent color; only component resources may carry color values.'
    }
    foreach ($themeResource in @([regex]::Matches($template.OuterXml, '\{ThemeResource\s+([^}\s]+)') | ForEach-Object { $_.Groups[1].Value })) {
        if ($themeResource -notlike 'EtherSegmentedControl*') {
            throw "EtherSegmentedControl template references non-component ThemeResource '$themeResource'."
        }
    }
}

foreach ($state in $segmentCommonStates) {
    if (@($segmentTemplate.SelectNodes(".//*[local-name()='VisualState']") | Where-Object {
        $_.GetAttribute('Name', $xamlNamespace) -eq $state
    }).Count -ne 1) {
        throw "EtherSegmentTemplate is missing CommonStates/$state."
    }
}
foreach ($state in $segmentFocusStates) {
    if (@($segmentTemplate.SelectNodes(".//*[local-name()='VisualState']") | Where-Object {
        $_.GetAttribute('Name', $xamlNamespace) -eq $state
    }).Count -ne 1) {
        throw "EtherSegmentTemplate is missing FocusStates/$state."
    }
}

$themeDictionaries = @($xaml.SelectNodes("//*[local-name()='ResourceDictionary.ThemeDictionaries']/*[local-name()='ResourceDictionary']"))
$keysByTheme = @{}
foreach ($dictionary in $themeDictionaries) {
    $themeAttribute = $dictionary.GetAttributeNode('Key', $xamlNamespace)
    if ($null -eq $themeAttribute) {
        throw 'EtherSegmentedControl has a theme dictionary without x:Key.'
    }

    $keys = Get-KeyedResources $dictionary
    if ($keys.Count -eq 0 -or @($keys | Where-Object { $_ -notlike 'EtherSegmentedControl*' }).Count -gt 0) {
        throw "EtherSegmentedControl $($themeAttribute.Value) resources must be component-scoped EtherSegmentedControl keys."
    }
    $keysByTheme[$themeAttribute.Value] = $keys
}

foreach ($theme in 'Light', 'Dark', 'HighContrast') {
    if (-not $keysByTheme.ContainsKey($theme)) {
        throw "EtherSegmentedControl is missing its $theme component theme dictionary."
    }
    if (($keysByTheme[$theme] -join ',') -cne ($keysByTheme.Light -join ',')) {
        throw "EtherSegmentedControl $theme component resource keys do not match Light."
    }
}
if (($keysByTheme.Light -join ',') -cne $expectedComponentKeys) {
    throw 'EtherSegmentedControl component resource keys differ from the expected lightweight key set.'
}

$highContrast = @($themeDictionaries | Where-Object { $_.GetAttribute('Key', $xamlNamespace) -eq 'HighContrast' })[0]
foreach ($resource in @($highContrast.ChildNodes | Where-Object { $_ -is [System.Xml.XmlElement] })) {
    if ($resource.OuterXml -notmatch 'SystemColor') {
        throw "EtherSegmentedControl HighContrast resource '$($resource.GetAttribute('Key', $xamlNamespace))' does not use a Windows SystemColor dynamic resource."
    }
}

$fixture = Get-Content -LiteralPath $fixturePath -Raw
foreach ($evidence in 'DefaultEtherSegmentedControlStyle', 'ShadowHost', 'TrackSurface', 'SegmentedControlVerification', 'segmentedControl = result\?\.SegmentedControl') {
    Assert-Contains $fixture $evidence "EtherSegmentedControl consumer runtime evidence for $evidence"
}

$ci = Get-Content -LiteralPath $ciPath -Raw
Assert-Contains $ci 'Verify-EtherSegmentedControlContract\.ps1' 'CI wiring for EtherSegmentedControl contract verifier'

Write-Host 'EtherSegmentedControl contract audit passed: style/defaults, track/segment keys, template parts, component theme keys, High Contrast system resources, and runtime evidence are present.'
