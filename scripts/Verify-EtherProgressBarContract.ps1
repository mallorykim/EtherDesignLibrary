[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$controlPath = Join-Path $repoRoot 'src\Controls\Inputs\EtherProgressBar.cs'
$xamlPath = Join-Path $repoRoot 'src\Controls\Inputs\EtherProgressBar.xaml'
$fixturePath = Join-Path $repoRoot 'tests\Ether.DesignSystem.ConsumerFixtures\RuntimeVerification.cs'
$xamlNamespace = 'http://schemas.microsoft.com/winfx/2006/xaml'

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
Assert-Contains $control 'DefaultStyleKey\s*=\s*typeof\(EtherProgressBar\)' 'EtherProgressBar constructor'
if ($control -match '(?m)^\s*(Minimum|Maximum|Value)\s*=') {
    throw 'EtherProgressBar constructor must not assign its range defaults; the keyed style owns them.'
}

foreach ($part in 'FillColumn', 'RestColumn', 'LabelRow', 'TitleText', 'ValueLabel') {
    Assert-Contains $control "TemplatePart\(Name = .*${part}" "EtherProgressBar TemplatePart contract for $part"
}
foreach ($state in 'BothLabelsVisible', 'TitleOnly', 'ValueOnly', 'LabelsHidden') {
    Assert-Contains $control "TemplateVisualState\(GroupName = LabelStatesGroup, Name = ${state}State\)" "EtherProgressBar TemplateVisualState contract for $state"
}
Assert-Contains $control 'VisualStateManager\.GoToState\(this, state, false\)' 'EtherProgressBar label state transition'
if ($control -match '\.Visibility\s*=') {
    throw 'EtherProgressBar must drive label visibility with VisualStateManager, not direct Visibility assignments.'
}
Assert-Contains $control 'AutomationControlType\.ProgressBar' 'EtherProgressBar automation control type'
Assert-Contains $control 'public bool IsReadOnly => true' 'EtherProgressBar read-only RangeValue provider'
Assert-Contains $control 'public double SmallChange => double\.NaN' 'EtherProgressBar read-only small change'
Assert-Contains $control 'public double LargeChange => double\.NaN' 'EtherProgressBar read-only large change'
Assert-Contains $control 'RangeValuePatternIdentifiers\.ValueProperty' 'EtherProgressBar RangeValue value-changed event'
Assert-Contains $control 'throw new InvalidOperationException\("EtherProgressBar is read-only\."\)' 'EtherProgressBar automation SetValue rejection'
Assert-Contains $control 'OwnerControl\.Title is string title' 'EtherProgressBar automation-name title fallback'

[xml]$xaml = Get-Content -LiteralPath $xamlPath -Raw
$styles = @($xaml.SelectNodes("//*[local-name()='Style']"))
$keyedStyle = @($styles | Where-Object { $_.GetAttribute('Key', $xamlNamespace) -eq 'DefaultEtherProgressBarStyle' })[0]
if ($null -eq $keyedStyle) {
    throw 'EtherProgressBar is missing keyed DefaultEtherProgressBarStyle.'
}
$implicitStyle = @($styles | Where-Object {
    -not $_.HasAttribute('Key', $xamlNamespace) -and $_.GetAttribute('BasedOn') -match 'DefaultEtherProgressBarStyle'
})[0]
if ($null -eq $implicitStyle) {
    throw 'EtherProgressBar is missing its implicit style BasedOn DefaultEtherProgressBarStyle.'
}
foreach ($setter in 'Minimum', 'Maximum', 'Value', 'ShowTitle', 'ShowValue') {
    if ($null -eq $keyedStyle.SelectSingleNode("./*[local-name()='Setter' and @Property='$setter']")) {
        throw "DefaultEtherProgressBarStyle is missing its $setter setter."
    }
}

$template = $keyedStyle.SelectSingleNode(".//*[local-name()='ControlTemplate']")
if ($null -eq $template) {
    throw 'DefaultEtherProgressBarStyle is missing its ControlTemplate.'
}
if ($template.OuterXml -match '#[0-9A-Fa-f]{3,8}') {
    throw 'EtherProgressBar ControlTemplate contains a literal hex color; only component resources may carry color values.'
}
foreach ($state in 'BothLabelsVisible', 'TitleOnly', 'ValueOnly', 'LabelsHidden') {
    if (@($template.SelectNodes(".//*[local-name()='VisualState']") | Where-Object {
        $_.GetAttribute('Name', $xamlNamespace) -eq $state
    }).Count -ne 1) {
        throw "EtherProgressBar template is missing LabelStates/$state."
    }
}
foreach ($themeResource in @([regex]::Matches($template.OuterXml, '\{ThemeResource\s+([^}\s]+)') | ForEach-Object { $_.Groups[1].Value })) {
    if ($themeResource -notlike 'EtherProgressBar*') {
        throw "EtherProgressBar template references non-component ThemeResource '$themeResource'."
    }
}

$themeDictionaries = @($xaml.SelectNodes("//*[local-name()='ResourceDictionary.ThemeDictionaries']/*[local-name()='ResourceDictionary']"))
$keysByTheme = @{}
foreach ($dictionary in $themeDictionaries) {
    $themeAttribute = $dictionary.GetAttributeNode('Key', $xamlNamespace)
    if ($null -eq $themeAttribute) {
        throw 'EtherProgressBar has a theme dictionary without x:Key.'
    }

    $keys = Get-KeyedResources $dictionary
    if ($keys.Count -eq 0 -or @($keys | Where-Object { $_ -notlike 'EtherProgressBar*' }).Count -gt 0) {
        throw "EtherProgressBar $($themeAttribute.Value) resources must be component-scoped EtherProgressBar keys."
    }
    $keysByTheme[$themeAttribute.Value] = $keys
}

foreach ($theme in 'Light', 'Dark', 'HighContrast') {
    if (-not $keysByTheme.ContainsKey($theme)) {
        throw "EtherProgressBar is missing its $theme component theme dictionary."
    }
    if (($keysByTheme[$theme] -join ',') -cne ($keysByTheme.Light -join ',')) {
        throw "EtherProgressBar $theme component resource keys do not match Light."
    }
}
if (($keysByTheme.Light -join ',') -cne 'EtherProgressBarFillBrush,EtherProgressBarTitleForegroundBrush,EtherProgressBarTrackBrush,EtherProgressBarValueForegroundBrush') {
    throw 'EtherProgressBar component resource keys differ from the expected lightweight key set.'
}

$highContrast = @($themeDictionaries | Where-Object { $_.GetAttribute('Key', $xamlNamespace) -eq 'HighContrast' })[0]
foreach ($resource in @($highContrast.ChildNodes | Where-Object { $_ -is [System.Xml.XmlElement] })) {
    if ($resource.OuterXml -notmatch 'SystemColor') {
        throw "EtherProgressBar HighContrast resource '$($resource.GetAttribute('Key', $xamlNamespace))' does not use a Windows SystemColor dynamic resource."
    }
}

$fixture = Get-Content -LiteralPath $fixturePath -Raw
foreach ($evidence in 'BothLabelsVisible', 'TitleOnly', 'ValueOnly', 'LabelsHidden', 'SetValueRejected', 'LightGradientColors', 'DarkGradientColors', 'LightTemplateBrushColors', 'DarkTemplateBrushColors', 'GetPattern\(PatternInterface\.RangeValue\)') {
    Assert-Contains $fixture $evidence "EtherProgressBar consumer runtime evidence for $evidence"
}

Write-Host 'EtherProgressBar contract audit passed: style/defaults, template metadata/states, component theme keys, High Contrast system resources, automation, and runtime evidence are present.'
