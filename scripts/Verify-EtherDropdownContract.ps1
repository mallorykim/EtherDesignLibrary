[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$controlPath = Join-Path $repoRoot 'src\Ether.DesignSystem.Controls\Controls\Inputs\EtherDropdown.cs'
$xamlPath = Join-Path $repoRoot 'src\Ether.DesignSystem.Controls\Controls\Inputs\EtherDropdown.xaml'
$fixtureDirectory = Join-Path $repoRoot 'tests\Ether.DesignSystem.ConsumerFixtures'
$ciPath = Join-Path $repoRoot '.github\workflows\build.yml'
$xamlNamespace = 'http://schemas.microsoft.com/winfx/2006/xaml'
$expectedComponentKeys = 'EtherDropdownActiveStrokeBrush,EtherDropdownFillDefaultBrush,EtherDropdownFillHoverBrush,EtherDropdownFillPressedBrush,EtherDropdownFocusBrush,EtherDropdownForegroundBrush,EtherDropdownItemSelectedBrush,EtherDropdownItemSelectedHoverBrush,EtherDropdownItemSelectedPressedBrush,EtherDropdownMenuBackgroundBrush'
$templateParts = @('TriggerText', 'Arrow', 'Popup', 'PopupBorder', 'ScrollViewer')

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
Assert-Contains $control 'DefaultStyleKey\s*=\s*typeof\(EtherDropdown\)' 'EtherDropdown constructor'
if ($control -notmatch '(?s)public EtherDropdown\(\)\s*\{(?<ctor>.*?)\n    \}') {
    throw 'EtherDropdown constructor body could not be isolated for default-setter audit.'
}
if ($Matches['ctor'] -match '(MaxVisibleItems|MenuGap|FontSize|Padding|MinWidth|MinHeight|UseSystemFocusVisuals)\s*=') {
    throw 'EtherDropdown constructor must not assign its default visual setters; the keyed style owns them.'
}
foreach ($part in $templateParts) {
    Assert-Contains $control "TemplatePart\(Name = .*${part}" "EtherDropdown TemplatePart contract for $part"
}
foreach ($state in 'Normal', 'PointerOver', 'Pressed', 'Disabled') {
    Assert-Contains $control "TemplateVisualState\(GroupName = CommonStatesGroup, Name = ${state}State\)" "EtherDropdown TemplateVisualState contract for CommonStates/$state"
}
foreach ($state in 'Focused', 'Unfocused') {
    Assert-Contains $control "TemplateVisualState\(GroupName = FocusStatesGroup, Name = ${state}State\)" "EtherDropdown TemplateVisualState contract for FocusStates/$state"
}
foreach ($state in 'Opened', 'Closed') {
    Assert-Contains $control "TemplateVisualState\(GroupName = DropDownStatesGroup, Name = ${state}State\)" "EtherDropdown TemplateVisualState contract for DropDownStates/$state"
}

[xml]$xaml = Get-Content -LiteralPath $xamlPath -Raw
$styles = @($xaml.SelectNodes("//*[local-name()='Style']"))
$keyedStyle = @($styles | Where-Object { $_.GetAttribute('Key', $xamlNamespace) -eq 'DefaultEtherDropdownStyle' })[0]
if ($null -eq $keyedStyle) {
    throw 'EtherDropdown is missing keyed DefaultEtherDropdownStyle.'
}
$implicitStyle = @($styles | Where-Object {
    -not $_.HasAttribute('Key', $xamlNamespace) -and $_.GetAttribute('BasedOn') -match 'DefaultEtherDropdownStyle'
})[0]
if ($null -eq $implicitStyle) {
    throw 'EtherDropdown is missing its implicit style BasedOn DefaultEtherDropdownStyle.'
}
$aliasStyle = @($styles | Where-Object { $_.GetAttribute('Key', $xamlNamespace) -eq 'EtherDropdown' })[0]
if ($null -eq $aliasStyle -or $aliasStyle.GetAttribute('BasedOn') -notmatch 'DefaultEtherDropdownStyle') {
    throw 'EtherDropdown is missing its keyed EtherDropdown alias BasedOn DefaultEtherDropdownStyle.'
}
$itemStyle = @($styles | Where-Object { $_.GetAttribute('Key', $xamlNamespace) -eq 'EtherDropdownItem' })[0]
if ($null -eq $itemStyle) {
    throw 'EtherDropdown is missing keyed EtherDropdownItem ComboBoxItem style.'
}
foreach ($setter in 'Padding', 'MinWidth', 'MinHeight', 'FontFamily', 'FontSize', 'MaxVisibleItems', 'MenuGap', 'ItemContainerStyle', 'ItemsPanel', 'UseSystemFocusVisuals', 'HighContrastAdjustment', 'Template') {
    if ($null -eq $keyedStyle.SelectSingleNode("./*[local-name()='Setter' and @Property='$setter']")) {
        throw "DefaultEtherDropdownStyle is missing its $setter setter."
    }
}

$templates = @($xaml.SelectNodes("//*[local-name()='ControlTemplate']"))
if ($templates.Count -lt 2) {
    throw 'EtherDropdown is missing its control and item ControlTemplates.'
}
$dropdownTemplate = @($templates | Where-Object { $_.GetAttribute('Key', $xamlNamespace) -eq 'EtherDropdownTemplate' })[0]
if ($null -eq $dropdownTemplate) {
    throw 'EtherDropdown is missing keyed EtherDropdownTemplate.'
}
foreach ($template in $templates) {
    if ($template.OuterXml -match '#[0-9A-Fa-f]{3,8}') {
        throw 'EtherDropdown ControlTemplate contains a literal hex color; only component resources may carry color values.'
    }
    if ($template.OuterXml -match '(Background|BorderBrush|Foreground|Stroke|Fill)="Transparent"') {
        throw 'EtherDropdown ControlTemplate contains a literal Transparent color; only component resources may carry color values.'
    }
    foreach ($themeResource in @([regex]::Matches($template.OuterXml, '\{ThemeResource\s+([^}\s]+)') | ForEach-Object { $_.Groups[1].Value })) {
        if ($themeResource -notlike 'EtherDropdown*') {
            throw "EtherDropdown template references non-component ThemeResource '$themeResource'."
        }
    }
}

foreach ($state in 'Normal', 'PointerOver', 'Pressed', 'Disabled') {
    if (@($dropdownTemplate.SelectNodes(".//*[local-name()='VisualState']") | Where-Object {
        $_.GetAttribute('Name', $xamlNamespace) -eq $state
    }).Count -ne 1) {
        throw "EtherDropdown template is missing visual state $state."
    }
}
foreach ($state in 'Focused', 'Unfocused', 'Opened', 'Closed') {
    if (@($dropdownTemplate.SelectNodes(".//*[local-name()='VisualState']") | Where-Object {
        $_.GetAttribute('Name', $xamlNamespace) -eq $state
    }).Count -ne 1) {
        throw "EtherDropdown template is missing visual state $state."
    }
}

$themeDictionaries = @($xaml.SelectNodes("//*[local-name()='ResourceDictionary.ThemeDictionaries']/*[local-name()='ResourceDictionary']"))
$keysByTheme = @{}
foreach ($dictionary in $themeDictionaries) {
    $themeAttribute = $dictionary.GetAttributeNode('Key', $xamlNamespace)
    if ($null -eq $themeAttribute) {
        throw 'EtherDropdown has a theme dictionary without x:Key.'
    }

    $keys = Get-KeyedResources $dictionary
    if ($keys.Count -eq 0 -or @($keys | Where-Object { $_ -notlike 'EtherDropdown*' }).Count -gt 0) {
        throw "EtherDropdown $($themeAttribute.Value) resources must be component-scoped EtherDropdown keys."
    }
    $keysByTheme[$themeAttribute.Value] = $keys
}

foreach ($theme in 'Light', 'Dark', 'HighContrast') {
    if (-not $keysByTheme.ContainsKey($theme)) {
        throw "EtherDropdown is missing its $theme component theme dictionary."
    }
    if (($keysByTheme[$theme] -join ',') -cne ($keysByTheme.Light -join ',')) {
        throw "EtherDropdown $theme component resource keys do not match Light."
    }
}
if (($keysByTheme.Light -join ',') -cne $expectedComponentKeys) {
    throw 'EtherDropdown component resource keys differ from the expected lightweight key set.'
}

$highContrast = @($themeDictionaries | Where-Object { $_.GetAttribute('Key', $xamlNamespace) -eq 'HighContrast' })[0]
foreach ($resource in @($highContrast.ChildNodes | Where-Object { $_ -is [System.Xml.XmlElement] })) {
    if ($resource.OuterXml -notmatch 'SystemColor') {
        throw "EtherDropdown HighContrast resource '$($resource.GetAttribute('Key', $xamlNamespace))' does not use a Windows SystemColor dynamic resource."
    }
}

$fixture = (Get-ChildItem -Path $fixtureDirectory -Filter 'RuntimeVerification*.cs' -File | Get-Content -Raw) -join [Environment]::NewLine
foreach ($evidence in 'DefaultEtherDropdownStyle', 'Opened', 'Closed', 'DropdownVerification', 'dropdown = result\?\.Dropdown', 'PopupBorder', 'ScrollViewer', 'popup\.IsOpen') {
    Assert-Contains $fixture $evidence "EtherDropdown consumer runtime evidence for $evidence"
}

$ci = Get-Content -LiteralPath $ciPath -Raw
Assert-Contains $ci 'Verify-EtherDropdownContract\.ps1' 'CI wiring for EtherDropdown contract verifier'

Write-Host 'EtherDropdown contract audit passed: style/defaults, template metadata/states, component theme keys, High Contrast system resources, and runtime evidence are present.'
