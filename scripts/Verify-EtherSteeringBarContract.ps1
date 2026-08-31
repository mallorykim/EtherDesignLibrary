[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$controlPath = Join-Path $repoRoot 'src\Ether.DesignSystem.Controls\Controls\Inputs\EtherSteeringBar.xaml.cs'
$xamlPath = Join-Path $repoRoot 'src\Ether.DesignSystem.Controls\Controls\Inputs\EtherSteeringBar.xaml'
$galleryPath = Join-Path $repoRoot 'samples\Ether.DesignSystem.Gallery\Views\Controls\SteeringBarPage.xaml'
$fixtureDirectory = Join-Path $repoRoot 'tests\Ether.DesignSystem.ConsumerFixtures'
$ciPath = Join-Path $repoRoot '.github\workflows\build.yml'
$xamlNamespace = 'http://schemas.microsoft.com/winfx/2006/xaml'
$expectedComponentKeys = 'EtherSteeringBarFillBrush,EtherSteeringBarStopMarkerActiveBrush,EtherSteeringBarStopMarkerDisabledActiveBrush,EtherSteeringBarStopMarkerDisabledInactiveBrush,EtherSteeringBarStopMarkerInactiveBrush,EtherSteeringBarThumbBorderBrush,EtherSteeringBarThumbDisabledFillBrush,EtherSteeringBarThumbFillBrush,EtherSteeringBarThumbHighlightBrush,EtherSteeringBarThumbShadowFarBrush,EtherSteeringBarThumbShadowNearBrush,EtherSteeringBarThumbSheenBrush,EtherSteeringBarThumbTopHighlightBrush,EtherSteeringBarTitleForegroundBrush,EtherSteeringBarTrackBrush,EtherSteeringBarValueForegroundBrush'
$templateParts = @('LayoutRoot', 'InteractionSurface', 'TrackBackground', 'FillBorder', 'StopMarkersHost', 'ThumbHost', 'ThumbBorder', 'ThumbFill', 'ThumbDisabledShell', 'ShadowFar', 'ShadowNear', 'LabelRow', 'TitleText', 'ValueLabel')

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
Assert-Contains $control 'DefaultStyleKey\s*=\s*typeof\(EtherSteeringBar\)' 'EtherSteeringBar constructor'
if ($control -notmatch '(?s)public EtherSteeringBar\(\)\s*\{(?<ctor>.*?)\n    \}') {
    throw 'EtherSteeringBar constructor body could not be isolated for default-setter audit.'
}
if ($Matches['ctor'] -match '(Minimum|Maximum|Value|ShowTitle|ShowValue|UseSystemFocusVisuals|IsTabStop)\s*=') {
    throw 'EtherSteeringBar constructor must not assign its default visual setters; the keyed style owns them.'
}
foreach ($part in $templateParts) {
    Assert-Contains $control "TemplatePart\(Name = .*${part}" "EtherSteeringBar TemplatePart contract for $part"
}
foreach ($state in 'BothLabelsVisible', 'TitleOnly', 'ValueOnly', 'LabelsHidden') {
    Assert-Contains $control "TemplateVisualState\(GroupName = LabelStatesGroup, Name = ${state}State\)" "EtherSteeringBar TemplateVisualState contract for $state"
}
Assert-Contains $control 'VisualStateManager\.GoToState\(this, state, false\)' 'EtherSteeringBar label state transition'
if ($control -match '\.Visibility\s*=') {
    throw 'EtherSteeringBar must drive label visibility with VisualStateManager, not direct Visibility assignments.'
}
Assert-Contains $control 'internal SteeringBarPreviewStatus PreviewStatus' 'EtherSteeringBar Gallery-only PreviewStatus support'
Assert-Contains $control 'AutomationControlType\.Slider' 'EtherSteeringBar automation control type'
Assert-Contains $control 'PatternInterface\.RangeValue' 'EtherSteeringBar RangeValue pattern'
Assert-Contains $control 'RangeValuePatternIdentifiers\.ValueProperty' 'EtherSteeringBar RangeValue value-changed event'
Assert-Contains $control 'Bindings and UI Automation write through the dependency-property system' 'EtherSteeringBar binding-path normalization rationale'
Assert-Contains $control 'var candidate = e\.Property == ValueProperty' 'EtherSteeringBar Value dependency-property normalization'
Assert-Contains $control '_normalizingValue' 'EtherSteeringBar Value normalization reentrancy guard'

[xml]$xaml = Get-Content -LiteralPath $xamlPath -Raw
$styles = @($xaml.SelectNodes("//*[local-name()='Style']"))
$keyedStyle = @($styles | Where-Object { $_.GetAttribute('Key', $xamlNamespace) -eq 'DefaultEtherSteeringBarStyle' })[0]
if ($null -eq $keyedStyle) {
    throw 'EtherSteeringBar is missing keyed DefaultEtherSteeringBarStyle.'
}
$implicitStyle = @($styles | Where-Object {
    -not $_.HasAttribute('Key', $xamlNamespace) -and $_.GetAttribute('BasedOn') -match 'DefaultEtherSteeringBarStyle'
})[0]
if ($null -eq $implicitStyle) {
    throw 'EtherSteeringBar is missing its implicit style BasedOn DefaultEtherSteeringBarStyle.'
}
foreach ($setter in 'IsTabStop', 'UseSystemFocusVisuals', 'HighContrastAdjustment', 'HorizontalAlignment', 'Minimum', 'Maximum', 'Value', 'SmallChange', 'LargeChange', 'ShowTitle', 'ShowValue', 'Padding', 'Template') {
    if ($null -eq $keyedStyle.SelectSingleNode("./*[local-name()='Setter' and @Property='$setter']")) {
        throw "DefaultEtherSteeringBarStyle is missing its $setter setter."
    }
}

$template = $keyedStyle.SelectSingleNode(".//*[local-name()='ControlTemplate']")
if ($null -eq $template) {
    throw 'DefaultEtherSteeringBarStyle is missing its ControlTemplate.'
}
if ($template.OuterXml -notmatch 'x:Name="LayoutRoot"') {
    throw 'EtherSteeringBar template is missing named part LayoutRoot.'
}
if ($template.OuterXml -notmatch 'Padding="\{TemplateBinding Padding\}"') {
    throw 'EtherSteeringBar LayoutRoot must template-bind Padding so the style owns the 0,0,0,6 shadow bleed.'
}
if ($template.OuterXml -notmatch 'InteractionSurface[\s\S]*AccessibilityView="Raw"') {
    throw 'EtherSteeringBar track chrome must be AccessibilityView=Raw so RangeValue stays on the control.'
}
if ($template.OuterXml -notmatch 'EtherSteeringBarTransparentBrush') {
    throw 'EtherSteeringBar InteractionSurface must use the unthemed transparent brush like EtherSlider.'
}
if ($template.OuterXml -notmatch 'x:Name="TrackBackground"' -or $template.OuterXml -notmatch 'Height="6"') {
    throw 'EtherSteeringBar track must be 6 epx tall.'
}
if ($control -notmatch 'TrackHeight = 6') {
    throw 'EtherSteeringBar code must keep fill height at 6 epx with the painted track.'
}
$trackBackground = @($template.SelectNodes(".//*[local-name()='Border']") | Where-Object {
    $_.GetAttribute('Name', $xamlNamespace) -eq 'TrackBackground'
})[0]
$fillBorder = @($template.SelectNodes(".//*[local-name()='Border']") | Where-Object {
    $_.GetAttribute('Name', $xamlNamespace) -eq 'FillBorder'
})[0]
if ($null -eq $trackBackground -or $trackBackground.GetAttribute('CornerRadius') -ne '3') {
    throw 'EtherSteeringBar track CornerRadius must be 3 (half of the 6 epx height).'
}
if ($null -eq $fillBorder -or $fillBorder.GetAttribute('CornerRadius') -ne '3') {
    throw 'EtherSteeringBar fill CornerRadius must be 3 (half of the 6 epx height).'
}
if ($template.OuterXml -match '#[0-9A-Fa-f]{3,8}') {
    throw 'EtherSteeringBar ControlTemplate contains a literal hex color; only component resources may carry color values.'
}
if ($template.OuterXml -match 'Translation=') {
    throw 'EtherSteeringBar must not fake thumb shadows with translated solid Borders.'
}
if ($control -notmatch 'AttachedCardShadow') {
    throw 'EtherSteeringBar must attach a visible card shadow to the thumb.'
}
$thumbBorder = @($template.SelectNodes(".//*[local-name()='Border']") | Where-Object {
    $_.GetAttribute('Name', $xamlNamespace) -eq 'ThumbBorder'
})[0]
if ($null -eq $thumbBorder -or $thumbBorder.GetAttribute('BorderThickness') -ne '0.8') {
    throw 'EtherSteeringBar knob stroke must be 0.8 epx.'
}
if ($control -notmatch 'IsPointerOverThumb') {
    throw 'EtherSteeringBar hover chrome must track the thumb, not the whole interaction strip.'
}
if ($control -notmatch 'thumbTop = \(surfaceHeight - thumbHeight\) / 2d') {
    throw 'EtherSteeringBar must vertically center the thumb on the painted track.'
}
if ($control -notmatch 'MeasureFillWidth') {
    throw 'EtherSteeringBar must keep a color seat under the acrylic thumb at minimum.'
}
if ($control -notmatch 'thumbWidth / 2d') {
    throw 'EtherSteeringBar minimum color seat must be half the thumb width.'
}
foreach ($state in 'BothLabelsVisible', 'TitleOnly', 'ValueOnly', 'LabelsHidden') {
    if (@($template.SelectNodes(".//*[local-name()='VisualState']") | Where-Object {
        $_.GetAttribute('Name', $xamlNamespace) -eq $state
    }).Count -ne 1) {
        throw "EtherSteeringBar template is missing LabelStates/$state."
    }
}
foreach ($themeResource in @([regex]::Matches($template.OuterXml, '\{ThemeResource\s+([^}\s]+)') | ForEach-Object { $_.Groups[1].Value })) {
    if ($themeResource -notlike 'EtherSteeringBar*') {
        throw "EtherSteeringBar template references non-component ThemeResource '$themeResource'."
    }
}

$themeDictionaries = @($xaml.SelectNodes("//*[local-name()='ResourceDictionary.ThemeDictionaries']/*[local-name()='ResourceDictionary']"))
$keysByTheme = @{}
foreach ($dictionary in $themeDictionaries) {
    $themeAttribute = $dictionary.GetAttributeNode('Key', $xamlNamespace)
    if ($null -eq $themeAttribute) {
        throw 'EtherSteeringBar has a theme dictionary without x:Key.'
    }

    $keys = Get-KeyedResources $dictionary
    if ($keys.Count -eq 0 -or @($keys | Where-Object { $_ -notlike 'EtherSteeringBar*' }).Count -gt 0) {
        throw "EtherSteeringBar $($themeAttribute.Value) resources must be component-scoped EtherSteeringBar keys."
    }
    $keysByTheme[$themeAttribute.Value] = $keys
}

foreach ($theme in 'Light', 'Dark', 'HighContrast') {
    if (-not $keysByTheme.ContainsKey($theme)) {
        throw "EtherSteeringBar is missing its $theme component theme dictionary."
    }
    if (($keysByTheme[$theme] -join ',') -cne ($keysByTheme.Light -join ',')) {
        throw "EtherSteeringBar $theme component resource keys do not match Light."
    }
}
if (($keysByTheme.Light -join ',') -cne $expectedComponentKeys) {
    throw 'EtherSteeringBar component resource keys differ from the expected lightweight key set.'
}

$xamlText = Get-Content -LiteralPath $xamlPath -Raw
if ($xamlText -notmatch '62128:25881' -or $xamlText -notmatch '62134:28164') {
    throw 'EtherSteeringBar must cite Figma Light 62128:25881 and Dark 62134:28164.'
}

$lightDictionary = @($themeDictionaries | Where-Object { $_.GetAttribute('Key', $xamlNamespace) -eq 'Light' })[0]
$darkDictionary = @($themeDictionaries | Where-Object { $_.GetAttribute('Key', $xamlNamespace) -eq 'Dark' })[0]
if ($lightDictionary.OuterXml -match 'background/track|text/primary|TextSecondary' -or
    $darkDictionary.OuterXml -match 'background/track|text/primary|TextSecondary') {
    throw 'EtherSteeringBar Light/Dark must bind primitives, not semantic track/text aliases.'
}

function Get-KeyedResourceXml {
    param([System.Xml.XmlElement]$Dictionary, [string]$Key)

    return @($Dictionary.ChildNodes | Where-Object {
        $_ -is [System.Xml.XmlElement] -and $_.GetAttribute('Key', $xamlNamespace) -eq $Key
    })[0]
}

$lightTrack = Get-KeyedResourceXml $lightDictionary 'EtherSteeringBarTrackBrush'
$darkTrack = Get-KeyedResourceXml $darkDictionary 'EtherSteeringBarTrackBrush'
$lightTitle = Get-KeyedResourceXml $lightDictionary 'EtherSteeringBarTitleForegroundBrush'
$darkTitle = Get-KeyedResourceXml $darkDictionary 'EtherSteeringBarTitleForegroundBrush'
$lightKnob = Get-KeyedResourceXml $lightDictionary 'EtherSteeringBarThumbBorderBrush'
$darkKnob = Get-KeyedResourceXml $darkDictionary 'EtherSteeringBarThumbBorderBrush'
if ($null -eq $lightTrack -or $lightTrack.OuterXml -notmatch 'Gray200') {
    throw 'EtherSteeringBar Light track must bind primitive Gray200.'
}
if ($null -eq $darkTrack -or $darkTrack.OuterXml -notmatch 'Gray600') {
    throw 'EtherSteeringBar Dark track must bind primitive Gray600.'
}
if ($null -eq $lightTitle -or $lightTitle.OuterXml -notmatch 'Gray1000') {
    throw 'EtherSteeringBar Light title must bind primitive Gray1000.'
}
if ($null -eq $darkTitle -or $darkTitle.OuterXml -notmatch 'Gray0') {
    throw 'EtherSteeringBar Dark title must bind primitive Gray0.'
}
if ($null -eq $lightKnob -or $lightKnob.OuterXml -notmatch 'Gray0') {
    throw 'EtherSteeringBar Light knob stroke must bind primitive Gray0.'
}
if ($null -eq $darkKnob -or $darkKnob.OuterXml -notmatch 'Gray600') {
    throw 'EtherSteeringBar Dark knob stroke must bind primitive Gray600.'
}
$lightDisabledFill = Get-KeyedResourceXml $lightDictionary 'EtherSteeringBarThumbDisabledFillBrush'
if ($null -eq $lightDisabledFill -or $lightDisabledFill.OuterXml -notmatch 'Gray200') {
    throw 'EtherSteeringBar Light disabled knob fill must bind primitive Gray200 so it remains visible on a white surface.'
}
$lightFill = Get-KeyedResourceXml $lightDictionary 'EtherSteeringBarThumbFillBrush'
$darkFill = Get-KeyedResourceXml $darkDictionary 'EtherSteeringBarThumbFillBrush'
if ($null -eq $lightFill -or $lightFill.LocalName -ne 'AcrylicBrush' -or $lightFill.OuterXml -notmatch 'BlurAmount="6"') {
    throw 'EtherSteeringBar Light knob fill must be an AcrylicBrush with Figma blur 6.'
}
if ($null -eq $darkFill -or $darkFill.LocalName -ne 'AcrylicBrush' -or $darkFill.OuterXml -notmatch 'BlurAmount="6"') {
    throw 'EtherSteeringBar Dark knob fill must be an AcrylicBrush with Figma blur 6.'
}

$highContrast = @($themeDictionaries | Where-Object { $_.GetAttribute('Key', $xamlNamespace) -eq 'HighContrast' })[0]
foreach ($resource in @($highContrast.ChildNodes | Where-Object { $_ -is [System.Xml.XmlElement] })) {
    if ($resource.OuterXml -notmatch 'SystemColor') {
        throw "EtherSteeringBar HighContrast resource '$($resource.GetAttribute('Key', $xamlNamespace))' does not use a Windows SystemColor dynamic resource."
    }
}

$gallery = Get-Content -LiteralPath $galleryPath -Raw
foreach ($name in 'Interactive steering bar', 'Interactive steering bar with stops', 'Default steering bar', 'Hover steering bar', 'Pressed steering bar', 'Disabled steering bar') {
    Assert-Contains $gallery ([regex]::Escape($name)) "EtherSteeringBar gallery automation name '$name'"
}

$fixture = (Get-ChildItem -Path $fixtureDirectory -Filter 'RuntimeVerification*.cs' -File | Get-Content -Raw) -join [Environment]::NewLine
foreach ($evidence in 'DefaultEtherSteeringBarStyle', 'GetPattern\(PatternInterface\.RangeValue\)', 'SteeringBarVerification', 'steeringBar = result\?\.SteeringBar', 'DisabledLocksAutomation', 'SetValueAccepted') {
    Assert-Contains $fixture $evidence "EtherSteeringBar consumer runtime evidence for $evidence"
}

$ci = Get-Content -LiteralPath $ciPath -Raw
Assert-Contains $ci 'Verify-EtherSteeringBarContract\.ps1' 'CI wiring for EtherSteeringBar contract verifier'

Write-Host 'EtherSteeringBar contract audit passed: style/defaults, template metadata/states, component theme keys, High Contrast system resources, gallery automation names, Gallery-only preview support, RangeValue GetPattern evidence, and CI wiring are present.'
