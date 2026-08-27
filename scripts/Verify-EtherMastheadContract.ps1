[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$controlPath = Join-Path $repoRoot 'src\Controls\Navigation\EtherMasthead.xaml.cs'
$xamlPath = Join-Path $repoRoot 'src\Controls\Navigation\EtherMasthead.xaml'
$galleryPath = Join-Path $repoRoot 'src\Views\Navigation\MastheadPage.xaml'
$fixturePath = Join-Path $repoRoot 'tests\Ether.DesignSystem.ConsumerFixtures\RuntimeVerification.cs'
$ciPath = Join-Path $repoRoot '.github\workflows\build.yml'
$controlsProjectPath = Join-Path $repoRoot 'src\Ether.DesignSystem.Controls\Ether.DesignSystem.Controls.csproj'
$galleryProjectPath = Join-Path $repoRoot 'samples\Ether.DesignSystem.Gallery\Ether.DesignSystem.Gallery.csproj'
$xamlNamespace = 'http://schemas.microsoft.com/winfx/2006/xaml'
$expectedComponentKeys = 'EtherMastheadCaptionHoverBrush,EtherMastheadCaptionPressedBrush,EtherMastheadFocusBrush,EtherMastheadIconForegroundBrush'
$templateParts = @('MenuIconSlot', 'SearchIconSlot', 'SettingsButton', 'ChevronSlot', 'MinimizeButton', 'MaximizeRestoreButton', 'CloseButton', 'MaximizeIcon', 'RestoreIcon')
$visualStates = @('SettingsVisible', 'SettingsCollapsed', 'SearchVisible', 'SearchCollapsed', 'MenuVisible', 'MenuCollapsed', 'ChevronVisible', 'ChevronCollapsed', 'WindowRestored', 'WindowMaximized')

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
Assert-Contains $control 'class EtherMasthead\s*:\s*Control' 'EtherMasthead Control conversion'
Assert-Contains $control 'DefaultStyleKey\s*=\s*typeof\(EtherMasthead\)' 'EtherMasthead constructor'
if ($control -notmatch '(?s)public EtherMasthead\(\)\s*\{(?<ctor>.*?)\n    \}') {
    throw 'EtherMasthead constructor body could not be isolated for default-setter audit.'
}
if ($Matches['ctor'] -match '(UseSystemFocusVisuals|Background|HorizontalAlignment|ShowSettings|ShowSearch)\s*=') {
    throw 'EtherMasthead constructor must not assign its default visual setters; the keyed style and dependency-property metadata own them.'
}
foreach ($part in $templateParts) {
    Assert-Contains $control "TemplatePart\(Name = .*${part}" "EtherMasthead TemplatePart contract for $part"
}
foreach ($state in $visualStates) {
    Assert-Contains $control "TemplateVisualState\(GroupName = .*StatesGroup, Name = ${state}State\)" "EtherMasthead TemplateVisualState contract for $state"
}
Assert-Contains $control 'VisualStateManager\.GoToState\(this, .*SearchVisibleState' 'EtherMasthead search icon state transition'
Assert-Contains $control 'ContentIslandEnvironment' 'EtherMasthead packable AppWindow host resolution'
Assert-Contains $control 'AppWindow\.GetFromWindowId' 'EtherMasthead AppWindow chrome wiring'
Assert-Contains $control 'GetHostAppWindow\(\)\?\.Destroy\(\)' 'EtherMasthead Close uses AppWindow.Destroy rather than sandbox App.MainWindow'
if ($control -match 'App\.MainWindow') {
    throw 'EtherMasthead must not couple window chrome to sandbox App.MainWindow.'
}
if ($control -match 'InitializeComponent\(') {
    throw 'EtherMasthead must not call InitializeComponent after the UserControl conversion.'
}

[xml]$xaml = Get-Content -LiteralPath $xamlPath -Raw
$styles = @($xaml.SelectNodes("//*[local-name()='Style']"))
$keyedStyle = @($styles | Where-Object { $_.GetAttribute('Key', $xamlNamespace) -eq 'DefaultEtherMastheadStyle' })[0]
if ($null -eq $keyedStyle) {
    throw 'EtherMasthead is missing keyed DefaultEtherMastheadStyle.'
}
$implicitStyle = @($styles | Where-Object {
    -not $_.HasAttribute('Key', $xamlNamespace) -and $_.GetAttribute('BasedOn') -match 'DefaultEtherMastheadStyle'
})[0]
if ($null -eq $implicitStyle) {
    throw 'EtherMasthead is missing its implicit style BasedOn DefaultEtherMastheadStyle.'
}
foreach ($setter in 'UseSystemFocusVisuals', 'Background', 'HorizontalAlignment', 'Template') {
    if ($null -eq $keyedStyle.SelectSingleNode("./*[local-name()='Setter' and @Property='$setter']")) {
        throw "DefaultEtherMastheadStyle is missing its $setter setter."
    }
}

$templates = @($xaml.SelectNodes("//*[local-name()='ControlTemplate']"))
if ($templates.Count -lt 2) {
    throw 'EtherMasthead is missing its masthead ControlTemplate or caption-button ControlTemplate.'
}
foreach ($template in $templates) {
    if ($template.OuterXml -match '#[0-9A-Fa-f]{3,8}') {
        throw 'EtherMasthead ControlTemplate contains a literal hex color; only component resources may carry color values.'
    }
    foreach ($themeResource in @([regex]::Matches($template.OuterXml, '\{ThemeResource\s+([^}\s]+)') | ForEach-Object { $_.Groups[1].Value })) {
        if ($themeResource -notlike 'EtherMasthead*') {
            throw "EtherMasthead template references non-component ThemeResource '$themeResource'."
        }
    }
}

$mastheadTemplate = $keyedStyle.SelectSingleNode(".//*[local-name()='ControlTemplate']")
if ($null -eq $mastheadTemplate) {
    throw 'DefaultEtherMastheadStyle is missing its ControlTemplate.'
}
foreach ($part in $templateParts) {
    if ($mastheadTemplate.OuterXml -notmatch "x:Name=`"$part`"") {
        throw "EtherMasthead template is missing named part '$part'."
    }
}
foreach ($state in $visualStates) {
    if ($mastheadTemplate.OuterXml -notmatch "Name=`"$state`"") {
        throw "EtherMasthead template is missing VisualState '$state'."
    }
}

$themeDictionaries = @($xaml.SelectNodes("//*[local-name()='ResourceDictionary.ThemeDictionaries']/*[local-name()='ResourceDictionary']"))
$keysByTheme = @{}
foreach ($dictionary in $themeDictionaries) {
    $themeAttribute = $dictionary.GetAttributeNode('Key', $xamlNamespace)
    if ($null -eq $themeAttribute) {
        throw 'EtherMasthead has a theme dictionary without x:Key.'
    }

    $keys = Get-KeyedResources $dictionary
    if ($keys.Count -eq 0 -or @($keys | Where-Object { $_ -notlike 'EtherMasthead*' }).Count -gt 0) {
        throw "EtherMasthead $($themeAttribute.Value) resources must be component-scoped EtherMasthead keys."
    }
    $keysByTheme[$themeAttribute.Value] = $keys
}

foreach ($theme in 'Light', 'Dark', 'HighContrast') {
    if (-not $keysByTheme.ContainsKey($theme)) {
        throw "EtherMasthead is missing its $theme component theme dictionary."
    }
    if (($keysByTheme[$theme] -join ',') -cne ($keysByTheme.Light -join ',')) {
        throw "EtherMasthead $theme component resource keys do not match Light."
    }
}
if (($keysByTheme.Light -join ',') -cne $expectedComponentKeys) {
    throw 'EtherMasthead component resource keys differ from the expected lightweight key set.'
}

$highContrast = @($themeDictionaries | Where-Object { $_.GetAttribute('Key', $xamlNamespace) -eq 'HighContrast' })[0]
foreach ($resource in @($highContrast.ChildNodes | Where-Object { $_ -is [System.Xml.XmlElement] })) {
    if ($resource.OuterXml -notmatch 'SystemColor') {
        throw "EtherMasthead HighContrast resource '$($resource.GetAttribute('Key', $xamlNamespace))' does not use a Windows SystemColor dynamic resource."
    }
}

$gallery = Get-Content -LiteralPath $galleryPath -Raw
foreach ($name in 'Default masthead', 'Maximized masthead') {
    Assert-Contains $gallery ([regex]::Escape($name)) "EtherMasthead gallery automation name '$name'"
}

$fixture = Get-Content -LiteralPath $fixturePath -Raw
foreach ($evidence in 'DefaultEtherMastheadStyle', 'MastheadVerification', 'masthead = result\?\.Masthead', 'SearchVisible', 'SearchIconSlot') {
    Assert-Contains $fixture $evidence "EtherMasthead consumer runtime evidence for $evidence"
}

$ci = Get-Content -LiteralPath $ciPath -Raw
Assert-Contains $ci 'Verify-EtherMastheadContract\.ps1' 'CI wiring for EtherMasthead contract verifier'

$controlsProject = Get-Content -LiteralPath $controlsProjectPath -Raw
if ($controlsProject -match 'EtherMasthead\.xaml') {
    throw 'EtherMasthead must be included in the Controls project glob, not listed as an Exclude.'
}
$galleryProject = Get-Content -LiteralPath $galleryProjectPath -Raw
if ($galleryProject -match 'EtherMasthead') {
    throw 'Gallery must consume EtherMasthead from the Controls package project, not compile a sandbox copy.'
}

Write-Host 'EtherMasthead contract audit passed: Control conversion, packable AppWindow chrome, style/defaults, template metadata/states, component theme keys, High Contrast system resources, gallery automation names, optional-icon runtime evidence, Controls packaging, and CI wiring are present.'
