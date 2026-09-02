[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$controlPath = Join-Path $repoRoot 'src\Ether.DesignSystem.Controls\Controls\Inputs\EtherSwitch.xaml.cs'
$xamlPath = Join-Path $repoRoot 'src\Ether.DesignSystem.Controls\Controls\Inputs\EtherSwitch.xaml'
$galleryPath = Join-Path $repoRoot 'samples\Ether.DesignSystem.Gallery\Views\Controls\ToggleSwitchPage.xaml'
$fixtureDirectory = Join-Path $repoRoot 'tests\Ether.DesignSystem.ConsumerFixtures'
$ciPath = Join-Path $repoRoot '.github\workflows\build.yml'
$xamlNamespace = 'http://schemas.microsoft.com/winfx/2006/xaml'
$expectedComponentKeys = 'EtherSwitchFocusBrush,EtherSwitchKnobDisabledBrush,EtherSwitchKnobDisabledStrokeBrush,EtherSwitchKnobFillBrush,EtherSwitchKnobFillHoverBrush,EtherSwitchKnobFillOnBrush,EtherSwitchKnobFillOnHoverBrush,EtherSwitchKnobFillOnPressedBrush,EtherSwitchKnobFillPressedBrush,EtherSwitchKnobShadow1Brush,EtherSwitchKnobShadow2Brush,EtherSwitchKnobShadow3Brush,EtherSwitchKnobSheenBrush,EtherSwitchKnobStrokeBrush,EtherSwitchKnobStrokeHoverBrush,EtherSwitchKnobStrokePressedBrush,EtherSwitchLabelForegroundBrush,EtherSwitchOffTrackBrush,EtherSwitchOnTrackBrush,EtherSwitchOnTrackGlowFillBrush,EtherSwitchOnTrackGlowStrokeBrush'

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
Assert-Contains $control 'class EtherSwitchResources\s*:\s*ResourceDictionary' 'EtherSwitch resources ResourceDictionary contract'
if ($control -match 'DefaultStyleKey') {
    throw 'EtherSwitch must stay a keyed ResourceDictionary style contract; do not introduce DefaultStyleKey.'
}
if ($control -match 'class EtherSwitchResources\s*:\s*(Control|ToggleSwitch)') {
    throw 'EtherSwitch must not become a custom ToggleSwitch subclass.'
}

[xml]$xaml = Get-Content -LiteralPath $xamlPath -Raw
$styles = @($xaml.SelectNodes("//*[local-name()='Style']"))
$keyedStyle = @($styles | Where-Object { $_.GetAttribute('Key', $xamlNamespace) -eq 'EtherSwitch' })[0]
if ($null -eq $keyedStyle) {
    throw 'EtherSwitch is missing keyed Style x:Key="EtherSwitch".'
}
if ($keyedStyle.GetAttribute('TargetType') -ne 'ToggleSwitch') {
    throw 'Keyed EtherSwitch must target the stock ToggleSwitch type.'
}
$implicitToggle = @($styles | Where-Object {
    -not $_.HasAttribute('Key', $xamlNamespace) -and $_.GetAttribute('TargetType') -eq 'ToggleSwitch'
})
if ($implicitToggle.Count -ne 0) {
    throw 'EtherSwitch must stay keyed; an implicit ToggleSwitch style would restyle every bare ToggleSwitch.'
}
foreach ($setter in 'UseSystemFocusVisuals', 'HighContrastAdjustment', 'Template', 'MinHeight') {
    if ($null -eq $keyedStyle.SelectSingleNode("./*[local-name()='Setter' and @Property='$setter']")) {
        throw "EtherSwitch style is missing its $setter setter."
    }
}

$rootTransparent = @($xaml.DocumentElement.ChildNodes | Where-Object {
    $_ -is [System.Xml.XmlElement] -and
    $_.LocalName -eq 'SolidColorBrush' -and
    $_.GetAttribute('Key', $xamlNamespace) -eq 'EtherSwitchTransparentBrush'
})[0]
if ($null -eq $rootTransparent) {
    throw 'EtherSwitch is missing its unthemed EtherSwitchTransparentBrush for structural overlay fills.'
}

$templates = @($xaml.SelectNodes("//*[local-name()='ControlTemplate']"))
if ($templates.Count -lt 1) {
    throw 'EtherSwitch is missing EtherSwitchTemplate.'
}
$namedTemplate = @($templates | Where-Object { $_.GetAttribute('Key', $xamlNamespace) -eq 'EtherSwitchTemplate' })[0]
if ($null -eq $namedTemplate) {
    throw 'EtherSwitch is missing keyed EtherSwitchTemplate.'
}
if ($namedTemplate.OuterXml -notmatch 'x:Name="LayoutRoot"') {
    throw 'EtherSwitch template is missing named part LayoutRoot.'
}
if ($namedTemplate.OuterXml -notmatch 'x:Name="Dragging"') {
    throw 'EtherSwitch ToggleStates must include empty Dragging so stock ToggleSwitch capture does not miss a state.'
}
if ($namedTemplate.OuterXml -match 'Background="Transparent"') {
    throw 'EtherSwitch template must use EtherSwitchTransparentBrush for structural fills so High Contrast dictionaries stay SystemColor-only.'
}
foreach ($template in $templates) {
    if ($template.OuterXml -match '#[0-9A-Fa-f]{3,8}') {
        throw 'EtherSwitch ControlTemplate contains a literal hex color; only component resources may carry color values.'
    }
    foreach ($themeResource in @([regex]::Matches($template.OuterXml, '\{ThemeResource\s+([^}\s]+)') | ForEach-Object { $_.Groups[1].Value })) {
        if ($themeResource -notlike 'EtherSwitch*') {
            throw "EtherSwitch template references non-component ThemeResource '$themeResource'."
        }
    }
}

$themeDictionaries = @($xaml.SelectNodes("//*[local-name()='ResourceDictionary.ThemeDictionaries']/*[local-name()='ResourceDictionary']"))
$keysByTheme = @{}
foreach ($dictionary in $themeDictionaries) {
    $themeAttribute = $dictionary.GetAttributeNode('Key', $xamlNamespace)
    if ($null -eq $themeAttribute) {
        throw 'EtherSwitch has a theme dictionary without x:Key.'
    }

    $keys = Get-KeyedResources $dictionary
    if ($keys.Count -eq 0 -or @($keys | Where-Object { $_ -notlike 'EtherSwitch*' }).Count -gt 0) {
        throw "EtherSwitch $($themeAttribute.Value) resources must be component-scoped EtherSwitch keys."
    }
    $keysByTheme[$themeAttribute.Value] = $keys
}

foreach ($theme in 'Light', 'Dark', 'HighContrast') {
    if (-not $keysByTheme.ContainsKey($theme)) {
        throw "EtherSwitch is missing its $theme component theme dictionary."
    }
    if (($keysByTheme[$theme] -join ',') -cne ($keysByTheme.Light -join ',')) {
        throw "EtherSwitch $theme component resource keys do not match Light."
    }
}
if (($keysByTheme.Light -join ',') -cne $expectedComponentKeys) {
    throw 'EtherSwitch component resource keys differ from the expected lightweight key set.'
}

$xamlText = Get-Content -LiteralPath $xamlPath -Raw
if ($xamlText -notmatch '62138:28454') {
    throw 'EtherSwitch must cite Figma Light Off/Default node 62138:28454.'
}
if ($xamlText -match 'BackgroundSwitchOff|BackgroundSwitchKnobDisabled|TextSecondary') {
    throw 'EtherSwitch Light/Dark must bind primitives, not orphan switch aliases or TextSecondary.'
}

$lightDictionary = @($themeDictionaries | Where-Object { $_.GetAttribute('Key', $xamlNamespace) -eq 'Light' })[0]
$darkDictionary = @($themeDictionaries | Where-Object { $_.GetAttribute('Key', $xamlNamespace) -eq 'Dark' })[0]
function Get-KeyedResourceXml {
    param([System.Xml.XmlElement]$Dictionary, [string]$Key)

    return @($Dictionary.ChildNodes | Where-Object {
        $_ -is [System.Xml.XmlElement] -and $_.GetAttribute('Key', $xamlNamespace) -eq $Key
    })[0]
}

$lightOffTrack = Get-KeyedResourceXml $lightDictionary 'EtherSwitchOffTrackBrush'
$darkOffTrack = Get-KeyedResourceXml $darkDictionary 'EtherSwitchOffTrackBrush'
$lightLabel = Get-KeyedResourceXml $lightDictionary 'EtherSwitchLabelForegroundBrush'
$darkLabel = Get-KeyedResourceXml $darkDictionary 'EtherSwitchLabelForegroundBrush'
if ($null -eq $lightOffTrack -or $lightOffTrack.OuterXml -notmatch 'AlphaBlack10') {
    throw 'EtherSwitch Light off-track must bind primitive AlphaBlack10.'
}
if ($null -eq $darkOffTrack -or $darkOffTrack.OuterXml -notmatch 'AlphaWhite10') {
    throw 'EtherSwitch Dark off-track must bind primitive AlphaWhite10.'
}
if ($null -eq $lightLabel -or $lightLabel.OuterXml -notmatch 'AlphaBlack70') {
    throw 'EtherSwitch Light label must bind primitive AlphaBlack70.'
}
if ($null -eq $darkLabel -or $darkLabel.OuterXml -notmatch 'AlphaWhite70') {
    throw 'EtherSwitch Dark label must bind primitive AlphaWhite70.'
}
$darkDisabled = Get-KeyedResourceXml $darkDictionary 'EtherSwitchKnobDisabledBrush'
$darkDisabledStroke = Get-KeyedResourceXml $darkDictionary 'EtherSwitchKnobDisabledStrokeBrush'
$darkStroke = Get-KeyedResourceXml $darkDictionary 'EtherSwitchKnobStrokeBrush'
$darkFill = Get-KeyedResourceXml $darkDictionary 'EtherSwitchKnobFillBrush'
if ($null -eq $darkDisabled -or $darkDisabled.OuterXml -notmatch 'Gray500') {
    throw 'EtherSwitch Dark Disabled knob fill must bind primitive Gray500.'
}
if ($null -eq $darkDisabledStroke -or $darkDisabledStroke.OuterXml -notmatch 'Gray500') {
    throw 'EtherSwitch Dark Disabled knob stroke must bind primitive Gray500 so the disc is not a near-white ring.'
}
if ($null -eq $darkStroke -or $darkStroke.OuterXml -notmatch 'Gray0') {
    throw 'EtherSwitch Dark enabled knob stroke must stay Gray0; only Disabled is dimmed.'
}
if ($null -eq $darkFill -or $darkFill.OuterXml -notmatch 'Gray25') {
    throw 'EtherSwitch Dark enabled Off knob fill must stay Gray25; only Disabled is dimmed.'
}

# Skeleton-faithful WinUI ToggleSwitch part names (commit 4cdfe3e rebuilt on the official
# template): the Off/On content presenters live in LabelsHost (Grid.Column 0) and the knob
# area is SwitchAreaGrid (Grid.Column 2), so the labels sit to the left of the switch.
$offLabelIndex = $xamlText.IndexOf('x:Name="OffContentPresenter"')
$onLabelIndex = $xamlText.IndexOf('x:Name="OnContentPresenter"')
$switchAreaIndex = $xamlText.IndexOf('x:Name="SwitchAreaGrid"')
if ($offLabelIndex -lt 0 -or $onLabelIndex -lt 0 -or $switchAreaIndex -lt 0 -or
    $offLabelIndex -gt $switchAreaIndex -or $onLabelIndex -gt $switchAreaIndex) {
    throw 'EtherSwitch OffContentPresenter/OnContentPresenter must sit to the left of SwitchAreaGrid.'
}
if ($namedTemplate.OuterXml -notmatch 'Width="22"' -or $namedTemplate.OuterXml -notmatch 'Height="14"') {
    throw 'EtherSwitch knob host must be 22x14 so the 2 px stroke sits outside the 18x10 fill.'
}
if ($namedTemplate.OuterXml -notmatch 'Width="18"' -or $namedTemplate.OuterXml -notmatch 'Height="10"') {
    throw 'EtherSwitch knob fill must stay 18x10.'
}
if ($namedTemplate.OuterXml -notmatch 'Margin="3,0,0,0"') {
    throw 'EtherSwitch knob host margin must be 3 so Off/On keep 3 px of track on each side.'
}
if ($namedTemplate.OuterXml -notmatch 'Storyboard\.TargetName="KnobTranslateTransform"[^>]*Storyboard\.TargetProperty="X"[^>]*To="12"') {
    throw 'EtherSwitch On travel must animate KnobTranslateTransform.X to 12 so the right inset matches the 3 px left host margin.'
}
if ($namedTemplate.OuterXml -notmatch 'TrackVisuals\.Opacity"\s+Value="0\.4"' -or
    $namedTemplate.OuterXml -notmatch 'LabelsHost\.Opacity"\s+Value="0\.4"') {
    throw 'EtherSwitch Disabled must fade the track and labels to 0.4 so On is not full-brightness.'
}
if ($namedTemplate.OuterXml -match 'SwitchContent\.Opacity"\s+Value="0\.') {
    throw 'EtherSwitch Disabled must not fade SwitchContent; parent opacity punches the cyan track through the On knob and tints it blue.'
}
if ($namedTemplate.OuterXml -match '<BitmapCache') {
    throw 'EtherSwitch must not BitmapCache SwitchContent; flatten-plus-parent-opacity still tints the On knob blue.'
}
if ($namedTemplate.OuterXml -notmatch 'KnobFrame\.BorderBrush"\s+Value="\{ThemeResource EtherSwitchKnobDisabledStrokeBrush\}"') {
    throw 'EtherSwitch Disabled must rebind KnobFrame.BorderBrush so Dark Disabled is not a Gray0 ring.'
}
if ($namedTemplate.OuterXml -notmatch 'SwitchKnobOff\.Fill"\s+Value="\{ThemeResource EtherSwitchKnobDisabledBrush\}"') {
    throw 'EtherSwitch Disabled Off knob must use EtherSwitchKnobDisabledBrush (Light Gray50, Dark Gray500).'
}
if ($namedTemplate.OuterXml -notmatch 'SwitchKnobOn\.Background"\s+Value="\{ThemeResource EtherSwitchKnobDisabledBrush\}"') {
    throw 'EtherSwitch Disabled On knob must use EtherSwitchKnobDisabledBrush so it stays grey instead of picking up the blue track.'
}

$highContrast = @($themeDictionaries | Where-Object { $_.GetAttribute('Key', $xamlNamespace) -eq 'HighContrast' })[0]
foreach ($resource in @($highContrast.ChildNodes | Where-Object { $_ -is [System.Xml.XmlElement] })) {
    if ($resource.OuterXml -notmatch 'SystemColor') {
        throw "EtherSwitch HighContrast resource '$($resource.GetAttribute('Key', $xamlNamespace))' does not use a Windows SystemColor dynamic resource."
    }
}

$gallery = Get-Content -LiteralPath $galleryPath -Raw
foreach ($name in 'Interactive toggle switch', 'Toggle switch off', 'Toggle switch on') {
    Assert-Contains $gallery ([regex]::Escape($name)) "EtherSwitch gallery automation name '$name'"
}

$fixture = (Get-ChildItem -Path $fixtureDirectory -Filter 'RuntimeVerification*.cs' -File | Get-Content -Raw) -join [Environment]::NewLine
foreach ($evidence in 'ToggleSwitchVerification', 'toggleSwitch = result\?\.ToggleSwitch', 'KeyedStyleResolved', 'ImplicitStyleRejected', 'EtherSwitch') {
    Assert-Contains $fixture $evidence "EtherSwitch consumer runtime evidence for $evidence"
}

$ci = Get-Content -LiteralPath $ciPath -Raw
Assert-Contains $ci 'Verify-EtherSwitchContract\.ps1' 'CI wiring for EtherSwitch contract verifier'

Write-Host 'EtherSwitch contract audit passed: keyed ResourceDictionary style, no implicit ToggleSwitch restyle, EtherSwitchTemplate, component theme keys, High Contrast system resources, gallery automation names, consumer evidence, and CI wiring are present.'
