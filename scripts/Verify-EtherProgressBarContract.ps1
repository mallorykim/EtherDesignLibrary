[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$controlPath = Join-Path $repoRoot 'src\Ether.DesignSystem.Controls\Controls\Inputs\EtherProgressBar.cs'
$xamlPath = Join-Path $repoRoot 'src\Ether.DesignSystem.Controls\Controls\Inputs\EtherProgressBar.xaml'
$fixtureDirectory = Join-Path $repoRoot 'tests\Ether.DesignSystem.ConsumerFixtures'
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

foreach ($part in 'LayoutRoot', 'FillColumn', 'RestColumn', 'LabelRow', 'TitleText', 'ValueLabel') {
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
Assert-Contains $control 'AutomationRangeValueChanged' 'EtherProgressBar in-process RangeValue Value subscription'
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
foreach ($setter in 'Minimum', 'Maximum', 'Value', 'ShowTitle', 'ShowValue', 'IsTabStop', 'UseSystemFocusVisuals', 'HighContrastAdjustment', 'Padding') {
    if ($null -eq $keyedStyle.SelectSingleNode("./*[local-name()='Setter' and @Property='$setter']")) {
        throw "DefaultEtherProgressBarStyle is missing its $setter setter."
    }
}

$template = $keyedStyle.SelectSingleNode(".//*[local-name()='ControlTemplate']")
if ($null -eq $template) {
    throw 'DefaultEtherProgressBarStyle is missing its ControlTemplate.'
}
if ($template.OuterXml -notmatch 'x:Name="LayoutRoot"') {
    throw 'EtherProgressBar template is missing named part LayoutRoot.'
}
if ($template.OuterXml -notmatch 'Padding="\{TemplateBinding Padding\}"') {
    throw 'EtherProgressBar LayoutRoot must template-bind Padding so the style owns the 4/12 epx inset.'
}
if ($template.OuterXml -notmatch 'CornerRadius="\{StaticResource RadiusXs\}"') {
    throw 'EtherProgressBar track and fill must use primitive RadiusXs (2) instead of a literal corner radius.'
}
if ($template.OuterXml -notmatch 'ProgressTrack[\s\S]*AccessibilityView="Raw"') {
    throw 'EtherProgressBar track chrome must be AccessibilityView=Raw so RangeValue stays on the control.'
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

$xamlText = Get-Content -LiteralPath $xamlPath -Raw
if ($xamlText -notmatch '62110:22813' -or $xamlText -notmatch '62120:1374') {
    throw 'EtherProgressBar must cite Figma Light 62110:22813 and Dark 62120:1374.'
}

$lightDictionary = @($themeDictionaries | Where-Object { $_.GetAttribute('Key', $xamlNamespace) -eq 'Light' })[0]
$darkDictionary = @($themeDictionaries | Where-Object { $_.GetAttribute('Key', $xamlNamespace) -eq 'Dark' })[0]
if ($lightDictionary.OuterXml -match 'background/track|background/progress-fill|background/Progress-fill|text/primary|text/secondary' -or
    $darkDictionary.OuterXml -match 'background/track|background/progress-fill|background/Progress-fill|text/primary|text/secondary') {
    throw 'EtherProgressBar Light/Dark must bind primitives, not semantic track/fill/text aliases.'
}
function Get-KeyedResourceXml {
    param([System.Xml.XmlElement]$Dictionary, [string]$Key)

    return @($Dictionary.ChildNodes | Where-Object {
        $_ -is [System.Xml.XmlElement] -and $_.GetAttribute('Key', $xamlNamespace) -eq $Key
    })[0]
}

$lightFill = Get-KeyedResourceXml $lightDictionary 'EtherProgressBarFillBrush'
$darkFill = Get-KeyedResourceXml $darkDictionary 'EtherProgressBarFillBrush'
$lightTrack = Get-KeyedResourceXml $lightDictionary 'EtherProgressBarTrackBrush'
$darkTrack = Get-KeyedResourceXml $darkDictionary 'EtherProgressBarTrackBrush'
$lightTitle = Get-KeyedResourceXml $lightDictionary 'EtherProgressBarTitleForegroundBrush'
$darkTitle = Get-KeyedResourceXml $darkDictionary 'EtherProgressBarTitleForegroundBrush'
$lightValue = Get-KeyedResourceXml $lightDictionary 'EtherProgressBarValueForegroundBrush'
$darkValue = Get-KeyedResourceXml $darkDictionary 'EtherProgressBarValueForegroundBrush'
if ($null -eq $lightFill -or $lightFill.LocalName -ne 'SolidColorBrush' -or $lightFill.OuterXml -notmatch 'Blue700') {
    throw 'EtherProgressBar Light fill must be a solid Blue700 primitive, not a navy-to-cyan gradient.'
}
if ($null -eq $darkFill -or $darkFill.LocalName -ne 'SolidColorBrush' -or $darkFill.OuterXml -notmatch 'Blue400') {
    throw 'EtherProgressBar Dark fill must be a solid Blue400 primitive, not a navy-to-cyan gradient.'
}
if ($null -eq $lightTrack -or $lightTrack.OuterXml -notmatch 'Gray75') {
    throw 'EtherProgressBar Light track must bind primitive Gray75.'
}
if ($null -eq $darkTrack -or $darkTrack.OuterXml -notmatch 'Gray700') {
    throw 'EtherProgressBar Dark track must bind primitive Gray700.'
}
if ($null -eq $lightTitle -or $lightTitle.OuterXml -notmatch 'Gray1000') {
    throw 'EtherProgressBar Light title must bind primitive Gray1000.'
}
if ($null -eq $darkTitle -or $darkTitle.OuterXml -notmatch 'Gray0') {
    throw 'EtherProgressBar Dark title must bind primitive Gray0.'
}
if ($null -eq $lightValue -or $lightValue.OuterXml -notmatch 'AlphaBlack70') {
    throw 'EtherProgressBar Light value must bind primitive AlphaBlack70.'
}
if ($null -eq $darkValue -or $darkValue.OuterXml -notmatch 'Gray0') {
    throw 'EtherProgressBar Dark value must bind primitive Gray0.'
}

$highContrast = @($themeDictionaries | Where-Object { $_.GetAttribute('Key', $xamlNamespace) -eq 'HighContrast' })[0]
foreach ($resource in @($highContrast.ChildNodes | Where-Object { $_ -is [System.Xml.XmlElement] })) {
    if ($resource.OuterXml -notmatch 'SystemColor') {
        throw "EtherProgressBar HighContrast resource '$($resource.GetAttribute('Key', $xamlNamespace))' does not use a Windows SystemColor dynamic resource."
    }
}

$fixture = (Get-ChildItem -Path $fixtureDirectory -Filter 'RuntimeVerification*.cs' -File | Get-Content -Raw) -join [Environment]::NewLine
foreach ($evidence in 'BothLabelsVisible', 'TitleOnly', 'ValueOnly', 'LabelsHidden', 'SetValueRejected', 'LightFillColors', 'DarkFillColors', 'LightTemplateBrushColors', 'DarkTemplateBrushColors', 'GetPattern\(PatternInterface\.RangeValue\)', 'valuePropertyChangedSubscribed', 'RangeValuePatternIdentifiers\.ValueProperty') {
    Assert-Contains $fixture $evidence "EtherProgressBar consumer runtime evidence for $evidence"
}

Write-Host 'EtherProgressBar contract audit passed: style/defaults, template metadata/states, component theme keys, High Contrast system resources, automation, and runtime evidence are present.'
