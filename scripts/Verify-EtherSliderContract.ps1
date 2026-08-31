[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$controlPath = Join-Path $repoRoot 'src\Ether.DesignSystem.Controls\Controls\Inputs\EtherSlider.xaml.cs'
$automationPath = Join-Path $repoRoot 'src\Ether.DesignSystem.Controls\Controls\Inputs\EtherSlider.Automation.cs'
$labelsPath = Join-Path $repoRoot 'src\Ether.DesignSystem.Controls\Controls\Inputs\EtherSlider.Labels.cs'
$xamlPath = Join-Path $repoRoot 'src\Ether.DesignSystem.Controls\Controls\Inputs\EtherSlider.xaml'
$galleryPath = Join-Path $repoRoot 'samples\Ether.DesignSystem.Gallery\Views\Controls\SliderPage.xaml'
$fixtureDirectory = Join-Path $repoRoot 'tests\Ether.DesignSystem.ConsumerFixtures'
$ciPath = Join-Path $repoRoot '.github\workflows\build.yml'
$xamlNamespace = 'http://schemas.microsoft.com/winfx/2006/xaml'
$expectedComponentKeys = 'EtherSliderHighlightBrush,EtherSliderInactiveBrush,EtherSliderKnobBrush,EtherSliderKnobPressedBrush,EtherSliderLabelForegroundBrush,EtherSliderValueForegroundBrush'
$templateParts = @('ValueText', 'BarCanvas', 'Knob', 'LabelRow', 'HighlightBrushSource', 'InactiveBrushSource', 'KnobBrushSource', 'KnobPressedBrushSource')

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
$csharp = $control + [Environment]::NewLine +
    (Get-Content -LiteralPath $automationPath -Raw) + [Environment]::NewLine +
    (Get-Content -LiteralPath $labelsPath -Raw)
Assert-Contains $control 'class EtherSlider\s*:\s*RangeBase' 'EtherSlider RangeBase conversion'
Assert-Contains $control 'DefaultStyleKey\s*=\s*typeof\(EtherSlider\)' 'EtherSlider constructor'
if ($control -notmatch '(?s)public EtherSlider\(\)\s*\{(?<ctor>.*?)\n    \}') {
    throw 'EtherSlider constructor body could not be isolated for default-setter audit.'
}
if ($Matches['ctor'] -match '(Minimum|Maximum|Value|SmallChange|LargeChange|UseSystemFocusVisuals|IsTabStop|ShowTitle|ShowLabels|Title)\s*=') {
    throw 'EtherSlider constructor must not assign its default visual setters; the keyed style owns them.'
}
foreach ($part in $templateParts) {
    Assert-Contains $control "TemplatePart\(Name = .*${part}" "EtherSlider TemplatePart contract for $part"
}
Assert-Contains $control 'GetTemplateChild\(LabelRowPart\)' 'EtherSlider tick-label row lookup'
Assert-Contains $control 'public bool SnapToStops' 'EtherSlider can snap pointer and keyboard input to stops'
Assert-Contains $control 'public DoubleCollection\? Stops' 'EtherSlider exposes a Stops collection'
Assert-Contains $control 'public bool MoveToNextStop\(\)' 'EtherSlider MoveToNextStop matches SteeringBar'
Assert-Contains $control 'public bool MoveToPreviousStop\(\)' 'EtherSlider MoveToPreviousStop matches SteeringBar'
Assert-Contains $control 'CoerceValueToStops' 'EtherSlider coerces Value onto stops when SnapToStops is enabled'
Assert-Contains $control '_keyboardFocused' 'EtherSlider uses the hover knob size as a keyboard focus cue'
Assert-Contains $control 'private const int MinBarCount = 8' 'EtherSlider internal even tick floor is 8'
Assert-Contains $control 'private const int MaxBarCount = 512' 'EtherSlider internal even tick ceiling is 512'
Assert-Contains $control 'SizeChanged' 'EtherSlider relayouts ticks when the parent width changes'
Assert-Contains $control 'ResolveEvenBarCount' 'EtherSlider keeps an even tick count'
Assert-Contains $control '_barCanvas\.Opacity\s*=\s*IsEnabled \? 1d : 0\.4d' 'EtherSlider disabled tick opacity treatment'
Assert-Contains $control '_labelRow\.Opacity\s*=\s*IsEnabled \? 1d : 0\.4d' 'EtherSlider disabled tick-label opacity treatment'
Assert-Contains $control '_valueText\.Opacity\s*=\s*IsEnabled \? 1d : 0\.4d' 'EtherSlider disabled value-label opacity treatment'
Assert-Contains $csharp 'AutomationControlType\.Slider' 'EtherSlider automation control type'
Assert-Contains $csharp 'PatternInterface\.RangeValue' 'EtherSlider RangeValue pattern'
Assert-Contains $csharp 'RangeValuePatternIdentifiers\.ValueProperty' 'EtherSlider RangeValue value-changed event'
Assert-Contains $control 'public string FormatValue\(double value\)' 'EtherSlider FormatValue public API'
Assert-Contains $control 'BindBarFill\(bar, _highlightBrushSource, "EtherSliderHighlightBrush"\)' 'EtherSlider template bars consume highlight component key'
Assert-Contains $control 'BindBarFill\(bar, _inactiveBrushSource, "EtherSliderInactiveBrush"\)' 'EtherSlider template bars consume inactive component key'
if ($control -notmatch 'new\s+Rectangle\b') {
    throw 'EtherSlider must construct even-count tick bars in code so the track can follow parent width.'
}
if ($csharp -match 'Children\.Clear\(') {
    throw 'EtherSlider must not clear BarCanvas children; generated bars are inserted before the template Knob.'
}
if ($csharp -match 'GetThemeColor\(') {
    throw 'EtherSlider must not resolve colors through GetThemeColor string lookups.'
}
if ($csharp -match 'InitializeComponent\(') {
    throw 'EtherSlider must not call InitializeComponent after the UserControl conversion.'
}

[xml]$xaml = Get-Content -LiteralPath $xamlPath -Raw
$xamlRaw = Get-Content -LiteralPath $xamlPath -Raw
$styles = @($xaml.SelectNodes("//*[local-name()='Style']"))
$keyedStyle = @($styles | Where-Object { $_.GetAttribute('Key', $xamlNamespace) -eq 'DefaultEtherSliderStyle' })[0]
if ($null -eq $keyedStyle) {
    throw 'EtherSlider is missing keyed DefaultEtherSliderStyle.'
}
$implicitStyle = @($styles | Where-Object {
    -not $_.HasAttribute('Key', $xamlNamespace) -and $_.GetAttribute('BasedOn') -match 'DefaultEtherSliderStyle'
})[0]
if ($null -eq $implicitStyle) {
    throw 'EtherSlider is missing its implicit style BasedOn DefaultEtherSliderStyle.'
}
foreach ($setter in 'IsTabStop', 'UseSystemFocusVisuals', 'HighContrastAdjustment', 'Minimum', 'Maximum', 'Value', 'SmallChange', 'LargeChange', 'ShowTitle', 'ShowLabels', 'HorizontalAlignment', 'Template') {
    if ($null -eq $keyedStyle.SelectSingleNode("./*[local-name()='Setter' and @Property='$setter']")) {
        throw "DefaultEtherSliderStyle is missing its $setter setter."
    }
}

$template = $keyedStyle.SelectSingleNode(".//*[local-name()='ControlTemplate']")
if ($null -eq $template) {
    throw 'DefaultEtherSliderStyle is missing its ControlTemplate.'
}
if ($template.OuterXml -match '#[0-9A-Fa-f]{3,8}') {
    throw 'EtherSlider ControlTemplate contains a literal hex color; only component resources may carry color values.'
}
if ($template.OuterXml -match '(Background|BorderBrush|Foreground|Stroke|Fill)="Transparent"') {
    throw 'EtherSlider ControlTemplate contains a literal Transparent color; only component resources may carry color values.'
}
if ($template.OuterXml -notmatch 'x:Name="LabelRow"') {
    throw 'EtherSlider ControlTemplate is missing the named LabelRow part.'
}
if ($template.OuterXml -match 'ColumnDefinitions') {
    throw 'EtherSlider LabelRow must not declare unused Grid columns; tick labels are positioned in one cell.'
}
$declaredLabelCount = @($template.SelectNodes(".//*[local-name()='Grid' and (@Name='LabelRow' or @*[local-name()='Name']='LabelRow')]/*[local-name()='TextBlock']")).Count
if ($declaredLabelCount -ne 11) {
    throw "EtherSlider ControlTemplate must declare 11 tick-label TextBlocks. Observed $declaredLabelCount."
}
if ($template.OuterXml -notmatch 'x:Name="Knob"') {
    throw 'EtherSlider ControlTemplate is missing the named Knob part.'
}
$declaredBarCount = @($template.SelectNodes(".//*[local-name()='Rectangle' and @Height='40']")).Count
if ($declaredBarCount -ne 0) {
    throw "EtherSlider ControlTemplate must not declare Height=40 tick bars; they are generated to an even count. Observed $declaredBarCount."
}
if ($xamlRaw -notmatch 'Property="HorizontalAlignment"\s+Value="Stretch"') {
    throw 'DefaultEtherSliderStyle must stretch with the parent container.'
}
foreach ($themeResource in @([regex]::Matches($template.OuterXml, '\{ThemeResource\s+([^}\s]+)') | ForEach-Object { $_.Groups[1].Value })) {
    if ($themeResource -notlike 'EtherSlider*') {
        throw "EtherSlider template references non-component ThemeResource '$themeResource'."
    }
}

$themeDictionaries = @($xaml.SelectNodes("//*[local-name()='ResourceDictionary.ThemeDictionaries']/*[local-name()='ResourceDictionary']"))
$keysByTheme = @{}
foreach ($dictionary in $themeDictionaries) {
    $themeAttribute = $dictionary.GetAttributeNode('Key', $xamlNamespace)
    if ($null -eq $themeAttribute) {
        throw 'EtherSlider has a theme dictionary without x:Key.'
    }

    $keys = Get-KeyedResources $dictionary
    if ($keys.Count -eq 0 -or @($keys | Where-Object { $_ -notlike 'EtherSlider*' }).Count -gt 0) {
        throw "EtherSlider $($themeAttribute.Value) resources must be component-scoped EtherSlider keys."
    }
    $keysByTheme[$themeAttribute.Value] = $keys
}

foreach ($theme in 'Light', 'Dark', 'HighContrast') {
    if (-not $keysByTheme.ContainsKey($theme)) {
        throw "EtherSlider is missing its $theme component theme dictionary."
    }
    if (($keysByTheme[$theme] -join ',') -cne ($keysByTheme.Light -join ',')) {
        throw "EtherSlider $theme component resource keys do not match Light."
    }
}
if (($keysByTheme.Light -join ',') -cne $expectedComponentKeys) {
    throw 'EtherSlider component resource keys differ from the expected lightweight key set.'
}

Assert-Contains $xamlRaw 'Color="\{StaticResource AlphaBrandGlow65\}"' 'EtherSlider highlight ticks bind AlphaBrandGlow65'
Assert-Contains $xamlRaw 'Color="\{StaticResource Blue600\}"' 'EtherSlider Light knob binds Blue600'
Assert-Contains $xamlRaw 'Color="\{StaticResource Blue500\}"' 'EtherSlider Dark knob binds Blue500'
Assert-Contains $xamlRaw 'Color="\{StaticResource Gray300\}"' 'EtherSlider Light unselected ticks bind Gray300'
Assert-Contains $xamlRaw 'Color="\{StaticResource Gray600\}"' 'EtherSlider Dark unselected ticks bind Gray600'
Assert-Contains $xamlRaw 'Color="\{StaticResource Gray1000\}"' 'EtherSlider Light title binds Gray1000'
Assert-Contains $xamlRaw 'FontFamily="\{StaticResource InstrumentSans\}"' 'EtherSlider title FontFamily is Instrument Sans'
Assert-Contains $xamlRaw 'FontWeight="\{StaticResource WeightSemibold\}"' 'EtherSlider title FontWeight is WeightSemibold'
Assert-Contains $xamlRaw 'Color="\{StaticResource AlphaBlack70\}"' 'EtherSlider Light tick labels bind AlphaBlack70'
Assert-Contains $xamlRaw 'Color="\{StaticResource AlphaWhite70\}"' 'EtherSlider Dark tick labels bind AlphaWhite70'
Assert-Contains $xamlRaw 'Value="\{StaticResource InterFont\}"' 'EtherSlider tick labels use Inter'
Assert-Contains $xamlRaw 'Value="\{StaticResource WeightMedium\}"' 'EtherSlider tick labels use WeightMedium'
Assert-Contains $xamlRaw 'Height="18"' 'EtherSlider title height matches Figma 18'
Assert-Contains $xamlRaw 'Margin="0,0,0,4"' 'EtherSlider title-to-track gap matches Figma 4'
if ($xamlRaw -match 'ActionPrimaryBg|BackgroundTrack|text/primary') {
    throw 'EtherSlider Light/Dark fills must bind primitives, not semantic aliases or literal hex.'
}

$highContrast = @($themeDictionaries | Where-Object { $_.GetAttribute('Key', $xamlNamespace) -eq 'HighContrast' })[0]
foreach ($resource in @($highContrast.ChildNodes | Where-Object { $_ -is [System.Xml.XmlElement] })) {
    if ($resource.OuterXml -notmatch 'SystemColor') {
        throw "EtherSlider HighContrast resource '$($resource.GetAttribute('Key', $xamlNamespace))' does not use a Windows SystemColor dynamic resource."
    }
}

$gallery = Get-Content -LiteralPath $galleryPath -Raw
foreach ($name in 'HasDisabledToggle="True"', 'Interactive slider', 'Interactive slider with named stops', 'Slider at 25 percent', 'Slider at 75 percent', 'Slider title only', 'Slider labels only', 'Slider track only', 'Slider named labels', 'Slider with named labels', 'Slider with explicit stops', 'Slider custom range', 'ExampleHorizontalContentAlignment="Stretch"') {
    Assert-Contains $gallery ([regex]::Escape($name)) "EtherSlider gallery contract '$name'"
}

$fixture = (Get-ChildItem -Path $fixtureDirectory -Filter 'RuntimeVerification*.cs' -File | Get-Content -Raw) -join [Environment]::NewLine
foreach ($evidence in 'DefaultEtherSliderStyle', 'GetPattern\(PatternInterface\.RangeValue\)', 'SliderVerification', 'slider = result\?\.Slider', 'DisabledLocksAutomation', 'DisabledOpacityApplied', 'SetValueAccepted', 'SetValueNoOpAccepted', 'NamedLabelsApplied', 'SnapCoerced', 'MoveToStopExercised') {
    Assert-Contains $fixture $evidence "EtherSlider consumer runtime evidence for $evidence"
}

$ci = Get-Content -LiteralPath $ciPath -Raw
Assert-Contains $ci 'Verify-EtherSliderContract\.ps1' 'CI wiring for EtherSlider contract verifier'

Write-Host 'EtherSlider contract audit passed: RangeBase conversion, style/defaults, template metadata, component theme keys, High Contrast system resources, gallery automation names, RangeValue GetPattern evidence, FormatValue, and CI wiring are present.'
